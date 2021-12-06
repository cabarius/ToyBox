using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ModKit.Utility {
    public static class StringExtensions {
        public static bool Matches(this string source, string other) {
            if (source == null || other == null)
                return false;
#if false
            return source.IndexOf(other, 0, StringComparison.InvariantCulture) != -1;
#else
            return source.IndexOf(other, 0, StringComparison.InvariantCultureIgnoreCase) != -1;
#endif
        }
        public static string MarkedSubstring(this string source, string other) {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(other))
                return source;
#if false
            if (source.Contains(other)) {
                return source.Replace(other, other.Cyan()).Bold();
            }
            //source = source.Replace(source, other.Cyan()).Bold();
#else
            var index = source.IndexOf(other, StringComparison.InvariantCultureIgnoreCase);
            if (index != -1) {
                var substr = source.Substring(index, other.Length);
                source = source.Replace(substr, substr.Cyan()).Bold();
            }
#endif
            return source;
        }
        public static string Repeat(this string s, int n) {
            if (n < 0 || s == null || s.Length == 0)
                return s;
            return new StringBuilder(s.Length * n).Insert(0, s, n).ToString();
        }
        public static string Indent(this string s, int n) => "    ".Repeat(n) + s;

    }

    public static class RichTextExtensions {
        // https://docs.unity3d.com/Manual/StyledText.html

        public enum RGBA : uint {
            aqua = 0x00ffffff,
            blue = 0x8080ffff,
            brown = 0xC09050ff, //0xa52a2aff,
            cyan = 0x00ffffff,
            darkblue = 0x0000a0ff,
            fuchsia = 0xff40ffff,
            green = 0x40C040ff,
            lightblue = 0xd8e6ff,
            lime = 0x40ff40ff,
            magenta = 0xff40ffff,
            maroon = 0xFF6060ff,
            navy = 0x000080ff,
            olive = 0xB0B000ff,
            orange = 0xffa500ff, // 0xffa500ff,
            purple = 0xC060F0ff,
            red = 0xFF4040ff,
            teal = 0x80f0c0ff,
            yellow = 0xffff00ff,
            black = 0x000000ff,
            darkgrey = 0x808080ff,
            silver = 0xD0D0D0ff,
            grey = 0xC0C0C0ff,
            lightgrey = 0xE8E8E8ff,
            white = 0xffffffff,
        }

        public static string ToHtmlString(this RGBA color) => $"{color:X}";

        public static string Bold(this string str) => $"<b>{str}</b>";

        public static string Color(this string str, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";

        public static string Color(this string str, RGBA color) => $"<color=#{color:X}>{str}</color>";

        public static string Color(this string str, string rrggbbaa) => $"<color=#{rrggbbaa}>{str}</color>";

        public static string color(this string s, string color) => _ = $"<color={color}>{s}</color>";
        public static string White(this string s) => _ = s.color("white");
        public static string Grey(this string s) => _ = s.color("#A0A0A0FF");
        public static string Red(this string s) => _ = s.color("#C04040E0");
        public static string Pink(this string s) => _ = s.color("#FFA0A0E0");
        public static string Green(this string s) => _ = s.color("#00ff00ff");
        public static string Blue(this string s) => _ = s.color("blue");
        public static string Cyan(this string s) => _ = s.color("cyan");
        public static string Magenta(this string s) => _ = s.color("magenta");
        public static string Yellow(this string s) => _ = s.color("yellow");
        public static string Orange(this string s) => _ = s.color("orange");



        public static string Italic(this string str) => $"<i>{str}</i>";

        public static string ToSentence(this string str) => Regex.Replace(str, @"((?<=\p{Ll})\p{Lu})|\p{Lu}(?=\p{Ll})", " $0").TrimStart();//return string.Concat(str.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).TrimStart(' ');

        public static string Size(this string str, int size) => $"<size={size}>{str}</size>";

        public static string SizePercent(this string str, int percent) => $"<size={percent}%>{str}</size>";
    }
}
