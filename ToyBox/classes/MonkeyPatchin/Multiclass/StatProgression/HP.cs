using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.classes.MonkeyPatchin.Multiclass.StatProgression {
    public enum MulticlassHPDiceType {
        SimpleAdd = 0,
        UseMaxValue = 1,
        OnlyUsePrimary = 2
    };
    public static class HPDice {

        public static MulticlassHPDiceType type => (MulticlassHPDiceType)(Main.settings.multiClassHPDiceType);
        public static void ApplyHPDice(UnitDescriptor unit, LevelUpState state, BlueprintCharacterClass[] classes) {
            //Logger.ModLoggerDebug($"应用对{stat}的更改");
            int[] newClassLvls = classes.Select(a => unit.Progression.GetClassLevel(a)).ToArray();
            int n_Class = newClassLvls.Length;
            int[] hpDices = classes.Select(a => (int)(a.HitDie)).ToArray();

            int mainClassIndex = classes.ToList().FindIndex(a => a == state.SelectedClass);
            //Logger.ModLoggerDebug($"mainClassIndex = {mainClassIndex}");
            int mainClassHPDie = hpDices[mainClassIndex];

            int inc = 0;
            if (type == MulticlassHPDiceType.OnlyUsePrimary) {
                // do nothing
            }
            else {
                if (type == MulticlassHPDiceType.SimpleAdd) {
                    for (int i = 0; i < n_Class; i++) {
                        // WoTR takes half (or expectation) on hit dice when leveling up
                        if (i != mainClassIndex) unit.Stats.GetStat(StatType.HitPoints).BaseValue += hpDices[i] / 2;
                    }
                }
                else {
                    if (type == MulticlassHPDiceType.UseMaxValue) {
                        int maxHPDice = 0;
                        for (int i = 0; i < n_Class; i++) maxHPDice = Math.Max(maxHPDice, hpDices[i]);
                        unit.Stats.GetStat(StatType.HitPoints).BaseValue += (maxHPDice - hpDices[mainClassIndex]) / 2;
                    }
                }
            }
        }
    }
}
