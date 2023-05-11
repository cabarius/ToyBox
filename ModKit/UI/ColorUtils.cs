using UnityEngine;

namespace ModKit {
    // https://docs.unity3d.com/Manual/StyledText.html
    public enum RGBA : uint {
        aqua = 0x00ffffff,
        blue = 0x8080ffff,
        brown = 0xC09050ff, //0xa52a2aff,
        crimson = 0x7b0340ff,
        cyan = 0x00ffffff,
        darkblue = 0x0000a0ff,
        charcoal = 0x202020ff,
        darkgrey = 0x808080ff,
        darkred = 0xa0333bff,
        fuchsia = 0xff40ffff,
        green = 0x40C040ff,
        gold = 0xED9B1Aff,
        lightblue = 0xd8e6ffff,
        lightgrey = 0xE8E8E8ff,
        lime = 0x40ff40ff,
        magenta = 0xff40ffff,
        maroon = 0xFF6060ff,
        medred = 0xd03333ff,
        navy = 0x3b5681ff,
        olive = 0xb0b000ff,
        orange = 0xffa500ff,    // 0xffa500ff,
        darkorange = 0xb1521fff,
        pink = 0xf03399ff,
        purple = 0xC060F0ff,
        red = 0xFF4040ff,
        black = 0x000000ff,
        medgrey = 0xA8A8A8ff,
        grey = 0xC0C0C0ff,
        silver = 0xD0D0D0ff,
        teal = 0x80f0c0ff,
        yellow = 0xffff00ff,
        white = 0xffffffff,
        none = silver,
        trash = 0x808080ff,     // 0x686868ff,  0x787878ff, // 0x734d26ff, // 0x86592dff, //0xA07040ff, // brown, 0x606060FF,
        common = 0xd8d8d8a0, // 0x505050ff,    // 0xd8d8d8a0,         //0xe8e8e8a0,
        uncommon = 0x086c26ff, // 0x00882bff,  //0x00802bff, //0x68b020ff, // 0x60B020ff,
        rare = 0x103080ff, // 0x2060ffff,
        epic = 0x481ca0ff, //0x5020c0ff, 0x6030F0ff, 0xc260f1ff, // 0x79297bff, //0x9f608cff, // 0x885278ff, // 0xc260f1ff,      //0xc860fff,
        legendary = 0xb2593aff, // 0xad5537ff, 0xb8603dff, 0xaa623dff, 0x9a5a3dff,0x9a4a2dff, (1.4.23 and older) 0xe67821e0, // 0x9a4a2dff, // 0x984c31ff, //0xe67821ff, //* 0xe67821e0,  // 0xe67821ff, // 0xe68019ff // 0xEDCB1Aff,
        mythic = 0xb02369ff, //0xc02369ff, 0xf03399ff, 0xc260f1ff, 0x8080ffff, //0x60ffffff, // 0x84e2d4ff, // 0x2cd8d4ff, // * 0x60ffffff,
        primal = 0x2cb8b4ff, //pink, red
        godly = 0x990033ff, // darkred,
        notable = 0x98761fff, //0xb1821fff, 0xffe000ff, // 0xC08020ff //0xffd840ff, // 0x40ff40c0, // 0xf03399ff, // 0xff3399ff,
        uncommon_dark = 0x00a000ff, // 0x00882bff,  //0x00802bff, //0x68b020ff, // 0x60B020ff,
        rare_dark = 0x1030e0ff, // 0x2060ffff,
        epic_dark = 0x6030F0ff, //0x5020c0ff, 0x6030F0ff, 0xc260f1ff, // 0x79297bff, //0x9f608cff, // 0x885278ff, // 0xc260f1ff,      //0xc860fff,
        legendary_dark = 0xe67821e0, // 0xad5537ff, 0xb8603dff, 0xaa623dff, 0x9a5a3dff,0x9a4a2dff, (1.4.23 and older) 0xe67821e0, // 0x9a4a2dff, // 0x984c31ff, //0xe67821ff, //* 0xe67821e0,  // 0xe67821ff, // 0xe68019ff // 0xEDCB1Aff,
        mythic_dark = 0xA000A0ff, //0xc02369ff, 0xf03399ff, 0xc260f1ff, 0x8080ffff, //0x60ffffff, // 0x84e2d4ff, // 0x2cd8d4ff, // * 0x60ffffff,
        primal_dark = 0x60ffffff, //pink, red
        godly_dark = 0xa00000ff, // darkred,
        notable_dark = 0x98761fff, //0xb1821fff, 0xffe000ff, // 0xC08020ff //0xffd840ff, // 0x40ff40c0, // 0xf03399ff, // 0xff3399ff,
    }
    public static class ColorUtils {
        public static Color color(this RGBA rga, float adjust = 0) {
            var red = (float)((long)rga >> 24) / 256f;
            var green = (float)(0xFF & ((long)rga >> 16)) / 256f;
            var blue = (float)(0xFF & ((long)rga >> 8)) / 256f;
            var alpha = (float)(0xFF & ((long)rga)) / 256f;
            var color = new Color(red, green, blue, alpha);
            if (adjust < 0)
                color = Color.Lerp(color, Color.black, -adjust);
            if (adjust > 0)
                color = Color.Lerp(color, Color.white, adjust);
            return color;
        }
    }
}
