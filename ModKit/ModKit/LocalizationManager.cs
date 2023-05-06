using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ModKit {
    public class Language {
        public string LanguageCode { get; set; }

        public string Version { get; set; }

        public string Contributors { get; set; }

        public string HomePage { get; set; }

        public Dictionary<string, string> Strings { get; set; }

        public static Language Deserialize(string pathToFile) {
            Language loadedLanguage;
            using (StreamReader file = File.OpenText(pathToFile)) {
                loadedLanguage = JsonConvert.DeserializeObject<Language>(file.ReadToEnd());
            }
            return loadedLanguage;
        }

        public static void Serialize(Language lang, string pathToFile) {
            File.WriteAllText(pathToFile, JsonConvert.SerializeObject(lang, Formatting.Indented));
        }
    }

    public static class LocalizationManager {
        private static string _localFolderPath;
        private static string _fileEnding = ".json";
        private static Language _localDefault;
        private static Language _local;
        private static bool IsDefault;
        private static bool buildLocale;
        public static string FilePath { get; private set; }

        public static void Enable(bool buildLocale = false) {
            IsDefault = true;
            _local = null;
            _localDefault = null;
            var separator = Path.DirectorySeparatorChar;
            _localFolderPath = Mod.modEntry.Path + "Localization" + separator;
            FilePath = _localFolderPath + "en";
            LocalizationManager.buildLocale = buildLocale;
            if (buildLocale) {
                IsDefault = true;
                _localDefault = new Language {
                    LanguageCode = "en",
                    Version = "1.0.0",
                    Contributors = "ToyBox Team",
                    HomePage = "https://github.com/cabarius/ToyBox/",
                    Strings = new()
                };
            }
            else {
                _localDefault = Import();
                var chosenLangauge = Mod.ModKitSettings.uiCultureCode;
                FilePath = _localFolderPath + chosenLangauge;
                if (chosenLangauge != "en") {
                    _local = Import();
                    IsDefault = _local != null;
                }
            }
        }
        public static void Update() {
            var locale = Mod.ModKitSettings.uiCultureCode;
            if (Mod.ModKitSettings.uiCultureCode == "en") {
                FilePath = _localFolderPath + "en";
                IsDefault = true;
                _local = null;
            }
            else {
                if (!(_local?.LanguageCode == locale)) {
                    FilePath = _localFolderPath + Mod.ModKitSettings.uiCultureCode;
                    _local = Import();
                    IsDefault = _local == null;
                }
            }
        }

        public static string localize(this string key) {
            if (key == null || key == "") return key;
            if (buildLocale) {
                if (!_localDefault.Strings.ContainsKey(key))
                    _localDefault.Strings.Add(key, key);
                return key;
            }
            else {
                string localizedString = "";
                if (!IsDefault) {
                    _local?.Strings.TryGetValue(key, out localizedString);
                    Mod.Debug("Unknown Key in current locale: key");
                }
                if (IsDefault || localizedString == "") {
                    _localDefault?.Strings.TryGetValue(key, out localizedString);
                    if (localizedString == "") {
                        Mod.Debug("Unknown Key in default and current locale: key");
                        return key;
                    }
                }
                return localizedString;
            }
        }
        public static Language Import(Action<Exception> onError = null) {
            try {
                if (File.Exists(FilePath + _fileEnding)) {
                    Language lang;
                    lang = Language.Deserialize(FilePath + _fileEnding);
                    return lang;
                }
            }
            catch (Exception e) {
                if (onError != null) {
                    onError(e);
                }
                else {
                    Mod.Error(e.ToString());
                }
            }
            return null;
        }

        public static bool Export(Action<Exception> onError = null) {
            try {
                if (!Directory.Exists(_localFolderPath)) {
                    Directory.CreateDirectory(_localFolderPath);
                }
                if (File.Exists(FilePath + _fileEnding)) {
                    File.Delete(FilePath + _fileEnding);
                }
                Language.Serialize(IsDefault ? _localDefault : _local, FilePath + _fileEnding);
                return true;
            }
            catch (Exception e) {
                if (onError != null) {
                    onError(e);
                }
                else {
                    Mod.Error(e.ToString());
                }
            }
            return false;
        }
    }
}