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
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.EntitySystem.Stats;
using static Kingmaker.EntitySystem.Stats.ModifiableValue;
using Kingmaker.EntitySystem.Entities;

namespace ToyBox.BagOfPatches {
    internal static class Unrestricted {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;
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

        [HarmonyPatch(typeof(BlueprintAnswerBase))]
        public static class BlueprintAnswerBasePatch {
            [HarmonyPatch(nameof(BlueprintAnswerBase.IsSoulMarkRequirementSatisfied))]
            [HarmonyPostfix]
            public static void IsSoulMarkRequirementSatisfied(BlueprintAnswerBase __instance, ref bool __result) {
                if (settings.toggleDialogRestrictions) {
                    __result = true;
                }
            }

        }

        [HarmonyPatch(typeof(BlueprintAnswer), nameof(BlueprintAnswer.CanSelect))]
        public static class BlueprintAnswer_CanSelect_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleDialogRestrictionsEverything) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Modifier), nameof(Modifier.Stacks), MethodType.Getter)]
        public static class ModifiableValue_UpdateValue_Patch {
            public static bool Prefix(Modifier __instance) {
                if (settings.toggleUnlimitedStatModifierStacking && __instance?.AppliedTo?.Owner is BaseUnitEntity entity && (entity?.IsPartyOrPet() ?? false)) {
                    __instance.StackMode = StackMode.ForceStack;
                }
                return true;
            }
        }
    }
}