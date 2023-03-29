using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Items;
using UnityEngine;
using Kingmaker.Items;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Components;
using ModKit;
using ToyBox;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.UI.Common;
using Kingmaker;
using Kingmaker.View.MapObjects;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap.Markers;
using Kingmaker.View;
using Kingmaker.EntitySystem.Entities;
using UnityEngine.UI;
using System.Reflection;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;

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
        public const int RarityScaling = 10;
        public static RarityType Rarity(this int rating) {
            var rarity = RarityType.Trash;
            if (rating >= 200) rarity = RarityType.Godly;
            else if (rating >= 115) rarity = RarityType.Primal;
            else if (rating >= 80) rarity = RarityType.Mythic;
            else if (rating >= 50) rarity = RarityType.Legendary;
            else if (rating >= 30) rarity = RarityType.Epic;
            else if (rating >= 20) rarity = RarityType.Rare;
            else if (rating >= 10) rarity = RarityType.Uncommon;
            else if (rating > 5) rarity = RarityType.Common;
            return rarity;
        }
        public static int Rating(this BlueprintItemEnchantment bp) {
            int rating;
            if (bp is BlueprintWeaponEnchantment || bp is BlueprintArmorEnchantment)
                rating = Math.Max(5, bp.EnchantmentCost * RarityScaling);
            else {
                var modifierRating = bp.Components?.Sum(c => c is AddStatBonusEquipment sbe ?sbe.Value : 0) ?? 0;
                rating = Math.Max(RarityScaling * modifierRating, (bp.IdentifyDC * 5) / 2);
            }
            return rating;
        }
        public static int Rating(this ItemEntity item) => item.Blueprint.Rating(item);
        public static int Rating(this BlueprintItem bp) {
            var bpRating = bp.CollectEnchantments().Sum((e) => e.Rating());
            var bpEnchRating = bp.CollectEnchantments().Sum((e) => e.Rating());
            return Math.Max(bpRating, bpEnchRating);
        }
        public static int Rating(this BlueprintItem bp, ItemEntity item = null) {
            var rating = 0;
            var itemRating = 0;
            var itemEnchRating = 0;
            var bpRating = 0;
            try {
                if (item != null) {
                    itemRating = item.Enchantments.Sum(e => e.Blueprint.Rating());
                    itemEnchRating = item.Enchantments.Sum(e => e.Blueprint.Rating());
                    //Mod.Log($"item itemRating: {itemRating} - {itemEnchRating}");
                    if (Game.Instance?.SelectionCharacter?.CurrentSelectedCharacter is var currentCharacter) {
                        var component = bp.GetComponent<CopyItem>();
                        if (component != null && component.CanCopy(item, currentCharacter)) {
                            itemRating = Math.Max(itemRating, RarityScaling);
                        }
                    }
                    itemRating = Math.Max(itemRating, itemEnchRating);
                }
                bpRating = bp.Rating();
                //if (enchantValue > 0) Main.Log($"blueprint enchantValue: {enchantValue}");
                rating = Math.Max(itemRating, bpRating);
            }
            catch {
            }
            //var rating = item.EnchantmentValue * rarityScaling;
            var cost = bp.Cost;
            var logCost = cost > 1 ? Math.Log(cost) / Math.Log(5) : 0;
            if (rating == 0 && bp is BlueprintItemEquipmentUsable usableBP) {
                rating = Math.Max(rating, (int)(2.5f * Math.Floor(logCost)));
            }
            else if (rating == 0 && bp is BlueprintItemEquipment equipBP) {
                rating = Math.Max(rating, (int)(2.5f * Math.Floor(logCost)));
            }
#if false
            Mod.Log($"{bp.Name} : {bp.GetType().Name.grey().bold()} -  itemRating: {itemRating} bpRating: {bpRating} logCost: {logCost} - rating: {rating}");
#endif
            if (bp is BlueprintItemWeapon bpWeap && !bpWeap.IsMagic) rating = Math.Min(rating, RarityScaling - 1);
            if (bp is BlueprintItemArmor bpArmor && !bpArmor.IsMagic) rating = Math.Min(rating, RarityScaling - 1);

            return rating;
        }
        public static RarityType Rarity(this BlueprintItem bp) {
            if (bp == null) return RarityType.None;
            if (bp.IsNotable) return RarityType.Notable;
            if (bp is BlueprintItemNote noteBP) {
                var component = noteBP.GetComponent<AddItemShowInfoCallback>();
                if (component != null) {
                    return RarityType.Notable;
                }
            }
            return Rarity(bp.Rating());
        }

        public static RarityType Rarity(this ItemEntity item) {
            var bp = item.Blueprint;
            if (bp == null) return RarityType.None;
            if (bp.IsNotable) return RarityType.Notable;
            if (bp is BlueprintItemNote noteBP) {
                var component = noteBP.GetComponent<AddItemShowInfoCallback>();
                if (component != null) {
                    return RarityType.Notable;
                }
            }
            return Rarity(bp.Rating(item));
        }
        public static RarityType Rarity(this BlueprintItemEnchantment bp) => bp.Rating().Rarity();
        public static Color color(this RarityType rarity, float adjust = 0) => RarityColors[(int)rarity].color(adjust);
        public static string Rarity(this string s, RarityType rarity, float adjust = 0) => s.color(RarityColors[(int)rarity]);
        public static string RarityInGame(this string s, RarityType rarity, float adjust = 0) {
            var name = settings.toggleColorLootByRarity ? s.color(RarityColors[(int)rarity]) : s;
            if (settings.toggleShowRarityTags)
                if (settings.toggleColorLootByRarity)
                    return name + " " + $"[{rarity}]".darkGrey().bold(); //.SizePercent(75);
                else
                    return name + " " + $"[{rarity}]".Rarity(rarity).bold(); //.SizePercent(75);
            return name;
        }
        public static string GetString(this RarityType rarity, float adjust = 0) => rarity.ToString().Rarity(rarity, adjust);
        public static void Hide(this LocalMapLootMarkerPCView localMapLootMarkerPCView) {
            LocalMapCommonMarkerVM markerVm = localMapLootMarkerPCView.ViewModel as LocalMapCommonMarkerVM;
            LocalMapMarkerPart mapPart = markerVm.m_Marker as LocalMapMarkerPart;
            RarityType highest = RarityType.None;
            if (mapPart?.GetMarkerType() == LocalMapMarkType.Loot) {
                MapObjectView MOV = mapPart.Owner.View as MapObjectView;
                InteractionLootPart lootPart = (MOV.Data.Interactions[0] as InteractionLootPart);
                var loot = lootPart.Loot;
                foreach (var item in loot) {
                    RarityType itemRarity = item.Rarity();
                    if (itemRarity > highest) {
                        highest = itemRarity;
                    }
                }
                if (highest <= settings.maxRarityToHide) {
                    localMapLootMarkerPCView.transform.localScale = new Vector3(0, 0, 0);
                }
                else {
                    localMapLootMarkerPCView.transform.localScale = new Vector3(1, 1, 1);
                }
            }
            else if (mapPart == null) {
                UnitLocalMapMarker unitMarker = markerVm.m_Marker as UnitLocalMapMarker;
                if (unitMarker != null) {
                    UnitEntityView unit = unitMarker.m_Unit;
                    UnitEntityData data = unit.Data;
                    foreach (ItemEntity item in data.Inventory) {
                        if (item.IsLootable) {
                            RarityType itemRarity = item.Rarity();
                            if (itemRarity > highest) {
                                highest = itemRarity;
                            }
                        }
                    }
                    if (highest <= settings.maxRarityToHide) {
                        localMapLootMarkerPCView.transform.localScale = new Vector3(0, 0, 0);
                    }
                    else {
                        localMapLootMarkerPCView.transform.localScale = new Vector3(1, 1, 1);
                    }
                }
            }
        }
    }
}
namespace ModKit {
    public static partial class UI {
        public static void RarityGrid(ref RarityType rarity, int xCols, params GUILayoutOption[] options) => EnumGrid(ref rarity, xCols, (n, rarity) => n.Rarity(rarity), rarityStyle, options);
        public static void RarityGrid(string title, ref RarityType rarity, int xCols, params GUILayoutOption[] options) => EnumGrid(title, ref rarity, xCols, (n, rarity) => n.Rarity(rarity), rarityStyle, options);
    }
}