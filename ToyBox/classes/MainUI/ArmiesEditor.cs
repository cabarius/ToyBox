using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Armies.State;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using ModKit;
using ModKit.Utility;
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

        private static readonly Dictionary<string, GlobalMapArmyState> armySelection = new();
        private static Dictionary<object, bool> toggleStates = new();
        public static IEnumerable<BlueprintLeaderSkill> allLeaderSkills;
        public static IEnumerable<BlueprintLeaderSkill> GetAllLeaderSkills() {
            if (allLeaderSkills != null) return allLeaderSkills;
            else {
                allLeaderSkills = BlueprintLoader.Shared.GetBlueprints<BlueprintLeaderSkill>();
                return allLeaderSkills;
            }
        }
        public static IEnumerable<BlueprintAbility> allLeaderAbilities;
        public static IEnumerable<BlueprintAbility> GetAllLeaderAbilities() {
            if (allLeaderAbilities != null) return allLeaderAbilities;
            else {
                allLeaderAbilities = BlueprintLoader.Shared.GetBlueprints<BlueprintAbility>();
                return allLeaderAbilities;
            }
        }

        public static void OnGUI() {
            if (allLeaderSkills == null) GetAllLeaderSkills();
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
                        UI.Label("Leader", UI.Width(350));
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
                                var showLeader = false;
                                var showSquads = false;
                                var showAllLeaderSkills = false;
                                var showAllRituals = false;
                                var army = armyEntry.Item1;
                                var leader = army.Data.Leader;
                                var distance = armyEntry.Item2;
                                using (UI.HorizontalScope()) {
                                    UI.Label(army.Data.ArmyName.ToString().orange().bold(), UI.MinWidth(100), UI.MaxWidth(250));
                                    UI.Label(army.ArmyType.ToString().cyan(), UI.MinWidth(100), UI.MaxWidth(250));
                                    if (leader != null) {
                                        showLeader = toggleStates.GetValueOrDefault(leader, false);
                                        if (UI.DisclosureToggle(leader.LocalizedName, ref showLeader, 350)) {
                                            selectedArmy = army == selectedArmy ? null : army;
                                            toggleStates[leader] = showLeader;
                                        }
                                    }
                                    else UI.Space(353);
                                    var squads = army.Data.Squads;
                                    UI.Label(squads.Count.ToString().cyan(), UI.Width(35));
                                    showSquads = toggleStates.GetValueOrDefault(squads, false);
                                    if (UI.DisclosureToggle("Squads", ref showSquads, 125)) {
                                        selectedArmy = army == selectedArmy ? null : army;
                                        toggleStates[squads] = showSquads;

                                    }
                                    UI.Space(50);
                                    var displayName = army.Location?.GetDisplayName() ?? "traveling on a path";
                                    UI.Label(displayName.yellow(), UI.Width(400));
                                    UI.Space(25);
                                    var distStr = distance >= 0 ? $"{distance:0.#}" : "-";
                                    UI.Label(distStr, UI.Width(50));
                                    UI.Space(50);
                                    UI.ActionButton("Teleport", () => TeleportToArmy(army), UI.Width(150));
                                    UI.Space(25);
                                    if (GlobalMapView.Instance != null) {
                                        UI.ActionButton("Summon", () => SummonArmy(army), UI.Width(150));
                                    }
                                    UI.Space(25);
                                    if (army.Data.Faction == ArmyFaction.Crusaders) {
                                        UI.ActionButton("Full MP", () => {
                                            var additionalMP = army.Data.GetArmyBonusSkills().Select(a => a.DailyMovementPoints);
                                            army.RestoreMovementPoints(40+additionalMP.Sum());
                                        }, UI.Width(150));
                                    }
                                    UI.Space(25);
                                    UI.ActionButton("Destroy", () => {
                                       // army.Data.RemoveAllSquads();
                                        Game.Instance.Player.GlobalMap.LastActivated.DestroyArmy(army);
                                        UpdateArmies();
                                    }, UI.Width(150));


                                }
                                if (showLeader) {
                                    UI.Div(0, 10);
                                    showAllLeaderSkills = toggleStates.GetValueOrDefault(leader.Skills, false);
                                    showAllRituals = toggleStates.GetValueOrDefault(leader.m_RitualSlots, false);
                                    using (UI.VerticalScope()) {
                                        using (UI.HorizontalScope()) {
                                            UI.Space(100);
                                            using (UI.VerticalScope()) {
                                                UI.Label("Stats".yellow());
                                                var stats = leader.Stats;
                                                UI.ValueAdjuster("Attack Bonus".cyan(), () => stats.AttackBonus.BaseValue, (v) => stats.AttackBonus.BaseValue = v, 1, stats.AttackBonus.MinValue, stats.AttackBonus.MaxValue, UI.Width(375));
                                                UI.ValueAdjuster("Defense Bonus".cyan(), () => stats.DefenseBonus.BaseValue, (v) => stats.DefenseBonus.BaseValue = v, 1, stats.DefenseBonus.MinValue, stats.DefenseBonus.MaxValue, UI.Width(375));
                                                UI.ValueAdjuster("Infirmary Size".cyan(), () => stats.InfirmarySize.BaseValue, (v) => stats.InfirmarySize.BaseValue = v, 25, stats.InfirmarySize.MinValue, stats.InfirmarySize.MaxValue, UI.Width(375));
                                                UI.ValueAdjuster("Max Mana".cyan(), () => stats.MaxMana.BaseValue, (v) => stats.MaxMana.BaseValue = v, 5, stats.MaxMana.MinValue, stats.MaxMana.MaxValue, UI.Width(375));
                                                UI.ValueAdjuster("Mana Regen".cyan(), () => stats.ManaRegeneration.BaseValue, (v) => stats.ManaRegeneration.BaseValue = v, 1, stats.ManaRegeneration.MinValue, stats.ManaRegeneration.MaxValue, UI.Width(375));
                                                UI.ValueAdjuster("Spell Strength".cyan(), () => stats.SpellStrength.BaseValue, (v) => stats.SpellStrength.BaseValue = v, 1, stats.SpellStrength.MinValue, stats.SpellStrength.MaxValue, UI.Width(375));
                                            }
                                        }
                                        using (UI.HorizontalScope()) {
                                            UI.Space(100);
                                            UI.Label("Skills".yellow(), UI.Width(85));
                                            if (UI.DisclosureToggle("Show All".orange().bold(), ref showAllLeaderSkills, 125)) {
                                                toggleStates[leader.Skills] = showAllLeaderSkills;
                                            }
                                            //UI.Space(285);
                                            //UI.Label("Action".yellow(), UI.Width(150));
                                        }
                                        var skills = showAllLeaderSkills ? GetAllLeaderSkills() : leader.Skills;
                                        BlueprintLeaderSkill skillToAdd = null;
                                        BlueprintLeaderSkill skillToRemove = null;
                                        if (skills != null)
                                            foreach (var skill in skills) {
                                                var leaderHasSkill = leader.Skills.Contains(skill);
                                                using (UI.HorizontalScope()) {
                                                    UI.Space(100);
                                                    var skillName = (string)skill.LocalizedName;
                                                    if (leaderHasSkill) skillName = skillName.cyan();
                                                    UI.Label(skillName, UI.Width(375));
                                                    UI.Space(25);
                                                    if (leaderHasSkill)
                                                        UI.ActionButton("Remove", () => { skillToRemove = skill; }, UI.Width(150));
                                                    else
                                                        UI.ActionButton("Add", () => { skillToAdd = skill; }, UI.Width(150));
                                                    UI.Space(100);
                                                    var description = (string)skill.LocalizedDescription;
                                                    UI.Label(description.StripHTML().green());
                                                }
                                            }
                                        if (skillToAdd != null) leader.AddSkill(skillToAdd, true);
                                        if (skillToRemove != null) leader.RemoveSkill(skillToRemove);
#if DEBUG
                                        using (UI.HorizontalScope()) {
                                            UI.Space(100);
                                            UI.Label("Rituals".yellow(), UI.Width(85));
                                            if (UI.DisclosureToggle("Show All".orange().bold(), ref showAllRituals, 125)) {
                                                toggleStates[leader.m_RitualSlots] = showAllRituals;
                                            }
                                            //UI.Space(285);
                                            //UI.Label("Action".yellow(), UI.Width(150));
                                        }

                                        var leaderAbilities = leader.m_RitualSlots.Select(s => (BlueprintAbility)s).Where(s => s != null);
                                        var abilities = showAllRituals ? GetAllLeaderAbilities() : leaderAbilities;
                                        if (abilities != null) {
                                            var canAdd = leaderAbilities.Count() < 14;
                                            foreach (var ability in abilities) {
                                                var leaderHasAbility = leaderAbilities.Contains(ability);
                                                using (UI.HorizontalScope()) {
                                                    UI.Space(100);
                                                    var name = (string)ability.name;
                                                    if (leaderHasAbility) name = name.cyan();
                                                    UI.Label(name, UI.Width(375));
                                                    UI.Space(25);
                                                    if (leaderHasAbility)
                                                        UI.ActionButton("Remove", () => { }, UI.Width(150));
                                                    else if (canAdd)
                                                        UI.ActionButton("Add", () => { }, UI.Width(150));
                                                    else UI.Space(153);
                                                    UI.Space(100);
                                                    var description = (string)ability.GetDescription();
                                                    UI.Label(description.StripHTML().green());
                                                }
                                            }
                                        }
#endif
                                        if (!showSquads)
                                            UI.Div(0, 10);
                                    }
                                }
                                if (showSquads) {
                                    UI.Div(0, 10);
                                    using (UI.VerticalScope()) {
                                        using (UI.HorizontalScope()) {
                                            UI.Label("Squad Name".yellow(), UI.Width(475));
                                            UI.Space(25);
                                            UI.Label("Unit Count".yellow(), UI.Width(250));
                                        }
                                    }
                                    using (UI.VerticalScope()) {
                                        var squads = army.Data.m_Squads;
                                        SquadState squadToRemove = null;
                                        foreach (var squad in squads) {
                                            using (UI.HorizontalScope()) {
                                                UI.Label(squad.Unit.NameSafe(), UI.Width(475));
                                                UI.Space(25);
                                                var count = squad.Count;
                                                UI.ActionIntTextField(ref count,
                                                    (value) => {
                                                        squad.SetCount(value);
                                                    }, UI.Width(225)
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
            if (mainMapState == null) return null;
            var armies = mainMapState.Armies;
            var position = mainMapState.PlayerPosition;
            var results = from army in armies select (army, mainMapState.ArmyDistance(army, position));
            results = from item in results where item.Item2 >= 0 || item.army.IsRevealed select item;
            results = results.OrderBy(r => r.Item2).ThenBy(r => r.army.Location?.GetDisplayName() ?? "traveling on a path");
            return results;
        }
    }
}
