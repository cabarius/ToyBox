using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints.Items;
using UnityEngine;
using ModKit;
using Kingmaker.Items;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Equipment;

namespace ToyBox {
    public enum RarityType {
        None,
        Trash,
        Common,
        Notable,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Godly
    }
    public static partial class BlueprintExensions {
        public static RGBA[] RarityColors = {
            RGBA.white,
            RGBA.trash,
            RGBA.common,
            RGBA.notable,
            RGBA.uncommon,
            RGBA.rare,
            RGBA.epic,
            RGBA.legendary,
            RGBA.mythic,
            RGBA.godly
        };

        public static RarityType Rarity(this BlueprintItem bp) {
            var rating = 0;
            try {
                var enchants = bp.CollectEnchantments();
                rating = 10 * enchants.Sum((e) => e.EnchantmentCost);
            }
            catch {
            }
            //var rating = item.EnchantmentValue * 10;
            var cost = bp.Cost;
            var logCost = cost > 1 ? Math.Log(cost) / Math.Log(5) : 0;
            if (bp.IsNotable) return RarityType.Notable;
            if (rating == 0 && bp is BlueprintItemEquipmentUsable usableBP) {
                rating = Math.Max(rating, (int)(2.5f * Math.Floor(logCost)));
            } else if (rating == 0 && bp is BlueprintItemEquipment equipBP) {
                rating = Math.Max(rating, (int)(2.5f * Math.Floor(logCost)));
            }
            //if (item.HasUniqueOriginArea) rating += 5;
            //if (item.HasUniqueVendor) rating += 5;
            //if (item.Ability != null) rating += 10;
            //if (item.ActivatableAbility != null) rating += 10;
            //rating = Math.Max(rating, );
            RarityType rarity = RarityType.Trash;
            if (rating > 100) rarity = RarityType.Godly;
            else if (rating >= 60) rarity = RarityType.Mythic;
            else if (rating >= 40) rarity = RarityType.Legendary;
            else if (rating >= 30) rarity = RarityType.Epic;
            else if (rating >= 20) rarity = RarityType.Rare;
            else if (rating >= 10) rarity = RarityType.Uncommon;
            else if (rating > 5) rarity = RarityType.Common;
            //Main.Log($"{item.Name.color(rgba)} : {bp.GetType().Name.orange()} -  enchantValue: {item.EnchantmentValue * 10} logCost: {logCost} - rating: {rating}");
            return rarity ;

        }
        public static Color Color(this RarityType rarity) {
            return RarityColors[(int)rarity].Color();
        }
        public static string Rarity(this string s, RarityType rarity) {
            return s.color(RarityColors[(int)rarity]);
        }
    }
}