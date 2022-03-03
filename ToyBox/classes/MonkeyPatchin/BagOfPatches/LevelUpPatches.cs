// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Skills;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.FeatureSelector;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.LevelClassScores.Experience;
using Kingmaker.UI.ServiceWindow;
using UnityEngine;
using ModKit;
using System.Reflection;
using System.Reflection.Emit;

namespace ToyBox.BagOfPatches {
    internal static class LevelUp {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.CanLevelUp))]
        private static class LevelUpController_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleNoLevelUpRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(UnitProgressionData))]
        private static class UnitProgressionData_LegendaryHero_Patch {
            // note: no need to set AssetGuid or anything, 'Bonuses' is the only field accessed
            private static BlueprintStatProgression XPcontinuous = new() { Bonuses = new int[] {
                0,0,2000,5000,9000,15000,23000,35000,51000,75000,105000,155000,220000,315000,445000,635000,890000,1300000,1800000,2550000,
                3600000,4650000,5700000,6750000,7800000,8850000,9900000,10950000,12000000,13050000,14100000,15150000,16200000,17250000,
                18300000,19350000,20400000,21450000,22500000,23550000,24600000 }};

            private static BlueprintStatProgression XPexponential = new() { Bonuses = new int[] {
                0,0,2000,5000,9000,15000,23000,35000,51000,75000,105000,155000,220000,315000,445000,635000,890000,1300000,1800000,2550000,
                3600000,5700000,9900000,18300000,35100000 }};

            [HarmonyPatch(nameof(UnitProgressionData.ExperienceTable), MethodType.Getter)]
            private static bool Prefix(ref BlueprintStatProgression __result, UnitProgressionData __instance) {
                var hashKey = __instance.Owner.HashKey();
                var perSave = settings.perSave;
                if (perSave is null) return true;
                perSave.charIsLegendaryHero.TryGetValue(hashKey, out var isFakeLegendaryHero);
                //Mod.Trace($"UnitProgressionData_ExperienceTable - {__instance.Owner.CharacterName.orange()} isFakeLegoHero:{isFakeLegendaryHero}");

                if (__instance.Owner.State.Features.LegendaryHero || isFakeLegendaryHero)
                    __result = Game.Instance.BlueprintRoot.Progression.LegendXPTable;
                else if (settings.toggleContinousLevelCap)
                    __result = XPcontinuous;
                else if (settings.toggleExponentialLevelCap)
                    __result = XPexponential;
                else
                    return true;

                return false;
            }

            [HarmonyPatch(nameof(UnitProgressionData.MaxCharacterLevel), MethodType.Getter)]
            private static bool Prefix(ref int __result, UnitProgressionData __instance) {
                var hashKey = __instance.Owner.HashKey();
                var perSave = settings.perSave;
                if (perSave is null) return true;
                perSave.charIsLegendaryHero.TryGetValue(hashKey, out var isFakeLegendaryHero);
                //Mod.Trace ($"UnitProgressionData_MaxCharacterLevel - {__instance.Owner.CharacterName.orange()} isFakeLegoHero:{isFakeLegendaryHero}");

                if (__instance.Owner.State.Features.LegendaryHero || isFakeLegendaryHero)
                    __result = 40;
                else if (settings.toggleContinousLevelCap)
                    __result = 40;
                else if (settings.toggleExponentialLevelCap)
                    __result = 24;
                else
                    return true;

                return false;
            }
        }
