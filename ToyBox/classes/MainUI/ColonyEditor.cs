using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Colonization;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ToyBox.BlueprintExtensions;

namespace ToyBox.classes.MainUI {
    public static class ColonyEditor {
        public static Settings Settings => Main.Settings;
        public static Dictionary<Colony, Browser<BlueprintColonyTrait, BlueprintColonyTrait>> colonyTraitBrowser = new();
        public static List<BlueprintColonyTrait> ColonyTraits;
        public static Browser<BlueprintResource, BlueprintResource> resourceBrowser = new(true, true);
        public static List<BlueprintResource> ColonyResources;
        public static string selectedStat;
        public static int statIndex;
        public static List<string> selections = new List<string>() { "Contentment", "Efficiency", "Security" };
        public static int statAdjustment = 1;
        public static ColoniesState.ColonyData selectedColony;
        public static string searchText = "";
        public static int pageSize = 20;
        public static int currentPage = 1;
        public static bool editResources = false;
        public static int resourceAdjustment = 10;
        public static List<BlueprintColonyEventResult> eventResults;
        public static BlueprintColonyEventResult selectedResult;
        public static string resultSearchText;
        public static void OnGUI() {
            var colonies = Game.Instance.Player.ColoniesState.Colonies;
            using (HorizontalScope()) {
                using (HorizontalScope(Width(300))) {
                    PagedVPicker("Colonies".localize(), ref selectedColony, colonies, "None".localize(), t => t.Colony.Blueprint.Name, ref searchText, ref pageSize, ref currentPage, Width(300));
                }
                using (VerticalScope()) {
                    if (selectedColony != null) {
                        /* 
                        if (eventResults == null) {
                            eventResults = BlueprintLoader.Shared.GetBlueprints<BlueprintColonyEventResult>();
                        }
                        else {
                            GridPicker("Result".localize(), ref selectedResult, eventResults, "None".localize(), t => t.Name, ref resultSearchText);
                        }
                        */
                        var colony = selectedColony.Colony;
                        Label("Ongoing Events:".localize());
                        using (HorizontalScope()) {
                            25.space();
                            using (VerticalScope()) {
                                foreach (var evt in colony.StartedEvents) {
                                    Label(evt.Name, Width(500));
                                    // ActionButton("Finish".localize(), () => colony.FinishEvent(evt, selectedResult));
                                }
                            }
                        }
                        Label("Ongoing Projects:".localize());
                        using (HorizontalScope()) {
                            25.space();
                            using (VerticalScope()) {
                                /* Adding a multiplier seems possible enough. Colony.Tick() is responsible for completing projects with the following code
                                TimeSpan gameTime = Game.Instance.TimeController.GameTime;
                                foreach (ColonyProject colonyProject in this.Projects) {
                                    if (!colonyProject.IsFinished && this.SegmentsToBuildProject(colonyProject.Blueprint) <= (gameTime - colonyProject.StartTime).TotalSegments()) {
                                        this.FinishProject(colonyProject);
                                    }
                                } */
                                foreach (var proj in colony.Projects) {
                                    if (!proj.IsFinished) {
                                        using (HorizontalScope()) {
                                            Label(proj.Blueprint.Name, Width(500));
                                            ActionButton("Finish".localize(), () => colony.FinishProject(proj));
                                        }
                                    }
                                }
                            }
                        }
                        using (HorizontalScope()) {
                            Label("Contentment".localize() + $": {colony.Contentment.Value}");
                            Label("Efficiency".localize() + $": {colony.Efficiency.Value}");
                            Label("Security".localize() + $": {colony.Security.Value}");
                        }
                        using (HorizontalScope()) {
                            Label("Change Colony Stat".localize(), Width(300));
                            ActionSelectionGrid(ref statIndex, selections.ToArray(), 3, null, Width(500));
                            selectedStat = selections[statIndex];
                        }
                        if (selectedStat != null) {
                            using (HorizontalScope()) {
                                Label("Adjust ".localize() + selectedStat.localize() + " by the following amount:".localize());
                                IntTextField(ref statAdjustment, null, MinWidth(200), AutoWidth());
                                statAdjustment = Math.Max(0, statAdjustment);
                                10.space();
                                ActionButton("Add".localize(), () => CheatsColonization.AddColonyStat(colony.Blueprint, selectedStat.ToLower(), statAdjustment));
                                10.space();
                                ActionButton("Remove".localize(), () => CheatsColonization.AddColonyStat(colony.Blueprint, selectedStat.ToLower(), -statAdjustment));
                            }
                        }
                        if (!colonyTraitBrowser.ContainsKey(colony)) {
                            colonyTraitBrowser[colony] = new(Mod.ModKitSettings.searchAsYouType);
                        }
                        if (ColonyTraits == null && colonyTraitBrowser[colony].ShowAll) {
                            ColonyTraits = BlueprintLoader.Shared.GetBlueprints<BlueprintColonyTrait>();
                        }
                        var traitBrowser = colonyTraitBrowser[colony];
                        25.space();
                        traitBrowser.OnGUI(colony.ColonyTraits.Keys,
                            () => ColonyTraits, trait => trait,
                            trait => $"{GetSearchKey(trait)}" + (Settings.searchDescriptions ? $"{trait.Description}" : ""),
                            trait => new[] { GetSortKey(trait) },
                            () => {
                                using (HorizontalScope()) {
                                    var reloadData = false;
                                    Toggle("Show GUIDs".localize(), ref Main.Settings.showAssetIDs);
                                    20.space();
                                    reloadData |= Toggle("Show Internal Names".localize(), ref Settings.showDisplayAndInternalNames);
                                    20.space();
                                    reloadData |= Toggle("Search Descriptions".localize(), ref Settings.searchDescriptions);
                                    if (reloadData) {
                                        traitBrowser.ResetSearch();
                                    }
                                }
                            },
                            (trait, maybeTrait) => {
                                bool isAdded = colony.ColonyTraits.ContainsKey(trait);
                                var remainingWidth = ummWidth;
                                remainingWidth -= 50;
                                var titleWidth = (remainingWidth / (IsWide ? 3.5f : 4.0f)) - 100;
                                remainingWidth -= titleWidth;

                                var text = GetTitle(trait).MarkedSubstring(traitBrowser.SearchText);
                                var titleKey = $"{trait.AssetGuid}";
                                if (isAdded) {
                                    text = text.Cyan().Bold();
                                }
                                Label(text, Width((int)titleWidth));
                                Space(190);
                                remainingWidth -= 190;
                                if (isAdded) {
                                    ActionButton("Remove".localize(), () => colony.RemoveTrait(trait), Width(150));
                                }
                                else {
                                    ActionButton("Add".localize(), () => colony.AddTrait(trait), Width(150));
                                }
                                remainingWidth -= 178;
                                Space(20); remainingWidth -= 20;
                                ReflectionTreeView.DetailToggle("", trait, trait, 0);
                                using (VerticalScope(Width(remainingWidth - 100))) {
                                    try {
                                        if (Settings.showAssetIDs)
                                            ClipboardLabel(trait.AssetGuid.ToString(), AutoWidth());
                                        Label(trait.Description.StripHTML().MarkedSubstring(traitBrowser.SearchText).green(), Width(remainingWidth - 100));
                                    }
                                    catch (Exception e) {
                                        Mod.Warn($"Error in blueprint: {trait.AssetGuid}");
                                        Mod.Warn($"         name: {trait.name}");
                                        Mod.Error(e);
                                    }
                                }
                            },
                            (trait, maybeTrait) => {
                                ReflectionTreeView.OnDetailGUI(trait);
                            }, 0);
                    }
                }
            }
            DisclosureToggle("Edit Resources".localize(), ref editResources);
            if (editResources) {
                if (ColonyResources == null) {
                    if (Event.current.type == EventType.Layout) {
                        ColonyResources = BlueprintLoader.Shared.GetBlueprints<BlueprintResource>();
                        resourceBrowser.DisplayShowAllGUI = false;
                    }
                }
                if (ColonyResources != null) {
                    using (HorizontalScope()) {
                        Label("Adjust Resource Factor by the following amount:".localize());
                        IntTextField(ref resourceAdjustment, null, MinWidth(200), AutoWidth());
                    }
                    resourceBrowser.OnGUI(ColonyResources,
                        () => ColonyResources,
                        c => c,
                        cr => $"{GetSearchKey(cr)} {cr.Description}",
                        cr => new[] { GetSearchKey(cr) },
                        () => {
                            Label("Resource".localize(), Width((int)((ummWidth / (IsWide ? 3.5f : 4.0f)) - 150)));
                            Label("Current Amount".localize(), Width(200));
                        },
                        (cr, maybeCR) => {
                            var remainingWidth = ummWidth;
                            remainingWidth -= 50;
                            var titleWidth = (remainingWidth / (IsWide ? 3.5f : 4.0f)) - 100;
                            remainingWidth -= titleWidth;

                            var text = GetTitle(cr).MarkedSubstring(resourceBrowser.SearchText);
                            var titleKey = $"{cr.AssetGuid}";
                            if (cr) {
                                text = text.Cyan().Bold();
                            }
                            Label(text, Width((int)titleWidth));
                            int amount;
                            Game.Instance.Player.ColoniesState.ResourcesNotFromColonies.TryGetValue(cr, out amount);
                            Label(amount.ToString(), Width(100));
                            remainingWidth -= 100;
                            25.space();
                            ActionButton("Add".localize(), () => Game.Instance.ColonizationController.AddResourceNotFromColonyToPool(cr, resourceAdjustment), Width(100));
                            25.space();
                            ActionButton("Remove".localize(), () => Game.Instance.ColonizationController.UseResourceFromPool(cr, resourceAdjustment), Width(100));
                            remainingWidth -= 250;
                            ReflectionTreeView.DetailToggle("", cr, cr, 0);
                            using (VerticalScope(Width(remainingWidth - 100))) {
                                try {
                                    if (Settings.showAssetIDs)
                                        ClipboardLabel(cr.AssetGuid.ToString(), AutoWidth());
                                    Label(cr.Description.StripHTML().MarkedSubstring(resourceBrowser.SearchText).green(), Width(remainingWidth - 100));
                                }
                                catch (Exception e) {
                                    Mod.Warn($"Error in blueprint: {cr.AssetGuid}");
                                    Mod.Warn($"         name: {cr.name}");
                                    Mod.Error(e);
                                }
                            }
                        },
                        (cr, maybeCR) => {
                            ReflectionTreeView.OnDetailGUI(cr);
                        }, 0);
                }
            }
        }
    }
}
