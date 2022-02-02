// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Blueprints;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using Kingmaker.UI.ActionBar;
using TurnBased.Controllers;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;
using ModKit;
using Kingmaker.View;
using Kingmaker.UI.Common;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.UnitSettings;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.EntitySystem.Persistence;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionHandEquip;
using Kingmaker.UnitLogic.Abilities.Blueprints;

namespace ToyBox.BagOfPatches {
    internal static class Summons {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;
        private static bool SummonedByPlayerFaction = false;

        // prefill actionbar slots for controlled summon with spell-like abilities and charge ability
        [HarmonyPatch(typeof(ActionBarVM), nameof(ActionBarVM.SetMechanicSlots))]
        private static class ActionBarVM_SetMechanicSlots_Patch {
            private static bool Prefix(ActionBarVM __instance, UnitEntityData unit) {
                if (settings.toggleMakeSummmonsControllable && !LoadingProcess.Instance.IsLoadingInProcess && unit != null && unit.IsSummoned()) {
                    if (unit.UISettings.GetSlot(0, unit) is MechanicActionBarSlotEmpty) {
                        var index = 1;
                        foreach (var ability in unit.Abilities) {
                            if (ability.Blueprint.AssetGuidThreadSafe == "c78506dd0e14f7c45a599990e4e65038") { //Setting charge ability to first slot
                                unit.UISettings.SetSlot(unit, ability, 0);
                            }
                            else if (index < __instance.Slots.Count && ability.Blueprint.Type != AbilityType.CombatManeuver && ability.Blueprint.Type != AbilityType.Physical) {
                                unit.UISettings.SetSlot(unit, ability, index++);
                            }
                        }
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIUtility), nameof(UIUtility.GetGroup))]
        private static class UIUtility_GetGroup_Patch {
            private static void Postfix(ref List<UnitEntityData> __result) {
                if (settings.toggleMakeSummmonsControllable) {
                    try {
                        __result.AddRange(Game.Instance.Player.Group.Select(u => u).Where(u => u.IsSummoned()));
                    }
                    catch {}
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.MoveCharacters))]
        private static class Player_MoveCharacters_Patch {
            private static void Postfix() {
                if (settings.toggleMakeSummmonsControllable) {
                    foreach (var unit in Game.Instance.Player.Group) {
                        if (unit.IsSummoned()) {
                            var view = unit.View;
                            if (view != null) {
                                view.StopMoving();
                            }
                            unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                            unit.DesiredOrientation = Game.Instance.Player.MainCharacter.Value.Orientation;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SummonPool), nameof(SummonPool.Register))]
        private static class SummonPool_Register_Patch {
            private static void Postfix(ref UnitEntityData unit) {
                //if (settings.toggleSetSpeedOnSummon) {
                //    unit.Descriptor.Stats.GetStat(StatType.Speed).BaseValue = settings.setSpeedOnSummonValue;
                //}

                if (settings.toggleMakeSummmonsControllable && SummonedByPlayerFaction) {
                    // Main.Log($"SummonPool.Register: Unit [{unit.CharacterName}] [{unit.UniqueId}]");
                    UnitEntityDataUtils.Charm(unit);
                    //unit.Ensure<UnitPartFollowUnit>().Init(Game.Instance.Player.MainCharacter.Value, true, false);
#if false
                    if (unit.Blueprint.AssetGuid == "6fdf7a3f850a1eb48bfbf44d9d0f45dd" && StringUtils.ToToggleBool(settings.toggleDisableWarpaintedSkullAbilityForSummonedBarbarians)) // WarpaintedSkullSummonedBarbarians
                    {
                        if (unit.Body.Head.HasItem && unit.Body.Head.Item?.Blueprint?.AssetGuid == "5d343648bb8887d42b24cbadfeb36991") // WarpaintedSkullItem
                        {
                            unit.Body.Head.Item.Ability.Deactivate();
                            Common.ModLoggerDebug(unit.Body.Head.Item.Name + "'s ability active: " + unit.Body.Head.Item.Ability.Active);
                        }
                    }
#endif
                    SummonedByPlayerFaction = false;
                }

#if false
                if (StringUtils.ToToggleBool(settings.toggleRemoveSummonsGlow)) {
                    unit.Buffs.RemoveFact(Utilities.GetBlueprintByGuid<BlueprintFact>("706c182e86d9be848b59ddccca73d13e")); // SummonedCreatureVisual
                    unit.Buffs.RemoveFact(Utilities.GetBlueprintByGuid<BlueprintFact>("e4b996b5168fe284ab3141a91895d7ea")); // NaturalAllyCreatureVisual
                }
#endif
            }
        }

        [HarmonyPatch(typeof(RuleSummonUnit), MethodType.Constructor, new Type[] {
            typeof(UnitEntityData),
            typeof(BlueprintUnit),
            typeof(Vector3),
            typeof(Rounds),
            typeof(int) }
        )]
        public static class RuleSummonUnit_Constructor_Patch {
            public static void Prefix(UnitEntityData initiator, BlueprintUnit blueprint, Vector3 position, ref Rounds duration, ref int level, RuleSummonUnit __instance) {
                Mod.Debug($"old duration: {duration} level: {level} \n mult: {settings.summonDurationMultiplier1} levelInc: {settings.summonLevelModifier1}\n initiatior: {initiator} tweakTarget: {settings.summonTweakTarget1} shouldTweak: {UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.summonTweakTarget1)}");
                if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.summonTweakTarget1)) {
                    if (settings.summonDurationMultiplier1 != 1) {
                        duration = new Rounds(Convert.ToInt32(duration.Value * settings.summonDurationMultiplier1));
                    }
                    if (settings.summonLevelModifier1 != 0) {
                        level = Math.Max(0, Math.Min(level + (int)settings.summonLevelModifier1, 20));
                    }
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.summonTweakTarget2)) {
                    if (settings.summonDurationMultiplier2 != 1) {
                        duration = new Rounds(Convert.ToInt32(duration.Value * settings.summonDurationMultiplier2));
                    }
                    if (settings.summonLevelModifier2 >= 0) {
                        level = Math.Max(0, Math.Min(level + (int)settings.summonLevelModifier1, 20));
                    }
                }
                Mod.Debug($"new duration: {duration} level: {level}");

