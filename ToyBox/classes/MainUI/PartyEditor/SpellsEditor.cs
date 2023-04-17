using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
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
using static ModKit.UI;


namespace ToyBox {
    public partial class PartyEditor {
        public static Dictionary<UnitEntityData, Browser<Spellbook, BlueprintSpellbook>> SpellbookBrowserDict = new();
        public static Dictionary<UnitEntityData, Browser<AbilityData, BlueprintAbility>> SpellBrowserDict = new();
        private static bool _startedLoading = false;
        public static List<Action> OnSpellsGUI(UnitEntityData ch, IEnumerable<Spellbook> spellbooks) {
            List<Action> todo = new();
            Space(20);
            var names = spellbooks.Select((sb) => sb.Blueprint.GetDisplayName()).ToArray();
            var titles = names.Select((name, i) => $"{name} ({spellbooks.ElementAt(i).CasterLevel})").ToArray();
            if (spellbooks.Any()) {
                var spellbook = spellbooks.ElementAt(selectedSpellbook);
                using (HorizontalScope()) {
                    SelectionGrid(ref selectedSpellbook, titles, Math.Min(titles.Length, 7), AutoWidth());
                    if (selectedSpellbook >= names.Length) selectedSpellbook = 0;
                    DisclosureToggle("Edit".orange().bold(), ref editSpellbooks);
                    Space(-50);
                    var mergableClasses = ch.MergableClasses();
                    if (spellbook.IsStandaloneMythic || mergableClasses.Count() == 0) {
                        Label($"Merge Mythic".cyan(), AutoWidth());
                        25.space();
                        Label("When you get standalone mythic spellbooks you can merge them here.".green());
                    }
                    else {
                        Label($"Merge Mythic:".cyan(), 175.width());
                        25.space();
                        foreach (var cl in mergableClasses) {
                            ActionButton(cl.CharacterClass.LocalizedName.ToString(), () => spellbook.MergeMythicSpellbook(cl));
                            15.space();
                        }
                        25.space();
                        using (VerticalScope()) {
                            Label("Merging your mythic spellbook will cause you to transfer all mythic spells to your normal spellbook and gain caster levels equal to your mythic level. You will then be able to re-select spells on next level up or mythic level up. Merging a second mythic spellbook will transfer the spells but not increase your caster level further.  If you want more CL then increase it below.".green());
                            Label("Warning: This is irreversible. Please save before continuing!".Orange());
                        }
                    }
                }
                spellbook = spellbooks.ElementAt(selectedSpellbook);
                if (editSpellbooks) {
                    spellbookEditCharacter = ch;
                    SpellBookBrowserOnGUI(ch, spellbooks, todo);
                }
                else {
                    var SpellBrowser = SpellBrowserDict.GetValueOrDefault(ch, null);
                    if (SpellBrowser == null) {
                        SpellBrowser = new Browser<AbilityData, BlueprintAbility>();
                        SpellBrowserDict[ch] = SpellBrowser;
                    }
                    var maxLevel = spellbook.Blueprint.MaxSpellLevel;
                    var casterLevel = spellbook.CasterLevel;
                    using (HorizontalScope()) {
                        int tempSelected = selectedSpellbookLevel;
                        EnumerablePicker(
                            "Spells known",
                            ref selectedSpellbookLevel,
                            Enumerable.Range(0, spellbook.Blueprint.MaxSpellLevel + 1),
                            0,
                            (lvl) => {
                                var levelText = spellbook.Blueprint?.SpellsPerDay?.GetCount(casterLevel, lvl) != null ? $"L{lvl}".bold() : $"L{lvl}".grey();
                                var knownCount = spellbook.GetKnownSpells(lvl).Count;
                                var countText = knownCount > 0 ? $" ({knownCount})".white() : "";
                                return levelText + countText;
                            },
                            AutoWidth()
                        );
                        if (tempSelected != selectedSpellbookLevel) {
                            SpellBrowser.needsReloadData = true;
                        }
                        Space(20);
                        if (casterLevel > 0) {
                            ActionButton("-1 CL", () => CasterHelpers.LowerCasterLevel(spellbook), AutoWidth());
                        }
                        if (casterLevel < 40) {
                            ActionButton("+1 CL", () => CasterHelpers.AddCasterLevel(spellbook), AutoWidth());
                        }
                        // removes opposition schools; these are not cleared when removing facts; to add new opposition schools, simply add the corresponding fact again
                        if (spellbook.OppositionSchools.Any()) {
                            ActionButton("Clear Opposition Schools", () => {
                                spellbook.OppositionSchools.Clear();
                                spellbook.ExOppositionSchools.Clear();
                                ch.Facts.RemoveAll<UnitFact>(r => r.Blueprint.GetComponent<AddOppositionSchool>(), true);
                            }, AutoWidth());
                        }
                        if (spellbook.OppositionDescriptors != 0) {
                            ActionButton("Clear Opposition Descriptors", () => {
                                spellbook.OppositionDescriptors = 0;
                                ch.Facts.RemoveAll<UnitFact>(r => r.Blueprint.GetComponent<AddOppositionDescriptor>(), true);
                            }, AutoWidth());
                        }
                    }

                    var spells = spellbook.GetKnownSpells(selectedSpellbookLevel).OrderBy(d => d.Name).ToList();
                    SelectedSpellbook[ch.HashKey()] = spellbook;
                    SpellBrowser.OnGUI($"{FactsEditor.getName(spellbook.Blueprint)} Spells",
                        spells,
                        () => {
                            HashSet<BlueprintAbility> availableSpells;
                            if (settings.showFromAllSpellbooks) {
                                availableSpells = new HashSet<BlueprintAbility>(CasterHelpers.GetAllSpells(selectedSpellbookLevel));
                                if (_startedLoading) {
                                    if (availableSpells?.Count > 0) {
                                        SpellBrowser.needsReloadData = true;
                                        _startedLoading = false;
                                    }
                                }
                            }
                            else {
                                availableSpells = new HashSet<BlueprintAbility>(spellbook.Blueprint.SpellList.GetSpells(selectedSpellbookLevel));
                            }
                            spells.ForEach((s) => availableSpells.Add(s.Blueprint));
                            return availableSpells;
                        },
                        (feature) => feature.Blueprint,
                        (feature) => FactsEditor.getName(feature),
                        (feature) => $"{FactsEditor.getName(feature)} {feature.NameSafe()} {feature.GetDisplayName()} {feature.Comment}",
                        (feature) => FactsEditor.getName(feature),
                        () => {
                            using (HorizontalScope()) {
                                Toggle("Show GUIDs", ref Main.settings.showAssetIDs, 150.width());
                                20.space();
                                Toggle("Show Internal Names", ref settings.showDisplayAndInternalNames, 200.width());
                                20.space();
                                Toggle("Show Inspector", ref FactsEditor.showInspector, 150.width());
                                if (Toggle("Search Descriptions", ref settings.searchesDescriptions, 250.width())) {
                                    SpellBrowser.needsReloadData = true;
                                }
                                if (Toggle("Search All Spellbooks", ref settings.showFromAllSpellbooks)) {
                                    SpellBrowser.needsReloadData = true;
                                    _startedLoading = true;
                                }
                                Space(25);
                                ActionButton("Add All", () => CasterHelpers.HandleAddAllSpellsOnPartyEditor(ch.Descriptor, SpellBrowser.filteredOrderedDefinitions.Cast<BlueprintAbility>().ToList()), AutoWidth());
                                Space(25);
                                ActionButton("Remove All", () => CasterHelpers.HandleAddAllSpellsOnPartyEditor(ch.Descriptor), AutoWidth());
                            }
                        },
                        (feature, blueprint) => FactsEditor.RowGUI(feature, blueprint, ch, SpellBrowser, todo), (feature, blueprint) => {
                            ReflectionTreeView.DetailsOnGUI(blueprint);
                            return null;
                        }, 50, false, true, 100, 300, "", true);
                }
            }
            else {
                SpellBookBrowserOnGUI(ch, spellbooks, todo, true);
            }
            return todo;
        }
        private static void SpellBookBrowserOnGUI(UnitEntityData ch, IEnumerable<Spellbook> spellbooks, List<Action> todo, bool forceShowAll = false) {
            var SpellbookBrowser = SpellbookBrowserDict.GetValueOrDefault(ch, null);
            if (SpellbookBrowser == null) {
                SpellbookBrowser = new Browser<Spellbook, BlueprintSpellbook>();
                SpellbookBrowserDict[ch] = SpellbookBrowser;
            }
            if (forceShowAll) {
                SpellbookBrowser.showAll = true;
                SpellbookBrowser.needsReloadData = true;
            }
            SpellbookBrowser.OnGUI("Spellbook Browser",
                        spellbooks,
                        () => BlueprintExtensions.GetBlueprints<BlueprintSpellbook>(),
                        (feature) => feature.Blueprint,
                        (feature) => FactsEditor.getName(feature),
                        (feature) => $"{FactsEditor.getName(feature)} {feature.NameSafe()} {feature.GetDisplayName()} {feature.Comment}",
                        (feature) => FactsEditor.getName(feature),
                        () => {
                            using (HorizontalScope()) {
                                Toggle("Show GUIDs", ref Main.settings.showAssetIDs, 150.width());
                                20.space();
                                Toggle("Show Internal Names", ref settings.showDisplayAndInternalNames, 200.width());
                                20.space();
                                Toggle("Show Inspector", ref FactsEditor.showInspector, 150.width());
                                if (Toggle("Search Descriptions", ref settings.searchesDescriptions, 250.width())) {
                                    SpellbookBrowser.needsReloadData = true;
                                }
                            }
                        },
                        (feature, blueprint) => FactsEditor.RowGUI(feature, blueprint, ch, SpellbookBrowser, todo), (feature, blueprint) => {
                            ReflectionTreeView.DetailsOnGUI(blueprint);
                            return null;
                        }, 50, false, true, 100, 300, "", true);
        }
    }
}
