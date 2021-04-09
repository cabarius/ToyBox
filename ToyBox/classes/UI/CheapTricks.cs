// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
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
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace ToyBox {
    public static class CheapTricks {
        public static Settings settings { get { return Main.settings; } }
        public static void ResetGUI() { }
        public static void OnGUI() {
            if (Main.IsInGame) {
                UI.BeginHorizontal();
                UI.Space(25);
                UI.Label("increment".cyan(), UI.AutoWidth());
                var increment = UI.IntTextField(ref settings.increment, null, UI.Width(150));
                UI.Space(25);
                UI.Toggle("Dividers", ref settings.showDivisions);
                UI.EndHorizontal();
                UI.Space(25);
                var mainChar = Game.Instance.Player.MainCharacter.Value;
                UI.Div(0, 25);
                UI.HStack("Resources", 1,
                    () => {
                        var money = Game.Instance.Player.Money;
                        UI.Label("Gold".cyan(), UI.Width(150));
                        UI.Label(money.ToString().orange().bold(), UI.Width(200));
                        UI.ActionButton($"Gain {increment}", () => { Game.Instance.Player.GainMoney(increment); }, UI.AutoWidth());
                        UI.ActionButton($"Lose {increment}", () => {
                            var loss = Math.Min(money, increment);
                            Game.Instance.Player.GainMoney(-loss);
                        }, UI.AutoWidth());
                    },
                    () => {
                        var exp = mainChar.Progression.Experience;
                        UI.Label("Experience".cyan(), UI.Width(150));
                        UI.Label(exp.ToString().orange().bold(), UI.Width(200));
                        UI.ActionButton($"Gain {increment}", () => {
                            Game.Instance.Player.GainPartyExperience(increment);
                        }, UI.AutoWidth());
#if false
                        UI.ActionButton($"Lose {increment}", () => {
                            var loss = Math.Min(exp, increment);
                            Game.Instance.Player.GainPartyExperience(-loss);
                        }, UI.AutoWidth());
#endif
                    },
                    () => { }
                    );
            }
            UI.Div(0, 25);
            UI.HStack("Combat", 4,
                () => { UI.ActionButton("Rest All", () => { CheatsCombat.RestAll(); }); },
                () => { UI.ActionButton("Empowered", () => { CheatsCombat.Empowered(""); }); },
                () => { UI.ActionButton("Full Buff Please", () => { CheatsCombat.FullBuffPlease(""); }); },
                () => { UI.ActionButton("Remove Buffs", () => { Actions.RemoveAllBuffs(); }); },
                () => { UI.ActionButton("Remove Death's Door", () => { CheatsCombat.DetachDebuff(); }); },
                () => { UI.ActionButton("Kill All Enemies", () => { CheatsCombat.KillAll(); }); },
                () => { UI.ActionButton("Summon Zoo", () => { CheatsCombat.SpawnInspectedEnemiesUnderCursor(""); }); }
                );
            UI.Div(0, 25);
            UI.HStack("Common", 4,
                () => { UI.ActionButton("Teleport Party To You", () => { Actions.TeleportPartyToPlayer(); }); },
                () => { UI.ActionButton("Perception Checks", () => { Actions.RunPerceptionTriggers(); }); },
                () => {
                    UI.ActionButton("Set Perception to 40", () => {
                        CheatsCommon.StatPerception();
                        Actions.RunPerceptionTriggers();
                    });
                },
                () => { UI.ActionButton("Change Weather", () => { CheatsCommon.ChangeWeather(""); }); },
                () => { UI.ActionButton("Give All Items", () => { CheatsUnlock.CreateAllItems(""); }); },
                //                    () => { UI.ActionButton("Change Party", () => { Actions.ChangeParty(); }); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Unlocks", 4, () => {
                UI.ActionButton("All Mythic Paths", () => { Actions.UnlockAllMythicPaths(); });
                UI.Space(25);
                UI.Label("Warning! Using this might break your game somehow. Recommend for experimental tinkering like trying out different builds, and not for actually playing the game.".green());
            });
            UI.Div(0, 25);
            UI.HStack("Preview", 0, () => {
                UI.Toggle("Dialog Results", ref settings.previewDialogResults, 0);
                UI.Toggle("Dialog Alignment", ref settings.previewAlignmentRestrictedDialog, 0);
                UI.Toggle("Random Encounters", ref settings.previewRandomEncounters, 0);
                UI.Toggle("Events", ref settings.previewEventResults, 0);
            });
            UI.Div(0, 25);
            UI.HStack("Tweaks", 3,
                () => { UI.Toggle("Object Highlight Toggle Mode", ref settings.highlightObjectsToggle, 0); },
                () => { UI.Toggle("Whole Team Moves Same Speed", ref settings.toggleMoveSpeedAsOne, 0); },
                () => { UI.Toggle("Unlimited Actions During Turn", ref settings.toggleUnlimitedActionsPerTurn, 0); },

                () => { UI.Toggle("Infinite Abilities", ref settings.toggleInfiniteAbilities    , 0); },
                () => { UI.Toggle("Infinite Spell Casts", ref settings.toggleInfiniteSpellCasts, 0); },
                () => { UI.Toggle("Infinite Charges On Items", ref settings.toggleInfiniteItems, 0); },

                () => { UI.Toggle("Instant Cooldown", ref settings.toggleInstantCooldown, 0); },
                () => { UI.Toggle("Spontaneous Caster Scroll Copy", ref settings.toggleSpontaneousCopyScrolls, 0); },
                () => { UI.Toggle("Disable Equipment Restrictions", ref settings.toggleEquipmentRestrictions, 0); },

                () => { UI.Toggle("Disable Dialog Restrictions", ref settings.toggleDialogRestrictions, 0); },
                () => { UI.Toggle("No Friendly Fire On AOEs", ref settings.toggleNoFriendlyFireForAOE, 0); },
                () => { UI.Toggle("Free Meta-Magic", ref settings.toggleMetamagicIsFree, 0); },

                () => { UI.Toggle("No Fog Of War", ref settings.toggleNoFogOfWar, 0); },
                () => { UI.Toggle("No Material Components", ref settings.toggleMaterialComponent, 0); },
                //() => { UI.Toggle("Restore Spells & Skills After Combat", ref settings.toggleRestoreSpellsAbilitiesAfterCombat,0); },
                //() => { UI.Toggle("Access Remote Characters", ref settings.toggleAccessRemoteCharacters,0); },
                //() => { UI.Toggle("Show Pet Portraits", ref settings.toggleShowAllPartyPortraits,0); },
                () => { UI.Toggle("Instant Rest After Combat", ref settings.toggleInstantRestAfterCombat, 0); },
                () => { }
                );
            UI.HStack("", 1,
                () => {
                    UI.EnumGrid("Disable Attacks Of Opportunity",
                        ref settings.noAttacksOfOpportunitySelection, 0, UI.AutoWidth());
                },
                () => {
                    UI.EnumGrid("Can Move Through", ref settings.allowMovementThroughSelection, 0, UI.AutoWidth());
                },
                () => { UI.Space(328);  UI.Label("This allows characters you control to move through the selected category of units during combat".green(), UI.AutoWidth());  },
#if false
                () => { UI.Slider("Collision Radius Multiplier", ref settings.collisionRadiusMultiplier, 0f, 2f, 1f, 1, "", UI.AutoWidth()); },
#endif
                () => {}
                );
            UI.Div(0, 25);
            UI.HStack("Level Up", 1,
                () => { UI.Slider("Feature Selection Multiplier", ref settings.featsMultiplier, 0, 10, 1, "", UI.AutoWidth()); },
                () => { UI.Toggle("Always Able To Level Up", ref settings.toggleNoLevelUpRestrictions, 0); },
                () => { UI.Toggle("Add Full Hit Die Value", ref settings.toggleFullHitdiceEachLevel, 0); },
                () => {
                    UI.Toggle("Ignore Class And Feat Restrictions", ref settings.toggleIgnorePrerequisites, 0);
                    UI.Space(25);
                    UI.Label("Experimental".cyan() + ": in addition to regular leveling, this allows you to choose any mythic class each time you level up starting from level 1. This may have interesting and unexpected effects. Backup early and often...".green());

                },
                () => { UI.Toggle("Ignore Prerequisites When Choosing A Feat", ref settings.toggleFeaturesIgnorePrerequisites, 0); },
                () => { UI.Toggle("Ignore Caster Type And Spell Level Restrictions", ref settings.toggleIgnoreCasterTypeSpellLevel, 0); },
                () => { UI.Toggle("Ignore Forbidden Archetypes", ref settings.toggleIgnoreForbiddenArchetype, 0); },
                () => { UI.Toggle("Ignore Required Stat Values", ref settings.toggleIgnorePrerequisiteStatValue, 0); },
                () => { UI.Toggle("Ignore Alignment When Choosing A Class", ref settings.toggleIgnoreAlignmentWhenChoosingClass, 0); },
                () => { UI.Toggle("Skip Spell Selection", ref settings.toggleSkipSpellSelection, 0); },

#if false
                // Do we need these or is it covered by toggleFeaturesIgnorePrerequisites
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass, 0); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref settings.toggle, 0); },
#endif

                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Multipliers", 1,
                () => { UI.Slider("Experience", ref settings.experienceMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Money Earned", ref settings.moneyMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Vendor Sell Price", ref settings.vendorSellPriceMultiplier, 0f, 30, settings.defaultVendorSellPriceMultiplier, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Vendor Buy Price", ref settings.vendorBuyPriceMultiplier, 0f, 30, settings.defaultVendorSellPriceMultiplier, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Encumberance", ref settings.encumberanceMultiplier, 1, 100, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Spells Per Day", ref settings.spellsPerDayMultiplier, 0.1f, 5, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Movement Speed", ref settings.partyMovementSpeedMultiplier, 1, 10, 1, 0, "", UI.AutoWidth()); },
                () => { UI.Slider("Travel Speed", ref settings.travelSpeedMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Companion Cost", ref settings.companionCostMultiplier, 0, 5, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Enemy HP Multiplier", ref settings.enemyBaseHitPointsMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Buff Duration", ref settings.buffDurationMultiplierValue, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Dice Rolls", 1,
                () => UI.EnumGrid("All Hits Critical", ref settings.allHitsCritical, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Roll With Avantage", ref settings.rollWithAdvantage, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Roll With Disavantage", ref settings.rollWithDisadvantage, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 20", ref settings.alwaysRoll20, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 1", ref settings.alwaysRoll1, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Never Roll 20", ref settings.neverRoll20, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Never Roll 1", ref settings.neverRoll1, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 20 Initiative ", ref settings.roll20Initiative, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 1 Initiative", ref settings.roll1Initiative, 0, UI.AutoWidth()),
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Character Creation", 1,
                () => { UI.Slider("Build Points (Main)", ref settings.characterCreationAbilityPointsPlayer, 1, 200, 25, "", UI.AutoWidth()); },
                () => { UI.Slider("Build Points (Mercenary)", ref settings.characterCreationAbilityPointsMerc, 1, 200, 20, "", UI.AutoWidth()); },
                () => { UI.Slider("Ability Max", ref settings.characterCreationAbilityPointsMax, 0, 50, 18, "", UI.AutoWidth()); },
                () => { UI.Slider("Ability Min", ref settings.characterCreationAbilityPointsMin, 0, 50, 7, "", UI.AutoWidth()); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Crusade", 1,
                () => { UI.Toggle("Instant Events", ref settings.toggleInstantEvent, 0); },
                () => {
                    UI.Slider("Build Time Modifer", ref settings.kingdomBuildingTimeModifier, -10, 10, 0, 1, "", UI.AutoWidth());
                    var instance = KingdomState.Instance;
                    if (instance != null) {
                        instance.BuildingTimeModifier = settings.kingdomBuildingTimeModifier;
                    }
                },
                () => { }
                );
            UI.Space(25);
        }
    }
}