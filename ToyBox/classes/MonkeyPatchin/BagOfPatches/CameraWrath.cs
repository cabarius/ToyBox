// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.GameModes;
using Kingmaker.View;
using System;
using System.Linq;
using UnityEngine;
using Kingmaker.Settings;
using ModKit;
using DG.Tweening;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Owlcat.Runtime.Visual.RenderPipeline;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.OccludedObjectHighlighting;
using CameraMode = Kingmaker.View.CameraMode;
using Kingmaker.UI.MVVM._VM.ServiceWindows;
using Kingmaker.UI.SettingsUI;

namespace ToyBox.BagOfPatches {
    internal static class CameraPatches {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;
        private static float CameraElevation = 0f;


        [HarmonyPatch(typeof(CameraZoom))]
        private static class CameraZoomPatch {
            private static bool firstCall = true;
            private static float BaseFovMin => (Settings.toggleZoomOnAllMaps || Settings.toggleZoomableLocalMaps) ? 12 : 17.5f;
            private static readonly float BaseFovMax = 30;
            private static float FovMin => BaseFovMin / Settings.fovMultiplier;
            private static float FovMax => BaseFovMax * Settings.AdjustedFovMultiplier;

            [HarmonyPatch(nameof(CameraZoom.TickZoom))]
            [HarmonyPrefix]
            private static bool TickZoom(CameraZoom __instance,
                        Coroutine ___m_ZoomRoutine,
                        float ___m_Smooth,
                        ref float ___m_PlayerScrollPosition,
                        ref float ___m_ScrollPosition,
                        ref float ___m_SmoothScrollPosition,
                        Camera ___m_Camera,
                        float ___m_ZoomLenght) {
                if (Settings.fovMultiplier == 1 && !Settings.toggleZoomableLocalMaps) return true;
                if (!__instance.IsScrollBusy && Game.Instance.IsControllerMouse && (double)Input.GetAxis("Mouse ScrollWheel") != 0.0 && ((double)___m_Camera.fieldOfView > (double)FovMin || (double)Input.GetAxis("Mouse ScrollWheel") < 0.0)) {
                    if (Settings.toggleUseAltMouseWheelToAdjustClipPlane && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftShift))) {
                        var cameraRig = Game.Instance.UI.GetCameraRig();
                        var highlightingFeature = OwlcatRenderPipeline.Asset.ScriptableRendererData.rendererFeatures.OfType<OccludedObjectHighlightingFeature>().Single<OccludedObjectHighlightingFeature>();
                        if (Input.GetKey(KeyCode.LeftAlt)) {
                            highlightingFeature.DepthClip.NearCameraClipDistance += Input.GetAxis("Mouse ScrollWheel") * 5;
                            Mod.Debug($"highlightingFeature.DepthClip.NearCameraClipDistance: {highlightingFeature.DepthClip.NearCameraClipDistance}");
                        }
                        if (Input.GetKey(KeyCode.LeftShift)) {
                            highlightingFeature.DepthClip.ClipTreshold += Input.GetAxis("Mouse ScrollWheel") / 5;
                            Mod.Debug($"highlightingFeature.DepthClip.ClipTreshold: {highlightingFeature.DepthClip.ClipTreshold}");
                            //highlightingFeature.DepthClip.AlphaScale += Input.GetAxis("Mouse ScrollWheel");
                            //highlightingFeature.DepthClip.NoiseTiling += Input.GetAxis("Mouse ScrollWheel");

                        }
                        return false;
                    }
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

        // Camera Rig Helper
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

        [HarmonyPatch(typeof(CameraRig))]
        public static class CameraRigPatch {
            private static UISettingsEntityKeyBinding _followKeyBinding = null;
            public static void OnAreaLoad() {
                _followKeyBinding = null;
            }

            [HarmonyPatch(nameof(CameraRig.Update))]
            [HarmonyPostfix]
            static void Update(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (Settings.toggleRotateOnAllMaps || Main.resetExtraCameraAngles)
                    __instance.TickRotate();
                if (Settings.toggleZoomOnAllMaps)
                    __instance.CameraZoom.TickZoom();
                if (Settings.toggleScrollOnAllMaps) {
                    __instance.TickScroll();
                    //__instance.TickCameraDrag();
                    //__instance.CameraDragToMove();
                }
                if (Settings.toggleAutoFollowHold) {
                    if (_followKeyBinding == null) {
                        var controlSettingsGroup = Game.Instance.UISettingsManager.m_ControlSettingsList.First(g => g.name == "KeybindingsGeneral");
                        _followKeyBinding = controlSettingsGroup.SettingsList
                                                                .OfType<UISettingsEntityKeyBinding>()
                                                                .First(item => item.name == "FollowUnit");
                    }
                    if (_followKeyBinding?.IsDown ?? false) {
                        var selectedUnit = WrathExtensions.GetCurrentCharacter();
                        if (selectedUnit != null)
                            Game.Instance.CameraController?.Follower.Follow(selectedUnit);
                    }
                }
            }

