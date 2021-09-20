// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// Special thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server for teaching me how to mod Unity games
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Utility;
using Owlcat.Runtime.Core.Logging;
using ToyBox.Multiclass;
using ModKit;
using ModKit.Utility;
using ToyBox.classes.MainUI;
using Kingmaker.GameModes;

namespace ToyBox {
#if DEBUG
    [EnableReloading]
#endif
    static class Main {
        static Harmony HarmonyInstance;
        //// OwlCatMM
        //public static OwlcatModification modEntry;
        public static readonly LogChannel logger = LogChannelFactory.GetOrCreate("Respec");
        //public static UnitEntityView entityView;
        //public static void EnterPoint(OwlcatModification modification) {
        //    try {
        //        modEntry = modification;
        //        var harmony = new Harmony("Respec");
        //        harmony.PatchAll();
        //        ///modification;
        //        modification.OnGUI += OnGUI;
        //        IsEnabled = true;
        //        if (!Main.haspatched) {
        //            Main.PatchLibrary();
        //        }
        //    }
        //    catch (Exception e) {
        //        throw e;
        //    }
        //}        // UMM
        static string modId;
        public static UnityModManager.ModEntry modEntry = null;
        public static Settings settings;
        public static Mod multiclassMod;
        public static bool Enabled;
        public static bool IsModGUIShown = false;
        public static bool freshlyLaunched = true;
        public static bool IsInGame { get { return Game.Instance.Player?.Party.Any() ?? false; } }

        static Exception caughtException = null;
        public static void Log(string s) { if (modEntry != null) ModKit.Logger.Log(s); }
        public static void Log(int indent, string s) { Log("    ".Repeat(indent) + s); }
        public static void Debug(String s) { if (modEntry != null) ModKit.Logger.ModLoggerDebug(s); }
        public static void Error(Exception e) { if (modEntry != null) ModKit.Logger.Log(e); }

        static bool Load(UnityModManager.ModEntry modEntry) {
            try {
#if DEBUG
                modEntry.OnUnload = Unload;
#endif
                modId = modEntry.Info.Id;
                ModKit.Logger.modLogger = modEntry.Logger;
                settings = Settings.Load<Settings>(modEntry);
                ModKit.Logger.modEntryPath = modEntry.Path;

                HarmonyInstance = new Harmony(modEntry.Info.Id);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                modEntry.OnToggle = OnToggle;
                modEntry.OnShowGUI = OnShowGUI;
                modEntry.OnHideGUI = OnHideGUI;
                modEntry.OnGUI = OnGUI;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnSaveGUI = OnSaveGUI;
                UI.KeyBindings.OnLoad(modEntry);
                multiclassMod = new Multiclass.Mod();
            }
            catch (Exception e) {
                Main.Error(e);
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
            CrusadeEditor.ResetGUI();
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
            if (!UI.IsWide) {
                UI.Label("Note ".magenta().bold() + "ToyBox was designed to offer the best user experience at widths of 1920 or higher. Please consider increasing your resolution up of at least 1920x1080 (ideally 4k) and go to Unity Mod Manager 'Settings' tab to change the mod window width to at least 1920.  Increasing the UI scale is nice too when running at 4k".orange().bold());
            }
            try {
                UI.KeyBindings.OnGUI();
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
                        if (BlueprintLoader.Shared.IsLoading) {
                            UI.Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
                        }
                        else { UI.Space(25); }
                    },
                    new NamedAction("Bag of Tricks", () => { CheapTricks.OnGUI(); }),
#if DEBUG
                    new NamedAction("Level Up & Multiclass", () => { LevelUp.OnGUI(); }),
#else
                    new NamedAction("Level Up", () => { LevelUp.OnGUI(); }),
#endif
                    new NamedAction("Party", () => { PartyEditor.OnGUI(); }),
                    new NamedAction("Search 'n Pick", () => { BlueprintBrowser.OnGUI(); }),
                    new NamedAction("Crusade", () => { CrusadeEditor.OnGUI(); }),
                    new NamedAction("Quests", () => { QuestEditor.OnGUI(); })
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
        static void OnShowGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = true;
        }

        static void OnHideGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = false;
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float z) {
            var currentMode = Game.Instance.CurrentMode;
            if (IsModGUIShown) return;
            UI.KeyBindings.OnUpdate();
            if (Main.IsInGame
                && settings.toggleTeleportKeysEnabled
                && (currentMode == GameModeType.Default
                    || currentMode == GameModeType.Pause
                    || currentMode == GameModeType.GlobalMap
                    )
                ) {
                if (currentMode == GameModeType.GlobalMap) {
                    if (UI.KeyBindings.IsActive("TeleportParty"))
                        Teleport.TeleportPartyOnGlobalMap();
                }
                if (UI.KeyBindings.IsActive("TeleportMain"))
                    Teleport.TeleportUnit(Game.Instance.Player.MainCharacter.Value, Utils.PointerPosition());
                if (UI.KeyBindings.IsActive("TeleportSelected"))
                    Teleport.TeleportSelected();
                if (UI.KeyBindings.IsActive("TeleportParty"))
                    Teleport.TeleportParty();
            }
        }
    }
}