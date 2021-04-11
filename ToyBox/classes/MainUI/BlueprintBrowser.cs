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
using Kingmaker.AreaLogic;
using Kingmaker.Armies;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
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
using Kingmaker.Craft;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Globalmap.Blueprints;
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
        public static Settings settings { get { return Main.settings; } }

        public static IEnumerable<BlueprintScriptableObject> filteredBPs = null;
        public static IEnumerable<IGrouping<String, BlueprintScriptableObject>> collatedBPs = null;
        public static int selectedCollationIndex = 0;
        static bool firstSearch = true;
        public static String[] filteredBPNames = null;
        public static int matchCount = 0;
        public static String parameter = "";
        static int selectedBlueprintIndex = -1;
        static BlueprintScriptableObject selectedBlueprint = null;
 
        static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter("All", typeof(BlueprintScriptableObject), null, null),
            new NamedTypeFilter("Facts",typeof(BlueprintFact)),
            new NamedTypeFilter("Features", typeof(BlueprintFeature)),
            new NamedTypeFilter("Abilities", typeof(BlueprintAbility), (bp) => !((BlueprintAbility)bp).IsSpell),
            new NamedTypeFilter("Spells", typeof(BlueprintAbility), (bp) => ((BlueprintAbility)bp).IsSpell),
            new NamedTypeFilter("Spellbooks", typeof(BlueprintSpellbook)),
            new NamedTypeFilter("Buffs", typeof(BlueprintBuff)),
            new NamedTypeFilter("Item", typeof(BlueprintItem), null,  (bp) => {
                var ibp = (BlueprintItem)bp;
                if (ibp.m_NonIdentifiedNameText?.ToString().Length > 0) return ibp.m_NonIdentifiedNameText;
                return ibp.ItemType.ToString();
            }),
            new NamedTypeFilter("Equipment", typeof(BlueprintItemEquipment), null, (bp) => ((BlueprintItemEquipment)bp).ItemType.ToString()),
            new NamedTypeFilter("Weapons", typeof(BlueprintItemWeapon), null, (bp) => ((BlueprintItemWeapon)bp).Type?.NameSafe() ?? "?"),
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
            new NamedTypeFilter("Ingredient", typeof(BlueprintIngredient)),
            new NamedTypeFilter("Units", typeof(BlueprintUnit), null, bp => {
                var bpu = (BlueprintUnit)bp;
                return bpu.Type?.Name ?? bpu.Race?.Name ?? "?";
            }),
            new NamedTypeFilter("Races", typeof(BlueprintRace)),
            new NamedTypeFilter("Areas", typeof(BlueprintArea)),
            new NamedTypeFilter("Enter Points", typeof(BlueprintAreaEnterPoint)),
//            new NamedTypeFilter("Enter Points", typeof(BlueprintAreaEnterPoint)),
            new NamedTypeFilter("Global Map", typeof(BlueprintGlobalMapPoint)),
            new NamedTypeFilter("Feature Sel", typeof(BlueprintFeatureSelection)),
//            new NamedTypeFilter("Armies", typeof(BlueprintArmyPreset)),
            new NamedTypeFilter("Quests", typeof(BlueprintQuest)),
        };

        public static NamedTypeFilter selectedTypeFilter = null;

        public static IEnumerable<BlueprintScriptableObject> blueprints = null;
        public static IEnumerable<BlueprintScriptableObject> GetBlueprints() {
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
            collatedBPs = null;
        }

        public static void ResetGUI() {
            ResetSearch();
            settings.selectedBPTypeFilter = 1;
        }
        public static void UpdateSearchResults() {
            if (blueprints == null) return;
            selectedCollationIndex = 0;
            selectedBlueprint = null;
            selectedBlueprintIndex = -1;
            if (settings.searchText.Trim().Length == 0) {
                ResetSearch();
            }
            var terms = settings.searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();
            selectedTypeFilter = blueprintTypeFilters[settings.selectedBPTypeFilter];
            var selectedType = selectedTypeFilter.type;
            var bps = BlueprintExensions.BlueprintsOfType(selectedType).Where((bp) => selectedTypeFilter.filter(bp));
            var filtered = new List<BlueprintScriptableObject>();
            foreach (BlueprintScriptableObject blueprint in bps) {
                var name = blueprint.name.ToLower();
                if (terms.All(term => name.Contains(term))) {
                    filtered.Add(blueprint);
                }
            }
            filteredBPs = filtered.OrderBy(bp => bp.name);
            matchCount = filtered.Count();
            if (selectedTypeFilter.collator != null) {
                collatedBPs = filtered.GroupBy(selectedTypeFilter.collator);
                // I could do something like this but I will leave it up to the UI when a collation is selected.
                // GetItems().GroupBy(g => g.Type).Select(s => new { Type = s.Key, LastTen = s.Take(10).ToList() });
            }
            filteredBPs = filteredBPs.Take(settings.searchLimit).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            firstSearch = false;
        }
        public static IEnumerable OnGUI() {
            UI.ActionSelectionGrid(ref settings.selectedBPTypeFilter,
                blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                10,
                (selected) => { UpdateSearchResults(); },
                UI.MinWidth(200));
            UI.Space(10);

            UI.BeginHorizontal();
            UI.ActionTextField(
                ref settings.searchText,
                "searhText", 
                (text) => { },
                () => { UpdateSearchResults(); },
                UI.Width(400));
            UI.Label("Limit", UI.ExpandWidth(false));
            UI.ActionIntTextField(
                ref settings.searchLimit,
                "searchLimit", 
                (limit) => { },
                () => { UpdateSearchResults(); },
                UI.Width(200));
            if (settings.searchLimit > 1000) { settings.searchLimit = 1000; }
            UI.Space(25);
            UI.Toggle("Show GUIs", ref settings.showAssetIDs);
            UI.Space(25);
            UI.Toggle("Dividers", ref settings.showDivisions);

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
                if (matchCount > settings.searchLimit) { title += " => ".cyan() + $"{settings.searchLimit}".cyan().bold(); }
                if (collatedBPs != null) {
                    foreach (var group in collatedBPs) {
                        title += $" {group.Key} ({group.Count()})";
                    }
                }
                UI.Label(title, UI.ExpandWidth(false));
            }
            UI.Space(50);
            UI.Label("".green(), UI.AutoWidth());
            UI.EndHorizontal();
            UI.Space(10);

            if (filteredBPs != null) {
                CharacterPicker.OnGUI();
                UnitReference selected = CharacterPicker.GetSelectedCharacter();
                BlueprintListUI.OnGUI(selected, filteredBPs, collatedBPs, 0, null, selectedTypeFilter);
            }
            UI.Space(25);
            return null;
        }
    }
}