// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityModManagerNet;
using System;
using System.Linq;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.RuleSystem;
//using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using ModKit.Utility;
using ModKit;

namespace ToyBox.Multiclass {
    public static class WrathExtensionsMulticlass {
        public static void AddClassLevel_NotCharacterLevel(this UnitProgressionData instance, BlueprintCharacterClass characterClass) {
            //instance.SureClassData(characterClass).Level++;
            Mod.Debug($"AddClassLevel_NotCharLevel: class = {characterClass.name.cyan()} - lvl:{instance.GetClassLevel(characterClass)} - {string.Join(", ", instance.Features.Enumerable.Select(f => f.Name.orange()))}");
            ReflectionCache.GetMethod<UnitProgressionData, Func<UnitProgressionData, BlueprintCharacterClass, ClassData>>
                ("SureClassData")(instance, characterClass).Level++;
            //instance.CharacterLevel++;
            instance.m_ClassesOrder.Add(characterClass);
            instance.Classes.Sort();
            instance.Features.AddFeature(characterClass.Progression, null).SetSource(characterClass, 1);
            instance.Owner.OnGainClassLevel(characterClass);
            //int[] bonuses = BlueprintRoot.Instance.Progression.XPTable.Bonuses;
            //int val = bonuses[Math.Min(bonuses.Length - 1, instance.CharacterLevel)];
            //instance.Experience = Math.Max(instance.Experience, val);
        }

        public static void Apply_NoStatsAndHitPoints(this ApplyClassMechanics instance, LevelUpState state, UnitDescriptor unit) {
            Mod.Trace($"Apply_NoStatsAndHitPoints: unit = {unit.CharacterName}, state.class = {(state.SelectedClass == null ? "NULL" : state.SelectedClass.Name)}");
            if (state.SelectedClass != null) {
                var classData = unit.Progression.GetClassData(state.SelectedClass);
                if (classData != null) {
                    //GetMethodDel<ApplyClassMechanics, Action<ApplyClassMechanics, LevelUpState, ClassData, UnitDescriptor>>
                    //    ("ApplyBaseStats")(null, state, classData, unit);
                    //GetMethodDel<ApplyClassMechanics, Action<ApplyClassMechanics, LevelUpState, ClassData, UnitDescriptor>>
                    //    ("ApplyHitPoints")(null, state, classData, unit);
                    ReflectionCache.GetMethod<ApplyClassMechanics, Action<ApplyClassMechanics, ClassData, UnitDescriptor>>
                        ("ApplyClassSkills")(null, classData, unit);
                    ReflectionCache.GetMethod<ApplyClassMechanics, Action<ApplyClassMechanics, LevelUpState, UnitDescriptor>>
                        ("ApplyProgressions")(null, state, unit);
                }
            }
        }

        public static BlueprintCharacterClass GetOwnerClass(this BlueprintSpellbook spellbook, UnitDescriptor unit) => unit.Progression.Classes.FirstOrDefault(item => item.Spellbook == spellbook)?.CharacterClass;

        public static ClassData GetOwnerClassData(this BlueprintSpellbook spellbook, UnitDescriptor unit) => unit.Progression.Classes.FirstOrDefault(item => item.Spellbook == spellbook);

        public static BlueprintCharacterClass GetSourceClass(this BlueprintFeature feature, UnitDescriptor unit) => unit.Progression.Features.Enumerable.FirstOrDefault(item => item.Blueprint == feature)?.GetSourceClass();

        public static bool IsChildProgressionOf(this BlueprintProgression progression, UnitDescriptor unit, BlueprintCharacterClass characterClass) {
            var classData = unit.Progression.GetClassData(characterClass);
            if (classData != null) {
                /*
                if (progression.Classes.Contains(characterClass) &&
                    !progression.Archetypes.Intersect(characterClass.Archetypes).Any())
                    return true;
                if (progression.Archetypes.Intersect(unit.Progression.GetClassData(characterClass).Archetypes).Any())
                    return true;
                    */
                if (progression.Classes.Contains(characterClass))
                    return true;
            }
            return false;
        }

        public static bool IsManualPlayerUnit(this LevelUpController controller, bool allowPet = false, bool allowAutoCommit = false, bool allowPregen = false) =>
            //Main.Log($"controller: {controller} AutoCommit: {controller.AutoCommit}");
            //Main.Log($"    unit: {controller.Unit} isPlayerFaction: {controller.Unit.IsPlayerFaction} isPet: {controller.Unit.IsPet}");
            //Main.Log($"    levelup state: {controller.State}");

            controller != null
                && controller.Unit.IsPlayerFaction
                && (allowPet || !controller.Unit.IsPet
                //&& (allowAutoCommit || !controller.AutoCommit)
                //&& (allowPregen || !controller.State.IsPreGen()
                );

#if false
        public static void SetSpellbook(this CharBPhaseSpells instance, BlueprintCharacterClass characterClass) {
            LevelUpState state = Game.Instance.LevelUpController.State;
            BlueprintCharacterClass selectedClass = state.SelectedClass;
            state.SelectedClass = characterClass;
            ReflectionCache.GetMethod<CharBPhaseSpells, Action<CharBPhaseSpells>>("SetupSpellBookView")(instance);
            state.SelectedClass = selectedClass;
        }
#endif
        //public static ClassData SureClassData(this UnitProgressionData progression, BlueprintCharacterClass characterClass)
        //{
        //    ClassData classData = progression.Classes.FirstOrDefault((ClassData cd) => cd.CharacterClass == characterClass);
        //    if (classData == null)
        //    {
        //        classData = new ClassData(characterClass);
        //        progression.Classes.Add(classData);
        //    }
        //    return classData;
        //}
    }
}