// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Group;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.MainMenuUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.Tutorial;
using Kingmaker.Armies.TacticalCombat.Parts;
using Kingmaker.Armies;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.Kineticist.ActivatableAbility;
using Kingmaker.UI.IngameMenu;
using System.Reflection;
using ModKit;

namespace ToyBox.BagOfPatches {
    static class Tweaks {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;
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

                        if (___m_KingdomEventView.IsFinished || ___m_KingdomEventView.m_Event.AssociatedTask?.AssignedLeader == null || ___m_KingdomEventView.Blueprint.NeedToVisitTheThroneRoom) {
                            return;
                        }

                        bool inProgress = ___m_KingdomEventView.IsInProgress;
                        var leader = ___m_KingdomEventView.m_Event.AssociatedTask?.AssignedLeader;

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

        // TODO - WoTR ver 1.03c changed movement speed again and now this is not needed.  leaving it here in case we need it later WTF???
#if false
        [HarmonyPatch(typeof(UnitEntityData), "CalculateSpeedModifier")]
        public static class UnitEntityData_CalculateSpeedModifier_Patch {
            private static void Postfix(UnitEntityData __instance, ref float __result) {
                //Main.Log($"UnitEntityData_CalculateSpeedModifier_Patch: isInParty:{__instance.Descriptor.IsPartyOrPet()} result:{__result}".cyan());
                if (settings.partyMovementSpeedMultiplier == 1.0f || !__instance.Descriptor.IsPartyOrPet())
                    return;
                UnitPartTacticalCombat partTacticalCombat = __instance.Get<UnitPartTacticalCombat>();
                if (partTacticalCombat != null && partTacticalCombat.Faction != ArmyFaction.Crusaders) return;
                __result *= settings.partyMovementSpeedMultiplier;
                //Main.Log($"finalREsult: {__result}".cyan());

            }
        }
#endif
        [HarmonyPatch(typeof(ClickGroundHandler), "RunCommand")]
        public static class ClickGroundHandler_RunCommand_Patch {
            static UnitMoveTo unitMoveTo = null;
            public static bool Prefix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings) {
                var moveAsOne = Main.settings.toggleMoveSpeedAsOne;
                //Main.Log($"ClickGroundHandler_RunCommand_Patch - isInCombat: {unit.IsInCombat} turnBased:{Game.Instance.Player.IsTurnBasedModeOn()} moveAsOne:{moveAsOne}");
                if (unit.IsInCombat && Game.Instance.Player.IsTurnBasedModeOn()) return true;

                // As of WoTR 1.03c RunCommand is once again the main place to adjust movement speed.  The following was needed when we used UnitEntityData_CalculateSpeedModifier_Patch above to adjust speed in non move as one cases.  
                //if (!moveAsOne) {
                //    return true;
                //}
                UnitPartTacticalCombat partTacticalCombat = unit.Get<UnitPartTacticalCombat>();
                if (partTacticalCombat != null && partTacticalCombat.Faction != ArmyFaction.Crusaders) return true;

                var speedLimit = moveAsOne ? UnitEntityDataUtils.GetMaxSpeed(Game.Instance.UI.SelectionManager.SelectedUnits) : unit.ModifiedSpeedMps;
                Main.Log($"RunCommand - moveAsOne: {moveAsOne} speedLimit: {speedLimit} selectedUnits: {String.Join(" ", Game.Instance.UI.SelectionManager.SelectedUnits.Select(u => $"{u.CharacterName} {u.ModifiedSpeedMps}"))}");
                speedLimit *= Main.settings.partyMovementSpeedMultiplier;

                unitMoveTo = new UnitMoveTo(settings.Destination, 0.3f) {
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
                unitMoveTo.SpeedLimit = speedLimit;
                unitMoveTo.ApplySpeedLimitInCombat = settings.ApplySpeedLimitInCombat;
                unitMoveTo.OverrideSpeed = speedLimit * 1.5f;
                unit.Commands.Run(unitMoveTo);
                if (unit.Commands.Queue.FirstOrDefault((UnitCommand c) => c is UnitMoveTo) == unitMoveTo || Game.Instance.IsPaused) {
                    ClickGroundHandler.ShowDestination(unit, unitMoveTo.Target, false);
                }
                return false;
            }

            //public static void Postfix(UnitEntityData unit, ClickGroundHandler.CommandSettings settings) {
            //    var moveAsOne = Main.settings.toggleMoveSpeedAsOne;
            //    var speedMultiplier = Main.settings.partyMovementSpeedMultiplier;
            //    if (unit.IsInCombat && Game.Instance.Player.IsTurnBasedModeOn()) return;
            //    if (moveAsOne || speedMultiplier == 1) return;

            //    var speedLimit = unit.ModifiedSpeedMps;
            //    speedLimit *= Main.settings.partyMovementSpeedMultiplier;
            //    Main.Log($"RunCommand - moveAsOne: {moveAsOne} speedLimit: {speedLimit} selectedUnits: {String.Join(" ", Game.Instance.UI.SelectionManager.SelectedUnits.Select(u => $"{u.CharacterName} {u.ModifiedSpeedMps}"))}");
            //    unitMoveTo.SpeedLimit = speedLimit;
            //    unitMoveTo.OverrideSpeed = speedLimit * 1.5f;
            //}
        }

