// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.Utility;
using ModKit;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Items;

namespace ToyBox {
    public class BlueprintListUI {
        public delegate void NavigateTo(params string[] argv);
        public static Settings settings => Main.settings;

        public static int repeatCount = 1;
        public static bool hasRepeatableAction = false;
        public static int maxActions = 0;
        public static bool needsLayout = true;
        public static int[] ParamSelected = new int[1000];
        public static Dictionary<BlueprintParametrizedFeature, string[]> paramBPValueNames = new() { };
        public static Dictionary<BlueprintFeatureSelection, string[]> selectionBPValuesNames = new() { };

        public static void OnGUI(UnitEntityData unit,
            IEnumerable<SimpleBlueprint> blueprints,
            float indent = 0, float remainingWidth = 0,
            Func<string, string> titleFormater = null,
            NamedTypeFilter typeFilter = null,
            NavigateTo navigateTo = null
        ) {
            if (titleFormater == null) titleFormater = (t) => t.orange().bold();
            if (remainingWidth == 0) remainingWidth = UI.ummWidth - indent;
            var index = 0;
            IEnumerable<SimpleBlueprint> simpleBlueprints = blueprints.ToList();
            if (needsLayout) {
                foreach (var blueprint in simpleBlueprints) {
                    var actions = blueprint.GetActions();
                    if (actions.Any(a => a.isRepeatable)) hasRepeatableAction = true;
                    var actionCount = actions.Sum(action => action.canPerform(blueprint, unit) ? 1 : 0);
                    maxActions = Math.Max(actionCount, maxActions);
                }
                needsLayout = false;
            }
            if (hasRepeatableAction) {
                UI.BeginHorizontal();
                UI.Label("", UI.MinWidth(350 - indent), UI.MaxWidth(600));
                UI.ActionIntTextField(
                    ref repeatCount,
                    "repeatCount",
                    (limit) => { },
                    () => { },
                    UI.Width(160));
                UI.Space(40);
                UI.Label("Parameter".cyan() + ": " + $"{repeatCount}".orange(), UI.ExpandWidth(false));
                repeatCount = Math.Max(1, repeatCount);
                repeatCount = Math.Min(100, repeatCount);
                UI.EndHorizontal();
            }
            UI.Div(indent);
            var count = 0;
            foreach (var blueprint in simpleBlueprints) {
                var currentCount = count++;
                var description = blueprint.GetDescription();
                if (blueprint is BlueprintItem itemBlueprint && itemBlueprint.FlavorText?.Length > 0)
                    description = $"{itemBlueprint.FlavorText.StripHTML().color(RGBA.notable)}\n{description}";
                float titleWidth = 0;
                var remWidth = remainingWidth - indent;
                using (UI.HorizontalScope()) {
                    UI.Space(indent);
                    var actions = blueprint.GetActions()
                        .Where(action => action.canPerform(blueprint, unit));
                    var titles = actions.Select(a => a.name);
                    var title = blueprint.NameSafe();
                    if (titles.Contains("Remove") || titles.Contains("Lock")) {
                        title = title.cyan().bold();
                    }
                    else {
                        title = titleFormater(title);
                    }
                    titleWidth = (remainingWidth / (UI.IsWide ? 3 : 4)) - indent;
                    UI.Label(title, UI.Width(titleWidth));
                    remWidth -= titleWidth;
                    var actionCount = actions != null ? actions.Count() : 0;
                    var lockIndex = titles.IndexOf("Lock");
                    if (blueprint is BlueprintUnlockableFlag flagBP) {
                        // special case this for now
                        if (lockIndex >= 0) {
                            var flags = Game.Instance.Player.UnlockableFlags;
                            var lockAction = actions.ElementAt(lockIndex);
                            UI.ActionButton("<", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) - 1); }, UI.Width(50));
                            UI.Space(25);
                            UI.Label($"{flags.GetFlagValue(flagBP)}".orange().bold(), UI.MinWidth(50));
                            UI.ActionButton(">", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) + 1); }, UI.Width(50));
                            UI.Space(50);
                            UI.ActionButton(lockAction.name, () => { lockAction.action(blueprint, unit, repeatCount); }, UI.Width(120));
                            UI.Space(100);
#if DEBUG
                            UI.Label(flagBP.GetDescription().green());
#endif
                        }
                        else {
                            var unlockIndex = titles.IndexOf("Unlock");
                            if (unlockIndex >= 0) {
                                var unlockAction = actions.ElementAt(unlockIndex);
                                UI.Space(240);
                                UI.ActionButton(unlockAction.name, () => { unlockAction.action(blueprint, unit, repeatCount); }, UI.Width(120));
                                UI.Space(100);
                            }
                        }
                        remWidth -= 300;
                    }
                    else {
                        for (var ii = 0; ii < maxActions; ii++) {
                            if (ii < actionCount) {
                                var action = actions.ElementAt(ii);
                                // TODO -don't show increase or decrease actions until we redo actions into a proper value editor that gives us Add/Remove and numeric item with the ability to show values.  For now users can edit ranks in the Facts Editor
                                if (action.name == "<" || action.name == ">") {
                                    UI.Space(174); continue;
                                }
                                var actionName = action.name;
                                float extraSpace = 0;
                                if (action.isRepeatable) {
                                    actionName += action.isRepeatable ? $" {repeatCount}" : "";
                                    extraSpace = 20 * (float)Math.Ceiling(Math.Log10((double)repeatCount));
                                }
                                UI.ActionButton(actionName, () => action.action(blueprint, unit, repeatCount, currentCount), UI.Width(160 + extraSpace));
                                UI.Space(10);
                                remWidth -= 174.0f + extraSpace;

                            }
                            else {
                                UI.Space(174);
                            }
                        }
                    }
                    UI.Space(10);
                    var typeString = blueprint.GetType().Name;
                    var navigateStrings = new List<string> { typeString };
                    if (typeFilter?.collator != null) {
                        var names = typeFilter.collator(blueprint);
                        if (names.Count > 0) {
                            var collatorString = names.First();
                            if (blueprint is BlueprintItem itemBP) {
                                var rarity = itemBP.Rarity();
                                typeString = $"{typeString} - {rarity}".Rarity(rarity);
                            }
                            if (!typeString.Contains(collatorString)) {
                                typeString += $" : {collatorString}".yellow();
                                navigateStrings.Add(collatorString);
                            }
                        }
                    }
                    var attributes = "";
                    if (settings.showAttributes) {
                        var attr = string.Join(" ", blueprint.Attributes());
                        if (!typeString.Contains(attr))
                            attributes = attr;
                    }

                    if (attributes.Length > 1) typeString += $" - {attributes.orange()}";

                    if (description != null && description.Length > 0) description = $"{description}";
                    else description = "";
                    if (blueprint is BlueprintScriptableObject bpso) {
                        if (settings.showComponents && bpso.ComponentsArray?.Length > 0) {
                            var componentStr = string.Join<object>(" ", bpso.ComponentsArray).color(RGBA.teal);
                            if (description.Length == 0) description = componentStr;
                            else description = componentStr + "\n" + description;
                        }
                        if (settings.showElements && bpso.ElementsArray?.Count > 0) {
                            var elementsStr = string.Join<object>(" ", bpso.ElementsArray).magenta();
                            if (description.Length == 0) description = elementsStr;
                            else description = elementsStr + "\n" + description;
                        }
                    }
                    using (UI.VerticalScope(UI.Width(remWidth))) {
                        using (UI.HorizontalScope(UI.Width(remWidth))) {
                            UI.Space(-17);
                            if (settings.showAssetIDs) {
                                UI.ActionButton(typeString, () => navigateTo?.Invoke(navigateStrings.ToArray()), UI.rarityButtonStyle);
                                GUILayout.TextField(blueprint.AssetGuid.ToString(), UI.ExpandWidth(false));
                            }
                            else UI.ActionButton(typeString, () => navigateTo?.Invoke(navigateStrings.ToArray()), UI.rarityButtonStyle);
                            UI.Space(17);
                        }
                        if (description.Length > 0) UI.Label(description.green(), UI.Width(remWidth));
                    }
                }
                if (blueprint is BlueprintParametrizedFeature paramBP) {
                    using (UI.HorizontalScope()) {
                        UI.Space(titleWidth);
                        using (UI.VerticalScope()) {
                            using (UI.HorizontalScope(GUI.skin.button)) {
                                var content = new GUIContent($"{paramBP.Name.yellow()}");
                                var labelWidth = GUI.skin.label.CalcSize(content).x;
                                UI.Space(indent);
                                //UI.Space(indent + titleWidth - labelWidth - 25);
                                UI.Label(content, UI.Width(labelWidth));
                                UI.Space(25);
                                var nameStrings = paramBPValueNames.GetValueOrDefault(paramBP, null);
                                if (nameStrings == null) {
                                    nameStrings = paramBP.Items.Select(x => x.Name).OrderBy(x => x).ToArray().TrimCommonPrefix();
                                    paramBPValueNames[paramBP] = nameStrings;
                                }
                                UI.ActionSelectionGrid(
                                    ref ParamSelected[currentCount],
                                    nameStrings,
                                    6,
                                    (selected) => { },
                                    GUI.skin.toggle,
                                    UI.Width(remWidth)
                                );
                                //UI.SelectionGrid(ref ParamSelected[currentCount], nameStrings, 6, UI.Width(remWidth + titleWidth)); // UI.Width(remWidth));
                            }
                            UI.Space(15);
                        }
                    }
                }
                if (blueprint is BlueprintFeatureSelection selectionBP) {
                    using (UI.HorizontalScope()) {
                        UI.Space(titleWidth);
                        using (UI.VerticalScope()) {
                            var needsSelection = false;
                            var nameStrings = selectionBPValuesNames.GetValueOrDefault(selectionBP, null);
                            if (nameStrings == null) {
                                needsSelection = true;
                                nameStrings = selectionBP.AllFeatures.Select(x => x.Name).OrderBy(x => x).ToArray().TrimCommonPrefix();
                                selectionBPValuesNames[selectionBP] = nameStrings;
                            }
                            using (UI.HorizontalScope(GUI.skin.button)) {
                                var content = new GUIContent($"{selectionBP.Name.yellow()}");
                                var labelWidth = GUI.skin.label.CalcSize(content).x;
                                UI.Space(indent);
                                //UI.Space(indent + titleWidth - labelWidth - 25);
                                UI.Label(content, UI.Width(labelWidth));
                                UI.Space(25);

                                UI.ActionSelectionGrid(
                                    ref ParamSelected[currentCount],
                                    nameStrings,
                                    4,
                                    (selected) => { },
                                    GUI.skin.toggle,
                                    UI.Width(remWidth)
                                );
                                //UI.SelectionGrid(ref ParamSelected[currentCount], nameStrings, 6, UI.Width(remWidth + titleWidth)); // UI.Width(remWidth));
                            }
                            if (unit.Progression.Selections.TryGetValue(selectionBP, out var selectionData)) {
                                foreach (var entry in selectionData.SelectionsByLevel) {
                                    foreach (var selection in entry.Value) {
                                        if (needsSelection) {
                                            ParamSelected[currentCount] = selectionBP.AllFeatures.IndexOf(selection);
                                            needsSelection = false;
                                        }
                                        using (UI.HorizontalScope()) {
                                            UI.ActionButton("Remove", () => {

                                            }, UI.Width(160));
                                            UI.Space(25);
                                            UI.Label($"{entry.Key} ".yellow() + selection.Name.orange(), UI.Width(250));
                                            UI.Space(25);
                                            UI.Label(selection.Description.StripHTML().green());
                                        }
                                    }
                                }
                            }
                            UI.Space(15);
                        }
                    }
                }
                UI.Div(indent);
                index++;
            }
        }
    }
}