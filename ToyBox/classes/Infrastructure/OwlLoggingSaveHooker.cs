using HarmonyLib;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints.JsonSystem;
using System.IO.Compression;
using ModKit;
using Kingmaker.GameInfo;

namespace ToyBox; 
[HarmonyPatch]
internal static class SaveHooker {
    [HarmonyPatch(typeof(ZipSaver))]
    [HarmonyPatch("SaveJson"), HarmonyPrefix]
    //[HarmonyPatch("SaveJson"), HarmonyPostfix]
    private static void Zip_Saver(string name, ZipSaver __instance) {
        if (!name.StartsWith("header"))
            return;

        OwlLogging.OnChange();
        try {
            var serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            var writer = new StringWriter();
            serializer.Serialize(writer, OwlLogging.SaveInfo.Instance);
            writer.Flush();
            /*
            ZipArchiveEntry zipArchiveEntry = __instance.FindEntry(LoadHooker.FileName);
            if (zipArchiveEntry == null) {
                zipArchiveEntry = __instance.ZipFile.CreateEntry(LoadHooker.FileName);
            }
            using (Stream stream = zipArchiveEntry.Open()) {
                using (StreamWriter streamWriter = new StreamWriter(stream)) {
                    streamWriter.Write(writer.ToString());
                    stream.SetLength(stream.Position);
                }
            }
            */
            Game.Instance.State.InGameSettings.List[LoadHooker.FileName] = writer.ToString();
        } catch (Exception e) {
            Main.logger.Log(e.ToString());
        }
    }
}

[HarmonyPatch(typeof(Game))]
internal static class LoadHooker {
    public const string FileName = "ToyBox.log";

    [HarmonyPatch(nameof(Game.LoadGame)), HarmonyPostfix]
    private static void LoadGame(SaveInfo saveInfo) {
        try {
            using (saveInfo) {
                using (saveInfo.GetReadScope()) {
                    string raw = null;
                    /*
                     * using (ZipSaver saver = saveInfo.Saver.Clone() as ZipSaver) {
                        using var stream = saver?.FindEntry(FileName)?.Open();
                        if (stream != null) {
                            raw = new StreamReader(stream).ReadToEnd();
                        }
                    }
                    */
                    object rawObj = null;
                    Game.Instance?.State?.InGameSettings?.List.TryGetValue(FileName, out rawObj);
                    raw = (string)rawObj;
                    if (raw != null) {
                        var serializer = new JsonSerializer();
                        var rawReader = new StringReader(raw);
                        var jsonReader = new JsonTextReader(rawReader);
                        OwlLogging.SaveInfo.Instance = serializer.Deserialize<OwlLogging.SaveInfo>(jsonReader);
                    } else {
                        OwlLogging.SaveInfo.Instance = new OwlLogging.SaveInfo();
                    }
                    OwlLogging.Log($"Safe loaded with ToyBox v{Main.modEntry.Version} and Game v{GameVersion.GetVersion()}");
                }
            }
        } catch (Exception e) {
            Main.logger.Error(e.ToString());
        }
        OwlLogging.OnChange();
    }
}