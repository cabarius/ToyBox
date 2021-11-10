
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

        [HarmonyPatch(typeof(CompanionInParty), nameof(CompanionInParty.CheckCondition))]
        public static class CompanionInParty_CheckCondition_Patch {
            public static void Postfix(CompanionInParty __instance, ref bool __result) {
                if (__instance.Not) return; // We only want this patch to run for conditions requiring the character to be in the party so if it is for the inverse we bail.  Example of this comes up with Lann and Wenduag in the final scene of the Prologue Labyrinth
                if (settings.toggleRemoteCompanionDialog && __instance.Owner is BlueprintCue cueBP) {
                    Mod.Debug($"overiding {cueBP.name} Companion {__instance.companion.name} In Party to true");
                    __result = true;
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
                var unit =
                    Game.Instance.State.Units.Concat(Game.Instance.Player.AllCrossSceneUnits)
                        //.Where(u => u.IsInGame && !u.Suppressed)
                        .Where(u => settings.toggleExCompanionDialog || !u.IsExCompanion())
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
                Mod.Debug($"BlueprintCue_CanShow_Patch - {__instance?.Speaker?.Blueprint?.Name.orange() ?? ""} result: {__result}");
            }
        }
    }
}