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
    static class BagOfPatches_Tweaks {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;
        public static Player player = Game.Instance.Player;

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

        [HarmonyPatch(typeof(FogOfWarArea), "Active", MethodType.Getter)]
        public static class FogOfWarArea_Active_Patch {
            private static void Postfix(ref FogOfWarArea __result) {
                // We need this to avoid hanging the game on launch
                if (Main.Enabled && Main.IsInGame && __result != null && settings != null) {
                    __result.enabled = !settings.toggleNoFogOfWar;
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

        [HarmonyPatch(typeof(AbilityData), "RequireMaterialComponent", MethodType.Getter)]
        public static class AbilityData_RequireMaterialComponent_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleMaterialComponent) {
                    __result = false;
                }
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
    }
}
