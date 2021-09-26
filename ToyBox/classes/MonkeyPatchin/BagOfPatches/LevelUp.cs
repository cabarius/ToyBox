// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Skills;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.FeatureSelector;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.LevelClassScores.Experience;
using Kingmaker.UI.ServiceWindow;
using UnityEngine;
using Kingmaker.UnitLogic.FactLogic;
using ModKit;


namespace ToyBox.BagOfPatches {
    static class LevelUp {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(LevelUpController), "CanLevelUp")]
        static class LevelUpController_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleNoLevelUpRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(UnitProgressionData))]
        static class UnitProgressionData_LegendaryHero_Patch {
            [HarmonyPatch("ExperienceTable", MethodType.Getter)]
            private static void Postfix(ref BlueprintStatProgression __result, UnitProgressionData __instance) {
                settings.charIsLegendaryHero.TryGetValue(__instance.Owner.HashKey(), out bool isFakeLegendaryHero);
                bool legendaryHero = __instance.Owner.State.Features.LegendaryHero || isFakeLegendaryHero;
                __result = !legendaryHero
                        ? Game.Instance.BlueprintRoot.Progression.XPTable
                        : Game.Instance.BlueprintRoot.Progression.LegendXPTable.Or(null)
                          ?? Game.Instance.BlueprintRoot.Progression.XPTable;
            }

            [HarmonyPatch("MaxCharacterLevel", MethodType.Getter)]
            private static void Postfix(ref int __result, UnitProgressionData __instance) {
                settings.charIsLegendaryHero.TryGetValue(__instance.Owner.HashKey(), out bool isFakeLegendaryHero);
                bool isLegendaryHero = __instance.Owner.State.Features.LegendaryHero || isFakeLegendaryHero;
                if (isLegendaryHero) {
                    __result = 40;
                }
            }
        }

        [HarmonyPatch(typeof(CharSheetCommonLevel), "Initialize")]
        static class CharSheetCommonLevel_FixExperienceBar_Patch {
            public static void Postfix(UnitProgressionData data, ref CharSheetCommonLevel __instance) {
                __instance.Level.text = "Level " + data.CharacterLevel;
                int nextLevel = data.ExperienceTable.Bonuses[data.CharacterLevel + 1];
                int currentLevel = data.ExperienceTable.Bonuses[data.CharacterLevel];
                int experience = data.Experience;
                __instance.Exp.text = $"{experience as object}/{nextLevel as object}";
                __instance.Bar.value = (float)(experience - currentLevel) / (float)(nextLevel - currentLevel);
            }
        }

        [HarmonyPatch(typeof(CharInfoExperienceVM), "RefreshData")]
        static class CharInfoExperienceVM_FixExperienceBar_Patch {
            public static void Postfix(ref CharInfoExperienceVM __instance) {
                var unit = __instance.Unit.Value;
                __instance.NextLevelExp = unit.Progression.ExperienceTable.Bonuses[Mathf.Min(unit.Progression.CharacterLevel + 1, unit.Progression.ExperienceTable.Bonuses.Length - 1)];
                __instance.CurrentLevelExp = unit.Progression.ExperienceTable.Bonuses.ElementAtOrDefault(unit.Progression.CharacterLevel);
            }
        }

        // ignoreAttributesPointsRemainng
        [HarmonyPatch(typeof(StatsDistribution), "IsComplete")]
        static class StatsDistribution_IsComplete_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreAttributePointsRemaining) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SpendAttributePoint), "Check")]
        static class SpendAttributePoint_Check_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreAttributePointsRemaining) {
                    __result = true;
                }
            }
        }
        // ignoreAttributeCap
        [HarmonyPatch(typeof(StatsDistribution), "CanAdd", new Type[] { typeof(StatType) })]
        static class StatsDistribution_CanAdd_Patch {
            /*
            public static bool Prefix() {
                return !settings.toggleIgnoreAttributeCap;
            }
            
            private static void Postfix(ref bool __result, StatsDistribution __instance, StatType attribute) {
               __result = __instance.Available 
                    && (settings.toggleIgnoreAttributeCap || __instance.StatValues[attribute] < 18)
                    && (__instance.GetAddCost(attribute) <= __instance.Points);
            }
            */
            private static void Postfix(ref bool __result, StatsDistribution __instance) {
                if (settings.toggleIgnoreAttributeCap && __instance.Available) {
                    __result = true;
                }
            }
        }
        // ignoreSkillPointsRemaining
        [HarmonyPatch(typeof(CharGenSkillsPhaseVM), "SelectionStateIsCompleted")]
        static class CharGenSkillsPhaseVM_SelectionStateIsCompleted_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreSkillPointsRemaining) {
                    __result = true;
                }
            }
        }
        // ignoreSkillPointsRemaing, ignoreSkillCap
        [HarmonyPatch(typeof(SpendSkillPoint), "Check", new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SpendSkillPoint_Check_Patch {
            public static bool Prefix(SpendSkillPoint __instance) {
                return !(settings.toggleIgnoreSkillCap || settings.toggleIgnoreSkillPointsRemaining);
            }
            private static void Postfix(ref bool __result, SpendSkillPoint __instance, LevelUpState state, UnitDescriptor unit) {
                __result = (StatTypeHelper.Skills).Contains<StatType>(__instance.Skill)
                    && (settings.toggleIgnoreSkillCap || unit.Stats.GetStat(__instance.Skill).BaseValue < state.NextCharacterLevel)
                    && (settings.toggleIgnoreSkillPointsRemaining || state.SkillPointsRemaining > 0);
            }
        }
        // ignoreSkillCap
        [HarmonyPatch(typeof(CharGenSkillAllocatorVM), "UpdateSkillAllocator")]
        static class CharGenSkillAllocatorVM_UpdateSkillAllocator_Patch {
            public static bool Prefix(CharGenSkillAllocatorVM __instance) {
                if (settings.toggleIgnoreSkillCap) {
                    __instance.IsClassSkill.Value = (bool)__instance.Skill?.ClassSkill;
                    ModifiableValue stat1 = __instance.m_LevelUpController.Unit.Stats.GetStat(__instance.StatType);
                    ModifiableValue stat2 = __instance.m_LevelUpController.Preview.Stats.GetStat(__instance.StatType);
                    __instance.CanAdd.Value = !__instance.m_LevelUpController.State.IsSkillPointsComplete() && __instance.m_LevelUpController.State.SkillPointsRemaining > 0;
                    __instance.CanRemove.Value = stat2.BaseValue > stat1.BaseValue;
                    return false;
                }
                return true;
            }
        }

        // full HD
        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyHitPoints", new Type[] { typeof(LevelUpState), typeof(ClassData), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_ApplyHitPoints_Patch {
            private static void Postfix(LevelUpState state, ClassData classData, ref UnitDescriptor unit) {
                if (settings.toggleFullHitdiceEachLevel && unit.IsPartyOrPet() && state.NextClassLevel > 1) {
                    int newHitDie = ((int)classData.CharacterClass.HitDie / 2) - 1;
                    unit.Stats.HitPoints.BaseValue += newHitDie;
                }
#if false
                else if (StringUtils.ToToggleBool(settings.toggleRollHitDiceEachLevel) && unit.IsPartyMemberOrPet() && state.NextLevel > 1) {
                    int oldHitDie = ((int)classData.CharacterClass.HitDie / 2) + 1;
                    DiceFormula diceFormula = new DiceFormula(1, classData.CharacterClass.HitDie);
                    int roll = RuleRollDice.Dice.D(diceFormula);

                    unit.Stats.HitPoints.BaseValue -= oldHitDie;
                    unit.Stats.HitPoints.BaseValue += roll;
                }
#endif
            }
        }
        [HarmonyPatch(typeof(PrerequisiteFeature), "CheckInternal")]
        static class PrerequisiteFeature_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreFeaturePrerequisites) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteFeaturesFromList), "CheckInternal")]
        static class PrerequisiteFeaturesFromList_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreFeatureListPrerequisites) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(FeatureSelectionState), "IgnorePrerequisites", MethodType.Getter)]
        static class FeatureSelectionState_IgnorePrerequisites_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleFeaturesIgnorePrerequisites) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(IgnorePrerequisites), "Ignore", MethodType.Getter)]
        static class IgnorePrerequisites_Ignore_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }
