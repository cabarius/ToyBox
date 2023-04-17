// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModKit.UI;
using static ToyBox.BlueprintExtensions;

namespace ToyBox {
    public class FactsEditor {
        public class CollectionChangedSubscriber : IFactCollectionUpdatedHandler {
            public CollectionChangedSubscriber() {
                EventBus.Subscribe(this);
            }
            public void HandleFactCollectionUpdated(EntityFactsProcessor collection) {
                foreach (var b in BuffBrowserDict.Values) {
                    b.needsReloadData = true;
                }
                foreach (var b in FeatureBrowserDict.Values) {
                    b.needsReloadData = true;
                }
                foreach (var b in AbilityBrowserDict.Values) {
                    b.needsReloadData = true;
                }
            }
        }
        private static Settings settings => Main.settings;
        private static bool showTree = false;
        private static readonly int repeatCount = 1;
        private static readonly FeaturesTreeEditor treeEditor = new();
        private static readonly CollectionChangedSubscriber collectionChangedSubscriber = new();

        private static Dictionary<UnitEntityData, Browser<Feature, BlueprintFeature>> FeatureBrowserDict = new();
        private static Dictionary<UnitEntityData, Browser<Buff, BlueprintBuff>> BuffBrowserDict = new();
        private static Dictionary<UnitEntityData, Browser<Ability, BlueprintAbility>> AbilityBrowserDict = new();
        private static Browser<FeatureSelectionEntry, BlueprintFeature> FeatureSelectionBrowser = new();
        private static Browser<IFeatureSelectionItem, IFeatureSelectionItem> ParameterizedFeatureBrowser = new();


