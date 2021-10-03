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
    class RespecHelper {

        public static List<UnitEntityData> GetRespecableUnits() {
            Player player = Game.Instance.Player;
            var enumerable = player.AllCrossSceneUnits.Where(delegate (UnitEntityData u) {
                UnitPartCompanion unitPartCompanion = u.Get<UnitPartCompanion>();
                if (unitPartCompanion == null || unitPartCompanion.State != CompanionState.InParty) {
                    UnitPartCompanion unitPartCompanion2 = u.Get<UnitPartCompanion>();
                    if (unitPartCompanion2 == null) {
                        return false;
                    }

                    return unitPartCompanion2.State == CompanionState.Remote;
                }

                return true;
            });
            List<UnitEntityData> respecUnits = (from ch in enumerable
                                                where RespecCompanion.CanRespec(ch)
                                                select ch).ToList();
            return respecUnits;
        }

        public static void Respec (UnitEntityData unit, Action successCallback = null) {
            Mod.Verbose("Initiating Respec");
            EventBus.RaiseEvent(delegate (IRespecInitiateUIHandler h) {
                h.HandleRespecInitiate(unit, FinishRespec);
            });

        }

        private static void FinishRespec() {
            Mod.Verbose("Finishing Respec");
            // Maybe Apply Rest Without Advancing Time ?
        }
    }
}
