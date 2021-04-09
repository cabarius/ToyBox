// borrowed shamelessly and enchanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

//using BagOfTricks.Favourites;
//using BagOfTricks.Utils;
//using BagOfTricks.Utils.HarmonyPatches;
//using BagOfTricks.Utils.Kingmaker;
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

namespace ToyBox {
    static class HarmonyPatches {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(EncumbranceHelper), "GetHeavy")]
        static class EncumbranceHelper_GetHeavy_Patch {
            static void Postfix(ref int __result) {
                __result = Mathf.RoundToInt(__result * settings.encumberanceMultiplier);
            }
        }

        [HarmonyPatch(typeof(UnitPartWeariness), "GetFatigueHoursModifier")]
        static class EncumbranceHelper_GetFatigueHoursModifier_Patch {
            static void Postfix(ref float __result) {
                __result = __result * (float)Math.Round(settings.fatigueHoursModifierMultiplier, 1);
            }
        }

        [HarmonyPatch(typeof(Player), "GainPartyExperience")]
        public static class Player_GainPartyExperience_Patch {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref int gained) {
                gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplier, 1));
                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "GainMoney")]
        public static class Player_GainMoney_Patch {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref long amount) {
                amount = Mathf.RoundToInt(amount * (float)Math.Round(settings.moneyMultiplier, 1));
                return true;
            }
        }
#if false
        [HarmonyPatch(typeof(RuleCastSpell), "IsArcaneSpellFailed", MethodType.Getter)]
        public static class RuleCastSpell_IsArcaneSpellFailed_Patch {
            static void Postfix(RuleCastSpell __instance, ref bool __result) {
                if ((__instance.Spell.Caster?.Unit?.IsPlayerFaction ?? false) && (StringUtils.ToToggleBool(settings.toggleArcaneSpellFailureRoll))) {
                    if (!StringUtils.ToToggleBool(settings.toggleArcaneSpellFailureRollOutOfCombatOnly)) {
                        __result = false;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArcaneSpellFailureRollOutOfCombatOnly) && !__instance.Initiator.IsInCombat) {
                        __result = false;
                    }

                }
            }
        }
