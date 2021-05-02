using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using ModKit;

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
                        () => {
                            UI.Toggle("All Appearance Options", ref settings.toggleAllRaceCustomizations, 0);
                            UI.Space(25);
                            UI.Label("Allows you to choose all appearance options from any race".green());
                        },
                        () => {
                            UI.Toggle("Gender Bending", ref settings.toggleIgnoreGenderRestrictions, 0);
                            UI.Space(25);
                            UI.Label("Removes gender restrictions from appearance options".green());
                        },
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
                () => UI.Slider("Feature Selection Multiplier", ref settings.featsMultiplier, 0, 10, 1, "", UI.AutoWidth()),
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
#if DEBUG
            UI.Div(0, 25);
            UI.HStack("Multiple Classes", 1,
                () => UI.Label("Experimental Preview".magenta(), UI.AutoWidth()),
                () => {
                    UI.Toggle("Multiple Classes On Level-Up", ref settings.toggleMulticlass, 0);
                    UI.Space(25);
                    UI.Label("With this enabled you can configure characters in the Party Editor to gain levels in additional classes whenever they level up. Please go to Party Editor > Character > Classes to configure this".green());
                },
                () => UI.Toggle("Use Highest Hit Die", ref settings.toggleTakeHighestHitDie, 0),
                () => UI.Toggle("Use Highest Skill Points", ref settings.toggleTakeHighestSkillPoints, 0),
                () => UI.Toggle("Use Highest BAB ", ref settings.toggleTakeHighestBAB, 0),
                () => UI.Toggle("Use Highest Save By Recalc", ref settings.toggleTakeHighestSaveByRecalculation, 0),
                () => UI.Toggle("Use Highest Save ", ref settings.toggleTakeHighestSaveByAlways, 0),
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
