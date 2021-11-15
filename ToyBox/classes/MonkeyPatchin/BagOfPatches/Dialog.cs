
using HarmonyLib;
using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Evalutors = Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
using ModKit;
using System.Linq;
using System;
using Kingmaker.DialogSystem;
using System.Collections.Generic;
using Kingmaker.Utility;
using Kingmaker.UnitLogic;
using Kingmaker.Controllers.Dialog;
using Kingmaker.Blueprints;
using UnityEngine;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem;
using Random = System.Random;

namespace ToyBox.BagOfPatches {
    internal static class Dialog {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        // These exclude certain secret companions
        internal static readonly HashSet<string> SecretCompanions = new() {
            { "0bb1c03b9f7bbcf42bb74478af2c6258" }, // Trever
            { "6b1f599497f5cfa42853d095bda6dafd" }, // Delamere for Lich
            { "d58b81fd7ec14784fa05bc29fb6c7ae0" }, // Galfrey for Lich
            { "e46927657a79db64ea30758db3f42bb9" }, // Galfrey
            { "7ece3afabe2b6f343b17d1eaa409d273" }, // Ciar for Lich
            { "e551850403d61eb48bb2de010d12c894" }, // Kestoglyr for Lich
            { "0bcf3c125a28d164191e874e3c0c52de" }  // Staunton for Lich
        };

        [HarmonyPatch(typeof(CompanionInParty), nameof(CompanionInParty.CheckCondition))]
        public static class CompanionInParty_CheckCondition_Patch {
            public static void Postfix(CompanionInParty __instance, ref bool __result) {
                if (__instance.Not) return; // We only want this patch to run for conditions requiring the character to be in the party so if it is for the inverse we bail.  Example of this comes up with Lann and Wenduag in the final scene of the Prologue Labyrinth
                if (SecretCompanions.Contains(__instance.companion.AssetGuid.ToString())) return;           
                if (settings.toggleRemoteCompanionDialog) {
                    if (__instance.Owner is BlueprintCue cueBP) {
                        Mod.Debug($"overiding {cueBP.name} Companion {__instance.companion.name} In Party to true");
                        __result = true;
                    }
                    if (__instance.Owner is BlueprintCue etudeBP) {

                    }
                }
            }
        }
        [HarmonyPatch(typeof(Evalutors.CompanionInParty), nameof(Evalutors.CompanionInParty.GetValueInternal))]
        public static class Evalualtors_CompanionInParty_GetValueInternal_Patch {
            public static bool Prefix(Kingmaker.Designers.EventConditionActionSystem.Evaluators.CompanionInParty __instance, ref UnitEntityData __result) {
                Mod.Debug($"Evalutors checking {__instance} guid:{__instance.AssetGuid} owner:{__instance.Owner.name} guid: {__instance.Owner.AssetGuid}) value: {__result}");
                if (!settings.toggleRemoteCompanionDialog) return true;
                if (__instance.Owner is BlueprintCue cueBP) {
                    var unitEntityData = Game.Instance.Player.AllCrossSceneUnits.FirstOrDefault<UnitEntityData>((Func<UnitEntityData, bool>)(unit => __instance.IsCompanion(unit.Blueprint)));
                    __result = unitEntityData;
                    return false;
                }

                return true;
            }
        }

