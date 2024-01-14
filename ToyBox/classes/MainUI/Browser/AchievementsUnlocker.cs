using Kingmaker;
using Kingmaker.Achievements;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox {
    public class AchievementsUnlocker {
        public static Browser<AchievementEntity, AchievementEntity> AchievementBrowser = new(true);
        public static List<AchievementEntity> availableAchievements = new();
        public static List<AchievementEntity> unlocked = new();
        public static Settings Settings => Main.Settings;
        public static void OnShowGUI() {
            try {
                justInit = true;
                availableAchievements.Clear();
                unlocked.Clear();
            } catch (Exception ex) {
                Mod.Debug(ex.ToString());
            }
        }
        //TODO: Check in RT release version whether there is a good heuristic to check if an achievement is blocked on the platform
        private static bool justInit = false;
        public static void OnGUI() {
            if (availableAchievements == null || availableAchievements?.Count == 0 || justInit) {
                UI.Label("Achievements not available until you load a save.".localize().yellow().bold());
                availableAchievements = Game.Instance?.Player?
                    .Achievements?
                    .m_Achievements?
                    .Where(ach => !ach.Data.SteamId.IsNullOrEmpty())
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
                achievement => $"{achievement.Data.SteamId} {achievement.Data.GetDescription()} {achievement.Data.name}",
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
                        ActionButton("Unlock".localize(), () => {
                            if (!achievement.IsUnlocked) {
                                achievement.IsUnlocked = true;
                                achievement.NeedCommit = true;
                                achievement.Manager.OnAchievementUnlocked(achievement);
                            }
                        }, Width(116));
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