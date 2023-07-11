// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// Special thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server for teaching me how to mod Unity games
using HarmonyLib;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using ModKit;
using ModKit.DataViewer;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ToyBox.classes.Infrastructure;
using ToyBox.classes.MainUI;
using UniRx;
using UnityEngine;
using UnityModManagerNet;
using static ModKit.UI;
using LocalizationManager = ModKit.LocalizationManager;
using Kingmaker.UI.Common;
#if RT
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.LifeEvents;
#endif
#if Wrath
using ToyBox.Multiclass;
#elif RT
using Kingmaker.UI.Models.Log.Enums;
#endif

namespace ToyBox {
#if DEBUG
    [EnableReloading]
#endif
    internal static class Main {
        internal static Harmony HarmonyInstance;
        public static readonly LogChannel logger = LogChannelFactory.GetOrCreate("Respec");
        private static string _modId;
        public static Settings Settings;
#if Wrath
        public static MulticlassMod multiclassMod;
#endif
        public static NamedAction[] tabs = {
                    new NamedAction("Bag of Tricks".localize(), BagOfTricks.OnGUI),
                    new NamedAction("Enhanced UI".localize(), EnhancedUI.OnGUI),
                    new NamedAction("Level Up".localize(), LevelUp.OnGUI),
                    new NamedAction("Party".localize(), PartyEditor.OnGUI),
                    new NamedAction("Loot".localize(), PhatLoot.OnGUI),
                    new NamedAction("Enchantment".localize(), EnchantmentEditor.OnGUI),
#if false
                    new NamedAction("Playground".localize(), () => Playground.OnGUI()),
#endif
                    new NamedAction("Search 'n Pick".localize(), SearchAndPick.OnGUI),
#if Wrath
                    new NamedAction("Crusade".localize(), CrusadeEditor.OnGUI),
                    new NamedAction("Armies".localize(), ArmiesEditor.OnGUI),
                    new NamedAction("Events/Decrees".localize(), EventEditor.OnGUI),
#if DEBUG
                    new NamedAction("Gambits (AI)".localize(), BraaainzEditor.OnGUI),
#endif
#elif RT
                    new NamedAction("Colonies".localize(), ColonyEditor.OnGUI),
#endif
                    new NamedAction("Etudes".localize(), EtudesEditor.OnGUI),
                    new NamedAction("Quests".localize(), QuestEditor.OnGUI),
                    new NamedAction("Dialog & NPCs", DialogAndNPCs.OnGUI),
                    new NamedAction("Saves".localize(), GameSavesBrowser.OnGUI),
                    new NamedAction("Achievements".localize(), AchievementsUnlocker.OnGUI),
                    new NamedAction("Settings".localize(), SettingsUI.OnGUI)};
        private static int partyTabID = -1;
        public static bool Enabled;
        public static bool IsModGUIShown = false;
        public static bool freshlyLaunched = true;
        public static bool NeedsActionInit = true;
        private static bool _needsResetGameUI = false;
        private static bool _resetRequested = false;
        private static DateTime _resetRequestTime = DateTime.Now;
        public static bool resetExtraCameraAngles = false;
        public static void SetNeedsResetGameUI() {
            _resetRequested = true;
            _resetRequestTime = DateTime.Now;
            Mod.Debug($"resetRequested - {_resetRequestTime}");
        }
        public static bool IsInGame => Game.Instance.Player?.Party.Any() ?? false;
        private static Exception _caughtException = null;

        public static List<GameObject> Objects;
        private static bool Load(UnityModManager.ModEntry modEntry) {
            try {
#if DEBUG
                modEntry.OnUnload = OnUnload;
#endif
                _modId = modEntry.Info.Id;

                Mod.OnLoad(modEntry);
                UIHelpers.OnLoad();
                LoadSettings(modEntry);
                SettingsDefaults.InitializeDefaultDamageTypes();


                HarmonyInstance = new Harmony(modEntry.Info.Id);
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

                LocalizationManager.Enable();

                modEntry.OnToggle = OnToggle;
                modEntry.OnShowGUI = OnShowGUI;
                modEntry.OnHideGUI = OnHideGUI;
                modEntry.OnGUI = OnGUI;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnSaveGUI = OnSaveGUI;
                Objects = new List<GameObject>();
                KeyBindings.OnLoad(modEntry);
#if Wrath
                multiclassMod = new Multiclass.MulticlassMod();
#endif
                HumanFriendlyStats.EnsureFriendlyTypesContainAll();
                Mod.logLevel = Settings.loggingLevel;
                Mod.InGameTranscriptLogger = text => {
                    Mod.Log("CombatLog - " + text);
#if Wrath
                    var message = new CombatLogMessage("ToyBox".blue() + " - " + text, Color.black, PrefixIcon.RightArrow);
                    var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Common].FirstOrDefault(x => x is MessageLogThread);
                    var tacticalCombatLog = LogThreadService.Instance.m_Logs[LogChannelType.TacticalCombat].FirstOrDefault(x => x is MessageLogThread);
                    messageLog?.AddMessage(message);
                    tacticalCombatLog?.AddMessage(message);
#elif RT
                    var messageText = "ToyBox".blue() + " - " + text;
                    var message = new CombatLogMessage(messageText, Color.black, PrefixIcon.RightArrow);
                    var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Dialog].FirstOrDefault(x => x is DialogLogThread);
                    messageLog?.AddMessage(message);
#endif
                };
            }
            catch (Exception e) {
                Mod.Error(e);
                throw e;
            }
            return true;
        }