#endif

        [HarmonyPatch(typeof(RuleRollDice), "Roll")]
        public static class RuleRollDice_Roll_Patch {
            static void Postfix(RuleRollDice __instance) {
                if (__instance.DiceFormula.Dice != DiceType.D20) return;
                var initiator = __instance.Initiator;
                int result = __instance.m_Result;
                Logger.ModLoggerDebug("Initial D20Roll: " + result);
                if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll20)) {
                    result = 20;
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.alwaysRoll1)) {
                    result = 1;
                }
                else {
                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.rollWithAdvantage)) {
                        result = Math.Max(result, UnityEngine.Random.Range(1, 21));
                    }
                    else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.rollWithDisadvantage)) {
                        result = Math.Min(result, UnityEngine.Random.Range(1, 21));
                    }
                    int min = 1;
                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.neverRoll1) && result == 1) {
                        result = UnityEngine.Random.Range(2, 21);
                        min = 2;
                    }
                    if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.neverRoll20) && result == 20) {
                        result = UnityEngine.Random.Range(min, 20);
                    }
                }
                Logger.ModLoggerDebug("Modified D20Roll: " + result);
                __instance.m_Result = result;
            }
        }

        [HarmonyPatch(typeof(RuleInitiativeRoll), "Result", MethodType.Getter)]
        public static class RuleInitiativeRoll_OnTrigger_Patch {
            static void Postfix(RuleInitiativeRoll __instance, ref int __result) {
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.roll1Initiative)) {
                    __result = 1;
                    Logger.ModLoggerDebug("Modified InitiativeRoll: " + __result);
                }
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.roll20Initiative)) {
                    __result = 20;
                    Logger.ModLoggerDebug("Modified InitiativeRoll: " + __result);
                }
            }
        }

        [HarmonyPatch(typeof(Spellbook), "GetSpellsPerDay")]
        static class Spellbook_GetSpellsPerDay_Patch {
            static void Postfix(ref int __result) {
                __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.spellsPerDayMultiplier, 1));
            }
        }

        public static BlueprintAbility ExtractSpell([NotNull] ItemEntity item) {
            ItemEntityUsable itemEntityUsable = item as ItemEntityUsable;
            if (itemEntityUsable?.Blueprint.Type != UsableItemType.Scroll) {
                return null;
            }
            return itemEntityUsable.Blueprint.Ability.Parent ? itemEntityUsable.Blueprint.Ability.Parent : itemEntityUsable.Blueprint.Ability;
        }

        public static string GetSpellbookActionName(string actionName, ItemEntity item, UnitEntityData unit) {
            if (actionName != LocalizedTexts.Instance.Items.CopyScroll) {
                return actionName;
            }

            BlueprintAbility spell = ExtractSpell(item);
            if (spell == null) {
                return actionName;
            }

            List<Spellbook> spellbooks = unit.Descriptor.Spellbooks.Where(x => x.Blueprint.SpellList.Contains(spell)).ToList();

            int count = spellbooks.Count;

            if (count <= 0) {
                return actionName;
            }

            string actionFormat = "{0} <{1}>";

            return string.Format(actionFormat, actionName, count == 1 ? spellbooks.First().Blueprint.Name : "Multiple");
        }

        //     private static bool CanCopySpell([NotNull] BlueprintAbility spell, [NotNull] Spellbook spellbook) => spellbook.Blueprint.CanCopyScrolls && !spellbook.IsKnown(spell) && spellbook.Blueprint.SpellList.Contains(spell);

        [HarmonyPatch(typeof(CopyScroll), "CanCopySpell")]
        [HarmonyPatch(new Type[] { typeof(BlueprintAbility), typeof(Spellbook) })]
        public static class CopyScroll_CanCopySpell_Patch {
            static bool Prefix() {
                return false;
            }
            static void Postfix([NotNull] BlueprintAbility spell, [NotNull] Spellbook spellbook, ref bool __result) {
                if (spellbook.IsKnown(spell)) {
                    __result = false;
                    return;
                }
                bool spellListContainsSpell = spellbook.Blueprint.SpellList.Contains(spell);

                if ((settings.toggleSpontaneousCopyScrolls) && spellbook.Blueprint.Spontaneous && spellListContainsSpell) {
                    __result = true;
                    return;
                }

                __result = spellbook.Blueprint.CanCopyScrolls && spellListContainsSpell;
            }
        }

        [HarmonyPatch(typeof(Kingmaker.UI.ServiceWindow.ItemSlot), "ScrollContent", MethodType.Getter)]
        public static class ItemSlot_ScrollContent_Patch {
            [HarmonyPostfix]
            static void Postfix(Kingmaker.UI.ServiceWindow.ItemSlot __instance, ref string __result) {
                UnitEntityData currentCharacter = UIUtility.GetCurrentCharacter();
                CopyItem component = __instance.Item.Blueprint.GetComponent<CopyItem>();
                string actionName = component?.GetActionName(currentCharacter) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(actionName)) {
                    actionName = GetSpellbookActionName(actionName, __instance.Item, currentCharacter);
                }
                __result = actionName;
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindow), "OnClose")]
        public static class KingdomUIEventWindow_OnClose_Patch {
            public static bool Prefix(ref bool __state) {
                __state = settings.toggleInstantEvent;
                return !__state;
            }

            public static void Postfix(bool __state, KingdomEventUIView ___m_KingdomEventView, KingdomEventHandCartController ___m_Cart) {
                if (__state) {
                    if (___m_KingdomEventView != null) {
                        EventBus.RaiseEvent((IEventSceneHandler h) => h.OnEventSelected(null, ___m_Cart));

                        if (___m_KingdomEventView.IsFinished || ___m_KingdomEventView.AssignedLeader == null || ___m_KingdomEventView.Blueprint.NeedToVisitTheThroneRoom) {
                            return;
                        }

                        bool inProgress = ___m_KingdomEventView.IsInProgress;
                        BlueprintUnit leader = ___m_KingdomEventView.AssignedLeader.Blueprint;

                        if (!inProgress || leader == null) {
                            return;
                        }

                        ___m_KingdomEventView.Event.Resolve(___m_KingdomEventView.Task);

                        if (___m_KingdomEventView.RulerTimeRequired <= 0) {
                            return;
                        }

                        foreach (UnitEntityData unitEntityData in player.AllCharacters) {
                            RestController.ApplyRest(unitEntityData.Descriptor);
                        }

                        new KingdomTimelineManager().MaybeUpdateTimeline();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "GetCustomCompanionCost")]
        public static class Player_GetCustomCompanionCost_Patch {
            public static bool Prefix(ref bool __state) {
                return !__state;
            }

            public static void Postfix(ref int __result) {
                __result = Mathf.RoundToInt(__result * settings.companionCostMultiplier);
            }
        }

        [HarmonyPatch(typeof(KingdomTaskEvent), "SkipPlayerTime", MethodType.Getter)]
        public static class KingdomTaskEvent_SkipPlayerTime_Patch {
            public static void Postfix(ref int __result) {
                if (settings.toggleInstantEvent) {
                    __result = 0;
                }
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindowFooter), "OnStart")]
        public static class KingdomUIEventWindowFooter_OnStart_Patch {
            public static bool Prefix(ref bool __state) {
                __state = settings.toggleInstantEvent;
                return !__state;
            }

            public static void Postfix(KingdomEventUIView ___m_KingdomEventView, bool __state) {
                if (__state) {
                    EventBus.RaiseEvent((IKingdomUIStartSpendTimeEvent h) => h.OnStartSpendTimeEvent(___m_KingdomEventView.Blueprint));
                    KingdomTaskEvent kingdomTaskEvent = ___m_KingdomEventView?.Task;
                    EventBus.RaiseEvent((IKingdomUICloseEventWindow h) => h.OnClose());
                    kingdomTaskEvent?.Start(false);

                    if (kingdomTaskEvent == null) {
                        return;
                    }

                    if (kingdomTaskEvent.IsFinished || kingdomTaskEvent.AssignedLeader == null || ___m_KingdomEventView.Blueprint.NeedToVisitTheThroneRoom) {
                        return;
                    }

                    kingdomTaskEvent.Event.Resolve(kingdomTaskEvent);

                    if (___m_KingdomEventView.RulerTimeRequired <= 0) {
                        return;
                    }
                    foreach (UnitEntityData unitEntityData in player.AllCharacters) {
                        RestController.ApplyRest(unitEntityData.Descriptor);
                    }
                    new KingdomTimelineManager().MaybeUpdateTimeline();
                }
            }
        }

        [HarmonyPatch(typeof(AbilityResourceLogic), "Spend")]
        public static class AbilityResourceLogic_Spend_Patch {
            public static bool Prefix(AbilityData ability) {
                UnitEntityData unit = ability.Caster.Unit;
                if (unit?.IsPlayerFaction == true && settings.toggleInfiniteAbilities) {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ActivatableAbilityResourceLogic), "SpendResource")]
        public static class ActivatableAbilityResourceLogic_SpendResource_Patch {
            public static bool Prefix() {
                return !settings.toggleInfiniteAbilities;
            }
        }

        [HarmonyPatch(typeof(AbilityData), "SpellSlotCost", MethodType.Getter)]
        public static class AbilityData_SpellSlotCost_Patch {
            public static bool Prefix() {
                return !settings.toggleInfiniteSpellCasts;
            }
        }

        [HarmonyPatch(typeof(UnitCombatState), "HasCooldownForCommand")]
        [HarmonyPatch(new Type[] { typeof(UnitCommand) })]
        public static class UnitCombatState_HasCooldownForCommand_Patch1 {
            public static void Postfix(ref bool __result, UnitCombatState __instance) {
                if (settings.toggleInstantCooldown && __instance.Unit.IsDirectlyControllable) {
                    __result = false;
                }
                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn) {
                    __result = false;
                }
            }
        }
        [HarmonyPatch(typeof(UnitCombatState), "HasCooldownForCommand")]
        [HarmonyPatch(new Type[] { typeof(UnitCommand.CommandType) })]
        public static class UnitCombatState_HasCooldownForCommand_Patch2 {
            public static void Postfix(ref bool __result, UnitCombatState __instance) {
                if (settings.toggleInstantCooldown && __instance.Unit.IsDirectlyControllable) {
                    __result = false;
                }
                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn) {
                    __result = false;
                }
            }
        }
        [HarmonyPatch(typeof(UnitCombatState), "OnNewRound")]
        public static class UnitCombatState_OnNewRound_Patch {
            public static bool Prefix(UnitCombatState __instance) {
                if (__instance.Unit.IsDirectlyControllable && settings.toggleInstantCooldown) {
                    __instance.Cooldown.Initiative = 0f;
                    __instance.Cooldown.StandardAction = 0f;
                    __instance.Cooldown.MoveAction = 0f;
                    __instance.Cooldown.SwiftAction = 0f;
                    __instance.Cooldown.AttackOfOpportunity = 0f;
                }
                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn) {
                    __instance.Cooldown.Initiative = 0f;
                    __instance.Cooldown.StandardAction = 0f;
                    __instance.Cooldown.MoveAction = 0f;
                    __instance.Cooldown.SwiftAction = 0f;
                    __instance.Cooldown.AttackOfOpportunity = 0f;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), "SpendAction")]
        public static class UnitEntityData_SpendAction_Patch {

            public static void Postfix(UnitCommand.CommandType type, bool isFullRound, float timeSinceCommandStart, UnitEntityData __instance) {
                if (!__instance.IsInCombat)
                    return;
                UnitCombatState.Cooldowns cooldown = __instance.CombatState.Cooldown;
                if (CombatController.IsInTurnBasedCombat()) {
                    if (settings.toggleUnlimitedActionsPerTurn) return;
                    switch (type) {
                        case UnitCommand.CommandType.Free:
                            break;
                        case UnitCommand.CommandType.Standard:
                            cooldown.StandardAction += 6f;
                            if (!isFullRound)
                                break;
                            cooldown.MoveAction += 3f;
                            break;
                        case UnitCommand.CommandType.Swift:
                            cooldown.SwiftAction += 6f;
                            break;
                        case UnitCommand.CommandType.Move:
                            cooldown.MoveAction += 3f;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else {
                    if (settings.toggleInstantCooldown) return;
                    switch (type) {
                        case UnitCommand.CommandType.Free:
                        case UnitCommand.CommandType.Move:
                            cooldown.MoveAction = 3f - timeSinceCommandStart;
                            break;
                        case UnitCommand.CommandType.Standard:
                            cooldown.StandardAction = 6f - timeSinceCommandStart;
                            break;
                        case UnitCommand.CommandType.Swift:
                            cooldown.SwiftAction = 6f - timeSinceCommandStart;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(LevelUpState), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(LevelUpState.CharBuildMode) })]
        public static class LevelUpState_Patch {
            [HarmonyPriority(Priority.Low)]
            public static void Postfix(UnitDescriptor unit, LevelUpState.CharBuildMode mode, ref LevelUpState __instance) {
                if (__instance.IsFirstCharacterLevel) {
                    if (mode != CharBuildMode.PreGen) {
                        // Kludge - there is some wierdness where the unit in the character generator does not return IsCustomCharacter() as true during character creation so I have to check the blueprint. The thing is if I actually try to get the blueprint name the game crashes so I do this kludge calling unit.Blueprint.ToString()
                        bool isCustom = unit.Blueprint.ToString() == "CustomCompanion";
                        Logger.Log($"unit.Blueprint: {unit.Blueprint.ToString()}");
                        Logger.Log($"not pregen - isCust: {isCustom}");
                        int pointCount = Math.Max(0, isCustom ? settings.characterCreationAbilityPointsMerc : settings.characterCreationAbilityPointsPlayer);
                        Logger.Log($"points: {pointCount}");

                        __instance.StatsDistribution.Start(pointCount);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SpendSkillPoint), "Apply")]
        public static class SpendSkillPoint_Apply_Patch {
            public static bool Prefix(ref bool __state) {
                __state = settings.toggleInfiniteSkillpoints;
                return !__state;
            }

            public static void Postfix(ref bool __state, LevelUpState state, UnitDescriptor unit, StatType ___Skill) {
                if (__state) {
                    unit.Stats.GetStat(___Skill).BaseValue++;
                }
            }
        }

        // stuff for fixing feat multiplier ???
        // LevelUpState
        //     public bool CanSelectAnything(LevelUpState state, UnitEntityData unit)

#if false
        [HarmonyPatch(typeof(FeatureSelectionState), "CanSelectAnything")]
        public static class FeatureSelectionState_CanSelectAnything_Patch {

            public static bool Prefix(ref bool __result, FeatureSelectionState __instance, LevelUpState state, UnitEntityData unit) {
                __result = false;
                if (__instance.Selection.IsObligatory())
                    __result = true;
                else if (__instance.Selection.IsSelectionProhibited(unit.Descriptor))
                    __result = false;
                else {
                    foreach (IFeatureSelectionItem featureSelectionItem in __instance.Selection.Items) {
                        if (featureSelectionItem != __instance.SelectedItem && __instance.Selection.CanSelect(unit.Descriptor, state, __instance, featureSelectionItem))
                            __result = true;
                    }
                }
                return true;
            }
        }
#endif
        [HarmonyPatch(typeof(LevelUpHelper), "AddFeaturesFromProgression")]
        public static class MultiplyFeatPoints_LevelUpHelper_AddFeatures_Patch {
            public static bool Prefix([NotNull] LevelUpState state, [NotNull] UnitDescriptor unit, [NotNull] IList<BlueprintFeatureBase> features, [CanBeNull] FeatureSource source, int level) {
                if (!Main.IsInGame) return false;
#if true
                //Logger.Log($"feature count = {features.ToArray().Count()} ");
                var description = source.Blueprint.GetDescription() ?? "nil";
                //Logger.Log($"source: {source.Blueprint.name} - {description}");
                foreach (BlueprintFeature blueprintFeature in features.OfType<BlueprintFeature>()) {
                    if (blueprintFeature.MeetsPrerequisites((FeatureSelectionState)null, unit, state, true)) {
                        //Logger.Log($"    name: {blueprintFeature.Name} : {blueprintFeature.GetType()}");
                        if (blueprintFeature is IFeatureSelection selection) {
                            // Bug Fix - due to issues in the implementation of FeatureSelectionState.CanSelectAnything we can get level up blocked so this is an attempt to work around for that
                            var numToAdd = settings.featsMultiplier;
                            if (selection is BlueprintFeatureSelection bpFS) {
                                var bpFeatures = bpFS;
                                var items = bpFS.ExtractSelectionItems(unit, null);
                                //Logger.Log($"        items: {items.Count()}");
                                var availableCount = 0;
                                foreach (var item in items) {
#if false
                                    if (bpFS. is BlueprintParametrizedFeature bppF) {
                                        Logger.Log($"checking parameterized feature {bppF.Name}");
                                        if (selection.CanSelect(unit, state, null, item)) {
                                            Logger.Log($"        {item.Feature.name}  is avaiable");
                                            availableCount++;
                                        }
                                    }
                                    else
#endif
                                    if (!unit.Progression.Features.HasFact(item.Feature)) {
                                        availableCount++;
                                        //                                        Logger.Log($"        {item.Feature.name}  is avaiable");
                                    }
#if false
                                    if (selection.CanSelect(unit, state, null, item)) {
                                        Logger.Log($"        {item.Feature.name}  is avaiable");
                                        availableCount++;
                                    }
                                    else Logger.Log($"        {item.Feature.name}  is NOT avaiable");
                                    if (!unit.Progression.Features.HasFact(item.Feature)) {
                                        if (item.Feature.MeetsPrerequisites(null, unit, state, true)) {
                                            Logger.Log($"        {item.Feature.name}  is avaiable");
                                            availableCount++;
                                        }
                                    }
                                    else Logger.Log($"        has Fact {item.Feature.Name}");
#endif
                                }
                                if (numToAdd > availableCount) {
                                    Logger.Log($"reduced numToAdd: {numToAdd} -> {availableCount}");
                                    numToAdd = availableCount;
                                }
                            }
                            //Logger.Log($"        IFeatureSelection: {selection} adding: {numToAdd}");
                            for (int i = 0; i < numToAdd; ++i) {
                                state.AddSelection((FeatureSelectionState)null, source, selection, level);
                            }
                        }
                        Kingmaker.UnitLogic.Feature feature = (Kingmaker.UnitLogic.Feature)unit.AddFact((BlueprintUnitFact)blueprintFeature);
                        BlueprintProgression progression = blueprintFeature as BlueprintProgression;
                        if ((UnityEngine.Object)progression != (UnityEngine.Object)null) {
                            //Logger.Log($"        BlueprintProgression: {progression}");
                            LevelUpHelper.UpdateProgression(state, unit, progression);
                        }
                        FeatureSource source1 = source;
                        int level1 = level;
                        feature.SetSource(source1, level1);
                    }
                }
                return false;
#else
                                    for (int i = 0; i < settings.featsMultiplier  ; ++i) {
                    foreach (BlueprintFeatureSelection item in features.OfType<BlueprintFeatureSelection>()) {
                        state.AddSelection(null, source, item, level);
                    }
                    foreach (BlueprintFeature item2 in features.OfType<BlueprintFeature>()) {
                        Feature feature = (Feature)unit.AddFact(item2);
                        BlueprintProgression blueprintProgression = item2 as BlueprintProgression;
                        if (blueprintProgression != null) {
                            LevelUpHelper.UpdateProgression(state, unit, blueprintProgression);
                        }
                        feature.SetSource(source, level);
                    }
                }
                return false;
#endif
            }
        }


        [HarmonyPatch(typeof(GlobalMapMovementController), "GetRegionalModifier", new Type[] { })]
        public static class MovementSpeed_GetRegionalModifier_Patch1 {
            public static void Postfix(ref float __result) {
                float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), "GetRegionalModifier", new Type[] { typeof(Vector3) })]
        public static class MovementSpeed_GetRegionalModifier_Patch2 {
            public static void Postfix(ref float __result) {
                float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }


        [HarmonyPatch(typeof(StatsDistribution), "CanRemove")]
        public static class StatsDistribution_CanRemove_Patch {
            public static void Postfix(ref bool __result, StatType attribute, StatsDistribution __instance) {
                if (settings.characterCreationAbilityPointsMin != 7) {
                    __result = __instance.Available && __instance.StatValues[attribute] > settings.characterCreationAbilityPointsMin;
                }
            }
        }

        [HarmonyPatch(typeof(StatsDistribution), "CanAdd")]
        public static class StatsDistribution_CanAdd_Patch {
            public static void Postfix(ref bool __result, StatType attribute, StatsDistribution __instance) {
                int attributeMax = settings.characterCreationAbilityPointsMax;
                if (!__instance.Available) {
                    __result = false;
                }
                else {
                    if (attributeMax <= 18) {
                        attributeMax = 18;
                    }
                    int attributeValue = __instance.StatValues[attribute];
                    __result = attributeValue < attributeMax && __instance.GetAddCost(attribute) <= __instance.Points;
                }
            }
        }
        [HarmonyPatch(typeof(StatsDistribution), "GetAddCost")]
        public static class StatsDistribution_GetAddCost_Patch {
            public static bool Prefix(StatsDistribution __instance, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                return (attributeValue > 7 && attributeValue < 17);
            }
            public static void Postfix(StatsDistribution __instance, ref int __result, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                if (attributeValue <= 7) {
                    __result = 2;
                }
                if (attributeValue >= 17) {
                    __result = 4;
                }
            }
        }
        [HarmonyPatch(typeof(StatsDistribution), "GetRemoveCost")]
        public static class StatsDistribution_GetRemoveCost_Patch {
            public static bool Prefix(StatsDistribution __instance, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                return (attributeValue > 7 && attributeValue < 17);
            }

            public static void Postfix(StatsDistribution __instance, ref int __result, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                if (attributeValue <= 7) {
                    __result = -2;
                }
                else if (attributeValue >= 17) {
                    __result = -4;
                }
            }
        }

        [HarmonyPatch(typeof(EquipmentRestrictionAlignment), "CanBeEquippedBy")]
        public static class EquipmentRestrictionAlignment_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(EquipmentRestrictionClass), "CanBeEquippedBy")]
        public static class EquipmentRestrictionClassNew_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(EquipmentRestrictionStat), "CanBeEquippedBy")]
        public static class EquipmentRestrictionStat_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityArmor), "CanBeEquippedInternal")]
        public static class ItemEntityArmor_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityArmor __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    var blueprint = __instance.Blueprint as BlueprintItemEquipment;
                    __result = blueprint == null ? false : blueprint.CanBeEquippedBy(owner);
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityShield), "CanBeEquippedInternal")]
        public static class ItemEntityShield_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityShield __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    var blueprint = __instance.Blueprint as BlueprintItemEquipment;
                    __result = blueprint == null ? false : blueprint.CanBeEquippedBy(owner);
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityWeapon), "CanBeEquippedInternal")]
        public static class ItemEntityWeapon_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityWeapon __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    var blueprint = __instance.Blueprint as BlueprintItemEquipment;
                    __result = blueprint == null ? false : blueprint.CanBeEquippedBy(owner);
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintAnswerBase), "IsAlignmentRequirementSatisfied", MethodType.Getter)]
        public static class BlueprintAnswerBase_IsAlignmentRequirementSatisfied_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleDialogRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintSettlementBuilding), "CheckRestrictions", new Type[] { typeof(SettlementState) })]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch1 {
            public static void Postfix(ref bool __result) {
                if (settings.toggleSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintSettlementBuilding), "CheckRestrictions", new Type[] { typeof(SettlementState), typeof(SettlementGridTopology.Slot) })]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch2 {
            public static void Postfix(ref bool __result) {
                if (settings.toggleSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        // public static bool Prefix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings) {
        // old: public static bool Prefix(UnitEntityData unit, Vector3 p, float? speedLimit, float orientation, float delay, bool showTargetMarker) {
        [HarmonyPatch(typeof(ClickGroundHandler), "RunCommand")]
        public static class ClickGroundHandler_RunCommand_Patch {
            public static bool Prefix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings) {
                var moveAsOne = Main.settings.toggleMoveSpeedAsOne;
                var speedLimit = moveAsOne ? UnitEntityDataUtils.GetMaxSpeed(Game.Instance.UI.SelectionManager.SelectedUnits) : unit.ModifiedSpeedMps;
                speedLimit *= Main.settings.partyMovementSpeedMultiplier;

                UnitMoveTo unitMoveTo = new UnitMoveTo(settings.Destination, 0.3f) {
                    MovementDelay = settings.Delay,
                    Orientation = new float?(settings.Orientation),
                    CreatedByPlayer = true
                };
                if (BuildModeUtility.IsDevelopment) {
                    if (CheatsAnimation.SpeedForce > 0f) {
                        unitMoveTo.OverrideSpeed = new float?(CheatsAnimation.SpeedForce);
                    }
                    unitMoveTo.MovementType = (UnitAnimationActionLocoMotion.WalkSpeedType)CheatsAnimation.MoveType.Get();
                }
                if (Main.settings.partyMovementSpeedMultiplier > 1) {
                    unitMoveTo.OverrideSpeed = speedLimit;
                }
                unitMoveTo.SpeedLimit = speedLimit;
                unitMoveTo.ApplySpeedLimitInCombat = settings.ApplySpeedLimitInCombat;
                unit.Commands.Run(unitMoveTo);
                if (unit.Commands.Queue.FirstOrDefault((UnitCommand c) => c is UnitMoveTo) == unitMoveTo || Game.Instance.IsPaused) {
                    ClickGroundHandler.ShowDestination(unit, unitMoveTo.Target, false);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(ItemEntity), "SpendCharges", new Type[] { typeof(UnitDescriptor) })]
        public static class ItemEntity_SpendCharges_Patch {
            public static bool Prefix(ref bool __state) {
                __state = settings.toggleInfiniteItems;
                return !__state;
            }
            public static void Postfix(bool __state, ItemEntity __instance, ref bool __result, UnitDescriptor user) {
                if (__state) {
                    BlueprintItemEquipment blueprintItemEquipment = __instance.Blueprint as BlueprintItemEquipment;
                    if (!blueprintItemEquipment || !blueprintItemEquipment.GainAbility) {
                        __result = false;
                        return;
                    }
                    if (!__instance.IsSpendCharges) {
                        __result = true;
                        return;
                    }
                    bool hasNoCharges = false;
                    if (__instance.Charges > 0) {
                        ItemEntityUsable itemEntityUsable = new ItemEntityUsable((BlueprintItemEquipmentUsable)__instance.Blueprint);
                        if (user.State.Features.HandOfMagusDan && itemEntityUsable.Blueprint.Type == UsableItemType.Scroll) {
                            RuleRollDice ruleRollDice = new RuleRollDice(user.Unit, new DiceFormula(1, DiceType.D100));
                            Rulebook.Trigger(ruleRollDice);
                            if (ruleRollDice.Result <= 25) {
                                __result = true;
                                return;
                            }
                        }

                        if (user.IsPlayerFaction) {
                            __result = true;
                            return;
                        }

                        --__instance.Charges;
                    }
                    else {
                        hasNoCharges = true;
                    }

                    if (__instance.Charges >= 1 || blueprintItemEquipment.RestoreChargesOnRest) {
                        __result = !hasNoCharges;
                        return;
                    }

                    if (__instance.Count > 1) {
                        __instance.DecrementCount(1);
                        __instance.Charges = 1;
                    }
                    else {
                        ItemsCollection collection = __instance.Collection;
                        collection?.Remove(__instance);
                    }

                    __result = !hasNoCharges;
                }
            }
        }

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

        [HarmonyPatch(typeof(MetamagicHelper), "DefaultCost")]
        public static class MetamagicHelper_DefaultCost_Patch {
            public static void Postfix(ref int __result) {
                if (settings.toggleMetamagicIsFree) {
                    __result = 0;
                }
            }
        }

        [HarmonyPatch(typeof(RuleCollectMetamagic), "AddMetamagic")]
        public static class RuleCollectMetamagic_AddMetamagic_Patch {
            public static bool Prefix() {
                return !settings.toggleMetamagicIsFree;
            }
            public static void Postfix(ref RuleCollectMetamagic __instance, int ___m_SpellLevel, Feature metamagicFeature) {
                if (settings.toggleMetamagicIsFree) {

                    AddMetamagicFeat component = metamagicFeature.GetComponent<AddMetamagicFeat>();
                    if (component == null) {
                        Logger.ModLoggerDebug(String.Format("Trying to add metamagic feature without metamagic component: {0}", (object)metamagicFeature));
                    }
                    else {
                        Metamagic metamagic = component.Metamagic;
                        __instance.KnownMetamagics.Add(metamagicFeature);
                        if (___m_SpellLevel < 0 || ___m_SpellLevel >= 10 || (___m_SpellLevel + component.Metamagic.DefaultCost() > 10 || __instance.SpellMetamagics.Contains(metamagicFeature)) || (__instance.Spell.AvailableMetamagic & metamagic) != metamagic)
                            return;
                        __instance.SpellMetamagics.Add(metamagicFeature);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(AbilityData), "RequireMaterialComponent", MethodType.Getter)]
        public static class AbilityData_RequireMaterialComponent_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleMaterialComponent) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteAlignment), "Check")]
        public static class PrerequisiteAlignment_Check_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreAlignmentWhenChoosingClass) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteNoFeature), "Check")]
        public static class PrerequisiteNoFeature_Check_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreForbiddenFeatures) {
                    __result = true;
                }
            }
        }
