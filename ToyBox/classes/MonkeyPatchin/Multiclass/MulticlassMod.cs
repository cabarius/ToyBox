using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.EntitySystem.Entities;
//using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.Utility;
using UnityEngine.SceneManagement;
using static ModKit.Utility.ReflectionCache;
using UnityModManager = UnityModManagerNet.UnityModManager;
using ModKit;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Kingdom.Settlements.BuildingComponents;

namespace ToyBox.Multiclass {
    public enum ProgressionPolicy {
        PrimaryClass = 0,
        Average = 1,
        Largest = 2,
        Sum = 3,
    };
    public class MulticlassMod {
        //public HashSet<Type> AbilityCasterCheckerTypes { get; } =
        //    new HashSet<Type>(Assembly.GetAssembly(typeof(IAbilityCasterChecker)).GetTypes()
        //        .Where(type => typeof(IAbilityCasterChecker).IsAssignableFrom(type) && !type.IsInterface));

        public HashSet<Type> ActivatableAbilityRestrictionTypes { get; } =
            new HashSet<Type>(Assembly.GetAssembly(typeof(ActivatableAbilityRestriction)).GetTypes()
                .Where(type => type.IsSubclassOf(typeof(ActivatableAbilityRestriction))));

        public HashSet<Type> BuildingRestrictionTypes { get; } =
            new HashSet<Type>(Assembly.GetAssembly(typeof(BuildingRestriction)).GetTypes()
                .Where(type => type.IsSubclassOf(typeof(BuildingRestriction))).Except(new[] { typeof(DLCRestriction) }));

        public HashSet<Type> EquipmentRestrictionTypes { get; } =
            new HashSet<Type>(Assembly.GetAssembly(typeof(EquipmentRestriction)).GetTypes()
                .Where(type => type.IsSubclassOf(typeof(EquipmentRestriction))));

        public HashSet<Type> PrerequisiteTypes { get; } =
            new HashSet<Type>(Assembly.GetAssembly(typeof(Prerequisite)).GetTypes()
                .Where(type => type.IsSubclassOf(typeof(Prerequisite))));

        public HashSet<BlueprintCharacterClass> AppliedMulticlassSet { get; internal set; }
            = new HashSet<BlueprintCharacterClass>();

        public HashSet<BlueprintProgression> UpdatedProgressions { get; internal set; }
            = new HashSet<BlueprintProgression>();

        public LevelUpController LevelUpController { get; internal set; }

        public HashSet<MethodBase> LockedPatchedMethods { get; internal set; } = new HashSet<MethodBase>();

        public bool IsLevelLocked { get; internal set; }

        public Scene ActiveScene => SceneManager.GetActiveScene();

        public BlueprintCharacterClass[] CharacterClasses => Game.Instance.BlueprintRoot.Progression.CharacterClasses.ToArray();

        public BlueprintCharacterClass[] MythicClasses => Game.Instance.BlueprintRoot.Progression.CharacterMythics.ToArray();

        public BlueprintCharacterClass[] AllClasses => this.CharacterClasses.Concat(this.MythicClasses).ToArray();

        public BlueprintScriptableObject LibraryObject => typeof(ResourcesLibrary).GetFieldValue<BlueprintScriptableObject>("s_LoadedResources");//("s_LibraryObject");

        public Player Player => Game.Instance.Player;
        //public static bool IsCharGen() => !Main.IsInGame && Game.Instance.LevelUpController.State.Mode == CharBuildMode.CharGen;
    }

    public static class MulticlassUtils {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        public static bool IsCharGen(this LevelUpState state) {
            var companionNames = Game.Instance?.Player?.AllCharacters.Where(c => !c.IsMainCharacter).Select(c => c.CharacterName).ToList();
            var isCompanion = companionNames?.Contains(state.Unit.CharacterName) ?? false;
            if (isCompanion) return false;
            return state.Mode == LevelUpState.CharBuildMode.CharGen || state.Unit.CharacterName == "Player Character";
        }

        public static bool IsLevelUp(this LevelUpState state) => state.Mode == LevelUpState.CharBuildMode.LevelUp;

        public static bool IsPreGen(this LevelUpState state) => state.IsPregen;
        public static bool IsClassGestalt(this UnitEntityData ch, BlueprintCharacterClass cl) {
            if (ch.HashKey() == null) return false;
            if (Main.settings.perSave == null) return false;
            var excludeSet = Main.settings.perSave.excludeClassesFromCharLevelSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
            return excludeSet.Contains(cl.AssetGuid.ToString());
        }

        public static void SetClassIsGestalt(this UnitEntityData ch, BlueprintCharacterClass cl, bool isGestalt) {
            if (ch.HashKey() == null) return;
            var classID = cl.AssetGuid.ToString();
            var excludeSet = Main.settings.perSave.excludeClassesFromCharLevelSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
            if (isGestalt) excludeSet.Add(classID);
            else excludeSet.Remove(classID);
            Mod.Trace($"Set - key: {classID} -> {isGestalt} excludeSet: ({string.Join(" ", excludeSet.ToArray())})");
            Main.settings.perSave.excludeClassesFromCharLevelSets[ch.HashKey()] = excludeSet;
            Settings.SavePerSaveSettings();
        }

