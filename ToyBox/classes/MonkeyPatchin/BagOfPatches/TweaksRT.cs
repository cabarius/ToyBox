// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Globalmap;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.Tutorial;
using Kingmaker.Code.UI.MVVM.VM.MainMenu;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.Sound;
using ModKit;
using Owlcat.Runtime.Core;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using Owlcat.Runtime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DG.Tweening;
using UnityEngine;
using static Kingmaker.Utility.MassLootHelper;
using Object = UnityEngine.Object;
using Kingmaker.Blueprints.Area;
using Kingmaker.Code.UI.MVVM.View.MainMenu.PC;
using Kingmaker.Globalmap;
using Kingmaker.UI.Legacy.LoadingScreen;
using ToyBox;
using Kingmaker.Code.UI.MVVM.View.LoadingScreen;
using Kingmaker.Networking;
using Kingmaker.UI.Sound;
using UniRx;
using Kingmaker.Designers;
using Kingmaker.Code.UI.MVVM.VM.LoadingScreen;
using Kingmaker.UI.Common;
using System.Collections;
using Kingmaker.UI;
using Kingmaker.UI.PathRenderer;
using static Kingmaker.Sound.AkAudioService;
using Kingmaker.Pathfinding;
using Kingmaker.View.Covers;
using static Kingmaker.UnitLogic.Abilities.AbilityData;

namespace ToyBox.BagOfPatches {
    internal static class Tweaks {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(PartUnitCombatState))]
        private static class PartUnitCombatStatePatch {
            public static void MaybeKill(PartUnitCombatState unitCombatState) {
                if (Settings.togglekillOnEngage) {
                    List<UnitEntityData> partyUnits = Game.Instance.Player.m_PartyAndPets;
                    UnitEntityData unit = unitCombatState.Owner;
                    if (unit.CombatGroup.IsEnemy(GameHelper.GetPlayerCharacter())
                        && !partyUnits.Contains(unit)) {
                        CheatsCombat.KillUnit(unit);
                    }
                }
            }

            [HarmonyPatch(nameof(PartUnitCombatState.JoinCombat))]
            [HarmonyPostfix]
            public static void JoinCombat(PartUnitCombatState __instance, bool surprised) {
                MaybeKill(__instance);
            }
        }

        [HarmonyPatch(typeof(AudioServiceDrivingBehaviour), nameof(AudioServiceDrivingBehaviour.OnApplicationFocus))]
        public static class AudioServiceDrivingBehaviourPatch {
            [HarmonyPrefix]
            private static bool OnApplicationFocus(AkSoundEngineController __instance) {
                Mod.Trace($"AudioServiceDrivingBehaviour.OnApplicationFocus - {Settings.toggleContinueAudioOnLostFocus}");
                return !Settings.toggleContinueAudioOnLostFocus;
            }
        }
        [HarmonyPatch(typeof(SoundState), nameof(SoundState.OnApplicationFocusChanged))]
        public static class SoundState_OnApplicationFocusChanged_Patch {
            [HarmonyPrefix]
            private static bool OnApplicationFocusChanged() {
                Mod.Trace($"AudioServiceDrivingBehaviour.OnApplicationFocusChanged - {Settings.toggleContinueAudioOnLostFocus}");
                return !Settings.toggleContinueAudioOnLostFocus;
            }
        }


        [HarmonyPatch(typeof(GameHistoryLog), nameof(GameHistoryLog.HandlePartyCombatStateChanged))]
        private static class GameHistoryLog_HandlePartyCombatStateChanged_Patch {
            private static void Postfix(ref bool inCombat) {
                if (!inCombat && Settings.toggleRestoreSpellsAbilitiesAfterCombat) {
                    var partyMembers = Game.Instance.Player.PartyAndPets;
                    foreach (var u in partyMembers) {
                        foreach (var resource in u.AbilityResources)
                            u.AbilityResources.Restore(resource);
                        u.Brain.RestoreAvailableActions();
                    }
                }
                if (!inCombat && Settings.toggleRechargeItemsAfterCombat) { }
                if (!inCombat && Settings.toggleInstantRestAfterCombat) {
                    CheatsCombat.RestAll();
                }
            }
        }