#if DEBUG
        private static bool OnUnload(UnityModManager.ModEntry modEntry) {
            foreach (var obj in Objects) {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            BlueprintExtensions.ResetCollationCache();
            HarmonyInstance.UnpatchAll(_modId);
            EnhancedInventory.OnUnload();
            NeedsActionInit = true;
            return true;
        }
#endif
        private static void LoadSettings(UnityModManager.ModEntry modEntry) {
            var thisToyBoxPath = modEntry.Path;
            var thisSettingsPath = Path.Combine(thisToyBoxPath, "Settings.xml");
            if (!File.Exists(thisSettingsPath)) {
                try {
                    Mod.Log("Settings file not found attempting to migrate from older ToyBox".yellow());
#if Wrath
                    var otherToyBoxPath = Path.Combine(UnityModManager.modsPath, "ToyBox");
#elif RT
                    var otherToyBoxPath = Path.Combine(UnityModManager.ModsPath, "ToyBox");
#endif
                    Mod.Log($"Checking {otherToyBoxPath}");
                    if (Directory.Exists(otherToyBoxPath)) {
                        Mod.Log($"    Found older ToyBox at {otherToyBoxPath} migrating all settings");
                        File.Copy(Path.Combine(otherToyBoxPath, "Settings.xml"), thisSettingsPath);
                        var thisUserSettingsPath = Path.Combine(thisToyBoxPath, "UserSettings");
                        var otherUserSettingsPath = Path.Combine(otherToyBoxPath, "UserSettings");
                        Directory.CreateDirectory(thisUserSettingsPath);
                        var allFiles = Directory.GetFiles(otherUserSettingsPath, "*.*", SearchOption.AllDirectories);
                        foreach (string otherPath in allFiles) {
                            var thisPath = otherPath.Replace(otherUserSettingsPath, thisUserSettingsPath);
                            Mod.Log($"    {otherPath} => {thisPath}");
                            File.Copy(otherPath, thisPath, true);
                        }
                        Mod.Log("ToyBox settings migration => SUCCESS".green());
                    }
                    else {
                        Mod.Log("Other ToyBox not found... creating default settings".yellow());
                    }
                }
                catch (Exception e) {
                    Mod.Error(e);
                }
            }
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

        }
        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            Enabled = value;
            return true;
        }

        private static void ResetGUI(UnityModManager.ModEntry modEntry) {
            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Settings.searchText = "";
            Settings.searchLimit = 100;
            Mod.ModKitSettings.browserSearchLimit = 25;
            ModKitSettings.Save();
            BagOfTricks.ResetGUI();
            EnhancedCamera.ResetGUI();
            LevelUp.ResetGUI();
            PartyEditor.ResetGUI();
#if Wrath
            CrusadeEditor.ResetGUI();
#endif
            CharacterPicker.ResetGUI();
            SearchAndPick.ResetGUI();
            QuestEditor.ResetGUI();
            BlueprintExtensions.ResetCollationCache();
            _caughtException = null;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            if (!Enabled) return;
            IsModGUIShown = true;
#if RT
            if (!IsInGame) {
                Label(("Warning: ".magenta().bold() + $"This is an experimental preview of ToyBox ({"Sh0dan".cyan()}) for Rogue Trader Beta.".orange() + " Save early and often.\r\n".yellow().bold() + "Note:".magenta().bold() + " Not all features are functional at this time. The ToyBox team is working hard to get as much working as fast as possible".orange()).localize());
            }
#endif
            if (!IsInGame) {
                Label("ToyBox has limited functionality from the main menu".localize().yellow().bold());
            }
            if (!IsWide) {
                using (HorizontalScope()) {
                    ActionButton("Maximize Window".localize(), Actions.MaximizeModWindow);
                    Label(("Note ".magenta().bold() + "ToyBox was designed to offer the best user experience at widths of 1920 or higher. Please consider increasing your resolution up of at least 1920x1080 (ideally 4k) and go to Unity Mod Manager 'Settings' tab to change the mod window width to at least 1920.  Increasing the UI scale is nice too when running at 4k".orange().bold()).localize());
                }
            }
            try {
                var e = Event.current;
                userHasHitReturn = e.keyCode == KeyCode.Return;
                focusedControlName = GUI.GetNameOfFocusedControl();
                if (_caughtException != null) {
                    Label("ERROR".red().bold() + $": caught exception {_caughtException}");
                    ActionButton("Reset".orange().bold(), () => { ResetGUI(modEntry); }, AutoWidth());
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
                TabBar(ref Settings.selectedTab,
                    () => {
                        if (BlueprintLoader.Shared.IsLoading) {
                            Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
                        }
                        else Space(25);
                    },
                    (oldTab, newTab) => {
                        if (partyTabID == -1) {
                            for (int i = 0; i < tabs.Length; i++) {
                                if (tabs[i].action == PartyEditor.OnGUI) {
                                    partyTabID = i;
                                    break;
                                }
                            }
                        }
                        if (partyTabID != -1) {
                            if (oldTab == partyTabID) {
                                PartyEditor.UnloadPortraits();
                            }
                        }
                    },
                    tabs
                    );
            }
            catch (Exception e) {
                Console.Write($"{e}");
                _caughtException = e;
                ReflectionSearch.Shared.Stop();
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry) {
            Settings.Save(modEntry);
            ModKitSettings.Save();
        }
        private static void OnShowGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = true;
            EnchantmentEditor.OnShowGUI();
#if Wrath
            ArmiesEditor.OnShowGUI();
#endif
            EtudesEditor.OnShowGUI();
            Mod.OnShowGUI();
        }

        private static void OnHideGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = false;
            PartyEditor.UnloadPortraits();
        }
        private static IEnumerator ResetGUI() {
            _needsResetGameUI = false;
            Game.ResetUI();
            Mod.InGameTranscriptLogger?.Invoke("ResetUI");
#if Wrath
            // TODO - Find out why the intiative tracker comes up when I do Game.ResetUI.  The following kludge makes it go away

            var canvas = Game.Instance?.UI?.Canvas?.transform;
            //Main.Log($"canvas: {canvas}");
            var hudLayout = canvas?.transform.Find("HUDLayout");
            //Main.Log($"hudLayout: {hudLayout}");
            var initiaveTracker = hudLayout.transform.Find("Console_InitiativeTrackerHorizontalPC");
            //Main.Log($"    initiaveTracker: {initiaveTracker}");
            initiaveTracker?.gameObject?.SetActive(false);
#endif
            yield return null;
        }
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float z) {
            if (Game.Instance?.Player != null) {
#if Wrath
                var corruption = Game.Instance.Player.Corruption;
                var corruptionDisabled = (bool)corruption.Disabled;
                if (corruptionDisabled != Settings.toggleDisableCorruption) {
                    if (Settings.toggleDisableCorruption)
                        corruption.Disabled.Retain();
                    else
                        corruption.Disabled.ReleaseAll();
                }
#endif
            }
            Mod.logLevel = Settings.loggingLevel;
            if (NeedsActionInit) {
                EnhancedCamera.OnLoad();
                BagOfTricks.OnLoad();
                PhatLoot.OnLoad();
#if Wrath
                ArmiesEditor.OnLoad();
#endif
                EnhancedInventory.OnLoad();
                NeedsActionInit = false;
            }
            //if (resetExtraCameraAngles) {
            //    Game.Instance.UI.GetCameraRig().TickRotate(); // Kludge - TODO: do something better...
            //}
            if (_resetRequested) {
                var timeSinceRequest = DateTime.Now.Subtract(_resetRequestTime).TotalMilliseconds;
                //Main.Log($"timeSinceRequest - {timeSinceRequest}");
                if (timeSinceRequest > 1000) {
                    Mod.Debug($"resetExecuted - {timeSinceRequest}".cyan());
                    _needsResetGameUI = true;
                    _resetRequested = false;
                }
            }
            if (_needsResetGameUI) {
#if true
                MainThreadDispatcher.StartCoroutine(ResetGUI());
#endif
            }
            var currentMode = Game.Instance.CurrentMode;
            if (IsModGUIShown || Event.current == null || !Event.current.isKey) return;
            KeyBindings.OnUpdate();
            if (IsInGame
                && Settings.toggleTeleportKeysEnabled
                && (currentMode == GameModeType.Default
                    || currentMode == GameModeType.Pause
                    || currentMode == GameModeType.GlobalMap
                    )
                ) {
#if Wrath
                if (UIUtility.IsGlobalMap()) {
                    if (KeyBindings.IsActive("TeleportParty"))
                        Teleport.TeleportPartyOnGlobalMap();
                }
#endif
                if (KeyBindings.IsActive("TeleportMain"))
                    Teleport.TeleportUnit(Shodan.MainCharacter, Utils.PointerPosition());
                if (KeyBindings.IsActive("TeleportSelected"))
                    Teleport.TeleportSelected();
                if (KeyBindings.IsActive("TeleportParty"))
                    Teleport.TeleportParty();
            }
        }
    }
}