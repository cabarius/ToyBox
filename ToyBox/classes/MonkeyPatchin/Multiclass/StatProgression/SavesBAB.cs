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
    //public enum MulticlassSaveBABType {
    //    SumOfAll = 0,
    //    UseMaxIncrement = 1,
    //    UseMaxValue = 2,
    //    OnlyPrimary = 3
    //};
    public static class SavesBAB {
        public static void ApplySingleStat(UnitDescriptor unit, LevelUpState state, BlueprintCharacterClass[] classes, StatType stat, BlueprintStatProgression[] statProgs, ProgressionPolicy policy = ProgressionPolicy.Largest) {
            //Main.Debug($"应用对{stat}的更改，{stat}现在是{unit.Stats.GetStat(stat).BaseValue}");
            int[] newClassLvls = classes.Select(a => unit.Progression.GetClassLevel(a)).ToArray();
            int classCount = newClassLvls.Length;
            int[] oldBonuses = new int[classCount];
            int[] newBonuses = new int[classCount];
            for (int i = 0; i < classCount; i++) {
                newBonuses[i] = statProgs[i].GetBonus(newClassLvls[i]);
                oldBonuses[i] = statProgs[i].GetBonus(newClassLvls[i] - 1);
            }
            
            /*
            Main.Debug("遍历所有class。");
            for(int i = 0; i < n_Class; i++) {
                Main.Debug($"{i} = {classes[i].name} {classes[i].Name}");
                Main.Debug($"新职业等级{newClassLvls[i]} 旧Bonus{oldBonuses[i]} 新Bonus{newBonuses[i]}");
            }
            */

            int mainClassIndex = classes.ToList().FindIndex(a => a == state.SelectedClass);
            //Logger.ModLoggerDebug($"mainClassIndex = {mainClassIndex}");
            int mainClassInc = newBonuses[mainClassIndex] - oldBonuses[mainClassIndex];

            int increase = 0;

            switch (policy) {
                case ProgressionPolicy.Average:
                    break;
                case ProgressionPolicy.Largest:
                    int maxOldValue = 0, maxNewValue = 0;
                    for (int i = 0; i < classCount; i++) maxOldValue = Math.Max(maxOldValue, oldBonuses[i]);
                    for (int i = 0; i < classCount; i++) maxNewValue = Math.Max(maxNewValue, newBonuses[i]);
                    increase = maxNewValue - maxOldValue;
                    unit.Stats.GetStat(stat).BaseValue += (increase - mainClassInc);
                    break;
                case ProgressionPolicy.Sum:
                    for (int i = 0; i < classCount; i++) increase += Math.Max(0, newBonuses[i] - oldBonuses[i]);
                    unit.Stats.GetStat(stat).BaseValue += (increase - mainClassInc);
                    break;
                default:
                    return;
            }

            /* Do we want this?
                if (type == MulticlassSaveBABType.UseMaxIncrement) {
                    for (int i = 0; i < classCount; i++) increase = Math.Max(increase, newBonuses[i] - oldBonuses[i]);
                    unit.Stats.GetStat(stat).BaseValue += (increase - mainClassInc);
                }
            */
            //Main.Debug($"应用完毕对{stat}的更改，{stat}现在是{unit.Stats.GetStat(stat).BaseValue}");
            //Logger.ModLoggerDebug($"Inc = {inc}");

            //Logger.ModLoggerDebug($"Inc Main Class = {mainClassInc}");
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
