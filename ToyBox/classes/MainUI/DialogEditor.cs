// Copyright < 2023 >  - Narria (github user Cabarius) - License: MIT
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
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.UI;

namespace ToyBox {
    public static class DialogEditor {
        public static Settings Settings => Main.Settings;
        public static Player player => Game.Instance.Player;
        private const int Indent = 100;

        public static void ResetGUI() { }

        public static void OnGUI() {
            if (!Main.IsInGame) return;
            if (Game.Instance?.DialogController is { } dialogController) {
                dialogController.OnGUI();
                ReflectionTreeView.DetailToggle("Inspect Dialog Controller", dialogController);
                ReflectionTreeView.OnDetailGUI(dialogController);
                25.space();
            }
        }

        public static void OnGUI(this DialogController dialogController) {
            if (dialogController.CurrentCue == null) {
                Label("No Active Dialog".cyan());
            }
            dialogController.CurrentCue?.OnGUI("Cur Cue:");
            dialogController.Answers?.OnGUI("Answers:");
            if (dialogController.m_ContinueCue is BlueprintCue cue) cue.OnGUI("Continue:");
            dialogController?.Dialog.OnGUI();
        }
        private static void OnGUI(this BlueprintDialog dialog) {

        }
        private static void OnGUI(this Dialog dialog) {

        }
        private static void OnGUI(this BlueprintCue cue, string? title = null) {
            using (HorizontalScope()) {
                OnTitleGUI(title);
                using (VerticalScope()) {
                    Label($"{cue.GetDisplayName().yellow()} {cue.DisplayText.orange()}");
                    var resultsText = cue.ResultsText().StripHTML().Trim();
                    if (!resultsText.IsNullOrEmpty()) {
                        using (HorizontalScope()) {
                            Label("", Indent.width());
                            Label(resultsText.yellow());
                        }
                    }
                    var index = 1;
                    foreach (var answerBaseRef in cue.Answers) {
                        var answerBase = answerBaseRef.Get();
                        switch (answerBase) {
                            case BlueprintAnswer answer:
                                answer.OnGUI($"answer {index}");
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
                        cueSelection.OnGUI("Cue Sel:");
                    }
                }
            }
        }
        private static void OnGUI(this CueSelection cueSelection, string? title = null) {
            var cues = cueSelection.Cues;
            if (cues.Count <= 0) return;
            using (HorizontalScope()) {
                OnTitleGUI((title));
                using (VerticalScope()) {
                    foreach (var cueBaseRef in cues) {
                        var index = 1;
                        if (cueBaseRef.Get() is BlueprintCue cue) {
                            cue.OnGUI($"cue {index}");
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
                    Label($"{answer.GetDisplayName().yellow()} {answer.DisplayText}");
                    var resultsText = answer.ResultsText().StripHTML();
                    if (!resultsText.IsNullOrEmpty()) {
                        using (HorizontalScope()) {
                            Indent.space();
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