#if false
        [HarmonyPatch(typeof(UnitProgressionData), nameof(UnitProgressionData.AddClassLevel))]
        public static class UnitProgressionData_AddClassLevel_Patch {
            private static readonly MethodInfo UnitProgressionData_GetExperienceTable =
                AccessTools.PropertyGetter(typeof(UnitProgressionData), "ExperienceTable");

            private static readonly FieldInfo BlueprintStatProgression_GetBonuses =
                AccessTools.Field(typeof(BlueprintStatProgression), "Bonuses");

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                var codes = new List<CodeInstruction>(instructions);
                var target = FindInsertionPoint(codes);
                if (target < 0) {
                    Mod.Error("UnitProgressionData_AddClassLevel_Patch Transpiler unable to find target!");
                    return codes;
                }

                codes[target] = new CodeInstruction(OpCodes.Nop);
                codes[target + 1] = new CodeInstruction(OpCodes.Ldarg_0);
                codes[target + 2] =
                    new CodeInstruction(new CodeInstruction(OpCodes.Callvirt, UnitProgressionData_GetExperienceTable));
                codes[target + 3] = new CodeInstruction(OpCodes.Ldfld, BlueprintStatProgression_GetBonuses);

                return codes;
            }
            private static int FindInsertionPoint(List<CodeInstruction> codes) {
                for (var i = 0; i < codes.Count; i++) {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].LoadsField(BlueprintStatProgression_GetBonuses)) {
                        return i - 3;
                    }
                }

                Mod.Error("UnitProgressionData_AddClassLevel_Patch: COULD NOT FIND TARGET");
                return -1;
            }
        }
#endif

        [HarmonyPatch(typeof(CharSheetCommonLevel), nameof(CharSheetCommonLevel.Initialize))]
        private static class CharSheetCommonLevel_FixExperienceBar_Patch {
            public static void Postfix(UnitProgressionData data, ref CharSheetCommonLevel __instance) {
                __instance.Level.text = "Level " + data.CharacterLevel;
                var nextLevel = data.ExperienceTable.Bonuses[data.CharacterLevel + 1];
                var currentLevel = data.ExperienceTable.Bonuses[data.CharacterLevel];
                var experience = data.Experience;
                __instance.Exp.text = $"{experience as object}/{nextLevel as object}";
                __instance.Bar.value = (float)(experience - currentLevel) / (float)(nextLevel - currentLevel);
            }
        }

        [HarmonyPatch(typeof(CharInfoExperienceVM), nameof(CharInfoExperienceVM.RefreshData))]
        private static class CharInfoExperienceVM_FixExperienceBar_Patch {
            public static void Postfix(ref CharInfoExperienceVM __instance) {
                var unit = __instance.Unit.Value;
                __instance.NextLevelExp = unit.Progression.ExperienceTable.Bonuses[Mathf.Min(unit.Progression.CharacterLevel + 1, unit.Progression.ExperienceTable.Bonuses.Length - 1)];
                __instance.CurrentLevelExp = unit.Progression.ExperienceTable.Bonuses.ElementAtOrDefault(unit.Progression.CharacterLevel);
            }
        }

        // ignoreAttributesPointsRemainng
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.IsComplete))]
        private static class StatsDistribution_IsComplete_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreAttributePointsRemaining) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SpendAttributePoint), nameof(SpendAttributePoint.Check))]
        private static class SpendAttributePoint_Check_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreAttributePointsRemaining) {
                    __result = true;
                }
            }
        }
        // ignoreAttributeCap
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.CanAdd), new Type[] { typeof(StatType) })]
        private static class StatsDistribution_CanAdd_Patch {
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
        [HarmonyPatch(typeof(CharGenSkillsPhaseVM), nameof(CharGenSkillsPhaseVM.SelectionStateIsCompleted))]
        private static class CharGenSkillsPhaseVM_SelectionStateIsCompleted_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreSkillPointsRemaining) {
                    __result = true;
                }
            }
        }
        // ignoreSkillPointsRemaing, ignoreSkillCap
        [HarmonyPatch(typeof(SpendSkillPoint), nameof(SpendSkillPoint.Check), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        private static class SpendSkillPoint_Check_Patch {
            public static bool Prefix(SpendSkillPoint __instance) => !(settings.toggleIgnoreSkillCap || settings.toggleIgnoreSkillPointsRemaining);
            private static void Postfix(ref bool __result, SpendSkillPoint __instance, LevelUpState state, UnitDescriptor unit) => __result = StatTypeHelper.Skills.Contains<StatType>(__instance.Skill)
                    && (settings.toggleIgnoreSkillCap || unit.Stats.GetStat(__instance.Skill).BaseValue < state.NextCharacterLevel)
                    && (settings.toggleIgnoreSkillPointsRemaining || state.SkillPointsRemaining > 0);
        }
        // ignoreSkillCap
        [HarmonyPatch(typeof(CharGenSkillAllocatorVM), nameof(CharGenSkillAllocatorVM.UpdateSkillAllocator))]
        private static class CharGenSkillAllocatorVM_UpdateSkillAllocator_Patch {
            public static bool Prefix(CharGenSkillAllocatorVM __instance) {
                if (settings.toggleIgnoreSkillCap) {
                    __instance.IsClassSkill.Value = (bool)__instance.Skill?.ClassSkill;
                    var stat1 = __instance.m_LevelUpController.Unit.Stats.GetStat(__instance.StatType);
                    var stat2 = __instance.m_LevelUpController.Preview.Stats.GetStat(__instance.StatType);
                    __instance.CanAdd.Value = !__instance.m_LevelUpController.State.IsSkillPointsComplete() && __instance.m_LevelUpController.State.SkillPointsRemaining > 0;
                    __instance.CanRemove.Value = stat2.BaseValue > stat1.BaseValue;
                    return false;
                }
                return true;
            }
        }

        // full HD
        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.ApplyHitPoints), new Type[] { typeof(LevelUpState), typeof(ClassData), typeof(UnitDescriptor) })]
        private static class ApplyClassMechanics_ApplyHitPoints_Patch {
            private static void Postfix(LevelUpState state, ClassData classData, ref UnitDescriptor unit) {
                if (settings.toggleFullHitdiceEachLevel && unit.IsPartyOrPet() && state.NextClassLevel > 1) {
                    var newHitDie = ((int)classData.CharacterClass.HitDie / 2) - 1;
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
        [HarmonyPatch(typeof(PrerequisiteFeature), nameof(PrerequisiteFeature.CheckInternal))]
        private static class PrerequisiteFeature_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreFeaturePrerequisites) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteFeaturesFromList), nameof(PrerequisiteFeaturesFromList.CheckInternal))]
        private static class PrerequisiteFeaturesFromList_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreFeatureListPrerequisites) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(FeatureSelectionState), nameof(FeatureSelectionState.IgnorePrerequisites), MethodType.Getter)]
        private static class FeatureSelectionState_IgnorePrerequisites_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleFeaturesIgnorePrerequisites) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(IgnorePrerequisites), nameof(IgnorePrerequisites.Ignore), MethodType.Getter)]
        private static class IgnorePrerequisites_Ignore_Patch {
            private static void Postfix(ref bool __result) {
                if (Game.Instance.LevelUpController == null) return;
                var state = Game.Instance.LevelUpController.State;

                if (!state.IsClassSelected) {
                    if (settings.toggleIgnoreClassRestrictions) {
                        __result = true;
                    }
                }
                else {
                    if (settings.toggleIgnoreFeatRestrictions) {
                        __result = true;
                    }
                }
            }
        }
