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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox
{
    public class BlueprintBrowser
    {
        public static Settings settings => Main.settings;

        public static IEnumerable<SimpleBlueprint> filteredBPs;

        public static IEnumerable<IGrouping<string, SimpleBlueprint>> collatedBPs;

        public static IEnumerable<SimpleBlueprint> selectedCollatedBPs;

        public static List<string> collationKeys;

        public static int selectedCollationIndex;

        private static bool firstSearch = true;

        public static string[] filteredBPNames;

        public static int matchCount;

        public static string parameter = "";

        private static readonly NamedTypeFilter[] blueprintTypeFilters =
        {
            new NamedTypeFilter<SimpleBlueprint>("All", collator: bp => bp.CollationName()),

            new NamedTypeFilter<BlueprintFact>("Facts", collator: bp => bp.CollationName()),

            new NamedTypeFilter<BlueprintFeature>("Features", collator: bp => bp.CollationName()),

            new NamedTypeFilter<BlueprintAbility>("Abilities", collator: bp => bp.Type.ToString()),

            new NamedTypeFilter<BlueprintAbility>("Actions", collator: bp => bp.ActionType.ToString()),

            new NamedTypeFilter<BlueprintAbility>("Spells",
                                                  bp => bp.IsSpell,
                                                  bp => bp.School.ToString()),

            new NamedTypeFilter<BlueprintSpellbook>("Spellbooks", collator: bp => bp.CollationName()),

            new NamedTypeFilter<BlueprintSpellbook>("Class SBs", collator: bp => bp.CharacterClass.Name.ToString()),

            new NamedTypeFilter<BlueprintBuff>("Buffs", collator: bp => bp.CollationName()),

            new NamedTypeFilter<BlueprintItem>("Item", collator: bp => bp.m_NonIdentifiedNameText?.ToString().Length > 0
                                                                     ? bp.m_NonIdentifiedNameText
                                                                     : bp.ItemType.ToString()),

            new NamedTypeFilter<BlueprintItemEquipment>("Equipment", collator: bp => bp.ItemType.ToString()),

            new NamedTypeFilter<BlueprintItemWeapon>("Weapons", collator: bp =>
                                                                          {
                                                                              var type = bp.Type;
                                                                              var category = type?.Category;

                                                                              return category != null
                                                                                  ? category.ToString()
                                                                                  : type != null
                                                                                      ? type.NameSafe()
                                                                                      : "?";
                                                                          }),

            new NamedTypeFilter<BlueprintItemEquipmentUsable>("Usable", collator: bp => bp.SubtypeName),

            new NamedTypeFilter<BlueprintIngredient>("Ingredient", collator: bp => bp.CollationName()),

            new NamedTypeFilter<BlueprintUnit>("Units", collator: bp => bp.Type?.Name ?? bp.Race?.Name ?? "?"),

            new NamedTypeFilter<BlueprintRace>("Races"),

            new NamedTypeFilter<BlueprintArea>("Areas", collator: bp => bp.CollationName()),

            new NamedTypeFilter<BlueprintAreaEnterPoint>("Area Entry", collator: bp => bp.m_Area.NameSafe()),

            new NamedTypeFilter<BlueprintGlobalMapPoint>("Map Points", collator: bp => bp.GlobalMapZone.ToString()),

            new NamedTypeFilter<BlueprintGlobalMap>("Global Map"),

            new NamedTypeFilter<Cutscene>("Cut Scenes", collator: bp => bp.Priority.ToString()),

            new NamedTypeFilter<BlueprintQuest>("Quests", collator: bp => bp.m_Type.ToString()),

            new NamedTypeFilter<BlueprintQuestObjective>("QuestObj", collator: bp => bp.m_Type.ToString()),

            new NamedTypeFilter<BlueprintEtude>("Etudes", collator: bp => bp.Parent?.GetBlueprint().NameSafe() ?? ""),

            new NamedTypeFilter<BlueprintFeatureSelection>("Feature Select"),

            new NamedTypeFilter<BlueprintArmyPreset>("Armies", collator: bp => bp.GetType().ToString()),
        };

        public static NamedTypeFilter selectedTypeFilter;

        public static IEnumerable<SimpleBlueprint> blueprints;

        public static IEnumerable<SimpleBlueprint> GetBlueprints()
        {
            if (blueprints == null)
            {
                if (BlueprintLoader.Shared.IsLoading) { return null; }

                Main.Log("calling BlueprintLoader.Load");

                BlueprintLoader.Shared.Load(bps =>
                                            {
                                                IEnumerable<SimpleBlueprint> simpleBlueprints = bps.ToList();
                                                blueprints = simpleBlueprints;
                                                UpdateSearchResults();
                                                Main.Log($"success got {simpleBlueprints.Count()} bluerints");
                                            });

                return null;
            }

            return blueprints;
        }

        public static void ResetSearch()
        {
            filteredBPs = null;
            filteredBPNames = null;
            collatedBPs = null;
            BlueprintListUI.needsLayout = true;
        }

        public static void ResetGUI()
        {
            ResetSearch();
            settings.selectedBPTypeFilter = 1;
        }

        public static void UpdateSearchResults()
        {
            if (blueprints == null)
            {
                return;
            }

            selectedCollationIndex = 0;
            selectedCollatedBPs = null;
            BlueprintListUI.needsLayout = true;

            if (settings.searchText.Trim().Length == 0)
            {
                ResetSearch();
            }

            string searchText = settings.searchText;
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            selectedTypeFilter = blueprintTypeFilters[settings.selectedBPTypeFilter];
            var selectedType = selectedTypeFilter.type;

            IEnumerable<SimpleBlueprint> bps = selectedTypeFilter.blueprintSource != null
                ? selectedTypeFilter.blueprintSource()
                : BlueprintExensions.BlueprintsOfType(selectedType).Where(bp => selectedTypeFilter.filter(bp));

            var filtered = new List<SimpleBlueprint>();

            foreach (SimpleBlueprint blueprint in bps)
            {
                if (blueprint.AssetGuid.ToString().Contains(searchText) || blueprint.GetType().ToString().Contains(searchText))
                {
                    filtered.Add(blueprint);

                    continue;
                }

                var name = blueprint.name;

                if (terms.All(term => StringExtensions.Matches(name, term)))
                {
                    filtered.Add(blueprint);
                }
            }

            filteredBPs = filtered.OrderBy(bp => bp.name);
            matchCount = filtered.Count();

            if (selectedTypeFilter.collator != null)
            {
                collatedBPs = filtered.GroupBy(selectedTypeFilter.collator).OrderBy(bp => bp.Key);
                collationKeys = new List<string> { "All" };
                collationKeys = collationKeys.Concat(collatedBPs.Select(cbp => cbp.Key)).ToList();
            }

            filteredBPs = filteredBPs.Take(settings.searchLimit).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            firstSearch = false;
        }

        public static IEnumerable OnGUI()
        {
            if (blueprints == null)
            {
                GetBlueprints();
            }

            // Stackable browser
            using (UI.HorizontalScope(UI.Width(350)))
            {
                float remainingWidth = UI.ummWidth;

                // First column - Type Selection Grid
                using (UI.VerticalScope(GUI.skin.box))
                {
                    UI.ActionSelectionGrid(ref settings.selectedBPTypeFilter,
                                           blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                                           1,
                                           selected => UpdateSearchResults(),
                                           UI.buttonStyle,
                                           UI.Width(200));
                }

                remainingWidth -= 350;
                bool collationChanged = false;

                if (collatedBPs != null)
                {
                    using (UI.VerticalScope(GUI.skin.box))
                    {
                        UI.ActionSelectionGrid(ref selectedCollationIndex, collationKeys.ToArray(),
                                               1,
                                               selected =>
                                               {
                                                   collationChanged = true;
                                                   BlueprintListUI.needsLayout = true;
                                               },
                                               UI.buttonStyle,
                                               UI.Width(200));
                    }

                    remainingWidth -= 350;
                }

                // Section Column  - Main Area
                using (UI.VerticalScope(UI.MinWidth(remainingWidth)))
                {
                    // Search Field and modifiers
                    using (UI.HorizontalScope())
                    {
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
                        UI.Toggle("Show GUIDs", ref settings.showAssetIDs);
                        UI.Space(25);
                        UI.Toggle("Components", ref settings.showComponents);
                        //UI.Space(25);
                        //UI.Toggle("Elements", ref settings.showElements);
                    }

                    // Search Button and Results Summary
                    using (UI.HorizontalScope())
                    {
                        UI.ActionButton("Search", UpdateSearchResults, UI.AutoWidth());

                        UI.Space(25);

                        if (firstSearch)
                        {
                            UI.Label("please note the first search may take a few seconds.".green(), UI.AutoWidth());
                        }
                        else if (matchCount > 0)
                        {
                            string title = "Matches: ".green().bold() + $"{matchCount}".orange().bold();

                            if (matchCount > settings.searchLimit) { title += " => ".cyan() + $"{settings.searchLimit}".cyan().bold(); }

                            UI.Label(title, UI.ExpandWidth(false));
                        }

                        UI.Space(50);
                        UI.Label("".green(), UI.AutoWidth());
                    }

                    UI.Space(10);

                    if (filteredBPs != null)
                    {
                        CharacterPicker.OnGUI();
                        UnitReference selected = CharacterPicker.GetSelectedCharacter();
                        var bps = filteredBPs;

                        if (selectedCollationIndex == 0)
                        {
                            selectedCollatedBPs = null;
                        }

                        if (selectedCollationIndex > 0)
                        {
                            if (collationChanged)
                            {
                                string selectedKey = collationKeys.ElementAt(selectedCollationIndex);

                                foreach (IGrouping<string, SimpleBlueprint> g in collatedBPs.Where(g => g.Key == selectedKey))
                                {
                                    selectedCollatedBPs = g.Take(settings.searchLimit).ToArray();
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