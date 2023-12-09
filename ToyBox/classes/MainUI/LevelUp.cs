using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using System;
using System.Linq;
using static ModKit.UI;

namespace ToyBox {
    public class LevelUp {
        public static Settings Settings => Main.Settings;
        public static void ResetGUI() { }
        public static void OnGUI() {
            Label("This area is under construction.\n".yellow().bold() + "As I play the game more it will get flushed out.  For now you see some of the anticipated features along side ones that work".orange());
            Div(0, 25);
            HStack("Create & Level Up".localize(), 1,
                () => { },
                () => {
                    Toggle("Respec from Level 0".localize(), ref Settings.toggleSetDefaultRespecLevelZero, 300.width());
                    Label("This allows rechosing the first arcehtype. Also makes Companion respec start from level 0.".green().localize());
                },
                () => Toggle("Ignore Talent Prerequisites".localize(), ref Settings.toggleFeaturesIgnorePrerequisites),
                () => Toggle("Ignore Required Stat Values".localize(), ref Settings.toggleIgnorePrerequisiteStatValue),
                () => Toggle("Ignore Required Class Levels".localize(), ref Settings.toggleIgnorePrerequisiteClassLevel),
                () => { }
                );
        }
    }
}
