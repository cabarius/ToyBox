// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.Rest;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Group;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.MainMenuUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using ModKit;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using System;
using System.Collections.Generic;
using System.Linq;
using TurnBased.Controllers;
using UnityModManagerNet;

namespace ToyBox.BagOfPatches
{
    static class Tweaks
    {
        public static Settings settings = Main.settings;

        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;

        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(CopyScroll), "CanCopySpell")]
        [HarmonyPatch(new[] { typeof(BlueprintAbility), typeof(Spellbook) })]
        public static class CopyScroll_CanCopySpell_Patch
        {
            static bool Prefix()
            {
                return false;
            }

            static void Postfix([NotNull] BlueprintAbility spell, [NotNull] Spellbook spellbook, ref bool __result)
            {
                if (spellbook.IsKnown(spell))
                {
                    __result = false;

                    return;
                }

                bool spellListContainsSpell = spellbook.Blueprint.SpellList.Contains(spell);

                if (settings.toggleSpontaneousCopyScrolls && spellbook.Blueprint.Spontaneous && spellListContainsSpell)
                {
                    __result = true;

                    return;
                }

                __result = spellbook.Blueprint.CanCopyScrolls && spellListContainsSpell;
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindow), "OnClose")]
        public static class KingdomUIEventWindow_OnClose_Patch
        {
            public static bool Prefix(ref bool __state)
            {
                __state = settings.toggleInstantEvent;

                return !__state;
            }

            public static void Postfix(bool __state, KingdomEventUIView ___m_KingdomEventView, KingdomEventHandCartController ___m_Cart)
            {
                if (__state)
                {
                    if (___m_KingdomEventView != null)
                    {
                        EventBus.RaiseEvent((IEventSceneHandler h) => h.OnEventSelected(null, ___m_Cart));

                        if (___m_KingdomEventView.IsFinished)
                        {
                            return;
                        }

                        if (___m_KingdomEventView.m_Event?.AssociatedTask?.AssignedLeader == null)
                        {
                            return;
                        }

                        if (___m_KingdomEventView.Blueprint.NeedToVisitTheThroneRoom)
                        {
                            return;
                        }

                        bool inProgress = ___m_KingdomEventView.IsInProgress;
                        var leader = ___m_KingdomEventView.m_Event.AssociatedTask?.AssignedLeader;

                        if (!inProgress || leader == null)
                        {
                            return;
                        }

                        ___m_KingdomEventView.Event.Resolve(___m_KingdomEventView.Task);

                        if (___m_KingdomEventView.RulerTimeRequired <= 0)
                        {
                            return;
                        }

                        foreach (UnitEntityData unitEntityData in player.AllCharacters)
                        {
                            RestController.ApplyRest(unitEntityData.Descriptor);
                        }

                        new KingdomTimelineManager().MaybeUpdateTimeline();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(KingdomTaskEvent), "SkipPlayerTime", MethodType.Getter)]
        public static class KingdomTaskEvent_SkipPlayerTime_Patch
        {
            public static void Postfix(ref int __result)
            {
                if (settings.toggleInstantEvent)
                {
                    __result = 0;
                }
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindowFooter), "OnStart")]
        public static class KingdomUIEventWindowFooter_OnStart_Patch
        {
            public static bool Prefix(ref bool __state)
            {
                __state = settings.toggleInstantEvent;

                return !__state;
            }

            public static void Postfix(KingdomEventUIView ___m_KingdomEventView, bool __state)
            {
                if (__state)
                {
                    EventBus.RaiseEvent((IKingdomUIStartSpendTimeEvent h) => h.OnStartSpendTimeEvent(___m_KingdomEventView.Blueprint));

                    KingdomTaskEvent kingdomTaskEvent = ___m_KingdomEventView?.Task;

                    EventBus.RaiseEvent((IKingdomUICloseEventWindow h) => h.OnClose());

                    kingdomTaskEvent?.Start(false);

                    if (kingdomTaskEvent == null)
                    {
                        return;
                    }

                    if (kingdomTaskEvent.IsFinished)
                    {
                        return;
                    }

                    if (kingdomTaskEvent.AssignedLeader == null)
                    {
                        return;
                    }

                    if (___m_KingdomEventView.Blueprint.NeedToVisitTheThroneRoom)
                    {
                        return;
                    }

                    kingdomTaskEvent.Event.Resolve(kingdomTaskEvent);

                    if (___m_KingdomEventView.RulerTimeRequired <= 0)
                    {
                        return;
                    }

                    foreach (UnitEntityData unitEntityData in player.AllCharacters)
                    {
                        RestController.ApplyRest(unitEntityData.Descriptor);
                    }

                    new KingdomTimelineManager().MaybeUpdateTimeline();
                }
            }
        }

        [HarmonyPatch(typeof(AbilityResourceLogic), "Spend")]
        public static class AbilityResourceLogic_Spend_Patch
        {
            public static bool Prefix(AbilityData ability)
            {
                UnitEntityData unit = ability.Caster.Unit;

                return unit?.IsPlayerFaction != true || !settings.toggleInfiniteAbilities;
            }
        }

        [HarmonyPatch(typeof(ActivatableAbilityResourceLogic), "SpendResource")]
        public static class ActivatableAbilityResourceLogic_SpendResource_Patch
        {
            public static bool Prefix()
            {
                return !settings.toggleInfiniteAbilities;
            }
        }

        [HarmonyPatch(typeof(AbilityData), "SpellSlotCost", MethodType.Getter)]
        public static class AbilityData_SpellSlotCost_Patch
        {
            public static bool Prefix()
            {
                return !settings.toggleInfiniteSpellCasts;
            }
        }

        [HarmonyPatch(typeof(SpendSkillPoint), "Apply")]
        public static class SpendSkillPoint_Apply_Patch
        {
            public static bool Prefix(ref bool __state)
            {
                __state = settings.toggleInfiniteSkillpoints;

                return !__state;
            }

            public static void Postfix(ref bool __state, LevelUpState state, UnitDescriptor unit, StatType ___Skill)
            {
                if (__state)
                {
                    unit.Stats.GetStat(___Skill).BaseValue++;
                }
            }
        }


        [HarmonyPatch(typeof(UnitCombatState), "HasCooldownForCommand")]
        [HarmonyPatch(new[] { typeof(UnitCommand) })]
        public static class UnitCombatState_HasCooldownForCommand_Patch1
        {
            public static void Postfix(ref bool __result, UnitCombatState __instance)
            {
                if (settings.toggleInstantCooldown && __instance.Unit.IsDirectlyControllable)
                {
                    __result = false;
                }

                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(UnitCombatState), "HasCooldownForCommand")]
        [HarmonyPatch(new[] { typeof(UnitCommand.CommandType) })]
        public static class UnitCombatState_HasCooldownForCommand_Patch2
        {
            public static void Postfix(ref bool __result, UnitCombatState __instance)
            {
                if (settings.toggleInstantCooldown && __instance.Unit.IsDirectlyControllable)
                {
                    __result = false;
                }

                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(UnitCombatState), "OnNewRound")]
        public static class UnitCombatState_OnNewRound_Patch
        {
            public static bool Prefix(UnitCombatState __instance)
            {
                if (__instance.Unit.IsDirectlyControllable && settings.toggleInstantCooldown)
                {
                    __instance.Cooldown.Initiative = 0f;
                    __instance.Cooldown.StandardAction = 0f;
                    __instance.Cooldown.MoveAction = 0f;
                    __instance.Cooldown.SwiftAction = 0f;
                    __instance.Cooldown.AttackOfOpportunity = 0f;
                }

                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn)
                {
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
        public static class UnitEntityData_SpendAction_Patch
        {
            public static void Postfix(UnitCommand.CommandType type, bool isFullRound, float timeSinceCommandStart, UnitEntityData __instance)
            {
                if (!__instance.IsInCombat)
                {
                    return;
                }

                UnitCombatState.Cooldowns cooldown = __instance.CombatState.Cooldown;

                if (CombatController.IsInTurnBasedCombat())
                {
                    if (settings.toggleUnlimitedActionsPerTurn)
                    {
                        return;
                    }

                    switch (type)
                    {
                        case UnitCommand.CommandType.Free:
                            break;

                        case UnitCommand.CommandType.Standard:
                            cooldown.StandardAction += 6f;

                            if (!isFullRound)
                            {
                                break;
                            }

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

                    return;
                }

                if (settings.toggleInstantCooldown)
                {
                    return;
                }

                switch (type)
                {
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

        [HarmonyPatch(typeof(EquipmentRestrictionAlignment), "CanBeEquippedBy")]
        public static class EquipmentRestrictionAlignment_CanBeEquippedBy_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleEquipmentRestrictions)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(EquipmentRestrictionClass), "CanBeEquippedBy")]
        public static class EquipmentRestrictionClassNew_CanBeEquippedBy_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleEquipmentRestrictions)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(EquipmentRestrictionStat), "CanBeEquippedBy")]
        public static class EquipmentRestrictionStat_CanBeEquippedBy_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleEquipmentRestrictions)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(ItemEntityArmor), "CanBeEquippedInternal")]
        public static class ItemEntityArmor_CanBeEquippedInternal_Patch
        {
            public static void Postfix(ItemEntityArmor __instance, UnitDescriptor owner, ref bool __result)
            {
                if (settings.toggleEquipmentRestrictions)
                {
                    __result = __instance.Blueprint is BlueprintItemEquipment blueprint && blueprint.CanBeEquippedBy(owner);
                }
            }
        }

        [HarmonyPatch(typeof(ItemEntityShield), "CanBeEquippedInternal")]
        public static class ItemEntityShield_CanBeEquippedInternal_Patch
        {
            public static void Postfix(ItemEntityShield __instance, UnitDescriptor owner, ref bool __result)
            {
                if (settings.toggleEquipmentRestrictions)
                {
                    __result = __instance.Blueprint is BlueprintItemEquipment blueprint && blueprint.CanBeEquippedBy(owner);
                }
            }
        }

        [HarmonyPatch(typeof(ItemEntityWeapon), "CanBeEquippedInternal")]
        public static class ItemEntityWeapon_CanBeEquippedInternal_Patch
        {
            public static void Postfix(ItemEntityWeapon __instance, UnitDescriptor owner, ref bool __result)
            {
                if (settings.toggleEquipmentRestrictions)
                {
                    __result = __instance.Blueprint is BlueprintItemEquipment blueprint && blueprint.CanBeEquippedBy(owner);
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintAnswerBase), "IsAlignmentRequirementSatisfied", MethodType.Getter)]
        public static class BlueprintAnswerBase_IsAlignmentRequirementSatisfied_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleDialogRestrictions)
                {
                    __result = true;
                }
            }
        }


        [HarmonyPatch(typeof(BlueprintSettlementBuilding), "CheckRestrictions", typeof(SettlementState))]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch1
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleSettlementRestrictions)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintSettlementBuilding), "CheckRestrictions", typeof(SettlementState), typeof(SettlementGridTopology.Slot))]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch2
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleSettlementRestrictions)
                {
                    __result = true;
                }
            }
        }

        // public static bool Prefix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings) {
        // old: public static bool Prefix(UnitEntityData unit, Vector3 p, float? speedLimit, float orientation, float delay, bool showTargetMarker) {
        [HarmonyPatch(typeof(ClickGroundHandler), "RunCommand")]
        public static class ClickGroundHandler_RunCommand_Patch
        {
            public static bool Prefix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings)
            {
                bool moveAsOne = Main.settings.toggleMoveSpeedAsOne;

                float speedLimit = moveAsOne ? UnitEntityDataUtils.GetMaxSpeed(Game.Instance.UI.SelectionManager.SelectedUnits) : unit.ModifiedSpeedMps;
                speedLimit *= Main.settings.partyMovementSpeedMultiplier;

                UnitMoveTo unitMoveTo = new UnitMoveTo(settings.Destination, 0.3f)
                {
                    MovementDelay = settings.Delay,
                    Orientation = settings.Orientation,
                    CreatedByPlayer = true
                };

                if (BuildModeUtility.IsDevelopment)
                {
                    if (CheatsAnimation.SpeedForce > 0f)
                    {
                        unitMoveTo.OverrideSpeed = CheatsAnimation.SpeedForce;
                    }

                    unitMoveTo.MovementType = (UnitAnimationActionLocoMotion.WalkSpeedType)CheatsAnimation.MoveType.Get();
                }

                if (Main.settings.partyMovementSpeedMultiplier > 1)
                {
                    unitMoveTo.OverrideSpeed = speedLimit;
                }

                unitMoveTo.SpeedLimit = speedLimit;
                unitMoveTo.ApplySpeedLimitInCombat = settings.ApplySpeedLimitInCombat;
                unit.Commands.Run(unitMoveTo);

                if (unit.Commands.Queue.FirstOrDefault(c => c is UnitMoveTo) == unitMoveTo || Game.Instance.IsPaused)
                {
                    ClickGroundHandler.ShowDestination(unit, unitMoveTo.Target, false);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(FogOfWarArea), "RevealOnStart", MethodType.Getter)]
        public static class FogOfWarArea_Active_Patch
        {
            private static bool Prefix(ref bool __result)
            {
                if (!settings.toggleNoFogOfWar)
                {
                    return true;
                }

                __result = true;

                return false;
            }
        }

        [HarmonyPatch(typeof(GameHistoryLog), "HandlePartyCombatStateChanged")]
        static class GameHistoryLog_HandlePartyCombatStateChanged_Patch
        {
            private static void Postfix(ref bool inCombat)
            {
                if (!inCombat && settings.toggleRestoreSpellsAbilitiesAfterCombat)
                {
                    List<UnitEntityData> partyMembers = Game.Instance.Player.PartyAndPets;

                    foreach (UnitEntityData u in partyMembers)
                    {
                        foreach (BlueprintScriptableObject resource in u.Descriptor.Resources)
                        {
                            u.Descriptor.Resources.Restore(resource);
                        }

                        foreach (Spellbook spellbook in u.Descriptor.Spellbooks)
                        {
                            spellbook.Rest();
                        }

                        u.Brain.RestoreAvailableActions();
                    }
                }

                if (!inCombat && settings.toggleInstantRestAfterCombat)
                {
                    CheatsCombat.RestAll();
                }
            }
        }

        [HarmonyPatch(typeof(GroupController), "WithRemote", MethodType.Getter)]
        static class GroupController_WithRemote_Patch
        {
            static void Postfix(GroupController __instance, ref bool __result)
            {
                if (!settings.toggleAccessRemoteCharacters)
                {
                    return;
                }

                if (!__instance.FullScreenEnabled)
                {
                    return;
                }

                switch (Traverse.Create(__instance).Field("m_FullScreenUIType").GetValue())
                {
                    case FullScreenUIType.Inventory:
                    case FullScreenUIType.CharacterScreen:
                    case FullScreenUIType.SpellBook:
                    case FullScreenUIType.Vendor:
                        __result = true;

                        break;
                }
            }
        }

        [HarmonyPatch(typeof(AbilityData), "RequireMaterialComponent", MethodType.Getter)]
        public static class AbilityData_RequireMaterialComponent_Patch
        {
            public static void Postfix(ref bool __result)
            {
                if (settings.toggleMaterialComponent)
                {
                    __result = false;
                }
            }
        }


        [HarmonyPatch(typeof(ItemEntity), "SpendCharges", typeof(UnitDescriptor))]
        public static class ItemEntity_SpendCharges_Patch
        {
            public static bool Prefix(ref bool __state)
            {
                __state = settings.toggleInfiniteItems;

                return !__state;
            }

            public static void Postfix(bool __state, ItemEntity __instance, ref bool __result, UnitDescriptor user)
            {
                if (__state)
                {
                    if (__instance.Blueprint is BlueprintItemEquipment blueprintItemEquipment && blueprintItemEquipment?.GainAbility != false)
                    {
                        if (__instance.IsSpendCharges)
                        {
                            bool hasNoCharges = false;

                            if (__instance.Charges > 0)
                            {
                                ItemEntityUsable itemEntityUsable = new ItemEntityUsable((BlueprintItemEquipmentUsable)__instance.Blueprint);

                                if (user.State.Features.HandOfMagusDan && itemEntityUsable.Blueprint.Type == UsableItemType.Scroll)
                                {
                                    RuleRollDice ruleRollDice = new RuleRollDice(user.Unit, new DiceFormula(1, DiceType.D100));
                                    Rulebook.Trigger(ruleRollDice);

                                    if (ruleRollDice.Result <= 25)
                                    {
                                        __result = true;

                                        return;
                                    }
                                }

                                if (user.IsPlayerFaction)
                                {
                                    __result = true;

                                    return;
                                }

                                --__instance.Charges;
                            }
                            else
                            {
                                hasNoCharges = true;
                            }

                            if (__instance.Charges >= 1 || blueprintItemEquipment.RestoreChargesOnRest)
                            {
                                __result = !hasNoCharges;

                                return;
                            }

                            if (__instance.Count > 1)
                            {
                                __instance.DecrementCount(1);
                                __instance.Charges = 1;
                            }
                            else
                            {
                                ItemsCollection collection = __instance.Collection;
                                collection?.Remove(__instance);
                            }

                            __result = !hasNoCharges;
                        }
                        else
                        {
                            __result = true;
                        }
                    }
                    else
                    {
                        __result = false;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(MetamagicHelper), "DefaultCost")]
        public static class MetamagicHelper_DefaultCost_Patch
        {
            public static void Postfix(ref int __result)
            {
                if (settings.toggleMetamagicIsFree)
                {
                    __result = 0;
                }
            }
        }

        [HarmonyPatch(typeof(RuleCollectMetamagic), "AddMetamagic")]
        public static class RuleCollectMetamagic_AddMetamagic_Patch
        {
            public static bool Prefix()
            {
                return !settings.toggleMetamagicIsFree;
            }

            public static void Postfix(ref RuleCollectMetamagic __instance, int ___m_SpellLevel, Feature metamagicFeature)
            {
                if (settings.toggleMetamagicIsFree)
                {
                    AddMetamagicFeat component = metamagicFeature.GetComponent<AddMetamagicFeat>();

                    if (component == null)
                    {
                        Main.Debug(string.Format("Trying to add metamagic feature without metamagic component: {0}", metamagicFeature));
                    }
                    else
                    {
                        Metamagic metamagic = component.Metamagic;
                        __instance.KnownMetamagics.Add(metamagicFeature);

                        if (___m_SpellLevel < 0 || ___m_SpellLevel >= 10)
                        {
                            return;
                        }

                        if (___m_SpellLevel + component.Metamagic.DefaultCost() > 10)
                        {
                            return;
                        }

                        if (__instance.SpellMetamagics.Contains(metamagicFeature))
                        {
                            return;
                        }

                        if (__instance.Spell != null && (__instance.Spell.AvailableMetamagic & metamagic) != metamagic)
                        {
                            return;
                        }

                        __instance.SpellMetamagics.Add(metamagicFeature);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuBoard), "Update")]
        static class MainMenuButtons_Update_Patch
        {
            static void Postfix()
            {
                if (settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched)
                {
                    Main.freshlyLaunched = false;
                    var mainMenuVM = Game.Instance.RootUiContext.MainMenuVM;
                    mainMenuVM.EnterGame(mainMenuVM.LoadLastSave);
                }

                Main.freshlyLaunched = false;
            }
        }
    }
}