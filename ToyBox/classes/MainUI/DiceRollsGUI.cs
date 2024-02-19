// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using ModKit;
using System;

namespace ToyBox {
    public static class DiceRollsGUI {
        public static Settings Settings => Main.Settings;
        public static void OnGUI() {
            HStack("Dice Rolls".localize(), 1,
                () => EnumGrid("All Attacks Hit".localize(), ref Settings.allAttacksHit, AutoWidth()),
                () => EnumGrid("All Hits Critical".localize(), ref Settings.allHitsCritical, AutoWidth()),
                () => EnumGrid("Roll With Advantage (take lower roll)".localize(), ref Settings.rollWithAdvantage, AutoWidth()),
                () => EnumGrid("Roll With Disavantage (take higher roll)".localize(), ref Settings.rollWithDisadvantage, AutoWidth()),
                () => EnumGrid("Always Roll 100".localize(), ref Settings.alwaysRoll100, AutoWidth()),
                () => EnumGrid("Always Roll 50".localize(), ref Settings.alwaysRoll50, AutoWidth()),
                () => EnumGrid("Always Roll 1".localize(), ref Settings.alwaysRoll1, AutoWidth()),
                () => EnumGrid("Never Roll 100".localize(), ref Settings.neverRoll100, AutoWidth()),
                () => EnumGrid("Never Roll 1".localize(), ref Settings.neverRoll1, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 10".localize(), ref Settings.roll10Initiative, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 5".localize(), ref Settings.roll5Initiative, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 1".localize(), ref Settings.roll1Initiative, AutoWidth()),
                () => EnumGrid("Non Combat: Take 1".localize(), ref Settings.alwaysRoll1OutOfCombat, AutoWidth()),
                () => { 330.space(); Label("The following skill check adjustments apply only out of combat".localize().green()); },
                () => EnumGrid("Skill Checks: Take 50".localize(), ref Settings.skillsTake50, AutoWidth()),
                () => EnumGrid("Skill Checks: Take 25".localize(), ref Settings.skillsTake25, AutoWidth()),
                () => EnumGrid("Skill Checks: Take 1".localize(), ref Settings.skillsTake1, AutoWidth()),
                () => { }
                );
        }
    }
}