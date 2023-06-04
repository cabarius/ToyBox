using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Code.UI.MVVM.VM.NavigatorResource;
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
        private static int navigatorResourceAdjustment = 100;
        private static int startingWidth = 250;
        public static void OnGUI() {
            if (factionsToPick == null) {
                List<NamedFunc<FactionType>> tmp = new();
                foreach (FactionType @enum in Enum.GetValues(typeof(FactionType))) {
                    tmp.Add(new NamedFunc<FactionType>(@enum.ToString(), () => @enum));
                }
                factionsToPick = tmp.ToArray();
            }
            var selected = TypePicker("Faction Selector".localize().bold(), ref selectedFaction, factionsToPick, true);
            15.space();
            var faction = selected.func();
            using (HorizontalScope()) {
                Label("Current Reputation".localize().bold() + ": ", Width(startingWidth));
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
            15.space();
            Div();
            15.space();
            if (Game.Instance.Player.WarpTravelState?.IsInitialized ?? false) {
                using (HorizontalScope()) {
                    Label("Current Navigator Resource".localize().bold() + ": ", Width(startingWidth));
                    using (VerticalScope()) {
                        Label(Game.Instance.Player.WarpTravelState.NavigatorResource.ToString());
                        using (HorizontalScope()) {
                            Label("Adjust Navigator Resource by the following amount:".localize());
                            IntTextField(ref navigatorResourceAdjustment, null, MinWidth(200), AutoWidth());
                            navigatorResourceAdjustment = Math.Max(0, navigatorResourceAdjustment);
                            10.space();
                            ActionButton("Add".localize(), () => { CheatsGlobalMap.AddNavigatorResource(navigatorResourceAdjustment); NavigatorResourceVM.Instance?.SetCurrentValue(); });
                            10.space();
                            ActionButton("Remove".localize(), () => { CheatsGlobalMap.AddNavigatorResource(-navigatorResourceAdjustment); NavigatorResourceVM.Instance?.SetCurrentValue(); });
                        }
                    }
                }
            }
        }
    }
}