#if false
        [HarmonyPatch(typeof(Spellbook), "AddCasterLevel")]
        public static class Spellbook_AddCasterLevel_Patch {
            public static bool Prefix() {
                return false;
            }

            public static void Postfix(ref Spellbook __instance, ref int ___m_CasterLevelInternal, List<BlueprintSpellList> ___m_SpecialLists) {
                int maxSpellLevel = __instance.MaxSpellLevel;
                ___m_CasterLevelInternal += settings.addCasterLevel;
                int maxSpellLevel2 = __instance.MaxSpellLevel;
                if (__instance.Blueprint.AllSpellsKnown) {
                    Traverse addSpecialMethod = Traverse.Create(__instance).Method("AddSpecial", new Type[] { typeof(int), typeof(BlueprintAbility) });
                    for (int i = maxSpellLevel + 1; i <= maxSpellLevel2; i++) {
                        foreach (BlueprintAbility spell in __instance.Blueprint.SpellList.GetSpells(i)) {
                            __instance.AddKnown(i, spell);
                        }
                        foreach (BlueprintSpellList specialList in ___m_SpecialLists) {
                            foreach (BlueprintAbility spell2 in specialList.GetSpells(i)) {
                                addSpecialMethod.GetValue(i, spell2);
                            }
                        }
                    }
                }
            }
        }
