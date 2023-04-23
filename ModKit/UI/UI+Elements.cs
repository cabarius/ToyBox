// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;

namespace ModKit {
    public static partial class UI {

        public static string ChecklyphOn = $"<color=green><b>{Glyphs.CheckOn}</b></color>";
        public static string CheckGlyphOff = $"<color=#B8B8B8FF>{Glyphs.CheckOff}</color>"; // #A0A0A0E0
        public static string CheckGlyphEmpty = $" <color=#B8B8B8FF>{Glyphs.CheckEmpty}</color> ";
        public static string DisclosureGlyphOn = $"<color=orange><b>{Glyphs.DisclosureOn}</b></color>"; // ▼▲∧⋀
        public static string DisclosureGlyphOff = $"<color=#C0C0C0FF><b>{Glyphs.DisclosureOff}</b></color>"; // ▶▲∨⋁
        public static string DisclosureGlyphEmpty = $" <color=#B8B8B8FF>{Glyphs.DisclosureEmpty}</color> ";

        // Basic UI Elements (box, div, etc.)

        public static void GUIDrawRect(Rect position, Color color) => GUI.Box(position, GUIContent.none, FillStyle(color));

        public static void Div(float indent = 0, float height = 0, float width = 0) => DrawDiv(fillColor, indent, height, width);
        public static void DivLast(float height = 0) {
            var rect = GUILayoutUtility.GetLastRect();
            DrawDiv(fillColor, rect.x, height, rect.width + 3);
        }
        public static void DivToLast(float indent = 0, float height = 0) {
            var rect = GUILayoutUtility.GetLastRect();
            DrawDiv(fillColor, indent, height, rect.x + rect.width + 3);
        }
        public static Rect DivLastRect() => GUILayoutUtility.GetLastRect();
        public static void DivLast(Rect rect, float height = 0) => DrawDiv(fillColor, rect.x, height, rect.width + 3);
        public static void DivToLast(Rect rect, float indent = 0, float height = 0) => DrawDiv(fillColor, indent, height, rect.x + rect.width + 3);

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
