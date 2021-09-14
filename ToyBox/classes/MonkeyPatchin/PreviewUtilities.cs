using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Kingdom.Blueprints;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace ToyBox {
    class PreviewUtilities {
        static private GUIStyle m_BoldLabel;
        static public GUIStyle BoldLabel {
            get {
                if (m_BoldLabel == null) {
                    m_BoldLabel = new GUIStyle(GUI.skin.label) {
                        fontStyle = FontStyle.Bold
                    };
                }
                return m_BoldLabel;
            }
        }
        static private GUIStyle m_BoxLabel;
        static public GUIStyle BoxLabel {
            get {
                if (m_BoxLabel == null) {
                    m_BoxLabel = new GUIStyle(GUI.skin.box) {
                        alignment = TextAnchor.LowerLeft
                    };
                }
                return m_BoxLabel;
            }
        }
        static private GUIStyle m_YellowBoxLabel;
        static public GUIStyle YellowBoxLabel {
            get {
                if (m_YellowBoxLabel == null) {
                    m_YellowBoxLabel = new GUIStyle(GUI.skin.box) {
                        alignment = TextAnchor.LowerLeft,
                        normal = new GUIStyleState { textColor = Color.yellow },
                        active = new GUIStyleState { textColor = Color.cyan },
                        focused = new GUIStyleState { textColor = Color.magenta },
                        hover = new GUIStyleState { textColor = Color.green },
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
            if (action is Conditional) {
                return ResolveConditional(action as Conditional);
            }
            var result = new List<string>();
            var caption = action.GetCaption();
            caption = caption == "" || caption == null ? action.GetType().Name : caption;
            result.Add(caption);
            return result;
        }
        public static string FormatActions(ActionList actions) {
            return FormatActions(actions.Actions);
        }
        public static string FormatActions(GameAction[] actions) {
            return actions
                .SelectMany(action => FormatActionAsList(action))
                .Select(actionText => actionText == "" ? "EmptyAction" : actionText)
                .Join();
        }

        public static string FormatConditions(Condition[] conditions) {
            return conditions.Join(c => c.GetCaption());
        }
        public static string FormatConditions(ConditionsChecker conditions) {
            return FormatConditions(conditions.Conditions);
        }
        public static bool CausesGameOver(BlueprintKingdomEventBase blueprint) {
            var results = blueprint.GetComponent<EventFinalResults>();
            if (results == null) return false;
            foreach (var result in results.Results) {
                foreach (var action in result.Actions.Actions) {
                    if (action is GameOver) return true;
                }
            }
            return false;
        }
        public class CodeTimer : IDisposable {
            private readonly Stopwatch m_Stopwatch;
            private readonly string m_Text;
            public CodeTimer(string text) {
                this.m_Text = text;
                this.m_Stopwatch = Stopwatch.StartNew();
            }
            public void Dispose() {
                this.m_Stopwatch.Stop();
                string message = string.Format("Profiled {0}: {1:0.00}ms", this.m_Text, this.m_Stopwatch.ElapsedMilliseconds);
                Main.Debug(message);
            }
        }
    }
}
