// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// Special thanks to @SpaceHampster and @Velk17 from Pathfinder: Wrath of the Rightous Discord server for teaching me how to mod Unity games
using HarmonyLib;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.UI.Common;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.LifeEvents;
using Kingmaker.UI.Models.Log.Enums;
using ModKit;
using ModKit.DataViewer;
using Newtonsoft.Json;
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

namespace ToyBox {
#if DEBUG
    [EnableReloading]
#endif
    internal static class Main {
        internal static Harmony HarmonyInstance;
        internal static UnityModManager.ModEntry modEntry;
        public static readonly LogChannel logger = LogChannelFactory.GetOrCreate("Respec");
        public static Settings Settings;
        public static NamedAction[] tabs = {
                    new NamedAction("Bag of Tricks", BagOfTricks.OnGUI),
                    new NamedAction("Enhanced UI", EnhancedUI.OnGUI),
                    new NamedAction("Level Up", LevelUp.OnGUI),
                    new NamedAction("Party", PartyEditor.OnGUI),
                    new NamedAction("Loot", PhatLoot.OnGUI),
#if false
                    new NamedAction("Enchantment", EnchantmentEditor.OnGUI),
                    new NamedAction("Playground", () => Playground.OnGUI()),
#endif
                    new NamedAction("Search 'n Pick", SearchAndPick.OnGUI),
                    new NamedAction("Colonies", ColonyEditor.OnGUI),
                    new NamedAction("Etudes", EtudesEditor.OnGUI),
                    new NamedAction("Quests", QuestEditor.OnGUI),
                    new NamedAction("Dialog & NPCs", DialogAndNPCs.OnGUI),
                    new NamedAction("Saves", GameSavesBrowser.OnGUI),
                    new NamedAction("Achievements", AchievementsUnlocker.OnGUI),
                    new NamedAction("Settings", SettingsUI.OnGUI)
        };
        private static int partyTabID = -1;
        public static bool Enabled;
        public static bool IsModGUIShown = false;
        public static bool freshlyLaunched = true;
        public static bool NeedsActionInit = true;
        private static bool _needsResetGameUI = false;
        private static bool _resetRequested = false;
        private static DateTime _resetRequestTime = DateTime.Now;
        public static bool resetExtraCameraAngles = false;
        internal static string path;
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
                Main.modEntry = modEntry;
#if DEBUG
                modEntry.OnUnload = OnUnload;
                _modId = modEntry.Info.Id;
#endif

                Mod.OnLoad(modEntry);
                path = modEntry.Path;
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
                HumanFriendlyStats.EnsureFriendlyTypesContainAll();
                Mod.logLevel = Settings.loggingLevel;
                Mod.InGameTranscriptLogger = text => {
                    Mod.Log("CombatLog - " + text);
                    var messageText = "ToyBox".blue() + " - " + text;
                    var message = new CombatLogMessage(messageText, Color.black, PrefixIcon.RightArrow);
                    var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Dialog].FirstOrDefault(x => x is DialogLogThread);
                    messageLog?.AddMessage(message);
                };
            } catch (Exception e) {
                Mod.Error(e.ToString());
                throw e;
            }
            return true;
        }
#if DEBUG
        internal static string _modId;
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
            try {
                if (!File.Exists(thisSettingsPath)) {
                    Mod.Log("No ToyBox settings found... creating default settings".yellow());
                }
            } catch (Exception e) {
                Mod.Error(e);
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
            CharacterPicker.ResetGUI();
            SearchAndPick.ResetGUI();
            QuestEditor.ResetGUI();
            BlueprintExtensions.ResetCollationCache();
            _caughtException = null;
        }
        private static bool IsFirstOnGUI = true;
        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            if (IsFirstOnGUI) {
                IsFirstOnGUI = false;
                Glyphs.CheckGlyphSupport();
            }
            if (!Enabled) return;
            IsModGUIShown = true;
            if (!IsInGame) {
                Label(("Warning: ".magenta().bold() + $"This is an experimental preview of ToyBox for Rogue Trader.".orange() + " Save early and often.\r\n".yellow().bold() + "Note:".magenta().bold() + " Not all features are functional at this time. If you notice a feature doesn't work please report that on GitHub or in the modding channels on the Owlcat Discord.".orange()).localize());
            }
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
                        } else Space(25);
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
                    s => s.localize(),
                    tabs
                    );
            } catch (Exception e) {
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
            AchievementsUnlocker.OnShowGUI();
            EtudesEditor.OnShowGUI();
            Mod.OnShowGUI();
        }

        private static void OnHideGUI(UnityModManager.ModEntry modEntry) {
            IsModGUIShown = false;
            PartyEditor.UnloadPortraits();
            BlueprintExtensions.descriptionCache.Clear();
            BlueprintExtensions.titleCache.Clear();
            BlueprintExtensions.sortKeyCache.Clear();
            BlueprintExtensions.searchKeyCache.Clear();
            OwlLogging.OnHideGUI();
        }
        private static IEnumerator ResetGUI() {
            _needsResetGameUI = false;
            Game.ResetUI();
            Mod.InGameTranscriptLogger?.Invoke("ResetUI");
            yield return null;
        }
        private static void OnUpdate(UnityModManager.ModEntry modEntry, float z) {
            if (Game.Instance?.Player != null) {
            }
            Mod.logLevel = Settings.loggingLevel;
            if (NeedsActionInit) {
                EnhancedCamera.OnLoad();
                BagOfTricks.OnLoad();
                PhatLoot.OnLoad();
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