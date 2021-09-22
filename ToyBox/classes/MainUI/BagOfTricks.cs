// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using System;
using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Kingdom;
using ModKit;
using static ModKit.UI;

namespace ToyBox {
    public static class BagOfTricks {
        public static Settings settings { get { return Main.settings; } }

        // cheats combat
        const string RestAll = "Rest All";
        const string Empowered = "Empowered";
        const string FullBuffPlease = "Full Buff Please";
        const string RemoveBuffs = "Remove Buffs";
        const string RemoveDeathsDoor = "Remove Deaths Door";
        const string KillAllEnemies = "Kill All Enemies";
        const string SummonZoo = "Summon Zoo";

        // cheats common
        const string TeleportPartyToYou = "Teleport Party To You";
        const string GoToGlobalMap = "Go To Global Map";
        const string RerollPerception = "Reroll Perception";
        const string ChangeParty = "Change Party";

        public static void OnLoad() {
            // Combat
            KeyBindings.RegisterAction(RestAll, () => CheatsCombat.RestAll());
            KeyBindings.RegisterAction(Empowered, () => CheatsCombat.Empowered(""));
            KeyBindings.RegisterAction(FullBuffPlease, () => CheatsCombat.FullBuffPlease(""));
            KeyBindings.RegisterAction(RemoveBuffs, () => Actions.RemoveAllBuffs());
            KeyBindings.RegisterAction(RemoveDeathsDoor, () => CheatsCombat.DetachDebuff());
            KeyBindings.RegisterAction(KillAllEnemies, () => CheatsCombat.KillAll());
            KeyBindings.RegisterAction(SummonZoo, () => CheatsCombat.SpawnInspectedEnemiesUnderCursor(""));
            // Common
            KeyBindings.RegisterAction(TeleportPartyToYou, () => Teleport.TeleportPartyToPlayer());
            KeyBindings.RegisterAction(GoToGlobalMap, () => Teleport.TeleportToGlobalMap());
            KeyBindings.RegisterAction(RerollPerception, () => Actions.RunPerceptionTriggers());
            KeyBindings.RegisterAction(ChangeParty, () => { Actions.ChangeParty(); });
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
                () => UI.BindableActionButton(Empowered),
                () => UI.BindableActionButton(FullBuffPlease),
                () => UI.BindableActionButton(RemoveBuffs),
                () => UI.BindableActionButton(RemoveDeathsDoor),
                () => UI.BindableActionButton(KillAllEnemies),
                () => UI.BindableActionButton(SummonZoo),
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
                () => { }
            );
            UI.Div(0, 25);
            UI.HStack("Loot", 1,
                () => {
                    UI.Toggle("Color Items By Rarity", ref settings.toggleColorLootByRarity, 0);
                    UI.Space(25);
                    using (UI.VerticalScope()) {
                        UI.Label($"This makes loot function like Diablo or Borderlands. {"Note: turning this off requires you to save and reload for it to take effect.".orange()}".green());
                        UI.Label("The coloring of rarity goes as follows:".green());
                        UI.HStack("Rarity".orange(), 1,
                            () => UI.Label("Trash".Rarity(RarityType.Trash).bold()),
                            () => UI.Label("Common".bold()),
                            () => UI.Label("Uncommon".Rarity(RarityType.Uncommon).bold()),
                            () => UI.Label("Rare".Rarity(RarityType.Rare).bold()),
                            () => UI.Label("Epic".Rarity(RarityType.Epic).bold()),
                            () => UI.Label("Legendary".Rarity(RarityType.Legendary).bold()),
                            () => UI.Label("Mythic".Rarity(RarityType.Mythic).bold()),
                            () => UI.Label("Godly".Rarity(RarityType.Godly)),
                            () => UI.Label("Notable".Rarity(RarityType.Notable).bold()),
                            () => { }
                        );
                    }

                    // The following options let you configure loot filtering and auto sell levels:".green());
                },
#if false
                () => UI.EnumGrid("Hide Level ", ref settings.lootFilterIgnore, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Auto Sell Level ", ref settings.lootFilterAutoSell, 0, UI.AutoWidth()),
#endif
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

                () => UI.Toggle("Disable Dialog Restrictions", ref settings.toggleDialogRestrictions, 0),

                () => UI.Toggle("No Friendly Fire On AOEs", ref settings.toggleNoFriendlyFireForAOE, 0),
                () => UI.Toggle("Free Meta-Magic", ref settings.toggleMetamagicIsFree, 0),

                () => UI.Toggle("No Fog Of War", ref settings.toggleNoFogOfWar, 0),
                //() => UI.Toggle("Restore Spells & Skills After Combat", ref settings.toggleRestoreSpellsAbilitiesAfterCombat,0),
                //() => UI.Toggle("Access Remote Characters", ref settings.toggleAccessRemoteCharacters,0),
                //() => UI.Toggle("Show Pet Portraits", ref settings.toggleShowAllPartyPortraits,0),
                () => UI.Toggle("Instant Rest After Combat", ref settings.toggleInstantRestAfterCombat, 0),
                () => UI.Toggle("Disallow Companions Leaving Party (experimental; only enable while needed)", ref settings.toggleBlockUnrecruit, 0),
                () => UI.Toggle("Disable Romance IsLocked Flag (experimental)", ref settings.toggleMultipleRomance, 0),
                () => UI.Toggle("Instant change party members", ref settings.toggleInstantChangeParty),
                () => UI.Toggle("Mass Loot Shows Everything When Leaving Map (some items might be invisible until looted)", ref settings.toggleMassLootEverything),
                () => UI.ToggleCallback("Equipment No Weight", ref settings.toggleEquipmentNoWeight, BagOfPatches.Tweaks.NoWeight_Patch1.Refresh),
                () => UI.Toggle("Allow Item Use From Inventory During Combat", ref settings.toggleUseItemsDuringCombat),
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
                () => UI.Slider("Kineticist: Burn Reduction", ref settings.kineticistBurnReduction, 0, 10, 1, "", UI.AutoWidth()),
                () => UI.Slider("Arcanist: Spell Slot Multiplier", ref settings.arcanistSpellslotMultiplier, 0.5f, 10f, 1f, 1, "", UI.AutoWidth()),
                () => UI.Toggle("Witch/Shaman: Cackling/Shanting Extends Hexes By 10 Min (Out Of Combat)", ref settings.toggleExtendHexes),
                () => UI.Toggle("Allow Simultaneous Activatable Abilities (Like Judgements)", ref settings.toggleAllowAllActivatable),
                () => UI.Toggle("Kineticist: Allow Gather Power Without Hands", ref settings.toggleKineticistGatherPower),
                () => UI.Toggle("Barbarian: Auto Start Rage When Entering Combat", ref settings.toggleEnterCombatAutoRage),
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
                    UI.LogSlider("Game Time Scale", ref settings.timeScaleMultiplier, 0f, 20, 1, 2, "", UI.Width(600));
                    UI.Space(25);
                    UI.Label("Speeds up or slows down the entire game (movement, animation, everything)".green());
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
        }
    }
}