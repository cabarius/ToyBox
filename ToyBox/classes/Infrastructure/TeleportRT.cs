// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
// based on code by hambeard (thank you ^_^)
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Code.UI.MVVM.View.ServiceWindows.LocalMap.PC;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameCommands;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.Blueprints.SectorMap;
using Kingmaker.Globalmap.View;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual.LocalMap;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
namespace ToyBox {
    public static partial class Teleport {

        public static void TeleportUnis(IEnumerable<BaseUnitEntity> units, Vector3 position)
            => CheatsTransfer.LocalTeleport(position, units);
        public static void TeleportUnit(BaseUnitEntity unit, Vector3 position)
            => CheatsTransfer.LocalTeleport(position, new List<BaseUnitEntity>() { unit });

        public static void TeleportTo(
            [NotNull] BlueprintAreaEnterPoint areaEnterPoint,
            bool includeFollowers = false,
            Action callback = null) {
            if (areaEnterPoint == null)
                throw new ArgumentException("areaEnterPoint is null", nameof(areaEnterPoint));
            if (Game.Instance.CurrentlyLoadedArea != areaEnterPoint.Area)
                throw new InvalidOperationException(string.Format(
                                                        "Cant teleport to {0}. Target zone {1} should be same as current {2}", areaEnterPoint,
                                                        areaEnterPoint.Area, Game.Instance.CurrentlyLoadedArea));
            LoadingProcess.Instance.StartLoadingProcess(Game.Instance.TeleportPartyCoroutine(areaEnterPoint, includeFollowers),
                                                        () => Game.ExecuteSafe(callback), LoadingProcessTag.TeleportParty);
            EventBus.RaiseEvent((Action<IAreaTransitionHandler>)(h => h.HandleAreaTransition()));
        }
        public static void TeleportToGlobalMap(Action callback = null) {
            var globalMap = BlueprintRoot.Instance.SectorMapArea;
            var areaEnterPoint = globalMap.SectorMapEnterPoint;
            //var areaEnterPoint = globalMap.All.FindOrDefault(i => i.Get().GlobalMapEnterPoint != null)?.Get().GlobalMapEnterPoint;
            Game.LoadArea(globalMap, areaEnterPoint, AutoSaveMode.None, callback: callback ?? (() => { }));
        }
#if false
        public static void To(this BlueprintSectorMapPoint mapPoint) {
            Game.Instance.LoadArea(mapPoint.
                                   .GlobalMap.GlobalMapEnterPoint, AutoSaveMode.None, () => { TeleportToGlobalMapPoint(mapPoint); });
            //if (!Teleport.TeleportToGlobalMapPoint(globalMapPoint)) {
            //    Teleport.TeleportToGlobalMap(() => Teleport.TeleportToGlobalMapPoint(globalMapPoint));
            //}
        }
#endif

    }
}