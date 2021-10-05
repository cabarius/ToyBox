﻿using HarmonyLib;
using Kingmaker.Armies;
using Kingmaker.Armies.State;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Armies.TacticalCombat.Parts;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Armies;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Buffs;
using Kingmaker.Kingdom.Flags;
using Kingmaker.Kingdom.Rules;
using System;
using UnityEngine;

namespace ToyBox.classes.MonkeyPatchin.BagOfPatches {
    public static class Crusade {
        public static Settings Settings = Main.settings;

        [HarmonyPatch(typeof(ArmyMercenariesManager), "Reroll")]
        public static class ArmyMercenariesManager_Reroll_Patch {
            public static void Prefix(ref ArmyMercenariesManager __instance, ref int __state) {
                if (Settings.toggleInfiniteArmyRerolls) {
                    __state = __instance.FreeRerollsLeftCount;
                    __instance.FreeRerollsLeftCount = 99;
                }
            }

            public static void Postfix(ref ArmyMercenariesManager __instance, int __state) {
                if (Settings.toggleInfiniteArmyRerolls) {
                    __instance.FreeRerollsLeftCount = __state;
                }
            }
        }

        [HarmonyPatch(typeof(TacticalCombatHelper), "GetSpellPower")]
        public static class TacticalCombatHelper_GetSpellPower_Patch {
            public static void Postfix(ref int __result, UnitEntityData leader) {
                var leaderPowerMultiplier = leader.Get<UnitPartLeaderTacticalCombat>()?.LeaderData.Faction != ArmyFaction.Crusaders
                    ? Settings.enemyLeaderPowerMultiplier
                    : Settings.playerLeaderPowerMultiplier;

                __result = Mathf.RoundToInt(__result * leaderPowerMultiplier);
            }
        }

        [HarmonyPatch(typeof(ArmyData), "MaxSquadsCount", MethodType.Getter)]
        public static class ArmyData_MaxSquadsCount_Patch {
            public static void Postfix(ref ArmyModifiableValue __result, ArmyData __instance) {
                if (Settings.toggleLargeArmies && __result != null && __result.ModifiedValue != 0 && __instance.Faction == ArmyFaction.Crusaders) {
                    __result.MaxValue = ArmyData.PositionsCount;
                    __result.MinValue = ArmyData.PositionsCount;
                    __result.m_BaseValue = __result.MaxValue;
                    __result.ModifiedValue = __result.MaxValue;
                    BlueprintRoot.Instance.Kingdom.StartArmySquadsCount = ArmyData.PositionsCount;
                    BlueprintRoot.Instance.Kingdom.MaxArmySquadsCount = ArmyData.PositionsCount;
                }
            }
        }

        [HarmonyPatch(typeof(ArmyData))]
        public static class ArmyData_CalculateExperience_Patch {
            [HarmonyPatch("CalculateExperience")]
            [HarmonyPostfix]
            public static void Postfix(ref int __result) => __result = Mathf.RoundToInt(__result * (float)Math.Round(Settings.armyExperienceMultiplier, 1));

            [HarmonyPrefix]
            [HarmonyPatch("CalculateDangerRating")]
            public static bool PrefixCalculateDangerRating(ArmyData __instance, ref int __result) {
                var armyRoot = BlueprintRoot.Instance.ArmyRoot;
                var num1 = Mathf.FloorToInt((float)(__instance.CalculateExperience() / Math.Round(Settings.experienceMultiplier, 1) + armyRoot.ArmyDangerBonus) * armyRoot.ArmyDangerMultiplier);
                if (num1 < 0) {
                    __result = 1;
                    return false;
                }

                var num2 = 0;
                var bonuses = BlueprintRoot.Instance.LeadersRoot.ExpTable.Bonuses;
                for (var index = 0; index < bonuses.Length && bonuses[index] <= num1; ++index)
                    ++num2;
                __result = num2;
                return false;
            }
        }

        [HarmonyPatch(typeof(SummonUnitsAfterArmyBattle), "HandleArmiesBattleResultsApplied")]
        public static class SummonUnitsAfterArmyBattle_Patch {
            public static void Prefix(ref TacticalCombatResults results) => results = new TacticalCombatResults(results.Attacker, results.Defender, Mathf.RoundToInt(results.BattleExp * Settings.postBattleSummonMultiplier),
                    results.CrusadeStatsBonus, results.Winner, results.ToResurrect, results.Units, results.Retreat);
        }

        [HarmonyPatch(typeof(MercenarySlot), "Price", MethodType.Getter)]
        private static class MercenarySlot_Price_Patch {
            private static void Postfix(ref KingdomResourcesAmount __result) {
                __result *= Settings.recruitmentCost;
                if (!__result.IsPositive) {
                    __result = KingdomResourcesAmount.Zero;
                }
            }
        }

        [HarmonyPatch(typeof(RuleCalculateUnitRecruitingCost), "ResultCost", MethodType.Getter)]
        private static class RuleCalculateUnitRecruitingCost_ResultCost_Patch {
            private static void Postfix(ref KingdomResourcesAmount __result) {
                var finances = __result.m_Finances > 0 ? Mathf.RoundToInt(Math.Max(1, Settings.recruitmentCost * __result.m_Finances)) : 0;
                var materials = __result.m_Materials > 0 ? Mathf.RoundToInt(Math.Max(1, Settings.recruitmentCost * __result.m_Materials)) : 0;
                var favors = __result.m_Favors > 0 ? Mathf.RoundToInt(Math.Max(1, Settings.recruitmentCost * __result.m_Favors)) : 0;

                __result = new KingdomResourcesAmount { m_Favors = favors, m_Finances = finances, m_Materials = materials };
            }
        }

        [HarmonyPatch(typeof(ArmyRecruitsManager), "Increase")]
        private static class ArmyRecruitsManager_Patch {
            private static void Prefix(ref int count) => count = Mathf.RoundToInt(count * Settings.recruitmentMultiplier);
        }

        [HarmonyPatch(typeof(ArmyMercenariesManager), "Recruit")]
        private static class ArmyMercenariesManager_Recruit_Patch {
            private static void Prefix(ref MercenarySlot slot) => slot.Recruits.Count = Mathf.RoundToInt(slot.Recruits.Count * Settings.recruitmentMultiplier);
        }

        [HarmonyPatch(typeof(KingdomMoraleFlag), "ChangeDaysLeft")]
        private static class KingdomMoraleFlag_Patch {
            private static void Prefix(ref int daysDelta) {
                if (Settings.toggleCrusadeFlagsStayGreen && daysDelta < 0) {
                    daysDelta = 0;
                }
            }
        }
    }
}
