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
    public class CheapTricks {

        public static void OnGUI(UnityModManager.ModEntry modEntry) {
            UI.Space(25);
            UI.HStack("Combat", 4,
                () => { UI.ActionButton("Rest All", () => { CheatsCombat.RestAll(); }); },
                () => { UI.ActionButton("Empowered", () => { CheatsCombat.Empowered(""); }); },
                () => { UI.ActionButton("Full Buff Please", () => { CheatsCombat.FullBuffPlease(""); }); },
                () => { UI.ActionButton("Remove Buffs", () => { Actions.RemoveAllBuffs(); }); },
                () => { UI.ActionButton("Remove Death's Door", () => { CheatsCombat.DetachDebuff(); }); },
                () => { UI.ActionButton("Kill All Enemies", () => { CheatsCombat.KillAll(); }); },
                () => { UI.ActionButton("Summon Zoo", () => { CheatsCombat.SpawnInspectedEnemiesUnderCursor(""); }); }
             );
            UI.Space(10);
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
            UI.Space(10);
            UI.HStack("Preview", 0, () => {
                UI.Toggle("Dialog Results", ref Main.settings.previewDialogResults, UI.AutoWidth());
                UI.Toggle("Dialog Alignment", ref Main.settings.previewAlignmentRestrictedDialog, UI.AutoWidth());
                UI.Toggle("Random Encounters", ref Main.settings.previewRandomEncounters, UI.AutoWidth());
                UI.Toggle("Events", ref Main.settings.previewEventResults, UI.AutoWidth());
            });
            UI.Space(10);
            UI.HStack("Flags", 4,
                () => { UI.Toggle("Object Highlight Toggle Mode", ref Main.settings.highlightObjectsToggle, UI.AutoWidth()); },
                () => { UI.Toggle("Whole Team Moves Same Speed", ref Main.settings.toggleMoveSpeedAsOne, UI.AutoWidth()); },
                () => { UI.Toggle("Instant Cooldown", ref Main.settings.toggleInstantCooldown, UI.AutoWidth()); },
                () => { UI.Toggle("Spontaneous Casters Can Copy Spells", ref Main.settings.toggleSpontaneousCopyScrolls, UI.AutoWidth()); },
                () => { UI.Toggle("Disable Equipment Restrictions", ref Main.settings.toggleEquipmentRestrictions, UI.AutoWidth()); },
                () => { UI.Toggle("Disable Dialog Restrictions", ref Main.settings.toggleDialogRestrictions, UI.AutoWidth()); },
                () => { UI.Toggle("Infinite Charges On Items", ref Main.settings.toggleInfiniteItems, UI.AutoWidth()); },
                () => { UI.Toggle("No Friendly Fire On AOEs", ref Main.settings.toggleNoFriendlyFireForAOE, UI.AutoWidth()); },
                () => { UI.Toggle("Free Meta-Magic", ref Main.settings.toggleMetamagicIsFree, UI.AutoWidth()); },
                () => { UI.Toggle("No Material Components", ref Main.settings.toggleMaterialComponent, UI.AutoWidth()); },
                //() => { UI.Toggle("Restore Spells & Skills After Combat", ref Main.settings.toggleRestoreSpellsAbilitiesAfterCombat, UI.AutoWidth()); },
                () => { UI.Toggle("Access Remote Characters", ref Main.settings.toggleAccessRemoteCharacters, UI.AutoWidth()); },
                //() => { UI.Toggle("Show Pet Portraits", ref Main.settings.toggleShowAllPartyPortraits, UI.AutoWidth()); },
                () => { UI.Toggle("Instant Rest After Combat", ref Main.settings.toggleInstantRestAfterCombat, UI.AutoWidth()); },
                () => { }
                );
            UI.Space(10);
            UI.HStack("Multipliers", 1, 
                () => { UI.Slider("Experience", ref Main.settings.experienceMultiplier, 0.1f, 10, 1, 1, UI.AutoWidth()); },
                () => { UI.Slider("Money Earned", ref Main.settings.moneyMultiplier, 0.1f, 10, 1, 1, UI.AutoWidth()); },
                () => { UI.Slider("Sell Price", ref Main.settings.vendorSellPriceMultiplier, 0.1f, 30, Main.settings.defaultVendorSellPriceMultiplier, 1, UI.AutoWidth()); },
                () => { UI.Slider("Encumberance", ref Main.settings.encumberanceMultiplier, 1, 100, 1, UI.AutoWidth()); },
                () => { UI.Slider("Spells Per Day", ref Main.settings.spellsPerDayMultiplier, 0.1f, 5, 1, 1, UI.AutoWidth()); },
                () => { UI.Slider("Movement Speed", ref Main.settings.partyMovementSpeedMultiplier, 0.1f, 10, 1, 1, UI.AutoWidth()); },
                () => { UI.Slider("Travel Speed", ref Main.settings.travelSpeedMultiplier, 0.1f, 10, 1, 1, UI.AutoWidth()); },
                () => { UI.Slider("Companion Cost", ref Main.settings.companionCostMultiplier, 0, 5, 1, 1, UI.AutoWidth()); },
                () => { UI.Slider("Enemy HP Multiplier", ref Main.settings.enemyBaseHitPointsMultiplier, 0.1f, 10, 1, 1, UI.AutoWidth()); },
                () => { }
                );
            UI.Space(10);
            UI.HStack("Level Up", 1,
                () => { UI.Slider("Feats Multiplier", ref Main.settings.featsMultiplier, 1, 5, 1, UI.AutoWidth()); },
                () => { UI.Toggle("Always Able To Level Up", ref Main.settings.toggleNoLevelUpRestirctions, UI.AutoWidth()); },
                () => { UI.Toggle("Add Full Hit Die Value", ref Main.settings.toggleFullHitdiceEachLevel, UI.AutoWidth()); },
                () => { UI.Toggle("Ignore Class And Feat Restrictions", ref Main.settings.toggleIgnorePrerequisites, UI.AutoWidth()); },
                () => { UI.Toggle("Ignore Prerequisites When Choosing A Feat", ref Main.settings.toggleFeaturesIgnorePrerequisites, UI.AutoWidth()); },
                () => { UI.Toggle("Ignore Caster Type And Spell Level Restrictions", ref Main.settings.toggleIgnoreCasterTypeSpellLevel, UI.AutoWidth()); },
                () => { UI.Toggle("Ignore Forbidden Archetypes", ref Main.settings.toggleIgnoreForbiddenArchetype, UI.AutoWidth()); },
                () => { UI.Toggle("Ignore Required Stat Values", ref Main.settings.toggleIgnorePrerequisiteStatValue, UI.AutoWidth()); },
                () => { UI.Toggle("Ignore Alignment When Choosing A Class", ref Main.settings.toggleIgnoreAlignmentWhenChoosingClass, UI.AutoWidth()); },
                () => { UI.Toggle("Skip Spell Selection", ref Main.settings.toggleSkipSpellSelection, UI.AutoWidth()); },

#if false
                // Do we need these or is it covered by Ignore Fe
                () => { UI.Toggle("Ignore Feat Prerequisites When Choosing A Class", ref Main.settings.toggleIgnoreFeaturePrerequisitesWhenChoosingClass, UI.AutoWidth()); },
                () => { UI.Toggle("Ignore Feat Prerequisits (List) When Choosing A Class", ref Main.settings.toggle, UI.AutoWidth()); },
#endif

                () => { }
                );
            UI.Space(10);
            UI.HStack("Character Creation", 1,
                () => { UI.Slider("Build Points (Main)", ref Main.settings.characterCreationAbilityPointsPlayer, 1, 200, 25, UI.AutoWidth()); },
                () => { UI.Slider("Build Points (Mercenary)", ref Main.settings.characterCreationAbilityPointsMerc, 1, 200, 20, UI.AutoWidth()); },
                () => { UI.Slider("Ability Max", ref Main.settings.characterCreationAbilityPointsMax, 0, 50, 18, UI.AutoWidth()); },
                () => { UI.Slider("Ability Min", ref Main.settings.characterCreationAbilityPointsMin, 0, 50, 7, UI.AutoWidth()); },
                () => { }
                );
            UI.Space(10);
            UI.HStack("Crusade", 1,
                () => { UI.Toggle("Instant Events", ref Main.settings.toggleInstantEvent, UI.AutoWidth()); },
                () => {
                    UI.Slider("Build Time Modifer", ref Main.settings.kingdomBuildingTimeModifier, -10, 10, 0, 1, UI.AutoWidth());
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