#endif
        [HarmonyPatch(typeof(SpellSelectionData), "CanSelectAnything", new Type[] { typeof(UnitDescriptor) })]
        public static class SpellSelectionData_CanSelectAnything_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleSkipSpellSelection) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(FogOfWarArea), "Active", MethodType.Getter)]
        public static class FogOfWarArea_Active_Patch {
            private static void Postfix(ref FogOfWarArea __result) {
                // We need this to avoid hanging the game on launch
                if (Main.Enabled && Main.IsInGame && __result != null && settings != null) {
                    __result.enabled = !settings.toggleNoFogOfWar;
                }
            }
        }

        [HarmonyPatch(typeof(LevelUpController), "CanLevelUp")]
        static class LevelUpController_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleNoLevelUpRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(GameHistoryLog), "HandlePartyCombatStateChanged")]
        static class GameHistoryLog_HandlePartyCombatStateChanged_Patch {
            private static void Postfix(ref bool inCombat) {
                if (!inCombat && settings.toggleRestoreSpellsAbilitiesAfterCombat) {
                    List<UnitEntityData> partyMembers = Game.Instance.Player.PartyAndPets;
                    foreach (UnitEntityData u in partyMembers) {
                        foreach (BlueprintScriptableObject resource in u.Descriptor.Resources)
                            u.Descriptor.Resources.Restore(resource);
                        foreach (Spellbook spellbook in u.Descriptor.Spellbooks)
                            spellbook.Rest();
                        u.Brain.RestoreAvailableActions();
                    }
                }
                if (!inCombat && settings.toggleInstantRestAfterCombat) {
                    CheatsCombat.RestAll();
                }
#if false
                if (!inCombat && settings.toggleRestoreItemChargesAfterCombat) {
                    Cheats.RestoreAllItemCharges();
                }

                if (inCombat && StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0) && StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                    foreach (UnitEntityData unitEntityData in Game.Instance.Player.Party) {
                        Common.RecalculateArmourItemStats(unitEntityData);
                    }
                }
                if (!inCombat && StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0) && StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                    foreach (UnitEntityData unitEntityData in Game.Instance.Player.Party) {
                        Common.RecalculateArmourItemStats(unitEntityData);
                    }
                }
