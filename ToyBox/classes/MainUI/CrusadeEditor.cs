using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Armies.State;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Kingdom;
using ModKit;
using System.Linq;

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

        var armies = Game.Instance.Player.GlobalMap.LastActivated.Armies;

        var playerArmies = armies.Where(army => army.Data.Faction == ArmyFaction.Crusaders);

        UI.Div(0, 25);
        UI.HStack("Player Armies", 1,
            () => {
                UI.Label("Name".yellow(), UI.MinWidth(100), UI.MaxWidth(200));
                UI.Label("Type".yellow(), UI.MinWidth(100), UI.MaxWidth(100));
                UI.Label("Squad Count".yellow(), UI.MinWidth(100), UI.MaxWidth(100));
            },
            () => {
                using (UI.VerticalScope()) {
                    foreach (var army in playerArmies) {
                        using (UI.HorizontalScope()) {
                            UI.Label(army.Data.ArmyName.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(200));
                            UI.Label(army.ArmyType.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(100));
                            UI.Label(army.Data.Squads.Count.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(100));
                        }
                    }
                }
            },
            () => {
                var squads = playerArmies.First().Data;
                using (UI.VerticalScope()) {
                    foreach (SquadState squad in squads.m_Squads) {
                        UI.BeginHorizontal();
                        UI.Label(squad.Unit.NameSafe(), UI.Width(100));
                        UI.Label(squad.Count.ToString(), UI.Width(100));
                        UI.Label(squad.Morale.m_Value.ToString(), UI.Width(100));
                        UI.EndHorizontal();
                    }    
                }
                
            }
        );

        UI.Div(0, 25);
        UI.HStack("Army Edits", 1,
            () => UI.Toggle("Infinite Mercenary Rerolls", ref Settings.toggleInfiniteArmyRerolls),
            () => {
                UI.Toggle("Experimental - Enable Large Player Armies", ref Settings.toggleLargeArmies);
                if (Settings.toggleLargeArmies) {
                    BlueprintRoot.Instance.Kingdom.StartArmySquadsCount = 14;
                    BlueprintRoot.Instance.Kingdom.MaxArmySquadsCount = 14;
                }
                else {
                    BlueprintRoot.Instance.Kingdom.StartArmySquadsCount = 4;
                    BlueprintRoot.Instance.Kingdom.MaxArmySquadsCount = 7;
                }
            },
            () => UI.Slider("Recruitment Cost", ref Settings.recruitmentCost, 0f, 1f, 1f, 2, "", UI.AutoWidth()),
            () => UI.LogSlider("Number of Recruits", ref Settings.recruitmentMultiplier, 0f, 100, 1, 1, "",
                UI.AutoWidth()),
            () => UI.LogSlider("Army Experience Multiplier", ref Settings.armyExperienceMultiplier, 0f, 100, 1, 1, "",
                UI.AutoWidth()),
            () => UI.LogSlider("After Army Battle Raise Multiplier", ref Settings.postBattleSummonMultiplier, 0f, 100,
                1, 1, "", UI.AutoWidth()),
            () => { }
        );

            UI.Div(0, 25);
            UI.HStack("Army Edits", 1,
                () => UI.Slider("Player Leader Ability Strength", ref Settings.playerLeaderPowerMultiplier, 0f, 10f, 1f, 2, "", UI.AutoWidth()),
                () => UI.Slider("Enemy Leader Ability Strength", ref Settings.enemyLeaderPowerMultiplier, 0f, 5f, 1f, 2, "", UI.AutoWidth())
            );

            UI.Div(0, 25);
            var moraleState = kingdom.MoraleState;
            UI.HStack("Morale", 1,
                () => {
                    UI.Toggle("Flags always green", ref Settings.toggleCrusadeFlagsStayGreen);
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
                () => UI.Toggle("Instant Events", ref Settings.toggleInstantEvent, 0),
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
