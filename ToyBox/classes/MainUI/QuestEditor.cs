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
using static ModKit.UI;

namespace ToyBox {
    public static class QuestExensions {
        private static readonly RGBA[] titleColors = new RGBA[] {
            RGBA.brown,
            RGBA.cyan,
            RGBA.darkgrey,
            RGBA.red
        };
        private static readonly string[] questColors = new string[] {
            "gray",
            "cyan",
            "white",
            "red"
        };
        public static string stateColored(this string text, Quest quest) => text.color(questColors[(int)quest.State]);
        public static string stateColored(this string text, QuestObjective objective) => text.color(questColors[(int)objective.State]);
        public static string titleColored(this Quest quest) => quest.Blueprint.Title.ToString().color(titleColors[(int)quest.State]);
        public static string titleColored(this QuestObjective quest) => quest.Blueprint.Title.ToString().color(titleColors[(int)quest.State]);

        public static string stateString(this Quest quest) => quest.State == QuestState.None ? "" : $"{quest.State}".stateColored(quest).bold();
        public static string stateString(this QuestObjective objective) => objective.State == QuestObjectiveState.None ? "" : $"{objective.State}".stateColored(objective).bold();
    }
    public class QuestEditor {
        private static bool[] selectedQuests = new bool[0];
        public static void ResetGUI() { }

        public static void OnGUI() {
            if (!Main.IsInGame) return;
            var quests = Game.Instance?.Player?.QuestBook.Quests.ToArray();
            if (quests == null) return;
            Toggle("Hide Completed", ref Main.settings.hideCompleted);
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
                        using (HorizontalScope()) {
                            50.space();
                            Label(quest.Blueprint.Title.ToString().orange().bold(), Width(600));
                            50.space();
                            DisclosureToggle(quest.stateString(), ref selectedQuests[index]);
                            50.space();
                            UI.Label(quest.Blueprint.Description.ToString().StripHTML().green());
                        }
                        if (selectedQuests[index]) {
                            foreach (var questObjective in quest.Objectives) {
                                if (questObjective.ParentObjective == null) {
                                    Div(100, 25);
                                    using (HorizontalScope()) {
                                        Space(50);
                                        Label($"{questObjective.Order}", Width(50));
                                        Label(questObjective.titleColored(), Width(600));
                                        Space(25);
                                        Label(questObjective.stateString(), Width(150));
                                        Space(25);
                                        using (HorizontalScope(300)) {
                                            Space(0);
                                            if (questObjective.State == QuestObjectiveState.None && quest.State == QuestState.Started) {
                                                ActionButton("Start", () => { questObjective.Start(); }, Width(150));
                                            }
                                            else if (questObjective.State == QuestObjectiveState.Started) {
                                                ActionButton(questObjective.Blueprint.IsFinishParent ? "Finish" : "Complete", () => {
                                                    questObjective.Complete();
                                                }, Width(150));
                                                if (questObjective.Blueprint.AutoFailDays > 0) {
                                                    ActionButton("Reset Time", () => {
                                                        Traverse.Create(questObjective).Field("m_ObjectiveStartTime").SetValue(Game.Instance.Player.GameTime);
                                                    }, Width(150));
                                                }
                                            }
                                            else if (questObjective.State == QuestObjectiveState.Failed && (questObjective.Blueprint.IsFinishParent || quest.State == QuestState.Started)) {
                                                ActionButton("Restart", () => {
                                                    if (quest.State == QuestState.Completed || quest.State == QuestState.Failed) {
                                                        Traverse.Create(quest).Field("m_State").SetValue(QuestState.Started);
                                                    }
                                                    questObjective.Reset();
                                                    questObjective.Start();
                                                }, Width(50));
                                            }
                                        }
                                        DrawTeleports(questObjective);
                                        50.space();
                                        UI.Label(questObjective.Blueprint.Description.ToString().StripHTML().green(), 1000.width());
                                    }
                                    if (questObjective.State == QuestObjectiveState.Started) {
                                        foreach (var childObjective in quest.Objectives) {
                                            if (childObjective.ParentObjective == questObjective) {
                                                Div(100, 25);
                                                using (HorizontalScope()) {
                                                    Space(100);
                                                    Label($"{childObjective.Order}", Width(50));
                                                    Space(10);
                                                    Label(childObjective.titleColored(), Width(600));
                                                    Space(25);
                                                    Label(childObjective.stateString(), Width(150));
                                                    Space(25);
                                                    using (HorizontalScope(300)) {
                                                        if (childObjective.State == QuestObjectiveState.None) {
                                                            ActionButton("Start", () => { childObjective.Start(); }, Width(150));
                                                        }
                                                        else if (childObjective.State == QuestObjectiveState.Started) {
                                                            ActionButton(childObjective.Blueprint.IsFinishParent ? "Complete (Final)" : "Complete", () => {
                                                                childObjective.Complete();
                                                            }, Width(150));
                                                        }
                                                    }
                                                    DrawTeleports(childObjective);
                                                    50.space();
                                                    UI.Label(questObjective.Blueprint.Description.ToString().StripHTML().green(), 1000.width());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Div(50, 25);
                    }
                    index++;
                }
            }
            Space(25);
        }
        public static void DrawTeleports(QuestObjective objective) {
            var areas = objective.Blueprint.Areas;
            var locations = objective.Blueprint.Locations;
            if (areas.Count > 0 || locations.Count > 0) {
                Label("Teleport");
                if (locations.Count > 0) {
                    Space(25);
                    foreach (var location in locations) {
                        var bp = location.GetBlueprint();
                        if (bp != null)
                            ActionButton(bp.name.yellow(), () => Teleport.To(location));
                    }
                }
#if DEBUG
                if (areas.Count > 0) {
                    Label(" Area");
                    Space(25);
                    foreach (var area in areas) {
                        ActionButton(area.name.yellow(), () => Teleport.To(area));
                    }
                }
#endif
            }
        }
    }
}
