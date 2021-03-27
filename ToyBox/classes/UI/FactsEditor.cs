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
        static String searchText = "";
        static public void OnGUI<T>(UnitEntityData unit,
                                    List<T> facts,
                                    Func<T, BlueprintScriptableObject> blueprint,
                                    Func<T, String> title,
                                    Func<T, String> description = null,
                                    Func<T, int> value = null,
                                    params BlueprintAction[] actions
            ) where T : IUIDataProvider {
            var mutatorLookup = actions.Distinct().ToDictionary(a => a.name, a => a);
            UI.BeginHorizontal();
            UI.Space(100);
            UI.TextField(ref searchText, null, UI.Width(200));
            UI.EndHorizontal();

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

            foreach (var fact in facts) {
                if (fact == null) continue;
                var bp = blueprint(fact);
                String name = fact.Name;
                if (name == null) { name = $"{title(fact)}"; }
                if (name != null && name.Length > 0 && (searchText.Length == 0 || name.Contains(searchText))) {
                    UI.BeginHorizontal();
                    UI.Space(100);
                    UI.Label($"{fact.Name}".cyan().bold(), UI.Width(400));
                    UI.Space(30);
                    if (value != null) {
                        var v = value(fact);
                        decrease.MutatorButton(unit, bp, () => { toDecrease = bp; }, 50);
                        UI.Space(10f);
                        UI.Label($"{v}".orange().bold(), UI.Width(30f));
                        increase.MutatorButton(unit, bp, () => { toIncrease = bp; }, 50);
                    }
                    UI.Space(30);
                    add.MutatorButton(unit, bp, () => { toAdd = bp; }, 150);
                    UI.Space(30);
                    remove.MutatorButton(unit, bp, () => { toAdd = bp; }, 150);
                    foreach (var action in actions) {
                        action.MutatorButton(unit, bp, () => { toValues[action.name] = bp; }, 150);
                    }
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
            OnGUI<Feature>(ch, facts,
                (fact) => fact.Blueprint,
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                ch.BlueprintActions<BlueprintFeature>().ToArray()
                );
        }
        static public void OnGUI(UnitEntityData ch, List<Ability> facts) {
            OnGUI<Ability>(ch, facts,
                (fact) => fact.Blueprint,
                (fact) => fact.Name,
                (fact) => fact.Description,
                (fact) => fact.GetRank(),
                ch.BlueprintActions<BlueprintAbility>().ToArray()
                );
        }
        static public void OnGUI(UnitEntityData ch, List<AbilityData> facts) {
            OnGUI<AbilityData>(ch, facts,
                (fact) => fact.Blueprint,
                (fact) => fact.Name,
                (fact) => fact.Description,
                null,
                ch.BlueprintActions<BlueprintAbility>().ToArray()
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
