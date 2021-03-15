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
using UnityEngine;

namespace ToyBox
{
    static class Main
    {

        public static Settings Settings;
        public static bool Enabled;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            Settings = Settings.Load<Settings>(modEntry);
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Enabled = value;
            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {

            GUILayout.BeginHorizontal();
            GUILayout.Label("Parameters", GUILayout.ExpandWidth(false));
            GUILayout.Space(10);
            Settings.parameterOption = GUILayout.TextField(Settings.parameterOption, GUILayout.Width(300f));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Combat");
            if (GUILayout.Button("FullBuffPlease", GUILayout.Width(300f))) {
                CheatsCombat.FullBuffPlease(Settings.parameterOption);
            }
            if (GUILayout.Button("KillAll", GUILayout.Width(300f))) {
                CheatsCombat.KillAll();
            }
            if (GUILayout.Button("Summon Zoo", GUILayout.Width(300f))) {
                CheatsCombat.SpawnInspectedEnemiesUnderCursor(Settings.parameterOption);
            }
            GUILayout.Space(10);
            GUILayout.Label("Unlocks");

            if (GUILayout.Button("Add Feature", GUILayout.Width(300f))) {
                CheatsUnlock.CheatAddFeature(Settings.parameterOption);
            }
            if (GUILayout.Button("Give Item", GUILayout.Width(300f))) {
                CheatsUnlock.CreateItem(Settings.parameterOption);
            }
            
            if (GUILayout.Button("Give All Items", GUILayout.Width(300f))) {
                CheatsUnlock.CreateAllItems("");
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
    }
}