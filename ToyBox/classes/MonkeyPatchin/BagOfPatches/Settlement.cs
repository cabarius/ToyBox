using HarmonyLib;
using Kingmaker.Kingdom.Settlements;
using System;

namespace ToyBox.BagOfPatches {
    public static class Settlement {
        public static Settings settings = Main.settings;

        [HarmonyPatch(typeof(BlueprintSettlementBuilding), nameof(BlueprintSettlementBuilding.CheckRestrictions), new Type[] { typeof(SettlementState) })]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch1 {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintSettlementBuilding), nameof(BlueprintSettlementBuilding.CheckRestrictions), new Type[] { typeof(SettlementState), typeof(SettlementGridTopology.Slot) })]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch2 {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SettlementState), nameof(SettlementState.CanBuildUprgade), new Type[] { typeof(BlueprintSettlementBuilding) })]
        public static class SettlementState_CanBuildUprgade_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SettlementState), nameof(SettlementState.CanBuildByLevel), new Type[] { typeof(BlueprintSettlementBuilding) })]
        public static class SettlementState_CanBuildByLevel_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SettlementState), nameof(SettlementState.CanBuild), new Type[] { typeof(BlueprintSettlementBuilding) })]
        public static class SettlementState_CanBuild_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreSettlementRestrictions) 
                    __result = true;
            }
        }
    }
}
