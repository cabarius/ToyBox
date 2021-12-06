using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Designers.Mechanics.Facts;
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
using System.Text.RegularExpressions;
using UnityEngine;

namespace ToyBox.classes.MainUI {
    public static class EnchantmentEditor {
        public static Settings settings => Main.settings;

        #region GUI
        public static BlueprintItemWeapon basicSpikeShield = ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>("62c90581f9892e9468f0d8229c7321c4"); //StandardWeaponLightShield

        public static int selectedItemType;
        public static int selectedItemIndex;
        public static int selectedEnchantIndex;
        public static (string, string) renameState = (null, null);
        public static ItemEntity selectedItem = null;
        public static ItemEntity editedItem = null;
        public static string itemSearchText = "";
        public static string[] ItemTypeNames = Enum.GetNames(typeof(ItemsFilter.ItemType));
        private static List<ItemEntity> inventory;
        private static List<BlueprintItemEnchantment> enchantments;
        private static List<BlueprintItemEnchantment> filteredEnchantments = new();
        public static IEnumerable<IGrouping<string, BlueprintItemEnchantment>> collatedBPs = null;
        private static List<BlueprintItemEnchantment> selectedCollatedEnchantments;
        private static List<string> collationKeys = new();
        private static string collationKey;
        private static string collationSearchText;
        public static int matchCount = 0;

        public static void ResetGUI() { }

