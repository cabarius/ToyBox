// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Kingdom;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.View;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using UnityEngine;
using UnityModManagerNet;
using static ModKit.UI;

namespace ToyBox {
    public static class BagOfTricks {
        public static Settings settings => Main.Settings;

        // cheats combat
        private const string RestAll = "Rest All";
        private const string RestSelected = "Rest Selected";
        private const string Empowered = "Empowered";
        private const string FullBuffPlease = "Common Buffs";
        private const string GoddesBuffs = "Buff Like A Goddess";
        private const string RemoveBuffs = "Remove Buffs";
        private const string RemoveDeathsDoor = "Remove Deaths Door";
        private const string KillAllEnemies = "Kill All Enemies";
        //private const string SummonZoo = "Summon Zoo"
        private const string LobotomizeAllEnemies = "Lobotomize Enemies";
        private const string ToggleMurderHobo = "Toggle Murder Hobo";

        // cheats common
        private const string TeleportPartyToYou = "Teleport Party To You";
        private const string GoToGlobalMap = "Go To Global Map";
        private const string RerollPerception = "Reroll Perception";
        private const string RerollInteractionSkillChecks = "Reset Interactables";
        private const string ChangeParty = "Change Party";
        private const string ChangWeather = "Change Weather";

        // other
        private const string TimeScaleMultToggle = "Main/Alt Timescale";
        private const string PreviewDialogResults = "Preview Results";
        private const string ResetAdditionalCameraAngles = "Fix Camera";

        //For buffs exceptions
        private static bool showBuffDurationExceptions = false;

