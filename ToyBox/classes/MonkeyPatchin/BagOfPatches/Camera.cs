// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using System;
using System.Linq;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.Settings;
using Kingmaker.Settings.Difficulty;
using ModKit;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Utility;
using System.Collections.Generic;
using CameraMode = Kingmaker.View.CameraMode;
using DG.Tweening;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;

namespace ToyBox.BagOfPatches {
    internal static class CameraPatches {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.Update))]
        static class CameraRig_Update_Patch {
            static void Postfix(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (settings.toggleRotateOnAllMaps || Main.resetExtraCameraAngles)
                    __instance.TickRotate();
                if (settings.toggleZoomOnAllMaps)
                    __instance.CameraZoom.TickZoom();
                if (settings.toggleScrollOnAllMaps) {
                    __instance.TickScroll();
                    //__instance.TickCameraDrag();
                    //__instance.CameraDragToMove();
                }
            }
        }

        //[HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickScroll))]
        //private static class CameraRig_TickScroll_Patch {
        //    public static void Postfix(CameraRig __instance) {
        //    }
        //}

        [HarmonyPatch(typeof(CameraZoom), nameof(CameraZoom.TickZoom))]
        private static class CameraZoom_TickZoom_Patch {
            private static bool firstCall = true;
            private static float BaseFovMin => settings.toggleZoomOnAllMaps ? 12 : 17.5f;
            private static readonly float BaseFovMax = 30;
            private static float FovMin => BaseFovMin / settings.fovMultiplier;
            private static float FovMax => BaseFovMax * settings.fovMultiplier;
            private static bool Prefix(CameraZoom __instance,
                        Coroutine ___m_ZoomRoutine,
                        float ___m_Smooth,
                        ref float ___m_PlayerScrollPosition,
                        ref float ___m_ScrollPosition,
                        ref float ___m_SmoothScrollPosition,
                        Camera ___m_Camera,
                        float ___m_ZoomLenght) {
                if (settings.fovMultiplier == 1) return true;
                if (!__instance.IsScrollBusy && Game.Instance.IsControllerMouse && (double)Input.GetAxis("Mouse ScrollWheel") != 0.0 && ((double)___m_Camera.fieldOfView > (double)FovMin || (double)Input.GetAxis("Mouse ScrollWheel") < 0.0)) {
                    ___m_PlayerScrollPosition += __instance.IsOutOfScreen ? 0.0f : Input.GetAxis("Mouse ScrollWheel");
                    if ((double)___m_PlayerScrollPosition <= 0.0)
                        ___m_PlayerScrollPosition = 0.01f;
                }
                if ((double)___m_PlayerScrollPosition <= 0.0) {
                    ___m_PlayerScrollPosition = (float)(((double)FovMax - (double)FovMin) / 18.0);
                }
                ___m_ScrollPosition = ___m_PlayerScrollPosition;
                ___m_SmoothScrollPosition = Mathf.Lerp(___m_SmoothScrollPosition, ___m_PlayerScrollPosition, Time.unscaledDeltaTime * ___m_Smooth);
                ___m_Camera.fieldOfView = Mathf.Lerp(FovMax, FovMin, (float)((double)__instance.CurrentNormalizePosition * ((double)__instance.FovMax - (double)__instance.FovMin) / ((double)FovMax - (double)FovMin)));
                ___m_PlayerScrollPosition = ___m_ScrollPosition;
                return false;
            }
        }

        private static Vector2 CameraDragToRotate2D(this CameraRig __instance) {
            if (!__instance.m_BaseMousePoint.HasValue)
                return new Vector2(0.0f, 0.0f);
            var basePoint = __instance.m_BaseMousePoint.Value;
            var dx = basePoint.x - __instance.GetLocalPointerPosition().x;
            var dy = basePoint.y - __instance.GetLocalPointerPosition().y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            var rotateDistance = ((double)__instance.m_RotateDistance - (double)dist) * ((bool)(SimpleBlueprint)BlueprintRoot.Instance ? (double)SettingsRoot.Controls.CameraRotationSpeedEdge.GetValue() : 2.0);
            __instance.m_RotateDistance = (float)rotateDistance;
            basePoint.x -= 0.25f * dx;
            basePoint.y -= 0.25f * dy;
            __instance.m_BaseMousePoint = basePoint;
            return new Vector2(dx, dy);
        }

        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickRotate))]
        private static class CameraRig_TickRotate_Patch {

            public static bool Prefix(CameraRig __instance) {
                if (!settings.toggleRotateOnAllMaps && !settings.toggleCameraPitch && !Main.resetExtraCameraAngles && !settings.toggleInvertXAxis && !settings.toggleInvertKeyboardXAxis) return true;
                bool usePitch = settings.toggleCameraPitch;
                if (__instance.m_RotateRoutine != null && (double)Time.time > (double)__instance.m_RotateRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_RotateRoutine);
                    __instance.m_RotateRoutine = (Coroutine)null;
                }

                if (__instance.m_ScrollRoutine != null || __instance.m_RotateRoutine != null)// || __instance.m_HandRotationLock)
                    return false;
                __instance.RotateByMiddleButton();
                var mouseMovement = new Vector2(0, 0);
                float xRotationSign = 1;
                if (__instance.m_RotationByMouse) {
                    if (!settings.toggleInvertXAxis) xRotationSign = -1;
                    mouseMovement = __instance.CameraDragToRotate2D();
                }
                else if (__instance.m_RotationByKeyboard) {
                    mouseMovement.x = __instance.m_RotateOffset;
                    if (settings.toggleInvertKeyboardXAxis) xRotationSign = -1;
                }
                if (__instance.m_RotationByMouse || __instance.m_RotationByKeyboard || Main.resetExtraCameraAngles) {
                    var eulerAngles = __instance.transform.rotation.eulerAngles;
                    eulerAngles.y += xRotationSign * mouseMovement.x * __instance.m_RotationSpeed * CameraRig.ConsoleRotationMod;
                    if (usePitch && !Main.resetExtraCameraAngles) {
                        eulerAngles.x += (settings.toggleInvertYAxis ? 1 : -1) * mouseMovement.y * __instance.m_RotationSpeed * CameraRig.ConsoleRotationMod;
                        //Mod.Debug($"eulerX: {eulerAngles.x}");
                    }
                    else {
                        eulerAngles.x = 0f;
                        Main.resetExtraCameraAngles = false;
                    }
                    __instance.transform.DOKill();
                    __instance.transform.DOLocalRotate(eulerAngles, __instance.m_RotationTime).SetUpdate<Tweener>(true);
                }

                __instance.m_RotationByKeyboard = false;
                __instance.m_RotateOffset = 0.0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.SetMode))]
        private static class CameraRig_SetMode_Apply {
            public static void Postfix(CameraRig __instance, CameraMode mode) {
                if (settings.fovMultiplierCutScenes == 1 && settings.fovMultiplier == 1) return;
                if (mode == CameraMode.Default && Game.Instance.CurrentMode == GameModeType.Cutscene) {
                    __instance.Camera.fieldOfView = __instance.CameraZoom.FovMax * settings.fovMultiplierCutScenes / settings.fovMultiplier;
                }
            }
        }

#if false
        [HarmonyPatch(typeof(BlueprintArea), nameof(BlueprintArea.CameraMode), MethodType.Getter)]
        static class BlueprintArea_CameraMode_Patch {
            public static void Postfix(BlueprintArea __instance, CameraMode __result) {
                Main.Log("hi");
                __result = CameraMode.Default;
            }
        }
#endif
    }
}
