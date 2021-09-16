// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
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
using ModKit;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.JsonSystem;

namespace ToyBox {
    public class BlueprintListUI {
        public static Settings settings { get { return Main.settings; } }

        public static int repeatCount = 1;
        public static bool hasRepeatableAction = false;
        public static int maxActions = 0;
        public static bool needsLayout = true;
        public static int[] ParamSelected = new int[1000];

        public static void OnGUI(UnitEntityData ch,
            IEnumerable<SimpleBlueprint> blueprints,
            float indent = 0, float remainingWidth = 0,
            Func<String, String> titleFormater = null,
            NamedTypeFilter typeFilter = null
        ) {
            if (titleFormater == null) titleFormater = (t) => t.orange().bold();
            if (remainingWidth == 0) remainingWidth = UI.ummWidth - indent;
            int index = 0;
            IEnumerable<SimpleBlueprint> simpleBlueprints = blueprints.ToList();
            if (needsLayout) {
                foreach (SimpleBlueprint blueprint in simpleBlueprints) {
                    var actions = blueprint.GetActions();
                    if (actions.Any(a => a.isRepeatable)) hasRepeatableAction = true;
                    int actionCount = actions.Sum(action => action.canPerform(blueprint, ch) ? 1 : 0);
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
            int count = 0;
            foreach (SimpleBlueprint blueprint in simpleBlueprints) {
                int currentCount = count++;
                var description = blueprint.GetDescription();
                float titleWidth = 0;
                var remWidth = remainingWidth - indent;
                using (UI.HorizontalScope()) {
                    UI.Space(indent);
                    var actions = blueprint.GetActions()
                        .Where(action => action.canPerform(blueprint, ch))
                        .ToArray();
                    var titles = actions.Select(a => a.name);
                    var title = blueprint.name;
                    if (titles.Contains("Remove") || titles.Contains("Lock")) {
                        title = title.cyan().bold();
                    }
                    else {
                        title = titleFormater(title);
                    }
                    titleWidth = (remainingWidth / (UI.IsWide ? 3 : 4)) - indent;
                    UI.Label(title, UI.Width(titleWidth));
                    remWidth -= titleWidth;
                    int actionCount = actions != null ? actions.Count() : 0;
                    var lockIndex = titles.IndexOf("Lock");
                    if (blueprint is BlueprintUnlockableFlag flagBP) {
                        // special case this for now
                        if (lockIndex >= 0) {
                            var flags = Game.Instance.Player.UnlockableFlags;
                            var lockAction = actions[lockIndex];
                            UI.ActionButton("<", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) - 1); }, UI.Width(50));
                            UI.Space(25);
                            UI.Label($"{flags.GetFlagValue(flagBP)}".orange().bold(), UI.MinWidth(50));
                            UI.ActionButton(">", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) + 1); }, UI.Width(50));
                            UI.Space(50);
                            UI.ActionButton(lockAction.name, () => { lockAction.action(blueprint, ch, repeatCount); }, UI.Width(120));
                            UI.Space(100);
                        }
                        else {
                            var unlockIndex = titles.IndexOf("Unlock");
                            var unlockAction = actions[unlockIndex];
                            UI.Space(240);
                            UI.ActionButton(unlockAction.name, () => { unlockAction.action(blueprint, ch, repeatCount); }, UI.Width(120));
                            UI.Space(100);
                        }
                    }
                    else {
                        for (int ii = 0; ii < maxActions; ii++) {
                            if (ii < actionCount) {
                                BlueprintAction action = actions[ii];
                                // TODO -don't show increase or decrease actions until we redo actions into a proper value editor that gives us Add/Remove and numeric item with the ability to show values.  For now users can edit ranks in the Facts Editor
                                if (action.name == "<" || action.name == ">") {
                                    UI.Space(174); continue;
                                }
                                var actionName = action.name;
                                float extraSpace = 0;
                                if (action.isRepeatable) {
                                    actionName += (action.isRepeatable ? $" {repeatCount}" : "");
                                    extraSpace = 20 * (float)Math.Ceiling(Math.Log10((double)repeatCount));
                                }
                                UI.ActionButton(actionName, () => { action.action(blueprint, ch, repeatCount, currentCount); }, UI.Width(160 + extraSpace));
                                UI.Space(10);
                                remWidth -= 174.0f + extraSpace;

                            }
                            else {
                                UI.Space(174);
                            }
                        }
                    }
                    UI.Space(10);
                    String typeString = blueprint.GetType().Name;
                    if (typeFilter?.collator != null) {
                        var collatorString = typeFilter.collator(blueprint);
                        if (!typeString.Contains(collatorString))
                            typeString += $" : {collatorString}".yellow();
                    }
                    if (description != null && description.Length > 0) description = $"{description}";
                    else description = "";
                    if (blueprint is BlueprintScriptableObject bpso) {
                        if (settings.showComponents && bpso.ComponentsArray?.Length > 0) {
                            String componentStr = String.Join<object>(" ", bpso.ComponentsArray).color(RGBA.teal);
                            if (description.Length == 0) description = componentStr;
                            else description = componentStr + "\n" + description;
                        }
                        if (settings.showElements && bpso.ElementsArray?.Count > 0) {
                            String elementsStr = String.Join<object>(" ", bpso.ElementsArray).magenta();
                            if (description.Length == 0) description = elementsStr;
                            else description = elementsStr + "\n" + description;
                        }
                    }

                    using (UI.VerticalScope(UI.Width(remWidth))) {
                        if (settings.showAssetIDs) {
                            using (UI.HorizontalScope(UI.Width(remWidth))) {
                                UI.Label(typeString.cyan());
                                GUILayout.TextField(blueprint.AssetGuid.ToString(), UI.ExpandWidth(false));
                            }
                        }
                        else UI.Label(typeString.cyan()); // + $" {remWidth}".bold());

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
                                string[] nameStrings = paramBP.Items.Select(x => x.Name).OrderBy(x => x).ToArray();
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
                UI.Div(indent);
                index++;
            }
        }
    }
}