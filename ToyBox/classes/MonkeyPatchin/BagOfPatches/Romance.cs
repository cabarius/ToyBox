using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using Kingmaker.UI.MVVM._PCView.ActionBar;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.UnitSettings;
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
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        // Any Gender Any Romance Overrides
        // These modify the PcFemale/PcMale conditions for specific Owner blueprints 
        internal static readonly Dictionary<string, bool> PcFemaleOverrides = new() {
            // Lann
            { "2e7f66de32ccad14bbb17855b0d125fb", true },   // Etude    LannKTC_WarCamp_LannAndFeelings
            { "d9baf40d38ceaf248bd5306f0e344bdb", true },   // Etude    LannRomance
            { "15fbf08c47b9cf34d8d535765e9a143a", true },   // Answer   Answer_0052
            { "2942d51c334b42748822ea2c48093a72", true },   // Answer   AnswersList_0201
            { "c68fad6a2d296f54f825eb1557153923", true },   // Dialog   WorldwoundEdge_GMBE
            // Sosiel 
            { "1071445514a15ec42a057a987886a0b5", false },  // Cue      Cue_0019
            { "8abf3aa7d2244f048abdcfbc48721eff", false },  // Cue      Cue_0030
            { "54fea9d1c9e0b69429bec08fb49a40d2", false },  // Cue      Cue_0235
            // Camellia 
            { "0144dcae4dc708744850d81254f28ec4", false },  // Cue      Cue_0067_NoGiefSex
            { "7fec7b3b23df5f9498083f096b09f055", false }   // Answer   Answer_0057
        };
        internal static readonly Dictionary<string, bool> PcMaleOverrides = new() {
            // Sosiel 
            { "5170dd15fdfd0094aa561e4f331c269f", true },   // Cue      Cue_0018
            { "7364becdf5cc4b94dba30a9fe7c3b790", true },   // Cue      Cue_0234
            { "e166872fc2989f548af1b3e2ba8f7156", true },   // Cue      Cue_0029
            // Camellia
            { "55c0fe80d141ecf40b49c7ad12746afb", true },   // Cue      Cue_0016
            { "789ffa9876fd92f439d4b975b16be283", true },   // Cue      Cue_0066_GiefSex
            { "f263d6ed04831f240bf2a8dce2b5ce33", true },   // Answer   Answer_0052
            { "a96fc116bb7af94488b6da41161a47c7", true },   // Answer   Answer_0060 
        };

        // Path Romance Overrides
        // These modify the EtudeStatus condition for specific Owner blueprints 
        internal static readonly Dictionary<(string, string), bool> ConditionCheckOverridesLoveIsFree = new()
        {
            // Lich Path
            { ("7f532b681d64f3741a7aa0aebba7c4db", "977f3380-2938-4cc8-a26a-448edc6f9259"), false },  // Etude CamelliaRomance_Start status is:  Not Playing;   
            { ("7f532b681d64f3741a7aa0aebba7c4db", "52632774-cbb4-4eea-ada6-37ec2708e07d"), false },  // Etude WenduagRomance_Active status is:  Not Playing;   
            { ("7f532b681d64f3741a7aa0aebba7c4db", "12928be5-97e8-4e7c-ac5c-02d704289e7f"), false },  // Etude LannRomance_Active status is:  Not Playing;      
            { ("7f532b681d64f3741a7aa0aebba7c4db", "2bbad7a7-5918-4c14-b909-f1a7bbce9248"), false },  // Etude ArueshalaeRomance_Active status is:  Not Playing;   
            { ("7f532b681d64f3741a7aa0aebba7c4db", "55fd2fa2-9644-462a-a02e-23987e05fd62"), false },  // Etude DaeranRomance_Active status is:  Not Playing;      
            { ("7f532b681d64f3741a7aa0aebba7c4db", "b0f684c1-0cc9-4ec7-a4a3-fe47e3d9847c"), false },  // Etude SosielRomance_Active status is:  Not Playing; 
        };
        internal static readonly Dictionary<string, bool> EtudeStatusOverridesLoveIsFree = new()
        {
            // Lich Path
            { "2ebd861e55143014c8067c6832cdf21c", false },  // Cue_0048
            // Vellexia
            { "81b15ede1bb2a3e4e926d5ca4be3e193", true },   // Cue_0066
            { "3aa48f68198afe14cb6de752ce80cc8f", true },   // Cue_0078
            // Queen
            { "7a160960668f2ef4180cb56edb8388e9", true },   // Cue_0044
        };

        // Multiple Romances overrides
        // This modify the EtudeStatus condition for specific Owner blueprints 
        internal static readonly Dictionary<(string, string), bool> ConditionCheckOverrides = new() {
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "6a5a5f14-0531-421e-8225-f777fd22fa52"), true },  // Not Etude CamelliaRomance_Start status is:   Playing;   
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "0dadef83-142b-4126-9ef3-d2b3d6ac3c00"), true },  // Not Etude WenduagRomance_Active status is:   Playing;   
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "6af9fc46-b172-45f6-991b-95864d7535dd"), true },  // Not Etude LannRomance_Active status is:   Playing;      
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "08562d17-c875-477d-916f-484c86d6d56b"), true },  // Not Etude ArueshalaeRomance_Active status is:   Playing;   
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "eed10502-68fe-4f06-93f6-9a5a344194e1"), true },  // Not Etude DaeranRomance_Active status is:   Playing;      
            { ("39bd3b1e9fb6fef4c8e92674c02295df", "79f99282-5d3a-4b2b-8d2e-61a0dc8f033f"), true },  // Not Etude SosielRomance_Active status is:   Playing; 
        };
        internal static readonly Dictionary<string, bool> EtudeStatusOverrides = new() {
            { "f4acc1a428ffbee42965a6f13fe270ac", false },  // Cue_0058
        };
        internal static readonly Dictionary<string, bool> FlagInRangeOverrides = new() {
            { "4799a25da39295b43a6eefcd2cb2b4a7", false },  // Etude    KTC_Jealousy
        };

        [HarmonyPatch(typeof(PcFemale), nameof(PcFemale.CheckCondition))]
        public static class PcFemale_CheckCondition_Patch {
            public static void Postfix(PcFemale __instance, ref bool __result) {
                if (!settings.toggleAllowAnyGenderRomance || __instance?.Owner is null) return;
                Mod.Debug($"checking {__instance.ToString()} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (PcFemaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
            }
        }
        [HarmonyPatch(typeof(PcMale), nameof(PcMale.CheckCondition))]
        public static class PcMale_CheckCondition_Patch {
            public static void Postfix(PcMale __instance, ref bool __result) {
                if (!settings.toggleAllowAnyGenderRomance || __instance?.Owner is null) return;
                Mod.Debug($"checking {__instance.ToString()} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (PcMaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
            }
        }

        [HarmonyPatch(typeof(Condition), nameof(Condition.Check))]
        public static class Condition_Check_Patch {
            public static void Postfix(Condition __instance, ref bool __result) {
                if (__instance?.Owner is null) return;

                var key = (__instance.Owner.AssetGuid.ToString(), __instance.AssetGuid);
                if (settings.toggleAllowAnyGenderRomance) {
                    if (ConditionCheckOverridesLoveIsFree.TryGetValue(key, out var valueLoveIsFree)) { Mod.Debug($"overiding {(__instance.Owner.name, __instance.name)} to {valueLoveIsFree}"); __result = valueLoveIsFree; }
                }
                if (settings.toggleMultipleRomance) {
                    if (ConditionCheckOverrides.TryGetValue(key, out var value)) {
                        Mod.Debug($"overiding {(__instance.Owner.name, __instance.name)} to {value}");
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
                    if (EtudeStatusOverridesLoveIsFree.TryGetValue(key, out var valueLoveIsFree)) { Mod.Debug($"overiding {(__instance.Owner.name)} to {valueLoveIsFree}"); __result = valueLoveIsFree; }
                }
                if (settings.toggleMultipleRomance) {
                    if (EtudeStatusOverrides.TryGetValue(key, out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
                }
            }
        }
        [HarmonyPatch(typeof(FlagInRange), nameof(FlagInRange.CheckCondition))]
        public static class FlagInRange_CheckCondition_Patch {
            public static void Postfix(FlagInRange __instance, ref bool __result) {
                if (!settings.toggleMultipleRomance || __instance?.Owner is null) return;
                if (FlagInRangeOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) { Mod.Debug($"overiding {__instance.Owner.name} to {value}"); __result = value; }
            }
        }
    }
}
