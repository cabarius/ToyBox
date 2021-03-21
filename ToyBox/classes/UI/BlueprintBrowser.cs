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
    public class BlueprintBrowser {
        public static BlueprintScriptableObject[] blueprints = null;
        public static BlueprintScriptableObject[] filteredBPs = null;
        public static String[] filteredBPNames = null;
        public static int matchCount = 0;
        public static String parameter = "";
        static int selectedBlueprintIndex = -1;
        static BlueprintScriptableObject selectedBlueprint = null;
        static bool searchChanged = false;
        static BackgroundWorker searchWorker = new BackgroundWorker();

        static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter("All", typeof(BlueprintScriptableObject)),
            new NamedTypeFilter("Facts",typeof(BlueprintFact)),
            new NamedTypeFilter("Features", typeof(BlueprintFeature)),
            new NamedTypeFilter("Buffs", typeof(BlueprintBuff)),
            new NamedTypeFilter("Weapons", typeof(BlueprintItemWeapon)),
            new NamedTypeFilter("Armor", typeof(BlueprintItemArmor)),
            new NamedTypeFilter("Shields", typeof(BlueprintItemShield)),
            new NamedTypeFilter("Equipment", typeof(BlueprintItemEquipment)),
            new NamedTypeFilter("Usable", typeof(BlueprintItemEquipmentUsable)),
        };
        public static BlueprintScriptableObject[] GetBlueprints() {
            var bundle = (AssetBundle)AccessTools.Field(typeof(ResourcesLibrary), "s_BlueprintsBundle")
                .GetValue(null);
            return bundle.LoadAllAssets<BlueprintScriptableObject>();
        }

        public static void ResetSearch() {
            filteredBPs = null;
            filteredBPNames = null;
        }
        static async void UpdateSearchResults() {
            if (blueprints == null) {
                blueprints = GetBlueprints().Where(bp => !BlueprintAction.ignoredBluePrintTypes.Contains(bp.GetType())).ToArray();
            }
            selectedBlueprint = null;
            selectedBlueprintIndex = -1;
            if (Main.settings.searchText.Trim().Length == 0) {
                ResetSearch();
            }
            var terms = Main.settings.searchText.Split(' ').Select(s => s.ToLower()).ToArray();
            var filtered = new List<BlueprintScriptableObject>();
            var selectedType = blueprintTypeFilters[Main.settings.selectedBPTypeFilter].type;
            foreach (BlueprintScriptableObject blueprint in blueprints) {
                var name = blueprint.name.ToLower();
                var type = blueprint.GetType();
                if (terms.All(term => name.Contains(term)) && type.IsKindOf(selectedType)) {
                    filtered.Add(blueprint);
                }
            }
            matchCount = filtered.Count();
            filteredBPs = filtered
                    .OrderBy(bp => bp.name)
                    .Take(Main.settings.searchLimit).OrderBy(bp => bp.name).ToArray();
            filteredBPNames = filteredBPs.Select(b => b.name).ToArray();
            searchChanged = false;
        }

        public static void OnGUI(UnityModManager.ModEntry modEntry) {
            if (selectedBlueprint != null) {
                UI.BeginHorizontal();
                UI.Label("Selected:", UI.ExpandWidth(false));
                UI.Space(10);
                UI.Label($"{selectedBlueprint.GetType().Name.cyan()}", UI.ExpandWidth(false));
                UI.Space(30);
                UI.Label($"{selectedBlueprint}".orange().bold());
                UI.EndHorizontal();
            }
            UI.Space(25);
            UI.Section("Search 'n Pick", () => {
                UI.Label("(please note the first search may take a few seconds)");
                UI.Space(25);
                UI.ActionSelectionGrid(ref Main.settings.selectedBPTypeFilter,
                    blueprintTypeFilters.Select(tf => tf.name).ToArray(),
                    5,
                    (selected) => {
                        searchChanged = true;
                    }, UI.ExpandWidth(false));
                UI.Space(10);

                UI.BeginHorizontal();
                UI.TextField(ref Main.settings.searchText, UI.Width(500f));
                UI.Space(50);
                UI.Label("Limit", UI.ExpandWidth(false));
                UI.IntTextField(ref Main.settings.searchLimit, UI.Width(500f));
                if (Main.settings.searchLimit > 1000) { Main.settings.searchLimit = 1000; }
                UI.EndHorizontal();

                UI.BeginHorizontal();
                UI.ActionButton("Search", () => {
                    UpdateSearchResults();
                }, UI.AutoWidth());
                if (Main.userHasHitReturn || searchChanged) {
                    UpdateSearchResults();
                }
                UI.Space(50);
                UI.Label((matchCount > 0
                            ? "Matches: ".green().bold() + $"{matchCount}".orange().bold()
                                + (matchCount > Main.settings.searchLimit
                                    ? " => ".cyan() + $"{Main.settings.searchLimit}".cyan().bold()
                                    : "")
                            : ""), UI.ExpandWidth(false));

                UI.EndHorizontal();
                UI.Space(10);

                if (filteredBPs != null) {
                    int index = 0;
                    int maxActionCount = 0;
                    foreach (BlueprintScriptableObject blueprint in filteredBPs) {
                        BlueprintAction[] actions = BlueprintAction.ActionsForBlueprint(blueprint);
                        int actionCount = actions != null ? actions.Count() : 0;
                        if (actionCount > maxActionCount) { maxActionCount = actionCount; }
                    }
                    foreach (BlueprintScriptableObject blueprint in filteredBPs) {
                        UI.BeginHorizontal();
                        UI.Label(blueprint.name.orange().bold(), UI.Width(650));
#if false
                    if (UI.Button("Select", UI.ExpandWidth(false)))
                    {
                        selectedBlueprintIndex = index;
                        selectedBlueprint = blueprint;
                        parameter = blueprint.name;
                    }
#endif
                        BlueprintAction[] actions = BlueprintAction.ActionsForBlueprint(blueprint);
                        int actionCount = actions != null ? actions.Count() : 0;
                        for (int ii = 0; ii < maxActionCount; ii++) {
                            if (ii < actionCount) {
                                BlueprintAction action = actions[ii];
                                UI.ActionButton(action.name, () => { action.action(blueprint); }, UI.Width(140));
                                UI.Space(10);
                            }
                            else {
                                UI.Space(154);
                            }
                        }
                        UI.Space(30);
                        UI.Label($"{blueprint.GetType().Name.cyan()}", UI.Width(400));
                        UI.EndHorizontal();
                        String description = blueprint.GetDescription();
                        if (description.Length > 0) {
                            UI.BeginHorizontal();
                            UI.Space(684 + maxActionCount * 154);
                            UI.Label($"{description.green()}");
                            UI.EndHorizontal();
                        }
                        index++;
                    }
                }

            });
        }

    }
}