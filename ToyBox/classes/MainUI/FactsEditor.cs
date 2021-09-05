// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox
{
    public class FactsEditor
    {
        public static Settings settings => Main.settings;

        public static IEnumerable<SimpleBlueprint> filteredBPs;

        static string prevCallerKey = string.Empty;

        static string searchText = string.Empty;

        static int searchLimit = 100;

        static int repeatCount = 1;

        public static int matchCount;

        static bool showAll;

        public static void UpdateSearchResults(string searchText, int limit, IEnumerable<SimpleBlueprint> blueprints)
        {
            if (blueprints == null)
            {
                return;
            }

            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            var filtered = new List<SimpleBlueprint>();

            foreach (SimpleBlueprint blueprint in blueprints)
            {
                if (blueprint.AssetGuid.ToString().Contains(searchText) || blueprint.GetType().ToString().Contains(searchText))
                {
                    filtered.Add(blueprint);

                    continue;
                }

                var name = blueprint.name.ToLower();

                if (terms.All(term => StringExtensions.Matches(name, term)))
                {
                    filtered.Add(blueprint);
                }
            }

            matchCount = filtered.Count();
            filteredBPs = filtered.Take(searchLimit).OrderBy(bp => bp.name).ToArray();
            BlueprintListUI.needsLayout = true;
        }

        public static void OnGUI<T>(string callerKey,
                                    UnitEntityData unit,
                                    List<T> facts,
                                    Func<T, SimpleBlueprint> blueprint,
                                    IEnumerable<SimpleBlueprint> blueprints,
                                    Func<T, string> title,
                                    Func<T, string> description = null,
                                    Func<T, int> value = null,
                                    IEnumerable<BlueprintAction> actions = null)
        {
            bool searchChanged = false;

            if (actions == null)
            {
                actions = new List<BlueprintAction>();
            }

            if (callerKey != prevCallerKey)
            {
                searchChanged = true;
                showAll = false;
            }

            prevCallerKey = callerKey;
            var mutatorLookup = actions.Distinct().ToDictionary(a => a.name, a => a);
            UI.BeginHorizontal();
            UI.Space(100);
            UI.ActionTextField(ref searchText, "searhText", null, () => searchChanged = true, UI.Width(320));
            UI.Space(25);
            UI.Label("Limit", UI.ExpandWidth(false));
            UI.ActionIntTextField(ref searchLimit, "searchLimit", null, () => searchChanged = true, UI.Width(175));

            if (searchLimit > 1000) { searchLimit = 1000; }

            UI.Space(25);
            UI.Toggle("Show GUIDs", ref Main.settings.showAssetIDs);
            UI.Space(25);
            searchChanged |= UI.DisclosureToggle("Show All".orange().bold(), ref showAll);
            UI.EndHorizontal();
            UI.BeginHorizontal();
            UI.Space(100);
            UI.ActionButton("Search", () => searchChanged = true, UI.AutoWidth());
            UI.Space(25);

            if (matchCount > 0 && searchText.Length > 0)
            {
                string matchesText = "Matches: ".green().bold() + $"{matchCount}".orange().bold();

                if (matchCount > searchLimit) { matchesText += " => ".cyan() + $"{searchLimit}".cyan().bold(); }

                UI.Label(matchesText, UI.ExpandWidth(false));
            }

            UI.EndHorizontal();
            float remainingWidth = UI.ummWidth;

            if (showAll)
            {
                // TODO - do we need this logic or can we make blueprint filtering fast enough to do keys by key searching?
                UpdateSearchResults(searchText, searchLimit, blueprints);
                BlueprintListUI.OnGUI(unit, filteredBPs, 100, remainingWidth - 100);

                return;
            }

            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

            BlueprintAction add = mutatorLookup.GetValueOrDefault("Add");
            BlueprintAction remove = mutatorLookup.GetValueOrDefault("Remove");
            BlueprintAction decrease = mutatorLookup.GetValueOrDefault("<");
            BlueprintAction increase = mutatorLookup.GetValueOrDefault(">");

            mutatorLookup.Remove("Add");
            mutatorLookup.Remove("Remove");
            mutatorLookup.Remove("<");
            mutatorLookup.Remove(">");

            SimpleBlueprint toRemove = null;
            SimpleBlueprint toIncrease = null;
            SimpleBlueprint toDecrease = null;

            // var toValues = new Dictionary<string, SimpleBlueprint>();

            var sorted = facts.OrderBy(title);
            matchCount = 0;
            UI.Div(100);

            foreach (var fact in sorted)
            {
                float remWidth = remainingWidth;

                if (!fact.Equals(null))
                {
                    var bp = blueprint(fact);
                    string name = title(fact);
                    string nameLower = name.ToLower();

                    if (name.Length > 0 && (searchText.Length == 0 || terms.All(term => nameLower.Contains(term))))
                    {
                        matchCount++;

                        using (UI.HorizontalScope())
                        {
                            UI.Space(100);
                            remWidth -= 100;
                            float titleWidth = (remainingWidth / (UI.IsWide ? 3.0f : 4.0f)) - 100;
                            UI.Label($"{name}".cyan().bold(), UI.Width(titleWidth));
                            remWidth -= titleWidth;
                            UI.Space(10);
                            remWidth -= 10;

                            if (value != null)
                            {
                                int v = value(fact);
                                decrease.BlueprintActionButton(unit, bp, () => toDecrease = bp, 60);
                                UI.Space(10f);
                                UI.Label($"{v}".orange().bold(), UI.Width(30));
                                increase.BlueprintActionButton(unit, bp, () => toIncrease = bp, 60);
                                remWidth -= 166;
                            }

                            UI.Space(10);
                            remWidth -= 10;
                            remove.BlueprintActionButton(unit, bp, () => toRemove = bp, 175);
                            remWidth -= 178;

                            UI.Space(20);
                            remWidth -= 20;

                            using (UI.VerticalScope(UI.Width(remWidth - 100)))
                            {
                                if (settings.showAssetIDs)
                                {
                                    GUILayout.TextField(blueprint(fact).AssetGuid.ToString(), UI.AutoWidth());
                                }

                                if (description != null)
                                {
                                    UI.Label(description(fact).RemoveHtmlTags().green(), UI.Width(remWidth - 100));
                                }
                            }
                        }

                        UI.Div(100);
                    }
                }
            }

            if (toRemove != null)
            {
                remove.action(toRemove, unit, repeatCount);
            }

            if (toDecrease != null)
            {
                decrease.action(toDecrease, unit, repeatCount);
            }

            if (toIncrease != null)
            {
                increase.action(toIncrease, unit, repeatCount);
            }

            // foreach ((string key, SimpleBlueprint simpleBlueprint) in toValues) {
            //     var muator = mutatorLookup[key];
            //
            //     muator?.action(simpleBlueprint, unit, repeatCount);
            // }
            //
            // toValues.Clear();
        }

        public static void OnGUI(UnitEntityData ch, List<Feature> facts)
        {
            var blueprints = BlueprintBrowser.GetBlueprints();

            if (blueprints == null)
            {
                return;
            }

            OnGUI("Features", ch, facts,
                  fact => fact.Blueprint,
                  BlueprintExensions.GetBlueprints<BlueprintFeature>(),
                  fact => fact.Name,
                  fact => fact.Description,
                  fact => fact.GetRank(),
                  BlueprintAction.ActionsForType(typeof(BlueprintFeature))
            );
        }

        public static void OnGUI(UnitEntityData ch, List<Buff> facts)
        {
            var blueprints = BlueprintBrowser.GetBlueprints();

            if (blueprints == null)
            {
                return;
            }

            OnGUI("Features", ch, facts,
                  fact => fact.Blueprint,
                  BlueprintExensions.GetBlueprints<BlueprintBuff>(),
                  fact => fact.Name,
                  fact => fact.Description,
                  fact => fact.GetRank(),
                  BlueprintAction.ActionsForType(typeof(BlueprintBuff))
            );
        }

        public static void OnGUI(UnitEntityData ch, List<Ability> facts)
        {
            var blueprints = BlueprintBrowser.GetBlueprints();

            if (blueprints == null)
            {
                return;
            }

            OnGUI("Abilities", ch, facts,
                  fact => fact.Blueprint,
                  BlueprintExensions.GetBlueprints<BlueprintAbility>().Where(bp => !((BlueprintAbility)bp).IsSpell),
                  fact => fact.Name,
                  fact => fact.Description,
                  fact => fact.GetRank(),
                  BlueprintAction.ActionsForType(typeof(BlueprintAbility))
            );
        }

        public static void OnGUI(UnitEntityData ch, Spellbook spellbook, int level)
        {
            var spells = spellbook.GetKnownSpells(level).OrderBy(d => d.Name).ToList();
            var spellbookBP = spellbook.Blueprint;
            var learnable = spellbookBP.SpellList.GetSpells(level);
            var blueprints = BlueprintBrowser.GetBlueprints();

            if (blueprints == null)
            {
                return;
            }

            OnGUI($"Spells.{spellbookBP.Name}", ch, spells,
                  fact => fact.Blueprint,
                  learnable,
                  fact => fact.Name,
                  fact => fact.Description,
                  null,
                  BlueprintAction.ActionsForType(typeof(BlueprintAbility))
            );
        }

        public static void OnGUI(UnitEntityData ch, List<Spellbook> spellbooks)
        {
            var blueprints = BlueprintBrowser.GetBlueprints();

            if (blueprints == null)
            {
                return;
            }

            OnGUI("Spellbooks", ch, spellbooks,
                  sb => sb.Blueprint,
                  BlueprintExensions.GetBlueprints<BlueprintSpellbook>(),
                  sb => sb.Blueprint.GetDisplayName(),
                  sb => sb.Blueprint.GetDescription(),
                  null,
                  BlueprintAction.ActionsForType(typeof(BlueprintSpellbook))
            );
        }
    }
}