#endif
            }
        }

        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyHitPoints", new Type[] { typeof(LevelUpState), typeof(ClassData), typeof(UnitDescriptor) })]
        static class ApplyClassMechanics_ApplyHitPoints_Patch {
            private static void Postfix(LevelUpState state, ClassData classData, ref UnitDescriptor unit) {
                if (settings.toggleFullHitdiceEachLevel && unit.IsPlayerFaction && state.NextClassLevel > 1) {

                    int newHitDie = ((int)classData.CharacterClass.HitDie / 2) - 1;
                    unit.Stats.HitPoints.BaseValue += newHitDie;
                }
#if false
                else if (StringUtils.ToToggleBool(settings.toggleRollHitDiceEachLevel) && unit.IsPlayerFaction && state.NextLevel > 1) {
                    int oldHitDie = ((int)classData.CharacterClass.HitDie / 2) + 1;
                    DiceFormula diceFormula = new DiceFormula(1, classData.CharacterClass.HitDie);
                    int roll = RuleRollDice.Dice.D(diceFormula);

                    unit.Stats.HitPoints.BaseValue -= oldHitDie;
                    unit.Stats.HitPoints.BaseValue += roll;
                }
#endif
            }
        }

        [HarmonyPatch(typeof(PrerequisiteFeature), "Check")]
        static class PrerequisiteFeature_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreFeaturePrerequisites) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteFeaturesFromList), "Check")]
        static class PrerequisiteFeaturesFromList_CanLevelUp_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreFeatureListPrerequisites) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(FeatureSelectionState), "IgnorePrerequisites", MethodType.Getter)]
        static class FeatureSelectionState_IgnorePrerequisites_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleFeaturesIgnorePrerequisites) {
                    __result = true;
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

        [HarmonyPatch(typeof(RuleAttackRoll), "IsCriticalConfirmed", MethodType.Getter)]
        static class HitPlayer_OnTriggerl_Patch {
            static void Postfix(ref bool __result, RuleAttackRoll __instance) {
                if (UnitEntityDataUtils.CheckUnitEntityData(__instance.Initiator, settings.allHitsCritical)) {
                    __result = true;
                }
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
        [HarmonyPatch(typeof(UnitCombatState), "AttackOfOpportunity")]
        static class UnitCombatState_AttackOfOpportunity_Patch {
            static bool Prefix(UnitEntityData target) {
                if (UnitEntityDataUtils.CheckUnitEntityData(target, settings.noAttacksOfOpportunitySelection)) {
                    return false;
                }
                return true;

            }
        }
#if false
        [HarmonyPatch(typeof(CampingSettings), "IsDungeon", MethodType.Getter)]
        static class CampingSettings_IsDungeon_Patch {
            static void Postfix(ref bool __result) {
                if (!Main.Enabled) {
                    return;
                }
                if (StringUtils.ToToggleBool(settings.toggleCookingAndHuntingInDungeons)) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(SoundState), "OnAreaLoadingComplete")]
        public static class SoundState_OnAreaLoadingComplete_Patch {
            private static void Postfix() {
                Logger.ModLoggerDebug(Game.Instance.CurrentlyLoadedArea.AreaName.ToString());
                Logger.ModLoggerDebug(Game.Instance.CurrentlyLoadedArea.AssetGuid);
                Logger.ModLoggerDebug(SceneManager.GetActiveScene().name);

                if (StringUtils.ToToggleBool(settings.toggleUnlimitedCasting) && SceneManager.GetActiveScene().name == "HouseAtTheEdgeOfTime_Courtyard_Light") {
                    UIUtility.ShowMessageBox(Strings.GetText("warning_UnlimitedCasting"), DialogMessageBoxBase.BoxType.Message, new Action<DialogMessageBoxBase.BoxButton>(Common.CloseMessageBox));
                }
                if (StringUtils.ToToggleBool(settings.toggleNoDamageFromEnemies) && Game.Instance.CurrentlyLoadedArea.AssetGuid == "0ba5b24abcd5523459e54cd5877cb837") {
                    UIUtility.ShowMessageBox(Strings.GetText("warning_NoDamageFromEnemies"), DialogMessageBoxBase.BoxType.Message, new Action<DialogMessageBoxBase.BoxButton>(Common.CloseMessageBox));
                }

            }
        }

        [HarmonyPatch(typeof(EncumbranceHelper.CarryingCapacity), "GetEncumbrance")]
        [HarmonyPatch(new Type[] { typeof(float) })]
        static class EncumbranceHelperCarryingCapacity_GetEncumbrance_Patch {
            static void Postfix(ref Encumbrance __result) {
                if (StringUtils.ToToggleBool(settings.toggleSetEncumbrance)) {
                    __result = Common.IntToEncumbrance(settings.setEncumbrancIndex);
                }
            }
        }

        [HarmonyPatch(typeof(PartyEncumbranceController), "UpdateUnitEncumbrance")]
        static class PartyEncumbranceController_UpdateUnitEncumbrance_Patch {
            static void Postfix(UnitDescriptor unit) {
                if (StringUtils.ToToggleBool(settings.toggleSetEncumbrance)) {
                    unit.Encumbrance = Common.IntToEncumbrance(settings.setEncumbrancIndex);
                    unit.Remove<UnitPartEncumbrance>();
                }
            }
        }
        [HarmonyPatch(typeof(PartyEncumbranceController), "UpdatePartyEncumbrance")]
        static class PartyEncumbranceController_UpdatePartyEncumbrance_Patch {
            static bool Prefix() {
                if (StringUtils.ToToggleBool(settings.toggleSetEncumbrance)) {
                    player.Encumbrance = Common.IntToEncumbrance(settings.setEncumbrancIndex);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(GlobalMapRules), "ChangePartyOnMap")]
        static class GlobalMapRules_ChangePartyOnMap_Patch {
            static bool Prefix() {
                if (StringUtils.ToToggleBool(settings.toggleInstantPartyChange)) {
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(IngameMenuManager), "OpenGroupManager")]
        static class IngameMenuManager_OpenGroupManager_Patch {
            static bool Prefix(IngameMenuManager __instance) {
                if (StringUtils.ToToggleBool(settings.toggleInstantPartyChange)) {
                    MethodInfo startChangedPartyOnGlobalMap = __instance.GetType().GetMethod("StartChangedPartyOnGlobalMap", BindingFlags.NonPublic | BindingFlags.Instance);
                    startChangedPartyOnGlobalMap.Invoke(__instance, new object[] { });
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BuildModeUtility), "IsDevelopment", MethodType.Getter)]
        static class BuildModeUtility_IsDevelopment_Patch {
            static void Postfix(ref bool __result) {
                if (StringUtils.ToToggleBool(settings.toggleDevTools)) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(SmartConsole), "WriteLine")]
        static class SmartConsole_WriteLine_Patch {
            static void Postfix(string message) {
                if (StringUtils.ToToggleBool(settings.toggleDevTools)) {
                    modLogger.Log(message);
                    UberLoggerAppWindow.Instance.Log(new UberLogger.LogInfo((UnityEngine.Object)null, nameof(SmartConsole), UberLogger.LogSeverity.Message, new List<UberLogger.LogStackFrame>(), (object)message, (object[])Array.Empty<object>()));

                }
            }
        }
        [HarmonyPatch(typeof(SmartConsole), "Initialise")]
        static class SmartConsole_Initialise_Patch {
            static void Postfix() {
                if (StringUtils.ToToggleBool(settings.toggleDevTools)) {
                    SmartConsoleCommands.Register();
                }
            }
        }
        [HarmonyPatch(typeof(Kingmaker.MainMenu), "Start")]
        static class MainMenu_Start_Patch {
            static void Postfix() {
                ModifiedBlueprintTools.Patch();

                if (StringUtils.ToToggleBool(settings.toggleNoTempHPKineticist)) {
                    Cheats.PatchBurnEffectBuff(0);
                }
            }
        }
        [HarmonyPatch(typeof(MainMenuButtons), "Update")]
        static class MainMenuButtons_Update_Patch {
            static void Postfix() {
                if (StringUtils.ToToggleBool(settings.toggleAutomaticallyLoadLastSave) && Storage.firstStart) {
                    Storage.firstStart = false;
                    EventBus.RaiseEvent<IUIMainMenu>((Action<IUIMainMenu>)(h => h.LoadLastGame()));
                }
                Storage.firstStart = false;
            }
        }


        [HarmonyPatch(typeof(UnitPartNegativeLevels), "Add")]
        static class UnitPartNegativeLevels_Add_Patch {
            static bool Prefix(UnitPartNegativeLevels __instance) {
                if (StringUtils.ToToggleBool(settings.toggleNoNegativeLevels) && __instance.Owner.IsPlayerFaction) {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Kingmaker.Items.Slots.ItemSlot), "RemoveItem")]
        [HarmonyPatch(new Type[] { typeof(bool) })]
        static class ItemSlot_RemoveItem_Patch {
            static void Prefix(Kingmaker.Items.Slots.ItemSlot __instance, ItemEntity ___m_Item, UnitDescriptor ___Owner, ref ItemEntity __state) {
                if (Game.Instance.CurrentMode == GameModeType.Default && StringUtils.ToToggleBool(settings.togglAutoEquipConsumables)) {
                    __state = null;
                    if (___Owner.Body.QuickSlots.Any(x => x.HasItem && x.Item == ___m_Item)) {
                        __state = ___m_Item;
                    }
                }
            }
            static void Postfix(Kingmaker.Items.Slots.ItemSlot __instance, ItemEntity ___m_Item, UnitDescriptor ___Owner, ItemEntity __state) {
                if (Game.Instance.CurrentMode == GameModeType.Default && StringUtils.ToToggleBool(settings.togglAutoEquipConsumables)) {
                    if (__state != null) {
                        BlueprintItem blueprint = __state.Blueprint;
                        foreach (ItemEntity item in Game.Instance.Player.Inventory.Items) {
                            if (item.Blueprint.ItemType == ItemsFilter.ItemType.Usable && item.Blueprint == blueprint) {
                                __instance.InsertItem(item);
                                break;
                            }
                        }
                        __state = null;
                    }
                }
            }
        }
#endif
        [HarmonyPatch(typeof(IgnorePrerequisites), "Ignore", MethodType.Getter)]
        static class IgnorePrerequisites_Ignore_Patch {
            private static void Postfix(ref bool __result) {
                if (settings.toggleIgnorePrerequisites) {
                    __result = true;
                }
            }
        }
#if false
        [HarmonyPatch(typeof(CharGenMythicPhaseVM), "IsClassVisible")]
        static class CharGenMythicPhaseVM_IsClassVisible_Patch {
            private static void Postfix(ref bool __result, BlueprintCharacterClass charClass) {
                Logger.Log("IsClassVisible");
                if (settings.toggleIgnorePrerequisites) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(CharGenMythicPhaseVM), "IsClassAvailableToSelect")]
        static class CharGenMythicPhaseVM_IsClassAvailableToSelect_Patch {
            private static void Postfix(ref bool __result, BlueprintCharacterClass charClass) {
                Logger.Log("CharGenMythicPhaseVM.IsClassAvailableToSelect");
                if (settings.toggleIgnorePrerequisites) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(CharGenMythicPhaseVM), "IsPossibleMythicSelection", MethodType.Getter)]
        static class CharGenMythicPhaseVM_IsPossibleMythicSelection_Patch {
            private static void Postfix(ref bool __result) {
                Logger.Log("CharGenMythicPhaseVM.IsPossibleMythicSelection");
                if (settings.toggleIgnorePrerequisites) {
                    __result = true;
                }
            }
        }
#endif

        [HarmonyPatch(typeof(LevelUpController), "IsPossibleMythicSelection", MethodType.Getter)]
        static class LevelUpControllerIsPossibleMythicSelection_Patch {
            private static void Postfix(ref bool __result) {
                //Logger.Log($"LevelUpController.IsPossibleMythicSelection {settings.toggleIgnorePrerequisites}");
                if (settings.toggleIgnorePrerequisites) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(PrerequisiteCasterTypeSpellLevel), "Check")]
        public static class PrerequisiteCasterTypeSpellLevel_Check_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreCasterTypeSpellLevel) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteNoArchetype), "Check")]
        public static class PrerequisiteNoArchetype_Check_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnoreForbiddenArchetype) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(PrerequisiteStatValue), "Check")]
        public static class PrerequisiteStatValue_Check_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleIgnorePrerequisiteStatValue) {
                    __result = true;
                }
            }
        }
