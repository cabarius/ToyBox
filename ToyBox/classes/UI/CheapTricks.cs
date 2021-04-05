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
        public static void ResetGUI() { }
        public static void OnGUI() {
            if (Main.IsInGame) {
                UI.BeginHorizontal();
                UI.Space(25);
                UI.Label("increment".cyan(), UI.AutoWidth());
                var increment = UI.IntTextField(ref Main.settings.increment, null, UI.Width(150));
                UI.Space(25);
                UI.Toggle("Dividers", ref Main.settings.showDivisions);
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
                            Game.Instance.Player.GainMoney(-loss); }, UI.AutoWidth());
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
                UI.Toggle("Dialog Results", ref Main.settings.previewDialogResults, 0);
                UI.Toggle("Dialog Alignment", ref Main.settings.previewAlignmentRestrictedDialog, 0);
                UI.Toggle("Random Encounters", ref Main.settings.previewRandomEncounters, 0);
                UI.Toggle("Events", ref Main.settings.previewEventResults, 0);
            });
            UI.Div(0, 25);
            UI.HStack("Flags", 3,
                () => { UI.Toggle("Object Highlight Toggle Mode", ref Main.settings.highlightObjectsToggle, 0); },
                () => { UI.Toggle("Whole Team Moves Same Speed", ref Main.settings.toggleMoveSpeedAsOne, 0); },
                () => { UI.Toggle("Instant Cooldown", ref Main.settings.toggleInstantCooldown, 0); },
                () => { UI.Toggle("Unlimited Actions During Turn", ref Main.settings.toggleUnlimitedActionsPerTurn, 0); },
                () => { UI.Toggle("Spontaneous Caster Scroll Copy", ref Main.settings.toggleSpontaneousCopyScrolls, 0); },
                () => { UI.Toggle("Disable Equipment Restrictions", ref Main.settings.toggleEquipmentRestrictions, 0); },
                () => { UI.Toggle("Disable Dialog Restrictions", ref Main.settings.toggleDialogRestrictions, 0); },
                () => { UI.Toggle("Infinite Charges On Items", ref Main.settings.toggleInfiniteItems, 0); },
                () => { UI.Toggle("No Friendly Fire On AOEs", ref Main.settings.toggleNoFriendlyFireForAOE, 0); },
                () => { UI.Toggle("Free Meta-Magic", ref Main.settings.toggleMetamagicIsFree, 0); },
                () => { UI.Toggle("No Material Components", ref Main.settings.toggleMaterialComponent, 0); },
                //() => { UI.Toggle("Restore Spells & Skills After Combat", ref Main.settings.toggleRestoreSpellsAbilitiesAfterCombat,0); },
                //() => { UI.Toggle("Access Remote Characters", ref Main.settings.toggleAccessRemoteCharacters,0); },
                //() => { UI.Toggle("Show Pet Portraits", ref Main.settings.toggleShowAllPartyPortraits,0); },
                () => { UI.Toggle("Instant Rest After Combat", ref Main.settings.toggleInstantRestAfterCombat, 0); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Level Up", 1,
                () => { UI.Slider("Feature Selection Multiplier", ref Main.settings.featsMultiplier, 0, 10, 1, "", UI.AutoWidth()); },
                () => { UI.Toggle("Always Able To Level Up", ref Main.settings.toggleNoLevelUpRestirctions, 0); },
                () => { UI.Toggle("Add Full Hit Die Value", ref Main.settings.toggleFullHitdiceEachLevel, 0); },
                () => {
                    UI.Toggle("Ignore Class And Feat Restrictions", ref Main.settings.toggleIgnorePrerequisites, 0);
                    UI.Space(25);
                    UI.Label("Warning! This currently can break leveling up mythic paths like when leveling  from 3 to 4 it reverts back to Mythic Hero instead of your choosen mythic until level 6 so just turn this off when leveling up mythics until you get past the first screen.".green());

                },
                () => { UI.Toggle("Ignore Prerequisites When Choosing A Feat", ref Main.settings.toggleFeaturesIgnorePrerequisites, 0); },
                () => { UI.Toggle("Ignore Caster Type And Spell Level Restrictions", ref Main.settings.toggleIgnoreCasterTypeSpellLevel, 0); },
                () => { UI.Toggle("Ignore Forbidden Archetypes", ref Main.settings.toggleIgnoreForbiddenArchetype, 0); },
                () => { UI.Toggle("Ignore Required Stat Values", ref Main.settings.toggleIgnorePrerequisiteStatValue, 0); },
                () => { UI.Toggle("Ignore Alignment When Choosing A Class", ref Main.settings.toggleIgnoreAlignmentWhenChoosingClass, 0); },
                () => { UI.Toggle("Skip Spell Selection", ref Main.settings.toggleSkipSpellSelection, 0); },

#if false
                // Do we need these or is it covered by toggleFeaturesIgnorePrerequisites
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref Main.settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass, 0); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref Main.settings.toggle, 0); },
#endif

                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Multipliers", 1,
                () => { UI.Slider("Experience", ref Main.settings.experienceMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Money Earned", ref Main.settings.moneyMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Sell Price", ref Main.settings.vendorSellPriceMultiplier, 0.1f, 30, Main.settings.defaultVendorSellPriceMultiplier, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Encumberance", ref Main.settings.encumberanceMultiplier, 1, 100, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Spells Per Day", ref Main.settings.spellsPerDayMultiplier, 0.1f, 5, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Movement Speed", ref Main.settings.partyMovementSpeedMultiplier, 1, 10, 1, 0, "", UI.AutoWidth()); },
                () => { UI.Slider("Travel Speed", ref Main.settings.travelSpeedMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Companion Cost", ref Main.settings.companionCostMultiplier, 0, 5, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Enemy HP Multiplier", ref Main.settings.enemyBaseHitPointsMultiplier, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { UI.Slider("Buff Duration", ref Main.settings.buffDurationMultiplierValue, 0.1f, 10, 1, 1, "", UI.AutoWidth()); },
                () => { }
                );

            UI.Div(0, 25);
            UI.HStack("Character Creation", 1,
                () => { UI.Slider("Build Points (Main)", ref Main.settings.characterCreationAbilityPointsPlayer, 1, 200, 25, "", UI.AutoWidth()); },
                () => { UI.Slider("Build Points (Mercenary)", ref Main.settings.characterCreationAbilityPointsMerc, 1, 200, 20, "", UI.AutoWidth()); },
                () => { UI.Slider("Ability Max", ref Main.settings.characterCreationAbilityPointsMax, 0, 50, 18, "", UI.AutoWidth()); },
                () => { UI.Slider("Ability Min", ref Main.settings.characterCreationAbilityPointsMin, 0, 50, 7, "", UI.AutoWidth()); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Crusade", 1,
                () => { UI.Toggle("Instant Events", ref Main.settings.toggleInstantEvent, 0); },
                () => {
                    UI.Slider("Build Time Modifer", ref Main.settings.kingdomBuildingTimeModifier, -10, 10, 0, 1, "", UI.AutoWidth());
                    var instance = KingdomState.Instance;
                    if (instance != null) {
                        instance.BuildingTimeModifier = Main.settings.kingdomBuildingTimeModifier;
                    }
                },
                () => { }
                );
            UI.Space(25);
        }
    }
}