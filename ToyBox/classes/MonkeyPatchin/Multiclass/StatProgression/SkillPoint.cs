using HarmonyLib;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox.Multiclass {
    public static class SkillPoint {
        public static int CalcTotalSkillPointsNonMythic(this ClassData cd) { 
            return cd.CharacterClass.IsMythic ? 0 : cd.CalcSkillPoints() * cd.Level;
        }
        public static int? GetTotalSkillPoints(UnitDescriptor unit, int nextLevel) {
            int intelligenceSkillPoints = LevelUpHelper.GetTotalIntelligenceSkillPoints(unit, nextLevel);
            List<ClassData> classes = unit.Progression.Classes;
            var split = classes.GroupBy(cd => unit.IsClassGestalt(cd.CharacterClass));
            int total = 0;
            foreach (var group in split) {
                if (group.Key == false) total += group.ToList().Sum(cd => cd.CalcTotalSkillPointsNonMythic());
                else {
                    var gestaltClasses = group.ToList();
                    var gestaltCount = classes.Count;
                    if (gestaltCount > 0) {
                        switch (Main.settings.multiclassSkillPointPolicy) {
                            case ProgressionPolicy.Average:
                                total += gestaltClasses.Sum(cl => cl.CalcTotalSkillPointsNonMythic()) / gestaltCount;
                                break;
                            case ProgressionPolicy.Largest:
                                total += gestaltClasses.Max(cl => cl.CalcTotalSkillPointsNonMythic());
                                break;
                            case ProgressionPolicy.Sum:
                                total += gestaltClasses.Sum(cl => cl.CalcTotalSkillPointsNonMythic());
                                break;
                        }
                    }
                }
            }
            return Mathf.Max(intelligenceSkillPoints + total, nextLevel);
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

                return true;
            }
        }
    }
}
