// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using UnityEditor;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {

        static GUIStyle linkStyle = null;

        public static bool LinkButton(String title, String url, Action action = null, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { UI.AutoWidth() }; }
            if (linkStyle == null) {
                linkStyle = new GUIStyle(GUI.skin.label);
                linkStyle.wordWrap = false;
                // Match selection color which works nicely for both light and dark skins
                linkStyle.normal.textColor = new Color(0f, 0.75f, 1f);
                linkStyle.stretchWidth = false;

            }
            var result = GL.Button(title, linkStyle, options);
            var rect = GUILayoutUtility.GetLastRect();
            UI.Div(linkStyle.normal.textColor, 4, 0, rect.width);
            if (result) {
                Application.OpenURL(url);
                if (action != null) action(); 
            }
            return result;
        }
    }
}
