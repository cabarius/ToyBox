using Kingmaker;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI;
using Kingmaker.Armies;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Armies.State;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Kingdom;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;
using static ModKit.UI;

namespace ToyBox.classes.MainUI {
    public static class BraaainzEditor {
        public static Settings settings => Main.settings;

        private static List<BlueprintBrain> allBraaainz = null;
        private static List<BlueprintBrain> AllBraaainz {
            get {
                if (allBraaainz == null) {
                    allBraaainz = BlueprintLoader.Shared.GetBlueprints<BlueprintBrain>()?.OrderBy(bp => bp.GetDisplayName())?.ToList();
                }
                return allBraaainz;
            }
        }

        private static bool pickBrain = false;
        private static bool customBrain = false;
        private static string brainSearchText = "";

        public static Browser<AiAction, BlueprintAiAction> ActionBrowser = new(true, true);
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
                if (Toggle("Customize", ref customBrain)) if (customBrain) pickBrain = false;
                if (!customBrain) {
                    10.space();
                    DisclosureToggle("Pick Existing", ref pickBrain);
                }

            }
            if (pickBrain) {
                var braainz = AllBraaainz;
                if (braainz != null) {
                    var selectedBrain = ch.Brain.Blueprint;
                    if (GridPicker<BlueprintBrain>("Braaainzzz!", ref selectedBrain, AllBraaainz, null, br => br.GetDisplayName(), ref brainSearchText, 1, 500.width())) {
                        ch.Brain.SetBrain(selectedBrain);
                        ch.Brain.RestoreAvailableActions();
                        ActionBrowser.needsReloadData = true;
                    }
                }
                else
                    Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
            }


            ActionBrowser.OnGUI(
                $"{ch.CharacterName}-Gambits",
                ch.Brain.Actions,
                () => BlueprintExtensions.GetBlueprints<BlueprintAiAction>(),
                a => (BlueprintAiAction)a.Blueprint,
                bp => bp.AssetGuid.ToString(),
                bp => bp.GetDisplayName(),
                bp => $"{bp.GetDisplayName()} {bp.GetDescription()}",
                bp => bp.GetDisplayName(),
                null,
                (action, bp) => {
                    var attributes = bp.GetCustomAttributes();
                    var text = String.Join("\n", attributes.Select((name, value) => $"{name}: {value}"));
                    Label($"{text.green()}", AutoWidth());
                },
                (action, bp) => (action, bp) => {
                    if (action?.ActorConsiderations.Count > 0) {
                        using (HorizontalScope()) {
                            150.space();
                            Label($"{"Actor Considerations".orange().bold()} - {ch.CharacterName.cyan()}");
                        }
                        ConsiderationBrowser.OnGUI(
                            $"{ch.CharacterName}-{bp.AssetGuid}-ActorConsiderations",
                            action.ActorConsiderations,
                            () => BlueprintExtensions.GetBlueprints<Consideration>(),
                            c => c,
                            c => c.AssetGuid.ToString(),
                            c => c.GetDisplayName(),
                            c => c.GetDisplayName(),
                            c => c.GetDisplayName(),
                            null,
                            (c, bp) => {
                                var attributes = bp.GetCustomAttributes();
                                var text = String.Join("\n", attributes.Select((name, value) => $"{name} : {value}"));
                                Label(text.green(), AutoWidth());
                            }, null,
                            150, true, false
                            );
                    }
                    using (HorizontalScope()) {
                        150.space();
                        Label($"Target Consideration".orange());
                    }
                    var targetConsiderationsBrowser = TargetConsiderationBrowser.GetValueOrDefault(bp, null);
                    if (targetConsiderationsBrowser == null) {
                        targetConsiderationsBrowser = new Browser<Consideration, Consideration>();
                        TargetConsiderationBrowser[bp] = targetConsiderationsBrowser;
                    }
                    targetConsiderationsBrowser.OnGUI(
                        $"{ch.CharacterName}-{bp.AssetGuid}-TargetConsiderations",
                        action?.TargetConsiderations,
                        () => BlueprintExtensions.GetBlueprints<Consideration>(),
                        c => c,
                        c => c.AssetGuid.ToString(),
                        c => c.GetDisplayName(),
                        c => c.GetDisplayName(),
                        c => c.GetDisplayName(),
                        null,
                            (c, bp) => {
                                var attributes = bp.GetCustomAttributes();
                                var text = String.Join("\n", attributes.Select((name, value) => $"{name} : {value}"));
                                Label(text.green(), AutoWidth());
                            }, null,
                            150, true, false
                            );
                }
                );
        }
    }
}