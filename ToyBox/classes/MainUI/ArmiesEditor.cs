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

        public static IEnumerable<GlobalMapArmyState> armies;
        public static IEnumerable<GlobalMapArmyState> playerArmies;
        public static IEnumerable<GlobalMapArmyState> demonArmies;
        public static void OnShowGUI() {
            UpdateArmies();
        }
        public static void UpdateArmies() {
            armies = ArmiesByDistanceFromPlayer()?.ToList();
            if (armies != null) {
                playerArmies = from army in armies
                               where army.Data.Faction == ArmyFaction.Crusaders
                               select army;
                demonArmies = from army in armies
                              where army.Data.Faction == ArmyFaction.Demons
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
        public static void ArmiesGUI(string title, IEnumerable<GlobalMapArmyState> armies) {
            var selectedArmy = armySelection.GetValueOrDefault(title, null);
            using (UI.VerticalScope()) {
                UI.HStack(title, 1,
                    () => {
                        UI.Label("Name", UI.MinWidth(100), UI.MaxWidth(250));
                        UI.Label("Type", UI.MinWidth(100), UI.MaxWidth(250));
                        UI.Label("Squad Count", UI.MinWidth(100), UI.MaxWidth(250));
                    },
                    () => {
                        using (UI.VerticalScope()) {
                            var last = armies.Last();
                            foreach (var army in armies) {
                                bool showSquads = false;
                                using (UI.HorizontalScope()) {
                                    UI.Label(army.Data.ArmyName.ToString().orange().bold(), UI.MinWidth(100), UI.MaxWidth(250));
                                    UI.Label(army.ArmyType.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(250));
                                    UI.Label(army.Data.Squads.Count.ToString().cyan(), UI.Width(35));
                                    showSquads = army == selectedArmy;
                                    if (UI.DisclosureToggle("Squads", ref showSquads, 125)) {
                                        selectedArmy = selectedArmy == army ? null : army;
                                    }
                                    UI.Space(50);
                                    UI.Label(army.Location.GetDisplayName().yellow(), UI.Width(400));
                                    UI.Space(25);
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
                                        UI.Label("Squad Name".yellow(), UI.Width(250));
                                        UI.Label("Unit Count".yellow(), UI.Width(250));
                                        UI.EndHorizontal();
                                    }

                                    using (UI.VerticalScope()) {
                                        var squads = selectedArmy.Data.m_Squads;
                                        SquadState squadToRemove = null;
                                        foreach (var squad in squads) {
                                            using (UI.HorizontalScope()) {
                                                UI.Label(squad.Unit.NameSafe(), UI.Width(250));
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
                                        UI.Div(0, 10);
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

        public static void SummonArmy(GlobalMapArmyState army) {

        }
        public static void TeleportToArmy(GlobalMapArmyState army) {
            var mapPoint = army.Position.Location;
            UnityModManager.UI.Instance.ToggleWindow();
            if (!Teleport.TeleportToGlobalMapPoint(mapPoint)) {
                Teleport.TeleportToGlobalMap(() => Teleport.TeleportToGlobalMapPoint(mapPoint));
            }
        }
        public static IEnumerable<GlobalMapArmyState> ArmiesByDistanceFromPlayer() {
            if (!Main.IsInGame) return null;
            var player = Game.Instance?.Player ?? null;
            var globalMapHelper = player?.GlobalMap;
            if (globalMapHelper == null) return null;
            var position = globalMapHelper.CurrentPosition;
            if (position == null) return globalMapHelper.LastActivated.Armies.OrderBy(a => a.Location.GetDisplayName());
            var globalMap = player.GetGlobalMap(position.Location.GlobalMap);
            var source = Game.Instance.Player.AllGlobalMaps
                .Where<GlobalMapState>(_globalMap => _globalMap.Blueprint == position.Location.GlobalMap)
                .SelectMany<GlobalMapState, GlobalMapArmyState>(
                    _globalMap => (IEnumerable<GlobalMapArmyState>)_globalMap.Armies
                    )
                //.Where<GlobalMapArmyState>(_army => _army.Data.Faction == this.Faction)
                .Select<GlobalMapArmyState, (GlobalMapArmyState, float)>(_army => {
                    var globalMapArmyState = _army;
                    float? length = globalMap?.PathManager?.CalculateArmyPathToPosition(_army, position)?.GetLength(false);
                    double num = length.HasValue ? (double)length.GetValueOrDefault() : -1.0;
                    return (globalMapArmyState, (float)num);
                });
            return source.OrderBy(t => t.Item2).Select(t => t.Item1);
        }
    }
}
