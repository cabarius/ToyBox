using ModKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ToyBox {
    public partial class SettingsUI {
        public static CultureInfo uiCulture;
        public static string cultureSearchText = "";
        public static void OnGUI() {
            Mod.logLevel = Main.settings.loggingLevel;
            UI.HStack("Settings", 1,
                () => {
                    UI.Toggle("Enable Game Development Mode", ref Main.settings.toggleDevopmentMode);
                    UI.Space(25);
                    UI.Label("This turns on the developer console which lets you access cheat commands, shows a FPS window (hife with F11), etc".green());
                },
                () => UI.Label(""),
                () => UI.EnumGrid("Log Level", ref Main.settings.loggingLevel, UI.AutoWidth()),
                () => UI.Label(""),
                () => UI.Toggle("Strip HTML (colors) from Native Console", ref Main.settings.stripHtmlTagsFromNativeConsole),
#if DEBUG
                () => UI.Toggle("Strip HTML (colors) from Logs Tab in Unity Mod Manager", ref Main.settings.stripHtmlTagsFromUMMLogsTab),
#endif
              () => { }
            );
            UI.HStack("Localizaton", 1,
                () => {
                    var cultureInfo = Thread.CurrentThread.CurrentUICulture;
                    var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures).OrderBy(ci => ci.DisplayName).ToList();
                    UI.GridPicker<CultureInfo>("Culture", ref uiCulture, cultures, null, ci => ci.DisplayName, ref cultureSearchText, 4);
                },
                () => { }
            );
        }
    }
}
