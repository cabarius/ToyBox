using HarmonyLib;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.classes.MonkeyPatchin.Multiclass.StatProgression {
    public enum MulticlassSkillPointType {
        SimpleAdd = 0,
        UseTopN = 1,
        UseAverageN = 2
    };
    public static class SkillPoint {
        public static MulticlassSkillPointType type => (MulticlassSkillPointType)(Main.settings.multiClassSkillPointType);
        public static int? GetTotalSkillPoints(UnitDescriptor unit, int nextLevel) {
            if (MulticlassSkillPointType.SimpleAdd == type) return null;

            List<ClassData> classes = unit.Progression.Classes;
            List<int> classLevelSkillPoints = new List<int>();
            foreach(ClassData classData in classes) {
                for (int i = 1; i <= classData.Level; i++) classLevelSkillPoints.Add(classData.CalcSkillPoints());
            }
            classLevelSkillPoints = classLevelSkillPoints.OrderByDescending<int, int>(a => a).ToList();
            int n_TotClassLevel = classLevelSkillPoints.Count;
            int ans = LevelUpHelper.GetTotalIntelligenceSkillPoints(unit, nextLevel);
            if (MulticlassSkillPointType.UseTopN == type) {
                for (int i = 0; i < nextLevel; i++) ans += classLevelSkillPoints[i];
                return Math.Max(ans, nextLevel);
            }
            else {
                if(MulticlassSkillPointType.UseAverageN == type) {
                    int totClassSkillPoints = 0;
                    foreach (int x in classLevelSkillPoints) totClassSkillPoints += x;
                    ans = ans + (int)((double)totClassSkillPoints / n_TotClassLevel * nextLevel);
                    return Math.Max(ans, nextLevel);
                }
                else {
                    return null;
                }
            }
        }

        [HarmonyPatch(typeof(LevelUpHelper), "GetTotalSkillPoints")]
        public static class LevelUpHelper_GetTotalSkillPoints_Patch {
            public static bool Prefix(UnitDescriptor unit, int nextLevel, ref int __result) {
                int? ans = GetTotalSkillPoints(unit, nextLevel);
                if (ans.HasValue) {
                    __result = ans.Value;
                    return false;
                }
                else return true;
            }
        }
    }
}
