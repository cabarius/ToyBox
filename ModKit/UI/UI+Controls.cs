// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using GL = UnityEngine.GUILayout;
using UnityModManagerNet;

namespace ModKit {
    public static partial class UI {
        public static void Label(string title, params GUILayoutOption[] options) =>
            // var content = tooltip == null ? new GUIContent(title) : new GUIContent(title, tooltip);
            //  if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(150f) }; }
            GL.Label(title, options);
        public static void Label(string title, GUIStyle style, params GUILayoutOption[] options) =>
            // var content = tooltip == null ? new GUIContent(title) : new GUIContent(title, tooltip);
            //  if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(150f) }; }
            GL.Label(title, style, options);
        public static void Label(GUIContent content, params GUILayoutOption[] options) =>
            // var content = tooltip == null ? new GUIContent(title) : new GUIContent(title, tooltip);
            //  if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(150f) }; }
            GL.Label(content, options);
        public static bool EditableLabel(ref string label, ref (string, string) editState, float minWidth, GUIStyle style, Func<string, string> formatter = null, params GUILayoutOption[] options) {
            var changed = false;
            if (editState.Item1 != label) {
                using (HorizontalScope(options)) {
                    Label(formatter(label), style, AutoWidth());
                    Space(5);
                    if (GL.Button("✎", GUI.skin.box, AutoWidth())) {
                        editState = (label, label);
                    }
                }
            }
            else {
                GUI.SetNextControlName(label);
                using (HorizontalScope(options)) {
                    TextField(ref editState.Item2, null, MinWidth(minWidth), AutoWidth());
                    Space(15);
                    if (GL.Button("✖".red(), GUI.skin.box, AutoWidth())) {
                        editState = (null, null);
                    }
                    if (GL.Button("✔".green(), GUI.skin.box, AutoWidth())
                        || userHasHitReturn && focusedControlName == label) {
                        label = editState.Item2;
                        changed = true;
                        editState = (null, null);
                    }
                }
            }
            return changed;
        }
        public static bool EditableLabel(ref string label, ref (string, string) editState, float minWidth, Func<string, string> formatter = null, params GUILayoutOption[] options) => EditableLabel(ref label, ref editState, minWidth, GUI.skin.label, formatter, options);