        [HarmonyPatch(typeof(FogOfWarArea), "RevealOnStart", MethodType.Getter)]
        public static class FogOfWarArea_Active_Patch {
            private static bool Prefix(ref bool __result) {
                if (!settings.toggleNoFogOfWar) return true;
                __result = true;
                return false;
                //    // We need this to avoid hanging the game on launch
                //    if (Main.Enabled && Main.IsInGame && __result != null && settings != null) {
                //        __result.enabled = !settings.toggleNoFogOfWar;
                //    }
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

        [HarmonyPatch(typeof(BlueprintArmorType), "MaxDexterityBonus", MethodType.Getter)]
        public static class BlueprintArmorType_MaxDexterityBonus_Patch {
            public static void Prefix(ref int ___m_MaxDexterityBonus) {
                if (settings.toggleIgnoreMaxDexterity) {
                    ___m_MaxDexterityBonus = 99;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintArmorType), "ArcaneSpellFailureChance", MethodType.Getter)]
        public static class BlueprintArmorType_ArcaneSpellFailureChance_Patch {
            public static bool Prefix(ref int __result) {
                if (settings.toggleIgnoreSpellFailure) {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(MainMenuBoard), "Update")]
        static class MainMenuButtons_Update_Patch {
            static void Postfix() {
                if (settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;
                    var mainMenuVM = Game.Instance.RootUiContext.MainMenuVM;
                    mainMenuVM.EnterGame(new Action(mainMenuVM.LoadLastSave));
                }
                Main.freshlyLaunched = false;
            }
        }

        [HarmonyPatch(typeof(Tutorial), "IsBanned")]
        static class Tutorial_IsBanned_Patch {
            static bool Prefix(ref Tutorial __instance, ref bool __result) {
                if (settings.toggleForceTutorialsToHonorSettings) {
                    //                    __result = !__instance.HasTrigger ? __instance.Owner.IsTagBanned(__instance.Blueprint.Tag) : __instance.Banned;
                    __result = __instance.Owner.IsTagBanned(__instance.Blueprint.Tag) || __instance.Banned;
                    //modLogger.Log($"hasTrigger: {__instance.HasTrigger} tag: {__instance.Blueprint.Tag} isTagBanned:{__instance.Owner.IsTagBanned(__instance.Blueprint.Tag)} this.Banned: {__instance.Banned} ==> {__result}");
                    return false;
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked), "CheckCondition")]
        public static class RomanceLocked_CheckCondition_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleMultipleRomance) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(ContextActionReduceBuffDuration), nameof(ContextActionReduceBuffDuration.RunAction))]
        public static class ContextActionReduceBuffDuration_RunAction_Patch {
            public static bool Prefix(ContextActionReduceBuffDuration __instance) {
                if (settings.toggleExtendHexes && !Game.Instance.Player.IsInCombat
                    && (__instance.TargetBuff.name.StartsWith("WitchHex") || __instance.TargetBuff.name.StartsWith("ShamanHex"))) {
                    __instance.Target.Unit.Buffs.GetBuff(__instance.TargetBuff).IncreaseDuration(new TimeSpan(0, 10, 0));
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnitPartActivatableAbility), nameof(UnitPartActivatableAbility.GetGroupSize))]
        public static class UnitPartActivatableAbility_GetGroupSize_Patch {
            public static ActivatableAbilityGroup[] groups = new ActivatableAbilityGroup[] {
                ActivatableAbilityGroup.AeonGaze,
                ActivatableAbilityGroup.ArcaneArmorProperty,
                ActivatableAbilityGroup.ArcaneWeaponProperty,
                ActivatableAbilityGroup.AzataMythicPerformance,
                ActivatableAbilityGroup.BarbarianStance,
                ActivatableAbilityGroup.BardicPerformance,
                ActivatableAbilityGroup.ChangeShape,
                ActivatableAbilityGroup.ChangeShapeKitsune,
                ActivatableAbilityGroup.CombatManeuverStrike,
                ActivatableAbilityGroup.CombatStyle,
                ActivatableAbilityGroup.CriticalFeat,
                ActivatableAbilityGroup.DebilitatingStrike,
                ActivatableAbilityGroup.DemonMajorAspect,
                ActivatableAbilityGroup.DivineWeaponProperty,
                ActivatableAbilityGroup.DrovierAspect,
                ActivatableAbilityGroup.DuelistCripplingCritical,
                ActivatableAbilityGroup.ElementalOverflow,
                ActivatableAbilityGroup.FeralTransformation,
                ActivatableAbilityGroup.FormInfusion,
                ActivatableAbilityGroup.GatherPower,
                ActivatableAbilityGroup.HellknightEnchantment,
                ActivatableAbilityGroup.HunterAnimalFocus,
                ActivatableAbilityGroup.Judgment,
                ActivatableAbilityGroup.MagicArrows,
                ActivatableAbilityGroup.MagicalItems,
                ActivatableAbilityGroup.MasterHealingTechnique,
                ActivatableAbilityGroup.MetamagicRod,
                ActivatableAbilityGroup.RagingTactician,
                ActivatableAbilityGroup.RingOfCircumstances,
                ActivatableAbilityGroup.SacredArmorProperty,
                ActivatableAbilityGroup.SacredWeaponProperty,
                ActivatableAbilityGroup.SerpentsFang,
                ActivatableAbilityGroup.ShroudOfWaterMode,
                ActivatableAbilityGroup.SpiritWeaponProperty,
                ActivatableAbilityGroup.StyleStrike,
                ActivatableAbilityGroup.SubstanceInfusion,
                ActivatableAbilityGroup.TransmutationPhysicalEnhancement,
                ActivatableAbilityGroup.TrueMagus,
                ActivatableAbilityGroup.Wings,
                ActivatableAbilityGroup.WitheringLife,
                ActivatableAbilityGroup.WizardDivinationAura,
            };
            public static bool Prefix(ActivatableAbilityGroup group, ref int __result) {
                if (settings.toggleAllowAllActivatable && groups.Any(group)) {
                    __result = 99;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RestrictionCanGatherPower), "IsAvailable")]
        public static class RestrictionCanGatherPower_IsAvailable_Patch {
            public static bool Prefix(ref bool __result) {
                if (!settings.toggleKineticistGatherPower) {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(KineticistAbilityBurnCost), "GetTotal")]
        public static class KineticistAbilityBurnCost_GetTotal_Patch {
            public static void Postfix(ref int __result) {
                __result = Math.Max(0, __result - settings.kineticistBurnReduction);
            }
        }

