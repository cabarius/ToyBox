using System;
using System.Linq;
using UnityEngine;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using ToyBox.Multiclass;
using static ModKit.UI;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;

namespace ToyBox {
    public class PhatLoot {
        public static Settings settings => Main.settings;
        public static string searchText = "";

        //
        private const string MassLootBox = "Open Area Exit Loot Window";

        public static void ResetGUI() { }

        public static void OnLoad() {
            KeyBindings.RegisterAction(MassLootBox, () => LootHelper.OpenMassLoot());
        }

        public static void OnGUI() {
#if DEBUG
            UI.Div(0, 25);
            var inventory = Game.Instance.Player.Inventory;
            var items = inventory.ToList();
            UI.HStack("Inventory", 1,
                () => {
                    UI.ActionButton("Export", () => items.Export("inventory.json"), UI.Width(150));
                    UI.Space(25);
                    UI.ActionButton("Import", () => inventory.Import("inventory.json"), UI.Width(150));
                    UI.Space(25);
                    UI.ActionButton("Replace", () => inventory.Import("inventory.json", true), UI.Width(150));
                },
                () => { }
            );
#endif
            UI.Div(0, 25);
            UI.HStack("Loot", 1,
                () => {
                    UI.BindableActionButton(MassLootBox, UI.Width(200));
                    UI.Space(5); UI.Label("Area exit loot screen useful with the mod Cleaner to clear junk loot mid dungeon leaving less clutter on the map".green());
                },
                () => {
                    UI.ActionButton("Reveal Ground Loot", () => LootHelper.ShowAllChestsOnMap(), UI.Width(200));
                    UI.Space(210); UI.Label("Shows all chests/bags/etc on the map excluding hidden".green());
                },
                () => {
                    UI.ActionButton("Reveal Hidden Ground Loot", () => LootHelper.ShowAllChestsOnMap(true), UI.Width(200));
                    UI.Space(210); UI.Label("Shows all chests/bags/etc on the map including hidden".green());
                },
                () => {
                    UI.ActionButton("Reveal Inevitable Loot", () => LootHelper.ShowAllInevitablePortalLoot(), UI.Width(200));
                    UI.Space(210); UI.Label("Shows unlocked Inevitable Excess DLC rewards on the map".green());
                },
                () => {
                    UI.Toggle("Mass Loot Shows Everything When Leaving Map", ref settings.toggleMassLootEverything);
                    UI.Space(100); UI.Label("Some items might be invisible until looted".green());
                },
                () => {
                    UI.Toggle("Color Items By Rarity", ref settings.toggleColorLootByRarity);
                    UI.Space(25);
                    using (UI.VerticalScope()) {
                        UI.Label($"This makes loot function like Diablo or Borderlands. {"Note: turning this off requires you to save and reload for it to take effect.".orange()}".green());
                        UI.Label("The coloring of rarity goes as follows:".green());
                        UI.HStack("Rarity".orange(), 1,
                            () => {
                                UI.Label("Trash".Rarity(RarityType.Trash).bold(), UI.rarityStyle, UI.Width(200));
                                UI.Space(5); UI.Label("Common".Rarity(RarityType.Common).bold(), UI.rarityStyle, UI.Width(200));
                                UI.Space(5); UI.Label("Uncommon".Rarity(RarityType.Uncommon).bold(), UI.rarityStyle, UI.Width(200));
                            },
                            () => {
                                UI.Space(3); UI.Label("Rare".Rarity(RarityType.Rare).bold(), UI.rarityStyle, UI.Width(200));
                                UI.Space(5); UI.Label("Epic".Rarity(RarityType.Epic).bold(), UI.rarityStyle, UI.Width(200));
                                UI.Space(5); UI.Label("Legendary".Rarity(RarityType.Legendary).bold(), UI.rarityStyle, UI.Width(200));
                            },
                            () => {
                                UI.Space(5); UI.Label("Mythic".Rarity(RarityType.Mythic).bold(), UI.rarityStyle, UI.Width(200));
                                UI.Space(5); UI.Label("Godly".Rarity(RarityType.Godly).bold(), UI.rarityStyle, UI.Width(200));
                                UI.Space(5); UI.Label("Notable".Rarity(RarityType.Notable).bold() + "*".orange().bold(), UI.rarityStyle, UI.Width(200));
                            },
                            () => {
                                UI.Space(3); UI.Label("*".orange().bold() + " Notable".Rarity(RarityType.Notable) + " denotes items that are deemed to be significant for plot reasons or have significant subtle properties".green(), UI.Width(615));
                            },
                            () => { }
                        );
                    }

                    // The following options let you configure loot filtering and auto sell levels:".green());
                },
#if false
                () => UI.RarityGrid("Hide Level ", ref settings.lootFilterIgnore, 0, UI.AutoWidth()),
                () => UI.RarityGrid("Auto Sell Level ", ref settings.lootFilterAutoSell, 0, UI.AutoWidth()),
#endif
                () => { }
            );
            UI.Div(0, 25);
            var isEmpty = true;
            UI.HStack("Loot Checklist", 1,
                () => {
                    var areaName = "";
                    if (Main.IsInGame) {
                        areaName = Game.Instance.CurrentlyLoadedArea.AreaDisplayName;
                        var areaPrivateName = Game.Instance.CurrentlyLoadedArea.name;
                        if (areaPrivateName != areaName) areaName += $"\n({areaPrivateName})".yellow();
                    }
                    UI.Label(areaName.orange().bold(), UI.Width(300));
                    UI.Label("Rarity: ".cyan(), UI.AutoWidth());
                    UI.RarityGrid(ref settings.lootChecklistFilterRarity, 4, UI.AutoWidth());
                },
                () => {
                    UI.ActionTextField(
                    ref searchText,
                    "itemSearchText",
                    (text) => { },
                    () => { },
                    UI.Width(300));
                    UI.Space(25); UI.Toggle("Show Friendly", ref settings.toggleLootChecklistFilterFriendlies);
                    UI.Space(25); UI.Toggle("Blueprint", ref settings.toggleLootChecklistFilterBlueprint, UI.AutoWidth());
                    UI.Space(25); UI.Toggle("Description", ref settings.toggleLootChecklistFilterDescription, UI.AutoWidth());
                },
                () => {
                    if (!Main.IsInGame) { UI.Label("Not available in the Main Menu".orange()); return; }
                    var presentGroups = LootHelper.GetMassLootFromCurrentArea().GroupBy(p => p.InteractionLoot != null ? "Containers" : "Units");
                    var indent = 3;
                    using (UI.VerticalScope()) {
                        foreach (var group in presentGroups.Reverse()) {
                            var presents = group.AsEnumerable().OrderByDescending(p => {
                                var loot = p.GetLewtz(searchText);
                                if (loot.Count == 0) return 0;
                                else return (int)loot.Max(l => l.Rarity());
                            });
                            var rarity = settings.lootChecklistFilterRarity;
                            var count = presents.Where(p => p.Unit == null || (settings.toggleLootChecklistFilterFriendlies && !p.Unit.IsPlayersEnemy || p.Unit.IsPlayersEnemy) || (!settings.toggleLootChecklistFilterFriendlies && p.Unit.IsPlayersEnemy)).Count(p => p.GetLewtz(searchText).Lootable(rarity).Count() > 0);
                            UI.Label($"{group.Key.cyan()}: {count}");
                            UI.Div(indent);
                            foreach (var present in presents) {
                                var pahtLewts = present.GetLewtz(searchText).Lootable(rarity).OrderByDescending(l => l.Rarity());
                                var unit = present.Unit;
                                if (pahtLewts.Count() > 0 && (unit == null || (settings.toggleLootChecklistFilterFriendlies && !unit.IsPlayersEnemy || unit.IsPlayersEnemy) || (!settings.toggleLootChecklistFilterFriendlies && unit.IsPlayersEnemy))) { 
                                    isEmpty = false;
                                    UI.Div();
                                    using (UI.HorizontalScope()) {
                                        UI.Space(indent);
                                        UI.Label($"{present.GetName()}".orange().bold(), UI.Width(325));
                                        if (present.InteractionLoot != null) {
                                            if (present.InteractionLoot?.Owner?.PerceptionCheckDC > 0)
                                                UI.Label($" Perception DC: {present.InteractionLoot?.Owner?.PerceptionCheckDC}".green().bold(), UI.Width(125));
                                            else
                                                UI.Label($" Perception DC: NA".orange().bold(), UI.Width(125));
                                            int? trickDc = present.InteractionLoot?.Owner?.Get<DisableDeviceRestrictionPart>()?.DC;
                                            if (trickDc > 0)
                                                UI.Label($" Trickery DC: {trickDc}".green().bold(), UI.Width(125));
                                            else
                                                UI.Label($" Trickery DC: NA".orange().bold(), UI.Width(125));
                                        }
                                        UI.Space(25);
                                        using (UI.VerticalScope()) {
                                            foreach (var lewt in pahtLewts) {
                                                var description = lewt.Blueprint.Description;
                                                var showBP = settings.toggleLootChecklistFilterBlueprint;
                                                var showDesc = settings.toggleLootChecklistFilterDescription && description != null && description.Length > 0;
                                                using (UI.HorizontalScope()) {
                                                    //Main.Log($"rarity: {lewt.Blueprint.Rarity()} - color: {lewt.Blueprint.Rarity().color()}");
                                                    UI.Label(lewt.Name.Rarity(lewt.Blueprint.Rarity()), showDesc || showBP ? UI.Width(350) : UI.AutoWidth());
                                                    if (showBP) {
                                                        UI.Space(100); UI.Label(lewt.Blueprint.GetDisplayName().grey(), showDesc ? UI.Width(350) : UI.AutoWidth());
                                                    }
                                                    if (showDesc) {
                                                        UI.Space(100); UI.Label(description.StripHTML().green());
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    UI.Space(25);
                                }
                            }
                            UI.Space(25);
                        }
                    }
                },
                () => {
                    if (isEmpty)
                        using (UI.HorizontalScope()) {
                            UI.Label("No Loot Available".orange(), UI.AutoWidth());
                        }
                }
            );
        }
    }
}
