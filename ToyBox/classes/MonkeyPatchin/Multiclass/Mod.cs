﻿using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Kingdom.Settlements.BuildingComponents;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Class.LevelUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;
using static ModKit.Utility.ReflectionCache;

namespace ToyBox.Multiclass {
    public enum ProgressionPolicy {
        PrimaryClass = 0,

        Average = 1,

        Largest = 2,

        Sum = 3,
    };

    public class Mod {
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

        public BlueprintScriptableObject LibraryObject =>
            typeof(ResourcesLibrary).GetFieldValue<BlueprintScriptableObject>("s_LoadedResources");

        public Player Player => Game.Instance.Player;
    }
}