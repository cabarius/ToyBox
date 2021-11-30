
using Kingmaker.Kingdom;
using ModKit;
using static ModKit.UI;
using ModKit.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;


namespace ToyBox.classes.MainUI {
    public static class SettlementsEditor {
        public static Settings Settings => Main.settings;
        private static Dictionary<object, bool> toggleStates = new();

        public static void OnGUI() {
            var kingdom = KingdomState.Instance;
            if (kingdom == null) {
                Label("You must unlock the crusade before you can access these toys.".yellow().bold());
                return;
            }
            HStack("Settlements", 1,
                () => {
                    using (VerticalScope()) {
                        if (kingdom.SettlementsManager.Settlements.Count == 0)
                            Label("None".orange().bold() + " - please progress further into the game".green());
                        Toggle("Ignore building restrions", ref Settings.toggleIgnoreSettlementRestrictions, AutoWidth());
                        /*
                        if (Settings.toggleIgnoreSettlementRestrictions) {
                            UI.Toggle("Ignore player class restrictions", ref Settings.toggleIgnoreBuildingClassRestrictions);
                            UI.Toggle("Ignore building adjacency restrictions", ref Settings.toggleIgnoreBuildingAdjanceyRestrictions);
                        }
                        */
                        foreach (var settlement in kingdom.SettlementsManager.Settlements) {
                            var showBuildings = false;
                            var buildings = settlement.Buildings;
                            using (HorizontalScope()) {
                                Label(settlement.Name.orange().bold(), 350.width());
                                25.space();
                                if (EnumGrid(ref settlement.m_Level)) {

                                }
                                25.space();
                                showBuildings = toggleStates.GetValueOrDefault(buildings, false);
                                if (DisclosureToggle($"Buildings: {buildings.Count()}", ref showBuildings, 150)) {
                                    toggleStates[buildings] = showBuildings;
                                }
                            }
                            if (showBuildings) {
                                foreach (var building in buildings) {
                                    using (HorizontalScope()) {
                                        100.space();
                                        Label(building.Blueprint.name.cyan(), 350.width());
                                        ActionButton("Finish", () => {
                                            building.IsFinished = true;
                                        }, AutoWidth());
                                        25.space();
                                        Label(building.IsFinished.ToString(), 200.width());
                                        25.space();
                                        Label(building.Blueprint.MechanicalDescription.ToString().StripHTML().orange() + "\n" + building.Blueprint.Description.ToString().StripHTML().green());
                                    }
                                }
                            }
                        }
                    }
                });
        }
    }
}