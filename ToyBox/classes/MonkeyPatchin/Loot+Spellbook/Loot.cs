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
using System.Linq;
using System.Linq.Expressions;
using Kingmaker.Items.Slots;
using Kingmaker.UI.Common;
using System.Collections.Generic;
using ModKit;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using ModKit.Utility;
using Kingmaker.UI.Tooltip;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.UnitLogic;
using System.Text;
using Kingmaker.Utility;
using Kingmaker.UI.MVVM._VM.Party;
using Kingmaker.View.MapObjects;
using Owlcat.Runtime.Core.Utils;
using Kingmaker.Blueprints.Items;
using Kingmaker.BundlesLoading;
using System.IO;

namespace ToyBox.Inventory {
    internal static class Loot {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        // Highlight copyable scolls
        [HarmonyPatch(typeof(LootSlotPCView), nameof(LootSlotPCView.BindViewImplementation))]
        private static class ItemSlot_IsUsable_Patch {
            public static void Postfix(ViewBase<ItemSlotVM> __instance) {
                if (__instance is LootSlotPCView itemSlotPCView) {
                    //                        modLogger.Log($"checking  {itemSlotPCView.ViewModel.Item}");
                    if (itemSlotPCView.ViewModel.HasItem && itemSlotPCView.ViewModel.IsScroll && Settings.toggleHighlightCopyableScrolls) {
                        //                            modLogger.Log($"found {itemSlotPCView.ViewModel}");
                        itemSlotPCView.m_Icon.CrossFadeColor(new Color(0.5f, 1.0f, 0.5f, 1.0f), 0.2f, true, true);
                    }
                    else {
                        itemSlotPCView.m_Icon.CrossFadeColor(Color.white, 0.2f, true, true);
                    }
                }
            }
        }

