﻿using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Armies.State;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Globalmap.State;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using ModKit;
using System.Linq;

namespace ToyBox.classes.MainUI {
public static class CrusadeEditor {
    public static void ResetGUI() { }
    public static Settings Settings => Main.settings;

    private static GlobalMapArmyState selectedArmy = null;

    public static void OnGUI() {
        var kingdom = KingdomState.Instance;
        if (kingdom == null) {
            UI.Label("You must unlock the crusade before you can access these toys.".yellow().bold());
            return;
        }


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
                    UI.Space(50);
                    UI.Label("EXPERIMENTAL".orange().bold());
                },
                () => {
                    using (UI.VerticalScope()) {
                        using (UI.HorizontalScope()) {
                            UI.Label("Kingdom Stat", UI.Width(150));
                            UI.Label("Experience", UI.Width(100));
                            UI.Label("Rank", UI.Width(100));
                            UI.Label("Required", UI.Width(100));
                        }

                        foreach (KingdomStats.Stat kingdomStat in kingdom.Stats) {
                            using (UI.HorizontalScope()) {
                                UI.Label(kingdomStat.Type.ToString().cyan(), UI.Width(150));
                                UI.Label(kingdomStat.Value.ToString().orange().bold(), UI.Width(100));
                                UI.Label($"Rank {kingdomStat.Rank}".orange().bold(), UI.Width(100));
                                UI.Label(KingdomRoot.Instance.RankUps.Conditions[kingdomStat.Type].GetRequiredStatValue(kingdomStat.Rank).ToString().orange().bold(), UI.Width(100));
                                UI.ActionButton($"Gain {Settings.increment} Experience", () => {
                                    kingdomStat.Value += Settings.increment;
                                }, UI.AutoWidth());
                                UI.ActionButton($"Lose {Settings.increment} Experience", () => {
                                    kingdomStat.Value -= Settings.increment;
                                }, UI.AutoWidth());
                                if (kingdomStat.Rank < 8) {
                                    UI.ActionButton("+ Rank", () => { kingdomStat.Rank++; }, UI.AutoWidth());
                                }
                                if (kingdomStat.Rank > 1) {
                                    UI.ActionButton("- Rank", () => { kingdomStat.Rank--; }, UI.AutoWidth());
                                }
                            }
                        }
                    }
                },
                () => {
                    UI.Space(50);
                    UI.Label("Kingdom Finances");},
                () => {
                    UI.Label("Finances".cyan(), UI.Width(150));
                    UI.Label(kingdom.Resources.Finances.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Materials".cyan(), UI.Width(150));
                    UI.Label(kingdom.Resources.Materials.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Favors".cyan(), UI.Width(150));
                    UI.Label(kingdom.Resources.Favors.ToString().orange().bold(), UI.Width(100));
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
            () => {
                using (UI.HorizontalScope()) {
                    UI.Label("Date".cyan(), UI.Width(150));
                    UI.Label(
                        Game.Instance.BlueprintRoot.Calendar.GetDateText(
                            KingdomState.Instance.Date - Game.Instance.BlueprintRoot.Calendar.GetStartDate(),
                            GameDateFormat.Full, true
                        ).orange().bold(), UI.Width(200));
                    UI.ActionButton($"+1 Day", () => { Actions.KingdomTimelineAdvanceDays(1); }, UI.AutoWidth());
                    UI.ActionButton($"+1 Month", () => {
                        Actions.KingdomTimelineAdvanceDays(KingdomState.Instance.DaysTillNextMonth);
                    }, UI.AutoWidth());
                }
            },
            () => { }
        );

        var armies = Game.Instance.Player.GlobalMap.LastActivated.Armies;

        var playerArmies = armies.Where(army => army.Data.Faction == ArmyFaction.Crusaders);
        var demonArmies = armies.Where(army => army.Data.Faction == ArmyFaction.Demons);

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
                        bool showSquads = false;
                        using (UI.HorizontalScope()) {
                            UI.Label(army.Data.ArmyName.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(200));
                            UI.Label(army.ArmyType.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(100));
                            UI.Label(army.Data.Squads.Count.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(100));
                            showSquads = army == selectedArmy;
                            if (UI.DisclosureToggle("Squads", ref showSquads, 125)) {
                                selectedArmy = army;
                            }
                        }

                        if (showSquads) {
                            UI.Div(0, 10);

                            using (UI.VerticalScope()) {
                                UI.BeginHorizontal();
                                UI.Label("Squad Name".yellow(), UI.Width(200));
                                UI.Label("Unit Count".yellow(), UI.Width(150));
                                UI.EndHorizontal();
                            }

                            using (UI.VerticalScope()) {
                                var squads = selectedArmy.Data.m_Squads;
                                SquadState squadToRemove = null;
                                foreach (var squad in squads) {
                                    UI.BeginHorizontal();
                                    
                                    UI.Label(squad.Unit.NameSafe(), UI.Width(200));
                                    var count = squad.Count;
                                    UI.ActionIntTextField(ref count, null,
                                        (value) => {
                                            squad.SetCount(value);
                                        },
                                        null, UI.Width(150)
                                    );

                                    UI.ActionButton("Remove", () => {
                                        squadToRemove = squad;
                                    }, UI.Width(100));


                                    UI.EndHorizontal();
                                }

                                if (squadToRemove != null) {
                                    squadToRemove.Army.RemoveSquad(squadToRemove);
                                }
                            }
                            UI.Div(0, 10);
                        }
                    }
                }
            }
        );
        
         UI.Div(0, 25);
        UI.HStack("Demon Armies", 1,
            () => {
                UI.Label("Name".yellow(), UI.MinWidth(100), UI.MaxWidth(200));
                UI.Label("Type".yellow(), UI.MinWidth(100), UI.MaxWidth(100));
                UI.Label("Squad Count".yellow(), UI.MinWidth(100), UI.MaxWidth(100));
            },
            () => {
                using (UI.VerticalScope()) {
                    foreach (var army in demonArmies) {
                        bool showSquads = false;
                        using (UI.HorizontalScope()) {
                            UI.Label(army.Data.ArmyName.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(200));
                            UI.Label(army.ArmyType.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(100));
                            UI.Label(army.Data.Squads.Count.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(100));
                            showSquads = army == selectedArmy;
                            if (UI.DisclosureToggle("Squads", ref showSquads, 125)) {
                                selectedArmy = army;
                            }
                        }

                        if (showSquads) {
                            UI.Div(0, 10);

                            using (UI.VerticalScope()) {
                                UI.BeginHorizontal();
                                UI.Label("Squad Name".yellow(), UI.Width(200));
                                UI.Label("Unit Count".yellow(), UI.Width(150));
                                UI.EndHorizontal();
                            }

                            using (UI.VerticalScope()) {
                                var squads = selectedArmy.Data.m_Squads;
                                SquadState squadToRemove = null;
                                foreach (var squad in squads) {
                                    UI.BeginHorizontal();
                                    
                                    UI.Label(squad.Unit.NameSafe(), UI.Width(200));
                                    var count = squad.Count;
                                    UI.ActionIntTextField(ref count, null,
                                        (value) => {
                                            squad.SetCount(value);
                                        },
                                        null, UI.Width(150)
                                    );

                                    UI.ActionButton("Remove", () => {
                                        squadToRemove = squad;
                                    }, UI.Width(100));


                                    UI.EndHorizontal();
                                }

                                if (squadToRemove != null) {
                                    squadToRemove.Army.RemoveSquad(squadToRemove);
                                }
                            }
                            UI.Div(0, 10);
                        }
                    }
                }
            }
        );
    }
}
}
