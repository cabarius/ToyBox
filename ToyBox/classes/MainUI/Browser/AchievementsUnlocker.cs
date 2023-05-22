using Kingmaker;
using Kingmaker.Achievements;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModKit.UI;

namespace ToyBox {
    public class AchievementsUnlocker {
        public static Browser<AchievementEntity, AchievementEntity> AchievementBrowser = new(true, true);
        public static List<AchievementEntity> availableAchievements = new();
        public static List<AchievementEntity> unlocked = new();
        public static Settings Settings => Main.Settings;
        public static void OnGUI() {
            bool justInit = false;
            if (availableAchievements == null || availableAchievements?.Count == 0) {
                UI.Label("Achievements not available on the current platform or at your current progression in the game".localize().yellow().bold());
                availableAchievements = Game.Instance?.Player?
                    .Achievements?
                    .m_Achievements?
#if Wrath
                    .Where(ach => !ach.Data.ExcludedFromCurrentPlatform)
#elif RT
                    .Where(ach => !ach.Data.SteamId.IsNullOrEmpty())
#endif
                    .ToList();
                if (availableAchievements != null && availableAchievements?.Count > 0)
                    unlocked = availableAchievements.Where(ach => ach.IsUnlocked).ToList();
                justInit = true;
            }
            if (justInit) {
                if (Event.current.type == EventType.Repaint) {
                    justInit = false;
                }
                return;
            }
            AchievementBrowser.OnGUI(unlocked,
                () => availableAchievements,
                current => current,
#if Wrath
                achievement => $"{achievement.Data.AchievementName} {achievement.Data.GetDescription()} {achievement.Data.name}",
#elif RT
                achievement => $"{achievement.Data.SteamId} {achievement.Data.GetDescription()} {achievement.Data.name}",
#endif                
                achievement => new[] { achievement.Data.name },
                () => {
                    using (VerticalScope()) {
                        Toggle("Show GUIDs".localize(), ref Main.Settings.showAssetIDs);
                        Div(0, 25);
                    }
                },
                (achievement, maybeAchievement) => {
                    var remainingWidth = ummWidth;
                    // Indent
                    remainingWidth -= 50;
                    var titleWidth = (remainingWidth / (IsWide ? 3.5f : 4.0f)) - 100;
                    remainingWidth -= titleWidth;

                    var text = achievement.Data.name.MarkedSubstring(AchievementBrowser.SearchText);
                    if (Main.Settings.showAssetIDs) text += $" ({achievement.Data.AssetGuid})";
                    if (maybeAchievement != null) {
                        text = text.Cyan().Bold();
                    }
                    Label(text, Width((int)titleWidth));

                    if (maybeAchievement == null) {
                        ActionButton("Unlock".localize(), () => achievement.Unlock(), Width(116));
                        Space(70);
                    }
                    else {
                        Space(190);
                    }
                    remainingWidth -= 190;
                    Space(20); remainingWidth -= 20;
                    ReflectionTreeView.DetailToggle("", achievement, achievement, 0);
                },
                (achievement, maybeAchievement) => {
                    ReflectionTreeView.OnDetailGUI(achievement);
                }, 50, false);
        }
    }
}