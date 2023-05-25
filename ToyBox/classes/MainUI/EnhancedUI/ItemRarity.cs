﻿using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using ModKit;
using System;
using System.Linq;
using ToyBox;
using UnityEngine;
#if Wrath
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
#endif

namespace ToyBox {
    public enum RarityType {
        None,
        Trash,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Primal,
        Godly,
        Notable,
    }
    public static partial class BlueprintExtensions {
        public static RGBA[] RarityColors = {
            RGBA.none,
            RGBA.trash,
            RGBA.common,
            RGBA.uncommon,
            RGBA.rare,
            RGBA.epic,
            RGBA.legendary,
            RGBA.mythic,
            RGBA.primal,
            RGBA.godly,
            RGBA.notable,
        };
        public static RGBA[] DarkModeRarityColors = {
            RGBA.none,
            RGBA.trash,
            RGBA.common,
            RGBA.uncommon_dark,
            RGBA.rare_dark,
            RGBA.epic_dark,
            RGBA.legendary_dark,
            RGBA.mythic_dark,
            RGBA.primal_dark,
            RGBA.godly_dark,
            RGBA.notable_dark,
        };
        public const int RarityScaling = 10;
        public static RarityType Rarity(this int rating) {
            var rarity = rating switch {
                >= 200 => RarityType.Godly,
                >= 115 => RarityType.Primal,
                >= 80 => RarityType.Mythic,
                >= 50 => RarityType.Legendary,
                >= 30 => RarityType.Epic,
                >= 20 => RarityType.Rare,
                >= 10 => RarityType.Uncommon,
                > 5 => RarityType.Common,
                _ => RarityType.Trash
            };
            return rarity;
        }
        public static int Rating(this BlueprintItemEnchantment bp) {
#if Wrath
            int rating;

            var modifierRating = RarityScaling * bp.Components?.Sum(
                c => c is AddStatBonusEquipment sbe ? sbe.Value
                    : c is AllSavesBonusEquipment asbe ? asbe.Value
                    : 0
                    ) ?? 0;
            if (bp is BlueprintWeaponEnchantment || bp is BlueprintArmorEnchantment)
                rating = Math.Max(5, bp.EnchantmentCost * RarityScaling);
            else {
                rating = (bp.IdentifyDC * 5) / 2;
            }
            return Math.Max(modifierRating, rating);
#elif RT
            return 0;
#endif
        }
        public static int Rating(this ItemEntity item) => item.Blueprint.Rating(item);
        public static int Rating(this BlueprintItem bp) {
            var bpRating = bp.CollectEnchantments().Sum((e) => e.Rating());
            var bpEnchantmentRating = bp.CollectEnchantments().Sum((e) => e.Rating());
            return Math.Max(bpRating, bpEnchantmentRating);
        }
        public static int Rating(this BlueprintItem bp, ItemEntity item = null) {
            var rating = 0;
            var itemRating = 0;
#if Wrath
            var cost = bp.Cost;
#elif RT
            var cost = 0;
#endif
            var logCost = cost > 1 ? Math.Log(cost) / Math.Log(5) : 0;
            var costRating = (int)(2.5f * Math.Floor(logCost));
            try {
                if (item != null) {
                    itemRating = item.Enchantments.Sum(e => e.Blueprint.Rating());
                    var itemEnchantmentRating = item.Enchantments.Sum(e => e.Blueprint.Rating());
                    //Mod.Log($"item itemRating: {itemRating} - {itemEnchRating}");
#if Wrath
                    if (Game.Instance?.SelectionCharacter?.CurrentSelectedCharacter is var currentCharacter) {
                        var component = bp.GetComponent<CopyItem>();
                        if (component != null && component.CanCopy(item, currentCharacter)) {
                            itemRating = Math.Max(itemRating, RarityScaling);
                        }
                    }
#endif
                    itemRating = Math.Max(itemRating, itemEnchantmentRating);
                }
                var bpRating = bp.Rating();
                //if (enchantValue > 0) Main.Log($"blueprint enchantValue: {enchantValue}");
                rating = Math.Max(itemRating, bpRating);
                rating = Math.Max(rating, costRating);
            }
            catch {
                // ignored
            }
            //var rating = item.EnchantmentValue * rarityScaling;
            switch (rating) {
                case 0 when bp is BlueprintItemEquipmentUsable usableBP:
                case 0 when bp is BlueprintItemEquipment equipBP:
                    rating = Math.Max(rating, costRating);
                    break;
            }
#if false
            Mod.Log($"{bp.Name} : {bp.GetType().Name.grey().bold()} -  itemRating: {itemRating} bpRating: {bpRating} logCost: {logCost} - rating: {rating}");
#endif
#if Wrath            
            rating = bp switch {
                BlueprintItemWeapon bpWeap when !bpWeap.IsMagic => Math.Min(rating, RarityScaling - 1),
                BlueprintItemArmor bpArmor when !bpArmor.IsMagic => Math.Min(rating, RarityScaling - 1),
                _ => rating
            };
#endif
            return rating;
        }
        public static RarityType Rarity(this BlueprintItem bp) {
            if (bp == null) return RarityType.None;
            if (bp.IsNotable) return RarityType.Notable;
            if (bp is not BlueprintItemNote noteBP) return Rarity(bp.Rating());
#if Wrath
            var component = noteBP.GetComponent<AddItemShowInfoCallback>();
            if (component != null) {
                return RarityType.Notable;
            }
#endif
            return Rarity(bp.Rating());
        }
        public static RarityType Rarity(this ItemEntity item) {
            var bp = item.Blueprint;
            if (bp == null) return RarityType.None;
            if (bp.IsNotable) return RarityType.Notable;
            if (bp is not BlueprintItemNote noteBP) return Rarity(bp.Rating(item));
#if Wrath 
            var component = noteBP.GetComponent<AddItemShowInfoCallback>();
            return component != null ? RarityType.Notable : Rarity(bp.Rating(item));
#elif RT
            return Rarity(bp.Rating());
#endif
        }
        public static RarityType Rarity(this BlueprintItemEnchantment bp) => bp.Rating().Rarity();
        public static Color Color(this RarityType rarity, float adjust = 0) => RarityColors[(int)rarity].color(adjust);
        public static string Rarity(this string s, RarityType rarity, float adjust = 0) => s.color(RarityColors[(int)rarity]);
        public static string DarkModeRarity(this string s, RarityType rarity, float adjust = 0) => s.color(DarkModeRarityColors[(int)rarity]);

