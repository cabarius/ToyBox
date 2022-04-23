// Based on MultipleArchetypes with kind permission by Vek17 (https://github.com/Vek17/WrathMods-MultipleArchetypes/)
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class.Mechanic;
using Kingmaker.UI.MVVM._VM.Other.NestedSelectionGroup;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.LevelClassScores.Classes;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.ChupaChupses;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.Main;
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Progression.Spellbook;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using ModKit;
using Owlcat.Runtime.UI.Tooltips;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace ToyBox.Multiclass {
    public static class Archetypes {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        public static string ArchetypesName(this ClassData cd) => string.Join("/", cd.Archetypes.Select(a => a.Name));

        [HarmonyPatch(typeof(ClassData), nameof(ClassData.CalcSkillPoints))]
        private static class ClassData_CalcSkillPoints_Patch {
            static bool Prefix(ClassData __instance, ref int __result) {
                if (!settings.toggleMultiArchetype) return true;
                if (!__instance.Archetypes.Any()) { return true; }
                __result = __instance.CharacterClass.SkillPoints + __instance.Archetypes.Select((BlueprintArchetype a) => a.AddSkillPoints).Max();
                return false;
            }
        }
        [HarmonyPatch(typeof(TooltipTemplateClass), MethodType.Constructor, new Type[] { typeof(ClassData) })]
        private static class TooltipTemplateClass_Constructor_Patch {
            static void Postfix(ref TooltipTemplateClass __instance, ClassData classData) {
                var name = classData.ArchetypesName();
                var description = string.Join("\n\n", classData.Archetypes.Select(a => a.Description));
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(name)) {
                    var NameSetter = AccessTools.Field(typeof(TooltipTemplateClass), "m_Name");
                    var DescSetter = AccessTools.Field(typeof(TooltipTemplateClass), "m_Desc");
                    NameSetter.SetValue(__instance, name);
                    DescSetter.SetValue(__instance, description);
                }
            }
        }
        [HarmonyPatch(typeof(CharInfoClassEntryVM), MethodType.Constructor, new Type[] { typeof(ClassData) })]
        private static class CharInfoClassEntryVM_Constructor_Patch {
            private static void Postfix(CharInfoClassEntryVM __instance, ClassData classData) {
                var Name = classData.ArchetypesName();
                if (!string.IsNullOrEmpty(Name)) {
                    var ClassName = AccessTools.Field(typeof(CharInfoClassEntryVM), "<ClassName>k__BackingField");
                    ClassName.SetValue(__instance, Name);
                }
            }
        }
        [HarmonyPatch(typeof(ClassProgressionVM), MethodType.Constructor, new Type[] { typeof(UnitDescriptor), typeof(ClassData) })]
        private static class ClassProgressionVM_Constructor_Patch {
            private static void Postfix(ClassProgressionVM __instance, UnitDescriptor unit, ClassData unitClass) {
                if (!settings.toggleMultiArchetype) return;
                var Name = string.Join("/", unitClass.Archetypes.Select(a => a.Name));
                if (!string.IsNullOrEmpty(Name)) {
                    __instance.Name = string.Join(" ", unitClass.CharacterClass.Name, $"({Name})");
                }
                var castingArchetype = unitClass.Archetypes.Where(a => a.ReplaceSpellbook != null).FirstOrDefault();
                if (castingArchetype != null) {
                    __instance.AddDisposable(__instance.SpellbookProgressionVM = new SpellbookProgressionVM(
                        __instance.m_UnitClass,
                        castingArchetype,
                        __instance.m_Unit,
                        __instance.m_LevelProgressionVM));
                }
            }
        }
        [HarmonyPatch(typeof(CharGenClassSelectorItemVM), MethodType.Constructor, new Type[] {
            typeof(BlueprintCharacterClass),
            typeof(BlueprintArchetype),
            typeof(LevelUpController),
            typeof(INestedListSource),
            typeof(ReactiveProperty<CharGenClassSelectorItemVM>),
            typeof(ReactiveProperty<TooltipBaseTemplate>),
            typeof(bool),
            typeof(bool),
            typeof(bool),
        })]
        private static class CharGenClassSelectorItemVM_Constructor_Patch {
            private static void Postfix(CharGenClassSelectorItemVM __instance,
                BlueprintCharacterClass cls,
                BlueprintArchetype archetype,
                LevelUpController levelUpController,
                INestedListSource source,
                ReactiveProperty<CharGenClassSelectorItemVM> selectedArchetype,
                ReactiveProperty<TooltipBaseTemplate> tooltipTemplate,
                bool prerequisitesDone,
                bool canSelect,
                bool allowSwitchOff) {
                if (__instance.HasClassLevel) {
                    var classData = levelUpController.Unit.Progression.GetClassData(cls);
                    if (!classData.Archetypes.Any()) return;
                    var name = classData.ArchetypesName();
                    var DisplayName = AccessTools.Field(typeof(CharGenClassSelectorItemVM), "DisplayName");
                    DisplayName.SetValue(__instance, $"{cls.Name} — {name}");
                }
            }
        }
        [HarmonyPatch(typeof(CharGenClassSelectorItemVM), nameof(CharGenClassSelectorItemVM.GetArchetypesList), new Type[] { typeof(BlueprintCharacterClass) })]
        private static class CharGenClassSelectorItemVM_GetArchetypesList_Patch {
            public static List<NestedSelectionGroupEntityVM> archetypes;
            private static void Postfix(CharGenClassSelectorItemVM __instance, List<NestedSelectionGroupEntityVM> __result) {
                archetypes = __result;
            }
        }
        [HarmonyPatch(typeof(NestedSelectionGroupEntityVM), nameof(NestedSelectionGroupEntityVM.SetSelected), new Type[] { typeof(bool) })]
        private static class NestedSelectionGroupEntityVM_SetSelected_Patch {
            private static bool Prefix(NestedSelectionGroupEntityVM __instance, ref bool state) {
                if (!settings.toggleMultiArchetype) return true;
                var VM = __instance as CharGenClassSelectorItemVM;
                var controller = Game.Instance?.LevelUpController;
                if (VM == null || controller == null) return true;
                var progression = controller.Preview?.Progression;
                var classData = controller?.Preview?.Progression?.GetClassData(controller.State.SelectedClass);
                if (classData == null) { return true; }
                if (controller.Unit.Progression.GetClassLevel(VM.Class) >= 1) { return true; }
                var hasArchetype = classData.Archetypes.HasItem(VM.Archetype);
                state |= hasArchetype;
                if (!state) {
                    if (progression != null && VM.Archetype != null) {
                        VM.SetAvailableState(progression.CanAddArchetype(classData.CharacterClass, VM.Archetype) && VM.PrerequisitesDone);
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(NestedSelectionGroupEntityVM), nameof(NestedSelectionGroupEntityVM.SetSelectedFromView), new Type[] { typeof(bool) })]
        private static class NestedSelectionGroupEntityVM_SetSelectedFromView_Patch {
            private static bool Prefix(NestedSelectionGroupEntityVM __instance, bool state) {
                if (!settings.toggleMultiArchetype) return true;
                if (!state && !__instance.AllowSwitchOff) {
                    return false;
                }
                __instance.IsSelected.Value = state;
                __instance.RefreshView.Execute();
                if (state) {
                    __instance.DoSelectMe();
                }
                //__instance.SetSelected(state);
                return false;
            }
        }
        [HarmonyPatch(typeof(CharGenClassPhaseVM), nameof(CharGenClassPhaseVM.OnSelectorArchetypeChanged), new Type[] { typeof(BlueprintArchetype) })]
        private static class CharGenClassPhaseVM_OnSelectorArchetypeChanged_Patch {
            private static bool Prefix(CharGenClassPhaseVM __instance, BlueprintArchetype archetype) {
                if (!settings.toggleMultiArchetype) return true;
                __instance.UpdateTooltipTemplate(false);
                if (__instance.LevelUpController.State.SelectedClass == null) {
                    return false;
                }
                var Progression = __instance.LevelUpController.Preview.Progression;
                var classData = __instance.LevelUpController.Preview
                    .Progression.GetClassData(__instance.LevelUpController.State.SelectedClass);

                if (classData != null && (archetype == null || !Progression.CanAddArchetype(classData.CharacterClass, archetype))) {
                    classData.Archetypes.ForEach(delegate (BlueprintArchetype a) {
                        __instance.LevelUpController.RemoveArchetype(a);
                    });
                }
                if (archetype != null) {
                    __instance.LevelUpController.RemoveArchetype(archetype);
                    if (!__instance.LevelUpController.AddArchetype(archetype)) {
                        MainThreadDispatcher.Post(delegate(object _) {
                            __instance.SelectedArchetypeVM.Value = null;
                        }, null);
                    }
                }
                __instance.UpdateClassInformation();
                return false;
            }
        }
        [HarmonyPatch(typeof(ClassProgressionVM), MethodType.Constructor, new Type[] {
            typeof(UnitDescriptor),
            typeof(BlueprintCharacterClass),
            typeof(BlueprintArchetype),
            typeof(bool),
            typeof(int),
        })]
        private static class ClassProgressionVM2_Constructor_Patch {
            private static void Postfix(ClassProgressionVM __instance, BlueprintCharacterClass classBlueprint, int level, bool buildDifference) {
                if (!settings.toggleMultiArchetype) return;
                var data = __instance.ProgressionVms.Select(vm => vm.ProgressionData).OfType<AdvancedProgressionData>().First();
                __instance.ProgressionVms.Clear();
                var addArchetypes = Game.Instance.LevelUpController.LevelUpActions.OfType<AddArchetype>();
                foreach (var add in addArchetypes) {
                    data.AddArchetype(add.Archetype);
                }
                var newVM = new ProgressionVM(data, __instance.m_Unit, new int?(level), buildDifference);
                __instance.ProgressionVms.Add(newVM);
                __instance.AddProgressions(__instance.m_Unit.Progression.GetClassProgressions(__instance.m_UnitClass).EmptyIfNull<ProgressionData>());
                __instance.AddProgressionSources(newVM.ProgressionSourceFeatures);
                var archetypeString = string.Join("/", addArchetypes.Select(a => a.Archetype.Name));
                if (!string.IsNullOrEmpty(archetypeString)) {
                    __instance.Name = string.Join(" ", classBlueprint.Name, $"({archetypeString})");
                }
                var castingArchetype = addArchetypes.Select(a => a.Archetype).Where(a => a.ReplaceSpellbook != null).FirstOrDefault();
                if (castingArchetype != null) {
                    __instance.AddDisposable(__instance.SpellbookProgressionVM = new SpellbookProgressionVM(
                        __instance.m_UnitClass,
                        castingArchetype,
                        __instance.m_Unit,
                        __instance.m_LevelProgressionVM));
                }
            }
        }
        [HarmonyPatch(typeof(ProgressionVM), nameof(ProgressionVM.SetClassArchetypeDifType), new Type[] { typeof(ProgressionVM.FeatureEntry) })]
        private static class ProgressionVM_SetClassArchetypeDifType_Patch {
            private static void Postfix(ProgressionVM __instance, ref ProgressionVM.FeatureEntry featureEntry) {
                if (!settings.toggleMultiArchetype) return;
                var featureEntry2 = featureEntry;
                foreach (var archetype in __instance.ProgressionData.Archetypes) {
                    foreach (var removeFeature in archetype.RemoveFeatures.Where(entry => entry.Level == featureEntry2.Level)) {
                        if (removeFeature.Features.Any(f => f == featureEntry2.Feature)) {
                            featureEntry.DifType = ClassArchetypeDifType.Removed;
                        };
                    }
                    foreach (var addFeature in archetype.AddFeatures.Where(entry => entry.Level == featureEntry2.Level)) {
                        if (addFeature.Features.Any(f => f == featureEntry2.Feature)) {
                            featureEntry.DifType = ClassArchetypeDifType.Added;
                        };
                    }
                }
            }
        }
        //Details Tab in CharGen
        [HarmonyPatch(typeof(CharGenClassCasterStatsVM), MethodType.Constructor, new Type[] { typeof(BlueprintCharacterClass), typeof(BlueprintArchetype) })]
        private static class CharGenClassCasterStatsVM_MultiArchetype_Patch {
            private static void Postfix(CharGenClassCasterStatsVM __instance, BlueprintCharacterClass valueClass, BlueprintArchetype valueArchetype) {
                if (!settings.toggleMultiArchetype) return;
                var controller = Game.Instance?.LevelUpController;
                if (controller == null) return;
                var classData = controller.Preview?.Progression?.GetClassData(valueClass);
                if (classData == null) return;
                __instance.CanCast.Value = classData.Spellbook != null;
                if (classData.Spellbook == null) return;
                var changeTypeArchetype = classData.Archetypes?.Where(a => a.ChangeCasterType).FirstOrDefault();
                __instance.MaxSpellsLevel.Value = classData.Spellbook.MaxSpellLevel.ToString();
                __instance.CasterAbilityScore.Value = LocalizedTexts.Instance.Stats.GetText(classData.Spellbook.CastingAttribute);
                __instance.CasterMindType.Value = ((changeTypeArchetype == null) ?
                    (UIUtilityUnit.GetCasterMindType(valueClass) ?? "—") : (UIUtilityUnit.GetCasterMindType(changeTypeArchetype) ?? "—"));
                __instance.SpellbookUseType.Value = UIUtilityUnit.GetCasterSpellbookUseType(classData.Spellbook);
            }
        }
        //Details Tab in CharGen
        [HarmonyPatch(typeof(CharGenClassMartialStatsVM), MethodType.Constructor, new Type[] { typeof(BlueprintCharacterClass), typeof(BlueprintArchetype), typeof(UnitDescriptor) })]
        private static class CharGenClassMartialStatsVM_MultiArchetype_Patch {
            private static void Postfix(CharGenClassMartialStatsVM __instance, BlueprintCharacterClass valueClass, BlueprintArchetype valueArchetype, UnitDescriptor unit) {
                if (!settings.toggleMultiArchetype) return;
                Mod.Debug("CharGenClassMartialStatsVM::Triggered");
                var controller = Game.Instance?.LevelUpController;
                if (controller == null) return;
                var classData = controller.Preview?.Progression?.GetClassData(valueClass);
                if (classData == null) return;
                Mod.Debug("Made it to override");
                __instance.Fortitude.Value = UIUtilityUnit.GetStatProgressionGrade(classData.FortitudeSave);
                __instance.Will.Value = UIUtilityUnit.GetStatProgressionGrade(classData.WillSave);
                __instance.Reflex.Value = UIUtilityUnit.GetStatProgressionGrade(classData.ReflexSave);
                __instance.BAB.Value = UIUtilityUnit.GetStatProgressionGrade(classData.BaseAttackBonus);
            }
        }
        //Details Tab in CharGen
        [HarmonyPatch(typeof(CharGenClassSkillsVM), MethodType.Constructor, new Type[] { typeof(BlueprintCharacterClass), typeof(BlueprintArchetype) })]
        private static class CharGenClassSkillsVM_MultiArchetype_Patch {
            private static void Postfix(CharGenClassSkillsVM __instance, BlueprintCharacterClass valueClass, BlueprintArchetype valueArchetype) {
                if (!settings.toggleMultiArchetype) return;
                Mod.Debug("CharGenClassSkillsVM::Triggered");
                var controller = Game.Instance?.LevelUpController;
                if (controller == null) return;
                var classData = controller.Preview?.Progression?.GetClassData(valueClass);
                if (classData == null) return;
                Mod.Debug("Made it to override");
                var classSkills = classData.Archetypes.SelectMany(a => a.ClassSkills)
                    .Concat(classData.CharacterClass.ClassSkills).Distinct().ToArray();
                __instance.ClassSkills.Clear();
                foreach (var skill in classSkills) {
                    var charGenClassStatEntryVM = new CharGenClassStatEntryVM(skill);
                    __instance.AddDisposable(charGenClassStatEntryVM);
                    __instance.ClassSkills.Add(charGenClassStatEntryVM);
                }
                return;
            }
        }
        //Details Tab in CharGen
        [HarmonyPatch(typeof(CharGenClassPhaseVM), nameof(CharGenClassPhaseVM.UpdateClassInformation))]
        private static class CharGenClassPhaseVM_UpdateClassInformation_MultiArchetype_Patch {
            private static void Postfix(CharGenClassPhaseVM __instance) {
                if (!settings.toggleMultiArchetype) return;
                Mod.Debug("CharGenClassPhaseVM::UpdateClassInformation");
                var controller = Game.Instance?.LevelUpController;
                if (controller == null) return;
                var classData = controller.Preview?.Progression?.GetClassData(__instance.SelectedClassVM.Value?.Class);
                if (classData == null) return;
                Mod.Debug("Made it to override");
                var classSkills = classData.Archetypes.SelectMany(a => a.ClassSkills)
                    .Concat(classData.CharacterClass.ClassSkills).Distinct().ToArray();
                //this.SelectedClassVM.Value.Class.Name + " — " + this.SelectedArchetypeVM.Value.Archetype.Name;
                var archetypeName = classData.ArchetypesName(); 
                if (!string.IsNullOrEmpty(archetypeName)) {
                    __instance.ClassDisplayName.Value = string.Join(" ", classData.CharacterClass.Name, $"({archetypeName})");
                }
                var archetypeDrescription = string.Join("\n\n", classData.Archetypes.Select(a => a.Description));
                if (!string.IsNullOrEmpty(archetypeName)) {
                    __instance.ClassDescription.Value = archetypeDrescription;
                }
                return;
            }
        }
    }
}