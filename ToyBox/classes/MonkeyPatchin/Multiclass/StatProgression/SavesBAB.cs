using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System;
using System.Linq;

namespace ToyBox.Multiclass {
    public static class SavesBAB {
        public static void ApplySingleStat(UnitDescriptor unit,
                                           LevelUpState state,
                                           BlueprintCharacterClass[] classes,
                                           StatType stat,
                                           BlueprintStatProgression[] statProgs,
                                           ProgressionPolicy policy = ProgressionPolicy.Largest) {
            int[] newClassLvls = classes.Select(a => unit.Progression.GetClassLevel(a)).ToArray();

            int classCount = newClassLvls.Length;

            int[] oldBonuses = new int[classCount];
            int[] newBonuses = new int[classCount];

            for (int i = 0; i < classCount; i++) {
                newBonuses[i] = statProgs[i].GetBonus(newClassLvls[i]);
                oldBonuses[i] = statProgs[i].GetBonus(newClassLvls[i] - 1);
            }

            int mainClassIndex = classes.ToList().FindIndex(a => a == state.SelectedClass);

            int mainClassInc = newBonuses[mainClassIndex] - oldBonuses[mainClassIndex];

            int increase = 0;

            switch (policy) {
                case ProgressionPolicy.Average:
                    break;

                case ProgressionPolicy.Largest:
                    int maxOldValue = 0, maxNewValue = 0;

                    for (int i = 0; i < classCount; i++) {
                        maxOldValue = Math.Max(maxOldValue, oldBonuses[i]);
                    }

                    for (int i = 0; i < classCount; i++) {
                        maxNewValue = Math.Max(maxNewValue, newBonuses[i]);
                    }

                    increase = maxNewValue - maxOldValue;
                    unit.Stats.GetStat(stat).BaseValue += (increase - mainClassInc);

                    break;

                case ProgressionPolicy.Sum:
                    for (int i = 0; i < classCount; i++) {
                        increase += Math.Max(0, newBonuses[i] - oldBonuses[i]);
                    }

                    unit.Stats.GetStat(stat).BaseValue += (increase - mainClassInc);

                    break;

                default:
                    return;
            }
        }

        public static void ApplySaveBAB(UnitDescriptor unit, LevelUpState state, BlueprintCharacterClass[] classes) {
            ApplySingleStat(
                unit,
                state,
                classes,
                StatType.BaseAttackBonus,
                classes.Select(a => a.BaseAttackBonus).ToArray(),
                Main.settings.multiclassBABPolicy
            );

            ApplySingleStat(
                unit,
                state,
                classes,
                StatType.SaveFortitude,
                classes.Select(a => a.FortitudeSave).ToArray(),
                Main.settings.multiclassSavingThrowPolicy
            );

            ApplySingleStat(
                unit,
                state,
                classes,
                StatType.SaveReflex,
                classes.Select(a => a.ReflexSave).ToArray(),
                Main.settings.multiclassSavingThrowPolicy
            );

            ApplySingleStat(
                unit,
                state,
                classes,
                StatType.SaveWill,
                classes.Select(a => a.WillSave).ToArray(),
                Main.settings.multiclassSavingThrowPolicy
            );
        }
    }
}