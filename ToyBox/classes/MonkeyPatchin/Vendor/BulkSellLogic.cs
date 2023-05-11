using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root;
using Kingmaker.Craft;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.Designers;
using Kingmaker.Enums.Damage;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.UI.MVVM._VM.Vendor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.EntitySystem.Stats;
using ToyBox.classes.Models;

namespace ToyBox {

    static class BulkSellLogic {

        struct CountArgs {
            public int itemInventoryCount;
            public int itemStackCount;
            public BulkSellSettings settings;
        }

        public static int canBulkSellCount(ItemEntity item) {
            if (item == null || item.Blueprint.IsNotable) {
                return 0;
            }

            var args = new CountArgs {
                itemInventoryCount = Game.Instance.Player.Inventory.Count(item.Blueprint),
                itemStackCount = item.Count,
                settings = Main.Settings.bulkSellSettings
            };

            var entityToSell = item switch {
                ItemEntityWeapon weapon => ShouldSellHowManyOfThisWeapon(weapon, args),
                ItemEntityArmor armor => ShouldSellHowManyOfThisArmor(armor, args),
                ItemEntityShield shield => ShouldSellHowManyOfThisShield(shield, args),
                ItemEntityUsable usable => ShouldSellHowManyOfThisConsumable(usable, args),
                _ => 0
            };

            if (entityToSell > 0) {
                return entityToSell;
            }

            var blueprintToSell = item.Blueprint switch {
                BlueprintIngredient _ => ShouldSellHowManyOfThisIngredient(args),
                BlueprintItemEquipmentBelt belt => ShouldSellHowManyOfThisBelt(belt, args),
                BlueprintItemEquipmentHead hat => ShouldSellHowManyOfThisHat(hat, args),
                BlueprintItemEquipmentRing ring => ShouldSellHowManyOfThisRing(ring, args),
                BlueprintItemEquipmentShoulders cloak => ShouldSellHowManyOfThisCloak(cloak, args),
                BlueprintItemEquipmentWrist bracer => ShouldSellHowManyOfThisBracer(bracer, args),
                BlueprintItemEquipmentNeck necklace => ShouldSellHowManyOfThisNecklace(necklace, args),
                _ => 0
            };

            return blueprintToSell;
        }

        private static int ShouldSellHowManyOfThisWeapon(ItemEntityWeapon weapon, CountArgs args) {

            bool shouldSellMasterwork =
                    Game.Instance.Player.UISettings.OptionsDictionary[VendorHelper.SaleOptions.MasterWork];


            if (!shouldSellMasterwork && weapon.Blueprint.IsMasterwork)
                return 0;

            string defName = weapon.Blueprint.DefaultNonIdentifiedName ?? "";
            string preEncNames = weapon.Blueprint.GetEnchantmentPrefixes() ?? "";
            string postEncNames = weapon.Blueprint.GetEnchantmentSuffixes() ?? "";
            string materialName = weapon.Blueprint.DamageType.Physical.Material == 0
                ? ""
                : LocalizedTexts.Instance.DamageMaterial.GetText(weapon.Blueprint.DamageType.Physical.Material) ?? "";

            var hasUniqueName = !weapon.Name.Contains(defName) || !weapon.Name.Contains(preEncNames) ||
                                !weapon.Name.Contains(postEncNames) || !weapon.Name.Contains(materialName);

            if (!args.settings.sellUniqueWeapons && hasUniqueName) {
                return 0;
            }


            var nonPhysicalDamageMatchesSettings = weapon.Blueprint?.Enchantments?.All(e => {
                var energy = e.GetComponent<WeaponEnergyDamageDice>();
                var reality = e.GetComponent<WeaponReality>();
                var alignment = e.GetComponent<WeaponAlignment>();
                var matchesEnergy = energy == null || args.settings.damageEnergy[energy.Element];
                var matchesReality = reality == null || args.settings.damageReality[reality.Reality];
                var matchesAlignment = alignment == null || args.settings.damageAlignment[alignment.Alignment];
                return matchesEnergy && matchesReality && matchesAlignment;
            }) ?? true;

            var physicalDamage = Enum.GetValues(typeof(PhysicalDamageMaterial)) as IEnumerable<PhysicalDamageMaterial>;

            var physicalDamageMatchesSettings = physicalDamage.All(type => {
                var weaponTypeNumeric = (uint)weapon.Blueprint.DamageType.Physical.Material;
                var currentTypeNumeric = (uint)type;
                //This may be "untyped" or just regular physical damage. Either way, we want to ignore values outside of the enum
                //This line may never be executed, but it's there in the original mod logic, so I included it
                if ((weaponTypeNumeric & currentTypeNumeric) == 0u) {
                    return true;
                }

                return args.settings.damageMaterial[type];
            });

            var enhancementBonus = GameHelper.GetWeaponEnhancementBonus(weapon.Blueprint);

            var allTypesMatch = nonPhysicalDamageMatchesSettings && physicalDamageMatchesSettings;
            var enhancementBonusIsSellable = enhancementBonus <= args.settings.weaponEnchantLevel;
            var stackSizeIsBiggerThanMinimum = args.itemInventoryCount > args.settings.weaponStackSize;

            if (allTypesMatch && enhancementBonusIsSellable && stackSizeIsBiggerThanMinimum) {
                return Mathf.Min(args.itemInventoryCount - args.settings.weaponStackSize, args.itemStackCount);
            }

            return 0;
        }

