using Kingmaker.EntitySystem.Stats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox.classes.Infrastructure {
    public static class HumanFriendlyStats {
        public static void EnsureFriendlyTypesContainAll() {
            if (Enum.GetValues(typeof(StatType)).Length != StatTypes.Count) {
                HashSet<int> friendlyTypes = new(StatTypes.Cast<int>().ToList());
                var missingTypes = Enum.GetValues(typeof(StatType)).Cast<int>().ToList()
                    .Where(orig => friendlyTypes.Contains(orig) == false)
                    .Select(x => (StatType)x);
                StatTypes.AddRange(missingTypes);
            }
        }

        public static List<StatType> StatTypes = new() {
            StatType.WarhammerWeaponSkill,
            StatType.WarhammerBallisticSkill,
            StatType.WarhammerStrength,
            StatType.WarhammerToughness,
            StatType.WarhammerAgility,
            StatType.WarhammerIntelligence,
            StatType.WarhammerPerception,
            StatType.WarhammerWillpower,
            StatType.WarhammerFellowship,
            StatType.WarhammerInitialAPBlue,
            StatType.WarhammerInitialAPYellow,
            StatType.HitPoints,
            StatType.Initiative,
            StatType.Speed,
            StatType.SkillAthletics,
            StatType.SkillAwareness,
            StatType.SkillCarouse,
            StatType.SkillPersuasion,
            StatType.SkillDemolition,
            StatType.SkillMedicae,
            StatType.SkillLogic,
            StatType.CheckBluff,
            StatType.CheckDiplomacy,
            StatType.CheckIntimidate
        };
    }
}
