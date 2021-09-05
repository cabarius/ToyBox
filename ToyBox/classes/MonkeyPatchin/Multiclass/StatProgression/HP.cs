using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.Multiclass {
    public static class HPDice {
        public static void ApplyHPDice(UnitDescriptor unit, LevelUpState state, BlueprintCharacterClass[] classes) {
            
            //Logger.ModLoggerDebug($"应用对{stat}的更改");
            int[] newClassLvls = classes.Select(a => unit.Progression.GetClassLevel(a)).ToArray();
            int classCount = newClassLvls.Length;
            int[] hitDies = classes.Select(a => (int)(a.HitDie)).ToArray();

            int mainClassIndex = classes.ToList().FindIndex(a => a == state.SelectedClass);
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
                    return;
            }
            unit.Stats.GetStat(StatType.HitPoints).BaseValue += newIncrease;
        }
    }
}
