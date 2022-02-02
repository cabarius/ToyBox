// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Cheats;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.Controllers.Rest;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
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
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.Tutorial;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.Kineticist.ActivatableAbility;
using Kingmaker.UI.IngameMenu;
using System.Reflection;
using System.Reflection.Emit;
using Kingmaker.View.MapObjects;
using Owlcat.Runtime.Core.Utils;
using Kingmaker.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using System.Linq;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.RuleSystem.Rules.Abilities;
using UnityEngine;
using Kingmaker.EntitySystem.Stats;
using ModKit;
using Kingmaker.Controllers.Combat;

namespace ToyBox.BagOfPatches {
    internal static class Tweaks {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;
        private static readonly BlueprintGuid rage_barbarian = BlueprintGuid.Parse("df6a2cce8e3a9bd4592fb1968b83f730");
        private static readonly BlueprintGuid rage_blood = BlueprintGuid.Parse("e3a0056eedac7754ca9a50603ba05177");
        private static readonly BlueprintGuid rage_focused = BlueprintGuid.Parse("eccb3f963b3f425dac1f5f384927c3cc");
        private static readonly BlueprintGuid rage_demon = BlueprintGuid.Parse("260daa5144194a8ab5117ff568b680f5");

        //     private static bool CanCopySpell([NotNull] BlueprintAbility spell, [NotNull] Spellbook spellbook) => spellbook.Blueprint.CanCopyScrolls && !spellbook.IsKnown(spell) && spellbook.Blueprint.SpellList.Contains(spell);

        [HarmonyPatch(typeof(CopyScroll), nameof(CopyScroll.CanCopySpell))]
        [HarmonyPatch(new Type[] { typeof(BlueprintAbility), typeof(Spellbook) })]
        public static class CopyScroll_CanCopySpell_Patch {
            private static bool Prefix() => false;

            private static void Postfix([NotNull] BlueprintAbility spell, [NotNull] Spellbook spellbook, ref bool __result) {
                if (spellbook.IsKnown(spell)) {
                    __result = false;
                    return;
                }
                var spellListContainsSpell = spellbook.Blueprint.SpellList.Contains(spell);

                if (settings.toggleSpontaneousCopyScrolls && spellbook.Blueprint.Spontaneous && spellListContainsSpell) {
                    __result = true;
                    return;
                }

                __result = spellbook.Blueprint.CanCopyScrolls && spellListContainsSpell;
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindow), nameof(KingdomUIEventWindow.OnClose))]
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

                        var inProgress = ___m_KingdomEventView.IsInProgress;
                        var leader = ___m_KingdomEventView.m_Event.AssociatedTask?.AssignedLeader;

                        if (!inProgress || leader == null) {
                            return;
                        }

                        ___m_KingdomEventView.Event.Resolve(___m_KingdomEventView.Task);

                        if (___m_KingdomEventView.RulerTimeRequired <= 0) {
                            return;
                        }

                        foreach (var unitEntityData in player.AllCharacters) {
                            RestController.ApplyRest(unitEntityData.Descriptor);
                        }

