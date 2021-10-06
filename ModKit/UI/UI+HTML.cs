// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {
        private static GUIStyle linkStyle = null;

        public static bool LinkButton(string title, string url, Action action = null, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { AutoWidth() }; }
            if (linkStyle == null) {
                linkStyle = new GUIStyle(GUI.skin.label) {
                    wordWrap = false
                };
                // Match selection color which works nicely for both light and dark skins
                linkStyle.normal.textColor = new Color(0f, 0.75f, 1f);
                linkStyle.stretchWidth = false;

            }
            var result = GL.Button(title, linkStyle, options);
            var rect = GUILayoutUtility.GetLastRect();
            Div(linkStyle.normal.textColor, 4, 0, rect.width);
            if (result) {
                Application.OpenURL(url);
                action?.Invoke();
            }
            return result;
        }
    }
}
