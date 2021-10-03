using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Armies.State;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Globalmap.State;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using ModKit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                    UI.Label("increment".cyan(), UI.Width(325));
                    var increment = UI.IntTextField(ref Settings.increment, null, UI.Width(150));
                    UI.Space(25);
                    UI.Label("Experimental".orange().bold());
                },
                () => {
                    using (UI.VerticalScope()) {
                        UI.Space(15);
                        UI.Div(0, 0, 800);
                        using (UI.HorizontalScope()) {
                            UI.Label("Kingdom Stat", UI.Width(325));
                            UI.Label("Rank", UI.Width(150));
                            UI.Label("Experience", UI.Width(150));
                            UI.Label("Next Rank", UI.Width(150));
                        }

                        foreach (KingdomStats.Stat kingdomStat in kingdom.Stats) {
                            var conditions = KingdomRoot.Instance.RankUps.Conditions[kingdomStat.Type];
                            using (UI.HorizontalScope()) {
                                UI.Label(kingdomStat.Type.ToString().cyan(), UI.Width(283));
                                var rank = kingdomStat.Rank;
                                var exp = kingdomStat.Value.ToString().orange();
                                var required = conditions.GetRequiredStatValue(kingdomStat.Rank + 1).ToString().cyan();
                                if (kingdomStat.Rank > 1) {
                                    UI.ActionButton("<", () => { kingdomStat.Rank--; }, GUI.skin.box, UI.Width(63));
                                }
                                else { UI.Label("<", GUI.skin.box, UI.Width(63)); }
                                UI.Space(-10);
                                UI.Label($"{kingdomStat.Rank}".orange().bold(), GUI.skin.box, UI.Width(35));
                                UI.Space(-10);
                                if (kingdomStat.Rank < 8) {
                                    UI.ActionButton(">", () => { kingdomStat.Rank++; }, GUI.skin.box, UI.Width(63));
                                }
                                else { UI.Label("max".cyan(), GUI.skin.box, UI.Width(63)); }
                                UI.Space(42);
                                UI.Label(exp, UI.Width(150));
                                UI.Label(required, UI.Width(150));
                                UI.Space(10);
                                UI.ActionButton($"Gain {Settings.increment}", () => {
                                    kingdomStat.Value += Settings.increment;
                                }, UI.AutoWidth());
                                UI.ActionButton($"Lose {Settings.increment}", () => {
                                    kingdomStat.Value -= Settings.increment;
                                }, UI.AutoWidth());
                            }
                        }
                    }
                },
                () => {
                    using (UI.VerticalScope()) {
                        UI.Space(15);
                        UI.Div(0, 0, 800);
                        UI.Label("Kingdom Finances");
                    }
                },
                () => {
                    UI.Label("Finances".cyan(), UI.Width(325));
                    UI.Label(kingdom.Resources.Finances.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Materials".cyan(), UI.Width(325));
                    UI.Label(kingdom.Resources.Materials.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Favors".cyan(), UI.Width(325));
                    UI.Label(kingdom.Resources.Favors.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        kingdom.Resources += KingdomResourcesAmount.FromFavors(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        kingdom.Resources -= KingdomResourcesAmount.FromFavors(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    using (UI.VerticalScope()) {
                        UI.Space(15);
                        UI.Div(0, 0, 800);
                    }
                },
                () => UI.Toggle("Instant Events", ref Settings.toggleInstantEvent, 0),
                () => {
                    UI.Slider("Build Time Modifer", ref Settings.kingdomBuildingTimeModifier, -2, 2, 0, 2, "", UI.Width(400));
                    var instance = KingdomState.Instance;
                    if (instance != null) {
                        instance.BuildingTimeModifier = Settings.kingdomBuildingTimeModifier;
                    }
                    UI.Space(25);
                    UI.Label("Multiplies build time by (1 + modifier). -1 will make new buildings instant.".green());
                },
                () => {
                    var startDate = Game.Instance.BlueprintRoot.Calendar.GetStartDate();
                    var currentDate = KingdomState.Instance.Date;
                    var dateText = Game.Instance.BlueprintRoot.Calendar.GetDateText(currentDate - startDate, GameDateFormat.Full, true);
                    using (UI.VerticalScope()) {
                        UI.Space(15);
                        UI.Div(0, 0, 800);
                        using (UI.HorizontalScope()) {
                            UI.Label("Date".cyan(), UI.Width(325));
                            UI.Label(dateText.orange().bold(), UI.AutoWidth());
                        }
                        using (UI.HorizontalScope()) {
                            UI.ActionButton($"+1 Day", () => { Actions.KingdomTimelineAdvanceDays(1); }, UI.Width(150));
                            UI.ActionButton($"+1 Month", () => {
                                Actions.KingdomTimelineAdvanceDays(KingdomState.Instance.DaysTillNextMonth);
                            }, UI.Width(150));
                        }
                    }
                },
               () => { }
        );

        }

    }
}
