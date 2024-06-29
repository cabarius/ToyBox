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
        // For character gen config we use the following special key:
        [JsonProperty]
        public Dictionary<string, MulticlassOptions> multiclassSettings = new();

        // This is the set of classes that each char has leveled up under multi-class.  They will be excluded from char level calculations
        [JsonProperty]
        public Dictionary<string, HashSet<string>> excludeClassesFromCharLevelSets = new();

        // This is the scaling modifier which is applied to the visual model of each character
        [JsonProperty]
        public Dictionary<string, float> characterModelSizeMultiplier = new();

        // This is the override setting for the character Descriptor size modifier
        [JsonProperty]
        public Dictionary<string, Kingmaker.Enums.Size> characterSizeModifier = new();
        // Dictionary<Character Hashcode,
        //              { doOverride, OverrideValue }
        [JsonProperty]
        public Dictionary<string, Tuple<bool, bool>> doOverrideEnableAiForCompanions = new();

        // This is the override setting for the Army Recruitment Growth Modifier
        [JsonProperty]
        public Dictionary<int, int> armyRecruitGrowthAdjustment = new();

        // Dictionary of Name/IsLegendaryHero for configuration per party member
        [JsonProperty]
        public Dictionary<string, bool> charIsLegendaryHero = new();
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
                } catch (Exception e) {
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
                Mod.Trace($"multiclass options: {string.Join(" ", cachedPerSave.multiclassSettings)}");
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
            } catch (Exception e) {
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
        public int alignmentIncrement = 5;
        public bool toggleBugFixes = true;
        public HashSet<string> namesToDisableVoiceOver = new();

        // Quality of Life
        public bool toggleContinueAudioOnLostFocus = false;
        public bool highlightObjectsToggle = false;
        public bool toggleHighlightCopyableScrolls = false;
        public bool toggleShiftClickToUseInventorySlot = false;
        public bool toggleShiftClickToFastTransfer = false;
        public bool toggleShowAcronymsInSpellAndActionSlots = false;
        public bool toggleWidenActionBarGroups = false;
        public bool toggleGameOverFixLeeerrroooooyJenkins = false;
        public bool enableLoadWithMissingBlueprints = false;
        public bool toggleExpandedPartyView = false;
        public bool togglePuzzleRelief = false;
        public bool toggleZoomableLocalMaps = false;
        public float zoomableLocalMapScrollSpeedMultiplier = 4;
        public bool toogleShowInterestingNPCsOnQuestTab = false;
        public bool toggleShowInterestingNPCsOnLocalMap = false;
        public bool toggleShowQuestMarkersOnLocalMap = false;
        public bool toggleEnhancedLoadSave = false;
        public bool toggleSkipAnyKeyToContinueWhenLoadingSaves = false;

        // Camera
        public bool toggleAutoFollowHold = true;
        public bool toggleZoomOnAllMaps = false;
        public bool toggleRotateOnAllMaps = false;
        public bool toggleScrollOnAllMaps = false;
        public bool toggleCameraPitch = false;
        public bool toggleCameraElevation = false;
        public bool toggleFreeCamera = false;
        public bool toggleInvertXAxis = false;
        public bool toggleInvertKeyboardXAxis = false;
        public bool toggleInvertYAxis = false;
        public bool toggleUseAltMouseWheelToAdjustClipPlane = false;
        public float fovMultiplier = 1;
        public float AdjustedFovMultiplier => Math.Max(fovMultiplier, toggleZoomableLocalMaps ? 1.25f : 0.4f);
        public float fovMultiplierCutScenes = 1;
        public float fovMultiplierMax = 1.25f;

        // Tweaks
        public int kineticistBurnReduction = 0;
        public bool toggleSpontaneousCopyScrolls = false;
        public bool toggleInstantEvent = false;
        public bool toggleInfiniteAbilities = false;
        public bool toggleInfiniteSpellCasts = false;
        public bool toggleInfiniteSkillpoints = false;
        public bool toggleInstantCooldown = false;
        public bool toggleInstantCrusadeSpellsCooldown = false;
        public bool toggleUnlimitedActionsPerTurn = false;
        public bool toggleEquipmentRestrictions = false;
        public bool toggleIgnoreMaxDexterity = false;
        public bool toggleIgnoreArmorChecksPenalty = false;
        public bool toggleIgnoreSpeedReduction = false;
        public bool toggleIgnoreSpellFailure = false;
        public bool togglePartyNegativeLevelImmunity = false;
        public bool togglePartyAbilityDamageImmunity = false;
        public bool toggleDialogRestrictions = false;
        public bool toggleDialogRestrictionsMythic = false;
        public bool toggleDialogRestrictionsEverything = false;
        public bool toggleNoFriendlyFireForAOE = false;
        public bool toggleIgnoreSettlementRestrictions = false;
        public bool toggleMoveSpeedAsOne = false;
        public bool toggleNoFogOfWar = false;
        public bool toggleRestoreSpellsAbilitiesAfterCombat = false;
        public bool toggleRechargeItemsAfterCombat = false;
        public bool toggleInstantRestAfterCombat = false;
        public bool toggleShowAllPartyPortraits = false;    // TODO - port this
        public bool toggleAccessRemoteCharacters = false;
        public bool toggleInfiniteItems = false;
        public bool toggleMetamagicIsFree = false;
        public bool toggleMaterialComponent = false;
        public bool toggleAutomaticallyLoadLastSave = false;
        public bool toggleBlockUnrecruit = false;
        public bool toggleAllowAchievementsDuringModdedGame = false;
        public bool toggleForceTutorialsToHonorSettings = false;
        public bool toggleForceDisableTutorials = false;
        public bool toggleAllowAnyGenderRomance = false;
        public bool toggleMultipleRomance = false;
        public int pickedDLC6Override = 0;
        public bool toggleFriendshipIsMagic = false;
        public bool toggleReplaceModelMenu = false;
        public bool toggleSpiderBegone = false;
        public bool toggleVescavorsBegone = false;
        public bool toggleRetrieversBegone = false;
        public bool toggleDeraknisBegone = false;
        public bool toggleDeskariBegone = false;
        public bool togglAutoEquipConsumables = false;
        public bool toggleInstantChangeParty = false;
        public bool toggleExtendHexes = false;
        public bool toggleAllowAllActivatable = false;
        public bool toggleKineticistGatherPower = false;
        public bool toggleAlwaysAllowSpellCombat = false;
        public bool toggleInstantPartyChange = false;
        public bool toggleEnterCombatAutoRage = false;
        public bool toggleEnterCombatAutoRageDemon = false;
        public bool toggleEquipmentNoWeight = false;
        public bool toggleEquipItemsDuringCombat = false;
        public bool toggleUseItemsDuringCombat = false;
        public bool toggleTeleportKeysEnabled = false;
        public bool toggleRespecRefundScrolls = false;
        public bool toggleAlignmentFix = false;
        public bool togglePreventAlignmentChanges = false;
        public bool toggleBrutalUnfair = false;
        public bool highlightHiddenObjects = false;
        public bool highlightHiddenObjectsInFog = false;
        public bool toggleAttacksofOpportunity = false;
        public bool toggleMakePetsRidable = false;
        public bool toggleRideAnything = false;
        public bool toggleUnlimitedStatModifierStacking = false;
        public bool disableTraps = false;
        public bool togglekillOnEngage = false;
        public bool toggleDisableCorruption = false;
        public float enduringSpellsTimeThreshold = 60f;
        public float greaterEnduringSpellsTimeThreshold = 5f;
        public bool allEtudesReadable = false;

        // Loot 
        public bool toggleColorLootByRarity = false;
        public bool toggleShowRarityTags = false;
        public bool toggleSortByRarirtyFirst = true;
        public bool UsingLootRarity => toggleColorLootByRarity || toggleShowRarityTags;
        public RarityType minRarityToColor = 0;
        public RarityType lootFilterIgnore = RarityType.None;
        public RarityType lootFilterAutoSell = RarityType.None;
        public bool toggleMassLootEverything = false;
        public bool toggleLootAliveUnits = false;
        public bool toggleOverrideLockedItems = false;
        public bool toggleLootChecklistFilterFriendlies = false;
        public bool toggleLootChecklistFilterBlueprint = false;
        public bool toggleLootChecklistFilterDescription = false;
        public bool toggleShowCantEquipReasons = false;
        public RarityType lootChecklistFilterRarity = RarityType.None;
        public RarityType maxRarityToHide = RarityType.None;
        public bool toggleCustomBulkSell = false;
        public BulkSellSettings bulkSellSettings = new();
        // Enhanced Inventory
        public bool toggleEnhancedInventory = false;
        public bool togglEquipSlotInventoryFiltering = false;
        public bool toggleDontClearSearchWhenLoseFocus = true;
        public bool toggleEnhancedSpellbook = false;
        public bool toggleSpellbookShowAllSpellsByDefault = false;
        public bool toggleSpellbookShowMetamagicByDefault = true;
        public bool toggleSpellbookShowLevelWhenViewingAllSpells = true;
        public bool toggleSpellbookShowEmptyMetamagicCircles = true;
        public bool toggleSpellbookAutoSwitchToMetamagicTab = false;
        public bool toggleSpellbookSearchBarFocusWhenOpening = false;
        public FilterCategories SearchFilterCategories = FilterCategories.Default;
        public ItemSortCategories InventoryItemSorterOptions = ItemSortCategories.Default;
        public InventorySearchCriteria InventorySearchCriteria = InventorySearchCriteria.Default;
        public SpellbookSearchCriteria SpellbookSearchCriteria = SpellbookSearchCriteria.Default;

        // Crusade
        public bool toggleInfiniteArmyRerolls = false;
        public bool toggleLargeArmies = false;
        public bool toggleCrusadeFlagsStayGreen = false;
        public bool toggleIgnoreStartTaskRestrictions = false;
        public bool toggleTaskNoResourcesCost = false;
        public bool toggleAddNewUnitsAsMercenaries = false;
        public float postBattleSummonMultiplier = 1;
        public float recruitmentCost = 1;
        public float recruitmentMultiplier = 1;
        public float armyExperienceMultiplier = 1;
        public float playerLeaderPowerMultiplier = 1;
        public float enemyLeaderPowerMultiplier = 1;
        public float kingdomTaskResolutionLengthMultiplier = 0;
        public bool toggleIgnoreEventSolutionRestrictions = false;
        public bool toggleHideEventSolutionRestrictionsPreview = false;
        public int unitCount = 100;

        // Settlement
        public bool toggleIgnoreBuildingClassRestrictions = false;
        public bool toggleIgnoreBuildingAdjanceyRestrictions = false;

        // selectors
        public UnitSelectType noAttacksOfOpportunitySelection = UnitSelectType.Off;
        public UnitSelectType allowMovementThroughSelection = UnitSelectType.Off;
        public float collisionRadiusMultiplier = 1;

        // char creation
        public bool toggleAllRaceCustomizations = false;
        public bool toggleIgnoreGenderRestrictions = false;

        // level up
        public bool toggleNoLevelUpRestrictions = false;
        public bool toggleFullHitdiceEachLevel = false;
        public bool toggleIgnoreClassRestrictions = false;
        public bool toggleIgnoreFeatRestrictions = false;
        public bool toggleAllowCompanionsToBecomeMythic = false;
        public bool toggleAllowMythicPets = false;
        public bool toggleIgnorePrerequisites = false;
        public bool toggleIgnoreCasterTypeSpellLevel = false;
        public bool toggleIgnoreForbiddenArchetype = false;
        public bool toggleIgnorePrerequisiteStatValue = false;
        public bool toggleIgnorePrerequisiteClassLevel = false;
        public bool toggleIgnoreAlignmentWhenChoosingClass = false;
        public bool toggleIgnoreFeaturePrerequisitesWhenChoosingClass = false;  // TODO - implement
        public bool toggleIgnoreForbiddenFeatures = false;
        public bool toggleIgnoreFeaturePrerequisites = false;
        public bool toggleIgnoreFeatureListPrerequisites = false;
        public bool toggleFeaturesIgnorePrerequisites = false;
        public bool toggleSkipSpellSelection = false;
        public bool toggleOptionalFeatSelection = false;
        public bool toggleUniversalSpellbookd = false;
        public bool toggleUncappedCasterLevel = false;
        public bool toggleContinousLevelCap = false;
        public bool toggleExponentialLevelCap = false;
        public bool toggleFeatureMultiplierCompanions = false;
        public bool toggleFeatureRecommendations = false;

        // Multipliers
        public int featsMultiplier = 1;
        public int encumberanceMultiplier = 1;
        public int encumberanceMultiplierPartyOnly = 1;
        public float experienceMultiplier = 1;
        public float experienceMultiplierCombat = 1;
        public float experienceMultiplierQuests = 1;
        public float experienceMultiplierSkillChecks = 1;
        public float experienceMultiplierTraps = 1;
        public bool useCombatExpSlider = false;
        public bool useQuestsExpSlider = false;
        public bool useSkillChecksExpSlider = false;
        public bool useTrapsExpSlider = false;
        public float fowMultiplier = 1;
        public float moneyMultiplier = 1;
        public float vendorSellPriceMultiplier = 1;
        public float vendorBuyPriceMultiplier = 1;
        public float fatigueHoursModifierMultiplier = 1;
        public float spellsPerDayMultiplier = 1;
        public float memorizedSpellsMultiplier = 1;
        public float partyMovementSpeedMultiplier = 1;
        public float travelSpeedMultiplier = 1;
        public int characterCreationAbilityPointsMax = 18;
        public int characterCreationAbilityPointsMin = 7;
        public int characterCreationAbilityPointsPlayer = 20;
        public int characterCreationAbilityPointsMerc = 20;
        public bool characterCreationAbilityPointsOverridePlayer = false;
        public bool characterCreationAbilityPointsOverrideMerc = false;
        public float companionCostMultiplier = 1;
        public float kingdomBuildingTimeModifier = 0;
        public float enemyBaseHitPointsMultiplier = 1;
        public float buffDurationMultiplierValue = 1;
        public float timeScaleMultiplier = 1;
        public float alternateTimeScaleMultiplier = 3;
        public bool useAlternateTimeScaleMultiplier = false;
        public float arcanistSpellslotMultiplier = 1;
        public float brutalDifficultyMultiplier = 1;
        public float turnBasedCombatStartDelay = 4;

        // Dice Rolls
        public UnitSelectType allAttacksHit = UnitSelectType.Off;
        public UnitSelectType allHitsCritical = UnitSelectType.Off;
        public UnitSelectType rollWithAdvantage = UnitSelectType.Off;
        public UnitSelectType rollWithDisadvantage = UnitSelectType.Off;
        public UnitSelectType alwaysRoll1 = UnitSelectType.Off;
        public UnitSelectType neverRoll1 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll10 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll20 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll20OutOfCombat = UnitSelectType.Off;
        public UnitSelectType rollAtLeast10OutOfCombat = UnitSelectType.Off;
        public UnitSelectType neverRoll20 = UnitSelectType.Off;
        public UnitSelectType roll1Initiative = UnitSelectType.Off;
        public UnitSelectType roll10Initiative = UnitSelectType.Off;
        public UnitSelectType roll20Initiative = UnitSelectType.Off;
        public UnitSelectType skillsTake10 = UnitSelectType.Off;
        public UnitSelectType skillsTake20 = UnitSelectType.Off;

        // Summons
        public bool toggleMakeSummmonsControllable = false;
        public UnitSelectType summonTweakTarget1 = UnitSelectType.Off;
        public float summonDurationMultiplier1 = 1;
        public float summonLevelModifier1 = 0;
        public UnitSelectType summonTweakTarget2 = UnitSelectType.Off;
        public float summonDurationMultiplier2 = 1;
        public float summonLevelModifier2 = 0;

        // Key Bindings
        //public SerializableDictionary<string, KeyCode>

        public KeyCode teleportMainHotKey = KeyCode.Comma;
        public KeyCode teleportSelectedHotKey = KeyCode.Period;
        public KeyCode teleportPartyHotKey = KeyCode.Slash;

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
        public bool showDivisions = true;
        public bool showFromAllSpellbooks = false;
        public bool showSpecialSpells = false;
        public bool showDisplayAndInternalNames = false;
        public bool factEditorShowInspector = true;
        public bool sortCollationByEntries = false;

        // Enchantment (Sandal)
        public string searchTextEnchantments = "";
        public bool showRatingForEnchantmentInventoryItems = true;

        // Dialog & Previews (Dialogs, Events ,etc)
        public bool previewDialogResults = false;
        public bool previewDialogConditions = false;
        public bool previewAlignmentRestrictedDialog = false;
        public bool previewRandomEncounters = false;
        public bool previewDecreeResults = false;
        public bool previewEventResults = false;
        public bool previewRelicResults = false;
        public bool toggleRemoteCompanionDialog = false;
        public bool toggleExCompanionDialog = false;
        public bool toggleRandomizeCueSelections = false;
        public bool toggleShowAnswersForEachConditionalResponse = false;
        public bool toggleShowAllAnswersForEachConditionalResponse = false;
        public bool toggleShowDialogConditions = false;
        public bool toggleMakePreviousAnswersMoreClear = false;

        // Etudes
        public bool showEtudeComments = true;

        // Quests
        public bool toggleQuestHideCompleted = true;
        public bool toggleQuestShowUnseen = false;
        public bool toggleQuestsShowUnrevealedObjectives = false;
        public bool toggleQuestInspector = false;
        public bool toggleIntrestingNPCsShowFalseConditions = false;
        public bool toggleInterestingNPCsShowHidden = false;

        // Saves 
        public bool toggleShowGameIDs = false;

        // Multi-Class 
        public bool toggleMulticlass = false;   // big switch - TODO - do we need this?
        public bool toggleMultiArchetype = false;
        public bool toggleMulticlassInGameUI = false;
        public bool toggleMulticlassShowClassDescriptions = false;
        public bool toggleAlwaysShowMigration = false;
        public int selectedClassToConfigMulticlass = 0;

        // schema for storing multiclass settings
        //      Dictionary<CharacterName, 
        //          Dictionary<ClassID, HashSet<ArchetypeIDs>
        // For character gen config we use the following special key:
        public SerializableDictionary<string, MulticlassOptions> multiclassSettings = new();

        // This is the set of classes that each char has leveled up under multi-class.  They will be excluded from char level calculations
        public SerializableDictionary<string, HashSet<string>> excludeClassesFromCharLevelSets = new();

        // Dictionary of Name/IsLegendaryHero for configuration per party member
        public SerializableDictionary<string, bool> charIsLegendaryHero = new();

        public Multiclass.ProgressionPolicy multiclassHitPointPolicy = 0;
        public Multiclass.ProgressionPolicy multiclassSavingThrowPolicy = 0;
        public Multiclass.ProgressionPolicy multiclassBABPolicy = 0;
        public Multiclass.ProgressionPolicy multiclassSkillPointPolicy = 0;

        public bool toggleTakeHighestHitDie = true;
        public bool toggleTakeHighestSkillPoints = true;
        public bool toggleTakeHighestBAB = true;
        public bool toggleTakeHighestSaveByRecalculation = true;
        public bool toggleTakeHighestSaveByAlways = false;
        public bool toggleRecalculateCasterLevelOnLevelingUp = true;
        public bool toggleRestrictCasterLevelToCharacterLevel = true;
        public bool toggleRestrictCasterLevelToCharacterLevelTemporary = false;
        public bool toggleRestrictClassLevelForPrerequisitesToCharacterLevel = true;
        public bool toggleFixFavoredClassHP = false;
        public bool toggleAlwaysReceiveFavoredClassHPExceptPrestige = true;
        public bool toggleAlwaysReceiveFavoredClassHP = false;

        // these are useful and belong in Char Generation
        public bool toggleIgnoreAlignmentRestrictionCharGen = false;    // was called toggleIgnoreAlignmentRestriction in Multi-Class mod
        public bool toggleIgnoreAttributeCap = false;
        public bool toggleIgnoreAttributePointsRemainingChargen = false;

        // these are useful and belong in LevelUp
        public bool toggleLockCharacterLevel = false;
        public bool toggleIgnoreAttributePointsRemaining = false;
        public bool toggleIgnoreSkillCap = false;
        public bool toggleIgnoreSkillPointsRemaining = false;

        // Some of these look redundant.  It might be nice to add the fine grain configuration but part of the philosphy of ToyBox is to avoid too much kitchen sink options.  I would like to focus and simplify this.  Maybe see if there is a way to unify these into some broader groupings like I did in Cheap Tricks for patches that adopted CheckUnitEntityData (Off, You, Party, Enemies, etc)
        // public bool toggleIgnoreClassAndFeatRestrictions = false; 
        public HashSet<string> ignoredPrerequisiteSet = new(); // adding this granularity might be nice

        public bool toggleIgnoreAbilityAlignmentRestriction = false;
        public bool toggleIgnoreAbilityAnyRestriction = false;
        public bool toggleIgnoreAbilityAoeOverlap = false;
        public bool toggleIgnoreAbilityLineOfSight = false;
        public bool toggleIgnoreAbilityTargetTooFar = false;
        public bool toggleIgnoreAbilityTargetTooClose = false;

        public bool toggleIgnoreAolityCasterCheckers = false;
        public HashSet<string> ignoredAbilityCasterCheckerSet = new();
        public bool toggleIgnoreActivatableAbilityRestrictions = false;
        public HashSet<string> ignoredActivatableAbilityRestrictionSet = new();
        public bool toggleIgnoreEquipmentRestrictions = false;
        public HashSet<string> ignoredEquipmentRestrictionSet = new();
        public bool toggleIgnoreBuildingRestrictions = false;
        public HashSet<string> ignoredBuildingRestrictionSet = new();
        public HashSet<string> buffsToIgnoreForDurationMultiplier = new(SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier);

        // Development
        public LogLevel loggingLevel = LogLevel.Info;
        public bool stripHtmlTagsFromUMMLogsTab = false;
        public bool stripHtmlTagsFromNativeConsole = true;
        public bool toggleShowDebugInfo = true;
        public bool toggleDevopmentMode = false;
        public bool toggleUberLoggerForwardPrefix = false;
        public bool toggleGuidsClipboard = true;
        public bool onlyShowLanguagesWithFiles = true;

        // Deprecated
        internal bool toggleSpellbookAbilityAlignmentChecks = false;

        // Save
        public override void Save(UnityModManager.ModEntry modEntry) => Save(this, modEntry);

    }
}

