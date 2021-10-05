using Kingmaker;
using Kingmaker.Blueprints.Items;
using Kingmaker.Items;
using Kingmaker.Utility;
using ModKit;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var toyboxFolder = Utils.ToyBoxUserPath;
            Directory.CreateDirectory(toyboxFolder);
            var filePath = Path.Combine(toyboxFolder, filename);
            File.WriteAllText(filePath, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
        public static T LoadFromFile<T>(string filename) {
            T obj = default;
            if (filename == null) filename = $"{obj.GetType().Name}.json";

            var toyboxFolder = Utils.ToyBoxUserPath;
            Directory.CreateDirectory(toyboxFolder);
            var filePath = Path.Combine(toyboxFolder, filename);
            try {

                using StreamReader reader = new(filePath); var text = reader.ReadToEnd();
                obj = JsonConvert.DeserializeObject<T>(text);
            }
            catch (Exception e) {
                Mod.Error($"{filename} could not be read: {e}");
            }
            return obj;
        }
        public static void Export(this List<ItemEntity> items, string filename) {
            var bps = items.Select(i => i.Blueprint).ToList();
            Utils.SaveToFile(bps, filename);
        }
        public static void Import(this ItemsCollection items, string filename, bool replace = false) {
            var bps = Utils.LoadFromFile<List<BlueprintItem>>(filename);
            if (bps != null) {
                if (replace) {
                    var doomed = items.Items.Where<ItemEntity>((x => x.HoldingSlot == null)).ToTempList<ItemEntity>();
                    foreach (var toDie in doomed) {
                        items.Remove(toDie);
                    }
                }
                foreach (var bp in bps) {
                    items.Add(bp);
                }
            }
        }
    }
}