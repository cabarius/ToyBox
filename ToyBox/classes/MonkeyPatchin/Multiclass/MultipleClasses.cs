// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;
using ModKit.Utility;
using ModKit;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Class;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kingmaker.UI;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Class;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.CharacterInfo.Sections.Progression.Main;
using Kingmaker.UI.MVVM._PCView.CharGen;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.Mythic;
using Kingmaker.UI.MVVM._VM.Party;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints;
using Kingmaker.DLC;
using Kingmaker.UI.MVVM._VM.Other.NestedSelectionGroup;

namespace ToyBox.Multiclass {
    public static partial class MultipleClasses {

        #region Class Level & Archetype
        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.UpdatePreview))]
        private static class LevelUpController_UpdatePreview_Patch {
            private static void Prefix(LevelUpController __instance) {
                if (IsAvailable()) {
                    // This is the critical place that gets called once before we go through all the level computations for setting up the level up screen
                    Mod.Debug("LevelUpController_UpdatePreview_Patch");
                    //Main.multiclassMod.AppliedMulticlassSet.Clear();
                    //Main.multiclassMod.UpdatedProgressions.Clear();
                }
            }
        }
        [HarmonyPatch(typeof(SelectClass), nameof(SelectClass.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        private static class SelectClass_Apply_Patch {
            [HarmonyPostfix]
            private static void Postfix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return;
                if (!unit.IsPartyOrPet()) return;
                //if (Mod.IsCharGen()) Main.Log($"stack: {System.Environment.StackTrace}");
                if (IsAvailable()) {
                    Main.multiclassMod.AppliedMulticlassSet.Clear();
                    Main.multiclassMod.UpdatedProgressions.Clear();
                    // Some companions have predefined levels so in some cases we get called iteratively for each level so we will make sure we only apply multiclass on the last level
                    if (unit.TryGetPartyMemberForLevelUpVersion(out var ch)
                        && ch.TryGetClass(state.SelectedClass, out var cl)
                        && unit != ch.Descriptor
                        && state.NextClassLevel <= cl.Level
                        ) {
                        Mod.Debug($"SelectClass_Apply_Patch, unit: {unit.CharacterName.orange()} isCH: {unit == ch.Descriptor}) - skip - lvl:{state.NextClassLevel} vs {cl.Level} ".green());
                        return;
                    }
                    // get multi-class setting
                    var useDefaultMulticlassOptions = state.IsCharGen();
                    var options = MulticlassOptions.Get(useDefaultMulticlassOptions ? null : unit);
                    Mod.Trace($"SelectClass_Apply_Patch, unit: {unit.CharacterName.orange()} useDefaultMulticlassOptions: {useDefaultMulticlassOptions} isCharGen: {state.IsCharGen()} is1stLvl: {state.IsFirstCharacterLevel} isPHChar: {unit.CharacterName == "Player Character"} level: {state.NextClassLevel.ToString().yellow()}".cyan().bold());

                    if (options == null || options.Count == 0)
                        return;
                    Mod.Trace($"    selected options: {options}".orange());
                    //selectedMulticlassSet.ForEach(cl => Main.Log($"    {cl}"));

                    // applying classes
                    var selectedClass = state.SelectedClass;
                    StateReplacer stateReplacer = new(state);
                    foreach (var characterClass in Main.multiclassMod.AllClasses) {
                        if (characterClass.IsMythic != selectedClass.IsMythic) continue;
                        if (Main.multiclassMod.AppliedMulticlassSet.Contains(characterClass)) {
                            Mod.Warn($"SelectClass_Apply_Patch - duplicate application of multiclass detected: {characterClass.name.yellow()}");
                            continue;
                        }
                        if (options.Contains(characterClass)) {
                            Mod.Trace($"   checking {characterClass.HashKey()} {characterClass.GetDisplayName()} ");
                        }
                        if (characterClass != stateReplacer.SelectedClass
                            && characterClass.IsMythic == state.IsMythicClassSelected
                            && options.Contains(characterClass)
                            ) {
                            stateReplacer.Replace(null, 0); // TODO - figure out and document what this is doing
                            Mod.Trace($"       {characterClass.Name} matches".cyan());
                            //stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));

                            if (new SelectClass(characterClass).Check(state, unit)) {
                                Mod.Trace($"         - {nameof(SelectClass)}.{nameof(SelectClass.Apply)}*({characterClass}, {unit})".cyan());

                                unit.Progression.AddClassLevel_NotCharacterLevel(characterClass);
                                //state.NextClassLevel = unit.Progression.GetClassLevel(characterClass);
                                //state.SelectedClass = characterClass;
                                characterClass.RestrictPrerequisites(unit, state);
                                //EventBus.RaiseEvent<ILevelUpSelectClassHandler>(h => h.HandleSelectClass(unit, state));

                                Main.multiclassMod.AppliedMulticlassSet.Add(characterClass);
                            }
                        }
                    }
                    stateReplacer.Restore();

                    Mod.Trace($"    checking archetypes for {unit.CharacterName}".cyan());
                    // applying archetypes
                    ForEachAppliedMulticlass(state, unit, () => {
                        Mod.Trace($"    {state.SelectedClass.HashKey()} SelectClass-ForEachApplied".cyan().bold());
                        var selectedClass = state.SelectedClass;
                        var archetypeOptions = options.ArchetypeOptions(selectedClass);
                        foreach (var archetype in state.SelectedClass.Archetypes) {
                            // here is where we need to start supporting multiple archetypes of the same class
                            if (archetypeOptions.Contains(archetype)) {
                                Mod.Trace($"    adding archetype: ${archetype.Name}".cyan().bold());
                                AddArchetype addArchetype = new(state.SelectedClass, archetype);
                                unit.SetClassIsGestalt(addArchetype.CharacterClass, true);
                                if (addArchetype.Check(state, unit)) {
                                    addArchetype.Apply(state, unit);
                                }
                            }
                        }
                    });
                }
            }
        }

        #endregion

        #region Skills & Features

        [HarmonyPatch(typeof(LevelUpController))]
        [HarmonyPatch("ApplyLevelup")]
        private static class LevelUpController_ApplyLevelup_Patch {
            private static void Prefix(LevelUpController __instance, UnitEntityData unit) {
                if (!settings.toggleMulticlass) return;

                if (unit == __instance.Preview) {
                    Mod.Trace($"Unit Preview = {unit.CharacterName}");
                    Mod.Trace("levelup action：");
                    foreach (var action in __instance.LevelUpActions) {
                        Mod.Trace($"{action.GetType()}");
                    }
                }
            }
        }
        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        private static class ApplyClassMechanics_Apply_Patch {
            [HarmonyPostfix]
            private static void Postfix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return;
                if (IsAvailable()) {
                    Mod.Debug($"ApplyClassMechanics_Apply_Patch - unit: {unit} {unit.CharacterName} class:{state.SelectedClass}");
                    if (state.SelectedClass != null) {
                        ForEachAppliedMulticlass(state, unit, () => {
                            unit.SetClassIsGestalt(state.SelectedClass, true);
                            //Main.Log($" - {nameof(ApplyClassMechanics)}.{nameof(ApplyClassMechanics.Apply)}*({state.SelectedClass}{state.SelectedClass.Archetypes}[{state.NextClassLevel}], {unit}) mythic: {state.IsMythicClassSelected} vs {state.SelectedClass.IsMythic}");

                            __instance.Apply_NoStatsAndHitPoints(state, unit);
                        });
                    }
                    var allAppliedClasses = Main.multiclassMod.AppliedMulticlassSet.ToList();
                    Mod.Debug($"ApplyClassMechanics_Apply_Patch - {String.Join(" ", allAppliedClasses.Select(cl => cl.Name))}".orange());
                    allAppliedClasses.Add(state.SelectedClass);
                    SavesBAB.ApplySaveBAB(unit, state, allAppliedClasses.ToArray());
                    HPDice.ApplyHPDice(unit, state, allAppliedClasses.ToArray());
                }
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        private static class SelectFeature_Apply_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (IsAvailable()) {
                    if (__instance.Item != null) {
                        var selectionState =
                            ReflectionCache.GetMethod<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>>
                            ("GetSelectionState")(__instance, state);
                        if (selectionState != null) {
                            var sourceClass = selectionState.SourceFeature?.GetSourceClass(unit);
                            if (sourceClass != null) {
                                __state = new StateReplacer(state);
                                __state.Replace(sourceClass, unit.Progression.GetClassLevel(sourceClass));
                            }
                        }
                    }
                }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(SelectFeature __instance, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (__state != null) {
                    __state.Restore();
                }
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Check), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        private static class SelectFeature_Check_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (IsAvailable()) {
                    if (__instance.Item != null) {
                        var selectionState =
                            ReflectionCache.GetMethod<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>>
                            ("GetSelectionState")(__instance, state);
                        if (selectionState != null) {
                            var sourceClass = selectionState.SourceFeature?.GetSourceClass(unit);
                            if (sourceClass != null) {
                                __state = new StateReplacer(state);
                                __state.Replace(sourceClass, unit.Progression.GetClassLevel(sourceClass));
                            }
                        }
                    }
                }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            private static void Postfix(SelectFeature __instance, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (__state != null) {
                    __state.Restore();
                }
            }
        }

        #endregion

        #region Spellbook

        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        private static class ApplySpellbook_Apply_Patch {
            [HarmonyPostfix]
            private static void Postfix(MethodBase __originalMethod, ApplySpellbook __instance, LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return;

                if (IsAvailable() && !Main.multiclassMod.LockedPatchedMethods.Contains(__originalMethod)) {
                    Main.multiclassMod.LockedPatchedMethods.Add(__originalMethod);
                    ForEachAppliedMulticlass(state, unit, () => {
                        __instance.Apply(state, unit);
                    });
                    Main.multiclassMod.LockedPatchedMethods.Remove(__originalMethod);
                }
            }
        }

        #endregion

        #region Commit

        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Commit))]
        private static class LevelUpController_Commit_Patch {
            [HarmonyPostfix]
            private static void Postfix(LevelUpController __instance) {
                if (!settings.toggleMulticlass) return;

                if (IsAvailable()) {
                    var isCharGen = __instance.State.IsCharGen();
                    var ch = __instance.Unit;
                    var options = MulticlassOptions.Get(isCharGen ? null : ch);
                    if (isCharGen
                            && __instance.Unit.IsCustomCompanion()
                            && options.Count > 0) {
                        Mod.Trace($"LevelUpController_Commit_Patch - {ch} - {options}");
                        MulticlassOptions.Set(ch, options);
                    }
                }
            }
        }

        #endregion

        [HarmonyPatch(typeof(UnitProgressionData), nameof(UnitProgressionData.SetupLevelsIfNecessary))]
        private static class UnitProgressionData_SetupLevelsIfNecessary_Patch {
            private static bool Prefix(UnitProgressionData __instance) {
                if (__instance.m_CharacterLevel.HasValue && __instance.m_MythicLevel.HasValue)
                    return false;
                __instance.UpdateLevelsForGestalt();
                return false;
            }
        }
        [HarmonyPatch(typeof(CharGenClassPhaseVM), nameof(CharGenClassPhaseVM.CreateClassListSelector))]
        private static class CharGenClassPhaseVM_CreateClassListSelector_Patch {
            private static bool Prefix(CharGenClassPhaseVM __instance) {
                if (settings.toggleMulticlass || settings.toggleIgnoreClassRestrictions) {
                    var progression = Game.Instance.BlueprintRoot.Progression;
                    __instance.m_ClassesVMs = (__instance.LevelUpController.State.Mode == LevelUpState.CharBuildMode.Mythic
                        ? __instance.GetMythicClasses()
                        : (__instance.LevelUpController.Unit.IsPet
                            ? progression.PetClasses.Concat(progression.CharacterClasses.OrderBy(cl => cl.Name))
                            : progression.CharacterClasses)
                        )
                        .Where(cls => {
                            if (cls.IsDlcRestricted())
                                return false;
                            return CharGenClassPhaseVM.MeetsPrerequisites(__instance.LevelUpController, cls) || !cls.HideIfRestricted;
                        })
                        .Select(cls => new CharGenClassSelectorItemVM(
                                                cls,
                                                null,
                                                __instance.LevelUpController,
                                                __instance,
                                                __instance.SelectedArchetypeVM,
                                                __instance.ReactiveTooltipTemplate,
                                                CharGenClassPhaseVM.IsClassAvailable(__instance.LevelUpController, cls),
                                                true, false))
                        .ToList<CharGenClassSelectorItemVM>();
                    __instance.ClassSelector = new NestedSelectionGroupRadioVM<CharGenClassSelectorItemVM>((INestedListSource)__instance);
                    return false;
                }
                return true;
            }
        }
        public static class MulticlassCheckBoxHelper {
            public static void UpdateCheckbox(CharGenClassSelectorItemPCView instance) {
                if (instance == null) return;
                var multicheckbox = instance.transform?.Find("MulticlassCheckbox-ToyBox");
                if (multicheckbox == null) return;
                var toggle = multicheckbox.GetComponent<ToggleWorkaround>();
                if (toggle == null) return;
                var viewModel = instance.ViewModel;
                if (viewModel == null) return;
                var ch = Main.IsInGame ? viewModel.LevelUpController.Unit : null;
                var cl = viewModel.Class;
                var image = multicheckbox.Find("Background").GetComponent<Image>();
                var canSelect = MulticlassOptions.CanSelectClassAsMulticlass(ch, cl);
                image.CrossFadeAlpha(canSelect ? 1.0f : 0f, 0, true);
                var options = MulticlassOptions.Get(ch);
                var shouldSelect = options.Contains(cl) && (!viewModel.IsArchetype || options.ArchetypeOptions(cl).Contains(viewModel.Archetype));

                toggle.SetIsOnWithoutNotify(shouldSelect);
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(v => MulticlassCheckBoxChanged(v, instance));
            }
            public static void MulticlassCheckBoxChanged(bool value, CharGenClassSelectorItemPCView instance) {
                if (instance == null) return;
                var viewModel = instance.ViewModel;
                if (viewModel == null) return;
                var ch = Main.IsInGame ? viewModel.LevelUpController.Unit : null;
                var cl = viewModel.Class;
                if (!MulticlassOptions.CanSelectClassAsMulticlass(ch, cl)) return;
                var options = MulticlassOptions.Get(ch);
                var cd = ch?.Progression.GetClassData(cl);
                var chArchetype = cd?.Archetypes.FirstOrDefault<BlueprintArchetype>();
                var archetypeOptions = options.ArchetypeOptions(cl);
                if (value)
                    archetypeOptions = options.Add(cl);
                else
                    options.Remove(cl);
                if (options.Contains(cl)) {
                    if (viewModel.IsArchetype) {
                        if (value)
                            archetypeOptions.AddExclusive(viewModel.Archetype);
                        else
                            archetypeOptions.Remove(viewModel.Archetype);
                    }
                    else if (chArchetype != null) {
                        // this is the case where the user clicks on the class and the character already has an archetype in this class
                        if (value)
                            archetypeOptions.AddExclusive(chArchetype);
                        else
                            archetypeOptions.Remove(chArchetype);
                    }
                    options.SetArchetypeOptions(cl, archetypeOptions);
                }
                MulticlassOptions.Set(ch, options);
                Mod.Debug($"ch: {ch?.CharacterName.ToString().orange() ?? "null"} class: {cl?.name ?? "null"} isArch: {viewModel.IsArchetype} arch: {viewModel.Archetype} chArchetype:{chArchetype} - options: {options}");
                var canvas = Game.Instance.UI.Canvas;
                var transform = canvas != null ? canvas.transform : Game.Instance.UI.MainMenu.transform;
                var charGemClassPhaseDetailedView = transform.Find("ChargenPCView/ContentWrapper/DetailedViewZone/PhaseClassDetaildPCView");
                var phaseClassDetailView = charGemClassPhaseDetailedView.GetComponent<Kingmaker.UI.MVVM._PCView.CharGen.Phases.Class.CharGenClassPhaseDetailedPCView>();
                var charGenClassPhaseVM = phaseClassDetailView.ViewModel;
                var selectedClassVM = charGenClassPhaseVM.SelectedClassVM.Value;
                charGenClassPhaseVM.OnSelectorClassChanged(viewModel);
                if (viewModel.IsArchetype) {
                    charGenClassPhaseVM.OnSelectorArchetypeChanged(viewModel.Archetype);
                }
                else {
                    charGenClassPhaseVM.LevelUpController.RemoveArchetype(viewModel.Archetype);
                    charGenClassPhaseVM.UpdateClassInformation();
                }
                charGenClassPhaseVM.OnSelectorClassChanged(selectedClassVM);

            }
        }
        [HarmonyPatch(typeof(CharGenClassSelectorItemPCView), nameof(CharGenClassSelectorItemPCView.BindViewImplementation))]
        private static class CharGenClassSelectorItemPCView_BindViewImplementation_Patch {
            private static void Postfix(CharGenClassSelectorItemPCView __instance) {
                //Mod.Warn("CharGenClassSelectorItemPCView_CharGenClassSelectorItemPCView_Patch");
                var multicheckbox = __instance.transform.Find("MulticlassCheckbox-ToyBox");
                if (multicheckbox != null) {
                    if (!settings.toggleMulticlass) {
                        UnityEngine.Object.Destroy(multicheckbox.gameObject);
                    }
                }
                if (!settings.toggleMulticlass) return;
                if (multicheckbox == null) {
                    var checkbox = Game.Instance.UI.FadeCanvas.transform.Find("TutorialView/BigWindow/Window/Content/Footer/Toggle");
                    //var checkbox = Game.Instance.UI.Canvas.transform.Find("ServiceWindowsPCView/SpellbookView/SpellbookScreen/MainContainer/KnownSpells/Toggle");
                    var sibling = __instance.transform.Find("CollapseButton");
                    var textContainer = __instance.transform.Find("TextContainer");
                    var textLayout = textContainer.GetComponent<LayoutElement>();
                    textLayout.preferredWidth = 1;
                    var siblingIndex = sibling.transform.GetSiblingIndex();
                    multicheckbox = UnityEngine.Object.Instantiate(checkbox, __instance.transform);
                    multicheckbox.transform.SetSiblingIndex(1);
                    multicheckbox.name = "MulticlassCheckbox-ToyBox";
                    multicheckbox.GetComponentInChildren<TextMeshProUGUI>().text = "";
                    multicheckbox.gameObject.SetActive(true);
                    MulticlassCheckBoxHelper.UpdateCheckbox(__instance);
                    PerSaveSettings.observers += (perSave) => MulticlassCheckBoxHelper.UpdateCheckbox(__instance);
                }
                else {
                    multicheckbox.gameObject.SetActive(true);
                    MulticlassCheckBoxHelper.UpdateCheckbox(__instance);
                }
            }
        }
        //[HarmonyPatch(typeof(CharGenClassSelectorItemPCView), nameof(CharGenClassSelectorItemPCView.RefreshView))]
        //private static class CharGenClassSelectorItemPCView_RefreshView_Patch {
        //    private static void Postfix(CharGenClassSelectorItemPCView __instance) {
        //        if (!settings.toggleMulticlass) return;
        //        MulticlassCheckBoxHelper.UpdateCheckbox(__instance);
        //    }
        //}
        private static String class_selection_text_initial = null;
        [HarmonyPatch(typeof(CharGenClassPhaseDetailedPCView), nameof(CharGenClassPhaseDetailedPCView.BindViewImplementation))]
        private static class CharGenClassPhaseDetailedPCView_BindViewImplementation_Patch {
            private static void Postfix(CharGenClassPhaseDetailedPCView __instance) {
                var chooseClass = __instance.transform.Find("ClassSelecotrPlace/Selector/HeaderH2/Label");
                if (class_selection_text_initial == null) {
                    class_selection_text_initial = chooseClass.GetComponentInChildren<TextMeshProUGUI>().text;
                }
                chooseClass.GetComponentInChildren<TextMeshProUGUI>().text = settings.toggleMulticlass ? "Choose Class <size=67%>(Checkbox for multiclass)</size>" : class_selection_text_initial;
            }
        }
    }
}

