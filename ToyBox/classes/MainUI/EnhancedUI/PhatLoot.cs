using System;
using System.Linq;
using UnityEngine;
using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using static ModKit.UI;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using ModKit.Utility;
#if Wrath
using ToyBox.Multiclass;
#endif

namespace ToyBox {
    public class PhatLoot {
        public static Settings Settings => Main.Settings;
        public static string searchText = "";

        //
        private const string MassLootBox = "Open Mass Loot Window";
        private const string OpenPlayerChest = "Open Player Chest";
        public static void ResetGUI() { }

        public static void OnLoad() {
            KeyBindings.RegisterAction(MassLootBox, LootHelper.OpenMassLoot);
            KeyBindings.RegisterAction(OpenPlayerChest, LootHelper.OpenPlayerChest);
        }

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
                    BindableActionButton(OpenPlayerChest, Width(400));
                    Space(95 - 150);
                    Label("Lets you open up your player storage chest that you find near your bed at the Inn and other places".green());
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
#if DEBUG
                () => Toggle("Show reasons you can not equip an item in tooltips", ref Settings.toggleShowCantEquipReasons),
#endif
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
                           Toggle("Show Rarity Tags", ref Settings.toggleShowRarityTags, 300.width());
                           Toggle("Color Item Names", ref Settings.toggleColorLootByRarity, 300.width());
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
                        using (VerticalScope(300)) {
                            using (HorizontalScope(300)) {
                                using (VerticalScope()) {
                                    Label("Maximum Rarity To Hide:".cyan(), AutoWidth());
                                    RarityGrid(ref Settings.maxRarityToHide, 4, AutoWidth());
                                }
                            }
                        }
                        50.space();
                        using (VerticalScope()) {
                            Label("");
                            HelpLabel($"This hides map pins of loot containers containing at most the selected rarity. {"Note: Changing settings requires reopening the map.".orange()}");
                        }
                    },
                    // The following options let you configure loot filtering and auto sell levels:".green());
                    () => { }
                    );
            Div(0, 25);
            HStack("Bulk Sell", 1,
                   () => {
                       Toggle("Enable custom bulk selling settings", ref Settings.toggleCustomBulkSell, 400.width());
                   },
                   () => {
                       if (!Settings.toggleCustomBulkSell) return;
                       using (VerticalScope()) {
                           BulkSell.OnGUI();
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
                            var count = presents
                                        .Where(p => 
                                                   p.Unit == null 
#if Wrath                                                   
                                                   || (Settings.toggleLootChecklistFilterFriendlies 
                                                       && !p.Unit.IsPlayersEnemy 
                                                       || p.Unit.IsPlayersEnemy
                                                       ) 
                                                   || (!Settings.toggleLootChecklistFilterFriendlies 
                                                       && p.Unit.IsPlayersEnemy
                                                       )
#endif
                                                   ).Count(p => p.GetLewtz(searchText).Lootable(rarity).Count() > 0);
                            Label($"{group.Key.cyan()}: {count}");
                            Div(indent);
                            foreach (var present in presents) {
                                var phatLewtz = present.GetLewtz(searchText).Lootable(rarity).OrderByDescending(l => l.Rarity()).ToList();
                                var unit = present.Unit;
                                if (phatLewtz.Any()
                                    && (unit == null
#if Wrath
                                        || (Settings.toggleLootChecklistFilterFriendlies 
                                            && !unit.IsPlayersEnemy 
                                            || unit.IsPlayersEnemy
                                            )
                                        || (!Settings.toggleLootChecklistFilterFriendlies 
                                            && unit.IsPlayersEnemy
                                            )
#endif
                                        )
                                    ) {
                                    isEmpty = false;
                                    Div();
                                    using (HorizontalScope()) {
                                        Space(indent);
                                        Label($"{present.GetName()}".orange().bold(), Width(325));
                                        if (present.InteractionLoot != null) {
#if Wrath
                                            if (present.InteractionLoot?.Owner?.PerceptionCheckDC > 0)
                                                Label($" Perception DC: {present.InteractionLoot?.Owner?.PerceptionCheckDC}".green().bold(), Width(125));
                                            else
                                                Label($" Perception DC: NA".orange().bold(), Width(125));
                                            int? trickDc = present.InteractionLoot?.Owner?.Get<DisableDeviceRestrictionPart>()?.DC;
                                            if (trickDc > 0)
                                                Label($" Trickery DC: {trickDc}".green().bold(), Width(125));
                                            else
                                                Label($" Trickery DC: NA".orange().bold(), Width(125));
#endif
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
