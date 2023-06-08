// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using ModKit;
using System;

namespace ToyBox.BagOfPatches {
    public static class DiceRollsWrath {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(RuleAttackRoll), nameof(RuleAttackRoll.IsCriticalConfirmed), MethodType.Getter)]
        private static class HitPlayer_OnTriggerl_Patch {
            private static void Postfix(ref bool __result, RuleAttackRoll __instance) {
                if (__instance.IsHit && UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.allHitsCritical)) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(RuleAttackRoll), nameof(RuleAttackRoll.IsHit), MethodType.Getter)]
        private static class HitPlayer_OnTrigger2_Patch {
            private static void Postfix(ref bool __result, RuleAttackRoll __instance) {
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.allAttacksHit)) {
                    __result = true;
                }
            }
        }

#if false
        [HarmonyPatch(typeof(RuleCastSpell), nameof(RuleCastSpell.IsArcaneSpellFailed), MethodType.Getter)]
        public static class RuleCastSpell_IsArcaneSpellFailed_Patch {
            static void Postfix(RuleCastSpell __instance, ref bool __result) {
                if ((__instance.Spell.Caster?.Unit?.IsPartyMemberOrPet() ?? false) && (StringUtils.ToToggleBool(settings.toggleArcaneSpellFailureRoll))) {
                    if (!StringUtils.ToToggleBool(settings.toggleArcaneSpellFailureRollOutOfCombatOnly)) {
                        __result = false;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArcaneSpellFailureRollOutOfCombatOnly) && !__instance.Initiator.IsInCombat) {
                        __result = false;
                    }

                }
            }
        }
#endif

        [HarmonyPatch(typeof(RuleRollDice), nameof(RuleRollDice.Roll))]
        public static class RuleRollDice_Roll_Patch {
            private static void Postfix(RuleRollDice __instance) {
                if (__instance.DiceFormula.Dice != DiceType.D20) return;
                var initiator = __instance.Initiator;
                var result = __instance.m_Result;
                //modLogger.Log($"initiator: {initiator.CharacterName} isInCombat: {initiator.IsInCombat} alwaysRole20OutOfCombat: {settings.alwaysRoll20OutOfCombat}");
                //Mod.Debug($"initiator: {initiator.CharacterName} Initial D20Roll: " + result);
                if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll20)
                   || (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll20OutOfCombat) && !initiator.IsInCombat)) {
                    result = 20;
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll10)) {
                    result = 10;
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll1)) {
                    result = 1;
                }
                else {
                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.rollWithAdvantage)) {
                        result = Math.Max(result, UnityEngine.Random.Range(1, 21));
                    }
                    else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.rollWithDisadvantage)) {
                        result = Math.Min(result, UnityEngine.Random.Range(1, 21));
                    }
                    var min = 1;
                    var max = 21;
                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.neverRoll1) && result == 1) {
                        result = UnityEngine.Random.Range(2, 21);
                        min = 2;
                    }
                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.neverRoll20) && result == 20) {
                        max = 20;
                        result = UnityEngine.Random.Range(min, max);
                    }
                }
                //Mod.Debug("Modified D20Roll: " + result);
                __instance.m_Result = result;
            }
        }

        [HarmonyPatch(typeof(RuleInitiativeRoll), nameof(RuleInitiativeRoll.Result), MethodType.Getter)]
        public static class RuleInitiativeRoll_OnTrigger_Patch {
            private static void Postfix(RuleInitiativeRoll __instance, ref int __result) {
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.roll1Initiative)) {
                    __result = 1 + __instance.Modifier;
                    Mod.Trace("Modified InitiativeRoll: " + __result);
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.roll10Initiative)) {
                    __result = 10 + __instance.Modifier;
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.roll20Initiative)) {
                    __result = 20 + __instance.Modifier;
                    Mod.Trace("Modified InitiativeRoll: " + __result);
                }
            }
        }

        // Thanks AlterAsc - https://github.com/alterasc/CombatRelief/blob/main/CombatRelief/SkillRolls.cs
        [HarmonyPatch(typeof(RuleSkillCheck), nameof(RuleSkillCheck.RollD20))]
        public static class RuleSkillCheck_RollD20_Patch {
            [HarmonyPrefix]
            private static bool Prefix(ref RuleRollD20 __result, RuleSkillCheck __instance) {
                if (__instance.Initiator.IsInCombat) {
                    return true;
                }
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.skillsTake20)) {
                    __result = RuleRollD20.FromInt(__instance.Initiator, 20);
                    return false;
                }
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.skillsTake10)) {
                    __result = RuleRollD20.FromInt(__instance.Initiator, 10);
                    return false;
                }
                return true;
            }
        }
    }
}
