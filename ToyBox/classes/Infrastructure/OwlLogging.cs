using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using ModKit;
using ModKit.Utility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox; 
public static class OwlLogging {
    private static FieldInfo[] generalFields = null;
    private static FieldInfo[] perSaveFields = null;
    private static Dictionary<FieldInfo, object> generalSettings;
    private static Dictionary<FieldInfo, object> perSaveSettings;
    public static void PopulateGeneralSettings() {
        generalSettings = new();
        if (Main.Settings != null) {
            if (generalFields == null) generalFields = typeof(Settings).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in generalFields) {
                generalSettings[field] = field.GetValue(Main.Settings);
            }
        }
    }
    public static void PopulatePerSaveSettings() {
        perSaveSettings = new();
        Settings.ClearCachedPerSave();
        if (Main.Settings.perSave != null) {
            if (perSaveFields == null) perSaveFields = typeof(PerSaveSettings).GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in perSaveFields) {
                perSaveSettings[field] = field.GetValue(Main.Settings.perSave);
            }
        }
    }
    public static void OnChange() {
        try {
            var info = SaveInfo.Instance;
            var settings = new Settings();
            var perSave = new PerSaveSettings();
            if (info == null) {
                info = new();
                SaveInfo.Instance = info;
            }
            PopulateGeneralSettings();
            foreach (var pair in generalSettings) {
                var defaultVal = pair.Key.GetValue(settings);
                if (info.ChangedSettings.TryGetValue(pair.Key.Name, out var val)) {
                    bool isSimple = TypeManager.IsSimpleType(pair.Key.FieldType);
                    bool areEqual = false;
                    if (isSimple) {
                        var method = AccessTools.Method(pair.Key.FieldType, "TryParse", [typeof(string), pair.Key.FieldType.MakeByRefType()]);
                        object[] parameters = [val, Activator.CreateInstance(pair.Key.FieldType)];
                        bool success = (bool)(method?.Invoke(null, parameters) ?? false);
                        if (success) {
                            areEqual = TypeManager.AreObjectsEqual(parameters[1], pair.Value);
                        }
                    }
                    if (!areEqual) {
                        if (TypeManager.AreObjectsEqual(defaultVal, pair.Value)) {
                            info.ChangedSettings.Remove(pair.Key.Name);
                            string toLog = "Mod Setting set to default: " + pair.Key.Name;
                            Log(toLog);
                        } else {
                            if (isSimple) {
                                if (isSimple) {
                                    string toLog = "Mod Setting changed: " + pair.Key.Name;
                                    toLog += $"; prev value: {val}; new value: {pair.Value}";
                                    Log(toLog);
                                }
                                info.ChangedSettings[pair.Key.Name] = pair.Value.ToString();
                            } else {
                                info.ChangedSettings[pair.Key.Name] = "!!Non-Primitive Type!!";
                            }
                        }
                    }
                } else {
                    if (!TypeManager.AreObjectsEqual(defaultVal, pair.Value)) {
                        string toLog = "Mod Setting changed: " + pair.Key.Name;
                        if (TypeManager.IsSimpleType(pair.Key.FieldType)) {
                            toLog += $"; prev value: {defaultVal}; new value: {pair.Value}";
                        }
                        Log(toLog);
                        if (TypeManager.IsSimpleType(pair.Key.FieldType)) {
                            info.ChangedSettings[pair.Key.Name] = pair.Value.ToString();
                        } else {
                            info.ChangedSettings[pair.Key.Name] = "!!Non-Primitive Type!!";
                        }
                    }
                }
            }
            PopulatePerSaveSettings();
            foreach (var pair in perSaveSettings) {
                var defaultVal = pair.Key.GetValue(perSave);
                if (info.ChangedSettings.TryGetValue(pair.Key.Name, out var val)) {
                    bool isSimple = TypeManager.IsSimpleType(pair.Key.FieldType);
                    bool areEqual = false;
                    if (isSimple) {
                        var method = AccessTools.Method(pair.Key.FieldType, "TryParse", [typeof(string), pair.Key.FieldType.MakeByRefType()]);
                        object[] parameters = [val, Activator.CreateInstance(pair.Key.FieldType)];
                        bool success = (bool)(method?.Invoke(null, parameters) ?? false);
                        if (success) {
                            areEqual = TypeManager.AreObjectsEqual(parameters[1], pair.Value);
                        }
                    }
                    if (!areEqual) {
                        if (TypeManager.AreObjectsEqual(defaultVal, pair.Value)) {
                            info.ChangedSettings.Remove(pair.Key.Name);
                            string toLog = "Per-Save Setting removed: " + pair.Key.Name;
                            Log(toLog);
                        } else {
                            if (isSimple) {
                                if (isSimple) {
                                    string toLog = "Per-Save Setting changed: " + pair.Key.Name;
                                    toLog += $"; prev value: {val}; new value: {pair.Value}";
                                    Log(toLog);
                                }
                                info.ChangedSettings[pair.Key.Name] = pair.Value.ToString();
                            } else {
                                info.ChangedSettings[pair.Key.Name] = "!!Non-Primitive Type!!";
                            }
                        }
                    }
                } else {
                    if (!TypeManager.AreObjectsEqual(defaultVal, pair.Value)) {
                        string toLog = "Per-Save Setting changed: " + pair.Key.Name;
                        if (TypeManager.IsSimpleType(pair.Key.FieldType)) {
                            toLog += $"; prev value: {defaultVal}; new value: {pair.Value}";
                        }
                        Log(toLog);
                        if (TypeManager.IsSimpleType(pair.Key.FieldType)) {
                            info.ChangedSettings[pair.Key.Name] = pair.Value.ToString();
                        } else {
                            info.ChangedSettings[pair.Key.Name] = "Non-Primitive Type";
                        }
                    }
                }
            }
        } catch (Exception ex) {
            Mod.Error(ex.ToString());
        }
    }
    public static void OnHideGUI() {
        if (Game.Instance?.Player != null && Main.Settings?.perSave != null) {
            OnChange();
        }
    }
    public static void Log(string toAdd) {
        if (SaveInfo.Instance != null) {
            Mod.Debug(toAdd);
            var timeString = DateTimeOffset.Now.ToString("[dd.MM.yyyy HH:mm:ss:ffff]");
            SaveInfo.Instance.History.Add(timeString + ": " + toAdd);
        }
    }
    [Serializable]
    public class SaveInfo {
        [JsonIgnore]
        public static SaveInfo Instance;
        [JsonProperty]
        public SerializableDictionary<string, string> ChangedSettings = new();
        [JsonProperty]
        public List<string> History = new();
    }
    public static class TypeManager {
        private static readonly ConcurrentDictionary<Type, bool> IsSimpleTypeCache = new ConcurrentDictionary<Type, bool>();

        public static bool IsSimpleType(Type type) {
            return IsSimpleTypeCache.GetOrAdd(type, t =>
                type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                IsNullableSimpleType(type));

            static bool IsNullableSimpleType(Type t) {
                var underlyingType = Nullable.GetUnderlyingType(t);
                return underlyingType != null && IsSimpleType(underlyingType);
            }
        }
        public static bool AreObjectsEqual(object obj1, object obj2) {
            if (obj1 == null || obj2 == null) {
                return obj1 == obj2;
            }

            Type type1 = obj1.GetType();
            Type type2 = obj2.GetType();

            if (type1 != type2) {
                return false;
            }

            if (IsSimpleType(type1)) {
                return obj1.Equals(obj2);
            } else if (typeof(IEnumerable).IsAssignableFrom(type1)) {
                return CompareEnumerables((IEnumerable)obj1, (IEnumerable)obj2);
            } else {
                return obj1.Equals(obj2);
            }
        }
        private static bool CompareEnumerables(IEnumerable enum1, IEnumerable enum2) {
            var enum1List = new List<object>();
            var enum2List = new List<object>();

            foreach (var item in enum1) {
                enum1List.Add(item);
            }
            foreach (var item in enum2) {
                enum2List.Add(item);
            }

            if (enum1List.Count != enum2List.Count) {
                return false;
            }

            for (int i = 0; i < enum1List.Count; i++) {
                if (!AreObjectsEqual(enum1List[i], enum2List[i])) {
                    return false;
                }
            }
            return true;
        }
    }
}
