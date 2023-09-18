using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using ModKit.DataViewer;
using System;
using System.Collections.Generic;
using System.Linq;
using static ModKit.UI;
#if Wrath
namespace ToyBox.classes.MainUI {
    public static class BraaainzEditor {
        public static Settings Settings => Main.Settings;

        private static List<BlueprintBrain> _allBraaainz = null;
        private static List<BlueprintBrain> AllBraaainz {
            get {
                if (_allBraaainz != null) return _allBraaainz;
                _allBraaainz = BlueprintLoader.Shared.GetBlueprints<BlueprintBrain>()?.OrderBy(bp => bp.GetDisplayName())?.ToList();
                return _allBraaainz;
            }
        }

        private static bool _pickBrain = false;
        private static bool _customBrain = false;
        private static string _brainSearchText = "";

        public static Browser<BlueprintAiAction, AiAction> ActionBrowser = new(true, true);
        public static Browser<Consideration, Consideration> ConsiderationBrowser = new(true, true);
        public static Dictionary<BlueprintAiAction, Browser<Consideration, Consideration>> TargetConsiderationBrowser = new();

        public static void OnGUI() {
            Label("Group".orange().bold());
            CharacterPicker.OnFilterPickerGUI();
            Div();
            using (HorizontalScope()) {
                50.space();
                Label("Character".orange().bold());
            }
            5.space();
            CharacterPicker.OnCharacterPickerGUI(50);
            5.space();
            Div(50);
            OnBrainGUI(CharacterPicker.GetSelectedCharacter());
        }
        public static void OnBrainGUI(UnitEntityData ch) {
            if (ch == null) return;
            Label("This allows you to edit the AI for your characters much like Gambits in Final Fantasy 12, Dragon Age, or Pillars of Eternity. You can either choose one of the available default brains or build a custom one by choosing from a list of AI actions".green());
            using (HorizontalScope()) {
                Label("Current Brain: ".cyan() + ch.Brain.Blueprint.GetDisplayName());
                10.space();
                if (Toggle("Customize", ref _customBrain)) if (_customBrain) _pickBrain = false;
                if (!_customBrain) {
                    10.space();
                    DisclosureToggle("Pick Existing", ref _pickBrain);
                }
            }
            if (_pickBrain) {
                var braaainz = AllBraaainz;
                if (braaainz != null) {
                    var selectedBrain = ch.Brain.Blueprint;
                    if (GridPicker<BlueprintBrain>("Braaainzzz!", ref selectedBrain, AllBraaainz, null, br => br.GetDisplayName(), ref _brainSearchText, 1, 500.width())) {
                        ch.Brain.SetBrain(selectedBrain);
                        ch.Brain.RestoreAvailableActions();
                        ActionBrowser.ResetSearch();
                    }
                }
                else
                    Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
            }


            ActionBrowser.OnGUI(
                ch.Brain.Actions,
                BlueprintExtensions.GetBlueprints<BlueprintAiAction>,
                a => (BlueprintAiAction)a.Blueprint,
                bp => bp.GetDisplayName(),
                bp => new string[] { $"{bp.GetDisplayName()} {bp.GetDescription()}" },
                null,
                (bp, action) => {
                    Browser.DetailToggle(bp.GetDisplayName(), bp, bp);
                    ReflectionTreeView.DetailToggle("Inspect", bp, action != null ? action : bp);
                    var attributes = bp.GetCustomAttributes();
                    var text = String.Join("\n", attributes.Select((name, value) => $"{name}: {value}"));
                    Label($"{text.green()}", AutoWidth());
                },
                (bp, action) => {
                    ReflectionTreeView.OnDetailGUI((bp));
                    Browser.OnDetailGUI(bp, _ => {
                        if (action?.ActorConsiderations.Count > 0) {
                            using (HorizontalScope()) {
                                150.space();
                                Label($"{"Actor Considerations".orange().bold()} - {ch.CharacterName.cyan()}");
                            }
                            ConsiderationBrowser.OnGUI(
                               action.ActorConsiderations,
                               BlueprintExtensions.GetBlueprints<Consideration>,
                               c => c,
                               c => c.GetDisplayName(),
                               c => new[] { c.GetDisplayName() },
                               null,
                               (bp, c) => {
                                   Label(c.GetDisplayName());
                                   ReflectionTreeView.DetailToggle("", bp, c ?? bp);

                                   var attributes = bp.GetCustomAttributes();
                                   var text = string.Join("\n", attributes.Select((name, value) => $"{name} : {value}"));
                                   Label(text.green(), AutoWidth());
                               },
                               (bp, _) => ReflectionTreeView.OnDetailGUI(bp, 150),
                               150, true, false
                              );
                        }
                        using (HorizontalScope()) {
                            150.space();
                            Label($"Target Consideration".orange());
                        }
                        var targetConsiderationsBrowser = TargetConsiderationBrowser.GetValueOrDefault(bp, null);
                        if (targetConsiderationsBrowser == null) {
                            targetConsiderationsBrowser = new Browser<Consideration, Consideration>(Mod.ModKitSettings.searchAsYouType);
                            TargetConsiderationBrowser[bp] = targetConsiderationsBrowser;
                        }
                        targetConsiderationsBrowser.OnGUI(
                            action?.TargetConsiderations,
                            BlueprintExtensions.GetBlueprints<Consideration>,
                            c => c,
                            c => c.GetDisplayName(),
                            c => new[] { c.GetDisplayName() },
                            null,
                            (bp, c) => {
                                Label(c.GetDisplayName());
                                ReflectionTreeView.DetailToggle("", bp);
                                var attributes = bp.GetCustomAttributes();
                                var text = string.Join("\n", attributes.Select((name, value) => $"{name} : {value}"));
                                Label(text.green(), AutoWidth());
                            },
                            (bp, _) => ReflectionTreeView.OnDetailGUI(bp, 150),
                            150, true, false
                            );
                    });
                }
                );
        }
    }
}
#endif