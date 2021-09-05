// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.Cheats;
using Kingmaker.Kingdom;
using ModKit;
using System;

namespace ToyBox
{
    public static class CheapTricks
    {
        public static Settings settings => Main.settings;

        public static void ResetGUI() { }

        public static void OnGUI()
        {
            if (Main.IsInGame)
            {
                UI.BeginHorizontal();
                UI.Space(25);
                UI.Label("increment".cyan(), UI.AutoWidth());
                int increment = UI.IntTextField(ref settings.increment, null, UI.Width(150));
                UI.EndHorizontal();
                var mainChar = Game.Instance.Player.MainCharacter.Value;
                var kingdom = KingdomState.Instance;
                UI.Div(0, 25);

                UI.HStack("Resources", 1,
                          () =>
                          {
                              long money = Game.Instance.Player.Money;
                              UI.Label("Gold".cyan(), UI.Width(150));
                              UI.Label(money.ToString().orange().bold(), UI.Width(200));

                              UI.ActionButton($"Gain {increment}",
                                              () => Game.Instance.Player.GainMoney(increment), UI.AutoWidth());

                              UI.ActionButton($"Lose {increment}",
                                              () =>
                                              {
                                                  long loss = Math.Min(money, increment);
                                                  Game.Instance.Player.GainMoney(-loss);
                                              }, UI.AutoWidth());
                          },
                          () =>
                          {
                              int exp = mainChar.Progression.Experience;
                              UI.Label("Experience".cyan(), UI.Width(150));
                              UI.Label(exp.ToString().orange().bold(), UI.Width(200));

                              UI.ActionButton($"Gain {increment}",
                                              () => Game.Instance.Player.GainPartyExperience(increment), UI.AutoWidth());
                          });

                if (kingdom != null)
                {
                    UI.Div(0, 25);

                    UI.HStack("Kingdom", 1,
                              () =>
                              {
                                  UI.Label("Finances".cyan(), UI.Width(150));
                                  UI.Label(kingdom.Resources.Finances.ToString().orange().bold(), UI.Width(200));

                                  UI.ActionButton($"Gain {increment}",
                                                  () => kingdom.Resources += KingdomResourcesAmount.FromFinances(increment), UI.AutoWidth());

                                  UI.ActionButton($"Lose {increment}",
                                                  () => kingdom.Resources -= KingdomResourcesAmount.FromFinances(increment), UI.AutoWidth());
                              },
                              () =>
                              {
                                  UI.Label("Materials".cyan(), UI.Width(150));
                                  UI.Label(kingdom.Resources.Materials.ToString().orange().bold(), UI.Width(200));

                                  UI.ActionButton($"Gain {increment}",
                                                  () => kingdom.Resources += KingdomResourcesAmount.FromMaterials(increment), UI.AutoWidth());

                                  UI.ActionButton($"Lose {increment}",
                                                  () => kingdom.Resources -= KingdomResourcesAmount.FromMaterials(increment), UI.AutoWidth());
                              },
                              () =>
                              {
                                  UI.Label("Favors".cyan(), UI.Width(150));
                                  UI.Label(kingdom.Resources.Favors.ToString().orange().bold(), UI.Width(200));

                                  UI.ActionButton($"Gain {increment}",
                                                  () => kingdom.Resources += KingdomResourcesAmount.FromFavors(increment), UI.AutoWidth());

                                  UI.ActionButton($"Lose {increment}",
                                                  () => kingdom.Resources -= KingdomResourcesAmount.FromFavors(increment), UI.AutoWidth());
                              });
                }
            }

            UI.Div(0, 25);

            UI.HStack("Combat", 4,
                      () => UI.ActionButton("Rest All", CheatsCombat.RestAll),
                      () => UI.ActionButton("Empowered", () => CheatsCombat.Empowered("")),
                      () => UI.ActionButton("Full Buff Please", () => CheatsCombat.FullBuffPlease("")),
                      () => UI.ActionButton("Remove Buffs", Actions.RemoveAllBuffs),
                      () => UI.ActionButton("Remove Death's Door", CheatsCombat.DetachDebuff),
                      () => UI.ActionButton("Kill All Enemies", CheatsCombat.KillAll),
                      () => UI.ActionButton("Summon Zoo", () => CheatsCombat.SpawnInspectedEnemiesUnderCursor(""))
            );

            UI.Div(0, 25);

            UI.HStack("Common", 4,
                      () => UI.ActionButton("Teleport Party To You", Actions.TeleportPartyToPlayer),
                      () => UI.ActionButton("Go To Global Map", () => Actions.TeleportToGlobalMap()),
                      () => UI.ActionButton("Perception Checks", Actions.RunPerceptionTriggers),
                      () => UI.ActionButton("Set Perception to 40", () =>
                                                                    {
                                                                        CheatsCommon.StatPerception();
                                                                        Actions.RunPerceptionTriggers();
                                                                    }),
                      () => UI.ActionButton("Change Weather", () => CheatsCommon.ChangeWeather("")),
                      () => UI.ActionButton("Give All Items", () => CheatsUnlock.CreateAllItems("")),
                      () => { }
            );

            UI.Div(0, 25);

            UI.HStack("Preview", 0, () =>
                                    {
                                        UI.Toggle("Dialog Results", ref settings.previewDialogResults);
                                        UI.Space(25);
                                        UI.Toggle("Dialog Alignment", ref settings.previewAlignmentRestrictedDialog);
                                        UI.Space(25);
                                        UI.Toggle("Random Encounters", ref settings.previewRandomEncounters);
                                        UI.Space(25);
                                        UI.Toggle("Events", ref settings.previewEventResults);
                                    });

            UI.Div(0, 25);

            UI.HStack("Tweaks", 1,
                      () => UI.Toggle("Allow Achievements While Using Mods", ref settings.toggleAllowAchievementsDuringModdedGame),
                      () => UI.Toggle("Object Highlight Toggle Mode", ref settings.highlightObjectsToggle),
                      () => UI.Toggle("Whole Team Moves Same Speed", ref settings.toggleMoveSpeedAsOne),
                      () => UI.Toggle("Infinite Abilities", ref settings.toggleInfiniteAbilities),
                      () => UI.Toggle("Infinite Spell Casts", ref settings.toggleInfiniteSpellCasts),
                      () => UI.Toggle("Unlimited Actions During Turn", ref settings.toggleUnlimitedActionsPerTurn),
                      () => UI.Toggle("Infinite Charges On Items", ref settings.toggleInfiniteItems),
                      () => UI.Toggle("Instant Cooldown", ref settings.toggleInstantCooldown),
                      () => UI.Toggle("Highlight Copyable Scrolls", ref settings.toggleHighlightCopyableScrolls),
                      () => UI.Toggle("Spontaneous Caster Scroll Copy", ref settings.toggleSpontaneousCopyScrolls),
                      () => UI.Toggle("Disable Equipment Restrictions", ref settings.toggleEquipmentRestrictions),
                      () => UI.Toggle("Disable Dialog Restrictions", ref settings.toggleDialogRestrictions),
                      () => UI.Toggle("No Friendly Fire On AOEs", ref settings.toggleNoFriendlyFireForAOE),
                      () => UI.Toggle("Free Meta-Magic", ref settings.toggleMetamagicIsFree),
                      () => UI.Toggle("No Fog Of War", ref settings.toggleNoFogOfWar),
                      () => UI.Toggle("No Material Components", ref settings.toggleMaterialComponent),
                      () => UI.Toggle("Instant Rest After Combat", ref settings.toggleInstantRestAfterCombat),
                      () => UI.Toggle("Auto Load Last Save On Launch", ref settings.toggleAutomaticallyLoadLastSave),
                      () => { }
            );

            UI.Div(153, 25);

            UI.HStack("", 1,
                      () => UI.EnumGrid("Disable Attacks Of Opportunity", ref settings.noAttacksOfOpportunitySelection, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Can Move Through", ref settings.allowMovementThroughSelection, 0, UI.AutoWidth()),
                      () =>
                      {
                          UI.Space(328);
                          UI.Label("This allows characters you control to move through the selected category of units during combat".green(), UI.AutoWidth());
                      }
            );

            UI.HStack("Multipliers", 1,
                      () => UI.LogSlider("Experience", ref settings.experienceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Money Earned", ref settings.moneyMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Vendor Sell Price", ref settings.vendorSellPriceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Vendor Buy Price", ref settings.vendorBuyPriceMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.Slider("Encumbrance", ref settings.encumberanceMultiplier, 1, 100, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Spells Per Day", ref settings.spellsPerDayMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Movement Speed", ref settings.partyMovementSpeedMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Travel Speed", ref settings.travelSpeedMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Companion Cost", ref settings.companionCostMultiplier, 0, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Enemy HP Multiplier", ref settings.enemyBaseHitPointsMultiplier, 0f, 20, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Buff Duration", ref settings.buffDurationMultiplierValue, 0f, 100, 1, 1, "", UI.AutoWidth()),
                      () => UI.LogSlider("Field Of View", ref settings.fovMultiplier, 0.4f, settings.fovMultiplierMax, 1, 2, "", UI.AutoWidth()),
                      () => UI.LogSlider("Max Field Of View", ref settings.fovMultiplierMax, 1.5f, 3f, 1, 2, "", UI.AutoWidth()),
                      () =>
                      {
                          UI.Space(328);
                          UI.Label("Experimental: Increasing this may cause performance issues when rotating".green(), UI.AutoWidth());
                      },
                      () => { }
            );

            UI.Div(0, 25);

            UI.HStack("Dice Rolls", 1,
                      () => UI.EnumGrid("All Hits Critical", ref settings.allHitsCritical, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Roll With Avantage", ref settings.rollWithAdvantage, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Roll With Disavantage", ref settings.rollWithDisadvantage, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Always Roll 20", ref settings.alwaysRoll20, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Always Roll 1", ref settings.alwaysRoll1, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Never Roll 20", ref settings.neverRoll20, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Never Roll 1", ref settings.neverRoll1, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Always Roll 20 Initiative ", ref settings.roll20Initiative, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Always Roll 1 Initiative", ref settings.roll1Initiative, 0, UI.AutoWidth()),
                      () => UI.EnumGrid("Always Roll 20 Out Of Combat", ref settings.alwaysRoll20OutOfCombat, 0, UI.AutoWidth()),
                      () => { }
            );

            UI.Div(0, 25);

            UI.HStack("Crusade", 1,
                      () => UI.Toggle("Instant Events", ref settings.toggleInstantEvent),
                      () =>
                      {
                          UI.Slider("Build Time Modifer", ref settings.kingdomBuildingTimeModifier, -10, 10, 0, 1, "", UI.AutoWidth());
                          var instance = KingdomState.Instance;

                          if (instance != null)
                          {
                              instance.BuildingTimeModifier = settings.kingdomBuildingTimeModifier;
                          }
                      },
                      () => { }
            );

            UI.Space(25);
        }
    }
}