#if false
        [HarmonyPatch(typeof(CharGenMythicPhaseVM), "IsClassVisible")]
        static class CharGenMythicPhaseVM_IsClassVisible_Patch {
            private static void Postfix(ref bool __result, BlueprintCharacterClass charClass) {
                Logger.Log("IsClassVisible");
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(CharGenMythicPhaseVM), "IsClassAvailableToSelect")]
        static class CharGenMythicPhaseVM_IsClassAvailableToSelect_Patch {
            private static void Postfix(ref bool __result, BlueprintCharacterClass charClass) {
                Logger.Log("CharGenMythicPhaseVM.IsClassAvailableToSelect");
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(CharGenMythicPhaseVM), "IsPossibleMythicSelection", MethodType.Getter)]
        static class CharGenMythicPhaseVM_IsPossibleMythicSelection_Patch {
            private static void Postfix(ref bool __result) {
                Logger.Log("CharGenMythicPhaseVM.IsPossibleMythicSelection");
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }
#endif

        [HarmonyPatch(typeof(LevelUpController), "IsPossibleMythicSelection", MethodType.Getter)]
        static class LevelUpControllerIsPossibleMythicSelection_Patch {
            private static void Postfix(ref bool __result) {
                //Logger.Log($"LevelUpController.IsPossibleMythicSelection {settings.toggleIgnoreClassAndFeatRestrictions}");
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteCasterTypeSpellLevel), "CheckInternal")]
        public static class PrerequisiteCasterTypeSpellLevel_Check_Patch {
            public static void Postfix(
                    [CanBeNull] FeatureSelectionState selectionState,
                    [NotNull] UnitDescriptor unit,
                    [CanBeNull] LevelUpState state,
                    ref bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (settings.toggleIgnoreCasterTypeSpellLevel) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteNoArchetype), "CheckInternal")]
        public static class PrerequisiteNoArchetype_Check_Patch {
            public static void Postfix(
                    [CanBeNull] FeatureSelectionState selectionState,
                    [NotNull] UnitDescriptor unit,
                    [CanBeNull] LevelUpState state,
                    ref bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (settings.toggleIgnoreForbiddenArchetype) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteStatValue), "CheckInternal")]
        public static class PrerequisiteStatValue_Check_Patch {
            public static void Postfix(
                    [CanBeNull] FeatureSelectionState selectionState,
                    [NotNull] UnitDescriptor unit,
                    [CanBeNull] LevelUpState state,
                    ref bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (settings.toggleIgnorePrerequisiteStatValue) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteFeature))]
        public static class PrerequisiteFeature_CheckInternal_Patch {
            [HarmonyPostfix]
            [HarmonyPatch("CheckInternal")]
            public static void PostfixCheckInternal([NotNull] UnitDescriptor unit, ref bool __result) {
                if (!unit.IsPartyOrPet()) {
                    return;
                }

                if (settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass) {
                    __result = true;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("ConsiderFulfilled")]
            public static void PostfixConsiderFulfilled([NotNull] UnitDescriptor unit, ref bool __result) {
                if (!unit.IsPartyOrPet()) {
                    return;
                }

                if (settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteAlignment), "CheckInternal")]
        public static class PrerequisiteAlignment_Check_Patch {
            public static void Postfix(
                    [CanBeNull] FeatureSelectionState selectionState,
                    [NotNull] UnitDescriptor unit,
                    [CanBeNull] LevelUpState state,
                    ref bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (settings.toggleIgnoreAlignmentWhenChoosingClass) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteNoFeature), "CheckInternal")]
        public static class PrerequisiteNoFeature_Check_Patch {
            public static void Postfix(
                    [CanBeNull] FeatureSelectionState selectionState,
                    [NotNull] UnitDescriptor unit,
                    [CanBeNull] LevelUpState state,
                    ref bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (settings.toggleIgnoreForbiddenFeatures) {
                    __result = true;
                }
            }
        }
