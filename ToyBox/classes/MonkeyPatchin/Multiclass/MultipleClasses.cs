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

namespace ToyBox.Multiclass {
    public static partial class MultipleClasses {

        #region Class Level & Archetype

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
                    var companionNames = Game.Instance?.Player?.AllCharacters.Where(c => !c.IsMainCharacter).Select(c => c.CharacterName).ToList();
                    Mod.Debug($"companions: {string.Join(", ", companionNames)}");
                    // get multi-class setting
                    bool isCompanion = companionNames?.Contains(unit.Unit.CharacterName) ?? false;
                    bool useDefaultMulticlassOptions = state.IsCharGen() && !isCompanion;
                    var options = MulticlassOptions.Get(useDefaultMulticlassOptions ? null : unit);
                    Mod.Trace($"SelectClass.Apply.Postfix, unit: {unit.CharacterName} useDefaultMulticlassOptions: {useDefaultMulticlassOptions} isCharGen: {state.IsCharGen()} is1stLvl: {state.IsFirstCharacterLevel} isCompan: {isCompanion} isPHChar: {unit.CharacterName == "Player Character"}".cyan().bold());

                    if (options == null || options.Count == 0)
                        return;

                    Mod.Trace($"selected options: {options}".orange());
                    //                    selectedMulticlassSet.ForEach(cl => Main.Log($"    {cl}"));

                    // applying classes
                    StateReplacer stateReplacer = new(state);
                    foreach (var characterClass in Main.multiclassMod.AllClasses) {
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
                    //Main.Log($"ApplyClassMechanics.Apply.Postfix - unit: {unit} {unit.CharacterName}");
                    if (state.SelectedClass != null) {
                        ForEachAppliedMulticlass(state, unit, () => {
                            unit.SetClassIsGestalt(state.SelectedClass, true);
                            //Main.Log($" - {nameof(ApplyClassMechanics)}.{nameof(ApplyClassMechanics.Apply)}*({state.SelectedClass}{state.SelectedClass.Archetypes}[{state.NextClassLevel}], {unit}) mythic: {state.IsMythicClassSelected} vs {state.SelectedClass.IsMythic}");

                            __instance.Apply_NoStatsAndHitPoints(state, unit);
                        });
                    }
                    var allAppliedClasses = Main.multiclassMod.AppliedMulticlassSet.ToList();
                    //Main.Log($"ApplyClassMechanics.Apply.Postfix - {String.Join(" ", allAppliedClasses.Select(cl => cl.Name))}".orange());
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
    }
}