        private static int ShouldSellHowManyOfThisArmor(ItemEntityArmor armor, CountArgs args) {
            var hasUniqueName = !armor.Name.Contains("+");

            if (!args.settings.sellUniqueArmors && hasUniqueName) {
                return 0;
            }

            var enhancementBonus = GameHelper.GetArmorEnhancementBonus(armor.Blueprint);
            var enhancementBonusIsSellable = enhancementBonus <= (args.settings.armorEnchantLevel);
            var stackSizeIsBiggerThanMinimum = args.itemInventoryCount > args.settings.armorStackSize;

            if (enhancementBonusIsSellable && stackSizeIsBiggerThanMinimum) {
                return Mathf.Min(args.itemInventoryCount - args.settings.armorStackSize, args.itemStackCount);
            }

            return 0;
        }

        private static int ShouldSellHowManyOfThisShield(ItemEntityShield shield, CountArgs args) {
            var hasUniqueName = !shield.Name.Contains("+");

            if (!args.settings.sellUniqueShields && hasUniqueName) {
                return 0;
            }

            var enhancementBonus = GameHelper.GetArmorEnhancementBonus(shield.ArmorComponent.Blueprint);
            var enhancementBonusIsSellable = enhancementBonus <= args.settings.shieldEnchantLevel;
            var stackSizeIsBiggerThanMinimum = args.itemInventoryCount > args.settings.shieldStackSize;
            if (enhancementBonusIsSellable && stackSizeIsBiggerThanMinimum)
                return Mathf.Min(args.itemInventoryCount - Main.Settings.bulkSellSettings.shieldStackSize, args.itemStackCount);

            return 0;
        }

        private static int ShouldSellHowManyOfThisConsumable(ItemEntityUsable usable, CountArgs args) {
            var shouldSellScrolls = args.itemInventoryCount > args.settings.scrollStackSize && args.settings.sellScrolls;
            var scrollsToSell = Mathf.Min(args.itemInventoryCount - Main.Settings.bulkSellSettings.scrollStackSize, args.itemStackCount);
            var shouldSellPotions = args.itemInventoryCount > args.settings.potionStackSize && args.settings.sellPotions;
            var potionsToSell = Mathf.Min(args.itemInventoryCount - Main.Settings.bulkSellSettings.potionStackSize, args.itemStackCount);

            return usable.Blueprint.Type switch {
                UsableItemType.Scroll => shouldSellScrolls ? scrollsToSell : 0,
                UsableItemType.Potion => shouldSellPotions ? potionsToSell : 0,
                _ => 0
            };
        }

