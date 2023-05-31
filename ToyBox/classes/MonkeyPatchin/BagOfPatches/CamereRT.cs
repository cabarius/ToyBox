// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.View;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.Settings;
using ModKit;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Owlcat.Runtime.Visual.RenderPipeline;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.OccludedObjectHighlighting;
using UnityEngine.EventSystems;
using Kingmaker.Controllers.Clicks;
using Kingmaker.UI.Models.SettingsUI.SettingAssets;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows;
using Kingmaker.Controllers.Rest;
using Kingmaker.Settings.Entities;
using Kingmaker.UI.Models;
using ModKit.Utility;
using TMPro;

namespace ToyBox.BagOfPatches {
    internal static class CameraPatches {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;
        private static float CameraElevation = 0f;

        [HarmonyPatch(typeof(CameraController))]
        private static class CameraControllerPatch {
            [HarmonyPatch(nameof(CameraController.Tick))]
            [HarmonyPrefix]
            public static bool Tick(CameraController __instance) {
                var instance = CameraRig.Instance;
                if (instance == null || (bool)instance.FixCamera)
                    return false;
                if (Settings.toggleCameraPitch) {
                    instance.m_EnableOrbitCamera = true;
                    //Mod.Log($"{instance.MinSpaceCameraAngle} {instance.MaxSpaceCameraAngle}");
                    //Mod.Log($"Euler angles: {instance.transform.eulerAngles}");
                    instance.MinSpaceCameraAngle = -47; // -15
                    instance.MaxSpaceCameraAngle = +120; // +20 
                }
                else {
                    instance.MinSpaceCameraAngle = -15;
                    instance.MaxSpaceCameraAngle = +20;

                }
                if (__instance.m_AllowScroll ||Settings.toggleScrollOnAllMaps) {
                    __instance.Follower.TryFollow();
                    instance.TickScroll();
                }
                if (__instance.m_AllowRotate || Settings.toggleRotateOnAllMaps || Main.resetExtraCameraAngles) {
                    instance.TickRotate();
                }
                if (__instance.m_AllowZoom || Settings.toggleZoomOnAllMaps)
                    instance.CameraZoom.TickZoom();
                instance.TickShake();
                return false;
            }
        }
        
        [HarmonyPatch(typeof(CameraZoom))]
        private static class CameraZoomPatch {
            private static bool firstCall = true;
            private static float BaseFovMin => (Settings.toggleZoomOnAllMaps || Settings.toggleZoomableLocalMaps) ? 12 : 17.5f;
            private static readonly float BaseFovMax = 30;
            private static float FovMin => BaseFovMin / Settings.fovMultiplier;
            private static float FovMax => BaseFovMax * Settings.AdjustedFovMultiplier;

            [HarmonyPatch(nameof(CameraZoom.TickZoom))]
            [HarmonyPrefix]
            private static bool TickZoom(CameraZoom __instance) {
                if (Settings.fovMultiplier == 1 && !Settings.toggleZoomableLocalMaps) return true;

                if (__instance.m_ZoomRoutine != null || __instance.ZoomLock)
                    return false;
                if (!__instance.IsScrollBusy
                    && Game.Instance.IsControllerMouse
                    && !__instance.IsOutOfScreen
                    && !PointerController.InGui)
                    __instance.m_PlayerScrollPosition += Input.GetAxis("Mouse ScrollWheel");
                __instance.m_ScrollPosition = __instance.m_PlayerScrollPosition
                                              + __instance.m_GamepadScrollPosition;
                __instance.m_GamepadScrollPosition = 0.0f;
                __instance.m_ScrollPosition = Mathf.Clamp(__instance.m_ScrollPosition, 0.0f, __instance.ZoomLength);
                __instance.m_SmoothScrollPosition =
                    Mathf.Lerp(__instance.m_SmoothScrollPosition, __instance.m_ScrollPosition, Time.unscaledDeltaTime * __instance.Smoothness);
                __instance.m_Camera.fieldOfView = Mathf.Lerp(FovMax, FovMin, __instance.CurrentNormalizePosition);
                if ((bool)(UnityEngine.Object)__instance.m_VirtualCamera)
                    __instance.m_VirtualCamera.m_Lens.FieldOfView = __instance.m_Camera.fieldOfView;
                if (__instance.EnablePhysicalZoom)
                    __instance.m_Camera.transform.localPosition = new Vector3(
                            __instance.m_Camera.transform.localPosition.x,
                            __instance.m_Camera.transform.localPosition.y,
                            Mathf.Lerp(__instance.PhysicalZoomMin,
                                       __instance.PhysicalZoomMax,
                                       __instance.CurrentNormalizePosition
                                )
                        );
                __instance.m_PlayerScrollPosition = __instance.m_ScrollPosition;
                return false;
            }
        }

