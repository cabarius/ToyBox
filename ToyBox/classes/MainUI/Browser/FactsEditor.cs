// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModKit.UI;

namespace ToyBox {
    public class FactsEditor {
        private static Settings settings => Main.settings;
        private static bool showTree = false;
        private static readonly int repeatCount = 1;
        private static readonly FeaturesTreeEditor treeEditor = new();
        public static void RowGUI<Item, Definition>(Item feature, Definition blueprint, UnitEntityData ch, Browser<Item, Definition> usedBrowser, List<Action> todo) where Definition : BlueprintScriptableObject, IUIDataProvider {
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
                remove.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { remove.action(blueprint, ch, repeatCount); usedBrowser.reloadData = true; }), 175);
            }
            if (canAdd) {
                add.BlueprintActionButton(ch, blueprint, () => todo.Add(() => { add.action(blueprint, ch, repeatCount); usedBrowser.reloadData = true; }), 175);
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
            if (settings.showDisplayAndInternalNames) {
                if (isEmpty) {
                    name = feature.name.cyan().bold();
                }
                else {
                    name = feature.Name;
                    if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                        name = feature.name.cyan().bold();
                    }
                    else {
                        name = name.cyan().bold() + $" : {feature.name.color(RGBA.darkgrey)}";
                    }
                }
            }
            else {
                if (isEmpty) {
                    name = feature.name;
                }
                else {
                    name = feature.Name;
                    if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                        name = feature.name;
                    }
                }
                name = name.cyan().bold();
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
                    () => BlueprintExtensions.GetBlueprints<Definition>(),
                    (feature) => (Definition)feature.Blueprint,
                    (feature) => getName(feature),
                    (feature) => $"{getName(feature)} {feature.NameSafe()} {feature.GetDisplayName()} {feature.Description}",
                    (feature) => getName(feature),
                    () => {
                        using (HorizontalScope()) {
                            Toggle("Show GUIDs", ref Main.settings.showAssetIDs, Width(250));
                            60.space();
                            Toggle("Show Display & Internal Names", ref settings.showDisplayAndInternalNames, Width(250));
                            60.space();
                            updateTree |= Toggle("Show Tree", ref showTree, Width(250));
                        }
                    },
                    (feature, blueprint) => RowGUI(feature, blueprint, ch, browser, todo), null, 50, false, true, 100, 300, "", true);
            }
            return todo;
        }
        private static Dictionary<UnitEntityData, Browser<Feature, BlueprintFeature>> FeatureBrowserDict = new();
        private static Dictionary<UnitEntityData, Browser<Buff, BlueprintBuff>> BuffBrowserDict = new();
        private static Dictionary<UnitEntityData, Browser<Ability, BlueprintAbility>> AbilityBrowserDict = new();
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