#if false
        [HarmonyPatch(typeof(Spellbook), "AddCasterLevel")]
        public static class Spellbook_AddCasterLevel_Patch {
            public static bool Prefix() {
                return false;
            }

            public static void Postfix(ref Spellbook __instance, ref int ___m_CasterLevelInternal, List<BlueprintSpellList> ___m_SpecialLists) {
                int maxSpellLevel = __instance.MaxSpellLevel;
                ___m_CasterLevelInternal += settings.addCasterLevel;
                int maxSpellLevel2 = __instance.MaxSpellLevel;
                if (__instance.Blueprint.AllSpellsKnown) {
                    Traverse addSpecialMethod = Traverse.Create(__instance).Method("AddSpecial", new Type[] { typeof(int), typeof(BlueprintAbility) });
                    for (int i = maxSpellLevel + 1; i <= maxSpellLevel2; i++) {
                        foreach (BlueprintAbility spell in __instance.Blueprint.SpellList.GetSpells(i)) {
                            __instance.AddKnown(i, spell);
                        }
                        foreach (BlueprintSpellList specialList in ___m_SpecialLists) {
                            foreach (BlueprintAbility spell2 in specialList.GetSpells(i)) {
                                addSpecialMethod.GetValue(i, spell2);
                            }
                        }
                    }
                }
            }
        }
