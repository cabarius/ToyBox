// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.Models.Log.Events;
using Kingmaker.Utility.Random;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ToyBox.BagOfPatches {
    public static class DiceRollsRT {
        private static bool changePolicy = true;
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;
        [HarmonyPatch(typeof(RulePerformAttackRoll))]
        private static class RulePerformAttackRollPatch {
            private static bool forceHit;
            private static bool forceCrit;
            [HarmonyPatch(nameof(RulePerformAttackRoll.OnTrigger))]
            [HarmonyPrefix]
            private static void OnTriggerPrefix(RulebookEventContext context) {
                if (context.Current.Initiator is BaseUnitEntity unit) {
                    forceCrit = BaseUnitDataUtils.CheckUnitEntityData(unit, settings.allHitsCritical);
                    forceHit = BaseUnitDataUtils.CheckUnitEntityData(unit, settings.allAttacksHit);
                    if (forceCrit || forceHit) {
                        changePolicy = true;
                    } else {
                        changePolicy = false;
                    }
                }
            }
            [HarmonyPatch(nameof(RulePerformAttackRoll.OnTrigger))]
            [HarmonyPostfix]
            private static void OnTriggerPostfix(ref RulePerformAttackRoll __instance) {
                if (forceCrit) {
                    __instance.ResultIsRighteousFury = true;
                    __instance.Result = AttackResult.RighteousFury;
                }
            }
        }
        [HarmonyPatch(typeof(AttackHitPolicyContextData))]
        private static class AttackHitPolicyPatch {
            [HarmonyPatch(nameof(AttackHitPolicyContextData.Current), MethodType.Getter)]
            [HarmonyPostfix]
            private static void GetCurrent(ref AttackHitPolicyType __result) {
                if (changePolicy) {
                    __result = AttackHitPolicyType.AutoHit;
                }
            }
        }
        [HarmonyPatch(typeof(RuleRollDice))]
        private static class RuleRollDicePatch {
            [HarmonyPatch(nameof(RuleRollDice.Result), MethodType.Getter)]
            [HarmonyPriority(Priority.VeryHigh)]
            [HarmonyPostfix]
            private static void Roll(RuleRollDice __instance) {
                if (Rulebook.CurrentContext.Current is RuleRollChance chanceRoll) {
                    var initiator = __instance.InitiatorUnit;
                    if (initiator == null) return;
                    if (chanceRoll.RollTypeValue == RollType.Skill) {
                        if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.skillsTake1)) {
                            __instance.m_Result = 1;
                            return;
                        }
                        else if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.skillsTake25)) {
                            __instance.m_Result = 25;
                            return;
                        }
                        else if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.skillsTake50)) {
                            __instance.m_Result = 50;
                            return;
                        }
                    }
                    if (__instance.DiceFormula.Dice != DiceType.D100) return;
                    var result = __instance.m_Result;
                    if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll1)
                       || (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll1OutOfCombat) && !initiator.IsInCombat)) {
                        result = 1;
                    }
                    else if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll50)) {
                        result = 50;
                    }
                    else if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll100)) {
                        result = 100;
                    }
                    else {
                        bool never1 = BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.neverRoll1) && !__instance.ReplaceOneWithMax;
                        bool never100 = BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.neverRoll100);
                        var min = never1 ? 2 : 1;
                        var max = never100 ? 100 : 101;
                        if ((result == 1 && never1) || (result == 100 && never100)) {
                            result = UnityEngine.Random.Range(min, max);
                        }
                        if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.rollWithAdvantage)) {
                            result = Math.Max(result, PFStatefulRandom.RuleSystem.Range(min, max));
                        }
                        else if (BaseUnitDataUtils.CheckUnitEntityData(initiator, settings.rollWithDisadvantage)) {
                            result = Math.Min(result, PFStatefulRandom.RuleSystem.Range(min, max));
                        }
                    }
                    __instance.m_Result = result;
                }
            }
        }
        [HarmonyPatch(typeof(RulebookEvent.Dice))]
        private static class RulebookEvent_Dice_Patch {
            [HarmonyPatch(nameof(RulebookEvent.Dice.D10), MethodType.Getter)]
            [HarmonyPriority(Priority.VeryHigh)]
            [HarmonyPostfix]
            private static void getD10(ref RuleRollD10 __result) {
                if (Rulebook.CurrentContext.Current is RuleRollInitiative initiativeEvent) {
                    if (BaseUnitDataUtils.CheckUnitEntityData(initiativeEvent.InitiatorUnit, settings.roll1Initiative)) {
                        __result.m_Result = 1;
                    }
                    else if (BaseUnitDataUtils.CheckUnitEntityData(initiativeEvent.InitiatorUnit, settings.roll5Initiative)) {
                        __result.m_Result = 5;
                    }
                    else if (BaseUnitDataUtils.CheckUnitEntityData(initiativeEvent.InitiatorUnit, settings.roll10Initiative)) {
                        __result.m_Result = 10;
                    }
                }
            }
        }
    }
}