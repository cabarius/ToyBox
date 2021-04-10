using UnityEditor;
using UnityEngine;

namespace ToyBox.Private {
    public static partial class UI {
        const string disclosureArrowOn = "<color=orange><b>▶</b></color>";
        const string disclosureArrowOff = "<color=white><b>▲</b></color>";

        // Helper functionality.

        private static readonly GUIContent _LabelContent = new GUIContent();
        private static readonly GUIContent OnContent = new GUIContent(disclosureArrowOn);
        private static readonly GUIContent OffContent = new GUIContent(disclosureArrowOff);
        private static GUIContent LabelContent(string text) {
            _LabelContent.text = text;
            _LabelContent.image = null;
            _LabelContent.tooltip = null;
            return _LabelContent;
        }

        private static readonly int s_ButtonHint = "MyGUI.Button".GetHashCode();

        public static bool DisclosureToggle(Rect rect, GUIContent label, bool value, GUIStyle style) {
            int controlID = GUIUtility.GetControlID(s_ButtonHint, FocusType.Passive, rect);
            bool result = false;
            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (GUI.enabled && rect.Contains(Event.current.mousePosition)) {
                        GUIUtility.hotControl = controlID;
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID) {
                        Event.current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID) {
                        GUIUtility.hotControl = 0;

                        if (rect.Contains(Event.current.mousePosition)) {
                            result = true;
                            Event.current.Use();
                        }
                    }
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.hotControl == controlID) {
                        if (Event.current.keyCode == KeyCode.Escape) {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();
                        }
                    }
                    break;

                case EventType.Repaint:
                    // Arrow lines up on the right
                    var arrowStyle = GUI.skin.button;
                    var arrow = value ? OnContent : OffContent;
                    var arrowSize = arrowStyle.CalcSize(arrow);
                    Rect arrowRect = new Rect(rect.xMax - arrowSize.x, rect.y, arrowSize.x, arrowSize.y);

                    // Label bumps up to arrow on the left (FIXME - BIDI?????)
                    var labelStyle = GUI.skin.label;
                    var labelSize = labelStyle.CalcSize(label);
                    Rect labelRect = new Rect(arrowRect.x - labelSize.x - 10, rect.y, labelSize.x, labelSize.y);

                    labelStyle.Draw(labelRect, label, controlID);
                    arrowStyle.Draw(arrowRect, arrow, controlID);
                    break;
            }

            return result;
        }

        public static bool DisclosureToggle(Rect position, GUIContent label, bool value) {
            return DisclosureToggle(position, label, value, GUI.skin.label);
        }

        public static bool DisclosureToggle(Rect position, string label, bool value, GUIStyle style) {
            return DisclosureToggle(position, LabelContent(label), value, style);
        }

        public static bool DisclosureToggle(Rect position, string label, bool value) {
            return DisclosureToggle(position, label, value, GUI.skin.label);
        }

        // Button Control - Layout Version
        static Vector2 cachedArrowSize = new Vector2(0, 0);
        public static bool DisclosureToggle(GUIContent label, bool value, GUIStyle style, params GUILayoutOption[] options) {
            style = new GUIStyle(style);
            if (cachedArrowSize.x == 0) cachedArrowSize = style.CalcSize(OffContent);
            RectOffset padding = new RectOffset(0, (int)cachedArrowSize.x + 10, 0, 0);
            style.padding = padding;
            Rect position = GUILayoutUtility.GetRect(label, style, options);
            return DisclosureToggle(position, label, value, style);
        }

        public static bool DisclosureToggle(GUIContent label, bool value, params GUILayoutOption[] options) {
            return DisclosureToggle(label, value, GUI.skin.label, options);
        }

        public static bool DisclosureToggle(string label, bool value, GUIStyle style, params GUILayoutOption[] options) {
            return DisclosureToggle(LabelContent(label), value, style, options);
        }

        public static bool DisclosureToggle(string label, bool value, params GUILayoutOption[] options) {
            return DisclosureToggle(label, value, GUI.skin.label, options);
        }
    }
}