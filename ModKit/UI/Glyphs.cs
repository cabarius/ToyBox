using UnityEngine;

using System.Runtime.InteropServices;

namespace ModKit {
    public static partial class Glyphs {
        private static Boolean IsDefaultGlyphs = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static string CheckOn() => this.IsDefaultGlyphs ? this.DefaultCheckOn : this.CharCodeCheckOn;
        public static string CheckOff() => this.IsDefaultGlyphs ? this.DefaultCheckOff : this.CharCodeCheckOff;
        public static string CheckEmpty() => this.IsDefaultGlyphs ? this.DefaultCheckEmpty : this.CharCodeCheckEmpty;
        public static string DisclosureOn() => this.IsDefaultGlyphs ? this.DefaultDisclosureOn : this.CharCodeDisclosureOn;
        public static string DisclosureOff() => this.IsDefaultGlyphs ? this.DefaultDisclosureOff : this.CharCodeDisclosureOff;
        public static string DisclosureEmpty() => this.IsDefaultGlyphs ? this.DefaultDisclosureEmpty : this.CharCodeDisclosureEmpty;
        public static string Edit() => this.IsDefaultGlyphs ? this.DefaultEdit : this.Edit;
    }
}
