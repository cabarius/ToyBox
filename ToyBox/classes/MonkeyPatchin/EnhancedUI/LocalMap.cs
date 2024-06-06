using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.Common.Markers;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.PC;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Markers;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Interaction;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.LocalMap;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Kingmaker.UnitLogic.Interaction.SpawnerInteractionPart;

namespace ToyBox.BagOfPatches {
    internal static class LocalMapPatches {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        public static float Zoom = 1.0f;
        public static float Width = 0.0f;
        public static Vector3 Position = Vector3.zero;
        public static Vector3 FrameRotation = Vector3.zero;
        [HarmonyPatch(typeof(LocalMapVM))]
        internal static class LocalMapVMPatch {
            [HarmonyPatch(nameof(LocalMapVM.SetMarkers))]
            [HarmonyPrefix]
            private static bool SetMarkers(LocalMapVM __instance) {
                Mod.Debug($"LocalMapVM.SetMarkers");
                LocalMapModel.Markers.RemoveWhere(m => m.GetMarkerType() == LocalMapMarkType.Invalid);
                foreach (var marker in LocalMapModel.Markers)
                    if (LocalMapModel.IsInCurrentArea(marker.GetPosition()))
                        __instance.MarkersVm.Add(new LocalMapCommonMarkerVM(marker));
                IEnumerable<BaseUnitEntity> first = Game.Instance.Player.PartyAndPets;
                if (Game.Instance.Player.CapitalPartyMode)
                    first = first.Concat(Game.Instance.Player.RemoteCompanions.Where(u => !u.IsCustomCompanion()));
                foreach (var unit in first)
                    if (unit.View != null
                        && unit.View.enabled
                        && !unit
                            .LifeState
                            .IsHiddenBecauseDead
                        && LocalMapModel.IsInCurrentArea(unit.Position)
                        ) {
                        __instance.MarkersVm.Add(new LocalMapCharacterMarkerVM(unit));
                        __instance.MarkersVm.Add(new LocalMapDestinationMarkerVM(unit));
                    }

                foreach (var units in Shodan.MainCharacter
                                            .CombatGroup
                                            .Memory.UnitsList) {
                    Mod.Debug($"Checking {units.Unit.CharacterName}");
                    if (!units.Unit.IsPlayerFaction
                        && (units.Unit.IsVisibleForPlayer || units.Unit.InterestingnessCoefficent() > 0)
                        && !units.Unit.Descriptor()
                                 .LifeState.IsDead
                        && LocalMapModel.IsInCurrentArea(units.Unit.Position)
                       ) {
                        __instance.MarkersVm.Add(new LocalMapUnitMarkerVM(units));
                    }
                }
                return false;
            }
        }



        [HarmonyPatch(typeof(LocalMapMarkerPCView), nameof(LocalMapMarkerPCView.BindViewImplementation))]
        private static class LocalMapMarkerPCView_BindViewImplementation_Patch {
            [HarmonyPostfix]
            public static void Postfix(LocalMapMarkerPCView __instance) {
                if (__instance == null)
                    return;
                //Mod.Debug($"LocalMapMarkerPCView.BindViewImplementation - {__instance.ViewModel.MarkerType} - {__instance.ViewModel.GetType().Name}");
                if (__instance.ViewModel.MarkerType == LocalMapMarkType.Loot)
                    __instance.AddDisposable(__instance.ViewModel.IsVisible.Subscribe(value => {
                        (__instance as LocalMapLootMarkerPCView)?
                            .gameObject.SetActive(value);
                    }));
            }

            // Helper Function - Not a Patch
            private static void UpdateMarker(LocalMapMarkerPCView markerView, BaseUnitEntity unit) {
                var count = unit.InterestingnessCoefficent();
                //Mod.Debug($"{unit.CharacterName.orange()} -> unit interestingness: {count}");
                //var attentionMark = markerView.transform.Find("ToyBoxAttentionMark")?.gameObject;
                //Mod.Debug($"attentionMark: {attentionMark}");
                var markImage = markerView.transform.Find("Mark").GetComponent<Image>();
                if (count >= 1) {
                    //Mod.Debug($"adding Mark to {unit.CharacterName.orange()}");
                    var mark = markerView.transform;
                    markImage.color = new Color(1, 1f, 0);
                } else {
                    //                    attentionMark?.SetActive(false);
                    markImage.color = new Color(1, 1, 1);
                }
            }
        }

