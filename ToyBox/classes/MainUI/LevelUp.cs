using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using ToyBox.Multiclass;

namespace ToyBox {
    public class LevelUp {
        public static Settings settings { get { return Main.settings; } }
        public static void ResetGUI() { }
        public static void OnGUI() {
            UI.HStack("Character Creation", 1,
                        () => UI.Slider("Build Points (Main)", ref settings.characterCreationAbilityPointsPlayer, 1, 200, 25, "", UI.AutoWidth()),
                        () => UI.Slider("Build Points (Mercenary)", ref settings.characterCreationAbilityPointsMerc, 1, 200, 20, "", UI.AutoWidth()),
                        () => UI.Slider("Ability Max", ref settings.characterCreationAbilityPointsMax, 0, 50, 18, "", UI.AutoWidth()),
                        () => UI.Slider("Ability Min", ref settings.characterCreationAbilityPointsMin, 0, 50, 7, "", UI.AutoWidth()),
                        //() => {
                        //    UI.Toggle("All Appearance Options", ref settings.toggleAllRaceCustomizations, 0);
                        //    UI.Space(25);
                        //    UI.Label("Allows you to choose all appearance options from any race".green());
                        //},
#if DEBUG
                        () => {
                            UI.Toggle("Gender Bending", ref settings.toggleIgnoreGenderRestrictions, 0);
                            UI.Space(25);
                            UI.Label("Removes gender restrictions from appearance options".green());
                        },
#endif
                    () => { }
                    );
            UI.Div(0, 25);
            UI.HStack("Unlocks", 4, () => {
                UI.ActionButton("All Mythic Paths", () => Actions.UnlockAllMythicPaths());
                UI.Space(25);
                UI.Label("Warning! Using this might break your game somehow. Recommend for experimental tinkering like trying out different builds, and not for actually playing the game.".green());
            });
            UI.Div(0, 25);
            UI.HStack("Create & Level Up", 1,
                () => UI.Slider("Feat Selection Multiplier", ref settings.featsMultiplier, 0, 10, 1, "", UI.AutoWidth()),
                () => UI.Toggle("Enable  'Next' when no feat selections are available", ref settings.toggleNextWhenNoAvailableFeatSelections),
                () => UI.Toggle("Make All Feat Selections Optional", ref settings.toggleOptionalFeatSelection),
                () => {
                    UI.Toggle("Ignore Attribute Cap", ref settings.toggleIgnoreAttributeCap, 0);
                    UI.Space(25);
                    UI.Toggle("Ignore Remaining Attribute Points", ref settings.toggleIgnoreAttributePointsRemaining, 0);
                },
                () => {
                    UI.Toggle("Ignore Skill Cap", ref settings.toggleIgnoreSkillCap, 0);
                    UI.Space(73);
                    UI.Toggle("Ignore Remaining Skill Points", ref settings.toggleIgnoreSkillPointsRemaining, 0);
                },
                () => UI.Toggle("Always Able To Level Up", ref settings.toggleNoLevelUpRestrictions, 0),
                () => UI.Toggle("Add Full Hit Die Value", ref settings.toggleFullHitdiceEachLevel, 0),
                () => {
                    UI.Toggle("Ignore Class And Feat Restrictions", ref settings.toggleIgnorePrerequisites, 0);
                    UI.Space(25);
                    UI.Label("Experimental".cyan() + ": in addition to regular leveling, this allows you to choose any mythic class each time you level up starting from level 1. This may have interesting and unexpected effects. Backup early and often...".green());
                },
                () => UI.Toggle("Ignore Prerequisites When Choosing A Feat", ref settings.toggleFeaturesIgnorePrerequisites, 0),
                () => UI.Toggle("Ignore Caster Type And Spell Level Restrictions", ref settings.toggleIgnoreCasterTypeSpellLevel, 0),
                () => UI.Toggle("Ignore Forbidden Archetypes", ref settings.toggleIgnoreForbiddenArchetype, 0),
                () => UI.Toggle("Ignore Required Stat Values", ref settings.toggleIgnorePrerequisiteStatValue, 0),
                () => UI.Toggle("Ignore Alignment When Choosing A Class", ref settings.toggleIgnoreAlignmentWhenChoosingClass, 0),
                () => UI.Toggle("Skip Spell Selection", ref settings.toggleSkipSpellSelection, 0),
#if DEBUG
                    () => UI.Toggle("Lock Character Level", ref settings.toggleLockCharacterLevel, 0),
                //                    () => UI.Toggle("Ignore Alignment Restrictions", ref settings.toggleIgnoreAlignmentRestriction, 0),
#endif
#if false
                // Do we need these or is it covered by toggleFeaturesIgnorePrerequisites
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass, 0); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref settings.toggle, 0); },
#endif

