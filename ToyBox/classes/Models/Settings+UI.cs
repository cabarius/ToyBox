using ModKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ToyBox {
    public partial class SettingsUI {
        public static string cultureSearchText = "";
        public static CultureInfo? uiCulture;
        public static List<CultureInfo> cultures = new();
        public static void OnGUI() {
            HStack("Settings".localize(), 1,
                () => Label("Mono Version".localize() + $": {Type.GetType("Mono.Runtime")?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null)?.ToString()}"),
                () => {
                    ActionButton("Reset UI".localize(), Main.SetNeedsResetGameUI);
                    25.space();
                    Label(("Tells the game to reset the in game UI.".green() + " Warning".yellow() + " Using this in dialog or the book will dismiss that dialog which may break progress so use with care".orange()).localize());
                },
                () => {
                    Toggle("Enable Game Development Mode".localize(), ref Main.Settings.toggleDevopmentMode);
                    Space(25);
                    HelpLabel($"This turns on the developer console which lets you access cheat commands, shows a FPS window (hide with F11), etc.\n{"Warning: ".yellow().bold()}{"You may need to restart the game for this to fully take effect".orange()}".localize());
                },
                () => Label(""),
                () => EnumGrid("Log Level".localize(), ref Main.Settings.loggingLevel, AutoWidth()),
                () => Label(""),
                () => Toggle("Strip HTML (colors) from Native Console".localize(), ref Main.Settings.stripHtmlTagsFromNativeConsole),
                () => Toggle("Enable Search as you type for Browsers (needs restart)".localize(), ref Mod.ModKitSettings.searchAsYouType),
#if DEBUG
                () => Toggle("Strip HTML (colors) from Logs Tab in Unity Mod Manager".localize(), ref Main.Settings.stripHtmlTagsFromUMMLogsTab),
#endif
                () => Toggle("Display guids in most tooltips, use shift + left click on items/abilities to copy guid to clipboard".localize(), ref Main.Settings.toggleGuidsClipboard),
              () => { }
            );
#if true
            Div(0, 25);
            HStack("Localization".localize(), 1,
                () => {
                    if (Event.current.type != EventType.Repaint) {
                        uiCulture = CultureInfo.GetCultureInfo(Mod.ModKitSettings.uiCultureCode);
                        cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).OrderBy(ci => ci.DisplayName).ToList();
                        if (Main.Settings.onlyShowLanguagesWithFiles) {
                            var languages = LocalizationManager.getLanguagesWithFile().ToHashSet();
                            cultures = cultures
                                       .Where(ci => languages.Contains(ci.Name))
                                       .OrderBy(ci => ci.DisplayName).
                                       ToList();
                        }
                    }
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            Label("Current Culture".localize().cyan(), Width(275));
                            Space(25);
                            Label($"{uiCulture.DisplayName}({uiCulture.Name})".orange());
                            Space(25);
                            ActionButton("Export current locale to file".localize().cyan(), () => LocalizationManager.Export());
                            Space(25);
                            LinkButton("Open the Localization Guide".localize(), "https://github.com/cabarius/ToyBox/wiki/Localization-Guide");
                        }
                        15.space();
                        using (HorizontalScope()) {
                            Toggle("Only show languages with existing localization files".localize(), ref Main.Settings.onlyShowLanguagesWithFiles);
                        }
                        Div(0, 25);
                        if (GridPicker<CultureInfo>("Culture", ref uiCulture, cultures, null, ci => $"{ci.Name.cyan().bold()} {ci.DisplayName.orange()}", ref cultureSearchText, 6, rarityButtonStyle, Width(ummWidth - 350))) {
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
