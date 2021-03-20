// Thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using BagOfTricks.Utils;

using GL = UnityEngine.GUILayout;

namespace ToyBox
{
#if DEBUG
    [EnableReloading]
#endif
    static class Main
    {
        public static Settings Settings;
        public static bool Enabled;
        public static BlueprintScriptableObject[] blueprints = null;
        public static BlueprintScriptableObject[] filteredBPs = null;
        public static String[] filteredBPNames = null;
        public static int matchCount = 0;
        public static String parameter = "";
        static int showStatsBitfield = 0;
        static int showBuffsBitfield = 0;
        static int showFactsBitfield = 0;
        static int showAutoBuffBitfield = 0;
        static int selectedBlueprintIndex = -1;
        static BlueprintScriptableObject selectedBlueprint = null;
        static bool searchChanged = false;

        static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter { name = "All", type = typeof(BlueprintScriptableObject) },
            new NamedTypeFilter { name = "Facts", type = typeof(BlueprintFact) },
            new NamedTypeFilter { name = "Features", type = typeof(BlueprintFeature) },
            new NamedTypeFilter { name = "Buffs", type = typeof(BlueprintBuff) },
            new NamedTypeFilter { name = "Weapons", type = typeof(BlueprintItemWeapon) },
            new NamedTypeFilter { name = "Armor", type = typeof(BlueprintItemArmor) },
            new NamedTypeFilter { name = "Shields", type = typeof(BlueprintItemShield) },
            new NamedTypeFilter { name = "Equipment", type = typeof(BlueprintItemEquipment) },
            new NamedTypeFilter { name = "Usable", type = typeof(BlueprintItemEquipmentUsable) },
        };

        static BackgroundWorker searchWorker = new BackgroundWorker();

