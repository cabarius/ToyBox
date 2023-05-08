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
using Kingmaker.EntitySystem.Entities;
using ModKit;
using static ModKit.UI;
using ModKit.DataViewer;
using System.Collections.Generic;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.UnitLogic.Parts;
using ModKit.Utility;
using static Kingmaker.UnitLogic.Interaction.SpawnerInteractionPart;
using static ToyBox.BlueprintExtensions;
using Kingmaker.Designers;
using Kingmaker.ElementsSystem;

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

        public static bool IsRevealed(this QuestObjective objective) => objective.State == QuestObjectiveState.Started || objective.State == QuestObjectiveState.Completed;
        public static string stateColored(this string text, Quest quest) => RichText.color(text, questColors[(int)quest.State]);
        public static string stateColored(this string text, QuestObjective objective) => RichText.color(text, questColors[(int)objective.State]);
        public static string titleColored(this Quest quest) => quest.Blueprint.Title.ToString().color(titleColors[(int)quest.State]);
        public static string titleColored(this QuestObjective objective, BlueprintQuestObjective bp = null) {
            var blueprint = objective?.Blueprint ?? bp;
            var state = objective?.State ?? QuestObjectiveState.None;
            var title = blueprint.Title.ToString();
            if (title.Length == 0) title = blueprint.ToString();
            if (blueprint.IsAddendum)
                title = "Addendum: ".color(RGBA.white) + title;
            if (blueprint.name.Contains("_Fail"))
                return title.red();
            else
                return title.color(titleColors[(int)state]);
        }
        public static string titleColored(this string title, QuestObjectiveState state) => title.color(titleColors[(int)state]);
        public static string stateString(this Quest quest) => quest.State == QuestState.None ? "" : $"{quest.State}".stateColored(quest).bold();
        public static string stateString(this QuestObjective objective) => objective.State == QuestObjectiveState.None ? "" : $"{objective.State}".stateColored(objective).bold();
    }
    public class QuestEditor {
        public static Settings settings => Main.Settings;
        public static Player player => Game.Instance.Player;
        private static bool[] selectedQuests = new bool[0];
        private static Browser<UnitEntityData, UnitEntityData> conditionsBrowser = new();

        public static void ResetGUI() { }

        public static void OnGUI() {
            if (!Main.IsInGame) return;
            var quests = Game.Instance?.Player?.QuestBook.Quests.ToArray();
            if (quests == null) return;
            selectedQuests = (selectedQuests.Length != quests.Length) ? new bool[quests.Length] : selectedQuests;
            var index = 0;
            var contentColor = GUI.contentColor;
            Div();
            10.space();
            using (HorizontalScope()) {
                Toggle("Mark Interesting NPCs on Map", ref settings.toggleShowInterestingNPCsOnLocalMap, 375.width());
                HelpLabel("This will change the color of NPC names on the highlight makers and change the color map markers to indicate that they have interesting or conditional interactions");
            }
            using (HorizontalScope()) {
                DisclosureToggle("Interesting NPCs in the local area".cyan(), ref settings.toogleShowInterestingNPCsOnQuestTab);
                200.space();
                HelpLabel("Show a list of NPCs that may have quest objectives or other interesting features " + "(Warning: Spoilers)".yellow());
            }
            if (settings.toogleShowInterestingNPCsOnQuestTab) {
                using (HorizontalScope()) {
                    50.space();
                    using (VerticalScope(GUI.skin.box)) {
                        if (Game.Instance?.State?.Units.All is { } units) {
                            conditionsBrowser.OnGUI(
                                    units.Where(u => u.GetUnitIterestingnessCoefficent() >= 1),
                                    () => units,
                                    i => i,
                                    u => u.CharacterName,
                                    u => u.CharacterName,
                                    () => {
                                        Toggle("Show Inactive Conditions", ref settings.toggleIntrestingNPCsShowFalseConditions);
                                    },
                                    (_, u) => {
                                        var name = u.CharacterName;
                                        var coefficient = u.GetUnitIterestingnessCoefficent();
                                        if (coefficient > 0)
                                            name = name.orange();
                                        else
                                            name = name.grey();
                                        Label(name, 600.width());
                                        175.space();
                                        Label($"Interestingness Coefficient: ".grey() + RichTextExtensions.Cyan(coefficient.ToString()));
                                        50.space();
                                        ReflectionTreeView.DetailToggle("", u.Parts.Parts);
                                    },
                                    (_, u) => {
                                        ReflectionTreeView.OnDetailGUI(u.Parts.Parts);
                                        var entries = u.GetUnitInteractionConditions();
                                        var checkerEntries = entries.Where(e => e.checker?.Conditions.Length > 0);
                                        var elementEntries = entries.Where(e => e.elements?.Count > 0);
                                        if (checkerEntries.Any()) {
                                            using (HorizontalScope()) {
                                                115.space();
                                                Label("Conditions".yellow());
                                            }
                                        }
                                        foreach (var entry in checkerEntries) {
                                            foreach (var condition in entry.checker.Conditions.OrderBy(c => c.GetType().Name)) {
                                                if (!condition.CheckCondition()
                                                    && !settings.toggleIntrestingNPCsShowFalseConditions
                                                   ) continue;
                                                using (HorizontalScope()) {
                                                    150.space();
                                                    switch (condition) {
                                                        case ObjectiveStatus objectiveStatus:
                                                            OnGUI(objectiveStatus, entry.source);
                                                            break;
                                                        case EtudeStatus etudeStatus:
                                                            OnGUI(etudeStatus);
                                                            break;
                                                        default:
                                                            OnGUI(condition);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                        if (elementEntries.Any()) {
                                            using (HorizontalScope()) {
                                                115.space();
                                                Label("Elements".yellow());
                                            }
                                        }
                                        foreach (var entry in elementEntries) {
                                            foreach (var element in entry.elements.OrderBy(e => e.GetType().Name)) {
                                                if (element is Condition condition
                                                    && !condition.CheckCondition()
                                                    && !settings.toggleIntrestingNPCsShowFalseConditions
                                                    ) continue;
                                                using (HorizontalScope()) {
                                                    150.space();
                                                    switch (element) {
                                                        case ObjectiveStatus objectiveStatus:
                                                            OnGUI(objectiveStatus, entry.source);
                                                            break;
                                                        case EtudeStatus etudeStatus:
                                                            OnGUI(etudeStatus);
                                                            break;
                                                        default:
                                                            OnGUI(element);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }, 50, false, true, 100, 300, "", true);
                        }
                    }
                }
            }
            Div(0,25);
            using (HorizontalScope()) {
                Label("Quests".cyan());
            }
            var split = quests.GroupBy(q => q.State == QuestState.Completed).OrderBy(g => g.Key);
            using (HorizontalScope()) {
                Toggle("Hide Completed", ref settings.toggleQuestHideCompleted);
                25.space();
                Toggle("Show Unrevealed Steps", ref settings.toggleQuestsShowUnrevealedObjectives);
                25.space();
                Toggle("Inspect Quests and Objectives", ref settings.toggleQuestInspector);
                if (settings.toggleQuestInspector) {
                    25.space();
                    ReflectionTreeView.DetailToggle("Inspect", selectedQuests, split, 0);
                }
            }
            if (settings.toggleQuestInspector) {
                ReflectionTreeView.OnDetailGUI(selectedQuests);
            }
            foreach (var group in split) {
                foreach (var quest in group.ToList()) {
                    if (settings.toggleQuestHideCompleted && quest.State == QuestState.Completed && selectedQuests[index]) {
                        selectedQuests[index] = false;
                    }
                    if (!settings.toggleQuestHideCompleted || quest.State != QuestState.Completed || selectedQuests[index]) {
                        using (HorizontalScope()) {
                            50.space();
                            Label(quest.Blueprint.Title.ToString().orange().bold(), Width(600));
                            50.space();
                            DisclosureToggle(quest.stateString(), ref selectedQuests[index]); 
                            if (settings.toggleQuestInspector)
                                ReflectionTreeView.DetailToggle("Inspect", quest, quest, 0);
                            50.space();
                            Label(quest.Blueprint.Description.ToString().StripHTML().green());
                        }
                        if (settings.toggleQuestInspector) {
                            ReflectionTreeView.OnDetailGUI(quest);
                        }
                        if (selectedQuests[index]) {
                            var objectiveIndex = 0;
                            foreach (var questObjective in quest.Objectives) {
                                if (settings.toggleQuestsShowUnrevealedObjectives || questObjective.IsRevealed()) {
                                    if (questObjective.ParentObjective == null) {
                                        Div(100, 25);
                                        using (HorizontalScope(AutoWidth())) {
                                            Space(50);
                                            objectiveIndex = questObjective.Order == 0 ? objectiveIndex + 1 : questObjective.Order;
                                            Label($"{objectiveIndex}", Width(50));
                                            Label(questObjective.titleColored(), Width(600));
                                            25.space();
                                            Label(questObjective.stateString(), Width(150));
                                            if (settings.toggleQuestInspector)
                                                ReflectionTreeView.DetailToggle("Inspect", questObjective, questObjective, 0);
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
                                            Label(questObjective.Blueprint.Description.ToString().StripHTML().green(), 1000.width());
                                            Label("", AutoWidth());
                                        }
                                        if (settings.toggleQuestInspector) {
                                            ReflectionTreeView.OnDetailGUI(questObjective);
                                        }
                                        if (questObjective.State == QuestObjectiveState.Started) {
                                            var childIndex = 0;
                                            foreach (var childObjective in quest.Objectives) {
                                                if (settings.toggleQuestsShowUnrevealedObjectives || childObjective.IsRevealed()) {
                                                    if (childObjective.ParentObjective == questObjective) {
                                                        Div(100, 25);
                                                        using (HorizontalScope(AutoWidth())) {
                                                            Space(100);
                                                            childIndex = childObjective.Order == 0 ? childIndex + 1 : questObjective.Order;
                                                            Label($"{childIndex}", Width(50));
                                                            Space(10);
                                                            Label(childObjective.titleColored(), Width(600));
                                                            25.space();
                                                            Label(childObjective.stateString(), Width(150));
                                                            if (settings.toggleQuestInspector)
                                                                ReflectionTreeView.DetailToggle("Inspect", questObjective, questObjective, 0);
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
                                                                else 153.space();
                                                            }
                                                            DrawTeleports(childObjective);
                                                            Label(childObjective.Blueprint.Description.ToString().StripHTML().green(), 1000.width());
                                                            Label("", AutoWidth());
                                                        }
                                                        if (settings.toggleQuestInspector) {
                                                            ReflectionTreeView.OnDetailGUI(childObjective);
                                                        }
                                                    }
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
            using (HorizontalScope(MaxWidth(850))) {
                var areas = objective.Blueprint.Areas;
                var locations = objective.Blueprint.Locations;
                if (areas.Count > 0 || locations.Count > 0) {
                    if (locations.Count > 0) {
                        Label("TP");
                        25.space();
                        using (VerticalScope(MaxWidth(600))) {
                            foreach (var location in locations) {
                                var bp = location.GetBlueprint();
                                if (bp != null)
                                    ActionButton(bp.name.yellow(), () => Teleport.To(location));
                            }
                        }
                    }
#if false
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
        public static void OnGUI(ObjectiveStatus objectiveStatus, object source) {
            Label("Quest: ".cyan(), 150.width());

            var objectiveBP = objectiveStatus.QuestObjective;
            var objective = Game.Instance.Player.QuestBook.GetObjective(objectiveBP);
            var quest = objectiveBP.Quest;
            var state = objective?.State ?? QuestObjectiveState.None;
            var title = $"{quest.Title.ToString().orange().bold()} : {objective.titleColored(objectiveBP)}";
            Label(title, 500.width());
            22.space();
            using (VerticalScope()) {
                HelpLabel(objectiveBP.Description);
                Label($"status: ".cyan() + state.ToString().titleColored(state));
                Label("condition: ".cyan() + objectiveStatus.CaptionString());
                Label("source: ".cyan() + source.ToString().yellow());
            }
        }
        public static void OnGUI(EtudeStatus etudeStatus) {
            Label("Etude: ".cyan(), 150.width());
            var etudeBP = etudeStatus.Etude;
            Label(etudeBP.name.orange(), 500.width());
            var etudeState = Game.Instance.Player.EtudesSystem.GetSavedState(etudeBP);
            var debugInfo = Game.Instance.Player.EtudesSystem.GetDebugInfo(etudeBP);
            22.space();
            using (VerticalScope()) {
                HelpLabel(debugInfo);
                Label($"status: ".cyan() + etudeState.ToString());
                Label("condition: ".cyan() + etudeStatus.CaptionString());
            }
        }
        public static void OnGUI(Condition condition) {
            Label($"{condition.GetType().Name}:".cyan(), 150.width());
            Label(condition.CaptionString(), 500.width());
            22.space();
            using (VerticalScope()) {
                Label("condition: ".cyan() + condition.CaptionString());
            }
        }
        public static void OnGUI(Element element) {
            Label($"{element.GetType().Name}:".cyan(), 150.width());
            Label(element.CaptionString(), 500.width());
        }
    }
}
