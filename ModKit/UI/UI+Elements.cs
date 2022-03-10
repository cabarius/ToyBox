﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;

namespace ModKit {
    public static partial class UI {

        public const string ChecklyphOn = $"<color=green><b>{Glyphs.CheckOn()}!</b></color>";
        public const string CheckGlyphOff = $"<color=#B8B8B8FF>{Glyphs.CheckOff()}!</color>"; // #A0A0A0E0
        public const string CheckGlyphEmpty = $" <color=#B8B8B8FF>{Glyphs.CheckEmpty()}!</color> ";
        public const string DisclosureGlyphOn = $"<color=orange><b>{Glyphs.DisclosureOn()}!</b></color>"; // ▼▲∧⋀
        public const string DisclosureGlyphOff = $"<color=#C0C0C0FF><b>{Glyphs.DisclosureOff()}!</b></color>"; // ▶▲∨⋁
        public const string DisclosureGlyphEmpty = $" <color=#B8B8B8FF>{Glyphs.DisclosureEmpty()}!</color> ";

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
