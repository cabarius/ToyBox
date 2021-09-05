// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using System;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches
{
    static class DiceRolls
    {
        public static Settings settings = Main.settings;

        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;

        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(RuleAttackRoll), "IsCriticalConfirmed", MethodType.Getter)]
        static class HitPlayer_OnTriggerl_Patch
        {
            static void Postfix(ref bool __result, RuleAttackRoll __instance)
            {
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.allHitsCritical))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(RuleRollDice), "Roll")]
        public static class RuleRollDice_Roll_Patch
        {
            static void Postfix(RuleRollDice __instance)
            {
                if (__instance.DiceFormula.Dice != DiceType.D20)
                {
                    return;
                }

                var initiator = __instance.Initiator;
                int result = __instance.m_Result;
                Main.Debug("Initial D20Roll: " + result);

                if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll20) 
                    || UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll20OutOfCombat) 
                    && !initiator.IsInCombat
                )
                {
                    result = 20;
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll1))
                {
                    result = 1;
                }
                else
                {
                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.rollWithAdvantage))
                    {
                        result = Math.Max(result, UnityEngine.Random.Range(1, 21));
                    }
                    else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.rollWithDisadvantage))
                    {
                        result = Math.Min(result, UnityEngine.Random.Range(1, 21));
                    }

                    int min = 1;

                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.neverRoll1) && result == 1)
                    {
                        result = UnityEngine.Random.Range(2, 21);
                        min = 2;
                    }

                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.neverRoll20) && result == 20)
                    {
                        result = UnityEngine.Random.Range(min, 20);
                    }
                }

                Main.Debug("Modified D20Roll: " + result);
                __instance.m_Result = result;
            }
        }

        [HarmonyPatch(typeof(RuleInitiativeRoll), "Result", MethodType.Getter)]
        public static class RuleInitiativeRoll_OnTrigger_Patch
        {
            static void Postfix(RuleInitiativeRoll __instance, ref int __result)
            {
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.roll1Initiative))
                {
                    __result = 1;
                    Main.Debug("Modified InitiativeRoll: " + __result);
                }

                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.roll20Initiative))
                {
                    __result = 20;
                    Main.Debug("Modified InitiativeRoll: " + __result);
                }
            }
        }
    }
}