#if FALSE

        [HarmonyPatch(typeof(FogOfWarArea), nameof(FogOfWarArea.RevealOnStart), MethodType.Getter)]
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

        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.RequireMaterialComponent), MethodType.Getter)]
        public static class AbilityData_RequireMaterialComponent_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleMaterialComponent) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintArmorType), nameof(BlueprintArmorType.HasDexterityBonusLimit), MethodType.Getter)]
        public static class BlueprintArmorType_HasDexterityBonusLimit_Patch {
            public static bool Prefix(ref bool __result) {
                if (settings.toggleIgnoreMaxDexterity) {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BlueprintArmorType), nameof(BlueprintArmorType.ArmorChecksPenalty), MethodType.Getter)]
        public static class BlueprintArmorType_ArmorChecksPenalty_Patch {
            public static bool Prefix(ref int __result) {
                if (settings.toggleIgnoreArmorChecksPenalty) {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ItemEntityArmor), nameof(ItemEntityArmor.RecalculateStats))]
        public static class ItemEntityArmor_RecalculateStats_Patch {
            public static void Postfix(ItemEntityArmor __instance) {
                if (settings.toggleIgnoreSpeedReduction) {
                    if (__instance.m_Modifiers != null) {
                        __instance.m_Modifiers.ForEach(delegate (ModifiableValue.Modifier m) {
                            var appliedTo = m.AppliedTo;
                            var desc = m.ModDescriptor;
                            if (appliedTo == __instance.Wielder.Stats.Speed && (desc == ModifierDescriptor.Shield || desc == ModifierDescriptor.Armor)) {
                                m.Remove();
                            }
                        });
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintArmorType), nameof(BlueprintArmorType.ArcaneSpellFailureChance), MethodType.Getter)]
        public static class BlueprintArmorType_ArcaneSpellFailureChance_Patch {
            public static bool Prefix(ref int __result) {
                if (settings.toggleIgnoreSpellFailure) {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RuleCastSpell))]
        public static class RuleCastSpell_SpellFailureChance_Patch {
            [HarmonyPatch(nameof(RuleCastSpell.SpellFailureChance), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool PrefixSpellFailureChance(ref int __result) {
                if (settings.toggleIgnoreSpellFailure) {
                    __result = 0;
                    return false;
                }
                return true;
            }

            [HarmonyPatch(nameof(RuleCastSpell.ArcaneSpellFailureChance), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool PrefixArcaneSpellFailureChance(ref int __result) {
                if (settings.toggleIgnoreSpellFailure) {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RuleDrainEnergy), nameof(RuleDrainEnergy.TargetIsImmune), MethodType.Getter)]
        private static class RuleDrainEnergy_Immune_Patch {
            public static void Postfix(RuleDrainEnergy __instance, ref bool __result) {
                if (__instance.Target.Descriptor.IsPartyOrPet() && settings.togglePartyNegativeLevelImmunity) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(RuleDealStatDamage), nameof(RuleDealStatDamage.Immune), MethodType.Getter)]
        private static class RuleDealStatDamage_Immune_Patch {
            public static void Postfix(RuleDrainEnergy __instance, ref bool __result) {
                if (__instance.Target.Descriptor.IsPartyOrPet() && settings.togglePartyAbilityDamageImmunity) {
                    __result = true;
                }
            }
        }

        // This is probably a no go for RT
        [HarmonyPatch]
        private static class AbilityAlignment_IsRestrictionPassed_Patch {
            [HarmonyPatch(typeof(AbilityCasterAlignment), nameof(AbilityCasterAlignment.IsCasterRestrictionPassed))]
            [HarmonyPostfix]
            public static void PostfixCasterRestriction(ref bool __result) {
                if (settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = true;
                }
            }

            [HarmonyPatch(typeof(UnitPartForbiddenSpellbooks), nameof(UnitPartForbiddenSpellbooks.IsForbidden))]
            [HarmonyPostfix]
            public static void PostfixForbiddenSpellbookRestriction(ref bool __result) {
                if (settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = false;
                }
            }

            [HarmonyPatch(typeof(UnitPartForbiddenSpellbooks), nameof(UnitPartForbiddenSpellbooks.Add))]
            [HarmonyPrefix]
            public static bool PrefixForbidSpellbook(ForbidSpellbookReason reason) {
                if (settings.toggleIgnoreAbilityAlignmentRestriction && reason == ForbidSpellbookReason.Alignment) { // Don't add to forbidden list
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(AbilityTargetAlignment), nameof(AbilityTargetAlignment.IsTargetRestrictionPassed))]
            [HarmonyPostfix]
            public static void PostfixTargetRestriction(ref bool __result) {
                if (settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = true;
                }
            }
        }

#endif
        [HarmonyPatch(typeof(AbilityData))]
        private static class AbilityDataPatch {
            //[HarmonyPatch(nameof(AbilityData.CanTargetFromNode), new Type[] {typeof(CustomGridNodeBase), typeof(CustomGridNodeBase), typeof(TargetWrapper), typeof(int), typeof(LosCalculations.CoverType), typeof(UnavailabilityReasonType?)})]
            [HarmonyPatch(nameof(AbilityData.CanTargetFromNode),
                          new Type[] {
                              typeof(CustomGridNodeBase),
                              typeof(CustomGridNodeBase),
                              typeof(TargetWrapper),
                              typeof(int),
                              typeof(LosCalculations.CoverType),
                              typeof(UnavailabilityReasonType?)
                          },
                          new ArgumentType[] {
                              ArgumentType.Normal,
                              ArgumentType.Normal,
                              ArgumentType.Normal,
                              ArgumentType.Out,
                              ArgumentType.Out,
                              ArgumentType.Out
                          })]
            [HarmonyPostfix]
            public static void CanTargetFromNode(
                    CustomGridNodeBase casterNode,
                    CustomGridNodeBase targetNodeHint,
                    TargetWrapper target,
                    ref int distance,
                    ref LosCalculations.CoverType los,
                    ref UnavailabilityReasonType? unavailabilityReason,
                    AbilityData __instance,
                    ref bool __result
                ) {
                if (!Settings.toggleIgnoreAbilityAnyRestriction) return;
                if (!(__instance?.Caster?.IsPartyOrPet() ?? false)) return;
                if (__result) return;

                if (unavailabilityReason is UnavailabilityReasonType reason) {
                    switch (reason) {
                        case UnavailabilityReasonType.AreaEffectsCannotOverlap:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityAoeOverlap)
                                __result = true;
                            break;
                        case UnavailabilityReasonType.HasNoLosToTarget:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityLineOfSight)
                                __result = true;
                            break;
                        case UnavailabilityReasonType.TargetTooFar:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityTargetTooFar)
                                __result = true;
                            break;
                        case UnavailabilityReasonType.TargetTooClose:
                            if (Settings.toggleIgnoreAbilityAnyRestriction || Settings.toggleIgnoreAbilityTargetTooClose)
                                __result = true;
                            break;
                        default:
                            if (Settings.toggleIgnoreAbilityAnyRestriction)
                                __result = true;
                            break;
                    }
                }
                else if (Settings.toggleIgnoreAbilityAnyRestriction)
                    __result = true;
            }
        }
#if false

toggleIgnoreAbilityAoeOverlap
toggleIgnoreAbilityLineOfSight
toggleIgnoreAbilityTargetTooFar
toggleIgnoreAbilityTargetTooClose

        [HarmonyPatch(typeof(AbilityData))]
        private static class AbilityDataPatch {
            [HarmonyPatch(nameof(AbilityData.CanTargetFromNode), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool CanTargetFromNode(
                    AbilityData __instance,
                    CustomGridNodeBase casterNode,
                    CustomGridNodeBase targetNodeHint,
                    TargetWrapper target,
                    out int distance,
                    out LosCalculations.CoverType los,
                    out UnavailabilityReasonType? unavailabilityReason,
                    ref bool __result
                ) {

                distance = -1;
                los = LosCalculations.CoverType.None;
                unavailabilityReason = new UnavailabilityReasonType?();
                if (!Settings.toggleIgnoreAbilityAnyRestriction) return true;
                if (!(__instance?.Caster?.IsPartyOrPet() ?? false)) return true;

                var shootingPosition = __instance.GetBestShootingPosition(casterNode, target);
                if (!__instance.IsValid(target, casterNode.Vector3Position)) {
                    unavailabilityReason = UnavailabilityReasonType.Unknown;
                    __result = false;
                    return false;
                }

                if (!__instance.IsPatternRestrictionPassed(target)) {
                    unavailabilityReason = UnavailabilityReasonType.AreaEffectsCannotOverlap;
                    __result = false;
                    return false;
                }

                var customGridNodeBase = targetNodeHint ?? target.NearestNode;
                if (__instance.Weapon?.Blueprint != null && __instance.Weapon.Blueprint.IsMelee && !LosCalculations.HasMeleeLos(casterNode, customGridNodeBase)) {
                    unavailabilityReason = UnavailabilityReasonType.HasNoLosToTarget;
                    __result = false;
                    return false;
                }

                if (__instance.IsRangeUnrestrictedForTarget(target)) {
                    __result = true;
                    return false;
                }
                distance = WarhammerGeometryUtils.DistanceToInCells(casterNode.Vector3Position,
                                                                    __instance.Caster.SizeRect,
                                                                    __instance.Caster.Forward,
                                                                    target.Point,
                                                                    target.SizeRect,
                                                                    target.Forward);
                if (distance > __instance.RangeCells) {
                    unavailabilityReason = UnavailabilityReasonType.TargetTooFar;
                    __result = false;
                    return false;
                }

                if (distance < __instance.MinRangeCells) {
                    unavailabilityReason = UnavailabilityReasonType.TargetTooClose;
                    __result = false;
                    return false;
                }

                if (__instance.NeedLoS) {
                    if (__instance.Blueprint.IsLosDefinedByPattern || __instance.Caster is StarshipEntity) {
                        if (UnitPredictionManager.Instance != null && UnitPredictionManager.Instance.AffectedNodes != null && !UnitPredictionManager.Instance.AffectedNodes.Contains(customGridNodeBase)) {
                            unavailabilityReason = UnavailabilityReasonType.HasNoLosToTarget;
                            __result = false;
                            return false;
                        }
                    }
                    else if (!LosCalculations.HasLos(__instance.UseBestShootingPosition ? shootingPosition : casterNode,
                                                     __instance.Caster.SizeRect,
                                                     customGridNodeBase,
                                                     target.SizeRect)) {
                        unavailabilityReason = UnavailabilityReasonType.HasNoLosToTarget;
                        __result = false;
                        return false;
                    }
                }

                __result = true;
                return false;
            }
        }
#endif

        [HarmonyPatch(typeof(MainMenuPCView))]
        private static class MainMenuPCViewPatch {
            [HarmonyPatch(nameof(MainMenuPCView.BindViewImplementation))]
            [HarmonyPostfix]
            private static void BindViewImplementation(MainMenuPCView __instance) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    Main.freshlyLaunched = false;
                    Mod.Warn("Auto Load Save on Launch disabled");
                    return;
                }
                if (Settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;
                    Game.Instance.SaveManager.UpdateSaveListIfNeeded();
                    MainThreadDispatcher.StartCoroutine(UIUtilityCheckSaves.WaitForSaveUpdated(() => { __instance.ViewModel.LoadLastGame(); }));
                }
                Main.freshlyLaunched = false;
            }
        }

        [HarmonyPatch(typeof(LoadingScreenBaseView))]
        public static class LoadingScreenBaseViewPatch {
            [HarmonyPatch(nameof(LoadingScreenBaseView.ShowUserInputLayer))]
            [HarmonyPrefix]
            private static bool ShowUserInputLayer(LoadingScreenBaseView __instance, bool state) {
                if (!Settings.toggleSkipAnyKeyToContinueWhenLoadingSaves) return true;
                if (!state)
                    return false;
                __instance.m_ProgressBarContainer.DOFade(0.0f, 1f).OnComplete(() => __instance.StartPressAnyKeyLoopAnimation()).SetUpdate(true);
                __instance.AddDisposable(MainThreadDispatcher.UpdateAsObservable()
                                                             .Subscribe(_ => {
                                                                 UISounds.Instance.Sounds.Buttons.ButtonClick.Play();
                                                                 if (PhotonManager.Lobby.IsLoading)
                                                                     PhotonManager.Instance.ContinueLoading();
                                                                 EventBus.RaiseEvent((Action<IContinueLoadingHandler>)(h => h.HandleContinueLoading()));
                                                             }));
                return false;
            }
        }
        public static class UnitEntityData_CanRollPerception_Extension {
            public static bool TriggerReroll = false;
            public static bool CanRollPerception(UnitEntityData unit) {
                if (TriggerReroll) {
                    return true;
                }

                return unit.MovementAgent.Position.To2D() != unit.MovementAgent.m_PreviousPosition;
            }
        }

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.IsUsableFromInventory), MethodType.Getter)]
        public static class ItemEntity_IsUsableFromInventory_Patch {
            // Allow Item Use From Inventory During Combat
            public static bool Prefix(ItemEntity __instance, ref bool __result) {
                if (!Settings.toggleUseItemsDuringCombat) return true;
                return __instance.Blueprint is not BlueprintItemEquipmentUsable;
            }
        }


        [HarmonyPatch(typeof(PartyAwarenessController))]
        public static class PartyAwarenessControllerPatch {
#if false // TODO: why does this crash the game on load into area
            public static MethodInfo HasMotionThisSimulationTick_Method = AccessTools.DeclaredMethod(typeof(PartMovable), "get_HasMotionThisSimulationTick");
            public static MethodInfo CanRollPerception_Method = AccessTools.DeclaredMethod(typeof(UnitEntityData_CanRollPerception_Extension), "CanRollPerception");

            [HarmonyPatch(nameof(PartyAwarenessController.Tick))]
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                foreach (var instr in instructions) {
                    if (instr.Calls(HasMotionThisSimulationTick_Method)) {
                        Mod.Trace("Found HasMotionThisSimulationTick and modded it");
                        yield return new CodeInstruction(OpCodes.Call, CanRollPerception_Method);
                    }
                    else {
                        yield return instr;
                    }
                }
            }
