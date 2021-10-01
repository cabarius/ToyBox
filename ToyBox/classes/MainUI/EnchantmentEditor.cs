﻿using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox.classes.MainUI {
    public static class EnchantmentEditor {
        public static Settings settings => Main.settings;

        #region GUI
        public static BlueprintItemWeaponReference basicSpikeShield = new BlueprintItemWeaponReference() { deserializedGuid = BlueprintGuid.Parse("98a0dc03586a6d04791901c41700e516") }; //SpikedLightShieldPlus1

        public static string searchText = "";
        public static int selectedItemType;
        public static int selectedItemIndex;
        public static int selectedEnchantIndex;
        public static (string, string) renameState = (null, null);
        public static ItemEntity selectedItem = null;
        public static ItemEntity editedItem = null;

        public static string[] ItemTypeNames = Enum.GetNames(typeof(ItemsFilter.ItemType));
        private static ItemEntity[] inventory;
        private static List<BlueprintItemEnchantment> enchantments;
        private static readonly List<BlueprintItemEnchantment> filteredEnchantments = new List<BlueprintItemEnchantment>();

        public static void ResetGUI() { }
        public static void OnGUI() {
            if (!Main.IsInGame) return;
            UI.Label("Sandal says '".orange() + "Enchantment'".cyan().bold());

            // load blueprints
            if (enchantments == null) {
                BlueprintBrowser.GetBlueprints();
                if (BlueprintBrowser.blueprints == null) return;

                enchantments = new List<BlueprintItemEnchantment>();
                foreach (var bp in BlueprintBrowser.blueprints) {
                    if (bp is BlueprintItemEnchantment enchantBP) {
                        enchantments.Add(enchantBP);
                    }
                }
                enchantments.Sort((l, r) => {
                    return r.Rarity().CompareTo(l.Rarity());
                    //if (l.Description != null && r.Description != null || l.Description == null && r.Description == null) {
                    //    return r.Rarity().CompareTo(r.Rarity());
                    //} else if (l.Description != null) return 1;
                    //return -1;
                });
                enchantments.TrimExcess();
                UpdateItems(selectedItemType);
                UpdateSearchResults();
            }

            // Stackable browser
            using (UI.HorizontalScope(UI.Width(350))) {
                float remainingWidth = UI.ummWidth;

                // First column - Type Selection Grid
                using (UI.VerticalScope(GUI.skin.box)) {
                    UI.ActionSelectionGrid(
                        ref selectedItemType,
                        ItemTypeNames,
                        1,
                        index => { },
                        UI.buttonStyle,
                        UI.Width(150));
                }
                remainingWidth -= 350;

                UpdateItems(selectedItemType); // maybe this should be cached?

                // Second column - Item Selection Grid
                using (UI.VerticalScope(GUI.skin.box)) {
                    if (inventory.Length > 0) {
                        UI.ActionSelectionGrid(
                            ref selectedItemIndex,
                            inventory.Select(bp => bp.Name).ToArray(),
                            1,
                            index => selectedItem = inventory[selectedItemIndex],
                            UI.rarityButtonStyle,
                            UI.Width(200));
                    }
                    else {
                        UI.Label("No Items".grey(), UI.Width(200));
                    }
                }
                remainingWidth -= 350;

                // Section Column - Main Area
                using (UI.VerticalScope(UI.MinWidth(remainingWidth))) {
                    if (selectedItem != null) {
                        var item = selectedItem;
                        UI.Label("Target".cyan());
                        using (UI.VerticalScope(GUI.skin.box, UI.MinHeight(125))) {
                            var rarity = item.Rarity();
                            //Main.Log($"item.Name - {item.Name.ToString().Rarity(rarity)} rating: {item.Blueprint.Rating(item)}");
                            var itemName = item.Blueprint.GetDisplayName();

                            UI.Label(item.Name.bold(), UI.Width(300));
                            UI.Space(20);

                            if (item is ItemEntityShield shield) {
                                TargetGUI(shield.ArmorComponent);
                                UI.Space(20);
                                if (shield.WeaponComponent != null) {
                                    UI.Label("Weapon Part".orange().bold(), UI.Width(300));
                                    UI.Space(20);
                                    TargetGUI(shield.WeaponComponent);
                                    UI.ActionButton("Remove Weapon Part", () => shield.WeaponComponent = null, UI.AutoWidth());
                                }
                                else {
                                    UI.ActionButton("Add Weapon Part", () => shield.WeaponComponent = new ItemEntityWeapon(shield.Blueprint.WeaponComponent ?? basicSpikeShield, shield), UI.AutoWidth());
                                }
                            }
                            else if (item is ItemEntityWeapon weapon) {
                                TargetGUI(weapon);
                                UI.Space(20);
                                if (weapon.Second != null) {
                                    UI.Label("2nd Weapon Part".orange().bold(), UI.Width(300));
                                    UI.Space(20);
                                    TargetGUI(weapon.Second);
                                }
                            }
                            else {
                                TargetGUI(item);
                            }
                        }
                        UI.Space(25);
                    }
                    // Search Field and modifiers
                    using (UI.HorizontalScope()) {
                        UI.ActionTextField(
                            ref searchText,
                            "searhText",
                            (text) => { },
                            () => { UpdateSearchResults(); },
                            UI.MinWidth(100), UI.MaxWidth(400));
                        UI.Space(15);
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

        public static void TargetGUI(ItemEntity item) {
            var enchantements = GetEnchantments(item);
            if (enchantements.Count > 0) {
                using (UI.VerticalScope()) {
                    foreach (var entry in enchantements) {
                        var enchant = entry.Key;
                        var enchantBP = enchant.Blueprint;
                        var name = enchantBP.name;
                        if (name != null && name.Length > 0) {
                            name = name.Rarity(enchantBP.Rarity());
                            using (UI.HorizontalScope()) {
                                UI.Label(name, UI.Width(400));
                                UI.Space(25);
                                UI.Label(entry.Value ? "Custom".yellow() : "Perm".orange(), UI.Width(100));
                                UI.Space(25);
                                UI.ActionButton("Remove", () => RemoveEnchantment(item, enchant), UI.AutoWidth());
                                var description = enchantBP.Description;
                                if (description != null) {
                                    UI.Space(25);
                                    UI.Label(description.RemoveHtmlTags().green());
                                }
                            }
                        }
                    }
                }
            }
            else {
                UI.Label("No Enchantments".orange());
            }
        }

        public static void ItemGUI() {
            UI.Div(5);
            for (int i = 0; i < filteredEnchantments.Count; i++) {
                var enchant = filteredEnchantments[i];
                var title = enchant.name.Rarity(enchant.Rarity());
                using (UI.HorizontalScope()) {
                    UI.Space(5);
                    UI.Label(title, UI.Width(400));

                    if (selectedItem != null) {
                        var shield = selectedItem as ItemEntityShield;
                        var weapon = selectedItem as ItemEntityWeapon;
                        // first part
                        UI.ActionButton("Add", () => AddClicked(i), UI.Width(100));
                        if (shield != null && shield.ArmorComponent.Enchantments.Any(e => e.Blueprint == enchant)) {
                            UI.ActionButton("Remove", () => RemoveClicked(i), UI.Width(100));
                        }
                        else if (selectedItem.Enchantments.Any(e => e.Blueprint == enchant)) {
                            UI.ActionButton("Remove", () => RemoveClicked(i), UI.Width(100));
                        }
                        else {
                            UI.Space(104);
                        }

                        // second part
                        if (shield?.WeaponComponent != null) {
                            UI.ActionButton("Sec. Add", () => AddClicked(i, true), UI.Width(100));
                            if (shield.WeaponComponent.Enchantments.Any(e => e.Blueprint == enchant)) {
                                UI.ActionButton("Sec. Remove", () => RemoveClicked(i, true), UI.Width(100));
                            }
                            else {
                                UI.Space(104);
                            }
                        }
                        else if (weapon?.Second != null) {
                            UI.ActionButton("Sec. Add", () => AddClicked(i, true), UI.Width(100));
                            if (weapon.Second.Enchantments.Any(e => e.Blueprint == enchant)) {
                                UI.ActionButton("Sec. Remove", () => RemoveClicked(i, true), UI.Width(100));
                            }
                            else {
                                UI.Space(104);
                            }
                        }
                    }

                    UI.Space(10);
                    if (settings.showAssetIDs) {
                        using (UI.VerticalScope()) {
                            using (UI.HorizontalScope()) {
                                UI.Label(enchant.CollationName().cyan(), UI.Width(300));
                                GUILayout.TextField(enchant.AssetGuid.ToString(), UI.AutoWidth());
                            }
                            if (enchant.Description.Length > 0) UI.Label(enchant.Description.RemoveHtmlTags().green());
                        }
                    }
                    else {
                        UI.Label(enchant.CollationName().cyan(), UI.Width(300));
                        if (enchant.Description.Length > 0) UI.Label(enchant.Description.RemoveHtmlTags().green());
                    }

                }
                UI.Div(5);
            }
        }

        public static void UpdateItems(int index) {
            inventory = Game.Instance.Player.Inventory
                            .Where(item => (int)item.Blueprint.ItemType == index)
                            .OrderByDescending(item => item.Blueprint.Rarity())
                            .ToArray();
            if (editedItem != null) {
                selectedItemIndex = inventory.IndexOf(editedItem);
                editedItem = null;
            }
            if (selectedItemIndex >= inventory.Length) {
                selectedItemIndex = 0;
            }
            selectedItem = selectedItemIndex < inventory.Length ? inventory[selectedItemIndex] : null;
        }

        public static void UpdateSearchResults() {
            filteredEnchantments.Clear();
            editedItem = null;
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

            for (int i = 0; filteredEnchantments.Count < settings.searchLimit && i < enchantments.Count; i++) {
                var enchant = enchantments[i];
                if (enchant.AssetGuid.ToString().Contains(searchText)
                    || enchant.GetType().ToString().Contains(searchText)
                    ) {
                    filteredEnchantments.Add(enchant);
                }
                else {
                    var name = enchant.name;
                    var description = enchant.GetDescription() ?? "";
                    description = description.RemoveHtmlTags();
                    if (terms.All(term => StringExtensions.Matches(name, term))
                        || settings.searchesDescriptions && terms.All(term => StringExtensions.Matches(description, term))
                        ) {
                        filteredEnchantments.Add(enchant);
                    }
                }
            }
        }

        public static void AddClicked(int index, bool second = false) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Length) return;
            if (index < 0 || index >= filteredEnchantments.Count) return;

            if (inventory[selectedItemIndex] is ItemEntityShield shield) {
                if (!second)
                    AddEnchantment(shield.ArmorComponent, filteredEnchantments[index]);
                else
                    AddEnchantment(shield.WeaponComponent, filteredEnchantments[index]);
                editedItem = shield;
            }
            else if (second && inventory[selectedItemIndex] is ItemEntityWeapon weapon) {
                AddEnchantment(weapon.Second, filteredEnchantments[index]);
                editedItem = weapon;
            }
            else {
                AddEnchantment(inventory[selectedItemIndex], filteredEnchantments[index]);
                editedItem = inventory[selectedItemIndex];
            }
        }

        public static void RemoveClicked(int index, bool second = false) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Length) return;
            if (index < 0 || index >= filteredEnchantments.Count) return;

            if (inventory[selectedItemIndex] is ItemEntityShield shield) {
                if (!second)
                    RemoveEnchantment(shield.ArmorComponent, filteredEnchantments[index]);
                else
                    RemoveEnchantment(shield.WeaponComponent, filteredEnchantments[index]);
                editedItem = shield;
            }
            if (second && inventory[selectedItemIndex] is ItemEntityWeapon weapon) {
                RemoveEnchantment(weapon.Second, filteredEnchantments[index]);
                editedItem = weapon;
            }
            else {
                RemoveEnchantment(inventory[selectedItemIndex], filteredEnchantments[index]);
                editedItem = inventory[selectedItemIndex];
            }
        }

        #endregion

        #region Code
        public static void AddEnchantment(ItemEntity item, BlueprintItemEnchantment enchantment, Rounds? duration = null) {
            if (item?.m_Enchantments == null)
                Main.Log("item.m_Enchantments is null");

            var fake_context = new MechanicsContext(default(JsonConstructorMark)); // if context is null, items may stack which could cause bugs

            //var fi = AccessTools.Field(typeof(MechanicsContext), nameof(MechanicsContext.AssociatedBlueprint));
            //fi.SetValue(fake_context, enchantment);  // check if AssociatedBlueprint must be set; I think not

            item.AddEnchantment(enchantment, fake_context, duration);
        }

        public static void RemoveEnchantment(ItemEntity item, BlueprintItemEnchantment enchantment) {
            if (item == null) return;
            item.RemoveEnchantment(item.GetEnchantment(enchantment));
        }

        public static void RemoveEnchantment(ItemEntity item, ItemEnchantment enchantment) {
            if (item == null) return;
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
        public enum Source {
            Not,       // enchantment is not on the item
            Removed,   // enchantment is removed by toybox; removing enchantments which should be on an item, might cause stacking bugs
            Timed,     // enchantment is temporarily added by ability/spell (like Magic Weapon)
            Added,     // enchantment is added by toybox
            Blueprint, // enchantment is part of item's blueprint
        }
        #endregion
    }
}