#if false
        [HarmonyPatch(typeof(CharGenMythicPhaseVM), nameof(CharGenMythicPhaseVM.IsClassVisible))]
        static class CharGenMythicPhaseVM_IsClassVisible_Patch {
            private static void Postfix(ref bool __result, BlueprintCharacterClass charClass) {
                Logger.Log("IsClassVisible");
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(CharGenMythicPhaseVM), nameof(CharGenMythicPhaseVM.IsClassAvailableToSelect))]
        static class CharGenMythicPhaseVM_IsClassAvailableToSelect_Patch {
            private static void Postfix(ref bool __result, BlueprintCharacterClass charClass) {
                Logger.Log("CharGenMythicPhaseVM.IsClassAvailableToSelect");
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(CharGenMythicPhaseVM), nameof(CharGenMythicPhaseVM.IsPossibleMythicSelection), MethodType.Getter)]
        static class CharGenMythicPhaseVM_IsPossibleMythicSelection_Patch {
            private static void Postfix(ref bool __result) {
                Logger.Log("CharGenMythicPhaseVM.IsPossibleMythicSelection");
                if (settings.toggleIgnoreClassAndFeatRestrictions) {
                    __result = true;
                }
            }
        }
#endif

        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.IsPossibleMythicSelection), MethodType.Getter)]
        private static class LevelUpControllerIsPossibleMythicSelection_Patch {
            private static void Postfix(ref bool __result, LevelUpController __instance) {
                if (settings.toggleIgnoreClassRestrictions || settings.toggleAllowCompanionsToBecomeMythic && !__instance.Unit.IsMainCharacter) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintCharacterClass), nameof(BlueprintCharacterClass.MeetsPrerequisites))]
        private static class BlueprintCharacterClass_MeetsPrerequisites_Patch {
            private static void Postfix(ref bool __result, BlueprintCharacterClass __instance, [NotNull] UnitDescriptor unit, [NotNull] LevelUpState state) {
                if (!settings.toggleAllowCompanionsToBecomeMythic || unit.IsMainCharacter || !__instance.IsMythic) return;

                if (__instance == BlueprintRoot.Instance.Progression.MythicCompanionClass &&
                    unit.Progression.LastMythicClass != __instance && unit.Progression.LastMythicClass != null) {
                    __result = false;
                    return;
                }

                if (state.NextMythicLevel == 8 && __instance.m_IsHigherMythic) {
                    __result = true;
                    return;
                }

                if (unit.Progression.LastMythicClass == __instance) {
                    __result = true;
                    return;
                }

                if (state.NextMythicLevel == 3 && !__instance.m_IsHigherMythic) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteCasterTypeSpellLevel), nameof(PrerequisiteCasterTypeSpellLevel.CheckInternal))]
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
        [HarmonyPatch(typeof(PrerequisiteNoArchetype), nameof(PrerequisiteNoArchetype.CheckInternal))]
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

        [HarmonyPatch(typeof(PrerequisiteStatValue), nameof(PrerequisiteStatValue.CheckInternal))]
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

        [HarmonyPatch(typeof(PrerequisiteClassLevel), nameof(PrerequisiteClassLevel.CheckInternal))]
        public static class PrerequisiteClassLevel_Check_Patch {
            public static void Postfix(
                    [CanBeNull] FeatureSelectionState selectionState,
                    [NotNull] UnitDescriptor unit,
                    [CanBeNull] LevelUpState state,
                    PrerequisiteClassLevel __instance,
                    ref bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (!__result && settings.toggleIgnorePrerequisiteClassLevel && !__instance.HideInUI) {
                    var characterClass = (BlueprintCharacterClass)(__instance.m_CharacterClass).GetBlueprint();
                    if (!characterClass.HideIfRestricted && !characterClass.IsMythic)
                        __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintRace), nameof(BlueprintRace.GetRestrictedByArchetype))]
        private static class BlueprintRace_GetRestrictedByArchetype_Patch {
            public static bool Prefix(BlueprintRace __instance, UnitDescriptor unit, ref BlueprintArchetype __result) {
                if (!settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass) return true;
                __result = null;
                return false;
            }
        }

        [HarmonyPatch(typeof(CharGenClassSelectorItemVM), nameof(CharGenClassSelectorItemVM.SpecialRequiredRace), MethodType.Getter)]
        private static class CharGenClassSelectorItemVM_SpecialRequiredRace_Patch {
            public static bool Prefix(CharGenClassSelectorItemVM __instance, ref BlueprintRace __result) {
                if (!settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass) return true;
                __result = null;
                return false;
            }
        }


    [HarmonyPatch(typeof(PrerequisiteFeature))]
        public static class PrerequisiteFeature_CheckInternal_Patch {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(PrerequisiteFeature.CheckInternal))]
            public static void PostfixCheckInternal([NotNull] UnitDescriptor unit, ref bool __result) {
                if (!unit.IsPartyOrPet()) {
                    return;
                }

                if (settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass) {
                    __result = true;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(PrerequisiteFeature.ConsiderFulfilled))]
            public static void PostfixConsiderFulfilled([NotNull] UnitDescriptor unit, ref bool __result) {
                if (!unit.IsPartyOrPet()) {
                    return;
                }

                if (settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteAlignment), nameof(PrerequisiteAlignment.CheckInternal))]
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

        [HarmonyPatch(typeof(PrerequisiteNoFeature), nameof(PrerequisiteNoFeature.CheckInternal))]
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
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.AddCasterLevel))]
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
        [HarmonyPatch(typeof(SpellSelectionData), nameof(SpellSelectionData.CanSelectAnything), new Type[] { typeof(UnitDescriptor) })]
        public static class SpellSelectionData_CanSelectAnything_Patch {
            public static void Postfix(UnitDescriptor unit, ref bool __result) {
                if (!unit.IsPartyOrPet()) return; // don't give extra feats to NPCs
                if (settings.toggleSkipSpellSelection) {
                    __result = false;
                }
            }
        }

        // Let user advance if no options left for feat selection
        [HarmonyPatch(typeof(CharGenFeatureSelectorPhaseVM), nameof(CharGenFeatureSelectorPhaseVM.CheckIsCompleted))]
        private static class CharGenFeatureSelectorPhaseVM_CheckIsCompleted_Patch {
            private static void Postfix(CharGenFeatureSelectorPhaseVM __instance, ref bool __result) {
                if (settings.toggleOptionalFeatSelection) {
                    __result = true;
                }
                else if (settings.toggleNextWhenNoAvailableFeatSelections || settings.featsMultiplier != 1) {
                    var featureSelectorStateVM = __instance.FeatureSelectorStateVM;
                    var selectionState = featureSelectorStateVM.SelectionState;
                    var selectionVM = __instance.FeatureSelectorStateVM;
                    var state = Game.Instance.LevelUpController.State;
                    IFeatureSelection selection = selection = selectionVM.Feature as IFeatureSelection;
                    var availableItems = selection?.Items
                        .Where((IFeatureSelectionItem item) => selection.CanSelect(state.Unit, state, selectionState, item));
                    //Main.Log($"CharGenFeatureSelectorPhaseVM_CheckIsCompleted_Patch - availableCount: {availableItems.Count()}");
                    if (availableItems.Count() == 0)
                        __result = true;
                }
            }
        }
