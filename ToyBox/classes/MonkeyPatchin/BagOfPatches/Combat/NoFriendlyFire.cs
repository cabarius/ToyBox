// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.Designers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class NoFriendlyFire {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(AbilityTargetsAround), nameof(AbilityTargetsAround.Select))]
        public static class AbilityTargetsAround_Select_Patch {
            public static void Postfix(ref IEnumerable<TargetWrapper> __result, AbilityTargetsAround __instance, ConditionsChecker ___m_Condition, AbilityExecutionContext context, TargetWrapper anchor) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    var caster = context.MaybeCaster;
                    var targets = GameHelper.GetTargetsAround(anchor.Point, __instance.AoERadius);
                    if (caster == null) {
                        __result = Enumerable.Empty<TargetWrapper>();
                        return;
                    }
                    switch (__instance.m_TargetType) {
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
                    if (___m_Condition.HasConditions) {
                        targets = targets.Where(u => { using (context.GetDataScope(u)) { return ___m_Condition.Check(); } }).ToList();
                    }
                    if (caster.Descriptor.IsPartyOrPet() && ((context.AbilityBlueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (context.AbilityBlueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                        if (context.AbilityBlueprint.HasLogic<AbilityUseOnRest>()) {
                            var componentType = context.AbilityBlueprint.GetComponent<AbilityUseOnRest>().Type;
                            //bool healDamage = componentType == AbilityUseOnRestType.HealDamage || componentType == AbilityUseOnRestType.HealDamage;
                            var healDamage = componentType == AbilityUseOnRestType.HealDamage;
                            targets = targets.Where(target => {
                                if (target.Descriptor.IsPartyOrPet() && !healDamage) {
                                    var forUndead = componentType == AbilityUseOnRestType.HealMassUndead || componentType == AbilityUseOnRestType.HealSelfUndead || componentType == AbilityUseOnRestType.HealUndead;
                                    return forUndead == target.Descriptor.IsUndead;
                                }
                                return true;
                            });
                        }
                        else {
                            targets = targets.Where(target => !target.Descriptor.IsPartyOrPet());
                        }
                    }
                    __result = targets.Select(target => new TargetWrapper(target));
                }
            }
        }

        [HarmonyPatch(typeof(RuleDealDamage), nameof(RuleDealDamage.ApplyDifficultyModifiers))]
        public static class RuleDealDamage_ApplyDifficultyModifiers_Patch {
            public static void Postfix(ref int __result, RuleDealDamage __instance, int damage) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    SimpleBlueprint blueprint = __instance.Reason.Context?.AssociatedBlueprint;
                    if (!(blueprint is BlueprintBuff)) {
                        var blueprintAbility = __instance.Reason.Context?.SourceAbility;
                        if (blueprintAbility != null &&
                            __instance.Initiator.Descriptor.IsPartyOrPet() &&
                            __instance.Target.Descriptor.IsPartyOrPet() &&
                            ((blueprintAbility.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (blueprintAbility.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                            __result = 0;
                        }
                    }
                }
            }
        }

        //        public bool IsSuccessRoll(int d20, int successBonus = 0) => d20 + this.TotalBonus + successBonus >= this.DC;
        [HarmonyPatch(typeof(RuleSkillCheck), nameof(RuleSkillCheck.IsSuccessRoll))]
        [HarmonyPatch(new Type[] { typeof(int), typeof(int) })]

        public static class RuleSkillCheck_IsSuccessRoll_Patch {
            private static void Postfix(ref bool __result, RuleSkillCheck __instance) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    if (__instance.Reason != null) {
                        if (__instance.Reason.Ability != null) {
                            if (__instance.Reason.Caster != null && __instance.Reason.Caster.Descriptor.IsPartyOrPet() && __instance.Initiator.Descriptor.IsPartyOrPet() && __instance.Reason.Ability.Blueprint != null && ((__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (__instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                                __result = true;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RulePartyStatCheck), nameof(RulePartyStatCheck.Success), MethodType.Getter)]
        public static class RulePartyStatCheck_IsPassed_Patch {
            private static void Postfix(ref bool __result, RulePartyStatCheck __instance) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    if (__instance.Reason != null) {
                        if (__instance.Reason.Ability != null) {
                            if (__instance.Reason.Caster != null && __instance.Reason.Caster.Descriptor.IsPartyOrPet() && __instance.Initiator.Descriptor.IsPartyOrPet() && __instance.Reason.Ability.Blueprint != null && ((__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (__instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                                __result = true;
                            }
                        }
                    }
                }
#if false
                if (StringUtils.ToToggleBool(settings.togglePassSavingThrowIndividual)) {
                    for (int i = 0; i < settings.togglePassSavingThrowIndividualArray.Count(); i++) {
                        if (StringUtils.ToToggleBool(settings.togglePassSavingThrowIndividualArray[i]) && Storage.statsSavesDict[Storage.individualSavesArray[i]] == __instance.StatType) {
                            if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, (UnitSelectType)settings.indexPassSavingThrowIndividuall)) {
                                __result = true;
                            }
                        }
                    }
                }
                if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividual)) {
                    for (int i = 0; i < settings.togglePassSkillChecksIndividualArray.Count(); i++) {
                        if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividualArray[i]) && Storage.statsSkillsDict.Union(Storage.statsSocialSkillsDict).ToDictionary(d => d.Key, d => d.Value)[Storage.individualSkillsArray[i]] == __instance.StatType) {
                            if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, (UnitSelectType)settings.indexPassSkillChecksIndividual)) {
                                __result = true;
                            }
                        }
                    }
                }
#endif
            }
        }
        [HarmonyPatch(typeof(RuleSavingThrow), "IsPassed", MethodType.Getter)]
        public static class RuleSavingThrow_IsPassed_Patch {
            internal static void Postfix(ref bool __result, RuleSavingThrow __instance) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    if (__instance.Reason != null) {
                        if (__instance.Reason.Ability != null) {
                            if (__instance.Reason.Caster != null && __instance.Reason.Caster.Descriptor.IsPartyOrPet() && __instance.Initiator.Descriptor.IsPartyOrPet() && __instance.Reason.Ability.Blueprint != null && ((__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (__instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                                __result = true;
                            }
                        }
                    }
                }
#if false
                if (StringUtils.ToToggleBool(settings.togglePassSavingThrowIndividual)) {
                    for (int i = 0; i < settings.togglePassSavingThrowIndividualArray.Count(); i++) {
                        if (StringUtils.ToToggleBool(settings.togglePassSavingThrowIndividualArray[i]) && Storage.statsSavesDict[Storage.individualSavesArray[i]] == __instance.StatType) {
                            if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, (UnitSelectType)settings.indexPassSavingThrowIndividuall)) {
                                __result = true;
                            }
                        }
                    }
                }
#endif
            }
        }
    }
}
