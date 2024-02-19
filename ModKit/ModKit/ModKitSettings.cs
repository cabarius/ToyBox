namespace ModKit {
    public partial class Mod {
        public static ModKitSettings ModKitSettings;
    }

    public class ModKitSettings {
        public static void Save() => Mod.modEntry.SaveSettings("ModKitSettings.json", Mod.ModKitSettings);
        public static void Load() => Mod.modEntry.LoadSettings("ModKitSettings.json", ref Mod.ModKitSettings);

        public int browserSearchLimit = 20;
        public int browserDetailSearchLimit = 10;
        public bool searchAsYouType = true;
        public bool toggleKeyBindingsOutputToTranscript = true;
        public bool toggleDataViewerShowNullAndEmpties = false;
        public bool UseDefaultGlyphs = true;
        public bool CheckForGlyphSupport = true;

        // Localization
        public string uiCultureCode = "en";
    }
}