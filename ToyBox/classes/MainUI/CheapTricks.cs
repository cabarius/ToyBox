// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
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
using ModKit;

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
                UI.EndHorizontal();
                var mainChar = Game.Instance.Player.MainCharacter.Value;
                var kingdom = KingdomState.Instance;
                UI.Div(0, 25);
                UI.HStack("Resources", 1,
                    () => {
                        var money = Game.Instance.Player.Money;
                        UI.Label("Gold".cyan(), UI.Width(150));
                        UI.Label(money.ToString().orange().bold(), UI.Width(200));
                        UI.ActionButton($"Gain {increment}", () => Game.Instance.Player.GainMoney(increment), UI.AutoWidth());
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
                    });
                if (kingdom != null) {
                    var moraleState = kingdom.MoraleState;
                    UI.Div(0, 25);
                    UI.HStack("Crusade", 1,
                        () => {
                            var value = moraleState.CurrentValue;
                            UI.Slider("Morale", ref value, moraleState.MinValue, moraleState.MaxValue, 1, "", UI.AutoWidth());
                            moraleState.CurrentValue = value;
                        },
                        () => {
                            var value = moraleState.MaxValue;
                            UI.Slider("Max Morale", ref value, -200, 200, 20, "", UI.AutoWidth());
                            moraleState.MaxValue = value;
                        },
                        () => {
                            var value = moraleState.MinValue;
                            UI.Slider("Min Morale", ref value, -200, 200, -100, "", UI.AutoWidth());
                            moraleState.MinValue = value;
                        },

                        () => {
                            UI.Label("Finances".cyan(), UI.Width(150));
                            UI.Label(kingdom.Resources.Finances.ToString().orange().bold(), UI.Width(200));
                            UI.ActionButton($"Gain {increment}", () => {
                                kingdom.Resources += KingdomResourcesAmount.FromFinances(increment);
                            }, UI.AutoWidth());
                            UI.ActionButton($"Lose {increment}", () => {
                                kingdom.Resources -= KingdomResourcesAmount.FromFinances(increment);
                            }, UI.AutoWidth());
                        },
                        () => {
                            UI.Label("Materials".cyan(), UI.Width(150));
                            UI.Label(kingdom.Resources.Materials.ToString().orange().bold(), UI.Width(200));
                            UI.ActionButton($"Gain {increment}", () => {
                                kingdom.Resources += KingdomResourcesAmount.FromMaterials(increment);
                            }, UI.AutoWidth());
                            UI.ActionButton($"Lose {increment}", () => {
                                kingdom.Resources -= KingdomResourcesAmount.FromMaterials(increment);
                            }, UI.AutoWidth());
                        },
                        () => {
                            UI.Label("Favors".cyan(), UI.Width(150));
                            UI.Label(kingdom.Resources.Favors.ToString().orange().bold(), UI.Width(200));
                            UI.ActionButton($"Gain {increment}", () => {
                                kingdom.Resources += KingdomResourcesAmount.FromFavors(increment);
                            }, UI.AutoWidth());
                            UI.ActionButton($"Lose {increment}", () => {
                                kingdom.Resources -= KingdomResourcesAmount.FromFavors(increment);
                            }, UI.AutoWidth());
                        });
                }
            }
            UI.Div(0, 25);
            UI.HStack("Combat", 4,
                () => UI.ActionButton("Rest All", () => CheatsCombat.RestAll()),
                () => UI.ActionButton("Empowered", () => CheatsCombat.Empowered("")),
                () => UI.ActionButton("Full Buff Please", () => CheatsCombat.FullBuffPlease("")),
                () => UI.ActionButton("Remove Buffs", () => Actions.RemoveAllBuffs()),
                () => UI.ActionButton("Remove Death's Door", () => CheatsCombat.DetachDebuff()),
                () => UI.ActionButton("Kill All Enemies", () => CheatsCombat.KillAll()),
                () => UI.ActionButton("Summon Zoo", () => CheatsCombat.SpawnInspectedEnemiesUnderCursor(""))
                );
            UI.Div(0, 25);
            UI.HStack("Common", 4,
                () => UI.ActionButton("Teleport Party To You", () => Actions.TeleportPartyToPlayer()),
                () => UI.ActionButton("Go To Global Map", () => Actions.TeleportToGlobalMap()),
                () => UI.ActionButton("Run All Perception Checks", () => Actions.RunPerceptionTriggers()),
                () => {
                    UI.ActionButton("Set Perception to 40", () => {
                        CheatsCommon.StatPerception();
                        Actions.RunPerceptionTriggers();
                    });
                },
                () => UI.ActionButton("Change Weather", () => CheatsCommon.ChangeWeather("")),
                () => UI.ActionButton("Give All Items", () => CheatsUnlock.CreateAllItems("")),
                //                    () => { UI.ActionButton("Change Party", () => { Actions.ChangeParty(); }); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Preview", 0, () => {
                UI.Toggle("Dialog Results", ref settings.previewDialogResults, 0);
                UI.Space(25);
                UI.Toggle("Dialog Alignment", ref settings.previewAlignmentRestrictedDialog, 0);
                UI.Space(25);
                UI.Toggle("Random Encounters", ref settings.previewRandomEncounters, 0);
                UI.Space(25);
                UI.Toggle("Events", ref settings.previewEventResults, 0);
            });
            UI.Div(0, 25);
            UI.HStack("Tweaks", 1,
                () => UI.Toggle("Allow Achievements While Using Mods", ref settings.toggleAllowAchievementsDuringModdedGame, 0),
                () => UI.Toggle("Object Highlight Toggle Mode", ref settings.highlightObjectsToggle, 0),
                () => UI.Toggle("Whole Team Moves Same Speed", ref settings.toggleMoveSpeedAsOne, 0),

                () => UI.Toggle("Infinite Abilities", ref settings.toggleInfiniteAbilities, 0),
                () => UI.Toggle("Infinite Spell Casts", ref settings.toggleInfiniteSpellCasts, 0),
                () => UI.Toggle("No Material Components", ref settings.toggleMaterialComponent, 0),

                () => UI.Toggle("Unlimited Actions During Turn", ref settings.toggleUnlimitedActionsPerTurn, 0),
                () => UI.Toggle("Infinite Charges On Items", ref settings.toggleInfiniteItems, 0),

                () => UI.Toggle("Instant Cooldown", ref settings.toggleInstantCooldown, 0),

                () => UI.Toggle("Highlight Copyable Scrolls", ref settings.toggleHighlightCopyableScrolls, 0),
                () => UI.Toggle("Spontaneous Caster Scroll Copy", ref settings.toggleSpontaneousCopyScrolls, 0),

                () => UI.Toggle("Disable Equipment Restrictions", ref settings.toggleEquipmentRestrictions, 0),
                () => UI.Toggle("Disable Dialog Restrictions", ref settings.toggleDialogRestrictions, 0),

                () => UI.Toggle("No Friendly Fire On AOEs", ref settings.toggleNoFriendlyFireForAOE, 0),
                () => UI.Toggle("Free Meta-Magic", ref settings.toggleMetamagicIsFree, 0),

                () => UI.Toggle("No Fog Of War", ref settings.toggleNoFogOfWar, 0),
                //() => UI.Toggle("Restore Spells & Skills After Combat", ref settings.toggleRestoreSpellsAbilitiesAfterCombat,0),
                //() => UI.Toggle("Access Remote Characters", ref settings.toggleAccessRemoteCharacters,0),
                //() => UI.Toggle("Show Pet Portraits", ref settings.toggleShowAllPartyPortraits,0),
                () => UI.Toggle("Instant Rest After Combat", ref settings.toggleInstantRestAfterCombat, 0),
                () => UI.Toggle("Auto Load Last Save On Launch", ref settings.toggleAutomaticallyLoadLastSave, 0),
                () => UI.Toggle("Enable multiple romance (experimental)", ref settings.toggleMultipleRomance, 0),
                () => UI.Toggle("Spiders begone (experimental)", ref settings.toggleSpiderBegone, 0),
                () => UI.Toggle("Make Tutorials Not Appear If Disabled In Settings", ref settings.toggleForceTutorialsToHonorSettings),
                () => UI.Toggle("Refill consumables in belt slots if in inventory", ref settings.togglAutoEquipConsumables),

                () => { }
                );
            UI.Div(153, 25);
            UI.HStack("", 1,
                () => UI.EnumGrid("Disable Attacks Of Opportunity", ref settings.noAttacksOfOpportunitySelection, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Can Move Through", ref settings.allowMovementThroughSelection, 0, UI.AutoWidth()),
                () => { UI.Space(328); UI.Label("This allows characters you control to move through the selected category of units during combat".green(), UI.AutoWidth()); }
#if false
                () => { UI.Slider("Collision Radius Multiplier", ref settings.collisionRadiusMultiplier, 0f, 2f, 1f, 1, "", UI.AutoWidth()); },
#endif
                );

            UI.HStack("Multipliers", 1,
                () => UI.LogSlider("Experience", ref settings.experienceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Money Earned", ref settings.moneyMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Vendor Sell Price", ref settings.vendorSellPriceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Vendor Buy Price", ref settings.vendorBuyPriceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.Slider("Increase Carry Capacity", ref settings.encumberanceMultiplier, 1, 100, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Spells Per Day", ref settings.spellsPerDayMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Movement Speed", ref settings.partyMovementSpeedMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Travel Speed", ref settings.travelSpeedMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Companion Cost", ref settings.companionCostMultiplier, 0, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Enemy HP Multiplier", ref settings.enemyBaseHitPointsMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Buff Duration", ref settings.buffDurationMultiplierValue, 0f, 999, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Field Of View", ref settings.fovMultiplier, 0.4f, settings.fovMultiplierMax, 1, 2, "", UI.AutoWidth()),
                () => UI.LogSlider("Max Field Of View", ref settings.fovMultiplierMax, 1.5f, 3f, 1, 2, "", UI.AutoWidth()),
                () => {
                    UI.Space(328); UI.Label("Experimental: Increasing this may cause performance issues when rotating".green(), UI.AutoWidth());
                },
                () => UI.LogSlider("Game Time Scale", ref settings.timeScaleMultiplier, 0f, 20, 1, 2, "", UI.AutoWidth()),
                () => { }
                );
            Game.Instance.TimeController.DebugTimeScale = settings.timeScaleMultiplier;
            UI.Div(0, 25);
            UI.HStack("Dice Rolls", 1,
                () => UI.EnumGrid("All Attacks Hit", ref settings.allAttacksHit, 0, UI.AutoWidth()),
                () => UI.EnumGrid("All Hits Critical", ref settings.allHitsCritical, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Roll With Avantage", ref settings.rollWithAdvantage, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Roll With Disavantage", ref settings.rollWithDisadvantage, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 20", ref settings.alwaysRoll20, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 1", ref settings.alwaysRoll1, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Never Roll 20", ref settings.neverRoll20, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Never Roll 1", ref settings.neverRoll1, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 20 Initiative ", ref settings.roll20Initiative, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 1 Initiative", ref settings.roll1Initiative, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Always Roll 20 Out Of Combat", ref settings.alwaysRoll20OutOfCombat, 0, UI.AutoWidth()),
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Summons", 1,
                () => UI.Toggle("Make Controllable", ref settings.toggleMakeSummmonsControllable, 0),
                () => {
                    using (UI.VerticalScope()) {
                        UI.Div(0, 25);
                        using (UI.HorizontalScope()) {
                            UI.Label("Primary".orange(), UI.AutoWidth()); UI.Space(215); UI.Label("good for party".green());
                        }
                        UI.Space(25);
                        UI.EnumGrid("Modify Summons For", ref settings.summonTweakTarget1, 0, UI.AutoWidth());
                        UI.LogSlider("Duration Multiplier", ref settings.summonDurationMultiplier1, 0f, 20, 1, 2, "", UI.AutoWidth());
                        UI.Slider("Level Increase/Decrease", ref settings.summonLevelModifier1, -20f, +20f, 0f, 0, "", UI.AutoWidth());
                        UI.Div(0, 25);
                        using (UI.HorizontalScope()) {
                            UI.Label("Secondary".orange(), UI.AutoWidth()); UI.Space(215); UI.Label("good for larger group or to reduce enemies".green());
                        }
                        UI.Space(25);
                        UI.EnumGrid("Modify Summons For", ref settings.summonTweakTarget2, 0, UI.AutoWidth());
                        UI.LogSlider("Duration Multiplier", ref settings.summonDurationMultiplier2, 0f, 20, 1, 2, "", UI.AutoWidth());
                        UI.Slider("Level Increase/Decrease", ref settings.summonLevelModifier2, -20f, +20f, 0f, 0, "", UI.AutoWidth());
                    }
                },
                () => { }
             );
            UI.Div(0, 25);
            UI.HStack("Crusade Extras - Coming Soon", 1,
                    () => UI.Toggle("Instant Events", ref settings.toggleInstantEvent, 0),
                    () => {
                        UI.Slider("Build Time Modifer", ref settings.kingdomBuildingTimeModifier, -10, 10, 0, 1, "", UI.AutoWidth());
                        var instance = KingdomState.Instance;
                        if (instance != null) {
                            instance.BuildingTimeModifier = settings.kingdomBuildingTimeModifier;
                        }
                    },
                    () => { }
                    );
        }
    }
}