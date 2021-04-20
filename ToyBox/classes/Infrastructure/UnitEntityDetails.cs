// borrowed shamelessly and enchanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT Licenseusing Kingmaker;
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
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
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;

namespace ToyBox 
{
    public enum UnitSelectType
    {
        Off,
        You,
        Party,
        Friendly,
        Enemies,
        Everyone,
    }
    static class UnitEntityDataUtils
    {
        public static float GetMaxSpeed(List<UnitEntityData> data)
        {
            return (data.Select((u => u.ModifiedSpeedMps)).Max());
        }

        public static bool CheckUnitEntityData(UnitEntityData unitEntityData, UnitSelectType selectType)
        {
            if (unitEntityData == null) return false;
            switch (selectType)
            {
                case UnitSelectType.Everyone:
                    return true;
                case UnitSelectType.Party:
                    if (unitEntityData.IsPlayerFaction)
                    {
                        return true;
                    }
                    return false;
                case UnitSelectType.You:
                    if (unitEntityData.IsMainCharacter)
                    {
                        return true;
                    }
                    return false;
                case UnitSelectType.Friendly:
                    return !unitEntityData.IsEnemy(GameHelper.GetPlayerCharacter());
                case UnitSelectType.Enemies:
                    // TODO - should this be IsEnemy instead?
                    if (!unitEntityData.IsPlayerFaction && unitEntityData.Descriptor.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction))
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
        public static void Kill(UnitEntityData unit)
        {
            unit.Descriptor.Damage = unit.Descriptor.Stats.HitPoints.ModifiedValue + unit.Descriptor.Stats.TemporaryHitPoints.ModifiedValue;
        }
        public static void ForceKill(UnitEntityData unit)
        {
            unit.Descriptor.State.ForceKill = true;
        }
        public static void ResurrectAndFullRestore(UnitEntityData unit)
        {
            unit.Descriptor.ResurrectAndFullRestore();
        }
        public static void Buff(UnitEntityData unit, string buffGuid)
        {
            unit.Descriptor.AddFact((BlueprintUnitFact)Utilities.GetBlueprintByGuid<BlueprintBuff>(buffGuid), (MechanicsContext)null, new FeatureParam());
        }
        public static void Charm(UnitEntityData unit)
        {
            if (unit != null)
            {
                unit.Descriptor.SwitchFactions(Game.Instance.BlueprintRoot.PlayerFaction, true);
            }
            else
            {
                Logger.ModLoggerDebug("Unit is null!");
            }
        }
        public static void AddToParty(UnitEntityData unit)
        {
            Charm(unit);
            Game.Instance.Player.AddCompanion(unit);
        }

        public static void AddCompanion(UnitEntityData unit)
        {
            GameModeType currentMode = Game.Instance.CurrentMode;
            Game.Instance.Player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause)
            {
                var pets = unit.Pets;
                unit.IsInGame = true;
                unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                foreach (var pet in pets)
                {
                    pet.Entity.Position = unit.Position;
                }
            }
        }

        public static void RemoveCompanion(UnitEntityData unit)
        {
            GameModeType currentMode = Game.Instance.CurrentMode;
            Game.Instance.Player.RemoveCompanion(unit);
        }
    }
}
