// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Items;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using ModKit;
using Kingmaker.ElementsSystem;
#if Wrath
using Kingmaker.Kingdom.Settlements;
#endif
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.EntitySystem.Stats;
using static Kingmaker.EntitySystem.Stats.ModifiableValue;

namespace ToyBox.BagOfPatches {
    internal static class Unrestricted {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;
#if Wrath
        [HarmonyPatch(typeof(EquipmentRestrictionAlignment), nameof(EquipmentRestrictionAlignment.CanBeEquippedBy))]
        public static class EquipmentRestrictionAlignment_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
#endif
        [HarmonyPatch(typeof(EquipmentRestrictionClass), nameof(EquipmentRestrictionClass.CanBeEquippedBy))]
        public static class EquipmentRestrictionClassNew_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(EquipmentRestrictionStat), nameof(EquipmentRestrictionStat.CanBeEquippedBy))]
        public static class EquipmentRestrictionStat_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityArmor), nameof(ItemEntityArmor.CanBeEquippedInternal))]
        public static class ItemEntityArmor_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityArmor __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    //Mod.Debug($"armor blueprint: {__instance?.Blueprint} - type:{__instance.Blueprint?.GetType().Name}");
                    if (__instance.Blueprint is BlueprintItemEquipment blueprint) {
                        __result = blueprint.CanBeEquippedBy(owner);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityShield), nameof(ItemEntityShield.CanBeEquippedInternal))]
        public static class ItemEntityShield_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityShield __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    if (__instance.Blueprint is BlueprintItemEquipment blueprint) {
                        __result = blueprint != null && blueprint.CanBeEquippedBy(owner);
                    }
                }
            }
        }
#if true
        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.CanBeEquippedInternal))]
        public static class ItemEntity_CanBeEquippedInternal_Patch {
            [HarmonyPostfix]
            public static void Postfix(ItemEntityWeapon __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    //Mod.Debug($"item: {__instance}");
                    __result = true;
#if false           // TODO: the following was old code that would crash Wrath app and RT doesn't like it either. Why was this ever here?"
                    var blueprint = __instance.Blueprint;
                    Mod.Debug($"blueprint: {blueprint} - type:{blueprint?.GetType().Name}");
                    return;

                    if (__instance.Blueprint is BlueprintItemEquipment blueprintItemEquipment) {
                        __result = blueprintItemEquipment != null;
                    }
#endif
                }
            }
        }
#endif
#if Wrath        
        internal static readonly Dictionary<string, bool> PlayerAlignmentIsOverrides = new() {
            { "fdc9eb3b03cf8ef4ca6132a04970fb41", false },  // DracoshaIntro_MythicAzata_dialog - Cue_0031
        };

        [HarmonyPatch(typeof(PlayerAlignmentIs), nameof(PlayerAlignmentIs.CheckCondition))]
        public static class PlayerAlignmentIs_CheckCondition_Patch {
            public static void Postfix(PlayerAlignmentIs __instance, ref bool __result) {
                if (!settings.toggleDialogRestrictions || __instance?.Owner is null) return;
                Mod.Debug($"checking {__instance} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (PlayerAlignmentIsOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
                else __result = true;
            }
        }
#endif

        [HarmonyPatch(typeof(BlueprintAnswerBase))]
        public static class BlueprintAnswerBasePatch {
#if Wrath
            [HarmonyPatch(nameof(BlueprintAnswerBase.IsAlignmentRequirementSatisfied), MethodType.Getter)]
            [HarmonyPostfix]
            public static void IsAlignmentRequirementSatisfied(BlueprintAnswerBase __instance, ref bool __result) {
                if (settings.toggleDialogRestrictions) {
                    __result = true;
                }
            }

            [HarmonyPatch(nameof(BlueprintAnswerBase.IsMythicRequirementSatisfied), MethodType.Getter)]
            [HarmonyPostfix]
            public static void IsMythicRequirementSatisfied(ref bool __result) {
                if (settings.toggleDialogRestrictionsMythic) {
                    __result = true;
                }
            }
#elif RT
            [HarmonyPatch(nameof(BlueprintAnswerBase.IsSoulMarkRequirementSatisfied))]
            [HarmonyPostfix]
            public static void IsSoulMarkRequirementSatisfied(BlueprintAnswerBase __instance, ref bool __result) {
                if (settings.toggleDialogRestrictions) {
                    __result = true;
                }
            }
#endif

        }

        [HarmonyPatch(typeof(BlueprintAnswer), nameof(BlueprintAnswer.CanSelect))]
        public static class BlueprintAnswer_CanSelect_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleDialogRestrictionsEverything) {
                    __result = true;
                }
            }
        }
#if Wrath        
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.CasterLevel), MethodType.Getter)]
        public static class Spellbook_CasterLevel_Patch {
            public static void Postfix(ref int __result, Spellbook __instance) {
                if (settings.toggleUncappedCasterLevel) {
                    __result = Math.Max(0, __instance.m_BaseLevelInternal + __instance.Blueprint.CasterLevelModifier) + __instance.m_MythicLevelInternal;
                }
            }
        }
#endif
        
        [HarmonyPatch(typeof(Modifier), nameof(Modifier.Stacks), MethodType.Getter)]
        public static class ModifiableValue_UpdateValue_Patch {
            public static bool Prefix(Modifier __instance) {
                if (settings.toggleUnlimitedStatModifierStacking) {
                    __instance.StackMode = StackMode.ForceStack;
                }
                return true;
            }
        }
    }
}