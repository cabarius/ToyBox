using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Armies.State;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using ModKit;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace ToyBox.classes.MainUI {
    public static class ArmiesEditor {
        public static Settings Settings => Main.settings;

        public static IEnumerable<(GlobalMapArmyState, float)> armies;
        public static IEnumerable<(GlobalMapArmyState, float)> playerArmies;
        public static IEnumerable<(GlobalMapArmyState, float)> demonArmies;
        public static void OnShowGUI() => UpdateArmies();
        public static void UpdateArmies() {
            armies = ArmiesByDistanceFromPlayer()?.ToList();
            if (armies != null) {
                playerArmies = from army in armies
                               where army.Item1.Data.Faction == ArmyFaction.Crusaders
                               select army;
                demonArmies = from army in armies
                              where army.Item1.Data.Faction == ArmyFaction.Demons
                              select army;
            }
        }

        private static readonly Dictionary<string, GlobalMapArmyState> armySelection = new() { };
        public static void OnGUI() {
            var kingdom = KingdomState.Instance;
            if (kingdom == null) {
                UI.Label("You must unlock the crusade before you can access these toys.".yellow().bold());
                return;
            }
            if (armies == null)
                UpdateArmies();
            if (playerArmies != null)
                ArmiesGUI("Player Armies", playerArmies);
            if (playerArmies != null && demonArmies != null) {
                UI.Div(0, 25, 0);
            }
            if (demonArmies != null)
                ArmiesGUI("Demon Armies", demonArmies);
        }
        public static void ArmiesGUI(string title, IEnumerable<(GlobalMapArmyState, float)> armies) {
            if (armies.Count() == 0) return;
            var selectedArmy = armySelection.GetValueOrDefault(title, null);
            using (UI.VerticalScope()) {
                UI.HStack(title, 1,
                    () => {
                        UI.Label("Name", UI.MinWidth(100), UI.MaxWidth(250));
                        UI.Label("Type", UI.MinWidth(100), UI.MaxWidth(250));
                        UI.Label("Squad Count", UI.Width(150));
                        UI.Space(55);
                        UI.Label("Location", UI.Width(400));
                        UI.Space(25);
                        UI.Label("Dist");
                    },
                    () => {
                        using (UI.VerticalScope()) {
                            var last = armies.Last().Item1;
                            foreach (var armyEntry in armies) {
                                var army = armyEntry.Item1;
                                var distance = armyEntry.Item2;
                                var showSquads = false;
                                using (UI.HorizontalScope()) {
                                    UI.Label(army.Data.ArmyName.ToString().orange().bold(), UI.MinWidth(100), UI.MaxWidth(250));
                                    UI.Label(army.ArmyType.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(250));
                                    UI.Label(army.Data.Squads.Count.ToString().cyan(), UI.Width(35));
                                    showSquads = army == selectedArmy;
                                    if (UI.DisclosureToggle("Squads", ref showSquads, 125)) {
                                        selectedArmy = selectedArmy == army ? null : army;
                                    }
                                    UI.Space(50);
                                    var displayName = army.Location?.GetDisplayName() ?? "traveling on a path";
                                    UI.Label(displayName.yellow(), UI.Width(400));
                                    UI.Space(25);
                                    var distStr = distance >= 0 ? $"{distance:0.#}" : "?";
                                    UI.Label(distStr, UI.Width(50));
                                    UI.Space(50 );
                                    UI.ActionButton("Teleport", () => TeleportToArmy(army), UI.Width(150));
                                    UI.Space(25);
                                    if (GlobalMapView.Instance != null) {
                                        UI.ActionButton("Summon", () => SummonArmy(army), UI.Width(150));
                                    }
                                }

                                if (showSquads) {
                                    UI.Div(0, 10);

                                    using (UI.VerticalScope()) {
                                        UI.BeginHorizontal();
                                        UI.Label("Squad Name".yellow(), UI.Width(475));
                                        UI.Space(25);
                                        UI.Label("Unit Count".yellow(), UI.Width(250));
                                        UI.EndHorizontal();
                                    }

                                    using (UI.VerticalScope()) {
                                        var squads = selectedArmy.Data.m_Squads;
                                        SquadState squadToRemove = null;
                                        foreach (var squad in squads) {
                                            using (UI.HorizontalScope()) {
                                                UI.Label(squad.Unit.NameSafe(), UI.Width(475));
                                                UI.Space(25);
                                                var count = squad.Count;
                                                UI.ActionIntTextField(ref count, null,
                                                    (value) => {
                                                        squad.SetCount(value);
                                                    },
                                                    null, UI.Width(225)
                                                );
                                                UI.Space(25);
                                                UI.ActionButton("Remove", () => {
                                                    squadToRemove = squad;
                                                }, UI.Width(150));
                                            }
                                        }

                                        if (squadToRemove != null) {
                                            squadToRemove.Army.RemoveSquad(squadToRemove);
                                        }
                                    }
                                    if (army != last)
                                        UI.Div();
                                }
                            }
                        }
                    }
                );
                if (selectedArmy != null) {
                    armySelection[title] = selectedArmy;
                }
                else {
                    armySelection.Remove(title);
                }
            }
        }

        public static GlobalMapState MainGlobalMapState() {
            var player = Game.Instance?.Player ?? null;
            var globalMapStates = player?.AllGlobalMaps;
            var armyMapStates = from mapState in globalMapStates
                                where mapState.Armies.Count > 0
                                select mapState;
            if (armyMapStates.Count() == 0) return null;
            // The current game only has one map that has armies so we will assume there is one. If DLC changes this when we need to do the calculations below on each of the maps and the player position in them separately
            var mainMapState = armyMapStates.First();
            return mainMapState;
        }
        public static void TeleportToArmy(GlobalMapArmyState army) {
            var mapPoint = army.Position.Location;
            UnityModManager.UI.Instance.ToggleWindow();
            if (!Teleport.TeleportToGlobalMapPoint(mapPoint)) {
                Teleport.TeleportToGlobalMap(() => Teleport.TeleportToGlobalMapPoint(mapPoint));
            }
        }
        public static void SummonArmy(GlobalMapArmyState army) {
            var mainMapState = MainGlobalMapState();
            var position = mainMapState.PlayerPosition;
            army.SetCurrentPosition(position);
        }
        public static float ArmyDistance(this GlobalMapState mapState, GlobalMapArmyState army, GlobalMapPosition position) {
            var dist = -1.0f;
            try {
                var location = position.Location;
                var travelData = mapState?.PathManager?.CalculateArmyPathToPosition(army, position);
                var length = travelData?.GetLength(false);
                dist = length.HasValue ? length.GetValueOrDefault() : -1.0f;
            }
            catch { }
            return dist;
        }
        public static IEnumerable<(GlobalMapArmyState, float)> ArmiesByDistanceFromPlayer() {
            if (!Main.IsInGame) return null;
            var mainMapState = MainGlobalMapState();
            var armies = mainMapState.Armies;
            var position = mainMapState.PlayerPosition;
            var results = from army in armies select (army, mainMapState.ArmyDistance(army, position));
            results = from item in results where item.Item2 >= 0 || item.army.IsRevealed select item;
            results = results.OrderBy(r => r.Item2).ThenBy(r => r.army.Location?.GetDisplayName() ?? "traveling on a path");
            return results;
        }
    }
}
