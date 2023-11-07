// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT Licenseusing Kingmaker;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
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
using Kingmaker.Controllers.Combat;
using Utilities = Kingmaker.Cheats.Utilities;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints;
using ModKit.Utility;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.UI.Common;
using UnityEngine;
using System;

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
        public static Settings settings => Main.Settings;
        public static float GetMaxSpeed(List<UnitEntityData> data) => Shodan.GetMaxSpeed(data);
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
                    return !unitEntityData.IsEnemy();
                case UnitSelectType.Enemies:
                    return unitEntityData.IsEnemy();
                default:
                    return false;
            }
        }

        public static void Kill(UnitEntityData unit) => unit.Health.Damage = unit.Stats.GetStat(StatType.HitPoints) + unit.Stats.GetStat(StatType.TemporaryHitPoints);

        public static void Charm(UnitEntityData unit) {
            if (unit != null) {
                // TODO: can we still do this?
                // unit.SetFaction() = Game.Instance.BlueprintRoot.PlayerFaction;
            }
            else
                Mod.Warn("Unit is null!");
        }
        public static void AddToParty(UnitEntityData unit) {
            Charm(unit);
            Game.Instance.Player.AddCompanion(unit);
        }
        public static void AddCompanion(UnitEntityData unit) {
            var currentMode = Game.Instance.CurrentMode;
            Game.Instance.Player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                unit.IsInGame = true;
                unit.Position = Game.Instance.Player.MainCharacter.Entity.Position;
                unit.CombatState.LeaveCombat();
                Charm(unit);
                var unitPartCompanion = unit.GetAll<UnitPartCompanion>();
                Game.Instance.Player.AddCompanion(unit);
                if (unit.IsDetached) {
                    Game.Instance.Player.AttachPartyMember(unit);
                }
            }
        }
        public static void RecruitCompanion(UnitEntityData unit) {
            var currentMode = Game.Instance.CurrentMode;
            unit = GameHelper.RecruitNPC(unit, unit.Blueprint);
            // this line worries me but the dev said I should do it
            //unit.HoldingState.RemoveEntityData(unit);  
            //player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                var pets = Game.Instance.Player.PartyAndPets.Where(u => u.IsPet && u.OwnerEntity == unit);
                unit.IsInGame = true;
                unit.Position = Shodan.MainCharacter.Position;
                unit.CombatState.LeaveCombat();
                Charm(unit);
                //unit.GroupId = Game.Instance.Player.MainCharacter.Value.GroupId;
                //Game.Instance.Player.CrossSceneState.AddEntityData(unit);
                if (unit.IsDetached) {
                    Game.Instance.Player.AttachPartyMember(unit);
                }
                foreach (var pet in pets) {
                    pet
                            .Position = unit.Position;
                }
            }
        }
        public static bool IsPartyOrPet(this UnitDescriptor unit) {
            if (unit?
                    .OriginalBlueprint == null
                || Game.Instance.Player?.AllCharacters == null
                || Game.Instance.Player?.AllCharacters.Count == 0) {
                return false;
            }

            return Game.Instance.Player.AllCharacters
                       .Any(x => x.OriginalBlueprint == unit
                                     .OriginalBlueprint
                                 && (x.Master == null
                                     || x.Master.OriginalBlueprint == null
                                     || Game.Instance.Player.AllCharacters.Any(
                                         y => y.OriginalBlueprint == x.Master.OriginalBlueprint)
                                    )
                           );
        }

        public static void RemoveCompanion(UnitEntityData unit) {
            _ = Game.Instance.CurrentMode;
            Game.Instance.Player.RemoveCompanion(unit);
        }
#if false
        public static string GetEquipmentRestrictionCaption(this EquipmentRestriction restriction) {
            switch (restriction) {
                case EquipmentRestrictionAlignment era: return $"Alignment must be {era.Alignment}";
                case EquipmentRestrictionCannotEquip: return "Unequipable";
                case EquipmentRestrictionClass erc: return $"Class must be {(erc.Not ? "not " : "")}{erc.Class}";
                case EquipmentRestrictionHasAnyClassFromList erhacfl: 
                    return $"Class must {(erhacfl.Not ? "not " : "")}be in {string.Join(", ", erhacfl.Classes.Select(c => c.GetDisplayName()))}";
                case EquipmentRestrictionMainPlayer ermp: return "Must be Main Character";
                case EquipmentRestrictionSpecialUnit ersu: return $"Must have blueprint {ersu.Blueprint.name}";
                case EquipmentRestrictionStat ers: return $"Stat: {ers.Stat} > {ers.MinValue}";
                default: return restriction.GetType().Name;
            }
        }

        public static string GetCantEquipReasonText(this ItemEntity item) {
            var unit = Game.Instance.SelectionCharacter.CurrentSelectedCharacter;
            if (unit == null)
                unit = Game.Instance.Player.MainCharacter;
            switch (item) {
                case ItemEntityWeapon _:
                case ItemEntityArmor _:
                case ItemEntityShield _:
                    if (item.Owner == null) {
                        if (!item.CanBeEquippedBy(unit.Descriptor)) {
                            string restrictionText = "";
                            var restrictions = item.Blueprint.GetComponents<EquipmentRestriction>();
                            foreach (var restriction in restrictions) {
                                var text = restriction.GetEquipmentRestrictionCaption();
                                if (restriction.CanBeEquippedBy(unit))
                                    continue;
                                else {
                                    text = text.color(RGBA.red).Bold().sizePercent(75);
                                }
                                restrictionText += $"{unit.CharacterName}: {text}\n";
                            }
                            return restrictionText;
                        }
                    }
                    break;
                default:
                    if (!(item.Blueprint is BlueprintItemEquipment) || item is ItemEntityUsable) {
                        if (item is ItemEntityUsable) {
                            ItemEntityUsable itemEntityUsable = item as ItemEntityUsable;
                            var blueprint = itemEntityUsable.Blueprint;
                            if (UIUtilityItem.IsItemAbilityInSpellListOfUnit(blueprint, unit.Descriptor))
                                return $"{unit.CharacterName}: Already has ability {blueprint.GetDisplayName()}".color(RGBA.red).Bold().sizePercent(75); ;
                            if (itemEntityUsable.Blueprint.Ability == null)
                                return $"blueprint {blueprint.GetDisplayName()} has no ability".color(RGBA.red).Bold().sizePercent(75); ;
                            if (itemEntityUsable.Blueprint.Type == UsableItemType.Potion 
                                    || itemEntityUsable.Blueprint.Type == UsableItemType.Other
                                    )
                                break;
                            int? useMagicDeviceDc = itemEntityUsable.GetUseMagicDeviceDC(unit);
                            if (useMagicDeviceDc.HasValue)
                                return $"Needs Use Magic Device >= {useMagicDeviceDc.Value}".color(RGBA.red).Bold().sizePercent(75);
                            if (!itemEntityUsable.Blueprint.IsUnitNeedUMDForUse(unit.Descriptor))
                                return $"Bugged??? {blueprint.name} - IsUnitNeedUMDForUse - {blueprint.RequireUMDIfCasterHasNoSpellInSpellList}".color(RGBA.red).Bold().sizePercent(75);
                            if (!unit.Descriptor.HasUMDSkill)
                                return $"{unit.CharacterName}: Needs Use Magic Device".color(RGBA.red).Bold().sizePercent(75);
                        }
                        break;

                    }
                    break;
            }
            return null;
        }
#endif
    }
}