        public static void OnLoad() {
            // Combat
            KeyBindings.RegisterAction(RestAll, () => CheatsCombat.RestAll());
            KeyBindings.RegisterAction(RestSelected, () => Actions.RestSelected());
            KeyBindings.RegisterAction(Empowered, () => CheatsCombat.Empowered(""));
            KeyBindings.RegisterAction(FullBuffPlease, () => CheatsCombat.FullBuffPlease(""));
            KeyBindings.RegisterAction(GoddesBuffs, () => CheatsCombat.Iddqd(""));
            KeyBindings.RegisterAction(RemoveBuffs, () => Actions.RemoveAllBuffs());
            KeyBindings.RegisterAction(RemoveDeathsDoor, () => CheatsCombat.DetachDebuff());
            KeyBindings.RegisterAction(KillAllEnemies, () => Actions.KillAll());
            //KeyBindings.RegisterAction(SummonZoo, () => CheatsCombat.SpawnInspectedEnemiesUnderCursor(""));
            KeyBindings.RegisterAction(LobotomizeAllEnemies, () => Actions.LobotomizeAllEnemies());
            // Common
            KeyBindings.RegisterAction(TeleportPartyToYou, () => Teleport.TeleportPartyToPlayer());
            KeyBindings.RegisterAction(GoToGlobalMap, () => Teleport.TeleportToGlobalMap());
            KeyBindings.RegisterAction(RerollPerception, () => Actions.RunPerceptionTriggers());
            KeyBindings.RegisterAction(RerollInteractionSkillChecks, () => Actions.RerollInteractionSkillChecks());
            KeyBindings.RegisterAction(ChangeParty, () => { Actions.ChangeParty(); });
            KeyBindings.RegisterAction(ChangWeather, () => CheatsCommon.ChangeWeather(""));
            // Other
            KeyBindings.RegisterAction(TimeScaleMultToggle,
                                       () => {
                                           settings.useAlternateTimeScaleMultiplier = !settings.useAlternateTimeScaleMultiplier;
                                           Actions.ApplyTimeScale();
                                       },
                                       title => ToggleTranscriptForState(title, settings.useAlternateTimeScaleMultiplier)
                );
            KeyBindings.RegisterAction(PreviewDialogResults, () => {
                settings.previewDialogResults = !settings.previewDialogResults;
                var dialogController = Game.Instance.DialogController;
            });
            KeyBindings.RegisterAction(ResetAdditionalCameraAngles, () => {
                Main.resetExtraCameraAngles = true;
            });
            KeyBindings.RegisterAction(ToggleMurderHobo,
                                       () => settings.togglekillOnEngage = !settings.togglekillOnEngage,
                                       title => ToggleTranscriptForState(title, settings.togglekillOnEngage)
                                       );
        }
        public static void ResetGUI() { }
        public static void OnGUI() {
#if BUILD_CRUI
            ActionButton("Demo crUI", () => ModKit.crUI.Demo());
#endif
            if (Main.IsInGame) {
                BeginHorizontal();
                Space(25);
                Label("increment".localize().cyan(), AutoWidth());
                var increment = IntTextField(ref settings.increment, null, Width(150));
                EndHorizontal();
                var mainChar = Game.Instance.Player.MainCharacter.Value;
                var kingdom = KingdomState.Instance;
                HStack("Resources".localize(), 1,
                    () => {
                        var money = Game.Instance.Player.Money;
                        Label("Gold".localize().cyan(), Width(150));
                        Label(money.ToString().orange().bold(), Width(200));
                        ActionButton("Gain ".localize() + $"{increment}", () => Game.Instance.Player.GainMoney(increment), AutoWidth());
                        ActionButton("Lose ".localize() + $"{increment}", () => {
                            var loss = Math.Min(money, increment);
                            Game.Instance.Player.GainMoney(-loss);
                        }, AutoWidth());
                    },
                    () => {
                        var exp = mainChar.Progression.Experience;
                        Label("Experience".localize().cyan(), Width(150));
                        Label(exp.ToString().orange().bold(), Width(200));
                        ActionButton("Gain ".localize() + $"{increment}", () => {
                            Game.Instance.Player.GainPartyExperience(increment);
                        }, AutoWidth());
                    },
                    () => {
                        var corruption = Game.Instance.Player.Corruption;
                        Label("Corruption".localize().cyan(), Width(150));
                        Label(corruption.CurrentValue.ToString().orange().bold(), Width(200));
                        ActionButton($"Clear".localize(), () => corruption.Clear(), AutoWidth());
                        25.space();
                        Toggle("Disable Corruption".localize(), ref settings.toggleDisableCorruption);
                    },
                    () => { }
                );
            }
            Div(0, 25);
            HStack("Combat".localize(), 2,
                () => BindableActionButton(RestAll, true),
                () => BindableActionButton(RestSelected, true),
                () => BindableActionButton(FullBuffPlease, true),
                () => BindableActionButton(Empowered, true),
                () => BindableActionButton(GoddesBuffs, true),
                () => BindableActionButton(RemoveBuffs, true),
                () => BindableActionButton(RemoveDeathsDoor, true),
                () => BindableActionButton(KillAllEnemies, true),
                //() => UI.BindableActionButton(SummonZoo),
                () => BindableActionButton(LobotomizeAllEnemies, true),
                () => { },
                () => {
                    using (VerticalScope()) {
                        using (HorizontalScope()) {
                            using (VerticalScope(220.width())) {
                                using (HorizontalScope()) {
                                    Toggle(("Be a " + "Murder".red().bold() + " Hobo".orange()).localize(), ref settings.togglekillOnEngage, 222.width());
                                    KeyBindPicker(ToggleMurderHobo, "", 50);
                                }
                            }
                            158.space();
                            Label(("If ticked, this will " + "MURDER".red().bold() + " all who dare to engage you!".green()).localize(), AutoWidth());
                        }
                        using (HorizontalScope()) {
                            if (Toggle("Log ToyBox Keyboard Commands In Game".localize(), ref Mod.ModKitSettings.toggleKeyBindingsOutputToTranscript, 450.width()))
                                ModKitSettings.Save();
                            50.space();
                            HelpLabel("When ticked this shows ToyBox commands in the combat log which is helpful for you to know when you used the shortcut".localize());
                        }
                    }
                }
                );
            Div(0, 25);
            HStack("Teleport".localize(), 2,
                () => BindableActionButton(TeleportPartyToYou, true),
                () => {
                    Toggle("Enable Teleport Keys".localize(), ref settings.toggleTeleportKeysEnabled);
                    Space(100);
                    if (settings.toggleTeleportKeysEnabled) {
                        using (VerticalScope()) {
                            KeyBindPicker("TeleportMain", "Main Character".localize(), 0, 200);
                            KeyBindPicker("TeleportSelected", "Selected Chars".localize(), 0, 200);
                            KeyBindPicker("TeleportParty", "Whole Party".localize(), 0, 200);
                        }
                    }
                    Space(25);
                    Label("You can enable hot keys to teleport members of your party to your mouse cursor on Area or the Global Map".localize().green());
                });
            Div(0, 25);
            HStack("Common".localize(), 2,
                () => BindableActionButton(GoToGlobalMap, true),
                () => {
                    BindableActionButton(ChangeParty, true);
                    Space(-75);
                    HelpLabel("Change the party without advancing time (good to bind)".localize());
                },
                () => BindableActionButton(RerollPerception, true),
                () => {
                    BindableActionButton(RerollInteractionSkillChecks, true);
                    Space(-75);
                    Label("This resets all the skill check rolls for all interactable objects in the area".localize().green());
                },
            () => {
                NonBindableActionButton("Set Perception to 40".localize(), () => {
                    CheatsCommon.StatPerception();
                    Actions.RunPerceptionTriggers();
                });
            },
            () => BindableActionButton(ChangWeather, true),
            () => NonBindableActionButton("Give All Items".localize(), () => CheatsUnlock.CreateAllItems("")),
            () => NonBindableActionButton("Identify All".localize(), () => Actions.IdentifyAll()),
            () => { }
            );
            Div(0, 25);
            HStack("Preview".localize(), 0, () => {
                Toggle("Dialog Results".localize(), ref settings.previewDialogResults);
                Space(25);
                Toggle("Dialog Alignment".localize(), ref settings.previewAlignmentRestrictedDialog);
                Space(25);
                Toggle("Random Encounters".localize(), ref settings.previewRandomEncounters);
                Space(25);
                Toggle("Events".localize(), ref settings.previewEventResults);
                Space(25);
                Toggle("Decrees".localize(), ref settings.previewDecreeResults);
                Space(25);
                Toggle("Relic Info".localize(), ref settings.previewRelicResults);
                Space(25);
                BindableActionButton(PreviewDialogResults, true);
            });
            Div(0, 25);
            HStack("Dialog".localize(), 1,
                () => {
                    Toggle(("♥♥ ".red() + "Love is Free".bold() + " ♥♥".red()).localize(), ref settings.toggleAllowAnyGenderRomance, 300.width());
                    25.space();
                    Label(("Allow ".green() + "any gender".color(RGBA.purple) + " " + "for any ".green() + "R".color(RGBA.red) + "o".orange() + "m".yellow() + "a".green() + "n".cyan() + "c".color(RGBA.rare) + "e".color(RGBA.purple)).localize());
                },
                () => {
                    Toggle("Jealousy Begone!".localize().bold(), ref settings.toggleMultipleRomance, 300.width());
                    25.space();
                    Label(("Allow ".green() + "multiple".color(RGBA.purple) + " romances at the same time".green()).localize());
                },
                () => {
                    Toggle("Friendship is Magic".localize().bold(), ref settings.toggleFriendshipIsMagic, 300.width());
                    25.space();
                    Label("Experimental ".localize().orange() + " your friends forgive even your most vile choices.".localize().green());
                },
                () => {
                    Toggle("Disallow Companions Leaving Party".localize(), ref settings.toggleBlockUnrecruit, 300.width());
                    200.space();
                    Label("Warning: ".localize().color(RGBA.red) + " Only use when Friendship is Magic doesn't work, and then turn off immediately after. Can  otherwise break your save".localize().orange());
                },
                () => {
                    Toggle("Previously Chosen Dialog Is Smaller ".localize(), ref settings.toggleMakePreviousAnswersMoreClear, 300.width());
                    200.space();
                    Label("Draws dialog choices that you have previously selected in smaller type".localize().green());
                },
                () => {
                    Toggle("Expand Dialog To Include Remote Companions".localize().bold(), ref settings.toggleRemoteCompanionDialog, 300.width());
                    200.space();
                    Label("Experimental".localize().orange() + " Allow remote companions to make comments on dialog you are having.".localize().green());
                },
                () => {
                    if (settings.toggleRemoteCompanionDialog) {
                        50.space();
                        Toggle("Include Former Companions".localize(), ref settings.toggleExCompanionDialog, 300.width());
                        150.space();
                        Label("This also includes companions who left the party such as Wenduag if you picked Lann".localize().green());
                    }
                },
                () => {
                    using (VerticalScope(300.width())) {
                        Toggle("Expand Answers For Conditional Responses".localize(), ref settings.toggleShowAnswersForEachConditionalResponse, 300.width());
                        if (settings.toggleShowAnswersForEachConditionalResponse) {
                            using (HorizontalScope()) {
                                50.space();
                                Toggle("Show Unavailable Responses".localize(), ref settings.toggleShowAllAnswersForEachConditionalResponse, 250.width());
                            }
                        }
                    }
                    200.space();
                    Label("Some responses such as comments about your mythic powers will always choose the first one by default. This will show a copy of the answer and the condition for each possible response that an NPC might make to you based on".localize().green());
                },
#if DEBUG
                () => {
                    Toggle("Randomize NPC Responses To Dialog Choices", ref settings.toggleRandomizeCueSelections, 300.width());
                    200.space();
                    Label("Some responses such as comments about your mythic powers will always choose the first one by default. This allows the game to mix things up a bit".green() + "\nWarning:".yellow().bold() + " this will introduce randomness to NPC responses to you in general and may lead to surprising or even wild outcomes".orange());
                },
#endif
                () => Toggle("Disable Dialog Restrictions (Alignment)".localize(), ref settings.toggleDialogRestrictions),
                () => Toggle("Disable Dialog Restrictions (Mythic Path)".localize(), ref settings.toggleDialogRestrictionsMythic),
                () => Toggle("Ignore Event Solution Restrictions".localize(), ref settings.toggleIgnoreEventSolutionRestrictions),
#if DEBUG
                () => Toggle("Disable Dialog Restrictions (Everything, Experimental)", ref settings.toggleDialogRestrictionsEverything),
#endif
                () => { }
            );
            Div(0, 25);
            HStack("Quality of Life".localize(), 1,
                () => {
                    Toggle("Allow Achievements While Using Mods".localize(), ref settings.toggleAllowAchievementsDuringModdedGame, 500.width());
                    Label("This is intended for you to be able to enjoy the game while using mods that enhance your quality of life.  Please be mindful of the player community and avoid using this mod to trivialize earning prestige achievements like Sadistic Gamer. The author is in discussion with Owlcat about reducing the scope of achievement blocking to just these. Let's show them that we as players can mod and cheat responsibly.".localize().orange());
                },
                // () => { if (Toggle("Expanded Party View", ref settings.toggleExpandedPartyView)) PartyVM_Patches.Repatch(),
                () => {
                    Toggle("Enhanced Map View".localize(), ref settings.toggleZoomableLocalMaps, 500.width());
                    HelpLabel("Makes mouse zoom works for the local map (cities, dungeons, etc). Game restart required if you turn it off".localize());
                },
                () => {
                    Toggle("Click On Equip Slots To Filter Inventory".localize(), ref settings.togglEquipSlotInventoryFiltering, 500.width());
                    HelpLabel($"If you tick this you can click on equipment slots to filter the inventory for items that fit in it.\nFor more {"Enhanced Inventory".orange()} and {"Spellbook".orange()} check out the {"Loot & Spellbook Tab".orange().bold()}".localize());
                },
                () => {
                    Toggle("Enhanced Load/Save".localize(), ref settings.toggleEnhancedLoadSave, 500.width());
                    HelpLabel("Adds a search field to Load/Save screen (in game only)".localize());
                },
                () => Toggle("Object Highlight Toggle Mode".localize(), ref settings.highlightObjectsToggle),
                () => {
                    Toggle("Mark Interesting NPCs".localize(), ref settings.toggleShowInterestingNPCsOnLocalMap, 500.width());
                    HelpLabel("This will change the color of NPC names on the highlike makers and change the color map markers to indicate that they have interesting or conditional interactions".localize());
                },
                () => Toggle("Make game continue to play music on lost focus".localize(), ref settings.toggleContinueAudioOnLostFocus),
                () => Toggle("Highlight Copyable Scrolls".localize(), ref settings.toggleHighlightCopyableScrolls),
                () => {
                    Toggle("Auto load Last Save on launch".localize(), ref settings.toggleAutomaticallyLoadLastSave, 500.width());
                    HelpLabel("Hold down shift during launch to bypass".localize());
                },
                () => Toggle(("Game Over Fix For " + "LEEEROOOOOOOYYY JEEEENKINS!!!".color(RGBA.maroon) + " omg he just ran in!").localize(), ref settings.toggleGameOverFixLeeerrroooooyJenkins),
                () => {
                    503.space();
                    HelpLabel("Prevents dumb companions (that's you Greybor) from wiping the party by running running into the dragon room and dying...".localize());
                },
                () => Toggle("Make Spell/Ability/Item Pop-Ups Wider ".localize(), ref settings.toggleWidenActionBarGroups),
                () => {
                    if (Toggle("Show Acronyms in Spell/Ability/Item Pop-Ups".localize(), ref settings.toggleShowAcronymsInSpellAndActionSlots)) {
                        Main.SetNeedsResetGameUI();
                    }
                },
                () => {
                    Toggle("Icky Stuff Begone!!!".localize(), ref settings.toggleReplaceModelMenu, (settings.toggleReplaceModelMenu ? 248 : 499).width());
                    if (settings.toggleReplaceModelMenu) {
                        using (VerticalScope(Width(247))) {
                            Toggle("Spiders Begone!".localize(), ref settings.toggleSpiderBegone);
                            Toggle("Vescavors Begone!".localize(), ref settings.toggleVescavorsBegone);
                            Toggle("Retrievers Begone!".localize(), ref settings.toggleRetrieversBegone);
                            Toggle("Deraknis Begone!".localize(), ref settings.toggleDeraknisBegone);
                            Toggle("Deskari Begone!".localize(), ref settings.toggleDeskariBegone);
                        }
                    }
                    Label("Some players find spiders and other swarms icky. This replaces them with something more pleasant".localize().green());
                },
                () => Toggle("Make tutorials not appear if disabled in settings".localize(), ref settings.toggleForceTutorialsToHonorSettings),
                () => Toggle("Refill consumables in belt slots if in inventory".localize(), ref settings.togglAutoEquipConsumables),
                () => {
                    var modifier = KeyBindings.GetBinding("InventoryUseModifier");
                    var modifierText = modifier.Key == KeyCode.None ? "Modifer" : modifier.ToString();
                    Toggle("Allow ".localize() + $"{modifierText}".cyan() + (" + Click".cyan() + " To Use Items In Inventory").localize(), ref settings.toggleShiftClickToUseInventorySlot, 470.width());
                    if (settings.toggleShiftClickToUseInventorySlot) {
                        ModifierPicker("InventoryUseModifier", "", 0);
                    }
                },
                () => {
                    var modifier = KeyBindings.GetBinding("ClickToTransferModifier");
                    var modifierText = modifier.Key == KeyCode.None ? "Modifer" : modifier.ToString();
                    Toggle("Allow ".localize() + $"{modifierText}".cyan() + (" + Click".cyan() + " To Transfer Entire Stack").localize(), ref settings.toggleShiftClickToFastTransfer, 470.width());
                    if (settings.toggleShiftClickToFastTransfer) {
                        ModifierPicker("ClickToTransferModifier", "", 0);
                    }
                },
                () => Toggle("Respec Refund Scrolls".localize(), ref settings.toggleRespecRefundScrolls),
                () => {
                    Toggle("Make Puzzle Symbols More Clear".localize(), ref settings.togglePuzzleRelief);
                    25.space();
                    HelpLabel(("ToyBox Archeologists can tag confusing puzzle pieces with green numbers in the game world and for inventory tool tips it will show text like this: " + "[PuzzlePiece Green3x1]".yellow().bold() + "\nNOTE: ".orange().bold() + "Needs game restart to take efect".orange()).localize());
                },
                () => {
                    ActionButton("Clear Action Bar".localize(), () => Actions.ClearActionBar());
                    50.space();
                    Label("Make sure you have auto-fill turned off in settings or else this will just reset to default".localize().green());
                },
                () => ActionButton("Fix Incorrect Main Character".localize(), () => {
                    var probablyPlayer = Game.Instance.Player?.Party?
                        .Where(x => !x.IsCustomCompanion())
                        .Where(x => !x.IsStoryCompanion()).ToList();
                    if (probablyPlayer is { Count: 1 }) {
                        var newMainCharacter = probablyPlayer.First();
                        var text = "Promoting % to main character!".localize().Split('%');
                        Mod.Warn($"{text[0]}{newMainCharacter.CharacterName}{text[1]}");
                        if (Game.Instance != null) Game.Instance.Player.MainCharacter = newMainCharacter;
                    }
                }, AutoWidth()),
                () => { Toggle("Enable Loading with Blueprint Errors".localize().color(RGBA.maroon), ref settings.enableLoadWithMissingBlueprints); 25.space(); Label($"This {"incredibly dangerous".bold()} setting overrides the default behavior of failing to load saves depending on missing blueprint mods. This desperate action can potentially enable you to recover your saved game, though you'll have to respec at minimum.".localize().orange()); },
                () => {
                    if (settings.enableLoadWithMissingBlueprints) {
                        Label("To permanently remove these modded blueprint dependencies, load the damaged saved game, change areas, and then save the game. You can then respec any characters that were impacted.".localize().orange());
                    }
                },
                () => {
                    using (VerticalScope()) {
                        Div(0, 25, 1280);
                        var useAlt = settings.useAlternateTimeScaleMultiplier;
                        var mainTimeScaleTitle = "Game Time Scale".localize();
                        if (useAlt) mainTimeScaleTitle = mainTimeScaleTitle.grey();
                        var altTimeScaleTitle = "Alternate Time Scale".localize();
                        if (!useAlt) altTimeScaleTitle = altTimeScaleTitle.grey();
                        using (HorizontalScope()) {
                            LogSlider(mainTimeScaleTitle, ref settings.timeScaleMultiplier, 0f, 20, 1, 1, "", Width(450));
                            Space(25);
                            Label("Speeds up or slows down the entire game (movement, animation, everything)".localize().green());
                        }
                        using (HorizontalScope()) {
                            LogSlider(altTimeScaleTitle, ref settings.alternateTimeScaleMultiplier, 0f, 20, 5, 1, "", Width(450));
                        }
                        using (HorizontalScope()) {
                            BindableActionButton(TimeScaleMultToggle, true);
                            Space(-95);
                            Label("Bindable hot key to swap between main and alternate time scale multipliers".localize().green());
                        }
                        Div(0, 25, 1280);
                    }
                },
                () => Slider("Turn Based Combat Delay".localize(), ref settings.turnBasedCombatStartDelay, 0f, 4f, 4f, 1, "", Width(450)),
                () => {
                    using (VerticalScope()) {

                        using (HorizontalScope()) {
                            using (VerticalScope()) {
                                Div(0, 25, 1280);
                                if (Toggle("Enable Brutal Unfair Difficulty".localize(), ref settings.toggleBrutalUnfair)) {
                                    EventBus.RaiseEvent<IDifficultyChangedClassHandler>((Action<IDifficultyChangedClassHandler>)(h => {
                                        h.HandleDifficultyChanged();
                                        Main.SetNeedsResetGameUI();
                                    }));
                                }
                                Space(15);
                                Label("This allows you to play with the originally released Unfair difficulty. ".localize().green() + ("Note:".orange().bold() + "This Unfair difficulty was bugged and applied the intended difficulty modifers twice. ToyBox allows you to keep playing at this Brutal difficulty level and beyond.  Use the slider below to select your desired Brutality Level".green()).localize(), Width(1200));
                                Space(15);
                                using (HorizontalScope()) {
                                    if (Slider("Brutality Level".localize(), ref settings.brutalDifficultyMultiplier, 1f, 8f, 2f, 1, "", Width(450))) {
                                        EventBus.RaiseEvent<IDifficultyChangedClassHandler>((Action<IDifficultyChangedClassHandler>)(h => {
                                            h.HandleDifficultyChanged();
                                            Main.SetNeedsResetGameUI();
                                        }));
                                    }
                                    Space(25);
                                    var brutaltiy = settings.brutalDifficultyMultiplier;
                                    string label;
                                    var suffix = Math.Abs(brutaltiy - Math.Floor(brutaltiy)) <= float.Epsilon ? "" : "+";
                                    switch (brutaltiy) {
                                        case float level when level < 2.0:
                                            label = $"Unfair{suffix}".Rarity(RarityType.Common);
                                            break;
                                        case float level when level < 3.0:
                                            label = $"Brutal{suffix}";
                                            break;
                                        default:
                                            var rarity = (RarityType)brutaltiy;
                                            label = $"{rarity}{suffix}".Rarity(rarity);
                                            break;
                                    }
                                    using (VerticalScope(AutoWidth())) {
                                        Space(UnityModManager.UI.Scale(3));
                                        Label(label.localize().bold(), largeStyle, AutoWidth());
                                    }
                                }
                                Space(-10);
                            }
                        }
                    }
                },
            () => { }
            );
            Div(0, 25);
            HStack("Camera".localize(),
                   1,
                   () => Toggle("Enable Zoom on all maps and cutscenes".localize(), ref settings.toggleZoomOnAllMaps),
                   () => {
                       Toggle("Enable Rotate on all maps and cutscenes".localize(), ref settings.toggleRotateOnAllMaps);
                       153.space();
                       Label(("Note:".orange() + " For cutscenes and some situations the rotation keys are disabled so you have to hold down Mouse3 to drag in order to get rotation".green()).localize());
                   },
                   () => {
                       Toggle("Alt + Mouse Wheel To Adjust Clip Plane".localize(), ref settings.toggleUseAltMouseWheelToAdjustClipPlane);
                   },
                   () => {
                       Toggle("Ctrl + Mouse3 Drag To Adjust Camera Elevation".localize(), ref settings.toggleCameraElevation);
                       25.space();
                       Toggle("Free Camera".localize(), ref settings.toggleFreeCamera);
                   },
                   () => Label("Rotation".localize().cyan()),
                   () => {
                       50.space();
                       if (Toggle("Allow Mouse3 Drag to adjust Camera Tilt".localize(), ref settings.toggleCameraPitch)) {
                           Main.resetExtraCameraAngles = true;
                       }
                       100.space();
                       Label(("Experimental".orange() + " This allows you to adjust pitch (Camera Tilt) by holding down Mouse3 (which previously just rotated).".green() + " Note:".orange() + " Holding alt while Mouse3 dragging lets you move the camera location.".green()).localize());
                   },
                   () => {
                       50.space();
                       Label("Mouse:".localize().cyan(), 125.width());
                       25.space();
                       Toggle("Invert X Axis".localize(), ref settings.toggleInvertXAxis);
                       if (settings.toggleCameraPitch) {
                           25.space();
                           Toggle("Invert Y Axis".localize(), ref settings.toggleInvertYAxis);
                       }
                   },
                   () => {
                       50.space();
                       Label("Keyboard:".localize().cyan(), 125.width());
                       25.space();
                       Toggle("Invert X Axis".localize(), ref settings.toggleInvertKeyboardXAxis);
                   },
                   () => {
                       50.space();
                       BindableActionButton(ResetAdditionalCameraAngles, true);
                   },
                   () => LogSlider("Field Of View".localize(), ref settings.fovMultiplier, 0.4f, 5.0f, 1, 2, "", AutoWidth()),
                   () => LogSlider("FoV (Cut Scenes)".localize(), ref settings.fovMultiplierCutScenes, 0.4f, 5.0f, 1, 2, "", AutoWidth()),
                   () => { }
                );
            Div(0, 25);
            HStack("Alignment".localize(), 1,
                () => { Toggle("Fix Alignment Shifts".localize(), ref settings.toggleAlignmentFix); Space(119); Label("Makes alignment shifts towards pure good/evil/lawful/chaotic only shift on those axes".localize().green()); },
                () => { Toggle("Prevent Alignment Changes".localize(), ref settings.togglePreventAlignmentChanges); Space(25); Label("See Party Editor for more fine grained alignment locking per character".localize().green()); },
                () => { }
                );
            Div(0, 25);
            HStack("Cheats".localize(), 1,
                () => Toggle("Unlimited Stacking of Modifiers (Stat/AC/Hit/Damage/Etc)".localize(), ref settings.toggleUnlimitedStatModifierStacking),
                () => {
                    using (HorizontalScope()) {
                        ToggleCallback("Highlight Hidden Objects".localize(), ref settings.highlightHiddenObjects, Actions.UpdateHighlights);
                        if (settings.highlightHiddenObjects) {
                            Space(100);
                            ToggleCallback("In Fog Of War ".localize(), ref settings.highlightHiddenObjectsInFog, Actions.UpdateHighlights);
                        }
                    }
                },
                () => Toggle("Infinite Abilities".localize(), ref settings.toggleInfiniteAbilities),
                () => Toggle("Infinite Spell Casts".localize(), ref settings.toggleInfiniteSpellCasts),
                () => Toggle("No Material Components".localize(), ref settings.toggleMaterialComponent),
                () => Toggle("Disable Party Negative Levels".localize(), ref settings.togglePartyNegativeLevelImmunity),
                () => Toggle("Disable Party Ability Damage".localize(), ref settings.togglePartyAbilityDamageImmunity),
                () => Toggle("Disable Attacks of Opportunity".localize(), ref settings.toggleAttacksofOpportunity),
                () => Toggle("Unlimited Actions During Turn".localize(), ref settings.toggleUnlimitedActionsPerTurn),
                () => Toggle("Infinite Charges On Items".localize(), ref settings.toggleInfiniteItems),

                () => Toggle("Instant Cooldown".localize(), ref settings.toggleInstantCooldown),

                () => Toggle("Spontaneous Caster Scroll Copy".localize(), ref settings.toggleSpontaneousCopyScrolls),

                () => Toggle("Disable Equipment Restrictions".localize(), ref settings.toggleEquipmentRestrictions),
                () => Toggle("Disable Armor Max Dexterity".localize(), ref settings.toggleIgnoreMaxDexterity),
                () => Toggle("Disable Armor Speed Reduction".localize(), ref settings.toggleIgnoreSpeedReduction),
                () => Toggle("Disable Armor & Shield Arcane Spell Failure".localize(), ref settings.toggleIgnoreSpellFailure),
                () => Toggle("Disable Armor & Shield Checks Penalty".localize(), ref settings.toggleIgnoreArmorChecksPenalty),

                () => Toggle("No Friendly Fire On AOEs".localize(), ref settings.toggleNoFriendlyFireForAOE),
                () => Toggle("Free Meta-Magic".localize(), ref settings.toggleMetamagicIsFree),

                () => Toggle("No Fog Of War".localize(), ref settings.toggleNoFogOfWar),
                () => Toggle("Restore Spells & Skills After Combat".localize(), ref settings.toggleRestoreSpellsAbilitiesAfterCombat),
                //() => UI.Toggle("Recharge Items After Combat", ref settings.toggleRechargeItemsAfterCombat),
                //() => UI.Toggle("Access Remote Characters", ref settings.toggleAccessRemoteCharacters,0),
                //() => UI.Toggle("Show Pet Portraits", ref settings.toggleShowAllPartyPortraits,0),
                () => Toggle("Instant Rest After Combat".localize(), ref settings.toggleInstantRestAfterCombat),
                () => Toggle("Instant change party members".localize(), ref settings.toggleInstantChangeParty),
                () => ToggleCallback("Equipment No Weight".localize(), ref settings.toggleEquipmentNoWeight, BagOfPatches.Tweaks.NoWeight_Patch1.Refresh),
                () => Toggle("Allow Item Use From Inventory During Combat".localize(), ref settings.toggleUseItemsDuringCombat),
                () => Toggle("Ignore Alignment Requirements for Abilities".localize(), ref settings.toggleIgnoreAbilityAlignmentRestriction),
                () => Toggle("Ignore all Requirements for Abilities".localize(), ref settings.toggleIgnoreAbilityAnyRestriction),
                () => Toggle("Ignore Pet Sizes For Mounting".localize(), ref settings.toggleMakePetsRidable),
                () => Toggle("Ride Any Unit As Your Mount".localize(), ref settings.toggleRideAnything),
                () => { }
                );
            Div(153, 25);
            HStack("", 1,
                () => EnumGrid("Disable Attacks Of Opportunity".localize(), ref settings.noAttacksOfOpportunitySelection, true, AutoWidth()),
                    () => EnumGrid("Can Move Through".localize(), ref settings.allowMovementThroughSelection, true, AutoWidth()),
                    () => {
                        Space(328); Label("This allows characters you control to move through the selected category of units during combat".localize().green(), AutoWidth());
                    }
#if false
                () => { UI.Slider("Collision Radius Multiplier", ref settings.collisionRadiusMultiplier, 0f, 2f, 1f, 1, "", UI.AutoWidth()); },
#endif
                );
            Div(0, 25);
            HStack("Class Specific".localize(), 1,
                () => Slider("Kineticist: Burn Reduction".localize(), ref settings.kineticistBurnReduction, 0, 30, 0, "", AutoWidth()),
                        () => Slider("Arcanist: Spell Slot Multiplier".localize(), ref settings.arcanistSpellslotMultiplier, 0.5f, 10f,
                                1f, 1, "", AutoWidth()),
                        () => {
                            Space(25);
                            Label("Please rest after adjusting to recalculate your spell slots.".localize().green());
                        },
                        () => Toggle("Witch/Shaman: Cackling/Shanting Extends Hexes By 10 Min (Out Of Combat)".localize(), ref settings.toggleExtendHexes),
                        () => Toggle("Allow Simultaneous Activatable Abilities (Like Judgements)".localize(), ref settings.toggleAllowAllActivatable),
                        () => Toggle("Kineticist: Allow Gather Power Without Hands".localize(), ref settings.toggleKineticistGatherPower),
                        () => Toggle("Barbarian: Auto Start Rage When Entering Combat".localize(), ref settings.toggleEnterCombatAutoRage),
                        () => Toggle("Demon: Auto Start Rage When Entering Combat".localize(), ref settings.toggleEnterCombatAutoRageDemon),
                        () => Toggle("Magus: Always Allow Spell Combat".localize(), ref settings.toggleAlwaysAllowSpellCombat),
                        () => { }
                        );
            Div(0, 25);
            HStack("Experience Multipliers".localize(), 1,
                () => LogSlider("All Experience".localize(), ref settings.experienceMultiplier, 0f, 100f, 1, 1, "", AutoWidth()),
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Combat".localize(), ref settings.useCombatExpSlider, Width(275));
                        if (settings.useCombatExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref settings.experienceMultiplierCombat, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Quests".localize(), ref settings.useQuestsExpSlider, Width(275));
                        if (settings.useQuestsExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref settings.experienceMultiplierQuests, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Skill Checks".localize(), ref settings.useSkillChecksExpSlider, Width(275));
                        if (settings.useSkillChecksExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref settings.experienceMultiplierSkillChecks, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Override for Traps".localize(), ref settings.useTrapsExpSlider, Width(275));
                        if (settings.useTrapsExpSlider) {
                            Space(10);
                            LogSliderCustomLabelWidth("", ref settings.experienceMultiplierTraps, 0f, 100f, 1, 1, "", 12, AutoWidth());
                        }
                    }
                }
                );
            Div(0, 25);
            HStack("Other Multipliers".localize(), 1,
                () => {
                    LogSlider("Fog of War Range".localize(), ref settings.fowMultiplier, 0f, 100f, 1, 1, "", AutoWidth());
                    List<UnitEntityData> units = Game.Instance?.Player?.m_PartyAndPets;
                    if (units != null) {
                        foreach (var unit in units) {
                            FogOfWarController.VisionRadiusMultiplier = settings.fowMultiplier;
                            FogOfWarRevealerSettings revealer = unit.View?.FogOfWarRevealer;
                            if (revealer != null) {
                                if (settings.fowMultiplier == 1) {
                                    revealer.DefaultRadius = true;
                                    revealer.UseDefaultFowBorder = true;
                                    revealer.Radius = 1.0f;
                                }
                                else {
                                    revealer.DefaultRadius = false;
                                    revealer.UseDefaultFowBorder = false;
                                    revealer.Radius = FogOfWarController.VisionRadius * settings.fowMultiplier;
                                }
                            }
                        }
                    }
                },
                () => LogSlider("Money Earned".localize(), ref settings.moneyMultiplier, 0f, 20, 1, 1, "", AutoWidth()),
                () => LogSlider("Vendor Sell Price".localize(), ref settings.vendorSellPriceMultiplier, 0f, 20, 1, 1, "", AutoWidth()),
                () => LogSlider("Vendor Buy Price".localize(), ref settings.vendorBuyPriceMultiplier, 0f, 20, 1, 1, "", AutoWidth()),
                () => Slider("Increase Carry Capacity".localize(), ref settings.encumberanceMultiplier, 1, 100, 1, "", AutoWidth()),
                () => Slider("Increase Carry Capacity (Party Only)".localize(), ref settings.encumberanceMultiplierPartyOnly, 1, 100, 1, "", AutoWidth()),
                () => LogSlider("Spontaneous Spells Per Day".localize(), ref settings.spellsPerDayMultiplier, 0f, 20, 1, 1, "", AutoWidth()),
                () => LogSlider("Prepared Spellslots".localize(), ref settings.memorizedSpellsMultiplier, 0f, 20, 1, 1, "", AutoWidth()),
                () => {
                    LogSlider("Movement Speed".localize(), ref settings.partyMovementSpeedMultiplier, 0f, 20, 1, 1, "", Width(600));
                    Space(25);
                    Toggle("Whole Team Moves Same Speed".localize(), ref settings.toggleMoveSpeedAsOne);
                    Space(25);
                    Label("Adjusts the movement speed of your party in area maps".localize().green());
                },
                () => {
                    LogSlider("Travel Speed".localize(), ref settings.travelSpeedMultiplier, 0f, 20, 1, 1, "", Width(600));
                    Space(25);
                    Label("Adjusts the movement speed of your party on world maps".localize().green());
                },
                () => {
                    LogSlider("Companion Cost".localize(), ref settings.companionCostMultiplier, 0, 20, 1, 1, "", Width(600));
                    Space(25);
                    Label("Adjusts costs of hiring mercenaries at the Pathfinder vendor".localize().green());

                },
                () => LogSlider("Enemy HP Multiplier".localize(), ref settings.enemyBaseHitPointsMultiplier, 0.1f, 20, 1, 1, "", AutoWidth()),
                () => LogSlider("Buff Duration".localize(), ref settings.buffDurationMultiplierValue, 0f, 9999, 1, 1, "", AutoWidth()),
                () => DisclosureToggle("Exceptions to Buff Duration Multiplier (Advanced; will cause blueprints to load)".localize(), ref showBuffDurationExceptions),
                () => {
                    if (!showBuffDurationExceptions) return;

                    BuffExclusionEditor.OnGUI();
                },
                () => { }
                );
            Actions.ApplyTimeScale();
            Div(0, 25);
            HStack("Dice Rolls".localize(), 1,
                () => EnumGrid("All Attacks Hit".localize(), ref settings.allAttacksHit, true, AutoWidth()),
                () => EnumGrid("All Hits Critical".localize(), ref settings.allHitsCritical, true, AutoWidth()),
                () => EnumGrid("Roll With Avantage".localize(), ref settings.rollWithAdvantage, true, AutoWidth()),
                () => EnumGrid("Roll With Disavantage".localize(), ref settings.rollWithDisadvantage, true, AutoWidth()),
                () => EnumGrid("Always Roll 20".localize(), ref settings.alwaysRoll20, true, AutoWidth()),
                () => EnumGrid("Always Roll 1".localize(), ref settings.alwaysRoll1, true, AutoWidth()),
                () => EnumGrid("Never Roll 20".localize(), ref settings.neverRoll20, true, AutoWidth()),
                () => EnumGrid("Never Roll 1".localize(), ref settings.neverRoll1, true, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 20".localize(), ref settings.roll20Initiative, true, AutoWidth()),
                () => EnumGrid("Initiative: Always Roll 1".localize(), ref settings.roll1Initiative, true, AutoWidth()),
                () => EnumGrid("Non Combat: Take 10".localize(), ref settings.take10always, true, AutoWidth()),
                //                () => EnumGrid("Non Combat: Take 10 (Min)", ref settings.take10minimum, AutoWidth()),
                () => EnumGrid("Non Combat: Take 20".localize(), ref settings.alwaysRoll20OutOfCombat, true, AutoWidth()),
                () => { 330.space(); Label("The following skill check adjustments apply only out of combat".localize().green()); },
                () => EnumGrid("Skill Checks: Take 10".localize(), ref settings.skillsTake10, true, AutoWidth()),
                () => EnumGrid("Skill Checks: Take 20".localize(), ref settings.skillsTake20, true, AutoWidth()),
                () => { }
                );
            Div(0, 25);
            HStack("Summons".localize(), 1,
                () => Toggle("Make Controllable".localize(), ref settings.toggleMakeSummmonsControllable),
                () => {
                    using (VerticalScope()) {
                        Div(0, 25);
                        using (HorizontalScope()) {
                            Label("Primary".localize().orange(), AutoWidth()); Space(215); Label("good for party".localize().green());
                        }
                        Space(25);
                        EnumGrid("Modify Summons For".localize(), ref settings.summonTweakTarget1, true, AutoWidth());
                        LogSlider("Duration Multiplier".localize(), ref settings.summonDurationMultiplier1, 0f, 20, 1, 2, "", AutoWidth());
                        Slider("Level Increase/Decrease".localize(), ref settings.summonLevelModifier1, -20f, +20f, 0f, 0, "", AutoWidth());
                        Div(0, 25);
                        using (HorizontalScope()) {
                            Label("Secondary".localize().orange(), AutoWidth()); Space(215); Label("good for larger group or to reduce enemies".localize().green());
                        }
                        Space(25);
                        EnumGrid("Modify Summons For".localize(), ref settings.summonTweakTarget2, true, AutoWidth());
                        LogSlider("Duration Multiplier".localize(), ref settings.summonDurationMultiplier2, 0f, 20, 1, 2, "", AutoWidth());
                        Slider("Level Increase/Decrease".localize(), ref settings.summonLevelModifier2, -20f, +20f, 0f, 0, "", AutoWidth());
                    }
                },
                () => { }
             );
        }
    }
}
