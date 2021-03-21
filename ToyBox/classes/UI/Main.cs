// Thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

using GL = UnityEngine.GUILayout;

namespace ToyBox
{
#if DEBUG
    [EnableReloading]
#endif
    static class Main
    {
        static Harmony HarmonyInstance;
        public static Settings settings;
        public static bool Enabled;
        public static BlueprintScriptableObject[] blueprints = null;
        public static BlueprintScriptableObject[] filteredBPs = null;
        public static String[] filteredBPNames = null;
        public static int matchCount = 0;
        public static String parameter = "";
        static int showStatsBitfield = 0;
        static int showDetailsBitfield = 0;
        static int selectedBlueprintIndex = -1;
        static BlueprintScriptableObject selectedBlueprint = null;
        static bool searchChanged = false;
        static Exception caughtException = null;
        static String playerDetailsSearch = "";
        static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter("All", typeof(BlueprintScriptableObject)),
            new NamedTypeFilter("Facts",typeof(BlueprintFact)),
            new NamedTypeFilter("Features", typeof(BlueprintFeature)),
            new NamedTypeFilter("Buffs", typeof(BlueprintBuff)),
            new NamedTypeFilter("Weapons", typeof(BlueprintItemWeapon)),
            new NamedTypeFilter("Armor", typeof(BlueprintItemArmor)),
            new NamedTypeFilter("Shields", typeof(BlueprintItemShield)),
            new NamedTypeFilter("Equipment", typeof(BlueprintItemEquipment)),
            new NamedTypeFilter("Usable", typeof(BlueprintItemEquipmentUsable)),
        };

        static BackgroundWorker searchWorker = new BackgroundWorker();

