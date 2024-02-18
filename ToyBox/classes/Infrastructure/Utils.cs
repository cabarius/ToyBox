using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Items;
using Kingmaker.Localization;
using Kingmaker.Utility;
using ModKit;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utility;
using UnityEngine;
using Attribute = System.Attribute;
using LocalizationManager = Kingmaker.Localization.LocalizationManager;
#nullable enable annotations

namespace ToyBox {

    public static partial class Utils {
        public static string ToyBoxUserPath => Path.Combine(ApplicationPaths.persistentDataPath, "ToyBox");
        public static Vector3 PointerPosition() {
            Vector3 result = new();

            var camera = Game.GetCamera();
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var raycastHit, camera.farClipPlane, 21761)) {
                result = raycastHit.point;
            }
            return result;
        }

        public static void SaveToFile<T>(this T obj, string? filename = null) {
            if (filename == null) filename = $"{obj.GetType().Name}.json";
            var toyboxFolder = ToyBoxUserPath;
            Directory.CreateDirectory(toyboxFolder);
            var path = Path.Combine(toyboxFolder, filename);
            File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
        public static T LoadFromFile<T>(string filename) {
            T obj = default;
            if (filename == null) filename = $"{obj.GetType().Name}.json";

            var toyboxFolder = ToyBoxUserPath;
            Directory.CreateDirectory(toyboxFolder);
            var path = Path.Combine(toyboxFolder, filename);
            try {

                using StreamReader reader = new(path); var text = reader.ReadToEnd();
                obj = JsonConvert.DeserializeObject<T>(text);
            }
            catch (Exception e) {
                Mod.Error($"{filename} could not be read: {e}");
            }
            return obj;
        }

        public static void Export(this List<ItemEntity> items, string filename) {
            var guids = items.Select(i => i.Blueprint.AssetGuid.ToString()).ToList();
            SaveToFile(guids, filename);
        }
        public static void Import(this ItemsCollection items, string filename, bool replace = false) {
            var guids = LoadFromFile<List<string>>(filename);
            if (guids != null) {
                if (replace) {
                    var doomed = items.Items.Where<ItemEntity>(x => x.HoldingSlot == null).ToTempList<ItemEntity>();
                    foreach (var toDie in doomed) {
                        items.Remove(toDie);
                    }
                }
                foreach (var guid in guids) {
                    var bp = ResourcesLibrary.TryGetBlueprint<BlueprintItem>(guid);
                    if (bp != null) 
                        items.Add(bp);
                }
            }
        }
        public static Dictionary<string, string> ReadTranslations() {
            try {
                var path = Mod.modEntryPath;
                path = Path.Combine(path, "Localization", "etude-comments.txt");
                var text = File.ReadAllText(path);
                text = Regex.Replace(text, @"(^\p{Zs}*\r\n){2,}", "\r\n", RegexOptions.Multiline);
                var chunks = text.Split('`');
                Dictionary<string, string> result = new();
                for (var ii = 0; ii + 1 < chunks.Length; ii += 4) {
                    //Mod.Debug($"{ii} => {chunks[ii]}");

                    var key = chunks[ii + 1].Trim();
                    var value = chunks[ii + 3].Trim();
                    if (key.Length == 0 || value.Length == 0) continue;
                    result[key] = value;
                    //Mod.Debug($"'{key}' => '{value}'");
                }
                return result;
            }
            catch (DirectoryNotFoundException) {
                Mod.Warn("Unable to load localization directory.");
                return new();
            }
            catch (FileNotFoundException) {
                Mod.Warn("Unable to load Etude localization file.");
                return new();
            }
        }
        public static string ToKM(this float v, string? units = "") {
            if (v < 1000) {
                return $"{v:0}{units}";
            }
            else if (v < 1000000) {
                v = Mathf.Floor(v / 1000);
                return $"{v:0.#}k{units}";
            }
            v = Mathf.Floor(v / 1000000);
            return $"{v:0.#}m{units}";
        }
        public static string ToBinString(this int v, string? units = "", float binSize = 2f) {
            if (v < 0) return "< 0";
            binSize = Mathf.Clamp(binSize, 1.1f, 20f);
            var logv = Mathf.Log(v) / Mathf.Log(binSize);
            var floorLogV = Mathf.Floor(logv);
            var min = Mathf.Pow(binSize, floorLogV);
            var minStr = min.ToKM(units);
            var max = Mathf.Pow(binSize, floorLogV + 1);
            if (min == max) return $"{min:0}{units}";
            var maxStr = max.ToKM(units);
            return $"{minStr} - {maxStr}";
        }
        public static string ToBinString(this float v, string? units = "", float binSize = 2f) {
            if (v < 0) return "< 0";
            binSize = Mathf.Clamp(binSize, 1.1f, 20f);
            var logv = Mathf.Log(v) / Mathf.Log(binSize);
            var floorLogV = Mathf.Floor(logv);
            var min = Mathf.Pow(binSize, floorLogV);
            var minStr = min.ToKM(units);
            var max = Mathf.Pow(binSize, floorLogV + 1);
            if (min == max) return $"{min:0}{units}";
            var maxStr = max.ToKM(units);
            return $"{minStr} - {maxStr}";
        }
        public static long LongSortKey(this string s) {
            s = s.StripHTML();
            var match = Regex.Match(s, @"\d+");
            if (match == null || match.Value.Length <= 0) return int.MinValue;
            var stringValue = match.Value;
            var v = long.Parse(stringValue, NumberFormatInfo.InvariantInfo);
            var index = match.Index + match.Length;
            if (index < s.Length) {
                if (s[index] == 'k') v *= 1000;
                if (s[index] == 'm') v *= 1000000;
            }
            return v;
        }
        public static string CollectionToString(this IEnumerable<object> col) => $"{{{string.Join(", ", col.Select(i => i.ToString()))}}}";
        // Object to Dictionary
        public static IDictionary<string, object> ToDictionary(this object source) => source.ToDictionary<object>();
        
        public static IDictionary<string, T> ToDictionary<T>(this object source) {
            if (source == null)
                ThrowExceptionWhenSourceArgumentIsNull();

            var dictionary = new Dictionary<string, T>();
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source))
                AddPropertyToDictionary<T>(property, source, dictionary);
            return dictionary;
        }
        private static void AddPropertyToDictionary<T>(PropertyDescriptor property, object source, Dictionary<string, T> dictionary) {
            object value = property.GetValue(source);
            if (IsOfType<T>(value))
                dictionary.Add(property.Name, (T)value);
        }
        private static bool IsOfType<T>(object value) => value is T;
        private static void ThrowExceptionWhenSourceArgumentIsNull() {
            throw new ArgumentNullException("source", "Unable to convert object to a dictionary. The source object is null.");
        }

        // Annotation attributes
        public static string StringValue(this PropertyInfo prop, object obj) {
            var value = prop.GetValue(obj);
            if (value is string str) return str;
            return value.ToString();
        }
        public static string StringValue(this FieldInfo field, object obj) {
            var value = field.GetValue(obj);
            if (value is string str) return str;
            return value.ToString();
        }
        public static Dictionary<string, string> ToStringDictionary(this object obj) {
            var propDict = obj.GetType()
                            .GetProperties(BindingFlags.Instance 
                                           | BindingFlags.NonPublic
                                           | BindingFlags.Public)
                            .Where(field => field.GetValue(obj) is string)
                            .ToDictionary(prop => prop.Name, prop => prop.StringValue(obj)
                            );
            var fieldDict = obj.GetType()
                            .GetFields(BindingFlags.Instance 
                                       | BindingFlags.NonPublic
                                       | BindingFlags.Public)
                            .Where(field => field.GetValue(obj) is string)
                            .ToDictionary(field => field.Name, field => field.StringValue(obj)
                            );
            return propDict.MergeLeft(fieldDict);
        }
        public static Dictionary<string, string> GetCustomAttributes<T>(this T model) where T : class {
            Dictionary<string, string> result = new Dictionary<string, string>();

            return result;
        }
        public static T GetAttributeFrom<T>(this object instance, string propertyName) where T : Attribute {
            var attrType = typeof(T);
            var property = instance.GetType().GetProperty(propertyName);
            return (T)property.GetCustomAttributes(attrType, false).First();
        }
        public static IEnumerable<T> GetAttributes<T>(this Type t) where T : Attribute => t.GetCustomAttributes(typeof(T), true).Cast<T>();
        public static IEnumerable<T> GetAttributes<T>(this object obj) where T : Attribute => obj.GetType().GetCustomAttributes(typeof(T), true).Cast<T>();
    }
    public class KeyComparer : IComparer<string> {

        public int Compare(string left, string right) {
            if (int.TryParse(left, out var l) && int.TryParse(right, out var r))
                return l.CompareTo(r);
            else
                return left.CompareTo(right);
        }
    }
