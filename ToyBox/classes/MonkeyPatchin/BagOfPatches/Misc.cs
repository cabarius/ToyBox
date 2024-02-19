using HarmonyLib;
using Kingmaker.Achievements;

namespace ToyBox.BagOfPatches {
    internal static partial class Misc {
        [HarmonyPatch(typeof(AchievementsManager), nameof(AchievementsManager.OnAchievementUnlocked))]
        private static class AchievementsManager_OnAchievementsUnlocked_Patch {
            private static void Postfix(AchievementEntity ach) {
                AchievementsUnlocker.unlocked.Add(ach);
                AchievementsUnlocker.AchievementBrowser.needsReloadData = true;
            }
        }
    }
}