// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityModManager = UnityModManagerNet.UnityModManager;
using ModKit.Utility;

namespace ToyBox.Multiclass {
    static class MultipleClasses {
        public static Settings settings = Main.settings;

        public static Player player = Game.Instance.Player;

        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;

        public static bool IsAvailable() {
            return Main.Enabled &&
                   settings.toggleMulticlass &&
                   General.levelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => settings.toggleMulticlass;

            set => settings.toggleMulticlass = value;
        }

        #region Utilities

        private static void ForEachAppliedMulticlass(LevelUpState state, UnitDescriptor unit, Action action) {
            StateReplacer stateReplacer = new StateReplacer(state);

            var unitMulticlassSet = unit.GetMulticlassSet();

            modLogger.Log($"hash key: {unit.HashKey()} multiclass set: {unitMulticlassSet.ToArray()}");

            foreach (BlueprintCharacterClass characterClass in Main.multiclassMod.CharacterClasses
                                                                   .Where(characterClass => characterClass != stateReplacer.SelectedClass
                                                                                            && unit.GetMulticlassSet()
                                                                                                   .Contains(characterClass.AssetGuid.ToString()))) {
                stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));
                action();
            }

            stateReplacer.Restore();
        }

        #endregion

        #region Class Level & Archetype

        [HarmonyPatch(typeof(SelectClass), nameof(SelectClass.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectClass_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) {
                    return;
                }

                //Logger.ModLog($"SelectClass.Apply.Postfix, is available  = {IsAvailable()}");
                if (IsAvailable()) {
                    Main.multiclassMod.AppliedMulticlassSet.Clear();
                    Main.multiclassMod.UpdatedProgressions.Clear();

                    // get multi-class setting
                    HashSet<string> selectedMulticlassSet;

                    if (!state.IsCharGen()) {
                        selectedMulticlassSet = unit.Unit != null ? unit.Unit.Descriptor.GetMulticlassSet() : unit.GetMulticlassSet();
                    }
                    else {
                        selectedMulticlassSet = Main.settings.charGenMulticlassSet;
                    }


                    if (selectedMulticlassSet == null || selectedMulticlassSet.Count == 0) {
                        return;
                    }

                    // applying classes
                    StateReplacer stateReplacer = new StateReplacer(state);

                    foreach (BlueprintCharacterClass bcc in Main.multiclassMod.CharacterClasses
                                                                           .Where(c => c != stateReplacer.SelectedClass 
                                                                                       && selectedMulticlassSet.Contains(c.AssetGuid.ToString()))) {
                        stateReplacer.Replace(null, 0);

                        if (new SelectClass(bcc).Check(state, unit)) {
                            Main.Debug(string.Format(" - {0}.{1}*({2}, {3})", nameof(SelectClass), nameof(SelectClass.Apply), bcc, unit));

                            unit.Progression.AddClassLevel_NotCharacterLevel(bcc);
                            bcc.RestrictPrerequisites(unit, state);

                            Main.multiclassMod.AppliedMulticlassSet.Add(bcc);
                        }
                    }

                    stateReplacer.Restore();

                    // applying archetypes
                    ForEachAppliedMulticlass(state, unit, () => {
                                                              foreach (BlueprintArchetype archetype in state.SelectedClass.Archetypes) {
                                                                  if (selectedMulticlassSet.Contains(archetype.AssetGuid.ToString())) {
                                                                      AddArchetype addArchetype = new AddArchetype(state.SelectedClass, archetype);

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
                if (!settings.toggleMulticlass) {
                    return;
                }

                if (unit == __instance.Preview) {
                    Main.Log($"Unit Preview = {unit.CharacterName}");
                    Main.Log("levelup action：");

                    foreach (var action in __instance.LevelUpActions) {
                        Main.Log($"{action.GetType()}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) {
                    return;
                }

                modLogger.Log($"ApplyClassMechanics.Apply.Postfix, Isavailable={IsAvailable()} unit: {unit} {unit.CharacterName}");

                if (IsAvailable()) {
                    if (state.SelectedClass != null) {
                        ForEachAppliedMulticlass(state, unit, () => {
                                                                  modLogger.Log(string.Format(" - {0}.{1}*({2}[{3}], {4})",
                                                                                              nameof(ApplyClassMechanics),
                                                                                              nameof(ApplyClassMechanics.Apply),
                                                                                              state.SelectedClass,
                                                                                              state.NextClassLevel,
                                                                                              unit));

                                                                  __instance.Apply_NoStatsAndHitPoints(state, unit);
                                                              });
                    }

                    List<BlueprintCharacterClass> allAppliedClasses = Main.multiclassMod.AppliedMulticlassSet.ToList();
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
                if (!settings.toggleMulticlass) {
                    return;
                }

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
                if (!settings.toggleMulticlass) {
                    return;
                }

                __state?.Restore();
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Check), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectFeature_Check_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) {
                    return;
                }

                if (!IsAvailable()) {
                    return;
                }

                if (__instance.Item == null) {
                    return;
                }

                FeatureSelectionState selectionState = ReflectionCache
                    .GetMethod<SelectFeature, Func<SelectFeature, LevelUpState, FeatureSelectionState>> ("GetSelectionState")(__instance, state);

                if (selectionState == null) {
                    return;
                }

                BlueprintCharacterClass sourceClass = selectionState.SourceFeature?.GetSourceClass(unit);

                if (sourceClass == null) {
                    return;
                }

                __state = new StateReplacer(state);
                __state.Replace(sourceClass, unit.Progression.GetClassLevel(sourceClass));
            }

            [HarmonyPostfix, HarmonyPriority(Priority.Last)]
            static void Postfix(SelectFeature __instance, ref StateReplacer __state) {
                if (!settings.toggleMulticlass) {
                    return;
                }

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
                if (!settings.toggleMulticlass) {
                    return;
                }

                if (IsAvailable() && !Main.multiclassMod.LockedPatchedMethods.Contains(__originalMethod)) {
                    Main.multiclassMod.LockedPatchedMethods.Add(__originalMethod);

                    ForEachAppliedMulticlass(state, unit, () => __instance.Apply(state, unit));

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
                if (!settings.toggleMulticlass) {
                    return;
                }

                if (IsAvailable()) {
                    var charGenMulticlassSet = settings.charGenMulticlassSet;

                    if (__instance.State.IsCharGen() && __instance.Unit.IsCustomCompanion() && charGenMulticlassSet.Count > 0) {
                        __instance.Unit.SetMulticlassSet(charGenMulticlassSet);
                    }
                }
            }
        }

        #endregion
    }
}