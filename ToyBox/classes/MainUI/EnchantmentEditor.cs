using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
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
        public static int selectedItemType;
        public static int selectedItemIndex;
        public static int selectedEnchantIndex;
        public static (string, string) renameState = (null, null);
        public static ItemEntity selectedItem = null;
        public static ItemEntity editedItem = null;

        public static string[] ItemTypeNames = Enum.GetNames(typeof(ItemsFilter.ItemType));
        private static ItemEntity[] inventory;
        private static List<BlueprintItemEnchantment> enchantments;
        private static List<BlueprintItemEnchantment> filteredEnchantments = new List<BlueprintItemEnchantment>(100);

        public static void ResetGUI() { }
        public static void OnGUI() {
            if (!Main.IsInGame) return;
            UI.Label("Sandal says '".orange() + "Enchantment'".cyan().bold());

            // load blueprints
            if (enchantments == null) {
                BlueprintBrowser.GetBlueprints();
                if (BlueprintBrowser.blueprints == null) return;

                enchantments = new List<BlueprintItemEnchantment>(1500);
                foreach (var bp in BlueprintBrowser.blueprints) {
                    if (bp is BlueprintItemEnchantment) {
                        enchantments.Add(bp as BlueprintItemEnchantment);
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
                float remainingWidth = UI.ummWidth; // TODO - fix remainingWidth; I don't know how that works

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
                        var enchantements = GetEnchantments(item);
                        UI.Label("Target".cyan());
                        using (UI.HorizontalScope(GUI.skin.box, UI.MinHeight(125))) {
                            var rarity = item.Rarity();
                            //Main.Log($"item.Name - {item.Name.ToString().Rarity(rarity)} rating: {item.Blueprint.Rating(item)}");
                            var itemName = item.Blueprint.GetDisplayName();
#if false
                            if (UI.EditableLabel(ref itemName, ref renameState,200, n => item.Name, UI.Width(300))) {
                                
                            }
#else
                            UI.Label(item.Name.bold(), UI.Width(300));
#endif
                            UI.Space(100);
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
                                                UI.ActionButton("Remove", () => RemoveEnchantment(selectedItem, enchant), UI.AutoWidth());
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
                        UI.Space(25);
                    }
                    // Search Field and modifiers
                    using (UI.HorizontalScope()) {
                        UI.ActionTextField(
                            ref settings.searchText,
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

        public static void ItemGUI() {
            var selectedItemEnchantments = selectedItem?.Enchantments.Select(e => e.Blueprint).ToHashSet();
            UI.Div(5);
            for (int i = 0;i < filteredEnchantments.Count;i++) {
                var enchant = filteredEnchantments[i];
                var title = enchant.name.Rarity(enchant.Rarity());
                using (UI.HorizontalScope()) {
                    UI.Space(5);
                    UI.Label(title, UI.Width(400));

                    UI.ActionButton("Add", () => AddClicked(i), UI.Width(160)); // TODO - switch Add/Remove and color

                    if (selectedItemEnchantments != null && selectedItemEnchantments.Contains(enchant)) {
                        UI.ActionButton("Remove", () => RemoveClicked(i), UI.Width(150));
                    }
                    else UI.Space(154);
                    UI.Space(10);
                    if (settings.showAssetIDs) {
                        using (UI.VerticalScope()) {
                            using (UI.HorizontalScope()) {
                                UI.Label(enchant.CollationName().cyan(), UI.Width(250));
                                GUILayout.TextField(enchant.AssetGuid.ToString(), UI.AutoWidth());
                            }
                            if (enchant.Description.Length > 0) UI.Label(enchant.Description.RemoveHtmlTags().green());
                        }
                    }
                    else {
                        UI.Label(enchant.CollationName().cyan(), UI.Width(250));
                        if (enchant.Description.Length > 0) UI.Label(enchant.Description.RemoveHtmlTags().green());
                    }

                }
                UI.Div(5);
            }
        }

        public static void UpdateItems(int index) {
            inventory = Game.Instance.Player.Inventory
                            .Where(item => (int)item.Blueprint.ItemType == index)
                            .OrderByDescending(item => item.Rarity())
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
            var terms = settings.searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

            for (int i = 0;filteredEnchantments.Count < settings.searchLimit && i < enchantments.Count;i++) {
                var enchant = enchantments[i];
                if (enchant.AssetGuid.ToString().Contains(settings.searchText)
                    || enchant.GetType().ToString().Contains(settings.searchText)
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

        public static void AddClicked(int index) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Length) return;
            if (index < 0 || index >= filteredEnchantments.Count) return;

            AddEnchantment(inventory[selectedItemIndex], filteredEnchantments[index]);
        }

        public static void RemoveClicked(int index) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Length) return;
            if (index < 0 || index >= filteredEnchantments.Count) return;

            RemoveEnchantment(inventory[selectedItemIndex], filteredEnchantments[index]);
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
            editedItem = item;
        }

        public static void RemoveEnchantment(ItemEntity item, BlueprintItemEnchantment enchantment) {
            item.RemoveEnchantment(item.GetEnchantment(enchantment));
            editedItem = item;
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
