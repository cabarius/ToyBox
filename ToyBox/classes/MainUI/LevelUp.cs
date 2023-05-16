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
            HStack("Character Creation".localize(), 1,
                () => {
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            Slider("Build Points (Main)".localize(), ref Settings.characterCreationAbilityPointsPlayer, 1, 600, 25, "", 300.width());
                            25.space();
                            Toggle("Ignore Game Minimum".localize(), ref Settings.characterCreationAbilityPointsOverrideGameMinimums);
                            25.space();
                            HelpLabel("Tick this if you want these sliders to let you go below game specified minimum point value".localize());
                            Space();
                        }
                        using (HorizontalScope()) {
                            Slider("Build Points (Mercenary)".localize(), ref Settings.characterCreationAbilityPointsMerc, 1, 600, 25, "", AutoWidth());
                        }
                    }
                },
                () => Slider("Ability Max".localize(), ref Settings.characterCreationAbilityPointsMax, 0, 50, 18, "", AutoWidth()),
                () => Slider("Ability Min".localize(), ref Settings.characterCreationAbilityPointsMin, 0, 50, 7, "", AutoWidth()),
                //() => {
                //    UI.Toggle("All Appearance Options", ref settings.toggleAllRaceCustomizations);
                //    UI.Space(25);
                //    UI.Label("Allows you to choose all appearance options from any race".green());
                //},
                () => { }
                );
            Div(0, 25);
            HStack("Create & Level Up".localize(), 1,
                () => {
                    Slider("Feature Selection Multiplier".localize(), ref Settings.featsMultiplier, 0, 10, 1, "", Width(600));
                    Space(25);
                    Label("This allows you to select a given feature more than once at level up".localize().green());
                },
                () => {
                    ActionButton("Maximize Mythic Flexibility",
                                 () => {
                                     Settings.toggleIgnoreClassRestrictions = true;
                                     Settings.toggleAllowCompanionsToBecomeMythic = true;
                                     Settings.toggleAllowMythicPets = true;
                                 });
                    25.space();
                    HelpLabel(("This will set options below to enable you to choose mythics more freely for both you, companions and even pets" + "\nNote:".orange().bold() + " this will also ignore other class restrictions for non mythic").localize());

                },
                () => Toggle("Apply Feature Selection Multiplier to party members".localize(), ref Settings.toggleFeatureMultiplierCompanions),
                () => {
                    Toggle("Allow Multiple Archetypes When Selecting A New Class".localize(), ref Settings.toggleMultiArchetype);
                    25.space();
                    Label("This allows you to select combinations of archetypes when selecting a class for the first time that contain distinct spellbooks".localize().green());
                },
                () => Toggle("Make All Feature Selections Optional".localize(), ref Settings.toggleOptionalFeatSelection),
                () => {
                    Toggle("Ignore Attribute Cap".localize(), ref Settings.toggleIgnoreAttributeCap);
                    Space(25);
                    Toggle("Ignore Remaining Attribute Points".localize(), ref Settings.toggleIgnoreAttributePointsRemaining);
                },
                () => {
                    Toggle("Ignore Skill Cap".localize(), ref Settings.toggleIgnoreSkillCap);
                    Space(73);
                    Toggle("Ignore Remaining Skill Points".localize(), ref Settings.toggleIgnoreSkillPointsRemaining);
                },
                () => Toggle("Always Able To Level Up".localize(), ref Settings.toggleNoLevelUpRestrictions),
                () => Toggle("Add Full Hit Die Value".localize(), ref Settings.toggleFullHitdiceEachLevel),
                () => {
                    Toggle("Ignore Class Restrictions".localize(), ref Settings.toggleIgnoreClassRestrictions);
                    Space(25);
                    Label(("Experimental".cyan() + ": in addition to regular leveling, this allows you to choose any mythic class each time you level up starting from mythic rank 1. This may have interesting and unexpected effects. Backup early and often...".green()).localize());
                },
                () => {
                    Toggle("Ignore Feat Restrictions".localize(), ref Settings.toggleIgnoreFeatRestrictions);
                    Space(25);
                    Label(("Experimental".cyan() + ": lets you select any feat ignoring prerequisites.".green()).localize());
                },
                () => Toggle("Allow Companions to Take Mythic Classes".localize(), ref Settings.toggleAllowCompanionsToBecomeMythic),
                () => Toggle("Allow Pets to Take Mythic Classes".localize(), ref Settings.toggleAllowMythicPets),
                () => Toggle("Ignore Prerequisites When Choosing A Feat".localize(), ref Settings.toggleFeaturesIgnorePrerequisites),
                () => Toggle("Ignore Caster Type And Spell Level Restrictions".localize(), ref Settings.toggleIgnoreCasterTypeSpellLevel),
                () => Toggle("Ignore Forbidden Archetypes".localize(), ref Settings.toggleIgnoreForbiddenArchetype),
                () => Toggle("Ignore Required Stat Values".localize(), ref Settings.toggleIgnorePrerequisiteStatValue),
                () => Toggle("Ignore Required Class Levels".localize(), ref Settings.toggleIgnorePrerequisiteClassLevel),
                () => Toggle("Ignore Alignment When Choosing A Class".localize(), ref Settings.toggleIgnoreAlignmentWhenChoosingClass),
                () => Toggle("Ignore Prerequisite Features (like Race) when choosing Class".localize(), ref Settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass),
#if false // This is incredibly optimistic and requires resolving a bunch of conflicts with the existing gestalt and scroll copy logic
                () => UI.Toggle("Ignore Spellbook Restrictions When Choosing Spells", ref settings.toggleUniversalSpellbookd),
#endif

                () => Toggle("Skip Spell Selection".localize(), ref Settings.toggleSkipSpellSelection),
#if DEBUG
                () => Toggle("Lock Character Level".localize(), ref Settings.toggleLockCharacterLevel),
            //                    () => UI.Toggle("Ignore Alignment Restrictions", ref settings.toggleIgnoreAlignmentRestriction),
#endif
#if false
                // Do we need these or is it covered by toggleFeaturesIgnorePrerequisites
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref settings.toggle); },
#endif
                () => Toggle("Remove Level 20 Caster Level Cap".localize(), ref Settings.toggleUncappedCasterLevel),
                () => Toggle("Party Level Cap 40 (continuous growth after 20)".localize(), ref Settings.toggleContinousLevelCap),
                () => Toggle("Party Level Cap 24 (exponential growth)".localize(), ref Settings.toggleExponentialLevelCap),

                () => { }
                );
