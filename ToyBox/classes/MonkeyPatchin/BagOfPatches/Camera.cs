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
using Owlcat.Runtime.Visual.RenderPipeline;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.OccludedObjectHighlighting;
using Kingmaker.Blueprints.Area;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap;
using Kingmaker.Visual.LocalMap;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap;
using Kingmaker.UI;
using Kingmaker.Visual.Particles.ForcedCulling;
using Kingmaker.Visual.LocalMap;

using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using static Kingmaker.Visual.LocalMap.LocalMapRenderer;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using UnityEngine.EventSystems;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;

using Kingmaker.Controllers.Clicks.Handlers;
using static ModKit.UI;

namespace ToyBox.BagOfPatches {
    internal static class CameraPatches {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;
        private static float CameraElevation = 0f;

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
                    if (settings.toggleUseAltMouseWheelToAdjustClipPlane && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftShift))) {
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

        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickScroll))]
        private static class CameraRig_TickScroll_Patch {
            public static bool Prefix(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (!settings.toggleCameraPitch && !settings.toggleCameraElevation && !Main.resetExtraCameraAngles && !settings.toggleFreeCamera) return true;
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
                    if (!settings.toggleFreeCamera) {
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

            public static bool Prefix(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (!settings.toggleRotateOnAllMaps && !settings.toggleCameraPitch && !settings.toggleCameraElevation && !Main.resetExtraCameraAngles && !settings.toggleInvertXAxis && !settings.toggleInvertKeyboardXAxis) return true;
                var usePitch = settings.toggleCameraPitch;
                if (__instance.m_RotateRoutine != null && (double)Time.time > (double)__instance.m_RotateRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_RotateRoutine);
                    __instance.m_RotateRoutine = (Coroutine)null;
                }

                if (__instance.m_ScrollRoutine != null || __instance.m_RotateRoutine != null)// || __instance.m_HandRotationLock)
                    return false;
                __instance.RotateByMiddleButton();
                var mouseMovement = new Vector2(0, 0);
                var xRotationSign = 1f;
                var yRotationSign = settings.toggleInvertYAxis ? 1f : -1f;
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
        }
        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.PlaceOnGround))]
        private static class CameraRig_PlaceOnGround_Patch {
            private static void Postfix(ref Vector3 __result) {
                if (!settings.toggleCameraElevation) return;
                __result.y = CameraElevation;
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
        [HarmonyPatch(typeof(LocalMapRenderer))]
        private static class LocalMapRenderer_Patch {
            public static float prevZoom = 1.0f;
            public static Vector2 prevOffset = new Vector2();
            [HarmonyPatch("Draw", new Type[] { typeof(Vector2) })]
            [HarmonyPrefix]
            public static bool Draw(LocalMapRenderer __instance, Vector2 size, ref DrawResult __result) {
                var zoom = LocalMapVM_Patch.zoom;
                var offset = LocalMapVM_Patch.offset;
                size *= zoom;
                //Mod.Log($"LocalMapRenderer_Patch -  zoom: {zoom} size: {size}");
                if (!Application.isPlaying || __instance.m_CurrentArea == null) {
                    __result = new DrawResult {
                        Canceled = true
                    };
                    return false;
                }
                if (Math.Abs(zoom - prevZoom) > 0.001 || Vector2.Distance(offset, prevOffset) > .001) 
                    __instance.IsAreaDirty = true;
                if (!__instance.IsDirty()) {
                    __result = __instance.GenerateDrawResult();
                    return false;
                }
                prevZoom = zoom;
                var localMapBounds = __instance.m_CurrentArea.Bounds.LocalMapBounds;
                var num = Vector3.Distance(localMapBounds.min, localMapBounds.max);
                __instance.m_Camera.transform.rotation = Quaternion.Euler(__instance.ViewAngle,
                                                                          (bool)(SimpleBlueprint)__instance.m_CurrentArea ? __instance.m_CurrentArea.LocalMapRotation - 180f : -180f,
                                                                          0.0f);
                var position = localMapBounds.center - __instance.m_Camera.transform.forward * num;
                Mod.Log($"offset: {offset}");
                //position.x -= offset.x;
                //position.y -= offset.y;
                __instance.m_Camera.transform.position = position;

                prevOffset = offset;
                if (__instance.lightInst == null)
                    __instance.lightInst = __instance.InstantiatePPLight();
                var vector2 = __instance.m_Camera.aspect > (double)(size.x / size.y)
                                  ? new Vector2(size.x, size.x / __instance.m_Camera.aspect)
                                  : new Vector2(size.y * __instance.m_Camera.aspect, size.y);
                if (__instance.m_ColorRT == null || __instance.m_ColorRT.width != (int)vector2.x || __instance.m_ColorRT.height != (int)vector2.y) {
                    if (__instance.m_ColorRT != null) {
                        __instance.m_ColorRT.Release();
                        UnityEngine.Object.Destroy(__instance.m_ColorRT);
                    }

                    if (__instance.m_DepthRT != null) {
                        __instance.m_DepthRT.Release();
                        UnityEngine.Object.Destroy(__instance.m_DepthRT);
                    }

                    __instance.m_ColorRT = new RenderTexture((int)vector2.x, (int)vector2.y, 0, RenderTextureFormat.ARGB32);
                    __instance.m_ColorRT.name = "LocalMapColorTex";
                    var active = RenderTexture.active;
                    RenderTexture.active = __instance.m_ColorRT;
                    GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
                    RenderTexture.active = active;
                    __instance.m_DepthRT = new RenderTexture((int)vector2.x, (int)vector2.y, 0, RenderTextureFormat.RFloat);
                    __instance.m_DepthRT.name = "LocalMapDepthTex";
                    __instance.m_DepthRT.filterMode = FilterMode.Point;
                    RenderTexture.active = __instance.m_DepthRT;
                    GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
                    RenderTexture.active = active;
                }
                __instance.m_Camera.targetTexture = __instance.m_ColorRT;
                __instance.m_AdditionalCameraData.DepthTexture = __instance.m_DepthRT;
                __instance.m_Camera.cullingMatrix = CalculateProjMatrix(__instance.m_CurrentArea) * CalculateViewMatrix(__instance.m_CurrentArea);
                using (ForcedCullingService.Instance.UncullEverything()) {
                    __instance.m_Camera.Render();
                }

                var drawResult = __instance.GenerateDrawResult();
                __instance.m_CachedArea = __instance.m_CurrentArea;
                __instance.m_CachedAngle = __instance.ViewAngle;
                if (!(null != __instance.lightInst)) {
                    __result = drawResult;
                    return false;
                }
                UnityEngine.Object.Destroy(__instance.lightInst);
                __result = drawResult;
                return false;
            }

            [HarmonyPatch(nameof(UpdateCamera), new Type[] { })]
            [HarmonyPrefix]
            public static bool UpdateCamera(LocalMapRenderer __instance) {
                __instance.m_Camera.enabled = false;
                __instance.m_Camera.orthographic = true;
                __instance.m_Camera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 1f);
                __instance.m_Camera.clearFlags = CameraClearFlags.Color;
                __instance.m_Camera.targetTexture = __instance.m_ColorRT;
                __instance.m_Camera.cullingMask = 102784273;
                __instance.m_AdditionalCameraData.RenderPostProcessing = false;
                __instance.m_AdditionalCameraData.AllowDistortion = true;
                __instance.m_AdditionalCameraData.AllowDecals = false;
                __instance.m_AdditionalCameraData.AllowIndirectRendering = false;
                __instance.m_AdditionalCameraData.AllowFog = false;
                __instance.m_AdditionalCameraData.AllowLighting = true;
                __instance.m_AdditionalCameraData.Dithering = false;
                __instance.m_AdditionalCameraData.DepthTexture = __instance.m_DepthRT;
                __instance.m_AdditionalCameraData.AllowVfxPreparation = false;
                __instance.m_AdditionalCameraData.DisableAllFeatures();
                if (Game.GetCamera() == null)
                    return false;
                if (Application.isPlaying)
                    __instance.m_CurrentArea = AreaService.Instance.CurrentAreaPart;
                if (__instance.m_CurrentArea == null)
                    return false;
                var localMapBounds = __instance.m_CurrentArea.Bounds.LocalMapBounds;
                var num = Vector3.Distance(localMapBounds.min, localMapBounds.max);
                __instance.m_Camera.transform.rotation = Quaternion.Euler(__instance.ViewAngle,
                                                                          (bool)(SimpleBlueprint)__instance.m_CurrentArea ? __instance.m_CurrentArea.LocalMapRotation - 180f : -180f,
                                                                          0.0f);
                __instance.m_Camera.transform.position = localMapBounds.center - __instance.m_Camera.transform.forward * num;
                __instance.CreateAABBPoints(ref localMapBounds, __instance.m_SceneAABBPointsLightSpace);
                var worldToLocalMatrix = __instance.m_Camera.transform.worldToLocalMatrix;
                var rhs1 = Vector3.one * float.MaxValue;
                var rhs2 = Vector3.one * float.MinValue;
                for (var index = 0; index < __instance.m_SceneAABBPointsLightSpace.Length; ++index) {
                    __instance.m_SceneAABBPointsLightSpace[index] =
                        worldToLocalMatrix.MultiplyPoint(__instance.m_SceneAABBPointsLightSpace[index]);
                    rhs1 = Vector3.Min(__instance.m_SceneAABBPointsLightSpace[index], rhs1);
                    rhs2 = Vector3.Max(__instance.m_SceneAABBPointsLightSpace[index], rhs2);
                }

                var bounds = new Bounds();
                bounds.min = rhs1;
                bounds.max = rhs2;
                __instance.m_Camera.transform.position = __instance.m_Camera.transform.TransformPoint(bounds.center - Vector3.forward * num);
                __instance.m_Camera.farClipPlane = num * 2f;
                __instance.m_Camera.orthographicSize = bounds.extents.y;
                __instance.m_Camera.aspect = bounds.size.x / bounds.size.y;
                return false;
            }
        }
#endif

        [HarmonyPatch(typeof(LocalMapVM))]
        private static class LocalMapVM_Patch {
            public static float zoom = 1.0f;
            public static float width = 0.0f;
            public static Vector2 offset = new Vector2();

            #if false
            [HarmonyPatch("OnUpdateHandler", new Type[] {})]
            [HarmonyPrefix]
            public static bool OnUpdateHandler(LocalMapVM __instance) {
                __instance.DrawResult.Value = LocalMapRenderer.Instance.Draw(__instance.m_MaxSize);
                __instance.CompassAngle.Value = Game.Instance.UI.GetCameraRig().transform.eulerAngles.y -
                                                Game.Instance.CurrentlyLoadedArea.LocalMapRotation;
                LocalMapModel.Markers.RemoveWhere(m => m.GetMarkerType() == LocalMapMarkType.Invalid);
                var unitsList = Game.Instance.Player.MainCharacter.Value.Memory.UnitsList;
                var list = __instance.MarkersVm.OfType<LocalMapUnitMarkerVM>().ToList();
                var unitInfoList = new List<UnitGroupMemory.UnitInfo>();
                foreach (var unitInfo in unitsList) {
                    var character = unitInfo;
                    if (character.Unit.IsPlayerFaction || !character.Unit.IsVisibleForPlayer ||
                        character.Unit.Descriptor.State.IsDead || !LocalMapModel.IsInCurrentArea(character.Unit.Position))
                        unitInfoList.Add(character);
                    else if (list.FirstOrDefault(vm => vm.UnitInfo == character) == null)
                        __instance.MarkersVm.Add(new LocalMapUnitMarkerVM(character));
                }

                for (var index = 0; index < __instance.MarkersVm.Count; ++index)
                    if (__instance.MarkersVm[index] is LocalMapUnitMarkerVM localMapUnitMarkerVm1 &&
                        unitInfoList.Contains(localMapUnitMarkerVm1.UnitInfo)) {
                        __instance.MarkersVm[index].Dispose();
                        __instance.MarkersVm.RemoveAt(index);
                    }

                __instance.GameTime.Value = Game.Instance.Player.GameTime;
                __instance.DateString.Value = BlueprintRoot.Instance.Calendar.GetCurrentDateText();
                return false;
            }
            #endif
            [HarmonyPatch(nameof(OnClick), new Type[] {typeof(Vector2), typeof(bool)})]
            [HarmonyPrefix]
            public static bool OnClick(LocalMapVM __instance, Vector2 localPos, bool state) {
                if (!settings.toggleZoomableLocalMaps) return true;
                var vector3 = LocalMapRenderer.Instance.ViewportToWorldPoint(
                    new Vector2(localPos.x / (__instance.DrawResult.Value.ColorRT.width), localPos.y / (__instance.DrawResult.Value.ColorRT.height)));
                if (!LocalMapModel.IsInCurrentArea(vector3))
                    vector3 = AreaService.Instance.CurrentAreaPart.Bounds.LocalMapBounds.ClosestPoint(vector3);
                if (state) {
                    Game.Instance.CameraController.Follower.Release();
                    Game.Instance.UI.GetCameraRig().ScrollTo(vector3);
                }
                else {
                    ClickGroundHandler.MoveSelectedUnitsToPoint(vector3);
                }
                return false;
            }
        }
        
        // Modifies Local Map View to zoom the map for easier reading
        // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/ServiceWindowsPCView/Background/Windows/LocalMapPCView/ContentGroup/MapBlock
        [HarmonyPatch(typeof(LocalMapBaseView))]
        private static class LocalMapBaseView_Patch {
            [HarmonyPatch(nameof(SetDrawResult), new Type[] {typeof(LocalMapRenderer.DrawResult)})]
            [HarmonyPrefix]
            public static  bool SetDrawResult(LocalMapBaseView __instance, LocalMapRenderer.DrawResult dr) {
                var width = dr.ColorRT.width;
                LocalMapVM_Patch.width = width;
                var height = dr.ColorRT.height;
                __instance.m_Image.rectTransform.sizeDelta = new Vector2(width, height);
                var a = (dr.ScreenRect.z - dr.ScreenRect.x) * width;
                var b = (dr.ScreenRect.w - dr.ScreenRect.y) * height;
                var sizeDelta = new Vector2(Mathf.Max(a, b), Mathf.Min(a, b));
                __instance.m_FrameBlock.sizeDelta = sizeDelta;
                __instance.m_FrameBlock.localPosition = new Vector2(dr.ScreenRect.x * width, dr.ScreenRect.y * height);
                __instance.SetupBPRVisible();
                var contentGroup = UIHelpers.LocalMapScreen.Find("ContentGroup");
                var mapBlock = UIHelpers.LocalMapScreen.Find("ContentGroup/MapBlock");
                var map = mapBlock.Find("Map");
                var frameBlock = mapBlock.Find("Map/FrameBlock");
                var frame = frameBlock.Find("Frame");
                if (contentGroup is RectTransform contentGroupRect 
                    && mapBlock is RectTransform mapBlockRect
                    && map is RectTransform mapRect
                    && frameBlock is RectTransform frameBlockRect
                    && frame is RectTransform frameRect
                    ) {
                    if (settings.toggleZoomableLocalMaps) {
                        //Mod.Log($"width: {width} sizeDelta.x: {sizeDelta.x} a:{a} dr.ScreenRect: {dr.ScreenRect}");
                        LocalMapVM_Patch.zoom = width / (2 * sizeDelta.x);
                        LocalMapVM_Patch.offset = frameBlockRect.localPosition * LocalMapVM_Patch.zoom;
                        var pos = mapBlock.localPosition;
                        pos.x = -3 - frameBlockRect.localPosition.x * LocalMapVM_Patch.zoom - width / 4;
                        pos.y = -22 - frameBlockRect.localPosition.y * LocalMapVM_Patch.zoom - width / 4;
                        mapBlock.localPosition = pos;
                        var zoomVector = new Vector3(LocalMapVM_Patch.zoom, LocalMapVM_Patch.zoom, 1.0f);
                        mapBlock.localScale = zoomVector;
                        mapBlockRect.pivot = new Vector2(0.0f, 0.0f);
                        Mod.Log($"zoom: {zoomVector}");
                    }
                    else {
                        var zoomVector = new Vector3(1, 1, 1.0f);
                        LocalMapVM_Patch.zoom = 1.0f;
                        LocalMapVM_Patch.offset = new Vector2(0.0f, 0.0f);
                        mapBlock.localScale = new Vector3(1, 1, 1);
                    }
                }
                return false;
            }
            #if false
                    ) {
                    if (settings.toggleZoomableLocalMaps) {
                        //Mod.Log($"width: {width} sizeDelta.x: {sizeDelta.x} a:{a} dr.ScreenRect: {dr.ScreenRect}");
                        LocalMapVM_Patch.zoom = width / (2 * sizeDelta.x);
                        LocalMapVM_Patch.offset = frameBlockRect.localPosition * LocalMapVM_Patch.zoom;
                        var pos = mapBlock.localPosition;
                        pos.x = -3 - frameBlockRect.localPosition.x * LocalMapVM_Patch.zoom - width / 4;
                        pos.y = -22 - frameBlockRect.localPosition.y * LocalMapVM_Patch.zoom - width / 4;
                        mapBlock.localPosition = pos;
                        var zoomVector = new Vector3(LocalMapVM_Patch.zoom, LocalMapVM_Patch.zoom, 1.0f);
                        mapBlock.localScale = zoomVector;
                        mapBlockRect.pivot = new Vector2(0.0f, 0.0f);
                        #if false
                        frameBlockRect.pivot = new Vector2(0.5f, 0.5f);
                        mapRect.pivot = new Vector2(0.5f, 0.5f);
                        #endif
                        //var frameQuat = frameRect.localRotation;
                        //frameRect.localRotation= new Quaternion(0, 0, 0, 0);
                    }

            }
            #endif

            [HarmonyPatch(nameof(SetupBPRVisible))]
            [HarmonyPrefix]
            public static bool SetupBPRVisible(LocalMapBaseView __instance) {
                if (!settings.toggleZoomableLocalMaps) return true;
                __instance.m_BPRImage?.gameObject?.SetActive(
                    //LocalMapVM_Patch.zoom  <= 1.0f &&
                     __instance.m_Image.rectTransform.rect.width < 975.0
                    );
                return false;
            }
        }
        #if false
        [HarmonyPatch(typeof(LocalMapPCView))]
        public static class LocalMapPCView_Patch {
            [HarmonyPatch(nameof(OnPointerClick))]
            [HarmonyPrefix]
            public static bool OnPointerClick(LocalMapPCView __instance, PointerEventData eventData) {
                if (!settings.toggleZoomableLocalMaps) return true;
                if (eventData.button == PointerEventData.InputButton.Middle)
                    return false;
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(__instance.m_Image.rectTransform, eventData.position, Game.Instance.UI.UICamera, out localPoint);
                var zoom = LocalMapVM_Patch.zoom;
                var oldPoint = localPoint;
                localPoint = localPoint;
                var sizeDelta = __instance.m_Image.rectTransform.sizeDelta;
                // zoom =  width / (2 * sizeDelta.x)
                var adjustedPoint = localPoint + Vector2.Scale(sizeDelta, __instance.m_Image.rectTransform.pivot);
                Mod.Log($"zoom: {LocalMapVM_Patch.zoom} - localPoint: {oldPoint} -> {localPoint} -> {adjustedPoint} ");
                __instance.ViewModel.OnClick(adjustedPoint, eventData.button == PointerEventData.InputButton.Left);
                return false;
            }
        }
        #endif
    }
}
