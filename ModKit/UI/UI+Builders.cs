// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {
        static public bool userHasHitReturn = false;
        static public String focusedControlName = null;

        public static Rect ummRect = new Rect();
        public static float ummWidth = 960f;
        public static int ummTabID = 0;
        public static bool IsNarrow { get { return ummWidth < 1200; } }
        public static bool IsWide { get { return ummWidth >= 1920; } }

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
        public static void HStack(String title = null, int stride = 0, params Action[] actions) {
            var length = actions.Length;
            if (stride < 1) { stride = length; }
            if (UI.IsNarrow) stride = Math.Min(3, stride);
            for (int ii = 0; ii < actions.Length; ii += stride) {
                bool hasTitle = title != null;
                UI.BeginHorizontal();
                if (hasTitle) {
                    if (ii == 0) { UI.Label(title.bold(), UI.Width(150f)); }
                    else { UI.Space(153); }
                }
                var filteredActions = actions.Skip(ii).Take(stride);
                foreach (var action in filteredActions) {
                    action();
                }
                UI.EndHorizontal();
            }
        }
        public static void VStack(String title = null, params Action[] actions) {
            UI.BeginVertical();
            if (title != null) { UI.Label(title); }
            UI.Group(actions);
            UI.EndVertical();
        }
        public static void Section(String title, params Action[] actions) {
            UI.Space(25);
            UI.Label($"====== {title} ======".bold(), GL.ExpandWidth(true));
            UI.Space(25);
            foreach (Action action in actions) { action(); }
            UI.Space(10);
        }

        public static void TabBar(ref int selected, Action header = null, params NamedAction[] actions) {
            if (selected >= actions.Count()) selected = 0;
            int sel = selected;
            var titles = actions.Select((a, i) => i == sel ? a.name.orange().bold() : a.name);
            selected = GL.Toolbar(selected, titles.ToArray());
            GL.BeginVertical("box");
            if (header != null) header();
            actions[selected].action();
            GL.EndVertical();
        }
    }
}
