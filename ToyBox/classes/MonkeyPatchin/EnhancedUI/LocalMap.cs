using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Visual.LocalMap;
using ModKit;
using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static Kingmaker.UnitLogic.Interaction.SpawnerInteractionPart;

namespace ToyBox.BagOfPatches {
    internal static class LocalMapPatches {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;


#if false
        [HarmonyPatch(typeof(LocalMapRenderer))]
        private static class LocalMapRenderer_Patch {
            public static float prevZoom = 1.0f;

            [HarmonyPatch("Draw", new Type[] { typeof(Vector2) })]
            [HarmonyPrefix]
            public static bool Draw(LocalMapRenderer __instance, Vector2 size, ref DrawResult __result) {
                var zoom = LocalMapVM_Patch.zoom;
                //Mod.Log($"LocalMapRenderer_Patch -  zoom: {zoom} size: {size}");
                if (!Application.isPlaying || __instance.m_CurrentArea == null) {
                    __result = new DrawResult {
                        Canceled = true
                    };
                    return false;
                }
                if (Math.Abs(zoom - prevZoom) > 0.001)
                    __instance.IsAreaDirty = true;
                if (!__instance.IsDirty()) {
                    __result = __instance.GenerateDrawResult();
                    return false;
                }
                size *= 1;
                prevZoom = zoom;
                var localMapBounds = __instance.m_CurrentArea.Bounds.LocalMapBounds;
                var num = Vector3.Distance(localMapBounds.min, localMapBounds.max);
                __instance.m_Camera.transform.rotation = Quaternion.Euler(__instance.ViewAngle,
                                                                          (bool)(SimpleBlueprint)__instance.m_CurrentArea ? __instance.m_CurrentArea.LocalMapRotation - 180f : -180f,
                                                                          0.0f);
                var position = localMapBounds.center - __instance.m_Camera.transform.forward * num;
                __instance.m_Camera.transform.position = position;

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
            // public static Vector2 offset = new Vector2();

            [HarmonyPatch(nameof(OnClick), new Type[] { typeof(Vector2), typeof(bool) })]
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
            private static float prevZoom = 0;
            // These are the transform paths for the different kinds of marks on the LocalMapView
            private static readonly string[] MarksPaths = { "MarksPC", "MarksUnits", "MarksLoot", "MarksPoi", "MarksVIT" };

            [HarmonyPatch(nameof(SetDrawResult), new Type[] { typeof(LocalMapRenderer.DrawResult) })]
            [HarmonyPrefix]
            public static bool SetDrawResult(LocalMapBaseView __instance, LocalMapRenderer.DrawResult dr) {
                // This is the original owlcat code.  This gets called when zoom changes to adjust the size of the FrameBlock, a widget that looks like a picture frame and depicts the users view into the world based on zoom and camera rotation
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

                // Now ToyBox wants to rock your world. We grab various transforms 
                var contentGroup = UIHelpers.LocalMapScreen.Find("ContentGroup"); // Overall map view including the compass
                var mapBlock = UIHelpers.LocalMapScreen.Find("ContentGroup/MapBlock"); // Container for map, border, markers and the frame
                var map = mapBlock.Find("Map"); // Just the map
                var frameBlock = mapBlock.Find("Map/FrameBlock"); // Camera viewport projected onto the map
                var frame = frameBlock.Find("Frame"); // intermediate container for the FrameBlock
                if (contentGroup is RectTransform contentGroupRect
                    && mapBlock is RectTransform mapBlockRect
                    && map is RectTransform mapRect
                    && frameBlock is RectTransform frameBlockRect
                    && frame is RectTransform frameRect
                    ) {
                    if (settings.toggleZoomableLocalMaps) {
                        // Calculate a zoom factor based on info used previously to scale the Frame Block. In our new world we will center the Frame Block in middle of the ContentGroup and then pan the map behind it.  TODO - make it rotate so that it matches exactly the view of the camera (Frame Block will always point up)
                        LocalMapVM_Patch.zoom = width / (2 * sizeDelta.x);
                        var zoom = LocalMapVM_Patch.zoom;
                        // LocalMapVM_Patch.offset = frameBlockRect.localPosition * LocalMapVM_Patch.zoom;

                        // Now adjust the position of the mapBlock to  keep the FrameBlock in a fixed position
                        var pos = mapBlock.localPosition;
                        pos.x = -3 - frameBlockRect.localPosition.x * zoom - width / 4; // ??? this is a weird correction (make better?)
                        pos.y = -22 - frameBlockRect.localPosition.y * zoom - width / 4; // ??? this is a weird correction (make better?)
                        mapBlock.localPosition = pos;

                        // Now apply the zoom to MapBlock
                        var zoomVector = new Vector3(zoom, zoom, 1.0f);
                        mapBlock.localScale = zoomVector;

                        // Fix the pivot to ensure we stay centered when we zoom
                        mapBlockRect.pivot = new Vector2(0.0f, 0.0f);
                        //Mod.Log($"zoom: {zoomVector}");
                        //frameBlockRect.pivot = new Vector2(0.5f, 0.5f);
                        //mapRect.pivot = new Vector2(0.5f, 0.5f);
                        if (Math.Abs(zoom - prevZoom) > .001) {
                            // Now we don't need all the POI and other map markers to get really big when you zoom so we will shrink them to a reasonable size 
                            var shrinkVector = new Vector3(1.5f / zoom, 1.5f / zoom, 1);

                            foreach (var markPath in MarksPaths) {
                                var marks = map.Find(markPath).gameObject.getChildren();
                                foreach (var mark in marks) {
                                    if (!mark.transform.localScale.Equals(new Vector3(0, 0, 0))) {
                                        mark.transform.localScale = shrinkVector;
                                    }
                                    var lootMarkerView = mark.GetComponent<LocalMapLootMarkerPCView>();
                                    lootMarkerView?.Hide();
                                }
                            }

                            // Finally we tweak the thickness of the Frame Block so it doesn't grow really small and thick.
                            if (frame.FindChild("Top")?.gameObject?.transform is Transform tt) tt.localScale = new Vector3(1, 1.5f / zoom, 1);
                            if (frame.FindChild("Bottom")?.gameObject?.transform is Transform tb) tb.localScale = new Vector3(1, 1.5f / zoom, 1);
                            if (frame.FindChild("Bottom/BottomEye")?.gameObject?.transform is Transform tbe) tbe.localScale = new Vector3(1.5f / zoom, 1f, 1);
                            if (frame.FindChild("Left")?.gameObject?.transform is Transform tl) tl.localScale = new Vector3(1.5f / zoom, 1, 1);
                            if (frame.FindChild("Right")?.gameObject?.transform is Transform tr) tr.localScale = new Vector3(1.5f / zoom, 1, 1);
                        }
                    }
                    else {
                        // TODO: Factor the above into a helper function and take zoom as a paremeter so we can call it to reset everything back to normal when we turn off Enhanced Map
                        var zoomVector = new Vector3(1, 1, 1.0f);
                        LocalMapVM_Patch.zoom = 1.0f;
                        //LocalMapVM_Patch.offset = new Vector2(0.0f, 0.0f);
                        mapBlock.localScale = new Vector3(1, 1, 1);
                    }
                }
                return false;
            }

            // The compass (kind for drawing circles) is pretty but we want to hide it when the map zooms
            [HarmonyPatch(nameof(SetupBPRVisible))]
            [HarmonyPrefix]
            public static bool SetupBPRVisible(LocalMapBaseView __instance) {
                if (!settings.toggleZoomableLocalMaps) return true;
                __instance.m_BPRImage?.gameObject?.SetActive(
                    LocalMapVM_Patch.zoom <= 1.0f &&
                     __instance.m_Image.rectTransform.rect.width < 975.0
                    );
                return false;
            }
        }

