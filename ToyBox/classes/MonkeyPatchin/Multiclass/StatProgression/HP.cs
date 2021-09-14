using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System.Linq;

namespace ToyBox.Multiclass {
    public static class HPDice {
        public static void ApplyHPDice(UnitDescriptor unit, LevelUpState state, BlueprintCharacterClass[] appliedClasses) {
            if (appliedClasses.Length <= 0) return;
            int[] newClassLvls = appliedClasses.Select(cl => unit.Progression.GetClassLevel(cl)).ToArray();
            int classCount = newClassLvls.Length;
            int[] hitDies = appliedClasses.Select(cl => (int)cl.HitDie).ToArray();

            int mainClassIndex = appliedClasses.ToList().FindIndex(ch => ch == state.SelectedClass);
            //Logger.ModLoggerDebug($"mainClassIndex = {mainClassIndex}");
            int mainClassHPDie = hitDies[mainClassIndex];

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
