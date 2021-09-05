// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Skills;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using ModKit;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;

namespace ToyBox.BagOfPatches
{
    static class LevelUp
    {
        public static Settings settings = Main.settings;

        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;

        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(LevelUpController), "CanLevelUp")]
        static class LevelUpController_CanLevelUp_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleNoLevelUpRestrictions)
                {
                    __result = true;
                }
            }
        }

        // ignoreAttributesPointsRemainng
        [HarmonyPatch(typeof(StatsDistribution), "IsComplete")]
        static class StatsDistribution_IsComplete_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreAttributePointsRemaining)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SpendAttributePoint), "Check")]
        static class SpendAttributePoint_Check_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreAttributePointsRemaining)
                {
                    __result = true;
                }
            }
        }

        // ignoreAttributeCap
        [HarmonyPatch(typeof(StatsDistribution), "CanAdd", typeof(StatType))]
        static class StatsDistribution_CanAdd_Patch
        {
            private static void Postfix(ref bool __result, StatsDistribution __instance)
            {
                if (settings.toggleIgnoreAttributeCap && __instance.Available)
                {
                    __result = true;
                }
            }
        }

        // ignoreSkillPointsRemaining
        [HarmonyPatch(typeof(CharGenSkillsPhaseVM), "SelectionStateIsCompleted")]
        static class CharGenSkillsPhaseVM_SelectionStateIsCompleted_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreSkillPointsRemaining)
                {
                    __result = true;
                }
            }
        }

        // ignoreSkillPointsRemaing, ignoreSkillCap
        [HarmonyPatch(typeof(SpendSkillPoint), "Check", typeof(LevelUpState), typeof(UnitDescriptor))]
        static class SpendSkillPoint_Check_Patch
        {
            public static bool Prefix(SpendSkillPoint __instance)
            {
                return !(settings.toggleIgnoreSkillCap || settings.toggleIgnoreSkillPointsRemaining);
            }

            private static void Postfix(ref bool __result, SpendSkillPoint __instance, LevelUpState state, UnitDescriptor unit)
            {
                __result = StatTypeHelper.Skills.Contains(__instance.Skill)
                           && (settings.toggleIgnoreSkillCap || unit.Stats.GetStat(__instance.Skill).BaseValue < state.NextCharacterLevel)
                           && (settings.toggleIgnoreSkillPointsRemaining || state.SkillPointsRemaining > 0);
            }
        }

        // ignoreSkillCap
        [HarmonyPatch(typeof(CharGenSkillAllocatorVM), "UpdateSkillAllocator")]
        static class CharGenSkillAllocatorVM_UpdateSkillAllocator_Patch
        {
            public static bool Prefix(CharGenSkillAllocatorVM __instance)
            {
                if (settings.toggleIgnoreSkillCap && __instance.Skill?.ClassSkill != null)
                {
                    __instance.IsClassSkill.Value = (bool)__instance.Skill?.ClassSkill;

                    ModifiableValue stat1 = __instance.m_LevelUpController.Unit.Stats.GetStat(__instance.StatType);
                    ModifiableValue stat2 = __instance.m_LevelUpController.Preview.Stats.GetStat(__instance.StatType);

                    __instance.CanAdd.Value = !__instance.m_LevelUpController.State.IsSkillPointsComplete() &&
                                              __instance.m_LevelUpController.State.SkillPointsRemaining > 0;

                    __instance.CanRemove.Value = stat2.BaseValue > stat1.BaseValue;

                    return false;
                }

                return true;
            }
        }

        // full HD
        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyHitPoints", typeof(LevelUpState), typeof(ClassData), typeof(UnitDescriptor))]
        static class ApplyClassMechanics_ApplyHitPoints_Patch
        {
            private static void Postfix(LevelUpState state, ClassData classData, ref UnitDescriptor unit)
            {
                if (settings.toggleFullHitdiceEachLevel && unit.IsPlayerFaction && state.NextClassLevel > 1)
                {
                    int newHitDie = (int)classData.CharacterClass.HitDie / 2 - 1;
                    unit.Stats.HitPoints.BaseValue += newHitDie;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteFeature), "CheckInternal")]
        static class PrerequisiteFeature_CanLevelUp_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreFeaturePrerequisites)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteFeaturesFromList), "CheckInternal")]
        static class PrerequisiteFeaturesFromList_CanLevelUp_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreFeatureListPrerequisites)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(FeatureSelectionState), "IgnorePrerequisites", MethodType.Getter)]
        static class FeatureSelectionState_IgnorePrerequisites_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleFeaturesIgnorePrerequisites)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(IgnorePrerequisites), "Ignore", MethodType.Getter)]
        static class IgnorePrerequisites_Ignore_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnorePrerequisites)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(LevelUpController), "IsPossibleMythicSelection", MethodType.Getter)]
        static class LevelUpControllerIsPossibleMythicSelection_Patch
        {
            private static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnorePrerequisites)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteCasterTypeSpellLevel), "CheckInternal")]
        public static class PrerequisiteCasterTypeSpellLevel_Check_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreCasterTypeSpellLevel)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteNoArchetype), "CheckInternal")]
        public static class PrerequisiteNoArchetype_Check_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreForbiddenArchetype)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteStatValue), "CheckInternal")]
        public static class PrerequisiteStatValue_Check_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnorePrerequisiteStatValue)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteAlignment), "CheckInternal")]
        public static class PrerequisiteAlignment_Check_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreAlignmentWhenChoosingClass)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteNoFeature), "CheckInternal")]
        public static class PrerequisiteNoFeature_Check_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleIgnoreForbiddenFeatures)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SpellSelectionData), "CanSelectAnything", typeof(UnitDescriptor))]
        public static class SpellSelectionData_CanSelectAnything_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleSkipSpellSelection)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(LevelUpHelper), "AddFeaturesFromProgression")]
        public static class MultiplyFeatPoints_LevelUpHelper_AddFeatures_Patch
        {
            public static bool Prefix([NotNull] LevelUpState state,
                                      [NotNull] UnitDescriptor unit,
                                      [NotNull] IList<BlueprintFeatureBase> features,
                                      [CanBeNull] FeatureSource source,
                                      int level)
            {
                if (!Main.IsInGame)
                {
                    return false;
                }

                string description = source.Blueprint.GetDescription() ?? "nil";

                foreach (BlueprintFeature blueprintFeature in features
                                                              .OfType<BlueprintFeature>()
                                                              .Where(blueprintFeature => blueprintFeature
                                                                         .MeetsPrerequisites(null, unit, state, true)))
                {
                    if (blueprintFeature is IFeatureSelection selection)
                    {
                        // Bug Fix - due to issues in the implementation of FeatureSelectionState.CanSelectAnything we can get level up blocked so this is an attempt to work around for that
                        int numToAdd = settings.featsMultiplier;

                        if (selection is BlueprintFeatureSelection bpFS)
                        {
                            var bpFeatures = bpFS;
                            var items = bpFS.ExtractSelectionItems(unit, null);
                            int availableCount = items.Count(item => !unit.Progression.Features.HasFact(item.Feature));

                            if (numToAdd > availableCount)
                            {
                                Main.Log($"reduced numToAdd: {numToAdd} -> {availableCount}");
                                numToAdd = availableCount;
                            }
                        }

                        //Logger.Log($"        IFeatureSelection: {selection} adding: {numToAdd}");
                        for (int i = 0; i < numToAdd; ++i)
                        {
                            state.AddSelection(null, source, selection, level);
                        }
                    }

                    Feature feature = (Feature)unit.AddFact(blueprintFeature);

                    if (blueprintFeature is BlueprintProgression progression)
                    {
                        LevelUpHelper.UpdateProgression(state, unit, progression);
                    }

                    feature.SetSource(source, level);
                }

                return false;
            }
        }
    }
}