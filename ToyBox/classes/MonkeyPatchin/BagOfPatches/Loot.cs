using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Items;
using Kingmaker.Items.Parts;
using Kingmaker.UI.MVVM._PCView.Loot;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._PCView.Tooltip.Bricks;
using Kingmaker.UI.MVVM._PCView.Vendor;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using static ToyBox.BlueprintExtensions;
using Kingmaker.UI.MVVM._VM.Slots;
using UniRx;
using Owlcat.Runtime.UI.MVVM;
using UnityEngine;
using UnityEngine.UI;
using System;
using Kingmaker.Items.Slots;
using Kingmaker.UI.Common;
using System.Collections.Generic;
using ModKit;

namespace ToyBox.BagOfPatches {
    internal static class Loot {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(LootSlotPCView), nameof(LootSlotPCView.BindViewImplementation))]
        private static class ItemSlot_IsUsable_Patch {
            public static void Postfix(ViewBase<ItemSlotVM> __instance) {
                if (__instance is LootSlotPCView itemSlotPCView) {
                    //                        modLogger.Log($"checking  {itemSlotPCView.ViewModel.Item}");
                    if (itemSlotPCView.ViewModel.HasItem && itemSlotPCView.ViewModel.IsScroll && settings.toggleHighlightCopyableScrolls) {
                        //                            modLogger.Log($"found {itemSlotPCView.ViewModel}");
                        itemSlotPCView.m_Icon.CrossFadeColor(new Color(0.5f, 1.0f, 0.5f, 1.0f), 0.2f, true, true);
                    }
                    else {
                        itemSlotPCView.m_Icon.CrossFadeColor(Color.white, 0.2f, true, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlotView<EquipSlotVM>), nameof(ItemSlotView<EquipSlotVM>.RefreshItem))]
        private static class ItemSlotView_RefreshItem_Patch {
            public static void Postfix(InventoryEquipSlotView __instance) {
                if (!__instance.SlotVM.HasItem || !__instance.SlotVM.IsScroll) {
                    __instance.m_Icon.color = Color.white;
                }
                else if (__instance.SlotVM.IsScroll) {
                    __instance.m_Icon.color = new Color(0.5f, 1.0f, 0.5f, 1.0f);
                }
                var item = __instance.Item;
                if (settings.UsingLootRarity && item != null) {
                    _ = item.Blueprint.GetComponent<AddItemShowInfoCallback>();
                    var cb = item.Get<ItemPartShowInfoCallback>();
                    if (cb != null && (!cb.m_Settings.Once || !cb.m_Triggered)) {
                        // This forces the item to display as notable
                        __instance.SlotVM.IsNotable.SetValueAndForceNotify(true);
                    }
                    var rarity = item.Rarity();
                    var color = rarity.Color();
                    //Main.Log($"ItemSlotView_RefreshItem_Patch - {item.Name} - {color}");
                    if (rarity == RarityType.Notable && __instance.m_NotableLayer != null) {
                        var objFX = __instance.m_NotableLayer.Find("NotableLayerFX");
                        if (objFX != null && objFX.TryGetComponent<Image>(out var image)) image.color = color;
                    }
                    else if (rarity != RarityType.Notable) {
                        if (rarity >= RarityType.Uncommon) // Make sure things uncommon or better get their color circles despite not being magic. Colored loot offers sligtly different UI assumptions
                            __instance.SlotVM.IsMagic.SetValueAndForceNotify(true);

                        if (__instance.m_MagicLayer != null) {
                            var ratingAlpha = (float)Math.Min(120, item.Rating() + 20) / 120;
                            var colorTranslucent = new Color(color.r, color.g, color.b, color.a * ratingAlpha); // 0.45f);
                            var obj = __instance.m_MagicLayer.gameObject;
                            obj.GetComponent<Image>().color = colorTranslucent;
                            var objFX = __instance.m_MagicLayer.Find("MagicLayerFX");
                            if (objFX != null && objFX.TryGetComponent<Image>(out var image)) image.color = color;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.Name), MethodType.Getter)]
        private static class ItemEntity_Name_Patch {
            public static void Postfix(ItemEntity __instance, ref string __result) {
                if (settings.UsingLootRarity && __result != null && __result.Length > 0) {
                    var bp = __instance.Blueprint;
                    var rarity = __instance.Rarity();
                    if (rarity < settings.minRarityToColor) return;
                    if (bp is BlueprintItemWeapon bpWeap && !bpWeap.IsMagic && rarity < RarityType.Uncommon) return;
                    if (bp is BlueprintItemArmor bpArmor && !bpArmor.IsMagic && rarity < RarityType.Uncommon) return;
                    var result = __result.RarityInGame(rarity);
                    //Main.Log($"ItemEntity_Name_Patch - Name: {__result} type:{__instance.GetType().FullName} - {rarity.ToString()} -> {result}");
                    __result = result;
                }
            }
        }

        internal static Color ColoredLootBackgroundColor = new(1f, 1f, 1f, 0.25f);
        internal static Color ColoredEquipSlotBackgroundColor = new(1f, 1f, 1f, 0.45f);


        [HarmonyPatch(typeof(InventoryPCView), nameof(InventoryPCView.BindViewImplementation))]
        private static class InventoryPCView_BindViewImplementation_Patch {
            public static void Postfix(InventoryPCView __instance) {
                if (!settings.UsingLootRarity) return;
                var decoration = __instance.gameObject?
                    .transform.Find("Inventory/Stash/StashContainer/StashScrollView/decoration");
                var image = decoration?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredLootBackgroundColor;
                }
            }
        }
        [HarmonyPatch(typeof(InventoryEquipSlotPCView), nameof(InventoryEquipSlotPCView.BindViewImplementation))]
        private static class InventoryEquipSlotPCView_BindViewImplementation_Patch {
            public static void Postfix(InventoryEquipSlotPCView __instance) {
                if (!settings.UsingLootRarity) return;
                var backfill = __instance.gameObject?
                    .transform.Find("Backfill");
                var image = backfill?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredEquipSlotBackgroundColor;
                }
            }
        }
        [HarmonyPatch(typeof(WeaponSetPCView), nameof(WeaponSetPCView.BindViewImplementation))]
        private static class WeaponSetPCView_BindViewImplementation_Patch {
            public static void Postfix(WeaponSetPCView __instance) {
                if (!settings.UsingLootRarity) return;
                var selected = __instance.gameObject?
                    .transform.Find("SelectedObject");
                var image = selected?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredLootBackgroundColor;
                }
            }
        }

        [HarmonyPatch(typeof(LootCollectorPCView), nameof(VendorPCView.BindViewImplementation))]
        private static class LootCollectorPCView_BindViewImplementation_Patch {
            public static void Postfix(LootCollectorPCView __instance) {
                if (!settings.UsingLootRarity) return;
                var image = __instance.gameObject?
                    .transform.Find("Collector/StashScrollView/Decoration")?
                    .GetComponent<UnityEngine.UI.Image>();
                image.color = ColoredLootBackgroundColor;

            }
        }
        [HarmonyPatch(typeof(VendorPCView), nameof(VendorPCView.BindViewImplementation))]
        private static class VendorPCView_BindViewImplementation_Patch {
            public static void Postfix(VendorPCView __instance) {
                if (!settings.UsingLootRarity) return;
                var vendorImage = __instance.gameObject?
                    .transform.Find("MainContent/VendorBlock/VendorStashScrollView/decoration")?
                    .GetComponent<UnityEngine.UI.Image>();
                vendorImage.color = ColoredLootBackgroundColor;
                var playerImage = __instance.gameObject?
                    .transform.Find("MainContent/PlayerStash/StashScrollView/decoration")?
                    .GetComponent<UnityEngine.UI.Image>();
                playerImage.color = ColoredLootBackgroundColor;
            }
        }
        [HarmonyPatch(typeof(TooltipBrickEntityHeaderView), nameof(TooltipBrickEntityHeaderView.BindViewImplementation))]
        private static class TooltipBrickEntityHeaderView_BindViewImplementation_Patch {
            public static void Postfix(TooltipBrickEntityHeaderView __instance) {
                if (!settings.UsingLootRarity) return;
                var image = __instance.gameObject?
                    .transform.Find("TextBlock/Back/ItemBackContainer")?
                    .GetComponent<UnityEngine.UI.Image>();
                image.color = ColoredLootBackgroundColor;

#if false
                __instance.m_MainTitle.color = (Color32)Color.green;
                __instance.m_MainTitle.overrideColorTags = true;
                __instance.m_MainTitle.outlineColor = (Color32)Color.magenta;
                __instance.m_MainTitle.outlineWidth = 5;
#endif
            }
        }
        [HarmonyPatch(typeof(LocalMapMarkerPCView), nameof(LocalMapMarkerPCView.BindViewImplementation))]
        private static class LocalMapMarkerPCView_BindViewImplementation_Patch {
            public static void Postfix(LocalMapMarkerPCView __instance) {
                if (__instance == null)
                    return;

                if (__instance.ViewModel.MarkerType == LocalMapMarkType.Loot)
                    __instance.AddDisposable(__instance.ViewModel.IsVisible.Subscribe(value => {
                        (__instance as LocalMapLootMarkerPCView)?.Hide();
                    }));
            }
        }
        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.CanRemoveItem))]
        private static class ItemSlot_CanRemoveItem_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleOverrideLockedItems)
                    __result = true;
            }
        }
        [HarmonyPatch(typeof(ItemsFilter), nameof(ItemsFilter.ItemSorter))]
        private static class ItemsFilter_ItemSorter_Patch {
            public static bool Prefix(ref List<ItemEntity> __result, ItemsFilter.SorterType type,
                                       List<ItemEntity> items,
                                       ItemsFilter.FilterType filter) {
                if (!settings.toggleEnhanceItemSortingWithRarity)
                    return true;
//                Mod.Log("Rarity Sorting");
                switch (type) {
                    case ItemsFilter.SorterType.NotSorted:
                        __result =  items;
                        return false;
                    case ItemsFilter.SorterType.TypeUp:
                        items.Sort((Comparison<ItemEntity>)((a, b) => RarityCompare(a, b, false,(a, b) => ItemsFilter.CompareByTypeAndName(a, b, filter))));
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
    }
}

