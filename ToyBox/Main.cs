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
        public static String searchText = "";
        public static String parameter = "";
        static Vector2 scrollPosition;
        static int selectedBlueprintIndex = -1;
        static BlueprintScriptableObject selectedBlueprint = null;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            modEntry.OnUnload = Unload;
            Settings = Settings.Load<Settings>(modEntry);
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
        static void UpdateSearchResults()
        {
            if (blueprints == null)
            {
                blueprints = GetBlueprints().Where(bp => !BlueprintActions.ignoredBluePrintTypes.Contains(bp.GetType())).ToArray();
            }
            selectedBlueprint = null;
            selectedBlueprintIndex = -1;
            if (searchText.Trim().Length == 0)
            {
                filteredBPs = null;
                filteredBPNames = null;
            }
            String[] terms = searchText.Split(' ').Select(s => s.ToLower()).ToArray();
            List<BlueprintScriptableObject> filtered = new List<BlueprintScriptableObject>();
            foreach (BlueprintScriptableObject blueprint in blueprints)
            {
                String name = blueprint.name.ToLower();
                if (terms.All(term => name.Contains(term)))
                {
                    filtered.Add(blueprint);
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered
                    .OrderBy(bp => bp.name)
                    .Take(Settings.searchLimit).OrderBy(bp => bp.name).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
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
            if (GL.Button("Add Feature", GL.Width(300f))) {
                BlueprintActions.addFact(selectedBlueprint);
            }
            if (GL.Button("Remove Feature", GL.Width(300f)))
            {
                BlueprintActions.removeFact(selectedBlueprint);
            }
            if (GL.Button("Give Item", GL.Width(300f))) {
                BlueprintActions.addItem(selectedBlueprint);
//                CheatsUnlock.CreateItem("- " + parameter);
            }
            if (GL.Button("Give All Items", GL.Width(300f))) {
                CheatsUnlock.CreateAllItems("");
            }
            GL.EndHorizontal();

            GL.BeginHorizontal();
            GL.Label("Parameter", GL.ExpandWidth(false));
            GL.Space(10);
            parameter = GL.TextField(parameter, GL.Width(500f));
            if (selectedBlueprint != null)
            {
                GL.Space(50);
                GL.Label($"{selectedBlueprint.GetType().Name}", GL.ExpandWidth(false));
            }

            GL.EndHorizontal();
            GL.Space(10);

            GL.Label("Picker");

            bool searchChanged = false;

            GL.BeginHorizontal();
            searchText = GL.TextField(searchText, GL.Width(500f));
            GL.Space(50);
            GL.Label("Limit", GL.ExpandWidth(false));
            String searchLimitString = GL.TextField($"{Settings.searchLimit}", GL.Width(500f));
            Int32.TryParse(searchLimitString, out Settings.searchLimit);
            GL.EndHorizontal();

            GL.BeginHorizontal();

            if (userHasHitReturn || GL.Button("Search", GL.ExpandWidth(false)))
            {
                UpdateSearchResults();
            }
            GL.Space(50);
            GL.Label("" + (matchCount > 0 ? 
                                        $"Matches: {matchCount}" + (
                                                        matchCount > Settings.searchLimit ? $" -> {Settings.searchLimit}" : ""
                                                   )
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
                    if (GL.Button(blueprint.name, GL.Width(500f)))
                    {
                        selectedBlueprintIndex = index;
                        selectedBlueprint = blueprint;
                    }
                    GL.Space(50);
                    GL.Label($"{blueprint.GetType().Name}", GL.Width(500f));
                    NamedAction[] actions = BlueprintActions.ActionsForBlueprint(blueprint);
                    if (actions != null)
                    {
                        GL.Space(40);
                        foreach (NamedAction action in actions)
                        {
                            GL.Space(10);
                            if (GL.Button(action.name, GL.ExpandWidth(false))) { action.action(blueprint); };
                        }
                    }
                    GL.EndHorizontal();

                    index++;
                }


            }
            GL.EndVertical();

            /* 

                //selectedBlueprintIndex = GL.SelectionGrid(selectedBlueprintIndex, filteredBPNames, 4);

                if (selectedBlueprintIndex  >= 0)
                {
                    parameter = filteredBPNames[selectedBlueprintIndex];
                    selectedBlueprint = filteredBPs[selectedBlueprintIndex];
                }                     blueprints
            
            .Where(bp => bp.name.ToLower().Contains(searchText.ToLower()))
                        .OrderBy(bp => bp.name)
                        .Take(Settings.searchLimit).ToArray();

            
            GL.Space(10);
                        GL.Label("MyFloatOption", GL.ExpandWidth(false));
                        GL.Space(10);
                        Settings.MyFloatOption = GL.HorizontalSlider(Settings.MyFloatOption, 1f, 10f, GL.Width(300f));
                        GL.Label($" {Settings.MyFloatOption:p0}", GL.ExpandWidth(false));
                        GL.EndHorizontal();

                        GL.BeginHorizontal();
                        GL.Label("MyBoolOption", GL.ExpandWidth(false));
                        GL.Space(10);
                        Settings.MyBoolOption = GL.Toggle(Settings.MyBoolOption, $" {Settings.MyBoolOption}", GL.ExpandWidth(false));
                        GL.EndHorizontal();

                        GL.BeginHorizontal();
                        GL.Label("MyTextOption", GL.ExpandWidth(false));
                        GL.Space(10);
                        Settings.MyTextOption = GL.TextField(Settings.MyTextOption, GL.Width(300f));
                        GL.EndHorizontal();
                        */
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