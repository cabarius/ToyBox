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
using static ModKit.UI;
using ModKit.Utility;
using ToyBox.classes.Infrastructure;
using Kingmaker.EntitySystem;
using Kingmaker.Blueprints.Facts;

namespace ToyBox {
    public class FactsEditor {
        public static Settings settings => Main.settings;
        public static IEnumerable<SimpleBlueprint> filteredBPs = null;
        private static string prevCallerKey = "";
        private static string searchText = "";
        private static int searchLimit = 100;
        private static readonly int repeatCount = 1;
        public static int matchCount = 0;
        private static bool showAll = false;
        private static bool showTree = false;
        private static readonly FeaturesTreeEditor treeEditor = new();

        public static void UpdateSearchResults(string searchText, IEnumerable<SimpleBlueprint> blueprints) {
            if (blueprints == null) return;
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            var filtered = new List<SimpleBlueprint>();
            foreach (var blueprint in blueprints) {
                if (blueprint.AssetGuid.ToString().Contains(searchText)
                    || blueprint.GetType().ToString().Contains(searchText)
                    ) {
                    filtered.Add(blueprint);
                }
                else {
                    var name = blueprint.name.ToLower();
                    var displayName = blueprint.GetDisplayName().ToLower();
                    if (terms.All(term => name.Matches(term))
                        || terms.All(term => displayName.Matches(term))
                        ) {
                        filtered.Add(blueprint);
                    }
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered.OrderBy(bp => bp.name).Take(searchLimit).ToArray();
            BlueprintListUI.needsLayout = true;
        }
        public static List<Action> OnGUI<T>(string callerKey,
                                    UnitEntityData unit,
                                    List<T> facts,
                                    Func<T, SimpleBlueprint> blueprint,
                                    IEnumerable<SimpleBlueprint> blueprints,
                                    Func<T, string> title,
                                    Func<T, string> description = null,
                                    Func<T, int> value = null,
                                    IEnumerable<BlueprintAction> actions = null
                ) {
            List<Action> todo = new();
            var searchChanged = false;
            var refreshTree = false;
            if (actions == null) actions = new List<BlueprintAction>();
            if (callerKey != prevCallerKey) { searchChanged = true; showAll = false; }
            prevCallerKey = callerKey;
            var mutatorLookup = actions.Distinct().ToDictionary(a => a.name, a => a);
            using (HorizontalScope()) {
                100.space();
                ActionTextField(ref searchText, "searhText", null, () => { searchChanged = true; }, Width(320));
                25.space();
                Label("Limit", ExpandWidth(false));
                ActionIntTextField(ref searchLimit, "searchLimit", null, () => { searchChanged = true; }, Width(175));
                if (searchLimit > 1000) { searchLimit = 1000; }
                25.space();
                searchChanged |= DisclosureToggle("Show All".orange().bold(), ref showAll);
                25.space();
                refreshTree |= DisclosureToggle("Show Tree".orange().bold(), ref showTree);
                50.space();
                Toggle("Show GUIDs", ref Main.settings.showAssetIDs);
                25.space();
                Toggle("Show Display & Internal Names", ref settings.showDisplayAndInternalNames, AutoWidth());
            }
            if (showTree) {
                treeEditor.OnGUI(unit, refreshTree);
                return new List<Action>();
            }
            using (HorizontalScope()) {
                Space(100);
                ActionButton("Search", () => { searchChanged = true; }, AutoWidth());
                Space(25);
                if (showAll && typeof(T) == typeof(AbilityData)) { // This dynamic type check is obviously tech debt and should be refactored, but we don't want to deal with Search All Spellbooks/Add All being on the facts editor as it is now
                    if (Toggle("Search All Spellbooks", ref settings.showFromAllSpellbooks, AutoWidth())) { searchChanged = true; }
                    Space(25);
                    ActionButton("Add All", () => CasterHelpers.HandleAddAllSpellsOnPartyEditor(unit.Descriptor, filteredBPs.Cast<BlueprintAbility>().ToList()), AutoWidth());
                    Space(25);
                    ActionButton("Remove All", () => CasterHelpers.HandleAddAllSpellsOnPartyEditor(unit.Descriptor), AutoWidth());
                }
                if (matchCount > 0 && searchText.Length > 0) {
                    var matchesText = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                    if (matchCount > searchLimit) { matchesText += " => ".cyan() + $"{searchLimit}".cyan().bold(); }
                    Label(matchesText, ExpandWidth(false));
                }
            }
            var remainingWidth = ummWidth;
            if (showAll) {
                // TODO - do we need this logic or can we make blueprint filtering fast enough to do keys by key searching?
                //if (filteredBPs == null || searchChanged) {
                UpdateSearchResults(searchText, blueprints);
                //}
                return BlueprintListUI.OnGUI(unit, filteredBPs, 100, remainingWidth - 100);
            }
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

            var add = mutatorLookup.GetValueOrDefault("Add", null);
            var remove = mutatorLookup.GetValueOrDefault("Remove", null);
            var decrease = mutatorLookup.GetValueOrDefault("<", null);
            var increase = mutatorLookup.GetValueOrDefault(">", null);

            mutatorLookup.Remove("Add");
            mutatorLookup.Remove("Remove");
            mutatorLookup.Remove("<");
            mutatorLookup.Remove(">");

            var sorted = facts.OrderBy((f) => title(f));
            matchCount = 0;
            Div(100);
            foreach (var fact in sorted) {
                var remWidth = remainingWidth;
                if (fact == null) continue;
                var bp = blueprint(fact);
                var name = title(fact);
                var nameLower = name.ToLower();
                if (name != null && name.Length > 0 && (searchText.Length == 0 || terms.All(term => nameLower.Contains(term)))) {
                    matchCount++;
                    using (HorizontalScope()) {
                        Space(100); remWidth -= 100;
                        var titleWidth = (remainingWidth / (IsWide ? 3.0f : 4.0f)) - 100;
                        string text;
                        if (settings.showDisplayAndInternalNames)
                            text = $"{name.cyan().bold()} : {bp.NameSafe().color(RGBA.darkgrey)}";
                        else {
                            text = name.cyan().bold();
                        }
                        Label(text, Width(titleWidth));
                        remWidth -= titleWidth;
                        Space(10); remWidth -= 10;
                        if (value != null) {
                            var v = value(fact);
                            decrease.BlueprintActionButton(unit, bp, () => todo.Add(() => decrease.action(bp, unit, repeatCount)), 60);
                            Space(10f);
                            Label($"{v}".orange().bold(), Width(30));
                            increase.BlueprintActionButton(unit, bp, () => todo.Add(() => increase.action(bp, unit, repeatCount)), 60);
                            remWidth -= 166;
                        }
#if false
                    UI.Space(30);
                    add.BlueprintActionButton(unit, bp, () => todo.Add(add.action(bp, unit, repeatCount)), 150);
#endif
                        Space(10); remWidth -= 10;
                        remove.BlueprintActionButton(unit, bp, () => todo.Add(() => remove.action(bp, unit, repeatCount)), 175);
                        remWidth -= 178;
#if false
                    foreach (var action in actions) {
                        action.MutatorButton(unit, bp, () => todo.Add(() => muator.action(bp, unit, repeatCount)), 150);
                    }
#endif
                        Space(20); remWidth -= 20;
                        using (VerticalScope(Width(remWidth - 100))) {
                            if (settings.showAssetIDs)
                                GUILayout.TextField(blueprint(fact).AssetGuid.ToString(), AutoWidth());
                            if (description != null) {
                                Label(description(fact).StripHTML().green(), Width(remWidth - 100));
                            }
                        }
                    }
                    Div(100);
                }
            }
            return todo;
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<EntityFact> facts) {
            var blueprints = BlueprintLoader.Shared.GetBlueprints();

            if (blueprints == null) return new List<Action>();
            return OnGUI<EntityFact>("Features", ch, facts,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintUnitFact>(),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                BlueprintAction.ActionsForType(typeof(BlueprintUnitFact))
                );
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Feature> feature) {
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<Action>();
            return OnGUI<Feature>("Features", ch, feature,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintFeature>(),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                BlueprintAction.ActionsForType(typeof(BlueprintFeature))
                );
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Buff> buff) {
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<Action>();
            return OnGUI<Buff>("Features", ch, buff,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintBuff>(),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
               BlueprintAction.ActionsForType(typeof(BlueprintBuff))
                );
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Ability> ability) {
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<Action>();
            return OnGUI<Ability>("Abilities", ch, ability,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintAbility>().Where((bp) => !((BlueprintAbility)bp).IsSpell),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                BlueprintAction.ActionsForType(typeof(BlueprintAbility))
                );
        }

        public static List<Action> OnGUI(UnitEntityData ch, Spellbook spellbook, int level) {
            var spells = spellbook.GetKnownSpells(level).OrderBy(d => d.Name).ToList();
            var spellbookBP = spellbook.Blueprint;

            var learnable = settings.showFromAllSpellbooks ? CasterHelpers.GetAllSpells(level) : spellbookBP.SpellList.GetSpells(level);
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<Action>();

            return OnGUI<AbilityData>($"Spells.{spellbookBP.Name}", ch, spells,
                (fact) => fact.Blueprint,
                learnable,
                (fact) => fact.Name,
                (fact) => fact.Description,
                null,
                BlueprintAction.ActionsForType(typeof(BlueprintAbility))
                );
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Spellbook> spellbooks) {
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<Action>();
            return OnGUI<Spellbook>("Spellbooks", ch, spellbooks,
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
