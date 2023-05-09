using ModKit;
using System.Globalization;
using System.Linq;

namespace ToyBox {
    public partial class SettingsUI {
        public static string cultureSearchText = "";
        public static void OnGUI() {
            UI.HStack("Settings", 1,
                () => {
                    UI.ActionButton("Reset UI", () => Main.SetNeedsResetGameUI());
                    25.space();
                    UI.Label("Tells the game to reset the in game UI.".green() + " Warning".yellow() + " Using this in dialog or the book will dismiss that dialog which may break progress so use with care".orange());
                },
                () => {
                    UI.Toggle("Enable Game Development Mode", ref Main.Settings.toggleDevopmentMode);
                    UI.Space(25);
                    UI.Label("This turns on the developer console which lets you access cheat commands, shows a FPS window (hide with F11), etc".green());
                },
                () => UI.Label(""),
                () => UI.EnumGrid("Log Level", ref Main.Settings.loggingLevel, UI.AutoWidth()),
                () => UI.Label(""),
                () => UI.Toggle("Strip HTML (colors) from Native Console", ref Main.Settings.stripHtmlTagsFromNativeConsole),
#if DEBUG
                () => UI.Toggle("Strip HTML (colors) from Logs Tab in Unity Mod Manager", ref Main.Settings.stripHtmlTagsFromUMMLogsTab),
#endif
                () => UI.Toggle("Display guids in most tooltips, use shift + left click on items/abilities to copy guid to clipboard", ref Main.Settings.toggleGuidsClipboard),
              () => { }
            );
#if DEBUG
            UI.Div(0, 25);
            UI.HStack("Localizaton", 1,
                () => {
                    var uiCulture = CultureInfo.GetCultureInfo(Mod.ModKitSettings.uiCultureCode);
                    var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(ci => ci.DisplayName).ToList();
                    using (UI.VerticalScope()) {
                        using (UI.HorizontalScope()) {
                            UI.Label("Current Cultrue".cyan(), UI.Width(275));
                            UI.Space(25);
                            UI.Label($"{uiCulture.DisplayName}({uiCulture.Name})".orange());
                            UI.Space(25);
                            UI.ActionButton("Export current locale to file".cyan(), () => LocalizationManager.Export());
                            UI.Space(25);
                            UI.LinkButton("Open the Localization Guide", "https://github.com/cabarius/ToyBox/wiki/Localization-Guide");
                        }
                        if (UI.GridPicker<CultureInfo>("Culture", ref uiCulture, cultures, null, ci => ci.DisplayName, ref cultureSearchText, 8, UI.rarityButtonStyle, UI.Width(UI.ummWidth - 350))) {
                            Mod.ModKitSettings.uiCultureCode = uiCulture.Name;
                            LocalizationManager.Update();
                        }
                    }
                },
                () => { }
            );
#endif
        }
    }
}
