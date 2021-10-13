using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System.Linq;

namespace ToyBox.Multiclass {
    public static class HPDice {
        public static void ApplyHPDice(UnitDescriptor unit, LevelUpState state, BlueprintCharacterClass[] appliedClasses) {
            if (appliedClasses.Count() <= 0) return;
            var newClassLvls = appliedClasses.Select(cl => unit.Progression.GetClassLevel(cl)).ToArray();
            var classCount = newClassLvls.Length;
            var hitDies = appliedClasses.Select(cl => (int)cl.HitDie).ToArray();

            var mainClassIndex = appliedClasses.ToList().FindIndex(ch => ch == state.SelectedClass);
            //Logger.ModLoggerDebug($"mainClassIndex = {mainClassIndex}");
            var mainClassHPDie = hitDies[mainClassIndex];

            var currentHPIncrease = hitDies[mainClassIndex];
            var newIncrease = currentHPIncrease;
            switch (Main.settings.multiclassHitPointPolicy) {
                case ProgressionPolicy.Average:
                    newIncrease = hitDies.Sum() / classCount;
                    break;
                case ProgressionPolicy.Largest:
                    newIncrease = hitDies.Max();
                    break;
                case ProgressionPolicy.Sum:
                    newIncrease = hitDies.Sum();
                    break;
                default:
                    break; ;
            }
            unit.Stats.GetStat(StatType.HitPoints).BaseValue += newIncrease - currentHPIncrease;
        }
    }
}
