// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.UI;
using Kingmaker.UI.Common;
//using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Kingmaker.Items.WeaponStatsHelper;
using Alignment = Kingmaker.Enums.Alignment;

namespace ToyBox {
    public static class RTExtensions {
        public static string HashKey(this UnitEntityData ch) => ch.CharacterName;  // + ch.UniqueId; }
        public static string HashKey(this MechanicEntity entity) =>
            entity is UnitEntityData ch ? ch.CharacterName : entity.Name;
        public static string HashKey(this BlueprintCharacterClass cl) => cl.NameSafe();
        public static string HashKey(this BlueprintArchetype arch) => arch.NameSafe();
        public static UnitEntityData GetCurrentCharacter() {
            var firstSelectedUnit = Game.Instance.SelectionCharacter.FirstSelectedUnit;
            return (object)firstSelectedUnit != null ? firstSelectedUnit : (UnitEntityData)Game.Instance.Player.MainCharacterEntity;
        }
    }
}