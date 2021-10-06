// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;

namespace ModKit {
    public static partial class UI {

        public const string ChecklyphOn = "<color=green><b>✔</b></color>";
        public const string CheckGlyphOff = "<color=#B8B8B8FF>✖</color>";      // #A0A0A0E0
        public const string DisclosureGlyphOn = "<color=orange><b>▼</b></color>";      // ▼▲∧⋀
        public const string DisclosureGlyphOff = "<color=#C0C0C0FF><b>▶</b></color>";  // ▶▲∨⋁
        public const string DisclosureGlyphEmpty = " <color=#B8B8B8FF>▪</color> ";

        // Basic UI Elements (box, div, etc.)

        private static Texture2D fillTexture = null;
        private static GUIStyle fillStyle = null;
        private static Color fillColor = new(1f, 1f, 1f, 0.65f);

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
        private static GUIStyle _buttonStyle;
        public static GUIStyle buttonStyle {
            get {
                if (_buttonStyle == null)
                    _buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
                return _buttonStyle;
            }
        }
        private static GUIStyle _largeStyle;

        public static GUIStyle largeStyle {
            get {
                if (_largeStyle == null)
                    _largeStyle = new GUIStyle(GUI.skin.box) {
                        richText = true
                    };
                _largeStyle.fixedHeight = UnityModManager.UI.Scale(24);
                //_largeStyle.contentOffset = new Vector2(0, UnityModManager.UI.Scale(-6));
                _largeStyle.padding = new RectOffset(0, 0, UnityModManager.UI.Scale(-3), 0);
#pragma warning disable CS0618 // Type or member is obsolete
                _largeStyle.clipOffset = new Vector2(0, UnityModManager.UI.Scale(3));
#pragma warning restore CS0618 // Type or member is obsolete
                _largeStyle.fontSize = UnityModManager.UI.Scale(21);
                _largeStyle.fontStyle = FontStyle.Bold;
                _largeStyle.normal.background = GUI.skin.label.normal.background;

                return _largeStyle;
            }
        }

        private static GUIStyle _toggleStyle;
        public static GUIStyle toggleStyle {
            get {
                if (_toggleStyle == null)
                    _toggleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
                return _toggleStyle;
            }
        }
        public static GUIStyle divStyle;
        public static void Div(Color color, float indent = 0, float height = 0, float width = 0) {
            if (fillTexture == null)
                fillTexture = new Texture2D(1, 1);
            //if (divStyle == null) {
            divStyle = new GUIStyle {
                fixedHeight = 1
            };
            //}
            fillTexture.SetPixel(0, 0, color);
            fillTexture.Apply();
            divStyle.normal.background = fillTexture;
            if (divStyle.margin == null) {
                divStyle.margin = new RectOffset((int)indent, 0, 4, 4);
            }
            else {
                divStyle.margin.left = (int)indent;
            }
            if (width > 0)
                divStyle.fixedWidth = width;
            else
                divStyle.fixedWidth = 0;
            Space((2f * height) / 3f);
            GUILayout.Box(GUIContent.none, divStyle);
            Space(height / 3f);
        }

        public static void Div(float indent = 0, float height = 0, float width = 0) => Div(fillColor, indent, height, width);

        public static void Wrap(bool condition, float indent = 0, float space = 10) {
            if (condition) {
                EndHorizontal();
                Space(space);
                BeginHorizontal();
                Space(indent);
            }
        }
    }
}
