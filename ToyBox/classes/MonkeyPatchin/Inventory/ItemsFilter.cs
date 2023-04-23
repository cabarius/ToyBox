using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Items.Parts;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.Loot;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._PCView.Tooltip.Bricks;
using Kingmaker.UI.MVVM._PCView.Vendor;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.UI.MVVM._VM.Slots;
using System;
using System.Collections.Generic;
using ModKit;
using static ToyBox.BlueprintExtensions;
using Kingmaker.Blueprints.Root;
using Kingmaker.Enums.Damage;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.Tooltip;
using System.Linq;

namespace ToyBox.Inventory {
    // Hook #1 out of #2 for filtering - this hook handled the custom filter categories.
    [HarmonyPatch(typeof(ItemsFilter), nameof(ItemsFilter.ShouldShowItem), new Type[] { typeof(BlueprintItem), typeof(ItemsFilter.FilterType), typeof(ItemEntity) })]
    public static class ItemsFilter_ShouldShowItem_ItemEntity {
        // Here, we handle filtering any expanded categories that we have.
        [HarmonyPrefix]
        public static bool Prefix(BlueprintItem blueprintItem,
            ItemsFilter.FilterType filter,
            ItemEntity item,
            ref bool __result) {
            ExpandedFilterType expanded_filter = (ExpandedFilterType)filter;
            Mod.Log($"ItemsFilter_ShouldShowItem_ItemEntity - filter: {filter} expandedFilter: {expanded_filter}");

            if (expanded_filter == ExpandedFilterType.QuickslotUtilities) {
                __result = item.Blueprint is BlueprintItemEquipmentUsable blueprint && blueprint.Type != UsableItemType.Potion && blueprint.Type != UsableItemType.Scroll;
            }
            else if (expanded_filter == ExpandedFilterType.UnlearnedScrolls) {
                CopyScroll scroll = item.Blueprint.GetComponent<CopyScroll>();
                UnitEntityData unit = WrathExtensions.GetCurrentCharacter();
                __result = scroll != null && unit != null && scroll.CanCopy(item, unit);
            }
            else if (expanded_filter == ExpandedFilterType.UnlearnedRecipes) {
                CopyRecipe recipe = item.Blueprint.GetComponent<CopyRecipe>();
                __result = recipe != null && recipe.CanCopy(item, null);
            }
            else if (expanded_filter == ExpandedFilterType.UnreadDocuments) {
                ItemPartShowInfoCallback cb = item.Get<ItemPartShowInfoCallback>();
                __result = cb != null && (!cb.m_Settings.Once || !cb.m_Triggered);
            }
            else if (expanded_filter == ExpandedFilterType.UsableWithoutUMD) {
                UnitEntityData unit = WrathExtensions.GetCurrentCharacter();
                __result = item.Blueprint is BlueprintItemEquipmentUsable blueprint && (blueprint.Type == UsableItemType.Scroll || blueprint.Type == UsableItemType.Wand) && unit != null && !blueprint.IsUnitNeedUMDForUse(unit);
            }
            else if (expanded_filter == ExpandedFilterType.CurrentEquipped) {
                UnitEntityData unit = WrathExtensions.GetCurrentCharacter();
                __result = unit != null;

                if (__result) {
                    bool weapon_match = item is ItemEntityWeapon weapon && ((unit.Body.PrimaryHand.HasWeapon && unit.Body.PrimaryHand.Weapon.Blueprint.Type == weapon.Blueprint.Type) || (unit.Body.SecondaryHand.HasWeapon && unit.Body.SecondaryHand.Weapon.Blueprint.Type == weapon.Blueprint.Type));

                    bool shield_match = item is ItemEntityShield shield && ((unit.Body.PrimaryHand.HasShield && unit.Body.PrimaryHand.Shield.Blueprint.Type == shield.Blueprint.Type) || (unit.Body.SecondaryHand.HasShield && unit.Body.SecondaryHand.Shield.Blueprint.Type == shield.Blueprint.Type));

                    bool armour_match = item is ItemEntityArmor armor && unit.Body.Armor.HasArmor && unit.Body.Armor.Armor.Blueprint.ProficiencyGroup == armor.Blueprint.ProficiencyGroup;

                    __result = weapon_match || shield_match || armour_match;
                }
            }
            else if (expanded_filter == ExpandedFilterType.NonZeroPW) {
                __result = item.Blueprint.SellPrice > 0 && item.Blueprint.Weight > 0.0f;
            }
            else {
                // Original call - proceed as normal.
                return true;
            }

            // This call to the blueprint version will skip original in prefix then apply the search bar logic in postfix.
            __result = __result && ItemsFilter.ShouldShowItem(item.Blueprint, filter);
            return false;
        }
    }

    // Hook #2 out of #2 for filtering - this hook handles filtering based on string search.
    [HarmonyPatch(typeof(ItemsFilter), nameof(ItemsFilter.ShouldShowItem), new Type[] { typeof(BlueprintItem), typeof(ItemsFilter.FilterType), typeof(ItemEntity) })]
    public static class ItemsFilter_ShouldShowItem_Blueprint {
        public static string SearchContents = null;

