// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.AreaLogic;
using Kingmaker.Armies;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Craft;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.Cutscenes;
using ModKit;

namespace ToyBox {
    public class BlueprintBrowser {
        public static Settings settings { get { return Main.settings; } }

        public static IEnumerable<SimpleBlueprint> filteredBPs = null;
        public static IEnumerable<IGrouping<String, SimpleBlueprint>> collatedBPs = null;
        public static IEnumerable<SimpleBlueprint> selectedCollatedBPs = null;
        public static List<String> collationKeys = null;
        public static int selectedCollationIndex = 0;
        static bool firstSearch = true;
        public static String[] filteredBPNames = null;
        public static int matchCount = 0;
        public static String parameter = "";
        static int selectedBlueprintIndex = -1;
        static SimpleBlueprint selectedBlueprint = null;

        static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintFact>("Facts", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintFeature>("Features", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAbility>("Abilities", null, bp => bp.Type.ToString()),
            new NamedTypeFilter<BlueprintAbility>("Actions", null, bp => bp.ActionType.ToString()),
            new NamedTypeFilter<BlueprintAbility>("Spells", bp => bp.IsSpell, bp => bp.School.ToString()),
            new NamedTypeFilter<BlueprintSpellbook>("Spellbooks", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintSpellbook>("Class SBs", null, bp => bp.CharacterClass.Name.ToString()),
            new NamedTypeFilter<BlueprintBuff>("Buffs", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintItem>("Item", null,  (bp) => {
                if (bp.m_NonIdentifiedNameText?.ToString().Length > 0) return bp.m_NonIdentifiedNameText;
                return bp.ItemType.ToString();
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equipment", null, (bp) => bp.ItemType.ToString()),
            new NamedTypeFilter<BlueprintItemWeapon>("Weapons", null, (bp) => {
                var type = bp.Type;
                var category = type?.Category;
                if (category != null) return category.ToString();
                if (type != null) return type.NameSafe();
                return "?";
                }),
            new NamedTypeFilter<BlueprintItemEquipmentUsable>("Usable", null, bp => bp.SubtypeName),
            new NamedTypeFilter<BlueprintIngredient>("Ingredient", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintUnit>("Units", null, bp => bp.Type?.Name ?? bp.Race?.Name ?? "?"),
            new NamedTypeFilter<BlueprintRace>("Races"),
            new NamedTypeFilter<BlueprintArea>("Areas", null, bp => bp.CollationName()),
            //new NamedTypeFilter<BlueprintAreaPart>("Area Parts", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAreaEnterPoint>("Area Entry", null, bp => bp.m_Area.NameSafe()),
            //new NamedTypeFilter<BlueprintAreaEnterPoint>("AreaEntry ", null, bp => bp.m_Tooltip.ToString()),
            new NamedTypeFilter<BlueprintGlobalMapPoint>("Map Points", null, bp => bp.GlobalMapZone.ToString()),
            new NamedTypeFilter<BlueprintGlobalMap>("Global Map"),
            new NamedTypeFilter<Cutscene>("Cut Scenes", null, bp => bp.Priority.ToString()),
            new NamedTypeFilter<BlueprintEtude>("Etudes", null, bp => bp.Parent?.GetBlueprint().NameSafe() ?? ""),
            new NamedTypeFilter<BlueprintFeatureSelection>("Feature Select"),
            new NamedTypeFilter<BlueprintArmyPreset>("Armies", null, bp => bp.GetType().ToString()),
            new NamedTypeFilter<BlueprintQuest>("Quests", null, bp => bp.GetFactType()?.ToString()),
            //new NamedTypeFilter<SimpleBlueprint>("In Memory", null, bp => bp.CollationName(), () => ResourcesLibrary.s_LoadedBlueprints.Values.Where(bp => bp != null)),

        };

        public static NamedTypeFilter selectedTypeFilter = null;

        public static IEnumerable<SimpleBlueprint> blueprints = null;
        public static IEnumerable<SimpleBlueprint> GetBlueprints() {
            if (blueprints == null) {
#if false
                if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints != null) {

                }
#else
                if (BlueprintLoader.Shared.LoadInProgress()) { return null; }
                else {
                    Logger.Log($"calling BlueprintLoader.Load");
                    BlueprintLoader.Shared.Load((bps) => {
                        blueprints = bps;
                        UpdateSearchResults();
                        Logger.Log($"success got {bps.Count()} bluerints");
                    });
                    return null;
                }
#endif
            }
            return blueprints;
        }

        public static void ResetSearch() {
            filteredBPs = null;
            filteredBPNames = null;
            collatedBPs = null;
            BlueprintListUI.needsLayout = true;
        }

        public static void ResetGUI() {
            ResetSearch();
            settings.selectedBPTypeFilter = 1;
        }
        public static void UpdateSearchResults() {
            if (blueprints == null) return;
            selectedCollationIndex = 0;
            selectedBlueprint = null;
            selectedBlueprintIndex = -1;
            selectedCollatedBPs = null;
            BlueprintListUI.needsLayout = true;
            if (settings.searchText.Trim().Length == 0) {
                ResetSearch();
            }
            var terms = settings.searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            selectedTypeFilter = blueprintTypeFilters[settings.selectedBPTypeFilter];
            var selectedType = selectedTypeFilter.type;
            IEnumerable<SimpleBlueprint> bps = null;
            if (selectedTypeFilter.blueprintSource != null) bps = selectedTypeFilter.blueprintSource();
            else bps = BlueprintExensions.BlueprintsOfType(selectedType).Where((bp) => selectedTypeFilter.filter(bp));
            var filtered = new List<SimpleBlueprint>();
            foreach (SimpleBlueprint blueprint in bps) {
                var name = blueprint.name.ToLower();
                if (terms.All(term => name.Contains(term))) {
                    filtered.Add(blueprint);
                }
            }
            filteredBPs = filtered.OrderBy(bp => bp.name);
            matchCount = filtered.Count();
            if (selectedTypeFilter.collator != null) {
                collatedBPs = filtered.GroupBy(selectedTypeFilter.collator).OrderBy(bp => bp.Key);
                // I could do something like this but I will leave it up to the UI when a collation is selected.
                // GetItems().GroupBy(g => g.Type).Select(s => new { Type = s.Key, LastTen = s.Take(10).ToList() });
                collationKeys = new List<String>() { "All" };
                collationKeys = collationKeys.Concat(collatedBPs.Select(cbp => cbp.Key)).ToList();
            }
            filteredBPs = filteredBPs.Take(settings.searchLimit).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            firstSearch = false;
        }
        public static IEnumerable OnGUI() {
            // Stackable browser
            using (UI.HorizontalScope(UI.Width(450))) {
                // First column - Type Selection Grid
                using (UI.VerticalScope(GUI.skin.box)) {
                    UI.ActionSelectionGrid(ref settings.selectedBPTypeFilter,
                        blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                        1,
                        (selected) => { UpdateSearchResults(); },
                        UI.buttonStyle,
                        UI.Width(200));
                }
                bool collationChanged = false;
                if (collatedBPs != null) {
                    using (UI.VerticalScope(GUI.skin.box)) {
                        UI.ActionSelectionGrid(ref selectedCollationIndex, collationKeys.ToArray(),
                            1,
                            (selected) => { collationChanged = true; BlueprintListUI.needsLayout = true; },
                            UI.buttonStyle,
                            UI.Width(200));
                    }
                }
                // Section Column  - Main Area
                float remainingWidth = UI.ummWidth - 325;
                using (UI.VerticalScope(UI.Width(remainingWidth))) {
                    // Search Field and modifiers
                    using (UI.HorizontalScope()) {
                        UI.ActionTextField(
                            ref settings.searchText,
                            "searhText",
                            (text) => { },
                            () => { UpdateSearchResults(); },
                            UI.MaxWidth(400));
                        UI.Label("Limit", UI.Width(150));
                        UI.ActionIntTextField(
                            ref settings.searchLimit,
                            "searchLimit",
                            (limit) => { },
                            () => { UpdateSearchResults(); },
                            UI.Width(150));
                        if (settings.searchLimit > 1000) { settings.searchLimit = 1000; }
                        UI.Space(25);
                        UI.Toggle("Show GUIDs", ref settings.showAssetIDs);
                        UI.Space(25);
                        UI.Toggle("Components", ref settings.showComponents);
                        UI.Space(25);
                        UI.Toggle("Elements", ref settings.showElements);
                        UI.Space(25);
                        UI.Toggle("Dividers", ref settings.showDivisions);
                    }
                    // Search Button and Results Summary
                    using (UI.HorizontalScope()) {
                        UI.ActionButton("Search", () => {
                            UpdateSearchResults();
                        }, UI.AutoWidth());
                        UI.Space(25);
                        if (firstSearch) {
                            UI.Label("please note the first search may take a few seconds.".green(), UI.AutoWidth());
                        }
                        else if (matchCount > 0) {
                            String title = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                            if (matchCount > settings.searchLimit) { title += " => ".cyan() + $"{settings.searchLimit}".cyan().bold(); }
                            UI.Label(title, UI.ExpandWidth(false));
                        }
                        UI.Space(50);
                        UI.Label($"".green(), UI.AutoWidth());
                    }
                    UI.Space(10);

                    if (filteredBPs != null) {
                        CharacterPicker.OnGUI();
                        UnitReference selected = CharacterPicker.GetSelectedCharacter();
                        var bps = filteredBPs;
                        if (selectedCollationIndex == 0) selectedCollatedBPs = null;
                        if (selectedCollationIndex > 0) {
                            if (collationChanged) {
                                var key = collationKeys.ElementAt(selectedCollationIndex);

                                var selectedKey = collationKeys.ElementAt(selectedCollationIndex);

                                foreach (var group in collatedBPs) {
                                    if (group.Key == selectedKey) {
                                        selectedCollatedBPs = group.Take(settings.searchLimit).ToArray();
                                    }
                                }
                                BlueprintListUI.needsLayout = true;
                            }
                            bps = selectedCollatedBPs;
                        }
                        BlueprintListUI.OnGUI(selected, bps, 0, remainingWidth, null, selectedTypeFilter);
                    }
                    UI.Space(25);
                }
            }
            return null;
        }
    }
}