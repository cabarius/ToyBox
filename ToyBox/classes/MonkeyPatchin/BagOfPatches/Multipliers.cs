// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.View;
using System;
using System.Linq;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.Settings;
using Kingmaker.Settings.Difficulty;
using ModKit;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Utility;
using System.Collections.Generic;
using CameraMode = Kingmaker.View.CameraMode;
using DG.Tweening;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.QA.Statistics;

namespace ToyBox.BagOfPatches {
    internal static class Multipliers {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(EncumbranceHelper), nameof(EncumbranceHelper.GetHeavy))]
        private static class EncumbranceHelper_GetHeavy_Patch {
            private static void Postfix(ref int __result) => __result = Mathf.RoundToInt(__result * settings.encumberanceMultiplier);
        }

        [HarmonyPatch(typeof(EncumbranceHelper), nameof(EncumbranceHelper.GetPartyCarryingCapacity), new Type[] { })]
        private static class EncumbranceHelper_GetPartyCarryingCapacity_Patch_1 {
            private static void Postfix(ref EncumbranceHelper.CarryingCapacity __result) {
                __result.Light = Mathf.RoundToInt(__result.Light * settings.encumberanceMultiplierPartyOnly);
                __result.Medium = Mathf.RoundToInt(__result.Medium * settings.encumberanceMultiplierPartyOnly);
                __result.Heavy = Mathf.RoundToInt(__result.Heavy * settings.encumberanceMultiplierPartyOnly);
            }
        }

        [HarmonyPatch(typeof(EncumbranceHelper), nameof(EncumbranceHelper.GetPartyCarryingCapacity), new Type[] { typeof(IEnumerable<UnitReference>) })]
        private static class EncumbranceHelper_GetPartyCarryingCapacity_Patch_2 {
            private static void Postfix(ref EncumbranceHelper.CarryingCapacity __result) {
                __result.Light = Mathf.RoundToInt(__result.Light * settings.encumberanceMultiplierPartyOnly);
                __result.Medium = Mathf.RoundToInt(__result.Medium * settings.encumberanceMultiplierPartyOnly);
                __result.Heavy = Mathf.RoundToInt(__result.Heavy * settings.encumberanceMultiplierPartyOnly);
            }
        }

        [HarmonyPatch(typeof(UnitPartWeariness), nameof(UnitPartWeariness.GetFatigueHoursModifier))]
        private static class EncumbranceHelper_GetFatigueHoursModifier_Patch {
            private static void Postfix(ref float __result) => __result *= (float)Math.Round(settings.fatigueHoursModifierMultiplier, 1);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GainPartyExperience))]
        public static class Player_GainPartyExperience_Patch {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref int gained, ExperienceGainStatistic.GainType statType) {
                bool useNormal = true;
                switch (statType) {
                    case ExperienceGainStatistic.GainType.Mob: {
                            if (settings.useCombatExpSlider) {
                                gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplierCombat, 1));
                                useNormal = false;
                            }
                        }; break;
                    case ExperienceGainStatistic.GainType.Check: {
                            if (settings.useSkillChecksExpSlider) {
                                gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplierSkillChecks, 1));
                                useNormal = false;
                            }
                        }; break;
                    case ExperienceGainStatistic.GainType.Quest: {
                            if (settings.useQuestsExpSlider) {
                                gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplierQuests, 1));
                                useNormal = false;
                            }
                        }; break;
                    case ExperienceGainStatistic.GainType.Trap: {
                            if (settings.useTrapsExpSlider) {
                                gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplierTraps, 1));
                                useNormal = false;
                            }
                        }; break;
                }
                if (useNormal) {
                    gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplier, 1));
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GainMoney))]
        public static class Player_GainMoney_Patch {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref long amount) {
                amount = Mathf.RoundToInt(amount * (float)Math.Round(settings.moneyMultiplier, 1));
                return true;
            }
        }

        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetSpellSlotsCount))]
        public static class BlueprintSpellsTable_GetCount_Patch {
            private static void Postfix(ref int __result, Spellbook __instance, int spellLevel) {
                if (__result > 0 && __instance.Blueprint.IsArcanist) {
                    var spellsKnown = __instance.m_KnownSpells[spellLevel].Count;
                    __result = Math.Min(Mathf.RoundToInt(__result * settings.arcanistSpellslotMultiplier), spellsKnown);
                }
            }
        }

        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetSpellsPerDay))]
        private static class Spellbook_GetSpellsPerDay_Patch {
            private static void Postfix(ref int __result, Spellbook __instance) {
                if (__instance.Blueprint.MemorizeSpells && !__instance.Blueprint.IsArcanist) { // prepapred spellcaster slots multiplier
                    __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.memorizedSpellsMultiplier, 1));
                    return;
                }

                // Spontaneous multiplier
                __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.spellsPerDayMultiplier, 1));
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetCustomCompanionCost))]
        public static class Player_GetCustomCompanionCost_Patch {
            public static bool Prefix(ref bool __state) => !__state; // FIXME - why did Bag of Tricks do this?

            public static void Postfix(ref int __result) => __result = Mathf.RoundToInt(__result * settings.companionCostMultiplier);
        }

        /**
        public Buff AddBuff(
          BlueprintBuff blueprint,
          UnitEntityData caster,
          TimeSpan? duration,
          [CanBeNull] AbilityParams abilityParams = null) {
            MechanicsContext context = new MechanicsContext(caster, this.Owner, (SimpleBlueprint)blueprint);
            if (abilityParams != null)
                context.SetParams(abilityParams);
            return this.Manager.Add<Buff>(new Buff(blueprint, context, duration));
        }
        */
