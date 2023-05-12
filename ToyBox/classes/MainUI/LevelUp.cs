using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using System;
using System.Linq;
using static ModKit.UI;

namespace ToyBox {
    public class LevelUp {
        public static Settings settings => Main.Settings;
        public static void ResetGUI() { }
        public static void OnGUI() {
            HStack("Character Creation".localize(), 1,
                () => {
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            Slider("Build Points (Main)".localize(), ref settings.characterCreationAbilityPointsPlayer, 1, 600, 25, "", 300.width());
                            25.space();
                            Toggle("Ignore Game Minimum".localize(), ref settings.characterCreationAbilityPointsOverrideGameMinimums);
                            25.space();
                            HelpLabel("Tick this if you want these sliders to let you go below game specified minimum point value".localize());
                            Space();
                        }
                        using (HorizontalScope()) {
                            Slider("Build Points (Mercenary)".localize(), ref settings.characterCreationAbilityPointsMerc, 1, 600, 25, "", AutoWidth());
                        }
                    }
                },
                () => Slider("Ability Max".localize(), ref settings.characterCreationAbilityPointsMax, 0, 50, 18, "", AutoWidth()),
                () => Slider("Ability Min".localize(), ref settings.characterCreationAbilityPointsMin, 0, 50, 7, "", AutoWidth()),
                //() => {
                //    UI.Toggle("All Appearance Options", ref settings.toggleAllRaceCustomizations);
                //    UI.Space(25);
                //    UI.Label("Allows you to choose all appearance options from any race".green());
                //},
                () => { }
                );
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
            HStack("Create & Level Up".localize(), 1,
                () => {
                    Slider("Feature Selection Multiplier".localize(), ref settings.featsMultiplier, 0, 10, 1, "", Width(600));
                    Space(25);
                    Label("This allows you to select a given feature more than once at level up".localize().green());
                },
                () => Toggle("Apply Feature Selection Multiplier to party members".localize(), ref settings.toggleFeatureMultiplierCompanions),
                () => {
                    Toggle("Allow Multiple Archetypes When Selecting A New Class".localize(), ref settings.toggleMultiArchetype);
                    25.space();
                    Label("This allows you to select combinations of archetypes when selecting a class for the first time that contain distinct spellbooks".localize().green());
                },
                () => Toggle("Make All Feature Selections Optional".localize(), ref settings.toggleOptionalFeatSelection),
                () => {
                    Toggle("Ignore Attribute Cap".localize(), ref settings.toggleIgnoreAttributeCap);
                    Space(25);
                    Toggle("Ignore Remaining Attribute Points".localize(), ref settings.toggleIgnoreAttributePointsRemaining);
                },
                () => {
                    Toggle("Ignore Skill Cap".localize(), ref settings.toggleIgnoreSkillCap);
                    Space(73);
                    Toggle("Ignore Remaining Skill Points".localize(), ref settings.toggleIgnoreSkillPointsRemaining);
                },
                () => Toggle("Always Able To Level Up".localize(), ref settings.toggleNoLevelUpRestrictions),
                () => Toggle("Add Full Hit Die Value".localize(), ref settings.toggleFullHitdiceEachLevel),
                () => {
                    Toggle("Ignore Class Restrictions".localize(), ref settings.toggleIgnoreClassRestrictions);
                    Space(25);
                    Label(("Experimental".cyan() + ": in addition to regular leveling, this allows you to choose any mythic class each time you level up starting from mythic rank 1. This may have interesting and unexpected effects. Backup early and often...".green()).localize());
                },
                () => {
                    Toggle("Ignore Feat Restrictions".localize(), ref settings.toggleIgnoreFeatRestrictions);
                    Space(25);
                    Label(("Experimental".cyan() + ": lets you select any feat ignoring prerequisites.".green()).localize());
                },
                () => Toggle("Allow Companions to Take Mythic Classes".localize(), ref settings.toggleAllowCompanionsToBecomeMythic),
                () => Toggle("Allow Pets to Take Mythic Classes".localize(), ref settings.toggleAllowMythicPets),
                () => Toggle("Ignore Prerequisites When Choosing A Feat".localize(), ref settings.toggleFeaturesIgnorePrerequisites),
                () => Toggle("Ignore Caster Type And Spell Level Restrictions".localize(), ref settings.toggleIgnoreCasterTypeSpellLevel),
                () => Toggle("Ignore Forbidden Archetypes".localize(), ref settings.toggleIgnoreForbiddenArchetype),
                () => Toggle("Ignore Required Stat Values".localize(), ref settings.toggleIgnorePrerequisiteStatValue),
                () => Toggle("Ignore Required Class Levels".localize(), ref settings.toggleIgnorePrerequisiteClassLevel),
                () => Toggle("Ignore Alignment When Choosing A Class".localize(), ref settings.toggleIgnoreAlignmentWhenChoosingClass),
                () => Toggle("Ignore Prerequisite Features (like Race) when choosing Class".localize(), ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass),
#if false // This is incredibly optimistic and requires resolving a bunch of conflicts with the existing gestalt and scroll copy logic
                () => UI.Toggle("Ignore Spellbook Restrictions When Choosing Spells", ref settings.toggleUniversalSpellbookd),
#endif

                () => Toggle("Skip Spell Selection".localize(), ref settings.toggleSkipSpellSelection),
#if DEBUG
                () => Toggle("Lock Character Level".localize(), ref settings.toggleLockCharacterLevel),
            //                    () => UI.Toggle("Ignore Alignment Restrictions", ref settings.toggleIgnoreAlignmentRestriction),
#endif
#if false
                // Do we need these or is it covered by toggleFeaturesIgnorePrerequisites
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref settings.toggle); },
#endif
                () => Toggle("Remove Level 20 Caster Level Cap".localize(), ref settings.toggleUncappedCasterLevel),
                () => Toggle("Party Level Cap 40 (continuous growth after 20)".localize(), ref settings.toggleContinousLevelCap),
                () => Toggle("Party Level Cap 24 (exponential growth)".localize(), ref settings.toggleExponentialLevelCap),

                () => { }
                );
