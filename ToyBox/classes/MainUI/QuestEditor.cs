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
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using System.Security.AccessControl;

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
#if Wrath
        public static string titleColored(this Quest quest) => quest.Blueprint.Title.ToString().color(titleColors[(int)quest.State]);
#elif RT
        public static string titleColored(this Quest quest) => quest.Blueprint.Title.Text.color(titleColors[(int)quest.State]);
#endif
        public static string titleColored(this QuestObjective objective, BlueprintQuestObjective bp = null) {
            var blueprint = objective?.Blueprint ?? bp;
            var state = objective?.State ?? QuestObjectiveState.None;
#if Wrath
            var title = blueprint.Title.ToString();
#elif RT
            var title = blueprint.Title.Text;
#endif            
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
        public static Settings Settings => Main.Settings;
        public static bool ShowInactive => Settings.toggleIntrestingNPCsShowFalseConditions;
        public static Player player => Game.Instance.Player;
        private static bool[] selectedQuests = new bool[0];
        private static readonly Browser<UnitEntityData, UnitEntityData> ConditionsBrowser = new();

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
                Toggle("Mark Interesting NPCs on Map", ref Settings.toggleShowInterestingNPCsOnLocalMap, 375.width());
                HelpLabel("This will change the color of NPC names on the highlight makers and change the color map markers to indicate that they have interesting or conditional interactions");
            }
            using (HorizontalScope()) {
                DisclosureToggle("Interesting NPCs in the local area".cyan(), ref Settings.toogleShowInterestingNPCsOnQuestTab);
                200.space();
                HelpLabel("Show a list of NPCs that may have quest objectives or other interesting features " + "(Warning: Spoilers)".yellow());
            }
            if (Settings.toogleShowInterestingNPCsOnQuestTab) {
                using (HorizontalScope()) {
                    50.space();
                    using (VerticalScope(GUI.skin.box)) {
                        //if (Game.Instance?.State?.Units.All is { } units) {
#if Wrath
                        if (Game.Instance?.State?.Units is { } unitsPool) {
#elif RT
                        if (Game.Instance?.State?.AllUnits is { } unitsPool) {
#endif
                            var units = Settings.toggleInterestingNPCsShowHidden ? unitsPool.All : unitsPool.ToList();
                            ConditionsBrowser.OnGUI(
                                units.Where(u => u.InterestingnessCoefficent() >= 1),
                                () => units,
                                i => i,
                                u => u.CharacterName,
                                u => u.CharacterName,
                                () => {
                                    Toggle("Show Inactive Conditions", ref Settings.toggleIntrestingNPCsShowFalseConditions);
                                    if (ConditionsBrowser.ShowAll) {
                                        25.space();
                                        if (Toggle("Show other versions of NPCs", ref Settings.toggleInterestingNPCsShowHidden))
                                            ConditionsBrowser.ReloadData();
                                    }
#if DEBUG
                                    25.space();
                                    ActionButton("Reveal All On Map", RevealInterestingNPCs);
#endif
                                },
                                (u, _) => {
                                    var name = u.CharacterName;
                                    var coefficient = u.InterestingnessCoefficent();
                                    if (coefficient > 0)
                                        name = name.orange();
                                    else
                                        name = name.grey();
                                    Label(name, 600.width());
                                    175.space();
                                    Label($"Interestingness Coefficient: ".grey() + RichTextExtensions.Cyan(coefficient.ToString()));
                                    50.space();
#if Wrath
                                    ReflectionTreeView.DetailToggle("Unit", u.Parts.Parts, u.Parts.Parts,100);
#elif RT
                                    ReflectionTreeView.DetailToggle("Unit", u.Parts.m_Parts, u.Parts.m_Parts,100);
#endif
                                    25.space();
                                    var dialogs = u.GetDialog();
                                    if (dialogs.Any()) 
                                        ReflectionTreeView.DetailToggle("Dialog", u, dialogs.Count == 1 ? dialogs.First() : dialogs, 100);
                                },
                                (u, _) => {
#if Wrath
                                    ReflectionTreeView.OnDetailGUI(u.Parts.Parts);
#elif RT
                                    ReflectionTreeView.OnDetailGUI(u.Parts.m_Parts);
#endif
                                    ReflectionTreeView.OnDetailGUI(u);
                                    var entries = u.GetUnitInteractionConditions();
                                    var checkerEntries = entries.Where(e => e.HasConditins && (ShowInactive || e.IsActive()));
                                    var conditions =
                                        from entry in checkerEntries
                                        from condition in entry.checker.Conditions
                                        group (condition, entry) by condition.GetCaption()
                                        into g
                                        select g.Select(p => (p.condition, new object[] { p.entry.source } as IEnumerable<object>))
                                                .Aggregate((p, q)
                                                               => (p.condition, p.Item2.Concat(q.Item2))
                                                    );
                                    var elementEntries = entries.Where(e => e.HasElements && (ShowInactive || e.IsActive()));
                                    if (conditions.Any()) {
                                        using (HorizontalScope()) {
                                            115.space();
                                            Label("Conditions".yellow());
                                        }
                                    }
                                    foreach (var entry in conditions) {
                                        OnGUI(entry.condition,
                                              string.Join(", ", entry.Item2.Select(source => source.ToString())),
                                              150
                                            );
                                    }
                                    if (elementEntries.Any()) {
                                        using (HorizontalScope()) {
                                            115.space();
                                            Label("Elements".yellow());
                                        }
                                    }
                                    foreach (var entry in elementEntries) {
                                        foreach (var element in entry.elements.OrderBy(e => e.GetType().Name)) {
                                            OnGUI(element, entry.source);
                                        }
                                    }
                                },
                                50,
                                false,
                                true,
                                100,
                                300,
                                "",
                                true);
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
                Toggle("Hide Completed", ref Settings.toggleQuestHideCompleted);
                25.space();
                Toggle("Show Unrevealed Steps", ref Settings.toggleQuestsShowUnrevealedObjectives);
                25.space();
                Toggle("Inspect Quests and Objectives", ref Settings.toggleQuestInspector);
                if (Settings.toggleQuestInspector) {
                    25.space();
                    ReflectionTreeView.DetailToggle("Inspect", selectedQuests, split, 0);
                }
            }
            if (Settings.toggleQuestInspector) {
                ReflectionTreeView.OnDetailGUI(selectedQuests);
            }
            foreach (var group in split) {
                foreach (var quest in group.ToList()) {
                    if (Settings.toggleQuestHideCompleted && quest.State == QuestState.Completed && selectedQuests[index]) {
                        selectedQuests[index] = false;
                    }
                    if (!Settings.toggleQuestHideCompleted || quest.State != QuestState.Completed || selectedQuests[index]) {
                        using (HorizontalScope()) {
                            50.space();
#if Wrath
                            Label(quest.Blueprint.Title.ToString().orange().bold(), Width(600));
#elif RT
                            Label(quest.Blueprint.Title.Text.orange().bold(), Width(600));
#endif
                            50.space();
                            DisclosureToggle(quest.stateString(), ref selectedQuests[index]); 
                            if (Settings.toggleQuestInspector)
                                ReflectionTreeView.DetailToggle("Inspect", quest, quest, 0);
                            50.space();
                            Label(quest.Blueprint.Description.ToString().StripHTML().green());
                        }
                        if (Settings.toggleQuestInspector) {
                            ReflectionTreeView.OnDetailGUI(quest);
                        }
                        if (selectedQuests[index]) {
                            var objectiveIndex = 0;
                            foreach (var questObjective in quest.Objectives) {
                                if (Settings.toggleQuestsShowUnrevealedObjectives || questObjective.IsRevealed()) {
                                    if (questObjective.ParentObjective == null) {
                                        Div(100, 25);
                                        using (HorizontalScope(AutoWidth())) {
                                            Space(50);
                                            objectiveIndex = questObjective.Order == 0 ? objectiveIndex + 1 : questObjective.Order;
                                            Label($"{objectiveIndex}", Width(50));
                                            Label(questObjective.titleColored(), Width(600));
                                            25.space();
                                            Label(questObjective.stateString(), Width(150));
                                            if (Settings.toggleQuestInspector)
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
                                        if (Settings.toggleQuestInspector) {
                                            ReflectionTreeView.OnDetailGUI(questObjective);
                                        }
                                        if (questObjective.State == QuestObjectiveState.Started) {
                                            var childIndex = 0;
                                            foreach (var childObjective in quest.Objectives) {
                                                if (Settings.toggleQuestsShowUnrevealedObjectives || childObjective.IsRevealed()) {
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
                                                            if (Settings.toggleQuestInspector)
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
                                                        if (Settings.toggleQuestInspector) {
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
#if Wrath
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
#endif
        }
        public static void OnGUI(Element element, object source, int indent = 150, bool forceShow = false) {
            if (!element.IsActive()
                && source is not ActionsHolder // kludge again for Actions holder for Lathimas
                && !Settings.toggleIntrestingNPCsShowFalseConditions
                && !forceShow
               ) return;
            using (HorizontalScope()) {
                Space(indent);
                switch (element) {
                    case ObjectiveStatus objectiveStatus:
                        OnGUI(objectiveStatus, source);
                        break;
                    case QuestStatus questStatus:
                        OnGUI(questStatus, source);
                        break;
                    case EtudeStatus etudeStatus:
                        OnGUI(etudeStatus, source);
                        break;
                    case Conditional conditional:
                        OnGUI(conditional, source);
                        break;
                    case Condition condition:
                        OnGUI(condition, source);
                        break;
                    default:
                        OnOtherElementGUI(element, source);
                        break;
                }
            }
        }
        public static void OnGUI(ConditionsChecker checker, object source, int indent = 150, bool forceShow = false) {
            foreach (var condition in checker.Conditions.OrderBy(c => c.GetType().Name)) {
                OnGUI(condition, source, indent, forceShow);
            }
        }
        public static void OnGUI(Conditional conditional, object source) {
            if (conditional.ConditionsChecker.Conditions.Any()) {
                Label("Conditional:".cyan(), 150.width());
                //Label(string.Join(", ", conditional.ConditionsChecker.Conditions.Select(c => c.GetCaption())));
                Label(conditional.Comment, 375.width());
                using (VerticalScope()) {
                    OnGUI(conditional.ConditionsChecker, source, 0, true);
                }
            }
        }
        public static void OnGUI(QuestStatus questStatus, object source) {
            Label("Quest Status: ".cyan(), 150.width());
            var quest = questStatus.Quest;
            var state = GameHelper.Quests.GetQuestState(quest);
            var title = $"{quest.Title.ToString().orange().bold()}";
            Label(title, 500.width());
            22.space();
            using (VerticalScope()) {
                HelpLabel(quest.Description);
                Label($"status: ".cyan() + state.ToString());
                Label("condition: ".cyan() + questStatus.CaptionString());
                Label("source: ".cyan() + source.ToString().yellow());
            }
        }
        public static void OnGUI(ObjectiveStatus objectiveStatus, object source) {
            Label("Objective Status: ".cyan(), 150.width());

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
        public static void OnGUI(EtudeStatus etudeStatus, object source) {
            Label("Etude Status: ".cyan(), 150.width());
            var etudeBP = etudeStatus.Etude;
            Label(etudeBP.name.orange(), 500.width());
            var etudeState = Game.Instance.Player.EtudesSystem.GetSavedState(etudeBP);
            var debugInfo = Game.Instance.Player.EtudesSystem.GetDebugInfo(etudeBP);
            22.space();
            using (VerticalScope()) {
                HelpLabel(debugInfo);
                Label($"status: ".cyan() + etudeState.ToString());
                Label("condition: ".cyan() + etudeStatus.CaptionString());
                Label("source: ".cyan() + source.ToString().yellow());
            }
        }
        public static void OnGUI(Condition condition, object source) {
            Label($"{condition.GetType().Name}:".cyan(), 150.width());
            Label(source.ToString().yellow(), 500.width());
            22.space();
            using (VerticalScope()) {
                Label("condition: ".cyan() + condition.CaptionString());
            }
        }
        public static void OnOtherElementGUI(Element element, object source) {
            Label($"{element.GetType().Name}:".cyan(), 150.width());
            Label(source.ToString().yellow(), 500.width());
            22.space();
            using (VerticalScope()) {
                Label("caption: ".cyan() + element.CaptionString());
            }
        }
    }
}