#if false
        [HarmonyPatch(typeof(ProgressionData), nameof(ProgressionData.CalculateLevelEntries))]
        public static class ProgressionData_CalculateLevelEntries_Patch {
            public static bool Prefix(ProgressionData __instance, ref LevelEntry[] __result) {
                var featMultiplier = settings.featsMultiplier;
                if (featMultiplier < 2) return true;
                List<LevelEntry> levelEntryList = new List<LevelEntry>();
                foreach (LevelEntry levelEntry in __instance.Blueprint.LevelEntries) {
                    int level = levelEntry.Level;
                    Main.Log($"levelEntry {level} - {string.Join(", ", levelEntry.Features.Select(f => f.name.cyan()))}");
                    var blueprintFeatureBaseList = new List<BlueprintFeatureBase>(levelEntry.Features);
                    foreach (BlueprintArchetype archetype in __instance.Archetypes) {
                        foreach (BlueprintFeatureBase feature in (IEnumerable<BlueprintFeatureBase>)archetype.GetRemoveEntry(level).Features)
                            blueprintFeatureBaseList.Remove(feature);
                        Main.Log($"adding archetype: {archetype.name.cyan()} - {levelEntry.Level}");
                        blueprintFeatureBaseList.AddRange((IEnumerable<BlueprintFeatureBase>)archetype.GetAddEntry(level).Features);
                    }
                    if (blueprintFeatureBaseList.Count > 0) {
                        LevelEntry levelEntry2 = new LevelEntry() {
                            Level = level
                        };
                        var features = new List<BlueprintFeatureBase>();
                        for (int ii = 0; ii < featMultiplier; ii++) {
                            features = features.Concat(blueprintFeatureBaseList).ToList();
                        }
                        levelEntry2.SetFeatures(features);
                        levelEntryList.Add(levelEntry2);
                    }
                }
                levelEntryList.Sort((e1, e2) => e1.Level.CompareTo(e2.Level));
                __result = levelEntryList.ToArray();
                return false;
            }
        }
        //if (__instance.Archetypes.Count <= 0) {
        //    List<LevelEntry> levelEntries = new List<LevelEntry>();
        //    foreach (LevelEntry levelEntry in __instance.Blueprint.LevelEntries) {
        //        Main.Log($"adding level entry - {levelEntry.Level}}");
        //        for (int ii = 0; ii < featMultiplier; ii++) {
        //            levelEntries.Add(levelEntry);
        //        }
        //    }
        //    levelEntries.Sort((e1, e2) => e1.Level.CompareTo(e2.Level));
        //    __result = levelEntries.ToArray();
        //    return false;
        //}

