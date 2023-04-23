using System;
using System.Linq;
using UnityEngine;
using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using ToyBox.Multiclass;
using static ModKit.UI;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using ModKit.Utility;

namespace ToyBox {
    public class PhatLoot {
        public static Settings Settings => Main.Settings;
        public static string searchText = "";

        //
        private const string MassLootBox = "Open Mass Loot Window";

        public static void ResetGUI() { }

        public static void OnLoad() => KeyBindings.RegisterAction(MassLootBox, LootHelper.OpenMassLoot);

        public static void OnGUI() {
            if (Game.Instance?.Player?.Inventory == null) return;
#if false
            Div(0, 25);
            var inventory = Game.Instance.Player.Inventory;
            var items = inventory.ToList();
            HStack("Inventory", 1,
                () => {
                    ActionButton("Export", () => items.Export("inventory.json"), Width(150));
                    Space(25);
                    ActionButton("Import", () => inventory.Import("inventory.json"), Width(150));
                    Space(25);
                    ActionButton("Replace", () => inventory.Import("inventory.json", true), Width(150));
                },
                () => { }
            );
#endif
            Div(0, 25);
            HStack("Loot", 1,
                () => {
                    BindableActionButton(MassLootBox, Width(400));
                    Space(95 - 150);
                    Label("Lets you open up the area's mass loot screen to grab goodies whenever you want. Normally shown only when you exit the area".green());
                },
                () => {
                    ActionButton("Reveal Ground Loot", () => LootHelper.ShowAllChestsOnMap(), Width(400));
                    Space(150);
                    Label("Shows all chests/bags/etc on the map excluding hidden".green());
                },
                () => {
                    ActionButton("Reveal Hidden Ground Loot", () => LootHelper.ShowAllChestsOnMap(true), Width(400));
                    Space(150);
                    Label("Shows all chests/bags/etc on the map including hidden".green());
                },
                () => {
                    ActionButton("Reveal Inevitable Loot", LootHelper.ShowAllInevitablePortalLoot, Width(400));
                    Space(150);
                    Label("Shows unlocked Inevitable Excess DLC rewards on the map".green());
                },
                () => { }
            );
            Div(0, 25);
            HStack(("Mass Loot"), 1,
                   () => {
                       Toggle("Show Everything When Leaving Map", ref Settings.toggleMassLootEverything, 400.width());
                       150.space();
                       Label("Some items might be invisible until looted".green());
                   },
                   () => {
                       Toggle("Steal from living NPCs", ref Settings.toggleLootAliveUnits, 400.width());
                       150.space();
                       Label("Allow Mass Loot to steal from living NPCs".green());
                   },
                   () => {
                       Toggle("Allow Looting Of Locked Items", ref Settings.toggleOverrideLockedItems, 400.width());
                       150.space();
                       Label("This allows you to loot items that are locked such as items carried by certain NPCs and items locked on your characters"
                                 .green()
                             + "\nWARNING: ".yellow().bold()
                             + "This may affect story progression (e.g. your purple knife)".yellow());
                   },
                   () => { }
                  );
            Div(0, 25);
            HStack("Loot Rarity Coloring", 1,
                   () => {
                       using (VerticalScope(300.width())) {
                           Toggle("Show Rarity Tags", ref Settings.toggleShowRarityTags);
                           Toggle("Color Item Names", ref Settings.toggleColorLootByRarity);
#if false
                           using (HorizontalScope()) {
                               30.space();
                               if (Settings.toggleEnhanceItemSortingWithRarity) {
                                   Toggle("Group By Rarity First", ref Settings.toggleSortByRarirtyFirst, 320.width());
                               }
                               else
                                   Label("", 320.width());
                           }
#endif
                       }
                       using (VerticalScope()) {
                           Label($"This makes loot function like Diablo or Borderlands. {"Note: turning this off requires you to save and reload for it to take effect.".orange()}"
                                     .green());
                       }
                   },
                   () => {
                       using (VerticalScope(400.width())) {
                           Label("Minimum Rarity For Loot Rarity Tags/Colors".cyan(), AutoWidth());
                           RarityGrid(ref Settings.minRarityToColor, 4, AutoWidth());
                       }
                   });
            Div(0, 25);
            HStack("Loot Rarity Filtering", 1,
                    () => {
                        using (VerticalScope()) {
                            Label("Warning: ".orange().bold() + "The following is experimental and might behave unexpectedly.".green());
                            using (HorizontalScope()) {
                                using (VerticalScope()) {
                                    Label($"This hides map pins of loot containers containing at most the selected rarity. {"Note: Changing settings requires reopening the map.".orange()}".green());
                                    Label("Maximum Rarity To Hide:".cyan(), AutoWidth());
                                    RarityGrid(ref Settings.maxRarityToHide, 4, AutoWidth());
                                }
                            }
                        }
                    },
                    // The following options let you configure loot filtering and auto sell levels:".green());
                    () => { }
                    );
            Div(0, 25);
            HStack("Enhanced Inventory",
                   1,
                   () => {
                       if (Toggle("Enable Enhanced Inventory", ref Settings.toggleEnhancedInventory, 300.width()))
                           EnhancedInventory.RefreshRemappers();
                       25.space();
                       Label("Selected features revived from Xenofell's excellent mod".green());
                   },
                   () => {
                       if (Settings.toggleEnhancedInventory) {
                           using (VerticalScope()) {
                               Rect divRect;
                               using (HorizontalScope()) {
                                   Label("Enabled Sort Categories".Cyan(), 300.width());
                                   25.space();
                                   HelpLabel("Here you can choose which Sort Options appear in the popup menu");
                                   divRect = DivLastRect();
                               }
                               var hscopeRect = DivLastRect();
                               Div(hscopeRect.x, 0, divRect.x + divRect.width - hscopeRect.x);
                               ItemSortCategories new_options = ItemSortCategories.NotSorted;
                               var selectableCategories = EnumHelper.ValidSorterCategories.Where(i => i != ItemSortCategories.NotSorted).ToList();
                               var changed = false;
                               Table(selectableCategories,
                                     (flag) => {
                                         //Mod.Log($"            {flag.ToString()}");
                                         if (flag == ItemSortCategories.NotSorted || flag == ItemSortCategories.Default)
                                             return;
                                         bool isSet = Settings.InventoryItemSorterOptions.HasFlag(flag);
                                         using (HorizontalScope(250)) {
                                             30.space();
                                             if (Toggle($" {EnhancedInventory.SorterCategoryMap[flag].Item2 ?? flag.ToString()}", ref isSet)) changed = true;
                                         }
                                         if (isSet) {
                                             new_options |= flag;
                                         }
                                     },
                                     2,
                                     null,
                                     375.width());
                               65.space(() => ActionButton("Use Default", () => new_options = ItemSortCategories.Default));
                               Settings.InventoryItemSorterOptions = new_options;
                               if (changed) EnhancedInventory.RefreshRemappers();
                           }
                       }
                   },
                   () => {
                       using (VerticalScope()) {
                           Rect divRect;
                           using (HorizontalScope()) {
                               Label("Enabled Search Filters".Cyan(), 300.width());
                               25.space();
                               HelpLabel("Here you can choose which Search filters appear in the popup menu");
                               divRect = DivLastRect();
                           }
                           var hscopeRect = DivLastRect();
                           Div(hscopeRect.x, 0, divRect.x + divRect.width - hscopeRect.x);
                           FilterCategories new_options = default;
                           var selectableFilters = EnumHelper.ValidFilterCategories.Where(i => i != FilterCategories.NoFilter).ToList();
                           var changed = false;
                           Table(selectableFilters,
                                 (flag) => {
                                     //Mod.Log($"            {flag.ToString()}");
                                     bool isSet = Settings.SearchFilterCategories.HasFlag(flag);
                                     using (HorizontalScope(250)) {
                                         30.space();
                                         if (Toggle($" {EnhancedInventory.FilterCategoryMap[flag].Item2 ?? flag.ToString()}", ref isSet)) changed = true;
                                     }
                                     if (isSet) {
                                         new_options |= flag;
                                     }
                                 },
                                 2,
                                 null,
                                 375.width());
                           65.space(() => ActionButton("Use Default", () => new_options = FilterCategories.Default));
                           Settings.SearchFilterCategories = new_options;
                           if (changed) EnhancedInventory.RefreshRemappers();
                       }
                   });
            Div(0, 25);
            if (Game.Instance.CurrentlyLoadedArea == null) return;
            var isEmpty = true;
            HStack("Loot Checklist", 1,
                () => {
                    var areaName = "";
                    if (Main.IsInGame) {
                        try {
                            areaName = Game.Instance.CurrentlyLoadedArea.AreaDisplayName;
                        }
                        catch { }
                        var areaPrivateName = Game.Instance.CurrentlyLoadedArea.name;
                        if (areaPrivateName != areaName) areaName += $"\n({areaPrivateName})".yellow();
                    }
                    Label(areaName.orange().bold(), Width(300));
                    Label("Rarity: ".cyan(), AutoWidth());
                    RarityGrid(ref Settings.lootChecklistFilterRarity, 4, AutoWidth());
                },
                () => {
                    ActionTextField(
                    ref searchText,
                    "itemSearchText",
                    (text) => { },
                    () => { },
                    Width(300));
                    Space(25); Toggle("Show Friendly", ref Settings.toggleLootChecklistFilterFriendlies);
                    Space(25); Toggle("Blueprint", ref Settings.toggleLootChecklistFilterBlueprint, AutoWidth());
                    Space(25); Toggle("Description", ref Settings.toggleLootChecklistFilterDescription, AutoWidth());
                },
                () => {
                    if (!Main.IsInGame) { Label("Not available in the Main Menu".orange()); return; }
                    var presentGroups = LootHelper.GetMassLootFromCurrentArea().GroupBy(p => p.InteractionLoot != null ? "Containers" : "Units");
                    var indent = 3;
                    using (VerticalScope()) {
                        foreach (var group in presentGroups.Reverse()) {
                            var presents = group.AsEnumerable().OrderByDescending(p => {
                                var loot = p.GetLewtz(searchText);
                                if (loot.Count == 0) return 0;
                                else return (int)loot.Max(l => l.Rarity());
                            }).ToList();
                            var rarity = Settings.lootChecklistFilterRarity;
                            var count = presents.Where(p => p.Unit == null || (Settings.toggleLootChecklistFilterFriendlies && !p.Unit.IsPlayersEnemy || p.Unit.IsPlayersEnemy) || (!Settings.toggleLootChecklistFilterFriendlies && p.Unit.IsPlayersEnemy)).Count(p => p.GetLewtz(searchText).Lootable(rarity).Count() > 0);
                            Label($"{group.Key.cyan()}: {count}");
                            Div(indent);
                            foreach (var present in presents) {
                                var phatLewtz = present.GetLewtz(searchText).Lootable(rarity).OrderByDescending(l => l.Rarity()).ToList();
                                var unit = present.Unit;
                                if (phatLewtz.Any() 
                                    && (unit == null 
                                        || (Settings.toggleLootChecklistFilterFriendlies && !unit.IsPlayersEnemy || unit.IsPlayersEnemy)
                                        || (!Settings.toggleLootChecklistFilterFriendlies && unit.IsPlayersEnemy)
                                        )
                                    ) {
                                    isEmpty = false;
                                    Div();
                                    using (HorizontalScope()) {
                                        Space(indent);
                                        Label($"{present.GetName()}".orange().bold(), Width(325));
                                        if (present.InteractionLoot != null) {
                                            if (present.InteractionLoot?.Owner?.PerceptionCheckDC > 0)
                                                Label($" Perception DC: {present.InteractionLoot?.Owner?.PerceptionCheckDC}".green().bold(), Width(125));
                                            else
                                                Label($" Perception DC: NA".orange().bold(), Width(125));
                                            int? trickDc = present.InteractionLoot?.Owner?.Get<DisableDeviceRestrictionPart>()?.DC;
                                            if (trickDc > 0)
                                                Label($" Trickery DC: {trickDc}".green().bold(), Width(125));
                                            else
                                                Label($" Trickery DC: NA".orange().bold(), Width(125));
                                        }
                                        Space(25);
                                        using (VerticalScope()) {
                                            foreach (var lewt in phatLewtz) {
                                                var description = lewt.Blueprint.Description;
                                                var showBP = Settings.toggleLootChecklistFilterBlueprint;
                                                var showDesc = Settings.toggleLootChecklistFilterDescription && description != null && description.Length > 0;
                                                using (HorizontalScope()) {
                                                    //Main.Log($"rarity: {lewt.Blueprint.Rarity()} - color: {lewt.Blueprint.Rarity().color()}");
                                                    Label(lewt.Name.StripHTML().Rarity(lewt.Blueprint.Rarity()), showDesc || showBP ? Width(350) : AutoWidth());
                                                    if (showBP) {
                                                        Space(100); Label(lewt.Blueprint.GetDisplayName().grey(), showDesc ? Width(350) : AutoWidth());
                                                    }
                                                    if (!showDesc) continue;
                                                    Space(100); Label(description.StripHTML().green());
                                                }
                                            }
                                        }
                                    }
                                    Space(25);
                                }
                            }
                            Space(25);
                        }
                    }
                },
                () => {
                    if (!isEmpty) return;
                    using (HorizontalScope()) {
                        Label("No Loot Available".orange(), AutoWidth());
                    }
                }
            );
        }
    }
}
