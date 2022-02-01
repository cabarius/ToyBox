// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;

namespace ModKit {
    public static partial class UI {

        public const string CheckGlyphOn = "<color=green><b>x</b></color>";
        public const string CheckGlyphOff = "<color=red>o</color>";      // #A0A0A0E0
        public const string CheckGlyphEmpty = " <color=#B8B8B8FF> </color> ";
        public const string DisclosureGlyphOn = "<color=#C0C0C0FF><b>v</b></color>";      // ▼▲∧⋀
        public const string DisclosureGlyphOff = "<color=#C0C0C0FF><b>></b></color>";  // ▶▲∨⋁
        public const string DisclosureGlyphEmpty = " <color=#B8B8B8FF> </color> ";

        // Basic UI Elements (box, div, etc.)

        public static void GUIDrawRect(Rect position, Color color) => GUI.Box(position, GUIContent.none, FillStyle(color));

        public static void Div(float indent = 0, float height = 0, float width = 0) => Div(fillColor, indent, height, width);
        public static void DivLast(float height = 0) {
            var rect = GUILayoutUtility.GetLastRect();
            Div(fillColor, rect.x, height, rect.width + 3);
        }
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
