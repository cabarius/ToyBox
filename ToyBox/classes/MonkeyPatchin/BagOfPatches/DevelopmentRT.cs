using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Persistence.Versioning;
using Kingmaker.Items;
using Kingmaker.Utility;
using Kingmaker.Utility.BuildModeUtils;
using ModKit;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Logging;
using UnityModManagerNet;
using static Kingmaker.EntitySystem.Persistence.Versioning.JsonUpgradeSystem;

namespace ToyBox.classes.MonkeyPatchin.BagOfPatches {
    internal class Development {
        public static Settings settings = Main.Settings;

        [HarmonyPatch(typeof(BuildModeUtility), nameof(BuildModeUtility.IsDevelopment), MethodType.Getter)]
        private static class BuildModeUtility_IsDevelopment_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleDevopmentMode) __result = true;
            }
        }

        [HarmonyPatch(typeof(BlueprintConverter))]
        private static class ForceSuccessfulLoad_Blueprints_Patch {
            [HarmonyPatch(nameof(BlueprintConverter.ReadJson), new Type[] {typeof(JsonReader), typeof(Type), typeof(object), typeof(JsonSerializer)})]
            [HarmonyPrefix]
            public static bool ReadJson(
                    BlueprintConverter __instance,
                    JsonReader reader,
                    Type objectType,
                    object existingValue,
                    JsonSerializer serializer,
                    ref object __result
                ) {
                if (!settings.enableLoadWithMissingBlueprints) return true;
                var text = (string)reader.Value;
                if (string.IsNullOrEmpty(text) || text == "null") {
                    //Mod.Warn($"ForceSuccessfulLoad_Blueprints_Patch - unable to find valid id - text: {text}");
                    __result = null; // We still can't look up a blueprint without a valid id
                    return false;
                }
                SimpleBlueprint retrievedBlueprint;
                try {
                    retrievedBlueprint = ResourcesLibrary.TryGetBlueprint(text);
                }
                catch {
                    retrievedBlueprint = null;
                }
                if (retrievedBlueprint == null) Mod.Warn($"Failed to load blueprint by guid '{text}' but continued with null blueprint.");
                __result = retrievedBlueprint;

                return false;
            }
        }

        [HarmonyPatch(typeof(EntityFact), nameof(EntityFact.AllComponentsCache), MethodType.Getter)]
        private static class ForceSuccessfulLoad_OfFacts_Patch {
            [HarmonyPrefix]
            private static void Prefix(ref EntityFact __instance) {
                if (__instance.Blueprint == null) Mod.Warn($"Fact type '{__instance}' failed to load. UniqueID: {__instance.UniqueId}");
            }
        }
        #if false
        [HarmonyPatch(typeof(JsonUpgradeSystem))]
        public static class JsonUpgradeSystemPatch {
            [HarmonyPatch(nameof(JsonUpgradeSystem.GetUpgraders), typeof(SaveInfo))]
            [HarmonyPrefix]
            private static bool GetUpgraders(SaveInfo saveInfo, IEnumerable<UpgraderEntry> __result) {
                return false;
                if (!settings.enableLoadWithMissingBlueprints) return true;
                var saveVersionsSet = new HashSet<int>(saveInfo.Versions);
                var availableList = s_Updaters.Select(u => u.Version).ToList();
                var availableSet = new HashSet<int>(availableList);
                var saveVersions = string.Join(", ", saveInfo.Versions.Select(i => i.ToString()).ToArray());
                var availVersions = string.Join(", ", availableList.Select(i => i.ToString()).ToArray());
                Mod.Warn($"save versions: {saveVersions}");
                Mod.Warn($"available versions: {availVersions}");
                foreach (var version in saveInfo.Versions) {
                    if (!availableSet.Contains(version)) {
                        Mod.Warn(string.Format("Unknown version in save info: {0}", version) + string.Format("\nSave versions: {0}", saveInfo.Versions) + string.Format("\nKnown versions: {0}", availableList));
//                        throw new JsonUpgradeException(string.Format("Unknown version in save info: {0}", version) + string.Format("\nSave versions: {0}", saveInfo.Versions) + string.Format("\nKnown versions: {0}", availableList));
                    }
                }
                __result = s_Updaters.Where(u => !saveVersionsSet.Contains(u.Version));
                return false;
            }
        }
#endif
    }
}