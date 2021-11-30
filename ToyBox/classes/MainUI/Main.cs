// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// Special thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server for teaching me how to mod Unity games
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;
using System;
using System.Collections.Generic;
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
    internal static class Main {
        private static Harmony HarmonyInstance;
        public static readonly LogChannel logger = LogChannelFactory.GetOrCreate("Respec");
        private static string modId;
        public static Settings settings;
        public static MulticlassMod multiclassMod;
        public static bool Enabled;
        public static bool IsModGUIShown = false;
        public static bool freshlyLaunched = true;
        public static bool NeedsActionInit = true;
        private static bool needsResetGameUI = false;
        private static bool resetRequested = false;
        private static DateTime resetRequestTime = DateTime.Now;
        public static bool resetExtraCameraAngles = false;
        public static void SetNeedsResetGameUI() {
            resetRequested = true;
            resetRequestTime = DateTime.Now;
            Mod.Debug($"resetRequested - {resetRequestTime}");
        }
        public static bool IsInGame => Game.Instance.Player?.Party.Any() ?? false;

        private static Exception caughtException = null;

        public static List<GameObject> Objects;

        private static bool Load(UnityModManager.ModEntry modEntry) {
            try {
#if DEBUG
                modEntry.OnUnload = Unload;
#endif
                modId = modEntry.Info.Id;

                Mod.OnLoad(modEntry);
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

                HarmonyInstance = new Harmony(modEntry.Info.Id);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                modEntry.OnToggle = OnToggle;
                modEntry.OnShowGUI = OnShowGUI;
                modEntry.OnHideGUI = OnHideGUI;
                modEntry.OnGUI = OnGUI;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnSaveGUI = OnSaveGUI;
                Objects = new List<GameObject>();
                UI.KeyBindings.OnLoad(modEntry);
                multiclassMod = new Multiclass.MulticlassMod();
                HumanFriendly.EnsureFriendlyTypesContainAll();
                Mod.logLevel = settings.loggingLevel;
            }
            catch (Exception e) {
                Mod.Error(e);
                throw e;
            }
            return true;
        }
#if DEBUG
        private static bool Unload(UnityModManager.ModEntry modEntry) {
            foreach (var obj in Objects) {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            BlueprintExensions.ResetCollationCache();
            HarmonyInstance.UnpatchAll(modId);
            NeedsActionInit = true;
            return true;
        }
#endif
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            Enabled = value;
            return true;
        }

        private static void ResetSearch() => BlueprintBrowser.ResetSearch();

        private static void ResetGUI(UnityModManager.ModEntry modEntry) {
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            settings.searchText = "";
            settings.searchLimit = 100;
            BagOfTricks.ResetGUI();
            LevelUp.ResetGUI();
            PartyEditor.ResetGUI();
            CrusadeEditor.ResetGUI();
            CharacterPicker.ResetGUI();
            BlueprintBrowser.ResetGUI();
            QuestEditor.ResetGUI();
            BlueprintExensions.ResetCollationCache();
            caughtException = null;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            if (!Enabled) return;
            IsModGUIShown = true;
            if (!IsInGame) {
                UI.Label("ToyBox has limited functionality from the main menu".yellow().bold());
            }
            if (!UI.IsWide) {
                UI.Label("Note ".magenta().bold() + "ToyBox was designed to offer the best user experience at widths of 1920 or higher. Please consider increasing your resolution up of at least 1920x1080 (ideally 4k) and go to Unity Mod Manager 'Settings' tab to change the mod window width to at least 1920.  Increasing the UI scale is nice too when running at 4k".orange().bold());
            }
            try {
                var e = Event.current;
                UI.userHasHitReturn = e.keyCode == KeyCode.Return;
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
                    new NamedAction("Armies", () => ArmiesEditor.OnGUI()),
                    new NamedAction("Events/Decrees", () => EventEditor.OnGUI()),
                    new NamedAction("Etudes", () => EtudesEditor.OnGUI()),
                    new NamedAction("Quests", () => QuestEditor.OnGUI()),
                    new NamedAction("Settings", () => SettingsUI.OnGUI())
                    );
            }
            catch (Exception e) {
                Console.Write($"{e}");
                caughtException = e;
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry) => settings.Save(modEntry);

        private static void OnShowGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = true;
            EnchantmentEditor.OnShowGUI();
            ArmiesEditor.OnShowGUI();
            EtudesEditor.OnShowGUI();
        }

        private static void OnHideGUI(UnityModManager.ModEntry modEntry) => IsModGUIShown = false;

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float z) {
            Mod.logLevel = settings.loggingLevel;
            if (NeedsActionInit) {
                BagOfTricks.OnLoad();
                NeedsActionInit = false;
            }
            //if (resetExtraCameraAngles) {
            //    Game.Instance.UI.GetCameraRig().TickRotate(); // Kludge - TODO: do something better...
            //}
            if (resetRequested) {
                var timeSinceRequest = DateTime.Now.Subtract(resetRequestTime).TotalMilliseconds;
                //Main.Log($"timeSinceRequest - {timeSinceRequest}");
                if (timeSinceRequest > 1000) {
                    Mod.Debug($"resetExecuted - {timeSinceRequest}".cyan());
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
            if (IsInGame
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