        // Camera Rig Helpers
        private static Vector2 CameraDragToRotate2D(this CameraRig __instance) {
            if (!__instance.m_BaseMousePoint.HasValue)
                return Vector2.zero;
            var localPointerPos = __instance.GetLocalPointerPosition();
            var deltaR = new Vector2(__instance.m_BaseMousePoint.Value.x - localPointerPos.x,
                                      __instance.m_BaseMousePoint.Value.y - localPointerPos.y);
            var rotate = (__instance.m_RotateDistance - deltaR)
                         * ((bool)(SimpleBlueprint)BlueprintRoot.Instance
                                ? SettingsRoot.Controls.CameraRotationSpeedEdge.GetValue() * 0.05f
                                : 2f);
            //Mod.Debug($"CameraDragToRotate2D - {__instance.m_BaseMousePoint.Value} => deltaR: {deltaR} - {rotate}");

            __instance.m_RotateDistance = deltaR;
            return rotate;
        }

        [HarmonyPatch(typeof(CameraRig))]
        public static class CameraRigPatch {
            private static UISettingsEntityKeyBinding _followKeyBinding = null;
            private static bool _RotationByMouse = false;
            public static void OnAreaLoad() {
                _followKeyBinding = null;
            }


            [HarmonyPatch(nameof(CameraRig.TickScroll))]
            [HarmonyPrefix]
            public static bool TickScroll(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (!Settings.toggleCameraPitch
                    && !Settings.toggleCameraElevation
                    && !Main.resetExtraCameraAngles
                    && !Settings.toggleFreeCamera
                   )
                    return true;
                var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                if (__instance.m_ScrollRoutine != null && Time.time > (double)__instance.m_ScrollRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_ScrollRoutine);
                    __instance.m_ScrollRoutine = null;
                    var newPosition = __instance.PlaceOnGround(__instance.m_ScrollRoutineEndPos);
                    ___m_TargetPosition = newPosition;
                    __instance.transform.position = newPosition;
                }

                if (__instance.m_HandScrollLock)
                    return false;
                if (__instance.m_ScrollRoutine == null) {
                    var scrollOffset = __instance.m_ScrollOffset;
                    var eulerAngles = __instance.transform.rotation.eulerAngles;
                    //Mod.Log($"eulerAngles: {eulerAngles}");
                    // TODO: find out from OwlCat why these angles are so weird
                    if (eulerAngles.x >= 42.75 && eulerAngles.x <= 312.75) {
                        scrollOffset.y = -scrollOffset.y;
                    }
                    if ((!Application.isEditor || CameraRig.DebugCameraScroll)
                        && !Game.Instance.IsControllerGamepad
                        && (bool)(SettingsEntity<bool>)SettingsRoot.Controls.ScreenEdgeScrolling
                        && __instance.m_FullScreenUIType == 0
                        && Application.isFocused)
                        scrollOffset += __instance.GetCameraScrollShiftByMouse();
                    var scrollVector2 = scrollOffset + __instance.m_ScrollBy2D;
                    __instance.m_ScrollBy2D.Set(0.0f, 0.0f);
                    if (scrollVector2 == Vector2.zero)
                        return false;
                    Game.Instance.CameraController?.Follower?.Release();
                    var scrollMultiplier = Game.Instance.CurrentlyLoadedArea.CameraScrollMultiplier;
                    var scaledScrollVector = scrollVector2 * (__instance.ScrollSpeed * dt * scrollMultiplier * CameraRig.ConsoleScrollMod);
                    __instance.FigureOutScreenBasis();
                    var targetPosition = __instance.TargetPosition;
                    var newTargetPosition = __instance.TargetPosition;
                    newTargetPosition += scaledScrollVector.x * __instance.Right
                                         + scaledScrollVector.y * __instance.Up;
                    if (!__instance.NoClamp && !__instance.m_SkipClampOneFrame)
                        newTargetPosition = __instance.ClampByLevelBounds(newTargetPosition);
                    newTargetPosition = __instance.PlaceOnGround(newTargetPosition);

                    if (__instance.NewBehaviour) {
                        if (!__instance.m_AttackPointPos.HasValue) {
                            __instance.m_AttackPointPos = __instance.Camera.transform.parent.localPosition;
                        }
                        newTargetPosition = __instance.LowerGently(targetPosition, newTargetPosition, dt);
                    }
                    ___m_TargetPosition = newTargetPosition;
                    var yAxis = new Vector3(0, 2, 0);
                    var dPos = scaledScrollVector.x * __instance.Right + scaledScrollVector.y * __instance.Up;
                    var pitch = __instance.transform.eulerAngles.x - 42.75; // TODO: why does this number exist?
                    var pitchRadians = Math.PI * pitch / 180f;
                    if (__instance.m_RotationByMouse)
                        dPos += scaledScrollVector.y * (float)Math.Sin(pitchRadians) * yAxis;
                    //Mod.Debug($"dPos: {dPos} pitch: {pitch:##.000} sine: {Math.Sin(pitchRadians):#.000}");
                    __instance.m_TargetPosition += dPos;
                    if (!Settings.toggleFreeCamera) {
                        if (!__instance.NoClamp && !__instance.m_SkipClampOneFrame)
                            __instance.m_TargetPosition = __instance.ClampByLevelBounds(__instance.m_TargetPosition);
                        __instance.m_TargetPosition = __instance.PlaceOnGround(__instance.m_TargetPosition);
                        if (__instance.NewBehaviour) {
                            if (!__instance.m_AttackPointPos.HasValue)
                                __instance.m_AttackPointPos = __instance.Camera.transform.parent.localPosition;
                            ___m_TargetPosition = __instance.LowerGently(targetPosition, __instance.TargetPosition, dt);
                        }
                    }

                }

