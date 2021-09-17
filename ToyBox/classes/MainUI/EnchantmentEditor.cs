using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Items;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.classes.MainUI {
    public static class EnchantmentEditor {
        public static Settings settings => Main.settings;
        public static void ResetGUI() { }
        public static void OnGUI() {
            UI.HStack("DEBUG", 1,
                () => UI.ActionButton("Add Frost enchanment", () => Test()),
                () => UI.ActionButton("Remove fake enchantments", () => Test2()),
                () => { }
                );

            using (UI.HorizontalScope(UI.Width(350))) {

            }
        }

        public static void Test() {
            var frost_enchantment = ResourcesLibrary.TryGetBlueprint<BlueprintWeaponEnchantment>("421e54078b7719d40915ce0672511d0b");

            var item = Game.Instance.Player.MainCharacter.Value.GetFirstWeapon();
            AddEnchantment(item, frost_enchantment);
        }

        public static void Test2() {
            var item = Game.Instance.Player.MainCharacter.Value.GetFirstWeapon();
            var enchantments = GetEnchantments(item);
            foreach (var enchantment in enchantments) {
                Main.Log($"{enchantment.Key.Blueprint.name} : {enchantment.Value}");
                if (enchantment.Value) {
                    RemoveEnchantment(item, enchantment.Key);
                }
            }
        }

        // not good; this should be split by ItemsFilter.ItemType
        public static List<ItemEntity> GetInventory() {
            var collection = new List<ItemEntity>();
            foreach (var item in Game.Instance.Player.Inventory) {
                if (item is ItemEntityWeapon || item is ItemEntityArmor || item is ItemEntityShield) {
                    collection.Add(item);
                }
            }
            return collection;
        }

        public static void AddEnchantment(ItemEntity item, BlueprintItemEnchantment enchantment, Rounds? duration = null) {
            if (item.m_Enchantments == null)
                Main.Log("item.m_Enchantments is null");

            var fake_context = new MechanicsContext(default(JsonConstructorMark)); // if context is null, items may stack which could cause bugs

            //var fi = AccessTools.Field(typeof(MechanicsContext), nameof(MechanicsContext.AssociatedBlueprint));
            //fi.SetValue(fake_context, enchantment);  // check if AssociatedBlueprint must be set; I think not

            item.AddEnchantment(enchantment, fake_context, duration);
        }

        public static void RemoveEnchantment(ItemEntity item, BlueprintItemEnchantment enchantment) {
            item.RemoveEnchantment(item.GetEnchantment(enchantment));
        }

        public static void RemoveEnchantment(ItemEntity item, ItemEnchantment enchantment) {
            item.RemoveEnchantment(enchantment);
        }

        /// <summary>probably useless</summary>
        /// <returns>Key is ItemEnchantments of given item. Value is true, if it is a temporary enchantment.</returns>
        public static Dictionary<ItemEnchantment, bool> GetEnchantments(ItemEntity item) {
            Dictionary<ItemEnchantment, bool> enchantments = new Dictionary<ItemEnchantment, bool>();
            var base_enchantments = item.Blueprint.Enchantments;
            foreach (var enchantment in item.Enchantments) {
                enchantments.Add(enchantment, !base_enchantments.Contains(enchantment.Blueprint));
            }
            return enchantments;
        }

        /// <summary>maybe useful to render button texts/colors</summary>
        /// <returns>Source of Enchantment</returns>
        public static Source GetSource(ItemEntity item, BlueprintItemEnchantment enchantment) {

            var enc = item.GetEnchantment(enchantment);
            if (enc == null) {
                if (item.Blueprint.Enchantments.Contains(enchantment)) {
                    return Source.Removed;
                }
                return Source.Not;
            }

            if (enc.EndTime != null) {
                return Source.Timed;
            }

            if (enc.ParentContext == null) { //item.Blueprint.Enchantments.Contains(enchantment)
                return Source.Blueprint;
            }

            return Source.Added;
        }

        public enum Source {
            Not,       // enchantment is not on the item
            Blueprint, // enchantment is part of item's blueprint
            Timed,     // enchantment is temporarily added by ability/spell (like Magic Weapon)
            Added,     // enchantment is added by toybox
            Removed,   // enchantment is removed by toybox; removing enchantments which should be on an item, might cause stacking bugs
        }
    }
}
