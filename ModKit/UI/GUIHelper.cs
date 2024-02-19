using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ModKit.Utility {
    public static class GUIHelper {
        public const string onMark = $"<color=green><b>{Glyphs.CheckOn()}!</b></color>";
        public const string offMark = $"<color=#A0A0A0E0>{Glyphs.CheckOff()}!</color>";

        public static string FormatOn = Glyphs.DisclosureOn().color(RGBA.white).Bold() + " {0}";
        public static string FormatOff = Glyphs.DisclosureOff().color(RGBA.lime).Bold() + " {0}";
        public static string FormatNone = $" {Glyphs.DisclosureEmpty()}!".color(RGBA.white) + "   {0}";

        public static string GetToggleText(ToggleState toggleState, string text) {
            return toggleState switch {
                ToggleState.Off => string.Format(FormatOff, text),
                ToggleState.On => string.Format(FormatOn, text),
                ToggleState.None => string.Format(FormatNone, text),
                _ => string.Format(FormatNone),
            };
        }

        public static int AdjusterButton(int value, string text, int min = int.MinValue, int max = int.MaxValue) {
            AdjusterButton(ref value, text, min, max);
            return value;
        }

        public static bool AdjusterButton(ref int value, string text, int min = int.MinValue, int max = int.MaxValue) {
            var oldValue = value;
            GUILayout.Label(text, GUILayout.ExpandWidth(false));
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)) && value > min)
                value--;
            GUILayout.Label(value.ToString(), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)) && value < max)
                value++;
            return value != oldValue;
        }

        public static void Hyperlink(string url, Color normalColor, Color hoverColor, GUIStyle style) => Hyperlink(url, url, normalColor, hoverColor, style);

        public static void Hyperlink(string text, string url, Color normalColor, Color hoverColor, GUIStyle style) {
            var color = GUI.color;
            GUI.color = Color.clear;
            GUILayout.Label(text, style, GUILayout.ExpandWidth(false));
            var lastRect = GUILayoutUtility.GetLastRect();
            GUI.color = lastRect.Contains(Event.current.mousePosition) ? hoverColor : normalColor;
            if (GUI.Button(lastRect, text, style))
                Application.OpenURL(url);
            lastRect.y += lastRect.height - 2;
            lastRect.height = 1;
            GUI.DrawTexture(lastRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = color;
        }

        public static void TextField(ref string value, GUIStyle style = null, params GUILayoutOption[] options) => value = GUILayout.TextField(value, style ?? GUI.skin.textField, options);

        public static void TextField(ref string value, Action onChanged, GUIStyle style = null, params GUILayoutOption[] options) => TextField(ref value, null, onChanged, style, options);

        public static void TextField(ref string value, Action onClear, Action onChanged, GUIStyle style = null, params GUILayoutOption[] options) {
            var old = value;
            TextField(ref value, style, options);
            if (value != old) {
                if (onClear != null && string.IsNullOrEmpty(value))
                    onClear();
                else
                    onChanged();
            }
        }
#if false
        static bool CheckboxPrivate(
            ref bool value,
            string title,
            GUIStyle style = null,
            params GUILayoutOption[] options
    ) {
            bool changed = false;
            title = value ? title.Bold() : title.color(RGBA.lightgrey);
            if (UI.Toggle(title, ref value, 0, options)) changed = true;
            //if (GUILayout.Button("" + (value ? onMark : offMark) + " " + title, style, options)) { value = !value; }
            return changed;
        }
        public static bool Checkbox(
                ref bool value,
                String title,
                GUIStyle style = null,
                params GUILayoutOption[] options) {
            return CheckboxPrivate(ref value, title, style, options);
        }
#endif
        public static ToggleState ToggleButton(ToggleState toggle, string text, GUIStyle style = null, params GUILayoutOption[] options) {
            UI.ToggleButton(ref toggle, text, style, options);
            return toggle;
        }

        public static ToggleState ToggleButton(ToggleState toggle, string text, Action on, Action off, GUIStyle style = null, params GUILayoutOption[] options) {
            ToggleButton(ref toggle, text, on, off, style, options);
            return toggle;
        }

        public static void ToggleButton(ref ToggleState toggle, string text, Action on, Action off, GUIStyle style = null, params GUILayoutOption[] options) {
            var old = toggle;
            UI.ToggleButton(ref toggle, text, style, options);
            if (toggle != old) {
                if (toggle.IsOn())
                    on?.Invoke();
                else
                    off?.Invoke();
            }
        }

        public static void ToggleButton(ref ToggleState toggle, string text, ref float minWidth, GUIStyle style = null, params GUILayoutOption[] options) {
            GUIContent content = new(GetToggleText(toggle, text));
            style ??= GUI.skin.button;
            minWidth = Math.Max(minWidth, style.CalcSize(content).x);
            if (GUILayout.Button(content, style, options?.Concat(new[] { GUILayout.Width(minWidth) }).ToArray() ?? new[] { GUILayout.Width(minWidth) }))
                toggle = toggle.Flip();
        }

        public static void ToggleButton(ref ToggleState toggle, string text, ref float minWidth, Action on, Action off, GUIStyle style = null, params GUILayoutOption[] options) {
            var old = toggle;
            ToggleButton(ref toggle, text, ref minWidth, style, options);
            if (toggle != old) {
                if (toggle.IsOn())
                    on?.Invoke();
                else
                    off?.Invoke();
            }
        }

        public static ToggleState ToggleTypeList(ToggleState toggle, string text, HashSet<string> selectedTypes, HashSet<Type> allTypes, GUIStyle style = null, params GUILayoutOption[] options) {
            GUILayout.BeginHorizontal();

            UI.ToggleButton(ref toggle, text, style, options);

            if (toggle.IsOn()) {
                using (new GUILayout.VerticalScope()) {
                    using (new GUILayout.HorizontalScope()) {
                        if (GUILayout.Button("Select All")) {
                            foreach (var type in allTypes) {
                                selectedTypes.Add(type.FullName);
                            }
                        }
                        if (GUILayout.Button("Deselect All")) {
                            selectedTypes.Clear();
                        }
                    }

                    foreach (var type in allTypes) {
                        ToggleButton(selectedTypes.Contains(type.FullName) ? ToggleState.On : ToggleState.Off, type.Name.ToSentence(),
                            () => selectedTypes.Add(type.FullName),
                            () => selectedTypes.Remove(type.FullName),
                            style, options);
                    }
                }
            }

            GUILayout.EndHorizontal();

            return toggle;
        }

        public static void Toolbar(ref int selected, string[] texts, GUIStyle style = null, params GUILayoutOption[] options) => selected = GUILayout.Toolbar(selected, texts, style ?? GUI.skin.button, options);

        public static void SelectionGrid(ref int selected, string[] texts, int xCount, GUIStyle style = null, params GUILayoutOption[] options) => selected = GUILayout.SelectionGrid(selected, texts, xCount, style ?? GUI.skin.button, options);

        public static void SelectionGrid(ref int selected, string[] texts, int xCount, Action onChanged, GUIStyle style = null, params GUILayoutOption[] options) {
            var old = selected;
            SelectionGrid(ref selected, texts, xCount, style, options);
            if (selected != old) {
                onChanged?.Invoke();
            }
        }

        public static float RoundedHorizontalSlider(float value, int digits, float leftValue, float rightValue, params GUILayoutOption[] options) {
            if (digits < 0) {
                var num = (float)Math.Pow(10d, -digits);
                return (float)Math.Round(GUILayout.HorizontalSlider(value, leftValue, rightValue, options) / num, 0) * num;
            }
            else {
                return (float)Math.Round(GUILayout.HorizontalSlider(value, leftValue, rightValue, options), digits);
            }
        }

        private static Texture2D fillTexture = null;
        private static GUIStyle fillStyle = null;
        private static Color fillColor = new(1f, 1f, 1f, 0.65f);
        private static readonly Color color = new(1f, 1f, 1f, 0.35f);
        private static Color fillColor2 = color;

        public static Color FillColor2 { get => fillColor2; set => fillColor2 = value; }

        public static GUIStyle FillStyle(Color color) {
            if (fillTexture == null)
                fillTexture = new Texture2D(1, 1);
            if (fillStyle == null)
                fillStyle = new GUIStyle();
            fillTexture.SetPixel(0, 0, color);
            fillTexture.Apply();
            fillStyle.normal.background = fillTexture;
            return fillStyle;
        }
        public static void GUIDrawRect(Rect position, Color color) => GUI.Box(position, GUIContent.none, FillStyle(color));
        //private static GUIStyle divStyle;
        public static void Div(Color color, float indent = 0, float height = 0, float width = 0) {
            if (fillTexture == null)
                fillTexture = new Texture2D(1, 1);
            var divStyle = new GUIStyle {
                fixedHeight = 1
            };
            fillTexture.SetPixel(0, 0, color);
            fillTexture.Apply();
            divStyle.normal.background = fillTexture;
            divStyle.margin = new RectOffset((int)indent, 0, 4, 4);
            if (width > 0)
                divStyle.fixedWidth = width;
            else
                divStyle.fixedWidth = 0;
            GUILayout.Space((1f * height) / 2f);
            GUILayout.Box(GUIContent.none, divStyle);
            GUILayout.Space(height / 2f);
        }

        public static void Div(float indent = 0, float height = 25, float width = 0) => Div(fillColor, indent, height, width);

    }
}
