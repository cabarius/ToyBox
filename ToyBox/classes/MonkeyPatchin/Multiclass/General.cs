// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Linq;
using UnityModManagerNet;

namespace ToyBox.Multiclass {
    static class General {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;

        public static LevelUpController levelUpController { get; internal set; }
        [HarmonyPatch(typeof(LevelUpController), MethodType.Constructor, typeof(UnitEntityData), typeof(bool), typeof(LevelUpState.CharBuildMode))]
        static class LevelUpController_ctor_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static bool Prefix(LevelUpController __instance) {
                if (Main.Enabled) {
                    levelUpController = __instance;
                }
                return true;
            }
        }
#if true
        /*     public static void UpdateProgression(
                                    [NotNull] LevelUpState state,
                                    [NotNull] UnitDescriptor unit,
                                    [NotNull] BlueprintProgression progression)
        */
        [HarmonyPatch(typeof(LevelUpHelper), "UpdateProgression")]
        [HarmonyPatch(new[] { typeof(LevelUpState), typeof(UnitDescriptor), typeof(BlueprintProgression) })]
        static class LevelUpHelper_UpdateProgression_Patch {
            public static bool Prefix([NotNull] LevelUpState state, [NotNull] UnitDescriptor unit, [NotNull] BlueprintProgression progression) {
                if (!settings.toggleMulticlass) return true;
                ProgressionData progressionData = unit.Progression.SureProgressionData(progression);
                int level = progressionData.Level;
                int nextLevel = progressionData.Blueprint.CalcLevel(unit);
                progressionData.Level = nextLevel;
                // TODO - this is from the mod but we need to figure out if max level 20 still makes sense with mythic levels
                // int maxLevel = 20 // unit.Progression.CharacterLevel;
                // if (nextLevel > maxLevel)
                //     nextLevel = maxLevel;
                if (level >= nextLevel || progression.ExclusiveProgression != null && state.SelectedClass != progression.ExclusiveProgression)
                    return false;
                if (!progression.GiveFeaturesForPreviousLevels)
                    level = nextLevel - 1;
                for (int lvl = level + 1; lvl <= nextLevel; ++lvl) {
//                    if (!AllowProceed(progression)) break;
                    LevelEntry levelEntry = progressionData.GetLevelEntry(lvl);
                    LevelUpHelper.AddFeaturesFromProgression(state, unit, levelEntry.Features, (FeatureSource)progression, lvl);
                }
                return false;
            }
            private static bool AllowProceed(BlueprintProgression progression) {
                // SpellSpecializationProgression << shouldn't be applied more than once per character level
                if (!Main.Enabled || Main.multiclassMod == null) return false;
                return Main.multiclassMod.UpdatedProgressions.Add(progression);
                // TODO - what is the following and does it still matter?
                // || progression.AssetGuid != "fe9220cdc16e5f444a84d85d5fa8e3d5";
            }
        }
#endif

        // TODO - figure out what the beta 2 replacement for this is
#if false
        [HarmonyPatch(typeof(CharBSelectionSwitchSpells), "ParseSpellSelection")]
        static class CharBSelectionSwitchSpells_ParseSpellSelection_Patch {
            public static bool Prefix(CharBSelectionSwitchSpells __instance) {
                if (!settings.toggleMulticlass) return true;
                int num = 0;
                __instance.HasEmptyCollections = false;
                foreach (SpellSelectionData spellsCollection in __instance.m_ShowedSpellsCollections) {
                    int newNum = __instance.TryParseMemorizersCollections(spellsCollection, num);
                    num = (newNum == num ? __instance.TryParseSpontaneuosCastersCollections(spellsCollection, num) : newNum);
                }
                __instance.HasSelections = num > 0;
                __instance.HideSelectionViewsFrom(num);
                if (__instance.HasSelections) {
                    if (__instance.HasEmptyCollections)
                        __instance.ActivateNextEmptyItem();
                    else
                        __instance.ActivateCurrentItem();
                }
                else
                    __instance.Hide();
                return false;
            }
        }
#endif

