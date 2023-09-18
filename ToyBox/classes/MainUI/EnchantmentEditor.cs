using DG.Tweening;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static ModKit.UI;
namespace ToyBox.classes.MainUI {
    public static class EnchantmentEditor {
        public static Settings Settings => Main.Settings;

        #region GUI
        public static BlueprintItemWeapon basicSpikeShield = ResourcesLibrary.TryGetBlueprint<BlueprintItemWeapon>("62c90581f9892e9468f0d8229c7321c4"); //StandardWeaponLightShield
        public static Browser<BlueprintItemEnchantment, BlueprintItemEnchantment> EnchantmentBrowser = new(Mod.ModKitSettings.searchAsYouType);

        public static int selectedItemType;
        public static int selectedItemIndex;
        public static int selectedEnchantIndex;
        public static int selectedPageItemIndex;
        public static string itemSearchText = "";
        public static (string, string) renameState = (null, null);
        public static ItemEntity selectedItem = null;
        public static ItemEntity editedItem = null;
        public static string[] ItemTypeNames = null;
        private static List<ItemEntity> inventory;
        private static List<BlueprintItemEnchantment> enchantments;
        private static string collationSearchText;
        private static int searchLimit = 30;
        private static int _currentPage = 1;
        private static int _pageCount => (int)Math.Ceiling((double)inventory?.Count / searchLimit);

        public static void ResetGUI() { }

