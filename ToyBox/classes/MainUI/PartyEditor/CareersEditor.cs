using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Progression.Paths;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using ToyBox.classes.Infrastructure;
using UnityEngine;
using static Kingmaker.Utility.UnitDescription.UnitDescription;
using static ModKit.UI;
using Alignment = Kingmaker.Enums.Alignment;

namespace ToyBox {
    public partial class PartyEditor {
        public static void OnClassesGUI(BaseUnitEntity ch, List<(BlueprintCareerPath path, int level)> careerPaths, BaseUnitEntity selectedCharacter) {
            using (HorizontalScope()) {
            }
            Div(100, 20);

            if (editMultiClass) {
            }
            else {
                var prog = ch.Descriptor().Progression;
                using (HorizontalScope()) {
                    using (HorizontalScope(Width(600))) {
                        Space(100);
                        Label("Character Level".localize().cyan(), Width(250));
                        ActionButton("<", () => prog.CharacterLevel = Math.Max(0, prog.CharacterLevel - 1), AutoWidth());
                        Space(25);
                        Label("level".localize().green() + $": {prog.CharacterLevel}", Width(100f));
                        ActionButton(">", () => prog.CharacterLevel = Math.Min(
                                                    int.MaxValue, // TODO: is this right?
                                                    prog.CharacterLevel + 1),
                                     AutoWidth());
                    }
                    ActionButton("Reset".localize(), () => ch.resetClassLevel(), Width(150));
                    Space(23);
                    using (VerticalScope()) {
                        Label("This directly changes your character level but will not change exp or adjust any features associated with your character. To do a normal level up use +1 Lvl above.  This gets recalculated when you reload the game.  ".localize().green());
                    }
                }
                using (HorizontalScope()) {
                    using (HorizontalScope(Width(600))) {
                        Space(100);
                        Label("Experience".localize().cyan(), Width(250));
                        Space(25);
                        int tmpExp = prog.Experience;
                        IntTextField(ref tmpExp, null, Width(150f));
                        prog.Experience = tmpExp;
                    }
                }
                using (HorizontalScope()) {
                    using (HorizontalScope(Width(781))) {
                        Space(100);
                        ActionButton("Adjust based on Level".localize(), () => {
                            var xpTable = prog.ExperienceTable;
                            prog.Experience = xpTable.GetBonus(prog.CharacterLevel);
                        }, AutoWidth());
                        Space(27);
                    }
                    Label("This sets your experience to match the current value of character level".localize().green());
                }
#if false
                Div(100, 25);
                using (HorizontalScope()) {
                    using (HorizontalScope(Width(600))) {
                        Space(100);
                        Label("Mythic Level".cyan(), Width(250));
                        ActionButton("<", () => prog.MythicLevel = Math.Max(0, prog.MythicLevel - 1), AutoWidth());
                        Space(25);
                        Label("my lvl".green() + $": {prog.MythicLevel}", Width(100f));
                        ActionButton(">", () => prog.MythicLevel = Math.Min(10, prog.MythicLevel + 1), AutoWidth());
                    }
                    Space(181);
                    Label("This directly changes your mythic level but will not adjust any features associated with your character. To do a normal mythic level up use +1 my above".green());
                }

                using (HorizontalScope()) {
                    using (HorizontalScope(Width(600))) {
                        Space(100);
                        Label("Experience".cyan(), Width(250));
                        Space(25);
                        int tmpMythicExp = prog.MythicExperience;
                        IntTextField(ref tmpMythicExp, null, Width(150f));
                        if (0 <= tmpMythicExp && tmpMythicExp <= 10) {
                            prog.MythicExperience = tmpMythicExp;
                        } // If Mythic experience is 0, entering any number besides 1 is > 10, meaning the number would be overwritten with; this is to prevent that
                        else if (tmpMythicExp % 10 == 0) {
                            prog.MythicExperience = tmpMythicExp / 10;
                        }
                    }
                }
                using (HorizontalScope()) {
                    using (HorizontalScope(Width(781))) {
                        Space(100);
                        ActionButton("Adjust based on Level", () => {
                            prog.MythicExperience = prog.MythicLevel;
                        }, AutoWidth());
                        Space(27);
                    }
                    Label("This sets your mythic experience to match the current value of mythic level. Note that mythic experience is 1 point per level".green());
                }
#endif
                var classCount = careerPaths.Count();
                foreach (var cd in careerPaths) {
                    Div(100, 20);
                    using (HorizontalScope()) {
                        Space(100);
                        using (VerticalScope(Width(250))) {
                            var className = cd.path.Name;
                            Label(className.orange(), Width(250));
                        }
                        //                        ActionButton("<", () => cd.level = Math.Max(0, cd.level - 1), AutoWidth());
                        Space(25);
                        Label("level".localize().green() + $": {cd.level}", Width(100f));
                        //var maxLevel = 20;
                        //ActionButton(">", () => cd.level = Math.Min(maxLevel, cd.level + 1), AutoWidth());
                        Space(23);
                    }
                }
            }
        }
    }
}