// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using GL = UnityEngine.GUILayout;

namespace ToyBox {
    public static partial class UI {

        static bool TogglePrivate(
                String title,
                ref bool value,
                bool disclosureStyle = false,
                bool forceHorizontal = true,
                float width = 0,
                params GUILayoutOption[] options
            ) {
            bool changed = false;
            options = options.AddItem(width == 0 ? UI.AutoWidth() : UI.Width(width)).ToArray();
            if (!disclosureStyle) {
                title = value ? title.bold() : title.grey();
                if (GL.Button("" + (value ? onMark : offMark) + " " + title, options)) { value = !value; }
            }
            else {
                if (Private.UI.DisclosureToggle(title, value, options)) { value = !value; changed = true; }
            }
            return changed;
        }
        public static bool Toggle(
                String title,
                ref bool value,
                float width = 0,
                params GUILayoutOption[] options) {
            return TogglePrivate(title, ref value, false, false, width, options);
        }

        public static bool BitFieldToggle(
                String title,
                ref int bitfield,
                int offset,
                float width = 0,
                params GUILayoutOption[] options
            ) {
            bool bit = ((1 << offset) & bitfield) != 0;
            bool newBit = bit;
            TogglePrivate(title, ref newBit, false, false, width, options);
            if (bit != newBit) { bitfield ^= 1 << offset; }
            return bit != newBit;
        }
        public static bool DisclosureToggle(String title, ref bool value, bool forceHorizontal = true, float width = 175, params Action[] actions) {
            bool changed = UI.TogglePrivate(title, ref value, true, forceHorizontal, width);
            UI.If(value, actions);
            return changed;
        }
        public static bool DisclosureBitFieldToggle(String title, ref int bitfield, int offset, bool exclusive = true, bool forceHorizontal = true, float width = 175, params Action[] actions) {
            bool bit = ((1 << offset) & bitfield) != 0;
            bool newBit = bit;
            TogglePrivate(title, ref newBit, true, forceHorizontal, width);
            if (bit != newBit) {
                if (exclusive) {
                    bitfield = (newBit ? 1 << offset : 0);
                }
                else {
                    bitfield ^= (1 << offset);
                }
            }
            UI.If(newBit, actions);
            return bit != newBit;
        }
    }
}
