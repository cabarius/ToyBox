// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using UnityModManagerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using ModKit.Utility;
using ModKit;

namespace ToyBox.Multiclass {
    public static class WrathExtensionsMulticlass {
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;

        public static HashSet<string> GetMulticlassSet(this UnitEntityData ch) {
            return ch.HashKey() == null ? null : Main.settings.selectedMulticlassSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
        }

        public static void SetMulticlassSet(this UnitEntityData ch, HashSet<string> multiclassSet) {
            if (ch.HashKey() == null) {
                return;
            }

            Main.settings.selectedMulticlassSets[ch.HashKey()] = multiclassSet;
        }

        public static HashSet<string> GetMulticlassSet(this UnitDescriptor ch) {
            return ch.HashKey() == null ? null : Main.settings.selectedMulticlassSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
        }

        public static void SetMulticlassSet(this UnitDescriptor ch, HashSet<string> multiclassSet) {
            if (ch.HashKey() == null) {
                return;
            }

            Main.settings.selectedMulticlassSets[ch.HashKey()] = multiclassSet;
        }

        public static void AddClassLevel_NotCharacterLevel(this UnitProgressionData instance, BlueprintCharacterClass characterClass) {
            Main.Log($"AddClassLevel_NotCharLevel: class = {characterClass}");

            ReflectionCache.GetMethod<UnitProgressionData, Func<UnitProgressionData, BlueprintCharacterClass, ClassData>>
                ("SureClassData")(instance, characterClass).Level++;

            instance.m_ClassesOrder.Add(characterClass);
            instance.Classes.Sort();
            instance.Features.AddFeature(characterClass.Progression, null).SetSource(characterClass, 1);
            instance.Owner.OnGainClassLevel(characterClass);
        }

        public static void Apply_NoStatsAndHitPoints(this ApplyClassMechanics instance, LevelUpState state, UnitDescriptor unit) {
            Main.Log(string.Format("Apply_NoStatsAndHitPoints: unit = {0}, state.class = {1}",
                                   unit.CharacterName,
                                   state.SelectedClass == null ? "NULL" : state.SelectedClass.Name));

            if (state.SelectedClass == null) {
                return;
            }

            ClassData classData = unit.Progression.GetClassData(state.SelectedClass);

            if (classData == null) {
                return;
            }

            ReflectionCache.GetMethod<ApplyClassMechanics, Action<ApplyClassMechanics, ClassData, UnitDescriptor>>("ApplyClassSkills")(null, classData, unit);

            ReflectionCache.GetMethod<ApplyClassMechanics, Action<ApplyClassMechanics, LevelUpState, UnitDescriptor>>("ApplyProgressions")(null, state, unit);
        }

        public static BlueprintCharacterClass GetOwnerClass(this BlueprintSpellbook spellbook, UnitDescriptor unit) {
            return unit.Progression.Classes.FirstOrDefault(item => item.Spellbook == spellbook)?.CharacterClass;
        }

        public static ClassData GetOwnerClassData(this BlueprintSpellbook spellbook, UnitDescriptor unit) {
            return unit.Progression.Classes.FirstOrDefault(item => item.Spellbook == spellbook);
        }

        public static BlueprintCharacterClass GetSourceClass(this BlueprintFeature feature, UnitDescriptor unit) {
            return unit.Progression.Features.Enumerable.FirstOrDefault(item => item.Blueprint == feature)?.GetSourceClass();
        }

        public static bool IsChildProgressionOf(this BlueprintProgression progression, UnitDescriptor unit, BlueprintCharacterClass characterClass) {
            ClassData classData = unit.Progression.GetClassData(characterClass);

            return classData != null && progression.Classes.Contains(characterClass);
        }

        public static bool IsManualPlayerUnit(this LevelUpController controller,
                                              bool allowPet = false,
                                              bool allowAutoCommit = false,
                                              bool allowPregen = false) {
            return controller != null
                   && controller.Unit.IsPlayerFaction
                   && (allowPet || !controller.Unit.IsPet)
                   && (allowAutoCommit || !controller.AutoCommit);
        }

        public static bool IsCharGen(this LevelUpState state) {
            return state.Mode == LevelUpState.CharBuildMode.CharGen;
        }

        public static bool IsLevelUp(this LevelUpState state) {
            return state.Mode == LevelUpState.CharBuildMode.LevelUp;
        }

        public static bool IsPreGen(this LevelUpState state) {
            return state.IsPregen;
        }
    }
}