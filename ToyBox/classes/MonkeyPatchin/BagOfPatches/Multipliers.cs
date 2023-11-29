using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using ModKit;
using System;
using UnityEngine;

namespace ToyBox.BagOfPatches {
    internal static class Multipliers {
        public static Settings settings = Main.Settings;

        [HarmonyPatch(typeof(BuffCollection))]
        public static class BuffCollection_Patch {
            private static bool isGoodBuff(BlueprintBuff blueprint) => !blueprint.Harmful && !settings.buffsToIgnoreForDurationMultiplier.Contains(blueprint.AssetGuidThreadSafe);
            [HarmonyPatch(nameof(BuffCollection.Add), new Type[] { typeof(BlueprintBuff), typeof(MechanicEntity), typeof(MechanicsContext), typeof(BuffDuration) })]
            [HarmonyPostfix]
            public static void Add(BlueprintBuff blueprint, MechanicEntity caster, MechanicsContext parentContext, ref BuffDuration duration) {
                try {
                    if (!caster.IsPlayerEnemy && isGoodBuff(blueprint)) {
                        if (!duration.IsPermanent) {
                            var newRounds = new Kingmaker.Utility.Rounds(Mathf.FloorToInt(duration.Rounds.Value.Value * settings.buffDurationMultiplierValue));
                            duration = new(newRounds, duration.EndCondition);
                        }
                    }
                }
                catch (Exception ex) {
                    Mod.Error(ex);
                }
            }
        }
        [HarmonyPatch(typeof(ExperienceHelper))]
        public static class ExperienceHelper_Patch {
            [HarmonyPatch(nameof(ExperienceHelper.GetXp))]
            [HarmonyPostfix]
            public static void GetXp(ref int __result, EncounterType type) {
                float mult = settings.experienceMultiplier;
                if (Game.Instance.CurrentMode == GameModeType.SpaceCombat) {
                    if (settings.useSpaceExpSlider) {
                        mult = settings.experienceMultiplierSpace;
                    }
                }
                else {
                    switch (type) {
                        case EncounterType.QuestNormal:
                        case EncounterType.QuestMain: {
                                if (settings.useQuestsExpSlider) {
                                    mult = settings.experienceMultiplierQuests;
                                }
                            }
                            break;
                        case EncounterType.Mob:
                        case EncounterType.Boss: {
                                if (settings.useCombatExpSlider) {
                                    mult = settings.experienceMultiplierCombat;
                                }
                            }
                            break;
                        case EncounterType.ChallengeMinor:
                        case EncounterType.ChallengeMajor: {
                                if (settings.useChallengesExpSlider) {
                                    mult = settings.experienceMultiplierChallenges;
                                }
                            }
                            break;
                        case EncounterType.SkillCheck: {
                                if (settings.useSkillChecksExpSlider) {
                                    mult = settings.experienceMultiplierSkillChecks;
                                }
                            }
                            break;
                    }
                }
                if (mult != 1) {
                    __result = Mathf.RoundToInt(__result * mult);
                }
            }
        }
    }
}