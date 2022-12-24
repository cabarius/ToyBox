using Kingmaker;
using Kingmaker.Controllers.Rest;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;

namespace ToyBox {
    public static class Cheats {
        public static void RestSelected() {
            foreach (var selectedUnit in Game.Instance.UI.SelectionManager.SelectedUnits) {
                if (selectedUnit.Descriptor.State.IsFinallyDead) {
                    selectedUnit.Descriptor.Resurrect();
                    selectedUnit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                }

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