        public static bool IsClassGestalt(this UnitDescriptor ch, BlueprintCharacterClass cl) {
            if (ch.HashKey() == null) return false;
            var excludeSet = Main.settings.perSave.excludeClassesFromCharLevelSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
            return excludeSet.Contains(cl.AssetGuid.ToString());
        }

        public static void SetClassIsGestalt(this UnitDescriptor ch, BlueprintCharacterClass cl, bool exclude) {
            if (ch.HashKey() == null) return;
            var classID = cl.AssetGuid.ToString();
            var excludeSet = Main.settings.perSave.excludeClassesFromCharLevelSets.GetValueOrDefault(ch.HashKey(), new HashSet<string>());
            if (exclude) excludeSet.Add(classID);
            else excludeSet.Remove(classID);
            // Main.Log($"Set - key: {classID} -> {exclude} excludeSet: ({String.Join(" ", excludeSet.ToArray())})");
            Main.settings.perSave.excludeClassesFromCharLevelSets[ch.HashKey()] = excludeSet;
        }
        public static bool IsClassGestalt(this UnitProgressionData progression, BlueprintCharacterClass cl) {
            var chars = Game.Instance.Player.AllCharacters;
            foreach (var ch in chars) {
                //Mod.Debug($"   {ch.Progression.Owner} vs { progression.Owner}");
                if (ch.Progression.Owner == progression.Owner) {
                    var result = ch.IsClassGestalt(cl);
                    //Mod.Debug($"   found: {ch.HashKey()} - {cl.Name.orange()} - IsGestalt: {result.ToString().cyan()}");
                    return result;
                }
            }
            return false;
        }
    }

    public static partial class MultipleClasses {

        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;
        public static LevelUpController levelUpController { get; internal set; }

        public static bool IsAvailable() => Main.Enabled &&
                settings.toggleMulticlass &&
                levelUpController.IsManualPlayerUnit(true);

        public static bool Enabled {
            get => settings.toggleMulticlass;
            set => settings.toggleMulticlass = value;
        }
        #region Utilities
        
        private static void ForEachAppliedMulticlass(LevelUpState state, UnitDescriptor unit, Action action) {
            var options = MulticlassOptions.Get(state.IsCharGen() ? null : unit);
            var selectedClass = state.SelectedClass;
            StateReplacer stateReplacer = new(state);
            Mod.Trace($"ForEachAppliedMulticlass\n    hash key: {unit.HashKey()}");
            Mod.Trace($"    mythic: {state.IsMythicClassSelected}");
            Mod.Trace($"    options: {options}");
            foreach (var characterClass in Main.multiclassMod.AllClasses) {
                if (characterClass != stateReplacer.SelectedClass 
                    //&& characterClass.IsMythic == selectedClass.IsMythic 
                    && options.Contains(characterClass)) {
                    var classes = unit?.Progression.Classes;
                    var match = classes.Find(c => c.CharacterClass == characterClass);
                    Mod.Trace($"       {characterClass.GetDisplayName()} lvl: {match?.Level ?? -1}");
                    if (state.IsMythicClassSelected == characterClass.IsMythic) {
                        stateReplacer.Replace(characterClass, unit.Progression.GetClassLevel(characterClass));
                        action();
                    }
                }
            }
            stateReplacer.Restore();
        }
        public static void UpdateLevelsForGestalt(this UnitProgressionData progression) {
            progression.m_CharacterLevel = new int?(0);
            progression.m_MythicLevel = new int?(0);
            int? nullable;
            // this logic is to work around a case where you may mark a character gestalt and then load an earlier save and have them get level 0 which breaks level up.  Here we will detect that you have no main class and prevent your first class from being gestalt
            var classToEnsureNonGestalt = progression.Classes.FirstOrDefault()?.CharacterClass ?? null;
            foreach (var classData in progression.Classes) {
                if (!progression.IsClassGestalt(classData.CharacterClass)) {
                    classToEnsureNonGestalt = classData.CharacterClass;
                    break;
                }
            }
            foreach (var classData in progression.Classes) {
                var cl = classData.CharacterClass;
                var shouldSkip = progression.IsClassGestalt(cl) && cl != classToEnsureNonGestalt;
                if (!shouldSkip) {
                    if (classData.CharacterClass.IsMythic) {
                        nullable = progression.m_MythicLevel;
                        var level = classData.Level;
                        progression.m_MythicLevel = nullable.HasValue ? new int?(nullable.GetValueOrDefault() + level) : new int?();
                    }
                    else {
                        nullable = progression.m_CharacterLevel;
                        var level = classData.Level;
                        progression.m_CharacterLevel = nullable.HasValue ? new int?(nullable.GetValueOrDefault() + level) : new int?();
                    }
                }
            }
        }
        public static void SyncAllGestaltState() {
            var chars = Game.Instance?.Player.AllCharacters;
            chars?.ForEach(ch => ch.Progression.UpdateLevelsForGestalt());
        }
        #endregion
    }
}
