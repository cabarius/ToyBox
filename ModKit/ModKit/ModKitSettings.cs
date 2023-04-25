using System.IO;
using UnityModManagerNet;
using static ModKit.UI;
using static UnityModManagerNet.UnityModManager;

namespace ModKit {
    public partial class Mod {
        public static ModKitSettings ModKitSettings;
    }
    public class ModKitSettings {
        public static void Save() => Mod.modEntry.SaveSettings("ModKitSettings.json", Mod.ModKitSettings);
        public static void Load() => Mod.modEntry.LoadSettings("ModKitSettings.json", ref Mod.ModKitSettings);

        public int browserSearchLimit = 20;
        public int browserDetailSearchLimit = 10;
    }
}
