using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToyBox.classes.MainUI {
    public static class EnchantmentEditor {
        public static Settings settings => Main.settings;

        #region GUI
        public static int enchantItemTypeFilter;
        public static int selectedItemFilter;
        public static int selectedEnchantFilter;
        public static string[] ItemTypeNames = Enum.GetNames(typeof(ItemsFilter.ItemType));
        private static ItemEntity[] cacheItems;
        private static List<BlueprintItemEnchantment> cacheEnchantments;
        private static List<BlueprintItemEnchantment> cacheSearch = new List<BlueprintItemEnchantment>(100);

        public static void ResetGUI() { }
        public static void OnGUI() {
            if (!Main.IsInGame) return;

            UI.HStack("DEBUG", 1,
                () => UI.ActionButton("Add Frost enchanment", () => Test()),
                () => UI.ActionButton("Remove fake enchantments", () => Test2()),
                () => { } // TODO - remove all fake enchantments in inventory
                );

            // load blueprints
            if (cacheEnchantments == null) {
                BlueprintBrowser.GetBlueprints();
                if (BlueprintBrowser.blueprints == null) return;

                cacheEnchantments = new List<BlueprintItemEnchantment>(1500);
                foreach (var bp in BlueprintBrowser.blueprints) {
                    if (bp is BlueprintItemEnchantment) {
                        cacheEnchantments.Add(bp as BlueprintItemEnchantment);
                    }
                }
                cacheEnchantments.TrimExcess();
                UpdateItems(enchantItemTypeFilter);
                UpdateSearchResults();
            }

            // Stackable browser
            using (UI.HorizontalScope(UI.Width(350))) {
                float remainingWidth = UI.ummWidth; // TODO - fix remainingWidth; I don't know how that works

                // First column - Type Selection Grid
                using (UI.VerticalScope(GUI.skin.box)) {
                    UI.ActionSelectionGrid(
                        ref enchantItemTypeFilter,
                        ItemTypeNames,
                        1,
                        index => { },
                        UI.buttonStyle,
                        UI.Width(150));
                }
                remainingWidth -= 350;

                UpdateItems(enchantItemTypeFilter); // maybe this should be cached?

                // Second column - Item Selection Grid
                using (UI.VerticalScope(GUI.skin.box)) {
                    UI.ActionSelectionGrid(
                        ref selectedItemFilter,
                        cacheItems.Select(s => s.Name).ToArray(),
                        1,
                        index => { },
                        UI.buttonStyle,
                        UI.Width(200));
                }
                remainingWidth -= 350;

                // Section Column - Main Area
                using (UI.VerticalScope(UI.MinWidth(remainingWidth))) {

                    // Search Field and modifiers
                    using (UI.HorizontalScope()) {
                        UI.ActionTextField(
                            ref settings.searchText,
                            "searhText",
                            (text) => { },
                            () => { UpdateSearchResults(); },
                            UI.MinWidth(100), UI.MaxWidth(400));
                        UI.Label("Limit", UI.Width(150));
                        UI.ActionIntTextField(
                            ref settings.searchLimit,
                            "searchLimit",
                            (limit) => { },
                            () => { UpdateSearchResults(); },
                            UI.MinWidth(75), UI.MaxWidth(250));
                        if (settings.searchLimit > 1000) { settings.searchLimit = 1000; }
                        UI.Space(25);
                        if (UI.Toggle("Search Descriptions", ref settings.searchesDescriptions)) UpdateSearchResults();
                        UI.Space(25);
                        UI.Toggle("Show GUIDs", ref settings.showAssetIDs);
                        UI.Space(25);
                        UI.Toggle("Components", ref settings.showComponents);
                        //UI.Space(25);
                        //UI.Toggle("Elements", ref settings.showElements);
                    }

                    // List of enchantments with buttons to add to item
                    ItemGUI();
                }
            }
        }

        public static void ItemGUI() {
            UI.Div(5);
            for (int i = 0; i < cacheSearch.Count; i++) {
                var enchant = cacheSearch[i];
                var title = enchant.name.Rarity((RarityType)enchant.EnchantmentCost);
                using (UI.HorizontalScope()) {
                    UI.Space(5);
                    UI.Label(title, UI.Width(300));

                    UI.ActionButton("Add", () => ClickEnchantment(i), UI.Width(160)); // TODO - switch Add/Remove and color
                    UI.ActionButton("Remove", () => ClickEnchantment2(i), UI.Width(160));
                    UI.Space(10);

                    using (UI.VerticalScope(UI.Width(200))) {
                        if (settings.showAssetIDs) {
                            using (UI.HorizontalScope(UI.Width(200))) {
                                UI.Label(enchant.GetType().ToString().cyan());
                                GUILayout.TextField(enchant.AssetGuid.ToString(), UI.ExpandWidth(false));
                            }
                        }
                        else UI.Label(enchant.GetType().ToString().cyan());

                        if (enchant.Description.Length > 0) UI.Label(enchant.Description.green(), UI.Width(400));
                    }

                }
                UI.Div(5);
            }
        }


        public static void UpdateItems(int index) {
            cacheItems = Game.Instance.Player.Inventory.Where(item => (int)item.Blueprint.ItemType == index).ToArray();
        }

        public static void UpdateSearchResults() {
            cacheSearch.Clear();
            var terms = settings.searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

            for (int i = 0; cacheSearch.Count < settings.searchLimit && i < cacheEnchantments.Count; i++) {
                var enchant = cacheEnchantments[i];

                if (enchant.AssetGuid.ToString().Contains(settings.searchText)
                    || enchant.GetType().ToString().Contains(settings.searchText)
                    ) {
                    cacheSearch.Add(enchant);
                }
                else {
                    var name = enchant.name;
                    var description = enchant.GetDescription().RemoveHtmlTags() ?? "";
                    if (terms.All(term => StringExtensions.Matches(name, term))
                        || settings.searchesDescriptions && terms.All(term => StringExtensions.Matches(description, term))
                        ) {
                        cacheSearch.Add(enchant);
                    }
                }
            }
        }

        public static void ClickEnchantment(int index) {
            if (selectedItemFilter < 0 || selectedItemFilter >= cacheItems.Length) return;
            if (index < 0 || index >= cacheSearch.Count) return;

            AddEnchantment(cacheItems[selectedItemFilter], cacheSearch[index]);
        }

        public static void ClickEnchantment2(int index) {
            if (selectedItemFilter < 0 || selectedItemFilter >= cacheItems.Length) return;
            if (index < 0 || index >= cacheSearch.Count) return;

            RemoveEnchantment(cacheItems[selectedItemFilter], cacheSearch[index]);
        }

        #endregion

        #region Code
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
        #endregion

        #region Classes
        public class ItemTypeFilter {

        }

        public enum Source {
            Not,       // enchantment is not on the item
            Blueprint, // enchantment is part of item's blueprint
            Timed,     // enchantment is temporarily added by ability/spell (like Magic Weapon)
            Added,     // enchantment is added by toybox
            Removed,   // enchantment is removed by toybox; removing enchantments which should be on an item, might cause stacking bugs
        }
        #endregion
    }
}
