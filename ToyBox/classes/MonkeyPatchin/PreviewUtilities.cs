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
        private static GUIStyle m_BoldLabel;
        public static GUIStyle BoldLabel =>
            m_BoldLabel ??= new GUIStyle(GUI.skin.label) {
                fontStyle = FontStyle.Bold
            };

        private static GUIStyle m_BoxLabel;
        public static GUIStyle BoxLabel =>
            m_BoxLabel ??= new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.LowerLeft
            };

        private static GUIStyle m_YellowBoxLabel;
        public static GUIStyle YellowBoxLabel =>
            m_YellowBoxLabel ??= new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.LowerLeft,
                normal = new GUIStyleState { textColor = Color.yellow },
                active = new GUIStyleState { textColor = Color.cyan },
                focused = new GUIStyleState { textColor = Color.magenta },
                hover = new GUIStyleState { textColor = Color.green },
            };

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
            var caption = action.GetCaption();
            caption = string.IsNullOrEmpty(caption) ? action.GetType().Name : caption;
            result.Add(caption);
            return result;
        }
        public static string FormatActions(ActionList actions) {
            return FormatActions(actions.Actions);
        }
        public static string FormatActions(GameAction[] actions) {
            return actions
                .SelectMany(FormatActionAsList)
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

            return results.Results.SelectMany(result => result.Actions.Actions).OfType<GameOver>().Any();
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
