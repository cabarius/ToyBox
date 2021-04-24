using System.Text.RegularExpressions;
using UnityEngine;

namespace ModMaker.Utility
{
    public static class RichTextExtensions
    {
        // https://docs.unity3d.com/Manual/StyledText.html

        public enum RGBA : uint
        {
            aqua = 0x00ffffff,
            black = 0x000000ff,
            blue = 0x8080ffff,
            brown = 0xC09050ff, //0xa52a2aff,
            cyan = 0x00ffffff,
            darkblue = 0x0000a0ff,
            fuchsia = 0xff40ffff,
            green = 0x40C040ff,
            grey = 0xC0C0C0ff,
            lightblue = 0xd8e6ff,
            lime = 0x40ff40ff,
            magenta = 0xff40ffff,
            maroon = 0xFF6060ff,
            navy = 0x000080ff,
            olive = 0xB0B000ff,
            orange = 0xffa500ff, // 0xffa500ff,
            purple = 0xC060F0ff,
            red = 0xFF4040ff,
            silver = 0xc0c0c0ff,
            teal = 0x80f0c0ff,
            white = 0xffffffff,
            yellow = 0xffff00ff
        }

        public static string ToHtmlString(this RGBA color) {
            return $"{color:X}";
        }

        public static string Bold(this string str)
        {
            return $"<b>{str}</b>";
        }

        public static string Color(this string str, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{str}</color>";
        }

        public static string Color(this string str, RGBA color)
        {
            return $"<color=#{color:X}>{str}</color>";
        }

        public static string Color(this string str, string rrggbbaa)
        {
            return $"<color=#{rrggbbaa}>{str}</color>";
        }

        public static string color(this string s, string color) {
            return s = $"<color={color}>{s}</color>";
        }
        public static string White(this string s) { return s = s.color("white"); }
        public static string Grey(this string s) { return s = s.color("#A0A0A0FF"); }
        public static string Red(this string s) { return s = s.color("#C04040E0"); }
        public static string Pink(this string s) { return s = s.color("#FFA0A0E0"); }
        public static string Green(this string s) { return s = s.color("#00ff00ff"); }
        public static string Blue(this string s) { return s = s.color("blue"); }
        public static string Cyan(this string s) { return s = s.color("cyan"); }
        public static string Magenta(this string s) { return s = s.color("magenta"); }
        public static string Yellow(this string s) { return s = s.color("yellow"); }
        public static string Orange(this string s) { return s = s.color("orange"); }



        public static string Italic(this string str)
        {
            return $"<i>{str}</i>";
        }

        public static string ToSentence(this string str)
        {
            return Regex.Replace(str, @"((?<=\p{Ll})\p{Lu})|\p{Lu}(?=\p{Ll})", " $0").TrimStart();
            //return string.Concat(str.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).TrimStart(' ');
        }

        public static string Size(this string str, int size)
        {
            return $"<size={size}>{str}</size>";
        }

        public static string SizePercent(this string str, int percent)
        {
            return $"<size={percent}%>{str}</size>";
        }
    }
}
