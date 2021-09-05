using HarmonyLib;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox.Multiclass {
    public static class SkillPoint {
        public static int? GetTotalSkillPoints(UnitDescriptor unit, int nextLevel) {
            List<ClassData> classes = unit.Progression.Classes;
            
            int classCount = classes.Count;
            
            switch (Main.settings.multiclassSkillPointPolicy) {
                case ProgressionPolicy.Average:
                    return classes.Sum(cl => cl.CalcSkillPoints()) / classCount;
                case ProgressionPolicy.Largest:
                    return classes.Max(cl => cl.CalcSkillPoints());
                case ProgressionPolicy.Sum:
                    return classes.Sum(cl => cl.CalcSkillPoints());
                default:            
                    return null;
            }
            // TODO - figure out the right thing to do here
        }

        [HarmonyPatch(typeof(LevelUpHelper), "GetTotalSkillPoints")]
        public static class LevelUpHelper_GetTotalSkillPoints_Patch {
            public static bool Prefix(UnitDescriptor unit, int nextLevel, ref int __result) {
                int? totalSkillPoints = GetTotalSkillPoints(unit, nextLevel);
                
                if (totalSkillPoints.HasValue) {
                    __result = totalSkillPoints.Value;
                    return false;
                }

                return true;
            }
        }
    }
}
