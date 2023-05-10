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
using Kingmaker.UI.MVVM._VM.CharGen;
using Kingmaker.Utility;
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

        [HarmonyPatch(typeof(SmartConsole), nameof(SmartConsole.WriteLine))]
        private static class SmartConsole_WriteLine_Patch {
            private static void Postfix(string message) {
                if (settings.toggleDevopmentMode) {
                    Mod.Log(message);
                    var timestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    UberLoggerAppWindow.Instance.Log(new LogInfo(null, LogChannel.Default, timestamp, LogSeverity.Message, new List<LogStackFrame>(), false, message, Array.Empty<object>())
                        );
                }
            }
        }
#if false
        [HarmonyPatch(typeof(SmartConsole), nameof(SmartConsole.Initialise))]
        private static class SmartConsole_Initialise_Patch {
            private static void Postfix() {
                if (settings.toggleDevopmentMode) {
                    SmartConsoleCommands.Register();
                }
            }
        }

        [HarmonyPatch(typeof(Owlcat.Runtime.Core.Logging.Logger), nameof(Owlcat.Runtime.Core.Logging.Logger.ForwardToUnity))]
        private static class UberLoggerLogger_ForwardToUnity_Patch {
            private static void Prefix(ref object message) {
                if (settings.toggleUberLoggerForwardPrefix) {
                    var message1 = "[UberLogger] " + message as string;
                    message = message1 as object;
                }
            }
        }
#endif
        // This patch if for you @ArcaneTrixter and @Vek17
        [HarmonyPatch(typeof(UnityModManager.Logger), nameof(UnityModManager.Logger.Write))]
        private static class Logger_Logger_Patch {
            private static bool Prefix(string str, bool onlyNative = false) {
                if (str == null)
                    return false;
                var stripHTMLNative = settings.stripHtmlTagsFromNativeConsole;
                var sriptHTMLHistory = settings.stripHtmlTagsFromUMMLogsTab;
                Console.WriteLine(stripHTMLNative ? str.StripHTML() : str);

                if (onlyNative)
                    return false;
                if (sriptHTMLHistory) str = str.StripHTML();
                UnityModManager.Logger.buffer.Add(str);
                UnityModManager.Logger.history.Add(str);

                if (UnityModManager.Logger.history.Count >= UnityModManager.Logger.historyCapacity * 2) {
                    var result = UnityModManager.Logger.history.Skip(UnityModManager.Logger.historyCapacity);
                    UnityModManager.Logger.history.Clear();
                    UnityModManager.Logger.history.AddRange(result);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(CharGenContextVM), nameof(CharGenContextVM.HandleRespecInitiate))]
        private static class CharGenContextVM_HandleRespecInitiate_Patch {
            private static void Prefix(ref CharGenContextVM __instance, ref UnitEntityData character, ref Action successAction) {
                if (settings.toggleRespecRefundScrolls) {
                    var scrolls = new List<BlueprintItemEquipmentUsable>();

                    var loadedscrolls = Game.Instance.BlueprintRoot.CraftRoot.m_ScrollsItems.Select(a => ResourcesLibrary.TryGetBlueprint<BlueprintItemEquipmentUsable>(a.Guid));
                    foreach (var spellbook in character.Spellbooks) {
                        foreach (var scrollspell in spellbook.GetAllKnownSpells())
                            if (scrollspell.CopiedFromScroll)
                                if (loadedscrolls.TryFind(a => a.Ability.NameForAcronym == scrollspell.Blueprint.NameForAcronym, out var item))
                                    scrolls.Add(item);
                    }

                    successAction = PatchedSuccessAction(successAction, scrolls);
                }
            }

            private static Action PatchedSuccessAction(Action successAction, List<BlueprintItemEquipmentUsable> scrolls) =>
                () => {
                    foreach (var scroll in scrolls) Game.Instance.Player.Inventory.Add(new ItemEntityUsable(scroll));
                    successAction.Invoke();
                };
        }

        [HarmonyPatch(typeof(BlueprintConverter), nameof(BlueprintConverter.ReadJson))]
        private static class ForceSuccessfulLoad_Blueprints_Patch {
            private static bool Prefix(ref object __result, JsonReader reader) {
                if (!settings.enableLoadWithMissingBlueprints) return true;
                var text = (string)reader.Value;
                if (string.IsNullOrEmpty(text) || text == "null") {
                    //Mod.Warn($"ForceSuccessfulLoad_Blueprints_Patch - unable to find valid id - text: {text}");
                    __result = null; // We still can't look up a blueprint without a valid id
                    return false;
                }
                SimpleBlueprint retrievedBlueprint;
                try {
                    retrievedBlueprint = ResourcesLibrary.TryGetBlueprint(BlueprintGuid.Parse(text));
                }
                catch {
                    retrievedBlueprint = null;
                }
                if (retrievedBlueprint == null) Mod.Warn($"Failed to load blueprint by guid '{text}' but continued with null blueprint.");
                __result = retrievedBlueprint;

                return false;
            }
        }

        [HarmonyPatch(typeof(EntityFact), nameof(EntityFact.ComponentsDictionary), MethodType.Setter)]
        private static class ForceSuccessfulLoad_OfFacts_Patch {
            private static void Prefix(ref EntityFact __instance) {
                if (__instance.Blueprint == null) Mod.Debug($"Fact type '{__instance}' failed to load. UniqueID: {__instance.UniqueId}");
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