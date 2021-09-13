﻿// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
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
using Kingmaker.UI.MVVM._PCView.CharGen;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Mythic;
//using Kingmaker.UI.RestCamp;
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
using static ModKit.Utility.ReflectionCache;
using ModKit.Utility;
using ToyBox.Multiclass;
using ModKit;

namespace ToyBox.Multiclass {
    static class MultipleClasses {

        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;

        public static bool IsAvailable() {
            return Main.Enabled &&
                settings.toggleMulticlass &&
                General.levelUpController.IsManualPlayerUnit(true);
        }

        public static bool Enabled {
            get => settings.toggleMulticlass;
            set => settings.toggleMulticlass = value;
        }

        #region Utilities

#if true
        private static void ForEachAppliedMulticlass(LevelUpState state, UnitDescriptor unit, Action action) {
            var options = MulticlassOptions.Get(state.IsCharGen() ? null : unit);
            StateReplacer stateReplacer = new StateReplacer(state);
            modLogger.Log($"ForEachAppliedMulticlass\n    hash key: {unit.HashKey()}");
            modLogger.Log($"    mythic: {state.IsMythicClassSelected}");
            modLogger.Log($"    options: {options}");
            foreach (BlueprintCharacterClass characterClass in Main.multiclassMod.AllClasses) {
                if (characterClass != stateReplacer.SelectedClass && options.Contains(characterClass)) {
                    modLogger.Log($"       {characterClass.GetDisplayName()} ");
                    if (state.IsMythicClassSelected == characterClass.IsMythic) {
                        stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));
                        action();
                    }
                }
            }
            stateReplacer.Restore();
        }
#else
        private static void ForEachAppliedMulticlass(LevelUpState state, UnitDescriptor unit, Action action) {
            var multiclassSet = MulticlassUtils.SelectedMulticlassSet(unit, state.IsCharGen());
            StateReplacer stateReplacer = new StateReplacer(state);
            modLogger.Log($"ForEachAppliedMulticlass\n    hash key: {unit.HashKey()}");
            modLogger.Log($"    mythic: {state.IsMythicClassSelected}");
            modLogger.Log($"    multiclass set: {multiclassSet.Count}");
            foreach (BlueprintCharacterClass characterClass in Main.multiclassMod.AllClasses) {
                if (characterClass != stateReplacer.SelectedClass && MulticlassUtils.GetMulticlassSet(unit).Contains(characterClass.AssetGuid.ToString())) {
                    modLogger.Log($"       {characterClass.GetDisplayName()} ");
                    if (state.IsMythicClassSelected == characterClass.IsMythic) {
                        stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));
                        action();
                    }
                }
            }
            stateReplacer.Restore();
        }
#endif
        #endregion

        #region Class Level & Archetype

