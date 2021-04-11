// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityModManagerNet;

namespace ToyBox {
    public class Settings : UnityModManager.ModSettings {
        // Main

        public int selectedTab = 0;

        // Cheap Tricks

        public int increment = 10000;

        // Tweaks
        public bool highlightObjectsToggle = false;
        public bool toggleSpontaneousCopyScrolls = false;
        public bool toggleInstantEvent = false;
        public bool toggleInfiniteAbilities = false;
        public bool toggleInfiniteSpellCasts = false;
        public bool toggleInfiniteSkillpoints = false;
        public bool toggleInstantCooldown = false;
        public bool toggleUnlimitedActionsPerTurn = false;
        public bool toggleEquipmentRestrictions = false;
        public bool toggleDialogRestrictions = false;
        public bool toggleNoFriendlyFireForAOE = false;
        public bool toggleSettlementRestrictions = false;
        public bool toggleMoveSpeedAsOne = false;
        public bool toggleNoFogOfWar = false;
        public bool toggleRestoreSpellsAbilitiesAfterCombat = false;
        public bool toggleInstantRestAfterCombat = false;
        public bool toggleShowAllPartyPortraits = false;    // TODO - port this
        public bool toggleAccessRemoteCharacters = false;
        public bool toggleInfiniteItems = false;
        public bool toggleMetamagicIsFree = false;
        public bool toggleMaterialComponent = false;
        public bool toggleAutomaticallyLoadLastSave = false;

        // selectors
        public UnitSelectType noAttacksOfOpportunitySelection = UnitSelectType.Off;
        public UnitSelectType allowMovementThroughSelection = UnitSelectType.Off;
        public float collisionRadiusMultiplier = 1;

        // level up
        public bool toggleNoLevelUpRestrictions = false;
        public bool toggleFullHitdiceEachLevel = false;
        public bool toggleIgnorePrerequisites = false;
        public bool toggleIgnoreCasterTypeSpellLevel = false;
        public bool toggleIgnoreForbiddenArchetype = false;
        public bool toggleIgnorePrerequisiteStatValue = false;
        public bool toggleIgnoreAlignmentWhenChoosingClass = false;
        public bool toggleIgnoreFeaturePrerequisitesWhenChoosingClass = false;  // TODO - implement
        public bool toggleIgnoreForbiddenFeatures = false;
        public bool toggleIgnoreFeaturePrerequisites = false;
        public bool toggleIgnoreFeatureListPrerequisites = false;
        public bool toggleFeaturesIgnorePrerequisites = false;
        public bool toggleSkipSpellSelection = false;

        // Multipliers
        public int encumberanceMultiplier = 1;
        public float experienceMultiplier = 1;
        public float moneyMultiplier = 1;
        public float vendorSellPriceMultiplier = 1;
        public float vendorBuyPriceMultiplier = 1;
        public float fatigueHoursModifierMultiplier = 1;
        public float spellsPerDayMultiplier = 1;
        public int featsMultiplier = 1;
        public float travelSpeedMultiplier = 1;
        public int characterCreationAbilityPointsMax = 18;
        public int characterCreationAbilityPointsMin = 7;
        public int characterCreationAbilityPointsPlayer = 25;
        public int characterCreationAbilityPointsMerc = 20;
        public float companionCostMultiplier = 1;
        public float kingdomBuildingTimeModifier = 0;
        public float partyMovementSpeedMultiplier = 1;
        public float enemyBaseHitPointsMultiplier = 1;
        public float buffDurationMultiplierValue = 1;

        // Dice Rolls
        public UnitSelectType allHitsCritical = UnitSelectType.Off;
        public UnitSelectType rollWithAdvantage = UnitSelectType.Off;
        public UnitSelectType rollWithDisadvantage = UnitSelectType.Off;
        public UnitSelectType alwaysRoll20 = UnitSelectType.Off;
        public UnitSelectType neverRoll20 = UnitSelectType.Off;
        public UnitSelectType alwaysRoll1 = UnitSelectType.Off;
        public UnitSelectType neverRoll1 = UnitSelectType.Off;
        public UnitSelectType roll20Initiative = UnitSelectType.Off;
        public UnitSelectType roll1Initiative = UnitSelectType.Off;

        // Party Editor
        public int selectedPartyFilter = 0;

        // Blueprint Browser
        public int searchLimit = 100;
        public int selectedBPTypeFilter = 1;
        public string searchText = "";
        public bool showAssetIDs = false;
        public bool showDivisions = true;

        // Previews (Dialogs, Events ,etc)

        public bool previewEventResults = false;
        public bool previewDialogResults = false;
        public bool previewAlignmentRestrictedDialog = false;
        public bool previewRandomEncounters = false;

        // Quests
        public bool hideCompleted = true;

        // Other
        public bool settingShowDebugInfo = true;

        // Deprecated
        public bool toggleNoLevelUpRestirctions = false;    // deprecated

        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }
    }
}