        static bool Load(UnityModManager.ModEntry modEntry)
        {
#if DEBUG
            modEntry.OnUnload = Unload;
#endif
            Logger.modLogger = modEntry.Logger;
            settings = Settings.Load<Settings>(modEntry);
            Logger.modEntryPath = modEntry.Path;

            HarmonyInstance = new Harmony(modEntry.Info.Id);
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            return true;
        }
#if DEBUG
        static bool Unload(UnityModManager.ModEntry modEntry)
        {
            //            HarmonyInstance.Create(modEntry.Info.Id).UnpatchAll();
            blueprints = null;
            return true;
        }
#endif

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        static void ResetSearch()
        {
            filteredBPs = null;
            filteredBPNames = null;
        }
        static async void UpdateSearchResults()
        {
            if (blueprints == null)
            {
                blueprints = GetBlueprints().Where(bp => !BlueprintAction.ignoredBluePrintTypes.Contains(bp.GetType())).ToArray();
            }
            selectedBlueprint = null;
            selectedBlueprintIndex = -1;
            if (settings.searchText.Trim().Length == 0)
            {
                ResetSearch();
            }
            var terms = settings.searchText.Split(' ').Select(s => s.ToLower()).ToArray();
            var filtered = new List<BlueprintScriptableObject>();
            var selectedType = blueprintTypeFilters[settings.selectedBPTypeFilter].type;
            foreach (BlueprintScriptableObject blueprint in blueprints)
            {
                var name = blueprint.name.ToLower();
                var type = blueprint.GetType();
                if (terms.All(term => name.Contains(term)) && type.IsKindOf(selectedType))
                {
                    filtered.Add(blueprint);
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered
                    .OrderBy(bp => bp.name)
                    .Take(settings.searchLimit).OrderBy(bp => bp.name).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            searchChanged = false;
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                Event e = Event.current;
                bool userHasHitReturn = false;
                if (e.keyCode == KeyCode.Return) userHasHitReturn = true;

                if (caughtException != null)
                {
                    GL.Label("ERROR".red().bold() + $": caught exception {caughtException}");
                    if (GL.Button("Reset".orange().bold(), GL.ExpandWidth(false)))
                    {
                        settings = Settings.Load<Settings>(modEntry);
                        settings.searchText = "";
                        settings.searchLimit = 100;
                        ResetSearch();
                        caughtException = null;
                    }
                    return;
                }

                GL.BeginVertical("box");
                UI.Section("Cheap Tricks", () =>
                {
                UI.HStack("Combat", 4,
                    () => { UI.ActionButton("Rest All", () => { CheatsCombat.RestAll(); }); },
                    () => { UI.ActionButton("Empowered", () => { CheatsCombat.Empowered(""); }); },
                    () => { UI.ActionButton("Full Buff Please", () => { CheatsCombat.RestAll(); }); },
                    () => { UI.ActionButton("Remove Death's Door", () => { CheatsCombat.Empowered(""); }); },
                    () => { UI.ActionButton("Kill All Enemies", () => { CheatsCombat.KillAll(); }); },
                    () => { UI.ActionButton("Summon Zoo", () => { CheatsCombat.SpawnInspectedEnemiesUnderCursor(""); }); }
                 );
                UI.Space(10);
                UI.HStack("Common", 4,
                    () => { UI.ActionButton("Teleport Party To You", () => { Actions.TeleportPartyToPlayer(); }); },
                    () => { UI.ActionButton("Perception Checks", () => { Actions.RunPerceptionTriggers(); }); },
                    () => { UI.ActionButton("Set Perception to 40", () => { 
                        CheatsCommon.StatPerception(); 
                        Actions.RunPerceptionTriggers(); }); 
                    },
                    () => { UI.ActionButton("Change Weather", () => { CheatsCommon.ChangeWeather(""); }); },
                    () => { UI.ActionButton("Give All Items", () => { CheatsUnlock.CreateAllItems(""); }); }
                    );
                });
                UI.HStack("Flags", 1,
                    () => { UI.Toggle("Object Highlight Toggle Mode", ref settings.highlightObjectsToggle, GL.ExpandWidth(false)); }
                    );

                var player = Game.Instance.Player;
                var partyFilterChoices = new List<NamedFunc<List<UnitEntityData>>>()
                {
                    new NamedFunc<List<UnitEntityData>>("Party", () => player.Party),
                    new NamedFunc<List<UnitEntityData>>("Party & Pets", () => player.m_PartyAndPets),
                    new NamedFunc<List<UnitEntityData>>("All Characters", () => player.AllCharacters),
                    new NamedFunc<List<UnitEntityData>>("Active Companions", () => player.ActiveCompanions),
                    new NamedFunc<List<UnitEntityData>>("Remote Companions", () => player.m_RemoteCompanions),
                    new NamedFunc<List<UnitEntityData>>("Custom (Mercs)", PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<UnitEntityData>>("Pets",  PartyUtils.GetPets)
                };

                UI.Section("Party Editor", () =>
                {
                    UnitEntityData charToAdd = null;
                    UnitEntityData charToRemove = null;
                    var characters = UI.TypePicker<List<UnitEntityData>>(
                        null,
                        ref settings.selectedPartyFilter,
                        partyFilterChoices
                        );

                    int chIndex = 0;
                    foreach (UnitEntityData ch in characters)
                    {
                        UnitProgressionData progression = ch.Descriptor.Progression;
                        BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;
                        int level = progression.CharacterLevel;
                        int mythicLevel = progression.MythicExperience;
                        GL.BeginHorizontal();

                        GL.Label(ch.CharacterName.orange().bold(), GL.Width(400f));
                        GL.Label("level".green() + $": {level}", GL.Width(125f));
                        // Level up code adapted from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/2
                        if (progression.Experience < xpTable.GetBonus(level + 1) && level < 20)
                        {
                            if (GL.Button(" +1 Level", GL.Width(150)))
                            {
                                progression.AdvanceExperienceTo(xpTable.GetBonus(level + 1), true);
                            }
                        }
                        else if (progression.Experience >= xpTable.GetBonus(level + 1) && level < 20)
                        {
                            GL.Label("Level Up".cyan().italic(), GL.Width(150));
                        }
                        else
                        {
                            GL.Space(153);
                        }
                        GL.Space(30);
                        GL.Label($"mythic".green() + $": {mythicLevel}", GL.Width(125));
                        if (progression.MythicExperience < 10)
                        {
                            if (GL.Button(" +1 Mythic", GL.Width(150)))
                            {
                                progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                            }
                        }
                        else
                        {
                            GL.Label("Max", GL.Width(150));
                        }
                        GL.Space(25);
                        UI.DisclosureBitFieldToggle("Stats", ref showStatsBitfield, chIndex);
                        GL.Space(25);
                        UI.DisclosureBitFieldToggle("Details", ref showDetailsBitfield, chIndex);
                        GL.Space(80);
                        if (!player.PartyAndPets.Contains(ch))
                        {
                            UI.ActionButton("Add To Party", () => { charToAdd = ch; }, GL.ExpandWidth(false));
                        }
                        else if (player.ActiveCompanions.Contains(ch))
                        {
                            UI.ActionButton("Remove From Party", () => { charToRemove = ch; }, GL.ExpandWidth(false));
                        }
                        GL.EndHorizontal();

                        if (((1 << chIndex) & showStatsBitfield) != 0)
                        {
                            foreach (object obj in Enum.GetValues(typeof(StatType)))
                            {
                                StatType statType = (StatType)obj;
                                ModifiableValue modifiableValue = ch.Stats.GetStat(statType);
                                if (modifiableValue != null)
                                {
                                    GL.BeginHorizontal();
                                    GL.Space(69);   // the best number...
                                    GL.Label(statType.ToString().green().bold(), GL.Width(400f));
                                    GL.Space(25f);
                                    if (GL.Button(" < ", GL.ExpandWidth(false))) { modifiableValue.BaseValue -= 1; }
                                    GL.Space(20f);
                                    GL.Label($"{modifiableValue.BaseValue}".orange().bold(), GL.Width(50f));
                                    if (GL.Button(" > ", GL.ExpandWidth(false))) { modifiableValue.BaseValue += 1; }
                                    GL.EndHorizontal();
                                }
                            }

                        }

                        if (((1 << chIndex) & showDetailsBitfield) != 0)
                        {
                            GL.BeginHorizontal();
                            GL.Space(100);
                            playerDetailsSearch = GL.TextField(playerDetailsSearch, GL.Width(200));
                            GL.EndHorizontal();
                            FeatureCollection features = ch.Descriptor.Progression.Features;
                            EntityFact featureToRemove = null;
                            foreach (Feature fact in features)
                            {
                                String name = fact.Name;
                                if (name == null) { name = $"{fact.Blueprint.name}"; }
                                if (name != null && name.Length > 0 && (playerDetailsSearch.Length == 0 || name.Contains(playerDetailsSearch)))
                                {
                                    GL.BeginHorizontal();
                                    GL.Space(100);
                                    GL.Label($"{fact.Name}".cyan().bold(), GL.Width(400));
                                    GL.Space(30);
                                    var rank = fact.GetRank();
                                    if (GL.Button("<", GL.ExpandWidth(false))) { fact.AddRank(); }
                                    GL.Space(10f);
                                    GL.Label($"{fact.GetRank()}".orange().bold(), GL.Width(30f));
                                    if (GL.Button(">", GL.ExpandWidth(false))) { fact.RemoveRank(); }
                                    GL.Space(30);
                                    if (GL.Button("Remove", GL.Width(150)))
                                    {
                                        featureToRemove = fact;
                                    }
                                    String description = fact.Description;
                                    if (description != null)
                                    {
                                        GL.Space(30);
                                        GL.Label(description.green(), GL.ExpandWidth(false));
                                    }
                                    GL.EndHorizontal();
                                }
                            }
                            if (featureToRemove != null)
                            {
                                ch.Descriptor.Progression.Features.RemoveFact(featureToRemove);
                            }
                        }
                        chIndex += 1;
                    }
                    if (charToAdd != null) { UnitEntityDataUtils.AddCompanion(charToAdd);  }
                    if (charToRemove != null) { UnitEntityDataUtils.RemoveCompanion(charToRemove);  }
                });
                GL.Space(20);
                if (selectedBlueprint != null)
                {
                    GL.BeginHorizontal();
                    GL.Label("Selected:", GL.ExpandWidth(false));
                    GL.Space(10);
                    GL.Label($"{selectedBlueprint.GetType().Name.cyan()}", GL.ExpandWidth(false));
                    GL.Space(30);
                    GL.Label($"{selectedBlueprint}".orange().bold());
                    GL.EndHorizontal();
                }
                GL.Space(25);
                GL.Label("====== Search 'n Pick ======".bold());
                GL.Space(10);
                GL.Label("(please note the first search may take a few seconds)");
                GL.Space(25);
                int newSelectedBPFilter = GL.SelectionGrid(
                    settings.selectedBPTypeFilter,
                    blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                    5,
                    GL.ExpandWidth(false)
                    );
                if (newSelectedBPFilter != settings.selectedBPTypeFilter)
                {
                    settings.selectedBPTypeFilter = newSelectedBPFilter;
                    searchChanged = true;
                }
                GL.Space(10);

                GL.BeginHorizontal();
                settings.searchText = GL.TextField(settings.searchText, GL.Width(500f));
                GL.Space(50);
                GL.Label("Limit", GL.ExpandWidth(false));
                String searchLimitString = GL.TextField($"{settings.searchLimit}", GL.Width(500f));
                Int32.TryParse(searchLimitString, out settings.searchLimit);
                if (settings.searchLimit > 1000) { settings.searchLimit = 1000; }
                GL.EndHorizontal();

                GL.BeginHorizontal();
                if (userHasHitReturn || searchChanged || GL.Button("Search", GL.ExpandWidth(false)))
                {
                    UpdateSearchResults();
                }
                GL.Space(50);
                GL.Label((matchCount > 0
                            ? "Matches: ".green().bold() + $"{matchCount}".orange().bold()
                                + (matchCount > settings.searchLimit
                                    ? " => ".cyan() + $"{settings.searchLimit}".cyan().bold()
                                    : "")
                            : ""), GL.ExpandWidth(false));

                GL.EndHorizontal();
                GL.Space(10);

                if (filteredBPs != null)
                {
                    int index = 0;
                    int maxActionCount = 0;
                    foreach (BlueprintScriptableObject blueprint in filteredBPs)
                    {
                        BlueprintAction[] actions = BlueprintAction.ActionsForBlueprint(blueprint);
                        int actionCount = actions != null ? actions.Count() : 0;
                        if (actionCount > maxActionCount) { maxActionCount = actionCount; }
                    }
                    foreach (BlueprintScriptableObject blueprint in filteredBPs)
                    {
                        GL.BeginHorizontal();
                        GL.Label(blueprint.name.orange().bold(), GL.Width(650));
#if false
                    if (GL.Button("Select", GL.ExpandWidth(false)))
                    {
                        selectedBlueprintIndex = index;
                        selectedBlueprint = blueprint;
                        parameter = blueprint.name;
                    }
#endif
                        BlueprintAction[] actions = BlueprintAction.ActionsForBlueprint(blueprint);
                        int actionCount = actions != null ? actions.Count() : 0;
                        for (int ii = 0; ii < maxActionCount; ii++)
                        {
                            if (ii < actionCount)
                            {
                                BlueprintAction action = actions[ii];
                                if (GL.Button(action.name, GL.Width(140))) { action.action(blueprint); };
                                GL.Space(10);
                            }
                            else
                            {
                                GL.Space(154);
                            }
                        }
                        GL.Space(30);
                        GL.Label($"{blueprint.GetType().Name.cyan()}", GL.Width(400));
                        GL.EndHorizontal();
                        String description = blueprint.GetDescription();
                        if (description.Length > 0)
                        {
                            GL.BeginHorizontal();
                            GL.Space(684 + maxActionCount * 154);
                            GL.Label($"{description.green()}");
                            GL.EndHorizontal();
                        }
                        index++;
                    }
                }
                GL.EndVertical();
            }
            catch (Exception e)
            {
                Console.Write($"{e}");
                caughtException = e;
            }
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }

        public static BlueprintScriptableObject[] GetBlueprints()
        {
            var bundle = (AssetBundle)AccessTools.Field(typeof(ResourcesLibrary), "s_BlueprintsBundle")
                .GetValue(null);
            return bundle.LoadAllAssets<BlueprintScriptableObject>();
        }
    }
}