        static bool Load(UnityModManager.ModEntry modEntry)
        {
#if DEBUG
            modEntry.OnUnload = Unload;
#endif
            Settings = Settings.Load<Settings>(modEntry);
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            //if (Settings.searchText.Length > 0) { searchChanged = true;  }
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

        static async void UpdateSearchResults()
        {
            if (blueprints == null)
            {
#if false
                List<BlueprintScriptableObject> bps = new List<BlueprintScriptableObject>();
                BlueprintScriptableObject[] allBPs = GetBlueprints();
                foreach (BlueprintScriptableObject bp in allBPs)
                {
                    bool ignoreFound = false;
                    foreach (Type t in BlueprintAction.ignoredBluePrintTypes)
                    {
                        if (bp.GetType().IsKindOf(t)) { ignoreFound = true; break; }
                    }
                    if (!ignoreFound)
                    {
                        bps.Add(bp);
                    }
                }
                blueprints = bps.ToArray();
#endif

                blueprints = GetBlueprints().Where(bp => !BlueprintAction.ignoredBluePrintTypes.Contains(bp.GetType())).ToArray();
            }
            selectedBlueprint = null;
            selectedBlueprintIndex = -1;
            if (Settings.searchText.Trim().Length == 0)
            {
                filteredBPs = null;
                filteredBPNames = null;
            }
            String[] terms = Settings.searchText.Split(' ').Select(s => s.ToLower()).ToArray();
            List<BlueprintScriptableObject> filtered = new List<BlueprintScriptableObject>();
            Type selectedType = blueprintTypeFilters[Settings.selectedBPTypeFilter].type;
            foreach (BlueprintScriptableObject blueprint in blueprints)
            {
                String name = blueprint.name.ToLower();
                Type type = blueprint.GetType();
                if (terms.All(term => name.Contains(term)) && type.IsKindOf(selectedType))
                {
                    filtered.Add(blueprint);
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered
                    .OrderBy(bp => bp.name)
                    .Take(Settings.searchLimit).OrderBy(bp => bp.name).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            searchChanged = false;
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            Event e = Event.current;
            bool userHasHitReturn = false;
            if (e.keyCode == KeyCode.Return) userHasHitReturn = true;
            GL.BeginVertical("box");

            //            scrollPosition = GL.BeginScrollView(scrollPosition, GL.ExpandWidth(true), GL.ExpandHeight(true));

            GL.Space(25);
            GL.Label("====== Cheap Tricks ======");
            GL.Space(25);
            GL.BeginHorizontal();
            GL.Label("Combat", GL.Width(150f));
            if (GL.Button("Rest All", GL.Width(300f)))
            {
                CheatsCombat.RestAll();
            }
            if (GL.Button("Empowered", GL.Width(300f)))
            {
                CheatsCombat.Empowered("");
            }
            if (GL.Button("Full Buff Please", GL.Width(300f)))
            {
                CheatsCombat.FullBuffPlease("");
            }
            if (GL.Button("Remove Death's Door", GL.Width(300f)))
            {
                CheatsCombat.DetachDebuff();
            }
            if (GL.Button("Kill All Enemies", GL.Width(300f)))
            {
                CheatsCombat.KillAll();
            }
            if (GL.Button("Summon Zoo", GL.Width(300f)))
            {
                CheatsCombat.SpawnInspectedEnemiesUnderCursor("");
            }
            GL.EndHorizontal();

            GL.Space(10);

            GL.BeginHorizontal();
            GL.Label("Common", GL.Width(150f));

            if (GL.Button("Change Weather", GL.Width(300f)))
            {
                CheatsCommon.ChangeWeather("");
            }

            if (GL.Button("Set Perception to 40", GL.Width(300f)))
            {
                CheatsCommon.StatPerception();
            }
            GL.EndHorizontal();

            GL.Space(10);

            GL.BeginHorizontal();
            GL.Label("Unlocks", GL.Width(150f));
            if (GL.Button("Give All Items", GL.Width(300f)))
            {
                CheatsUnlock.CreateAllItems("");
            }
            GL.EndHorizontal();

            GL.Space(25);
            GL.Label("====== Party Editor ======");
            GL.Space(25);
            int chIndex = 0;
            foreach (UnitEntityData ch in Game.Instance.Player.Party)
            {
                UnitProgressionData progression = ch.Descriptor.Progression;
                BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;
                int level = progression.CharacterLevel;
                int mythicLevel = progression.MythicExperience;
                GL.BeginHorizontal();

                GL.Label(ch.CharacterName.orange().bold() , GL.Width(400f));
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
                bool show = ((1 << chIndex) & showStatsBitfield) != 0;
                bool nShow = GL.Toggle( show, "Show Stats", GL.ExpandWidth(false));
                if (show != nShow) { showStatsBitfield ^= 1 << chIndex; }
#if false
                GL.Space(25);
                show = ((1 << chIndex) & showBuffsBitfield) != 0;
                nShow = GL.Toggle(show, "Show Buffs", GL.ExpandWidth(false));
                if (show != nShow) { showBuffsBitfield ^= 1 << chIndex; }
                GL.Space(25);
                show = ((1 << chIndex) & showFactsBitfield) != 0;
                nShow = GL.Toggle(show, "Show Facts", GL.ExpandWidth(false));
                if (show!= nShow) { showStatsBitfield ^= 1 << chIndex; }
                GL.Space(25);
                show = ((1 << chIndex) & showAutoBuffBitfield) != 0;
                nShow = GL.Toggle(show, "Show AutoBuffs", GL.ExpandWidth(false));
                if (show != nShow) { showAutoBuffBitfield ^= 1 << chIndex; }
#endif
                GL.EndHorizontal();
                if (((1 << chIndex) & showStatsBitfield) != 0) {
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
                chIndex += 1;
            }

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
            GL.Label("====== Search 'n Pick ======");
            GL.Space(10);
            GL.Label("(please note the first search may take a few seconds)");
            GL.Space(25);
            int newSelectedBPFilter = GL.SelectionGrid(
                Settings.selectedBPTypeFilter,
                blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                5,
                GL.ExpandWidth(false)
                );
            if (newSelectedBPFilter != Settings.selectedBPTypeFilter)
            {
                Settings.selectedBPTypeFilter = newSelectedBPFilter;
                searchChanged = true;
            }
            GL.Space(10);

            GL.BeginHorizontal();
            Settings.searchText = GL.TextField(Settings.searchText, GL.Width(500f));
            GL.Space(50);
            GL.Label("Limit", GL.ExpandWidth(false));
            String searchLimitString = GL.TextField($"{Settings.searchLimit}", GL.Width(500f));
            Int32.TryParse(searchLimitString, out Settings.searchLimit);
            if (Settings.searchLimit > 1000) { Settings.searchLimit = 1000; }
            GL.EndHorizontal();

            GL.BeginHorizontal();
            if (userHasHitReturn || searchChanged || GL.Button("Search", GL.ExpandWidth(false)))
            {
                UpdateSearchResults();
            }
            GL.Space(50);
            GL.Label((matchCount > 0
                        ? "Matches: ".green().bold() + $"{matchCount}".orange().bold()
                            + (matchCount > Settings.searchLimit
                                ? " => ".cyan() + $"{Settings.searchLimit}".cyan().bold()
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
                    if (actionCount > maxActionCount) { maxActionCount = actionCount;  }
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
                    for (int ii = 0; ii< maxActionCount; ii++) 
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
                    if (description.Length > 0) {
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

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Settings.Save(modEntry);
        }

        public static BlueprintScriptableObject[] GetBlueprints()
        {
            var bundle = (AssetBundle)AccessTools.Field(typeof(ResourcesLibrary), "s_BlueprintsBundle")
                .GetValue(null);
            return bundle.LoadAllAssets<BlueprintScriptableObject>();
        }
    }
}