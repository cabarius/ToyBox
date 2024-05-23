// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using DG.Tweening;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Cargo;
using Kingmaker.Cheats;
using Kingmaker.Code.UI.MVVM.View.LoadingScreen;
using Kingmaker.Code.UI.MVVM.View.MainMenu.PC;
using Kingmaker.Code.UI.MVVM.VM.MessageBox;
using Kingmaker.Code.UI.MVVM.VM.ServiceWindows.Inventory;
using Kingmaker.Code.UI.MVVM.VM.ShipCustomization;
using Kingmaker.Code.UI.MVVM.VM.Slots;
using Kingmaker.Code.UI.MVVM.VM.WarningNotification;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameCommands;
using Kingmaker.Items;
using Kingmaker.Items.Slots;
using Kingmaker.Networking;
using Kingmaker.Pathfinding;
using Kingmaker.PubSubSystem;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.Sound;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.Utility;
using Kingmaker.View.Covers;
using ModKit;
using Owlcat.Runtime.Core.Utility;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Warhammer.SpaceCombat.Blueprints;
using static Kingmaker.UnitLogic.Abilities.AbilityData;

namespace ToyBox.BagOfPatches {
    internal static partial class Tweaks {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(TurnController))]
        private static class TurnController_Patch {
            [HarmonyPatch(nameof(TurnController.IsPlayerTurn), MethodType.Getter)]
            [HarmonyPostfix]
            private static void IsPlayerTurn(TurnController __instance, ref bool __result) {
                if (__instance.CurrentUnit == null) return;
                if (Main.Settings.perSave.doOverrideEnableAiForCompanions.TryGetValue(__instance.CurrentUnit.HashKey(), out var maybeOverride)) {
                    if (maybeOverride.Item1) {
                        __result = !maybeOverride.Item2;
                    }
                }
            }
            [HarmonyPatch(nameof(TurnController.IsAiTurn), MethodType.Getter)]
            [HarmonyPostfix]
            private static void IsAiTurn(TurnController __instance, ref bool __result) {
                if (__instance.CurrentUnit == null) return;
                if (Main.Settings.perSave.doOverrideEnableAiForCompanions.TryGetValue(__instance.CurrentUnit.HashKey(), out var maybeOverride)) {
                    if (maybeOverride.Item1) {
                        __result = maybeOverride.Item2;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(PartUnitCombatState))]
        private static class PartUnitCombatStatePatch {
            public static void MaybeKill(PartUnitCombatState unitCombatState) {
                if (Settings.togglekillOnEngage) {
                    List<BaseUnitEntity> partyUnits = Game.Instance.Player.m_PartyAndPets;
                    BaseUnitEntity unit = unitCombatState.Owner;
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

        [HarmonyPatch(typeof(GameHistoryLog), nameof(GameHistoryLog.HandlePartyCombatStateChanged))]
        private static class GameHistoryLogHandlePartyCombatStateChangedPatch {
            private static void Postfix(ref bool inCombat) {
                if (!inCombat && Settings.toggleRestoreSpellsAbilitiesAfterCombat) {
                    var partyMembers = Game.Instance.Player.PartyAndPets;
                    foreach (var u in partyMembers) {
                        foreach (var resource in u.AbilityResources)
                            u.AbilityResources.Restore(resource);
                        u.Brain.RestoreAvailableActions();
                    }
                }
                if (!inCombat && Settings.toggleInstantRestAfterCombat) {
                    CheatsCombat.RestAll();
                }
            }
        }
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
                              typeof(UnavailabilityReasonType?),
                              typeof(int?)
                          },
                          new ArgumentType[] {
                              ArgumentType.Normal,
                              ArgumentType.Normal,
                              ArgumentType.Normal,
                              ArgumentType.Out,
                              ArgumentType.Out,
                              ArgumentType.Out,
                              ArgumentType.Normal
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
            [HarmonyPatch(nameof(LoadingScreenBaseView.ShowUserInputWaiting))]
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
        public static class UnitEntityDataCanRollPerceptionExtension {
            public static bool TriggerReroll = false;
            public static bool CanRollPerception(BaseUnitEntity unit) {
                if (TriggerReroll) {
                    return true;
                }

                return unit.MovementAgent.Position.To2D() != unit.MovementAgent.m_PreviousPosition;
            }
        }
#if true // TODO: these don't work by themselves so figure out what to do
        [HarmonyPatch(typeof(InventoryHelper))]
        public static class InventoryHelperPatch {
            [HarmonyPatch(nameof(InventoryHelper.CanChangeEquipment))]
            [HarmonyPrefix]
            public static bool CanChangeEquipment(BaseUnitEntity unit, ref bool __result) {
                if (!Settings.toggleEquipItemsDuringCombat) return true;
                __result = true;
                return false;
            }

            [HarmonyPatch(nameof(InventoryHelper.CanEquipItem))]
            [HarmonyPrefix]
            public static bool CanEquipItem(ItemEntity item, BaseUnitEntity unit, ref bool __result) {
                if (!Settings.toggleEquipItemsDuringCombat) return true;
                __result = InventoryHelper.CanEquipItemInCombat(item);
                return false;
            }

            [HarmonyPatch(nameof(InventoryHelper.TryDrop), new Type[] { typeof(ItemEntity) })]
            [HarmonyPrefix]
            public static bool TryDrop(ItemEntity item) {
                if (!Settings.toggleEquipItemsDuringCombat) return true;
                if (UIUtility.IsGlobalMap()) {
                    InventoryHelper.s_Item = item;
                    Action<IDropItemHandler> someAction = null;
                    InventoryHelper.s_ItemCallback = delegate {
                        Action<IDropItemHandler> action;
                        if ((action = someAction) == null) {
                            action = (someAction = delegate (IDropItemHandler h) {
                                h.HandleDropItem(item, false);
                            });
                        }
                        EventBus.RaiseEvent<IDropItemHandler>(action, true);
                    };
                    UIUtility.ShowMessageBox(UIStrings.Instance.CommonTexts.DropItemFromGlobalMap, DialogMessageBoxBase.BoxType.Dialog, delegate (DialogMessageBoxBase.BoxButton button) {
                        if (button == DialogMessageBoxBase.BoxButton.Yes) {
                            ItemSlot itemSlot = InventoryHelper.s_ItemSlot;
                        }
                    }, null, null, null, 0);
                    return false;
                }
                InventoryHelper.DropItemMechanic(item, false);
                return false;
            }

            [HarmonyPatch(nameof(InventoryHelper.TryMoveSlotInInventory))]
            [HarmonyPrefix]
            public static bool TryMoveSlotInInventory(ItemSlotVM from, ItemSlotVM to) {
                if (!Settings.toggleEquipItemsDuringCombat) return true;

                ISlotsGroupVM group = from.Group;
                CargoEntity cargoEntity = ((group != null) ? group.MechanicCollection.Owner : null) as CargoEntity;
                ISlotsGroupVM group2 = to.Group;
                CargoEntity cargoEntity2 = ((group2 != null) ? group2.MechanicCollection.Owner : null) as CargoEntity;
                if ((from.Group != to.Group && cargoEntity != null) || cargoEntity2 != null) {
                    int num;
                    if (cargoEntity != null && !cargoEntity.CanTransferFromCargo(from.ItemEntity)) {
                        if (cargoEntity.Blueprint.Integral) {
                            PFLog.Default.Log("Cannot transfer items from this cargo cause Integral true");
                            return false;
                        }
                        if (CargoHelper.IsTrashItem(from.ItemEntity)) {
                            EventBus.RaiseEvent<IWarningNotificationUIHandler>(delegate (IWarningNotificationUIHandler h) {
                                h.HandleWarning(UIStrings.Instance.CargoTexts.TrashItemCargo.Text, false, WarningNotificationFormat.Short);
                            }, true);
                            PFLog.Default.Log(string.Format("Cannot transfer items from this cargo cause {0} is trash item", from.ItemEntity));
                            return false;
                        }
                        PFLog.Default.Log("Cannot transfer items from this cargo cause CanRemoveItems false");
                        return false;
                    }
                    else if (cargoEntity2 != null && !cargoEntity2.CanAdd(from.ItemEntity, out num)) {
                        if (cargoEntity2.Blueprint.Integral) {
                            PFLog.Default.Log("Cannot add to cargo cause Integral true");
                        }
                        else if (!cargoEntity2.CorrectOrigin(from.ItemEntity.Blueprint.Origin)) {
                            PFLog.Default.Log("Cannot add to cargo cause item origin not correct");
                        }
                        else {
                            PFLog.Default.Log("Cannot add to cargo cause is is full");
                        }
                        UISounds.Instance.Sounds.Combat.CombatGridCantPerformActionClick.Play(null);
                        return false;
                    }
                }
                if (from.Group != to.Group) {
                    EquipSlotVM equipSlotVM = to as EquipSlotVM;
                    if (equipSlotVM != null) {
                        BaseUnitEntity baseUnitEntity = equipSlotVM.ItemSlot.Owner as BaseUnitEntity;
                        if (baseUnitEntity != null && baseUnitEntity.CanBeControlled()) {
                            Game.Instance.GameCommandQueue.EquipItem(from.Item.Value, equipSlotVM.ItemSlot.Owner, equipSlotVM.ToSlotRef());
                        }
                        return false;
                    }
                }
                if (from.Group != to.Group) {
                    ShipComponentSlotVM shipComponentSlotVM = to as ShipComponentSlotVM;
                    if (shipComponentSlotVM != null) {
                        Game.Instance.GameCommandQueue.EquipItem(from.Item.Value, shipComponentSlotVM.ItemSlot.Owner, shipComponentSlotVM.ToSlotRef());
                        return false;
                    }
                }
                if (from.Group != to.Group) {
                    ReactiveProperty<ItemEntity> item = to.Item;
                    object obj;
                    if (item == null) {
                        obj = null;
                    }
                    else {
                        ItemEntity value = item.Value;
                        obj = ((value != null) ? value.Blueprint : null);
                    }
                    if (obj as BlueprintStarshipItem) {
                        GameCommandQueue gameCommandQueue = Game.Instance.GameCommandQueue;
                        ReactiveProperty<ItemEntity> item2 = to.Item;
                        ItemEntity itemEntity = ((item2 != null) ? item2.Value : null);
                        ItemEntity itemEntity2 = from.ItemEntity;
                        gameCommandQueue.EquipItem(itemEntity, (itemEntity2 != null) ? itemEntity2.Owner : null, from.ToSlotRef());
                        return false;
                    }
                }
                if (from.Group != to.Group) {
                    EquipSlotVM equipSlotVM2 = from as EquipSlotVM;
                    if (equipSlotVM2 != null) {
                        BaseUnitEntity baseUnitEntity2 = equipSlotVM2.ItemSlot.Owner as BaseUnitEntity;
                        if (baseUnitEntity2 != null && baseUnitEntity2.CanBeControlled()) {
                            Game.Instance.GameCommandQueue.UnequipItem(baseUnitEntity2, equipSlotVM2.ToSlotRef(), to.ToSlotRef());
                        }
                        return false;
                    }
                }
                if (from.Group != to.Group) {
                    ShipComponentSlotVM shipComponentSlotVM2 = from as ShipComponentSlotVM;
                    if (shipComponentSlotVM2 != null) {
                        Game.Instance.GameCommandQueue.UnequipItem(shipComponentSlotVM2.ItemSlot.Owner, shipComponentSlotVM2.ToSlotRef(), to.ToSlotRef());
                        return false;
                    }
                }
                bool flag = from.Item != null && from.Item.Value.Origin == ItemsItemOrigin.ShipComponents;
                bool flag2 = cargoEntity != null || cargoEntity2 != null;
                InventoryHelper.ProcessDragEnd(from, to, flag2, flag);
                return false;
            }
        }

        [HarmonyPatch(typeof(ItemSlot))]
        public static class ItemSlotPatch {
            [HarmonyPatch(nameof(ItemSlot.IsPossibleInsertItems))]
            [HarmonyPrefix]
            public static bool IsPossibleInsertItems(ItemSlot __instance, ref bool __result) {
                if (!Settings.toggleEquipItemsDuringCombat) return true;
                __result = !(bool)__instance.Lock
                           || (bool)(Kingmaker.ElementsSystem.ContextData.ContextData<ItemSlot.IgnoreLock>)Kingmaker.ElementsSystem.ContextData.ContextData<ItemSlot.IgnoreLock>.Current
                           || __instance.IsBodyInitializing;
                Mod.Log($"ItemSlot.IsPossibleInsertItems: {__result}");

                return false;
            }
        }

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.IsUsableFromInventory), MethodType.Getter)]
        public static class ItemEntityIsUsableFromInventoryPatch {
            // Allow Item Use From Inventory During Combat
            public static bool Prefix(ItemEntity __instance, ref bool __result) {
                if (!Settings.toggleUseItemsDuringCombat) return true;
                return __instance.Blueprint is not BlueprintItemEquipmentUsable;
            }
        }
#endif

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
            private static void Tick() => UnitEntityDataCanRollPerceptionExtension.TriggerReroll = false;
        }
    }
}
