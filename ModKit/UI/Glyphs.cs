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
        public static string CharCodeCheckOn = "<b><color=green>[X]</color></b>";
        public static string CharCodeCheckOff = "<b>[ ]</b>";
        public static string CharCodeCheckEmpty = "<b> <color=yellow>-</color> </b>";
        public static string CharCodeDisclosureOn = "v";
        public static string CharCodeDisclosureOff = ">";
        public static string CharCodeDisclosureEmpty = "-";
        public static string CharCodeEdit = "edit";
        private static bool UseDefaultGlyphs => Mod.ModKitSettings.UseDefaultGlyphs;
        public static void CheckGlyphSupport() {
            if (Mod.ModKitSettings.CheckForGlyphSupport) {
                if (GUI.skin.font.HasCharacter(DefaultCheckOn[0]) &&
                    GUI.skin.font.HasCharacter(DefaultCheckOff[0]) &&
                    GUI.skin.font.HasCharacter(DefaultCheckEmpty[0]) &&
                    GUI.skin.font.HasCharacter(DefaultDisclosureOn[0]) &&
                    GUI.skin.font.HasCharacter(DefaultDisclosureOff[0]) &&
                    GUI.skin.font.HasCharacter(DefaultDisclosureEmpty[0]) &&
                    GUI.skin.font.HasCharacter(DefaultEdit[0])) {
                    Mod.ModKitSettings.UseDefaultGlyphs = true;
                } else {
                    Mod.ModKitSettings.UseDefaultGlyphs = false;
                }
                Mod.Log($"Glyph Support Check returned: {Mod.ModKitSettings.UseDefaultGlyphs}");
            } else {
                Mod.Log("Skip Glyph Support Check because disabled.");
            }
        }
        public static string CheckOn => UseDefaultGlyphs ? DefaultCheckOn : CharCodeCheckOn;
        public static string CheckOff => UseDefaultGlyphs ? DefaultCheckOff : CharCodeCheckOff;
        public static string CheckEmpty => UseDefaultGlyphs ? DefaultCheckEmpty : CharCodeCheckEmpty;
        public static string DisclosureOn => UseDefaultGlyphs ? DefaultDisclosureOn : CharCodeDisclosureOn;
        public static string DisclosureOff => UseDefaultGlyphs ? DefaultDisclosureOff : CharCodeDisclosureOff;
        public static string DisclosureEmpty => UseDefaultGlyphs ? DefaultDisclosureEmpty : CharCodeDisclosureEmpty;
        public static string Edit => UseDefaultGlyphs ? DefaultEdit : CharCodeEdit;
    }
}
