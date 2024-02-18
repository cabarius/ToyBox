#nullable enable annotations
﻿using HarmonyLib;
using Kingmaker.View.MapObjects.Traps;

namespace ToyBox.BagOfPatches {
    internal static partial class Tweaks {
        [HarmonyPatch(typeof(TrapObjectData), nameof(TrapObjectData.TryTriggerTrap))]
        public static class TrapObjectData_TryTriggerTrap_Patch {
            private static bool Prefix() {
                if (Settings.disableTraps) {
                    return false;
                }
                return true;
            }
        }
    }
}