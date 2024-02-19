// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {
        private static GUIStyle linkStyle = null;

        public static bool LinkButton(string? title, string url, Action? action = null, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { AutoWidth() }; }
            if (linkStyle == null) {
                linkStyle = new GUIStyle(rarityStyle) {
                    wordWrap = false
                };
                //linkStyle.normal.background = RarityTexture;
                // Match selection color which works nicely for both light and dark skins
                linkStyle.padding = new RectOffset(-3.point(), 0, 0, 0);
#pragma warning disable CS0618 // Type or member is obsolete
                linkStyle.clipOffset = new Vector2(0.point(), 0);
#pragma warning restore CS0618 // Type or member is obsolete
                linkStyle.normal.textColor = new Color(0f, 0.75f, 1f);
                linkStyle.stretchWidth = false;

            }
            bool result;
            Rect rect;
            using (VerticalScope()) {
                using (HorizontalScope()) {
                    Space(6.point());
                    result = GL.Button(title, linkStyle, options);
                    rect = GUILayoutUtility.GetLastRect();
                }
                DrawDiv(linkStyle.normal.textColor, 0, 0, rect.width + 4.point());
            }
            if (result) {
                Application.OpenURL(url);
                action?.Invoke();
            }
            return result;
        }
    }
}
