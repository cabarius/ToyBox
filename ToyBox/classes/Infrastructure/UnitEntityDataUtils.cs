// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT Licenseusing Kingmaker;

using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Cheats;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.Parts;

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

    public static class UnitEntityDataUtils {
        public static float GetMaxSpeed(List<UnitEntityData> data) {
            return (data.Select((u => u.ModifiedSpeedMps)).Max());
        }

        public static bool CheckUnitEntityData(UnitEntityData unitEntityData, UnitSelectType selectType) {
            if (unitEntityData == null) return false;
            switch (selectType) {
                case UnitSelectType.Everyone:
                    return true;
                case UnitSelectType.Party:
                    if (unitEntityData.IsPlayerFaction) {
                        return true;
                    }

                    return false;
                case UnitSelectType.You:
                    if (unitEntityData.IsMainCharacter) {
                        return true;
                    }

                    return false;
                case UnitSelectType.Friendly:
                    return !unitEntityData.IsEnemy(GameHelper.GetPlayerCharacter());
                case UnitSelectType.Enemies:
                    // TODO - should this be IsEnemy instead?
                    if (!unitEntityData.IsPlayerFaction &&
                        unitEntityData.Descriptor.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction)) {
                        return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        public static void Kill(UnitEntityData unit) {
            unit.Descriptor.Damage = unit.Descriptor.Stats.HitPoints.ModifiedValue +
                                     unit.Descriptor.Stats.TemporaryHitPoints.ModifiedValue;
        }

        public static void ForceKill(UnitEntityData unit) {
            unit.Descriptor.State.ForceKill = true;
        }

        public static void ResurrectAndFullRestore(UnitEntityData unit) {
            unit.Descriptor.ResurrectAndFullRestore();
        }

        public static void Buff(UnitEntityData unit, string buffGuid) {
            unit.Descriptor.AddFact((BlueprintUnitFact)Utilities.GetBlueprintByGuid<BlueprintBuff>(buffGuid),
                (MechanicsContext)null, new FeatureParam());
        }

        public static void Charm(UnitEntityData unit) {
            if (unit != null) {
                unit.Descriptor.SwitchFactions(Game.Instance.BlueprintRoot.PlayerFaction, true);
            }
            else {
                Main.Debug("Unit is null!");
            }
        }

        public static void AddToParty(UnitEntityData unit) {
            Charm(unit);
            Game.Instance.Player.AddCompanion(unit);
        }

        public static void AddCompanion(UnitEntityData unit) {
            GameModeType currentMode = Game.Instance.CurrentMode;
            Game.Instance.Player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
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

                foreach (var pet in pets) {
                    pet.Entity.Position = unit.Position;
                }
            }
        }

        public static void RemoveCompanion(UnitEntityData unit) {
            GameModeType currentMode = Game.Instance.CurrentMode;
            Game.Instance.Player.RemoveCompanion(unit);
        }

        public static bool IsPartyOrPet(this UnitDescriptor unit) {
            return Game.Instance.Player.AllCharacters
                .Any(x => x.OriginalBlueprint == unit.Unit.OriginalBlueprint && (x.Master == null ||
                    Game.Instance.Player.AllCharacters.Any(y => y.OriginalBlueprint == x.Master.OriginalBlueprint)));
        }
    }
}
