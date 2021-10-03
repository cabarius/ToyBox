﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Kingdom;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Alignments;
using ModKit;
using UnityEngine;
using UnityModManagerNet;
using static ModKit.UI;

namespace ToyBox {
    public static class BagOfTricks {
        public static Settings settings => Main.settings;

        // cheats combat
        const string RestAll = "Rest All";
        const string Empowered = "Empowered";
        const string FullBuffPlease = "Common Buffs";
        const string GoddesBuffs = "Buff Like A Godess";
        const string RemoveBuffs = "Remove Buffs";
        const string RemoveDeathsDoor = "Remove Deaths Door";
        const string KillAllEnemies = "Kill All Enemies";
        const string SummonZoo = "Summon Zoo";
        const string LobotomizeAllEnemies = "Lobotomize Enemies";

        // cheats common
        const string TeleportPartyToYou = "Teleport Party To You";
        const string GoToGlobalMap = "Go To Global Map";
        const string RerollPerception = "Reroll Perception";
        const string ChangeParty = "Change Party";

        // other
        const string TimeScaleMultToggle = "Main/Alt Timescale";

        public static void OnLoad() {
            // Combat
            KeyBindings.RegisterAction(RestAll, () => CheatsCombat.RestAll());
            KeyBindings.RegisterAction(Empowered, () => CheatsCombat.Empowered(""));
            KeyBindings.RegisterAction(FullBuffPlease, () => CheatsCombat.FullBuffPlease(""));
            KeyBindings.RegisterAction(GoddesBuffs, () => CheatsCombat.Iddqd(""));
            KeyBindings.RegisterAction(RemoveBuffs, () => Actions.RemoveAllBuffs());
            KeyBindings.RegisterAction(RemoveDeathsDoor, () => CheatsCombat.DetachDebuff());
            KeyBindings.RegisterAction(KillAllEnemies, () => CheatsCombat.KillAll());
            //KeyBindings.RegisterAction(SummonZoo, () => CheatsCombat.SpawnInspectedEnemiesUnderCursor(""));
            KeyBindings.RegisterAction(LobotomizeAllEnemies, () => Actions.LobotomizeAllEnemies());
            // Common
            KeyBindings.RegisterAction(TeleportPartyToYou, () => Teleport.TeleportPartyToPlayer());
            KeyBindings.RegisterAction(GoToGlobalMap, () => Teleport.TeleportToGlobalMap());
            KeyBindings.RegisterAction(RerollPerception, () => Actions.RunPerceptionTriggers());
            KeyBindings.RegisterAction(ChangeParty, () => { Actions.ChangeParty(); });
            // Other
            KeyBindings.RegisterAction(TimeScaleMultToggle, () => {
                settings.useAlternateTimeScaleMultiplier = !settings.useAlternateTimeScaleMultiplier;
                Actions.ApplyTimeScale();
            });
        }
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
            }
            UI.Div(0, 25);
            UI.HStack("Combat", 2,
                () => UI.BindableActionButton(RestAll),
                () => UI.BindableActionButton(FullBuffPlease),
                () => UI.BindableActionButton(Empowered),
                () => UI.BindableActionButton(GoddesBuffs),
                () => UI.BindableActionButton(RemoveBuffs),
                () => UI.BindableActionButton(RemoveDeathsDoor),
                () => UI.BindableActionButton(KillAllEnemies),
                //() => UI.BindableActionButton(SummonZoo),
                () => UI.BindableActionButton(LobotomizeAllEnemies),
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Common", 2,
                () => UI.BindableActionButton(TeleportPartyToYou),
                () => UI.BindableActionButton(GoToGlobalMap),
                () => UI.BindableActionButton(RerollPerception),
                () => UI.BindableActionButton(ChangeParty),
                () => {
                    UI.NonBindableActionButton("Set Perception to 40", () => {
                        CheatsCommon.StatPerception();
                        Actions.RunPerceptionTriggers();
                    });
                },
                () => UI.NonBindableActionButton("Change Weather", () => CheatsCommon.ChangeWeather("")),
                () => UI.NonBindableActionButton("Give All Items", () => CheatsUnlock.CreateAllItems("")),
                () => UI.NonBindableActionButton("Identify All", () => Actions.IdentifyAll()),
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
            UI.HStack("Quality of Life", 1,
                () => {
                    UI.Toggle("Allow Achievements While Using Mods", ref settings.toggleAllowAchievementsDuringModdedGame, 0);
                    UI.Label("This is intended for you to be able to enjoy the game while using mods that enhance your quality of life.  Please be mindful of the player community and avoid using this mod to trivialize earning prestige achievements like Sadistic Gamer. The author is in discussion with Owlcat about reducing the scope of achievement blocking to just these. Let's show them that we as players can mod and cheat responsibly.".orange());
                },
                () => UI.Toggle("Object Highlight Toggle Mode", ref settings.highlightObjectsToggle, 0),
                () => UI.Toggle("Highlight Copyable Scrolls", ref settings.toggleHighlightCopyableScrolls, 0),
                () => UI.Toggle("Spiders begone (experimental)", ref settings.toggleSpiderBegone, 0),
                () => UI.Toggle("Make Tutorials Not Appear If Disabled In Settings", ref settings.toggleForceTutorialsToHonorSettings),
                () => UI.Toggle("Refill consumables in belt slots if in inventory", ref settings.togglAutoEquipConsumables),
                () => UI.Toggle("Auto Load Last Save On Launch", ref settings.toggleAutomaticallyLoadLastSave, 0),
                () => UI.Toggle("Allow Shift Click To Use Items In Inventory", ref settings.toggleShiftClickToUseInventorySlot, 0),
                () => UI.Toggle("Allow Shift Click To Transfer Entire Stack", ref settings.toggleShiftClickToFastTransfer, 0),
                () => {
                    using (UI.VerticalScope()) {
                        UI.Div(0, 25, 1280);
                        var useAlt = settings.useAlternateTimeScaleMultiplier;
                        var mainTimeScaleTitle = "Game Time Scale";
                        if (useAlt) mainTimeScaleTitle = mainTimeScaleTitle.grey();
                        var altTimeScaleTitle = "Alternate Time Scale";
                        if (!useAlt) altTimeScaleTitle = altTimeScaleTitle.grey();
                        using (UI.HorizontalScope()) {
                            UI.LogSlider(mainTimeScaleTitle, ref settings.timeScaleMultiplier, 0f, 20, 1, 1, "", UI.Width(450));
                            UI.Space(25);
                            UI.Label("Speeds up or slows down the entire game (movement, animation, everything)".green());
                        }
                        using (UI.HorizontalScope()) {
                            UI.LogSlider(altTimeScaleTitle, ref settings.alternateTimeScaleMultiplier, 0f, 20, 5, 1, "", UI.Width(450));
                        }
                        using (UI.HorizontalScope()) {
                            UI.BindableActionButton(TimeScaleMultToggle);
                            UI.Space(-95);
                            UI.Label("Bindable hot key to swap between main and alternate time scale multipliers".green());
                        }
                        UI.Div(0, 25, 1280);
                    }
                },
                () => UI.Slider("Turn Based Combat Delay", ref settings.turnBasedCombatStartDelay, 0f, 4f, 4f, 1, "", UI.Width((450))),
                () => {
                    using (UI.VerticalScope()) {

                        using (UI.HorizontalScope()) {
                            using (UI.VerticalScope()) {
                                UI.Div(0, 25, 1280);
                                if (UI.Toggle("Enable Brutal Unfair Difficulty", ref settings.toggleBrutalUnfair, 0)) {
                                    EventBus.RaiseEvent<IDifficultyChangedClassHandler>((Action<IDifficultyChangedClassHandler>)(h => {
                                        h.HandleDifficultyChanged();
                                        Main.SetNeedsResetGameUI();
                                    }));
                                }
                                UI.Space(15);
                                UI.Label("This allows you to play with the originally released Unfair difficulty. ".green() + "Note:".orange().bold() + "This Unfair difficulty was bugged and applied the intended difficulty modifers twice. ToyBox allows you to keep playing at this Brutal difficulty level and beyond.  Use the slider below to select your desired Brutality Level".green(), UI.Width(1200));
                                UI.Space(15);
                                using (UI.HorizontalScope()) {
                                    if (UI.Slider("Brutality Level", ref settings.brutalDifficultyMultiplier, 1f, 8f, 2f, 1, "", UI.Width((450)))) {
                                        EventBus.RaiseEvent<IDifficultyChangedClassHandler>((Action<IDifficultyChangedClassHandler>)(h => {
                                            h.HandleDifficultyChanged();
                                            Main.SetNeedsResetGameUI();
                                        }));
                                    }
                                    UI.Space(25);
                                    var brutaltiy = settings.brutalDifficultyMultiplier;
                                    string label;
                                    string suffix = Math.Abs(brutaltiy - Math.Floor(brutaltiy)) <= float.Epsilon ? "" : "+";
                                    switch (brutaltiy) {
                                        case float level when level < 2.0:
                                            label = $"Unfair{suffix}".Rarity(RarityType.Common);
                                            break;
                                        case float level when level < 3.0:
                                            label = $"Brutal{suffix}";
                                            break;
                                        default:
                                            RarityType rarity = (RarityType)(brutaltiy);
                                            label = $"{rarity}{suffix}".Rarity(rarity);
                                            break;
                                    }
                                    using (UI.VerticalScope(UI.AutoWidth())) {
                                        UI.Space(UnityModManager.UI.Scale(3));
                                        UI.Label(label.bold(), UI.largeStyle, UI.AutoWidth());
                                    }
                                }
                                UI.Space(-10);
                            }
                        }
                    }
                },
            () => { }
            );
            UI.Div(0, 25);
            UI.HStack("Alignment", 1,
                () => { UI.Toggle("Fix Alignment Shifts", ref settings.toggleAlignmentFix, 0); UI.Space(119); UI.Label("Makes alignment shifts towards pure good/evil/lawful/chaotic only shift on those axes".green()); },
                () => { UI.Toggle("Prevent Alignment Changes", ref settings.togglePreventAlignmentChanges, 0); UI.Space(25); UI.Label("See Party Editor for more fine grained alignment locking per character".green()); },
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Cheats", 1,
                () => {
                    UI.Toggle("Enable Teleport Keys", ref settings.toggleTeleportKeysEnabled, 0);
                    if (settings.toggleTeleportKeysEnabled) {
                        UI.Space(100);
                        using (UI.VerticalScope()) {
                            UI.KeyBindPicker("TeleportMain", "Main Character", 0, 200);
                            UI.KeyBindPicker("TeleportSelected", "Selected Chars.", 0, 200);
                            UI.KeyBindPicker("TeleportParty", "Whole Party", 0, 200);
                        }
                    }
                },
                () => UI.Toggle("Infinite Abilities", ref settings.toggleInfiniteAbilities, 0),
                () => UI.Toggle("Infinite Spell Casts", ref settings.toggleInfiniteSpellCasts, 0),
                () => UI.Toggle("No Material Components", ref settings.toggleMaterialComponent, 0),
                () => UI.Toggle("Disable Arcane Spell Failure", ref settings.toggleIgnoreSpellFailure, 0),
                () => UI.Toggle("Disable Party Negative Levels", ref settings.togglePartyNegativeLevelImmunity, 0),
                () => UI.Toggle("Disable Party Ability Damage", ref settings.togglePartyAbilityDamageImmunity, 0),

                () => UI.Toggle("Unlimited Actions During Turn", ref settings.toggleUnlimitedActionsPerTurn, 0),
                () => UI.Toggle("Infinite Charges On Items", ref settings.toggleInfiniteItems, 0),

                () => UI.Toggle("Instant Cooldown", ref settings.toggleInstantCooldown, 0),

                () => UI.Toggle("Spontaneous Caster Scroll Copy", ref settings.toggleSpontaneousCopyScrolls, 0),

                () => UI.Toggle("Disable Equipment Restrictions", ref settings.toggleEquipmentRestrictions, 0),
                () => UI.Toggle("Disable Armor Max Dexterity", ref settings.toggleIgnoreMaxDexterity, 0),

                () => UI.Toggle("Disable Dialog Restrictions (Alignment)", ref settings.toggleDialogRestrictions, 0),
                () => UI.Toggle("Disable Dialog Restrictions (Mythic Path)", ref settings.toggleDialogRestrictionsMythic, 0),
#if DEBUG
                () => UI.Toggle("Disable Dialog Restrictions (Everything, Experimental)", ref settings.toggleDialogRestrictionsEverything, 0),
#endif
                () => UI.Toggle("No Friendly Fire On AOEs", ref settings.toggleNoFriendlyFireForAOE, 0),
                () => UI.Toggle("Free Meta-Magic", ref settings.toggleMetamagicIsFree, 0),

                () => UI.Toggle("No Fog Of War", ref settings.toggleNoFogOfWar, 0),
                () => UI.Toggle("Restore Spells & Skills After Combat", ref settings.toggleRestoreSpellsAbilitiesAfterCombat, 0),
                //() => UI.Toggle("Recharge Items After Combat", ref settings.toggleRechargeItemsAfterCombat, 0),
                //() => UI.Toggle("Access Remote Characters", ref settings.toggleAccessRemoteCharacters,0),
                //() => UI.Toggle("Show Pet Portraits", ref settings.toggleShowAllPartyPortraits,0),
                () => UI.Toggle("Instant Rest After Combat", ref settings.toggleInstantRestAfterCombat, 0),
                () => UI.Toggle("Disallow Companions Leaving Party (experimental; only enable while needed)", ref settings.toggleBlockUnrecruit, 0),
                () => UI.Toggle("Disable Romance IsLocked Flag (experimental)", ref settings.toggleMultipleRomance, 0),
                () => UI.Toggle("Instant change party members", ref settings.toggleInstantChangeParty),
                () => UI.ToggleCallback("Equipment No Weight", ref settings.toggleEquipmentNoWeight, BagOfPatches.Tweaks.NoWeight_Patch1.Refresh),
                () => UI.Toggle("Allow Item Use From Inventory During Combat", ref settings.toggleUseItemsDuringCombat),
                () => UI.Toggle("Ignore Alignment Requirements for Abilities", ref settings.toggleIgnoreAbilityAlignmentRestriction),
                () => UI.Toggle("Remove Level 20 Caster Level Cap", ref settings.toggleUncappedCasterLevel),
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
            UI.Div(0, 25);
            UI.HStack("Class Specific", 1,
                () => UI.Slider("Kineticist: Burn Reduction", ref settings.kineticistBurnReduction, 0, 30, 1, "", UI.AutoWidth()),
                () => UI.Slider("Arcanist: Spell Slot Multiplier", ref settings.arcanistSpellslotMultiplier, 0.5f, 10f,
                        1f, 1, "", UI.AutoWidth()),
                () => {
                    UI.Space(25);
                    UI.Label("Please rest after adjusting to recalculate your spell slots.".green());
                },
                () => UI.Toggle("Witch/Shaman: Cackling/Shanting Extends Hexes By 10 Min (Out Of Combat)", ref settings.toggleExtendHexes),
                () => UI.Toggle("Allow Simultaneous Activatable Abilities (Like Judgements)", ref settings.toggleAllowAllActivatable),
                () => UI.Toggle("Kineticist: Allow Gather Power Without Hands", ref settings.toggleKineticistGatherPower),
                () => UI.Toggle("Barbarian: Auto Start Rage When Entering Combat", ref settings.toggleEnterCombatAutoRage),
                () => UI.Toggle("Demon: Auto Start Rage When Entering Combat", ref settings.toggleEnterCombatAutoRageDemon),
                () => UI.Toggle("Magus: Always Allow Spell Combat", ref settings.toggleAlwaysAllowSpellCombat),
                () => { }
                );
            UI.Div(0, 25);
            UI.HStack("Multipliers", 1,
                () => UI.LogSlider("Experience", ref settings.experienceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Money Earned", ref settings.moneyMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Vendor Sell Price", ref settings.vendorSellPriceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Vendor Buy Price", ref settings.vendorBuyPriceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.Slider("Increase Carry Capacity", ref settings.encumberanceMultiplier, 1, 100, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Spells Per Day", ref settings.spellsPerDayMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                () => {
                    UI.LogSlider("Movement Speed", ref settings.partyMovementSpeedMultiplier, 0f, 20, 1, 1, "", UI.Width(600));
                    UI.Space(25);
                    UI.Toggle("Whole Team Moves Same Speed", ref settings.toggleMoveSpeedAsOne, 0);
                    UI.Space(25);
                    UI.Label("Adjusts the movement speed of your party in area maps".green());
                },
                () => {
                    UI.LogSlider("Travel Speed", ref settings.travelSpeedMultiplier, 0f, 20, 1, 1, "", UI.Width(600));
                    UI.Space(25);
                    UI.Label("Adjusts the movement speed of your party on world maps".green());
                },
                () => {
                    UI.LogSlider("Companion Cost", ref settings.companionCostMultiplier, 0, 20, 1, 1, "", UI.Width(600));
                    UI.Space(25);
                    UI.Label("Adjusts costs of hiring mercenaries at the Pathfinder vendor".green());

                },
                () => UI.LogSlider("Enemy HP Multiplier", ref settings.enemyBaseHitPointsMultiplier, 0.1f, 20, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Buff Duration", ref settings.buffDurationMultiplierValue, 0f, 999, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("Field Of View", ref settings.fovMultiplier, 0.4f, 5.0f, 1, 2, "", UI.AutoWidth()),
                () => UI.LogSlider("FoV (Cut Scenes)", ref settings.fovMultiplierCutScenes, 0.4f, 5.0f, 1, 2, "", UI.AutoWidth()),
                () => { }
                );
            Actions.ApplyTimeScale();
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
                () => UI.EnumGrid("Take 10 Out of Combat (Always)", ref settings.take10always, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Take 10 Out of Combat (Minimum)", ref settings.take10minimum, 0, UI.AutoWidth()),
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
        }
    }
}