#endif
        [HarmonyPatch(typeof(SpellSelectionData), "CanSelectAnything", new Type[] { typeof(UnitDescriptor) })]
        public static class SpellSelectionData_CanSelectAnything_Patch {
            public static void Postfix(UnitDescriptor unit, bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (settings.toggleSkipSpellSelection) {
                    __result = false;
                }
            }
        }

        // Let user advance if no options left for feat selection
        [HarmonyPatch(typeof(CharGenFeatureSelectorPhaseVM), "CheckIsCompleted")]
        static class CharGenFeatureSelectorPhaseVM_CheckIsCompleted_Patch {
            private static void Postfix(CharGenFeatureSelectorPhaseVM __instance, ref bool __result) {
                if (settings.toggleOptionalFeatSelection) {
                    __result = true;
                }
                else if (settings.toggleNextWhenNoAvailableFeatSelections || settings.featsMultiplier != 1) {
                    var featureSelectorStateVM = __instance.FeatureSelectorStateVM;
                    var selectionState = featureSelectorStateVM.SelectionState;
                    var selectionVM = __instance.FeatureSelectorStateVM;
                    var state = Game.Instance.LevelUpController.State;
                    IFeatureSelection selection = (selection = (selectionVM.Feature as IFeatureSelection));
                    var availableItems = selection?.Items
                        .Where((IFeatureSelectionItem item) => selection.CanSelect(state.Unit, state, selectionState, item));
                    //modLogger.Log($"CharGenFeatureSelectorPhaseVM_CheckIsCompleted_Patch - availableCount: {availableItems.Count()}");
                    if (availableItems.Count() == 0)
                        __result = true;
                }
            }
        }
        /**
         * The feat multiplier is the source of several hard to track down bugs. To quote ArcaneTrixter:
         * All story companions feats/backgrounds/etc. most notably a certain wizard who unlearns how to cast spells if your multiplier is at least 8. Also this is retroactive if you ever level up in the future with the multiplier on.
         * All mythic 'fake' companions like Skeleton Minion for lich or Azata summon.
         * Required adding in "skip feat selection" because it broke level ups.
         * Causes certain gestalt combinations to give sudden ridiculous level-ups of companions or sneak attack or kinetic blast.
        */
#if true
        [HarmonyPatch(typeof(LevelUpHelper), "AddFeaturesFromProgression")]
        public static class MultiplyFeatPoints_LevelUpHelper_AddFeatures_Patch {
            public static bool Prefix(
                [NotNull] LevelUpState state,
                [NotNull] UnitDescriptor unit,
                [NotNull] IList<BlueprintFeatureBase> features,
                FeatureSource source,
                int level) {
                if (settings.featsMultiplier < 2) return true;
                //modLogger.Log($"name: {unit.CharacterName} isMemberOrPet:{unit.IsPartyMemberOrPet()}".cyan().bold());
                if (!unit.IsPartyOrPet()) return true;
                modLogger.Log($"Log adding {settings.featsMultiplier}x features for {unit.CharacterName}");
                foreach (BlueprintFeature blueprintFeature in features.OfType<BlueprintFeature>()) {
                    for (int i = 0;i < settings.featsMultiplier;++i) {
                        if (blueprintFeature.MeetsPrerequisites((FeatureSelectionState)null, unit, state, true)) {
                            if (blueprintFeature is IFeatureSelection selection && (!selection.IsSelectionProhibited(unit) || selection.IsObligatory())) {
                                modLogger.Log($"    adding: {blueprintFeature.NameSafe()}".cyan());
                                state.AddSelection((FeatureSelectionState)null, source, selection, level);
                            }
                        }
                    }
                    Kingmaker.UnitLogic.Feature feature = (Kingmaker.UnitLogic.Feature)unit.AddFact((BlueprintUnitFact)blueprintFeature);
                    FeatureSource source1 = source;
                    int level1 = level;
                    feature.SetSource(source1, level1);
                    if (blueprintFeature is BlueprintProgression progression) {
                        modLogger.Log($"    updating unit: {unit.CharacterName.orange()} {progression} bp: {blueprintFeature.NameSafe()}".cyan());
                        LevelUpHelper.UpdateProgression(state, unit, progression);
                    }
                }
                return false;
            }
        }