#if false
        [HarmonyPatch(typeof(Buff), nameof(Buff.AddBuff))]
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
                    Mod.Error(e);
                }

                Mod.Debug("Initiator: " + caster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }
#endif


        private static readonly HashSet<string> badBuffs = settings.buffsToIgnoreForDurationMultiplier;

        private static bool isGoodBuff(BlueprintBuff blueprint) => !blueprint.Harmful && !badBuffs.Contains(blueprint.AssetGuidThreadSafe);

        [HarmonyPatch(typeof(BuffCollection), nameof(BuffCollection.AddBuff), new Type[] {
            typeof(BlueprintBuff),
            typeof(UnitEntityData),
            typeof(TimeSpan?),
            typeof(AbilityParams)
        })]
        public static class BuffCollection_AddBuff_patch {
            public static void Prefix(BlueprintBuff blueprint, UnitEntityData caster, ref TimeSpan? duration, [CanBeNull] AbilityParams abilityParams = null) {
                try {
                    if (!caster.IsPlayersEnemy && isGoodBuff(blueprint)) {
                        if (duration != null) {
                            duration = GetNewBuffDuration((TimeSpan)duration);
                        }
                    }
                }
                catch (Exception e) {
                    Mod.Error(e);
                }

                //Mod.Debug("Initiator: " + caster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }

        [HarmonyPatch(typeof(BuffCollection), nameof(BuffCollection.AddBuff), new Type[] {
            typeof(BlueprintBuff),
            typeof(MechanicsContext),
            typeof(TimeSpan?)
        })]
        public static class BuffCollection_AddBuff2_patch {
            public static void Prefix(BlueprintBuff blueprint, MechanicsContext parentContext, ref TimeSpan? duration) {
                float adjusted = 0;
                try {
                    if (!parentContext.MaybeCaster.IsPlayersEnemy && isGoodBuff(blueprint)) {
                        if (duration != null) {
                            var oldDuration = duration;
                            duration = GetNewBuffDuration((TimeSpan)duration);
                            Mod.Warn($"BuffCollection_AddBuff2_patch - buff: {blueprint.name} duration: {oldDuration} => {duration} - ticks: {duration.Value.Ticks} * {settings.buffDurationMultiplierValue}");
                        }
                    }
                }
                catch (Exception e) {
                    Mod.Error($"BuffCollection_AddBuff2_patch - duration: {duration} - ticks: {duration.Value.Ticks} * {settings.buffDurationMultiplierValue} => {adjusted}");
                    Mod.Error(e);
                }

                //Mod.Debug("Initiator: " + parentContext.MaybeCaster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }

        private static TimeSpan GetNewBuffDuration(TimeSpan originalDuration) {
            // deal with large value edge cases, assume that any value over half of Max Ticks divided by our multiplier should be left alone
            if (originalDuration == TimeSpan.MaxValue 
                || (double)originalDuration.Ticks > (((double)TimeSpan.MaxValue.Ticks) / (2.0f * (double)settings.buffDurationMultiplierValue))
                )
                return originalDuration;
            // Ok we have a duration we can actually modify without overflow
            var ticks = originalDuration.Ticks;
            var adjusted = (long)(ticks * settings.buffDurationMultiplierValue);
            Mod.Log($"originalDur: {originalDuration} ticks: {ticks} adjusted:{adjusted}");
            adjusted = Math.Max(0, adjusted);
            return TimeSpan.FromTicks(Convert.ToInt64(adjusted));
        }

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.AddEnchantment), new Type[] {
            typeof(BlueprintItemEnchantment),
            typeof(MechanicsContext),
            typeof(Rounds?)
        })]
        public static class ItemEntity_AddEnchantment_Patch {
            public static void Prefix(BlueprintBuff blueprint, MechanicsContext parentContext, ref Rounds? duration) {
                try {
                    if (!parentContext?.MaybeCaster?.IsPlayersEnemy ?? false && isGoodBuff(blueprint)) {
                        if (duration != null) {
                            duration = new Rounds((int)(duration.Value.Value * settings.buffDurationMultiplierValue));
                        }
                    }
                }
                catch (Exception e) {
                    Mod.Error(e);
                }
            }
        }

        [HarmonyPatch(typeof(DifficultyPresetsList), nameof(DifficultyPresetsList.GetAdjustmentPreset))]
        public static class DifficultyPresetList_EnemyHpMultiplier_Patch {
            public static void Postfix(ref DifficultyPresetsList.StatsAdjustmentPreset __result, StatsAdjustmentsType preset) {
                var hp = preset switch {
                    StatsAdjustmentsType.ExtraDecline => 0.4f,
                    StatsAdjustmentsType.StrongDecline => 0.6f,
                    StatsAdjustmentsType.Decline => 0.8f,
                    _ => 1f
                };

                __result.HPMultiplier = hp * settings.enemyBaseHitPointsMultiplier;

                if (settings.toggleBrutalUnfair) {
                    __result.BasicStatBonusMultiplier = Mathf.RoundToInt(2 * (1 + settings.brutalDifficultyMultiplier));
                    __result.DerivativeStatBonusMultiplier = Mathf.RoundToInt(2 * (1 + settings.brutalDifficultyMultiplier));
                    //__result.HPMultiplier = Mathf.RoundToInt(__result.HPMultiplier * settings.brutalDifficultyMultiplier);
                    __result.AbilityDCBonus = Mathf.RoundToInt(2 * (1 + settings.brutalDifficultyMultiplier));
                    __result.SkillCheckDCBonus = Mathf.RoundToInt(2 * (1 + settings.brutalDifficultyMultiplier));
                }
            }
        }

        [HarmonyPatch(typeof(VendorLogic), nameof(VendorLogic.GetItemSellPrice), new Type[] { typeof(ItemEntity) })]
        private static class VendorLogic_GetItemSellPrice_Patch {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorSellPriceMultiplier);
        }

        [HarmonyPatch(typeof(VendorLogic), nameof(VendorLogic.GetItemSellPrice), new Type[] { typeof(BlueprintItem) })]
        private static class VendorLogic_GetItemSellPrice_Patch2 {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorSellPriceMultiplier);
        }

        [HarmonyPatch(typeof(VendorLogic), nameof(VendorLogic.GetItemBuyPrice), new Type[] { typeof(ItemEntity) })]
        private static class VendorLogic_GetItemBuyPrice_Patch {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorBuyPriceMultiplier);
        }

        [HarmonyPatch(typeof(VendorLogic), nameof(VendorLogic.GetItemBuyPrice), new Type[] { typeof(BlueprintItem) })]
        private static class VendorLogic_GetItemBuyPrice_Patc2h {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorBuyPriceMultiplier);
        }
    }
}
