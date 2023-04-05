// some stuff borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using ModKit.Utility;
using System;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace ModKit {
    public enum LogLevel : int {
        Error,
        Warning,
        Info,
        Debug,
        Trace
    }
    public static partial class Mod {
        public static ModEntry modEntry { get; set; } = null;
        public static string modEntryPath { get; set; } = null;
        private static UnityModManager.ModEntry.ModLogger modLogger;

        public static LogLevel logLevel = LogLevel.Info;


        public static void OnLoad(UnityModManager.ModEntry modEntry) {
            Mod.modEntry = modEntry;
            modLogger = modEntry.Logger;
            modEntryPath = modEntry.Path;
        }
        public static void Error(string str) {
            str = str.yellow().bold();
            modLogger?.Error(str + "\n" + Environment.StackTrace);
        }
        public static void Error(Exception ex) => Error(ex.ToString());
        public static void Warn(string str) {
            if (logLevel >= LogLevel.Warning)
                modLogger?.Log("[Warn] ".orange().bold() + str);
        }
        public static void Log(string str) {
            if (logLevel >= LogLevel.Info)
                modLogger?.Log("[Info] " + str);
        }
        public static void Log(int indent, string s) { Log("    ".Repeat(indent) + s); }
        public static void Debug(string str) {
            if (logLevel >= LogLevel.Debug)
                modLogger?.Log("[Debug] ".green() + str);
        }
        public static void Trace(string str) {
            if (logLevel >= LogLevel.Trace)
                modLogger?.Log("[Trace] ".color(RGBA.lightblue) + str);
        }
    }
#if false

    public class Logger {

        public static readonly string logFile = "ModKit";

        private String path;
        public bool RemoveHtmlTags { get; set; }
        private bool useTimeStamp = true;
        public bool UseTimeStamp { get => useTimeStamp; set => useTimeStamp = value; }

        public Logger() : this(logFile) {

        }

        public Logger(String fileName, String fileExtension = ".log") {
            path = Path.Combine(Mod.modEntryPath, (fileName + fileExtension));
            Clear();
        }


        public void LogToFiles(string str) {
            if (RemoveHtmlTags) {
                str = Utilties.StripHTML(str);
            }
            if (UseTimeStamp) {
                ToFile(TimeStamp() + " " + str);
            }
            else {
                ToFile(str);

            }
        }

        private static string TimeStamp() {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ff".blue()) + "]";
        }

        private void ToFile(string s) {
            try {
                using (StreamWriter stream = File.AppendText(path)) {
                    stream.WriteLine(s);
                }
            }
            catch (Exception e) {
                Mod.Error(e);
            }
        }

        public void Clear() {
            if (File.Exists(path)) {
                try {
                    File.Delete(path);
                    using (File.Create(path)) {
                    }
                }
                catch (Exception e) {
                    Mod.Error(e);
                }
            }
        }
        public static void ModLoggerDebug(string message) {
            //if (Main.settings.settingShowDebugInfo) {
            Mod.Debug(message);
            //}
        }
        public static void ModLoggerDebug(int message) {
            //if (Main.settings.settingShowDebugInfo) {
            Mod.Debug(message.ToString());
            //}
        }
        public static void ModLoggerDebug(bool message) {
            Mod.Debug(message.ToString());
        }
    }

    public class HtmlLogger : Logger {

        public HtmlLogger() : this(Logger.logFile) {
        }

        public HtmlLogger(String fileName) : base(fileName, ".html") {
            this.RemoveHtmlTags = false;
            this.UseTimeStamp = false;
        }

        public void Log(string str) {
            str = Utilties.UnityRichTextToHtml(str);
            base.LogToFiles(str);
        }

        public static string[] getObjectInfo(object o) {

            string fields = "";
            foreach (string field in Traverse.Create(o).Fields()) {
                fields = fields + field + ", ";
            }
            string methods = "";
            foreach (string method in Traverse.Create(o).Methods()) {
                methods = methods + method + ", ";
            }
            string properties = "";
            foreach (string property in Traverse.Create(o).Properties()) {
                properties = properties + property + ", ";
            }
            return new string[] { fields, methods, properties };
        }
    }
#endif
}
