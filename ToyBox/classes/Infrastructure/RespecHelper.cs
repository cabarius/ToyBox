using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Parts;
using System;
using System.Collections.Generic;
using System.Linq;
using ModKit;

namespace ToyBox {
    internal class RespecHelper {

        public static List<UnitEntityData> GetRespecableUnits() {
            var player = Game.Instance.Player;
            var enumerable = player.AllCrossSceneUnits.Where(delegate (UnitEntityData u) {
                var unitPartCompanion = u.Get<UnitPartCompanion>();
                if (unitPartCompanion == null || unitPartCompanion.State != CompanionState.InParty) {
                    var unitPartCompanion2 = u.Get<UnitPartCompanion>();
                    if (unitPartCompanion2 == null) {
                        return false;
                    }

                    return unitPartCompanion2.State == CompanionState.Remote;
                }

                return true;
            });
            var respecUnits = (from ch in enumerable
                               where RespecCompanion.CanRespec(ch)
                               select ch).ToList();
            return respecUnits;
        }

        public static void Respec(UnitEntityData unit) {
            Mod.Debug("Initiating Respec");
            EventBus.RaiseEvent(delegate (IRespecInitiateUIHandler h) {
                h.HandleRespecInitiate(unit, FinishRespec);
            });

        }

        private static void FinishRespec() => Mod.Debug("Finishing Respec");// Maybe Apply Rest Without Advancing Time ?
    }
}
