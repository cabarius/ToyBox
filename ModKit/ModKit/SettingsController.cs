using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using static UnityModManagerNet.UnityModManager;

namespace ModKit {
    public interface IUpdatableSettings {
        void AddMissingKeys(IUpdatableSettings from);
    }

    internal static class SettingsController {
        public static void SaveSettings<T>(this ModEntry modEntry, string fileName, T settings) {
            var userConfigFolder = modEntry.Path + "UserSettings";
            Directory.CreateDirectory(userConfigFolder);
            var userPath = $"{userConfigFolder}{Path.DirectorySeparatorChar}{fileName}";
            File.WriteAllText(userPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
        public static void LoadSettings<T>(this ModEntry modEntry, string fileName, ref T settings) where T : new() {
            settings = new T { };
            var assembly = Assembly.GetExecutingAssembly();
            var userConfigFolder = modEntry.Path + "UserSettings";
            Directory.CreateDirectory(userConfigFolder);
            var userPath = $"{userConfigFolder}{Path.DirectorySeparatorChar}{fileName}";
            try {
                foreach (var res in assembly.GetManifestResourceNames())
                    //Logger.Log("found resource: " + res);
                    if (res.Contains(fileName)) {
                        var stream = assembly.GetManifestResourceStream(res);
                        using StreamReader reader = new(stream);
                        var text = reader.ReadToEnd();
                        //Logger.Log($"read: {text}");
                        settings = JsonConvert.DeserializeObject<T>(text);
                        //Logger.Log($"read settings: {string.Join(Environment.NewLine, settings)}");
                    }
            } catch (Exception e) {
                Mod.Error($"{fileName} resource is not present or is malformed. exception: {e}");
            }
            if (File.Exists(userPath)) {
                using var reader = File.OpenText(userPath);
                try {
                    var userSettings = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                    if (userSettings is IUpdatableSettings updatableSettings) updatableSettings?.AddMissingKeys((IUpdatableSettings)settings);
                    settings = userSettings;
                } catch {
                    Mod.Error("Failed to load user settings. Settings will be rebuilt.");
                    try {
                        File.Copy(userPath, userConfigFolder + $"{Path.DirectorySeparatorChar}BROKEN_{fileName}", true);
                    } catch {
                        Mod.Error("Failed to archive broken settings.");
                    }
                }
            }
            settings ??= new();
            File.WriteAllText(userPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}