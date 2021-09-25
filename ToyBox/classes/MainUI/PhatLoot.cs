using System;
using System.Linq;
using UnityEngine;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using ToyBox.Multiclass;

namespace ToyBox {
    public class PhatLoot {
        public static Settings settings { get { return Main.settings; } }
        public static void ResetGUI() { }
        public static void OnGUI() {
            UI.Div(0, 25);
            UI.HStack("Loot", 1,
                () => {
                    UI.Toggle("Color Items By Rarity", ref settings.toggleColorLootByRarity, 0);
                    UI.Space(25);
                    using (UI.VerticalScope()) {
                        UI.Label($"This makes loot function like Diablo or Borderlands. {"Note: turning this off requires you to save and reload for it to take effect.".orange()}".green());
                        UI.Label("The coloring of rarity goes as follows:".green());
                        UI.HStack("Rarity".orange(), 1,
                            () => UI.Label("Trash".Rarity(RarityType.Trash).bold()),
                            () => UI.Label("Common".bold()),
                            () => UI.Label("Uncommon".Rarity(RarityType.Uncommon).bold()),
                            () => UI.Label("Rare".Rarity(RarityType.Rare).bold()),
                            () => UI.Label("Epic".Rarity(RarityType.Epic).bold()),
                            () => UI.Label("Legendary".Rarity(RarityType.Legendary).bold()),
                            () => UI.Label("Mythic".Rarity(RarityType.Mythic).bold()),
                            () => UI.Label("Godly".Rarity(RarityType.Godly)),
                            () => UI.Label("Notable".Rarity(RarityType.Notable).bold()),
                            () => { }
                        );
                    }

                    // The following options let you configure loot filtering and auto sell levels:".green());
                },
#if DEBUG
                () => UI.RarityGrid("Hide Level ", ref settings.lootFilterIgnore, 0, UI.AutoWidth()),
                () => UI.RarityGrid("Auto Sell Level ", ref settings.lootFilterAutoSell, 0, UI.AutoWidth()),
#endif
                () => {
                    UI.Toggle("Mass Loot Shows Everything When Leaving Map", ref settings.toggleMassLootEverything);
                    UI.Space(100); UI.Label("Some items might be invisible until looted".green());
                },
                () => { }
            );
            UI.Div(0, 25);
            UI.HStack("Loot Checklist", 1,
                () => {
                    UI.Space(25); UI.Toggle("Show Friendly", ref settings.toggleLootChecklistFilterFriendlies);
                    UI.Space(25); UI.Toggle("Blueprint", ref settings.toggleLootChecklistFilterBlueprint);
                    UI.Space(25); UI.Toggle("Description", ref settings.toggleLootChecklistFilterDescription);
                    UI.Space(25); UI.Label("Rarity: ".cyan(), UI.AutoWidth());
                    UI.RarityGrid(ref settings.lootChecklistFilterRarity, 0, UI.AutoWidth());
                },
                () => {
                    if (!Main.IsInGame) { UI.Label("Not available in the Main Menu".orange()); return; }
                    var presentGroups = LootHelper.GetMassLootFromCurrentArea().GroupBy(p => p.InteractionLoot != null ? "Containers" : "Units");
                    var isEmpty = true;
                    var indent = 3;
                    using (UI.VerticalScope()) {
                        foreach (var group in presentGroups) {
                            UI.Label(group.Key.cyan());
                            UI.Div(indent);
                            foreach (var present in group.AsEnumerable()) {
                                var pahtLewts = present.GetLewtz().Lootable(settings.lootChecklistFilterRarity);
                                var unit = present.Unit;
                                if (pahtLewts.Count > 0
                                    //&& (unit == null
                                    //    || settings.toggleLootChecklistFilterFriendlies && !unit.IsPlayersEnemy
                                    //    )
                                    ) {
                                    isEmpty = false;
                                    using (UI.HorizontalScope()) {
                                        UI.Space(indent);
                                        UI.Label(present.GetName().orange().bold(), UI.Width(300));
                                        using (UI.VerticalScope()) {
                                            foreach (var lewt in pahtLewts) {
                                                var description = lewt.Blueprint.Description;
                                                bool shouldShowDescription = settings.toggleLootChecklistFilterDescription && description != null && description.Length > 0;
                                                using (UI.HorizontalScope()) {
                                                    Main.Log($"rarity: {lewt.Blueprint.Rarity()} - color: {lewt.Blueprint.Rarity().color()}");
                                                    UI.Label(lewt.Name.Rarity(lewt.Blueprint.Rarity()), UI.Width(300));
                                                    if (settings.toggleLootChecklistFilterBlueprint) {
                                                        UI.Space(100); UI.Label(lewt.Blueprint.GetDisplayName(), UI.Width(300));
                                                    }
                                                    if (shouldShowDescription) {
                                                        UI.Space(100); UI.Label(description.RemoveHtmlTags().green());
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //UI.Div(indent);
                                }
                            }
                            UI.Space(25);
                        }
                    }
                    if (isEmpty)
                        using (UI.HorizontalScope()) {
                            UI.Space(indent);
                            UI.Label("No Loot Available".orange());
                        }
                },
                () => { }
            );
        }
}
}
