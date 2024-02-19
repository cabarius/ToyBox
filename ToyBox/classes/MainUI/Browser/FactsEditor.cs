// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Facts;
using Kingmaker.Utility;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using ToyBox.BagOfPatches;
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

        private static readonly Dictionary<BaseUnitEntity, Browser<BlueprintFeature, Feature>> FeatureBrowserDict = new();
        private static readonly Dictionary<BaseUnitEntity, Browser<BlueprintBuff, Buff>> BuffBrowserDict = new();
        private static readonly Dictionary<BaseUnitEntity, Browser<BlueprintMechanicEntityFact, MechanicEntityFact>> AbilityBrowserDict = new();
        public static void BlueprintRowGUI<Item, Definition>(Browser<Definition, Item> browser,
                                                             Item feature,
                                                             Definition blueprint,
                                                             BaseUnitEntity ch,
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
                ) {
                if (Browser.DetailToggle(text, blueprint, feature != null ? feature : blueprint, (int)titleWidth))
                    browser.ReloadData();
            }
            else
                Label(text, Width((int)titleWidth));

            var lastRect = GUILayoutUtility.GetLastRect();

            var mutatorLookup = BlueprintAction.ActionsForType(blueprint.GetType())
                .GroupBy(a => a.name).Select(g => g.FirstOrDefault())
                .ToDictionary(a => a.name, a => a);
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
                if ((canDecrease || canIncrease) && feature is MechanicEntityFact rankFeature) {
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
                        ClipboardLabel(blueprint.AssetGuid.ToString(), AutoWidth());
                    Label(blueprint.Description.StripHTML().MarkedSubstring(browser.SearchText).green(), Width(remainingWidth - 100));
                }
                catch (Exception e) {
                    Mod.Warn($"Error in blueprint: {blueprint.AssetGuid}");
                    Mod.Warn($"         name: {blueprint.name}");
                    Mod.Error(e);
                }
            }
        }
        public static void BlueprintDetailGUI<Item, Definition, k, v>(Definition blueprint, Item feature, BaseUnitEntity ch, Browser<k, v> browser)
            where Item : MechanicEntityFact
            where Definition : BlueprintMechanicEntityFact {
            // TODO: RT
        }
        public static List<Action> OnGUI<Item, Definition>(BaseUnitEntity ch, Browser<Definition, Item> browser, List<Item> fact, string name)
            where Item : MechanicEntityFact
            where Definition : BlueprintMechanicEntityFact {
            bool updateTree = false;
            List<Action> todo = new();
            if (_showTree) {
                using (HorizontalScope()) {
                    Space(670);
                    Toggle("Show Tree".localize(), ref _showTree, Width(250));
                }
                treeEditor.OnGUI(ch, updateTree);
            }
            else {
                browser.OnGUI(
                    fact,
                    () => {
                        var types = fact.GroupBy(f => f.Blueprint.GetType()).Select(g => g.FirstOrDefault().Blueprint.GetType());
                        return GetBlueprints<Definition>()?.Where(bp => types.Contains(bp.GetType()));
                    },
                    (feature) => (Definition)feature.Blueprint,
                    (blueprint) => $"{GetSearchKey(blueprint)}" + (Settings.searchDescriptions ? $"{blueprint.Description}" : ""),
                    blueprint => new[] { GetSortKey(blueprint) },
                    () => {
                        using (HorizontalScope()) {
                            var reloadData = false;
                            Toggle("Show GUIDs".localize(), ref Main.Settings.showAssetIDs);
                            20.space();
                            reloadData |= Toggle("Show Internal Names".localize(), ref Settings.showDisplayAndInternalNames);
                            20.space();
                            updateTree |= Toggle("Show Tree".localize(), ref _showTree);
                            20.space();
                            //Toggle("Show Inspector", ref Settings.factEditorShowInspector);
                            //20.space();
                            reloadData |= Toggle("Search Descriptions".localize(), ref Settings.searchDescriptions);
                            if (reloadData) {
                                browser.ResetSearch();
                            }
                        }
                    },
                    (blueprint, feature) => BlueprintRowGUI(browser, feature, blueprint, ch, todo),
                    (blueprint, feature) => {
                        ReflectionTreeView.OnDetailGUI(blueprint);
                        BlueprintDetailGUI(blueprint, feature, ch, browser);
                    }, 50, false, true, 100, 300, "", true);
            }
            return todo;
        }
        public static List<Action> OnGUI(BaseUnitEntity ch, List<Feature> feature) {
            var featureBrowser = FeatureBrowserDict.GetValueOrDefault(ch, null);
            if (featureBrowser == null) {
                featureBrowser = new Browser<BlueprintFeature, Feature>(Mod.ModKitSettings.searchAsYouType, true) { };
                FeatureBrowserDict[ch] = featureBrowser;
            }
            return OnGUI(ch, featureBrowser, feature, "Features");
        }
        public static List<Action> OnGUI(BaseUnitEntity ch, List<Buff> buff) {
            var buffBrowser = BuffBrowserDict.GetValueOrDefault(ch, null);
            if (buffBrowser == null) {
                buffBrowser = new Browser<BlueprintBuff, Buff>(Mod.ModKitSettings.searchAsYouType, true);
                BuffBrowserDict[ch] = buffBrowser;
            }
            return OnGUI(ch, buffBrowser, buff, "Buffs");
        }
        public static List<Action> OnGUI(BaseUnitEntity ch, List<Ability> ability, List<ActivatableAbility> activatable) {
            var abilityBrowser = AbilityBrowserDict.GetValueOrDefault(ch, null);
            var combined = new List<MechanicEntityFact>();
            if (abilityBrowser == null) {
                abilityBrowser = new Browser<BlueprintMechanicEntityFact, MechanicEntityFact>(Mod.ModKitSettings.searchAsYouType, true);
                AbilityBrowserDict[ch] = abilityBrowser;
            }
            combined.AddRange(ability);
            combined.AddRange(activatable);
            return OnGUI(ch, abilityBrowser, combined, "Abilities");
        }
    }
}
