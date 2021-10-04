using Kingmaker.EntitySystem.Stats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox.classes.Infrastructure {
    public static class HumanFriendly {
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
            StatType.Unknown,
            StatType.Strength,
            StatType.Dexterity,
            StatType.Constitution,
            StatType.Intelligence,
            StatType.Wisdom,
            StatType.Charisma,

            StatType.BaseAttackBonus,
            StatType.AdditionalAttackBonus,
            StatType.AdditionalDamage,
            StatType.AttackOfOpportunityCount,
            StatType.Reach,
            StatType.SneakAttack,

            StatType.HitPoints,
            StatType.TemporaryHitPoints,
            StatType.DamageNonLethal,
            StatType.AC,
            StatType.AdditionalCMB,
            StatType.AdditionalCMD,
            StatType.SaveFortitude,
            StatType.SaveWill,
            StatType.SaveReflex,
            StatType.Initiative,
            StatType.Speed,

            StatType.SkillAthletics,
            StatType.SkillKnowledgeArcana,
            StatType.SkillKnowledgeWorld,
            StatType.SkillLoreNature,
            StatType.SkillLoreReligion,
            StatType.SkillMobility,
            StatType.SkillPerception,
            StatType.SkillPersuasion,
            StatType.SkillStealth,
            StatType.SkillThievery,
            StatType.SkillUseMagicDevice,
            StatType.CheckBluff,
            StatType.CheckDiplomacy,
            StatType.CheckIntimidate
        };
    }
}
