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
using ToyBox.classes.Infrastructure;

namespace ToyBox {
#if DEBUG
    [EnableReloading]
#endif
    static class Main {
        static Harmony HarmonyInstance;
        public static readonly LogChannel logger = LogChannelFactory.GetOrCreate("Respec");
        static string modId;
        public static UnityModManager.ModEntry modEntry = null;
        public static Settings settings;
        public static Mod multiclassMod;
        public static bool Enabled;
        public static bool IsModGUIShown = false;
        public static bool freshlyLaunched = true;
        public static bool NeedsActionInit = true;
        private static bool needsResetGameUI = false;
        private static bool resetRequested = false;
        private static DateTime resetRequestTime = DateTime.Now;
        public static void SetNeedsResetGameUI() {
            resetRequested = true;
            resetRequestTime = DateTime.Now;
            Main.Log($"resetRequested - {resetRequestTime}");
        }
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
                HumanFriendly.EnsureFriendlyTypesContainAll();
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
            NeedsActionInit = true;
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
            BagOfTricks.ResetGUI();
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
            IsModGUIShown = true;
            if (!IsInGame) {
                UI.Label("ToyBox has limited functionality from the main menu".yellow().bold());
            }
            if (!UI.IsWide) {
                UI.Label("Note ".magenta().bold() + "ToyBox was designed to offer the best user experience at widths of 1920 or higher. Please consider increasing your resolution up of at least 1920x1080 (ideally 4k) and go to Unity Mod Manager 'Settings' tab to change the mod window width to at least 1920.  Increasing the UI scale is nice too when running at 4k".orange().bold());
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
#if false
                using (UI.HorizontalScope()) {
                    UI.Label("Suggestions or issues click ".green(), UI.AutoWidth());
                    UI.LinkButton("here", "https://github.com/cabarius/ToyBox/issues");
                    UI.Space(50);
                    UI.Label("Chat with the Authors, Narria et all on the ".green(), UI.AutoWidth());
                    UI.LinkButton("WoTR Discord", "https://discord.gg/wotr");
                }
#endif
                UI.TabBar(ref settings.selectedTab,
                    () => {
                        if (BlueprintLoader.Shared.IsLoading) {
                            UI.Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
                        }
                        else UI.Space(25);
                    },
                    new NamedAction("Bag of Tricks", () => BagOfTricks.OnGUI()),
                    new NamedAction("Level Up", () => LevelUp.OnGUI()),
                    new NamedAction("Party", () => PartyEditor.OnGUI()),
                    new NamedAction("Loot", () => PhatLoot.OnGUI()),
                    new NamedAction("Enchantment", () => EnchantmentEditor.OnGUI()),
#if false
                    new NamedAction("Playground", () => Playground.OnGUI()),
#endif
                    new NamedAction("Search 'n Pick", () => BlueprintBrowser.OnGUI()),
                    new NamedAction("Crusade", () => CrusadeEditor.OnGUI()),
                    new NamedAction("Quests", () => QuestEditor.OnGUI())
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
            if (NeedsActionInit) {
                BagOfTricks.OnLoad();
                NeedsActionInit = false;
            }
            if (resetRequested) {
                var timeSinceRequest = DateTime.Now.Subtract(resetRequestTime).TotalMilliseconds;
                //Main.Log($"timeSinceRequest - {timeSinceRequest}");
                if (timeSinceRequest > 1000) {
                    Main.Log($"resetExecuted - {timeSinceRequest}".cyan());
                    needsResetGameUI = true;
                    resetRequested = false;
                }
            }
            if (needsResetGameUI) {
                Game.Instance.ScheduleAction(() => {
                    needsResetGameUI = false;
                    Game.ResetUI();

                    // TODO - Find out why the intiative tracker comes up when I do Game.ResetUI.  The following kludge makes it go away

                    var canvas = Game.Instance?.UI?.Canvas?.transform;
                    //Main.Log($"canvas: {canvas}");
                    var hudLayout = canvas?.transform.Find("HUDLayout");
                    //Main.Log($"hudLayout: {hudLayout}");
                    var initiaveTracker = hudLayout.transform.Find("Console_InitiativeTrackerHorizontalPC");
                    //Main.Log($"    initiaveTracker: {initiaveTracker}");
                    initiaveTracker?.gameObject?.SetActive(false);

                });
            }
            var currentMode = Game.Instance.CurrentMode;
            if (IsModGUIShown || Event.current == null || !Event.current.isKey) return;
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