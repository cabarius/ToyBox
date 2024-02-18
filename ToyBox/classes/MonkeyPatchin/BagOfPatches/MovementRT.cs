#nullable enable annotations
﻿// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap.View;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using ModKit;
using System;
using System.Linq;
using Kingmaker.Pathfinding;
using Kingmaker.UnitLogic;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Movement {
        public static Settings Settings = Main.Settings;
        public static Player Player = Game.Instance.Player;


        [HarmonyPatch(typeof(PartMovable))]
        public static class PartMoveablePatch {
            [HarmonyPatch(nameof(PartMovable.ModifiedSpeedMps), MethodType.Getter)]
            [HarmonyPostfix]
            private static void ModifiedSpeedMps(PartMovable __instance, ref float __result) {
                if (Settings.partyMovementSpeedMultiplier == 1.0f) return;
                if (!(__instance.Owner is UnitEntity unit)) return;
                if (!unit.CombatGroup.IsPlayerParty) return;
                //Mod.Debug($"PartMovable.ModifiedSpeedMps - owner:{unit.CharacterName} old: {__result}");
                // TODO: deal with space movement
                __result *= Settings.partyMovementSpeedMultiplier;
            }
        }

        [HarmonyPatch(typeof(UnitHelper))]
        public static class UnitHelperPatch {
            [HarmonyPatch(nameof(UnitHelper.CreateMoveCommandUnit))]
            [HarmonyPostfix]
            public static void CreateMoveCommandUnit(
                    BaseUnitEntity unit,
                    MoveCommandSettings settings,
                    float[] costPerEveryCell,
                    ForcedPath forcedPath,
                    ref UnitMoveToProperParams __result

                ) {
                if (Settings.partyMovementSpeedMultiplier == 1.0f) return;
                if (!unit.CombatGroup.IsPlayerParty) return;
                __result.SpeedLimit = settings.SpeedLimit * Settings.partyMovementSpeedMultiplier;
                __result.OverrideSpeed = 5 * Settings.partyMovementSpeedMultiplier;
            }

            [HarmonyPatch(nameof(UnitHelper.CreateMoveCommandShip))]
            [HarmonyPostfix]
            public static void CreateMoveCommandShip(
                    MoveCommandSettings settings,
                    int straightDistance,
                    int diagonalsCount,
                    int length,
                    ForcedPath forcedPath,
                    ref UnitMoveToProperParams __result
                ) {
                __result.SpeedLimit = settings.SpeedLimit * Settings.partyMovementSpeedMultiplier;
                __result.OverrideSpeed = 5 * Settings.partyMovementSpeedMultiplier;
            }

            [HarmonyPatch(nameof(UnitHelper.CreateMoveCommandParamsRT))]
            [HarmonyPostfix]
            public static void CreateMoveCommandParamsRT(
                    BaseUnitEntity unit,
                    MoveCommandSettings settings,
                    ref UnitMoveToParams __result
                ) {
                if (Settings.partyMovementSpeedMultiplier == 1.0f) return;
                if (!unit.CombatGroup.IsPlayerParty) return;
                __result.SpeedLimit = settings.SpeedLimit * Settings.partyMovementSpeedMultiplier;
                __result.OverrideSpeed = 5 * Settings.partyMovementSpeedMultiplier;
            }
        }
#if false
        [HarmonyPatch(typeof(ClickGroundHandler), nameof(ClickGroundHandler.RunCommand))]
        public static class ClickGroundHandler_RunCommand_Patch {
            private static UnitMoveTo unitMoveTo = null;
            public static bool Prefix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings) {
                var moveAsOne = Main.Settings.toggleMoveSpeedAsOne;
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
                speedLimit *= Main.Settings.partyMovementSpeedMultiplier;

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
                var speedMultiplier = Mathf.Clamp(Settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), nameof(GlobalMapMovementController.GetRegionalModifier), new Type[] { typeof(Vector3) })]
        public static class MovementSpeed_GetRegionalModifier_Patch2 {
            public static void Postfix(ref float __result) {
                var speedMultiplier = Mathf.Clamp(Settings.travelSpeedMultiplier, 0.1f, 100f);
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
                    var speedMultiplier = Mathf.Clamp(Settings.travelSpeedMultiplier, 0.1f, 100f);
                    visualStepDistance = speedMultiplier * visualStepDistance;
                }
            }
        }

        [HarmonyPatch(typeof(GlobalMapArmyState), nameof(GlobalMapArmyState.SpendMovementPoints), new Type[] { typeof(float) })]
        public static class GlobalMapArmyState_SpendMovementPoints_Patch {
            public static void Prefix(GlobalMapArmyState __instance, ref float points) {
                if (__instance.Data.Faction == ArmyFaction.Crusaders) {
                    var speedMultiplier = Mathf.Clamp(Settings.travelSpeedMultiplier, 0.1f, 100f);
                    points /= speedMultiplier;
                }
            }
        }
#endif
    }
}
