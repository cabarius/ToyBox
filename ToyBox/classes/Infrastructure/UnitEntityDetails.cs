// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT Licenseusing Kingmaker;

using Kingmaker;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Cheats;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Parts;
using System.Collections.Generic;
using System.Linq;

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
            return data.Select(u => u.ModifiedSpeedMps).Max();
        }

        public static bool CheckUnitEntityData(UnitEntityData unitEntityData, UnitSelectType selectType) {
            if (unitEntityData == null) return false;

            return selectType switch {
                UnitSelectType.Everyone => true,
                UnitSelectType.Party => unitEntityData.IsPlayerFaction,
                UnitSelectType.You => unitEntityData.IsMainCharacter,
                UnitSelectType.Friendly => !unitEntityData.IsEnemy(GameHelper.GetPlayerCharacter()),
                UnitSelectType.Enemies => !unitEntityData.IsPlayerFaction && unitEntityData.Descriptor.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction),
                var _ => false
            };
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
            unit.Descriptor.AddFact(Utilities.GetBlueprintByGuid<BlueprintBuff>(buffGuid), null, new FeatureParam());
        }
        public static void Charm(UnitEntityData unit)
        {
            if (unit != null) {
                 unit.Descriptor.SwitchFactions(Game.Instance.BlueprintRoot.PlayerFaction, true);
            }
            else
            {
                Main.Debug("Unit is null!");
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
                unit.LeaveCombat();
                Charm(unit);
                UnitPartCompanion unitPartCompanion = unit.Get<UnitPartCompanion>();
                unitPartCompanion.State = CompanionState.InParty;
                if (unit.IsDetached) {
                    Game.Instance.Player.AttachPartyMember(unit);
                }
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
