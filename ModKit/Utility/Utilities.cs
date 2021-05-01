// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using UnityEditor;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ModKit {
    public static class Utilties {

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue)) {
            if (dictionary == null) { throw new ArgumentNullException(nameof(dictionary)); } // using C# 6
            if (key == null) { throw new ArgumentNullException(nameof(key)); } //  using C# 6

            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
        public static object GetPropValue(this object obj, String name) {
            foreach (String part in name.Split('.')) {
                if (obj == null) { return null; }

                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }

                obj = info.GetValue(obj, null);
            }
            return obj;
        }
        public static T GetPropValue<T>(this object obj, String name) {
            object retval = GetPropValue(obj, name);
            if (retval == null) { return default(T); }
            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }
        public static object SetPropValue(this object obj, String name, object value) {
            var parts = name.Split('.');
            var final = parts.Last();
            if (final == null) return null;
            foreach (String part in parts) {
                if (obj == null) { return null; }
                Type type = obj.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) { return null; }
                if (part == final) {
                    info.SetValue(obj, value);
                    return value;
                }
                else {
                    obj = info.GetValue(obj, null);
                }
            }
            return null;
        }
        public static T SetPropValue<T>(this object obj, String name, T value) {
            object retval = SetPropValue(obj, name, value);
            if (retval == null) { return default(T); }
            // throws InvalidCastException if types are incompatible
            return (T)retval;
        }
        public static string RemoveHtmlTags(string s) {
            return Regex.Replace(s, "<.*?>", String.Empty);
        }
        public static string UnityRichTextToHtml(string s) {
            s = s.Replace("<color=", "<font color=");
            s = s.Replace("</color>", "</font>");
            s = s.Replace("<size=", "<size size=");
            s = s.Replace("</size>", "</font>");
            s += "<br/>";

            return s;
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
    public static class MK {
        public static bool IsKindOf(this Type type, Type baseType) {
            return type.IsSubclassOf(baseType) || type == baseType;
        }
    }
}
