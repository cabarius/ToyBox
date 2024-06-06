using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Blueprints;
using System.Diagnostics;
using System;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem.Blueprints;
using ModKit;

namespace ToyBox {
    internal class PreviewUtilities {
        private static GUIStyle m_BoldLabel;
        public static GUIStyle BoldLabel {
            get {
                if (m_BoldLabel == null) {
                    m_BoldLabel = new GUIStyle(GUI.skin.label) {
                        fontStyle = FontStyle.Bold
                    };
                }
                return m_BoldLabel;
            }
        }
        private static GUIStyle m_BoxLabel;
        public static GUIStyle BoxLabel {
            get {
                if (m_BoxLabel == null) {
                    m_BoxLabel = new GUIStyle(GUI.skin.box) {
                        alignment = TextAnchor.LowerLeft
                    };
                }
                return m_BoxLabel;
            }
        }
        private static GUIStyle m_YellowBoxLabel;
        public static GUIStyle YellowBoxLabel {
            get {
                if (m_YellowBoxLabel == null) {
                    m_YellowBoxLabel = new GUIStyle(GUI.skin.box) {
                        alignment = TextAnchor.LowerLeft,
                        normal = new GUIStyleState() { textColor = Color.yellow },
                        active = new GUIStyleState() { textColor = Color.cyan },
                        focused = new GUIStyleState() { textColor = Color.magenta },
                        hover = new GUIStyleState() { textColor = Color.green },
                    };
                }
                return m_YellowBoxLabel;
            }
        }
        public static List<string> ResolveConditional(Conditional conditional) {
            var actionList = conditional.ConditionsChecker.Check(null) ? conditional.IfTrue : conditional.IfFalse;
            var result = new List<string>();
            foreach (var action in actionList.Actions) {
                result.AddRange(FormatActionAsList(action));
            }
            return result;
        }
        public static List<string> FormatActionAsList(GameAction action) {
            if (action is Conditional conditional) {
                return ResolveConditional(conditional);
            }
            var result = new List<string>();
            var caption = "";
            if (action is RunActionHolder actionHolder) {
                if (actionHolder.Holder.Get()?.Actions is { } subActions) {
                    var subActionList = FormatActions(subActions);
                    caption = $"Run Action Holder({string.Join(", ", subActionList)})";
                }
            } else {
                caption = action?.GetCaption();
            }
            caption = caption == "" || caption == null ? action?.GetType().Name ?? "" : caption;
            result.Add(caption);
            return result;
        }
        public static string FormatActions(ActionList actions) => FormatActions(actions.Actions);
        public static string FormatActions(GameAction[] actions) => actions
                .SelectMany(action => FormatActionAsList(action))
                .Select(actionText => actionText == "" ? "EmptyAction" : actionText)
                .Join();

        public static string FormatConditions(Condition[] conditions) => conditions.Join(c => {
            if (c is CheckConditionsHolder holder) {
                return "Conditions Holder".localize() + $"({FormatConditions(holder.ConditionsHolder.Get().Conditions)})";
            } else
                return c.GetCaption();
        });
        public static string FormatConditions(ConditionsChecker conditions) => FormatConditions(conditions.Conditions);
        public static List<string> FormatConditionsAsList(BlueprintAnswer answer) {
            var list = new List<String>();
            if (answer.HasShowCheck)
                list.Add("Show Check".localize() + $"({answer.ShowCheck.Type} " + "DC".localize() + $": {answer.ShowCheck.DC})");
            if (answer.ShowConditions.Conditions.Length > 0)
                list.Add("Show Conditions".localize() + $"({FormatConditions(answer.ShowConditions)}");
            if (answer.SelectConditions is ConditionsChecker selectChecker && selectChecker.Conditions.Count() > 0)
                list.Add("Select Conditions".localize() + $"({PreviewUtilities.FormatConditions(selectChecker)})"); ;
            return list;
        }
        public class CodeTimer : IDisposable {
            private readonly Stopwatch m_Stopwatch;
            private readonly string m_Text;
            public CodeTimer(string text) {
                m_Text = text;
                m_Stopwatch = Stopwatch.StartNew();
            }
            public void Dispose() {
                m_Stopwatch.Stop();
                var message = string.Format("Profiled {0}: {1:0.00}ms", m_Text, m_Stopwatch.ElapsedMilliseconds);
                Mod.Trace(message);
            }
        }
    }
}
