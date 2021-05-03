// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ModKit.Utility;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public enum ToggleState {
        Off = 0,
        On = 1,
        None = 2
    }
    public static partial class UI {
        public const string onMark = "<color=green><b>✔</b></color>";
        public const string offMark = "<color=#A0A0A0E0>✖</color>";
        public static bool IsOn(this ToggleState state) { return state == ToggleState.On; }
        public static bool IsOff(this ToggleState state) { return state == ToggleState.Off; }
        public static ToggleState Flip(this ToggleState state) {
            switch (state) {
                case ToggleState.Off:
                    return ToggleState.On;
                case ToggleState.On:
                    return ToggleState.Off;
                case ToggleState.None:
                    return ToggleState.None;
            }
            return ToggleState.None;
        }

        static bool TogglePrivate(
                String title,
                ref bool value,
                bool isEmpty,
                bool disclosureStyle = false,
                float width = 0,
                params GUILayoutOption[] options
            ) {
            bool changed = false;
            if (width == 0 && !disclosureStyle) {
                width  = UI.toggleStyle.CalcSize(new GUIContent(title.bold())).x + GUI.skin.box.CalcSize(Private.UI.CheckOn).x + 10;
            }
            options = options.AddItem(width == 0 ? UI.AutoWidth() : UI.Width(width)).ToArray();
            if (!disclosureStyle) {
                title = value ? title.bold() : title.color(RGBA.medgrey).bold();
                if (Private.UI.CheckBox(title, value, UI.toggleStyle, options)) { value = !value; changed = true; }
            }
            else {
                if (Private.UI.DisclosureToggle(title, value, isEmpty, options)) { value = !value; changed = true; }
            }
            return changed;
        }
        public static void ToggleButton(ref ToggleState toggle, string title, GUIStyle style = null, params GUILayoutOption[] options) {
            bool state = toggle.IsOn();
            bool isEmpty = toggle == ToggleState.None;
            if (UI.TogglePrivate(title, ref state, isEmpty, true, 0, options))
                toggle = toggle.Flip();
#if true

#else
            if (GUILayout.Button(GetToggleText(toggle, text), style ?? GUI.skin.button, options))
                toggle = toggle.Flip();
#endif
        }

        public static bool Toggle(
                String title,
                ref bool value,
                float width = 0,
                params GUILayoutOption[] options) {
            return TogglePrivate(title, ref value, false, false, width, options);
        }

        public static bool ActionToggle(
                String title,
                Func<bool> get,
                Action<bool> set,
                float width = 0,
                params GUILayoutOption[] options) {
            bool value = get();
            if (TogglePrivate(title, ref value, false, false, width, options)) {
                set(value);
            }
            return value;
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
        public static bool DisclosureToggle(String title, ref bool value, float width = 175, params Action[] actions) {
            bool changed = UI.TogglePrivate(title, ref value, false, true, width);
            UI.If(value, actions);
            return changed;
        }
        public static bool DisclosureBitFieldToggle(String title, ref int bitfield, int offset, bool exclusive = true, float width = 175, params Action[] actions) {
            bool bit = ((1 << offset) & bitfield) != 0;
            bool newBit = bit;
            TogglePrivate(title, ref newBit, false, true, width);
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
