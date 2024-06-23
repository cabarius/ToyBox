using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using Kingmaker.ElementsSystem.Interfaces;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace ToyBox.BagOfPatches {
    internal static class Romance {
        public static Settings settings = Main.Settings;

        // Any Gender Any Romance Overrides
        // These modify the PcFemale/PcMale conditions for specific Owner blueprints 
        internal static readonly Dictionary<string, bool> PcFemaleOverrides = new() {
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_0004
            { "5457755c30ac417d9279fd740b90f549", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_0023
            { "8d6b7c53af134494a64a4de789759fb9", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_11
            { "4b4b769261f04a8cb5726e111c3f7081", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Answer_2
            { "cf1d7205cf854709b038db477db48ac9", true },
            // World\Dialogs\Companions\Romances\Heinrix\StartingEvent\Check_0011
            { "d2c500fbc1b5450c8663d453a33b0eee", true },
            // Dialog Blueprints which contain the PcMale override but seem not directly related to romance
            // World\Dialogs\Ch1\BridgeAndCabinet\Briefing\Answer_15
            { "02e0bc30b5a146708dd62d68ac7490bd", true },
            // World\Dialogs\Companions\CompanionDialogues\Interrogator\Cue_10
            { "2df6bd21ad5a45a9b1c5142d51d647dc", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_45
            { "0739ef639d774629a27d396cd733cfd4", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_67
            { "ea42722c44c84835b7363db2fc09b23b", true },
            // World\Dialogs\Companions\CompanionDialogues\Ulfar\Cue_47
            { "41897fd7a52249d3a53691fbcfcc9c19", true },
            // World\Dialogs\Companions\CompanionDialogues\Ulfar\Cue_89
            { "c5efaa0ace544ca7a81d439e7cfc6ae5", true }
        };
        internal static readonly Dictionary<string, bool> PcMaleOverrides = new() {
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_0017
            { "85b651edb4f74381bbe762999273c6ec", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_10
            { "56bbf1612e05489ba44bb4a52718e222", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_5
            { "eb76f93740824d16b1e1f54b82de21e0", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\Answer_8
            { "c292b399f4344a639ccb4df9ba66329e", true },
            // World\Dialogs\Companions\Romances\Cassia\StartingEvent\CassFirstTimeBlushing_a
            { "95b0ba7d08e34f6c895b2fbeb53ea404", true },
            // Dialog Blueprints which contain the PcMale override but seem not directly related to romance
            // Dialogs\Companions\CompanionQuests\Navigator\Navigator_Q1\CassiaSeriousTalk\Answer_8
            { "966f0cc2defa42bd836950aa1ebcde72", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_24
            { "a903589840ba4ab683d6e6b9f985d458", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_11
            { "c051d0c9f2ba4c23bff1d1e6f2cfe13d", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_12
            { "3d24df76aacf4e2db047cf47ef3474d5", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_19
            { "b3601cd9e84d43dbb4078bf77c89d728", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Answer_6
            { "17b34e1ae36443408805af3a3c2866f7", true },
            // World\Dialogs\Ch3\Chasm\PitCassia\Cue_29
            { "7f71e0b93dd9420d87151fc3e7114865", true },
            // World\Dialogs\Companions\CompanionDialogues\Navigator\Cue_47
            { "588a3c2e96c6403ca2c7104949b066e4", true },
            // World\Dialogs\Companions\CompanionQuests\Navigator\Navigator_Q2\Cassia_Q2_BE\Cue_0037
            { "bf7813b4ee3d49cdbc6305f454479db3", true }
        };

        // Path Romance Overrides
        // These modify the EtudeStatus condition for specific Owner blueprints 
        internal static readonly Dictionary<(string, string), bool> ConditionCheckOverridesLoveIsFree = new() {
            #region Wrath
            /*
            // Lich Path
            { ("7f532b681d64f3741a7aa0aebba7c4db", "977f3380-2938-4cc8-a26a-448edc6f9259"), false },  // Etude CamelliaRomance_Start status is:  Not Playing;   
            { ("7f532b681d64f3741a7aa0aebba7c4db", "52632774-cbb4-4eea-ada6-37ec2708e07d"), false },  // Etude WenduagRomance_Active status is:  Not Playing;   
            { ("7f532b681d64f3741a7aa0aebba7c4db", "12928be5-97e8-4e7c-ac5c-02d704289e7f"), false },  // Etude LannRomance_Active status is:  Not Playing;      
            { ("7f532b681d64f3741a7aa0aebba7c4db", "2bbad7a7-5918-4c14-b909-f1a7bbce9248"), false },  // Etude ArueshalaeRomance_Active status is:  Not Playing;   
            { ("7f532b681d64f3741a7aa0aebba7c4db", "55fd2fa2-9644-462a-a02e-23987e05fd62"), false },  // Etude DaeranRomance_Active status is:  Not Playing;      
            { ("7f532b681d64f3741a7aa0aebba7c4db", "b0f684c1-0cc9-4ec7-a4a3-fe47e3d9847c"), false },  // Etude SosielRomance_Active status is:  Not Playing; 
            */
            #endregion
        };
        internal static readonly Dictionary<string, bool> EtudeStatusOverridesLoveIsFree = new() {
            #region Wrath
            /*
            // Lich Path
            { "2ebd861e55143014c8067c6832cdf21c", false },  // Cue_0048
            // Vellexia
            { "81b15ede1bb2a3e4e926d5ca4be3e193", true },   // Cue_0066
            { "3aa48f68198afe14cb6de752ce80cc8f", true },   // Cue_0078
            // Queen
            { "7a160960668f2ef4180cb56edb8388e9", true },   // Cue_0044
            */
            #endregion
        };

        // Multiple Romances overrides
        // This modify the EtudeStatus condition for specific Owner blueprints 
        internal static readonly Dictionary<(string, string), bool> ConditionCheckOverrides = new() {
            #region Wrath
            /*
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "6a5a5f14-0531-421e-8225-f777fd22fa52"), true },  // Not Etude CamelliaRomance_Start status is:   Playing;   
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "0dadef83-142b-4126-9ef3-d2b3d6ac3c00"), true },  // Not Etude WenduagRomance_Active status is:   Playing;   
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "6af9fc46-b172-45f6-991b-95864d7535dd"), true },  // Not Etude LannRomance_Active status is:   Playing;      
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "08562d17-c875-477d-916f-484c86d6d56b"), true },  // Not Etude ArueshalaeRomance_Active status is:   Playing;   
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "eed10502-68fe-4f06-93f6-9a5a344194e1"), true },  // Not Etude DaeranRomance_Active status is:   Playing;      
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "79f99282-5d3a-4b2b-8d2e-61a0dc8f033f"), true },  // Not Etude SosielRomance_Active status is:   Playing; 
            */
            #endregion
        };
        internal static readonly Dictionary<string, bool> EtudeStatusOverrides = new() {
            #region Wrath
            /*
            { "f4acc1a428ffbee42965a6f13fe270ac", false },  // Cue_0058
            */
            #endregion
        };
        internal static readonly Dictionary<string, bool> FlagInRangeOverrides = new() {
            // RomanceCount Flag, as conditioned in Jealousy_event Blueprint, Activated by Jealousy_preparation
            { "cbb219fcb46948fba48a8bed94663e5d", false }
        };


        [HarmonyPatch(typeof(PcFemale), nameof(PcFemale.CheckCondition))]
        public static class PcFemale_CheckCondition_Patch {
            public static void Postfix(PcFemale __instance, ref bool __result) {
                if (!settings.toggleAllowAnyGenderRomance || __instance?.Owner is null) return;
                OwlLogging.Log($"checking {__instance} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (PcFemaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
            }
        }
        [HarmonyPatch(typeof(PcMale), nameof(PcMale.CheckCondition))]
        public static class PcMale_CheckCondition_Patch {
            public static void Postfix(PcMale __instance, ref bool __result) {
                if (!settings.toggleAllowAnyGenderRomance || __instance?.Owner is null) return;
                OwlLogging.Log($"checking {__instance} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (PcMaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
            }
        }

        [HarmonyPatch(typeof(Condition), nameof(Condition.Check), [typeof(ConditionsChecker), typeof(IConditionDebugContext)])]
        public static class Condition_Check_Patch {
            public static void Postfix(Condition __instance, ref bool __result) {
                if (__instance?.Owner is null) return;

                var key = (__instance.Owner.AssetGuid.ToString(), __instance.AssetGuid);
                if (settings.toggleAllowAnyGenderRomance) {
                    if (ConditionCheckOverridesLoveIsFree.TryGetValue(key, out var valueLoveIsFree)) { OwlLogging.Log($"overiding {(__instance.Owner.name, __instance.name)} to {valueLoveIsFree}"); __result = valueLoveIsFree; }
                }
                if (settings.toggleMultipleRomance) {
                    if (ConditionCheckOverrides.TryGetValue(key, out var value)) {
                        OwlLogging.Log($"overiding {(__instance.Owner.name, __instance.name)} to {value}");
                        __result = value;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EtudeStatus), nameof(EtudeStatus.CheckCondition))]
        public static class EtudeStatus_CheckCondition_Patch {
            public static void Postfix(EtudeStatus __instance, ref bool __result) {
                if (__instance?.Owner is null) return;

                var key = (__instance.Owner.AssetGuid.ToString());
                if (settings.toggleAllowAnyGenderRomance) {
                    if (EtudeStatusOverridesLoveIsFree.TryGetValue(key, out var valueLoveIsFree)) { OwlLogging.Log($"overiding {(__instance.Owner.name)} to {valueLoveIsFree}"); __result = valueLoveIsFree; }
                }
                if (settings.toggleMultipleRomance) {
                    if (EtudeStatusOverrides.TryGetValue(key, out var value)) { OwlLogging.Log($"overiding {__instance.Owner.name} to {value}"); __result = value; }
                }
            }
        }

        [HarmonyPatch(typeof(FlagInRange), nameof(FlagInRange.CheckCondition))]
        public static class FlagInRange_CheckCondition_Patch {
            public static void Postfix(FlagInRange __instance, ref bool __result) {
                if (__instance?.Owner is null) return;
                if (settings.toggleMultipleRomance) {
                    if (FlagInRangeOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { OwlLogging.Log($"overiding {__instance.Owner.name} to {value}"); __result = value; }
                }
            }
        }
        [HarmonyPatch(typeof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked), nameof(Kingmaker.Designers.EventConditionActionSystem.Conditions.RomanceLocked.CheckCondition))]
        public static class RomanceLocked_CheckCondition_Patch {
            public static void Postfix(ref bool __result) {
                if (settings.toggleMultipleRomance) {
                    if (__result) OwlLogging.Log("Overriding RomanceLocked.CheckCondition result to false");
                    __result = false;
                }
            }
        }
    }
}
