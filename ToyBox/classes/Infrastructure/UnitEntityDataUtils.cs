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
#if Wrath
using Kingmaker.Blueprints.Classes.Selection;
#endif

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
        public static float GetMaxSpeed(List<UnitEntityData> data) => GetMaxSpeed(data);
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
                    if (!unitEntityData.IsPlayerFaction && unitEntityData.IsPlayerFaction()) {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

#if Wrath
        public static void Kill(UnitEntityData unit) => unit.Descriptor.Damage = unit.Descriptor.Stats.HitPoints.ModifiedValue + unit.Descriptor.Stats.TemporaryHitPoints.ModifiedValue;
                   
        public static void ForceKill(UnitEntityData unit) => unit.Descriptor.State.ForceKill = true;

        public static void ResurrectAndFullRestore(UnitEntityData unit) => unit.Descriptor.ResurrectAndFullRestore();

        public static void Buff(UnitEntityData unit, string buffGuid) => unit.Descriptor.AddFact((BlueprintUnitFact)Utilities.GetBlueprintByGuid<BlueprintBuff>(buffGuid), (MechanicsContext)null, new FeatureParam());

        public static void Charm(UnitEntityData unit) {
            if (unit != null)
                unit.Descriptor.SwitchFactions(Game.Instance.BlueprintRoot.PlayerFaction, true);
            else
                Mod.Warn("Unit is null!");
        }
#elif RT
        public static void Kill(UnitEntityData unit) => unit.Health.Damage = unit.Stats.GetStat(StatType.HitPoints) + unit.Stats.GetStat(StatType.TemporaryHitPoints);

        public static void Charm(UnitEntityData unit) {
            if (unit != null) {
                // TODO: can we still do this?
                // unit.SetFaction() = Game.Instance.BlueprintRoot.PlayerFaction;
            }
            else
                Mod.Warn("Unit is null!");
        }
#endif
        public static void AddToParty(UnitEntityData unit) {
            Charm(unit);
            Game.Instance.Player.AddCompanion(unit);
        }
        public static void AddCompanion(UnitEntityData unit) {
            var currentMode = Game.Instance.CurrentMode;
            Game.Instance.Player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
#if Wrath
                var pets = unit.Pets;
#endif
                unit.IsInGame = true;
#if Wrath
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
#elif RT
                unit.Position = Game.Instance.Player.MainCharacter.Entity.Position;
                unit.CombatState.LeaveCombat();
                Charm(unit);
                var unitPartCompanion = unit.GetAll<UnitPartCompanion>();
                Game.Instance.Player.AddCompanion(unit);
                if (unit.IsDetached) {
                    Game.Instance.Player.AttachPartyMember(unit);
                }
#endif
            }
        }
        public static void RecruitCompanion(UnitEntityData unit) {
            var currentMode = Game.Instance.CurrentMode;
            unit = GameHelper.RecruitNPC(unit, unit.Blueprint);
            // this line worries me but the dev said I should do it
            //unit.HoldingState.RemoveEntityData(unit);  
            //player.AddCompanion(unit);
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
#if Wrath
                var pets = unit.Pets;
#elif RT
                var pets = Game.Instance.Player.PartyAndPets.Where(u => u.IsPet && u.OwnerEntity == unit);
#endif
                unit.IsInGame = true;
                unit.Position = Shodan.MainCharacter.Position;
#if Wrath
                unit.LeaveCombat();
#elif RT
                unit.CombatState.LeaveCombat();
#endif
                Charm(unit);
#if Wrath
                unit.SwitchFactions(Shodan.MainCharacter.Faction);
#endif
                //unit.GroupId = Game.Instance.Player.MainCharacter.Value.GroupId;
                //Game.Instance.Player.CrossSceneState.AddEntityData(unit);
                if (unit.IsDetached) {
                    Game.Instance.Player.AttachPartyMember(unit);
                }
                foreach (var pet in pets) {
                    pet
#if Wrath
                            .Entity!
#endif
                            .Position = unit.Position;
                }
            }
        }
        public static bool IsPartyOrPet(this UnitDescriptor unit) {
            if (unit?
#if Wrath
                    .Unit?
#endif
                    .OriginalBlueprint == null
                || Game.Instance.Player?.AllCharacters == null
                || Game.Instance.Player?.AllCharacters.Count == 0) {
                return false;
            }

            return Game.Instance.Player.AllCharacters
                       .Any(x => x.OriginalBlueprint                                 == unit
#if Wrath
                                                        .Unit
#endif
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
