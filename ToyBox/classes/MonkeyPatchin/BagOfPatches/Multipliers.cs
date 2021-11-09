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

namespace ToyBox.BagOfPatches {
    internal static class Multipliers {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(EncumbranceHelper), "GetHeavy")]
        private static class EncumbranceHelper_GetHeavy_Patch {
            private static void Postfix(ref int __result) => __result = Mathf.RoundToInt(__result * settings.encumberanceMultiplier);
        }

        [HarmonyPatch(typeof(EncumbranceHelper), "GetPartyCarryingCapacity", new Type[] { })]
        private static class EncumbranceHelper_GetPartyCarryingCapacity_Patch_1 {
            private static void Postfix(ref EncumbranceHelper.CarryingCapacity __result) {
                __result.Light = Mathf.RoundToInt(__result.Light * settings.encumberanceMultiplierPartyOnly);
                __result.Medium = Mathf.RoundToInt(__result.Medium * settings.encumberanceMultiplierPartyOnly);
                __result.Heavy = Mathf.RoundToInt(__result.Heavy * settings.encumberanceMultiplierPartyOnly);
            }
        }

        [HarmonyPatch(typeof(EncumbranceHelper), "GetPartyCarryingCapacity", new Type[] { typeof(IEnumerable<UnitReference>) })]
        private static class EncumbranceHelper_GetPartyCarryingCapacity_Patch_2 {
            private static void Postfix(ref EncumbranceHelper.CarryingCapacity __result) {
                __result.Light = Mathf.RoundToInt(__result.Light * settings.encumberanceMultiplierPartyOnly);
                __result.Medium = Mathf.RoundToInt(__result.Medium * settings.encumberanceMultiplierPartyOnly);
                __result.Heavy = Mathf.RoundToInt(__result.Heavy * settings.encumberanceMultiplierPartyOnly);
            }
        }

        [HarmonyPatch(typeof(UnitPartWeariness), "GetFatigueHoursModifier")]
        private static class EncumbranceHelper_GetFatigueHoursModifier_Patch {
            private static void Postfix(ref float __result) => __result *= (float)Math.Round(settings.fatigueHoursModifierMultiplier, 1);
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

        [HarmonyPatch(typeof(Spellbook), "GetSpellSlotsCount")]
        public static class BlueprintSpellsTable_GetCount_Patch {
            private static void Postfix(ref int __result, Spellbook __instance, int spellLevel) {
                if (__result > 0 && __instance.Blueprint.IsArcanist) {
                    var spellsKnown = __instance.m_KnownSpells[spellLevel].Count;
                    __result = Math.Min(Mathf.RoundToInt(__result * settings.arcanistSpellslotMultiplier), spellsKnown);
                }
            }
        }

        [HarmonyPatch(typeof(Spellbook), "GetSpellsPerDay")]
        private static class Spellbook_GetSpellsPerDay_Patch {
            private static void Postfix(ref int __result) => __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.spellsPerDayMultiplier, 1));
        }

        [HarmonyPatch(typeof(Player), "GetCustomCompanionCost")]
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
        [HarmonyPatch(typeof(Buff), "AddBuff")]
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


        private static readonly string[] badBuffs = new string[] {
            "24cf3deb078d3df4d92ba24b176bda97", //Prone
            "e6f2fc5d73d88064583cb828801212f4" //Fatigued
        };

        private static bool isGoodBuff(BlueprintBuff blueprint) => !blueprint.Harmful && !badBuffs.Contains(blueprint.AssetGuidThreadSafe);

        [HarmonyPatch(typeof(BuffCollection), "AddBuff", new Type[] {
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
                            var adjusted = Math.Max(0, Math.Min((float)long.MaxValue, duration.Value.Ticks * settings.buffDurationMultiplierValue));
                            duration = TimeSpan.FromTicks(Convert.ToInt64(adjusted));
                        }
                    }
                }
                catch (Exception e) {
                    Mod.Error(e);
                }

                //Mod.Debug("Initiator: " + caster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }

        [HarmonyPatch(typeof(BuffCollection), "AddBuff", new Type[] {
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
                            adjusted = Math.Max(0, Math.Min((float)long.MaxValue, duration.Value.Ticks * settings.buffDurationMultiplierValue));
                            duration = TimeSpan.FromTicks(Convert.ToInt64(adjusted));
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

        [HarmonyPatch(typeof(ItemEntity), "AddEnchantment", new Type[] {
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

        [HarmonyPatch(typeof(DifficultyPresetsList), "GetAdjustmentPreset")]
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

        [HarmonyPatch(typeof(VendorLogic), "GetItemSellPrice", new Type[] { typeof(ItemEntity) })]
        private static class VendorLogic_GetItemSellPrice_Patch {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorSellPriceMultiplier);
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemSellPrice", new Type[] { typeof(BlueprintItem) })]
        private static class VendorLogic_GetItemSellPrice_Patch2 {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorSellPriceMultiplier);
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemBuyPrice", new Type[] { typeof(ItemEntity) })]
        private static class VendorLogic_GetItemBuyPrice_Patch {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorBuyPriceMultiplier);
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemBuyPrice", new Type[] { typeof(BlueprintItem) })]
        private static class VendorLogic_GetItemBuyPrice_Patc2h {
            private static void Postfix(ref long __result) => __result = (long)(__result * settings.vendorBuyPriceMultiplier);
        }

        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.Update))]
        static class CameraRig_Update_Patch {
            static void Postfix(CameraRig __instance, ref Vector3 ___m_TargetPosition) {
                if (settings.toggleRotateOnAllMaps || Main.resetExtraCameraAngles)
                    __instance.TickRotate();
                if (settings.toggleZoomOnAllMaps)
                    __instance.CameraZoom.TickZoom();
                if (settings.toggleScrollOnAllMaps) {
                    __instance.TickScroll();
                    //__instance.TickCameraDrag();
                    //__instance.CameraDragToMove();
                }
            }
        }

        //[HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickScroll))]
        //private static class CameraRig_TickScroll_Patch {
        //    public static void Postfix(CameraRig __instance) {
        //    }
        //}

        [HarmonyPatch(typeof(CameraZoom), nameof(CameraZoom.TickZoom))]
        private static class CameraZoom_TickZoom {
            private static bool firstCall = true;
            private static readonly float BaseFovMin = 17.5f;
            private static readonly float BaseFovMax = 30;

            public static bool Prefix(CameraZoom __instance) {
                if (settings.fovMultiplier == 1) return true;
                if (firstCall) {
                    //Main.Log($"baseMin/Max: {__instance.FovMin} {__instance.FovMax}");
                    if (__instance.FovMin != BaseFovMin) {
                        Mod.Warn($"Warning: game has changed FovMin to {__instance.FovMin} vs {BaseFovMin}. Toy Box should be updated to avoid stability issues when enabling and disabling the mod repeatedly".orange().bold());
                        //BaseFovMin = __instance.FovMin;
                    }

                    if (__instance.FovMax != BaseFovMax) {
                        Mod.Warn($"Warning: game has changed FovMax to {__instance.FovMax} vs {BaseFovMax}. Toy Box should be updated to avoid stability issues when enabling and disabling the mod repeatedly".orange().bold());
                        //BaseFovMax = __instance.FovMax;
                    }

                    firstCall = false;
                }

                __instance.FovMax = BaseFovMax * settings.fovMultiplier;
                __instance.FovMin = BaseFovMin / settings.fovMultiplier;
                if (__instance.m_ZoomRoutine != null)
                    return true;
                if (!__instance.IsScrollBusy && Game.Instance.IsControllerMouse)
                    __instance.m_PlayerScrollPosition += __instance.IsOutOfScreen ? 0.0f : Input.GetAxis("Mouse ScrollWheel");
                __instance.m_ScrollPosition = __instance.m_PlayerScrollPosition;
                __instance.m_ScrollPosition = Mathf.Clamp(__instance.m_ScrollPosition, 0.0f, __instance.m_ZoomLenght);
                __instance.m_SmoothScrollPosition = Mathf.Lerp(__instance.m_SmoothScrollPosition, __instance.m_ScrollPosition, Time.unscaledDeltaTime * __instance.m_Smooth);
                __instance.m_Camera.fieldOfView = Mathf.Lerp(__instance.FovMax, __instance.FovMin, __instance.CurrentNormalizePosition);
                __instance.m_PlayerScrollPosition = __instance.m_ScrollPosition;
                return true;
            }
        }

        private static Vector2 CameraDragToRotate2D(this CameraRig __instance) {
            if (!__instance.m_BaseMousePoint.HasValue)
                return new Vector2(0.0f, 0.0f);
            var basePoint = __instance.m_BaseMousePoint.Value;
            var dx = basePoint.x - __instance.GetLocalPointerPosition().x;
            var dy = basePoint.y - __instance.GetLocalPointerPosition().y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            var rotateDistance = ((double)__instance.m_RotateDistance - (double)dist) * ((bool)(SimpleBlueprint)BlueprintRoot.Instance ? (double)SettingsRoot.Controls.CameraRotationSpeedEdge.GetValue() : 2.0);
            __instance.m_RotateDistance = (float)rotateDistance;
            basePoint.x -= 0.25f * dx;
            basePoint.y -= 0.25f * dy;
            __instance.m_BaseMousePoint = basePoint;
            return new Vector2(dx, -dy);
        }

        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.TickRotate))]
        private static class CameraRig_TickRotate_Patch {

            public static bool Prefix(CameraRig __instance) {
                if (!settings.toggleRotateOnAllMaps && !settings.toggleCameraPitch && !Main.resetExtraCameraAngles) return true;
                bool usePitch = settings.toggleCameraPitch;
                if (__instance.m_RotateRoutine != null && (double)Time.time > (double)__instance.m_RotateRoutineEndsOn) {
                    __instance.StopCoroutine(__instance.m_RotateRoutine);
                    __instance.m_RotateRoutine = (Coroutine)null;
                }

                if (__instance.m_ScrollRoutine != null || __instance.m_RotateRoutine != null)// || __instance.m_HandRotationLock)
                    return false;
                __instance.RotateByMiddleButton();
                var mouseMovement = new Vector2(0,0);
                if (__instance.m_RotationByMouse) {
                    mouseMovement = __instance.CameraDragToRotate2D();
                }
                else if (__instance.m_RotationByKeyboard)
                    mouseMovement.x = __instance.m_RotateOffset;
                if (__instance.m_RotationByMouse || __instance.m_RotationByKeyboard || Main.resetExtraCameraAngles) {
                    var eulerAngles = __instance.transform.rotation.eulerAngles;
                    eulerAngles.y += mouseMovement.x * __instance.m_RotationSpeed * CameraRig.ConsoleRotationMod;
                    if (usePitch && !Main.resetExtraCameraAngles) {
                        eulerAngles.x += mouseMovement.y * __instance.m_RotationSpeed * CameraRig.ConsoleRotationMod;
                        //Mod.Debug($"eulerX: {eulerAngles.x}");
                    }
                    else {
                        eulerAngles.x = 0f;
                        Main.resetExtraCameraAngles = false;
                    }
                    __instance.transform.DOKill();
                    __instance.transform.DOLocalRotate(eulerAngles, __instance.m_RotationTime).SetUpdate<Tweener>(true);
                }

                __instance.m_RotationByKeyboard = false;
                __instance.m_RotateOffset = 0.0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(CameraRig), nameof(CameraRig.SetMode))]
        private static class CameraRig_SetMode_Apply {
            public static void Postfix(CameraRig __instance, CameraMode mode) {
                if (settings.fovMultiplierCutScenes == 1 && settings.fovMultiplier == 1) return;
                if (mode == CameraMode.Default && Game.Instance.CurrentMode == GameModeType.Cutscene) {
                    __instance.Camera.fieldOfView = __instance.CameraZoom.FovMax * settings.fovMultiplierCutScenes / settings.fovMultiplier;
                }
            }
        }

#if false
        [HarmonyPatch(typeof(BlueprintArea), nameof(BlueprintArea.CameraMode), MethodType.Getter)]
        static class BlueprintArea_CameraMode_Patch {
            public static void Postfix(BlueprintArea __instance, CameraMode __result) {
                Main.Log("hi");
                __result = CameraMode.Default;
            }
        }
#endif
    }
}