#if false
        [HarmonyPatch(typeof(UnitEntityData), "CreateView")]
        public static class UnitEntityData_CreateView_Patch {
            public static void Prefix(ref UnitEntityData __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref __instance);
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintUnit), "PreloadResources")]
        public static class BlueprintUnit_PreloadResources_Patch {
            public static void Prefix(ref BlueprintUnit __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref __instance);
                }
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), "SpawnUnit")]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState) })]
        public static class EntityCreationControllert_SpawnUnit_Patch1 {
            public static void Prefix(ref BlueprintUnit unit) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref unit);
                }
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), "SpawnUnit")]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(UnitEntityView), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState) })]
        public static class EntityCreationControllert_SpawnUnit_Patch2 {
            public static void Prefix(ref BlueprintUnit unit) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref unit);
                }
            }

        }

        [HarmonyPatch(typeof(ContextConditionAlignment), "CheckCondition")]
        public static class ContextConditionAlignment_CheckCondition_Patch {
            public static void Postfix(ref bool __result, ContextConditionAlignment __instance) {
                if (StringUtils.ToToggleBool(settings.toggleReverseCasterAlignmentChecks)) {
                    if (__instance.CheckCaster) {
                        __result = !__result;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RuleSummonUnit), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(BlueprintUnit), typeof(Vector3), typeof(Rounds), typeof(int) })]
        public static class RuleSummonUnit_Constructor_Patch {
            public static void Prefix(UnitEntityData initiator, BlueprintUnit blueprint, Vector3 position, ref Rounds duration, ref int level, RuleSummonUnit __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSummonDurationMultiplier) && UnitEntityDataUtils.CheckUnitEntityData(initiator, (UnitSelectType)settings.indexSummonDurationMultiplier)) {
                    duration = new Rounds(Convert.ToInt32(duration.Value * settings.finalSummonDurationMultiplierValue)); ;
                }

                if (StringUtils.ToToggleBool(settings.toggleSetSummonLevelTo20) && UnitEntityDataUtils.CheckUnitEntityData(initiator, (UnitSelectType)settings.indexSetSummonLevelTo20)) {
                    level = 20;
                }

                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable)) {
                    Storage.SummonedByPlayerFaction = initiator.IsPlayerFaction;
                }

                Logger.ModLoggerDebug("Initiator: " + initiator.CharacterName + $"(PlayerFaction : {initiator.IsPlayerFaction})" + "\nBlueprint: " + blueprint.CharacterName + "\nPosition: " + position.ToString() + "\nDuration: " + duration.Value + "\nLevel: " + level);
            }
        }

        [HarmonyPatch(typeof(ActionBarManager), "CheckTurnPanelView")]
        internal static class ActionBarManager_CheckTurnPanelView_Patch {
            private static void Postfix(ActionBarManager __instance) {
                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable) && CombatController.IsInTurnBasedCombat()) {
                    Traverse.Create((object)__instance).Method("ShowTurnPanel", Array.Empty<object>()).GetValue();
                }
            }
        }
