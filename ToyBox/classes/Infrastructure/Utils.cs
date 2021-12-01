using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Items;
using Kingmaker.Items;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.Utility;
using Kingmaker.Visual.LocalMap;
using ModKit;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

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

        public static void SaveToFile<T>(this T obj, string filename = null) {
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
            var bps = items.Select(i => i.Blueprint).ToList();
            SaveToFile(bps, filename);
        }
        public static void Import(this ItemsCollection items, string filename, bool replace = false) {
            var bps = LoadFromFile<List<BlueprintItem>>(filename);
            if (bps != null) {
                if (replace) {
                    var doomed = items.Items.Where<ItemEntity>(x => x.HoldingSlot == null).ToTempList<ItemEntity>();
                    foreach (var toDie in doomed) {
                        items.Remove(toDie);
                    }
                }
                foreach (var bp in bps) {
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
                Mod.Error("Unable to load localization directory.");
                return new();
            }
            catch (FileNotFoundException) {
                Mod.Error("Unable to load localization file.");
                return new();
            }
        }
        public static string ToKM(this float v, string units = "") {
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
        public static string ToBinString(this int v, string units = "", float binSize = 2f) {
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
    }
    public class KeyComparer : IComparer<string> {

        public int Compare(string left, string right) {
            if (int.TryParse(left, out var l) && int.TryParse(right, out var r))
                return l.CompareTo(r);
            else
                return left.CompareTo(right);
        }
    }
}