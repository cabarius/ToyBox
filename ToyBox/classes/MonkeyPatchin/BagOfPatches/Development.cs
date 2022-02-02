using HarmonyLib;
using Kingmaker.Utility;
using Owlcat.Runtime.Core.Logging;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using Logger = UnityModManagerNet.UnityModManager.Logger;
using Kingmaker.UI.MVVM._VM.CharGen;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Items;

namespace ToyBox.classes.MonkeyPatchin.BagOfPatches {
    internal class Development {
        public static Settings settings = Main.settings;

        [HarmonyPatch(typeof(BuildModeUtility), nameof(BuildModeUtility.IsDevelopment), MethodType.Getter)]
        private static class BuildModeUtility_IsDevelopment_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleDevopmentMode) {
                    __result = true;
                }
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
        [HarmonyPatch(typeof(Logger), nameof(Logger.Write))]
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
                Logger.buffer.Add(str);
                Logger.history.Add(str);

                if (Logger.history.Count >= Logger.historyCapacity * 2) {
                    var result = Logger.history.Skip(Logger.historyCapacity);
                    Logger.history.Clear();
                    Logger.history.AddRange(result);
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
                        foreach (var scrollspell in spellbook.GetAllKnownSpells()) {
                            if (scrollspell.CopiedFromScroll) {
                                if (loadedscrolls.TryFind(a => a.Ability.NameForAcronym == scrollspell.Blueprint.NameForAcronym, out var item)) {
                                    scrolls.Add(item);
                                }
                            }
                        }
                    }

                    successAction = PatchedSuccessAction(successAction, scrolls);
                }
            }

            private static Action PatchedSuccessAction(Action successAction, List<BlueprintItemEquipmentUsable> scrolls) {
                return () => {
                    foreach (var scroll in scrolls) {
                        Game.Instance.Player.Inventory.Add(new ItemEntityUsable(scroll));
                    }
                    successAction.Invoke();
                };
            }
        }
    }
}
