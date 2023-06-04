// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using System;
using UnityEngine;

using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {

        // GUILayout wrappers and extensions so other modules can use UI.MethodName()
        public static GUILayoutOption ExpandWidth(bool v) => GL.ExpandWidth(v);
        public static GUILayoutOption ExpandHeight(bool v) => GL.ExpandHeight(v);
        public static GUILayoutOption AutoWidth() => GL.ExpandWidth(false);
        public static GUILayoutOption AutoHeight() => GL.ExpandHeight(false);
        public static GUILayoutOption Width(float v) => GL.Width(v);
        public static GUILayoutOption width(this int v) => GL.Width(v);

        public static GUILayoutOption[] Width(float min, float max) => new GUILayoutOption[] { GL.MinWidth(min), GL.MaxWidth(max) };
        public static GUILayoutOption[] Height(float min, float max) => new GUILayoutOption[] { GL.MinHeight(min), GL.MaxHeight(max) };
        public static GUILayoutOption Height(float v) => GL.Height(v);
        public static GUILayoutOption height(this int v) => GL.Height(v);
        public static GUILayoutOption MaxWidth(float v) => GL.MaxWidth(v);
        public static GUILayoutOption MaxHeight(float v) => GL.MaxHeight(v);
        public static GUILayoutOption MinWidth(float v) => GL.MinWidth(v);
        public static GUILayoutOption MinHeight(float v) => GL.MinHeight(v);

        public static void Space(float size = 150f) => GL.Space(size);
        public static void space(this int size) => GL.Space(size);
        public static void space(this int indent, Action action, params GUILayoutOption[] options) {
            using (HorizontalScope(options)) {
                GL.Space(indent);
                action();
            }
        }
        public static void Indent(int indent, float size = 75f) => GL.Space(indent * size);
        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) => GL.BeginHorizontal(style, options);
        public static void BeginHorizontal(params GUILayoutOption[] options) => GL.BeginHorizontal(options);
        public static void EndHorizontal() => GL.EndHorizontal();
        public static GL.AreaScope AreaScope(Rect screenRect) => new(screenRect);
        public static GL.AreaScope AreaScope(Rect screenRect, string text) => new(screenRect, text);
        public static GL.HorizontalScope HorizontalScope(params GUILayoutOption[] options) => new(options);
        public static GL.HorizontalScope HorizontalScope(float width) => new(Width(width));

        public static GL.HorizontalScope HorizontalScope(GUIStyle style, params GUILayoutOption[] options) => new(style, options);
        public static GL.HorizontalScope HorizontalScope(GUIStyle style, float width) => new(style, Width(width));

        public static GL.VerticalScope VerticalScope(params GUILayoutOption[] options) => new(options);
        public static GL.VerticalScope VerticalScope(GUIStyle style, params GUILayoutOption[] options) => new(style, options);
        public static GL.VerticalScope VerticalScope(float width) => new(Width(width));

        public static GL.ScrollViewScope ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options) => new(scrollPosition, options);
        public static GL.ScrollViewScope ScrollViewScope(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options) => new(scrollPosition, style, options);
        public static void BeginVertical(params GUILayoutOption[] options) => GL.BeginVertical(options);
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) => GL.BeginVertical(style, options);

        public static void EndVertical() => GL.EndVertical();
    }
}
