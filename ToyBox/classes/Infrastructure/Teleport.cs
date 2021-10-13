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

namespace ToyBox {
    public static class Teleport {
        public static Settings Settings => Main.settings;
        //private static readonly HoverHandler _hover = new();

        public static void TeleportUnit(UnitEntityData unit, Vector3 position) {
            var view = unit.View;

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
                    globalMapView.GetEdgeView(edge.Blueprint).UpdateRenderers();

                }
                globalMapController.StartTravels();
                EventBus.RaiseEvent<IGlobalMapPlayerTravelHandler>(h => h.HandleGlobalMapPlayerTravelStarted(globalMapView.State.Player, false));
                globalMapView.State.Player.SetCurrentPosition(new GlobalMapPosition(destination));
                globalMapView.GetPointView(destination)?.OpenOutgoingEdges((GlobalMapPointView)null);
                globalMapView.UpdatePawnPosition();
                globalMapController.Stop();
                EventBus.RaiseEvent<IGlobalMapPlayerTravelHandler>((Action<IGlobalMapPlayerTravelHandler>)(h => h.HandleGlobalMapPlayerTravelStopped((IGlobalMapTraveler)globalMapView.State.Player)));
                globalMapView.PlayerPawn.m_Compass.TryClear();
                globalMapView.PlayerPawn.m_Compass.TrySet();
#if false
                globalMapView.TeleportParty(globalMapPoint);
                globalMapUI.HandleGlobalMapPlayerTravelStopped(globalMapState.Player);
                     GlobalMapPointState pointState = Game.Instance.Player.GlobalMap.GetPointState(globalMapPoint);
                pointState.EdgesOpened = true;
                pointState.Reveal();
                GlobalMapPointView pointView = globalMapView.GetPointView(globalMapPoint);
                pointView?.OpenOutgoingEdges((GlobalMapPointView)null);
                globalMapView.RevealLocation(pointView);
                if ((bool)(UnityEngine.Object)globalMapView) {
                    if ((bool)(UnityEngine.Object)pointView)
                        globalMapView.RevealLocation(pointView);
                }
                globalMapView.UpdatePawnPosition();
                pointState.LastVisited = Game.Instance.TimeController.GameTime;
#endif
                return true;
            }
            return false;
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