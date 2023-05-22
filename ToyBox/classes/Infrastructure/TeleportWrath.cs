// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// based on code by hambeard (thank you ^_^)
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using System;
using UnityEngine;
using JetBrains.Annotations;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.View;
using Kingmaker.Globalmap;
using Kingmaker.Utility;
using Kingmaker.EntitySystem.Persistence;
using ModKit;
using UnityModManagerNet;
using Kingmaker.Visual.LocalMap;
using Kingmaker.Blueprints.Area;
using Kingmaker.Designers;
using System.Linq;
using Kingmaker.Cheats;
using static Kingmaker.Cheats.CheatsTransfer;
#if Wrath
using Kingmaker.Globalmap.State;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
#elif RT
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.PC;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.PubSubSystem.Core;
#endif
namespace ToyBox {
    public static partial class Teleport {
        public static void TeleportUnit(UnitEntityData unit, Vector3 position) {
            var view = unit.View;
            var localMap = Game.Instance?.UI.Canvas?.transform?.Find("ServiceWindowsPCView/LocalMapPCView");
            if (localMap?.gameObject.activeSelf ?? false) {
                var localMapView = localMap.GetComponent<LocalMapPCView>();
                var viewModel = localMapView.ViewModel;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(localMapView.m_Image.rectTransform, (Vector2)Input.mousePosition, Game.Instance.UI.UICamera, out var localPoint);
                var localPos = localPoint + Vector2.Scale(localMapView.m_Image.rectTransform.sizeDelta, localMapView.m_Image.rectTransform.pivot);
                var vector3 = LocalMapRenderer.Instance.ViewportToWorldPoint((Vector3)new Vector2(localPos.x / (float)viewModel.DrawResult.Value.ColorRT.width, localPos.y / (float)viewModel.DrawResult.Value.ColorRT.height));
                if (!LocalMapModel.IsInCurrentArea(vector3))
                    vector3 = AreaService.Instance.CurrentAreaPart.Bounds.LocalMapBounds.ClosestPoint(vector3);
                Mod.Debug($"PointerPosition - adjusting result {position} to {vector3}");
                Game.Instance.UI.GetCameraRig().ScrollTo(vector3);
                position = vector3;
            }
            if (view != null) view.StopMoving();
            unit.Stop();

            unit.Position = position;

            foreach (var fam in unit.Familiars) {
                if (fam)
                    fam.TeleportToMaster(false);
            }
        }

        public static void TeleportPartyOnGlobalMap() {
            _ = GlobalMapView.Instance;
            var pointerPos = Utils.PointerPosition();
            var pointerTransform = new GameObject().transform;
            pointerTransform.position = pointerPos;
            var locationToObject = GlobalMapView.Instance.GetNearestLocationToObject(pointerTransform);
            locationToObject.Blueprint.TeleportToGlobalMapPoint();
        }
        public static void TeleportToGlobalMap(Action callback = null) {
            var globalMap = Game.Instance.BlueprintRoot.GlobalMap;
            var areaEnterPoint = globalMap.All.FindOrDefault(i => i.Get().GlobalMapEnterPoint != null)?.Get().GlobalMapEnterPoint;
            Game.Instance.LoadArea(areaEnterPoint.Area, areaEnterPoint, AutoSaveMode.None, callback: callback ?? (() => { }));
        }
        public static bool TeleportToGlobalMapPoint(this BlueprintGlobalMapPoint destination) {
            if (GlobalMapView.Instance != null) {
                var globalMapController = Game.Instance.GlobalMapController;
                var globalMapUI = Game.Instance.UI.GlobalMapUI;
                var globalMapView = GlobalMapView.Instance;
                var globalMapState = Game.Instance.Player.GetGlobalMap(destination.GlobalMap);

                var pointState = Game.Instance.Player.GetGlobalMap(destination.GlobalMap).GetPointState(destination);
                pointState.EdgesOpened = true;
                pointState.Reveal();
                var pointView = globalMapView.GetPointView(destination);
                if ((bool)(UnityEngine.Object)globalMapView) {
                    if ((bool)(UnityEngine.Object)pointView)
                        globalMapView.RevealLocation(pointView);
                }
                foreach (var edge in pointState.Edges) {
                    edge.UpdateExplored(1f, 1);
                    globalMapView.GetEdgeView(edge.Blueprint)?.UpdateRenderers();

                }
                globalMapController.StartTravels();
                EventBus.RaiseEvent<IGlobalMapPlayerTravelHandler>(h => h.HandleGlobalMapPlayerTravelStarted(globalMapView.State.Player, false));
                globalMapView.State.Player.SetCurrentPosition(new GlobalMapPosition(destination));
                globalMapView.GetPointView(destination)?.OpenOutgoingEdges((GlobalMapPointView)null);
                globalMapView.UpdatePawnPosition();
                globalMapController.Stop();
                EventBus.RaiseEvent<IGlobalMapPlayerTravelHandler>((Action<IGlobalMapPlayerTravelHandler>)(h => h.HandleGlobalMapPlayerTravelStopped((IGlobalMapTraveler)globalMapView.State.Player)));
                globalMapView.PlayerPawn?.m_Compass?.TryClear();
                globalMapView.PlayerPawn?.m_Compass?.TrySet();
                return true;
            }
            return false;
        }
#if false
        public static void To(this BlueprintAreaEnterPoint enterPoint) => GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
        public static void To(this BlueprintArea area) {
            var areaEnterPoints = BlueprintExtensions.BlueprintsOfType<BlueprintAreaEnterPoint>();
            var blueprint = areaEnterPoints.FirstOrDefault(bp => bp is BlueprintAreaEnterPoint ep && ep.Area == area);
            if (blueprint is BlueprintAreaEnterPoint enterPoint) {
                GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
            }
        }
#endif
        public static void To(this BlueprintGlobalMap globalMap) => GameHelper.EnterToArea(globalMap.GlobalMapEnterPoint, AutoSaveMode.None);
        public static void To(this BlueprintGlobalMapPoint globalMapPoint) {
            Game.Instance.LoadArea(globalMapPoint.GlobalMap.GlobalMapEnterPoint, AutoSaveMode.None, () => { TeleportToGlobalMapPoint(globalMapPoint); });
            //if (!Teleport.TeleportToGlobalMapPoint(globalMapPoint)) {
            //    Teleport.TeleportToGlobalMap(() => Teleport.TeleportToGlobalMapPoint(globalMapPoint));
            //}
        }
    }
}