                __instance.m_ScrollOffset = Vector2.zero;
                return false;
            }

            [HarmonyPatch(nameof(CameraRig.TickRotate))]
            [HarmonyPrefix]
            public static bool TickRotate(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (!Settings.toggleRotateOnAllMaps
                    && !Settings.toggleCameraPitch 
                    && !Settings.toggleCameraElevation 
                    && !Main.resetExtraCameraAngles 
                    && !Settings.toggleInvertXAxis 
                    && !Settings.toggleInvertKeyboardXAxis
                    ) 
                    return true;
                if (__instance.m_RotateRoutine != null && Time.time > (double)__instance.m_RotateRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_RotateRoutine);
                    __instance.m_RotateRoutine = null;
                }

                if (__instance.m_ScrollRoutine != null 
                    || __instance.m_RotateRoutine != null 
                    || __instance.m_HandRotationLock
                    )
                    return false;
                __instance.RotateByMiddleButton();
                var mouseMovement = Vector2.zero;
                var xRotationSign = 1f;
                var yRotationSign = Settings.toggleInvertYAxis ? 1f : -1f;
                if (__instance.RotationByMouse) {
                    if (!Settings.toggleInvertXAxis) xRotationSign = -1;
                    mouseMovement = __instance.CameraDragToRotate();
                }
                else if (__instance.m_RotationByKeyboard) {
                    mouseMovement.x = __instance.m_RotateOffset;
                    if (Settings.toggleInvertKeyboardXAxis) xRotationSign = -1;
                }
                if (__instance.RotationByMouse || __instance.m_RotationByKeyboard) {
                    var eulerAngles = __instance.transform.rotation.eulerAngles;
                    eulerAngles.y += mouseMovement.x * __instance.RotationSpeed * CameraRig.ConsoleRotationMod;
                    if (__instance.m_EnableOrbitCamera) {
                        eulerAngles.x += mouseMovement.y * __instance.RotationSpeed;
                        eulerAngles.x =
                            Mathf.Clamp(eulerAngles.x <= 180.0 
                                            ? eulerAngles.x
                                            : (float)-(360.0 - eulerAngles.x),
                                        __instance.MinSpaceCameraAngle,
                                        __instance.MaxSpaceCameraAngle);
                    }
                    if (Main.resetExtraCameraAngles) {
                        eulerAngles.x = 0f;
                        CameraElevation = 60f;
                        __instance.m_TargetPosition = __instance.PlaceOnGround2(__instance.m_TargetPosition);
                        var cameraRig = CameraRig.Instance;
#if false               // TODO: figure out if we still need this
                        var highlightingFeature = OwlcatRenderPipeline.Asset.ScriptableRendererData.rendererFeatures.OfType<OccludedObjectHighlightingFeature>().Single<OccludedObjectHighlightingFeature>();
                        highlightingFeature.DepthClip.NearCameraClipDistance = 10;
                        highlightingFeature.DepthClip.ClipTreshold = 0;
#endif
                        Main.resetExtraCameraAngles = false;
                    }
                    else {
                        if (Input.GetKey(KeyCode.LeftControl)) {
                            ___m_TargetPosition.y += yRotationSign * mouseMovement.y / 10f;
                            CameraElevation = ___m_TargetPosition.y;
                        }
                    }
                    __instance.transform.DOKill();
                    __instance.transform.DOLocalRotate(eulerAngles, __instance.RotationTime).SetUpdate(true);
                }

                __instance.m_RotationByKeyboard = false;
                __instance.m_RotateOffset = 0.0f;
                __instance.FigureOutScreenBasis();
                return false;
            }

            [HarmonyPatch(nameof(CameraRig.ClampByLevelBounds))]
            [HarmonyPostfix]

            public static void ClampByLevelBounds(Vector3 point, ref Vector3 __result) {
                if (!Settings.toggleCameraElevation && !Settings.toggleFreeCamera) return;
                __result = point;
            }
            [HarmonyPatch(nameof(CameraRig.PlaceOnGround))]
            [HarmonyPostfix]
            private static void PlaceOnGround(ref Vector3 __result) {
                if (!Settings.toggleCameraElevation && !Settings.toggleFreeCamera) return;
                __result.y = CameraElevation;
            }
        }
    }
}
