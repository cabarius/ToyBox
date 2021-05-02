// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {

        // Basic UI Elements (box, div, etc.)

        private static Texture2D fillTexture = null;
        private static GUIStyle fillStyle = null;
        private static Color fillColor = new Color(1f, 1f, 1f, 0.65f);
        private static Color fillColor2 = new Color(1f, 1f, 1f, 0.35f);

        public static GUIStyle FillStyle(Color color) {
            if (fillTexture == null) fillTexture = new Texture2D(1, 1);
            if (fillStyle == null) fillStyle = new GUIStyle();
            fillTexture.SetPixel(0, 0, color);
            fillTexture.Apply();
            fillStyle.normal.background = fillTexture;
            return fillStyle;
        }
        public static void GUIDrawRect(Rect position, Color color) {

            GUI.Box(position, GUIContent.none, FillStyle(color));
        }

        private static GUIStyle _buttonStyle;
        public static GUIStyle buttonStyle { get { 
                if (_buttonStyle == null) _buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };
                return _buttonStyle;
            } }
        private static GUIStyle _toggleStyle;
        public static GUIStyle toggleStyle {
            get {
                if (_toggleStyle == null)
                    _toggleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft };
                return _toggleStyle;
            }
        }
        private static GUIStyle divStyle;
        public static void Div(Color color, float indent = 0, float height = 0, float width = 0) {
            if (fillTexture == null) fillTexture = new Texture2D(1, 1);
            //if (divStyle == null) {
                divStyle = new GUIStyle();
                divStyle.fixedHeight = 1;
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
            if (width > 0) divStyle.fixedWidth = width;
            else divStyle.fixedWidth = 0;
            UI.Space((2f * height) / 3f);
            GUILayout.Box(GUIContent.none, divStyle);
            UI.Space(height / 3f);
        }

        public static void Div(float indent = 0, float height = 0, float width = 0) {
            Div(fillColor, indent, height, width);
        }

        public static void Wrap(bool condition, float indent = 0, float space = 10) {
            if (condition) {
                UI.EndHorizontal();
                UI.Space(space);
                UI.BeginHorizontal();
                UI.Space(indent);
            }
        }
    }
}