        [HarmonyPatch(typeof(UnitOvertipView))]
        private static class UnitOvertipViewPatch {
            [HarmonyPatch(nameof(UnitOvertipView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void BindViewImplementation(UnitOvertipView __instance) {
                if (!Settings.toggleShowInterestingNPCsOnLocalMap) return;
            }
            [HarmonyPatch(nameof(UnitOvertipView.UpdateVisibility))]
            [HarmonyPostfix]
            public static void UpdateInternal(UnitOvertipView __instance) {
                if (!Settings.toggleShowInterestingNPCsOnLocalMap || __instance is null) return;
            }
        }
#if false
        [HarmonyPatch(typeof(LocalMapMarkerPart))]
        public static class LocalMapMarkerPartPatch {
            [HarmonyPatch(nameof(LocalMapMarkerPart.OnTurnOn))]
            [HarmonyPostfix]
            public static void Markers() {
                Mod.Error($"Marker Add");
            }
        }
        [HarmonyPatch(typeof(AddLocalMapMarker.Runtime))]
        public static class AddLocalMapMarkerPatch {
            [HarmonyPatch(nameof(AddLocalMapMarker.Runtime.OnTurnOn))]
            [HarmonyPostfix]
            public static void Markers() {
                Mod.Error($"Marker Add");
            }
        }
        [HarmonyPatch(typeof(UnitEntityView))]
        public static class UnitEntityViewPatch {
            [HarmonyPatch(nameof(UnitEntityView.UpdateLootLocalMapMark))]
            [HarmonyPostfix]
            public static void Markers() {
                //Mod.Error($"Marker Add");
            }
        }
#endif
#if false
        [HarmonyPatch(typeof(EntityVisibilityForPlayerController))]
        public static class AddLocalMapMarkerRuntimePatch {
            [HarmonyPatch(nameof(EntityVisibilityForPlayerController.IsVisible), new Type[] { typeof(UnitEntityData)})]
            [HarmonyPrefix]
            private static bool IsUnitVisible(UnitEntityData unit, ref bool __result) {
                if (unit.GetUnitIterestingnessCoefficent() > 0) {
                    //__result = true;
                    return false;
                }
                return true;
            }
            [HarmonyPatch(nameof(EntityVisibilityForPlayerController.IsVisible), new Type[] { typeof(MapObjectEntityData)})]
            [HarmonyPrefix]
            private static bool IsMapObjectVisible(MapObjectEntityData mapObject, ref bool __result) {
                //__result = true;
                return false;
                //Mod.Debug($"MapObject: {mapObject}");
                return true;
            }
        }
#endif


#if false
#if false
                        var mapBlockRotation = mapBlock.localEulerAngles;
                        var frameRotation = frame.localEulerAngles;
                        var frameBlockRotation = frameBlock.localEulerAngles;
                        var mbPos = mapBlock.position;
                        var center = new Vector3(width, width, 0);
                        mapBlock.ResetRotation();
                        mapBlock.localPosition += center;
                        mapBlock.Rotate(mapBlock.forward, -frameRotation.z);
                        mapBlock.localPosition -= center;
                        //mapBlock.Rotate(new Vector3());
//                        mapBlock.localEulerAngles = mapBlockRotation;
                        Mod.Log($"frameRotation: {mapBlockRotation}");
#endif

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
                    var uiRoot = UIHelpers.UIRoot;
                    if (attentionMark == null) {
                        var attentionPrototype = uiRoot.Find("TransitionViewPCView/Alushinyrra/LegendBlock/Nexus_Legend/Attention");
                        attentionMark = GameObject.Instantiate(attentionPrototype).gameObject;
                        attentionMark.name = "ToyBoxAttentionMark";
                        attentionMark.AddTo(mark);
                    }
                    attentionMark.SetActive(true);
#endif
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


    }
}