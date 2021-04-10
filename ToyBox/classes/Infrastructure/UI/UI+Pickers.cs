// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

using GL = UnityEngine.GUILayout;

namespace ToyBox {
    public static partial class UI {

        // UI for picking items from a collection
        public static void Toolbar(ref int value, String[] texts, params GUILayoutOption[] options) {
            value = GL.Toolbar(value, texts, options);
        }

        public static bool SelectionGrid(ref int selected, String[] texts, int xCols, params GUILayoutOption[] options) {
            if (xCols <= 0) xCols = texts.Count();
            int sel = selected;
            var titles = texts.Select((a, i) => i == sel ? a.orange().bold() : a);
            if (xCols <= 0) xCols = texts.Count();
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
            return sel != selected;
        }
        public static bool SelectionGrid<T>(ref int selected, T[] items, int xCols, params GUILayoutOption[] options) {
            if (xCols <= 0) xCols = items.Count();
            int sel = selected;
            var titles = items.Select((a, i) => i == sel ? $"{a}".orange().bold() : $"{a}");
            if (xCols <= 0) xCols = items.Count();
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
            return sel != selected;
        }
        public static void EnumGrid<TEnum>(Func<TEnum> get, Action<TEnum> set, int xCols, params GUILayoutOption[] options) where TEnum : struct {
            var value = get();
            var names = Enum.GetNames(typeof(TEnum));
            int index = Array.IndexOf(names, value.ToString());
            if (UI.SelectionGrid(ref index, names, xCols, options)) {
                TEnum newValue;
                if (Enum.TryParse(names[index], out newValue)) {
                    set(newValue);
                }
            }
        }
        public static void EnumGrid<TEnum>(ref TEnum value, int xCols, params GUILayoutOption[] options) where TEnum : struct {
            var names = Enum.GetNames(typeof(TEnum));
            int index = Array.IndexOf(names, value.ToString());
            if (UI.SelectionGrid(ref index, names, xCols, options)) {
                TEnum newValue;
                if (Enum.TryParse(names[index], out newValue)) {
                    value = newValue;
                }
            }
        }
        public static void EnumGrid<TEnum>(String title, ref TEnum value, int xCols, params GUILayoutOption[] options) where TEnum : struct {
            UI.BeginHorizontal();
            UI.Label(title.cyan(), UI.Width(300));
            UI.Space(25);
            UI.EnumGrid<TEnum>(ref value, xCols, options);
            UI.EndHorizontal();
        }
        public static void ActionSelectionGrid(ref int selected, String[] texts, int xCols, Action<int> action, params GUILayoutOption[] options) {
            int sel = selected;
            var titles = texts.Select((a, i) => i == sel ? a.orange().bold() : a);
            if (xCols <= 0) xCols = texts.Count();
            sel = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
            if (selected != sel) {
                selected = sel;
                action(selected);
            }
        }
        public static void EnumerablePicker<T>(
                String title,
                ref int selected,
                IEnumerable<T> range,
                int xCols,
                Func<T, String> titleFormater = null,
                params GUILayoutOption[] options
            ) {
            if (titleFormater == null) titleFormater = (a) => $"{a}";
            if (selected > range.Count()) selected = 0;
            int sel = selected;
            var titles = range.Select((a, i) => i == sel ? titleFormater(a).orange().bold() : titleFormater(a));
            if (xCols > range.Count()) xCols = range.Count();
            if (xCols <= 0) xCols = range.Count();
            UI.Label(title, UI.AutoWidth());
            UI.Space(25);
            selected = GL.SelectionGrid(selected, titles.ToArray(), xCols, options);
        }

        public static NamedFunc<T> TypePicker<T>(String title, ref int selectedIndex, NamedFunc<T>[] items) where T : class {
            int sel = selectedIndex;
            var titles = items.Select((item, i) => i == sel ? item.name.orange().bold() : item.name).ToArray();
            if (title?.Length > 0) { Label(title); }
            selectedIndex = GL.SelectionGrid(selectedIndex, titles, 6);
            return items[selectedIndex];
        }
    }
}
