using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using ToyBox.Multiclass;

namespace ToyBox {
    public class PhatLoot{
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
                () => UI.EnumGrid("Hide Level ", ref settings.lootFilterIgnore, 0, UI.AutoWidth()),
                () => UI.EnumGrid("Auto Sell Level ", ref settings.lootFilterAutoSell, 0, UI.AutoWidth()),
#endif
                () => { }
            );
            UI.Div(0, 25);
            UI.HStack("Loot Checklist", 1,
                () => {
                    var presents = LootHelper.GetMassLootFromCurrentArea();
                    using (UI.VerticalScope()) {
                        foreach (var present in presents) {
                            using (UI.HorizontalScope()) {
                                UI.Space(150);
                                UI.Label(present.GetName(), UI.Width(300));
                                using (UI.VerticalScope()) {
                                    foreach (var lewt in present.GetLewtz()) {
                                        using (UI.HorizontalScope()) {
                                            UI.Label(lewt.Name.color(lewt.Blueprint.Rarity().Color()));
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                () => { }
            );
        }
    }
}
