// This code is licensed under MIT license (see LICENSE for details)
// Based on work in https://github.com/hsinyuhcan/KingmakerTurnBasedMod by Hsinyu Chan 
// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker.View;

namespace ToyBox {
    // TODO - do we really need this?  Someone requested it but I don't observe any movement restrictions
    public static class MoveThroughOthers {
        // moving through ... feature
        public static Settings settings => Main.settings;
        [HarmonyPatch(typeof(UnitMovementAgent), nameof(UnitMovementAgent.AvoidanceDisabled), MethodType.Getter)]
        private static class UnitMovementAgent_AvoidanceDisabled_Patch {
            [HarmonyPostfix]
            private static void Postfix(UnitMovementAgent __instance, ref bool __result) {
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Unit?.EntityData, settings.allowMovementThroughSelection)) {
                    __result = true;
                }
            }
        }

        // forbid moving through non selected entity type
        [HarmonyPatch(typeof(UnitMovementAgent), nameof(UnitMovementAgent.IsSoftObstacle), typeof(UnitMovementAgent))]
        private static class UnitMovementAgent_IsSoftObstacle_Patch {
            [HarmonyPrefix]
            private static bool Prefix(UnitMovementAgent __instance, ref bool __result) {
                if (!UnitEntityDataUtils.CheckUnitEntityData(__instance.Unit?.EntityData, settings.allowMovementThroughSelection)) {
                    __result = !__instance.CombatMode;  // this duplicates the logic in the original logic for IsSoftObstacle.  If we are not in combat mode and it is not in our allow movement through category then it is a soft obstacle
                    return false;
                }
                return true;
            }
        }

        // modify collision radius
        [HarmonyPatch(typeof(UnitMovementAgentBase), nameof(UnitMovementAgent.Corpulence), MethodType.Getter)]
        private static class UnitMovementAgentBaset_get_Corpulence_Patch {
            [HarmonyPostfix]
            private static void Postfix(ref float __result) => __result *= settings.collisionRadiusMultiplier;
        }
    }
}