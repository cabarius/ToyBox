using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Reflection;
using static UnityModManagerNet.UnityModManager;
using System;

namespace ModKit {
    public interface IUpdatableSettings {
        void AddMissingKeys(IUpdatableSettings from);
    }

    static class ModSettings {
        public static void SaveSettings<T>(this ModEntry modEntry, string fileName, T settings) { 
            string userConfigFolder = modEntry.Path + "UserSettings";
            Directory.CreateDirectory(userConfigFolder);
            var userPath = $"{userConfigFolder}{Path.DirectorySeparatorChar}{fileName}";
            File.WriteAllText(userPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
        public static void LoadSettings<T>(this ModEntry modEntry, string fileName, ref T settings) where T : IUpdatableSettings, new() {
            var assembly = Assembly.GetExecutingAssembly();
            string userConfigFolder = modEntry.Path + "UserSettings";
            Directory.CreateDirectory(userConfigFolder);
            var userPath = $"{userConfigFolder}{Path.DirectorySeparatorChar}{fileName}";
            try {
                foreach (var res in assembly.GetManifestResourceNames()) {
                    //Logger.Log("found resource: " + res);
                    if (res.Contains(fileName)) {
                        var stream = assembly.GetManifestResourceStream(res);
                        using (StreamReader reader = new StreamReader(stream)) {
                            var text = reader.ReadToEnd();
                            //Logger.Log($"read: {text}");
                            settings = JsonConvert.DeserializeObject<T>(text);
                            //Logger.Log($"read settings: {string.Join(Environment.NewLine, settings)}");
                        }
                    }
                }
            }
            catch (Exception e) {
                Logger.Log($"{fileName} resource is not present or is malformed. exception: {e}");
                settings = new T { };
            }
            if (File.Exists(userPath)) {
                using (StreamReader reader = File.OpenText(userPath)) {
                    try {
                        T userSettings = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                        userSettings.AddMissingKeys(settings);
                        settings = userSettings;
                    }
                    catch {
                        Logger.Log("Failed to load user settings. Settings will be rebuilt.");
                        try { File.Copy(userPath, userConfigFolder + $"{Path.DirectorySeparatorChar}BROKEN_{fileName}", true); }
                        catch { Logger.Log("Failed to archive broken settings."); }
                    }
                }
            }
            File.WriteAllText(userPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}