#if true
            Div(0, 25);
            HStack("Mythic Paths".localize(), 1,
                   () => Label("Warning! Using these might break your game somehow. Recommend for experimental tinkering like trying out different builds, and not for actually playing the game.".localize().green()),
                   () => ActionButton("Unlock Aeon".localize(), Actions.UnlockAeon, Width(300)),
                   () => ActionButton("Unlock Azata".localize(), Actions.UnlockAzata, Width(300)),
                   () => ActionButton("Unlock Trickster".localize(), Actions.UnlockTrickster, Width(300)),
                   () => ActionButton("Unlock Lich".localize(), Actions.UnlockLich, Width(300)),
                   () => { ActionButton("Unlock Swarm".localize(), Actions.UnlockSwarm, Width(300)); Space(25); Label("Only available at Mythic level 8 or higher".localize().green()); },
                   () => { ActionButton("Unlock Gold Dragon".localize(), Actions.UnlockGoldDragon, Width(300)); Space(25); Label("Only available at Mythic level 8 or higher".localize().green()); },
                   () => {
                       ActionButton("All Mythic Paths".localize().orange(), Actions.UnlockAllBasicMythicPaths, Width(300));
                       Space(25);
                       Label("Unlock mythic paths besides Legend and Devil which block progression".localize().green());
                   },
                   () => Label("", Height(10)),
                   () => { ActionButton("Unlock Devil".localize(), Actions.UnlockDevil, Width(300)); Space(25); Label("Prevents you from advancing in Aeon or Azata".localize().green()); },
                   () => { ActionButton("Unlock Legend".localize(), Actions.UnlockLegend, Width(300)); Space(25); Label("Prevents you from advancing all other Mythic Path".localize().green()); },
                   () => { }
                );

            Div(0, 25);
            HStack("Multiple Classes".localize(), 1,
                //() => UI.Label("Experimental Preview".magenta(), UI.AutoWidth()),
                () => {
                    Toggle("Multiple Classes On Level-Up".localize(), ref Settings.toggleMulticlass);
                    Space(25);
                    using (VerticalScope()) {
                        Label("Experimental - With this enabled you can configure characters in the Party Editor to gain levels in additional classes whenever they level up. See the link for more information on this campaign variant.".localize().green());
                        LinkButton("Gestalt Characters".localize(), "https://www.d20srd.org/srd/variant/classes/gestaltCharacters.htm");
                        Space(15);
                    }
                },
                () => {
                    EnumGrid("Hit Point (Hit Die) Growth".localize(), ref Settings.multiclassHitPointPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Basic Attack Growth Pr".localize(), ref Settings.multiclassBABPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Saving Throw Growth".localize(), ref Settings.multiclassSavingThrowPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Skill Point Growth".localize(), ref Settings.multiclassSkillPointPolicy, 0, AutoWidth());
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

            if (Settings.toggleMulticlass) {
                UnitEntityData selectedChar = null;
                Div(0, 25);
                HStack("Class Selection".localize(), 1,
                     () => {
                         if (Main.IsInGame) {
                             var characters = Game.Instance.Player.m_PartyAndPets;
                             if (characters == null) { return; }
                             Settings.selectedClassToConfigMulticlass = Math.Min(characters.Count, Settings.selectedClassToConfigMulticlass);
                             ActionSelectionGrid(ref Settings.selectedClassToConfigMulticlass,
                                 characters.Select((ch) => ch.CharacterName).Prepend("Char Gen".localize()).ToArray(),
                                 6,
                                 (index) => { },
                                 AutoWidth()
                                 );
                             if (Settings.selectedClassToConfigMulticlass <= 0) selectedChar = null;
                             else selectedChar = characters[Settings.selectedClassToConfigMulticlass - 1];
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
