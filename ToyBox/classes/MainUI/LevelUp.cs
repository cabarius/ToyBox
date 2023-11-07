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
                () => {
                    Toggle("Ignore Class Restrictions".localize(), ref Settings.toggleIgnoreClassRestrictions);
                    Space(25);
                },
                () => {
                },
                () => Toggle("Ignore Talent Prerequisites".localize(), ref Settings.toggleFeaturesIgnorePrerequisites),
                () => Toggle("Ignore Required Stat Values".localize(), ref Settings.toggleIgnorePrerequisiteStatValue),
                () => Toggle("Ignore Required Class Levels".localize(), ref Settings.toggleIgnorePrerequisiteClassLevel),
#if false // This is incredibly optimistic and requires resolving a bunch of conflicts with the existing gestalt and scroll copy logic
                () => UI.Toggle("Ignore Spellbook Restrictions When Choosing Spells", ref settings.toggleUniversalSpellbookd),
#endif
                () => { }
                );
        }
    }
}
