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

namespace ToyBox.Multiclass {
    public static class SkillPoint {
        public static int? GetTotalSkillPoints(UnitDescriptor unit, int nextLevel) {
            List<ClassData> classes = unit.Progression.Classes;
            var split = classes.GroupBy(cd => unit.IsClassGestalt(cd.CharacterClass));
            int total = 0;
            foreach (var group in split) {
                if (group.Key == false) total += group.ToList().Sum(cd => cd.CalcSkillPoints());
                else {
                    var gestaltClasses = group.ToList();
                    var gestaltCount = classes.Count;
                    if (gestaltCount > 0) {
                        switch (Main.settings.multiclassSkillPointPolicy) {
                            case ProgressionPolicy.Average:
                                total += gestaltClasses.Sum(cl => cl.CalcSkillPoints()) / gestaltCount;
                                break;
                            case ProgressionPolicy.Largest:
                                total += gestaltClasses.Max(cl => cl.CalcSkillPoints());
                                break;
                            case ProgressionPolicy.Sum:
                                total += gestaltClasses.Sum(cl => cl.CalcSkillPoints());
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            return total;
            // TODO - figure out the right thing to do here
#if false
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
#endif
        }

        [HarmonyPatch(typeof(LevelUpHelper), "GetTotalSkillPoints")]
        public static class LevelUpHelper_GetTotalSkillPoints_Patch {
            public static bool Prefix(UnitDescriptor unit, int nextLevel, ref int __result) {
                if (!MultipleClasses.IsAvailable()) return true;
                var totalSkillPoints = GetTotalSkillPoints(unit, nextLevel);
                if (totalSkillPoints.HasValue) {
                    __result = totalSkillPoints.Value;
                    return false;
                }
                else return true;
            }
        }
    }
}
