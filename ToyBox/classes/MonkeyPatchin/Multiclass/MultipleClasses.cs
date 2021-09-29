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
        static class SelectClass_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return;
                if (!unit.IsPartyOrPet()) return;
                //if (Mod.IsCharGen()) Main.Log($"stack: {System.Environment.StackTrace}");
                if (IsAvailable()) {
                    Main.multiclassMod.AppliedMulticlassSet.Clear();
                    Main.multiclassMod.UpdatedProgressions.Clear();

                    // get multi-class setting
                    var options = MulticlassOptions.Get(state.IsCharGen() ? null : unit);
                    modLogger.Log($"SelectClass.Apply.Postfix, unit: {unit.CharacterName} isCharGen: {state.IsCharGen()} isPHChar: {unit.CharacterName == "Player Character"}".cyan().bold());

                    if (options == null || options.Count == 0)
                        return;

                    modLogger.Log($"selected options: {options}".orange());
                    //                    selectedMulticlassSet.ForEach(cl => modLogger.Log($"    {cl}"));

                    // applying classes
                    StateReplacer stateReplacer = new StateReplacer(state);
                    foreach (BlueprintCharacterClass characterClass in Main.multiclassMod.AllClasses) {
                        if (options.Contains(characterClass)) {
                            modLogger.Log($"   checking {characterClass.HashKey()} {characterClass.GetDisplayName()} ");
                        }
                        if (characterClass != stateReplacer.SelectedClass
                            && characterClass.IsMythic == state.IsMythicClassSelected
                            && options.Contains(characterClass)
                            ) {
                            stateReplacer.Replace(null, 0); // TODO - figure out and document what this is doing
                            modLogger.Log($"       {characterClass.Name} matches".cyan());
                            //stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));

                            if (new SelectClass(characterClass).Check(state, unit)) {
                                Main.Debug($"         - {nameof(SelectClass)}.{nameof(SelectClass.Apply)}*({characterClass}, {unit})".cyan());

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

                    modLogger.Log($"    checking archetypes for {unit.CharacterName}".cyan());
                    // applying archetypes
                    ForEachAppliedMulticlass(state, unit, () => {
                        modLogger.Log($"    {state.SelectedClass.HashKey()} SelectClass-ForEachApplied".cyan().bold());
                        var selectedClass = state.SelectedClass;
                        var archetypeOptions = options.ArchetypeOptions(selectedClass);
                        foreach (var archetype in state.SelectedClass.Archetypes) {
                            // here is where we need to start supporting multiple archetypes of the same class
                            if (archetypeOptions.Contains(archetype)) {
                                modLogger.Log($"    adding archetype: ${archetype.Name}".cyan().bold());
                                AddArchetype addArchetype = new AddArchetype(state.SelectedClass, archetype);
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
        static class LevelUpController_ApplyLevelup_Patch {
            static void Prefix(LevelUpController __instance, UnitEntityData unit) {
                if (!settings.toggleMulticlass) return;

                if (unit == __instance.Preview) {
                    Main.Log($"Unit Preview = {unit.CharacterName}");
                    Main.Log("levelup action：");
                    foreach (var action in __instance.LevelUpActions) {
                        Main.Log($"{action.GetType().ToString()}");
                    }
                }
            }
        }
        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return;
                if (IsAvailable()) {
                    //modLogger.Log($"ApplyClassMechanics.Apply.Postfix - unit: {unit} {unit.CharacterName}");
                    if (state.SelectedClass != null) {
                        ForEachAppliedMulticlass(state, unit, () => {
                            unit.SetClassIsGestalt(state.SelectedClass, true);
                            //modLogger.Log($" - {nameof(ApplyClassMechanics)}.{nameof(ApplyClassMechanics.Apply)}*({state.SelectedClass}{state.SelectedClass.Archetypes}[{state.NextClassLevel}], {unit}) mythic: {state.IsMythicClassSelected} vs {state.SelectedClass.IsMythic}");

                            __instance.Apply_NoStatsAndHitPoints(state, unit);
                        });
                    }
                    List<BlueprintCharacterClass> allAppliedClasses = Main.multiclassMod.AppliedMulticlassSet.ToList();
                    //modLogger.Log($"ApplyClassMechanics.Apply.Postfix - {String.Join(" ", allAppliedClasses.Select(cl => cl.Name))}".orange());
                    allAppliedClasses.Add(state.SelectedClass);
                    SavesBAB.ApplySaveBAB(unit, state, allAppliedClasses.ToArray());
                    HPDice.ApplyHPDice(unit, state, allAppliedClasses.ToArray());
                }
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectFeature_Apply_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (IsAvailable()) {
                    if (__instance.Item != null) {
                        FeatureSelectionState selectionState =
                            ReflectionCache.GetMethod<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>>
                            ("GetSelectionState")(__instance, state);
                        if (selectionState != null) {
                            BlueprintCharacterClass sourceClass = selectionState.SourceFeature?.GetSourceClass(unit);
                            if (sourceClass != null) {
                                __state = new StateReplacer(state);
                                __state.Replace(sourceClass, unit.Progression.GetClassLevel(sourceClass));
                            }
                        }
                    }
                }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(SelectFeature __instance, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (__state != null) {
                    __state.Restore();
                }
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Check), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectFeature_Check_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (IsAvailable()) {
                    if (__instance.Item != null) {
                        FeatureSelectionState selectionState =
                            ReflectionCache.GetMethod<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>>
                            ("GetSelectionState")(__instance, state);
                        if (selectionState != null) {
                            BlueprintCharacterClass sourceClass = selectionState.SourceFeature?.GetSourceClass(unit);
                            if (sourceClass != null) {
                                __state = new StateReplacer(state);
                                __state.Replace(sourceClass, unit.Progression.GetClassLevel(sourceClass));
                            }
                        }
                    }
                }
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(SelectFeature __instance, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) return;
                if (__state != null) {
                    __state.Restore();
                }
            }
        }

        #endregion

        #region Spellbook

        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(MethodBase __originalMethod, ApplySpellbook __instance, LevelUpState state, UnitDescriptor unit) {
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
        static class LevelUpController_Commit_Patch {
            [HarmonyPostfix]
            static void Postfix(LevelUpController __instance) {
                if (!settings.toggleMulticlass) return;

                if (IsAvailable()) {
                    var isCharGen = __instance.State.IsCharGen();
                    var ch = __instance.Unit;
                    var options = MulticlassOptions.Get(isCharGen ? null : ch);
                    if (isCharGen
                            && __instance.Unit.IsCustomCompanion()
                            && options.Count > 0) {
                        Main.Log($"LevelUpController_Commit_Patch - {ch} - {options}");
                        MulticlassOptions.Set(ch, options);
                    }
                }
            }
        }

        #endregion

        [HarmonyPatch(typeof(UnitProgressionData), nameof(UnitProgressionData.SetupLevelsIfNecessary))]
        static class UnitProgressionData_SetupLevelsIfNecessary_Patch {
            static private bool Prefix(UnitProgressionData __instance) {
                if (__instance.m_CharacterLevel.HasValue && __instance.m_MythicLevel.HasValue)
                    return false;
                __instance.UpdateLevelsForGestalt();
                return false;
            }
        }
    }
}