#else
        [HarmonyPatch(typeof(LevelUpHelper), "AddFeaturesFromProgression")]
        public static class MultiplyFeatPoints_LevelUpHelper_AddFeatures_Patch {
            //Defines which progressions are allowed to be multiplied to prevent unexpected behavior
            private static readonly BlueprintGuid[] AllowedProgressions = new BlueprintGuid[] {
                BlueprintGuid.Parse("5b72dd2ca2cb73b49903806ee8986325") //BasicFeatsProgression
            };
            public static void Postfix(
                [NotNull] LevelUpState state,
                [NotNull] UnitDescriptor unit,
                [NotNull] IList<BlueprintFeatureBase> features,
                FeatureSource source,
                int level) {

                if (settings.featsMultiplier < 2) { return; }
                if (!unit.IsPartyOrPet()) { return; }
                if (!AllowedProgressions.Any(allowed => source.Blueprint.AssetGuid.Equals(allowed))) { return; }

                modLogger.Log($"Log adding {settings.featsMultiplier}x feats for {unit.CharacterName}");
                int multiplier = settings.featsMultiplier - 1;
                //We filter to only include feat selections of the feat group to prevent things like deities being multiplied
                var featSelections = features
                    .OfType<BlueprintFeatureSelection>()
                    .Where(s => s.GetGroup() == FeatureGroup.Feat);
                foreach (var selection in featSelections) {
                    if (selection.MeetsPrerequisites(null, unit, state, true)
                        && (!selection.IsSelectionProhibited(unit) || selection.IsObligatory())) {

                        ExecuteByMultiplier(multiplier, () => state.AddSelection(null, source, selection, level));
                    }
                }
                return;
            }
            private static void ExecuteByMultiplier(int multiplier, Action run = null) {
                for (int i = 0;i < multiplier;++i) {
                    run.Invoke();
                }
            }
        }
#endif


    }

    [HarmonyPatch(typeof(BlueprintCharacterClass))]
    public static class BlueprintCharacterClass_Patch {
        [HarmonyPatch("MeetsPrerequisites")]
        public static void Postfix(ref UnitDescriptor unit, BlueprintCharacterClass __instance, ref bool __result) {

            if (Main.settings.toggleUnlockClassUpperLimit) {
                if (!__result) {
                    int classLevel = unit.Progression.GetClassLevel(__instance);

                    if (classLevel >= 20 && classLevel < 40) {
                        __result = true;
                    }
                    if (__instance.PrestigeClass && classLevel < 40 && classLevel >= 10) {
                        __result = true;
                    }
                }

            }
        }
    }
    [HarmonyPatch(typeof(ProgressionData), "GetLevelEntry")]
    public static class ProgressionData_Patch {
        public static bool Prefix(ProgressionData __instance, int level, ref LevelEntry __result) {
            if (Main.settings.toggleUnlockClassUpperLimit) {
                int i = level;
                if (i >= 40) {
                    i = 20;
                }
                if (i > 20) {
                    if (i % 2 == 0) {
                        i = 18;
                    } else {
                        i = 19;

                    }
                }
                level = i;
                __result = __instance.LevelEntries.FirstOrDefault((LevelEntry le) => le.Level == level) ?? new LevelEntry(); ;
                return false;
            }
            return true;
        }


    }
            
  [HarmonyPatch(typeof(ApplyClassProgression), "ApplyProgressionLevel")]
        public static class ApplyClassProgression_Patch {
            private static bool Prefix(ref int level) {
                if (settings.toggleUnlockClassUpperLimit) {
                    int i = level;
                    if (i >= 40) {
                        i = 20;
                    }
                    if (level > 20) {
                        if (i % 2 == 0) {
                            i = 18;
                        } else {
                            i = 19;

                        }

                    }
                    level = i;
                }
                return true;

            }
        }

}