#if true
            Div(0, 25);
            HStack("Multiple Classes".localize(), 1,
                //() => UI.Label("Experimental Preview".magenta(), UI.AutoWidth()),
                () => {
                    Toggle("Multiple Classes On Level-Up".localize(), ref settings.toggleMulticlass);
                    Space(25);
                    using (VerticalScope()) {
                        Label("Experimental - With this enabled you can configure characters in the Party Editor to gain levels in additional classes whenever they level up. See the link for more information on this campaign variant.".localize().green());
                        LinkButton("Gestalt Characters".localize(), "https://www.d20srd.org/srd/variant/classes/gestaltCharacters.htm");
                        Space(15);
                    }
                },
                () => {
                    EnumGrid("Hit Point (Hit Die) Growth".localize(), ref settings.multiclassHitPointPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Basic Attack Growth Pr".localize(), ref settings.multiclassBABPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Saving Throw Growth".localize(), ref settings.multiclassSavingThrowPolicy, 0, AutoWidth());
                },
                () => {
                    EnumGrid("Skill Point Growth".localize(), ref settings.multiclassSkillPointPolicy, 0, AutoWidth());
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
                HStack("Class Selection".localize(), 1,
                     () => {
                         if (Main.IsInGame) {
                             var characters = Game.Instance.Player.m_PartyAndPets;
                             if (characters == null) { return; }
                             settings.selectedClassToConfigMulticlass = Math.Min(characters.Count, settings.selectedClassToConfigMulticlass);
                             ActionSelectionGrid(ref settings.selectedClassToConfigMulticlass,
                                 characters.Select((ch) => ch.CharacterName).Prepend("Char Gen".localize()).ToArray(),
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
