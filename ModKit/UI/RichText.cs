// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using System;
using UnityEngine;

namespace ModKit {
    public static class RichText {
        public static string ToHtmlString(this RGBA color) {
            return $"{color:X}";
        }
        public static string size(this string s, int size) {
            return s = $"<size={size}>{s}</size>";
        }
        public static string mainCategory(this string s) { return s = s.size(16).bold(); }
        public static string bold(this string s) {
            return s = $"<b>{s}</b>";
        }
        public static string italic(this string s) {
            return s = $"<i>{s}</i>";
        }
        public static string color(this string s, string color) {
            return s = $"<color={color}>{s}</color>";
        }
        public static string color(this string str, RGBA color) {
            return $"<color=#{color:X}>{str}</color>";
        }
        public static string color(this string str, Color32 color) {
            return $"<color=#{color.r:X}{color.g:X}{color.b:X}{color.a:X}>{str}</color>";
        }
        public static string white(this string s) { return s = s.color("white"); }
        public static string grey(this string s) { return s = s.color("#A0A0A0FF"); }
        public static string red(this string s) { return s = s.color("#C04040E0"); }
        public static string pink(this string s) { return s = s.color("#FFA0A0E0"); }
        public static string green(this string s) { return s = s.color("#00ff00ff"); }
        public static string blue(this string s) { return s = s.color("blue"); }
        public static string cyan(this string s) { return s = s.color("cyan"); }
        public static string magenta(this string s) { return s = s.color("magenta"); }
        public static string yellow(this string s) { return s = s.color("yellow"); }
        public static string orange(this string s) { return s = s.color("orange"); }

        public static string warningLargeRedFormat(this string s) {
            return s = s.red().size(16).bold();
        }

        public static string SizePercent(this string s, int percent) {
            return s = $"<size={percent}%>{s}</size>";
        }
    }
}

