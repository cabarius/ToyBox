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
using static ModKit.UI;
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
        public static string[] ItemTypeNames = null;
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
            if (ItemTypeNames == null) 
                ItemTypeNames =  Enum.GetNames(typeof(ItemsFilter.ItemType)).ToList().Prepend("All").ToArray();
            Label("Sandal says '".orange() + "Enchantment'".cyan().bold());
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
            using (HorizontalScope(Width(350))) {
                var remainingWidth = ummWidth;

                // First column - Type Selection Grid
                using (VerticalScope()) {
                    ActionSelectionGrid(
                        ref selectedItemType,
                        ItemTypeNames,
                        1,
                        index => { 
                            selectedItemIndex = index;
                            UpdateItems(); 
                        },
                        buttonStyle,
                        Width(175));
                    Space(25);
                        if (VPicker("Ench. Types".cyan(), ref collationKey, collationKeys, "All", (s) => s, ref collationSearchText, Width(175))) {
                        Mod.Debug($"collationKey: {collationKey}");
                        UpdateCollation();
                    }
                }
                var itemTypeName = ItemTypeNames[selectedItemType];
                remainingWidth -= 250;
                Space(10);
                // Second column - Item Selection Grid
                using (VerticalScope(GUI.skin.box)) {
                    ActionTextField(
                        ref itemSearchText,
                        "itemSearchText",
                        (text) => UpdateItems(),
                        () => UpdateItems(),
                        Width(375));
                    using (HorizontalScope()) {
                        10.space();
                        Toggle("Ratings", ref settings.showRatingForEnchantmentInventoryItems, 147.width());
                        10.space();
                        ActionButton("Export", () => inventory.Export(itemTypeName + ".json"), 100.width());
                        ActionButton("Import", () => {
                            Game.Instance.Player.Inventory.Import(itemTypeName + ".json");
                            UpdateItems();
                        }, 100.width());
                    }
                    if (inventory.Count > 0) {
                        ActionSelectionGrid(
                            ref selectedItemIndex,
                            inventory.Select(item => item.NameAndOwner()).ToArray(),
                            1,
                            index => selectedItem = inventory[selectedItemIndex],
                            rarityButtonStyle,
                            Width(375));
                    }
                    else {
                        Label("No Items".grey(), Width(375));
                    }
                }
                remainingWidth -= 400;
                Space(10);
                // Section Column - Main Area
                using (VerticalScope(MinWidth(remainingWidth))) {
                    Label("Import/Export allows you to save and add a list of items to a file based on the type (e.g. Weapon.json). These files live in a new ToyBox folder that in the same folder that contains your saved games ".green());
                    if (selectedItem != null) {
                        var item = selectedItem;
                        //UI.Label("Target".cyan());
                        Div();
                        using (HorizontalScope(GUI.skin.box, MinHeight(125))) {
                            var rarity = item.Rarity();
                            //Main.Log($"item.Name - {item.Name.ToString().Rarity(rarity)} rating: {item.Blueprint.Rating(item)}");
                            Space(25);
                            using (VerticalScope(Width(400))) {
                                Label(item.NameAndOwner(false).bold(), Width(400));
                                25.space();
                                var bp = item.Blueprint;
                                Label($"rating: {item.Rating().ToString().orange().bold()} (bp:{item.Blueprint.Rating().ToString().orange().bold()})".cyan());
                                using (HorizontalScope()) {
                                    var modifers = bp.Attributes();
                                    if (item.IsEpic) modifers = modifers.Prepend("epic ");
                                    Label(string.Join(" ", modifers).cyan(), AutoWidth());
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
                            Space(25);
                            if (item is ItemEntityShield shield) {
                                using (VerticalScope()) {
                                    using (HorizontalScope()) {
                                        Label("Shield".orange(), Width(100));
                                        TargetItemGUI(shield.ArmorComponent);
                                    }
                                    Div();
                                    if (shield.WeaponComponent != null) {
                                        using (HorizontalScope()) {
                                            Label("Spikes".orange(), Width(100));
                                            TargetItemGUI(shield.WeaponComponent);
                                        }
                                        ActionButton("Remove ", () => shield.WeaponComponent = null, AutoWidth());
                                    }
                                    else {
                                        var compTitle = shield.Blueprint.WeaponComponent?.name;
                                        compTitle = compTitle != null ? " from " + compTitle.yellow() : "";
                                        ActionButton("Add " + "Spikes".orange() + compTitle, () => shield.WeaponComponent = new ItemEntityWeapon(shield.Blueprint.WeaponComponent ?? basicSpikeShield, shield), AutoWidth());
                                    }
                                }
                            }
                            else if (item is ItemEntityWeapon weapon && weapon.Second != null) {
                                using (VerticalScope()) {
                                    using (HorizontalScope()) {
                                        Label("Main".orange(), Width(100));
                                        TargetItemGUI(weapon);
                                    }
                                    Div();
                                    using (HorizontalScope()) {
                                        Label("2nd".orange(), Width(100));
                                        TargetItemGUI(weapon.Second);
                                    }
                                }
                            }
                            else {
                                TargetItemGUI(item);
                            }
                        }
                        using (HorizontalScope()) {
                            ActionButton("Sandal".cyan() + ", yer a Trickster!", () => {
                                AddTricksterEnchantmentsTier1(item);
                            }, AutoWidth());
                            ActionButton("Gimmie More!".Rarity(RarityType.Epic), () => {
                                AddTricksterEnchantmentsTier2or3(item, false);
                            }, rarityButtonStyle, AutoWidth());
                            ActionButton("En-chaannt-ment".Rarity(RarityType.Legendary), () => {
                                AddTricksterEnchantmentsTier2or3(item, true);
                            }, rarityButtonStyle, AutoWidth());
                            Label("Sandal".cyan() + " has discovered the mythic path of Trickster and can reveal hidden secrets in your items".green());
                        }
                        Div();
                    }
                    // Search Field and modifiers
                    Space(10);
                    using (HorizontalScope()) {
                        ActionTextField(
                            ref settings.searchTextEnchantments,
                            "searchText",
                            (text) => { UpdateSearchResults(); },
                            () => { UpdateSearchResults(); },
                            MinWidth(100), MaxWidth(450));
                        Space(25);
                        Label("Search Limit", AutoWidth());
                        ActionIntTextField(
                            ref settings.searchLimit,
                            "searchLimit",
                            (limit) => { },
                            () => { UpdateSearchResults(); },
                            MinWidth(75), MaxWidth(175));
                        if (settings.searchLimit > 1000) { settings.searchLimit = 1000; }
                        Space(25);
                        if (Toggle("Search Descriptions", ref settings.searchesDescriptions)) UpdateSearchResults();
                        Space(25);
                        Toggle("Show GUIDs", ref settings.showAssetIDs);
                        Space(25);
                        Toggle("Components", ref settings.showComponents);
                        //UI.Space(25);
                        //UI.Toggle("Elements", ref settings.showElements);
                    }
                    Space(10);
                    using (HorizontalScope()) {
                        ActionButton("Search", () => UpdateSearchResults(), AutoWidth());
                        Space(25);
                        if (matchCount > 0 && settings.searchTextEnchantments.Length > 0) {
                            var matchesText = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                            if (matchCount > settings.searchLimit) { matchesText += "Displaying: ".cyan() + $"{settings.searchLimit}".cyan().bold(); }
                            Label(matchesText, ExpandWidth(false));
                        }
                    }
                    using (HorizontalScope()) {
                        Space(5);
                        Label("Enchantment".blue(), Width(400));
                        Space(314);
                        Label("Rating".blue(), Width(75));
                        Space(310);
                        Label("Description".blue());

                    }
                    Space(10);
                    Div();
                    // List of enchantments with buttons to add to item
                    EnchantmentsListGUI();
                }
            }
        }

        public static void TargetItemGUI(ItemEntity item) {
            var enchantements = GetEnchantments(item);
            if (enchantements.Count > 0) {
                using (VerticalScope()) {
                    var index = 0;
                    foreach (var entry in enchantements) {
                        if (index++ > 0) Div();
                        var enchant = entry.Key;
                        var enchantBP = enchant.Blueprint;
                        var name = enchantBP.name;
                        if (name != null && name.Length > 0) {
                            name = name.Rarity(enchantBP.Rarity());
                            using (HorizontalScope()) {
                                Label(name, Width(450));
                                Space(25);
                                Label($"{(enchantBP.Rating()).ToString().orange().bold()}".cyan(), 50.width());
                                Space(25);
                                Label(entry.Value ? "Custom".yellow() : "Perm".orange(), Width(100));
                                Space(25);
                                ActionButton("Remove", () => RemoveEnchantment(item, enchant), AutoWidth());
                                var description = enchantBP.Description;
                                if (description != null) {
                                    Space(25);
                                    Label(description.StripHTML().green());
                                }
                            }
                        }
                    }
                }
            }
            else {
                Label("No Enchantments".orange());
            }
        }
        public static void EnchantmentsListGUI() {
            Div(5);
            var enchantements = selectedCollatedEnchantments ?? filteredEnchantments;
            
            for (var i = 0; i < enchantements.Count; i++) {
                var enchant = enchantements[i];
                var title = enchant.name.Rarity(enchant.Rarity());
               
                using (HorizontalScope()) {
                    Space(5);
                    Label(title, Width(400));
                    if (selectedItem is ItemEntityShield shield) {
                        ActionButton("+ " + "Armor".orange(), () => AddClicked(i), Width(150));
                        if (shield.ArmorComponent.Enchantments.Any(e => e.Blueprint == enchant))
                            ActionButton("- " + "Armor".orange(), () => RemoveClicked(i), Width(150));
                        else
                            Space(154);
                        if (shield.WeaponComponent != null) {
                            ActionButton("+ " + "Spikes".orange(), () => AddClicked(i, true), Width(150));
                            if (shield.WeaponComponent.Enchantments.Any(e => e.Blueprint == enchant))
                                ActionButton("- " + "Spikes".orange(), () => RemoveClicked(i, true), Width(150));
                            else
                                Space(154);
                        }
                    }
                    else if (selectedItem is ItemEntityWeapon weapon && weapon?.Second != null) {
                        ActionButton("+ " + "Main".orange(), () => AddClicked(i), Width(150));
                        if (weapon.Enchantments.Any(e => e.Blueprint == enchant))
                            ActionButton("- " + "Main".orange(), () => RemoveClicked(i), Width(150));
                        else
                            Space(154);
                        ActionButton("+ " + "2nd".orange(), () => AddClicked(i, true), Width(150));
                        if (weapon.Second.Enchantments.Any(e => e.Blueprint == enchant))
                            ActionButton("- " + "2nd".orange(), () => RemoveClicked(i, true), Width(150));
                        else
                            Space(154);
                    }
                    else {
                        ActionButton("Add", () => AddClicked(i), Width(150));
                        if (selectedItem?.Enchantments.Any(e => e.Blueprint == enchant) ?? false)
                            ActionButton("Remove", () => RemoveClicked(i), Width(150));
                        else
                            Space(154);
                    }

                    Space(10);
                    Label($"{enchant.Rating()}".yellow(), 75.width()); // ⊙
                    Space(10);
                    var description = enchant.Description.StripHTML().green();
                    if (enchant.Comment?.Length > 0) description = enchant.Comment.orange() + " " + description;
                    if (enchant.Prefix?.Length > 0) description = enchant.Prefix.yellow() + " " + description;
                    if (enchant.Suffix?.Length > 0) description = enchant.Suffix.yellow() + " " + description;
                    if (settings.showAssetIDs) {
                        using (VerticalScope()) {
                            using (HorizontalScope()) {
                                Label(enchant.CollationNames().First().cyan(), Width(300));
                                GUILayout.TextField(enchant.AssetGuid.ToString(), AutoWidth());
                            }
                            Label(description);
                            
                        }
                    }
                    else {
                        Label(enchant.CollationNames().First().cyan(), Width(300));
                        Label(description);
                    }
                }
                Div();
            }
        }
        public static void UpdateItems() {
            var selectedItemTypeEnumIndex = selectedItemType - 1;
            var searchText = itemSearchText.ToLower();
            inventory = (from item in Game.Instance.Player.Inventory
                         where item.Name.ToLower().Contains(searchText) 
                            && (selectedItemType == 0
                                || (int)item.Blueprint.ItemType == selectedItemTypeEnumIndex
                                )
                         orderby item.Rating() descending, item.Name
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
            var enchantements = selectedCollatedEnchantments ?? filteredEnchantments;
            var selected = inventory.ElementAt(selectedItemIndex);
            if (selected is ItemEntityShield shield) {
                if (!second)
                    AddEnchantment(shield.ArmorComponent, enchantements[index]);
                else
                    AddEnchantment(shield.WeaponComponent, enchantements[index]);
                editedItem = shield;
            }
            else if (second && selected is ItemEntityWeapon weapon) {
                AddEnchantment(weapon.Second, enchantements[index]);
                editedItem = weapon;
            }
            else {
                AddEnchantment(selected, enchantements[index]);
                editedItem = selected;
            }
        }
        public static void RemoveClicked(int index, bool second = false) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Count) return;
            if (index < 0 || index >= filteredEnchantments.Count) return;
            var enchantements = selectedCollatedEnchantments ?? filteredEnchantments;
            var selected = inventory.ElementAt(selectedItemIndex);
            if (selected is ItemEntityShield shield) {
                if (!second)
                    RemoveEnchantment(shield.ArmorComponent, enchantements[index]);
                else
                    RemoveEnchantment(shield.WeaponComponent, enchantements[index]);
                editedItem = shield;
            }
            if (second && selected is ItemEntityWeapon weapon) {
                RemoveEnchantment(weapon.Second, enchantements[index]);
                editedItem = weapon;
            }
            else {
                RemoveEnchantment(selected, enchantements[index]);
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
