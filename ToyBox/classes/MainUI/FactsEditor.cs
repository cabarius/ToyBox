// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
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
using ToyBox.classes.Infrastructure;

namespace ToyBox {
    public class FactsEditor {
        public static Settings settings { get { return Main.settings; } }
        public static IEnumerable<SimpleBlueprint> filteredBPs = null;
        static String prevCallerKey = "";
        static String searchText = "";
        static int searchLimit = 100;
        static int repeatCount = 1;
        public static int matchCount = 0;
        static bool showAll = false;
        static bool showTree = false;
        static FeaturesTreeEditor treeEditor = new FeaturesTreeEditor();

        public static void UpdateSearchResults(String searchText, int limit, IEnumerable<SimpleBlueprint> blueprints) {
            if (blueprints == null) return;
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            var filtered = new List<SimpleBlueprint>();
            foreach (SimpleBlueprint blueprint in blueprints) {
                if (blueprint.AssetGuid.ToString().Contains(searchText)
                    || blueprint.GetType().ToString().Contains(searchText)
                    ) {
                    filtered.Add(blueprint);
                }
                else {
                    var name = blueprint.name.ToLower();
                    if (terms.All(term => StringExtensions.Matches(name, term))) {
                        filtered.Add(blueprint);
                    }
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered.Take(searchLimit).OrderBy(bp => bp.name).ToArray();
            BlueprintListUI.needsLayout = true;
        }
        static public void OnGUI<T>(String callerKey,
                                    UnitEntityData unit,
                                    List<T> facts,
                                    Func<T, SimpleBlueprint> blueprint,
                                    IEnumerable<SimpleBlueprint> blueprints,
                                    Func<T, String> title,
                                    Func<T, String> description = null,
                                    Func<T, int> value = null,
                                    IEnumerable<BlueprintAction> actions = null
                ) {
            bool searchChanged = false;
            bool refreshTree = false;
            if (actions == null) actions = new List<BlueprintAction>();
            if (callerKey != prevCallerKey) { searchChanged = true; showAll = false; }
            prevCallerKey = callerKey;
            var mutatorLookup = actions.Distinct().ToDictionary(a => a.name, a => a);
            using (UI.HorizontalScope()) {
                UI.Space(100);
                UI.ActionTextField(ref searchText, "searhText", null, () => { searchChanged = true; }, UI.Width(320));
                UI.Space(25);
                UI.Label("Limit", UI.ExpandWidth(false));
                UI.ActionIntTextField(ref searchLimit, "searchLimit", null, () => { searchChanged = true; }, UI.Width(175));
                if (searchLimit > 1000) { searchLimit = 1000; }
                UI.Space(25);
                searchChanged |= UI.DisclosureToggle("Show All".orange().bold(), ref showAll);
#if DEBUG
                UI.Space(25);
                refreshTree |= UI.DisclosureToggle("Show Tree".orange().bold(), ref showTree);
#endif
                UI.Space(50);
                UI.Toggle("Show GUIDs", ref Main.settings.showAssetIDs);
            }
            if (showTree) {
                treeEditor.OnGUI(unit, refreshTree);
                return;
            }
            using (UI.HorizontalScope()) {
                UI.Space(100);
                UI.ActionButton("Search", () => { searchChanged = true; }, UI.AutoWidth());
                UI.Space(25);
                if (showAll && typeof(T) == typeof(AbilityData)) { // This is obviously tech debt, but I don't want to deal with Search All Spellbooks/Add All being on the facts editor as it is now
                    UI.Toggle("Search All Spellbooks", ref settings.showFromAllSpellbooks);
                    UI.Space(25);
                    UI.ActionButton("Add All", () => { CasterHelpers.HandleAddAllSpellsOnPartyEditor(unit.Descriptor, filteredBPs.Cast<BlueprintAbility>().ToList());}, UI.AutoWidth());
                }
                if (matchCount > 0 && searchText.Length > 0) {
                    String matchesText = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                    if (matchCount > searchLimit) { matchesText += " => ".cyan() + $"{searchLimit}".cyan().bold(); }
                    UI.Label(matchesText, UI.ExpandWidth(false));
                }
            }
            var remainingWidth = UI.ummWidth;
            if (showAll) {
                // TODO - do we need this logic or can we make blueprint filtering fast enough to do keys by key searching?
                //if (filteredBPs == null || searchChanged) {
                    UpdateSearchResults(searchText, searchLimit, blueprints);
                //}
                BlueprintListUI.OnGUI(unit, filteredBPs, 100, remainingWidth - 100);
                return;
            }
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

            BlueprintAction add = mutatorLookup.GetValueOrDefault("Add", null);
            BlueprintAction remove = mutatorLookup.GetValueOrDefault("Remove", null);
            BlueprintAction decrease = mutatorLookup.GetValueOrDefault("<", null);
            BlueprintAction increase = mutatorLookup.GetValueOrDefault(">", null);

            mutatorLookup.Remove("Add");
            mutatorLookup.Remove("Remove");
            mutatorLookup.Remove("<");
            mutatorLookup.Remove(">");

            SimpleBlueprint toAdd = null;
            SimpleBlueprint toRemove = null;
            SimpleBlueprint toIncrease = null;
            SimpleBlueprint toDecrease = null;
            var toValues = new Dictionary<String, SimpleBlueprint>();
            var sorted = facts.OrderBy((f) => title(f));
            matchCount = 0;
            UI.Div(100);
            foreach (var fact in sorted) {
                var remWidth = remainingWidth;
                if (fact == null) continue;
                var bp = blueprint(fact);
                String name = title(fact);
                String nameLower = name.ToLower();
                if (name != null && name.Length > 0 && (searchText.Length == 0 || terms.All(term => nameLower.Contains(term)))) {
                    matchCount++;
                    using (UI.HorizontalScope()) {
                        UI.Space(100); remWidth -= 100;
                        var titleWidth = (remainingWidth / (UI.IsWide ? 3.0f : 4.0f)) - 100;
                        UI.Label($"{name}".cyan().bold(), UI.Width(titleWidth));
                        remWidth -= titleWidth;
                        UI.Space(10); remWidth -= 10;
                        if (value != null) {
                            var v = value(fact);
                            decrease.BlueprintActionButton(unit, bp, () => { toDecrease = bp; }, 60);
                            UI.Space(10f);
                            UI.Label($"{v}".orange().bold(), UI.Width(30));
                            increase.BlueprintActionButton(unit, bp, () => { toIncrease = bp; }, 60);
                            remWidth -= 166;
                        }
#if false
                    UI.Space(30);
                    add.BlueprintActionButton(unit, bp, () => { toAdd = bp; }, 150);
#endif
                        UI.Space(10); remWidth -= 10;
                        remove.BlueprintActionButton(unit, bp, () => { toRemove = bp; }, 175);
                        remWidth -= 178;
#if false
                    foreach (var action in actions) {
                        action.MutatorButton(unit, bp, () => { toValues[action.name] = bp; }, 150);
                    }
#endif
                        UI.Space(20); remWidth -= 20;
                        using (UI.VerticalScope(UI.Width(remWidth - 100))) {
                            if (settings.showAssetIDs)
                                GUILayout.TextField(blueprint(fact).AssetGuid.ToString(), UI.AutoWidth());
                            if (description != null) {
                                UI.Label(description(fact).RemoveHtmlTags().green(), UI.Width(remWidth - 100));
                            }
                        }
                    }
                    UI.Div(100);
                }
            }
            if (toAdd != null) { add.action(toAdd, unit, repeatCount); toAdd = null; }
            if (toRemove != null) { remove.action(toRemove, unit, repeatCount); toRemove = null; }
            if (toDecrease != null) { decrease.action(toDecrease, unit, repeatCount); toDecrease = null; }
            if (toIncrease != null) { increase.action(toIncrease, unit, repeatCount); toIncrease = null; }
            foreach (var item in toValues) {
                var muator = mutatorLookup[item.Key];
                if (muator != null) {
                    muator.action(item.Value, unit, repeatCount);
                }
            }
            toValues.Clear();
        }
        static public void OnGUI(UnitEntityData ch, List<Feature> facts) {
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return;
            OnGUI<Feature>("Features", ch, facts,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintFeature>(),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                BlueprintAction.ActionsForType(typeof(BlueprintFeature))
                );
        }
        static public void OnGUI(UnitEntityData ch, List<Buff> facts) {
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return;
            OnGUI<Buff>("Features", ch, facts,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintBuff>(),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
               BlueprintAction.ActionsForType(typeof(BlueprintBuff))
                );
        }
        static public void OnGUI(UnitEntityData ch, List<Ability> facts) {
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return;
            OnGUI<Ability>("Abilities", ch, facts,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintAbility>().Where((bp) => !((BlueprintAbility)bp).IsSpell),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                BlueprintAction.ActionsForType(typeof(BlueprintAbility))
                );
        }

        static public void OnGUI(UnitEntityData ch, Spellbook spellbook, int level) {
            var spells = spellbook.GetKnownSpells(level).OrderBy(d => d.Name).ToList();
            var spellbookBP = spellbook.Blueprint;
            var normal = BlueprintExensions.GetBlueprints<BlueprintSpellbook>()
                .Where(x => ((BlueprintSpellbook)x).SpellList != null)
                .SelectMany(x => ((BlueprintSpellbook)x).SpellList.GetSpells(level));
            var mythic = BlueprintExensions.GetBlueprints<BlueprintSpellbook>()
                .Where(x => ((BlueprintSpellbook)x).MythicSpellList != null)
                .SelectMany(x => ((BlueprintSpellbook)x).MythicSpellList.GetSpells(level));

            var learnable = settings.showFromAllSpellbooks ?  normal.Concat(mythic).Distinct() : spellbookBP.SpellList.GetSpells(level);
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return;

            OnGUI<AbilityData>($"Spells.{spellbookBP.Name}", ch, spells,
                (fact) => fact.Blueprint,
                learnable,
                (fact) => fact.Name,
                (fact) => fact.Description,
                null,
                BlueprintAction.ActionsForType(typeof(BlueprintAbility))
                );
        }
        static public void OnGUI(UnitEntityData ch, List<Spellbook> spellbooks) {
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return;
            OnGUI<Spellbook>("Spellbooks", ch, spellbooks,
                (sb) => sb.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintSpellbook>(),
                (sb) => sb.Blueprint.GetDisplayName(),
                (sb) => sb.Blueprint.GetDescription(),
                null,
                BlueprintAction.ActionsForType(typeof(BlueprintSpellbook))
                );
        }
    }
}
