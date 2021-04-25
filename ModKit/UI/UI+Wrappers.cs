// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {

        // GUILayout wrappers and extensions so other modules can use UI.MethodName()
        public static GUILayoutOption ExpandWidth(bool v) { return GL.ExpandWidth(v); }
        public static GUILayoutOption ExpandHeight(bool v) { return GL.ExpandHeight(v); }
        public static GUILayoutOption AutoWidth() { return GL.ExpandWidth(false); }
        public static GUILayoutOption AutoHeight() { return GL.ExpandHeight(false); }
        public static GUILayoutOption Width(float v) { return GL.Width(v); }
        public static GUILayoutOption[] Width(float min, float max) {
            return new GUILayoutOption[] { GL.MinWidth(min), GL.MaxWidth(max) };
        }
        public static GUILayoutOption[] Height(float min, float max) {
            return new GUILayoutOption[] { GL.MinHeight(min), GL.MaxHeight(max) };
        }
        public static GUILayoutOption Height(float v) { return GL.Width(v); }
        public static GUILayoutOption MaxWidth(float v) { return GL.MaxWidth(v); }
        public static GUILayoutOption MaxHeight(float v) { return GL.MaxHeight(v); }
        public static GUILayoutOption MinWidth(float v) { return GL.MinWidth(v); }
        public static GUILayoutOption MinHeight(float v) { return GL.MinHeight(v); }

        public static void Space(float size = 150f) { GL.Space(size); }
        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) { GL.BeginHorizontal(style, options); }
        public static void BeginHorizontal(params GUILayoutOption[] options) { GL.BeginHorizontal(options); }
        public static void EndHorizontal() { GL.EndHorizontal(); }
        public static GL.AreaScope AreaScope(Rect screenRect) { return new GL.AreaScope(screenRect); }
        public static GL.AreaScope AreaScope(Rect screenRect, String text) { return new GL.AreaScope(screenRect, text); }
        public static GL.HorizontalScope HorizontalScope(params GUILayoutOption[] options) { return new GL.HorizontalScope(options);  }
        public static GL.HorizontalScope HorizontalScope(GUIStyle style, params GUILayoutOption[] options) { return new GL.HorizontalScope(style, options); }
        public static GL.VerticalScope VerticalScope(params GUILayoutOption[] options) { return new GL.VerticalScope(options); }
        public static GL.VerticalScope VerticalScope(GUIStyle style, params GUILayoutOption[] options) { return new GL.VerticalScope(style, options); }
        public static GL.ScrollViewScope ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options) { return new GL.ScrollViewScope(scrollPosition, options); }
        public static GL.ScrollViewScope ScrollViewScope(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options) { return new GL.ScrollViewScope(scrollPosition, style, options); }
        public static void BeginVertical(params GUILayoutOption[] options) { GL.BeginHorizontal(options); }
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) { GL.BeginHorizontal(style, options); }

        public static void EndVertical() { GL.BeginHorizontal(); }

    }
}
