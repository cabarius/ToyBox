
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
using JetBrains.Annotations;
using Kingmaker.Localization;
using Kingmaker.ElementsSystem;
using Kingmaker.DialogSystem.State;

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

        // These exclude certain problematic cues
        internal static readonly HashSet<string> ProblemCues = new() {
            // Underground Army
            { "0b3974b21835707458e535fcb330a2a6" }, // SullLannIsNeutralized_dialogue - Cue 0014

            // The Last Resort
            { "372e6b1be6427f04788827503d8e3330" }, // SullWeak_dialogue - Cue 0041
            { "a9802b4720687bc498e0722b89b45ec9" }, // SullWeak_dialogue - Cue 0009

            // Feud of the Faithful
            { "a2f183f2a53b6eb4ea4337b709eb2320" }, // HurlunRamien_Early_Dialogue - Cue 0098
            { "5de6be5c80b5f25439151dec4a812714" }, // HurlunRamien_Early_Dialogue - Cue 0100

        };

        [HarmonyPatch(typeof(CompanionInParty), nameof(CompanionInParty.CheckCondition))]
        public static class CompanionInParty_CheckCondition_Patch {
            public static void Postfix(CompanionInParty __instance, ref bool __result) {
                if (__instance.Not) return; // We only want this patch to run for conditions requiring the character to be in the party so if it is for the inverse we bail.  Example of this comes up with Lann and Wenduag in the final scene of the Prologue Labyrinth
                if (SecretCompanions.Contains(__instance.companion.AssetGuid.ToString())) return;
                if (ProblemCues.Contains(__instance.Owner.AssetGuid.ToString())) return;
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
            // A Strike From The Sky
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

            // One Final Breath
            { "1fa9c8f8fb552d64eba30fb435a1ed78", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0001
            { "6013c47a174db6a4e9f9fa7abc44dcba", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0003
            { "64bfc76453ad0bb43991f0b456d4b1a8", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0007
            { "947dfadc76c571546acece7947554004", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0010
            { "2945a18cdcdd7924a8a8f3cdb1fcc1a7", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0012
            { "d8e461d62efe194449f169aa88929a7f", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0024
            { "c3e3214669f9eb54399e6df062ff3326", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0025
            { "3ecc04f4da4c06b4b90a26729c94c2ae", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0028
            { "b81f18abf6968ac47bb86d727f57c9c3", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0030
            { "219b6fb6e2eacdb4895a4a20ee68a0d1", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0032
            { "c2f232bfa0826f448b6e51400163898d", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0037
            { "22a7597e84a7ebe41869388ce4025eb2", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0039
            { "ceb4520d1fc5e1a45922af456b764669", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0040
            { "e4322c31183ec0e4bb9159e6fb86e02f", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0041
            { "08cdc40c3ce7abc4697b81a59b45882a", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0043
            { "ebfcd069e0c471c4cab44a128583788b", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0044
            { "d9cad9cb374deec4b9d8ef5cd34d34cd", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0045
            { "9b3f488091a6579479b9d23b9561e663", true }, // DearanVsInquisitor_DaeranQ3_dialog - Cue 0048

            // The Final Verdict
            { "3abf579b8b99990418f870b8350a0b64", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0002
            { "41298acd8a6ce234692f2a2724c78af7", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0007
            { "ec0d6193faacf944f9303a06b57578cd", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0008
            { "1ca45b5b401cd974ea892b42125a0245", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0010
            { "0750a00382c75a44d873ceb2c6edec4a", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0011
            { "c778136b1ddac6a419e7d1ff7c6368fc", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0014
            { "1d172a6a0bebca14baa10072305c596e", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0015
            { "cf0d6cedef0d1b340969125d7e4361de", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0016
            { "5270af5d90b9bdd41b6e83699c329ddd", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0017
            { "431285b5b253f4b4cad882e2574052c2", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0018
            { "6f83cc97f55ece04189f6bcc28bd6e3a", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0019
            { "4d0cf02e31717b941be04d2869cbf6ab", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0020
            { "c90837ea131d93640a9f58aacafe7e69", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0022
            { "376560c11107db146be2e606db3839ef", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0023
            { "745d1e58066b94042bdb00d506ca550e", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0024
            { "d5242a096679b84439976e955c90a0dc", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0025
            { "12cafec86b525a641afa85445b046948", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0026
            { "eba7118fa0ecfbd49a4f9cd84adb5716", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0027
            { "e5cf9f39c45268c4697b8099c75ef143", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0028
            { "e044efad38f07bb4b9f08ca121d613b2", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0039
            { "be8ad66cf18e17a4fbd0da3bd58ce8be", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0043
            { "4f7dc53630dd196438bd52b56319eb62", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0054
            { "3fe3bc638e439ee409a100621585bcae", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0055
            { "8dc13ca2bd472c64d97ccb8ce431e1b3", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0060
            { "6c66d7a49a47f68449c05c3fa6e61b75", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0066
            { "dbc87f69fc4862a40bb3ea4439086dda", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0073
            { "e933bde184f18c94791447a17666820f", true }, // HellknightsTribunal_RegillQ3_dialog - Cue 0075

            { "0d2a8f574df78f548ad0f58fe297a245", true }, // Avowal_RegillQ3_dialog - Cue 0001
            { "178532df60851a14d9b4318142b46774", true }, // Avowal_RegillQ3_dialog - Cue 0007
            { "414978cfcef3eb949bd389474fc56543", true }, // Avowal_RegillQ3_dialog - Cue 0008
            { "8efc97ff604d5bb48a7ffeeaddab2e99", true }, // Avowal_RegillQ3_dialog - Cue 0009
            { "5a6e7097b358732439c443ab4958fa4f", true }, // Avowal_RegillQ3_dialog - Cue 0010
            { "86f245555e750f847a13e8b5635ace4d", true }, // Avowal_RegillQ3_dialog - Cue 0011
            { "d162eaf4f12017041846607338bcbfe6", true }, // Avowal_RegillQ3_dialog - Cue 0012
            { "a1acca12337104a46a245e5faaa353e1", true }, // Avowal_RegillQ3_dialog - Cue 0016
            { "e63c40e99ccd4c64b8cd2d2fac159587", true }, // Avowal_RegillQ3_dialog - Cue 0018
            { "e969b1813a80f7e468974136a37fdac0", true }, // Avowal_RegillQ3_dialog - Cue 0019
            { "68d369b7338e6a348bf39850373ebbba", true }, // Avowal_RegillQ3_dialog - Cue 0020
            { "0b3622ca632196c4cacb3157dd7bf2dd", true }, // Avowal_RegillQ3_dialog - Cue 0021
            { "86c19dc0f1aef5c43b1ae5f14928696d", true }, // Avowal_RegillQ3_dialog - Cue 0022
            { "efde34d4a08ebbb42a54f8bda616f611", true }, // Avowal_RegillQ3_dialog - Cue 0023
            { "b720b1397a2d8914580e1589f7f4e3db", true }, // Avowal_RegillQ3_dialog - Cue 0030
            { "74ebb09c60ccca3448f35422c5b29979", true }, // Avowal_RegillQ3_dialog - Cue 0031
            { "f2a7a52ba0f1e7346aa47c9ff53c22bc", true }, // Avowal_RegillQ3_dialog - Cue 0032
            { "037c6ab7548083f49bc43b1195cf05ac", true }, // Avowal_RegillQ3_dialog - Cue 0033

            // Bad Blood
            { "c3ca34383f9ee3a4e9908ce68a2e828b", true }, // MongrelsDefeated_dialogue - Cue 0001

            // More Than Nothing
            { "cb4cc74067ccf074aad0974519a6013b", true }, // FoxMyself_dialogue - Cue 0001

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

        [HarmonyPatch(typeof(DialogController), nameof(DialogController.AddAnswers))]
        public static class DialogController_AddAnswers_Patch {
            public static bool Prefix(DialogController __instance, [NotNull] ref IEnumerable<BlueprintAnswerBase> answers, [CanBeNull] BlueprintCueBase continueCue) {
                if (!settings.toggleShowAnswersForEachConditionalResponse) return true;
                List<BlueprintAnswerBase> expandedAnswers = new();
                foreach (var answerBase in answers) {
                    if (answerBase is BlueprintAnswer answer) {
                        if (answer.NextCue is CueSelection cueSelection) {
                            Mod.Debug($"checking: {answer.name} - {cueSelection.Cues.Count} {answer.Text}");

                            if (cueSelection.Cues.Count <= 1)
                                expandedAnswers.Add(answer);
                            else
                                foreach (var cueBase in cueSelection.Cues.Dereference<BlueprintCueBase>()) {
                                    if (answer.ShowOnce) {
                                        var dialog = Game.Instance.Player.Dialog;
                                        var dialogController = Game.Instance.DialogController;
                                        if (dialogController.LocalSelectedAnswers.Where(a => a.AssetGuid == answer.AssetGuid).Any())
                                            continue;
                                        if (dialog.SelectedAnswers.Where(a => a.AssetGuid == answer.AssetGuid).Any())
                                            continue;
                                    }
                                    if (settings.toggleShowAllAnswersForEachConditionalResponse || cueBase.CanShow()) {
                                        var multiAnswer = answer.ShallowClone();
                                        var cueSel = cueSelection.ShallowClone();
                                        cueSel.Cues = new List<BlueprintCueBaseReference>() { cueBase.ToReference<BlueprintCueBaseReference>() };
                                        cueSel.Strategy = Strategy.First;
                                        multiAnswer.NextCue = cueSel;
                                        expandedAnswers.Add(multiAnswer);
                                    }
                                }
                        }
                        else
                            expandedAnswers.Add(answer);
                    }
                    else
                        expandedAnswers.Add(answerBase);
                }
                answers = expandedAnswers;
                return true;
            }
        }

#if false
        [HarmonyPatch(typeof(BlueprintAnswer), nameof(BlueprintAnswer.CanShow))]
        public static class BlueprintAnswer_CanShow_Patch {
            public static bool Prefix(BlueprintAnswer __instance, ref bool __result) {
                if (!settings.toggleShowAnswersForEachConditionalResponse) return true;
                if (__instance.ShowOnce) {
                    var dialog = Game.Instance.Player.Dialog;
                    var dialogController = Game.Instance.DialogController;
                    var matchingSelectedAnswers = dialog.SelectedAnswers.Where(a => a.AssetGuid == __instance.AssetGuid);
                    Mod.Debug($"matchingAnswers = {string.Join(", ", matchingSelectedAnswers.Select(a => a.name))}");
                    if (__instance.ShowOnceCurrentDialog) {
                        if (dialogController.LocalSelectedAnswers.Where(a => a.AssetGuid == __instance.AssetGuid).Any()) {
                            __result = true;
                            return false;
                        }
                    }
                    else if (matchingSelectedAnswers.Any()) {
                        __result = true;
                        return false;
                    }
                }
                return true;
            }
        }
#endif
#if false
        [HarmonyPatch(typeof(CueSelection), nameof(CueSelection.Select))]
        public static class CueSelection_Select_Patch {
            public static bool Prefix(CueSelection __instance, ref BlueprintCueBase __result) {
                if (!settings.toggleRandomizeCueSelections) return true;
                List<BlueprintCueBase> blueprintCueBaseList = null;
                foreach (BlueprintCueBase blueprintCueBase in __instance.Cues.Dereference()) {
                    if (blueprintCueBase != null && blueprintCueBase.CanShow()) {
                        if (blueprintCueBaseList == null)
                            blueprintCueBaseList = new();
                        blueprintCueBaseList.Add(blueprintCueBase);
                    }
                }
                if (blueprintCueBaseList == null) {
                    __result = null;
                    return false;
                }
                int index = UnityEngine.Random.Range(0, blueprintCueBaseList.Count);
                Mod.Debug($"CueSelection_Select_Patch - index: {index}");
                __result = blueprintCueBaseList[index];
                return false;
            }
        }
#endif
    }
}