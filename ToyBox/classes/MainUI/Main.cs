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
using ToyBox.Multiclass;
using GL = UnityEngine.GUILayout;
using ToyKit;

namespace ToyBox {
#if DEBUG
    [EnableReloading]
#endif
    static class Main {
        static Harmony HarmonyInstance;
        static string modId;
        public static UnityModManager.ModEntry modEntry = null;
        public static Settings settings;
        public static Mod multiclassMod;
        public static bool Enabled;
        public static bool freshlyLaunched = true;
        public static bool IsInGame { get { return Game.Instance.Player.Party.Any(); } }

        static Exception caughtException = null;

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
                multiclassMod = new Multiclass.Mod();
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
            CheapTricks.ResetGUI();
            LevelUp.ResetGUI();
            PartyEditor.ResetGUI();
            CharacterPicker.ResetGUI();
            BlueprintBrowser.ResetGUI();
            QuestEditor.ResetGUI();
            caughtException = null;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry) {
            Main.modEntry = modEntry;
            if (!Enabled) return;
            if (!IsInGame) {
                UI.Label("ToyBox has limited functionality from the main menu".yellow().bold());
            }
            try {
                Event e = Event.current;
                UI.userHasHitReturn = (e.keyCode == KeyCode.Return);
                UI.focusedControlName = GUI.GetNameOfFocusedControl();

                if (caughtException != null) {
                    UI.Label("ERROR".red().bold() + $": caught exception {caughtException}");
                    UI.ActionButton("Reset".orange().bold(), () => { ResetGUI(modEntry); }, UI.AutoWidth());
                    return;
                }
                UI.TabBar(ref settings.selectedTab,
                    () => {
                        if (BlueprintBrowser.GetBlueprints() == null) {
                            UI.Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.progress.ToString("P2").cyan().bold());
                        }
                        else { UI.Space(25); }
                    },
                    new NamedAction("Cheap Tricks", () => { CheapTricks.OnGUI(); }),
#if DEBUG
                    new NamedAction("Level Up & Multiclass", () => { LevelUp.OnGUI(); }),
#else
                    new NamedAction("Level Up", () => { LevelUp.OnGUI(); }),
#endif
                    new NamedAction("Party Editor", () => { PartyEditor.OnGUI(); }),
                    new NamedAction("Search 'n Pick", () => { BlueprintBrowser.OnGUI(); }),
                    new NamedAction("Quest Editor", () => { QuestEditor.OnGUI(); })
                    );
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