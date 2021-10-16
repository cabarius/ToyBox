using UnityEngine;

namespace ModKit.Private {
    public static partial class UI {

        // Helper functionality.

        private static readonly GUIContent _LabelContent = new();
        public static readonly GUIContent CheckOn = new(ModKit.UI.ChecklyphOn);
        public static readonly GUIContent CheckOff = new(ModKit.UI.CheckGlyphOff);
        public static readonly GUIContent DisclosureOn = new(ModKit.UI.DisclosureGlyphOn);
        public static readonly GUIContent DisclosureOff = new(ModKit.UI.DisclosureGlyphOff);
        public static readonly GUIContent DisclosureEmpty = new(ModKit.UI.DisclosureGlyphEmpty);
        private static GUIContent LabelContent(string text) {
            _LabelContent.text = text;
            _LabelContent.image = null;
            _LabelContent.tooltip = null;
            return _LabelContent;
        }

        private static readonly int s_ButtonHint = "MyGUI.Button".GetHashCode();

        public static bool Toggle(Rect rect, GUIContent label, bool value, bool isEmpty, GUIContent on, GUIContent off, GUIStyle stateStyle, GUIStyle labelStyle) {
            var controlID = GUIUtility.GetControlID(s_ButtonHint, FocusType.Passive, rect);
            var result = false;
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

                case EventType.Repaint: {
                        //bool leftAlign = stateStyle.alignment == TextAnchor.MiddleLeft
                        //                || stateStyle.alignment == TextAnchor.UpperLeft
                        //                || stateStyle.alignment == TextAnchor.LowerLeft
                        //                ;
                        var rightAlign = stateStyle.alignment == TextAnchor.MiddleRight
                                        || stateStyle.alignment == TextAnchor.UpperRight
                                        || stateStyle.alignment == TextAnchor.LowerRight
                                        ;
                        // stateStyle.alignment determines position of state element
                        var state = isEmpty ? DisclosureEmpty : value ? on : off;
                        var stateSize = stateStyle.CalcSize(value ? on : off);  // don't use the empty content to calculate size so titles line up in lists
                        var x = rightAlign ? rect.xMax - stateSize.x : rect.x;
                        Rect stateRect = new(x, rect.y, stateSize.x, stateSize.y);

                        // layout state before or after following alignment
                        var labelSize = labelStyle.CalcSize(label);
                        x = rightAlign ? stateRect.x - stateSize.x - 5 : stateRect.xMax + 5;
                        Rect labelRect = new(x, rect.y, labelSize.x, labelSize.y);

                        stateStyle.Draw(stateRect, state, controlID);
                        labelStyle.Draw(labelRect, label, controlID);
                    }
                    break;
            }
            return result;
        }

        // Button Control - Layout Version

#if false
        static Vector2 cachedArrowSize = new Vector2(0, 0);
        public static bool Toggle(GUIContent label, bool value, GUIContent on, GUIContent off, GUIStyle stateStyle, GUIStyle labelStyle, params GUILayoutOption[] options) {
            var style = new GUIStyle(labelStyle);
            if (cachedArrowSize.x == 0)
                cachedArrowSize = style.CalcSize(off);
            RectOffset padding = new RectOffset(0, (int)cachedArrowSize.x + 10, 0, 0);
            style.padding = padding;
            Rect rect = GUILayoutUtility.GetRect(label, style, options);
            return Toggle(rect, label, value, on, off, stateStyle, style);
        }
#else
        public static bool Toggle(GUIContent label, bool value, GUIContent on, GUIContent off, GUIStyle stateStyle, GUIStyle labelStyle, bool isEmpty = false, params GUILayoutOption[] options) {
            var state = value ? on : off;
            var sStyle = new GUIStyle(stateStyle);
            var lStyle = new GUIStyle(labelStyle) {
                wordWrap = false
            };
            var stateSize = sStyle.CalcSize(state);
            lStyle.fixedHeight = stateSize.y - 2;
            var padding = new RectOffset(0, (int)stateSize.x + 5, 0, 0);
            lStyle.padding = padding;
            var rect = GUILayoutUtility.GetRect(label, lStyle, options);
#if false
            var labelSize = lStyle.CalcSize(label);
            var width = stateSize.x + 10 + stateSize.x;
            var height = Mathf.Max(stateSize.y, labelSize.y);
            var rect = GUILayoutUtility.GetRect(width, height);
            int controlID = GUIUtility.GetControlID(s_ButtonHint, FocusType.Passive, rect);
            var eventType = Event.current.GetTypeForControl(controlID);

            Logger.Log($"event: {eventType.ToString()} label: {label.text} w: {width} h: {height} rect: {rect} options: {options.Length}");
#endif
            return Toggle(rect, label, value, isEmpty, on, off, stateStyle, labelStyle);
        }
#endif
        public static bool Toggle(string label, bool value, string on, string off, GUIStyle stateStyle, GUIStyle labelStyle, params GUILayoutOption[] options) => Toggle(LabelContent(label), value, new GUIContent(on), new GUIContent(off), stateStyle, labelStyle, false, options);
        // Disclosure Toggles
        public static bool DisclosureToggle(GUIContent label, bool value, bool isEmpty = false, params GUILayoutOption[] options) => Toggle(label, value, DisclosureOn, DisclosureOff, GUI.skin.textArea, GUI.skin.label, isEmpty, options);
        public static bool DisclosureToggle(string label, bool value, GUIStyle stateStyle, GUIStyle labelStyle, bool isEmpty = false, params GUILayoutOption[] options) => Toggle(LabelContent(label), value, DisclosureOn, DisclosureOff, stateStyle, labelStyle, isEmpty, options);
        public static bool DisclosureToggle(string label, bool value, bool isEmpty = false, params GUILayoutOption[] options) => DisclosureToggle(label, value, GUI.skin.box, GUI.skin.label, isEmpty, options);
        // CheckBox 
        public static bool CheckBox(GUIContent label, bool value, bool isEmpty, params GUILayoutOption[] options) => Toggle(label, value, CheckOn, CheckOff, GUI.skin.textArea, GUI.skin.label, isEmpty, options);

        public static bool CheckBox(string label, bool value, bool isEmpty, GUIStyle style, params GUILayoutOption[] options) => Toggle(LabelContent(label), value, CheckOn, CheckOff, GUI.skin.box, style, isEmpty, options);

        public static bool CheckBox(string label, bool value, bool isEmpty, params GUILayoutOption[] options) => CheckBox(label, value, isEmpty, GUI.skin.label, options);
    }
}