#endif

        /**
        public Buff AddBuff(
          BlueprintBuff blueprint,
          UnitEntityData caster,
          TimeSpan? duration,
          [CanBeNull] AbilityParams abilityParams = null) {
            MechanicsContext context = new MechanicsContext(caster, this.Owner, (BlueprintScriptableObject)blueprint);
            if (abilityParams != null)
                context.SetParams(abilityParams);
            return this.Manager.Add<Buff>(new Buff(blueprint, context, duration));
        }
        */

        [HarmonyPatch(typeof(BuffCollection), "AddBuff")]
        [HarmonyPatch(new Type[] { typeof(BlueprintBuff), typeof(UnitEntityData), typeof(TimeSpan?), typeof(AbilityParams) })]
        public static class Buff_AddBuff_patch {
            public static void Prefix(BlueprintBuff blueprint, UnitEntityData caster, ref TimeSpan? duration, [CanBeNull] AbilityParams abilityParams = null) {
                try {
                    if (!caster.IsPlayersEnemy) {
                        if (duration != null) {
                            duration = TimeSpan.FromTicks(Convert.ToInt64(duration.Value.Ticks * settings.buffDurationMultiplierValue));
                        }
                    }
                }
                catch (Exception e) {
                    modLogger.Log(e.ToString());
                }

                Logger.ModLoggerDebug("Initiator: " + caster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }
#if false
        [HarmonyPatch(typeof(UberLogger.Logger), "ForwardToUnity")]
        static class UberLoggerLogger_ForwardToUnity_Patch {
            static void Prefix(ref object message) {
                if (StringUtils.ToToggleBool(settings.toggleUberLoggerForwardPrefix)) {
                    string message1 = "[UberLogger] " + message as string;
                    message = message1 as object;
                }
            }
        }


        [HarmonyPatch(typeof(DungeonStageInitializer), "Initialize")]
        static class DungeonStageInitializer_Initialize_Patch {
            static void Prefix(BlueprintDungeonArea area) {
                Logger.ModLoggerDebug("Game.Instance.Player.DungeonState.Stage: " + Game.Instance.Player.DungeonState.Stage);
            }
        }
        [HarmonyPatch(typeof(DungeonDebug), "SaveStage")]
        static class DungeonDebug_SaveStage_Patch_Pre {
            static void Prefix(string filename) {
                Logger.ModLoggerDebug("DungeonDebug.SaveStage filename: " + filename);
                Logger.ModLoggerDebug("DungeonDebug.SaveStage Path: " + Path.Combine(Application.persistentDataPath, "DungeonStages"));
            }
        }
        [HarmonyPatch(typeof(DungeonDebug), "SaveStage")]
        static class DungeonDebug_SaveStage_Patch_Post {
            static void Postfix(string filename) {
                if (settings.settingShowDebugInfo) {
                    try {
                        string str = File.ReadAllText(Path.Combine(Application.persistentDataPath, $"DungeonStages\\{filename}"));
                        modLogger.Log($"START---{filename}---START\n" + str + $"\nEND---{filename}---END");
                    }
                    catch (Exception e) {
                        modLogger.Log(e.ToString());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RuleCalculateArmorCheckPenalty), "OnTrigger")]
        public static class RuleCalculateArmorCheckPenalty_OnTrigger_Patch {
            private static bool Prefix(RuleCalculateArmorCheckPenalty __instance) {
                if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0)) {
                    if (!StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        Traverse.Create(__instance).Property("Penalty").SetValue(0);
                        return false;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly) && !__instance.Armor.Wielder.Unit.IsInCombat) {
                        Traverse.Create(__instance).Property("Penalty").SetValue(0);
                        return false;
                    }

                }
                return true;
            }
        }
        [HarmonyPatch(typeof(UIUtilityItem), "GetArmorData")]
        public static class UIUtilityItem_GetArmorData_Patch {
            private static void Postfix(ref UIUtilityItem.ArmorData __result, ref ItemEntityArmor armor) {
                if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0)) {
                    if (!StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        UIUtilityItem.ArmorData armorData = __result;
                        armorData.ArmorCheckPenalty = 0;
                        __result = armorData;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        Logger.ModLoggerDebug(armor.Name);
                        if (armor.Wielder != null) {
                            Logger.ModLoggerDebug(armor.Name + ": " + armor.Wielder.CharacterName);
                            if (!armor.Wielder.Unit.IsInCombat) {
                                Logger.ModLoggerDebug(armor.Name + ": " + armor.Wielder.CharacterName + " - " + armor.Wielder.Unit.IsInCombat);
                                UIUtilityItem.ArmorData armorData = __result;
                                armorData.ArmorCheckPenalty = 0;
                                __result = armorData;
                            }
                        }
                        else {
                            Logger.ModLoggerDebug("!" + armor.Name);
                            UIUtilityItem.ArmorData armorData = __result;
                            armorData.ArmorCheckPenalty = 0;
                            __result = armorData;
                        }

                    }
                }
            }
        }


        [HarmonyPatch(typeof(BlueprintAbility), "GetRange")]
        static class BlueprintAbility_GetRange_Patch_Pre {

            private static Feet defaultClose = 30.Feet();
            private static Feet defaultMedium = 40.Feet();
            private static Feet defaultlong = 50.Feet();
            private static Feet cotwMedium = 60.Feet();
            private static Feet cotwLong = 100.Feet();
            [HarmonyPriority(Priority.Low)]
            static void Postfix(ref Feet __result) {
                if (StringUtils.ToToggleBool(settings.toggleTabletopSpellAbilityRange)) {
                    if (Main.callOfTheWild.ModIsActive()) {
                        if (__result == defaultClose) {
                            __result = 25.Feet();
                        }
                        else if (__result == cotwMedium) {
                            __result = 100.Feet();
                        }
                        else if (__result == cotwLong) {
                            __result = 400.Feet();
                        }
                    }
                    else {
                        if (__result == defaultClose) {
                            __result = 25.Feet();
                        }
                        else if (__result == defaultMedium) {
                            __result = 100.Feet();
                        }
                        else if (__result == defaultlong) {
                            __result = 400.Feet();
                        }
                    }
                }
                if (StringUtils.ToToggleBool(settings.toggleCustomSpellAbilityRange)) {
                    if (Main.callOfTheWild.ModIsActive()) {
                        if (__result == defaultClose) {
                            __result = settings.customSpellAbilityRangeClose.Feet();
                        }
                        else if (__result == cotwMedium) {
                            __result = settings.customSpellAbilityRangeMedium.Feet();
                        }
                        else if (__result == cotwLong) {
                            __result = settings.customSpellAbilityRangeLong.Feet();
                        }
                    }
                    else {
                        if (__result == defaultClose) {
                            __result = settings.customSpellAbilityRangeClose.Feet();
                        }
                        else if (__result == defaultMedium) {
                            __result = settings.customSpellAbilityRangeMedium.Feet();
                        }
                        else if (__result == defaultlong) {
                            __result = settings.customSpellAbilityRangeLong.Feet();
                        }
                    }

                }



                if (StringUtils.ToToggleBool(settings.toggleSpellAbilityRangeMultiplier)) {
                    if (settings.useCustomSpellAbilityRangeMultiplier) {
                        __result = __result * settings.customSpellAbilityRangeMultiplier;
                    }
                    else {
                        __result = __result * settings.spellAbilityRangeMultiplier;
                    }
                }
            }
        }