        // Do not proceed the spell selection if the caster level was not changed
        [HarmonyPatch(typeof(ApplySpellbook), "Apply")]
        [HarmonyPatch(new[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch {
            public static bool Prefix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return true;
                if (state.SelectedClass == null)
                    return false;
                SkipLevelsForSpellProgression component1 = state.SelectedClass.GetComponent<SkipLevelsForSpellProgression>();
                if (component1?.Levels.Contains(state.NextClassLevel) == true)
                    return false;
                ClassData classData = unit.Progression.GetClassData(state.SelectedClass);
                if (classData?.Spellbook == null)
                    return false;
                Spellbook spellbook1 = unit.DemandSpellbook(classData.Spellbook);
                if ((bool)state.SelectedClass.Spellbook && state.SelectedClass.Spellbook != classData.Spellbook) {
                    Spellbook spellbook2 = unit.Spellbooks.FirstOrDefault(s => s.Blueprint == state.SelectedClass.Spellbook);
                    if (spellbook2 != null) {
                        foreach (AbilityData allKnownSpell in spellbook2.GetAllKnownSpells())
                            spellbook1.AddKnown(allKnownSpell.SpellLevel, allKnownSpell.Blueprint);
                        unit.DeleteSpellbook(state.SelectedClass.Spellbook);
                    }
                }
                int casterLevelBefore = spellbook1.CasterLevel;
                spellbook1.AddLevelFromClass(classData.CharacterClass);
                int casterLevelAfter = spellbook1.CasterLevel;
                if (casterLevelBefore == casterLevelAfter) return false; // Mod line
                SpellSelectionData spellSelectionData = state.DemandSpellSelection(spellbook1.Blueprint, spellbook1.Blueprint.SpellList);
                if (spellbook1.Blueprint.SpellsKnown != null) {
                    for (int index = 0; index <= 10; ++index) {
                        BlueprintSpellsTable spellsKnown = spellbook1.Blueprint.SpellsKnown;
                        int? count = spellsKnown.GetCount(casterLevelBefore, index);
                        int num1 = count ?? 0;
                        count = spellsKnown.GetCount(casterLevelAfter, index);
                        int num2 = count ?? 0;
                        spellSelectionData.SetLevelSpells(index, Math.Max(0, num2 - num1));
                    }
                }
                int maxSpellLevel = spellbook1.MaxSpellLevel;
                if (spellbook1.Blueprint.SpellsPerLevel > 0) {
                    if (casterLevelBefore == 0) {
                        spellSelectionData.SetExtraSpells(0, maxSpellLevel);
                        spellSelectionData.ExtraByStat = true;
                        spellSelectionData.UpdateMaxLevelSpells(unit);
                    }
                    else
                        spellSelectionData.SetExtraSpells(spellbook1.Blueprint.SpellsPerLevel, maxSpellLevel);
                }
                foreach (AddCustomSpells component2 in spellbook1.Blueprint.GetComponents<AddCustomSpells>())
                    ApplySpellbook.TryApplyCustomSpells(spellbook1, component2, state, unit);
                return false;
            }
        }

        /*
        // Fixed new spell slots (to be calculated not only from the highest caster level when gaining more than one level of a spontaneous caster at a time)
        [HarmonyPatch(typeof(SpellSelectionData), nameof(SpellSelectionData.SetLevelSpells), new Type[] { typeof(int), typeof(int) })]
        static class SpellSelectionData_SetLevelSpells_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SpellSelectionData __instance, int level, ref int count) {
                if (!settings.toggleMulticlass) return;
                if (__instance.LevelCount[level] != null) {
                    count += __instance.LevelCount[level].SpellSelections.Length;
                }
            }
        }

        // Fixed new spell slots (to be calculated not only from the highest caster level when gaining more than one level of a memorizer at a time)
        [HarmonyPatch(typeof(SpellSelectionData), nameof(SpellSelectionData.SetExtraSpells), new Type[] { typeof(int), typeof(int) })]
        static class SpellSelectionData_SetExtraSpells_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static bool Prefix(SpellSelectionData __instance, ref int count, ref int maxLevel) {
                if (!settings.toggleMulticlass) return true;
                if (__instance.ExtraSelected != null) {
                    __instance.ExtraMaxLevel = maxLevel = Math.Max(__instance.ExtraMaxLevel, maxLevel);
                    count += __instance.ExtraSelected.Length;
                }
                return true;
            }
        }
        */
        // TODO - figure out what the replacement is for this in beta2
#if false
        // Fixed the UI for selecting new spells (to refresh the level tabs of the spellbook correctly on toggling the spellbook)
        [HarmonyPatch(typeof(CharBPhaseSpells), "RefreshSpelbookView")]
        static class CharBPhaseSpells_RefreshSpelbookView_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static bool Prefix() {
                return false;
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(CharBPhaseSpells __instance) {
                if (!settings.toggleMulticlass) return;
                __instance.HandleSelectLevel(__instance.ChooseLevelForBook());
            }
        }

        // Fixed the UI for selecting new spells (to switch the spellbook correctly on selecting a spell)
        [HarmonyPatch(typeof(CharacterBuildController), nameof(CharacterBuildController.SetSpell))]
        [HarmonyPatch(new Type[] { typeof(BlueprintAbility), typeof(int), typeof(bool) })]
        static class CharacterBuildController_SetSpell_Patch {

            public static bool Prefix(CharacterBuildController __instance, BlueprintAbility spell, int spellLevel, bool multilevel) {
                if (!settings.toggleMulticlass) return true;
                BlueprintSpellbook spellbook = __instance.Spells.CurrentSpellSelectionData.Spellbook;
                BlueprintSpellList spellList = __instance.Spells.CurrentSpellSelectionData.SpellList;
                int spellsCollectionIndex = __instance.Spells.CurrentSpellsCollectionIndex;
                if (multilevel)
                    __instance.LevelUpController.UnselectSpell(spellbook, spellList, spellsCollectionIndex, spellLevel);
                else
                    __instance.LevelUpController.UnselectSpell(spellbook, spellList, spellsCollectionIndex, -1);
                if (!__instance.LevelUpController.SelectSpell(spellbook, spellList, spellLevel, spell, spellsCollectionIndex))
                    return true;
                // Begin Mod Lines
                BlueprintCharacterClass selectedClass = __instance.LevelUpController.State.SelectedClass;
                __instance.LevelUpController.State.SelectedClass = spellbook.CharacterClass;
                // End Mod Lines
                __instance.DefineAvailibleData();
                __instance.Spells.IsDirty = true;
                __instance.Total.IsDirty = true;
                __instance.Spells.SetSpellbooklevel(spellLevel);
                __instance.SetupUI();
                __instance.LevelUpController.State.SelectedClass = selectedClass; // Mod Line
                return true;
            }
        }

        // Fixed the UI for selecting new spells (to switch the spellbook correctly on clicking a slot)
        [HarmonyPatch(typeof(CharBPhaseSpells), nameof(CharBPhaseSpells.OnChangeCurrentCollection))]
        static class CharBPhaseSpells_OnChangeCurrentCollection_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(CharBPhaseSpells __instance) {
                if (!settings.toggleMulticlass) return;
                if (__instance.CurrentSpellSelectionData != null) {
                    __instance.SetSpellbook(__instance.CurrentSpellSelectionData.Spellbook.CharacterClass);
                }
            }
        }

        // Fixed the UI for selecting new spells (to refresh the spell list correctly on clicking a slot when the selections have the same spell list) - memorizer
        [HarmonyPatch(typeof(CharBFeatureSelector), nameof(CharBFeatureSelector.FillDataAllSpells))]
        [HarmonyPatch(new Type[] { typeof(SpellSelectionData), typeof(int) })]
        static class CharBFeatureSelector_FillDataAllSpells_Patch {
            public static bool Prefix(CharBFeatureSelector __instance, SpellSelectionData spellSelectionData, int maxLevel) {
                if (!settings.toggleMulticlass) return true;
                __instance.Init();
                CharBSelectorLayer selectorLayerBody = __instance.SelectorLayerBody;
                if (selectorLayerBody.CurrentSpellSelectionData != null
                    && spellSelectionData.SpellList == selectorLayerBody.CurrentSpellSelectionData.SpellList
                    && spellSelectionData.Spellbook == selectorLayerBody.CurrentSpellSelectionData.Spellbook // Mod Line
                    ) {
                    selectorLayerBody.FillSpellLightUpdate();
                }
                else {
                    __instance.SetFilter();
                    if (maxLevel >= 2) {
                        __instance.FilterState.ShowAllAvailible = true;
                        __instance.Filter.SetupButtonStates();
                    }
                    __instance.SetLabel((string)__instance.t.CharGen.Spells);
                    selectorLayerBody.FillSpellLevel(spellSelectionData, maxLevel, false, __instance);
                    __instance.SwitchToMultiSelector(false);
                }
                return true;
            }
        }

        // Fixed the UI for selecting new spells (to refresh the spell list correctly on clicking a slot when the selections have the same spell list) - spontaneous caster
        [HarmonyPatch(typeof(CharBFeatureSelector), nameof(CharBFeatureSelector.FillDataSpellLevel))]
        [HarmonyPatch(new Type[] { typeof(SpellSelectionData), typeof(int) })]
        static class CharBFeatureSelector_FillDataSpellLevel_Patch {
            public static bool Prefix(CharBFeatureSelector __instance,SpellSelectionData spellSelectionData, int spellLevel) {
                if (!settings.toggleMulticlass) return true;
                __instance.Init();
                CharBSelectorLayer selectorLayerBody = __instance.SelectorLayerBody;
                if (selectorLayerBody.CurrentSpellSelectionData != null
                    && spellSelectionData.SpellList == selectorLayerBody.CurrentSpellSelectionData.SpellList
                    && spellSelectionData.Spellbook == selectorLayerBody.CurrentSpellSelectionData.Spellbook // Mod Line
                    && spellLevel == selectorLayerBody.CurrentLevel) {
                    selectorLayerBody.FillSpellLightUpdate();
                }
                else {
                    __instance.SetFilter();
                    __instance.FilterState.ShowAll = false;
                    __instance.Filter.SetupButtonStates();
                    __instance.FilterState.ShowAllAvailible = false;
                    __instance.SetLabel((string)__instance.t.CharGen.Spells);
                    if (spellLevel < 0)
                        selectorLayerBody.SwitchOffElementsFromIndex(0);
                    else
                        selectorLayerBody.FillSpellLevel(spellSelectionData, spellLevel, true, __instance);
                    __instance.SwitchToMultiSelector(false);
                }
                return true;
            }
        }
#endif

        // Fixed a vanilla PFK bug that caused dragon bloodline to be displayed in Magus' feats tree
        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyProgressions")]
        static class ApplyClassMechanics_ApplyProgressions_Patch {
            public static bool Prefix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return true;
                BlueprintCharacterClass blueprintCharacterClass = state.NextClassLevel <= 1 ? state.SelectedClass : null;
                foreach (BlueprintProgression blueprintProgression in unit.Progression.Features.Enumerable.Select(f => f.Blueprint).OfType<BlueprintProgression>().ToList()) {
                    BlueprintProgression p = blueprintProgression;
                    if (blueprintCharacterClass != null
                        // && p.Classes.Contains<BlueprintCharacterClass>(blueprintCharacterClass))
                        && p.IsChildProgressionOf(unit, blueprintCharacterClass) // Mod Line replacing above
                        )
                        unit.Progression.Features.Enumerable.FirstItem(
                            f => f.Blueprint == p)?.SetSource((FeatureSource)blueprintCharacterClass, 1
                            );
                    LevelUpHelper.UpdateProgression(state, unit, p);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnitHelper))]
        [HarmonyPatch("CopyInternal")]
        static class UnitProgressionData_CopyFrom_Patch {
            static void Postfix(UnitEntityData unit, UnitEntityData __result) {
                if (!settings.toggleMulticlass) return;

                //升级时会用这个方法复制一个UnitEntityData，其中涉及到复制UnitProgressionData
                //默认状况下，复制的UnitProgressionData的CharacterLevel等于所有非神话职业等级之和
                //如果人物等级不等于这个默认值，会出问题（比如在低于默认值时，可能没到20级就升不了级了，因为非神话职业等级之和已经提前超过了20级）
                //修正这一点。

                var UnitProgressionData_CharacterLevel = AccessTools.Property(typeof(UnitProgressionData), nameof(UnitProgressionData.CharacterLevel));
                UnitProgressionData_CharacterLevel.SetValue(__result.Descriptor.Progression, unit.Descriptor.Progression.CharacterLevel);
            }
        }
    }
}
