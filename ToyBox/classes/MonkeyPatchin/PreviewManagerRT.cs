using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Controllers.Dialog;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.ElementsSystem;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RandomEncounters;
using Kingmaker.Settings;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.Utility;
using ModKit;
using Owlcat.Runtime.UI.Tooltips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Kingmaker.Code.UI.MVVM.VM.Dialog.Dialog;
using Kingmaker.Settings.Entities;
using Kingmaker.TextTools;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Kingmaker.Code.Utility;
using Kingmaker.Code.UI.MVVM.View.Dialog.Dialog;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace ToyBox {
    internal static class PreviewManager {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;
        private static GameDialogsSettings DialogSettings => SettingsRoot.Game.Dialogs;

        private static List<Tuple<BlueprintCueBase, int, GameAction[], SoulMarkShift>> CollateAnswerData(BlueprintAnswer answer, out bool isRecursive) {
            var cueResults = new List<Tuple<BlueprintCueBase, int, GameAction[], SoulMarkShift>>();
            var toCheck = new Queue<Tuple<BlueprintCueBase, int>>();
            isRecursive = false;
            var visited = new HashSet<BlueprintAnswerBase> { };
            visited.Add(answer);
            if (answer.NextCue.Cues.Count > 0) {
                toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(answer.NextCue.Cues[0], 1));
            }
            cueResults.Add(new Tuple<BlueprintCueBase, int, GameAction[], SoulMarkShift>(
                null,
                0,
                answer.OnSelect.Actions,
                answer.SoulMarkShift
            ));
            while (toCheck.Count > 0) {
                var item = toCheck.Dequeue();
                var cueBase = item.Item1;
                var currentDepth = item.Item2;
                if (currentDepth > 20) break;
                if (cueBase is BlueprintCue cue) {
                    cueResults.Add(new Tuple<BlueprintCueBase, int, GameAction[], SoulMarkShift>(
                        cue,
                        currentDepth,
                        cue.OnShow.Actions.Concat(cue.OnStop.Actions).ToArray(),
                        cue.SoulMarkShift
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
                    cueResults.Add(new Tuple<BlueprintCueBase, int, GameAction[], SoulMarkShift>(
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
                        foreach (var c in page.Cues)
                            if (c.Get().CanShow())
                                toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(c, currentDepth + 1));
                    }
                }
                else if (cueBase is BlueprintCheck check) {
                    toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(check.Success, currentDepth + 1));
                    toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(check.Fail, currentDepth + 1));
                }
                else if (cueBase is BlueprintCueSequence sequence) {
                    foreach (var c in sequence.Cues)
                        if (c.Get().CanShow())
                            toCheck.Enqueue(new Tuple<BlueprintCueBase, int>(c, currentDepth + 1));
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
            var isBook = Game.Instance.DialogController.Dialog.Type == DialogType.Book;
            string checkFormat = !isBook ? UIDialog.Instance.AnswerStringWithCheckFormat : UIDialog.Instance.AnswerStringWithCheckBeFormat;
            var text = string.Empty;
            if (DialogSettings.ShowSkillcheckDC) {
                text = answer.SkillChecks.Aggregate(
                    "",
                    (current, skillCheck) 
                        => current 
                           + string.Format(checkFormat,
                                           UIUtility.PackKeys(UIUtility.EntityLinkType.SkillcheckDC, skillCheck.Type),
                                           LocalizedTexts.Instance.Stats.GetText(skillCheck.Type),
                                           skillCheck.DC));
#if FALSE
                text = answer.SkillChecksDC.Aggregate(
                    string.Empty, 
                    (string current, SkillCheckDC skillCheck)
                        => current
                           + string.Format(checkFormat, UIUtility.PackKeys(new object[] {
                               TooltipType.SkillcheckDC,
                               skillCheck.StatType
                           }), 
                           LocalizedTexts.Instance.Stats.GetText(skillCheck.StatType), skillCheck.ValueDC));
#endif
            }
            if (DialogSettings.ShowAlignmentRequirements && answer.SoulMarkRequirement.Empty) {
                // TODO: recheck later versions of the game code to make sure this clause is still NOOP
//                text = string.Format(UIDialog.Instance.AlignmentRequirementFormat, UIUtility.GetAlignmentRequirementText(answer.SoulMarkRequirement)) + text;
            }
            if ((bool)(SettingsEntity<bool>)SettingsRoot.Game.Dialogs.ShowAlignmentShiftsInAnswer &&
                answer.SoulMarkRequirement.Empty && answer.SoulMarkShift.Value != 0 &&
                (bool)(SettingsEntity<bool>)SettingsRoot.Game.Dialogs.ShowAlignmentShiftsInAnswer)
                text = string.Format(UIDialog.Instance.AligmentShiftedFormat,
                                       UIUtility.GetSoulMarkDirectionText(answer.SoulMarkShift.Direction).Text) + text;

            var stringByBinding = UIKeyboardTexts.Instance.GetStringByBinding(Game.Instance.Keyboard.GetBindingByName(bind));
            
            return string.Format(UIDialog.Instance.AnswerDialogueFormat,
                (!stringByBinding.Empty()) ? stringByBinding : index.ToString(),
                text + ((!text.Empty()) ? " " : string.Empty) + answer.DisplayText);
        }

        [HarmonyPatch(typeof(UIConstsExtensions), nameof(UIConstsExtensions.GetAnswerString))]
        private static class UIConsts_GetAnswerString_Patch {
            private static void Postfix(ref string __result, BlueprintAnswer answer, string bind, int index) {
                try {
                    if (!Main.Enabled) return;
                    if (Main.Settings.previewAlignmentRestrictedDialog && !answer.IsSoulMarkRequirementSatisfied()) {
                        __result = GetFixedAnswerString(answer, bind, index);
                    }
                    if (!Main.Settings.previewDialogResults) return;
                    var answerData = CollateAnswerData(answer, out var isRecursive);
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
                            line.AddRange(actions.SelectMany(action => PreviewUtilities.FormatActionAsList(action)
                                .Select(actionText => actionText == "" ? "EmptyAction" : actionText)));
                        }
                        if (alignment != null && alignment.Value > 0) {
                            line.Add($"SoulMarkShift({alignment.Direction}, {alignment.Value}, {alignment.Description})");
                        }
                        if (cue is BlueprintCheck check) {
                            line.Add($"Check({check.Type}, DC {check.DC}, hidden {check.Hidden})");
                        }
                        if (line.Count > 0) results.Add($"{depth}: {line.Join()}");
                    }
                    if (results.Count > 0) __result += $" \v<size=75%>[{results.Join()}]</size>";
                }
                catch (Exception ex) {
                    Mod.Error(ex);
                }
            }
        }

        [HarmonyPatch(typeof(CueVM), nameof(CueVM.GetCueText))]
        private static class DialogCurrentPart_Fill_Patch {
            [HarmonyPostfix]
            public static void GetCueText(CueVM __instance, DialogColors dialogColors, ref string __result) {
                try {
                    if (!Main.Enabled) return;
                    if (!Main.Settings.previewDialogResults) return;
                    var cue = Game.Instance.DialogController.CurrentCue;
                    var actions = cue.OnShow.Actions.Concat(cue.OnStop.Actions).ToArray();
                    var alignment = cue.SoulMarkShift;
                    var text = "";
                    if (actions.Length > 0) {
                        var result = PreviewUtilities.FormatActions(actions);
                        if (result == "") result = "EmptyAction";
                        text += $" \n<size=75%>[{result}]</size>";
                    }
                    if (alignment != null && alignment.Value > 0) {
                        text += $" \n<size=75%>[SoulMarkShift {alignment.Direction} by {alignment.Value} - {alignment.Description}]";
                    }
                    __result += text;
                }
                catch (Exception ex) {
                    Mod.Error(ex);
                }
            }
        }
        
        [HarmonyPatch(typeof(DialogAnswerBaseView))]
        private static class DialogAnswerViewPatch {
            [HarmonyPatch(nameof(DialogAnswerBaseView.SetAnswer))]
            [HarmonyPrefix]
            private static bool SetAnswer(DialogAnswerBaseView __instance, BlueprintAnswer answer) {
                if (!settings.previewDialogResults && !settings.toggleShowAnswersForEachConditionalResponse && !settings.toggleMakePreviousAnswersMoreClear) return true;
                var type = Game.Instance.DialogController.Dialog.Type;
                var str = string.Format("DialogChoice{0}", (object)__instance.ViewModel.Index);
                var text = UIConstsExtensions.GetAnswerString(answer, str, __instance.ViewModel.Index);
                var isAvail = answer.CanSelect();
                if (answer.NextCue.Cues.Count == 1) {
                    var cue = answer.NextCue.Cues.Dereference<BlueprintCueBase>().FirstOrDefault();
                    var conditionText = $"{string.Join(", ", cue.Conditions.Conditions.Select(c => c.GetCaption()))}";
                    // the following is a kludge for toggleShowAnswersForEachConditionalResponse  to work around cases where there may be a next cue that doesn't get shown due it being already seen and the dialog being intended to fall through.  We assume that any singleton conditional nextCue (CueSelection) was generated by this feature.  We should look for edge cases to be sure.
                    isAvail = isAvail && (cue.CanShow() 
                                          || !settings.toggleShowAnswersForEachConditionalResponse 
                                          || !answer.name.Contains("ToyBox")
                                          || conditionText.Length == 0
                                          );
                    Mod.Debug($"Fixing up available for ${answer.name} canShow: {cue.CanShow()} isAvail: {isAvail} - cue: {conditionText}");
                    var color = isAvail ? "#005800><b>" : "#800000>";
                    if (conditionText.Length > 0)
                        text += $"<size=75%><color={color}[{conditionText.MergeSpaces(true)}]</color></size>";
                }
                __instance.SetAnswerText(type == DialogType.Interchapter
                                  ? answer.DisplayText
                                  : UIConstsExtensions.GetAnswerString(answer, str, __instance.ViewModel.Index));
                __instance.ViewModel.Enable.Value = answer.CanSelect() && isAvail;

                // TODO: this is new in RT so figure out whether we should do more preview stuff here
                var color32 = isAvail ? __instance.m_DialogColors.NormalAnswer : __instance.m_DialogColors.DisabledAnswer;

                if (type == DialogType.Common || type == DialogType.StarSystemEvent) {
                    color32 = answer.CanSelect()
                                  ? !__instance.ViewModel.IsAlreadySelected() || __instance.ViewModel.IsSystem
                                        ? __instance.m_DialogColors.NormalAnswer
                                        : __instance.m_DialogColors.SelectedAnswer
                                  : __instance.m_DialogColors.DisabledAnswer;
                    if (answer.SelectConditions.HasConditions) {
                        foreach (var condition in answer.SelectConditions.Conditions)
                            switch (condition) {
                                case ConditionHaveFullCargo _ when !condition.Not:
                                    __instance.SetAnswerText(string.Format(UIDialog.Instance.AnswerYouNeedFullCargo,
                                                                           condition.GetCaption(), answer.DisplayText, __instance.ViewModel.Index));
                                    goto label_8;
                                case ContextConditionHasItem _ when !condition.Not:
                                    __instance.SetAnswerText(string.Format(UIDialog.Instance.AnswerYouNeedItem, condition.GetCaption(),
                                                                           answer.DisplayText, __instance.ViewModel.Index));
                                    goto label_8;
                                default:
                                    continue;
                            }

                    label_8: ;
                    }
                }

#if false                
                if (type == DialogType.Common 
                    && answer.IsAlreadySelected() 
                    && (Game.Instance.DialogController.NextCueWasShown(answer) 
                        || !Game.Instance.DialogController.NextCueHasNewAnswers(answer)
                       )
                   ) {
                    color32 = DialogAnswerView.Colors.SelectedAnswer;
                    __instance.AnswerText.alpha = 0.45f;
                    if (settings.toggleMakePreviousAnswersMoreClear)
                        __instance.AnswerText.text = text.sizePercent(83);
                }
                else
                    __instance.AnswerText.alpha = 1.0f;
#endif
                __instance.m_AnswerText.color = (Color)color32;
                __instance.AddDisposable(Game.Instance.Keyboard.Bind(str, new Action(__instance.Confirm)));
                if (!__instance.ViewModel.IsSystem)
                    return false;
                __instance.AddDisposable(Game.Instance.Keyboard.Bind("NextOrEnd", new Action(__instance.Confirm)));
                return false;
            }
#if false
        private void SetAnswer(BlueprintAnswer answer) {
            var type = Game.Instance.DialogController.Dialog.Type;
            var str = string.Format("DialogChoice{0}", ViewModel.Index);
            AnswerText.text = UIConsts.GetAnswerString(answer, str, ViewModel.Index);
            var color32 = answer.CanSelect() ? Colors.NormalAnswer : Colors.DisabledAnswer;
            if (type == DialogType.Common 
                && answer.IsAlreadySelected() 
                && (Game.Instance.DialogController.NextCueWasShown(answer) 
                   || !Game.Instance.DialogController.NextCueHasNewAnswers(answer)
                   )
                )
                color32 = Colors.SelectedAnswer;
            AnswerText.color = color32;
            AddDisposable(Game.Instance.Keyboard.Bind(str, Confirm));
            if (ViewModel.Index != 1 || (type != DialogType.Interchapter && type != DialogType.Epilogue))
                return;
            AddDisposable(Game.Instance.Keyboard.Bind("NextOrEnd", Confirm));
        }
#endif
        }
    }
}