                () => { }
                );
#if true
            UI.Div(0, 25);
            UI.HStack("Multiple Classes", 1,
                //() => UI.Label("Experimental Preview".magenta(), UI.AutoWidth()),
                () => {
                    UI.Toggle("Multiple Classes On Level-Up", ref settings.toggleMulticlass, 0);
                    UI.Space(25);
                    using (UI.VerticalScope()) {
                        UI.Label("Experimental - With this enabled you can configure characters in the Party Editor to gain levels in additional classes whenever they level up. See the link for more information on this campaign variant.".green());
                        UI.LinkButton("Gestalt Characters", "https://www.d20srd.org/srd/variant/classes/gestaltCharacters.htm");
                        UI.Space(15);
                    }
                },
                () => {
                    UI.EnumGrid<ProgressionPolicy>("Hit Point (Hit Die) Growth", ref settings.multiclassHitPointPolicy, 0, UI.AutoWidth());
                },
                () => {
                    UI.EnumGrid<ProgressionPolicy>("Basic Attack Growth Pr", ref settings.multiclassBABPolicy, 0, UI.AutoWidth());
                },
                () => {
                    UI.EnumGrid<ProgressionPolicy>("Saving Throw Growth", ref settings.multiclassSavingThrowPolicy, 0, UI.AutoWidth());
                },
                () => {
                    UI.EnumGrid<ProgressionPolicy>("Skill Point Growth", ref settings.multiclassSkillPointPolicy, 0, UI.AutoWidth());
                },
                () => UI.Toggle("Use Recalculate Caster Levels", ref settings.toggleRecalculateCasterLevelOnLevelingUp, 0),
                () => UI.Toggle("Restrict Caster Level To Current", ref settings.toggleRestrictCasterLevelToCharacterLevel, 0),
                //() => { UI.Toggle("Restrict CL to Current (temp) ", ref settings.toggleRestrictCasterLevelToCharacterLevelTemporary, 0),
                () => UI.Toggle("Restrict Class Level for Prerequisites to Caster Level", ref settings.toggleRestrictClassLevelForPrerequisitesToCharacterLevel, 0),
                () => UI.Toggle("Fix Favored Class HP", ref settings.toggleFixFavoredClassHP, 0),
                () => UI.Toggle("Always Receive Favored Class HP", ref settings.toggleAlwaysReceiveFavoredClassHP, 0),
                () => UI.Toggle("Always Receive Favored Class HP Except Prestige", ref settings.toggleAlwaysReceiveFavoredClassHPExceptPrestige, 0),
                () => { }
                );

            if (settings.toggleMulticlass) {
                UnitEntityData selectedChar = null;
                UI.Div(0, 25);
                UI.HStack("Class Selection", 1,
                     () => {
                         if (Main.IsInGame) {
                             var characters = Game.Instance.Player.m_PartyAndPets;
                             if (characters == null) { return; }
                             settings.selectedClassToConfigMulticlass = Math.Min(characters.Count, settings.selectedClassToConfigMulticlass);
                             UI.ActionSelectionGrid(ref settings.selectedClassToConfigMulticlass,
                                 characters.Select((ch) => ch.CharacterName).Prepend("Char Gen").ToArray(),
                                 6,
                                 (index) => { },
                                 UI.AutoWidth()
                                 );
                             if (settings.selectedClassToConfigMulticlass <= 0) selectedChar = null;
                             else selectedChar = characters[settings.selectedClassToConfigMulticlass - 1];
                         }
                     },
                     () => {
                         var targetString = selectedChar == null
                         ? "creation of ".green() + "new characters".orange().bold() : $"when leveling up ".green() + selectedChar.CharacterName.orange().bold();
                         UI.Label($"Configure gestalt classes to use during {targetString}".green(), UI.AutoWidth());
                         UI.Space(25);
                         UI.Toggle("Show Class Descriptions", ref settings.toggleMulticlassShowClassDescriptions);
                     }
                 );
                var options = MulticlassOptions.Get(selectedChar);
                MulticlassPicker.OnGUI(options, 150);
                MulticlassOptions.Set(selectedChar, options);
            }
#endif
        }
    }
}