        [HarmonyPatch(typeof(UnitPartMagus), "IsSpellCombatThisRoundAllowed")]
        public static class UnitPartMagus_IsSpellCombatThisRoundAllowed_Patch {
            public static void Postfix(ref bool __result, UnitPartMagus __instance) {
                if (settings.toggleAlwaysAllowSpellCombat && __instance.Owner != null && __instance.Owner.IsPartyOrPet()) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(GlobalMapPathManager), nameof(GlobalMapPathManager.GetTimeToCapital))]
        public static class GlobalMapPathManager_GetTimeToCapital_Patch {
            public static void Postfix(bool andBack, ref TimeSpan? __result) {
                if (settings.toggleInstantChangeParty && andBack && __result != null) {
                    __result = TimeSpan.Zero;
                }
            }
        }

        [HarmonyPatch(typeof(IngameMenuManager), "OpenGroupManager")]
        static class IngameMenuManager_OpenGroupManager_Patch {
            static bool Prefix(IngameMenuManager __instance) {
                if (settings.toggleInstantPartyChange) {
                    MethodInfo startChangedPartyOnGlobalMap = __instance.GetType().GetMethod("StartChangedPartyOnGlobalMap", BindingFlags.NonPublic | BindingFlags.Instance);
                    startChangedPartyOnGlobalMap.Invoke(__instance, new object[] { });
                    return false;
                }
                return true;
            }
        }
    }
}