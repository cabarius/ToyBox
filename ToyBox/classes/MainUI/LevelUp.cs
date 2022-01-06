using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using static ModKit.UI;
using ToyBox.Multiclass;

namespace ToyBox {
    public class LevelUp {
        public static Settings settings => Main.settings;
        public static void ResetGUI() { }
        public static void OnGUI() {
            HStack("Character Creation", 1,
                () => Slider("Build Points (Main)", ref settings.characterCreationAbilityPointsPlayer, 1, 200, 25, "", AutoWidth()),
                () => Slider("Build Points (Mercenary)", ref settings.characterCreationAbilityPointsMerc, 1, 200, 20, "", AutoWidth()),
                () => Slider("Ability Max", ref settings.characterCreationAbilityPointsMax, 0, 50, 18, "", AutoWidth()),
                () => Slider("Ability Min", ref settings.characterCreationAbilityPointsMin, 0, 50, 7, "", AutoWidth()),
                //() => {
                //    UI.Toggle("All Appearance Options", ref settings.toggleAllRaceCustomizations);
                //    UI.Space(25);
                //    UI.Label("Allows you to choose all appearance options from any race".green());
                //},
                () => { }
                );
            Div(0, 25);
            HStack("Mythic Paths", 1,
                () => Label("Warning! Using these might break your game somehow. Recommend for experimental tinkering like trying out different builds, and not for actually playing the game.".green()),
                () => ActionButton("Unlock Aeon", Actions.UnlockAeon, Width(300)),
                () => ActionButton("Unlock Azata", Actions.UnlockAzata, Width(300)),
                () => ActionButton("Unlock Trickster", Actions.UnlockTrickster, Width(300)),
                () => ActionButton("Unlock Lich", Actions.UnlockLich, Width(300)),
                () => { ActionButton("Unlock Swarm", Actions.UnlockSwarm, Width(300)); Space(25); Label("Only available at Mythic level 8 or higher".green()); },
                () => { ActionButton("Unlock Gold Dragon", Actions.UnlockGoldDragon, Width(300)); Space(25); Label("Only available at Mythic level 8 or higher".green()); },
                () => {
                    ActionButton("All Mythic Paths".orange(), Actions.UnlockAllBasicMythicPaths, Width(300));
                    Space(25);
                    Label("Unlock mythic paths besides Legend and Devil which block progression".green());
                },
                () => Label("", Height(10)),
                () => { ActionButton("Unlock Devil", Actions.UnlockDevil, Width(300)); Space(25); Label("Prevents you from advancing in Aeon or Azata".green()); },
                () => { ActionButton("Unlock Legend", Actions.UnlockLegend, Width(300)); Space(25); Label("Prevents you from advancing all other Mythic Path".green()); },
                () => { }
                );
            Div(0, 25);
            HStack("Create & Level Up", 1,
                () => {
                    Slider("Feature Selection Multiplier", ref settings.featsMultiplier, 0, 10, 1, "", Width(600));
                    Space(25);
                    Label("This allows you to select a given feature more than once at level up".green());
                },
                () => Toggle("Apply Feature Selection Multiplier to party members", ref settings.toggleFeatureMultiplierCompanions),
                () => {
                    Toggle("Allow Multiple Archetypes When Selecting A New Class", ref settings.toggleMultiArchetype);
                    25.space();
                    Label("This allows you to select combinations of archetypes when selecting a class for the first time that contain distinct spellbooks".green());
                },
                () => Toggle("Enable  'Next' when no feat selections are available", ref settings.toggleNextWhenNoAvailableFeatSelections),
                () => Toggle("Make All Feature Selections Optional", ref settings.toggleOptionalFeatSelection),
                () => {
                    Toggle("Ignore Attribute Cap", ref settings.toggleIgnoreAttributeCap);
                    Space(25);
                    Toggle("Ignore Remaining Attribute Points", ref settings.toggleIgnoreAttributePointsRemaining);
                },
                () => {
                    Toggle("Ignore Skill Cap", ref settings.toggleIgnoreSkillCap);
                    Space(73);
                    Toggle("Ignore Remaining Skill Points", ref settings.toggleIgnoreSkillPointsRemaining);
                },
                () => Toggle("Always Able To Level Up", ref settings.toggleNoLevelUpRestrictions),
                () => Toggle("Add Full Hit Die Value", ref settings.toggleFullHitdiceEachLevel),
                (Action)(() => {
                    Toggle((string)"Ignore Class Restrictions", ref settings.toggleIgnoreClassRestrictions);
                    Space(25);
                    Label("Experimental".cyan() + ": in addition to regular leveling, this allows you to choose any mythic class each time you level up starting from level 1. This may have interesting and unexpected effects. Backup early and often...".green());
                }),
                (Action)(() => {
                    Toggle((string)"Ignore Feat Restrictions", ref settings.toggleIgnoreFeatRestrictions);
                    Space(25);
                    Label("Experimental".cyan() + ": lets you select any feat ignoring prerequisites.".green());
                }),
                () => Toggle("Allow Companions to Take Mythic Classes", ref settings.toggleAllowCompanionsToBecomeMythic),
                () => Toggle("Allow Pets to Take Mythic Classes", ref settings.toggleAllowMythicPets),
                () => Toggle("Ignore Prerequisites When Choosing A Feat", ref settings.toggleFeaturesIgnorePrerequisites),
                () => Toggle("Ignore Caster Type And Spell Level Restrictions", ref settings.toggleIgnoreCasterTypeSpellLevel),
                () => Toggle("Ignore Forbidden Archetypes", ref settings.toggleIgnoreForbiddenArchetype),
                () => Toggle("Ignore Required Stat Values", ref settings.toggleIgnorePrerequisiteStatValue),
                () => Toggle("Ignore Required Class Levels", ref settings.toggleIgnorePrerequisiteClassLevel),
                () => Toggle("Ignore Alignment When Choosing A Class", ref settings.toggleIgnoreAlignmentWhenChoosingClass),
                () => Toggle("Ignore Prerequisite Features (like Race) when choosing Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass),
#if false // This is incredibly optimistic and requires resolving a bunch of conflicts with the existing gestalt and scroll copy logic
                () => UI.Toggle("Ignore Spellbook Restrictions When Choosing Spells", ref settings.toggleUniversalSpellbookd),
#endif

                () => Toggle("Skip Spell Selection", ref settings.toggleSkipSpellSelection),
#if DEBUG
                () => Toggle("Lock Character Level", ref settings.toggleLockCharacterLevel),
            //                    () => UI.Toggle("Ignore Alignment Restrictions", ref settings.toggleIgnoreAlignmentRestriction),
#endif
#if false
                // Do we need these or is it covered by toggleFeaturesIgnorePrerequisites
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref settings.toggle); },
#endif
                () => Toggle("Remove Level 20 Caster Level Cap", ref settings.toggleUncappedCasterLevel), 
                () => Toggle("Party Level Cap 40 (continuous growth after 20)", ref settings.toggleContinousLevelCap),
                () => Toggle("Party Level Cap 24 (exponential growth)", ref settings.toggleExponentialLevelCap),

                () => { }
                );
#if true
            Div(0, 25);
            HStack("Multiple Classes", 1,
                //() => UI.Label("Experimental Preview".magenta(), UI.AutoWidth()),
                () => {
                    Toggle("Multiple Classes On Level-Up", ref settings.toggleMulticlass);
                    Space(25);
                    using (VerticalScope()) {
                        Label("Experimental - With this enabled you can configure characters in the Party Editor to gain levels in additional classes whenever they level up. See the link for more information on this campaign variant.".green());
                        LinkButton("Gestalt Characters", "https://www.d20srd.org/srd/variant/classes/gestaltCharacters.htm");
                        Space(15);
                    }
                },
                () => {
                    EnumGrid("Hit Point (Hit Die) Growth", ref settings.multiclassHitPointPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Basic Attack Growth Pr", ref settings.multiclassBABPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Saving Throw Growth", ref settings.multiclassSavingThrowPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Skill Point Growth", ref settings.multiclassSkillPointPolicy, 0, AutoWidth());
                },
#if false
                () => UI.Toggle("Use Recalculate Caster Levels", ref settings.toggleRecalculateCasterLevelOnLevelingUp),
                () => UI.Toggle("Restrict Caster Level To Current", ref settings.toggleRestrictCasterLevelToCharacterLevel),
                //() => { UI.Toggle("Restrict CL to Current (temp) ", ref settings.toggleRestrictCasterLevelToCharacterLevelTemporary),
                () => UI.Toggle("Restrict Class Level for Prerequisites to Caster Level", ref settings.toggleRestrictClassLevelForPrerequisitesToCharacterLevel),
                () => UI.Toggle("Fix Favored Class HP", ref settings.toggleFixFavoredClassHP),
                () => UI.Toggle("Always Receive Favored Class HP", ref settings.toggleAlwaysReceiveFavoredClassHP),
                () => UI.Toggle("Always Receive Favored Class HP Except Prestige", ref settings.toggleAlwaysReceiveFavoredClassHPExceptPrestige),
#endif
                () => { }
                );

            if (settings.toggleMulticlass) {
                UnitEntityData selectedChar = null;
                Div(0, 25);
                HStack("Class Selection", 1,
                     () => {
                         if (Main.IsInGame) {
                             var characters = Game.Instance.Player.m_PartyAndPets;
                             if (characters == null) { return; }
                             settings.selectedClassToConfigMulticlass = Math.Min(characters.Count, settings.selectedClassToConfigMulticlass);
                             ActionSelectionGrid(ref settings.selectedClassToConfigMulticlass,
                                 characters.Select((ch) => ch.CharacterName).Prepend("Char Gen").ToArray(),
                                 6,
                                 (index) => { },
                                 AutoWidth()
                                 );
                             if (settings.selectedClassToConfigMulticlass <= 0) selectedChar = null;
                             else selectedChar = characters[settings.selectedClassToConfigMulticlass - 1];
                         }
                     },
                     () => { }
                 );

                MulticlassPicker.OnGUI(selectedChar, 150);
            }
#endif
        }
    }
}
