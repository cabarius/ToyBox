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
        public static String searchText = "";
        public static String parameter = "";
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

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label("Combat");
            if (GUILayout.Button("FullBuffPlease", GUILayout.Width(300f))) {
                CheatsCombat.FullBuffPlease(parameter);
            }
            if (GUILayout.Button("KillAll", GUILayout.Width(300f))) {
                CheatsCombat.KillAll();
            }
            if (GUILayout.Button("Summon Zoo", GUILayout.Width(300f))) {
                CheatsCombat.SpawnInspectedEnemiesUnderCursor(parameter);
            }
            GUILayout.Space(10);
            GUILayout.Label("Unlocks");

            if (GUILayout.Button("Add Feature", GUILayout.Width(300f))) {
                CheatsUnlock.CheatAddFeature(parameter);
            }
            if (GUILayout.Button("Give Item", GUILayout.Width(300f))) {
                CheatsUnlock.CreateItem(parameter);
            }
            
            if (GUILayout.Button("Give All Items", GUILayout.Width(300f))) {
                CheatsUnlock.CreateAllItems("");
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Parameter", GUILayout.ExpandWidth(false));
            GUILayout.Space(10);
            parameter = GUILayout.TextField(parameter, GUILayout.Width(500f));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.Label("Picker");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.ExpandWidth(false));
            GUILayout.Space(10);
            searchText = GUILayout.TextField(searchText, GUILayout.Width(500f));
            GUILayout.Space(50);
            GUILayout.Label("Limit", GUILayout.ExpandWidth(false));

            String searchLimitString = GUILayout.TextField($"{Settings.searchLimit}");
            Int32.TryParse(searchLimitString, out Settings.searchLimit);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);


            if (searchText.Length > 0)
            {
                if (blueprints == null)
                {
                    blueprints = GetBlueprints();
                }
                BlueprintScriptableObject[] matchingBP = blueprints
                        .Where(bp => bp.name.ToLower().Contains(searchText.ToLower()))
                        .OrderBy(bp => bp.name)
                        .Take(Settings.searchLimit).ToArray();

                if (matchingBP.Length > 0)
                {
                    int selectedBlueprint = 0;
                    string[] blueprintNames = matchingBP.Select(b => b.name).ToArray();
                    GUILayout.BeginVertical("Box");
                    selectedBlueprint = GUILayout.SelectionGrid(selectedBlueprint, blueprintNames, 1);
                    if (GUILayout.Button("Start"))
                    {
                        Debug.Log($"You chose {blueprintNames[selectedBlueprint]}");

                    }
                    GUILayout.EndVertical();
                }

            }

            /* GUILayout.Space(10);
                        GUILayout.Label("MyFloatOption", GUILayout.ExpandWidth(false));
                        GUILayout.Space(10);
                        Settings.MyFloatOption = GUILayout.HorizontalSlider(Settings.MyFloatOption, 1f, 10f, GUILayout.Width(300f));
                        GUILayout.Label($" {Settings.MyFloatOption:p0}", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("MyBoolOption", GUILayout.ExpandWidth(false));
                        GUILayout.Space(10);
                        Settings.MyBoolOption = GUILayout.Toggle(Settings.MyBoolOption, $" {Settings.MyBoolOption}", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label("MyTextOption", GUILayout.ExpandWidth(false));
                        GUILayout.Space(10);
                        Settings.MyTextOption = GUILayout.TextField(Settings.MyTextOption, GUILayout.Width(300f));
                        GUILayout.EndHorizontal();
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