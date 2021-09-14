// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// based on code by hambeard (thank you ^_^)

using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using System;
using UnityEngine;

namespace ToyBox {

    public static class Teleport {
        private static HoverHandler _hover = new();
        public static void OnUpdate() {
            if ((Game.Instance.CurrentMode != GameModeType.Default || Game.Instance.CurrentMode != GameModeType.Pause) && Main.IsInGame) {
                if (Input.GetKeyDown(KeyCode.Period))
                    TeleportUnit(Game.Instance.Player.MainCharacter.Value, PointerPosition());
                else if (Input.GetKeyDown(KeyCode.Comma))
                    TeleportSelected();
                else if (Input.GetKeyDown(KeyCode.Slash))
                    _hover.LockUnit();
                else if (Input.GetKeyDown(KeyCode.Semicolon))
                    if (_hover.Unit != null) TeleportUnit(_hover.Unit, PointerPosition());
            }
    }

    private static Vector3 PointerPosition() {
            Vector3 result = new();

            Camera camera = Game.GetCamera();
            RaycastHit raycastHit = default(RaycastHit);
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out raycastHit, camera.farClipPlane, 21761)) {
                result = raycastHit.point;
            }
            return result;
        }

        private static void TeleportUnit(UnitEntityData unit, Vector3 position) {
            UnitEntityView view = unit.View;

            if (view != null) view.StopMoving();

            unit.Stop();
            unit.Position = position;

            foreach (var fam in unit.Familiars) {
                if (fam)
                    fam.TeleportToMaster();
            }
        }

        private static void TeleportSelected() {
            foreach (var unit in Game.Instance.UI.SelectionManager.SelectedUnits) {
                TeleportUnit(unit, PointerPosition());
            }
        }

        internal class HoverHandler : IUnitDirectHoverUIHandler, IDisposable {
            public UnitEntityData Unit { get; private set; }
            private UnitEntityData _currentUnit;

            public HoverHandler() {
                EventBus.Subscribe(this);
            }
            public void Dispose() {
                throw new NotImplementedException();
            }

            public void HandleHoverChange([NotNull] UnitEntityView unitEntityView, bool isHover) {
                if (isHover) _currentUnit = unitEntityView.Data;
            }

            public void LockUnit() {
                if (_currentUnit != null)
                    Unit = _currentUnit;
            }
        }
    }
}