        // Controls
        public static string TextField(ref string text, string name = null, params GUILayoutOption[] options) {
            if (name != null) { GUI.SetNextControlName(name); }
            text = GL.TextField(text, options);
            return text;
        }
        public static int IntTextField(ref int value, string name = null, params GUILayoutOption[] options) {
            var text = $"{value}";
            TextField(ref text, name, options);
            int.TryParse(text, out value);
            return value;
        }
        public static float FloatTextField(ref float value, string name = null, params GUILayoutOption[] options) {
            var text = $"{value}";
            TextField(ref text, name, options);
            if (float.TryParse(text, out var val)) {
                value = val;
            }
            return value;
        }
        public static bool Button(string title, ref bool pressed, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300f) }; }
            if (GL.Button(title, options)) { pressed = true; }
            return pressed;
        }
        public static void ActionButton(string title, Action action, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300f) }; }
            if (GL.Button(title, options)) { action(); }
        }
        public static void ActionButton(string title, Action action, GUIStyle style, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300f) }; }
            if (GL.Button(title, style, options)) { action(); }
        }
        public static void ActionTextField(ref string text,
                string name,
                Action<string> action,
                Action enterAction,
                params GUILayoutOption[] options
            ) {
            GUI.SetNextControlName(name);
            var newText = GL.TextField(text, options);
            if (newText != text) {
                text = newText;
                action?.Invoke(text);
            }
            if (enterAction != null && userHasHitReturn && focusedControlName == name) {
                enterAction();
            }
        }
        public static void ActionIntTextField(
                ref int value,
                string name,
                Action<int> action,
                Action enterAction,
                int min = 0,
                int max = int.MaxValue,
                params GUILayoutOption[] options
            ) {
            var changed = false;
            var hitEnter = false;
            var str = $"{value}";
            ActionTextField(ref str,
                name,
                (text) => { changed = true; },
                () => { hitEnter = true; },
                options);
            int.TryParse(str, out value);
            value = Math.Min(max, Math.Max(value, min));
            if (changed) { action(value); }
            if (hitEnter && enterAction != null) { enterAction(); }
        }
        public static void ActionIntTextField(
                ref int value,
                string name,
                Action<int> action,
                Action enterAction,
                params GUILayoutOption[] options) => ActionIntTextField(ref value, name, action, enterAction, int.MinValue, int.MaxValue, options);
        public static void ValueEditor(string title, ref int increment, Func<int> get, Action<long> set, int min = 0, int max = int.MaxValue, float titleWidth = 500) {
            var value = get();
            var inc = increment;
            Label(title.cyan(), Width(titleWidth));
            Space(25);
            var fieldWidth = GUI.skin.textField.CalcSize(new GUIContent(max.ToString())).x;
            ActionButton(" < ", () => { set(Math.Max(value - inc, min)); }, AutoWidth());
            Space(20);
            Label($"{value}".orange().bold(), AutoWidth());
            ;
            Space(20);
            ActionButton(" > ", () => { set(Math.Min(value + inc, max)); }, AutoWidth());
            Space(50);
            ActionIntTextField(ref inc, title, (v) => { }, null, Width(fieldWidth + 25));
            increment = inc;
        }
        public static bool Slider(ref float value, float min, float max, float defaultValue = 1.0f, int decimals = 0, string units = "", params GUILayoutOption[] options) {
            value = Math.Max(min, Math.Min(max, value));    // clamp it
            var newValue = (float)Math.Round(GL.HorizontalSlider(value, min, max, Width(200)), decimals);
            using (HorizontalScope(options)) {
                Space(25);
                FloatTextField(ref newValue, null, Width(75));
                if (units.Length > 0)
                    Label($"{units}".orange().bold(), Width(25 + GUI.skin.label.CalcSize(new GUIContent(units)).x));
                Space(25);
                ActionButton("Reset", () => { newValue = defaultValue; }, AutoWidth());
            }
            var changed = value != newValue;
            value = newValue;
            return changed;
        }

        private const int sliderTop = 3;
        private const int sliderBottom = -7;
        public static bool Slider(string title, ref float value, float min, float max, float defaultValue = 1.0f, int decimals = 0, string units = "", params GUILayoutOption[] options) {
            value = Math.Max(min, Math.Min(max, value));    // clamp it
            var newValue = value;
            using (HorizontalScope(options)) {
                using (VerticalScope(Width(300))) {
                    Space(UnityModManager.UI.Scale(sliderTop - 1));
                    Label(title.cyan(), Width(300));
                    Space(UnityModManager.UI.Scale(sliderBottom));
                }
                Space(25);
                using (VerticalScope(Width(200))) {
                    Space(UnityModManager.UI.Scale(sliderTop + 4));
                    newValue = (float)Math.Round(GL.HorizontalSlider(value, min, max, Width(200)), decimals);
                    Space(UnityModManager.UI.Scale(sliderBottom));
                }
                Space(25);
                using (VerticalScope(Width(75))) {
                    Space(UnityModManager.UI.Scale(sliderTop + 2));
                    FloatTextField(ref newValue, null, Width(75));
                    Space(UnityModManager.UI.Scale(sliderBottom));
                }
                if (units.Length > 0)
                    Label($"{units}".orange().bold(), Width(25 + GUI.skin.label.CalcSize(new GUIContent(units)).x));
                Space(25);
                using (VerticalScope(AutoWidth())) {
                    Space(UnityModManager.UI.Scale(sliderTop - 0));
                    ActionButton("Reset", () => { newValue = defaultValue; }, AutoWidth());
                    Space(UnityModManager.UI.Scale(sliderBottom));
                }
            }
            var changed = value != newValue;
            value = newValue;
            return changed;
        }
        public static bool Slider(string title, ref int value, int min, int max, int defaultValue = 1, string units = "", params GUILayoutOption[] options) {
            float fvalue = value;
            var changed = Slider(title, ref fvalue, min, max, (float)defaultValue, 0, units, options);
            value = (int)fvalue;
            return changed;
        }
        public static bool Slider(ref int value, int min, int max, int defaultValue = 1, string units = "", params GUILayoutOption[] options) {
            float fvalue = value;
            var changed = Slider(ref fvalue, min, max, (float)defaultValue, 0, units, options);
            value = (int)fvalue;
            return changed;
        }
        public static bool LogSlider(string title, ref float value, float min, float max, float defaultValue = 1.0f, int decimals = 0, string units = "", params GUILayoutOption[] options) {
            if (min < 0)
                throw new Exception("LogSlider - min value: {min} must be >= 0");
            BeginHorizontal(options);
            using (VerticalScope(Width(300))) {
                Space(UnityModManager.UI.Scale(sliderTop - 1));
                Label(title.cyan(), Width(300));
                Space(UnityModManager.UI.Scale(sliderBottom));
            }
            Space(25);
            value = Math.Max(min, Math.Min(max, value));    // clamp it
            var offset = 1;
            var places = (int)Math.Max(0, Math.Min(15, decimals + 1.01 - Math.Log10(value + offset)));
            var logMin = 100f * (float)Math.Log10(min + offset);
            var logMax = 100f * (float)Math.Log10(max + offset);
            var logValue = 100f * (float)Math.Log10(value + offset);
            var logNewValue = logValue;
            using (VerticalScope(Width(200))) {
                Space(UnityModManager.UI.Scale(sliderTop + 4));
                logNewValue = (float)(GL.HorizontalSlider(logValue, logMin, logMax, Width(200)));
                Space(UnityModManager.UI.Scale(sliderBottom));
            }
            var newValue = (float)Math.Round(Math.Pow(10, logNewValue / 100f) - offset, places);
            Space(25);
            using (VerticalScope(Width(75))) {
                Space(UnityModManager.UI.Scale(sliderTop + 2));
                FloatTextField(ref newValue, null, Width(75));
                Space(UnityModManager.UI.Scale(sliderBottom));
            }
            if (units.Length > 0)
                Label($"{units}".orange().bold(), Width(25 + GUI.skin.label.CalcSize(new GUIContent(units)).x));
            Space(25);
            using (VerticalScope(AutoWidth())) {
                Space(UnityModManager.UI.Scale(sliderTop + 0));
                ActionButton("Reset", () => { newValue = defaultValue; }, AutoWidth());
                Space(UnityModManager.UI.Scale(sliderBottom));
            }
            EndHorizontal();
            var changed = value != newValue;
            value = newValue;
            return changed;
        }
    }
}
