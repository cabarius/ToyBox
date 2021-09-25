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
        Godly,
        Notable,
    }
    public static partial class BlueprintExensions {
        public static RGBA[] RarityColors = {
            RGBA.none,
            RGBA.trash,
            RGBA.common,
            RGBA.uncommon,
            RGBA.rare,
            RGBA.epic,
            RGBA.legendary,
            RGBA.mythic,
            RGBA.godly,
            RGBA.notable,
        };
        public static RarityType Rarity(this BlueprintItem bp) {
            if (bp == null) return RarityType.None;
            var rating = 0;
            var enchantValue = 0;
            try {
                var enchants = bp.CollectEnchantments();
                enchantValue = 10 * enchants.Sum((e) => e.EnchantmentCost);
                rating = enchantValue;
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
            } else if (rating == 0 && bp is BlueprintItemNote noteBP) {
                var component = noteBP.GetComponent<AddItemShowInfoCallback>();
                if (component != null) {
                    return RarityType.Notable;
                }
            }
            RarityType rarity = RarityType.Trash;
            if (rating > 100) rarity = RarityType.Godly;
            else if (rating >= 60) rarity = RarityType.Mythic;
            else if (rating >= 40) rarity = RarityType.Legendary;
            else if (rating >= 30) rarity = RarityType.Epic;
            else if (rating >= 20) rarity = RarityType.Rare;
            else if (rating >= 10) rarity = RarityType.Uncommon;
            else if (rating > 5) rarity = RarityType.Common;
#if false
            Main.Log($"{bp.Name.Rarity(rarity)} : {bp.GetType().Name.grey().bold()} -  enchantValue: {enchantValue} logCost: {logCost} - rating: {rating}");
#endif
            return rarity;

        }
        public static Color color(this RarityType rarity, float adjust = 0) {
            return RarityColors[(int)rarity].color(adjust);
        }
        public static string Rarity(this string s, RarityType rarity, float adjust = 0) {
            return s.color(RarityColors[(int)rarity]);
        }
        public static string GetString(this RarityType rarity) => rarity.ToString().Rarity(rarity);
    }
}
namespace ModKit {
    public static partial class UI {
        private static Texture2D _rarityTexture = null;
        public static Texture2D RarityTexture {
            get {
                if (_rarityTexture == null) _rarityTexture = new Texture2D(1, 1);
                _rarityTexture.SetPixel(0, 0, RGBA.black.color());
                _rarityTexture.Apply();
                return _rarityTexture;
            }
        }
        private static GUIStyle _rarityStyle;
        public static GUIStyle rarityStyle {
            get {
                if (_rarityStyle == null) {
                    _rarityStyle = new GUIStyle(GUI.skin.button);
                    _rarityStyle.normal.background = RarityTexture;
                }
                return _rarityStyle;
            }
        }
        public static void RarityGrid(ref RarityType rarity, int xCols, params GUILayoutOption[] options) {
            UI.EnumGrid(ref rarity, xCols, (n, rarity) => n.Rarity(rarity), UI.rarityStyle, options);
        }
        public static void RarityGrid(string title, ref RarityType rarity, int xCols, params GUILayoutOption[] options) {
            UI.EnumGrid(title, ref rarity, xCols, (n, rarity) => n.Rarity(rarity), UI.rarityStyle, options);
        }
    }
}