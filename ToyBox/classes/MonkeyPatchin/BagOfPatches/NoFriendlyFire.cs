// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches
{
    static class NoFriendlyFire
    {
        public static Settings settings = Main.settings;

        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;

        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(AbilityTargetsAround), "Select")]
        public static class AbilityTargetsAround_Select_Patch
        {
            public static void Postfix(ref IEnumerable<TargetWrapper> __result,
                                       AbilityTargetsAround __instance,
                                       ConditionsChecker ___m_Condition,
                                       AbilityExecutionContext context,
                                       TargetWrapper anchor)
            {
                if (!settings.toggleNoFriendlyFireForAOE)
                {
                    return;
                }

                UnitEntityData caster = context.MaybeCaster;
                IEnumerable<UnitEntityData> targets = GameHelper.GetTargetsAround(anchor.Point, __instance.AoERadius);

                if (caster == null)
                {
                    __result = Enumerable.Empty<TargetWrapper>();

                    return;
                }

                switch (__instance.m_TargetType)
                {
                    case TargetType.Enemy:
                        targets = targets.Where(caster.IsEnemy);

                        break;

                    case TargetType.Ally:
                        targets = targets.Where(caster.IsAlly);

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();

                    case TargetType.Any:
                        break;
                }

                if (___m_Condition.HasConditions)
                {
                    targets = targets.Where(u =>
                                            {
                                                using (context.GetDataScope(u)) { return ___m_Condition.Check(); }
                                            }).ToList();
                }

                if (caster.IsPlayerFaction && (context.AbilityBlueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful 
                                               || context.AbilityBlueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))
                {
                    if (context.AbilityBlueprint.HasLogic<AbilityUseOnRest>())
                    {
                        var abilityUseOnRest = context.AbilityBlueprint.GetComponent<AbilityUseOnRest>();

                        if (abilityUseOnRest != null)
                        {
                            AbilityUseOnRestType componentType = abilityUseOnRest.Type;

                            bool healDamage = componentType == AbilityUseOnRestType.HealDamage;

                            targets = targets.Where(target =>
                                                    {
                                                        if (target.IsPlayerFaction && !healDamage)
                                                        {
                                                            bool forUndead = componentType == AbilityUseOnRestType.HealMassUndead
                                                                             || componentType == AbilityUseOnRestType.HealSelfUndead
                                                                             || componentType == AbilityUseOnRestType.HealUndead;

                                                            return forUndead == target.Descriptor.IsUndead;
                                                        }

                                                        return true;
                                                    });
                        }
                    }
                    else
                    {
                        targets = targets.Where(target => !target.IsPlayerFaction);
                    }
                }

                __result = targets.Select(target => new TargetWrapper(target));
            }
        }

        [HarmonyPatch(typeof(RuleDealDamage), "ApplyDifficultyModifiers")]
        public static class RuleDealDamage_ApplyDifficultyModifiers_Patch
        {
            public static void Postfix(ref int __result, RuleDealDamage __instance, int damage)
            {
                if (!settings.toggleNoFriendlyFireForAOE)
                {
                    return;
                }

                if (__instance.Reason.Context?.AssociatedBlueprint is BlueprintBuff)
                {
                    return;
                }

                BlueprintAbility blueprintAbility = __instance.Reason.Context?.SourceAbility;

                if (blueprintAbility == null)
                {
                    return;
                }

                if (!__instance.Initiator.IsPlayerFaction || !__instance.Target.IsPlayerFaction)
                {
                    return;
                }

                if (blueprintAbility.EffectOnAlly == AbilityEffectOnUnit.Harmful 
                    || blueprintAbility.EffectOnEnemy == AbilityEffectOnUnit.Harmful)
                {
                    __result = 0;
                }
            }
        }

        [HarmonyPatch(typeof(RuleSkillCheck), "IsSuccessRoll")]
        [HarmonyPatch(new Type[] { typeof(int), typeof(int) })]
        public static class RuleSkillCheck_IsSuccessRoll_Patch
        {
            private static void Postfix(ref bool __result, RuleSkillCheck __instance)
            {
                if (!settings.toggleNoFriendlyFireForAOE)
                {
                    return;
                }

                if (__instance.Reason == null || __instance.Reason.Ability == null || __instance.Reason.Caster == null || __instance.Reason.Ability.Blueprint == null)
                {
                    return;
                }

                if (!__instance.Reason.Caster.IsPlayerFaction || !__instance.Initiator.IsPlayerFaction)
                {
                    return;
                }

                if (__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful 
                    || __instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(RulePartySkillCheck), "Success", MethodType.Getter)]
        public static class RulePartySkillCheck_IsPassed_Patch
        {
            private static void Postfix(ref bool __result, RulePartySkillCheck __instance)
            {
                if (!settings.toggleNoFriendlyFireForAOE)
                {
                    return;
                }

                if (__instance.Reason == null || __instance.Reason.Ability == null || __instance.Reason.Caster == null || __instance.Reason.Ability.Blueprint == null)
                {
                    return;
                }

                if (!__instance.Reason.Caster.IsPlayerFaction || !__instance.Initiator.IsPlayerFaction)
                {
                    return;
                }

                if (__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful 
                    || __instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(RuleSavingThrow), "IsPassed", MethodType.Getter)]
        public static class RuleSavingThrow_IsPassed_Patch
        {
            static void Postfix(ref bool __result, RuleSavingThrow __instance)
            {
                if (!settings.toggleNoFriendlyFireForAOE)
                {
                    return;
                }
                
                if (__instance.Reason == null || __instance.Reason.Ability == null || __instance.Reason.Caster == null || __instance.Reason.Ability.Blueprint == null)
                {
                    return;
                }

                if (!__instance.Reason.Caster.IsPlayerFaction || !__instance.Initiator.IsPlayerFaction)
                {
                    return;
                }

                if (__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful 
                    || __instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful)
                {
                    __result = true;
                }
            }
        }
    }
}