        // Prefix: If we're filtering one of the expanded categories, we require more than the blueprint - we require the instance.
        // If someone calls the function to check the blueprint directly, for expanded categories, we must simply allow everything.
        [HarmonyPrefix]
        public static bool Prefix(BlueprintItem blueprintItem,
            ItemsFilter.FilterType filter,
            ItemEntity item,
            ref bool __result) {
            __result = true;
            return (int)filter < (int)ExpandedFilterType.QuickslotUtilities;
        }

        // Postfix: We apply the string match, if any, to the resulting matches from the original call (or our prefix).
        [HarmonyPostfix]
        public static void Postfix(BlueprintItem blueprintItem,
            ItemsFilter.FilterType filter,
            ItemEntity item,
            ref bool __result) {
            if (__result && !string.IsNullOrWhiteSpace(SearchContents)) {
                __result = false;

                if (Main.Settings.InventorySearchCriteria.HasFlag(InventorySearchCriteria.ItemName)) {
                    __result |= blueprintItem.Name.IndexOf(SearchContents, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (Main.Settings.InventorySearchCriteria.HasFlag(InventorySearchCriteria.ItemType)) {
                    __result |= blueprintItem.ItemType.ToString().IndexOf(SearchContents, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (Main.Settings.InventorySearchCriteria.HasFlag(InventorySearchCriteria.ItemSubtype)) {
                    __result |= blueprintItem.SubtypeName.IndexOf(SearchContents, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                if (Main.Settings.InventorySearchCriteria.HasFlag(InventorySearchCriteria.ItemDescription)) {
                    __result |= blueprintItem.Description.IndexOf(SearchContents, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
        }
    }

    // Sorting - this hook handles custom sorting categories.
    [HarmonyPatch(typeof(ItemsFilter))]
    public static class ItemsFilter_ItemSorter {
        private static int CompareByWeightValue(ItemEntity a, ItemEntity b, ItemsFilter.FilterType filter) {
            float a_weight_value = a.Blueprint.Weight <= 0.0f ? float.PositiveInfinity : a.Blueprint.Cost / a.Blueprint.Weight;
            float b_weight_value = b.Blueprint.Weight <= 0.0f ? float.PositiveInfinity : b.Blueprint.Cost / b.Blueprint.Weight;
            return a_weight_value == b_weight_value ? ItemsFilter.CompareByTypeAndName(a, b, filter) : (a_weight_value > b_weight_value ? 1 : -1);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemsFilter.CompareByTypeAndName))]
        private static bool CompareByTypeAndName(ItemEntity a, ItemEntity b, ItemsFilter.FilterType filter, ref int __result) {
            // First by main type
            int a_b_comparison = a.Blueprint.ItemType.CompareTo(b.Blueprint.ItemType);
            if (a_b_comparison != 0) {
                __result = a_b_comparison;
                return false;
            }

            // Then by subtype
            a_b_comparison = string.Compare(a.Blueprint.SubtypeName, b.Blueprint.SubtypeName, StringComparison.OrdinalIgnoreCase);
            if (a_b_comparison != 0) {
                __result = a_b_comparison;
                return false;
            }

            // Finally by name
            __result = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemsFilter.ItemSorter))]
        public static bool Prefix(ItemsFilter.SorterType type, List<ItemEntity> items, ItemsFilter.FilterType filter, ref List<ItemEntity> __result) {
            ExpandedSorterType expanded_type = (ExpandedSorterType)type;

            if (expanded_type == ExpandedSorterType.WeightValueUp) {
                items.Sort((ItemEntity a, ItemEntity b) => CompareByWeightValue(a, b, filter));
            }
            else if (expanded_type == ExpandedSorterType.WeightValueDown) {
                items.Sort((ItemEntity a, ItemEntity b) => CompareByWeightValue(a, b, filter));
                items.Reverse();
            }
            else if (expanded_type == ExpandedSorterType.RarityUp) {
                items.Sort((a, b) => RarityCompare(a, b, false, filter, ItemsFilter.CompareByPrice));
            }
            else if (expanded_type == ExpandedSorterType.RarityDown) {
                items.Sort((a,b) => RarityCompare(a, b, true, filter, ItemsFilter.CompareByPrice));
            }
            else {
                return true;
            }

            __result = items;
            return false;
        }

        [HarmonyPatch(typeof(ItemsFilter), nameof(ItemsFilter.IsMatchSearchRequest))]
        private static class ItemsFilter_IsMatchSearchRequest_Patch {

            public static bool Prefix(ref bool __result, ItemEntity item, string searchRequest) {
                if (string.IsNullOrEmpty(searchRequest)) {
                    __result = true;
                    return false;
                }
                string[] separator = new string[1] { ", " };
                string[] strArray = searchRequest.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (strArray.Length > 1) {
                    __result = strArray.All<string>(searchWord => ItemsFilter.IsMatchSearchRequest(item, searchWord));
                    return false;
                }
                if (item.Name.StringEntry(searchRequest, item)) {
                    __result = true;
                    return false;
                }
                foreach (ItemsFilter.FilterType filterType in Enum.GetValues(typeof(ItemsFilter.FilterType))) {
                    var itemFilter = LocalizedTexts.Instance.ItemsFilter;
                    if (itemFilter.GetText(filterType).Equals(searchRequest, StringComparison.InvariantCultureIgnoreCase)) {
                        __result = ItemsFilter.ShouldShowItem(item, filterType);
                        return false;
                    }
                }
                foreach (ItemsFilter.FilterType filterType in Enum.GetValues(typeof(ExpandedFilterType))) {
                    var itemFilter = LocalizedTexts.Instance.ItemsFilter;
                    if (itemFilter.GetText(filterType).Equals(searchRequest, StringComparison.InvariantCultureIgnoreCase)) {
                        __result = ItemsFilter.ShouldShowItem(item, filterType);
                        return false;
                    }
                }
                ItemTooltipData itemTooltipData;
                if (!ItemsFilter.s_ItemTooltipDataSet.TryGetValue(item, out itemTooltipData)) {
                    itemTooltipData = UIUtilityItem.GetItemTooltipData(item, true);
                    ItemsFilter.s_ItemTooltipDataSet.Add(item, itemTooltipData);
                }
                foreach (KeyValuePair<TooltipElement, string> text in itemTooltipData.Texts) {
                    switch (text.Key) {
                        case TooltipElement.Name:
                        case TooltipElement.Count:
                        case TooltipElement.ItemType:
                        case TooltipElement.Price:
                        case TooltipElement.SellPrice:
                        case TooltipElement.Wielder:
                        case TooltipElement.WielderSlot:
                        case TooltipElement.Damage:
                        case TooltipElement.PhysicalDamage:
                        case TooltipElement.EquipDamage:
                        case TooltipElement.ArmorCheckPenaltyDetails:
                        case TooltipElement.ArcaneSpellFailureDetails:
                        case TooltipElement.Charges:
                        case TooltipElement.CasterLevel:
                            continue;
                        default:
                            if (text.Value.StringEntry(searchRequest, item)) {
                                __result = true;
                                return false;
                            }
                            continue;
                    }
                }
                __result = itemTooltipData.Energy.Any(e => UIUtilityTexts.GetTextByKey(e.Key).StringEntry(searchRequest, item)) || itemTooltipData.PhysicalDamage.Any(d => UIUtilityTexts.GetDamageFormText(d.Key).StringEntry(searchRequest, item));
                return false;
            }
        }
#if false
        [HarmonyPatch(typeof(ItemsFilter), nameof(ItemsFilter.ItemSorter))]
        private static class ItemsFilter_ItemSorter_Patch {
            public static bool Prefix(ref List<ItemEntity> __result, ItemsFilter.SorterType type,
                                       List<ItemEntity> items,
                                       ItemsFilter.FilterType filter) {
                if (!Main.Settings.toggleEnhanceItemSortingWithRarity)
                    return true;
//                Mod.Log("Rarity Sorting");
                switch (type) {
                    case ItemsFilter.SorterType.NotSorted:
                        __result =  items;
                        return false;
                    case ItemsFilter.SorterType.TypeUp:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, false, filter, ItemsFilter.CompareByTypeAndName)));
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.TypeDown:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, true, (a, b) => ItemsFilter.CompareByTypeAndName(a, b, filter))));
                        items.Reverse();
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.PriceUp:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, false, (a, b) => ItemsFilter.CompareByPrice(a, b, filter))));
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.PriceDown:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, true,(a, b) => ItemsFilter.CompareByPrice(a, b, filter))));
                        items.Reverse();
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.NameUp:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, false, (a, b) => ItemsFilter.CompareByName(a, b, filter))));
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.NameDown:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, true, (a, b) => ItemsFilter.CompareByName(a, b, filter))));
                        items.Reverse();
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.DateUp:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, false, (a, b) => ItemsFilter.CompareByDate(a, b, filter))));
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.DateDown:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, true, (a, b) => ItemsFilter.CompareByDate(a, b, filter))));
                        items.Reverse();
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.WeightUp:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, false, (a, b) => ItemsFilter.CompareByWeight(a, b, filter))));
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.WeightDown:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, true, (a, b) => ItemsFilter.CompareByWeight(a, b, filter))));
                        items.Reverse();
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.WeightSingleUp:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, false, (a, b) => ItemsFilter.CompareByWeight(a, b, filter, true))));
                        goto case ItemsFilter.SorterType.NotSorted;
                    case ItemsFilter.SorterType.WeightSingleDown:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, true, (a, b) => ItemsFilter.CompareByWeight(a, b, filter, true))));
                        items.Reverse();
                        goto case ItemsFilter.SorterType.NotSorted;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), (object)type, (string)null);
                }
                return false;
            }
        }
#endif
    }
}