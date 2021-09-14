using Kingmaker.Kingdom;
using ModKit;

namespace ToyBox.classes.MainUI {
    public static class CrusadeEditor {
        public static void ResetGUI() { }
        public static Settings Settings => Main.settings;

        public static void OnGUI() {
            var kingdom = KingdomState.Instance;
            if (kingdom == null) {
                UI.Label("You must unlock the crusade before you can access these toys.".yellow().bold());
                return;
            }

            UI.Div(0, 25);
            UI.HStack("Army Edits", 1,
                () => UI.Toggle("Infinite Mercenary Rerolls", ref Settings.toggleInfiniteArmyRerolls),
                () => UI.Slider("Recruitment Cost", ref Settings.recruitmentCost, 0f, 1f, 1f, 2, "", UI.AutoWidth()),
                () => UI.LogSlider("Number of Recruits", ref Settings.recruitmentMultiplier, 0f, 100, 1, 1, "", UI.AutoWidth()),

                () => UI.LogSlider("Army Experience Multiplier", ref Settings.armyExperienceMultiplier, 0f, 100, 1, 1, "", UI.AutoWidth()),
                () => UI.LogSlider("After Army Battle Raise Multiplier", ref Settings.postBattleSummonMultiplier, 0f, 100, 1, 1, "", UI.AutoWidth()),
                () => { }
            );

            UI.Div(0, 25);
            var moraleState = kingdom.MoraleState;
            UI.HStack("Morale", 1,
                () => {
                    UI.Toggle("Flags always green", ref Settings.toggleFlagsStayGreen);
                    KingdomCheats.AddMorale();
                },
                () => {
                    int value = moraleState.CurrentValue;
                    UI.Slider("Morale", ref value, moraleState.MinValue, moraleState.MaxValue, 1, "", UI.AutoWidth());
                    moraleState.CurrentValue = value;
                },
                () => {
                    int value = moraleState.MaxValue;
                    UI.Slider("Max Morale", ref value, -200, 200, 20, "", UI.AutoWidth());
                    moraleState.MaxValue = value;
                },
                () => {
                    int value = moraleState.MinValue;
                    UI.Slider("Min Morale", ref value, -200, 200, -100, "", UI.AutoWidth());
                    moraleState.MinValue = value;
                },
                () => { }
            );

            UI.Div(0, 25);
            UI.HStack("Kingdom", 1,
                () => {
                    UI.BeginHorizontal();
                    UI.Label("increment".cyan(), UI.AutoWidth());
                    var increment = UI.IntTextField(ref Settings.increment, null, UI.Width(150));
                    UI.EndHorizontal();
                },
                () => {
                    UI.Label("Finances".cyan(), UI.Width(150));
                    UI.Label(kingdom.Resources.Finances.ToString().orange().bold(), UI.Width(200));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Materials".cyan(), UI.Width(150));
                    UI.Label(kingdom.Resources.Materials.ToString().orange().bold(), UI.Width(200));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Favors".cyan(), UI.Width(150));
                    UI.Label(kingdom.Resources.Favors.ToString().orange().bold(), UI.Width(200));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromFavors(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromFavors(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => { },
                () => UI.Toggle("Instant Events", ref Settings.toggleInstantEvent),
                () => {
                    UI.Slider("Build Time Modifer", ref Settings.kingdomBuildingTimeModifier, -2, 2, 0, 2, "", UI.AutoWidth());
                    var instance = KingdomState.Instance;
                    if (instance != null) {
                        instance.BuildingTimeModifier = Settings.kingdomBuildingTimeModifier;
                    }
                },
                () => UI.Label("Multiplies build time by (1 + modifier). -1 will make new buildings instant.".cyan()),
            () => { }
            );
        }
    }
}
