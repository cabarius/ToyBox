// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityModManagerNet;

namespace ToyBox {
    public class Settings : UnityModManager.ModSettings {
        // Main

        public int selectedTab = 0;

        // Cheap Tricks
        
           // flags
        public bool highlightObjectsToggle = false;
        public bool toggleSpontaneousCopyScrolls = false;
        public bool toggleInstantEvent = false;
        public bool toggleInfiniteAbilities = false;
        public bool toggleInfiniteSkillpoints = false;
        public bool toggleInstantCooldown = false;
        public bool toggleEquipmentRestrictions = false;
        public bool toggleDialogRestrictions = false;
        public bool toggleSettlementRestrictions = false;
        public bool toggleMoveSpeedAsOne = false;
        public bool toggleRestoreSpellsAbilitiesAfterCombat = false;
        public bool toggleInstantRestAfterCombat = false;
        public bool toggleShowAllPartyPortraits = false;
        public bool toggleAccessRemoteCharacters = false;

            // level up
        public bool toggleNoLevelUpRestirctions = false;
        public bool toggleFullHitdiceEachLevel = false;
        public bool toggleIgnorePrerequisites = false;
        public bool toggleIgnoreCasterTypeSpellLevel = false;
        public bool toggleIgnoreForbiddenArchetype = false;
        public bool toggleIgnorePrerequisiteStatValue = false;
        public bool toggleIgnoreAlignmentWhenChoosingClass = false;
        public bool toggleIgnoreFeaturePrerequisitesWhenChoosingClass = false;
        public bool toggleIgnoreForbiddenFeatures = false;
        public bool toggleIgnoreFeaturePrerequisites = false;
        public bool toggleIgnoreFeatureListPrerequisites = false;
        public bool toggleFeaturesIgnorePrerequisites = false;
        public bool toggleInfiniteItems = false;
        public bool toggleNoFriendlyFireForAOE = false;
        public bool toggleMetamagicIsFree = false;
        public bool toggleMaterialComponent = false;
        public bool toggleSkipSpellSelection = false;

        // Multipliers
        public int encumberanceMultiplier = 1;
        public float experienceMultiplier = 1;
        public float moneyMultiplier = 1;
        public float vendorSellPriceMultiplier = 1;
        public float defaultVendorSellPriceMultiplier = 1;
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

        // Party Editor
        public int selectedPartyFilter = 0;

        // Blueprint Browser
        public int searchLimit = 100;
        public int selectedBPTypeFilter = 1;
        public string searchText = "";

        // Previews (Dialogs, Events ,etc)

        public bool previewEventResults = false;
        public bool previewDialogResults = false;
        public bool previewAlignmentRestrictedDialog = false;
        public bool previewRandomEncounters = false;

        // Quests
        public bool hideCompleted = true;

        // Other
        public bool settingShowDebugInfo = true;

        public override void Save(UnityModManager.ModEntry modEntry) {
            Save(this, modEntry);
        }
    }
}
