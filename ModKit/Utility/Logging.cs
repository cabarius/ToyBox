// some stuff borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using ModKit;

namespace ModKit {
    public class Logger {
        public static UnityModManager.ModEntry.ModLogger modLogger;
        public static string modEntryPath = null;

        public static readonly string logFile = "ModKit";

        private String path;
        private bool removeHtmlTags = true;
        public bool RemoveHtmlTags { get => removeHtmlTags; set => removeHtmlTags = value; }
        private bool useTimeStamp = true;
        public bool UseTimeStamp { get => useTimeStamp; set => useTimeStamp = value; }

        public Logger() : this(logFile) {

        }

        public Logger(String fileName, String fileExtension = ".log") {
            path = Path.Combine(modEntryPath, (fileName + fileExtension));
            Clear();
        }

        public static void Log(string str) {
            Logger.modLogger.Log(str);
        }

        public static void Log(Exception ex) {
            Logger.modLogger.Log(ex.ToString().red().bold() + "\n" + ex.StackTrace);
        }

        public static void Error(Exception ex) {
            Logger.modLogger.Log(ex.ToString() + "\n" + ex.StackTrace);
        }

        public void LogToFiles(string str) {
            if (removeHtmlTags) {
                str = Utilties.RemoveHtmlTags(str);
            }
            if (UseTimeStamp) {
                ToFile(TimeStamp() + " " + str);
            }
            else {
                ToFile(str);

            }
        }

        private static string TimeStamp() {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss.ff") + "]";
        }

        private void ToFile(string s) {
            try {
                using (StreamWriter stream = File.AppendText(path)) {
                    stream.WriteLine(s);
                }
            }
            catch (Exception e) {
                modLogger.Log(e.ToString());
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
                    modLogger.Log(e.ToString());
                }
            }
        }
        public static void ModLog(string message) {
            Logger.modLogger.Log(message);
        }
        public static void ModLoggerDebug(string message) {
            //if (Main.settings.settingShowDebugInfo) {
                Logger.modLogger.Log(message);
            //}
        }
        public static void ModLoggerDebug(int message) {
            //if (Main.settings.settingShowDebugInfo) {
                Logger.modLogger.Log(message.ToString());
            //}
        }
        public static void ModLoggerDebug(bool message) {
            //if (Main.settings.settingShowDebugInfo) {
                Logger.modLogger.Log(message.ToString());
            //}
        }
    }

    public class HtmlLogger : Logger {

        public HtmlLogger() : this(Logger.logFile) {
        }

        public HtmlLogger(String fileName) : base(fileName, ".html") {
            this.RemoveHtmlTags = false;
            this.UseTimeStamp = false;
        }

        public new void Log(string str) {
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

}
