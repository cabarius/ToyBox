using HarmonyLib;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox.Multiclass {
    public static class SkillPoint {
        public static int CalcTotalSkillPointsNonMythic(this ClassData cd) => cd.CharacterClass.IsMythic ? 0 : cd.CalcSkillPoints() * cd.Level;
        public static int? GetTotalSkillPoints(UnitDescriptor unit, int nextLevel) {
            var intelligenceSkillPoints = LevelUpHelper.GetTotalIntelligenceSkillPoints(unit, nextLevel);
            var classes = unit.Progression.Classes;
            var split = classes.GroupBy(cd => unit.IsClassGestalt(cd.CharacterClass));
            var total = 0;
            var gestaltCount = 0;
            var baseTotal = 0;
            var gestaltSumOrMax = 0;
            foreach (var group in split) {
                if (group.Key == false) baseTotal += group.ToList().Sum(cd => cd.CalcTotalSkillPointsNonMythic());
                else {
                    var gestaltClasses = group.ToList();
                    gestaltCount = gestaltClasses.Count;
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
            total = Main.settings.multiclassSkillPointPolicy switch {
                ProgressionPolicy.Largest => Mathf.Max(baseTotal, gestaltSumOrMax),
                ProgressionPolicy.Average => (gestaltSumOrMax + baseTotal) / (gestaltCount + 1),
                ProgressionPolicy.Sum => gestaltSumOrMax + baseTotal,
                _ => baseTotal,
            };
            return Mathf.Max(intelligenceSkillPoints + total, nextLevel);
        }

        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.GetTotalSkillPoints))]
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
