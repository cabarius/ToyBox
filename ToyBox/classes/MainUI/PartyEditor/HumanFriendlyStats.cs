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
#if Wrath
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
#elif RT
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
#endif
#if Wrath
            StatType.AttackOfOpportunityCount,
            StatType.Reach,
            StatType.SneakAttack,
#endif
            StatType.HitPoints,
#if Wrath
            StatType.TemporaryHitPoints,
            StatType.DamageNonLethal,
            StatType.AC,
            StatType.AdditionalCMB,
            StatType.AdditionalCMD,
            StatType.SaveFortitude,
            StatType.SaveWill,
            StatType.SaveReflex,
#endif
            StatType.Initiative,
            StatType.Speed,
            StatType.SkillAthletics,
#if Wrath
            StatType.SkillKnowledgeArcana,
            StatType.SkillKnowledgeWorld,
            StatType.SkillLoreNature,
            StatType.SkillLoreReligion,
            StatType.SkillMobility,
            StatType.SkillPerception,
#elif RT
            StatType.SkillAwareness,
            StatType.SkillCarouse,
#endif
            StatType.SkillPersuasion,
#if Wrath
            StatType.SkillStealth,
            StatType.SkillThievery,
            StatType.SkillUseMagicDevice,
#elif RT
            StatType.SkillDemolition,
            StatType.SkillMedicae,
            StatType.SkillLogic,
#endif
            StatType.CheckBluff,
            StatType.CheckDiplomacy,
            StatType.CheckIntimidate
        };
    }
}
