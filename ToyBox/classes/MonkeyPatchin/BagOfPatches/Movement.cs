// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Armies.TacticalCombat.Parts;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using ModKit;
using System;
using System.Linq;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Movement {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(UnitEntityData), nameof(UnitEntityData.ModifiedSpeedMps), MethodType.Getter)]
        public static class UnitEntityData_CalculateSpeedModifier_Patch {
            private static void Postfix(UnitEntityData __instance, ref float __result) {
                //Main.Log($"UnitEntityData_CalculateSpeedModifier_Patch: isInParty:{__instance.Descriptor.IsPartyOrPet()} result:{__result}".cyan());
                if (settings.partyMovementSpeedMultiplier == 1.0f || !__instance.Descriptor.IsPartyOrPet())
                    return;
                var partTacticalCombat = __instance.Get<UnitPartTacticalCombat>();
                if (partTacticalCombat != null && partTacticalCombat.Faction != ArmyFaction.Crusaders) return;
                __result *= settings.partyMovementSpeedMultiplier;
                //Main.Log($"finalREsult: {__result}".cyan());

            }
        }

        [HarmonyPatch(typeof(ClickGroundHandler), nameof(ClickGroundHandler.RunCommand))]
        public static class ClickGroundHandler_RunCommand_Patch {
            private static UnitMoveTo unitMoveTo = null;
            public static bool Prefix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings) {
                var moveAsOne = Main.settings.toggleMoveSpeedAsOne;
                //Main.Log($"ClickGroundHandler_RunCommand_Patch - isInCombat: {unit.IsInCombat} turnBased:{Game.Instance.Player.IsTurnBasedModeOn()} moveAsOne:{moveAsOne}");
                if (unit.IsInCombat && Game.Instance.Player.IsTurnBasedModeOn()) return true;

                // As of WoTR 1.03c RunCommand is once again the main place to adjust movement speed.  The following was needed when we used UnitEntityData_CalculateSpeedModifier_Patch above to adjust speed in non move as one cases.  
                if (!moveAsOne) {
                    return true;
                }
                var partTacticalCombat = unit.Get<UnitPartTacticalCombat>();
                if (partTacticalCombat != null && partTacticalCombat.Faction != ArmyFaction.Crusaders) return true;

                var speedLimit = moveAsOne ? UnitEntityDataUtils.GetMaxSpeed(Game.Instance.UI.SelectionManager.SelectedUnits) : unit.ModifiedSpeedMps;
                Mod.Trace($"RunCommand - moveAsOne: {moveAsOne} speedLimit: {speedLimit} selectedUnits: {string.Join(" ", Game.Instance.UI.SelectionManager.SelectedUnits.Select(u => $"{u.CharacterName} {u.ModifiedSpeedMps}"))}");
                speedLimit *= Main.settings.partyMovementSpeedMultiplier;

                unitMoveTo = new UnitMoveTo(settings.Destination, 0.3f) {
                    MovementDelay = settings.Delay,
                    Orientation = new float?(settings.Orientation),
                    CreatedByPlayer = true
                };
                if (BuildModeUtility.IsDevelopment) {
                    if (CheatsAnimation.SpeedForce > 0f) {
                        unitMoveTo.OverrideSpeed = new float?(CheatsAnimation.SpeedForce);
                    }
                    unitMoveTo.MovementType = (UnitAnimationActionLocoMotion.WalkSpeedType)CheatsAnimation.MoveType.Get();
                }
                unitMoveTo.SpeedLimit = speedLimit;
                unitMoveTo.ApplySpeedLimitInCombat = settings.ApplySpeedLimitInCombat;
                unitMoveTo.OverrideSpeed = speedLimit * 1.5f;
                unit.Commands.Run(unitMoveTo);
                if (unit.Commands.Queue.FirstOrDefault((UnitCommand c) => c is UnitMoveTo) == unitMoveTo || Game.Instance.IsPaused) {
                    ClickGroundHandler.ShowDestination(unit, unitMoveTo.Target, false);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), nameof(GlobalMapMovementController.GetRegionalModifier), new Type[] { })]
        public static class MovementSpeed_GetRegionalModifier_Patch1 {
            public static void Postfix(ref float __result) {
                var speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), nameof(GlobalMapMovementController.GetRegionalModifier), new Type[] { typeof(Vector3) })]
        public static class MovementSpeed_GetRegionalModifier_Patch2 {
            public static void Postfix(ref float __result) {
                var speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        /**
            GlobalMapState state,
            GlobalMapView view,
            IGlobalMapTraveler traveler,
            float visualStepDistance)
        */
        [HarmonyPatch(typeof(GlobalMapMovementUtility), nameof(GlobalMapMovementUtility.MoveAlongEdge), new Type[] {
            typeof(GlobalMapState), typeof(GlobalMapView), typeof(IGlobalMapTraveler), typeof(float)
            })]
        public static class GlobalMapMovementUtility_MoveAlongEdge_Patch {
            public static void Prefix(
                GlobalMapState state,
                GlobalMapView view,
                IGlobalMapTraveler traveler,
                ref float visualStepDistance) {
                // TODO - can we get rid of the other map movement multipliers and do them all here?
                if (traveler is GlobalMapArmyState armyState && armyState.Data.Faction == ArmyFaction.Crusaders) {
                    var speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                    visualStepDistance = speedMultiplier * visualStepDistance;
                }
            }
        }

        [HarmonyPatch(typeof(GlobalMapArmyState), nameof(GlobalMapArmyState.SpendMovementPoints), new Type[] { typeof(float) })]
        public static class GlobalMapArmyState_SpendMovementPoints_Patch {
            public static void Prefix(GlobalMapArmyState __instance, ref float points) {
                if (__instance.Data.Faction == ArmyFaction.Crusaders) {
                    var speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                    points /= speedMultiplier;
                }
            }
        }
    }
}
