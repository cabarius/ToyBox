// borrowed shamelessly and enhanced from Kingdom Resolution Mod
//   "Author": "spacehamster",
//   "HomePage": "https://www.nexusmods.com/pathfinderkingmaker/mods/36",
//   "Repository": "https://raw.githubusercontent.com/spacehamster/KingmakerKingdomResolutionMod/master/KingdomResolution/Repository.json"
// Copyright < 2018 > Spacehamster 
// Copyright < 2021 > Ported version - Narria (github user Cabarius) - License: MIT
using UnityEngine;
using HarmonyLib;
using System;
using System.Linq;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using ModKit;

namespace ToyBox {
    public static class QuestExensions {
        private static readonly string[] questColors = new string[] {
            "gray",
            "yellow",
            "green",
            "red"
        };
        public static string stateColored(this string text, Quest quest) => text.color(questColors[(int)quest.State]);
        public static string stateColored(this string text, QuestObjective objective) => text.color(questColors[(int)objective.State]);

        public static string stateString(this Quest quest) => quest.State == QuestState.None ? "" : $"{quest.State}".stateColored(quest);
        public static string stateString(this QuestObjective objective) => objective.State == QuestObjectiveState.None ? "" : $"{objective.State}".stateColored(objective);
    }
    public class QuestEditor {
        private static bool[] selectedQuests = new bool[0];
        public static void ResetGUI() { }

        public static void OnGUI() {
            if (!Main.IsInGame) return;
            var quests = Game.Instance?.Player?.QuestBook.Quests.ToArray();
            if (quests == null) return;
            UI.Toggle("Hide Completed", ref Main.settings.hideCompleted);
            GUILayout.Space(5f);
            selectedQuests = (selectedQuests.Length != quests.Length) ? new bool[quests.Length] : selectedQuests;
            var index = 0;
            var contentColor = GUI.contentColor;
            var split = quests.GroupBy(q => q.State == QuestState.Completed).OrderBy(g => g.Key);
            foreach (var group in split) {
                foreach (var quest in group.ToList()) {
                    if (Main.settings.hideCompleted && quest.State == QuestState.Completed && selectedQuests[index]) {
                        selectedQuests[index] = false;
                    }
                    if (!Main.settings.hideCompleted || quest.State != QuestState.Completed || selectedQuests[index]) {
                        using (UI.HorizontalScope()) {
                            UI.Space(50);
                            UI.Label(quest.Blueprint.Title, UI.Width(600));
                            UI.Space(50);
                            UI.DisclosureToggle(quest.stateString(), ref selectedQuests[index]);
                        }
                        if (selectedQuests[index]) {
                            foreach (var questObjective in quest.Objectives) {
                                if (questObjective.ParentObjective == null) {
                                    using (UI.HorizontalScope()) {
                                        UI.Space(50);
                                        UI.Label($"{questObjective.Order}", UI.Width(50));
                                        UI.Space(10);
                                        UI.Label(questObjective.Blueprint.Title, UI.Width(600));
                                        UI.Space(25);
                                        UI.Label(questObjective.stateString(), UI.Width(150));
                                        UI.Space(25);
                                        using (UI.HorizontalScope(300)) {
                                            UI.Space(0);
                                            if (questObjective.State == QuestObjectiveState.None && quest.State == QuestState.Started) {
                                                UI.ActionButton("Start", () => { questObjective.Start(); }, UI.Width(150));
                                            }
                                            else if (questObjective.State == QuestObjectiveState.Started) {
                                                UI.ActionButton(questObjective.Blueprint.IsFinishParent ? "Finish" : "Complete", () => {
                                                    questObjective.Complete();
                                                }, UI.Width(150));
                                                if (questObjective.Blueprint.AutoFailDays > 0) {
                                                    UI.ActionButton("Reset Time", () => {
                                                        Traverse.Create(questObjective).Field("m_ObjectiveStartTime").SetValue(Game.Instance.Player.GameTime);
                                                    }, UI.Width(150));
                                                }
                                            }
                                            else if (questObjective.State == QuestObjectiveState.Failed && (questObjective.Blueprint.IsFinishParent || quest.State == QuestState.Started)) {
                                                UI.ActionButton("Restart", () => {
                                                    if (quest.State == QuestState.Completed || quest.State == QuestState.Failed) {
                                                        Traverse.Create(quest).Field("m_State").SetValue(QuestState.Started);
                                                    }
                                                    questObjective.Reset();
                                                    questObjective.Start();
                                                }, UI.Width(150));
                                            }
                                        }
                                        DrawTeleports(questObjective);
                                    }
                                    if (questObjective.State == QuestObjectiveState.Started) {
                                        foreach (var childObjective in quest.Objectives) {
                                            if (childObjective.ParentObjective == questObjective) {
                                                using (UI.HorizontalScope()) { 
                                                    UI.Space(100);
                                                    UI.Label($"{childObjective.Order}", UI.Width(50));
                                                    UI.Space(10);
                                                    UI.Label(childObjective.Blueprint.Title, UI.Width(600));
                                                    UI.Space(25);
                                                    UI.Label(childObjective.stateString(), UI.Width(150));
                                                    UI.Space(25);
                                                    using (UI.HorizontalScope(300)) {
                                                        if (childObjective.State == QuestObjectiveState.None) {
                                                            UI.ActionButton("Start", () => { childObjective.Start(); }, UI.Width(150));
                                                        }
                                                        else if (childObjective.State == QuestObjectiveState.Started) {
                                                            UI.ActionButton(childObjective.Blueprint.IsFinishParent ? "Complete (Final)" : "Complete", () => {
                                                                childObjective.Complete();
                                                            }, UI.Width(150));
                                                        }
                                                    }
                                                    DrawTeleports(childObjective);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    index++;
                }
            }
            UI.Space(25);
        }
        public static void DrawTeleports(QuestObjective objective) {
            var areas = objective.Blueprint.Areas;
            var locations = objective.Blueprint.Locations;
            if (areas.Count > 0 || locations.Count > 0) {
                UI.Label("Teleport");
                if (locations.Count > 0) {
                    UI.Space(25);
                    foreach (var location in locations) {
                        var bp = location.GetBlueprint();
                        if (bp != null)
                            UI.ActionButton(bp.name.yellow(), () => Teleport.To(location));
                    }
                }
#if DEBUG
                if (areas.Count > 0) {
                    UI.Label(" Area");
                    UI.Space(25);
                    foreach (var area in areas) {
                        UI.ActionButton(area.name.yellow(), () => Teleport.To(area));
                    }
                }
#endif
            }
        }
    }
}
