// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Items;
using Kingmaker.RandomEncounters;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using Kingmaker.View.Spawners;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
using Logger = ModKit.Logger;

namespace ToyBox.BagOfPatches
{
    static class Multipliers
    {
        public static Settings settings = Main.settings;

        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;

        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(EncumbranceHelper), "GetHeavy")]
        static class EncumbranceHelper_GetHeavy_Patch
        {
            static void Postfix(ref int __result)
            {
                __result = Mathf.RoundToInt(__result * settings.encumberanceMultiplier);
            }
        }

        [HarmonyPatch(typeof(UnitPartWeariness), "GetFatigueHoursModifier")]
        static class EncumbranceHelper_GetFatigueHoursModifier_Patch
        {
            static void Postfix(ref float __result)
            {
                __result *= (float)Math.Round(settings.fatigueHoursModifierMultiplier, 1);
            }
        }

        [HarmonyPatch(typeof(Player), "GainPartyExperience")]
        public static class Player_GainPartyExperience_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref int gained)
            {
                gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplier, 1));

                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "GainMoney")]
        public static class Player_GainMoney_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref long amount)
            {
                amount = Mathf.RoundToInt(amount * (float)Math.Round(settings.moneyMultiplier, 1));

                return true;
            }
        }

        [HarmonyPatch(typeof(Spellbook), "GetSpellsPerDay")]
        static class Spellbook_GetSpellsPerDay_Patch
        {
            static void Postfix(ref int __result)
            {
                __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.spellsPerDayMultiplier, 1));
            }
        }

        [HarmonyPatch(typeof(Player), "GetCustomCompanionCost")]
        public static class Player_GetCustomCompanionCost_Patch
        {
            public static bool Prefix(ref bool __state)
            {
                return !__state;
            }

            public static void Postfix(ref int __result)
            {
                __result = Mathf.RoundToInt(__result * settings.companionCostMultiplier);
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), "GetRegionalModifier", new Type[] { })]
        public static class MovementSpeed_GetRegionalModifier_Patch1
        {
            public static void Postfix(ref float __result)
            {
                float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), "GetRegionalModifier", typeof(Vector3))]
        public static class MovementSpeed_GetRegionalModifier_Patch2
        {
            public static void Postfix(ref float __result)
            {
                float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementUtility), "MoveAlongEdge", typeof(GlobalMapState), typeof(GlobalMapView), typeof(IGlobalMapTraveler), typeof(float))]
        public static class GlobalMapMovementUtility_MoveAlongEdge_Patch
        {
            public static void Prefix(GlobalMapState state,
                                      GlobalMapView view,
                                      IGlobalMapTraveler traveler,
                                      ref float visualStepDistance)
            {
                // TODO - can we get rid of the other map movement multipliers and do them all here?
                if (traveler is GlobalMapArmyState armyState && armyState.Data.Faction == ArmyFaction.Crusaders)
                {
                    float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                    visualStepDistance = speedMultiplier * visualStepDistance;
                }
            }
        }

        [HarmonyPatch(typeof(GlobalMapArmyState), "SpendMovementPoints", typeof(float))]
        public static class GlobalMapArmyState_SpendMovementPoints_Patch
        {
            public static void Prefix(GlobalMapArmyState __instance, ref float points)
            {
                if (__instance.Data.Faction == ArmyFaction.Crusaders)
                {
                    float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                    points /= speedMultiplier;
                }
            }
        }

        [HarmonyPatch(typeof(BuffCollection), "AddBuff", typeof(BlueprintBuff), typeof(UnitEntityData), typeof(TimeSpan?), typeof(AbilityParams))]
        public static class BuffCollection_AddBuff_patch
        {
            public static void Prefix(BlueprintBuff blueprint, UnitEntityData caster, ref TimeSpan? duration, [CanBeNull] AbilityParams abilityParams = null)
            {
                try
                {
                    if (!caster.IsPlayersEnemy && duration != null)
                    {
                        duration = TimeSpan.FromTicks(Convert.ToInt64(duration.Value.Ticks * settings.buffDurationMultiplierValue));
                    }
                }
                catch (Exception e)
                {
                    modLogger.Log(e.ToString());
                }

                Main.Debug("Initiator: " + caster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration);
            }
        }

        [HarmonyPatch(typeof(BuffCollection), "AddBuff", typeof(BlueprintBuff), typeof(MechanicsContext), typeof(TimeSpan?))]
        public static class BuffCollection_AddBuff2_patch
        {
            public static void Prefix(BlueprintBuff blueprint, MechanicsContext parentContext, ref TimeSpan? duration)
            {
                try
                {
                    if (parentContext.MaybeCaster?.IsPlayersEnemy == false && duration != null)
                    {
                        duration = TimeSpan.FromTicks(Convert.ToInt64(duration.Value.Ticks * settings.buffDurationMultiplierValue));
                    }
                }
                catch (Exception e)
                {
                    modLogger.Log(e.ToString());
                }

                Main.Debug(string.Format("Initiator: {0}\nBlueprintBuff: {1}\nDuration: {2}", parentContext.MaybeCaster?.CharacterName, blueprint.Name,
                                         duration));
            }
        }

        [HarmonyPatch(typeof(RandomEncounterUnitSelector), "PlaceUnits")]
        internal static class RandomEncounterUnitSelector_PlaceUnits_Patch
        {
            private static void Postfix(ref IList<UnitEntityData> units)
            {
                foreach (UnitEntityData unit in units.Where(unit => unit.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction)))
                {
                    Main.Debug("RandomEncounterUnitSelector.PlaceUnits: " + unit.CharacterName);
                    unit.Stats.HitPoints.BaseValue = Mathf.RoundToInt(unit.Stats.HitPoints.BaseValue * settings.enemyBaseHitPointsMultiplier);
                }
            }
        }

        [HarmonyPatch(typeof(UnitSpawnerBase), "Spawn")]
        internal static class UnitSpawner_Spawn_Patch
        {
            private static void Postfix(ref UnitEntityData __result)
            {
                if (__result != null && __result.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction))
                {
                    Main.Debug("UnitSpawner.Spawn: " + __result.CharacterName);
                    __result.Stats.HitPoints.BaseValue = Mathf.RoundToInt(__result.Stats.HitPoints.BaseValue * settings.enemyBaseHitPointsMultiplier);
                }
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemSellPrice", typeof(ItemEntity))]
        static class VendorLogic_GetItemSellPrice_Patch
        {
            private static void Postfix(ref long __result)
            {
                __result = (long)(__result * settings.vendorSellPriceMultiplier);
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemSellPrice", typeof(BlueprintItem))]
        static class VendorLogic_GetItemSellPrice_Patch2
        {
            private static void Postfix(ref long __result)
            {
                __result = (long)(__result * settings.vendorSellPriceMultiplier);
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemBuyPrice", typeof(ItemEntity))]
        static class VendorLogic_GetItemBuyPrice_Patch
        {
            private static void Postfix(ref long __result)
            {
                __result = (long)(__result * settings.vendorBuyPriceMultiplier);
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemBuyPrice", typeof(BlueprintItem))]
        static class VendorLogic_GetItemBuyPrice_Patc2h
        {
            private static void Postfix(ref long __result)
            {
                __result = (long)(__result * settings.vendorBuyPriceMultiplier);
            }
        }

        [HarmonyPatch(typeof(CameraZoom), "TickZoom")]
        static class CameraZoom_TickZoom
        {
            static bool firstCall = true;

            static float BaseFovMin = 17.5f;

            static float BaseFovMax = 30;

            public static bool Prefix(CameraZoom __instance)
            {
                if (firstCall)
                {
                    modLogger.Log($"baseMin/Max: {__instance.FovMin} {__instance.FovMax}");

                    if (!Mathf.Approximately(__instance.FovMin, BaseFovMin))
                    {
                        modLogger.Log(string.Format("Warning: game has changed FovMin to {0} vs {1}. Toy Box should be updated to avoid stability issues " +
                                                    "when enabling and disabling the mod repeatedly", __instance.FovMin, BaseFovMin).orange().bold());
                    }

                    if (!Mathf.Approximately(__instance.FovMax, BaseFovMax))
                    {
                        modLogger.Log(string.Format("Warning: game has changed FovMax to {0} vs {1}. Toy Box should be updated to avoid stability issues" +
                                                    " when enabling and disabling the mod repeatedly", __instance.FovMax, BaseFovMax).orange().bold());
                    }

                    firstCall = false;
                }

                __instance.FovMax = BaseFovMax * settings.fovMultiplier;
                __instance.FovMin = BaseFovMin / settings.fovMultiplier;

                if (__instance.m_ZoomRoutine == null)
                {
                    if (!__instance.IsScrollBusy && Game.Instance.IsControllerMouse)
                    {
                        __instance.m_PlayerScrollPosition += __instance.IsOutOfScreen ? 0.0f : Input.GetAxis("Mouse ScrollWheel");
                    }

                    __instance.m_ScrollPosition = __instance.m_PlayerScrollPosition;
                    __instance.m_ScrollPosition = Mathf.Clamp(__instance.m_ScrollPosition, 0.0f, __instance.m_ZoomLenght);

                    __instance.m_SmoothScrollPosition = Mathf.Lerp(__instance.m_SmoothScrollPosition, __instance.m_ScrollPosition,
                                                                   Time.unscaledDeltaTime * __instance.m_Smooth);

                    __instance.m_Camera.fieldOfView = Mathf.Lerp(__instance.FovMax, __instance.FovMin, __instance.CurrentNormalizePosition);
                    __instance.m_PlayerScrollPosition = __instance.m_ScrollPosition;
                }

                return true;
            }
        }
    }
}