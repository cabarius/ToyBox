using HarmonyLib;
using Kingmaker.Achievements;
using Kingmaker.QA;
using ModKit;
using Owlcat.Runtime.Core.Logging;
using System;

namespace ToyBox.BagOfPatches {
    internal static partial class Misc {
        [HarmonyPatch(typeof(AchievementsManager), nameof(AchievementsManager.OnAchievementUnlocked))]
        private static class AchievementsManager_OnAchievementsUnlocked_Patch {
            private static void Postfix(AchievementEntity ach) {
                AchievementsUnlocker.unlocked.Add(ach);
                AchievementsUnlocker.AchievementBrowser.needsReloadData = true;
            }
        }
        [HarmonyPatch(typeof(LogChannelEx), nameof(LogChannelEx.ErrorWithReport), [typeof(LogChannel), typeof(string), typeof(object[])])]
        private static class PatchyPatch {
            [HarmonyFinalizer]
            private static Exception NoErrorInLoggingPlease(Exception __exception) {
                if (__exception != null) {
                    Mod.Log(__exception?.ToString() ?? "");
                }
                return null;
            }
        }
    }
}