#endif
            [HarmonyPatch(nameof(PartyAwarenessController.Tick))]
            [HarmonyPostfix]
            private static void Tick() => UnitEntityData_CanRollPerception_Extension.TriggerReroll = false;
        }

#if false

        private static void BindViewImplementation(MainMenuPCView __instance) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    Main.freshlyLaunched = false;
                    Mod.Warn("Auto Load Save on Launch disabled");
                    return;
                }
                if (Settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;
                    __instance.ViewModel.LoadLastGame();
                }
                Main.freshlyLaunched = false;
            }
        }

        [HarmonyPatch(typeof(MainMenuSideBarVM))]
        private static class UIUtilityCheckSavesPatch {
            [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(IUIMainMenu) })]
            [HarmonyPostfix]
            public static void Postfix(MainMenuSideBarVM __instance) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    Main.freshlyLaunched = false;
                    Mod.Warn("Auto Load Save on Launch disabled");
                    return;
                }
                if (Settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;
                    Game.Instance.SaveManager.UpdateSaveListIfNeeded();
                    MainThreadDispatcher.StartCoroutine(UIUtilityCheckSaves.WaitForSaveUpdated(() => {
                        var latestSave = Game.Instance.SaveManager.GetLatestSave();
                        if (latestSave != null)
                            Game.Instance.LoadGameFromMainMenu(latestSave);
                        else
                            Game.Instance.LoadNewGame();
                    }));
                }
            }
        }


        [HarmonyPatch(typeof(MainMenuPCView))]
        private static class MainMenuPCViewPatch {
            [HarmonyPatch(nameof(MainMenuPCView.BindViewImplementation))]
            [HarmonyPostfix]
            private static void BindViewImplementation(MainMenuPCView __instance) {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    Main.freshlyLaunched = false;
                    Mod.Warn("Auto Load Save on Launch disabled");
                    return;
                }
                if (Settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;
                    __instance.ViewModel.LoadLastGame();
                }
                Main.freshlyLaunched = false;
            }
        }


        [HarmonyPatch(typeof(LoadingScreenBaseView))]
        public static class LoadingScreenBaseViewPatch {
            [HarmonyPatch(nameof(LoadingScreenBaseView.Show))]
            [HarmonyPrefix]
            private static bool Show(LoadingScreenBaseView __instance) {
                if (!Settings.toggleSkipAnyKeyToContinueWhenLoadingSaves) return true;
                __instance.ViewModel.NeedUserInput.Value = false;
                return true;
            }
        }
