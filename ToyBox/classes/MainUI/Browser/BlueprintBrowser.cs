// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using Kingmaker.Utility;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.Cutscenes;
using ModKit;
using static ModKit.UI;
using ModKit.Utility;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;

namespace ToyBox {
    public static class BlueprintBrowser {
        public static Settings settings => Main.settings;

        public static IEnumerable<SimpleBlueprint> unpagedBPs = null;
        public static IEnumerable<SimpleBlueprint> filteredBPs = null;
        public static IEnumerable<IGrouping<string, SimpleBlueprint>> collatedBPs = null;
        public static IEnumerable<SimpleBlueprint> selectedCollatedBPs = null;
        public static List<string> collationKeys = null;
        public static int selectedCollationIndex = 0;
        private static bool firstSearch = true;
        public static string[] filteredBPNames = null;
        public static int uncolatedMatchCount = 0;
        public static int matchCount = 0;
        public static int pageCount = 0;
        public static int currentPage = 0;
        public static string collationSearchText = "";
        public static string parameter = "";
        private static readonly char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(
#if DEBUG
                bp.m_AllElements?.OfType<Condition>()?.Select(e => e.GetCaption() ?? "")?.ToArray() ?? new string[] {}
#endif
                )),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.Collat`ionNames(bp.m_AllElements?.Select(e => e.GetType().Name).ToArray() ?? new string[] {})),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(bp.m_AllElements?.Select(e => e.ToString().TrimEnd(digits)).ToArray() ?? new string[] {})),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(bp.m_AllElements?.Select(e => e.name.Split('$')[1].TrimEnd(digits)).ToArray() ?? new string[] {})),
            new NamedTypeFilter<BlueprintFact>("Facts", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintFeature>("Features", null, bp => bp.CollationNames( bp.Groups.Select(g => g.ToString()).ToArray())),
            new NamedTypeFilter<BlueprintParametrizedFeature>("ParamFeatures", null, bp => new List<string> {bp.ParameterType.ToString() }),
            new NamedTypeFilter<BlueprintFeatureSelection>("Feature Selection", null, bp => bp.CollationNames(bp.Group.ToString(), bp.Group2.ToString())),
            new NamedTypeFilter<BlueprintCharacterClass>("Classes", null, bp => bp.CollationNames("Standard")),
                                                                          //bp => bp.IsArcaneCaster ? "Arcane"
                                                                          //    : bp.IsDivineCaster ? "Divine"
                                                                          //    : bp.IsMythic ? "Mythic"
                                                                          //    : bp.IsHigherMythic ? "Higher Mythic"
                                                                          //    : "Standard"),
            new NamedTypeFilter<BlueprintProgression>("Progression", null, bp => bp.Classes.Select(cl => cl.Name).ToList()),
            new NamedTypeFilter<BlueprintArchetype>("Archetypes", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Abilities", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Spells", bp => bp.IsSpell, bp => bp.CollationNames(bp.School.ToString())),
            new NamedTypeFilter<BlueprintAbilityResource>("Ability Rsrc", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintSpellbook>("Spellbooks", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintSpellbook>("Class SBs", null, bp => bp.CollationNames(bp.CharacterClass.Name.ToString())),
            new NamedTypeFilter<BlueprintBuff>("Buffs", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintItem>("Item", null,  (bp) => {
                if (bp.m_NonIdentifiedNameText?.ToString().Length > 0) return bp.CollationNames(bp.m_NonIdentifiedNameText);
                return bp.CollationNames(bp.ItemType.ToString());
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equipment", null, (bp) =>  bp.CollationNames(bp.ItemType.ToString(), $"{bp.Cost.ToBinString("⊙".yellow())}")),
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (rarity)", null, (bp) => new List<string> {bp.Rarity().GetString() }),
            new NamedTypeFilter<BlueprintItemWeapon>("Weapons", null, (bp) => {
                var type = bp.Type;
                var category = type?.Category;
                if (category != null) return bp.CollationNames(category.ToString(), $"{bp.Cost.ToBinString("⊙".yellow())}");
                if (type != null) return bp.CollationNames(type.NameSafe(), $"{bp.Cost.ToBinString("⊙".yellow())}");
                return bp.CollationNames("?", $"{bp.Cost.ToBinString("⊙".yellow())}");
                }),
            new NamedTypeFilter<BlueprintItemArmor>("Armor", null, (bp) => {
                var type = bp.Type;
                if (type != null) return bp.CollationNames(type.DefaultName, $"{bp.Cost.ToBinString("⊙".yellow())}");
                return bp.CollationNames("?", $"{bp.Cost.ToBinString("⊙".yellow())}");
                }),
            new NamedTypeFilter<BlueprintItemEquipmentUsable>("Usable", null, bp => bp.CollationNames(bp.SubtypeName, $"{bp.Cost.ToBinString("⊙".yellow())}")),
            new NamedTypeFilter<BlueprintIngredient>("Ingredient", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintUnit>("Units", null, bp => bp.CollationNames(bp.Type?.Name ?? bp.Race?.Name ?? "?", $"CR{bp.CR}")),
            new NamedTypeFilter<BlueprintUnit>("Units CR", null, bp => bp.CollationNames($"CR {bp.CR}")),
            new NamedTypeFilter<BlueprintRace>("Races", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintArea>("Areas", null, bp => bp.CollationNames()),
            //new NamedTypeFilter<BlueprintAreaPart>("Area Parts", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAreaEnterPoint>("Area Entry", null, bp =>bp.CollationNames(bp.m_Area.NameSafe())),
            //new NamedTypeFilter<BlueprintAreaEnterPoint>("AreaEntry ", null, bp => bp.m_Tooltip.ToString()),
            new NamedTypeFilter<BlueprintGlobalMapPoint>("Map Points", null, bp => bp.CollationNames(bp.GlobalMapZone.ToString())),
            new NamedTypeFilter<BlueprintGlobalMap>("Global Map"),
            new NamedTypeFilter<Cutscene>("Cut Scenes", null, bp => bp.CollationNames(bp.Priority.ToString())),
            //new NamedTypeFilter<BlueprintMythicInfo>("Mythic Info"),
            new NamedTypeFilter<BlueprintQuest>("Quests", null, bp => bp.CollationNames(bp.m_Type.ToString())),
            new NamedTypeFilter<BlueprintQuestObjective>("QuestObj", null, bp =>bp.CollationNames(bp.m_Type.ToString())),
            new NamedTypeFilter<BlueprintEtude>("Etudes", null, bp =>bp.CollationNames(bp.Parent?.GetBlueprint().NameSafe() ?? "" )),
            new NamedTypeFilter<BlueprintUnlockableFlag>("Flags", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintDialog>("Dialog", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintCue>("Cues", null, bp => {
                if (bp.Conditions.HasConditions) {
                    return bp.CollationNames(bp.Conditions.Conditions.First().NameSafe().SubstringBetweenCharacters('$', '$'));
                }
                return new List<string> { "-" };
                }),
            new NamedTypeFilter<BlueprintAnswer>("Answer", null),
            new NamedTypeFilter<BlueprintFeatureSelection>("Feature Select"),
            new NamedTypeFilter<BlueprintArmyPreset>("Armies", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintLeaderSkill>("ArmyGeneralSkill", null, bp =>  bp.CollationNames()),
#if false
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (ench)", null, (bp) => {
                try {
                    var enchants = bp.CollectEnchantments();
                    var value = enchants.Sum((e) => e.EnchantmentCost);
                    return new List<string> { value.ToString() };
                }
                catch {
                    return new List<string> { "0" };
                }
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (cost)", null, (bp) => new List<string> {bp.Cost.ToBinString() }),  
#endif
            //new NamedTypeFilter<SimpleBlueprint>("In Memory", null, bp => bp.CollationName(), () => ResourcesLibrary.s_LoadedBlueprints.Values.Where(bp => bp != null)),

        };

        public static NamedTypeFilter selectedTypeFilter = null;

        public static IEnumerable<SimpleBlueprint> blueprints = null;

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
        public static void UpdatePageCount() {
            if (settings.searchLimit > 0) {
                pageCount = matchCount / settings.searchLimit;
                currentPage = Math.Min(currentPage, pageCount);
            }
            else {
                pageCount = 1;
                currentPage = 1;
            }
        }
        public static void UpdatePaginatedResults() {
            var limit = settings.searchLimit;
            var count = unpagedBPs.Count();
            var offset = Math.Min(count, currentPage * limit);
            limit = Math.Min(limit, Math.Max(count, count - limit));
            Mod.Trace($"{currentPage} / {pageCount} count: {count} => offset: {offset} limit: {limit} ");
            filteredBPs = unpagedBPs.Skip(offset).Take(limit).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();

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
            else bps = from bp in BlueprintExensions.BlueprintsOfType(selectedType)
                       where selectedTypeFilter.filter(bp)
                       select bp;
            var filtered = new List<SimpleBlueprint>();
            foreach (var blueprint in bps) {
                if (blueprint.AssetGuid.ToString().Contains(searchText)
                    || blueprint.GetType().ToString().Contains(searchText)) {
                    filtered.Add(blueprint);
                }
                else {
                    var name = blueprint.name;
                    var displayName = blueprint.GetDisplayName();
                    var description = blueprint.GetDescription() ?? "";
                    if (terms.All(term => name.Matches(term))
                        || terms.All(term => displayName.Matches(term))
                        || settings.searchesDescriptions && 
                            (  terms.All(term => description.Matches(term))
                            || blueprint is BlueprintItem itemBP 
                                && terms.All(term => itemBP.FlavorText.Matches(term))
                            )
                        ) {
                        filtered.Add(blueprint);
                    }
                }
            }
            filteredBPs = filtered.OrderBy(bp => bp.name);
            matchCount = filtered.Count();
            UpdatePageCount();
            for (var i = 0; i < BlueprintListUI.ParamSelected.Length; i++) {
                BlueprintListUI.ParamSelected[i] = 0;
            }
            uncolatedMatchCount = matchCount;
            if (selectedTypeFilter.collator != null) {
                collatedBPs = from bp in filtered
                    from key in selectedTypeFilter.collator(bp)
                    //where selectedTypeFilter.collator(bp).Contains(key) // this line causes a mutation error
                    group bp by key into g
                    orderby g.Key.LongSortKey(), g.Key
                    select g;
                _ = collatedBPs.Count();
                var keys = collatedBPs.ToList().Select(cbp => cbp.Key).ToList();
                collationKeys = new List<string> { "All" };
                collationKeys.AddRange(keys);
            }
            else {
                collationKeys = null;
            }

            unpagedBPs = filteredBPs;
            UpdatePaginatedResults();
            firstSearch = false;
            UpdateCollation();
        }
        public static void UpdateCollation() {
            if (collationKeys == null || collatedBPs == null) return;
            var selectedKey = collationKeys.ElementAt(selectedCollationIndex);
            foreach (var group in collatedBPs) {
                if (group.Key == selectedKey) {
                    matchCount = group.Count();
                    selectedCollatedBPs = group.Take(settings.searchLimit).ToArray();
                    UpdatePageCount();
                }
            }
            BlueprintListUI.needsLayout = true;
        }
        public static IEnumerable OnGUI() {
            if (blueprints == null) {
                blueprints = BlueprintLoader.Shared.GetBlueprints();
                if (blueprints != null) UpdateSearchResults();
            }
            // Stackable browser
            using (HorizontalScope(Width(350))) {
                var remainingWidth = ummWidth;
                // First column - Type Selection Grid
                using (VerticalScope(GUI.skin.box)) {
                    ActionSelectionGrid(ref settings.selectedBPTypeFilter,
                        blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                        1,
                        (selected) => { UpdateSearchResults(); },
                        buttonStyle,
                        Width(200));
                }
                remainingWidth -= 350;
                var collationChanged = false;
                if (collatedBPs != null) {
                    using (VerticalScope(GUI.skin.box)) {
                        var selectedKey = collationKeys.ElementAt(selectedCollationIndex);
                        if (VPicker("Categories", ref selectedKey, collationKeys, null, s => s, ref collationSearchText, Width(300))) {
                            collationChanged = true; BlueprintListUI.needsLayout = true;
                        }
                        if (selectedKey != null)
                            selectedCollationIndex = collationKeys.IndexOf(selectedKey);

#if false
                        UI.ActionSelectionGrid(ref selectedCollationIndex, collationKeys.ToArray(),
                            1,
                            (selected) => { collationChanged = true; BlueprintListUI.needsLayout = true; },
                            UI.buttonStyle,
                            UI.Width(200));
#endif
                    }
                    remainingWidth -= 450;
                }

                // Section Column  - Main Area
                using (VerticalScope(MinWidth(remainingWidth))) {
                    // Search Field and modifiers
                    using (HorizontalScope()) {
                        ActionTextField(
                            ref settings.searchText,
                            "searhText",
                            (text) => { },
                            () => UpdateSearchResults(),
                            Width(400));
                        50.space();
                        Label("Limit", AutoWidth());
                        15.space();
                        ActionIntTextField(
                            ref settings.searchLimit,
                            "searchLimit",
                            (limit) => { },
                            () => UpdateSearchResults(),
                            Width(75));
                        if (settings.searchLimit > 1000) { settings.searchLimit = 1000; }
                        25.space();
                        if (Toggle("Search Descriptions", ref settings.searchesDescriptions, AutoWidth())) UpdateSearchResults();
                        25.space();
                        if (Toggle("Attributes", ref settings.showAttributes, AutoWidth())) UpdateSearchResults();
                        25.space();
                        Toggle("Show GUIDs", ref settings.showAssetIDs, AutoWidth());
                        25.space();
                        Toggle("Components", ref settings.showComponents, AutoWidth());
                        25.space();
                        Toggle("Show Display & Internal Names", ref settings.showDisplayAndInternalNames, AutoWidth());
                    }
                    // Search Button and Results Summary
                    using (HorizontalScope()) {
                        ActionButton("Search", () => {
                            UpdateSearchResults();
                        }, AutoWidth());
                        Space(25);
                        if (firstSearch) {
                            Label("please note the first search may take a few seconds.".green(), AutoWidth());
                        }
                        else if (matchCount > 0) {
                            var title = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                            if (matchCount > settings.searchLimit) { title += " => ".cyan() + $"{settings.searchLimit}".cyan().bold(); }
                            Label(title, ExpandWidth(false));
                        }
                        Space(130);
                        Label($"Page: ".green() + $"{Math.Min(currentPage + 1, pageCount + 1)}".orange() + " / " + $"{pageCount + 1}".cyan(), AutoWidth());
                        ActionButton("-", () => {
                            currentPage = Math.Max(currentPage -= 1, 0);
                            UpdatePaginatedResults();
                        }, AutoWidth());
                        ActionButton("+", () => {
                            currentPage = Math.Min(currentPage += 1, pageCount);
                            UpdatePaginatedResults();
                        }, AutoWidth());
                        Space(25);
                        var pageNum = currentPage + 1;
                        if (Slider(ref pageNum, 1, pageCount + 1, 1)) UpdatePaginatedResults();
                        currentPage = pageNum - 1;
                    }
                    Space(10);

                    if (filteredBPs != null) {
                        CharacterPicker.OnGUI();
                        UnitReference selected = CharacterPicker.GetSelectedCharacter();
                        var bps = filteredBPs;
                        if (selectedCollationIndex == 0) {
                            selectedCollatedBPs = null;
                            matchCount = uncolatedMatchCount;
                            UpdatePageCount();
                        }
                        if (selectedCollationIndex > 0) {
                            if (collationChanged) {
                                UpdateCollation();
                            }
                            bps = selectedCollatedBPs;
                        }
                        BlueprintListUI.OnGUI(selected, bps, 0, remainingWidth, null, selectedTypeFilter, (keys) => {
                            if (keys.Length > 0) {
                                var changed = false;
                                //var bpTypeName = keys[0];
                                //var newTypeFilterIndex = blueprintTypeFilters.FindIndex(f => f.type.Name == bpTypeName);
                                //if (newTypeFilterIndex >= 0) {
                                //    settings.selectedBPTypeFilter = newTypeFilterIndex;
                                //    changed = true;
                                //}
                                if (keys.Length > 1) {
                                    var collationKey = keys[1];
                                    var newCollationIndex = collationKeys.FindIndex(ck => ck == collationKey);
                                    if (newCollationIndex >= 0) {
                                        selectedCollationIndex = newCollationIndex;
                                        UpdateCollation();
                                    }
                                }
                                if (changed) {
                                    UpdateSearchResults();
                                }
                            }
                        }).ForEach(action => action());
                    }
                    Space(25);
                }
            }
            return null;
        }
    }
}