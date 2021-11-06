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
using Kingmaker.ElementsSystem;
using ModKit;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Items;

namespace ToyBox {
    public enum UnitSelectType {
        Off,
        You,
        Party,
        Friendly,
        Enemies,
        Everyone,
    }

    public static class UnitEntityDataUtils {
        public static float GetMaxSpeed(List<UnitEntityData> data) => data.Select(u => u.ModifiedSpeedMps).Max();

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

        public static void Kill(UnitEntityData unit) => unit.Descriptor.Damage = unit.Descriptor.Stats.HitPoints.ModifiedValue +
                                     unit.Descriptor.Stats.TemporaryHitPoints.ModifiedValue;

        public static void ForceKill(UnitEntityData unit) => unit.Descriptor.State.ForceKill = true;

        public static void ResurrectAndFullRestore(UnitEntityData unit) => unit.Descriptor.ResurrectAndFullRestore();

        public static void Buff(UnitEntityData unit, string buffGuid) => unit.Descriptor.AddFact((BlueprintUnitFact)Utilities.GetBlueprintByGuid<BlueprintBuff>(buffGuid),
                (MechanicsContext)null, new FeatureParam());

        public static void Charm(UnitEntityData unit) {
            if (unit != null)
                unit.Descriptor.SwitchFactions(Game.Instance.BlueprintRoot.PlayerFaction, true);
            else
                Mod.Warn("Unit is null!");
        }

        public static void AddToParty(UnitEntityData unit) {
            Charm(unit);
            Game.Instance.Player.AddCompanion(unit);
        }
#if true
        public static void AddCompanion(UnitEntityData unit) {
            var currentMode = Game.Instance.CurrentMode;
            Game.Instance.Player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                var pets = unit.Pets;
                unit.IsInGame = true;
                unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                unit.LeaveCombat();
                Charm(unit);
                var unitPartCompanion = unit.Get<UnitPartCompanion>();
                unitPartCompanion.State = CompanionState.InParty;
                if (unit.IsDetached) {
                    Game.Instance.Player.AttachPartyMember(unit);
                }
                foreach (var pet in pets) {
                    pet.Entity.Position = unit.Position;
                }
            }
        }
        public static void RecruitCompanion(UnitEntityData unit) {
            var currentMode = Game.Instance.CurrentMode;
            unit = Game.Instance.EntityCreator.RecruitNPC(unit, unit.Blueprint);
            // this line worries me but the dev said I should do it
            //unit.HoldingState.RemoveEntityData(unit);  
            //player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                var pets = unit.Pets;
                unit.IsInGame = true;
                unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                unit.LeaveCombat();
                Charm(unit);
                unit.SwitchFactions(Game.Instance.Player.MainCharacter.Value.Faction);
                //unit.GroupId = Game.Instance.Player.MainCharacter.Value.GroupId;
                //Game.Instance.Player.CrossSceneState.AddEntityData(unit);
                if (unit.IsDetached) {
                    Game.Instance.Player.AttachPartyMember(unit);
                }
                foreach (var pet in pets) {
                    pet.Entity.Position = unit.Position;
                }
            }
        }
#else
note this code from Owlcat 
  private static void RecruitCompanion(string parameters)
    {
      string paramString = Utilities.GetParamString(parameters, 1, (string) null);
      bool? paramBool = Utilities.GetParamBool(parameters, 2, (string) null);
      BlueprintUnit blueprint = Utilities.GetBlueprint<BlueprintUnit>(paramString);
      if (blueprint == null)
        PFLog.SmartConsole.Log("Cant get companion with name '" + paramString + "'", (object[]) Array.Empty<object>());
      else if (!paramBool.HasValue || paramBool.Value)
      {
        UnitEntityData unitVacuum = Game.Instance.CreateUnitVacuum(blueprint);
        Game.Instance.State.PlayerState.CrossSceneState.AddEntityData((EntityDataBase) unitVacuum);
        unitVacuum.IsInGame = false;
        unitVacuum.Ensure<UnitPartCompanion>().SetState(CompanionState.ExCompanion);
      }
      else
      {
        Vector3 position = Game.Instance.Player.MainCharacter.Value.Position;
        SceneEntitiesState crossSceneState = Game.Instance.State.PlayerState.CrossSceneState;
        UnitEntityData unit = Game.Instance.EntityCreator.SpawnUnit(blueprint, position, Quaternion.identity, crossSceneState);
        Game.Instance.Player.AddCompanion(unit);
        EventBus.RaiseEvent<IPartyHandler>((Action<IPartyHandler>) (h => h.HandleAddCompanion(unit)));
      }
    }


        public static void AddCompanion(UnitEntityData unit) {
            Player player = Game.Instance.Player;
            player.AddCompanion(unit);
            GameModeType currentMode = Game.Instance.CurrentMode;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                var pets = unit.Pets;
                unit.IsInGame = true;
                unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                unit.LeaveCombat();
                Charm(unit);
                UnitPartCompanion unitPartCompanion = unit.Get<UnitPartCompanion>();
                unit.Ensure<UnitPartCompanion>().SetState(CompanionState.InParty);
                unit.SwitchFactions(Game.Instance.Player.MainCharacter.Value.Faction);
                unit.GroupId = Game.Instance.Player.MainCharacter.Value.GroupId;
                unit.HoldingState.RemoveEntityData(unit);
                Game.Instance.Player.CrossSceneState.AddEntityData(unit);
                foreach (var pet in pets) {
                    pet.Entity.Position = unit.Position;
                }
            }
        }
#endif

        public static void RemoveCompanion(UnitEntityData unit) {
            _ = Game.Instance.CurrentMode;
            Game.Instance.Player.RemoveCompanion(unit);
        }

        public static bool IsPartyOrPet(this UnitDescriptor unit) {
            if (unit?.Unit?.OriginalBlueprint == null || Game.Instance.Player?.AllCharacters == null || Game.Instance.Player?.AllCharacters.Count == 0) {
                return false;
            }

            return Game.Instance.Player.AllCharacters
                .Any(x => x.OriginalBlueprint == unit.Unit.OriginalBlueprint && (x.Master == null || x.Master.OriginalBlueprint == null ||
                    Game.Instance.Player.AllCharacters.Any(y => y.OriginalBlueprint == x.Master.OriginalBlueprint)));
        }

        public static bool TryGetPartyMemberForLevelUpVersion(this UnitDescriptor levelUpUnit, out UnitEntityData ch) {
            ch = Game.Instance?.Player?.AllCharacters.Find(c => c.CharacterName == levelUpUnit.CharacterName);
            return ch != null;
        }
        
        public static bool TryGetClass(this UnitEntityData unit, BlueprintCharacterClass cl, out ClassData cd) {
            cd = unit.Progression.Classes.Find(c => c.CharacterClass == cl);
            return cd != null;
        } 
    }
}
