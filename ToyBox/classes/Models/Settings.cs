// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.RuleSystem;
using ModKit;
using ModKit.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace ToyBox {
    public class PerSaveSettings : EntityPart {
        public const string ID = "ToyBox.PerSaveSettings";
        public delegate void Changed(PerSaveSettings perSave);
        [JsonIgnore]
        public static Changed observers;

        // schema for storing multiclass settings
        //      Dictionary<CharacterName, 
        //          Dictionary<ClassID, HashSet<ArchetypeIDs>

        // This is the scaling modifier which is applied to the visual model of each character
        [JsonProperty]
        public Dictionary<string, float> characterModelSizeMultiplier = new();
    }

    public class Settings : UnityModManager.ModSettings {
        private static PerSaveSettings cachedPerSave = null;
        public const string PerSaveKey = "ToyBox";
        public static void ClearCachedPerSave() => cachedPerSave = null;
        public static void ReloadPerSaveSettings() {
            var player = Game.Instance?.Player;
            if (player == null || Game.Instance.SaveManager.CurrentState == SaveManager.State.Loading) return;
            Mod.Debug($"reloading per save settings from Player.SettingsList[{PerSaveKey}]");
            if (Shodan.GetInGameSettingsList().TryGetValue(PerSaveKey, out var obj) && obj is string json) {
                try {
                    cachedPerSave = JsonConvert.DeserializeObject<PerSaveSettings>(json);
                    Mod.Debug($"read successfully from Player.SettingsList[{PerSaveKey}]");
                }
                catch (Exception e) {
                    Mod.Error($"failed to read from Player.SettingsList[{PerSaveKey}]");
                    Mod.Error(e);
                }
            }
            if (cachedPerSave == null) {
                Mod.Warn("per save settings not found, creating new...");
                cachedPerSave = new PerSaveSettings {
                };
                SavePerSaveSettings();
            }
        }
        public static void SavePerSaveSettings() {
            var player = Game.Instance?.Player;
            if (player == null) return;
            if (cachedPerSave == null)
                ReloadPerSaveSettings();
            var json = JsonConvert.SerializeObject(cachedPerSave);
            Shodan.GetInGameSettingsList()[PerSaveKey] = json;
            try {
                Mod.Debug($"saved to Player.SettingsList[{PerSaveKey}]");
                if (PerSaveSettings.observers is MulticastDelegate mcdel) {
                    var doomed = new List<PerSaveSettings.Changed>();
                    foreach (var inv in mcdel.GetInvocationList()) {
                        if (inv.Target == null && inv is PerSaveSettings.Changed changed)
                            doomed.Add(changed);
                    }
                    foreach (var del in doomed) {
                        Mod.Debug("removing observer: {del} from PerSaveSettings");
                        PerSaveSettings.observers -= del;
                    }
                }
                if (cachedPerSave)
                    PerSaveSettings.observers?.Invoke(cachedPerSave);
            }
            catch (Exception e) {
                Mod.Error(e);
            }
        }
        public PerSaveSettings perSave {
            get {
                if (cachedPerSave != null) return cachedPerSave;
                ReloadPerSaveSettings();
                return cachedPerSave;
            }
        }

        // Main
        public int selectedTab = 0;
        public int increment = 10000;

        // Quality of Life
        public bool toggleContinueAudioOnLostFocus = false;
        public bool highlightObjectsToggle = false;
        public bool toggleShiftClickToUseInventorySlot = false;
        public bool toggleShiftClickToFastTransfer = false;
        // TODO: Public Interface? UI?
        public bool enableLoadWithMissingBlueprints = false;
        public bool toggleZoomableLocalMaps = false;
        public bool toogleShowInterestingNPCsOnQuestTab = false;
        public bool toggleShowInterestingNPCsOnLocalMap = false;
        public bool toggleSkipAnyKeyToContinueWhenLoadingSaves = false;

        // Camera
        public bool toggleZoomOnAllMaps = false;
        public bool toggleRotateOnAllMaps = false;
        // TODO: Public Interface? UI?
        public bool toggleScrollOnAllMaps = false;
        public bool toggleCameraPitch = false;
        public bool toggleCameraElevation = false;
        public bool toggleFreeCamera = false;
        public bool toggleInvertXAxis = false;
        public bool toggleInvertKeyboardXAxis = false;
        public bool toggleInvertYAxis = false;
        public float fovMultiplier = 1;
        public float AdjustedFovMultiplier => Math.Max(fovMultiplier, toggleZoomableLocalMaps ? 1.25f : 0.4f);

        // Tweaks
        public bool toggleNoPsychicPhenomena = false;
        public bool toggleInfiniteAbilities = false;
        public bool toggleInfiniteSpellCasts = false;
        public bool toggleUnlimitedActionsPerTurn = false;
        public bool toggleEquipmentRestrictions = false;
        public bool toggleDialogRestrictions = false;
        // TODO: Should this stay experimental?
        public bool toggleDialogRestrictionsEverything = false;
        public bool toggleRestoreSpellsAbilitiesAfterCombat = false;
        public bool toggleInstantRestAfterCombat = false;
        public bool toggleInfiniteItems = false;
        public bool toggleAutomaticallyLoadLastSave = false;
        public bool toggleAllowAchievementsDuringModdedGame = false;
        public bool togglAutoEquipConsumables = false;
        public bool toggleEquipItemsDuringCombat = false;
        public bool toggleUseItemsDuringCombat = false;
        public bool toggleTeleportKeysEnabled = false;
        public bool highlightHiddenObjects = false;
        public bool highlightHiddenObjectsInFog = false;
        public bool toggleUnlimitedStatModifierStacking = false;
        public bool disableTraps = false;
        public bool togglekillOnEngage = false;
        public bool disableWarpRandomEncounter = false;
        public bool disableEndTurnHotkey = false;

        // Loot 
        public bool toggleColorLootByRarity = false;
        public bool toggleShowRarityTags = false;
        public bool UsingLootRarity => toggleColorLootByRarity || toggleShowRarityTags;
        public RarityType minRarityToColor = 0;
        public bool toggleMassLootEverything = false;
        public bool toggleLootAliveUnits = false;
        public bool toggleLootChecklistFilterBlueprint = false;
        public bool toggleLootChecklistFilterDescription = false;
        public RarityType lootChecklistFilterRarity = RarityType.None;
        public RarityType maxRarityToHide = RarityType.None;

        // Enhanced Inventory
        // TODO: Public Interface? UI?
        public FilterCategories SearchFilterCategories = FilterCategories.Default;
        public ItemSortCategories InventoryItemSorterOptions = ItemSortCategories.Default;

        // level up
        public bool toggleIgnorePrerequisiteStatValue = false;
        public bool toggleIgnorePrerequisiteClassLevel = false;
        public bool toggleFeaturesIgnorePrerequisites = false;
        public bool toggleSetDefaultRespecLevelZero = false;

        // Multipliers
        public float experienceMultiplier = 1;
        public float experienceMultiplierCombat = 1;
        public float experienceMultiplierQuests = 1;
        public float experienceMultiplierSkillChecks = 1;
        public float experienceMultiplierChallenges = 1;
        public float experienceMultiplierSpace = 1;
        public bool useCombatExpSlider = false;
        public bool useQuestsExpSlider = false;
        public bool useSkillChecksExpSlider = false;
        public bool useChallengesExpSlider = false;
        public bool useSpaceExpSlider = false;
        public float fowMultiplier = 1;
        public float partyMovementSpeedMultiplier = 1;
        public float buffDurationMultiplierValue = 1;
        public float timeScaleMultiplier = 1;
        public float alternateTimeScaleMultiplier = 3;
        public bool useAlternateTimeScaleMultiplier = false;

        // Dice Rolls
        public UnitSelectType allAttacksHit = UnitSelectType.Off;
        public UnitSelectType allHitsCritical = UnitSelectType.Off;
        public UnitSelectType rollWithAdvantage = UnitSelectType.Off;
        public UnitSelectType rollWithDisadvantage = UnitSelectType.Off;
        public UnitSelectType alwaysRoll1 = UnitSelectType.Off;
        public UnitSelectType neverRoll1 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll1OutOfCombat = UnitSelectType.Off;
        public UnitSelectType alwaysRoll50 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll100 = UnitSelectType.Off;
        public UnitSelectType neverRoll100 = UnitSelectType.Off;
        public UnitSelectType roll1Initiative = UnitSelectType.Off;
        public UnitSelectType roll5Initiative = UnitSelectType.Off;
        public UnitSelectType roll10Initiative = UnitSelectType.Off;
        public UnitSelectType skillsTake1 = UnitSelectType.Off;
        public UnitSelectType skillsTake25 = UnitSelectType.Off;
        public UnitSelectType skillsTake50 = UnitSelectType.Off;

        // Party Editor
        public int selectedPartyFilter = 0;

        // Blueprint Browser
        public int searchLimit = 100;
        public int selectedBPTypeFilter = 1;
        public string searchText = "";
        public bool searchDescriptions = true;
        public bool showAttributes = false;
        public bool showAssetIDs = false;
        public bool showComponents = false;
        public bool showElements = false;
        public bool showDisplayAndInternalNames = false;
        public bool sortCollationByEntries = false;

        // Enchantment (Sandal)
        public bool showRatingForEnchantmentInventoryItems = true;

        // Dialog & Previews (Dialogs, Events ,etc)
        public bool previewDialogResults = false;
        public bool previewDialogConditions = false;
        public bool previewAlignmentRestrictedDialog = false;
        public bool toggleAllowAnyGenderRomance = false;
        public bool toggleMultipleRomance = false;
        public bool toggleRemoteCompanionDialog = false;
        public bool toggleExCompanionDialog = false;
        public bool toggleShowAnswersForEachConditionalResponse = false;
        public bool toggleMakePreviousAnswersMoreClear = false;

        // Etudes
        public bool showEtudeComments = true;

        // Quests
        public bool toggleQuestHideCompleted = true;
        public bool toggleQuestsShowUnrevealedObjectives = false;
        public bool toggleQuestInspector = false;
        public bool toggleIntrestingNPCsShowFalseConditions = false;
        public bool toggleInterestingNPCsShowHidden = false;

        // Saves 
        public bool toggleShowGameIDs = false;

        public bool toggleIgnoreAbilityAnyRestriction = false;
        public bool toggleIgnoreAbilityAoeOverlap = false;
        public bool toggleIgnoreAbilityLineOfSight = false;
        public bool toggleIgnoreAbilityTargetTooFar = false;
        public bool toggleIgnoreAbilityTargetTooClose = false;

        public HashSet<string> buffsToIgnoreForDurationMultiplier = new(SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier);

        // Development
        public LogLevel loggingLevel = LogLevel.Info;
        public bool stripHtmlTagsFromNativeConsole = true;
        public bool stripHtmlTagsFromUMMLogsTab = true;
        public bool toggleDevopmentMode = false;
        public bool toggleGuidsClipboard = true;
        public bool toggleRiskyToggles = false;
        public bool onlyShowLanguagesWithFiles = true;

        // Save
        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);

    }
}

