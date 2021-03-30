// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
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
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace ToyBox {
    public class BlueprintBrowser {
        public static IEnumerable<BlueprintScriptableObject> filteredBPs = null;
        static bool firstSearch = true;
        public static String[] filteredBPNames = null;
        public static int matchCount = 0;
        public static String parameter = "";
        static int selectedBlueprintIndex = -1;
        static BlueprintScriptableObject selectedBlueprint = null;
 

        static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter("All", typeof(BlueprintScriptableObject)),
            new NamedTypeFilter("Facts",typeof(BlueprintFact)),
            new NamedTypeFilter("Features", typeof(BlueprintFeature)),
            new NamedTypeFilter("Abilities", typeof(BlueprintAbility), (bp) => !((BlueprintAbility)bp).IsSpell),
            new NamedTypeFilter("Spells", typeof(BlueprintAbility), (bp) => ((BlueprintAbility)bp).IsSpell),
            new NamedTypeFilter("Spellbooks", typeof(BlueprintSpellbook)),
            new NamedTypeFilter("Buffs", typeof(BlueprintBuff)),
            new NamedTypeFilter("Equipment", typeof(BlueprintItemEquipment)),
            new NamedTypeFilter("Weapons", typeof(BlueprintItemWeapon)),
            new NamedTypeFilter("Shields", typeof(BlueprintItemShield)),
            new NamedTypeFilter("Head", typeof(BlueprintItemEquipmentHead)),
            new NamedTypeFilter("Glasses", typeof(BlueprintItemEquipmentGlasses)),
            new NamedTypeFilter("Neck", typeof(BlueprintItemEquipmentNeck)),
            new NamedTypeFilter("Shoulders", typeof(BlueprintItemEquipmentShoulders)),
            new NamedTypeFilter("Armor", typeof(BlueprintItemArmor)),
            new NamedTypeFilter("Shirt", typeof(BlueprintItemEquipmentShirt)),
            new NamedTypeFilter("Belts", typeof(BlueprintItemEquipmentBelt)),
            new NamedTypeFilter("Wrist", typeof(BlueprintItemEquipmentWrist)),
            new NamedTypeFilter("Hand", typeof(BlueprintItemEquipmentHand)),
            new NamedTypeFilter("Rings", typeof(BlueprintItemEquipmentRing)),
            new NamedTypeFilter("Gloves", typeof(BlueprintItemEquipmentGloves)),
            new NamedTypeFilter("Boots", typeof(BlueprintItemEquipmentFeet)),
            new NamedTypeFilter("Usable", typeof(BlueprintItemEquipmentUsable)),
            new NamedTypeFilter("Units", typeof(BlueprintUnit)),
            new NamedTypeFilter("Races", typeof(BlueprintRace)),
            new NamedTypeFilter("Quests", typeof(BlueprintQuest)),

        };

        public static IEnumerable<BlueprintScriptableObject> blueprints = null;
        public static IEnumerable<BlueprintScriptableObject> GetBluePrints() {
            if (blueprints == null) {
                if (BlueprintLoader.LoadInProgress()) { return null; }
                else {
                    Logger.Log($"calling BlueprintLoader.Load");
                    BlueprintLoader.Load((bps) => {
                        blueprints = bps;
                        UpdateSearchResults();
                        Logger.Log($"success got {bps.Count()} bluerints");
                    });
                    return null;
                }
            }
            return blueprints;
        }

        public static void ResetSearch() {
            filteredBPs = null;
            filteredBPNames = null;
        }

        public static void ResetGUI() {
            ResetSearch();
            Main.settings.selectedBPTypeFilter = 1;
        }
        public static void UpdateSearchResults() {
            if (blueprints == null) return; 
            selectedBlueprint = null;
            selectedBlueprintIndex = -1;
            if (Main.settings.searchText.Trim().Length == 0) {
                ResetSearch();
            }
            var terms = Main.settings.searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            var bpTypeFilter = blueprintTypeFilters[Main.settings.selectedBPTypeFilter];
            var selectedType = bpTypeFilter.type;
            var bps = BlueprintExensions.BlueprintsOfType(selectedType).Where((bp) => bpTypeFilter.filter(bp));
            var filtered = new List<BlueprintScriptableObject>();
            foreach (BlueprintScriptableObject blueprint in bps) {
                var name = blueprint.name.ToLower();
                if (terms.All(term => name.Contains(term))) {
                    filtered.Add(blueprint);
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered.OrderBy(bp => bp.name).Take(Main.settings.searchLimit).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            firstSearch = false;
        }

        public static IEnumerable OnGUI() {
            UI.ActionSelectionGrid(ref Main.settings.selectedBPTypeFilter,
                blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                9,
                (selected) => { UpdateSearchResults(); },
                UI.MinWidth(200));
            UI.Space(10);

            UI.BeginHorizontal();
            UI.ActionTextField(
                ref Main.settings.searchText,
                "searhText", 
                (text) => { },
                () => { UpdateSearchResults(); },
                UI.Width(400));
            UI.Label("Limit", UI.ExpandWidth(false));
            UI.ActionIntTextField(
                ref Main.settings.searchLimit,
                "searchLimit", 
                (limit) => { },
                () => { UpdateSearchResults(); },
                UI.Width(200));
            if (Main.settings.searchLimit > 1000) { Main.settings.searchLimit = 1000; }
            UI.EndHorizontal();
            UI.BeginHorizontal();
            UI.ActionButton("Search", () => {
                UpdateSearchResults();
            }, UI.AutoWidth());
            UI.Space(25);
            if (firstSearch) {
                UI.Label("please note the first search may take a few seconds.".green(), UI.AutoWidth());
            }
            else if (matchCount > 0) {
                String title = "Matches: ".green().bold() + $"{matchCount}".orange().bold();
                if (matchCount > Main.settings.searchLimit) { title += " => ".cyan() + $"{Main.settings.searchLimit}".cyan().bold(); }
                UI.Label(title, UI.ExpandWidth(false));
            }
            UI.EndHorizontal();
            UI.Space(10);

            if (filteredBPs != null) {
                CharacterPicker.OnGUI();
                UI.Space(25);
                UnitReference selected = CharacterPicker.GetSelectedCharacter();
                BlueprintListUI.OnGUI(selected, filteredBPs);
            }
            UI.Space(25);
            return null;
        }
    }
}