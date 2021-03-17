// Thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
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
        static Vector2 scrollPosition;
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

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnUnload = Unload;
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
        static void UpdateSearchResults()
        {
            if (blueprints == null)
            {
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

            GL.Label("Combat");
            GL.BeginHorizontal();
            if (GL.Button("Full Buff Please", GL.Width(300f)))
            {
                CheatsCombat.FullBuffPlease("");
            }

            if (GL.Button("Full Buff Please", GL.Width(300f))) {
                CheatsCombat.FullBuffPlease("");
            }
            if (GL.Button("Kill All Enemies", GL.Width(300f))) {
                CheatsCombat.KillAll();
            }
            if (GL.Button("Summon Zoo", GL.Width(300f))) {
                CheatsCombat.SpawnInspectedEnemiesUnderCursor("");
            }
            GL.EndHorizontal();

            GL.Space(10);
            GL.Label("Unlocks");

            GL.BeginHorizontal();
            if (GL.Button("Give All Items", GL.Width(300f))) {
                CheatsUnlock.CreateAllItems("");
            }
            GL.EndHorizontal();

            GL.Space(20);
            if (selectedBlueprint != null)
            {
                GL.BeginHorizontal();
                GL.Label("Selected:", GL.ExpandWidth(false));
                GL.Space(10);
                GL.Label($"{ parameter.green().bold() }", GL.ExpandWidth(false));
                GL.Space(30);
                GL.Label($"{selectedBlueprint.GetType().Name.cyan()}", GL.ExpandWidth(false));
                GL.Label($"{selectedBlueprint}");
                GL.EndHorizontal();
            }
            GL.Space(20);
            GL.Label("Picker");
            int newSelectedBPFilter = GL.Toolbar(
                Settings.selectedBPTypeFilter, 
                blueprintTypeFilters.Select(tf => tf.name).ToArray()
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
                foreach (BlueprintScriptableObject blueprint in filteredBPs)
                {
                    Action add = null;
                    Action remove = null;
                    GL.BeginHorizontal();
                    GL.Label($"{blueprint.GetType().Name.cyan()}", GL.Width(400));
                    GL.Space(30);
                    if (GL.Button("Select", GL.ExpandWidth(false)))
                    {
                        selectedBlueprintIndex = index;
                        selectedBlueprint = blueprint;
                        parameter = blueprint.name;
                    }
                    BlueprintAction[] actions = BlueprintAction.ActionsForBlueprint(blueprint);
                    if (actions != null)
                    {
                        foreach (BlueprintAction action in actions)
                        {
                            GL.Space(10);
                            if (GL.Button(action.name, GL.ExpandWidth(false))) { action.action(blueprint); };
                        }
                    }
                    GL.Space(20);
                    GL.Label(blueprint.name.orange().bold(), GL.ExpandWidth(false));
                    GL.EndHorizontal();

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