#if false
    public static class TempListExtension {
        public static List<T> ToTempList<T>([CanBeNull] this List<T> _this) {
            List<T> list = TempList.Get<T>();
            if (_this == null) {
                return list;
            }

            list.Capacity = System.Math.Max(list.Capacity, _this.Capacity);
            foreach (T _thi in _this) {
                list.Add(_thi);
            }

            return list;
        }

        public static List<KeyValuePair<TKey, TValue>> ToTempList<TKey, TValue>([CanBeNull] this Dictionary<TKey, TValue> _this) {
            List<KeyValuePair<TKey, TValue>> list = TempList.Get<KeyValuePair<TKey, TValue>>();
            if (_this == null) {
                return list;
            }

            foreach (KeyValuePair<TKey, TValue> _thi in _this) {
                list.Add(_thi);
            }

            return list;
        }

        public static List<T> ToTempList<T>([CanBeNull] this IEnumerable<T> _this) {
            List<T> list = TempList.Get<T>();
            if (_this == null) {
                return list;
            }

            foreach (T _thi in _this) {
                list.Add(_thi);
            }

            return list;
        }
    }
    public class TempList {
        private abstract class Releasable {
            public abstract void ReleaseInternal();
        }

        private class PoolHolder<T> : Releasable {
            public static readonly PoolHolder<T> Instance;

            public readonly Stack<List<T>> Pool = new Stack<List<T>>();

            public readonly Stack<List<T>> Claimed = new Stack<List<T>>();

            static PoolHolder() {
                s_Pools.Add(Instance = new PoolHolder<T>());
            }

            public override void ReleaseInternal() {
                while (Claimed.Count > 0) {
                    List<T> list = Claimed.Pop();
                    list.Clear();
                    Pool.Push(list);
                }
            }
        }

        private static readonly List<Releasable> s_Pools = new List<Releasable>();

        public static List<T> Get<T>() {
            if (!UnityThreadHolder.IsMainThread) {
                return new List<T>();
            }

            List<T> list = ((PoolHolder<T>.Instance.Pool.Count > 0) ? PoolHolder<T>.Instance.Pool.Pop() : new List<T>());
            if (list.Count > 0) {
                Mod.Error("Templist (of " + typeof(T).Name + ") is not empty on claim! Someone is storing a reference to templist.");
                return Get<T>();
            }

            PoolHolder<T>.Instance.Claimed.Push(list);
            return list;
        }

        public static void Release() {
            foreach (Releasable s_Pool in s_Pools) {
                s_Pool.ReleaseInternal();
            }
        }
    }
#endif
    public static class DictionaryExtensions {
        // Works in C#3/VS2008:
        // Returns a new dictionary of this ... others merged leftward.
        // Keeps the type of 'this', which must be default-instantiable.
        // Example: 
        //   result = map.MergeLeft(other1, other2, ...)
        public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others)
            where T : IDictionary<K, V>, new() {
            T newMap = new T();
            foreach (IDictionary<K, V> src in
                (new List<IDictionary<K, V>> { me }).Concat(others)) {
                // ^-- echk. Not quite there type-system.
                foreach (KeyValuePair<K, V> p in src) {
                    newMap[p.Key] = p.Value;
                }
            }
            return newMap;
        }
    }

    public static class LocalizationUtils {
        public static void AddLocalizedString(this string value) => Kingmaker.Localization.LocalizationManager.Instance.CurrentPack.PutString(value, value);
        public static LocalizedString LocalizedStringInGame(this string key) => new LocalizedString() { Key = key };

    }
}