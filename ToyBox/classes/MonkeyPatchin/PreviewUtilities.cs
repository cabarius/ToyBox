using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Blueprints;
using System.Diagnostics;
using System;

namespace ToyBox {
    class PreviewUtilities {
        private static GUIStyle m_BoldLabel;

        public static GUIStyle BoldLabel =>
            m_BoldLabel ?? (m_BoldLabel = new GUIStyle(GUI.skin.label) {
                fontStyle = FontStyle.Bold
            });

        private static GUIStyle m_BoxLabel;

        public static GUIStyle BoxLabel =>
            m_BoxLabel ?? (m_BoxLabel = new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.LowerLeft
            });

        private static GUIStyle m_YellowBoxLabel;

        public static GUIStyle YellowBoxLabel =>
            m_YellowBoxLabel ?? (m_YellowBoxLabel = new GUIStyle(GUI.skin.box) {
                alignment = TextAnchor.LowerLeft,
                normal = new GUIStyleState() { textColor = Color.yellow },
                active = new GUIStyleState() { textColor = Color.cyan },
                focused = new GUIStyleState() { textColor = Color.magenta },
                hover = new GUIStyleState() { textColor = Color.green },
            });

        public static List<string> ResolveConditional(Conditional conditional) {
            var actionList = conditional.ConditionsChecker.Check(null) ? conditional.IfTrue : conditional.IfFalse;
            var result = new List<string>();

            foreach (var action in actionList.Actions) {
                result.AddRange(FormatActionAsList(action));
            }

            return result;
        }

        public static IEnumerable<string> FormatActionAsList(GameAction action) {
            if (action is Conditional conditional) {
                return ResolveConditional(conditional);
            }

            var result = new List<string>();
            string caption = action.GetCaption();
            caption = string.IsNullOrEmpty(caption) ? action.GetType().Name : caption;
            result.Add(caption);

            return result;
        }

        public static string FormatActions(ActionList actions) {
            return FormatActions(actions.Actions);
        }

        public static string FormatActions(IEnumerable<GameAction> actions) {
            return actions
                   .SelectMany(FormatActionAsList)
                   .Select(actionText => actionText == string.Empty ? "EmptyAction" : actionText)
                   .Join();
        }

        public static string FormatConditions(IEnumerable<Condition> conditions) {
            return conditions.Join(c => c.GetCaption());
        }

        public static string FormatConditions(ConditionsChecker conditions) {
            return FormatConditions(conditions.Conditions);
        }

        public static bool CausesGameOver(BlueprintKingdomEventBase blueprint) {
            var results = blueprint.GetComponent<EventFinalResults>();

            return results != null && results.Results.SelectMany(result => result.Actions.Actions).OfType<GameOver>().Any();
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