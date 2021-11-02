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
            var ks = KingdomState.Instance;
            if (ks == null) {
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
            var moraleState = ks.MoraleState;
            UI.HStack("Morale", 1,
                () => {
                    UI.Toggle("Flags always green", ref Settings.toggleCrusadeFlagsStayGreen);
                    KingdomCheats.AddMorale();
                },
                () => {
                    var value = moraleState.CurrentValue;
                    UI.Slider("Morale", ref value, moraleState.MinValue, moraleState.MaxValue, 1, "", UI.AutoWidth());
                    moraleState.CurrentValue = value;
                },
                () => {
                    var value = moraleState.MaxValue;
                    UI.Slider("Max Morale", ref value, -200, 200, 20, "", UI.AutoWidth());
                    moraleState.MaxValue = value;
                },
                () => {
                    var value = moraleState.MinValue;
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
                        foreach (var kingdomStat in ks.Stats) {
                            var conditions = KingdomRoot.Instance.RankUps.Conditions[kingdomStat.Type];
                            using (UI.HorizontalScope()) {
                                var rank = kingdomStat.Rank;
                                var exp = kingdomStat.Value.ToString().orange();
                                var required = conditions.GetRequiredStatValue(kingdomStat.Rank + 1).ToString().cyan();
                                UI.ValueAdjuster(kingdomStat.Type.ToString(), () => kingdomStat.Rank, v => kingdomStat.Rank = v, 1, 0, 8);
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
                        UI.Div(0, 0, 800);
                        UI.DescriptiveLabel("Cost Modifiers", "The following modifiers all work on ".green() + "cost = cost (1 + modifier) ".yellow() + "so a value of ".green() + "-1".yellow() + " means the cost is free, ".green() + "0".yellow() +" is normal cost and ".green() + "2".yellow() + " increases it 3x".green());
                        UI.Slider("Claim Cost Modifier", () => ks.ClaimCostModifier, v => ks.ClaimCostModifier = v, -1, 2, 0, 1);
                        UI.Slider("Claim Time Modifier", () => ks.ClaimTimeModifier, v => ks.ClaimTimeModifier = v, -1, 2, 0, 1);
                        UI.Slider("Rankup Time Modifer", () => ks.RankupTimeModifier, v => ks.RankupTimeModifier = v, -1, 2, 0, 1);
                        UI.Slider("Build Time Modifier", ref Settings.kingdomBuildingTimeModifier, -1, 2, 0, 1);
                        UI.Div(0, 0, 800);
                        UI.DescriptiveLabel("Random Encounters", "The following modifiers all work on ".green() + "chance = chance (1 + modifier) ".yellow() + "so a value of ".green() + "-1".yellow() + " means the chance is 0, ".green() + "0".yellow() + " is chance cost and ".green() + "2".yellow() + " increases it 3x".green());
                        UI.Slider("% Chance (Unclaimed)", () => ks.REModifierUnclaimed, v => ks.REModifierUnclaimed = v, -1f, 2f, 0f, 1);
                        UI.Slider("% Chance (Claimed)", () => ks.REModifierClaimed, v => ks.REModifierClaimed = v, -1, 2, -0.5f, 1);
                        UI.Slider("% Chance (Upgraded)", () => ks.REModifierUpgraded, v => ks.REModifierUnclaimed = v, -1f, 2f, -1f, 1);
                        UI.Div(0, 0, 800);
                        UI.ValueAdjuster("Confidence (Royal Court)", () => ks.RoyalCourtConfidence, v => ks.RoyalCourtConfidence = v, 1, 0, int.MaxValue);
                        UI.ValueAdjuster("Confidence (Nobles)", () => ks.NobilityConfidence, v => ks.NobilityConfidence = v, 1, 0, int.MaxValue);
                        UI.ValueAdjuster("Victories This Week", () => ks.VictoriesThisWeek, v => ks.VictoriesThisWeek = v, 1, 0, int.MaxValue);
                        UI.EnumGrid("Unrest", () => ks.Unrest, (u) => ks.Unrest = u);
                        UI.AlignmentGrid("Alignment", ks.Alignment, (a) => ks.Alignment = a, UI.Width(325));
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
                    UI.Label(ks.Resources.Finances.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        ks.Resources += KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        ks.Resources -= KingdomResourcesAmount.FromFinances(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Materials".cyan(), UI.Width(325));
                    UI.Label(ks.Resources.Materials.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        ks.Resources += KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        ks.Resources -= KingdomResourcesAmount.FromMaterials(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    UI.Label("Favors".cyan(), UI.Width(325));
                    UI.Label(ks.Resources.Favors.ToString().orange().bold(), UI.Width(100));
                    UI.ActionButton($"Gain {Settings.increment}", () => {
                        ks.Resources += KingdomResourcesAmount.FromFavors(Settings.increment);
                    }, UI.AutoWidth());
                    UI.ActionButton($"Lose {Settings.increment}", () => {
                        ks.Resources -= KingdomResourcesAmount.FromFavors(Settings.increment);
                    }, UI.AutoWidth());
                },
                () => {
                    using (UI.VerticalScope()) {
                        UI.Space(15);
                        UI.Div(0, 0, 800);
                    }
                },
                () => UI.Toggle("Instant Events", ref Settings.toggleInstantEvent),
                () => {

                    UI.Slider("Crusade card resolution time multiplier", ref Settings.kingdomTaskResolutionLengthMultiplier, -1, 2, 0, 2, "", UI.Width(400));
                    UI.Space(25);
                    UI.Label("Multiplies crusade card resolution time by (1 + modifier). -1 will make things as fast as possible (minimum 1 day to avoid possible bugs)".green());
                },
            () => {
                    UI.Slider("Build Time Modifier", ref Settings.kingdomBuildingTimeModifier, -1, 2, 0, 2, "", UI.Width(400));
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
                            UI.ActionButton($"+1 Day", () => { Actions.KingdomTimelineAdvanceDays(1); }, UI.Width(150));
                            UI.ActionButton($"+1 Month", () => {
                                Actions.KingdomTimelineAdvanceDays(KingdomState.Instance.DaysTillNextMonth);
                            }, UI.Width(150));
                        }
                        UI.ValueAdjuster("Current Day", () => ks.CurrentDay, v => ks.CurrentDay = v, 1, 0, int.MaxValue);
                        UI.ValueAdjuster("Current Turn", () => ks.CurrentTurn, v => ks.CurrentTurn = v, 1, 0, int.MaxValue);
                    }
                },
               () => { }
        );

        }
    }
}
