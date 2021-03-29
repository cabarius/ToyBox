// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
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
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
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

namespace ToyBox {
    public class FactsEditor {
        public static IEnumerable<BlueprintScriptableObject> filteredBPs = null;
        static String prevCallerKey = "";
        static String searchText = "";
        static int searchLimit = 100;
        public static int matchCount = 0;

        static bool showAll = false;
        public static void UpdateSearchResults(String searchText, int limit, IEnumerable<BlueprintScriptableObject> blueprints) {
            if (blueprints == null) return;
            var terms = searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            var filtered = new List<BlueprintScriptableObject>();
            foreach (BlueprintScriptableObject blueprint in blueprints) {
                var name = blueprint.name.ToLower();
                var type = blueprint.GetType();
                if (terms.All(term => name.Contains(term))) {
                    filtered.Add(blueprint);
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered.Take(searchLimit).OrderBy(bp => bp.name).ToArray();
        }
        static public void OnGUI<T>(String callerKey,
                                    UnitEntityData unit,
                                    List<T> facts,
                                    Func<T, BlueprintScriptableObject> blueprint,
                                    IEnumerable<BlueprintScriptableObject> blueprints,
                                    Func<T, String> title,
                                    Func<T, String> description = null,
                                    Func<T, int> value = null,
                                    List<BlueprintAction> actions = null
                ) where T : IUIDataProvider {
            bool searchChanged = false;
            if (actions == null) actions = new List<BlueprintAction>();
            if (callerKey != prevCallerKey) { searchChanged = true; showAll = false; }
            prevCallerKey = callerKey;
            var mutatorLookup = actions.Distinct().ToDictionary(a => a.name, a => a);
            UI.BeginHorizontal();
            UI.Space(100);
            UI.ActionTextField(ref searchText, "searhText", null, () => { searchChanged = true; }, UI.Width(320));
            UI.Space(25);
            UI.Label("Limit", UI.ExpandWidth(false));
            UI.ActionIntTextField(ref searchLimit, "searchLimit", null, () => { searchChanged = true; }, UI.Width(175));
            if (searchLimit > 1000) { searchLimit = 1000; }
            UI.Space(25);
            searchChanged |= UI.DisclosureToggle("Show All".orange().bold(), ref showAll);
            UI.EndHorizontal();
            UI.BeginHorizontal();
            UI.Space(100);
            UI.ActionButton("Search", () => { searchChanged = true; }, UI.AutoWidth());
            UI.Space(25);
            if (matchCount > 0 && searchText.Length > 0) {
                String matchesText = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                if (matchCount > searchLimit) { matchesText += " => ".cyan() + $"{searchLimit}".cyan().bold(); }
                UI.Label(matchesText, UI.ExpandWidth(false));
            }
            UI.EndHorizontal();

            if (showAll) {
                // TODO - do we need this logic or can we make blueprint filtering fast enough to do keys by key searching?
                //if (filteredBPs == null || searchChanged) {
                    UpdateSearchResults(searchText, searchLimit, blueprints);
                //}
                BlueprintListUI.OnGUI(unit, filteredBPs, 100);
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

            BlueprintScriptableObject toAdd = null;
            BlueprintScriptableObject toRemove = null;
            BlueprintScriptableObject toIncrease = null;
            BlueprintScriptableObject toDecrease = null;
            var toValues = new Dictionary<String, BlueprintScriptableObject>();
            var sorted = facts.OrderBy((f) => f.Name);
            matchCount = 0;
            foreach (var fact in sorted) {
                if (fact == null) continue;
                var bp = blueprint(fact);
                String name = fact.Name.ToLower();
                if (name == null) { name = $"{title(fact)}"; }
                if (name != null && name.Length > 0 && (searchText.Length == 0 || terms.All(term => name.Contains(term)))) {
                    matchCount++;
                    UI.BeginHorizontal();
                    UI.Space(100);
                    UI.Label($"{fact.Name}".cyan().bold(), UI.Width(400));
                    UI.Space(30);
                    if (value != null) {
                        var v = value(fact);
                        decrease.MutatorButton(unit, bp, () => { toDecrease = bp; }, 50);
                        UI.Space(10f);
                        UI.Label($"{v}".orange().bold(), UI.Width(30));
                        increase.MutatorButton(unit, bp, () => { toIncrease = bp; }, 50);
                    }
#if false
                    UI.Space(30);
                    add.MutatorButton(unit, bp, () => { toAdd = bp; }, 150);
#endif
                    UI.Space(30);
                    remove.MutatorButton(unit, bp, () => { toRemove = bp; }, 150);
#if false
                    foreach (var action in actions) {
                        action.MutatorButton(unit, bp, () => { toValues[action.name] = bp; }, 150);
                    }
#endif
                    if (description != null) {
                        UI.Space(30);
                        UI.Label(description(fact).green(), UI.AutoWidth());
                    }
                    UI.EndHorizontal();
                }
            }
            if (toAdd != null) { add.action(unit, toAdd); toAdd = null; }
            if (toRemove != null) { remove.action(unit, toRemove); toRemove = null; }
            if (toDecrease != null) { decrease.action(unit, toDecrease); toDecrease = null; }
            if (toIncrease != null) { increase.action(unit, toIncrease); toIncrease = null; }
            foreach (var item in toValues) {
                var muator = mutatorLookup[item.Key];
                if (muator != null) {
                    muator.action(unit, item.Value);
                }
            }
            toValues.Clear();
        }
        static public void OnGUI(UnitEntityData ch, List<Feature> facts) {
            var blueprints = BlueprintBrowser.GetBluePrints();
            if (blueprints == null) return;
            OnGUI<Feature>("Features", ch, facts,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintFeature>(),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                ch.BlueprintActions(typeof(BlueprintFeature))
                );
        }
        static public void OnGUI(UnitEntityData ch, List<Ability> facts) {
            var blueprints = BlueprintBrowser.GetBluePrints();
            if (blueprints == null) return;
            OnGUI<Ability>("Abilities", ch, facts,
                (fact) => fact.Blueprint,
                BlueprintExensions.GetBlueprints<BlueprintAbility>().Where((bp) => !((BlueprintAbility)bp).IsSpell),
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                ch.BlueprintActions(typeof(BlueprintAbility))
                );
        }
        static public void OnGUI(UnitEntityData ch, Spellbook spellbook, int level) {
            var spells = spellbook.GetKnownSpells(level).OrderBy(d => d.Name).ToList();
            var spellbookBP = spellbook.Blueprint;
            var learnable = spellbookBP.SpellList.GetSpells(level);
            var blueprints = BlueprintBrowser.GetBluePrints();
            if (blueprints == null) return;
            OnGUI<AbilityData>($"Spells.{spellbookBP.Name}.{level}", ch, spells,
                (fact) => fact.Blueprint,
                learnable,
                (fact) => fact.Name,
                (fact) => fact.Description,
                null,
                ch.BlueprintActions(typeof(BlueprintAbility))
                );
        }
    }
}
#if false
            null,
                new NamedMutator<U, T>("Remove", (fact) => collection.RemoveFact(fact), (fact) => collection.HasFact(fact)),
                new NamedMutator<U, T>("Decrease", (fact) => collection.RemoveFact(fact), (fact) => collection.HasFact(fact) && fact.GetRank() > 1 ),
                new NamedMutator<U, T>("Increase", (fact) => collection.RemoveFact(fact), (fact) => collection.HasFact(fact) && fact.GetRank() < fact.Blueprint.GetRanks() - 1)
                );
#endif

#if false
        static public void OnGUI(UnitLogicCollection<T> facts) { OnGUI(facts.Enumerable.GetEnumerator()); }
        static public void OnGUI(List<T> facts) { OnGUI(facts.GetEnumerator()); }

        static public void OnGUI(IEnumerator<T> facts) {
        static public void OnGUI(UnitLogicCollection<T> facts) {
#endif
