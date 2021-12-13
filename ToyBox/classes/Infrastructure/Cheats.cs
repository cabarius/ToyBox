using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
    public static class Cheats {
        public static void RestSelected() {
            foreach (var selectedUnit in Game.Instance.UI.SelectionManager.SelectedUnits) {
                if (selectedUnit.Descriptor.State.IsFinallyDead) {
                    selectedUnit.Descriptor.Resurrect();
                    selectedUnit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                }

                RestController.RemoveNegativeEffects(selectedUnit.Descriptor);
                RestController.ApplyRest(selectedUnit.Descriptor);
                Rulebook.Trigger(new RuleHealDamage(selectedUnit, selectedUnit, default(DiceFormula), selectedUnit.Descriptor.Stats.HitPoints.ModifiedValue));
                foreach (var attribute in selectedUnit.Stats.Attributes) {
                    attribute.Damage = 0;
                    attribute.Drain = 0;
                }
            }
        }
    }
}
