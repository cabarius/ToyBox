using UnityEngine;

using System.Runtime.InteropServices;

namespace ModKit {

    public static partial class Glyphs {
        public static string DefaultCheckOn = "✔";
        public static string DefaultCheckOff = "✖";
        public static string DefaultCheckEmpty = "▪";
        public static string DefaultDisclosureOn = "▼";
        public static string DefaultDisclosureOff = "▶";
        public static string DefaultDisclosureEmpty = "▪";
        public static string DefaultEdit = "✎";
        public static string CharCodeCheckOn = "[x]";
        public static string CharCodeCheckOff = "<b><color=green>[</color><color=red>o</color><color=green>]</color></b>";
        public static string CharCodeCheckEmpty = "<b> <color=yellow>-</color> </b>";
        public static string CharCodeDisclosureOn = "v";
        public static string CharCodeDisclosureOff = ">";
        public static string CharCodeDisclosureEmpty = "-";
        public static string CharCodeEdit = "edit";

        private static bool UseDefaultGlyphs = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static string CheckOn => UseDefaultGlyphs ? DefaultCheckOn : CharCodeCheckOn;
        public static string CheckOff => UseDefaultGlyphs ? DefaultCheckOff : CharCodeCheckOff;
        public static string CheckEmpty => UseDefaultGlyphs ? DefaultCheckEmpty : CharCodeCheckEmpty;
        public static string DisclosureOn => UseDefaultGlyphs ? DefaultDisclosureOn : CharCodeDisclosureOn;
        public static string DisclosureOff => UseDefaultGlyphs ? DefaultDisclosureOff : CharCodeDisclosureOff;
        public static string DisclosureEmpty => UseDefaultGlyphs ? DefaultDisclosureEmpty : CharCodeDisclosureEmpty;
        public static string Edit => UseDefaultGlyphs ? DefaultEdit : CharCodeEdit;
    }
}