        public static void OnShowGUI() => UpdateItems();
        public static void OnGUI() {
            if (!Main.IsInGame) return;
            if (ItemTypeNames == null) {
                ItemTypeNames = Enum.GetNames(typeof(ItemsFilter.ItemType)).ToList().Prepend("All").Select(item => item.localize()).ToArray();
                EnchantmentBrowser.DisplayShowAllGUI = false;
                EnchantmentBrowser.doCollation = true;
                EnchantmentBrowser.SortDirection = Browser.sortDirection.Descending;
            }
            Label(("Sandal says '".orange() + "Enchantment'".cyan().bold()).localize());
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
                EnchantmentBrowser.RedoCollation();
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
                    if (VPicker("Ench. Types".localize().cyan(), ref EnchantmentBrowser.collationKey, EnchantmentBrowser.collatedDefinitions?.Keys.ToList() ?? new(), "All".localize(), (s) => s.localize(), ref collationSearchText, Width(175))) {
                        Mod.Debug($"collationKey: {EnchantmentBrowser.collationKey}");
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
                        Label("Limit".localize(), ExpandWidth(false));
                        ActionIntTextField(ref searchLimit, "Search Limit".localize(), (i) => searchLimit = i < 1 ? 1 : i, null, 100.width());
                        if (searchLimit > 1000) { searchLimit = 1000; }
                        if (inventory.Count > searchLimit) {
                            string pageLabel = "Page: ".localize().orange() + _currentPage.ToString().cyan() + " / " + _pageCount.ToString().cyan();
                            25.space();
                            Label(pageLabel, ExpandWidth(false));
                            ActionButton("-", () => {
                                if (_currentPage >= 1) {
                                    if (_currentPage == 1) {
                                        _currentPage = _pageCount;
                                    }
                                    else {
                                        _currentPage -= 1;
                                    }
                                }
                                var offset = Math.Min(inventory.Count, (_currentPage - 1) * searchLimit);
                                selectedItemIndex = offset + selectedPageItemIndex;
                                selectedItem = inventory[selectedItemIndex];
                            }, AutoWidth());
                            ActionButton("+", () => {
                                if (_currentPage > _pageCount) _currentPage = 1;
                                if (_currentPage == _pageCount) {
                                    _currentPage = 1;
                                }
                                else {
                                    _currentPage += 1;
                                }
                                var offset = Math.Min(inventory.Count, (_currentPage - 1) * searchLimit);
                                selectedItemIndex = offset + selectedPageItemIndex;
                                selectedItem = inventory[selectedItemIndex];
                            }, AutoWidth());
                        }
                    }
                    using (HorizontalScope()) {
                        10.space();
                        Toggle("Ratings".localize(), ref Settings.showRatingForEnchantmentInventoryItems, 147.width());
                        10.space();
                        ActionButton("Export".localize(), () => inventory.Export(itemTypeName + ".json"), 100.width());
                        ActionButton("Import".localize(), () => {
                            Game.Instance.Player.Inventory.Import(itemTypeName + ".json");
                            UpdateItems();
                        }, 100.width());
                    }
                    if (inventory.Count > 0) {
                        var offset = Math.Min(inventory.Count, (_currentPage - 1) * searchLimit);
                        var limit = Math.Min(searchLimit, Math.Max(inventory.Count, inventory.Count - searchLimit));
                        ActionSelectionGrid(
                            ref selectedPageItemIndex,
                            inventory.Select(item => item.NameAndOwner(true)).Skip(offset).Take(limit).ToArray(),
                            1,
                            index => {
                                selectedItemIndex = offset + selectedPageItemIndex;
                                selectedItem = inventory[selectedItemIndex];
                            },
                            rarityButtonStyle,
                            Width(375));
                    }
                    else {
                        Label("No Items".localize().grey(), Width(375));
                    }
                }
                remainingWidth -= 400;
                Space(10);
                // Section Column - Main Area
                using (VerticalScope(MinWidth(remainingWidth))) {
                    Label("Import/Export allows you to save and add a list of items to a file based on the type (e.g. Weapon.json). These files live in a new ToyBox folder that in the same folder that contains your saved games ".localize().green());
                    if (selectedItem != null) {
                        var item = selectedItem;
                        //UI.Label("Target".cyan());
                        Div();
                        using (HorizontalScope(GUI.skin.box, MinHeight(125))) {
                            var rarity = item.Rarity();
                            //Main.Log($"item.Name - {item.Name.ToString().Rarity(rarity)} rating: {item.Blueprint.Rating(item)}");
                            Space(25);
                            using (VerticalScope(Width(320))) {
                                Label(item.NameAndOwner(false).bold(), Width(320));
                                25.space();
                                var bp = item.Blueprint;
                                Label("rating: ".localize() + $"{item.Rating().ToString().orange().bold()} (" + "bp".localize() + $":{item.Blueprint.Rating().ToString().orange().bold()})".cyan());
                                using (HorizontalScope()) {
                                    var modifers = bp.Attributes();
#if Wrath
                                    if (item.IsEpic) modifers = modifers.Prepend("epic ");
#endif
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
#if Wrath
                        using (HorizontalScope()) {
                            ActionButton(("Sandal".cyan() + ", yer a Trickster!").localize(), () => {
                                AddTricksterEnchantmentsTier1(item);
                            }, AutoWidth());
                            ActionButton("Gimmie More!".localize().DarkModeRarity(RarityType.Epic), () => {
                                AddTricksterEnchantmentsTier2or3(item, false);
                            }, rarityButtonStyle, AutoWidth());
                            ActionButton("En-chaannt-ment".localize().DarkModeRarity(RarityType.Legendary), () => {
                                AddTricksterEnchantmentsTier2or3(item, true);
                            }, rarityButtonStyle, AutoWidth());
                            using (VerticalScope()) {
                                Label(("Sandal".cyan() + " has discovered the mythic path of Trickster and can reveal hidden secrets in your items".green()).localize());
                                Label("This applies the Trickster Lore Nature Enchantment Bonus at stage 1/2/3 respectively".localize().green());
                            }
                        }
#endif
                        Div();
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
                            name = name.DarkModeRarity(enchantBP.Rarity());
                            using (HorizontalScope()) {
                                Label(name, Width(100));
                                Space(25);
                                Label($"{enchantBP.Rating().ToString().orange().bold()}".cyan(), 30.width());
                                Space(25);
                                Label(entry.Value ? "Custom".localize().yellow() : "Perm".localize().orange(), Width(100));
                                Space(25);
                                ActionButton("Remove".localize(), () => RemoveEnchantment(item, enchant), AutoWidth());
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
            using (HorizontalScope()) {
                Space(-50);
                using (VerticalScope()) {
                    EnchantmentBrowser.OnGUI(enchantments, null, e => e,
                        (BlueprintItemEnchantment blueprint) => $"{BlueprintExtensions.GetTitle(blueprint)} {blueprint.AssetGuid} {blueprint.GetType()}" + (Settings.searchDescriptions ? blueprint.Description.ToString() : ""),
                        (BlueprintItemEnchantment blueprint) => new IComparable[] { blueprint.Rating(), BlueprintExtensions.GetTitle(blueprint) },
                        () => {
                            // Search Field and modifiers
                            using (VerticalScope()) {
                                using (HorizontalScope()) {
                                    var reloadData = false;
                                    Toggle("Show GUIDs".localize(), ref Settings.showAssetIDs);
                                    20.space();
                                    reloadData |= Toggle("Show Internal Names".localize(), ref Settings.showDisplayAndInternalNames);
                                    20.space();
                                    reloadData |= Toggle("Search Descriptions".localize(), ref Settings.searchDescriptions);
                                    if (reloadData) {
                                        EnchantmentBrowser.ResetSearch();
                                    }
                                }
                                using (HorizontalScope()) {
                                    Space(5);
                                    Label("Enchantment".localize().blue(), Width(320));
                                    Space(275);
                                    Label("Rating".localize().blue(), Width(75));
                                    Space(10);
                                    Label("Ench. Type".localize().blue(), Width(140));
                                    Space(130);
                                    Label("Description".localize().blue());

                                }
                            }
                        },
                        (enchant, maybeEnchantment) => {
                            var title = BlueprintExtensions.GetTitle(enchant).DarkModeRarity(enchant.Rarity());
                            using (HorizontalScope()) {
                                Space(5);
                                Label(title, Width(320));
                                if (selectedItem is ItemEntityShield shield) {
                                    using (VerticalScope(Width(260))) {
                                        using (HorizontalScope()) {
                                            ActionButton("+ " + "Armor".localize().orange(), () => AddClicked(enchant), Width(130));
                                            if (shield.ArmorComponent.Enchantments.Any(e => e.Blueprint == enchant))
                                                ActionButton("- " + "Armor".localize().orange(), () => RemoveClicked(enchant), Width(130));
                                            else
                                                Space(130);
                                        }
                                        if (shield.WeaponComponent != null) {
                                            using (HorizontalScope()) {
                                                ActionButton("+ " + "Spikes".localize().orange(), () => AddClicked(enchant, true), Width(130));
                                                if (shield.WeaponComponent.Enchantments.Any(e => e.Blueprint == enchant))
                                                    ActionButton("- " + "Spikes".localize().orange(), () => RemoveClicked(enchant, true), Width(130));
                                                else
                                                    Space(130);
                                            }
                                        }
                                    }
                                }
                                else if (selectedItem is ItemEntityWeapon weapon && weapon?.Second != null) {
                                    using (VerticalScope()) {
                                        using (HorizontalScope()) {
                                            ActionButton("+ " + "Main".localize().orange(), () => AddClicked(enchant), Width(130));
                                            if (weapon.Enchantments.Any(e => e.Blueprint == enchant))
                                                ActionButton("- " + "Main".localize().orange(), () => RemoveClicked(enchant), Width(130));
                                            else
                                                Space(130);
                                        }
                                        using (HorizontalScope()) {
                                            ActionButton("+ " + "Offhand".localize().orange(), () => AddClicked(enchant, true), Width(130));
                                            if (weapon.Second.Enchantments.Any(e => e.Blueprint == enchant))
                                                ActionButton("- " + "Offhand".localize().orange(), () => RemoveClicked(enchant, true), Width(130));
                                            else
                                                Space(130);
                                        }
                                    }
                                }
                                else {
                                    ActionButton("Add".localize(), () => AddClicked(enchant), Width(130));
                                    if (selectedItem?.Enchantments.Any(e => e.Blueprint == enchant) ?? false)
                                        ActionButton("Remove".localize(), () => RemoveClicked(enchant), Width(130));
                                    else
                                        Space(133);
                                }
                                Space(15);
                                Label($"{enchant.Rating()}".yellow(), 75.width()); // ⊙
                                Space(10);
                                var description = enchant.Description.StripHTML().green();
                                if (enchant.Comment?.Length > 0) description = enchant.Comment.orange() + " " + description;
                                if (enchant.Prefix?.Length > 0) description = enchant.Prefix.yellow() + " " + description;
                                if (enchant.Suffix?.Length > 0) description = enchant.Suffix.yellow() + " " + description;
                                Label(enchant.CollationNames().First().Replace("Enchantment", "").cyan(), Width(150));
                                using (VerticalScope()) {
                                    if (Settings.showAssetIDs) ClipboardLabel(enchant.AssetGuid.ToString(), AutoWidth());
                                    using (HorizontalScope()) {
                                        ReflectionTreeView.DetailToggle("", enchant, enchant, 0);
                                        Label(description);
                                    }
                                }
                            }
                        },
                        (enchant, maybeEnchantment) => {
                            ReflectionTreeView.OnDetailGUI(enchant);
                        }, 50, true, true, 100, 300, "", false, bp => bp.CollationNames().Select(n => n.Replace("Enchantment", "")));
                }
            }
        }
        public static void UpdateItems() {
            var selectedItemTypeEnumIndex = selectedItemType - 1;
            var searchText = itemSearchText.ToLower();
            if (Game.Instance?.Player?.Inventory == null) return;
            inventory = (from item in Game.Instance.Player.Inventory
                         where BlueprintExtensions.GetSearchKey(item.Blueprint, true).ToLower().Contains(searchText)
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
            _currentPage = 1;
            selectedItem = selectedItemIndex < inventory.Count ? inventory.ElementAt(selectedItemIndex) : null;
        }
        public static void AddClicked(BlueprintItemEnchantment ench, bool second = false) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Count) return;
            if (ench == null) return;
            var selected = inventory.ElementAt(selectedItemIndex);
            if (selected is ItemEntityShield shield) {
                if (!second)
                    AddEnchantment(shield.ArmorComponent, ench);
                else
                    AddEnchantment(shield.WeaponComponent, ench);
                editedItem = shield;
            }
            else if (second && selected is ItemEntityWeapon weapon) {
                AddEnchantment(weapon.Second, ench);
                editedItem = weapon;
            }
            else {
                AddEnchantment(selected, ench);
                editedItem = selected;
            }
        }
        public static void RemoveClicked(BlueprintItemEnchantment ench, bool second = false) {
            if (selectedItemIndex < 0 || selectedItemIndex >= inventory.Count) return;
            if (ench == null) return;
            var selected = inventory.ElementAt(selectedItemIndex);
            if (selected is ItemEntityShield shield) {
                if (!second)
                    RemoveEnchantment(shield.ArmorComponent, ench);
                else
                    RemoveEnchantment(shield.WeaponComponent, ench);
                editedItem = shield;
            }
            if (second && selected is ItemEntityWeapon weapon) {
                RemoveEnchantment(weapon.Second, ench);
                editedItem = weapon;
            }
            else {
                RemoveEnchantment(selected, ench);
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
#if Wrath
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
#if Wrath
            if (source.Empty<BlueprintItemEnchantment>())
#elif RT
            if (source.DefaultIfEmpty<BlueprintItemEnchantment>())
#endif
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
#endif
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

            if (enhancements.Any()) {
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
