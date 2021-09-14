﻿using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.RandomEncounters;
using Kingmaker.Settings;
using Kingmaker.UI.Common;
using Kingmaker.UI.Dialog;
using Kingmaker.UI.GlobalMap;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.Tooltip;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace ToyBox {
    class PreviewManager {
        static GameDialogsSettings DialogSettings { get { return SettingsRoot.Game.Dialogs; } }
        public static KingdomStats.Changes CalculateEventResult(KingdomEvent kingdomEvent, EventResult.MarginType margin, AlignmentMaskType alignment, LeaderType leaderType) {
            var checkMargin = EventResult.MarginToInt(margin);
            var result = new KingdomStats.Changes();
            var m_TriggerChange = Traverse.Create(kingdomEvent).Field("m_TriggerChange").GetValue<KingdomStats.Changes>();
            var m_SuccessCount = Traverse.Create(kingdomEvent).Field("m_SuccessCount").GetValue<int>();
            BlueprintKingdomEvent blueprintKingdomEvent = kingdomEvent.EventBlueprint as BlueprintKingdomEvent;
            if (blueprintKingdomEvent && blueprintKingdomEvent.UnapplyTriggerOnResolve && m_TriggerChange != null) {
                result.Accumulate(m_TriggerChange.Opposite(), 1);
            }
            var resolutions = kingdomEvent.EventBlueprint.Solutions.GetResolutions(leaderType);
            if (resolutions == null) resolutions = Array.Empty<EventResult>();
            foreach (var eventResult in resolutions) {
                var validConditions = eventResult.Condition == null; // || eventResult.Condition.Check(kingdomEvent.GetKingdomEventData());
                if (eventResult.MatchesMargin(checkMargin) && (alignment & eventResult.LeaderAlignment) != AlignmentMaskType.None && validConditions) {
                    result.Accumulate(eventResult.StatChanges, 1);
                    m_SuccessCount += eventResult.SuccessCount;
                }
            }
            if (checkMargin >= 0 && blueprintKingdomEvent != null) {
                result.Accumulate((KingdomStats.Type)leaderType, Game.Instance.BlueprintRoot.Kingdom.StatIncreaseOnEvent);
            }
            bool willBeFinished = true;
            if (blueprintKingdomEvent != null && blueprintKingdomEvent.IsRecurrent) {
                willBeFinished = m_SuccessCount >= blueprintKingdomEvent.Solutions.GetSuccessCount(leaderType);
            }
            if (willBeFinished) {
                var eventFinalResults = kingdomEvent.EventBlueprint.GetComponent<EventFinalResults>();
                if (eventFinalResults != null && eventFinalResults.Results != null) {
                    foreach (var eventResult in eventFinalResults.Results) {
                        var validConditions = eventResult.Condition == null; // || eventResult.Condition.Check(kingdomEvent.EventBlueprint);
                        if (eventResult.MatchesMargin(checkMargin) && (alignment & eventResult.LeaderAlignment) != AlignmentMaskType.None && validConditions) {
                            result.Accumulate(eventResult.StatChanges, 1);
                        }
                    }
                }
            }
            return result;

        }
        static string FormatResult(KingdomEvent kingdomEvent, EventResult.MarginType margin, AlignmentMaskType alignment, LeaderType leaderType) {
            string text = "";
            var statChanges = CalculateEventResult(kingdomEvent, margin, alignment, leaderType);
            var statChangesText = statChanges.ToStringWithPrefix(" ");
            text += string.Format("{0}:{1}",
                margin,
                statChangesText == "" ? " No Change" : statChangesText);
            //TODO: Solution for presenting actions
            text += "\n";
            return text;
        }
        static List<Tuple<BlueprintCueBase, int, GameAction[], AlignmentShift>> CollateAnswerData(BlueprintAnswer answer, out bool isRecursive) {
            var cueResults = new List<Tuple<BlueprintCueBase, int, GameAction[], AlignmentShift>>();
            var toCheck = new Queue<Tuple<BlueprintCueBase, int>>();
            isRecursive = false;
            var visited = new HashSet<BlueprintAnswerBase> { };
            visited.Add(answer);
            if (answer.NextCue.Cues.Count > 0) {
                toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(answer.NextCue.Cues[0], 1));
            }
            cueResults.Add(new Tuple<BlueprintCueBase, int, GameAction[], AlignmentShift>(
                null,
                0,
                answer.OnSelect.Actions,
                answer.AlignmentShift
                ));
            while (toCheck.Count > 0) {
                var item = toCheck.Dequeue();
                var cueBase = item.Item1;
                int currentDepth = item.Item2;
                if (currentDepth > 20) break;
                if (cueBase is BlueprintCue cue) {
                    cueResults.Add(new Tuple<BlueprintCueBase, int, GameAction[], AlignmentShift>(
                        cue,
                        currentDepth,
                        cue.OnShow.Actions.Concat(cue.OnStop.Actions).ToArray(),
                        cue.AlignmentShift
                        ));
                    if (cue.Answers.Count > 0) {
                        var subAnswer = cue.Answers[0].Get();
                        if (visited.Contains(subAnswer)) {
                            isRecursive = true;
                            break;
                        }
                        visited.Add(subAnswer);
                    }
                    if (cue.Continue.Cues.Count > 0) {
                        toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(cue.Continue.Cues[0], currentDepth + 1));
                    }
                }
                else if (cueBase is BlueprintBookPage page) {
                    cueResults.Add(new Tuple<BlueprintCueBase, int, GameAction[], AlignmentShift>(
                        page,
                        currentDepth,
                        page.OnShow.Actions,
                        null
                        ));
                    if (page.Answers.Count > 0) {
                        var subAnswer = page.Answers[0].Get();
                        if (visited.Contains(subAnswer)) {
                            isRecursive = true;
                            break;
                        }
                        visited.Add(subAnswer);
                        if (page.Answers[0].Get() is BlueprintAnswersList) break;
                    }
                    if (page.Cues.Count > 0) {
                        foreach (var c in page.Cues) if (c.Get().CanShow()) toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(c, currentDepth + 1));
                    }
                }
                else if (cueBase is BlueprintCheck check) {
                    toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(check.Success, currentDepth + 1));
                    toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(check.Fail, currentDepth + 1));
                }
                else if (cueBase is BlueprintCueSequence sequence) {
                    foreach (var c in sequence.Cues) if (c.Get().CanShow()) toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(c, currentDepth + 1));
                    if (sequence.Exit != null) {
                        var exit = sequence.Exit;
                        if (exit.Answers.Count > 0) {
                            var subAnswer = exit.Answers[0];
                            if (visited.Contains(subAnswer)) {
                                isRecursive = true;
                                break;
                            }
                            visited.Add(subAnswer);
                            if (exit.Continue.Cues.Count > 0) {
                                toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(exit.Continue.Cues[0], currentDepth + 1));
                            }
                        }
                    }
                }
                else {
                    break;
                }
            }
            return cueResults;
        }
        public static string GetFixedAnswerString(BlueprintAnswer answer, string bind, int index) {
            bool flag = Game.Instance.DialogController.Dialog.Type == DialogType.Book;
            string checkFormat = (!flag) ? UIDialog.Instance.AnswerStringWithCheckFormat : UIDialog.Instance.AnswerStringWithCheckBeFormat;
            string text = string.Empty;
            if (DialogSettings.ShowSkillcheckDC) {
                text = answer.SkillChecksDC.Aggregate(string.Empty, (string current, SkillCheckDC skillCheck) => current + string.Format(checkFormat, UIUtility.PackKeys(new object[]
                {
                    TooltipType.SkillcheckDC,
                    skillCheck.StatType
                }), LocalizedTexts.Instance.Stats.GetText(skillCheck.StatType), skillCheck.ValueDC));
            }
            if (DialogSettings.ShowAlignmentRequirements && answer.AlignmentRequirement != AlignmentComponent.None) {
                text = string.Format(UIDialog.Instance.AlignmentRequirementFormat, UIUtility.GetAlignmentRequirementText(answer.AlignmentRequirement)) + text;
            }
            if (answer.HasShowCheck) {
                text = string.Format(UIDialog.Instance.AnswerShowCheckFormat, LocalizedTexts.Instance.Stats.GetText(answer.ShowCheck.Type), text);
            }
            if (DialogSettings.ShowAlignmentShiftsInAnswer && answer.AlignmentRequirement == AlignmentComponent.None && answer.AlignmentShift.Value > 0 && DialogSettings.ShowAlignmentShiftsInAnswer) {
                text = string.Format(UIDialog.Instance.AligmentShiftedFormat, UIUtility.GetAlignmentShiftDirectionText(answer.AlignmentShift.Direction)) + text;
            }
            string stringByBinding = UIKeyboardTexts.Instance.GetStringByBinding(Game.Instance.Keyboard.GetBindingByName(bind));
            return string.Format(UIDialog.Instance.AnswerDialogueFormat,
                (!stringByBinding.Empty()) ? stringByBinding : index.ToString(),
                text + ((!text.Empty()) ? " " : string.Empty) + answer.DisplayText);
        }
        [HarmonyPatch(typeof(UIConsts), "GetAnswerString")]
        static class UIConsts_GetAnswerString_Patch {
            static void Postfix(ref string __result, BlueprintAnswer answer, string bind, int index) {
                try {
                    if (!Main.Enabled) return;
                    if (Main.settings.previewAlignmentRestrictedDialog && !answer.IsAlignmentRequirementSatisfied) {
                        __result = GetFixedAnswerString(answer, bind, index);
                    }
                    if (!Main.settings.previewDialogResults) return;
                    var answerData = CollateAnswerData(answer, out bool isRecursive);
                    if (isRecursive) {
                        __result += $" <size=75%>[Repeats]</size>";
                    }
                    var results = new List<string>();
                    foreach (var data in answerData) {
                        var cue = data.Item1;
                        var depth = data.Item2;
                        var actions = data.Item3;
                        var alignment = data.Item4;
                        var line = new List<string>();
                        if (actions.Length > 0) {
                            line.AddRange(actions.
                                SelectMany(action => PreviewUtilities.FormatActionAsList(action)
                                .Select(actionText => actionText == "" ? "EmptyAction" : actionText)));
                        }
                        if (alignment != null && alignment.Value > 0) {
                            line.Add($"AlignmentShift({alignment.Direction}, {alignment.Value}, {alignment.Description})");
                        }
                        if (cue is BlueprintCheck check) {
                            line.Add($"Check({check.Type}, DC {check.DC}, hidden {check.Hidden})");
                        }
                        if (line.Count > 0) results.Add($"{depth}: {line.Join()}");
                    }
                    if (results.Count > 0) __result += $" \n<size=75%>[{results.Join()}]</size>";
                }
                catch (Exception ex) {
                    Main.Error(ex);
                }
            }
        }
        [HarmonyPatch(typeof(DialogCurrentPart), "Fill")]
        static class DialogCurrentPart_Fill_Patch {
            static void Postfix(DialogCurrentPart __instance) {
                try {
                    if (!Main.Enabled) return;
                    if (!Main.settings.previewDialogResults) return;
                    var cue = Game.Instance.DialogController.CurrentCue;
                    var actions = cue.OnShow.Actions.Concat(cue.OnStop.Actions).ToArray();
                    var alignment = cue.AlignmentShift;
                    var text = "";
                    if (actions.Length > 0) {
                        var result = PreviewUtilities.FormatActions(actions);
                        if (result == "") result = "EmptyAction";
                        text += $" \n<size=75%>[{result}]</size>";
                    }
                    if (alignment != null && alignment.Value > 0) {
                        text += $" \n<size=75%>[AlignmentShift {alignment.Direction} by {alignment.Value} - {alignment.Description}]";
                    }
                    __instance.DialogPhrase.text += text;
                }
                catch (Exception ex) {
                    Main.Error(ex);
                }
            }
        }
        [HarmonyPatch(typeof(KingdomUIEventWindow), "SetHeader")]
        static class KingdomUIEventWindow_SetHeader_Patch {
            static void Postfix(KingdomUIEventWindow __instance, KingdomEventUIView kingdomEventView) {
                try {
                    if (!Main.Enabled) return;
                    if (!Main.settings.previewEventResults) return;
                    if (kingdomEventView.Task == null || kingdomEventView.Task.Event == null) {
                        return; //Task is null on event results;
                    }
                    //Calculate results for current leader
                    var solutionText = Traverse.Create(__instance).Field("m_Description").GetValue<TextMeshProUGUI>();
                    solutionText.text += "\n";
                    var leader = kingdomEventView.Task.AssignedLeader;
                    if (leader == null) {
                        solutionText.text += "<size=75%>Select a leader to preview results</size>";
                        return;
                    }
                    var blueprint = kingdomEventView.Blueprint;
                    var solutions = blueprint.Solutions;
                    var resolutions = solutions.GetResolutions(leader.Type);
                    solutionText.text += "<size=75%>";
#if false
                    TODO - does this matter in WoTR?  It seems like the ability to get alignment or name is gone from LeaderState
                    var leaderAlignmentMask = leader.LeaderSelection.Alignment.ToMask();
                    bool isValid(EventResult result) => (leaderAlignmentMask & result.LeaderAlignment) != AlignmentMaskType.None;
                    var validResults = resolutions.Where(isValid);
                    solutionText.text += "Leader " + leader.LeaderSelection.CharacterName + " - Alignment " + leaderAlignmentMask + "\n";
                    foreach (var eventResult in validResults) {
                        solutionText.text += FormatResult(kingdomEventView.Task.Event, eventResult.Margin, leaderAlignmentMask, leader.Type);
                    }
#endif
                    //Calculate best result
                    int bestResult = 0;
                    KingdomStats.Changes bestEventResult = null;
                    LeaderType bestLeader = 0;
                    AlignmentMaskType bestAlignment = 0;
                    foreach (var solution in solutions.Entries) {
                        if (!solution.CanBeSolved) continue;
                        foreach (var alignmentMask in solution.Resolutions.Select(s => s.LeaderAlignment).Distinct()) {
                            var eventResult = CalculateEventResult(kingdomEventView.Task.Event, EventResult.MarginType.GreatSuccess, alignmentMask, solution.Leader);
                            int sum = 0;
                            for (int i = 0; i < 10; i++) sum += eventResult[(KingdomStats.Type)i];
                            if (sum > bestResult) {
                                bestResult = sum;
                                bestLeader = solution.Leader;
                                bestEventResult = eventResult;
                                bestAlignment = alignmentMask;
                            }
                        }
                    }

                    if (bestEventResult != null) {
                        solutionText.text += "<size=50%>\n<size=75%>";
                        solutionText.text += "Best Result: Leader " + bestLeader + " - Alignment " + bestAlignment + "\n";
#if false
                        if (bestLeader == leader.Type && (leaderAlignmentMask & bestAlignment) != AlignmentMaskType.None) {
                            solutionText.text += "<color=#308014>";
                        }
                        else {
                            solutionText.text += "<color=#808080>";
                        }
#else
                        solutionText.text += "<color=#808080>";
#endif
                        solutionText.text += FormatResult(kingdomEventView.Task.Event, EventResult.MarginType.GreatSuccess, bestAlignment, bestLeader);
                        solutionText.text += "</color>";
                    }
                    solutionText.text += "</size>";
                }

                catch (Exception ex) {
                    Main.Error(ex);
                }
            }
        }
        [HarmonyPatch(typeof(GlobalMapRandomEncounterController), "OnRandomEncounterStarted")]
        static class GlobalMapRandomEncounterController_OnRandomEncounterStarted_Patch {
            static AccessTools.FieldRef<GlobalMapRandomEncounterController, TextMeshProUGUI> m_DescriptionRef;
            static bool Prepare() {
                m_DescriptionRef = Accessors.CreateFieldRef<GlobalMapRandomEncounterController, TextMeshProUGUI>("m_Description");
                return true;
            }
            static void Postfix(GlobalMapRandomEncounterController __instance, ref CombatRandomEncounterData encounter) {
                try {
                    if (!Main.Enabled) return;
                    if (Main.settings.previewRandomEncounters) {
                        var blueprint = encounter.Blueprint;
                        var text = $"\n<size=70%>Name: {blueprint.Name}\nType: {blueprint.Type}\nCR: {encounter.Blueprint.AvoidDC}</size>";
                        m_DescriptionRef(__instance).text += text;
                    }
                }
                catch (Exception ex) {
                    Main.Error(ex);
                }
            }
        }
    }
}
