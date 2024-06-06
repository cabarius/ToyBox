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
using Kingmaker.PubSubSystem.Core.Interfaces;

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
                if (Game.Instance.CurrentlyLoadedArea.AreaStatGameMode == GameModeType.StarSystem || Game.Instance.CurrentlyLoadedArea.AreaStatGameMode == GameModeType.GlobalMap) {
                    return false;
                }
                var instance = CameraRig.Instance;
                if (instance == null || (bool)instance.FixCamera)
                    return false;
                if (Settings.toggleCameraPitch) {
                    instance.m_EnableOrbitCamera = true;
                    //Mod.Log($"{instance.MinSpaceCameraAngle} {instance.MaxSpaceCameraAngle}");
                    //Mod.Log($"Euler angles: {instance.transform.eulerAngles}");
                    instance.MinSpaceCameraAngle = -63; // -15
                    instance.MaxSpaceCameraAngle = +100; // +20 
                } else {
                    instance.MinSpaceCameraAngle = -20;
                    instance.MaxSpaceCameraAngle = +15;

                }
                if (__instance.m_AllowScroll || Settings.toggleScrollOnAllMaps) {
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

        [HarmonyPatch(typeof(CameraRig))]
        public static class CameraRigPatch {
            public static void OnAreaLoad() {
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
                float num = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                if (__instance.m_ScrollRoutine != null && Time.time > __instance.m_ScrollRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_ScrollRoutine);
                    __instance.m_ScrollRoutine = null;
                    Vector3 vector = __instance.PlaceOnGround(__instance.m_ScrollRoutineEndPos);
                    __instance.transform.position = (__instance.m_TargetPosition = vector);
                }
                if (__instance.m_HandScrollLock) {
                    return false;
                }
                if (__instance.m_ScrollRoutine == null) {
                    Vector2 vector2 = __instance.m_ScrollOffset;
                    bool flag = __instance.m_FullScreenUIType > FullScreenUIType.Unknown;
                    if ((!Application.isEditor || CameraRig.DebugCameraScroll) && !Game.Instance.IsControllerGamepad && SettingsRoot.Controls.ScreenEdgeScrolling && !flag && Application.isFocused) {
                        vector2 += __instance.GetCameraScrollShiftByMouse();
                    }
                    vector2 += __instance.m_ScrollBy2D;
                    __instance.m_ScrollBy2D.Set(0f, 0f);
                    if (vector2 == Vector2.zero) {
                        return false;
                    }
                    CameraController cameraController = Game.Instance.CameraController;
                    if (cameraController != null) {
                        CameraController.CameraUnitFollower follower = cameraController.Follower;
                        if (follower != null) {
                            follower.Release();
                        }
                    }
                    float cameraScrollMultiplier = Game.Instance.CurrentlyLoadedArea.CameraScrollMultiplier;
                    vector2 *= __instance.ScrollSpeed * num * cameraScrollMultiplier * CameraRig.ConsoleScrollMod;
                    __instance.FigureOutScreenBasis();
                    Vector3 prevPos = __instance.m_TargetPosition;
                    __instance.m_TargetPosition += vector2.x * __instance.Right + vector2.y * __instance.Up;
                    EventBus.RaiseEvent<ICameraMovementHandler>(delegate (ICameraMovementHandler h) {
                        h.HandleCameraTransformed(Vector3.Dot(prevPos, __instance.m_TargetPosition));
                    }, true);
                    if (!Settings.toggleFreeCamera) {
                        if (!__instance.NoClamp && !__instance.m_SkipClampOneFrame) {
                            __instance.m_TargetPosition = __instance.ClampByLevelBounds(__instance.m_TargetPosition);
                        }
                        __instance.m_TargetPosition = __instance.PlaceOnGround(__instance.m_TargetPosition);
                        if (__instance.NewBehaviour) {
                            if (__instance.m_AttackPointPos == null) {
                                __instance.m_AttackPointPos = new Vector3?(__instance.Camera.transform.parent.localPosition);
                            }
                            __instance.m_TargetPosition = __instance.LowerGently(prevPos, __instance.m_TargetPosition, num);
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
                if (__instance.RotationByMouse || __instance.m_RotationByKeyboard) {
                    var eulerAngles = __instance.transform.rotation.eulerAngles;
                    var mouseMovement = Vector2.zero;
                    if (Main.resetExtraCameraAngles) {
                        eulerAngles.x = 0f;
                        CameraElevation = 60f;
                        __instance.m_TargetPosition = __instance.PlaceOnGround2(__instance.m_TargetPosition);
                        Main.resetExtraCameraAngles = false;
                    } else if (Input.GetKey(KeyCode.LeftControl) && Settings.toggleCameraElevation) {
                        var yRotationSign = Settings.toggleInvertYAxis ? 1f : -1f;
                        mouseMovement = __instance.CameraDragToRotate();
                        ___m_TargetPosition.y += yRotationSign * mouseMovement.y / 10f;
                        CameraElevation = ___m_TargetPosition.y;
                    }
                }
                return true;
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