#endif
        [HarmonyPatch(typeof(RandomEncounterUnitSelector), "PlaceUnits")]
        internal static class RandomEncounterUnitSelector_PlaceUnits_Patch {
            private static void Postfix(ref IList<UnitEntityData> units) {
                foreach (UnitEntityData unit in units) {
                    if (unit.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction)) {
                        Logger.ModLoggerDebug("RandomEncounterUnitSelector.PlaceUnits: " + unit.CharacterName);
                        unit.Stats.HitPoints.BaseValue = Mathf.RoundToInt(unit.Stats.HitPoints.BaseValue * settings.enemyBaseHitPointsMultiplier);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnitSpawnerBase), "Spawn")]
        internal static class UnitSpawner_Spawn_Patch {
            private static void Postfix(ref UnitEntityData __result) {
                if (__result != null && __result.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction)) {
                    Logger.ModLoggerDebug("UnitSpawner.Spawn: " + __result.CharacterName);
                    __result.Stats.HitPoints.BaseValue = Mathf.RoundToInt(__result.Stats.HitPoints.BaseValue * settings.enemyBaseHitPointsMultiplier);
                }
            }
        }

        [HarmonyPatch(typeof(GroupController), "WithRemote", MethodType.Getter)]
        static class GroupController_WithRemote_Patch {
            static void Postfix(GroupController __instance, ref bool __result) {
                if (settings.toggleAccessRemoteCharacters) {
                    if (__instance.FullScreenEnabled) {
                        switch (Traverse.Create(__instance).Field("m_FullScreenUIType").GetValue()) {
                            case FullScreenUIType.Inventory:
                            case FullScreenUIType.CharacterScreen:
                            case FullScreenUIType.SpellBook:
                            case FullScreenUIType.Vendor:
                                __result = true;
                                break;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemSellPrice")]
        static class VendorLogic_GetItemSellPrice_Patch {
            private static void Postfix(ref long __result) {
                __result *= (long)settings.vendorSellPriceMultiplier;
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemBuyPrice")]
        static class VendorLogic_GetItemBuyPrice_Patch {
            private static void Postfix(ref long __result) {
                __result *= (long)settings.vendorBuyPriceMultiplier;
            }
        }
    }
}
