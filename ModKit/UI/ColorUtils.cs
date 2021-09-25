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
        darkred = 0xa0333bff,
        fuchsia = 0xff40ffff,
        green = 0x40C040ff,
        gold = 0xED9B1Aff,
        lightblue = 0xd8e6ff,
        lightgrey = 0xE8E8E8ff,
        lime = 0x40ff40ff,
        magenta = 0xff40ffff,
        maroon = 0xFF6060ff,
        medred = 0xd03333ff,
        navy = 0x000080ff,
        olive = 0xb0b000ff,
        orange = 0xffa500ff,    // 0xffa500ff,
        pink = 0xf03399ff,
        purple = 0xC060F0ff,
        red = 0xFF4040ff,
        teal = 0x80f0c0ff,
        yellow = 0xffff00ff,
        black = 0x000000ff,
        darkgrey = 0x808080ff,
        medgrey = 0xA8A8A8ff,
        grey = 0xC0C0C0ff,
        silver = 0xD0D0D0ff,
        white = 0xffffffff,
        none = silver,
        trash = brown, // 0x606060FF,
        notable = yellow, // 0x40ff40c0, // 0xf03399ff, // 0xff3399ff,
        common = silver,         //0xe8e8e8a0,
        uncommon = 0x00882bff,  //0x00802bff, //0x68b020ff, // 0x60B020ff,
        rare = 0x2060ffff,
        epic = 0xc260f1ff,      //0xc860fff,
        legendary = 0xe67821ff, // 0xe68019ff // 0xEDCB1Aff,
        mythic = 0x60ffffff,
        godly =  pink           //red
    }

    public static class ColorUtils {
        public static Color color(this RGBA rga, float adjust = 0) {
            var red = (float)((Int64)rga >> 24) / 256f;
            var green = (float)(0xFF & ((Int64)rga >> 16)) / 256f;
            var blue = (float)(0xFF & ((Int64)rga >> 8)) / 256f;
            var alpha = (float)(0xFF & ((Int64)rga)) / 256f;
            var color = new Color(red, green, blue, alpha);
            if (adjust < 0) color = Color.Lerp(color, Color.black, -adjust);
            if (adjust > 0) color = Color.Lerp(color, Color.white, adjust);
            return color;
        }
    }
}