        public static string RarityInGame(this string s, RarityType rarity, float adjust = 0) {
            var name = Settings.toggleColorLootByRarity ? s.color(RarityColors[(int)rarity]) : s;
            if (!Settings.toggleShowRarityTags) return name;
            if (Settings.toggleColorLootByRarity)
                return name + " " + $"[{rarity}]".darkGrey().bold(); //.SizePercent(75);
            else
                return name + " " + $"[{rarity}]".Rarity(rarity).bold(); //.SizePercent(75);
        }
        public static string GetString(this RarityType rarity, float adjust = 0) => rarity.ToString().Rarity(rarity, adjust);
#if Wrath
        public static void Hide(this LocalMapLootMarkerPCView localMapLootMarkerPCView) {
            LocalMapCommonMarkerVM markerVm = localMapLootMarkerPCView.ViewModel as LocalMapCommonMarkerVM;
            LocalMapMarkerPart mapPart = markerVm.m_Marker as LocalMapMarkerPart;
            if (mapPart?.GetMarkerType() == LocalMapMarkType.Loot) {
                MapObjectView MOV = mapPart.Owner.View as MapObjectView;
                InteractionLootPart lootPart = (MOV.Data.Interactions[0] as InteractionLootPart);
                DoHide(lootPart.Loot, localMapLootMarkerPCView);
            }
            else if (mapPart == null) {
                if (markerVm.m_Marker is not UnitLocalMapMarker unitMarker) return;
                UnitEntityView unit = unitMarker.m_Unit;
                UnitEntityData data = unit.Data;
                DoHide(data.Inventory, localMapLootMarkerPCView);
            }
        }
        public static void DoHide(ItemsCollection loot, LocalMapLootMarkerPCView localMapLootMarkerPCView) {
            if (loot == Game.Instance.Player.SharedStash) return;
            RarityType highest = RarityType.None;
            foreach (ItemEntity item in loot) {
                if (!item.IsLootable) continue;
                RarityType itemRarity = item.Rarity();
                if (itemRarity > highest) {
                    highest = itemRarity;
                }
            }
            if (highest <= Settings.maxRarityToHide) {
                localMapLootMarkerPCView.transform.localScale = new Vector3(0, 0, 0);
            }
            else {
                localMapLootMarkerPCView.transform.localScale = new Vector3(1, 1, 1);
            }
        }
#endif
        // Compare function for item rarity
        public static float RaritySortScore(this ItemEntity item) {
            var rarity = item.Rarity();
            return rarity == RarityType.Notable ? (float)25f : 10f * (int)rarity;
        }
        public static int RarityCompare(
                ItemEntity a,
                ItemEntity b,
                bool invert,
                ItemsFilter.FilterType filterType,
                Func<ItemEntity, ItemEntity, ItemsFilter.FilterType, int> otherCompare
            ) {
            var result = a.RaritySortScore().CompareTo(b.RaritySortScore());
            if (invert) result *= -1;
            if (result != 0) return result;
            return otherCompare(a, b, filterType);
        }
    }
}
namespace ModKit {
    public static partial class UI {
        public static void RarityGrid(ref RarityType rarity, int xCols, bool shouldLocalize, params GUILayoutOption[] options) => EnumGrid(ref rarity, xCols, (n, rarity) => shouldLocalize ? n.DarkModeRarity(rarity).localize() : n.DarkModeRarity(rarity), rarityStyle, options);
        public static void RarityGrid(ref RarityType rarity, int xCols, params GUILayoutOption[] options) => EnumGrid(ref rarity, xCols, (n, rarity) => n.DarkModeRarity(rarity), rarityStyle, options);
        public static void RarityGrid(string title, ref RarityType rarity, int xCols, params GUILayoutOption[] options) => EnumGrid(title, ref rarity, xCols, (n, rarity) => n.DarkModeRarity(rarity), rarityStyle, options);
    }
}