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
using ModKit.DataViewer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private static Settings Settings => Main.settings;
        private static bool _showTree = false;
        private static readonly int repeatCount = 1;
        private static readonly FeaturesTreeEditor treeEditor = new();
        private static readonly CollectionChangedSubscriber collectionChangedSubscriber = new();

        private static readonly Dictionary<UnitEntityData, Browser<Feature, BlueprintFeature>> FeatureBrowserDict = new();
        private static readonly Dictionary<UnitEntityData, Browser<Buff, BlueprintBuff>> BuffBrowserDict = new();
        private static readonly Dictionary<UnitEntityData, Browser<Ability, BlueprintAbility>> AbilityBrowserDict = new();
        private static readonly Browser<FeatureSelectionEntry, BlueprintFeature> FeatureSelectionBrowser = new() { SearchLimit = 12 };
        private static readonly Browser<IFeatureSelectionItem, IFeatureSelectionItem> ParameterizedFeatureBrowser = new() { SearchLimit = 12 };
        private static SimpleBlueprint _selectedDetailsBlueprint = null;


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
            var titleWidth = (remainingWidth / (IsWide ? 3.5f : 4.0f)) - 100;
            remainingWidth -= titleWidth;
            if (feature != null) {
                bool canDecrease = decrease?.canPerform(blueprint, ch) ?? false;
                bool canIncrease = increase?.canPerform(blueprint, ch) ?? false;
                if ((canDecrease || canIncrease) && feature is UnitFact rankFeature) {
                    var v = rankFeature.GetRank();
                    decrease.BlueprintActionButton(ch, blueprint, () => todo.Add(() => decrease!.action(blueprint, ch, repeatCount)), 60);
                    Space(10f);
                    Label($"{v}".orange().bold(), Width(30));
                    increase.BlueprintActionButton(ch, blueprint, () => todo.Add(() => increase!.action(blueprint, ch, repeatCount)), 60);
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
            var canAdd = add?.canPerform(blueprint, ch) ?? false;
            var canRemove = remove?.canPerform(blueprint, ch) ?? false;
            if (canRemove) {
                remove.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { usedBrowser.needsReloadData = true; remove.action(blueprint, ch, repeatCount); }), 175);
            }
            if (canAdd) {
                add.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { usedBrowser.needsReloadData = true; add.action(blueprint, ch, repeatCount); }), 175);
            }
            remainingWidth -= 178;
            Space(20); remainingWidth -= 20;
            ReflectionTreeView.DetailToggle("", blueprint, blueprint, 0);
            using (VerticalScope(Width(remainingWidth - 100))) {
                if (Settings.showAssetIDs)
                    GUILayout.TextField(blueprint.AssetGuid.ToString(), AutoWidth());
                Label(blueprint.Description.StripHTML().green(), Width(remainingWidth - 100));
            }
        }
        public static string GetName<Definition>(Definition feature) where Definition : BlueprintScriptableObject, IUIDataProvider {
            var isEmpty = feature.Name.IsNullOrEmpty();
            string name;
            if (isEmpty) {
                name = feature.name;
            }
            else {
                name = feature.Name;
                if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                    name = feature.name;
                }
                else if (Settings.showDisplayAndInternalNames) {
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
            if (_showTree) {
                using (HorizontalScope()) {
                    Space(670);
                    Toggle("Show Tree", ref _showTree, Width(250));
                }
                treeEditor.OnGUI(ch, updateTree);
            }
            else {
                browser.OnGUI(name,
                    fact,
                    GetBlueprints<Definition>,
                    (feature) => (Definition)feature.Blueprint,
                    GetName,
                    (feature) => $"{GetName(feature)} {feature.NameSafe()} {feature.GetDisplayName()} " + (Settings.searchesDescriptions ? feature.Description : ""),
                    GetName,
                    () => {
                        using (HorizontalScope()) {
                            Toggle("Show GUIDs", ref Main.settings.showAssetIDs);
                            20.space();
                            Toggle("Show Internal Names", ref Settings.showDisplayAndInternalNames);
                            20.space();
                            updateTree |= Toggle("Show Tree", ref _showTree);
                            20.space();
                            //Toggle("Show Inspector", ref Settings.factEditorShowInspector);
                            //20.space();
                            if (Toggle("Search Descriptions", ref Settings.searchesDescriptions)) {
                                browser.needsReloadData = true;
                                FeatureSelectionBrowser.needsReloadData = true;
                                ParameterizedFeatureBrowser.needsReloadData = true;
                            }
                        }
                    },
                    (feature, blueprint) => RowGUI(feature, blueprint, ch, browser, todo),
                    (feature, blueprint) => ReflectionTreeView.DetailsOnGUI(blueprint),
                    (unitFact, blueprint) => {
                        if (blueprint is BlueprintFeatureSelection featureSelection) {
                            if (blueprint != _selectedDetailsBlueprint) FeatureSelectionBrowser.ReloadData();
                            return (entry, f) => FeatureSelectionBrowser.OnGUI(
                                    $"{f.Name}-featureSelection",
                                    ch.FeatureSelectionEntries(featureSelection),
                                    () => featureSelection.AllFeatures.OrderBy(f => f.Name),
                                    e => e.feature,
                                    GetName,
                                    f => $"{GetName(f)} {f.NameSafe()} {f.GetDisplayName()} " + (Settings.searchesDescriptions ? f.Description : ""),
                                    GetName,
                                    null,
                                    (selectionEntry, f) => {
                                        if (selectionEntry != null) {
                                            var level = selectionEntry.level;
                                            if (ValueAdjuster(ref level, 1, 0, 20, false)) {
                                                ch.RemoveFeatureSelection(featureSelection, selectionEntry.data, f);
                                                ch.AddFeatureSelection(featureSelection, f, level);
                                                FeatureSelectionBrowser.ReloadData();
                                                browser.ReloadData();
                                            }
                                            10.space();
                                            Label($"{selectionEntry.data.Source.Blueprint.GetDisplayName()}",
                                                  250.width());
                                        }
                                        else
                                            354.space();
                                        if (ch.HasFeatureSelection(featureSelection, f))
                                            ActionButton("Remove",
                                                         () => {
                                                             if (selectionEntry == null) return;
                                                             ch.RemoveFeatureSelection(featureSelection,
                                                                 selectionEntry.data,
                                                                 f);
                                                             FeatureSelectionBrowser.needsReloadData = true;
                                                             browser.needsReloadData = true;
                                                         },
                                                         150.width());
                                        else
                                            ActionButton("Add",
                                                         () => {
                                                             ch.AddFeatureSelection(featureSelection, f);
                                                             FeatureSelectionBrowser.needsReloadData = true;
                                                             browser.needsReloadData = true;
                                                         },
                                                         150.width());
                                        15.space();
                                        Label(f.GetDescription().StripHTML().green());
                                    }, null, null, 100);
                            }
                        else if (blueprint is BlueprintParametrizedFeature parametrizedFeature) {
                            if (blueprint != _selectedDetailsBlueprint) ParameterizedFeatureBrowser.ReloadData();
                            _selectedDetailsBlueprint = blueprint;
                            return (_, item) => ParameterizedFeatureBrowser.OnGUI(
                             $"{item.Name}-parameterSelection",
                             ch.ParameterizedFeatureItems(parametrizedFeature),
                             () => parametrizedFeature.Items.OrderBy(i => i.Name),
                             i => i,
                             i => i.Name,
                             i => $"{i.Name} "
                                  + (Settings.searchesDescriptions ? i.Param?.Blueprint?.GetDescription() : ""),
                             i => i.Name,
                             null,
                             (_, i) => {
                                 if (ch.HasParameterizedFeatureItem(parametrizedFeature, i))
                                     ActionButton("Remove",
                                                  () => {
                                                      ch.RemoveParameterizedFeatureItem(parametrizedFeature, i);
                                                      FeatureSelectionBrowser.needsReloadData = true;
                                                      browser.needsReloadData = true;
                                                  },
                                                  150.width());
                                 else
                                     ActionButton("Add",
                                                  () => {
                                                      ch.AddParameterizedFeatureItem(parametrizedFeature, i);
                                                      FeatureSelectionBrowser.needsReloadData = true;
                                                      browser.needsReloadData = true;
                                                  },
                                                  150.width());
                                 15.space();
                                 Label(i.Param?.Blueprint?.GetDescription().StripHTML().green());
                             },
                             null,
                             null,
                             100);
                        }
                        return null;
                    }, 50, false, true, 100, 300, "", true);
            }
            return todo;
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Feature> feature) {
            var featureBrowser = FeatureBrowserDict.GetValueOrDefault(ch, null);
            if (featureBrowser == null) {
                featureBrowser = new Browser<Feature, BlueprintFeature>();
                FeatureBrowserDict[ch] = featureBrowser;
            }
            return OnGUI(ch, featureBrowser, feature, "Features");
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Buff> buff) {
            var buffBrowser = BuffBrowserDict.GetValueOrDefault(ch, null);
            if (buffBrowser == null) {
                buffBrowser = new Browser<Buff, BlueprintBuff>();
                BuffBrowserDict[ch] = buffBrowser;
            }
            return OnGUI(ch, buffBrowser, buff, "Buffs");
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Ability> ability) {
            var abilityBrowser = AbilityBrowserDict.GetValueOrDefault(ch, null);
            if (abilityBrowser == null) {
                abilityBrowser = new Browser<Ability, BlueprintAbility>();
                AbilityBrowserDict[ch] = abilityBrowser;
            }
            return OnGUI(ch, abilityBrowser, ability, "Abilities");
        }
    }
}
