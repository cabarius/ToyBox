using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem;
using Kingmaker.Localization;
using Kingmaker.UI.MVVM._VM.Dialog.Dialog;
using Kingmaker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.UI.Dialog;
using ModKit;

namespace ToyBox.classes.MonkeyPatchin.BagOfPatches {
    [HarmonyPatch]
    internal static class VoiceOver {
        private static BlueprintUnit currentSpeaker = null;
        [HarmonyPatch(typeof(DialogController), nameof(DialogController.HandleOnCueShow))]
        [HarmonyPrefix]
        public static void HandleOnCueShow(Kingmaker.Controllers.Dialog.CueShowData data) {
            currentSpeaker = data?.Cue?.Speaker?.Blueprint;
        }
        [HarmonyPatch(typeof(DialogVM), nameof(DialogVM.HandleOnCueShow))]
        [HarmonyPrefix]
        public static void HandleOnCueShow2(Kingmaker.Controllers.Dialog.CueShowData data) {
            currentSpeaker = data?.Cue?.Speaker?.Blueprint;
        }
        [HarmonyPatch(typeof(UIAccess), nameof(UIAccess.Bark), [typeof(EntityDataBase), typeof(LocalizedString), typeof(float), typeof(bool)])]
        [HarmonyPrefix]
        public static void Bark(EntityDataBase entity) {
            if (entity is UnitEntityData entity2) {
                currentSpeaker = entity2?.Blueprint;
            }
        }
        [HarmonyPatch(typeof(UIAccess), nameof(UIAccess.BarkSubtitle), [typeof(UnitEntityData), typeof(LocalizedString), typeof(float), typeof(bool)])]
        [HarmonyPrefix]
        public static void BarkSubtitle(UnitEntityData entity) {
            currentSpeaker = entity?.Blueprint;
        }

        [HarmonyPatch(typeof(LocalizedString), nameof(LocalizedString.PlayVoiceOver))]
        [HarmonyPrefix]
        public static bool PlayVoiceOver() {
            if (Main.Settings.namesToDisableVoiceOver.Contains(currentSpeaker?.CharacterName?.ToLower() ?? currentSpeaker?.AssetGuid.ToString())) {
                Mod.Debug($"Skipped {currentSpeaker?.ToString() ?? "null"} VoiceOver.");
                return false;
            }
            return true;
        }
    }
}
