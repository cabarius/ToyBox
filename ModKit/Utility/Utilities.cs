// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModKit {
    public static class Utilities {
        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default) {
            if (dictionary == null) { throw new ArgumentNullException(nameof(dictionary)); } // using C# 6
            if (key == null) { throw new ArgumentNullException(nameof(key)); } //  using C# 6

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
        public static Dictionary<TKey, TElement> ToDictionaryIgnoringDuplicates<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer = null) {
            if (source == null)
                throw new ArgumentException("source");
            if (keySelector == null)
                throw new ArgumentException("keySelector");
            if (elementSelector == null)
                throw new ArgumentException("elementSelector");
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(comparer);
            foreach (TSource element in source) {
                if (!d.ContainsKey(keySelector(element)))
                    d.Add(keySelector(element), elementSelector(element));
            }
            return d;
        }
        public static object GetPropValue(this object obj, string name) {
            foreach (var part in name.Split('.')) {
                if (obj == null) { return null; }

                var type = obj.GetType();
                var info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }
        public static T GetPropValue<T>(this object obj, string name) {
            var retval = GetPropValue(obj, name);
            if (retval == null) { return default; }
            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }
        public static object SetPropValue(this object obj, string name, object value) {
            var parts = name.Split('.');
            var final = parts.Last();
            if (final == null)
                return null;
            foreach (var part in parts) {
                if (obj == null) { return null; }
                var type = obj.GetType();
                var info = type.GetProperty(part);
                if (info == null) { return null; }
                if (part == final) {
                    info.SetValue(obj, value);
                    return value;
                } else {
                    obj = info.GetValue(obj, null);
                }
            }
            return null;
        }
        public static T SetPropValue<T>(this object obj, string name, T value) {
            object retval = SetPropValue(obj, name, value);
            if (retval == null) { return default; }
            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }
        public static string StripHTML(this string s) => Regex.Replace(s, "<.*?>", string.Empty);
        public static string UnityRichTextToHtml(string s) {
            s = s.Replace("<color=", "<font color=");
            s = s.Replace("</color>", "</font>");
            s = s.Replace("<size=", "<size size=");
            s = s.Replace("</size>", "</font>");
            s += "<br/>";

            return s;
        }
        public static string MergeSpaces(this string str, bool trim = false) {
            if (str == null)
                return null;
            else {
                StringBuilder stringBuilder = new StringBuilder(str.Length);

                int i = 0;
                foreach (char c in str) {
                    if (c != ' ' || i == 0 || str[i - 1] != ' ')
                        stringBuilder.Append(c);
                    i++;
                }
                if (trim)
                    return stringBuilder.ToString().Trim();
                else
                    return stringBuilder.ToString();
            }
        }
        public static string ReplaceLastOccurrence(this string Source, string Find, string Replace) {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }
        public static string[] getObjectInfo(object o) {

            var fields = "";
            foreach (var field in Traverse.Create(o).Fields()) {
                fields = fields + field + ", ";
            }
            var methods = "";
            foreach (var method in Traverse.Create(o).Methods()) {
                methods = methods + method + ", ";
            }
            var properties = "";
            foreach (var property in Traverse.Create(o).Properties()) {
                properties = properties + property + ", ";
            }
            return new string[] { fields, methods, properties };
        }
        public static string? SubstringBetweenCharacters(this string input, char charFrom, char charTo) {
            var posFrom = input.IndexOf(charFrom);
            if (posFrom != -1) //if found char
            {
                var posTo = input.IndexOf(charTo, posFrom + 1);
                if (posTo != -1) //if found char
                {
                    return input.Substring(posFrom + 1, posTo - posFrom - 1);
                }
            }

            return string.Empty;
        }
        public static string[] TrimCommonPrefix(this string[] values) {
            var prefix = string.Empty;
            int? resultLength = null;

            if (values != null) {
                if (values.Length > 1) {
                    var min = values.Min(value => value.Length);
                    for (var charIndex = 0; charIndex < min; charIndex++) {
                        for (var valueIndex = 1; valueIndex < values.Length; valueIndex++) {
                            if (values[0][charIndex] != values[valueIndex][charIndex]) {
                                resultLength = charIndex;
                                break;
                            }
                        }
                        if (resultLength.HasValue) {
                            break;
                        }
                    }
                    if (resultLength.HasValue &&
                        resultLength.Value > 0) {
                        prefix = values[0].Substring(0, resultLength.Value);
                    }
                } else if (values.Length > 0) {
                    prefix = values[0];
                }
            }
            return prefix.Length > 0 ? values.Select(s => s.Replace(prefix, "")).ToArray() : values;
        }

        public static Dictionary<string, TEnum> NameToValueDictionary<TEnum>(this TEnum enumValue) where TEnum : struct {
            var enumType = enumValue.GetType();
            return Enum.GetValues(enumType)
                .Cast<TEnum>()
                .ToDictionary(e => Enum.GetName(enumType, e), e => e);
        }
        public static Dictionary<TEnum, string> ValueToNameDictionary<TEnum>(this TEnum enumValue) where TEnum : struct {
            var enumType = enumValue.GetType();
            return Enum.GetValues(enumType)
                .Cast<TEnum>()
                .ToDictionary(e => e, e => Enum.GetName(enumType, e));
        }
        public static Dictionary<K, V> Filter<K, V>(this Dictionary<K, V> dict,
        Predicate<KeyValuePair<K, V>> predicate) => dict.Where(it => predicate(it)).ToDictionary(it => it.Key, it => it.Value);
        public static List<List<T>> Partition<T>(this List<T> values, int chunkSize) {
            return values.Select((x, i) => new { Index = i, Value = x })
                         .GroupBy(x => x.Index / chunkSize)
                         .Select(x => x.Select(v => v.Value).ToList())
                         .ToList();
        }
        public static List<List<T>> BuildChunksWithIteration<T>(this List<T> fullList, int batchSize) {
            var chunkedList = new List<List<T>>();
            var temporary = new List<T>();
            for (int i = 0; i < fullList.Count; i++) {
                //Mod.Log($"{i}/{fullList.Count}");
                var e = fullList[i];
                if (temporary.Count < batchSize) {
                    temporary.Add(e);
                } else {
                    //Mod.Log("new row");
                    chunkedList.Add(temporary);
                    temporary = new List<T>() { e };
                }

                if (i == fullList.Count - 1) {
                    //Mod.Log("last row");
                    chunkedList.Add(temporary);
                }
            }
            //Mod.Log($"result - rows: {chunkedList.Count}");
            return chunkedList;
        }
        // Chunk
        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size) {
            if (source == null) {
                throw new ArgumentNullException("source");
                // ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (size < 1) {
                throw new ArgumentOutOfRangeException("size < 1");
                // ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.size);
            }

            return ChunkIterator(source, size);
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size) {
            using IEnumerator<TSource> e = source.GetEnumerator();

            // Before allocating anything, make sure there's at least one element.
            if (e.MoveNext()) {
                // Now that we know we have at least one item, allocate an initial storage array. This is not
                // the array we'll yield.  It starts out small in order to avoid significantly overallocating
                // when the source has many fewer elements than the chunk size.
                int arraySize = Math.Min(size, 4);
                int i;
                do {
                    var array = new TSource[arraySize];

                    // Store the first item.
                    array[0] = e.Current;
                    i = 1;

                    if (size != array.Length) {
                        // This is the first chunk. As we fill the array, grow it as needed.
                        for (; i < size && e.MoveNext(); i++) {
                            if (i >= array.Length) {
                                arraySize = (int)Math.Min((uint)size, 2 * (uint)array.Length);
                                Array.Resize(ref array, arraySize);
                            }

                            array[i] = e.Current;
                        }
                    } else {
                        // For all but the first chunk, the array will already be correctly sized.
                        // We can just store into it until either it's full or MoveNext returns false.
                        TSource[] local = array; // avoid bounds checks by using cached local (`array` is lifted to iterator object as a field)
                        //Debug.Assert(local.Length == size);
                        for (; (uint)i < (uint)local.Length && e.MoveNext(); i++) {
                            local[i] = e.Current;
                        }
                    }

                    if (i != array.Length) {
                        Array.Resize(ref array, i);
                    }

                    yield return array;
                }
                while (i >= size && e.MoveNext());
            }
        }
    }
    public static class MK {
        public static bool IsKindOf(this Type type, Type baseType) => type.IsSubclassOf(baseType) || type == baseType;
    }
    public static class CloneUtil<T> {
        private static readonly Func<T, object> clone;

        static CloneUtil() {
            var cloneMethod = typeof(T).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            clone = (Func<T, object>)cloneMethod.CreateDelegate(typeof(Func<T, object>));
        }

        public static T ShallowClone(T obj) => (T)clone(obj);
    }

    public static class CloneUtil {
        public static T ShallowClone<T>(this T obj) => CloneUtil<T>.ShallowClone(obj);
    }
}
