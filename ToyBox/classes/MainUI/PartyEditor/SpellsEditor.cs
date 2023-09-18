using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using ToyBox.classes.Infrastructure;
using UnityEngine;
using static ModKit.UI;
using static ToyBox.BlueprintExtensions;
#if Wrath
using Kingmaker.Blueprints.Classes.Spells;
#elif RT
using Kingmaker.Code.UnitLogic;
#endif

namespace ToyBox {
    public partial class PartyEditor {
        public static Dictionary<UnitEntityData, Browser<BlueprintSpellbook, Spellbook>> SpellbookBrowserDict = new();
        public static Dictionary<UnitEntityData, Browser<BlueprintAbility, AbilityData>> SpellBrowserDict = new();
        private static bool _startedLoading = false;
        public static List<Action> OnSpellsGUI(UnitEntityData ch, List<Spellbook> spellbooks) {
            List<Action> todo = new();
            Space(20);
            var names = spellbooks.Select((sb) => sb.Blueprint.GetDisplayName()).ToArray();
            var titles = names.Select((name, i) => $"{name} ({spellbooks.ElementAt(i).CasterLevel})").ToArray();
            if (spellbooks.Any()) {
                if (selectedSpellbook >= spellbooks.Count)
                    selectedSpellbook = 0;
                var spellbook = spellbooks.ElementAt(selectedSpellbook);
                bool selectedSpellBookChanged = false;
                using (HorizontalScope()) {
                    selectedSpellBookChanged = SelectionGrid(ref selectedSpellbook, titles, Math.Min(titles.Length, 7), AutoWidth());
                    if (selectedSpellbook >= names.Length) selectedSpellbook = 0;
                    DisclosureToggle("Edit".localize().orange().bold(), ref editSpellbooks);
#if Wrath
                    Space(-50);
                    var mergeableClasses = ch.MergableClasses().ToList();
                    if (spellbook.IsStandaloneMythic || mergeableClasses.Count() == 0) {
                        Label($"Mythic Merging".localize().cyan(), AutoWidth());
                        25.space();
                        Label("When you get standalone mythic spellbooks you can merge them here by select.".localize().green());
                    }
                    else {
                        Label($"Merge Mythic:".localize().cyan(), 175.width());
                        25.space();
                        foreach (var cl in mergeableClasses) {
                            var sb = spellbook;
                            ActionButton(cl.CharacterClass.LocalizedName.ToString(), () => sb.MergeMythicSpellbook(cl));
                            15.space();
                        }
                        25.space();
                        using (VerticalScope()) {
                            Label("Merging your mythic spellbook will cause you to transfer all mythic spells to your normal spellbook and gain caster levels equal to your mythic level. You will then be able to re-select spells on next level up or mythic level up. Merging a second mythic spellbook will transfer the spells but not increase your caster level further.  If you want more CL then increase it below.".localize().green());
                            Label("Warning: This is irreversible. Please save before continuing!".localize().Orange());
                        }
                    }
#endif
                }
                spellbook = spellbooks.ElementAt(selectedSpellbook);
                if (editSpellbooks) {
                    spellbookEditCharacter = ch;
                    SpellBookBrowserOnGUI(ch, spellbooks, todo);
                }
                else {
                    var spellBrowser = SpellBrowserDict.GetValueOrDefault(ch, null);
                    if (spellBrowser == null) {
                        spellBrowser = new Browser<BlueprintAbility, AbilityData>(Mod.ModKitSettings.searchAsYouType);
                        SpellBrowserDict[ch] = spellBrowser;
                    }
                    var maxLevel = spellbook.Blueprint.MaxSpellLevel;
                    var casterLevel = spellbook.CasterLevel;
                    using (HorizontalScope()) {
                        var tempSelected = selectedSpellbookLevel;
                        EnumerablePicker(
                            "Spells known".localize(),
                            ref selectedSpellbookLevel,
                            Enumerable.Range(0, spellbook.Blueprint.MaxSpellLevel + 2),
                            0,
                            (lvl) => {
                                if (lvl < spellbook.Blueprint.MaxSpellLevel + 1) {
                                    var levelText = spellbook.Blueprint?.SpellsPerDay?.GetCount(casterLevel, lvl) != null ? $"L{lvl}".bold() : $"L{lvl}".grey();
                                    var knownCount = spellbook.GetKnownSpells(lvl).Count;
                                    var countText = knownCount > 0 ? $" ({knownCount})".white() : "";
                                    return levelText + countText;
                                }
                                else {
                                    return "All Spells".localize();
                                }
                            },
                            AutoWidth()
                        );
                        if (tempSelected != selectedSpellbookLevel || selectedSpellBookChanged) {
                            spellBrowser.ResetSearch();
                            spellBrowser.startedLoadingAvailable = true;
                        }
                        Space(20);
                        if (casterLevel > 0) {
                            ActionButton("-1 CL", () => CasterHelpers.LowerCasterLevel(spellbook), AutoWidth());
                        }
                        if (casterLevel < 80) {
                            ActionButton("+1 CL", () => CasterHelpers.AddCasterLevel(spellbook), AutoWidth());
                        }
                        // removes opposition schools; these are not cleared when removing facts; to add new opposition schools, simply add the corresponding fact again
#if Wrath
                        if (spellbook.OppositionSchools.Any()) {
                            ActionButton("Clear Opposition Schools".localize(), () => {
                                spellbook.OppositionSchools.Clear();
                                spellbook.ExOppositionSchools.Clear();
                                ch.Facts.RemoveAll<UnitFact>(r => r.Blueprint.GetComponent<AddOppositionSchool>(), true);
                            }, AutoWidth());
                        }
                        if (spellbook.OppositionDescriptors != 0) {
                            ActionButton("Clear Opposition Descriptors".localize(), () => {
                                spellbook.OppositionDescriptors = 0;
                                ch.Facts.RemoveAll<UnitFact>(r => r.Blueprint.GetComponent<AddOppositionDescriptor>(), true);
                            }, AutoWidth());
                        }
#endif
                    }
                    var unorderedSpells = selectedSpellbookLevel <= spellbook.Blueprint.MaxSpellLevel ? spellbook.GetKnownSpells(selectedSpellbookLevel) : spellbook.GetAllKnownSpells();
                    var spells = unorderedSpells.OrderBy(d => d.Name).ToList();
                    SelectedSpellbook[ch.HashKey()] = spellbook;
                    spellBrowser.OnGUI(
                        spells,
                        () => {
                            List<BlueprintAbility> availableSpells;
                            if (Settings.showFromAllSpellbooks || (spellbook.Blueprint.MaxSpellLevel + 1) == selectedSpellbookLevel) {
                                if ((spellbook.Blueprint.MaxSpellLevel + 1) == selectedSpellbookLevel) {
                                    availableSpells = new List<BlueprintAbility>(CasterHelpers.GetAllSpells(-1));
                                }
                                else {
                                    availableSpells = new List<BlueprintAbility>(CasterHelpers.GetAllSpells(selectedSpellbookLevel));
                                }
                            }
                            else {
                                availableSpells = new List<BlueprintAbility>(spellbook.Blueprint.SpellList.GetSpells(selectedSpellbookLevel));
                            }
                            if (!((spellbook.Blueprint.MaxSpellLevel + 1) == selectedSpellbookLevel)) {
                                spells.ForEach((s) => availableSpells.Add(s.Blueprint));
                            }
                            return availableSpells.ToHashSet().ToList(); // todo: why are there duplicates here?
                        },
                        feature => feature.Blueprint,
                        blueprint => $"{GetTitle(blueprint)}" + (Settings.searchDescriptions ? $" {blueprint.GetDescription()}" : ""),
                                        blueprint => new[] { GetTitle(blueprint) },
                        () => {
                            using (HorizontalScope()) {
                                bool needsReload = false;
                                Toggle("Show GUIDs".localize(), ref Main.Settings.showAssetIDs);
                                20.space();
                                needsReload |= Toggle("Show Internal Names".localize(), ref Settings.showDisplayAndInternalNames);
                                20.space();
                                // Toggle("Show Inspector", ref Settings.factEditorShowInspector);
                                // 20.space();
                                needsReload |= Toggle("Search Descriptions".localize(), ref Settings.searchDescriptions);
                                20.space();
                                if (Toggle("Search All Spellbooks".localize(), ref Settings.showFromAllSpellbooks)) {
                                    spellBrowser.ResetSearch();
                                    _startedLoading = true;
                                }
                                if (needsReload) spellBrowser.ResetSearch();
                                GUI.enabled = !spellBrowser.isSearching;
                                Space(20);
                                ActionButton("Add All".localize(), () => CasterHelpers.HandleAddAllSpellsOnPartyEditor(ch.Descriptor(), spellBrowser.filteredDefinitions.Cast<BlueprintAbility>().ToList()), AutoWidth());
                                Space(20);
                                ActionButton("Remove All".localize(), () => CasterHelpers.HandleAddAllSpellsOnPartyEditor(ch.Descriptor()), AutoWidth());
                                GUI.enabled = true;
                                if ((spellbook.Blueprint.MaxSpellLevel + 1) == selectedSpellbookLevel) {
                                    10.space();
                                    Label("Spells are added at Level: ".localize().green() + SelectedNewSpellLvl.ToString().orange(), AutoWidth());
                                    10.space();
                                    ActionButton("-", () => {
                                        if (SelectedNewSpellLvl >= 0) {
                                            if (SelectedNewSpellLvl == 0) {
                                                SelectedNewSpellLvl = spellbook.Blueprint.MaxSpellLevel;
                                            }
                                            else {
                                                SelectedNewSpellLvl -= 1;
                                            }
                                        }
                                    }, AutoWidth());
                                    ActionButton("+", () => {
                                        if (SelectedNewSpellLvl == spellbook.MaxSpellLevel) {
                                            SelectedNewSpellLvl = 1;
                                        }
                                        else {
                                            SelectedNewSpellLvl += 1;
                                        }
                                    }, AutoWidth());
                                }
                            }
                        },
                        (blueprint, feature) => FactsEditor.BlueprintRowGUI(spellBrowser, feature,
                        blueprint, ch, todo),
                        (blueprint, feature) => {
                            ReflectionTreeView.OnDetailGUI(blueprint);
                        }, 50, false, true, 100, 300, "", true);
                }
            }
            else {
                SpellBookBrowserOnGUI(ch, spellbooks, todo, true);
            }
            return todo;
        }
        private static void SpellBookBrowserOnGUI(UnitEntityData ch, IEnumerable<Spellbook> spellbooks, List<Action> todo, bool forceShowAll = false) {
            var spellbookBrowser = SpellbookBrowserDict.GetValueOrDefault(ch, null);
            if (spellbookBrowser == null) {
                spellbookBrowser = new Browser<BlueprintSpellbook, Spellbook>(Mod.ModKitSettings.searchAsYouType);
                SpellbookBrowserDict[ch] = spellbookBrowser;
            }
            if (forceShowAll) {
                spellbookBrowser.ShowAll = true;
                spellbookBrowser.needsReloadData = true;
            }
            spellbookBrowser.OnGUI(
                spellbooks,
                BlueprintExtensions.GetBlueprints<BlueprintSpellbook>,
                (feature) => feature.Blueprint,
                blueprint => $"{GetTitle(blueprint)}" + (Settings.searchDescriptions ? $" {blueprint.GetDescription()}" : ""),
                blueprint => new[] { GetTitle(blueprint) },
                () => {
                    using (HorizontalScope()) {
                        Toggle("Show GUIDs".localize(), ref Main.Settings.showAssetIDs, 150.width());
                        20.space();
                        if (Toggle("Show Internal Names".localize(), ref Settings.showDisplayAndInternalNames, 200.width()))
                            spellbookBrowser.ResetSearch();
                        20.space();
                        //                                Toggle("Show Inspector", ref Settings.factEditorShowInspector, 150.width());
                        //                                20.space();

                        if (Toggle("Search Descriptions".localize(), ref Settings.searchDescriptions, 250.width())) {
                            spellbookBrowser.ResetSearch();
                        }
                    }
                },
                (blueprint, feature) => FactsEditor.BlueprintRowGUI(spellbookBrowser, feature, blueprint, ch, todo),
                (blueprint, feature) => { ReflectionTreeView.OnDetailGUI(blueprint); },
                50,
                false,
                true,
                100,
                300,
                "",
                true);
        }
    }
}
