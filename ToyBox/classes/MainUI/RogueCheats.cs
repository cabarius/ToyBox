using Kingmaker.Controllers;
using Kingmaker.Enums;
using ModKit;
using System;
using System.Collections.Generic;

namespace ToyBox.classes.MainUI {
    // Made for Rogue Trader specific stuff which I have no idea where to put
    public static class RogueCheats {
        public static Settings Settings => Main.Settings;
        private static int selectedFaction = 0;
        public static NamedFunc<FactionType>[] factionsToPick;
        private static int reputationAdjustment = 100;
        public static void OnGUI() {
            if (factionsToPick == null) {
                List<NamedFunc<FactionType>> tmp = new();
                foreach (FactionType @enum in Enum.GetValues(typeof(FactionType))) {
                    tmp.Add(new NamedFunc<FactionType>(@enum.ToString(), () => @enum));
                }
                factionsToPick = tmp.ToArray();
            }
            var selected = TypePicker("Faction Selector".localize(), ref selectedFaction, factionsToPick, true);
            25.space();
            var faction = selected.func();
            using (HorizontalScope()) {
                Label("Current Reputation".localize() + ": ");
                using (VerticalScope()) {
                    using (HorizontalScope()) {
                        Label("Level".localize() + ": ", Width(100));
                        Label(ReputationHelper.GetCurrentReputationLevel(faction).ToString());
                    }
                    using (HorizontalScope()) {
                        Label("Experience".localize() + ": ", Width(100));
                        Label($"{ReputationHelper.GetCurrentReputationPoints(faction)}/{ReputationHelper.GetNextLevelReputationPoints(faction)}");
                    }
                    using (HorizontalScope()) {
                        Label("Adjust Reputation by the following amount:".localize());
                        IntTextField(ref reputationAdjustment, null, MinWidth(200), AutoWidth());
                        reputationAdjustment = Math.Max(0, reputationAdjustment);
                        10.space();
                        ActionButton("Add".localize(), () => ReputationHelper.GainFactionReputation(faction, reputationAdjustment));
                        10.space();
                        ActionButton("Remove".localize(), () => ReputationHelper.GainFactionReputation(faction, -reputationAdjustment));
                    }
                }
            }
        }
    }
}
