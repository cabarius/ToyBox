// borrowed shamelessly and enchanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.Controllers.Rest;
using Kingmaker.Controllers.Rest.Cooking;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.Dungeon.Units.Debug;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Formations;
using DG.Tweening;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.PubSubSystem;
using Kingmaker.RandomEncounters;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.TextTools;
using Kingmaker.UI;
//using Kingmaker.UI._ConsoleUI.Models;
using Kingmaker.UI.Common;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Group;
using Kingmaker.UI.IngameMenu;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.Log;
using Kingmaker.UI.MainMenuUI;
using Kingmaker.UI.MVVM;
using Kingmaker.UI.MVVM.CharGen;
using Kingmaker.UI.MVVM.CharGen.Phases;
using Kingmaker.UI.MVVM.CharGen.Phases.Mythic;
using Kingmaker.UI.RestCamp;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.ServiceWindow.LocalMap;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using Kingmaker.View.Spawners;
using Kingmaker.Visual;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.HitSystem;
using Kingmaker.Visual.LocalMap;
using Kingmaker.Visual.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using Kingmaker.UI.ActionBar;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using TMPro;
using TurnBased.Controllers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Kingmaker.UnitLogic.Class.LevelUp.LevelUpState;
using UnityModManager = UnityModManagerNet.UnityModManager;
using static ModMaker.Utility.ReflectionCache;
using ModMaker.Utility;

namespace ToyBox.Multiclass {
    static class MultipleClasses {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        public static bool IsAvailable() {
            return Main.Enabled &&
                settings.toggleMulticlass &&
                General.levelUpController.IsManualPlayerUnit();
        }

        public static bool Enabled {
            get => settings.toggleMulticlass;
            set => settings.toggleMulticlass = value;
        }

#if false
        public static HashSet<string> MainCharSet => settings.selectedMulticlassSet;

        public static HashSet<string> CharGenSet => settings.selectedCharGenMulticlassSet;

        public static SerializableDictionary<string, HashSet<string>> CompanionSets => settings.selectedCompanionMulticlassSet;

        public static HashSet<string> DemandCompanionSet(string name) {
            if (!settings.selectedCompanionMulticlassSet.TryGetValue(name, out HashSet<string> classes)) {
                settings.selectedCompanionMulticlassSet.Add(name, classes = new HashSet<string>());
            }
            return classes;
        }
#endif

#region Utilities

        private static void ForEachAppliedMulticlass(LevelUpState state, UnitDescriptor unit, Action action) {
            StateReplacer stateReplacer = new StateReplacer(state);
            foreach (BlueprintCharacterClass characterClass in Main.multiclassMod.AppliedMulticlassSet) {
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
                //Logger.ModLog($"SelectClass.Apply.Postfix, is avialable  = {IsAvailable()}");
                if (IsAvailable()) {
                    Main.multiclassMod.AppliedMulticlassSet.Clear();
                    Main.multiclassMod.UpdatedProgressions.Clear();

                    // get multi-class setting
                    HashSet<string> selectedMulticlassSet;
                    if (!state.IsCharGen()) {
                        if (unit.Unit.CopyOf != null) {
                            selectedMulticlassSet = unit.Unit.CopyOf.Entity.Descriptor.GetMulticlassSet();
                        }
                        else {
                            selectedMulticlassSet = unit.GetMulticlassSet();
                        }
                    }
                    else {
                        selectedMulticlassSet = Main.settings.charGenMulticlassSet;
                    }


                    if (selectedMulticlassSet == null || selectedMulticlassSet.Count == 0)
                        return;

                    // applying classes
                    StateReplacer stateReplacer = new StateReplacer(state);
                    foreach (BlueprintCharacterClass characterClass in Main.multiclassMod.CharacterClasses) {
                        if (characterClass != stateReplacer.SelectedClass && selectedMulticlassSet.Contains(characterClass.AssetGuid)) {
                            stateReplacer.Replace(null, 0);
                            //Logger.ModLog($"进行{characterClass.AssetGuid}（{characterClass.Name}）的SelectClass操作");
                            //stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));

                            if (new SelectClass(characterClass).Check(state, unit)) {
                                Logger.ModLoggerDebug($" - {nameof(SelectClass)}.{nameof(SelectClass.Apply)}*({characterClass}, {unit})");

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

                    // applying archetypes
                    ForEachAppliedMulticlass(state, unit, () => {
                        //Logger.ModLog($"进行{state.SelectedClass.AssetGuid}（{state.SelectedClass.Name}）的SelectClass-ForEachApplied操作");
                        foreach (BlueprintArchetype archetype in state.SelectedClass.Archetypes) {
                            if (selectedMulticlassSet.Contains(archetype.AssetGuid)) {
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
                if(unit == __instance.Preview) {
                    Logger.ModLog($"Unit Preview = {unit.CharacterName}");
                    Logger.ModLog("所有的levelup action：");
                    foreach(var action in __instance.LevelUpActions) {
                        Logger.ModLog($"{action.GetType().ToString()}");
                    }
                }
            }
        }
        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(ApplyClassMechanics __instance, LevelUpState state, UnitDescriptor unit) {
                //Logger.ModLog($"ApplyClassMechanics.Apply.Postfix, Isavailable={IsAvailable()}");
                if (IsAvailable()) {
                    if (state.SelectedClass != null) {
                        ForEachAppliedMulticlass(state, unit, () => {
                            Logger.ModLoggerDebug($" - {nameof(ApplyClassMechanics)}.{nameof(ApplyClassMechanics.Apply)}*({state.SelectedClass}[{state.NextClassLevel}], {unit})");

                            __instance.Apply_NoStatsAndHitPoints(state, unit);
                        });
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectFeature_Apply_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static void Prefix(SelectFeature __instance, LevelUpState state, UnitDescriptor unit, ref StateReplacer __state) {
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
                if (IsAvailable()) {
                    var charGenMulticlassSet = settings.charGenMulticlassSet;
                    if (__instance.State.IsCharGen() 
                        && __instance.Unit.IsCustomCompanion() 
                        && charGenMulticlassSet.Count > 0) {
                        __instance.Unit.SetMulticlassSet(charGenMulticlassSet);
                    }
                }
            }
        }
#endregion
    }
}