#if true
        [HarmonyPatch(typeof(SelectClass), nameof(SelectClass.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectClass_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return;
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
#else
        [HarmonyPatch(typeof(SelectClass), nameof(SelectClass.Apply), new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class SelectClass_Apply_Patch {
            [HarmonyPostfix]
            static void Postfix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return;
                modLogger.Log($"SelectClass.Apply.Postfix, is available  = {IsAvailable()} isCharGen: {state.IsCharGen()} vs Mod.isCharGen {Mod.IsCharGen()}");
                if (IsAvailable()) {
                    Main.multiclassMod.AppliedMulticlassSet.Clear();
                    Main.multiclassMod.UpdatedProgressions.Clear();

                    // get multi-class setting
                    HashSet<string> selectedMulticlassSet = MulticlassUtils.SelectedMulticlassSet(unit, state.IsCharGen());

                    if (selectedMulticlassSet == null || selectedMulticlassSet.Count == 0)
                        return;

                    modLogger.Log($"selected {selectedMulticlassSet.Count} multiclass classes:");
//                    selectedMulticlassSet.ForEach(cl => modLogger.Log($"    {cl}"));

                    // applying classes
                    StateReplacer stateReplacer = new StateReplacer(state);
                    foreach (BlueprintCharacterClass characterClass in Main.multiclassMod.AllClasses) {
                        if (selectedMulticlassSet.Contains(characterClass.AssetGuid.ToString())) {
                            modLogger.Log($"   checking {characterClass.AssetGuid} {characterClass.GetDisplayName()} ");
                        }
                        if (characterClass != stateReplacer.SelectedClass
                            && characterClass.IsMythic == state.IsMythicClassSelected
                            && selectedMulticlassSet.Contains(characterClass.AssetGuid.ToString())
                            ) {
                            stateReplacer.Replace(null, 0);
                            modLogger.Log($"       {characterClass.Name} matches");
                            //stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));

                            if (new SelectClass(characterClass).Check(state, unit)) {
                                Main.Debug($"         - {nameof(SelectClass)}.{nameof(SelectClass.Apply)}*({characterClass}, {unit})");

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
                        modLogger.Log($"    {state.SelectedClass.AssetGuid}（{state.SelectedClass.Name}）SelectClass-ForEachApplied");
                        foreach (BlueprintArchetype archetype in state.SelectedClass.Archetypes) {
                            if (selectedMulticlassSet.Contains(archetype.AssetGuid.ToString())) {
                                modLogger.Log($"    adding archetype: ${archetype.Name}");
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
#endif
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
                    modLogger.Log($"ApplyClassMechanics.Apply.Postfix - unit: {unit} {unit.CharacterName}");
                    if (state.SelectedClass != null) {
                        ForEachAppliedMulticlass(state, unit, () => {
                            unit.SetClassIsGestalt(state.SelectedClass, true);
                            modLogger.Log($" - {nameof(ApplyClassMechanics)}.{nameof(ApplyClassMechanics.Apply)}*({state.SelectedClass}{state.SelectedClass.Archetypes}[{state.NextClassLevel}], {unit}) mythic: {state.IsMythicClassSelected} vs {state.SelectedClass.IsMythic}");

                            __instance.Apply_NoStatsAndHitPoints(state, unit);
                        });
                    }
                    List<BlueprintCharacterClass> allAppliedClasses = Main.multiclassMod.AppliedMulticlassSet.ToList();
                    modLogger.Log($"ApplyClassMechanics.Apply.Postfix - {String.Join(" ", allAppliedClasses.Select(cl => cl.Name))}".orange());
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

#if true
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
#else
        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Commit))]
        static class LevelUpController_Commit_Patch {
            [HarmonyPostfix]
            static void Postfix(LevelUpController __instance) {
                if (!settings.toggleMulticlass) return;

                if (IsAvailable()) {
                    var isCharGen = Mod.IsCharGen();
                    var ch = __instance.Unit;
                    var options = MulticlassOptions.Get(isCharGen ? null : ch);
                    if (    isCharGen
                            && __instance.Unit.IsCustomCompanion()
                            && options.Count > 0) {
                        Main.Log($"LevelUpController_Commit_Patch - {ch} - {options}");
                        MulticlassOptions.Set(ch, options);
                    }
                }
            }
        }
#endif
#endregion

        public static void UpdateLevelsForGestalt(this UnitProgressionData __instance) {
            __instance.m_CharacterLevel = new int?(0);
            __instance.m_MythicLevel = new int?(0);
            int? nullable;
            foreach (ClassData classData in __instance.Classes) {
                var shouldSkip = __instance.IsClassGestalt(classData.CharacterClass);
                //modLogger.Log($"- owner: {__instance.Owner} class: {classData.CharacterClass.Name} shouldSkip: {shouldSkip}");
                if (!shouldSkip) {
                    if (classData.CharacterClass.IsMythic) {
                        nullable = __instance.m_MythicLevel;
                        int level = classData.Level;
                        __instance.m_MythicLevel = nullable.HasValue ? new int?(nullable.GetValueOrDefault() + level) : new int?();
                    }
                    else {
                        nullable = __instance.m_CharacterLevel;
                        int level = classData.Level;
                        __instance.m_CharacterLevel = nullable.HasValue ? new int?(nullable.GetValueOrDefault() + level) : new int?();
                    }
                }
            }
        }

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
