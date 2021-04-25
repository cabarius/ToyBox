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


        public static void Label(String title, params GUILayoutOption[] options) {
            // var content = tooltip == null ? new GUIContent(title) : new GUIContent(title, tooltip);
            //  if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(150f) }; }
            GL.Label(title, options);
        }

        // Controls

        public static String TextField(ref String text, String name = null, params GUILayoutOption[] options) {
            if (name != null) { GUI.SetNextControlName(name); }
            text = GL.TextField(text, options);
            return text;
        }
        public static int IntTextField(ref int value, String name = null, params GUILayoutOption[] options) {
            String text = $"{value}";
            UI.TextField(ref text, name, options);
            Int32.TryParse(text, out value);
            return value;
        }
        public static float FloatTextField(ref float value, String name = null, params GUILayoutOption[] options) {
            String text = $"{value}";
            UI.TextField(ref text, name, options);
            var val = value;
            if (float.TryParse(text, out val)) {
                value = val;
            }
            return value;
        }

        public static void ActionButton(String title, Action action, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300f) }; }
            if (GL.Button(title, options)) { action(); }
        }
        public static void ActionTextField(ref string text,
                String name,
                Action<String> action,
                Action enterAction,
                params GUILayoutOption[] options
            ) {
            GUI.SetNextControlName(name);
            String newText = GL.TextField(text, options);
            if (newText != text) {
                text = newText;
                if (action != null) action(text);
            }
            if (enterAction != null && UI.userHasHitReturn && UI.focusedControlName == name) {
                enterAction();
            }
        }
        public static void ActionIntTextField(ref int value,
                String name,
                Action<int> action,
                Action enterAction,
                params GUILayoutOption[] options
            ) {
            bool changed = false;
            bool hitEnter = false;
            String str = $"{value}";
            UI.ActionTextField(ref str,
                name,
                (text) => { changed = true; },
                () => { hitEnter = true; },
                options);
            Int32.TryParse(str, out value);
            if (changed) { action(value); }
            if (hitEnter) { enterAction(); }
        }
        public static void ValueEditor(String title, ref int increment, Func<int> get, Action<long> set, int min = 0, int max = int.MaxValue, float titleWidth = 500) {
            var value = get();
            var inc = increment;
            UI.Label(title.cyan(), UI.Width(titleWidth));
            UI.Space(25);
            float fieldWidth = GUI.skin.textField.CalcSize(new GUIContent(max.ToString())).x;
            UI.ActionButton(" < ", () => { set(Math.Max(value - inc, min)); }, UI.AutoWidth());
            UI.Space(20);
            UI.Label($"{value}".orange().bold(), UI.AutoWidth()); ;
            UI.Space(20);
            UI.ActionButton(" > ", () => { set(Math.Min(value + inc, max)); }, UI.AutoWidth());
            UI.Space(50);
            UI.ActionIntTextField(ref inc, title, (v) => { }, null, UI.Width(fieldWidth + 25));
            increment = inc;
        }
        public static bool Slider(String title, ref float value, float min, float max, float defaultValue = 1.0f, int decimals = 0, String units = "", params GUILayoutOption[] options) {
            value = Math.Max(min, Math.Min(max, value));    // clamp it
            UI.BeginHorizontal(options);
            UI.Label(title.cyan(), UI.Width(300));
            UI.Space(25);
            float newValue = (float)Math.Round(GL.HorizontalSlider(value, min, max, UI.Width(200)), decimals);
            UI.Space(25);
            UI.FloatTextField(ref newValue, null, UI.Width(75));
            if (units.Length > 0)
                UI.Label($"{units}".orange().bold(), UI.Width(25 + GUI.skin.label.CalcSize(new GUIContent(units)).x));
            UI.Space(25);
            UI.ActionButton("Reset", () => { newValue = defaultValue; }, UI.AutoWidth());
            UI.EndHorizontal();
            bool changed = value != newValue;
            value = newValue;
            return changed;
        }
        public static bool Slider(String title, ref int value, int min, int max, int defaultValue = 1, String units = "", params GUILayoutOption[] options) {
            float fvalue = value;
            bool changed = UI.Slider(title, ref fvalue, min, max, (float)defaultValue, 0, "", options);
            value = (int)fvalue;
            return changed;
        }
        public static bool LogSlider(String title, ref float value, float min, float max, float defaultValue = 1.0f, int decimals = 0, String units = "", params GUILayoutOption[] options) {
            if (min < 0) throw new Exception("LogSlider - min value: {min} must be >= 0");
            UI.BeginHorizontal(options);
            UI.Label(title.cyan(), UI.Width(300));
            UI.Space(25);
            value = Math.Max(min, Math.Min(max, value));    // clamp it
            var offset = 1;
            var places = (int)Math.Max(0, Math.Min(15, 2.01 - Math.Log10(value + offset)));
            var logMin = 100f * (float)Math.Log10(min + offset);
            var logMax = 100f * (float)Math.Log10(max + offset);
            var logValue = 100f * (float)Math.Log10(value + offset);
            var logNewValue = (float)(GL.HorizontalSlider(logValue, logMin, logMax, UI.Width(200)));
            var newValue = (float)Math.Round(Math.Pow(10, logNewValue / 100f) - offset, places);
            UI.Space(25);
            UI.FloatTextField(ref newValue, null, UI.Width(75));
            if (units.Length > 0)
                UI.Label($"{units}".orange().bold(), UI.Width(25 + GUI.skin.label.CalcSize(new GUIContent(units)).x));
            UI.Space(25);
            UI.ActionButton("Reset", () => { newValue = defaultValue; }, UI.AutoWidth());
            UI.EndHorizontal();
            bool changed = value != newValue;
            value = newValue;
            return changed;
        }
    }
}
