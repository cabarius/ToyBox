using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
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
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

using GL = UnityEngine.GUILayout;

namespace ToyBox {
    public class UI {
        /*** ToyBox UI
         * 
         * This is a simple UI framework that simulates the style of SwiftUI.  
         * 
         * Usage - these are intended to be called from any OnGUI render path usedd in your mod
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

        public const string onMark = "<color=green><b>✔</b></color>";
        public const string offMark = "<color=red><b>✖</b></color>";
        public const string disclosureArrowOn = "<color=orange><b>▶</b></color>";
        public const string disclosureArrowOff = "<color=white><b>▲</b></color>";

        // GUILayout wrappers and extensions so other modules can use UI.MethodName()
        public static GUILayoutOption ExpandWidth(bool v) { return GL.ExpandWidth(v); }
        public static GUILayoutOption ExpandHeight(bool v) { return GL.ExpandHeight(v); }
        public static GUILayoutOption AutoWidth() { return GL.ExpandWidth(false); }
        public static GUILayoutOption AutoHeight() { return GL.ExpandHeight(false); }
        public static GUILayoutOption Width(float v) { return GL.Width(v); }
        public static GUILayoutOption[] Width(float min, float max) { 
            return new GUILayoutOption[] { GL.MinWidth(min), GL.MaxWidth(max) };
        }
        public static GUILayoutOption[] Height(float min, float max) {
            return new GUILayoutOption[] { GL.MinHeight(min), GL.MaxHeight(max) };
        }
        public static GUILayoutOption Height(float v) { return GL.Width(v); }
        public static GUILayoutOption MaxWidth(float v) { return GL.MaxWidth(v); }
        public static GUILayoutOption MaxHeight(float v) { return GL.MaxHeight(v); }
        public static GUILayoutOption MinWidth(float v) { return GL.MinWidth(v); }
        public static GUILayoutOption MinHeight(float v) { return GL.MinHeight(v); }

        public static void Space(float size = 150f) { GL.Space(size); }
        public static void BeginHorizontal(params GUILayoutOption[] options) { GL.BeginHorizontal(options); }
        public static void EndHorizontal() { GL.EndHorizontal(); }
        public static void BeginVertical(params GUILayoutOption[] options) { GL.BeginHorizontal(options); }

        public static void EndVertical() { GL.BeginHorizontal(); }

        public static void Label(String title, params GUILayoutOption[] options) {
            // var content = tooltip == null ? new GUIContent(title) : new GUIContent(title, tooltip);
            //  if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(150f) }; }
            GL.Label(title, options);
        }

        public static void TextField(ref String text, String name = null, params GUILayoutOption[] options) {
            if (name != null) { GUI.SetNextControlName(name); }
            text = GL.TextField(text, options);
        }

        public static void IntTextField(ref int value, String name = null, params GUILayoutOption[] options) {
            String searchLimitString = $"{value}";
            UI.TextField(ref searchLimitString, name, options);
            Int32.TryParse(searchLimitString, out value);
        }

        public static void SelectionGrid(ref int value, String[] texts, int xCols, params GUILayoutOption[] options) {
            value = GL.SelectionGrid(value, texts, xCols, options);
        }

        // UI Elements

        public static void ActionButton(String title, Action action, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300f) }; }
            if (GL.Button(title, options)) { action(); }
        }

        public static void ActionTextField(ref string text,
            Action<String> action,
            String name,
            Action enterAction,
            params GUILayoutOption[] options
            ) {
            GUI.SetNextControlName(name);
            String newText = GL.TextField(text, options);
            if (newText != text) {
                text = newText;
                action(text);
            }
            if (Main.userHasHitReturn && Main.focusedControlName == name) {
                enterAction();
            }
        }

        public static void ActionIntTextField(ref int value,
            Action<int> action,
            String name,
            Action enterAction,
            params GUILayoutOption[] options
            ) {
            bool changed = false;
            bool hitEnter = false;
            String searchLimitString = $"{value}";
            UI.ActionTextField(ref searchLimitString, 
                (text) => { changed = true;  }, 
                name, 
                () => { hitEnter = true; }, 
                options);
            Int32.TryParse(searchLimitString, out value);
            if (changed) { action(value); }
            if (hitEnter) { enterAction(); }
        }

        public static void ActionSelectionGrid(ref int value, String[] texts, int xCols, Action<int> action, params GUILayoutOption[] options) {
            int newValue = GL.SelectionGrid(value, texts, xCols, options);
            if (newValue != value) {
                value = newValue;
                action(value);
            }
        }

        static void TogglePrivate(
            String title,
            ref bool value,
            bool disclosureStyle = true,
            params GUILayoutOption[] options) {
            if (!disclosureStyle) {
                if (GL.Button(title + " " + (value ? onMark : offMark), AutoWidth())) { value = !value; }
            }
            else {
                UI.Label(title, AutoWidth());
                GL.Space(10);
                if (GL.Button(value ? disclosureArrowOn : disclosureArrowOff, AutoWidth())) { value = !value; }
            }
        }

        public static void Toggle(
            String title,
            ref bool value,
            params GUILayoutOption[] options) {
            TogglePrivate(title, ref value, false, options);
        }

        public static void BitFieldToggle(
            String title,
            ref int bitfield,
            int offset,
            params GUILayoutOption[] options) {
            bool bit = ((1 << offset) & bitfield) != 0;
            bool newBit = bit;
            TogglePrivate(title, ref newBit, false, options);
            if (bit != newBit) { bitfield ^= 1 << offset; }
        }

        public static void DisclosureToggle(String title, ref bool value, params Action[] actions) {
            UI.TogglePrivate(title, ref value, true, AutoWidth());
            UI.If(value, actions);
        }

        public static void DisclosureBitFieldToggle(String title, ref int bitfield, int offset, params Action[] actions) {

            bool bit = ((1 << offset) & bitfield) != 0;
            bool newBit = bit;
            TogglePrivate(title, ref newBit, true, AutoWidth());
            if (bit != newBit) { bitfield ^= (1 << offset); }
            UI.If(newBit, actions);
        }

        public static T TypePicker<T>(String title, ref int selectedIndex, List<NamedFunc<T>> items) where T : class {
            var titles = items.Select((item) => item.name).ToArray();
            if (title?.Length > 0) { Label(title); }
            selectedIndex = GL.SelectionGrid(selectedIndex, titles, 6);
            return items[selectedIndex].func();
        }

        // UI Builders

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
            for (int ii = 0; ii < actions.Length; ii += stride) {
                bool hasTitle = title != null;
                UI.BeginHorizontal();
                if (hasTitle) {
                    if (ii == 0) { UI.Label(title, UI.Width(150f)); }
                    else { UI.Space(153); }
                }
                UI.Group(actions.Skip(ii).Take(stride).ToArray());
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
    }
}
