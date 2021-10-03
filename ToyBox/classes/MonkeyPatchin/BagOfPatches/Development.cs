using HarmonyLib;
using Kingmaker.Utility;
using Owlcat.Runtime.Core.Logging;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.classes.MonkeyPatchin.BagOfPatches {
    class Development {
        public static Settings settings = Main.settings;
        
        [HarmonyPatch(typeof(BuildModeUtility), "IsDevelopment", MethodType.Getter)]
        static class BuildModeUtility_IsDevelopment_Patch {
            static void Postfix(ref bool __result) {
                if (settings.toggleDevopmentMode) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(SmartConsole), "WriteLine")]
        static class SmartConsole_WriteLine_Patch {
            static void Postfix(string message) {
                if (settings.toggleDevopmentMode) {
                    Mod.Log(message);
                    var timestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    UberLoggerAppWindow.Instance.Log(new LogInfo(null, LogChannel.Default, timestamp, LogSeverity.Message, new List<LogStackFrame>(), false, message, Array.Empty<object>())
                        );
                }
            }
        }
        [HarmonyPatch(typeof(SmartConsole), "Initialise")]
        static class SmartConsole_Initialise_Patch {
            static void Postfix() {
                if (settings.toggleDevopmentMode) {
                    SmartConsoleCommands.Register();
                }
            }
        }


        [HarmonyPatch(typeof(Owlcat.Runtime.Core.Logging.Logger), "ForwardToUnity")]
        static class UberLoggerLogger_ForwardToUnity_Patch {
            static void Prefix(ref object message) {
                if (settings.toggleUberLoggerForwardPrefix) {
                    string message1 = "[UberLogger] " + message as string;
                    message = message1 as object;
                }
            }
        }

    }
}
