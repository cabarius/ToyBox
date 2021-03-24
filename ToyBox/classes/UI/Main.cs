// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
// Special thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server for teaching me how to mod Unity games
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

namespace ToyBox {
#if DEBUG
    [EnableReloading]
#endif
    static class Main {
        static Harmony HarmonyInstance;
        static string modId;
        public static Settings settings;
        public static bool Enabled;

        static Exception caughtException = null;
        static public bool userHasHitReturn = false;
        static public String focusedControlName = null;
        static bool Load(UnityModManager.ModEntry modEntry) {
            try {
#if DEBUG
                modEntry.OnUnload = Unload;
#endif
                modId = modEntry.Info.Id;
                Logger.modLogger = modEntry.Logger;
                settings = Settings.Load<Settings>(modEntry);
                Logger.modEntryPath = modEntry.Path;

                HarmonyInstance = new Harmony(modEntry.Info.Id);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
            }
            catch (Exception e) {
                Logger.Error(e);
                throw e;
            }
            return true;
        }
#if DEBUG
        static bool Unload(UnityModManager.ModEntry modEntry) {
            HarmonyInstance.UnpatchAll(modId);
            return true;
        }
#endif
        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            Enabled = value;
            return true;
        }

        static void ResetSearch() {
            BlueprintBrowser.ResetSearch();
        }

        static void ResetGUI(UnityModManager.ModEntry modEntry) {
            settings = Settings.Load<Settings>(modEntry);
            settings.searchText = "";
            settings.searchLimit = 100;
            ResetSearch();
            caughtException = null;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry) {
            try {
                Event e = Event.current;
                userHasHitReturn = (e.keyCode == KeyCode.Return);
                focusedControlName = GUI.GetNameOfFocusedControl();

                if (caughtException != null) {
                    UI.Label("ERROR".red().bold() + $": caught exception {caughtException}");
                    UI.ActionButton("Reset".orange().bold(), () => { ResetGUI(modEntry); }, UI.AutoWidth());
                    return;
                }
                GL.BeginVertical("box");
#if false
                UI.Label("focused: " 
                    + $"{GUI.GetNameOfFocusedControl()}".orange().bold() 
                    + "(" + $"{GUIUtility.keyboardControl}".cyan().bold() + ")", 
                    UI.AutoWidth());
#endif
                CheapTricks.OnGUI(modEntry);
                QuestEditor.OnGUI(modEntry);
                PartyEditor.OnGUI(modEntry);
                BlueprintBrowser.OnGUI(modEntry);
                GL.EndVertical();
            }
            catch (Exception e) {
                Console.Write($"{e}");
                caughtException = e;
            }
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
            settings.Save(modEntry);
        }
    }
}