        private static int ShouldSellHowManyOfThisIngredient(CountArgs args) {
            if (args.itemInventoryCount > args.settings.ingredientStackSize && args.settings.sellIngredients)
                return Mathf.Min(args.itemInventoryCount - args.settings.ingredientStackSize, args.itemStackCount);

            return 0;
        }

        private static int ShouldSellHowManyOfThisBelt(BlueprintItemEquipmentBelt belt, CountArgs args) {
            if (belt.Enchantments.Count > 0 && belt.Enchantments.TrueForAll(e =>
                    e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                    isAttribute(sb.Stat) && sb.Descriptor == ModifierDescriptor.Enhancement &&
                    args.settings.maxAttributeBonusForBelt >= sb.Value)) {
                return Mathf.Min(args.itemInventoryCount - args.settings.beltStackSize, args.itemStackCount);
            }
            return 0;
        }

        private static int ShouldSellHowManyOfThisHat(BlueprintItemEquipmentHead hat, CountArgs args) {
            if (hat.Enchantments.Count == 1 &&
                    hat.Enchantments.TrueForAll(e =>
                    e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                    isAttribute(sb.Stat) && sb.Descriptor == ModifierDescriptor.Enhancement &&
                    args.settings.maxAttributeBonusForHead >= sb.Value)) {
                return Mathf.Min(args.itemInventoryCount - args.settings.headStackSize, args.itemStackCount);
            }
            return 0;
        }

        private static int ShouldSellHowManyOfThisRing(BlueprintItemEquipmentRing ring, CountArgs args) {
            if (ring.Enchantments.Count == 1 &&
                    ring.Enchantments.TrueForAll(e =>
                    e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                    sb.Descriptor == ModifierDescriptor.Deflection && isAC(sb.Stat) &&
                    args.settings.maxACBonusForRings >= sb.Value)) {
                return Mathf.Min(args.itemInventoryCount - args.settings.ringStackSize, args.itemStackCount);
            }
            return 0;
        }

        private static int ShouldSellHowManyOfThisCloak(BlueprintItemEquipmentShoulders cloak, CountArgs args) {
            if (cloak.Enchantments.Count == 1 &&
                    cloak.Enchantments.TrueForAll(e =>
                    e.GetComponent<AllSavesBonusEquipment>() is AllSavesBonusEquipment sb &&
                    sb.Descriptor == ModifierDescriptor.Resistance &&
                    args.settings.maxSaveBonusForCloaks >= sb.Value)) {
                return Mathf.Min(args.itemInventoryCount - args.settings.cloakStackSize, args.itemStackCount);
            }
            return 0;
        }

        private static int ShouldSellHowManyOfThisBracer(BlueprintItemEquipmentWrist bracer, CountArgs args) {
            if (isBracersOfArmor(bracer, args.settings.maxACBonusForBracers) || isBracersOfLesserArchery(bracer, args.settings.maxACBonusForBracers)) {
                return Mathf.Min(args.itemInventoryCount - args.settings.bracerStackSize, args.itemStackCount);

            }
            return 0;
        }

        private static int ShouldSellHowManyOfThisNecklace(BlueprintItemEquipmentNeck necklace, CountArgs args) {
            if ((necklace.Enchantments.Count == 1 &&
                    necklace.Enchantments.TrueForAll(e =>
                        isNaturalArmorEnc(e, args.settings.maxACBonusForNeck) ||
                        isMightyFistsEnc(e, args.settings.maxACBonusForNeck))) ||
                        isAgileFistsEnc(necklace, args.settings.maxACBonusForNeck)) {
                return Mathf.Min(args.itemInventoryCount - args.settings.neckStackSize, args.itemStackCount);
            }
            return 0;
        }

