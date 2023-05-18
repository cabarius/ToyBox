// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
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
using Kingmaker.AI.Blueprints;
using ModKit.DataViewer;
using Kingmaker.AreaLogic.QuestSystem;
#if Wrath
using Kingmaker.Armies.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Craft;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.Crusade.GlobalMagic;
#endif
using static ToyBox.BlueprintExtensions;

namespace ToyBox {
    public static class SearchAndPick {
        public static Settings Settings => Main.Settings;

        public static IEnumerable<SimpleBlueprint> unpagedBPs = null;
        public static IEnumerable<SimpleBlueprint> filteredBPs = null;
        public static Dictionary<string, List<SimpleBlueprint>> collatedBPs = null;
        public static IEnumerable<SimpleBlueprint> selectedCollatedBPs = null;
        public static List<string> collationKeys = null;
        public static List<string> collationTitles = null;
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
            new NamedTypeFilter<BlueprintFeature>("Features", null, bp => bp.CollationNames( 
#if Wrath
                                                      bp.Groups.Select(g => g.ToString()).ToArray()
#endif
                                                      )),
#if Wrath
            new NamedTypeFilter<BlueprintParametrizedFeature>("ParamFeatures", null, bp => new List<string> {bp.ParameterType.ToString() }),
            new NamedTypeFilter<BlueprintFeatureSelection>("Feature Selection", null, bp => bp.CollationNames(bp.Group.ToString(), bp.Group2.ToString())),
#endif
            new NamedTypeFilter<BlueprintCharacterClass>("Classes", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintProgression>("Progression", null, bp => bp.Classes.Select(cl => cl.Name).ToList()),
            new NamedTypeFilter<BlueprintArchetype>("Archetypes", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Abilities", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Spells", bp => bp.IsSpell, bp => bp.CollationNames(bp.School.ToString())),
#if Wrath

            new NamedTypeFilter<BlueprintGlobalMagicSpell>("Global Spells", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintBrain>("Brains", null, bp => bp.CollationNames()),
#if Wrath
            new NamedTypeFilter<BlueprintAiAction>("AIActions", null, bp => bp.CollationNames()),
            new NamedTypeFilter<Consideration>("Considerations", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintAbilityResource>("Ability Rsrc", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintSpellbook>("Spellbooks", null, bp => bp.CollationNames(bp.CharacterClass.Name.ToString())),
            new NamedTypeFilter<BlueprintBuff>("Buffs", null, bp => bp.CollationNames()),
#if Wrath
            new NamedTypeFilter<BlueprintKingdomBuff>("Kingdom Buffs", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintItem>("Item", null,  (bp) => {
                if (bp.m_NonIdentifiedNameText?.ToString().Length > 0) return bp.CollationNames(bp.m_NonIdentifiedNameText);
                return bp.CollationNames(bp.ItemType.ToString());
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equipment", null, (bp) =>  bp.CollationNames(bp.ItemType.ToString(), $"{bp.GetCost().ToBinString("⊙".yellow())}")),
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (rarity)", null, (bp) => new List<string> {bp.Rarity().GetString() }),
            new NamedTypeFilter<BlueprintItemWeapon>("Weapons", null, (bp) => {
#if Wrath
                var type = bp.Type;
                var category = type?.Category;
                if (category != null) return bp.CollationNames(category.ToString(), $"{bp.GetCost().ToBinString("⊙".yellow())}");
                if (type != null) return bp.CollationNames(type.NameSafe(), $"{bp.GetCost().ToBinString("⊙".yellow())}");
#elif RT
                var family = bp.Family;
                var category = bp.Category;
                return bp.CollationNames(family.ToString(), category.ToString(), $"{bp.GetCost().ToBinString("⊙".yellow())}");
#endif
                return bp.CollationNames("?", $"{bp.GetCost().ToBinString("⊙".yellow())}");
                }),
            new NamedTypeFilter<BlueprintItemArmor>("Armor", null, (bp) => {
                var type = bp.Type;
                if (type != null) return bp.CollationNames(type.DefaultName, $"{bp.GetCost().ToBinString("⊙".yellow())}");
                return bp.CollationNames("?", $"{bp.GetCost().ToBinString("⊙".yellow())}");
                }),
            new NamedTypeFilter<BlueprintItemEquipmentUsable>("Usable", null, bp => bp.CollationNames(bp.SubtypeName, $"{bp.GetCost().ToBinString("⊙".yellow())}")),
#if Wrath
            new NamedTypeFilter<BlueprintIngredient>("Ingredient", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintUnit>("Units", null, bp => bp.CollationNames(bp.Type?.Name ?? bp.Race?.Name ?? "?", $"CR{bp.CR}")),
            new NamedTypeFilter<BlueprintUnit>("Units CR", null, bp => bp.CollationNames($"CR {bp.CR}")),
            new NamedTypeFilter<BlueprintRace>("Races", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintArea>("Areas", null, bp => bp.CollationNames()),
            //new NamedTypeFilter<BlueprintAreaPart>("Area Parts", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAreaEnterPoint>("Area Entry", null, bp =>bp.CollationNames(bp.m_Area.NameSafe())),
            //new NamedTypeFilter<BlueprintAreaEnterPoint>("AreaEntry ", null, bp => bp.m_Tooltip.ToString()),
#if Wrath
            new NamedTypeFilter<BlueprintGlobalMapPoint>("Map Points", null, bp => bp.CollationNames(bp.GlobalMapZone.ToString())),
            new NamedTypeFilter<BlueprintGlobalMap>("Global Map"),
#endif
            new NamedTypeFilter<Cutscene>("Cut Scenes", null, bp => bp.CollationNames(bp.Priority.ToString())),
            //new NamedTypeFilter<BlueprintMythicInfo>("Mythic Info"),
            new NamedTypeFilter<BlueprintQuest>("Quests", null, bp => bp.CollationNames(bp.m_Type.ToString())),
            new NamedTypeFilter<BlueprintQuestObjective>("QuestObj", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintEtude>("Etudes", null, bp =>bp.CollationNames(bp.Parent?.GetBlueprint().NameSafe() ?? "" )),
            new NamedTypeFilter<BlueprintUnlockableFlag>("Flags", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintDialog>("Dialog",null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintCue>("Cues", null, bp => {
                if (bp.Conditions.HasConditions) {
                    return bp.CollationNames(bp.Conditions.Conditions.First().NameSafe().SubstringBetweenCharacters('$', '$'));
                }
                return new List<string> { "-" };
                }),
            new NamedTypeFilter<BlueprintAnswer>("Answer", null, bp => bp.CaptionCollationNames()),
#if Wrath
            new NamedTypeFilter<BlueprintArmyPreset>("Armies", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintLeaderSkill>("ArmyGeneralSkill", null, bp =>  bp.CollationNames()),
#endif
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
            ReflectionTreeView.ClearExpanded();
            BlueprintListUI.needsLayout = true;
        }
        public static void ResetGUI() {
            ResetSearch();
            Settings.selectedBPTypeFilter = 1;
        }
        public static void UpdatePageCount() {
            if (Settings.searchLimit > 0) {
                pageCount = matchCount / Settings.searchLimit;
                currentPage = Math.Min(currentPage, pageCount);
            }
            else {
                pageCount = 1;
                currentPage = 1;
            }
        }
        public static void UpdatePaginatedResults() {
            var limit = Settings.searchLimit;
            var count = unpagedBPs.Count();
            var offset = Math.Min(count, currentPage * limit);
            limit = Math.Min(limit, Math.Max(count, count - limit));
            Mod.Trace($"{currentPage} / {pageCount} count: {count} => offset: {offset} limit: {limit} ");
            filteredBPs = unpagedBPs.Skip(offset).Take(limit).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.NameSafe()).ToArray();

        }
        public static void UpdateSearchResults() {
            if (blueprints == null) return;
            selectedCollationIndex = 0;
            selectedCollatedBPs = null;
            BlueprintListUI.needsLayout = true;
            if (Settings.searchText.Trim().Length == 0) {
                ResetSearch();
            }
            var searchText = Settings.searchText;
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            selectedTypeFilter = blueprintTypeFilters[Settings.selectedBPTypeFilter];
            var selectedType = selectedTypeFilter.type;
            IEnumerable<SimpleBlueprint> bps = null;
            if (selectedTypeFilter.blueprintSource != null) bps = selectedTypeFilter.blueprintSource();
            else bps = from bp in BlueprintExtensions.BlueprintsOfType(selectedType)
                       where selectedTypeFilter.filter(bp)
                       select bp;
            var filtered = new List<SimpleBlueprint>();
            foreach (var blueprint in bps) {
                if (blueprint.AssetGuid.ToString().Contains(searchText)
                    || blueprint.GetType().ToString().Contains(searchText)) {
                    filtered.Add(blueprint);
                }
                else {
                    var name = GetTitle(blueprint);
                    var displayName = blueprint.GetDisplayName();
                    var description = blueprint.GetDescription() ?? "";
                    if (terms.All(term => name.Matches(term))
                        || terms.All(term => displayName.Matches(term))
                        || Settings.searchDescriptions && 
                            (  terms.All(term => description.Matches(term))
                            || blueprint is BlueprintItem itemBP 
                                && terms.All(term => {
                                    try {
                                        return itemBP.FlavorText.Matches(term);                                        
                                    } catch (NullReferenceException e) {
                                        return false;
                                    }
                                })
                            )
                        ) {
                        filtered.Add(blueprint);
                    }
                }
            }
            filteredBPs = filtered.OrderBy(bp => bp.NameSafe());
            matchCount = filtered.Count();
            UpdatePageCount();
            for (var i = 0; i < BlueprintListUI.ParamSelected.Length; i++) {
                BlueprintListUI.ParamSelected[i] = 0;
            }
            uncolatedMatchCount = matchCount;
            if (selectedTypeFilter.collator != null) {
                collatedBPs = (from bp in filtered
                    from key in selectedTypeFilter.collator(bp)
                    //where selectedTypeFilter.collator(bp).Contains(key) // this line causes a mutation error
                    group bp by key into g
                    orderby g.Key.LongSortKey(), g.Key
                    select g).ToDictionary(g => g.Key, g => g.ToList().Distinct().ToList());
                _ = collatedBPs.Count();
                var keys = collatedBPs.ToList().Select(cbp => cbp.Key).ToList();
                collationKeys = new List<string> { "All" };
                collationKeys.AddRange(keys);
                var titles = collatedBPs.ToList().Select(cbp => $"{cbp.Key} ({cbp.Value.Count()})").ToList();
                collationTitles = new List<string> { $"All ({filtered.Count()})" };
                collationTitles.AddRange(titles);
            }
            else {
                collationKeys = null;
                collationTitles = null;
            }

            unpagedBPs = filteredBPs;
            UpdatePaginatedResults();
            firstSearch = false;
            UpdateCollation();
        }
        public static void UpdateCollation() {
            if (collationKeys == null || collatedBPs == null) return;
            var selectedKey = collationKeys.ElementAt(selectedCollationIndex);
            foreach (var pair in collatedBPs) {
                if (pair.Key == selectedKey) {
                    matchCount = pair.Value.Count();
                    selectedCollatedBPs = pair.Value.Take(Settings.searchLimit).Distinct().ToArray();
                    UpdatePageCount();
                }
            }
            BlueprintListUI.needsLayout = true;
        }
        public static void OnGUI() {
            if (blueprints == null) {
                blueprints = BlueprintLoader.Shared.GetBlueprints();
                if (blueprints != null) UpdateSearchResults();
            }
            // Stackable browser
            using (HorizontalScope(Width(350))) {
                var remainingWidth = ummWidth;
                // First column - Type Selection Grid
                using (VerticalScope(GUI.skin.box)) {
                    ActionSelectionGrid(ref Settings.selectedBPTypeFilter,
                        blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                        1,
                        (selected) => { UpdateSearchResults(); },
                        buttonStyle,
                        Width(200));
                }
                remainingWidth -= 350;
                var collationChanged = false;
                if (collatedBPs != null && collationTitles != null) {
                    using (VerticalScope(GUI.skin.box)) {
                        var selectedKey = collationTitles.ElementAt(selectedCollationIndex);
                        if (VPicker("Categories", ref selectedKey, collationTitles, null, s => s, ref collationSearchText, Width(300))) {
                            collationChanged = true; BlueprintListUI.needsLayout = true;
                        }
                        if (selectedKey != null)
                            selectedCollationIndex = collationTitles.IndexOf(selectedKey);

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
                            ref Settings.searchText,
                            "searchText",
                            (text) => { },
                            () => UpdateSearchResults(),
                            Width(400));
                        50.space();
                        Label("Limit", AutoWidth());
                        15.space();
                        ActionIntTextField(
                            ref Settings.searchLimit,
                            "searchLimit",
                            (limit) => { },
                            () => UpdateSearchResults(),
                            Width(75));
                        if (Settings.searchLimit > 1000) { Settings.searchLimit = 1000; }
                        25.space();
                        if (Toggle("Search Descriptions", ref Settings.searchDescriptions, AutoWidth())) UpdateSearchResults();
                        25.space();
                        if (Toggle("Attributes", ref Settings.showAttributes, AutoWidth())) UpdateSearchResults();
                        25.space();
                        Toggle("Show GUIDs", ref Settings.showAssetIDs, AutoWidth());
                        25.space();
                        Toggle("Components", ref Settings.showComponents, AutoWidth());
                        25.space();
                        Toggle("Elements", ref Settings.showElements, AutoWidth());
                        25.space();
                        Toggle("Show Display & Internal Names", ref Settings.showDisplayAndInternalNames, AutoWidth());
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
                            if (matchCount > Settings.searchLimit) { title += " => ".cyan() + $"{Settings.searchLimit}".cyan().bold(); }
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
                        CharacterPicker.OnCharacterPickerGUI();
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
        }
    }
}