﻿using System;
using System.Linq;
using UnityEngine;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using ToyBox.Multiclass;

namespace ToyBox {
    public class PhatLoot {
        public static Settings settings => Main.settings;
        public static void ResetGUI() { }
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
                    UI.Toggle("Mass Loot Shows Everything When Leaving Map", ref settings.toggleMassLootEverything);
                    UI.Space(100); UI.Label("Some items might be invisible until looted".green());
                },
                () => {
                    UI.Toggle("Color Items By Rarity", ref settings.toggleColorLootByRarity, 0);
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
                    }
                    UI.Label(areaName.orange().bold(), UI.Width(300));
                    UI.Label("Rarity: ".cyan(), UI.AutoWidth());
                    UI.RarityGrid(ref settings.lootChecklistFilterRarity, 4, UI.AutoWidth());
                },
                () => {
                    //UI.Space(390); UI.Toggle("Show Friendly", ref settings.toggleLootChecklistFilterFriendlies);
                    UI.Space(390); UI.Toggle("Blueprint", ref settings.toggleLootChecklistFilterBlueprint);
                    UI.Space(25); UI.Toggle("Description", ref settings.toggleLootChecklistFilterDescription);
                },
                () => {
                    if (!Main.IsInGame) { UI.Label("Not available in the Main Menu".orange()); return; }
                    var presentGroups = LootHelper.GetMassLootFromCurrentArea().GroupBy(p => p.InteractionLoot != null ? "Containers" : "Units");
                    var indent = 3;
                    using (UI.VerticalScope()) {
                        foreach (var group in presentGroups.Reverse()) {
                            var presents = group.AsEnumerable().OrderByDescending(p => {
                                var loot = p.GetLewtz();
                                if (loot.Count == 0) return 0;
                                else return (int)loot.Max(l => l.Rarity());
                            });
                            var rarity = settings.lootChecklistFilterRarity;
                            var count = presents.Count(p => p.GetLewtz().Lootable(rarity).Count() > 0);
                            UI.Label($"{group.Key.cyan()}: {count}");
                            UI.Div(indent);
                            foreach (var present in presents) {
                                var pahtLewts = present.GetLewtz().Lootable(rarity);
                                var unit = present.Unit;
                                if (pahtLewts.Count > 0
                                    //&& (unit == null
                                    //    || settings.toggleLootChecklistFilterFriendlies && !unit.IsPlayersEnemy
                                    //    )
                                    ) {
                                    isEmpty = false;
                                    UI.Div();
                                    using (UI.HorizontalScope()) {
                                        UI.Space(indent);
                                        UI.Label(present.GetName().orange().bold(), UI.Width(300));
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
