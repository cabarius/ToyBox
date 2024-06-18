// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModKit {
    public static class RichText {
        public static string ToHtmlString(this RGBA color) => $"{color:X}";
        public static string ToAcronym(this string s) => string.Concat(s.Where((c, i) => char.IsUpper(c) || (char.IsLetter(c) && i == 0)));
        public static string size(this string? s, int size) => _ = $"<size={size}>{s}</size>";
        public static string? mainCategory(this string s) => s.size(16).bold();

        public static string? bold(this string? s) => _ = $"<b>{s}</b>";
        public static string italic(this string s) => _ = $"<i>{s}</i>";
        public static string? color(this string? s, string color) => _ = $"<color={color}>{s}</color>";
        public static string? color(this string? str, RGBA color) => $"<color=#{color:X}>{str}</color>";
        public static string color(this string str, Color32 color) => $"<color=#{color.r:X}{color.g:X}{color.b:X}{color.a:X}>{str}</color>";
        public static string color(this string str, Color color) => $"<color=#{(int)(color.r * 256):X}{(int)(color.g * 256):X}{(int)(color.b * 256):X}{(int)(color.a * 256):X}>{str}</color>";
        public static string colorCaps(this string str, RGBA color) => Regex.Replace(str, @"([A-Z])([A-Za-z]+)",
                                                  "$1".color(color) + "$2");
        public static string? white(this string s) => s.color("white");

        public static string? grey(this string? s) => s.color("#A0A0A0FF");
        public static string? darkGrey(this string s) => s.color("#505050FF");

        public static string? red(this string? s) => s.color("#C04040E0");

        public static string? pink(this string s) => s.color("#FFA0A0E0");

        public static string? green(this string? s) => s.color("#00ff00ff");

        public static string? blue(this string s) => s.color("blue");

        public static string? cyan(this string? s) => s.color("cyan");

        public static string? magenta(this string s) => s.color("magenta");

        public static string? yellow(this string? s) => s.color("yellow");

        public static string? orange(this string? s) => s.color("orange");

        public static string warningLargeRedFormat(this string s) => _ = s.red().size(16).bold();

        public static string sizePercent(this string s, int percent) => _ = $"<size={percent}%>{s}</size>";
    }
}

