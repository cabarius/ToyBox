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
using Kingmaker.Globalmap.State;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;

namespace ToyBox {
    public static partial class Teleport {
        public static Settings Settings => Main.Settings;
        //private static readonly HoverHandler _hover = new();

        public static void TeleportSelected() {
            foreach (var unit in  Shodan.SelectedUnits) {
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
                    if (unit != Shodan.MainCharacter) {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Shodan.MainCharacter.Position;
                    }
                }
            }
        }
        public static void TeleportEveryoneToPlayer() {
            var currentMode = Game.Instance.CurrentMode;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                foreach (var unit in Shodan.AllUnits) {
                    if (unit != Shodan.MainCharacter) {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Shodan.MainCharacter.Position;
                    }
                }
            }
        }

        public static void To(this BlueprintAreaEnterPoint enterPoint) => Shodan.EnterToArea(enterPoint);
        public static void To(this BlueprintArea area) {
            var areaEnterPoints = BlueprintExtensions.BlueprintsOfType<BlueprintAreaEnterPoint>();
            var blueprint = areaEnterPoints.FirstOrDefault(bp => bp is BlueprintAreaEnterPoint ep && ep.Area == area);
            if (blueprint is BlueprintAreaEnterPoint enterPoint) {
                ;Shodan.EnterToArea(enterPoint);
            }
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