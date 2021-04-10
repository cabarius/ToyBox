// borrowed shamelessly and enchanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.Controllers.Rest;
using Kingmaker.Controllers.Rest.Cooking;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.Dungeon.Units.Debug;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Formations;
using DG.Tweening;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.PubSubSystem;
using Kingmaker.RandomEncounters;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.TextTools;
using Kingmaker.UI;
//using Kingmaker.UI._ConsoleUI.Models;
using Kingmaker.UI.Common;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Group;
using Kingmaker.UI.IngameMenu;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.Log;
using Kingmaker.UI.MainMenuUI;
using Kingmaker.UI.MVVM;
using Kingmaker.UI.MVVM.CharGen;
using Kingmaker.UI.MVVM.CharGen.Phases;
using Kingmaker.UI.MVVM.CharGen.Phases.Mythic;
using Kingmaker.UI.RestCamp;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.ServiceWindow.LocalMap;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using Kingmaker.View.Spawners;
using Kingmaker.Visual;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.HitSystem;
using Kingmaker.Visual.LocalMap;
using Kingmaker.Visual.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using Kingmaker.UI.ActionBar;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using TMPro;
using TurnBased.Controllers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Kingmaker.UnitLogic.Class.LevelUp.LevelUpState;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    static class NoFriendlyFire {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;
        public static Player player = Game.Instance.Player;


        [HarmonyPatch(typeof(AbilityTargetsAround), "Select")]
        public static class AbilityTargetsAround_Select_Patch {
            public static void Postfix(ref IEnumerable<TargetWrapper> __result, AbilityTargetsAround __instance, ConditionsChecker ___m_Condition, AbilityExecutionContext context, TargetWrapper anchor) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    UnitEntityData caster = context.MaybeCaster;
                    IEnumerable<UnitEntityData> targets = GameHelper.GetTargetsAround(anchor.Point, __instance.AoERadius);
                    if (caster == null) {
                        __result = Enumerable.Empty<TargetWrapper>();
                        return;
                    }
                    switch (__instance.TargetType) {
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
                    if (caster.IsPlayerFaction && ((context.AbilityBlueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (context.AbilityBlueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                        if (context.AbilityBlueprint.HasLogic<AbilityUseOnRest>()) {
                            AbilityUseOnRestType componentType = context.AbilityBlueprint.GetComponent<AbilityUseOnRest>().Type;
                            //bool healDamage = componentType == AbilityUseOnRestType.HealDamage || componentType == AbilityUseOnRestType.HealDamage;
                            bool healDamage = componentType == AbilityUseOnRestType.HealDamage;
                            targets = targets.Where(target => {
                                if (target.IsPlayerFaction && !healDamage) {
                                    bool forUndead = componentType == AbilityUseOnRestType.HealMassUndead || componentType == AbilityUseOnRestType.HealSelfUndead || componentType == AbilityUseOnRestType.HealUndead;
                                    return (forUndead == target.Descriptor.IsUndead);
                                }
                                return true;
                            });
                        }
                        else {
                            targets = targets.Where(target => !target.IsPlayerFaction);
                        }
                    }
                    __result = targets.Select(target => new TargetWrapper(target));
                }
            }
        }

        [HarmonyPatch(typeof(RuleDealDamage), "ApplyDifficultyModifiers")]
        public static class RuleDealDamage_ApplyDifficultyModifiers_Patch {
            public static void Postfix(ref int __result, RuleDealDamage __instance, int damage) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    BlueprintScriptableObject blueprint = __instance.Reason.Context?.AssociatedBlueprint;
                    if (!(blueprint is BlueprintBuff)) {
                        BlueprintAbility blueprintAbility = __instance.Reason.Context?.SourceAbility;
                        if (blueprintAbility != null &&
                            __instance.Initiator.IsPlayerFaction &&
                            __instance.Target.IsPlayerFaction &&
                            ((blueprintAbility.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (blueprintAbility.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                            __result = 0;
                        }
                    }
                }
            }
        }



        [HarmonyPatch(typeof(RuleSkillCheck), "IsPassed", MethodType.Getter)]
        public static class RuleSkillCheck_IsPassed_Patch {
            private static void Postfix(ref bool __result, RuleSkillCheck __instance) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    if (__instance.Reason != null) {
                        if (__instance.Reason.Ability != null) {
                            if (__instance.Reason.Caster != null && __instance.Reason.Caster.IsPlayerFaction && __instance.Initiator.IsPlayerFaction && __instance.Reason.Ability.Blueprint != null && ((__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (__instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
                                __result = true;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RulePartySkillCheck), "IsPassed", MethodType.Getter)]
        public static class RulePartySkillCheck_IsPassed_Patch {
            private static void Postfix(ref bool __result, RulePartySkillCheck __instance) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    if (__instance.Reason != null) {
                        if (__instance.Reason.Ability != null) {
                            if (__instance.Reason.Caster != null && __instance.Reason.Caster.IsPlayerFaction && __instance.Initiator.IsPlayerFaction && __instance.Reason.Ability.Blueprint != null && ((__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (__instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
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
            static void Postfix(ref bool __result, RuleSavingThrow __instance) {
                if (settings.toggleNoFriendlyFireForAOE) {
                    if (__instance.Reason != null) {
                        if (__instance.Reason.Ability != null) {
                            if (__instance.Reason.Caster != null && __instance.Reason.Caster.IsPlayerFaction && __instance.Initiator.IsPlayerFaction && __instance.Reason.Ability.Blueprint != null && ((__instance.Reason.Ability.Blueprint.EffectOnAlly == AbilityEffectOnUnit.Harmful) || (__instance.Reason.Ability.Blueprint.EffectOnEnemy == AbilityEffectOnUnit.Harmful))) {
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
