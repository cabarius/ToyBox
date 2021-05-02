// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModKit {
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
        medgrey = 0xA8A8A8ff,
        grey = 0xC0C0C0ff,
        silver = 0xD0D0D0ff,
        lightgrey = 0xE8E8E8ff,
        white = 0xffffffff,
    }

    public static class RichText
    {
        public static string ToHtmlString(this RGBA color) {
            return $"{color:X}";
        }
        public static string size(this string s, int size)
        {
            return s = $"<size={size}>{s}</size>";
        }

        public static string mainCategory(this string s) { return s = s.size(16).bold();  }

        public static string bold(this string s)
        {
            return s = $"<b>{s}</b>";
        }

        public static string italic(this string s)
        {
            return s = $"<i>{s}</i>";
        }

        public static string color(this string s, string color)
        {
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
        public static string pink(this string s) { return s = s.color("#FFA0A0E0");  }
        public static string green(this string s) { return s = s.color("#00ff00ff"); }
        public static string blue(this string s) { return s = s.color("blue"); }
        public static string cyan(this string s) { return s = s.color("cyan"); }
        public static string magenta(this string s) { return s = s.color("magenta"); }
        public static string yellow(this string s) { return s = s.color("yellow"); }
        public static string orange(this string s) { return s = s.color("orange"); }

        public static string warningLargeRedFormat(this string s)
        {
            return s = s.red().size(16).bold();
        }

        public static string SizePercent(this string s, int percent)
        {
            return s = $"<size={percent}%>{s}</size>";
        }
    }
}

