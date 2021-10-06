// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {

        // UI for picking items from a collection
        public static void Toolbar(ref int value, string[] texts, params GUILayoutOption[] options) => value = GL.Toolbar(value, texts, options);
        public static void Toolbar(ref int value, string[] texts, GUIStyle style, params GUILayoutOption[] options) => value = GL.Toolbar(value, texts, style, options);
        public static bool SelectionGrid(ref int selected, string[] texts, int xCols, params GUILayoutOption[] options) {
            if (xCols <= 0)
                xCols = texts.Count();
            if (IsNarrow)
                xCols = Math.Min(4, xCols);
            var sel = selected;
            var titles = texts.Select((a, i) => i == sel ? a.orange().bold() : a);
            if (xCols <= 0)
                xCols = texts.Count();
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
            return sel != selected;
        }
        public static bool SelectionGrid(string title, ref int selected, string[] texts, int xCols, params GUILayoutOption[] options) {
            using (HorizontalScope()) {
                Label(title.cyan(), Width(300));
                Space(25);
                return SelectionGrid(ref selected, texts, xCols, options);
            }
        }
        public static bool SelectionGrid(ref int selected, string[] texts, int xCols, GUIStyle style, params GUILayoutOption[] options) {
            if (xCols <= 0)
                xCols = texts.Count();
            if (IsNarrow)
                xCols = Math.Min(4, xCols);
            var sel = selected;
            var titles = texts.Select((a, i) => i == sel ? a.orange().bold() : a);
            if (xCols <= 0)
                xCols = texts.Count();
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, style, options);
            return sel != selected;
        }
        public static bool SelectionGrid<T>(ref int selected, T[] items, int xCols, params GUILayoutOption[] options) {
            if (xCols <= 0)
                xCols = items.Count();
            if (IsNarrow)
                xCols = Math.Min(4, xCols);
            var sel = selected;
            var titles = items.Select((a, i) => i == sel ? $"{a}".orange().bold() : $"{a}");
            if (xCols <= 0)
                xCols = items.Count();
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
            return sel != selected;
        }
        public static bool SelectionGrid<T>(ref int selected, T[] items, int xCols, GUIStyle style, params GUILayoutOption[] options) {
            if (xCols <= 0)
                xCols = items.Count();
            if (IsNarrow)
                xCols = Math.Min(4, xCols);
            var sel = selected;
            var titles = items.Select((a, i) => i == sel ? $"{a}".orange().bold() : $"{a}");
            if (xCols <= 0)
                xCols = items.Count();
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, style, options);
            return sel != selected;
        }
        public static void ActionSelectionGrid(ref int selected, string[] texts, int xCols, Action<int> action, params GUILayoutOption[] options) {
            var sel = selected;
            var titles = texts.Select((a, i) => i == sel ? a.orange().bold() : a);
            if (xCols <= 0)
                xCols = texts.Count();
            sel = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
            if (selected != sel) {
                selected = sel;
                action(selected);
            }
        }
        public static void ActionSelectionGrid(ref int selected, string[] texts, int xCols, Action<int> action, GUIStyle style, params GUILayoutOption[] options) {
            var sel = selected;
            var titles = texts.Select((a, i) => i == sel ? a.orange().bold() : a);
            if (xCols <= 0)
                xCols = texts.Count();
            sel = GL.SelectionGrid(selected, titles.ToArray(), xCols, style, options);
            if (selected != sel) {
                selected = sel;
                action(selected);
            }
        }
        public static void EnumGrid<TEnum>(Func<TEnum> get, Action<TEnum> set, int xCols, params GUILayoutOption[] options) where TEnum : struct {
            var value = get();
            var names = Enum.GetNames(typeof(TEnum));
            var index = Array.IndexOf(names, value.ToString());
            if (SelectionGrid(ref index, names, xCols, options)) {
                if (Enum.TryParse(names[index], out TEnum newValue)) {
                    set(newValue);
                }
            }
        }
        public static void EnumGrid<TEnum>(ref TEnum value, int xCols, Func<string, TEnum, string> titleFormater = null, GUIStyle style = null, params GUILayoutOption[] options) where TEnum : struct {
            var names = Enum.GetNames(typeof(TEnum));
            var formatedNames = names;
            var nameToEnum = value.NameToValueDictionary();
            if (titleFormater != null)
                formatedNames = names.Select((n) => titleFormater(n, nameToEnum[n])).ToArray();
            var index = Array.IndexOf(names, value.ToString());
            var oldIndex = index;
            if (style == null ? SelectionGrid(ref index, formatedNames, xCols, options) : SelectionGrid(ref index, formatedNames, xCols, style, options)) {
                if (Enum.TryParse(names[index], out TEnum newValue)) {
                    value = newValue;
                }
            }
        }
        public static void EnumGrid<TEnum>(ref TEnum value, int xCols, Func<string, TEnum, string> titleFormater = null, params GUILayoutOption[] options) where TEnum : struct => EnumGrid(ref value, xCols, titleFormater, null, options);
        public static void EnumGrid<TEnum>(ref TEnum value, int xCols, params GUILayoutOption[] options) where TEnum : struct => EnumGrid(ref value, xCols, null, options);
        public static void EnumGrid<TEnum>(string title, ref TEnum value, int xCols, params GUILayoutOption[] options) where TEnum : struct {
            using (HorizontalScope()) {
                Label(title.cyan(), Width(300));
                Space(25);
                EnumGrid(ref value, xCols, null, options);
            }
        }
        public static void EnumGrid<TEnum>(string title, ref TEnum value, params GUILayoutOption[] options) where TEnum : struct {
            using (HorizontalScope()) {
                Label(title.cyan(), Width(300));
                Space(25);
                EnumGrid(ref value, 0, null, options);
            }
        }

        public static void EnumGrid<TEnum>(string title, ref TEnum value, int xCols, GUIStyle style = null, params GUILayoutOption[] options) where TEnum : struct {
            using (HorizontalScope()) {
                Label(title.cyan(), Width(300));
                Space(25);
                EnumGrid(ref value, xCols, null, style, options);
            }
        }

        public static void EnumGrid<TEnum>(string title, ref TEnum value, int xCols, Func<string, TEnum, string> titleFormater = null, params GUILayoutOption[] options) where TEnum : struct {
            using (HorizontalScope()) {
                Label(title.cyan(), Width(300));
                Space(25);
                EnumGrid(ref value, xCols, titleFormater, options);
            }
        }
        public static void EnumGrid<TEnum>(string title, ref TEnum value, int xCols, Func<string, TEnum, string> titleFormater = null, GUIStyle style = null, params GUILayoutOption[] options) where TEnum : struct {
            using (HorizontalScope()) {
                Label(title.cyan(), Width(300));
                Space(25);
                EnumGrid(ref value, xCols, titleFormater, style, options);
            }
        }
        public static void EnumerablePicker<T>(
                string title,
                ref int selected,
                IEnumerable<T> range,
                int xCols,
                Func<T, string> titleFormater = null,
                params GUILayoutOption[] options
            ) {
            if (titleFormater == null)
                titleFormater = (a) => $"{a}";
            if (selected > range.Count())
                selected = 0;
            var sel = selected;
            var titles = range.Select((a, i) => i == sel ? titleFormater(a).orange().bold() : titleFormater(a));
            if (xCols > range.Count())
                xCols = range.Count();
            if (xCols <= 0)
                xCols = range.Count();
            Label(title, AutoWidth());
            Space(25);
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
        }
        public static NamedFunc<T> TypePicker<T>(string title, ref int selectedIndex, NamedFunc<T>[] items) where T : class {
            var sel = selectedIndex;
            var titles = items.Select((item, i) => i == sel ? item.name.orange().bold() : item.name).ToArray();
            if (title?.Length > 0) { Label(title); }
            selectedIndex = GL.SelectionGrid(selectedIndex, titles, 6);
            return items[selectedIndex];
        }
        public static bool VPicker<T>(
            string title, 
            ref T selected, List<T> items,
            string unselectedTitle,
            Func<T, string> titler, 
            ref string searchText,
            GUIStyle style = null, params GUILayoutOption[] options
            ) where T : class {
            var changed = false;
            using (UI.VerticalScope(style, options)) {
                if (title != null)
                    UI.Label(title, style, options);
                if (searchText != null) {
                    UI.ActionTextField(
                        ref searchText,
                        "itemSearchText",
                        (text) => { changed = true; },
                        () => { },
                        options);
                    if (searchText?.Length > 0) {
                        var searchStr = searchText;
                        items = items.Where(i => titler(i).Contains(searchStr)).ToList();
                    }
                }
                var selectedItemIndex = items.IndexOf(selected);
                if (items.Count() > 0) {
                    selectedItemIndex = Math.Max(0, selectedItemIndex);
                    var newSelected = selected;
                    var titles = items.Select(i => titler(i));
                    var hasUnselectedTitle = unselectedTitle != null;
                    if (hasUnselectedTitle)
                        titles.Prepend<string>(unselectedTitle);
                    UI.ActionSelectionGrid(
                        ref selectedItemIndex,
                        titles.ToArray(),
                        1,
                        index => { changed = true; },
                        style,
                        options);
                    if (hasUnselectedTitle)
                        selectedItemIndex -= 1;

                    selected = selectedItemIndex >= 0 ? items[selectedItemIndex] : null;
                }
                else {
                    UI.Label("No Items".grey(), options);
                }
            }
            return changed;
        }
    }
}