        internal static readonly Dictionary<string, bool> DialogSpeaker_GetEntityOverrides = new() {
            { "804b8b87618f8c840a731383a5b448ed", true }, // GargoyleAttack_Start_Dialogue - Cue 0002
            { "7c812f8f46bd3bb4ba8e6561888f6416", true }, // GargoyleAttack_Start_Dialogue - Cue 0008
            { "2d027a65c2cdc95409d4e47da952bc07", true }, // GargoyleAttack_Start_Dialogue - Cue 0013
            { "209a861fefed80b44b29a5a648f2ed95", true }, // GargoyleAttack_Start_Dialogue - Cue 0014

            { "872dbbcca83313944b923fe9076b522d", true }, // GargoyleAttack_Camellia_Dialog - Cue 0001
            { "3dc95933510ddc342acac646976ca331", true }, // GargoyleAttack_Camellia_Dialog - Cue 0006
            { "8834a2a27b3662548910b9d2cdd84a68", true }, // GargoyleAttack_Camellia_Dialog - Cue 0007
            { "1d6e85632481ea6469fe0ce058043092", true }, // GargoyleAttack_Camellia_Dialog - Cue 0008

            { "0111658988726d449b900d4e21866fa4", true }, // GargoyleAttack_Seelah_Dialogue - Cue 0001
            { "4041ed38a35a5ca4fb7b128a4e2bf2d8", true }, // GargoyleAttack_Seelah_Dialogue - Cue 0006
            { "65c43eb9234fcec488106a3f10b98e46", true }, // GargoyleAttack_Seelah_Dialogue - Cue 0012

            { "f018c76c10022274ab7852cb89664183", true }, // Anevia_Start_Dialogue - Cue 0081

            { "20a9fb7961786ca4ab55c370eae1dd2b", true }, // Companion_Ember_Dialogue - Cue 0037
            { "a10cb691784999a41ba9ff07d621f5e2", true }, // Companion_Ember_Dialogue - Cue 0039
            { "04a2c833eb9b59141a330c562def4156", true }, // Companion_Ember_Dialogue - Cue 0042
        };

        [HarmonyPatch(typeof(DialogSpeaker), nameof(DialogSpeaker.GetEntity))]
        public static class DialogSpeaker_GetEntity_Patch {
            public static bool Prefix(DialogSpeaker __instance, BlueprintCueBase cue, ref UnitEntityData __result) {
                if (!settings.toggleRemoteCompanionDialog) return true;
                if (__instance.Blueprint == null) {
                    __result = null;
                    return false;
                }
                Mod.Trace($"getting unit for speaker {__instance.Blueprint.name}");
                var dialogPosition = Game.Instance.DialogController.DialogPosition;
                Mod.Trace($"dialogPos: {dialogPosition}");
                var second = Game.Instance.EntityCreator.CreationQueue.Select(ce => ce.Entity).OfType<UnitEntityData>();
                __instance.MakeEssentialCharactersConscious();
                Mod.Trace($"second: {second?.ToString()} matching: {second.Select(u => __instance.SelectMatchingUnit(u))}");
                var overrides = DialogSpeaker_GetEntityOverrides;
                var GUID = cue?.AssetGuid.ToString();
                bool hasOverride = GUID != null ? DialogSpeaker_GetEntityOverrides.ContainsKey(GUID) : false;
                bool overrideValue = hasOverride && DialogSpeaker_GetEntityOverrides[GUID];
                var unit = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCrossSceneUnits)
                        //.Where(u => u.IsInGame && !u.Suppressed)
                        .Where(u => hasOverride ? overrideValue : settings.toggleExCompanionDialog || !u.IsExCompanion())
                        .Concat(second)
                        .Select(new Func<UnitEntityData, UnitEntityData>(__instance.SelectMatchingUnit))
                        .NotNull()
                        .Distinct()
                        .Nearest(dialogPosition);
                Mod.Debug($"found {unit?.CharacterName ?? "no one".cyan()} position: {unit?.Position.ToString() ?? "n/a"}");
                if (unit != null) {
                    if (unit.DistanceTo(dialogPosition) > 25) {
                        var mainChar = Game.Instance.Player.MainCharacter.Value;
                        var mainPos = mainChar.Position;
                        var offset = 4f * UnityEngine.Random.insideUnitSphere;
                        var mainDirection = mainChar.OrientationDirection;
                        unit.Position = mainPos - 5 * mainDirection + offset;
                    }
                    __result = unit;
                    return false;
                }
                DialogDebug.Add((BlueprintScriptableObject)cue, "speaker doesnt exist", Color.red);
                __result = null;
                return false;
            }
        }

        [HarmonyPatch(typeof(BlueprintCue), nameof(BlueprintCue.CanShow))]
        public static class BlueprintCue_CanShow_Patch {

            public static void Postfix(BlueprintCue __instance, ref bool __result) {
                Mod.Debug($"BlueprintCue_CanShow_Patch - {__instance?.Speaker?.Blueprint?.Name.orange() ?? ""} BP: {__instance} result: {__result}");
            }
        }
    }
}