        public static void OnShowGUI() => UpdateItems();
        public static void OnGUI() {
            if (!Main.IsInGame) return;
            UI.Label("Sandal says '".orange() + "Enchantment'".cyan().bold());

            // load blueprints
            if (enchantments == null) {
                var blueprints = BlueprintLoader.Shared.GetBlueprints();
                if (blueprints == null) return;

                enchantments = new List<BlueprintItemEnchantment>();
                foreach (var bp in blueprints) {
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
                UpdateItems();
                UpdateSearchResults();
            }

            // Stackable browser
            using (UI.HorizontalScope(UI.Width(350))) {
                var remainingWidth = UI.ummWidth;

                // First column - Type Selection Grid
                using (UI.VerticalScope()) {
                    UI.ActionSelectionGrid(
                        ref selectedItemType,
                        ItemTypeNames,
                        1,
                        index => { selectedItemIndex = index; UpdateItems(); },
                        UI.buttonStyle,
                        UI.Width(175));
                        UI.Space(25);
                        if (UI.VPicker("Ench. Types".cyan(), ref collationKey, collationKeys, "All", (s) => s, ref collationSearchText, UI.Width(175))) {
                        Mod.Debug($"collationKey: {collationKey}");
                        UpdateCollation();
                    }
                }
                var itemTypeName = ItemTypeNames[selectedItemType];
                remainingWidth -= 250;
                UI.Space(10);
                // Second column - Item Selection Grid
                using (UI.VerticalScope(GUI.skin.box)) {
                    UI.ActionTextField(
                        ref itemSearchText,
                        "itemSearchText",
                        (text) => UpdateItems(),
                        () => UpdateItems(),
                        UI.Width(375));
                    using (UI.HorizontalScope()) {
                        UI.ActionButton("Export", () => inventory.Export(itemTypeName + ".json"), UI.Width(160));
                        UI.ActionButton("Import", () => {
                            Game.Instance.Player.Inventory.Import(itemTypeName + ".json");
                            UpdateItems();
                        }, UI.Width(160));
                    }
                    if (inventory.Count > 0) {
                        UI.ActionSelectionGrid(
                            ref selectedItemIndex,
                            inventory.Select(item => item.NameAndOwner()).ToArray(),
                            1,
                            index => selectedItem = inventory[selectedItemIndex],
                            UI.rarityButtonStyle,
                            UI.Width(375));
                    }
                    else {
                        UI.Label("No Items".grey(), UI.Width(375));
                    }
                }
                remainingWidth -= 400;
                UI.Space(10);
                // Section Column - Main Area
                using (UI.VerticalScope(UI.MinWidth(remainingWidth))) {
                    UI.Label("Import/Export allows you to save and add a list of items to a file based on the type (e.g. Weapon.json). These files live in a new ToyBox folder that in the same folder that contains your saved games ".green());
                    if (selectedItem != null) {
                        var item = selectedItem;
                        //UI.Label("Target".cyan());
                        UI.Div();
                        using (UI.HorizontalScope(GUI.skin.box, UI.MinHeight(125))) {
                            var rarity = item.Rarity();
                            //Main.Log($"item.Name - {item.Name.ToString().Rarity(rarity)} rating: {item.Blueprint.Rating(item)}");
                            UI.Space(25);
                            using (UI.VerticalScope(UI.Width(400))) {
                                UI.Label(item.NameAndOwner().bold(), UI.Width(400));
                                var bp = item.Blueprint;
                                using (UI.HorizontalScope()) {
                                    var modifers = bp.Attributes();
                                    if (item.IsEpic) modifers = modifers.Prepend("epic ");
                                    UI.Label(string.Join(" ", modifers).cyan(), UI.AutoWidth());
                                    //if (bp is BlueprintItemWeapon bpW) {
                                    //    if (bpW.IsMagic) UI.Label("magic ".cyan(), UI.AutoWidth());
                                    //    if (bpW.IsNotable) UI.Label("notable ".Rarity(RarityType.Notable), UI.AutoWidth());
                                    //    if (bpW.IsNatural) UI.Label("natural ".grey(), UI.AutoWidth());
                                    //    if (bpW.IsMelee) UI.Label("melee ".grey(), UI.AutoWidth());
                                    //    if (bpW.IsRanged) UI.Label("ranged ".grey(), UI.AutoWidth());
                                    //}
                                    //if (bp is BlueprintItemArmor bpA) {
                                    //    if (bpA.IsMagic) UI.Label("magic ".cyan(), UI.AutoWidth());
                                    //    if (bpA.IsNotable) UI.Label("notable ".Rarity(RarityType.Notable), UI.AutoWidth());
                                    //    if (bpA.IsShield) UI.Label("shield ".grey(), UI.AutoWidth());
                                    //}
                                }
                            }
                            UI.Space(25);
                            if (item is ItemEntityShield shield) {
                                using (UI.VerticalScope()) {
                                    using (UI.HorizontalScope()) {
                                        UI.Label("Shield".orange(), UI.Width(100));
                                        TargetItemGUI(shield.ArmorComponent);
                                    }
                                    UI.Div();
                                    if (shield.WeaponComponent != null) {
                                        using (UI.HorizontalScope()) {
                                            UI.Label("Spikes".orange(), UI.Width(100));
                                            TargetItemGUI(shield.WeaponComponent);
                                        }
                                        UI.ActionButton("Remove ", () => shield.WeaponComponent = null, UI.AutoWidth());
                                    }
                                    else {
                                        var compTitle = shield.Blueprint.WeaponComponent?.name;
                                        compTitle = compTitle != null ? " from " + compTitle.yellow() : "";
                                        UI.ActionButton("Add " + "Spikes".orange() + compTitle, () => shield.WeaponComponent = new ItemEntityWeapon(shield.Blueprint.WeaponComponent ?? basicSpikeShield, shield), UI.AutoWidth());
                                    }
                                }
                            }
                            else if (item is ItemEntityWeapon weapon && weapon.Second != null) {
                                using (UI.VerticalScope()) {
                                    using (UI.HorizontalScope()) {
                                        UI.Label("Main".orange(), UI.Width(100));
                                        TargetItemGUI(weapon);
                                    }
                                    UI.Div();
                                    using (UI.HorizontalScope()) {
                                        UI.Label("2nd".orange(), UI.Width(100));
                                        TargetItemGUI(weapon.Second);
                                    }
                                }
                            }
                            else {
                                TargetItemGUI(item);
                            }
                        }
                        using (UI.HorizontalScope()) {
                            UI.ActionButton("Sandal".cyan() + ", yer a Trickster!", () => {
                                AddTricksterEnchantmentsTier1(item);
                            }, UI.AutoWidth());
                            UI.ActionButton("Gimmie More!".Rarity(RarityType.Epic), () => {
                                AddTricksterEnchantmentsTier2or3(item, false);
                            }, UI.rarityButtonStyle, UI.AutoWidth());
                            UI.ActionButton("En-chaannt-ment".Rarity(RarityType.Legendary), () => {
                                AddTricksterEnchantmentsTier2or3(item, true);
                            }, UI.rarityButtonStyle, UI.AutoWidth());
                            UI.Label("Sandal".cyan() + " has discovered the mythic path of Trickster and can reveal hidden secrets in your items".green());
                        }
                        UI.Div();
                    }
                    // Search Field and modifiers
                    UI.Space(10);
                    using (UI.HorizontalScope()) {
                        UI.ActionTextField(
                            ref settings.searchTextEnchantments,
                            "searchText",
                            (text) => { UpdateSearchResults(); },
                            () => { UpdateSearchResults(); },
                            UI.MinWidth(100), UI.MaxWidth(450));
                        UI.Space(25);
                        UI.Label("Search Limit", UI.AutoWidth());
                        UI.ActionIntTextField(
                            ref settings.searchLimit,
                            "searchLimit",
                            (limit) => { },
                            () => { UpdateSearchResults(); },
                            UI.MinWidth(75), UI.MaxWidth(175));
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
                    UI.Space(10);
                    using (UI.HorizontalScope()) {
                        UI.ActionButton("Search", () => UpdateSearchResults(), UI.AutoWidth());
                        UI.Space(25);
                        if (matchCount > 0 && settings.searchTextEnchantments.Length > 0) {
                            var matchesText = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                            if (matchCount > settings.searchLimit) { matchesText += "Displaying: ".cyan() + $"{settings.searchLimit}".cyan().bold(); }
                            UI.Label(matchesText, UI.ExpandWidth(false));
                        }
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(5);
                        UI.Label("Enchantment".blue(), UI.Width(400));
                        UI.Space(314);
                        UI.Label("Rating".blue(), UI.Width(75));
                        UI.Space(310);
                        UI.Label("Description".blue());

                    }
                    UI.Space(10);
                    UI.Div();
                    // List of enchantments with buttons to add to item
                    EnchantmentsListGUI();
                }
            }
        }

        public static void TargetItemGUI(ItemEntity item) {
            var enchantements = GetEnchantments(item);
            if (enchantements.Count > 0) {
                using (UI.VerticalScope()) {
                    var index = 0;
                    foreach (var entry in enchantements) {
                        if (index++ > 0) UI.Div();
                        var enchant = entry.Key;
                        var enchantBP = enchant.Blueprint;
                        var name = enchantBP.name;
                        if (name != null && name.Length > 0) {
                            name = name.Rarity(enchantBP.Rarity());
                            using (UI.HorizontalScope()) {
                                UI.Label(name, UI.Width(450));
                                UI.Space(25);
                                UI.Label(entry.Value ? "Custom".yellow() : "Perm".orange(), UI.Width(100));
                                UI.Space(25);
                                UI.ActionButton("Remove", () => RemoveEnchantment(item, enchant), UI.AutoWidth());
                                var description = enchantBP.Description;
                                if (description != null) {
                                    UI.Space(25);
                                    UI.Label(description.StripHTML().green());
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
        public static void EnchantmentsListGUI() {
            UI.Div(5);
            var enchantement = selectedCollatedEnchantments ?? filteredEnchantments;
            
            for (var i = 0; i < enchantement.Count; i++) {
                var enchant = enchantement[i];
                var title = enchant.name.Rarity(enchant.Rarity());
               
                using (UI.HorizontalScope()) {
                    UI.Space(5);
                    UI.Label(title, UI.Width(400));
                    if (selectedItem is ItemEntityShield shield) {
                        UI.ActionButton("+ " + "Armor".orange(), () => AddClicked(i), UI.Width(150));
                        if (shield.ArmorComponent.Enchantments.Any(e => e.Blueprint == enchant))
                            UI.ActionButton("- " + "Armor".orange(), () => RemoveClicked(i), UI.Width(150));
                        else
                            UI.Space(154);
                        if (shield.WeaponComponent != null) {
                            UI.ActionButton("+ " + "Spikes".orange(), () => AddClicked(i, true), UI.Width(150));
                            if (shield.WeaponComponent.Enchantments.Any(e => e.Blueprint == enchant))
                                UI.ActionButton("- " + "Spikes".orange(), () => RemoveClicked(i, true), UI.Width(150));
                            else
                                UI.Space(154);
                        }
                    }
                    else if (selectedItem is ItemEntityWeapon weapon && weapon?.Second != null) {
                        UI.ActionButton("+ " + "Main".orange(), () => AddClicked(i), UI.Width(150));
                        if (weapon.Enchantments.Any(e => e.Blueprint == enchant))
                            UI.ActionButton("- " + "Main".orange(), () => RemoveClicked(i), UI.Width(150));
                        else
                            UI.Space(154);
                        UI.ActionButton("+ " + "2nd".orange(), () => AddClicked(i, true), UI.Width(150));
                        if (weapon.Second.Enchantments.Any(e => e.Blueprint == enchant))
                            UI.ActionButton("- " + "2nd".orange(), () => RemoveClicked(i, true), UI.Width(150));
                        else
                            UI.Space(154);
                    }
                    else {
                        UI.ActionButton("Add", () => AddClicked(i), UI.Width(150));
                        if (selectedItem?.Enchantments.Any(e => e.Blueprint == enchant) ?? false)
                            UI.ActionButton("Remove", () => RemoveClicked(i), UI.Width(150));
                        else
                            UI.Space(154);
                    }

                    UI.Space(10);
                    UI.Label($"{enchant.Rating()}".yellow(), 75.width()); // ⊙
                    UI.Space(10);
                    var description = enchant.Description.StripHTML().green();
                    if (enchant.Comment?.Length > 0) description = enchant.Comment.orange() + " " + description;
                    if (enchant.Prefix?.Length > 0) description = enchant.Prefix.yellow() + " " + description;
                    if (enchant.Suffix?.Length > 0) description = enchant.Suffix.yellow() + " " + description;
                    if (settings.showAssetIDs) {
                        using (UI.VerticalScope()) {
                            using (UI.HorizontalScope()) {
                                UI.Label(enchant.CollationNames().First().cyan(), UI.Width(300));
                                GUILayout.TextField(enchant.AssetGuid.ToString(), UI.AutoWidth());
                            }
                            UI.Label(description);
                            
                        }
                    }
                    else {
                        UI.Label(enchant.CollationNames().First().cyan(), UI.Width(300));
                        UI.Label(description);
                    }
                }
                UI.Div();
            }
        }
        public static void UpdateItems() {
            var searchText = itemSearchText.ToLower();
            inventory = (from item in Game.Instance.Player.Inventory
                         where item.Name.ToLower().Contains(searchText) && (int)item.Blueprint.ItemType == selectedItemIndex
                         orderby item.Rarity() descending, item.Name
                         select item
                         ).ToList();
            if (editedItem != null) {
                selectedItemIndex = inventory.IndexOf(editedItem);
                editedItem = null;
            }
            if (selectedItemIndex >= inventory.Count || selectedItemIndex < 0) {
                selectedItemIndex = 0;
            }
            selectedItem = selectedItemIndex < inventory.Count ? inventory.ElementAt(selectedItemIndex) : null;
        }
        public static void UpdateSearchResults() {
            filteredEnchantments.Clear();
            editedItem = null;
            var terms = settings.searchTextEnchantments.Split(' ').Select(s => s.ToLower()).ToHashSet();

            for (var i = 0; i < enchantments.Count; i++) {
                var enchant = enchantments[i];
                if (enchant.AssetGuid.ToString().Contains(settings.searchTextEnchantments)
                    || enchant.GetType().ToString().Contains(settings.searchTextEnchantments)
                    ) {
                    filteredEnchantments.Add(enchant);
                }
                else {
                    var name = enchant.name;
                    var displayName = enchant.GetDisplayName();
                    var description = enchant.Description ?? "";
                    description = description.StripHTML();
                    if (terms.All(term => name.Matches( term))
                        || terms.All(term => displayName.Matches(term))
                        || settings.searchesDescriptions && terms.All(term => description.Matches(term))
                        ) {
                        filteredEnchantments.Add(enchant);
                    }
                }
            }
            matchCount = filteredEnchantments.Count();
            var filtered = from bp in filteredEnchantments
                           orderby bp.Rating() descending, bp.name
                           select bp;
            //.ThenByDescending(bp => bp.IdentifyDC)
            collatedBPs = from bp in filtered
                          from key in bp.CollationNames().Select(n => n.Replace("Enchantment", ""))
                          group bp by key into g
                          orderby g.Key.LongSortKey(), g.Key
                          select g;
            _ = collatedBPs.Count();
            var keys = collatedBPs.ToList().Select(cbp => cbp.Key).ToList();
            collationKeys = new List<string> { };
            collationKeys.AddRange(keys);
            filteredEnchantments = filtered.Take(settings.searchLimit).ToList();
            UpdateCollation();
        }
        public static void UpdateCollation() {
            if (collationKey == null)
                selectedCollatedEnchantments = null;
            else
                foreach (var group in collatedBPs) {
                    Mod.Debug($"group: {group.Key}");
                    if (group.Key == collationKey) {
                        matchCount = group.Count();
                        selectedCollatedEnchantments = group.ToList();
                    }
                }
        }
        public static void AddClicked(int index, bool second = false) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Count) return;
            if (index < 0 || index >= filteredEnchantments.Count) return;
            var selected = inventory.ElementAt(selectedItemIndex);
            if (selected is ItemEntityShield shield) {
                if (!second)
                    AddEnchantment(shield.ArmorComponent, filteredEnchantments[index]);
                else
                    AddEnchantment(shield.WeaponComponent, filteredEnchantments[index]);
                editedItem = shield;
            }
            else if (second && selected is ItemEntityWeapon weapon) {
                AddEnchantment(weapon.Second, filteredEnchantments[index]);
                editedItem = weapon;
            }
            else {
                AddEnchantment(selected, filteredEnchantments[index]);
                editedItem = selected;
            }
        }
        public static void RemoveClicked(int index, bool second = false) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Count) return;
            if (index < 0 || index >= filteredEnchantments.Count) return;
            var selected = inventory.ElementAt(selectedItemIndex);
            if (selected is ItemEntityShield shield) {
                if (!second)
                    RemoveEnchantment(shield.ArmorComponent, filteredEnchantments[index]);
                else
                    RemoveEnchantment(shield.WeaponComponent, filteredEnchantments[index]);
                editedItem = shield;
            }
            if (second && selected is ItemEntityWeapon weapon) {
                RemoveEnchantment(weapon.Second, filteredEnchantments[index]);
                editedItem = weapon;
            }
            else {
                RemoveEnchantment(selected, filteredEnchantments[index]);
                editedItem = selected;
            }
        }

        #endregion

        #region Code
        public static void AddEnchantment(ItemEntity item, BlueprintItemEnchantment enchantment, Rounds? duration = null) {
            if (item?.m_Enchantments == null)
                Mod.Trace("item.m_Enchantments is null");

            var fake_context = new MechanicsContext(default); // if context is null, items may stack which could cause bugs

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



        public static void AddTricksterEnchantmentsTier1(ItemEntity item) {
            var tricksterKnowledgeArcanaTier1 = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c7bb946de7454df4380c489a8350ba38");
            var tricksterTier1Toy = tricksterKnowledgeArcanaTier1.GetComponent<TricksterArcanaBetterEnhancements>();
            var fake_context = new MechanicsContext(default); // if context is null, items may stack which could cause bugs

            var itemEnchantmentList = new List<ItemEnchantment>();
            foreach (var enchantment in item.Enchantments)
                itemEnchantmentList.Add(enchantment);
            if (!item.Enchantments.Any<ItemEnchantment>((Func<ItemEnchantment, bool>)(p => ((IList<BlueprintItemEnchantmentReference>)tricksterTier1Toy.EnhancementEnchantments).Any<BlueprintItemEnchantmentReference>((Func<BlueprintItemEnchantmentReference, bool>)(param => param.Get() == p.Blueprint)))))
                return;
            foreach (var itemEnchantment in itemEnchantmentList) {
                var enchantment = itemEnchantment;
                if (!tricksterTier1Toy.BestEnchantments.Any<BlueprintItemEnchantmentReference>((Func<BlueprintItemEnchantmentReference, bool>)(p => p.Get() == enchantment.Blueprint)) && ((IList<BlueprintItemEnchantmentReference>)tricksterTier1Toy.EnhancementEnchantments).Any<BlueprintItemEnchantmentReference>((Func<BlueprintItemEnchantmentReference, bool>)(p => p.Get() == enchantment.Blueprint))) {
                    var index = ((IEnumerable<BlueprintItemEnchantmentReference>)tricksterTier1Toy.EnhancementEnchantments).FindIndex<BlueprintItemEnchantmentReference>((Func<BlueprintItemEnchantmentReference, bool>)(p => p.Get() == enchantment.Blueprint));
                    if (tricksterTier1Toy.EnhancementEnchantments.Length > index + 1) {
                        item.RemoveEnchantment(enchantment);
                        item.AddEnchantment(tricksterTier1Toy.EnhancementEnchantments[index + 1].Get(), fake_context);
                    }
                }
            }
        }
        public static void AddTricksterEnchantmentsTier2or3(ItemEntity item, bool isTier3) {
            var tricksterKnowledgeArcanaBP = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(isTier3 ? "5e26c673173e423881e318d2f0ae84f0" : "7bbd9f681440a294382b527a554e419d");
            var tricksterToy = tricksterKnowledgeArcanaBP.GetComponent<TricksterArcanaAdditionalEnchantments>();
            var fake_context = new MechanicsContext(default); // if context is null, items may stack which could cause bugs

            var source = new List<BlueprintItemEnchantment>();
            foreach (var commonEnchantment in tricksterToy.CommonEnchantments)
                source.Add((BlueprintItemEnchantment)(BlueprintReference<BlueprintItemEnchantment>)commonEnchantment);
            if (item is ItemEntityWeapon || item is ItemEntityShield) {
                foreach (var weaponEnchantment in tricksterToy.WeaponEnchantments)
                    source.Add((BlueprintItemEnchantment)(BlueprintWeaponEnchantment)(BlueprintReference<BlueprintWeaponEnchantment>)weaponEnchantment);
            }
            if (item is ItemEntityArmor || item is ItemEntityShield) {
                foreach (var armorEnchantment in tricksterToy.ArmorEnchantments)
                    source.Add((BlueprintItemEnchantment)(BlueprintArmorEnchantment)(BlueprintReference<BlueprintArmorEnchantment>)armorEnchantment);
            }
            foreach (var enchantment in item.Enchantments)
                source.Remove(enchantment.Blueprint);
            if (source.Empty<BlueprintItemEnchantment>())
                return;
            var blueprint = source.ToList<BlueprintItemEnchantment>().Random<BlueprintItemEnchantment>();
            var itemEntityShield = item as ItemEntityShield;
            switch (blueprint) {
                case BlueprintWeaponEnchantment _ when itemEntityShield != null:
                    var weaponComponent = itemEntityShield.WeaponComponent;
                    if (weaponComponent == null)
                        break;
                    weaponComponent.AddEnchantment(blueprint, fake_context);
                    break;
                case BlueprintArmorEnchantment _ when itemEntityShield != null:
                    itemEntityShield.ArmorComponent.AddEnchantment(blueprint, fake_context);
                    break;
                default:
                    item.AddEnchantment(blueprint, fake_context);
                    break;
            }
        }
        /// <summary>definitely not useless</summary>
        /// <returns>Key is ItemEnchantments of given item. Value is true, if it is a temporary enchantment.</returns>
        public static Dictionary<ItemEnchantment, bool> GetEnchantments(ItemEntity item) {
            Dictionary<ItemEnchantment, bool> enchantments = new();
            if (item == null) return enchantments;
            var base_enchantments = item.Blueprint.Enchantments;
            foreach (var enchantment in item.Enchantments) {
                enchantments.Add(enchantment, !base_enchantments.Contains(enchantment.Blueprint));
            }
            return enchantments;
        }

        // currently nonfunctional until Nordic gets his patch working

        //public static int CalcCost(ItemEntity item, BlueprintItemEnchantment enchantment) {
        //    if (item.Blueprint is BlueprintItemWeapon || item.Blueprint is BlueprintItemArmor || item.Blueprint is BlueprintItemShield) {
        //        int currentBonus = GetEffectiveBonus(item);

        //        if (currentBonus + enchantment.EnchantmentCost > 10) {
        //            return -1;
        //        }

        //        long currentPrice = item.Blueprint.SellPrice;
        //        long basePrice;
        //        if (item.Blueprint is BlueprintItemArmor) {
        //            basePrice = currentPrice - (1000 * currentBonus * currentBonus);   // get the price of the item without its bonuses
        //        } else if (item.Blueprint is BlueprintItemWeapon) {

        //        } 
        //    }

        //    return -1;
        //}

        /// <summary>
        /// Makes getting the effective bonus of an item more readable
        /// </summary>
        /// <returns>Total effective bonus of all permanent enchantments on the item; 0 if none</returns>
        public static int GetEffectiveBonus(ItemEntity item) {
            if (item == null) return 0;

            return item.Enchantments.Sum((enchantment) => enchantment.Blueprint.EnchantmentCost);
        }

        /// <summary>
        /// Gives the current enhancement bonus of the item
        /// </summary>
        /// <returns></returns>
        public static int CurrentEnhancement(ItemEntity item) {
            if (item == null) return 0;

            var enhanceCheck = new Regex(@"Enhancement\d$");
            var enhancements = new int[20];

            foreach (var enchant in item.Blueprint.Enchantments) {
                if (enhanceCheck.IsMatch(enchant.Name)) {
                    try {
                        enhancements.Append(int.Parse(enchant.name.Substring(11)));
                    }
                    catch { // catches any edge cases where the name is something like "Enhancement3hop" and just ignores those
                        continue;
                    }
                }
            }

            if (!enhancements.Empty()) {
                return enhancements.Max();
            }

            return 0;
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
