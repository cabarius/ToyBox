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
            var gestaltCount = 0;
            int baseTotal = 0;
            int gestaltSumOrMax = 0;
            foreach (var group in split) {
                if (group.Key == false) baseTotal += group.ToList().Sum(cd => cd.CalcTotalSkillPointsNonMythic());
                else {
                    var gestaltClasses = group.ToList();
                    gestaltCount = classes.Count;
                    if (gestaltCount > 0) {
                        switch (Main.settings.multiclassSkillPointPolicy) {
                            case ProgressionPolicy.Largest:
                                gestaltSumOrMax += gestaltClasses.Max(cl => cl.CalcTotalSkillPointsNonMythic());
                                break;
                            case ProgressionPolicy.Average:
                            case ProgressionPolicy.Sum:
                                gestaltSumOrMax += gestaltClasses.Sum(cl => cl.CalcTotalSkillPointsNonMythic());
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            switch (Main.settings.multiclassSkillPointPolicy) {
                case ProgressionPolicy.Largest:
                    total = Mathf.Max(baseTotal, gestaltSumOrMax);
                    //if user gestalts unevenly this gets weird
                    //like if they level up to 2 in main class (that gives 2 points/level) an 1 in gestalt (that gives 6 points) this should return 6.
                    //but that should be fine? Hard to imagine how else it could work.
                    break;
                case ProgressionPolicy.Average:
                    total = (gestaltSumOrMax + baseTotal) / (gestaltCount + 1);
                    //if someone has uneven gestalt (5Pal / 5 Wiz / 1 Monk with Wiz and Monk as gestalt) Avg will bring skillpoint totals down
                    //but I suppose that's unavoidable
                    break;
                case ProgressionPolicy.Sum:
                    total = gestaltSumOrMax + baseTotal;
                    break;
                default:
                    total = baseTotal;
                    break;
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
                else return true;
            }
        }
    }
}