                        new KingdomTimelineManager().MaybeUpdateTimeline();
                    }
                }
            }
        }
        [HarmonyPatch(typeof(KingdomTaskEvent), nameof(KingdomTaskEvent.SkipPlayerTime), MethodType.Getter)]
        public static class KingdomTaskEvent_SkipPlayerTime_Patch {
            public static void Postfix(ref int __result) {
                if (settings.toggleInstantEvent) {
                    __result = 0;
                }
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindowFooter), nameof(KingdomUIEventWindowFooter.OnStart))]
        public static class KingdomUIEventWindowFooter_OnStart_Patch {
            public static bool Prefix(ref bool __state) {
                __state = settings.toggleInstantEvent;
                return !__state;
            }

            public static void Postfix(KingdomEventUIView ___m_KingdomEventView, bool __state) {
                if (__state) {
                    EventBus.RaiseEvent((IKingdomUIStartSpendTimeEvent h) => h.OnStartSpendTimeEvent(___m_KingdomEventView.Blueprint));
                    var kingdomTaskEvent = ___m_KingdomEventView?.Task;
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
                    foreach (var unitEntityData in player.AllCharacters) {
                        RestController.ApplyRest(unitEntityData.Descriptor);
                    }
                    new KingdomTimelineManager().MaybeUpdateTimeline();
                }
            }
        }

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

        [HarmonyPatch(typeof(GameHistoryLog), nameof(GameHistoryLog.HandlePartyCombatStateChanged))]
        private static class GameHistoryLog_HandlePartyCombatStateChanged_Patch {
            private static void Postfix(ref bool inCombat) {
                if (!inCombat && settings.toggleRestoreSpellsAbilitiesAfterCombat) {
                    var partyMembers = Game.Instance.Player.PartyAndPets;
                    foreach (var u in partyMembers) {
                        foreach (var resource in u.Descriptor.Resources)
                            u.Descriptor.Resources.Restore(resource);
                        foreach (var spellbook in u.Descriptor.Spellbooks)
                            spellbook.Rest();
                        u.Brain.RestoreAvailableActions();
                    }
                }
                if (!inCombat && settings.toggleRechargeItemsAfterCombat) {

                }
                if (!inCombat && settings.toggleInstantRestAfterCombat) {
                    CheatsCombat.RestAll();
                }
                if (inCombat && (settings.toggleEnterCombatAutoRage || settings.toggleEnterCombatAutoRage)) {
                    foreach (var unit in Game.Instance.Player.Party) {
                        var flag = true;
                        if (settings.toggleEnterCombatAutoRageDemon) { // we prefer demon rage, as it's more powerful
                            foreach (var ability in unit.Abilities) {
                                if (ability.Blueprint.AssetGuid == rage_demon && ability.Data.IsAvailableForCast) {
                                    Kingmaker.RuleSystem.Rulebook.Trigger(new RuleCastSpell(ability.Data, unit));
                                    ability.Data.Spend();
                                    flag = false; // if demon rage is active, we skip the normal rage checks
                                    break;
                                }
                            }
                        }
                        if (flag && settings.toggleEnterCombatAutoRage) {
                            foreach (var activatable in unit.ActivatableAbilities) {
                                if (activatable.Blueprint.AssetGuid == rage_barbarian
                                    || activatable.Blueprint.AssetGuid == rage_blood
                                    || activatable.Blueprint.AssetGuid == rage_focused) {
                                    activatable.IsOn = true;
                                    break;
                                }
                            }
                        }
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

        [HarmonyPatch(typeof(GroupController), nameof(GroupController.WithRemote), MethodType.Getter)]
        private static class GroupController_WithRemote_Patch {
            private static void Postfix(GroupController __instance, ref bool __result) {
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

        [HarmonyPatch]
        private static class AbilityAlignment_IsRestrictionPassed_Patch {
            [HarmonyPatch(typeof(AbilityCasterAlignment), nameof(AbilityCasterAlignment.IsCasterRestrictionPassed))]
            [HarmonyPostfix]
            public static void PostfixCasterRestriction(ref bool __result) {
                if (settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = true;
                }
            }

            [HarmonyPatch(typeof(AbilityTargetAlignment), nameof(AbilityTargetAlignment.IsTargetRestrictionPassed))]
            [HarmonyPostfix]
            public static void PostfixTargetRestriction(ref bool __result) {
                if (settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(MainMenuBoard), nameof(MainMenuBoard.Update))]
        private static class MainMenuButtons_UpdatePatch {
            private static void Postfix() {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                    Main.freshlyLaunched = false;
                    Mod.Warn("Auto Load Save on Launch disabled");
                    return;
                }
                if (settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;
                    var mainMenuVM = Game.Instance.RootUiContext.MainMenuVM;
                    mainMenuVM?.EnterGame(new Action(mainMenuVM.LoadLastSave));
                }
                Main.freshlyLaunched = false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GameOver))]
        private static class Player_GameOverReason_Patch {
            private static bool Prefix(Player __instance, Player.GameOverReasonType reason) {
                if (!settings.toggleGameOverFixLeeerrroooooyJenkins || reason  != Player.GameOverReasonType.EssentialUnitIsDead) return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Tutorial), nameof(Tutorial.IsBanned))]
        private static class Tutorial_IsBanned_Patch {
            private static bool Prefix(ref Tutorial __instance, ref bool __result) {
                if (settings.toggleForceTutorialsToHonorSettings) {
                    //                    __result = !__instance.HasTrigger ? __instance.Owner.IsTagBanned(__instance.Blueprint.Tag) : __instance.Banned;
                    __result = __instance.Owner.IsTagBanned(__instance.Blueprint.Tag) || __instance.Banned;
                    //modLogger.Log($"hasTrigger: {__instance.HasTrigger} tag: {__instance.Blueprint.Tag} isTagBanned:{__instance.Owner.IsTagBanned(__instance.Blueprint.Tag)} this.Banned: {__instance.Banned} ==> {__result}");
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(MassLootHelper), nameof(MassLootHelper.GetMassLootFromCurrentArea))]
        public static class PatchLootEverythingOnLeave_Patch {
            public static bool Prefix(ref IEnumerable<LootWrapper> __result) {
                if (!settings.toggleMassLootEverything) return true;

                var all_units = Game.Instance.State.Units.All.Where(w => w.IsInGame);
                var result_units = all_units.Where(unit => unit.HasLoot).Select(unit => new LootWrapper { Unit = unit }); //unit.IsRevealed && unit.IsDeadAndHasLoot

                var all_entities = Game.Instance.State.Entities.All.Where(w => w.IsInGame);
                var all_chests = all_entities.Select(s => s.Get<InteractionLootPart>()).Where(i => i?.Loot != Game.Instance.Player.SharedStash).NotNull();

                var tmp = TempList.Get<InteractionLootPart>();

                foreach (var i in all_chests) {
                    //if (i.Owner.IsRevealed
                    //    && i.Loot.HasLoot
                    //    && (i.LootViewed
                    //        || (i.View is DroppedLoot && !i.Owner.Get<DroppedLoot.EntityPartBreathOfMoney>())
                    //        || i.View.GetComponent<SkinnedMeshRenderer>()))
                    if (i.Loot.HasLoot) {
                        tmp.Add(i);
                    }
                }

                var result_chests = tmp.Distinct(new MassLootHelper.LootDuplicateCheck()).Select(i => new LootWrapper { InteractionLoot = i });

                __result = result_units.Concat(result_chests);
#if false   
                foreach (var loot in __result) // showing inventories from living enemies makes the items invisible (also they can still be looted with the Get All option)
                {
                    if (loot.Unit != null)
                    ;
                    if (loot.InteractionLoot != null)
                    ;
                }
#endif
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

        [HarmonyPatch(typeof(UnitBody), nameof(UnitBody.EquipmentWeight), MethodType.Getter)]
        public static class NoWeight_Patch2 {
            public static bool Prefix(ref float __result) {
                if (!settings.toggleEquipmentNoWeight) return true;

                __result = 0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.IsUsableFromInventory), MethodType.Getter)]
        public static class ItemEntity_IsUsableFromInventory_Patch {
            // Allow Item Use From Inventory During Combat
            public static bool Prefix(ItemEntity __instance, ref bool __result) {
                if (!settings.toggleUseItemsDuringCombat) return true;

                var item = __instance.Blueprint as BlueprintItemEquipment;
                __result = item?.Ability != null;
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

        [HarmonyPatch(typeof(GlobalMapPathManager), nameof(GlobalMapPathManager.GetTimeToCapital))]
        public static class GlobalMapPathManager_GetTimeToCapital_Patch {
            public static void Postfix(bool andBack, ref TimeSpan? __result) {
                if (settings.toggleInstantChangeParty && andBack && __result != null) {
                    __result = TimeSpan.Zero;
                }
            }
        }

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
    }
}
