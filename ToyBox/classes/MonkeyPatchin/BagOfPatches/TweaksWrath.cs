// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Armies.TacticalCombat.Controllers;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Combat;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Globalmap;
using Kingmaker.Globalmap.State;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.Tutorial;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Group;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.MainMenuUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.Kineticist.ActivatableAbility;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects;
using Kingmaker.Visual.Sound;
using ModKit;
using Owlcat.Runtime.Core.Utils;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ToyBox;
using TurnBased.Controllers;
using TurnBased.Utility;
using UnityEngine;
using static Kingmaker.Utility.MassLootHelper;
using Object = UnityEngine.Object;

namespace ToyBox.BagOfPatches {
    internal static partial class Tweaks {
        public static Settings Settings = Main.Settings;
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

                if (Settings.toggleSpontaneousCopyScrolls && spellbook.Blueprint.Spontaneous && spellListContainsSpell) {
                    __result = true;
                    return;
                }

                __result = spellbook.Blueprint.CanCopyScrolls && spellListContainsSpell;
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindow), nameof(KingdomUIEventWindow.OnClose))]
        public static class KingdomUIEventWindow_OnClose_Patch {
            public static bool Prefix(ref bool __state) {
                __state = Settings.toggleInstantEvent;
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
                if (Settings.toggleInstantEvent) {
                    __result = 0;
                }
            }
        }

        [HarmonyPatch(typeof(KingdomUIEventWindowFooter), nameof(KingdomUIEventWindowFooter.OnStart))]
        public static class KingdomUIEventWindowFooter_OnStart_Patch {
            public static bool Prefix(ref bool __state) {
                __state = Settings.toggleInstantEvent;
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
                if (!Settings.toggleNoFogOfWar) return true;
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
                if (!inCombat && Settings.toggleRestoreSpellsAbilitiesAfterCombat) {
                    var partyMembers = Game.Instance.Player.PartyAndPets;
                    foreach (var u in partyMembers) {
                        foreach (var resource in u.Descriptor.Resources)
                            u.Descriptor.Resources.Restore(resource);
                        foreach (var spellbook in u.Descriptor.Spellbooks)
                            spellbook.Rest();
                        u.Brain.RestoreAvailableActions();
                    }
                }
                if (!inCombat && Settings.toggleRechargeItemsAfterCombat) {

                }
                if (!inCombat && Settings.toggleInstantRestAfterCombat) {
                    CheatsCombat.RestAll();
                }
                if (inCombat && (Settings.toggleEnterCombatAutoRage || Settings.toggleEnterCombatAutoRage)) {
                    foreach (var unit in Game.Instance.Player.Party) {
                        var flag = true;
                        if (Settings.toggleEnterCombatAutoRageDemon) { // we prefer demon rage, as it's more powerful
                            foreach (var ability in unit.Abilities) {
                                if (ability.Blueprint.AssetGuid == rage_demon && ability.Data.IsAvailableForCast) {
                                    Kingmaker.RuleSystem.Rulebook.Trigger(new RuleCastSpell(ability.Data, unit));
                                    ability.Data.Spend();
                                    flag = false; // if demon rage is active, we skip the normal rage checks
                                    break;
                                }
                            }
                        }
                        if (flag && Settings.toggleEnterCombatAutoRage) {
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
                if (Settings.toggleAccessRemoteCharacters) {
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
                if (Settings.toggleMaterialComponent) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintArmorType), nameof(BlueprintArmorType.HasDexterityBonusLimit), MethodType.Getter)]
        public static class BlueprintArmorType_HasDexterityBonusLimit_Patch {
            public static bool Prefix(ref bool __result) {
                if (Settings.toggleIgnoreMaxDexterity) {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BlueprintArmorType), nameof(BlueprintArmorType.ArmorChecksPenalty), MethodType.Getter)]
        public static class BlueprintArmorType_ArmorChecksPenalty_Patch {
            public static bool Prefix(ref int __result) {
                if (Settings.toggleIgnoreArmorChecksPenalty) {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ItemEntityArmor), nameof(ItemEntityArmor.RecalculateStats))]
        public static class ItemEntityArmor_RecalculateStats_Patch {
            public static void Postfix(ItemEntityArmor __instance) {
                if (Settings.toggleIgnoreSpeedReduction) {
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
                if (Settings.toggleIgnoreSpellFailure) {
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
                if (Settings.toggleIgnoreSpellFailure) {
                    __result = 0;
                    return false;
                }
                return true;
            }

            [HarmonyPatch(nameof(RuleCastSpell.ArcaneSpellFailureChance), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool PrefixArcaneSpellFailureChance(ref int __result) {
                if (Settings.toggleIgnoreSpellFailure) {
                    __result = 0;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RuleDrainEnergy), nameof(RuleDrainEnergy.TargetIsImmune), MethodType.Getter)]
        private static class RuleDrainEnergy_Immune_Patch {
            public static void Postfix(RuleDrainEnergy __instance, ref bool __result) {
                if (__instance.Target.Descriptor.IsPartyOrPet() && Settings.togglePartyNegativeLevelImmunity) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(RuleDealStatDamage), nameof(RuleDealStatDamage.Immune), MethodType.Getter)]
        private static class RuleDealStatDamage_Immune_Patch {
            public static void Postfix(RuleDrainEnergy __instance, ref bool __result) {
                if (__instance.Target.Descriptor.IsPartyOrPet() && Settings.togglePartyAbilityDamageImmunity) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch]
        private static class AbilityAlignment_IsRestrictionPassed_Patch {
            [HarmonyPatch(typeof(AbilityCasterAlignment), nameof(AbilityCasterAlignment.IsCasterRestrictionPassed))]
            [HarmonyPostfix]
            public static void PostfixCasterRestriction(ref bool __result) {
                if (Settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = true;
                }
            }

            [HarmonyPatch(typeof(UnitPartForbiddenSpellbooks), nameof(UnitPartForbiddenSpellbooks.IsForbidden))]
            [HarmonyPostfix]
            public static void PostfixForbiddenSpellbookRestriction(ref bool __result) {
                if (Settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = false;
                }
            }

            [HarmonyPatch(typeof(UnitPartForbiddenSpellbooks), nameof(UnitPartForbiddenSpellbooks.Add))]
            [HarmonyPrefix]
            public static bool PrefixForbidSpellbook(ForbidSpellbookReason reason) {
                if (Settings.toggleIgnoreAbilityAlignmentRestriction && reason == ForbidSpellbookReason.Alignment) { // Don't add to forbidden list
                    return false;
                }
                return true;
            }

            [HarmonyPatch(typeof(AbilityTargetAlignment), nameof(AbilityTargetAlignment.IsTargetRestrictionPassed))]
            [HarmonyPostfix]
            public static void PostfixTargetRestriction(ref bool __result) {
                if (Settings.toggleIgnoreAbilityAlignmentRestriction) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch]
        private static class AbilityData_CanBeCastByCaster_Patch {
            [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.CanBeCastByCaster), MethodType.Getter)]
            [HarmonyPrefix]
            public static bool PostfixCasterRestriction(ref bool __result, AbilityData __instance) {
                if (Settings.toggleIgnoreAbilityAnyRestriction && __instance?.Caster?.Unit?.Descriptor?.IsPartyOrPet() == true) {
                    __result = true;
                    return false;
                }
                return true;
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
                if (Settings.toggleAutomaticallyLoadLastSave && Main.freshlyLaunched) {
                    Main.freshlyLaunched = false;

                    var mainMenu = UnityEngine.Object.FindObjectOfType<MainMenu>();
                    mainMenu.EnterGame(new Action(() => {
                        if (Game.Instance?.SaveManager?.GetLatestSave() == null)
                            return;
                        Game.Instance.LoadGame(Game.Instance.SaveManager.GetLatestSave());
                    }));
                }
                Main.freshlyLaunched = false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GameOver))]
        private static class Player_GameOverReason_Patch {
            private static bool Prefix(Player __instance, Player.GameOverReasonType reason) {
                if (!Settings.toggleGameOverFixLeeerrroooooyJenkins || reason != Player.GameOverReasonType.EssentialUnitIsDead) return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Tutorial), nameof(Tutorial.IsBanned))]
        private static class Tutorial_IsBanned_Patch {
            private static bool Prefix(ref Tutorial __instance, ref bool __result) {
                if (Settings.toggleForceTutorialsToHonorSettings) {
                    //                    __result = !__instance.HasTrigger ? __instance.Owner.IsTagBanned(__instance.Blueprint.Tag) : __instance.Banned;
                    __result = __instance.Owner.IsTagBanned(__instance.Blueprint.Tag) || __instance.Banned;
                    //modLogger.Log($"hasTrigger: {__instance.HasTrigger} tag: {__instance.Blueprint.Tag} isTagBanned:{__instance.Owner.IsTagBanned(__instance.Blueprint.Tag)} this.Banned: {__instance.Banned} ==> {__result}");
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(TutorialSystem), nameof(TutorialSystem.Trigger))]
        private static class TutorialSystem_Trigger_Patch {
            [HarmonyPrefix]
            private static bool Trigger() {
                return !Settings.toggleForceDisableTutorials;
            }
        }
        [HarmonyPatch(typeof(ItemsCollection), nameof(ItemsCollection.DeltaWeight))]
        public static class NoWeight_Patch1 {
            public static void Refresh(bool SetEquipmentWeightZero) {
                Game.Instance.Player.Inventory.Weight = 0f;
                if (!SetEquipmentWeightZero)
                    Game.Instance.Player.Inventory.UpdateWeight();
            }

            public static bool Prefix(ItemsCollection __instance) {
                if (!Settings.toggleEquipmentNoWeight) return true;

                if (__instance.IsPlayerInventory) {
                    __instance.Weight = 0f;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(UnitBody), nameof(UnitBody.EquipmentWeight), MethodType.Getter)]
        public static class NoWeight_Patch2 {
            public static bool Prefix(ref float __result) {
                if (!Settings.toggleEquipmentNoWeight) return true;

                __result = 0f;
                return false;
            }
        }
        [HarmonyPatch(typeof(UnitBody), nameof(UnitBody.EquipmentWeightAfterBuff), MethodType.Getter)]
        public static class NoWeight_Patch3 {
            public static bool Prefix(ref float __result) {
                if (!Settings.toggleEquipmentNoWeight) return true;

                __result = 0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.IsUsableFromInventory), MethodType.Getter)]
        public static class ItemEntity_IsUsableFromInventory_Patch {
            // Allow Item Use From Inventory During Combat
            public static bool Prefix(ItemEntity __instance, ref bool __result) {
                if (!Settings.toggleUseItemsDuringCombat) return true;

                var item = __instance.Blueprint as BlueprintItemEquipment;
                __result = item?.Ability != null;
                return false;
            }
        }

        [HarmonyPatch(typeof(Unrecruit), nameof(Unrecruit.RunAction))]
        public class Unrecruit_RunAction_Patch {
            public static bool Prefix() => !Settings.toggleBlockUnrecruit;
        }

        [HarmonyPatch(typeof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked), nameof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked.CheckCondition))]
        public static class RomanceLocked_CheckCondition_Patch {
            public static void Postfix(ref bool __result) {
                if (Settings.toggleMultipleRomance) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(ContextActionReduceBuffDuration), nameof(ContextActionReduceBuffDuration.RunAction))]
        public static class ContextActionReduceBuffDuration_RunAction_Patch {
            public static bool Prefix(ContextActionReduceBuffDuration __instance) {
                if (Settings.toggleExtendHexes && !Game.Instance.Player.IsInCombat
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
                if (Settings.toggleAllowAllActivatable && groups.Any(group)) {
                    __result = 99;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ActivatableAbility), nameof(ActivatableAbility.SetIsOn))]
        public static class ActivatableAbility_SetIsOn_Patch {
            public static void Prefix(ref bool value, ActivatableAbility __instance) {
                if (Settings.toggleAllowAllActivatable && __instance.Blueprint.Group == ActivatableAbilityGroup.Judgment) {
                    value = true;
                }
            }
        }

        [HarmonyPatch(typeof(RestrictionCanGatherPower), nameof(RestrictionCanGatherPower.IsAvailable))]
        public static class RestrictionCanGatherPower_IsAvailable_Patch {
            public static bool Prefix(ref bool __result) {
                if (!Settings.toggleKineticistGatherPower) {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(KineticistAbilityBurnCost), nameof(KineticistAbilityBurnCost.GetTotal))]
        public static class KineticistAbilityBurnCost_GetTotal_Patch {
            public static void Postfix(ref int __result) => __result = Math.Max(0, __result - Settings.kineticistBurnReduction);
        }
        [HarmonyPatch(typeof(UnitPartMagus))]
        public static class UnitPartMagus_Patches {
            [HarmonyPatch(nameof(UnitPartMagus.IsSpellCombatThisRoundAllowed)), HarmonyPostfix]
            public static void IsSpellCombatThisRoundAllowed(ref bool __result, UnitPartMagus __instance) {
                if (Settings.toggleAlwaysAllowSpellCombat && __instance.Owner != null && __instance.Owner.IsPartyOrPet()) {
                    __result = true;
                }
            }
            [HarmonyPatch(nameof(UnitPartMagus.CanUseSpellCombat), MethodType.Getter), HarmonyPostfix]
            public static void CanUseSpellCombat(ref bool __result, UnitPartMagus __instance) {
                if (Settings.toggleAlwaysAllowSpellCombat && __instance.Owner != null && __instance.Owner.IsPartyOrPet()) {
                    __result = true;
                }
            }
            [HarmonyPatch(nameof(UnitPartMagus.IsSpellFromMagusSpellList)), HarmonyFinalizer]
            public static Exception IsSpellFromMagusSpellList(ref bool __result, UnitPartMagus __instance, AbilityData spell) {
                if (Settings.toggleAlwaysAllowSpellCombat && __instance.Owner != null && __instance.Owner.IsPartyOrPet()) {
                    try {
                        bool flag = (bool)__instance.WandWielder && spell.SourceItemUsableBlueprint != null && spell.SourceItemUsableBlueprint?.Type == UsableItemType.Wand;
                        __result = spell.IsInSpellList(__instance.Spellbook?.Blueprint?.SpellList) || (__instance.Spellbook?.IsKnown(spell?.Blueprint) ?? false) || flag;
                    } catch {
                        __result = spell.Range == AbilityRange.Touch && spell.Spellbook != null;
                    }
                }
                return null;
            }
        }
        [HarmonyPatch(typeof(ActivatableAbilityRestrictionByEquipmentSet), nameof(ActivatableAbilityRestrictionByEquipmentSet.IsAvailable))]
        public static class ActivatableAbilityRestrictionByEquipmentSet_IsAvailable_Patch {
            [HarmonyPostfix]
            public static void IsAvailable(ActivatableAbilityRestrictionByEquipmentSet __instance, ref bool __result) {
                const string SpellCombatAbilityGUID = "8898a573e8a8a184b8186dbc3a26da74";
                if (Settings.toggleAlwaysAllowSpellCombat && __instance.Fact.Blueprint.AssetGuid.ToString().ToLower() == SpellCombatAbilityGUID && __instance.Owner.IsPartyOrPet()) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(DeactivateOnGripChanged), nameof(DeactivateOnGripChanged.TryDeactivate))]
        public static class DeactivateOnGripChanged_TryDeactivate {
            [HarmonyPrefix]
            public static bool TryDeactivate(DeactivateOnGripChanged __instance) {
                const string SpellCombatAbilityGUID = "8898a573e8a8a184b8186dbc3a26da74";
                if (Settings.toggleAlwaysAllowSpellCombat && __instance.Fact.Blueprint.AssetGuid.ToString().ToLower() == SpellCombatAbilityGUID && __instance.Owner.IsPartyOrPet()) {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GlobalMapPathManager), nameof(GlobalMapPathManager.GetTimeToCapital))]
        public static class GlobalMapPathManager_GetTimeToCapital_Patch {
            public static void Postfix(bool andBack, ref TimeSpan? __result) {
                if (Settings.toggleInstantChangeParty && andBack && __result != null) {
                    __result = TimeSpan.Zero;
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
                    } else {
                        yield return instr;
                    }
                }
            }

            private static void Postfix() => UnitEntityData_CanRollPerception_Extension.TriggerReroll = false;
        }
        public static void MaybeKill(UnitCombatState unitCombatState) {
            if (Settings.togglekillOnEngage) {
                List<UnitEntityData> partyUnits = Game.Instance.Player.m_PartyAndPets;
                UnitEntityData unit = unitCombatState.Unit;
                if (unit.IsPlayersEnemy && !partyUnits.Contains(unit)) {
                    CheatsCombat.KillUnit(unit);
                }
            }
        }


        [HarmonyPatch(typeof(UnitCombatState), nameof(UnitCombatState.Engage))]
        public static class UnitCombatState_Engage_Patch {
            private static void Postfix(UnitCombatState __instance) {
                MaybeKill(__instance);
            }
        }

        [HarmonyPatch(typeof(UnitCombatState), nameof(UnitCombatState.JoinCombat))]
        public static class UnitCombatState_JoinCombat_Patch {
            private static void Postfix(UnitCombatState __instance) {
                MaybeKill(__instance);
            }
        }

        [HarmonyPatch(typeof(TacticalCombatUnitEngagementController))]
        public static class TacticalCombatUnitEngagementControllerPatch {
            [HarmonyPatch(nameof(TacticalCombatUnitEngagementController.Tick))]
            [HarmonyPostfix]
            public static void Tick(TacticalCombatUnitEngagementController __instance) {
                if (Settings.togglekillOnEngage) {
                    ToyBox.Actions.KillAllTacticalUnits();
                }
            }

        }

        [HarmonyPatch(typeof(AkSoundEngineController), nameof(AkSoundEngineController.OnApplicationFocus))]
        public static class AkSoundEngineController_OnApplicationFocus_Patch {
            private static bool Prefix(AkSoundEngineController __instance) {
                if (Settings.toggleContinueAudioOnLostFocus) {
                    return false;
                } else {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(SoundState), nameof(SoundState.OnApplicationFocusChanged))]
        public static class SoundState_OnApplicationFocusChanged_Patch {
            private static bool Prefix(SoundState __instance) {
                if (Settings.toggleContinueAudioOnLostFocus) {
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(FogOfWarController), "<CollectRevealers>g__CollectUnit|15_0")]
        public static class FogOfWarController_CollectRevealers_CompilerMethod_Patch {
            public static void Prefix(UnitEntityData unit) {
                var revealer = unit.View.SureFogOfWarRevealer();
                if (Settings.fowMultiplier != 1) {
                    revealer.DefaultRadius = false;
                    revealer.UseDefaultFowBorder = false;
                    revealer.Radius = FogOfWarController.VisionRadius * Settings.fowMultiplier;
                } else {
                    revealer.DefaultRadius = true;
                    revealer.UseDefaultFowBorder = true;
                    revealer.Radius = 1.0f;
                }
            }
        }
        [HarmonyPatch(typeof(EnduringSpells), nameof(EnduringSpells.HandleBuffDidAdded))]
        public static class EnduringSpells_HandleBuffDidAdded_Patch {
            private static void Postfix(EnduringSpells __instance, Buff buff) {
                if (buff.TimeLeft >= 23.Hours()) {
                    return;
                }
                AbilityExecutionContext sourceAbilityContext = buff.Context.SourceAbilityContext;
                AbilityData abilityData = ((sourceAbilityContext != null) ? sourceAbilityContext.Ability : null);
                if (abilityData == null || abilityData.Spellbook == null || abilityData.SourceItem != null) {
                    return;
                }
                bool hasGreater = __instance.Owner.HasFact(__instance.Greater);
                MechanicsContext maybeContext = buff.MaybeContext;
                if (((maybeContext != null) ? maybeContext.MaybeCaster : null) == __instance.Owner
                    && (buff.TimeLeft >= Settings.enduringSpellsTimeThreshold.Minutes()
                    || (buff.TimeLeft >= Settings.greaterEnduringSpellsTimeThreshold.Minutes() && __instance.Owner.HasFact(__instance.Greater)))
                    && buff.TimeLeft <= 24.Hours()) {
                    buff.SetEndTime(24.Hours() + buff.AttachTime);
                }
            }
        }
    }
}
