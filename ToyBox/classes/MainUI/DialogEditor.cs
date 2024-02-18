// Copyright < 2023 >  - Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Assets.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Controllers.Dialog;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic.Parts;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using UnityEngine;
using static Kingmaker.UnitLogic.Interaction.SpawnerInteractionPart;
using static ModKit.UI;
using static ToyBox.BlueprintExtensions;
#nullable enable annotations

namespace ToyBox {
    public static class DialogEditor {
        public static Settings Settings => Main.Settings;
        public static Player player => Game.Instance.Player;
        private const int Indent = 75;
        private static HashSet<BlueprintScriptableObject> Visited = new();

        public static void ResetGUI() { }

        public static void OnGUI() {
            if (!Main.IsInGame) return;
            if (Game.Instance?.DialogController is { } dialogController) {
                Visited.Clear();
                dialogController.OnGUI();
                ReflectionTreeView.DetailToggle("Inspect Dialog Controller".localize(), dialogController);
                ReflectionTreeView.OnDetailGUI(dialogController);
                25.space();
            }
        }

        public static void OnGUI(this DialogController dialogController) {
            if (dialogController.CurrentCue == null) {
                Label("No Active Dialog".localize().cyan());
            }
            dialogController.CurrentCue?.OnGUI("Current".localize());
            dialogController.Answers?.OnGUI("Answer".localize());
            //if (dialogController.m_ContinueCue is BlueprintCue cue) cue.OnGUI("Continue:");
            dialogController?.Dialog.OnGUI();
        }
        private static void OnGUI(this BlueprintDialog dialog) {

        }
        private static void OnGUI(this Dialog dialog) {

        }
        private static void OnGUI(this BlueprintCue cue, string? title = null) {
            bool visited = Visited.Contains(cue);
            using (HorizontalScope()) {
                OnTitleGUI(title);
                using (VerticalScope()) {
                    var displayText = cue.DisplayText;
                    if (visited && displayText.Length > 50)
                        displayText = displayText.StripHTML().Substring(0, 50) + "...";
                    Label($"{cue.GetDisplayName().yellow()} {displayText.orange()}");
                    var resultsText = cue.ResultsText().StripHTML().Trim();
                    if (!resultsText.IsNullOrEmpty()) {
                        using (HorizontalScope()) {
                            Label("", Indent.width());
                            Label(resultsText.yellow());
                        }
                    }
                    if (cue.Conditions?.Conditions?.Count() > 0) {
                        using (HorizontalScope()) {
                            Label("Cond".localize().color(RGBA.teal), Indent.width());
                            Label(PreviewUtilities.FormatConditions(cue.Conditions).color(RGBA.teal));
                        }
                    }
                    if (visited) {
                        Label($"[Repeat]".localize().yellow());
                        return;
                    }
                    Visited.Add(cue);
                    var index = 1;
                    foreach (var answerBaseRef in cue.Answers) {
                        var answerBase = answerBaseRef.Get();
                        switch (answerBase) {
                            case BlueprintAnswer answer:
                                answer.OnGUI("Answer".localize() + $" {index}");
                                index++;
                                break;
                            case BlueprintAnswersList answersList: {
                                    var subIndex = 1;
                                    foreach (var subAnswerBaseRef in answersList.Answers) {
                                        var subAnswerBase = subAnswerBaseRef.Get();
                                        if (subAnswerBase is BlueprintAnswer subAnswer) {
                                            subAnswer.OnGUI($"{index}-{subIndex}");
                                            subIndex++;
                                        }
                                    }
                                    index++;
                                    break;
                                }
                        }
                    }
                    if (cue.Continue is { } cueSelection) {
                        cueSelection.OnGUI("Selection".localize());
                    }
                }
            }
        }
        private static void OnGUI(this CueSelection cueSelection, string? title = null) {
            var cues = cueSelection.Cues;
            if (cues.Count(cbr => cbr.Get() is BlueprintCue) <= 0) return;
            using (HorizontalScope()) {
                OnTitleGUI((title));
                using (VerticalScope()) {
                    foreach (var cueBaseRef in cues) {
                        var index = 1;
                        if (cueBaseRef.Get() is BlueprintCue cue) {
                            cue.OnGUI("Cue".localize() + $" {index}");
                            index++;
                        }
                    }
                }
            }
        }
        private static void OnGUI(this BlueprintAnswer answer, string? title = null) {
            using (HorizontalScope()) {
                OnTitleGUI(title);
                using (VerticalScope()) {
                    var text = $"{answer.GetDisplayName().yellow()} {answer.DisplayText}";
                    if (answer.NextCue is CueSelection nextCueSelection && nextCueSelection.Cues.Any()) {
                        Browser.DetailToggle(text, nextCueSelection, nextCueSelection);
                        Browser.OnDetailGUI(nextCueSelection, (_) => nextCueSelection.OnGUI("Next".localize()));
                    }
                    else
                        Label(text);
                    var checkStrings = PreviewUtilities.FormatConditionsAsList(answer);
                    foreach (var checkString in checkStrings) {
                        Label(checkString.color(RGBA.teal));
                    }
#if false                    
                    if (answer.HasShowCheck) {
                        using (HorizontalScope()) {
                            Label("Check".color(RGBA.teal), Indent.width());
                            Label($"{answer.ShowCheck.Type} DC: {answer.ShowCheck.DC}".color(RGBA.teal));
                        }
                    }
                    if (answer.ShowConditions.Conditions.Length > 0) {
                        using (HorizontalScope()) {
                            Label("Show".color(RGBA.teal), Indent.width());
                            Label(PreviewUtilities.FormatConditions(answer.ShowConditions).color(RGBA.teal));
                        }
                    }
                    if (answer.SelectConditions is ConditionsChecker selectChecker && selectChecker.Conditions.Count() > 0) {
                        Label("Select".color(RGBA.teal), Indent.width());
                        Label(PreviewUtilities.FormatConditions(selectChecker).color(RGBA.teal));
                    }
#endif
                    var resultsText = answer.ResultsText().StripHTML();
                    if (!resultsText.IsNullOrEmpty()) {
                        using (HorizontalScope()) {
                            Label("", Indent.width());
                            Label(resultsText.yellow());
                        }
                    }
                }
            }
        }
        private static void OnGUI(this BlueprintAnswersList answersList) {
            if (answersList?.Answers?.Count <= 0) return;
            if (answersList.Answers.Select(ar => ar.Get() as BlueprintAnswer) is { } answers) {
                answers.OnGUI();
            }
        }
        private static void OnGUI(this IEnumerable<BlueprintAnswer> answersList, string? title = null) {
            if (answersList?.Count() <= 0) return;
            using (HorizontalScope()) {
                OnTitleGUI(title);
                using (VerticalScope()) {
                    var index = 1;
                    foreach (var answer in answersList) {
                        answer.OnGUI($"{index}");
                        index++;
                    }
                }
            }
        }
        private static void OnTitleGUI(string? title) {
            if (title != null) {
                Label(title.cyan(), Indent.width());
            }
            else
                Indent.space();
        }
    }
}
