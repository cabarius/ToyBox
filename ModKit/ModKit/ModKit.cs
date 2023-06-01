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
        private static ModEntry.ModLogger modLogger;
        public static LogLevel logLevel = LogLevel.Info;
        public delegate void UITranscriptLogger(string text);
        public static UITranscriptLogger InGameTranscriptLogger;

        public static void OnLoad(ModEntry modEntry) {
            modEntry.OnSaveGUI -= OnSaveGUI;
            modEntry.OnSaveGUI += OnSaveGUI;
            Mod.modEntry = modEntry;
            modLogger = modEntry.Logger;
            modEntryPath = modEntry.Path;
            ModKitSettings.Load();
            Debug($"ModKitSettings.browserSearchLimit: {ModKitSettings.browserDetailSearchLimit}");
        }
        public static void OnSaveGUI(ModEntry entry) {
            ModKitSettings.Save();
            LocalizationManager.Export();
        }
        private static void ResetGUI(ModEntry modEntry) => ModKitSettings.Load();
        public static void Error(string? str) {
            str = str.yellow().bold();
            modLogger?.Error(str + "\n" + Environment.StackTrace);
        }
        public static void Error(Exception ex) => Error(ex.ToString());
        public static void Warn(string str) {
            if (logLevel >= LogLevel.Warning)
                modLogger?.Log("[Warn] ".orange().bold() + str);
        }
        public static void Log(string? str) {
            if (logLevel >= LogLevel.Info)
                modLogger?.Log("[Info] " + str);
        }
        public static void Log(int indent, string s) => Log("    ".Repeat(indent) + s);
        public static void Debug(string? str) {
            if (logLevel >= LogLevel.Debug)
                modLogger?.Log("[Debug] ".green() + str);
        }
        public static void Trace(string? str) {
            if (logLevel >= LogLevel.Trace)
                modLogger?.Log("[Trace] ".color(RGBA.lightblue) + str);
        }
    }
}