            [HarmonyPatch(nameof(CameraRig.TickScroll))]
            [HarmonyPrefix]
            public static bool TickScroll(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (!Settings.toggleCameraPitch && !Settings.toggleCameraElevation && !Main.resetExtraCameraAngles && !Settings.toggleFreeCamera) return true;
                var isInlocalMap = Game.Instance?.RootUiContext.CurrentServiceWindow == ServiceWindowsType.LocalMap;
                var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                if (__instance.m_ScrollRoutine != null && (double)Time.time > (double)__instance.m_ScrollRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_ScrollRoutine);
                    __instance.m_ScrollRoutine = (Coroutine)null;
                }
                if (__instance.m_ScrollRoutine == null) {
                    var eulerAngles = __instance.transform.rotation.eulerAngles;
                    var scrollOffset = __instance.m_ScrollOffset;
                    if (eulerAngles.x > 180)
                        scrollOffset.y = -scrollOffset.y;
                    if (Settings.toggleZoomableLocalMaps && isInlocalMap && scrollOffset.magnitude > 0) {
                        var frameRotation = LocalMapPatches.FrameRotation;
                        var zoom = LocalMapPatches.Zoom;
                        var newScrollOffset = Quaternion.AngleAxis(-frameRotation.z, Vector3.forward) * scrollOffset;
                        //Mod.Debug($"inMap: {scrollOffset} -> {newScrollOffset} angle: {frameRotation}");
                        scrollOffset = newScrollOffset * Settings.zoomableLocalMapScrollSpeedMultiplier / zoom;
                    }
                    if ((bool)(SimpleBlueprint)BlueprintRoot.Instance && !Game.Instance.IsControllerGamepad && (bool)(SettingsEntity<bool>)SettingsRoot.Controls.ScreenEdgeScrolling && (Cursor.visible || (bool)(SettingsEntity<bool>)SettingsRoot.Controls.CameraScrollOutOfScreenEnabled) && Game.Instance.CurrentMode != GameModeType.FullScreenUi)
                        scrollOffset += __instance.GetCameraScrollShiftByMouse();
                    var scrollVector2 = scrollOffset + __instance.CameraDragToMove() + __instance.m_ScrollBy2D;
                    __instance.m_ScrollBy2D.Set(0.0f, 0.0f);
                    __instance.TickCameraDrag();
                    Game.Instance.CameraController?.Follower?.Release();
                    var currentlyLoadedArea = Game.Instance.CurrentlyLoadedArea;
                    var num = currentlyLoadedArea != null ? currentlyLoadedArea.CameraScrollMultiplier : 1f;
                    var scaledScrollVector = scrollVector2 * (__instance.m_ScrollSpeed * dt * num * CameraRig.ConsoleScrollMod);
                    var scrollMagnatude = (float)Math.Sqrt(Vector2.Dot(scaledScrollVector, scaledScrollVector));

                    //var scaledScrollVector3 = scrollVector3 * (__instance.m_ScrollSpeed * dt * num * CameraRig.ConsoleScrollMod);
                    var worldPoint1 = __instance.Camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 1f));
                    var worldPoint2 = __instance.Camera.ViewportToWorldPoint(new Vector3(1f, 0.5f, 1f));
                    var worldPoint3 = __instance.Camera.ViewportToWorldPoint(new Vector3(0.5f, 1f, 1f));
                    //Mod.Debug($"worldPoint xyZ: {worldPoint1.normalized} XyZ: {worldPoint2.normalized} zYZ: {worldPoint3.normalized} ");
                    var pitch = __instance.transform.eulerAngles.x - 42.75;
                    var pitchRadians = Math.PI * pitch / 180f;
                    worldPoint2.y = worldPoint1.y;
                    worldPoint3.y = worldPoint1.y;
                    var vector3 = worldPoint2 - worldPoint1;
                    __instance.Right = vector3.normalized;
                    vector3 = worldPoint3 - worldPoint1;
                    __instance.Up = vector3.normalized;
                    if (pitch >= 0) __instance.Up = -__instance.Up;
                    var targetPosition = __instance.m_TargetPosition;
                    var yAxis = new Vector3(0, 2, 0);
                    var dPos = scaledScrollVector.x * __instance.Right + scaledScrollVector.y * __instance.Up;
                    if (__instance.m_RotationByMouse)
                        dPos += scaledScrollVector.y * (float)Math.Sin(pitchRadians) * yAxis;
                    //Mod.Debug($"dPos: {dPos} pitch: {pitch:##.000} sine: {Math.Sin(pitchRadians):#.000}");
                    __instance.m_TargetPosition += dPos;
                    if (!Settings.toggleFreeCamera) {
                        if (!__instance.NoClamp && !__instance.m_SkipClampOneFrame)
                            __instance.m_TargetPosition = __instance.ClampByLevelBounds(__instance.m_TargetPosition);
                        __instance.m_TargetPosition = __instance.PlaceOnGround(__instance.m_TargetPosition);
                        if (__instance.NewBehaviour) {
                            if (!__instance.m_AttachPointPos.HasValue)
                                __instance.m_AttachPointPos = new Vector3?(__instance.Camera.transform.parent.localPosition);
                            __instance.m_TargetPosition = __instance.LowerGently(targetPosition, __instance.m_TargetPosition, dt);
                        }
                    }
                }
                __instance.m_ScrollOffset = Vector2.zero;
                return false;
            }

