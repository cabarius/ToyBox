// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox
{
    public class BlueprintListUI
    {
        public static Settings settings => Main.settings;

        public static int repeatCount = 1;

        public static bool hasRepeatableAction;

        public static int maxActions;

        public static bool needsLayout = true;

        public static void OnGUI(UnitEntityData ch,
                                 IEnumerable<SimpleBlueprint> blueprints,
                                 float indent = 0,
                                 float remainingWidth = 0,
                                 Func<string, string> titleFormater = null,
                                 NamedTypeFilter typeFilter = null)
        {
            if (titleFormater == null)
            {
                titleFormater = t => t.orange().bold();
            }

            if (remainingWidth == 0)
            {
                remainingWidth = UI.ummWidth - indent;
            }

            IEnumerable<SimpleBlueprint> simpleBlueprints = blueprints.ToList();

            if (needsLayout)
            {
                foreach (SimpleBlueprint blueprint in simpleBlueprints)
                {
                    var actions = blueprint.GetActions();

                    IEnumerable<BlueprintAction> blueprintActions = actions.ToList();

                    if (blueprintActions.Any(a => a.isRepeatable))
                    {
                        hasRepeatableAction = true;
                    }

                    int actionCount = blueprintActions.Sum(action => action.canPerform(blueprint, ch) ? 1 : 0);
                    maxActions = Math.Max(actionCount, maxActions);
                }

                needsLayout = false;
            }

            if (hasRepeatableAction)
            {
                UI.BeginHorizontal();
                UI.Label("", UI.MinWidth(350 - indent), UI.MaxWidth(600));

                UI.ActionIntTextField(
                    ref repeatCount,
                    "repeatCount",
                    limit => { },
                    () => { },
                    UI.Width(160));

                UI.Space(40);
                UI.Label("Parameter".cyan() + ": " + $"{repeatCount}".orange(), UI.ExpandWidth(false));
                repeatCount = Math.Max(1, repeatCount);
                repeatCount = Math.Min(100, repeatCount);
                UI.EndHorizontal();
            }

            UI.Div(indent);

            foreach (SimpleBlueprint blueprint in simpleBlueprints)
            {
                string description = blueprint.GetDescription();

                using (UI.HorizontalScope())
                {
                    float remWidth = remainingWidth - indent;
                    UI.Space(indent);
                    var actions = blueprint.GetActions().Where(action => action.canPerform(blueprint, ch)).ToArray();
                    var titles = actions.Select(a => a.name);
                    var title = blueprint.name;

                    if (titles.Contains("Remove"))
                    {
                        title = title.cyan().bold();
                    }
                    else
                    {
                        title = titleFormater(title);
                    }

                    float titleWidth = remainingWidth / (UI.IsWide ? 3 : 4) - indent;
                    UI.Label(title, UI.Width(titleWidth));
                    remWidth -= titleWidth;
                    int actionCount = actions?.Count() ?? 0;

                    for (int ii = 0; ii < maxActions; ii++)
                    {
                        if (ii < actionCount)
                        {
                            BlueprintAction action = actions[ii];

                            // TODO -don't show increase or decrease actions until we redo actions into a proper value editor that gives us Add/Remove and numeric item with the ability to show values.  For now users can edit ranks in the Facts Editor
                            if (action.name == "<" || action.name == ">")
                            {
                                UI.Space(174);

                                continue;
                            }

                            string actionName = action.name;
                            float extraSpace = 0;

                            if (action.isRepeatable)
                            {
                                actionName += action.isRepeatable ? $" {repeatCount}" : string.Empty;
                                extraSpace = 20 * (float)Math.Ceiling(Math.Log10(repeatCount));
                            }

                            UI.ActionButton(actionName, () => action.action(blueprint, ch, repeatCount), UI.Width(160 + extraSpace));
                            UI.Space(10);
                            remWidth -= 174.0f + extraSpace;
                        }
                        else
                        {
                            UI.Space(174);
                        }
                    }

                    UI.Space(10);
                    string typeString = blueprint.GetType().Name;

                    if (typeFilter?.collator != null)
                    {
                        string collatorString = typeFilter.collator(blueprint);

                        if (!typeString.Contains(collatorString))
                        {
                            typeString += $" : {collatorString}".yellow();
                        }
                    }

                    description = string.IsNullOrEmpty(description) ? string.Empty : $"{description}";

                    if (blueprint is BlueprintScriptableObject bpso)
                    {
                        if (settings.showComponents && bpso.ComponentsArray?.Length > 0)
                        {
                            string componentStr = string.Join<object>(" ", bpso.ComponentsArray).color(RGBA.teal);

                            if (description.Length == 0)
                            {
                                description = componentStr;
                            }
                            else
                            {
                                description = componentStr + "\n" + description;
                            }
                        }

                        if (settings.showElements && bpso.ElementsArray?.Count > 0)
                        {
                            string elementsStr = string.Join<object>(" ", bpso.ElementsArray).magenta();

                            if (description.Length == 0)
                            {
                                description = elementsStr;
                            }
                            else
                            {
                                description = elementsStr + "\n" + description;
                            }
                        }
                    }

                    using (UI.VerticalScope(UI.Width(remWidth)))
                    {
                        if (settings.showAssetIDs)
                        {
                            using (UI.HorizontalScope(UI.Width(remWidth)))
                            {
                                UI.Label(typeString.cyan());
                                GUILayout.TextField(blueprint.AssetGuid.ToString(), UI.ExpandWidth(false));
                            }
                        }
                        else
                        {
                            UI.Label(typeString.cyan());
                        }

                        if (description.Length > 0)
                        {
                            UI.Label(description.green(), UI.Width(remWidth));
                        }
                    }
                }

                UI.Div(indent);
            }
        }
    }
}