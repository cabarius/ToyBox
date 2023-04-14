using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using static ModKit.UI;

namespace ToyBox {
    public partial class PartyEditor {
        public static Browser<AiAction, BlueprintAiAction> browser = new(true, true);
        public static Browser<Consideration, Consideration> browser2 = new(true, true);
        public static Dictionary<BlueprintAiAction, Browser<Consideration, Consideration>> browsers3 = new();
        public static void OnBrainGUI(UnitEntityData ch) {
            bool changed = false;
            browser.OnGUI(
                $"{ch.CharacterName}-Gambits",
                ref changed,
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
                (action, bp) => {
                    bool changed = false;
                    if (action?.ActorConsiderations.Count > 0) {
                        using (HorizontalScope()) {
                            150.space();
                            Label($"{"Actor Considerations".orange().bold()} - {ch.CharacterName.cyan()}");
                        }
                        browser2.OnGUI(
                            $"{ch.CharacterName}-{bp.AssetGuid}-ActorConsiderations",
                            ref changed,
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
                    var browser3 = browsers3.GetValueOrDefault(bp, null);
                    if (browser3 == null) {
                        browser3 = new Browser<Consideration, Consideration>();
                        browsers3[bp] = browser3;
                    }
                    browser3.OnGUI(
                        $"{ch.CharacterName}-{bp.AssetGuid}-TargetConsiderations",
                        ref changed,
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