// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
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
using ModKit.Utility;
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
        private static Settings Settings => Main.Settings;
        private static bool _showTree = false;
        private static readonly int repeatCount = 1;
        private static readonly FeaturesTreeEditor treeEditor = new();
        private static readonly CollectionChangedSubscriber collectionChangedSubscriber = new();

        private static readonly Dictionary<UnitEntityData, Browser<Feature, BlueprintFeature>> FeatureBrowserDict = new();
        private static readonly Dictionary<UnitEntityData, Browser<Buff, BlueprintBuff>> BuffBrowserDict = new();
        private static readonly Dictionary<UnitEntityData, Browser<Ability, BlueprintAbility>> AbilityBrowserDict = new();
        private static readonly Browser<FeatureSelectionEntry, BlueprintFeature> FeatureSelectionBrowser = new() { IsDetailBrowser = true };
        private static readonly Browser<IFeatureSelectionItem, IFeatureSelectionItem> ParameterizedFeatureBrowser = new() { IsDetailBrowser = true };

        public static void BlueprintRowGUI<Item, Definition>(Browser<Item, Definition> browser,
                                                             Item feature, 
                                                             Definition blueprint, 
                                                             UnitEntityData ch, 
                                                             List<Action> todo
                ) where Definition : BlueprintScriptableObject, IUIDataProvider {
            var remainingWidth = ummWidth;
            // Indent
            remainingWidth -= 50;
            var titleWidth = (remainingWidth / (IsWide ? 3.5f : 4.0f)) - 100;
            remainingWidth -= titleWidth;

            var text = GetTitle(blueprint).MarkedSubstring(browser.SearchText);
            var titleKey = $"{blueprint.AssetGuid}";
            if (feature != null) {
                text = text.Cyan().Bold();
            }
            if (blueprint is BlueprintFeatureSelection featureSelection
                || blueprint is BlueprintParametrizedFeature parametrizedFeature
                ) {
                if (Browser.DetailToggle(text, blueprint, blueprint, (int)titleWidth)) 
                    browser.ReloadData();
            }
            else
                Label(text, Width((int)titleWidth));

            var lastRect = GUILayoutUtility.GetLastRect();

            var mutatorLookup = BlueprintAction.ActionsForType(typeof(Definition)).Distinct().ToDictionary(a => a.name, a => a);
            var add = mutatorLookup.GetValueOrDefault("Add", null);
            var remove = mutatorLookup.GetValueOrDefault("Remove", null);
            var decrease = mutatorLookup.GetValueOrDefault("<", null);
            var increase = mutatorLookup.GetValueOrDefault(">", null);

            mutatorLookup.Remove("Add");
            mutatorLookup.Remove("Remove");
            mutatorLookup.Remove("<");
            mutatorLookup.Remove(">");
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
                } else {
                    Space(190);
                    remainingWidth -= 190;
                }
            } else {
                Space(190);
                remainingWidth -= 190;
            }
            var canAdd = add?.canPerform(blueprint, ch) ?? false;
            var canRemove = remove?.canPerform(blueprint, ch) ?? false;
            if (canRemove) {
                remove.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { browser.needsReloadData = true; remove.action(blueprint, ch, repeatCount); }), 150);
            }
            if (canAdd) {
                add.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { browser.needsReloadData = true; add.action(blueprint, ch, repeatCount); }), 150);
            }
            remainingWidth -= 178;
            Space(20); remainingWidth -= 20;
            ReflectionTreeView.DetailToggle("", blueprint, feature != null ? feature : blueprint, 0);
            using (VerticalScope(Width(remainingWidth - 100))) {
                try {
                    if (Settings.showAssetIDs)
                        GUILayout.TextField(blueprint.AssetGuid.ToString(), AutoWidth());
                    Label(blueprint.Description.StripHTML().MarkedSubstring(browser.SearchText).green(), Width(remainingWidth - 100));
                }
                catch (Exception e) {
                    Mod.Warn($"Error in blueprint: {blueprint.AssetGuid}");
                    Mod.Warn($"         name: {blueprint.name}");
                    Mod.Error(e);
                }
            }
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
            } else {
                browser.OnGUI(
                    fact,
                    GetBlueprints<Definition>,
                    (feature) => (Definition)feature.Blueprint,
                    (blueprint) => $"{GetSearchKey(blueprint)}" + (Settings.searchDescriptions ? $"{blueprint.Description}" : ""), 
                    GetSortKey,
                    () => {
                        using (HorizontalScope()) {
                            var reloadData = false;
                            Toggle("Show GUIDs", ref Main.Settings.showAssetIDs);
                            20.space();
                            reloadData |= Toggle("Show Internal Names", ref Settings.showDisplayAndInternalNames);
                            20.space();
                            updateTree |= Toggle("Show Tree", ref _showTree);
                            20.space();
                            //Toggle("Show Inspector", ref Settings.factEditorShowInspector);
                            //20.space();
                            reloadData |= Toggle("Search Descriptions", ref Settings.searchDescriptions);
                            if (reloadData) {
                                browser.ResetSearch();
                                FeatureSelectionBrowser.ResetSearch();
                                ParameterizedFeatureBrowser.ResetSearch();
                            }
                        }
                    },
                    (feature, blueprint) => BlueprintRowGUI(browser,feature, blueprint, ch, todo),
                    (feature, blueprint) => {
                        ReflectionTreeView.OnDetailGUI(blueprint);
                        switch (blueprint) {
                            case BlueprintFeatureSelection featureSelection:
                                Browser.OnDetailGUI(blueprint, bp => {
                                    FeatureSelectionBrowser.needsReloadData |= browser.needsReloadData;
                                    FeatureSelectionBrowser.OnGUI(
                                    ch.FeatureSelectionEntries(featureSelection),
                                    () =>
                                      featureSelection.AllFeatures.OrderBy(f => f.Name),
                                    e => e.feature,
                                    f => $"{GetSearchKey(f)} " + (Settings.searchDescriptions ? f.Description : ""),
                                    GetTitle,
                                    null,
                                    (selectionEntry, f) => {
                                        var title = GetTitle(f).MarkedSubstring(FeatureSelectionBrowser.SearchText);
                                        if (selectionEntry != null) title = title.Cyan().Bold();
                                        var titleWidth = (ummWidth / (IsWide ? 3.5f : 4.0f)) - 200;
                                        Label(title, Width(titleWidth));
                                        78.space();
                                        if (selectionEntry != null) {
                                            var level = selectionEntry.level;
                                            Space(-25);
                                            using (VerticalScope(125)) {
                                                using (HorizontalScope(125)) {
                                                    Label("sel lvl", 50.width());
                                                    if (ValueAdjuster(ref level, 1, 0, 20, false)) {
                                                        ch.RemoveFeatureSelection(featureSelection,
                                                            selectionEntry.data,
                                                            f);
                                                        ch.AddFeatureSelection(featureSelection, f, level);
                                                        FeatureSelectionBrowser.ReloadData();
                                                        browser.ReloadData();
                                                    }
                                                }
                                            }
                                            20.space();
                                            Label($"{selectionEntry.data.Source.Blueprint.GetDisplayName()}",
                                                  250.width());
                                        }
                                        else
                                            354.space();
                                        if (ch.HasFeatureSelection(featureSelection, f))
                                            ActionButton("Remove",
                                                         () => {
                                                             if (selectionEntry == null) return;
                                                             ch.RemoveFeatureSelection(featureSelection, selectionEntry.data, f);
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
                                        Label(f.GetDescription().StripHTML().MarkedSubstring(FeatureSelectionBrowser.SearchText).green());
                                    },
                                    null,
                                    100);
                                });
                                break;
                            case BlueprintParametrizedFeature parametrizedFeature:
                                Browser.OnDetailGUI(blueprint, bp => {
                                    ParameterizedFeatureBrowser.needsReloadData |= browser.needsReloadData;
                                    ParameterizedFeatureBrowser.OnGUI(
                                      ch.ParameterizedFeatureItems(parametrizedFeature),
                                      () => parametrizedFeature.Items.OrderBy(i => i.Name),
                                      i => i,
                                      i => $"{i.Name} " + (Settings.searchDescriptions ? i.Param?.Blueprint?.GetDescription() : ""),
                                      i => i.Name,
                                      null,
                                      (item, i) => {
                                          var title = i.Name.MarkedSubstring(ParameterizedFeatureBrowser.SearchText);
                                          if (item != null) title = title.Cyan().Bold();

                                          var titleWidth = (ummWidth / (IsWide ? 3.5f : 4.0f));
                                          Label(title, Width(titleWidth));
                                          25.space();
                                          if (ch.HasParameterizedFeatureItem(parametrizedFeature, i))
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
                                          Label(i.Param?.Blueprint?.GetDescription().StripHTML().MarkedSubstring(ParameterizedFeatureBrowser.SearchText).green());
                                      }, null, 100);
                                    });
                                    break;
                            }
                    }, 50, false, true, 100, 300, "", true);
            }
            return todo;
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Feature> feature) {
            var featureBrowser = FeatureBrowserDict.GetValueOrDefault(ch, null);
            if (featureBrowser == null) {
                featureBrowser = new Browser<Feature, BlueprintFeature>(true, true) {};
                FeatureBrowserDict[ch] = featureBrowser;
            }
            return OnGUI(ch, featureBrowser, feature, "Features");
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Buff> buff) {
            var buffBrowser = BuffBrowserDict.GetValueOrDefault(ch, null);
            if (buffBrowser == null) {
                buffBrowser = new Browser<Buff, BlueprintBuff>(true, true);
                BuffBrowserDict[ch] = buffBrowser;
            }
            return OnGUI(ch, buffBrowser, buff, "Buffs");
        }
        public static List<Action> OnGUI(UnitEntityData ch, List<Ability> ability) {
            var abilityBrowser = AbilityBrowserDict.GetValueOrDefault(ch, null);
            if (abilityBrowser == null) {
                abilityBrowser = new Browser<Ability, BlueprintAbility>(true, true);
                AbilityBrowserDict[ch] = abilityBrowser;
            }
            return OnGUI(ch, abilityBrowser, ability, "Abilities");
        }
    }
}
