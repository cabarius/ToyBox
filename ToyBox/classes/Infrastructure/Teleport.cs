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
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Globalmap;
using Kingmaker.Utility;
using Kingmaker.EntitySystem.Persistence;
using ModKit;
using UnityModManagerNet;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap;
using Kingmaker.Visual.LocalMap;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.Blueprints.Area;
using Kingmaker.Designers;
using System.Linq;

namespace ToyBox {
    public static class Teleport {
        public static Settings Settings => Main.settings;
        //private static readonly HoverHandler _hover = new();

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

        public static void TeleportSelected() {
            foreach (var unit in Game.Instance.UI.SelectionManager.SelectedUnits) {
                TeleportUnit(unit, Utils.PointerPosition());
            }
        }

        public static void TeleportParty() {
            foreach (var unit in Game.Instance.Player.m_PartyAndPets) {
                TeleportUnit(unit, Utils.PointerPosition());
            }
        }
        public static void TeleportPartyToPlayer() {
            var currentMode = Game.Instance.CurrentMode;
            var partyMembers = Game.Instance.Player.m_PartyAndPets;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                foreach (var unit in partyMembers) {
                    if (unit != Game.Instance.Player.MainCharacter.Value) {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                    }
                }
            }
        }
        public static void TeleportEveryoneToPlayer() {
            var currentMode = Game.Instance.CurrentMode;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                foreach (var unit in Game.Instance.State.Units) {
                    if (unit != Game.Instance.Player.MainCharacter.Value) {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                    }
                }
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

        public static void To(this BlueprintAreaEnterPoint enterPoint) => GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
        public static void To(this BlueprintGlobalMap globalMap) => GameHelper.EnterToArea(globalMap.GlobalMapEnterPoint, AutoSaveMode.None);
        public static void To(this BlueprintArea area) {
            var areaEnterPoints = BlueprintExensions.BlueprintsOfType<BlueprintAreaEnterPoint>();
            var blueprint = areaEnterPoints.FirstOrDefault(bp => bp is BlueprintAreaEnterPoint ep && ep.Area == area);
            if (blueprint is BlueprintAreaEnterPoint enterPoint) {
                GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
            }
        }
        public static void To(this BlueprintGlobalMapPoint globalMapPoint) {
            Game.Instance.LoadArea(globalMapPoint.GlobalMap.GlobalMapEnterPoint, AutoSaveMode.None, () => {
                TeleportToGlobalMapPoint(globalMapPoint);
            });
            //if (!Teleport.TeleportToGlobalMapPoint(globalMapPoint)) {
            //    Teleport.TeleportToGlobalMap(() => Teleport.TeleportToGlobalMapPoint(globalMapPoint));
            //}
        }

        internal class HoverHandler : IUnitDirectHoverUIHandler, IDisposable {
            public UnitEntityData Unit { get; private set; }
            private UnitEntityData _currentUnit;

            public HoverHandler() {
                EventBus.Subscribe(this);
            }
            public void Dispose() => throw new NotImplementedException();

            public void HandleHoverChange([NotNull] UnitEntityView unitEntityView, bool isHover) {
                if (isHover) _currentUnit = unitEntityView.Data;
            }

            public void LockUnit() {
                if (_currentUnit != null)
                    this.Unit = _currentUnit;
            }
        }
    }
}