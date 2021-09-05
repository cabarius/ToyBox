using ModKit;

namespace ToyBox
{
    public class LevelUp
    {
        public static Settings settings => Main.settings;

        public static void ResetGUI() { }

        public static void OnGUI()
        {
            UI.HStack("Character Creation", 1,
                      () => UI.Slider("Build Points (Main)", ref settings.characterCreationAbilityPointsPlayer, 1, 200, 25, "", UI.AutoWidth()),
                      () => UI.Slider("Build Points (Mercenary)", ref settings.characterCreationAbilityPointsMerc, 1, 200, 20, "", UI.AutoWidth()),
                      () => UI.Slider("Ability Max", ref settings.characterCreationAbilityPointsMax, 0, 50, 18, "", UI.AutoWidth()),
                      () => UI.Slider("Ability Min", ref settings.characterCreationAbilityPointsMin, 0, 50, 7, "", UI.AutoWidth()),
#if DEBUG
                      () =>
                      {
                          UI.Toggle("Gender Bending", ref settings.toggleIgnoreGenderRestrictions);
                          UI.Space(25);
                          UI.Label("Removes gender restrictions from appearance options".green());
                      },
#endif
                      () => { }
            );

            UI.Div(0, 25);

            UI.HStack("Unlocks", 4, () =>
                                    {
                                        UI.ActionButton("All Mythic Paths", Actions.UnlockAllMythicPaths);
                                        UI.Space(25);

                                        UI.Label(
                                            "Warning! Using this might break your game somehow. Recommend for experimental tinkering like trying out different builds, and not for actually playing the game."
                                                .green());
                                    });

            UI.Div(0, 25);

            UI.HStack("Create & Level Up", 1,
                      () => UI.Slider("Feature Selection Multiplier", ref settings.featsMultiplier, 0, 10, 1, "", UI.AutoWidth()),
                      () =>
                      {
                          UI.Toggle("Ignore Attribute Cap", ref settings.toggleIgnoreAttributeCap);
                          UI.Space(25);
                          UI.Toggle("Ignore Remaining Attribute Points", ref settings.toggleIgnoreAttributePointsRemaining);
                      },
                      () =>
                      {
                          UI.Toggle("Ignore Skill Cap", ref settings.toggleIgnoreSkillCap);
                          UI.Space(73);
                          UI.Toggle("Ignore Remaining Skill Points", ref settings.toggleIgnoreSkillPointsRemaining);
                      },
                      () => UI.Toggle("Always Able To Level Up", ref settings.toggleNoLevelUpRestrictions),
                      () => UI.Toggle("Add Full Hit Die Value", ref settings.toggleFullHitdiceEachLevel),
                      () =>
                      {
                          UI.Toggle("Ignore Class And Feat Restrictions", ref settings.toggleIgnorePrerequisites);
                          UI.Space(25);

                          UI.Label("Experimental".cyan() +
                                   ": in addition to regular leveling, this allows you to choose any mythic class each time you level up starting from level 1. This may have interesting and unexpected effects. Backup early and often..."
                                       .green());
                      },
                      () => UI.Toggle("Ignore Prerequisites When Choosing A Feat", ref settings.toggleFeaturesIgnorePrerequisites),
                      () => UI.Toggle("Ignore Caster Type And Spell Level Restrictions", ref settings.toggleIgnoreCasterTypeSpellLevel),
                      () => UI.Toggle("Ignore Forbidden Archetypes", ref settings.toggleIgnoreForbiddenArchetype),
                      () => UI.Toggle("Ignore Required Stat Values", ref settings.toggleIgnorePrerequisiteStatValue),
                      () => UI.Toggle("Ignore Alignment When Choosing A Class", ref settings.toggleIgnoreAlignmentWhenChoosingClass),
                      () => UI.Toggle("Skip Spell Selection", ref settings.toggleSkipSpellSelection),
#if DEBUG
                      () => UI.Toggle("Lock Character Level", ref settings.toggleLockCharacterLevel),
#endif
                      () => { }
            );
#if DEBUG
            UI.Div(0, 25);

            UI.HStack("Multiple Classes", 1,
                      () => UI.Label("Experimental Preview".magenta(), UI.AutoWidth()),
                      () =>
                      {
                          UI.Toggle("Multiple Classes On Level-Up", ref settings.toggleMulticlass);
                          UI.Space(25);

                          UI.Label(
                              "With this enabled you can configure characters in the Party Editor to gain levels in additional classes whenever they level up. Please go to Party Editor > Character > Classes to configure this"
                                  .green());
                      },
                      () => UI.EnumGrid("Hit Point (Hit Die) Growth", ref settings.multiclassHitPointPolicy, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Basic Attack Growth Pr", ref settings.multiclassBABPolicy, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Saving Throw Growth", ref settings.multiclassSavingThrowPolicy, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Skill Point Growth", ref settings.multiclassSkillPointPolicy, 0, UI.AutoWidth()),
                      () => UI.Toggle("Use Recalculate Caster Levels", ref settings.toggleRecalculateCasterLevelOnLevelingUp),
                      () => UI.Toggle("Restrict Caster Level To Current", ref settings.toggleRestrictCasterLevelToCharacterLevel),
                      () => UI.Toggle("Restrict Class Level for Prerequisites to Caster Level",
                                      ref settings.toggleRestrictClassLevelForPrerequisitesToCharacterLevel),
                      () => UI.Toggle("Fix Favored Class HP", ref settings.toggleFixFavoredClassHP),
                      () => UI.Toggle("Always Receive Favored Class HP", ref settings.toggleAlwaysReceiveFavoredClassHP),
                      () => UI.Toggle("Always Receive Favored Class HP Except Prestige", ref settings.toggleAlwaysReceiveFavoredClassHPExceptPrestige),
                      () => { }
            );

            if (settings.toggleMulticlass)
            {
                UI.Div(0, 25);

                UI.HStack("Character Generation", 1,
                          () =>
                              UI.Label("Choose default multiclass setting to use during creation of new characters".green(), UI.AutoWidth())
                );

                var multiclassSet = settings.charGenMulticlassSet;
                MulticlassPicker.OnGUI(multiclassSet, 150);
                settings.charGenMulticlassSet = multiclassSet;
            }
#endif
        }
    }
}