using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using ToyBox.Multiclass;
using Alignment = Kingmaker.Enums.Alignment;
using ModKit;
using static ModKit.UI;
using ModKit.Utility;
using ToyBox.classes.Infrastructure;
using Kingmaker.PubSubSystem;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using static Kingmaker.Utility.UnitDescription.UnitDescription;
using Kingmaker.AI;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using System.Web.UI;

namespace ToyBox {
    public partial class PartyEditor {
        public static void OnBrainGUI(UnitEntityData ch) {
            bool changed = false;
            Browser<AiAction, BlueprintAiAction>.OnGUI(
                $"{ch.CharacterName}-Gambits",
                ref changed,
                ch.Brain.Actions,
                () => BlueprintExensions.GetBlueprints<BlueprintAiAction>(),
                a => (BlueprintAiAction)a.Blueprint,
                bp => bp.AssetGuid.ToString(),
                bp => bp.GetDisplayName(),
                bp => $"{bp.GetDisplayName()} {bp.GetDescription()}",
                bp => bp.GetDisplayName(),
                50, 100, 200,
                (action, bp) => {
                    var attributes = bp.GetCustomAttributes();
                    var text = String.Join("\n", attributes.Select((name, value) => $"{name}: {value}"));
                    Label($"bla bla bla {text.green()}", AutoWidth());
                },
                (action, bp) => {
                    bool changed = false;
                    using (HorizontalScope()) {
                        150.space();
                        Label($"{ch.CharacterName} Considerations".orange());
                    }
                    Browser<Consideration, Consideration>.OnGUI(
                        $"{ch.CharacterName}-{bp.AssetGuid}-ActorConsiderations", 
                        ref changed,
                        action.ActorConsiderations,
                        () => BlueprintExensions.GetBlueprints<Consideration>(),
                        c => c,
                        c => c.AssetGuid.ToString(),
                        c => c.GetDisplayName(),
                        c => c.GetDisplayName(),
                        c => c.GetDisplayName(),
                        150, 100, 200, (c, bp) => {
                            var attributes = bp.GetCustomAttributes();
                            var text = String.Join("\n", attributes.Select((name, value) => $"{name}: {value}"));
                            Label(text.green(), AutoWidth());
                        }, null, 50, false
                        );
                    using (HorizontalScope()) {
                        150.space();
                        Label($"Target Consideration".orange());
                    }
                    Browser<Consideration, Consideration>.OnGUI(
                        $"{ch.CharacterName}-{bp.AssetGuid}-TargetConsiderations", 
                        ref changed,
                        action.TargetConsiderations,
                        () => BlueprintExensions.GetBlueprints<Consideration>(),
                        c => c,
                        c => c.AssetGuid.ToString(),
                        c => c.GetDisplayName(),
                        c => c.GetDisplayName(),
                        c => c.GetDisplayName(),
                        150, 100, 200, (c, bp) => {
                            var attributes = bp.GetCustomAttributes();
                            var text = String.Join("\n", attributes.Select((name, value) => $"{name}: {value}"));
                            Label(text.green(), AutoWidth());
                        }, null, 50, false
                        );
                }
                );
        }
    }
}