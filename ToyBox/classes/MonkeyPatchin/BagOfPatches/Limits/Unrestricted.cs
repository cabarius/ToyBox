// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Items;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.UnitLogic;
using System;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Unrestricted {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(EquipmentRestrictionAlignment), "CanBeEquippedBy")]
        public static class EquipmentRestrictionAlignment_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(EquipmentRestrictionClass), "CanBeEquippedBy")]
        public static class EquipmentRestrictionClassNew_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(EquipmentRestrictionStat), "CanBeEquippedBy")]
        public static class EquipmentRestrictionStat_CanBeEquippedBy_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityArmor), "CanBeEquippedInternal")]
        public static class ItemEntityArmor_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityArmor __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    var blueprint = __instance.Blueprint as BlueprintItemEquipment;
                    __result = blueprint != null && blueprint.CanBeEquippedBy(owner);
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityShield), "CanBeEquippedInternal")]
        public static class ItemEntityShield_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityShield __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    var blueprint = __instance.Blueprint as BlueprintItemEquipment;
                    __result = blueprint != null && blueprint.CanBeEquippedBy(owner);
                }
            }
        }
        [HarmonyPatch(typeof(ItemEntityWeapon), "CanBeEquippedInternal")]
        public static class ItemEntityWeapon_CanBeEquippedInternal_Patch {
            public static void Postfix(ItemEntityWeapon __instance, UnitDescriptor owner, ref bool __result) {
                if (settings.toggleEquipmentRestrictions) {
                    var blueprint = __instance.Blueprint as BlueprintItemEquipment;
                    __result = blueprint != null && blueprint.CanBeEquippedBy(owner);
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintAnswerBase), nameof(BlueprintAnswerBase.IsAlignmentRequirementSatisfied), MethodType.Getter)]
        public static class BlueprintAnswerBase_IsAlignmentRequirementSatisfied_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleDialogRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintAnswerBase), nameof(BlueprintAnswerBase.IsMythicRequirementSatisfied), MethodType.Getter)]
        public static class BlueprintAnswerBase_IsMythicRequirementSatisfied_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleDialogRestrictionsMythic) {
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

        [HarmonyPatch(typeof(BlueprintSettlementBuilding), "CheckRestrictions", new Type[] { typeof(SettlementState) })]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch1 {
            public static void Postfix(ref bool __result) {
                if (settings.toggleSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintSettlementBuilding), "CheckRestrictions", new Type[] { typeof(SettlementState), typeof(SettlementGridTopology.Slot) })]
        public static class BlueprintSettlementBuilding_CheckRestrictions_Patch2 {
            public static void Postfix(ref bool __result) {
                if (settings.toggleSettlementRestrictions) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.CasterLevel), MethodType.Getter)]
        public static class Spellbook_CasterLevel_Patch {
            public static void Postfix(ref int __result, Spellbook __instance) {
                if (settings.toggleUncappedCasterLevel) {
                    __result += __instance.m_BaseLevelInternal - __instance.BaseLevel - __instance.Blueprint.CasterLevelModifier;
                }
            }
        }
    }
}