#endif
#if false
        [HarmonyPatch(typeof(Tutorial), nameof(Tutorial.IsBanned))]
        private static class Tutorial_IsBanned_Patch {
            private static bool Prefix(ref Tutorial __instance, ref bool __result) {
                if (Settings.toggleForceTutorialsToHonorSettings) {
                    //                    __result = !__instance.HasTrigger ? __instance.Owner.IsTagBanned(__instance.Blueprint.Tag) : __instance.Banned;
                    __result = !__instance.Owner.IsTagBanned(__instance.Blueprint.Tag) || !__instance.Banned;
                    //modLogger.Log($"hasTrigger: {__instance.HasTrigger} tag: {__instance.Blueprint.Tag} isTagBanned:{__instance.Owner.IsTagBanned(__instance.Blueprint.Tag)} this.Banned: {__instance.Banned} ==> {__result}");
                    return false;
                }
                return true;
            }
        }
#endif
#if false
        [HarmonyPatch(typeof(Player), nameof(Player.GameOver))]
        private static class Player_GameOverReason_Patch {
            private static bool Prefix(Player __instance, Player.GameOverReasonType reason) {
                if (!settings.toggleGameOverFixLeeerrroooooyJenkins || reason != Player.GameOverReasonType.EssentialUnitIsDead) return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ItemsCollection), nameof(ItemsCollection.DeltaWeight))]
        public static class NoWeight_Patch1 {
            public static void Refresh(bool value) {
                if (value)
                    Game.Instance.Player.Inventory.Weight = 0f;
                else
                    Game.Instance.Player.Inventory.UpdateWeight();
            }

            public static bool Prefix(ItemsCollection __instance) {
                if (!settings.toggleEquipmentNoWeight) return true;

                if (__instance.IsPlayerInventory) {
                    __instance.Weight = 0f;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(BlueprintUnit.UnitBody), nameof(BlueprintUnit.UnitBody.EquipmentWeight), MethodType.Getter)]
        public static class NoWeight_Patch2 {
            public static bool Prefix(ref float __result) {
                if (!settings.toggleEquipmentNoWeight) return true;

                __result = 0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(Unrecruit), nameof(Unrecruit.RunAction))]
        public class Unrecruit_RunAction_Patch {
            public static bool Prefix() => !settings.toggleBlockUnrecruit;
        }

        [HarmonyPatch(typeof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked), nameof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked.CheckCondition))]
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
            public static List<ActivatableAbilityGroup> groups = Enum.GetValues(typeof(ActivatableAbilityGroup)).Cast<ActivatableAbilityGroup>().ToList();
            public static bool Prefix(ActivatableAbilityGroup group, ref int __result) {
                if (settings.toggleAllowAllActivatable && groups.Any(group)) {
                    __result = 99;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.SetIsOn))]
        public static class ActivatableAbility_SetIsOn_Patch {
            public static void Prefix(ref bool value, ActivatableAbility __instance) {
                if (settings.toggleAllowAllActivatable && __instance.Blueprint.Group == ActivatableAbilityGroup.Judgment) {
                    value = true;
                }
            }
        }

        [HarmonyPatch(typeof(RestrictionCanGatherPower), nameof(RestrictionCanGatherPower.IsAvailable))]
        public static class RestrictionCanGatherPower_IsAvailable_Patch {
            public static bool Prefix(ref bool __result) {
                if (!settings.toggleKineticistGatherPower) {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(KineticistAbilityBurnCost), nameof(KineticistAbilityBurnCost.GetTotal))]
        public static class KineticistAbilityBurnCost_GetTotal_Patch {
            public static void Postfix(ref int __result) => __result = Math.Max(0, __result - settings.kineticistBurnReduction);
        }

        [HarmonyPatch(typeof(UnitPartMagus), nameof(UnitPartMagus.IsSpellCombatThisRoundAllowed))]
        public static class UnitPartMagus_IsSpellCombatThisRoundAllowed_Patch {
            public static void Postfix(ref bool __result, UnitPartMagus __instance) {
                if (settings.toggleAlwaysAllowSpellCombat && __instance.Owner != null && __instance.Owner.IsPartyOrPet()) {
                    __result = true;
                }
            }
        }

#if false
        [HarmonyPatch(typeof(IngameMenuManager), nameof(IngameMenuManager.OpenGroupManager))]
        private static class IngameMenuManager_OpenGroupManager_Patch {
            private static bool Prefix(IngameMenuManager __instance) {
                if (settings.toggleInstantPartyChange) {
                    var startChangedPartyOnGlobalMap = __instance.GetType().GetMethod("StartChangedPartyOnGlobalMap", BindingFlags.NonPublic | BindingFlags.Instance);
                    startChangedPartyOnGlobalMap.Invoke(__instance, new object[] { });
                    return false;
                }
                return true;
            }
        }
#endif
        public static class UnitEntityData_CanRollPerception_Extension {
            public static bool TriggerReroll = false;
            public static bool CanRollPerception(UnitEntityData unit) {
                if (TriggerReroll) {
                    return true;
                }

                return unit.HasMotionThisTick;
            }
        }

        [HarmonyPatch(typeof(PartyPerceptionController), nameof(PartyPerceptionController.Tick))]
        public static class PartyPerceptionController_Tick_Patch {
            public static MethodInfo HasMotionThisTick_Method = AccessTools.DeclaredMethod(typeof(UnitEntityData), "get_HasMotionThisTick");
            public static MethodInfo CanRollPerception_Method = AccessTools.DeclaredMethod(typeof(UnitEntityData_CanRollPerception_Extension), "CanRollPerception");

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                foreach (var instr in instructions) {
                    if (instr.Calls(HasMotionThisTick_Method)) {
                        yield return new CodeInstruction(OpCodes.Call, CanRollPerception_Method);
                    }
                    else {
                        yield return instr;
                    }
                }
            }

            private static void Postfix() => UnitEntityData_CanRollPerception_Extension.TriggerReroll = false;
        }

        [HarmonyPatch(typeof(FogOfWarController), "<CollectRevealers>g__CollectUnit|15_0")]
        public static class FogOfWarController_CollectRevealers_CompilerMethod_Patch {
            public static void Prefix(UnitEntityData unit) {
                var revealer = unit.View.SureFogOfWarRevealer();
                if (settings.fowMultiplier != 1) {
                    revealer.DefaultRadius = false;
                    revealer.UseDefaultFowBorder = false;
                    revealer.Radius = FogOfWarController.VisionRadius * settings.fowMultiplier;
                }
                else {
                    revealer.DefaultRadius = true;
                    revealer.UseDefaultFowBorder = true;
                    revealer.Radius = 1.0f;
                }
            }
        }
#endif
    }
}
