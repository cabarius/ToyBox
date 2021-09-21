using System;
using System.Collections.Generic;
using System.Text;
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
        gold = 0xED9B1Aff,
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
        trash = grey,
        notable = silver,
        common = silver,
        uncommon = 0x50d30cff,
        rare = 0x0020F0ff,
        epic = 0xc860FFff,
        legendary = 0xEDCB1Aff,
        mythic = cyan,
        godly = red
    }

    public static class ColorUtils {
        public static Color Color(this RGBA rga) {
            var red = (float)((Int64)rga >> 24) / 256f;
            var green = (float)(0xFF & ((Int64)rga >> 16)) / 256f;
            var blue = (float)(0xFF & ((Int64)rga >> 8)) / 256f;
            var alpha = (float)(0xFF & ((Int64)rga)) / 256f;
            return new Color(red, green, blue, alpha);
        }
    }
}
