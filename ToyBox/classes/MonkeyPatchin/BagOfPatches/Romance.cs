using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
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
            { "7364becdf5cc4b94dba30a9fe7c3b790", false },  // Cue      Cue_0234
            { "e166872fc2989f548af1b3e2ba8f7156", false },  // Cue      Cue_0029
            // Camellia
            { "789ffa9876fd92f439d4b975b16be283", true },   // Cue      Cue_0066_GiefSex
            { "f263d6ed04831f240bf2a8dce2b5ce33", true },   // Answer   Answer_0052
            { "a96fc116bb7af94488b6da41161a47c7", true },   // Answer   Answer_0060 
        };

        [HarmonyPatch(typeof(PcFemale), nameof(PcFemale.CheckCondition))]
        public static class PcFemale_CheckCondition_Patch {
            public static void Postfix(PcFemale __instance, bool __result) {
                if (settings.toggleAllowAnyGenderRomance 
                    && PcFemaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) __result = value;
            }
        }
        [HarmonyPatch(typeof(PcMale), nameof(PcMale.CheckCondition))]
        public static class PcMale_CheckCondition_Patch {
            public static void Postfix(PcMale __instance, bool __result) {
                if (settings.toggleAllowAnyGenderRomance
                    && PcMaleOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) __result = value;
            }
        }

        // Multiple Romances overrides
        // This modify the EtudeStatus condition for specific Owner blueprints 
        internal static readonly Dictionary<string, bool> EtudeStatusOverrides = new() {
            { "f4acc1a428ffbee42965a6f13fe270ac", false },
        };

        [HarmonyPatch(typeof(EtudeStatus), nameof(EtudeStatus.CheckCondition))]
        public static class EtudeStatus_CheckCondition_Patch {
            public static void Postfix(EtudeStatus __instance, bool __result) {
                if (settings.toggleMultipleRomance
                    && EtudeStatusOverrides.TryGetValue(__instance.Owner.AssetGuid.ToString(), out var value)) __result = value;
            }
        }
    }
}
