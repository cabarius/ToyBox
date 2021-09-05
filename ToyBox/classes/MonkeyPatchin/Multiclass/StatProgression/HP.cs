using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System.Linq;

namespace ToyBox.Multiclass {
    public static class HPDice {
        public static void ApplyHPDice(UnitDescriptor unit, LevelUpState state, BlueprintCharacterClass[] classes) {
            int[] newClassLvls = classes.Select(a => unit.Progression.GetClassLevel(a)).ToArray();

            int classCount = newClassLvls.Length;

            int[] hitDies = classes.Select(a => (int)(a.HitDie)).ToArray();

            int mainClassIndex = classes.ToList().FindIndex(a => a == state.SelectedClass);

            int currentHPIncrease = hitDies[mainClassIndex] / 2;

            int newIncrease;

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
                    return;
            }

            unit.Stats.GetStat(StatType.HitPoints).BaseValue += newIncrease - currentHPIncrease;
        }
    }
}