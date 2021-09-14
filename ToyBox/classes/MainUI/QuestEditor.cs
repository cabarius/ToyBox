// borrowed shamelessly and enhanced from Kingdom Resolution Mod
//   "Author": "spacehamster",
//   "HomePage": "https://www.nexusmods.com/pathfinderkingmaker/mods/36",
//   "Repository": "https://raw.githubusercontent.com/spacehamster/KingmakerKingdomResolutionMod/master/KingdomResolution/Repository.json"
// Copyright < 2018 > Spacehamster 
// Copyright < 2021 > Ported version - Narria (github user Cabarius) - License: MIT

using HarmonyLib;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using ModKit;
using System;
using System.Linq;
using UnityEngine;

namespace ToyBox {
    public static class QuestExensions {
        private static String[] questColors = {
            "gray",
            "yellow",
            "green",
            "red"
        };
        public static String stateColored(this String text, Quest quest) { return text.color(questColors[(int)quest.State]); }
        public static String stateColored(this String text, QuestObjective objective) { return text.color(questColors[(int)objective.State]); }

        public static String stateString(this Quest quest) { return quest.State == QuestState.None ? "" : $"{quest.State}".stateColored(quest); }
        public static String stateString(this QuestObjective objective) {
            return objective.State == QuestObjectiveState.None ? "" : $"{objective.State}".stateColored(objective);
        }
    }
    public class QuestEditor {
        static bool[] selectedQuests = new bool[0];
        public static void ResetGUI() { }

        public static void OnGUI() {
            UI.Toggle("Hide Completed", ref Main.settings.hideCompleted);
            GUILayout.Space(5f);
            Quest[] quests = Game.Instance.Player.QuestBook.Quests.ToArray();
            selectedQuests = ((selectedQuests.Length != quests.Length) ? new bool[quests.Length] : selectedQuests);
            int index = 0;
            Color contentColor = GUI.contentColor;
            var split = quests.GroupBy(q => q.State == QuestState.Completed).OrderBy(g => g.Key);
            foreach (var group in split) {
                foreach (var quest in group.ToList()) {
                    if (Main.settings.hideCompleted && quest.State == QuestState.Completed && selectedQuests[index]) {
                        selectedQuests[index] = false;
                    }
                    if (!Main.settings.hideCompleted || quest.State != QuestState.Completed || selectedQuests[index]) {
                        UI.HStack(null, 0, () => {
                            UI.Space(50);
                            UI.Label(quest.Blueprint.Title, UI.Width(600));
                            UI.Space(50);
                            UI.DisclosureToggle(quest.stateString(), ref selectedQuests[index]);
                        });
                        if (selectedQuests[index]) {
                            foreach (QuestObjective questObjective in quest.Objectives) {
                                if (questObjective.ParentObjective == null) {
                                    UI.HStack(null, 0, () => {
                                        UI.Space(50);
                                        UI.Label($"{questObjective.Order}", UI.Width(50));
                                        UI.Space(10);
                                        UI.Label(questObjective.Blueprint.Title, UI.Width(600));
                                        UI.Space(25);
                                        UI.Label(questObjective.stateString(), UI.Width(150));
                                        if (questObjective.State == QuestObjectiveState.None && quest.State == QuestState.Started) {
                                            UI.ActionButton("Start", () => { questObjective.Start(); }, UI.AutoWidth());
                                        }
                                        else if (questObjective.State == QuestObjectiveState.Started) {
                                            UI.ActionButton(questObjective.Blueprint.IsFinishParent ? "Complete (Final)" : "Complete", () => {
                                                questObjective.Complete();
                                            }, UI.AutoWidth());
                                            if (questObjective.Blueprint.AutoFailDays > 0) {
                                                UI.ActionButton("Reset Time", () => {
                                                    Traverse.Create(questObjective).Field("m_ObjectiveStartTime").SetValue(Game.Instance.Player.GameTime);
                                                }, UI.AutoWidth());
                                            }
                                        }
                                        else if (questObjective.State == QuestObjectiveState.Failed && (questObjective.Blueprint.IsFinishParent || quest.State == QuestState.Started)) {
                                            UI.ActionButton("Restart", () => {
                                                if (quest.State == QuestState.Completed || quest.State == QuestState.Failed) {
                                                    Traverse.Create(quest).Field("m_State").SetValue(QuestState.Started);
                                                }
                                                questObjective.Reset();
                                                questObjective.Start();
                                            }, UI.AutoWidth());
                                        }
                                    });
                                    if (questObjective.State == QuestObjectiveState.Started) {
                                        foreach (QuestObjective childObjective in quest.Objectives) {
                                            if (childObjective.ParentObjective == questObjective) {
                                                UI.HStack(null, 0, () => {
                                                    UI.Space(100);
                                                    UI.Label($"{childObjective.Order}", UI.Width(50));
                                                    UI.Space(10);
                                                    UI.Label(childObjective.Blueprint.Title, UI.Width(600));
                                                    UI.Space(25);
                                                    UI.Label(childObjective.stateString(), UI.Width(150));
                                                    if (childObjective.State == QuestObjectiveState.None) {
                                                        UI.ActionButton("Start", () => { childObjective.Start(); }, UI.AutoWidth());
                                                    }
                                                    else if (childObjective.State == QuestObjectiveState.Started) {
                                                        UI.ActionButton(childObjective.Blueprint.IsFinishParent ? "Complete (Final)" : "Complete", () => {
                                                            childObjective.Complete();
                                                        }, UI.AutoWidth());

                                                    }
                                                });
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
    }
}