        [HarmonyPatch(typeof(LocalMapMarkerPCView), nameof(LocalMapMarkerPCView.BindViewImplementation))]
        private static class LocalMapMarkerPCView_BindViewImplementation_Patch {
            [HarmonyPostfix]
            public static void Postfix(LocalMapMarkerPCView __instance) {
                if (__instance == null)
                    return;
                Mod.Debug($"LocalMapMarkerPCView.BindViewImplementation - {__instance.ViewModel.MarkerType} - {__instance.ViewModel.GetType().Name}");
                if (__instance.ViewModel.MarkerType == LocalMapMarkType.Loot)
                    __instance.AddDisposable(__instance.ViewModel.IsVisible.Subscribe(value => { (__instance as LocalMapLootMarkerPCView)?.Hide(); }));
                if (settings.toggleShowInterestingNPCsOnLocalMap) {
                    if (__instance.ViewModel is LocalMapCommonMarkerVM markerVM
                        && markerVM.m_Marker is AddLocalMapMarker.Runtime marker) {
                        var unit = marker.Owner;
                        UpdateMarker(__instance, unit);
                    }
                    if (__instance.ViewModel is LocalMapUnitMarkerVM unitMarkerVM) {
                        UpdateMarker(__instance, unitMarkerVM.m_Unit);
                    }
                }
            }

            private static void UpdateMarker(LocalMapMarkerPCView markerView, UnitEntityData unit) {
                var count = unit.GetDialogAndActionCounts();
                Mod.Debug($"{unit.CharacterName.orange()} -> dialogActionCounts: {count}");
                //var attentionMark = markerView.transform.Find("ToyBoxAttentionMark")?.gameObject;
                //Mod.Debug($"attentionMark: {attentionMark}");
                var markImage = markerView.transform.FindChild("Mark").GetComponent<Image>();
                if (count >= 1) {
                    Mod.Debug($"adding Mark to {unit.CharacterName.orange()}");
                    var mark = markerView.transform;
                    var uiRoot = UIHelpers.UIRoot;
#if false
                    if (attentionMark == null) {
                        var attentionPrototype = uiRoot.Find("TransitionViewPCView/Alushinyrra/LegendBlock/Nexus_Legend/Attention");
                        attentionMark = GameObject.Instantiate(attentionPrototype).gameObject;
                        attentionMark.name = "ToyBoxAttentionMark";
                        attentionMark.AddTo(mark);
                    }
                    attentionMark.SetActive(true);
#endif
                    markImage.color = new Color(1, 1f, 0);
                }
                else {
//                    attentionMark?.SetActive(false);
                    markImage.color = new Color(1, 1, 1);
                }
            }
        }

#if false
            // some experimental code to implement map rotation
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