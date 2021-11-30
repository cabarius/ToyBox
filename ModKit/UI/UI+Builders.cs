// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Linq;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {
        public static bool userHasHitReturn = false;
        public static string focusedControlName = null;

        public static Rect ummRect = new();
        public static float ummWidth = 960f;
        public static int ummTabID = 0;
        public static bool IsNarrow => ummWidth < 1200;
        public static bool IsWide => ummWidth >= 1920;

        public static Vector2[] ummScrollPosition;
        /*** UI Builders
         * 
         * This is a simple UI framework that simulates the style of SwiftUI.  
         * 
         * Usage - these are intended to be called from any OnGUI render path used in your mod
         * 
         * Elements will be defined like this
                UI.Section("Cheap Tricks", () =>
                {
                    UI.HStack("Combat", 4,
                        () => { UI.ActionButton("Rest All", () => { CheatsCombat.RestAll(); }); },
                        () => { UI.ActionButton("Empowered", () => { CheatsCombat.Empowered(""); }); },
                        () => { UI.ActionButton("Full Buff Please", () => { CheatsCombat.RestAll(); }); },
                        () => { UI.ActionButton("Remove Death's Door", () => { CheatsCombat.Empowered(""); }); },
                        () => { UI.ActionButton("Kill All Enemies", () => { CheatsCombat.KillAll(); }); },
                        () => { UI.ActionButton("Summon Zoo", () => { CheatsCombat.SpawnInspectedEnemiesUnderCursor(""); }); }
                     );
                    UI.Space(10);
                    UI.HStack("Common", 4,
                        () => { UI.ActionButton("Change Weather", () => { CheatsCommon.ChangeWeather(""); }); },
                        () => { UI.ActionButton("Set Perception to 40", () => { CheatsCommon.StatPerception(); }); }
                     );
                    UI.Space(10);
                    UI.HStack("Unlocks", 4,
                        () => { UI.ActionButton("Give All Items", () => { CheatsUnlock.CreateAllItems(""); }); }
                     );
                });
        */

        public static void If(bool value, params Action[] actions) {
            if (value) {
                foreach (var action in actions) {
                    action();
                }
            }
        }
        public static void Group(params Action[] actions) {
            foreach (var action in actions) {
                action();
            }
        }
        public static void HStack(string title = null, int stride = 0, params Action[] actions) {
            var length = actions.Length;
            if (stride < 1) { stride = length; }
            if (IsNarrow)
                stride = Math.Min(3, stride);
            for (var ii = 0; ii < actions.Length; ii += stride) {
                var hasTitle = title != null;
                BeginHorizontal();
                if (hasTitle) {
                    if (ii == 0) { Label(title.bold(), Width(150f)); }
                    else { Space(153); }
                }
                var filteredActions = actions.Skip(ii).Take(stride);
                foreach (var action in filteredActions) {
                    action();
                }
                EndHorizontal();
            }
        }
        public static void VStack(string title = null, params Action[] actions) {
            BeginVertical();
            if (title != null) { Label(title); }
            Group(actions);
            EndVertical();
        }
        public static void Section(string title, params Action[] actions) {
            Space(25);
            Label($"====== {title} ======".bold(), GL.ExpandWidth(true));
            Space(25);
            foreach (var action in actions) { action(); }
            Space(10);
        }

        public static void TabBar(ref int selected, Action header = null, params NamedAction[] actions) {
            if (selected >= actions.Count())
                selected = 0;
            var sel = selected;
            var titles = actions.Select((a, i) => i == sel ? a.name.orange().bold() : a.name);
            SelectionGrid(ref selected, titles.ToArray(), 8, Width(ummWidth - 60));
            GL.BeginVertical("box");
            header?.Invoke();
            actions[selected].action();
            GL.EndVertical();
        }
    }
}
