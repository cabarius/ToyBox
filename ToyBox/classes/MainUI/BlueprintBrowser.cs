// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Craft;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox {
    public class BlueprintBrowser {
        public static Settings settings => Main.settings;

        public static IEnumerable<SimpleBlueprint> filteredBPs;
        public static IEnumerable<IGrouping<string, SimpleBlueprint>> collatedBPs;
        public static IEnumerable<SimpleBlueprint> selectedCollatedBPs;
        public static List<string> collationKeys;
        public static int selectedCollationIndex;
        static bool firstSearch = true;
        public static string[] filteredBPNames;
        public static int uncolatedMatchCount;
        public static int matchCount;
        public static string parameter = "";

        static readonly NamedTypeFilter[] blueprintTypeFilters = {
            new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintFact>("Facts", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintFeature>("Features", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintParametrizedFeature>("ParamFeatures", null, bp => bp.ParameterType.ToString()),
            new NamedTypeFilter<BlueprintCharacterClass>("Classes", null, bp => bp.IsArcaneCaster ? "Arcane"
                                                                              : bp.IsDivineCaster ? "Divine"
                                                                              : bp.IsMythic ? "Mythic"
                                                                              : bp.IsHigherMythic ? "Higher Mythic"
                                                                              : "Standard"),
            new NamedTypeFilter<BlueprintArchetype>("Archetypes", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAbility>("Abilities", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAbility>("Actions", null, bp => bp.ActionType.ToString()),
            new NamedTypeFilter<BlueprintAbility>("Spells", bp => bp.IsSpell, bp => bp.School.ToString()),
            new NamedTypeFilter<BlueprintAbilityResource>("Ability Rsrc", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintSpellbook>("Spellbooks", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintSpellbook>("Class SBs", null, bp => bp.CharacterClass.Name.ToString()),
            new NamedTypeFilter<BlueprintBuff>("Buffs", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintItem>("Item", null,  bp => {
                if (bp.m_NonIdentifiedNameText?.ToString().Length > 0) return bp.m_NonIdentifiedNameText;
                return bp.ItemType.ToString();
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equipment", null, bp => bp.ItemType.ToString()),
            new NamedTypeFilter<BlueprintItemWeapon>("Weapons", null, bp => {
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
            //new NamedTypeFilter<BlueprintMythicInfo>("Mythic Info"),
            new NamedTypeFilter<BlueprintQuest>("Quests", null, bp => bp.m_Type.ToString()),
            new NamedTypeFilter<BlueprintQuestObjective>("QuestObj", null, bp => bp.m_Type.ToString()),
            new NamedTypeFilter<BlueprintEtude>("Etudes", null, bp => bp.Parent?.GetBlueprint().NameSafe() ?? ""),
            new NamedTypeFilter<BlueprintUnlockableFlag>("Flags", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintFeatureSelection>("Feature Select"),
            new NamedTypeFilter<BlueprintArmyPreset>("Armies", null, bp => bp.GetType().ToString()),
            //new NamedTypeFilter<SimpleBlueprint>("In Memory", null, bp => bp.CollationName(), () => ResourcesLibrary.s_LoadedBlueprints.Values.Where(bp => bp != null)),

        };

        public static NamedTypeFilter selectedTypeFilter;

        public static IEnumerable<SimpleBlueprint> blueprints;
        public static IEnumerable<SimpleBlueprint> GetBlueprints() {
            if (blueprints == null) {
#if false
                if (BlueprintLoaderOld.LoadInProgress()) { return null; }
                else {
                    Main.Log($"calling BlueprintLoader.Load");
                    BlueprintLoaderOld.Load((bps) => {
                        blueprints = bps;
                        UpdateSearchResults();
                        Main.Log($"success got {bps.Count()} blueprints");
                    });
                    return null;
                }
#else
                if (BlueprintLoader.Shared.IsLoading) { return null; }

                Main.Log("calling BlueprintLoader.Load");
                BlueprintLoader.Shared.Load(bps => {
                                                blueprints = bps;
                                                UpdateSearchResults();
                                                Main.Log($"success got {bps.Count()} bluerints");
                                            });
                return null;
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
            selectedCollatedBPs = null;
            BlueprintListUI.needsLayout = true;
            if (settings.searchText.Trim().Length == 0) {
                ResetSearch();
            }
            var searchText = settings.searchText;
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            selectedTypeFilter = blueprintTypeFilters[settings.selectedBPTypeFilter];
            var selectedType = selectedTypeFilter.type;
            IEnumerable<SimpleBlueprint> bps = null;
            if (selectedTypeFilter.blueprintSource != null) bps = selectedTypeFilter.blueprintSource();
            else bps = BlueprintExensions.BlueprintsOfType(selectedType).Where(bp => selectedTypeFilter.filter(bp));
            var filtered = new List<SimpleBlueprint>();
            foreach (SimpleBlueprint blueprint in bps) {
                if (blueprint.AssetGuid.ToString().Contains(searchText)
                    || blueprint.GetType().ToString().Contains(searchText)
                    ) {
                    filtered.Add(blueprint);
                }
                else {
                    var name = blueprint.name;
                    var description = blueprint.GetDescription() ?? "";
                    if (    terms.All(term => StringExtensions.Matches(name, term))
                        || settings.searchDescriptions && terms.All(term => StringExtensions.Matches(description, term))
                        ) {
                        filtered.Add(blueprint);
                    }
                }
            }
            filteredBPs = filtered.OrderBy(bp => bp.name);
            matchCount = filtered.Count();
            uncolatedMatchCount = matchCount;
            if (selectedTypeFilter.collator != null) {
                collatedBPs = filtered.GroupBy(selectedTypeFilter.collator).OrderBy(bp => bp.Key);
                // I could do something like this but I will leave it up to the UI when a collation is selected.
                // GetItems().GroupBy(g => g.Type).Select(s => new { Type = s.Key, LastTen = s.Take(10).ToList() });
                collationKeys = new List<string> { "All" };
                collationKeys = collationKeys.Concat(collatedBPs.Select(cbp => cbp.Key)).ToList();
            }
            filteredBPs = filteredBPs.Take(settings.searchLimit).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            firstSearch = false;
        }
        public static IEnumerable OnGUI() {
            if (blueprints == null) GetBlueprints();
            // Stackable browser
            using (UI.HorizontalScope(UI.Width(350))) {
                float remainingWidth = UI.ummWidth;
                // First column - Type Selection Grid
                using (UI.VerticalScope(GUI.skin.box)) {
                    UI.ActionSelectionGrid(ref settings.selectedBPTypeFilter,
                        blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                        1,
                        selected => UpdateSearchResults(),
                        UI.buttonStyle,
                        UI.Width(200));
                }
                remainingWidth -= 350;
                bool collationChanged = false;
                if (collatedBPs != null) {
                    using (UI.VerticalScope(GUI.skin.box)) {
                        UI.ActionSelectionGrid(ref selectedCollationIndex, collationKeys.ToArray(),
                            1,
                            selected => { collationChanged = true; BlueprintListUI.needsLayout = true; },
                            UI.buttonStyle,
                            UI.Width(200));
                    }
                    remainingWidth -= 350;
                }

                // Section Column  - Main Area
                using (UI.VerticalScope(UI.MinWidth(remainingWidth))) {
                    // Search Field and modifiers
                    using (UI.HorizontalScope()) {
                        UI.ActionTextField(
                            ref settings.searchText,
                            "searhText",
                            text => { },
                            UpdateSearchResults,
                            UI.MinWidth(100), UI.MaxWidth(400));
                        UI.Label("Limit", UI.Width(150));
                        UI.ActionIntTextField(
                            ref settings.searchLimit,
                            "searchLimit",
                            limit => { },
                            UpdateSearchResults,
                            UI.MinWidth(75), UI.MaxWidth(250));
                        if (settings.searchLimit > 1000) { settings.searchLimit = 1000; }
                        UI.Space(25);
                        UI.Toggle("Search Descriptions", ref settings.searchDescriptions);
                        UI.Space(25);
                        UI.Toggle("Show GUIDs", ref settings.showAssetIDs);
                        UI.Space(25);
                        UI.Toggle("Components", ref settings.showComponents);
                        //UI.Space(25);
                        //UI.Toggle("Elements", ref settings.showElements);
                    }
                    // Search Button and Results Summary
                    using (UI.HorizontalScope()) {
                        UI.ActionButton("Search", UpdateSearchResults, UI.AutoWidth());
                        UI.Space(25);
                        if (firstSearch) {
                            UI.Label("please note the first search may take a few seconds.".green(), UI.AutoWidth());
                        }
                        else if (matchCount > 0) {
                            string title = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                            if (matchCount > settings.searchLimit) { title += " => ".cyan() + $"{settings.searchLimit}".cyan().bold(); }
                            UI.Label(title, UI.ExpandWidth(false));
                        }
                        UI.Space(50);
                        UI.Label("".green(), UI.AutoWidth());
                    }
                    UI.Space(10);

                    if (filteredBPs != null) {
                        CharacterPicker.OnGUI();
                        UnitReference selected = CharacterPicker.GetSelectedCharacter();
                        var bps = filteredBPs;
                        if (selectedCollationIndex == 0) {
                            selectedCollatedBPs = null;
                            matchCount = uncolatedMatchCount;
                        }
                        if (selectedCollationIndex > 0) {
                            if (collationChanged) {
                                var key = collationKeys.ElementAt(selectedCollationIndex);

                                var selectedKey = collationKeys.ElementAt(selectedCollationIndex);

                                foreach (var group in collatedBPs) {
                                    if (group.Key == selectedKey) {
                                        selectedCollatedBPs = group.Take(settings.searchLimit).ToArray();
                                        matchCount = selectedCollatedBPs.Count();
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