            [HarmonyPatch(nameof(CameraRig.TickRotate))]
            [HarmonyPrefix]
            public static bool TickRotate(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (!Settings.toggleRotateOnAllMaps && !Settings.toggleCameraPitch && !Settings.toggleCameraElevation && !Main.resetExtraCameraAngles && !Settings.toggleInvertXAxis && !Settings.toggleInvertKeyboardXAxis) return true;
                var usePitch = Settings.toggleCameraPitch;
                if (__instance.m_RotateRoutine != null && (double)Time.time > (double)__instance.m_RotateRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_RotateRoutine);
                    __instance.m_RotateRoutine = (Coroutine)null;
                }

                if (__instance.m_ScrollRoutine != null || __instance.m_RotateRoutine != null)// || __instance.m_HandRotationLock)
                    return false;
                __instance.RotateByMiddleButton();
                var mouseMovement = new Vector2(0, 0);
                var xRotationSign = 1f;
                var yRotationSign = Settings.toggleInvertYAxis ? 1f : -1f;
                if (__instance.m_RotationByMouse) {
                    if (!Settings.toggleInvertXAxis) xRotationSign = -1;
                    mouseMovement = __instance.CameraDragToRotate2D();
                }
                else if (__instance.m_RotationByKeyboard) {
                    mouseMovement.x = __instance.m_RotateOffset;
                    if (Settings.toggleInvertKeyboardXAxis) xRotationSign = -1;
                }
                if (__instance.m_RotationByMouse || __instance.m_RotationByKeyboard || Main.resetExtraCameraAngles) {
                    var eulerAngles = __instance.transform.rotation.eulerAngles;
                    eulerAngles.y += xRotationSign * mouseMovement.x * __instance.m_RotationSpeed * CameraRig.ConsoleRotationMod;
                    if (Main.resetExtraCameraAngles) {
                        eulerAngles.x = 0f;
                        CameraElevation = 60f;
                        __instance.m_TargetPosition = __instance.PlaceOnGround2(__instance.m_TargetPosition);
                        var cameraRig = Game.Instance.UI.GetCameraRig();
                        var highlightingFeature = OwlcatRenderPipeline.Asset.ScriptableRendererData.rendererFeatures.OfType<OccludedObjectHighlightingFeature>().Single<OccludedObjectHighlightingFeature>();
                        highlightingFeature.DepthClip.NearCameraClipDistance = 10;
                        highlightingFeature.DepthClip.ClipTreshold = 0;
                        Main.resetExtraCameraAngles = false;
                    }
                    else {
                        if (Input.GetKey(KeyCode.LeftControl)) {
                            ___m_TargetPosition.y += yRotationSign * mouseMovement.y / 10f;
                            CameraElevation = ___m_TargetPosition.y;
                        }
                        else if (usePitch) {
                            eulerAngles.x += yRotationSign * mouseMovement.y * __instance.m_RotationSpeed * CameraRig.ConsoleRotationMod;
                            //Mod.Debug($"eulerX: {eulerAngles.x} Y: {eulerAngles.y} Z: {eulerAngles.z}");
                        }
                    }
                    __instance.transform.DOKill();
                    __instance.transform.DOLocalRotate(eulerAngles, __instance.m_RotationTime).SetUpdate<Tweener>(true);
                }

                __instance.m_RotationByKeyboard = false;
                __instance.m_RotateOffset = 0.0f;
                return false;
            }

            [HarmonyPatch(nameof(CameraRig.PlaceOnGround))]
            [HarmonyPostfix]
            private static void PlaceOnGround(ref Vector3 __result) {
                if (!Settings.toggleCameraElevation) return;
                __result.y = CameraElevation;
            }
            [HarmonyPatch(nameof(CameraRig.SetMode))]
            [HarmonyPostfix]
            public static void SetMode(CameraRig __instance, CameraMode mode) {
                if (Settings.fovMultiplierCutScenes == 1 && Settings.fovMultiplier == 1) return;
                if (mode == CameraMode.Default && Game.Instance.CurrentMode == GameModeType.Cutscene) {
                    __instance.Camera.fieldOfView = __instance.CameraZoom.FovMax * Settings.fovMultiplierCutScenes / Settings.fovMultiplier;
                }
            }
        }
    }
}
