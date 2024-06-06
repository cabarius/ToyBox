using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.Localization;
using Kingmaker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModKit;
using Kingmaker.Code.UI.MVVM.VM.Bark;
using Kingmaker.Code.UI.MVVM.VM.Dialog.Dialog;
using Kingmaker.Controllers.Dialog;
using System.Diagnostics;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker;
using Kingmaker.Blueprints;

namespace ToyBox.BagOfPatches {
    [HarmonyPatch]
    internal static class VoiceOver {
        internal static BlueprintUnit currentSpeaker = null;
        [HarmonyPatch(typeof(BarkPlayer))]
        internal static class BarkPlayer_Patches {
            [HarmonyPatch(nameof(BarkPlayer.Bark), [typeof(Entity), typeof(LocalizedString), typeof(string), typeof(float), typeof(bool)])]
            [HarmonyPrefix]
            internal static void Bark1(Entity entity) {
                if (entity is BaseUnitEntity entity2) {
                    currentSpeaker = entity2?.Blueprint;
                } else {
                    currentSpeaker = null;
                }
            }
            [HarmonyPatch(nameof(BarkPlayer.Bark), [typeof(Entity), typeof(LocalizedString), typeof(float), typeof(bool), typeof(BaseUnitEntity), typeof(bool)])]
            [HarmonyPrefix]
            internal static void Bark2(Entity entity) {
                if (entity is BaseUnitEntity entity2) {
                    currentSpeaker = entity2?.Blueprint;
                } else {
                    currentSpeaker = null;
                }
            }
        }
        [HarmonyPatch(typeof(DialogVM), nameof(DialogVM.HandleOnCueShow))]
        [HarmonyPrefix]
        internal static void DialogVM_HandleOnCueShow(CueShowData data) {
            currentSpeaker = data?.Cue?.Speaker?.Blueprint ?? Game.Instance.DialogController?.CurrentSpeaker?.Blueprint;
        }
        [HarmonyPatch(typeof(SpaceEventVM), nameof(SpaceEventVM.HandleOnCueShow))]
        [HarmonyPrefix]
        internal static void SpaceEventVM_HandleOnCueShow(CueShowData data) {
            currentSpeaker = data?.Cue?.Speaker?.Blueprint ?? Game.Instance.DialogController?.CurrentSpeaker?.Blueprint;
        }
        [HarmonyPatch(typeof(LocalizedString), nameof(LocalizedString.GetVoiceOverSound))]
        [HarmonyPrefix]
        internal static bool GetVoiceOverSound(ref string __result) {
            var cName = currentSpeaker?.CharacterName?.ToLower() ?? currentSpeaker?.AssetGuid?.ToString() ?? "";
            if (cName != "" && Main.Settings.namesToDisableVoiceOver.Contains(cName)) {
                __result = "";
                return false;
            }
            return true;
        }
    }
}