        static bool isBracersOfLesserArchery(BlueprintItemEquipmentWrist wrist, int maxEncLevel) {
            return wrist.Enchantments.Count == 1 &&
                    (wrist.Enchantments.TrueForAll(e =>
                      (e.GetComponent<AddUnitFeatureEquipment>() is AddUnitFeatureEquipment uf &&
                      uf.Feature.ComponentsArray.Length == 2 &&
                      uf.Feature.ComponentsArray.All(comp =>
                        (comp is WeaponGroupAttackBonus ab &&
                          ab.AttackBonus <= maxEncLevel && ab.WeaponGroup == WeaponFighterGroup.Bows) ||
                        (comp is AddFacts facts))))); // probably not necessary to check the facts...
        }

        static bool isBracersOfArmor(BlueprintItemEquipmentWrist wrist, int maxEncLevel) {
            return wrist.Enchantments.Count == 2 &&
                    (wrist.Enchantments.TrueForAll(e =>
                      (e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                        sb.Descriptor == ModifierDescriptor.Armor && isAC(sb.Stat) && maxEncLevel >= sb.Value) ||
                      (e.GetComponent<AddUnitFeatureEquipment>() is AddUnitFeatureEquipment uf && uf.Feature.GetComponent<ACBonusAgainstWeaponType>() != null)));
        }

        static bool isAgileFistsEnc(BlueprintItemEquipmentNeck bp, int maxEncLevel) {
            return (bp.Enchantments.Count == 2 &&
                    bp.Enchantments.TrueForAll(encBp =>
                      (encBp.GetComponent<EquipmentWeaponTypeDamageStatReplacement>() is EquipmentWeaponTypeDamageStatReplacement enc &&
                        enc.AllNaturalAndUnarmed && enc.Stat == StatType.Dexterity && enc.RequiresFinesse &&
                        enc.Category == WeaponCategory.UnarmedStrike) ||
                      (encBp.GetComponent<EquipmentWeaponTypeEnhancement>() is EquipmentWeaponTypeEnhancement enc2 &&
                        enc2.AllNaturalAndUnarmed && enc2.Category == WeaponCategory.UnarmedStrike && maxEncLevel >= enc2.Enhancement)));
        }

        static bool isNaturalArmorEnc(BlueprintItemEnchantment bp, int maxEncLevel) {
            return bp.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment enc &&
              enc.Descriptor == ModifierDescriptor.NaturalArmorEnhancement && maxEncLevel >= enc.Value;
        }

        static bool isMightyFistsEnc(BlueprintItemEnchantment bp, int maxEncLevel) {
            return bp.GetComponent<EquipmentWeaponTypeEnhancement>() is EquipmentWeaponTypeEnhancement enc &&
              enc.AllNaturalAndUnarmed && enc.Category == WeaponCategory.UnarmedStrike &&
              maxEncLevel >= enc.Enhancement;
        }

        static bool isAttribute(StatType stat) {
            return
              stat == StatType.Strength ||
              stat == StatType.Dexterity ||
              stat == StatType.Constitution ||
              stat == StatType.Intelligence ||
              stat == StatType.Wisdom ||
              stat == StatType.Charisma;
        }

        static bool isAC(StatType stat) {
            return stat == StatType.AC;
        }
    }

    [HarmonyPatch(typeof(VendorVM), nameof(VendorVM.TryMassSale))]
    internal class VendorVM_TryMassSalePatch {
        static bool Prefix(ref bool __result) {
            if (!Main.Settings.toggleCustomBulkSell) return true;

            var list = Game.Instance.Player.Inventory
                .Select(item => new {
                    item,
                    vanillaMassSell = VendorHelper.IsAppropriateForMassSelling(item),
                    numberToSell = BulkSellLogic.canBulkSellCount(item)
                })
                .Where(item => item.vanillaMassSell || item.numberToSell > 0)
                .Select(item => (new { item.item, quantity = item.vanillaMassSell ? -1 : item.numberToSell }))
                .ToList();

            if (list.Count <= 0) {
                __result = false;
            }

            list.ForEach(item => {
                Game.Instance.Vendor.AddForSell(item.item, item.quantity);
            });

            __result = true;

            return false;
        }
    }
}
