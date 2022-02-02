using System;
using HarmonyLib;
using UnityModManagerNet;
using Kingmaker;
using Kingmaker.Enums;
using Kingmaker.UnitLogic.Alignments;
using UnityEngine;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using ModKit;

namespace ToyBox {
    public static class AlignmentPatches {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(UnitAlignment), nameof(UnitAlignment.GetDirection))]
        private static class UnitAlignment_GetDirection_Patch {
            private static void Postfix(UnitAlignment __instance, ref Vector2 __result, AlignmentShiftDirection direction) {
                if (settings.toggleAlignmentFix) {
                    if (direction == AlignmentShiftDirection.NeutralGood) __result = new Vector2(0, 1);
                    if (direction == AlignmentShiftDirection.NeutralEvil) __result = new Vector2(0, -1);
                    if (direction == AlignmentShiftDirection.LawfulNeutral) __result = new Vector2(-1, 0);
                    if (direction == AlignmentShiftDirection.ChaoticNeutral) __result = new Vector2(1, 0);
                }
            }
        }
        [HarmonyPatch(typeof(UnitAlignment), nameof(UnitAlignment.Set), new Type[] { typeof(Alignment), typeof(bool) })]
        private static class UnitAlignment_Set_Patch {
            private static void Prefix(UnitAlignment __instance, ref Kingmaker.Enums.Alignment alignment) {
                if (settings.togglePreventAlignmentChanges) {
                    if (__instance.m_Value != null)
                        alignment = (Kingmaker.Enums.Alignment)__instance.m_Value;
                }
            }
        }
        [HarmonyPatch(typeof(UnitAlignment), nameof(UnitAlignment.Shift), new Type[] { typeof(AlignmentShiftDirection), typeof(int), typeof(IAlignmentShiftProvider) })]
        private static class UnitAlignment_Shift_Patch {
            private static bool Prefix(UnitAlignment __instance, AlignmentShiftDirection direction, ref int value, IAlignmentShiftProvider provider) {
                try {
                    if (settings.togglePreventAlignmentChanges) {
                        value = 0;
                    }

                    if (settings.toggleAlignmentFix) {
                        if (value == 0) {
                            return false;
                        }
                        var vector = __instance.m_Vector;
                        var num = (float)value / 50f;
                        var directionVector = Traverse.Create(__instance).Method("GetDirection", new object[] { direction }).GetValue<Vector2>();
                        var newAlignment = __instance.m_Vector + directionVector * num;
                        if (newAlignment.magnitude > 1f) {
                            //Instead of normalizing towards true neutral, normalize opposite to the alignment vector
                            //to prevent sliding towards neutral
                            newAlignment -= (newAlignment.magnitude - newAlignment.normalized.magnitude) * directionVector;
                        }
                        if (direction == AlignmentShiftDirection.TrueNeutral && (Vector2.zero - __instance.m_Vector).magnitude < num) {
                            newAlignment = Vector2.zero;
                        }
                        Traverse.Create(__instance).Property<Vector2>("Vector").Value = newAlignment;
                        Traverse.Create(__instance).Method("UpdateValue").GetValue();
                        //Traverse requires the parameter types to find interface parameters
                        Traverse.Create(__instance).Method("OnChanged",
                            new Type[] { typeof(AlignmentShiftDirection), typeof(Vector2), typeof(IAlignmentShiftProvider), typeof(bool) },
                            new object[] { direction, vector, provider, true }).GetValue();
                        return false;
                    }
                }
                catch (Exception e) {
                    Mod.Error(e);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ForbidSpellbookOnAlignmentDeviation), nameof(ForbidSpellbookOnAlignmentDeviation.CheckAlignment))]
        private static class ForbidSpellbookOnAlignmentDeviation_CheckAlignment_Patch {
            private static bool Prefix(ForbidSpellbookOnAlignmentDeviation __instance) {
                if (settings.toggleSpellbookAbilityAlignmentChecks) {
                    __instance.Alignment = __instance.Owner.Alignment.ValueRaw.ToMask();
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(AbilityCasterAlignment), nameof(AbilityCasterAlignment.IsCasterRestrictionPassed))]
        private static class AbilityCasterAlignment_CheckAlignment_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleSpellbookAbilityAlignmentChecks) {
                    __result = true;
                }
            }
        }
    }
}