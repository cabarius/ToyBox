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
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
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

        [HarmonyPatch(typeof(BuildModeUtility), nameof(BuildModeUtility.CheatsEnabled), MethodType.Getter)]
        private static class BuildModeUtility_CheatsEnabled_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleDevopmentMode) __result = true;
            }
        }

        [HarmonyPatch(typeof(BlueprintConverter))]
        private static class ForceSuccessfulLoad_Blueprints_Patch {
            [HarmonyPatch(nameof(BlueprintConverter.ReadJson), new Type[] { typeof(JsonReader), typeof(Type), typeof(object), typeof(JsonSerializer) })]
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
    }
}