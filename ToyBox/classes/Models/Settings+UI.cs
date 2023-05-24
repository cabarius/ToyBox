using ModKit;
using static ModKit.UI
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace ToyBox {
    public partial class SettingsUI {
        public static string cultureSearchText = "";
        public static CultureInfo uiCulture;
        public static List<CultureInfo> cultures = new();
        public static void OnGUI() {
            HStack("Settings", 1,
                () => {
                    ActionButton("Reset UI", () => Main.SetNeedsResetGameUI());
                    25.space();
                    Label("Tells the game to reset the in game UI.".green() + " Warning".yellow() + " Using this in dialog or the book will dismiss that dialog which may break progress so use with care".orange());
                },
                () => {
                    Toggle("Enable Game Development Mode", ref Main.Settings.toggleDevopmentMode);
                    Space(25);
                    Label("This turns on the developer console which lets you access cheat commands, shows a FPS window (hide with F11), etc".green());
                },
                () => Label(""),
                () => EnumGrid("Log Level", ref Main.Settings.loggingLevel, AutoWidth()),
                () => Label(""),
                () => Toggle("Strip HTML (colors) from Native Console", ref Main.Settings.stripHtmlTagsFromNativeConsole),
#if DEBUG
                () => Toggle("Strip HTML (colors) from Logs Tab in Unity Mod Manager", ref Main.Settings.stripHtmlTagsFromUMMLogsTab),
#endif
                () => Toggle("Display guids in most tooltips, use shift + left click on items/abilities to copy guid to clipboard", ref Main.Settings.toggleGuidsClipboard),
              () => { }
            );
#if DEBUG
            Div(0, 25);
            HStack("Localizaton", 1,
                () => {
                    if (Event.current.type != EventType.Repaint) {
                        uiCulture = CultureInfo.GetCultureInfo(Mod.ModKitSettings.uiCultureCode);
                        if (Main.Settings.onlyShowLanguagesWithFiles) {
                            cultures = LocalizationManager.getLanguagesWithFile().Select((code, index) => CultureInfo.GetCultureInfo(code)).OrderBy(ci => ci.DisplayName).ToList();
                        }
                        else {
                            cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(ci => ci.DisplayName).ToList();
                        }
                    }
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            Label("Current Cultrue".cyan(), Width(275));
                            Space(25);
                            Label($"{uiCulture.DisplayName}({uiCulture.Name})".orange());
                            Space(25);
                            ActionButton("Export current locale to file".cyan(), () => LocalizationManager.Export());
                            Space(25);
                            Toggle("Only show languages with existing localization files", ref Main.Settings.onlyShowLanguagesWithFiles);
                            Space(25);
                            LinkButton("Open the Localization Guide", "https://github.com/cabarius/ToyBox/wiki/Localization-Guide");
                        }
                        if (GridPicker<CultureInfo>("Culture", ref uiCulture, cultures, null, ci => ci.DisplayName, ref cultureSearchText, 8, rarityButtonStyle, Width(ummWidth - 350))) {
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
