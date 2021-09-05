// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using UnityEditor;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Armies.TacticalCombat.Blueprints;
using Kingmaker.Armies.TacticalCombat.Brain;
using Kingmaker.Armies.TacticalCombat.Brain.Considerations;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Credits;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Blueprints.Console;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Interaction;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Tutorial;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.LevelUp;
//using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;
using Kingmaker.Assets.UI;
using ModKit.Utility;
using static ModKit.Utility.ReflectionCache;
using Alignment = Kingmaker.Enums.Alignment;
using ModKit;

namespace ToyBox.Multiclass {
    public static class WrathExtensionsMulticlass {
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;

        public static HashSet<string> GetMulticlassSet(this UnitEntityData ch) {
            if (ch.HashKey() == null) return null;
            return Main.settings.selectedMulticlassSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
        }
        public static void SetMulticlassSet(this UnitEntityData ch, HashSet<string> multiclassSet) {
            if (ch.HashKey() == null) return;
            Main.settings.selectedMulticlassSets[ch.HashKey()] = multiclassSet;
        }
        public static HashSet<string> GetMulticlassSet(this UnitDescriptor ch) {
            if (ch.HashKey() == null) return null;
            return Main.settings.selectedMulticlassSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
        }
        public static void SetMulticlassSet(this UnitDescriptor ch, HashSet<string> multiclassSet) {
            if (ch.HashKey() == null) return;
            Main.settings.selectedMulticlassSets[ch.HashKey()] = multiclassSet;
        }

        public static void AddClassLevel_NotCharacterLevel(this UnitProgressionData instance, BlueprintCharacterClass characterClass) {
            //instance.SureClassData(characterClass).Level++;
            Main.Log($"AddClassLevel_NotCharLevel: class = {characterClass}");
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
            Main.Log($"Apply_NoStatsAndHitPoints: unit = {unit.CharacterName}, state.class = {(state.SelectedClass==null?"NULL":state.SelectedClass.Name)}");
            if (state.SelectedClass != null) {
                ClassData classData = unit.Progression.GetClassData(state.SelectedClass);
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

        public static bool IsManualPlayerUnit(this LevelUpController controller, bool allowPet = false, bool allowAutoCommit = false, bool allowPregen = false) {
            //Main.Log($"controller: {controller}");
            //Main.Log($"    unit: {controller.Unit}");
            //Main.Log($"    levelup state: {controller.State}");
            return controller != null &&
                controller.Unit.IsPlayerFaction &&
                (allowPet || !controller.Unit.IsPet) &&
                (allowAutoCommit || !controller.AutoCommit);
                //&& (allowPregen || !controller.State.IsPreGen()
                
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