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
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ModKit;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.EntitySystem.Stats;

namespace ToyBox
{

    static class Logic
    {

        public static int canMassSellCount(ItemEntity item)
        {
            bool returnEarly = item == null || item.Blueprint.IsNotable;

            var inventory = Game.Instance.Player.Inventory;
            int itemInventoryCount = inventory.Count(item.Blueprint);
            int itemStackCount = item.Count;
            int itemsToSell = 0;
            if (!returnEarly)
            {
                if (item is ItemEntityWeapon weapon)
                {

                    bool sellMasterwork =
                      Game.Instance.Player.UISettings.OptionsDictionary[VendorHelper.SaleOptions.MasterWork];

                    if (!sellMasterwork && weapon.Blueprint.IsMasterwork)
                        return 0;

                    string defName = item.Blueprint.DefaultNonIdentifiedName ?? "";
                    Mod.Trace("defName: " + defName);
                    string preEncNames = weapon.Blueprint.GetEnchantmentPrefixes() ?? "";
                    Mod.Trace("preEncNames: " + preEncNames);
                    string postEncNames = weapon.Blueprint.GetEnchantmentSuffixes() ?? "";
                    Mod.Trace("postEncNames: " + postEncNames);
                    string materialName =
                      weapon.Blueprint.DamageType.Physical.Material == 0 ? "" :
                        LocalizedTexts.Instance.DamageMaterial.GetText(weapon.Blueprint.DamageType.Physical.Material) ?? "";
                    Mod.Trace("materialName: " + materialName);
                    var enhancementBonus = GameHelper.GetWeaponEnhancementBonus(weapon.Blueprint);
                    Mod.Trace("enhancementBonus: " + enhancementBonus);
                    var hasUniqueName = !item.Name.Contains(defName) || !item.Name.Contains(preEncNames) ||
                                        !item.Name.Contains(postEncNames) || !item.Name.Contains(materialName);
                    Mod.Trace("item name: " + item.Name + " has unique name? " + hasUniqueName);
                    if (!Main.Settings.bulkSellSettings.sellUniqueWeapons && hasUniqueName) return 0;
                    //var enhancementBonus = GameHelper.GetWeaponEnhancementBonus(weapon.Blueprint);
                    Mod.Trace("enc bonus: " + enhancementBonus);
                    bool allTypesMatch = true;
                    foreach (BlueprintItemEnchantment e in item.Blueprint.Enchantments)
                    {
                        var type = e.GetComponent<WeaponEnergyDamageDice>();
                        if (type != null)
                        {
                            allTypesMatch &= Main.Settings.bulkSellSettings.damageEnergy[type.Element];
                        }
                    }
                    Mod.Trace("WeaponEnergyDamageDice matches: " + allTypesMatch);
                    foreach (BlueprintItemEnchantment e in item.Blueprint.Enchantments)
                    {
                        var type = e.GetComponent<WeaponReality>();
                        if (type != null)
                        {
                            allTypesMatch &= Main.Settings.bulkSellSettings.damageReality[type.Reality];
                        }
                    }
                    Mod.Trace("WeaponReality matches: " + allTypesMatch);
                    foreach (BlueprintItemEnchantment e in item.Blueprint.Enchantments)
                    {
                        var type = e.GetComponent<WeaponAlignment>();
                        if (type != null)
                        {
                            allTypesMatch &= Main.Settings.bulkSellSettings.damageAlignment[type.Alignment];
                        }
                    }
                    Mod.Trace("WeaponAlignment matches: " + allTypesMatch);
                    foreach (PhysicalDamageMaterial type in Enum.GetValues(typeof(PhysicalDamageMaterial)))
                    {
                        if ((((uint)weapon.Blueprint.DamageType.Physical.Material) & ((uint)type)) != 0u)
                        {
                            allTypesMatch &= Main.Settings.bulkSellSettings.damageMaterial[type];
                        }
                    }
                    Mod.Trace("PhysicalDamageMaterial matches: " + allTypesMatch);
                    if (enhancementBonus <= Main.Settings.bulkSellSettings.weaponEnchantLevel && itemInventoryCount > Main.Settings.bulkSellSettings.weaponStackSize && allTypesMatch)
                    {
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.weaponStackSize, itemStackCount);
                    }
                    Mod.Trace("enhancementBonus matches: " + allTypesMatch);
                }
                else if (item is ItemEntityArmor armor)
                { // armors
                    var hasUniqueName = !armor.Name.Contains("+");
                    if (!Main.Settings.bulkSellSettings.sellUniqueArmors && hasUniqueName) return 0;
                    var enhancementBonus = GameHelper.GetArmorEnhancementBonus(armor.Blueprint);
                    if (enhancementBonus <= Main.Settings.bulkSellSettings.armorEnchantLevel && itemInventoryCount > Main.Settings.bulkSellSettings.armorStackSize)
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.armorStackSize, itemStackCount);
                }
                else if (item is ItemEntityShield shield)
                { // shields
                    var hasUniqueName = !shield.Name.Contains("+");
                    if (!Main.Settings.bulkSellSettings.sellUniqueShields && hasUniqueName) return 0;
                    var enhancementBonus = GameHelper.GetArmorEnhancementBonus(shield.ArmorComponent.Blueprint);
                    if (enhancementBonus <= Main.Settings.bulkSellSettings.shieldEnchantLevel && itemInventoryCount > Main.Settings.bulkSellSettings.shieldStackSize)
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.shieldStackSize, itemStackCount);
                }
                else if (item is ItemEntityUsable usable)
                {
                    switch (usable.Blueprint.Type)
                    {
                        case UsableItemType.Other:
                            break;
                        case UsableItemType.Wand:
                            break;
                        case UsableItemType.Scroll:
                            if (itemInventoryCount > Main.Settings.bulkSellSettings.scrollStackSize && Main.Settings.bulkSellSettings.sellScrolls)
                                itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.scrollStackSize, itemStackCount);
                            break;
                        case UsableItemType.Potion:
                            if (itemInventoryCount > Main.Settings.bulkSellSettings.potionStackSize && Main.Settings.bulkSellSettings.sellPotions)
                                itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.potionStackSize, itemStackCount);
                            break;
                    }
                }
                else if (item.Blueprint is BlueprintIngredient)
                {
                    if (itemInventoryCount > Main.Settings.bulkSellSettings.ingredientStackSize && Main.Settings.bulkSellSettings.sellIngredients)
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.ingredientStackSize, itemStackCount);
                }
                else if (item.Blueprint is BlueprintItemEquipmentBelt belt)
                { // use stat bonus for belts //
                    if (belt.Enchantments.Count > 0 && belt.Enchantments.TrueForAll(e =>
                        e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                        isAttribute(sb.Stat) && sb.Descriptor == ModifierDescriptor.Enhancement &&
                        Main.Settings.bulkSellSettings.maxAttributeBonusForBelt >= sb.Value))
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.beltStackSize, itemStackCount);
                    else itemsToSell = 0;
                }
                else if (item.Blueprint is BlueprintItemEquipmentHead head)
                { // use stat bonus for head //
                    if (head.Enchantments.Count == 1 &&
                      head.Enchantments.TrueForAll(e =>
                      e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                      isAttribute(sb.Stat) && sb.Descriptor == ModifierDescriptor.Enhancement &&
                      Main.Settings.bulkSellSettings.maxAttributeBonusForHead >= sb.Value))
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.headStackSize, itemStackCount);
                    else itemsToSell = 0;
                }
                else if (item.Blueprint is BlueprintItemEquipmentRing ring)
                { // use AC bonus for rings //
                    if (ring.Enchantments.Count == 1 &&
                      ring.Enchantments.TrueForAll(e =>
                      e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                      sb.Descriptor == ModifierDescriptor.Deflection && isAC(sb.Stat) &&
                      Main.Settings.bulkSellSettings.maxACBonusForRings >= sb.Value))
                    {
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.ringStackSize, itemStackCount);
                    }
                    else itemsToSell = 0;
                }
                else if (item.Blueprint is BlueprintItemEquipmentShoulders cloak)
                { // use save bonus for cloaks //
                    if (cloak.Enchantments.Count == 1 &&
                        cloak.Enchantments.TrueForAll(e =>
                        e.GetComponent<AllSavesBonusEquipment>() is AllSavesBonusEquipment sb &&
                        sb.Descriptor == ModifierDescriptor.Resistance &&
                        Main.Settings.bulkSellSettings.maxSaveBonusForCloaks >= sb.Value))
                    {
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.cloakStackSize, itemStackCount);
                    }
                    else itemsToSell = 0;
                }
                else if (item.Blueprint is BlueprintItemEquipmentWrist wrist)
                { // bracers of armor & lesser archery - wrists //
                    if (isBracersOfArmor(wrist, Main.Settings.bulkSellSettings.maxACBonusForBracers) ||
                      isBracersOfLesserArchery(wrist, Main.Settings.bulkSellSettings.maxACBonusForBracers))
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.bracerStackSize, itemStackCount);
                    else itemsToSell = 0;
                }
                else if (item.Blueprint is BlueprintItemEquipmentNeck neck)
                { // necklaces of natural armor & agile fists & mighty fists
                    if ((neck.Enchantments.Count == 1 &&
                        neck.Enchantments.TrueForAll(e =>
                          isNaturalArmorEnc(e, Main.Settings.bulkSellSettings.maxACBonusForNeck) || isMightyFistsEnc(e, Main.Settings.bulkSellSettings.maxACBonusForNeck))) ||
                        isAgileFistsEnc(neck, Main.Settings.bulkSellSettings.maxACBonusForNeck))
                        itemsToSell = Mathf.Min(itemInventoryCount - Main.Settings.bulkSellSettings.neckStackSize, itemStackCount);
                    else itemsToSell = 0;
                }
            }
            return itemsToSell;
        }


        static bool isBracersOfLesserArchery(BlueprintItemEquipmentWrist wrist, int maxEncLevel)
        {
            return wrist.Enchantments.Count == 1 &&
                    (wrist.Enchantments.TrueForAll(e =>
                      (e.GetComponent<AddUnitFeatureEquipment>() is AddUnitFeatureEquipment uf &&
                      uf.Feature.ComponentsArray.Length == 2 &&
                      uf.Feature.ComponentsArray.All(comp =>
                        (comp is WeaponGroupAttackBonus ab &&
                          ab.AttackBonus <= maxEncLevel && ab.WeaponGroup == WeaponFighterGroup.Bows) ||
                        (comp is AddFacts facts))))); // probably not necessary to check the facts...
        }

        static bool isBracersOfArmor(BlueprintItemEquipmentWrist wrist, int maxEncLevel)
        {
            return wrist.Enchantments.Count == 2 &&
                    (wrist.Enchantments.TrueForAll(e =>
                      (e.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment sb &&
                        sb.Descriptor == ModifierDescriptor.Armor && isAC(sb.Stat) && maxEncLevel >= sb.Value) ||
                      (e.GetComponent<AddUnitFeatureEquipment>() is AddUnitFeatureEquipment uf && uf.Feature.GetComponent<ACBonusAgainstWeaponType>() != null)));
        }

        static bool isAgileFistsEnc(BlueprintItemEquipmentNeck bp, int maxEncLevel)
        {
            return (bp.Enchantments.Count == 2 &&
                    bp.Enchantments.TrueForAll(encBp =>
                      (encBp.GetComponent<EquipmentWeaponTypeDamageStatReplacement>() is EquipmentWeaponTypeDamageStatReplacement enc &&
                        enc.AllNaturalAndUnarmed && enc.Stat == StatType.Dexterity && enc.RequiresFinesse &&
                        enc.Category == WeaponCategory.UnarmedStrike) ||
                      (encBp.GetComponent<EquipmentWeaponTypeEnhancement>() is EquipmentWeaponTypeEnhancement enc2 &&
                        enc2.AllNaturalAndUnarmed && enc2.Category == WeaponCategory.UnarmedStrike && maxEncLevel >= enc2.Enhancement)));
        }

        static bool isNaturalArmorEnc(BlueprintItemEnchantment bp, int maxEncLevel)
        {
            return bp.GetComponent<AddStatBonusEquipment>() is AddStatBonusEquipment enc &&
              enc.Descriptor == ModifierDescriptor.NaturalArmorEnhancement && maxEncLevel >= enc.Value;
        }

        static bool isMightyFistsEnc(BlueprintItemEnchantment bp, int maxEncLevel)
        {
            return bp.GetComponent<EquipmentWeaponTypeEnhancement>() is EquipmentWeaponTypeEnhancement enc &&
              enc.AllNaturalAndUnarmed && enc.Category == WeaponCategory.UnarmedStrike &&
              maxEncLevel >= enc.Enhancement;
        }

        static bool isAttribute(StatType stat)
        {
            return
              stat == StatType.Strength ||
              stat == StatType.Dexterity ||
              stat == StatType.Constitution ||
              stat == StatType.Intelligence ||
              stat == StatType.Wisdom ||
              stat == StatType.Charisma;
        }
        static bool isAC(StatType stat)
        {
            return stat == StatType.AC;
        }
    }
    [HarmonyPatch(typeof(VendorVM), nameof(VendorVM.TryMassSale))]
    internal class VendorVM_TryMassSalePatch
    {
        static bool Prefix(ref bool __result)
        {
            if (!Main.Settings.toggleCustomBulkSell) return true;

            var list = Game.Instance.Player.Inventory
                .Select(item => new
                {
                    item,
                    vanillaMassSell = VendorHelper.IsAppropriateForMassSelling(item),
                    numberToSell = Logic.canMassSellCount(item)
                })
                .Where(item => item.item?.Owner != null && (item.vanillaMassSell || item.numberToSell > 0))
                .Select(item => (new { item.item, quantity = item.vanillaMassSell ? -1 : item.numberToSell }))
                .ToList();

            if (list.Count <= 0)
            {
                __result = false;
            }

            list.ForEach(item =>
            {
                Game.Instance.Vendor.AddForSell(item.item, item.quantity);
            });


            return false;
        }
    }
}
