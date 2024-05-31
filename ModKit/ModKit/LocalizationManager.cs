using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModKit {
    public class Language {
        public string LanguageCode { get; set; }

        public string Version { get; set; }

        public string Contributors { get; set; }

        public string HomePage { get; set; }

        public SortedDictionary<string, string> Strings { get; set; }

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
        private static bool isEnabled = false;
        public static string FilePath { get; private set; }

        public static void Enable() {
            try {
                isEnabled = true;
                IsDefault = true;
                _local = null;
                _localDefault = null;
                var separator = Path.DirectorySeparatorChar;
                _localFolderPath = Mod.modEntry.Path + "Localization" + separator;
                FilePath = _localFolderPath + "en";
                _localDefault = Import();
                if (_localDefault == null) {
                    _localDefault = new();
                    _localDefault.Strings = new();
                }
                var chosenLangauge = Mod.ModKitSettings.uiCultureCode;
                FilePath = _localFolderPath + chosenLangauge;
                if (chosenLangauge != "en") {
                    _local = Import();
                    IsDefault = _local == null;
                }

            } catch (Exception ex) {
                Mod.Error("Could not load localization files!");
                Mod.Warn(ex.ToString());
            }
        }
        public static void Update() {
            var locale = Mod.ModKitSettings.uiCultureCode;
            if (Mod.ModKitSettings.uiCultureCode == "en") {
                FilePath = _localFolderPath + "en";
                IsDefault = true;
                _local = null;
            } else {
                if (!(_local?.LanguageCode == locale)) {
                    FilePath = _localFolderPath + Mod.ModKitSettings.uiCultureCode;
                    _local = Import();
                    IsDefault = _local == null;
                }
            }
        }

        public static string localize(this string key) {
            if (string.IsNullOrEmpty(key) || !isEnabled) return key;
            else {
                string localizedString = null;
                if (!IsDefault) {
                    if (!(_local?.Strings.TryGetValue(key, out localizedString)) ?? true) {
                        _local?.Strings.Add(key, "");
                        Mod.Debug($"Unlocalized Key: '{key.orange().bold()}' in current locale: " + key);
                    }
                }
                if (IsDefault || localizedString == "") {
                    if (!(_localDefault?.Strings.TryGetValue(key, out localizedString)) ?? true) {
                        _localDefault?.Strings.Add(key, key);
                        Mod.Warn($"Unlocalized Key: '{key.orange().bold()}' in default locale");
                    }
                }
                return localizedString != null ? localizedString : key;
            }
        }
        public static Language Import(Action<Exception> onError = null) {
            try {
                if (File.Exists(FilePath + _fileEnding)) {
                    Language lang;
                    lang = Language.Deserialize(FilePath + _fileEnding);
                    return lang;
                } // If default is missing recreate empty default
                else if (FilePath.ToLower().EndsWith("en")) {
                    Language lang = new();
                    lang.Strings = new();
                    lang.LanguageCode = "en";
                    lang.Version = Mod.modEntry.Version.ToString();
                    lang.Contributors = "The ToyBox Team";
                    lang.HomePage = "https://github.com/cabarius/ToyBox/";
                    Language.Serialize(lang, FilePath + _fileEnding);
                }
            } catch (Exception e) {
                if (onError != null) {
                    onError(e);
                } else {
                    Mod.Error(e.ToString());
                }
            }
            return null;
        }
        private static bool _cacheFiles = false;
        private static HashSet<string> _LanguageCache = new();
        public static HashSet<string> getLanguagesWithFile() {
            if (!_cacheFiles) {
                if (Directory.Exists(_localFolderPath)) {
                    foreach (var file in Directory.GetFiles(_localFolderPath)) {
                        var parts = file.Split(Path.DirectorySeparatorChar).Last().Split('.');
                        if (parts[1] == "json") {
                            _LanguageCache.Add(parts[0]);
                        }
                    }
                }
                _cacheFiles = true;
            }
            return _LanguageCache;
        }

        public static bool Export(Action<Exception> onError = null) {
            try {
                if (!Directory.Exists(_localFolderPath)) {
                    Directory.CreateDirectory(_localFolderPath);
                }
                if (File.Exists(FilePath + _fileEnding)) {
                    File.Delete(FilePath + _fileEnding);
                } else {
                    _LanguageCache.Add(Mod.ModKitSettings.uiCultureCode);
                }
                var toSerialize = Mod.ModKitSettings.uiCultureCode == "en" ? _localDefault : _local;
                if (toSerialize == null) {
                    toSerialize = new();
                    toSerialize.Strings = new();
                    foreach (var k in _localDefault.Strings.Keys) {
                        toSerialize.Strings.Add(k, "");
                    }
                } else {
                    var notToSerialize = Mod.ModKitSettings.uiCultureCode == "en" ? _local : _localDefault;
                    if (notToSerialize != null) {
                        foreach (var k in notToSerialize.Strings.Keys) {
                            if (!toSerialize.Strings.ContainsKey(k)) {
                                toSerialize.Strings.Add(k, "");
                            }
                        }
                    }
                }
                toSerialize.LanguageCode = Mod.ModKitSettings.uiCultureCode;
                toSerialize.Version = Mod.modEntry.Version.ToString();
                if (string.IsNullOrEmpty(toSerialize.Contributors)) toSerialize.Contributors = "The ToyBox Team";
                toSerialize.HomePage = "https://github.com/cabarius/ToyBox/";
                Language.Serialize(toSerialize, FilePath + _fileEnding);
                return true;
            } catch (Exception e) {
                if (onError != null) {
                    onError(e);
                } else {
                    Mod.Error(e.ToString());
                }
            }
            return false;
        }
    }
}