        // Adds Rarity color circles to items in inventory
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
                if (Settings.UsingLootRarity && item != null) {
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

        // Adds Rarity tags/colors to item names
        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.Name), MethodType.Getter)]
        private static class ItemEntity_Name_Patch {
            public static void Postfix(ItemEntity __instance, ref string __result) {
                if (Settings.UsingLootRarity && __result != null && __result.Length > 0) {
                    var bp = __instance.Blueprint;
                    var rarity = __instance.Rarity();
                    if (rarity < Settings.minRarityToColor) return;
                    if (bp is BlueprintItemWeapon bpWeap && !bpWeap.IsMagic && rarity < RarityType.Uncommon) return;
                    if (bp is BlueprintItemArmor bpArmor && !bpArmor.IsMagic && rarity < RarityType.Uncommon) return;
                    if (bp is BlueprintItem bpItem && bpItem.NameForAcronym.Contains("Domino")) {
                        if (Settings.toggleColorLootByRarity)
                            __result = __result.Rarity(rarity);
                        __result = __result + $"\n[Puzzle Piece: {bpItem.NameForAcronym.Replace("Domino", "")}]".bold().sizePercent(75);
                        return;

                    }
                    var result = __result.RarityInGame(rarity);
                    //Main.Log($"ItemEntity_Name_Patch - Name: {__result} type:{__instance.GetType().FullName} - {rarity.ToString()} -> {result}");
                    __result = result;
                }
            }
        }

        internal static Color ColoredLootBackgroundColor = new(1f, 1f, 1f, 0.25f);
        internal static Color ColoredEquipSlotBackgroundColor = new(1f, 1f, 1f, 0.45f);

        // Modifies inventory slot background to work with rarity coloring
        [HarmonyPatch(typeof(InventoryPCView), nameof(InventoryPCView.BindViewImplementation))]
        private static class InventoryPCView_BindViewImplementation_Patch {
            public static void Postfix(InventoryPCView __instance) {
                if (!Settings.UsingLootRarity) return;
                var decoration = __instance.gameObject?
                    .transform.Find("Inventory/Stash/StashContainer/StashScrollView/decoration");
                var image = decoration?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredLootBackgroundColor;
                }
            }
        }

        // Modifies equipment slot background to work with rarity coloring
        [HarmonyPatch(typeof(InventoryEquipSlotPCView), nameof(InventoryEquipSlotPCView.BindViewImplementation))]
        private static class InventoryEquipSlotPCView_BindViewImplementation_Patch {
            public static void Postfix(InventoryEquipSlotPCView __instance) {
                if (!Settings.UsingLootRarity) return;
                var backfill = __instance.gameObject?
                    .transform.Find("Backfill");
                var image = backfill?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredEquipSlotBackgroundColor;
                }
            }
        }

        // modifies weapon slot backgrounds to work with rarity coloring
        [HarmonyPatch(typeof(WeaponSetPCView), nameof(WeaponSetPCView.BindViewImplementation))]
        private static class WeaponSetPCView_BindViewImplementation_Patch {
            public static void Postfix(WeaponSetPCView __instance) {
                if (!Settings.UsingLootRarity) return;
                var selected = __instance.gameObject?
                    .transform.Find("SelectedObject");
                var image = selected?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredLootBackgroundColor;
                }
            }
        }

        // modifies slot backgrounds in chests and such to work with rarity coloring
        [HarmonyPatch(typeof(LootCollectorPCView), nameof(VendorPCView.BindViewImplementation))]
        private static class LootCollectorPCView_BindViewImplementation_Patch {
            public static void Postfix(LootCollectorPCView __instance) {
                if (!Settings.UsingLootRarity) return;
                var image = __instance.gameObject?
                    .transform.Find("Collector/StashScrollView/Decoration")?
                    .GetComponent<UnityEngine.UI.Image>();
                image.color = ColoredLootBackgroundColor;

            }
        }

        // modify vender slot backgrounds to work with rarity coloring
        [HarmonyPatch(typeof(VendorPCView), nameof(VendorPCView.BindViewImplementation))]
        private static class VendorPCView_BindViewImplementation_Patch {
            public static void Postfix(VendorPCView __instance) {
                if (!Settings.UsingLootRarity) return;
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

        // modifies tooltip title background color
        [HarmonyPatch(typeof(TooltipBrickEntityHeaderView), nameof(TooltipBrickEntityHeaderView.BindViewImplementation))]
        private static class TooltipBrickEntityHeaderView_BindViewImplementation_Patch {
            public static void Postfix(TooltipBrickEntityHeaderView __instance) {
                if (!Settings.UsingLootRarity) return;
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
                    __instance.AddDisposable(__instance.ViewModel.IsVisible.Subscribe(value => { (__instance as LocalMapLootMarkerPCView)?.Hide(); }));
            }
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.CanRemoveItem))]
        private static class ItemSlot_CanRemoveItem_Patch {
            public static void Postfix(ref bool __result) {
                if (Settings.toggleOverrideLockedItems)
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(MassLootHelper), nameof(MassLootHelper.GetMassLootFromCurrentArea))]
        public static class PatchLootEverythingOnLeave_Patch {
            public static bool Prefix(ref IEnumerable<LootWrapper> __result) {
                if (!Settings.toggleMassLootEverything) return true;
                IEnumerable<UnitEntityData> all_units = Game.Instance.State.Units.All;
                if (Settings.toggleLootAliveUnits) {
                    all_units = all_units.Where(unit => unit.IsInGame && unit.HasLoot);
                }
                else {
                    all_units = all_units.Where(unit => unit.IsInGame && unit.IsDeadAndHasLoot);
                }

                var result_units = all_units.Select(unit => new LootWrapper { Unit = unit });

                var all_entities = Game.Instance.State.Entities.All.Where(w => w.IsInGame);
                var all_chests = all_entities.Select(s => s.Get<InteractionLootPart>()).Where(i => i?.Loot != Game.Instance.Player.SharedStash).NotNull();

                var tmp = TempList.Get<InteractionLootPart>();

                foreach (var i in all_chests) {
                    //if (i.Owner.IsRevealed
                    //    && i.Loot.HasLoot
                    //    && (i.LootViewed
                    //        || (i.View is DroppedLoot && !i.Owner.Get<DroppedLoot.EntityPartBreathOfMoney>())
                    //        || i.View.GetComponent<SkinnedMeshRenderer>()))
                    if (i.Loot.HasLoot) {
                        tmp.Add(i);
                    }
                }

                var result_chests = tmp.Distinct(new MassLootHelper.LootDuplicateCheck()).Select(i => new LootWrapper { InteractionLoot = i });

                __result = result_units.Concat(result_chests);
#if false
                foreach (var loot in __result) // showing inventories from living enemies makes the items invisible (also they can still be looted with the Get All option)
                {
                    if (loot.Unit != null)
                    ;
                    if (loot.InteractionLoot != null)
                    ;
                }
#endif
                return false;
            }
        }
        [HarmonyPatch(typeof(BundlesLoadService), nameof(BundlesLoadService.RequestBundle))]
        static class TexturePatchForNarria
        {
            const string BundleName = "dungeons_areshkagal.worldtex";
            const string texture_purple = "areshkagal_puzzle_cian_d";
            const string texture_purple_dark = "areshkagal_puzzle_cian_dark_d";
            static void Postfix(ref AssetBundle __result)
            {
                if (!__result.name.Contains(BundleName)) return;
                Mod.Log($"Found Asset Bundle named {BundleName}.");
                Texture2D[] textures = __result.LoadAllAssets<Texture2D>();
                Texture2D[] matches = textures.Where(t => t.name.Equals(texture_purple)).ToArray();
                Mod.Log($"Found {matches.Length} textures of name {texture_purple}");
                var pathBase = Mod.modEntryPath 
                           + Path.DirectorySeparatorChar + "Art" 
                           + Path.DirectorySeparatorChar + "Texture2D" 
                           + Path.DirectorySeparatorChar;
                matches.ForEach(t => t.LoadImage(File.ReadAllBytes(pathBase + texture_purple + ".png")));
                Texture2D[] cians_dark = textures.Where(t => t.name.Equals(texture_purple_dark)).ToArray();
                Mod.Log($"Found {cians_dark.Length} textures of name {texture_purple_dark}");
                cians_dark.ForEach(t => t.LoadImage(File.ReadAllBytes(pathBase + texture_purple_dark + ".png")));
            }
        }
    }
}