                if (settings.toggleMakeSummmonsControllable) {
                    SummonedByPlayerFaction = initiator.IsPlayerFaction;
                }
                Mod.Debug("Initiator: " + initiator.CharacterName + $"(PlayerFaction : {initiator.IsPlayerFaction})" + "\nBlueprint: " + blueprint.CharacterName + "\nDuration: " + duration.Value);
            }
        }

        [HarmonyPatch(typeof(ActionBarManager), nameof(ActionBarManager.CheckTurnPanelView))]
        internal static class ActionBarManager_CheckTurnPanelView_Patch {
            private static void Postfix(ActionBarManager __instance) {
                if (settings.toggleMakeSummmonsControllable && CombatController.IsInTurnBasedCombat()) {
                    Traverse.Create((object)__instance).Method("ShowTurnPanel", Array.Empty<object>()).GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), nameof(UnitEntityData.IsDirectlyControllable), MethodType.Getter)]
        public static class UnitEntityData_IsDirectlyControllable_Patch {
            public static void Postfix(UnitEntityData __instance, ref bool __result) {
                if (settings.toggleMakeSummmonsControllable && __instance.Descriptor.IsPartyOrPet() && !__result && __instance.Get<UnitPartSummonedMonster>() != null && !__instance.Descriptor.State.IsFinallyDead && !__instance.Descriptor.State.IsPanicked && !__instance.IsDetached && !__instance.PreventDirectControl) {
                    __result = true;
                }
            }
        }
    }
}