        public static void RowGUI<Item, Definition>(Item feature, Definition blueprint, UnitEntityData ch, Browser<Item, Definition> usedBrowser, List<Action> todo)
            where Definition : BlueprintScriptableObject, IUIDataProvider {
            var mutatorLookup = BlueprintAction.ActionsForType(typeof(Definition)).Distinct().ToDictionary(a => a.name, a => a);
            var add = mutatorLookup.GetValueOrDefault("Add", null);
            var remove = mutatorLookup.GetValueOrDefault("Remove", null);
            var decrease = mutatorLookup.GetValueOrDefault("<", null);
            var increase = mutatorLookup.GetValueOrDefault(">", null);

            mutatorLookup.Remove("Add");
            mutatorLookup.Remove("Remove");
            mutatorLookup.Remove("<");
            mutatorLookup.Remove(">");
            var remainingWidth = ummWidth;
            // Indent
            remainingWidth -= 50;
            var titleWidth = (remainingWidth / (IsWide ? 3.0f : 4.0f)) - 100; ;
            remainingWidth -= titleWidth;
            if (feature != null) {
                bool canDecrease = decrease?.canPerform(blueprint, ch) ?? false;
                bool canIncrease = increase?.canPerform(blueprint, ch) ?? false;
                var rankFeature = feature as UnitFact;
                if ((canDecrease || canIncrease) && rankFeature != null) {
                    var v = rankFeature.GetRank();
                    decrease.BlueprintActionButton(ch, blueprint, () => todo.Add(() => decrease.action(blueprint, ch, repeatCount)), 60);
                    Space(10f);
                    Label($"{v}".orange().bold(), Width(30));
                    increase.BlueprintActionButton(ch, blueprint, () => todo.Add(() => increase.action(blueprint, ch, repeatCount)), 60);
                    Space(17);
                    remainingWidth -= 190;
                }
                else {
                    Space(190);
                    remainingWidth -= 190;
                }
            }
            else {
                Space(190);
                remainingWidth -= 190;
            }
            bool canAdd = add?.canPerform(blueprint, ch) ?? false;
            bool canRemove = remove?.canPerform(blueprint, ch) ?? false;
            if (canRemove) {
                remove.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { usedBrowser.needsReloadData = true; remove.action(blueprint, ch, repeatCount); }), 175);
            }
            if (canAdd) {
                add.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { usedBrowser.needsReloadData = true; add.action(blueprint, ch, repeatCount); }), 175);
            }
            remainingWidth -= 178;
            Space(20); remainingWidth -= 20;
            using (VerticalScope(Width(remainingWidth - 100))) {
                if (settings.showAssetIDs)
                    GUILayout.TextField(blueprint.AssetGuid.ToString(), AutoWidth());
                if (blueprint.Description != null) {
                    Label(blueprint.Description.StripHTML().green(), Width(remainingWidth - 100));
                }
            }
        }
        public static string getName<Definition>(Definition feature) where Definition : BlueprintScriptableObject, IUIDataProvider {
            bool isEmpty = feature.Name.IsNullOrEmpty();
            string name;
            if (isEmpty) {
                name = feature.name;
            }
            else {
                name = feature.Name;
                if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                    name = feature.name;
                }
                else if (settings.showDisplayAndInternalNames) {
                    name += $" : {feature.name.color(RGBA.darkgrey)}";
                }
            }
            return name;
        }
        public static List<Action> OnGUI<Item, Definition>(UnitEntityData ch, Browser<Item, Definition> browser, List<Item> fact, string name)
            where Item : UnitFact
            where Definition : BlueprintUnitFact {
            bool updateTree = false;
            List<Action> todo = new();
            if (showTree) {
                using (HorizontalScope()) {
                    Space(670);
                    Toggle("Show Tree", ref showTree, Width(250));
                }
                treeEditor.OnGUI(ch, updateTree);
            }
            else {
                browser.OnGUI(name,
                    fact,
                    () => GetBlueprints<Definition>(),
                    (feature) => (Definition)feature.Blueprint,
                    (feature) => getName(feature),
                    (feature) => $"{getName(feature)} {feature.NameSafe()} {feature.GetDisplayName()} " + (settings.searchesDescriptions ? feature.Description : ""),
                    (feature) => getName(feature),
                    () => {
                        using (HorizontalScope()) {
                            Toggle("Show GUIDs", ref Main.settings.showAssetIDs, 150.width());
                            20.space();
                            Toggle("Show Internal Names", ref settings.showDisplayAndInternalNames, 230.width());
                            20.space();
                            updateTree |= Toggle("Show Tree", ref showTree, Width(130));
                            20.space();
                            if (Toggle("Search Descriptions", ref settings.searchesDescriptions, 250.width())) {
                                browser.needsReloadData = true;
                                FeatureSelectionBrowser.needsReloadData = true;
                                ParameterizedFeatureBrowser.needsReloadData = true;
                            }
                        }
                    },
                    (feature, blueprint) => RowGUI(feature, blueprint, ch, browser, todo),
                    (fact, blueprint) => {
                        if (blueprint is BlueprintFeatureSelection featureSelection) {
                            return (entry, f) => FeatureSelectionBrowser.OnGUI(
                                $"{f.Name}-featureSelection",
                                ch.FeatureSelectionEntries(featureSelection),
                                () => featureSelection.AllFeatures.OrderBy(f => f.Name),
                                entry => entry.feature,
                                f => getName(f),
                                f => $"{getName(f)} {f.NameSafe()} {f.GetDisplayName()} " + (settings.searchesDescriptions ? f.Description : ""),
                                f => getName(f),
                                null,
                                (entry, f) => {
                                    if (entry != null) {
                                        Label($"{entry.level}", 50.width());
                                        10.space();
                                        Label($"{entry.data.Source.Blueprint.GetDisplayName()}", 200.width());
                                    }
                                    else
                                        268.space();
                                    if (ch.HasFeatureSelection(featureSelection, f))
                                        ActionButton("Remove", () => {
                                            ch.RemoveFeatureSelection(featureSelection, entry.data, f);
                                            FeatureSelectionBrowser.needsReloadData = true;
                                            browser.needsReloadData = true;
                                        }, 150.width());
                                    else
                                        ActionButton("Add", () => {
                                            ch.AddFeatureSelection(featureSelection, f);
                                            FeatureSelectionBrowser.needsReloadData = true;
                                            browser.needsReloadData = true;
                                        }, 150.width());
                                    15.space();
                                    Label(f.GetDescription().StripHTML().green());
                                }, null, 100);
                        }
                        else if (blueprint is BlueprintParametrizedFeature parametrizedFeature) {
                            return (_, item) => ParameterizedFeatureBrowser.OnGUI(
                                $"{item.Name}-parameterSelection",
                                ch.ParamterizedFeatureItems(parametrizedFeature),
                                () => parametrizedFeature.Items.OrderBy(i => i.Name),
                                i => i,
                                i => i.Name,
                                i => $"{i.Name} " + (settings.searchesDescriptions ? i.Description : ""),
                                i => i.Name,
                                null,
                                (_, i) => {
                                    if (ch.HasParamemterizedFeatureItem(parametrizedFeature, i))
                                        ActionButton("Remove", () => {
                                            ch.RemoveParameterizedFeatureItem(parametrizedFeature, i);
                                            FeatureSelectionBrowser.needsReloadData = true;
                                            browser.needsReloadData = true;
                                        }, 150.width());
                                    else
                                        ActionButton("Add", () => {
                                            ch.AddParameterizedFeatureItem(parametrizedFeature, i);
                                            FeatureSelectionBrowser.needsReloadData = true;
                                            browser.needsReloadData = true;
                                        }, 150.width());
                                    15.space();
                                    Label(i.Description.StripHTML().green());
                                }, null, 100);

                        }
                        return null;
                    }, 50, false, true, 100, 300, "", true);
            }
            return todo;
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Feature> feature) {
            var FeatureBrowser = FeatureBrowserDict.GetValueOrDefault(ch, null);
            if (FeatureBrowser == null) {
                FeatureBrowser = new Browser<Feature, BlueprintFeature>();
                FeatureBrowserDict[ch] = FeatureBrowser;
            }
            return OnGUI<Feature, BlueprintFeature>(ch, FeatureBrowser, feature, "Features");
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Buff> buff) {
            var BuffBrowser = BuffBrowserDict.GetValueOrDefault(ch, null);
            if (BuffBrowser == null) {
                BuffBrowser = new Browser<Buff, BlueprintBuff>();
                BuffBrowserDict[ch] = BuffBrowser;
            }
            return OnGUI<Buff, BlueprintBuff>(ch, BuffBrowser, buff, "Buffs");
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Ability> ability) {
            var AbilityBrowser = AbilityBrowserDict.GetValueOrDefault(ch, null);
            if (AbilityBrowser == null) {
                AbilityBrowser = new Browser<Ability, BlueprintAbility>();
                AbilityBrowserDict[ch] = AbilityBrowser;
            }
            return OnGUI<Ability, BlueprintAbility>(ch, AbilityBrowser, ability, "Abilities");
        }
    }
}