#else
        /**
         * The feat multiplier is the source of several hard to track down bugs. To quote ArcaneTrixter:
         * All story companions feats/backgrounds/etc. most notably a certain wizard who unlearns how to cast spells if your multiplier is at least 8. Also this is retroactive if you ever level up in the future with the multiplier on.
         * All mythic 'fake' companions like Skeleton Minion for lich or Azata summon.
         * Required adding in "skip feat selection" because it broke level ups.
         * Causes certain gestalt combinations to give sudden ridiculous level-ups of companions or sneak attack or kinetic blast.
        */
        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.AddFeaturesFromProgression))]
        public static class MultiplyFeatPoints_LevelUpHelper_AddFeatures_Patch {
            public static bool Prefix(
                [NotNull] LevelUpState state,
                [NotNull] UnitDescriptor unit,
                [NotNull] IList<BlueprintFeatureBase> features,
                FeatureSource source,
                int level) {
                if (settings.featsMultiplier < 2 || (!settings.toggleFeatureMultiplierCompanions && !unit.IsMainCharacter)) return true;
                //Main.Log($"name: {unit.CharacterName} isMemberOrPet:{unit.IsPartyMemberOrPet()}".cyan().bold());
                if (!unit.IsPartyOrPet()) return true;
                Mod.Trace($"Log adding {settings.featsMultiplier}x features from {source.Blueprint.name.orange()} : {source.Blueprint.GetType().Name.yellow()} for {unit.CharacterName.green()} {string.Join(", ", state.Selections.Select(s => $"{s.Selection}")).cyan()}");
                foreach (var featureBP in features.OfType<BlueprintFeature>()) {
                    Mod.Trace($"    checking {featureBP.NameSafe().cyan()} : {featureBP.GetType().Name.yellow()}");
                    var multiplier = settings.featsMultiplier;
                    for (var i = 0; i < multiplier; ++i) {
                        if (featureBP.MeetsPrerequisites(null, unit, state, true)) {
                            if (featureBP is IFeatureSelection selection && (!selection.IsSelectionProhibited(unit) || selection.IsObligatory())) {
                                Mod.Trace($"    adding: {featureBP.NameSafe().cyan()}".orange());
                                state.AddSelection(null, source, selection, level);
                            }
                        }
                    }
                    var feature = (Kingmaker.UnitLogic.Feature)unit.AddFact(featureBP);
                    var source1 = source;
                    var level1 = level;
                    feature.SetSource(source1, level1);
                    if (featureBP is BlueprintProgression progression) {
                        Mod.Trace($"    updating unit: {unit.CharacterName.orange()} {progression} bp: {featureBP.NameSafe()}".cyan());
                        LevelUpHelper.UpdateProgression(state, unit, progression);
                    }
                }
                return false;
            }
        }
        /**
         * This alternative re-targets the multiplier into a Postfix instead of a Prefix to reduce the patch foot print, as well as adds progression white listing to make feature multiplication opt in by the developer instead of just multiplying everything always. As setup in this request only the base feat selections that all characters get will be multiplied, which to my mind best suits the name and description of what this setting does. This should also significantly reduce or resolve several associated bugs due to the reduction of scope on this feature
         */

#endif
#if false
        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.AddFeaturesFromProgression))]
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

                Main.Log($"Log adding {settings.featsMultiplier}x feats for {unit.CharacterName}");
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
}
