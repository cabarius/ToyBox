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
                () => EnumGrid("Roll With Avantage".localize(), ref Settings.rollWithAdvantage, AutoWidth()),
                () => EnumGrid("Roll With Disavantage".localize(), ref Settings.rollWithDisadvantage, AutoWidth()),
                () => EnumGrid("Always Roll 20".localize(), ref Settings.alwaysRoll20, AutoWidth()),
                () => EnumGrid("Always Roll 10".localize(), ref Settings.alwaysRoll10, AutoWidth()),
                () => EnumGrid("Always Roll 1".localize(), ref Settings.alwaysRoll1, AutoWidth()),
                () => EnumGrid("Never Roll 20".localize(), ref Settings.neverRoll20, AutoWidth()),
                () => EnumGrid("Never Roll 1".localize(), ref Settings.neverRoll1, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 20".localize(), ref Settings.roll20Initiative, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 10".localize(), ref Settings.roll20Initiative, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 1".localize(), ref Settings.roll1Initiative, AutoWidth()),
                () => EnumGrid("Non Combat: Take 20".localize(), ref Settings.alwaysRoll20OutOfCombat, AutoWidth()),
                () => EnumGrid("Non Combat: Roll at least 10".localize(), ref Settings.rollAtLeast10OutOfCombat, AutoWidth()),
                () => { 330.space(); Label("The following skill check adjustments apply only out of combat".localize().green()); },
                () => EnumGrid("Skill Checks: Take 20".localize(), ref Settings.skillsTake20, AutoWidth()),
                () => EnumGrid("Skill Checks: Take 10".localize(), ref Settings.skillsTake10, AutoWidth()),
                () => { }
                );
        }
    }
}