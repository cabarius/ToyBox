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
using Kingmaker.Kingdom.Armies;
using ModKit;
using static ModKit.UI;
using ModKit.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace ToyBox.classes.MainUI {
    public static class ArmiesEditor {
        public static Settings settings => Main.settings;

        public static IEnumerable<(GlobalMapArmyState, float)> armies;
        public static IEnumerable<(GlobalMapArmyState, float)> playerArmies;
        public static IEnumerable<(GlobalMapArmyState, float)> demonArmies;
        public static string skillsSearchText = "";


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
        private static Dictionary<object, bool> toggleShowSquadStates = new();
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
            var kingdom = KingdomState.Instance;
            if (kingdom == null) {
                Label("You must unlock the crusade before you can access these toys.".yellow().bold());
                return;
            }
            if (allLeaderSkills == null) GetAllLeaderSkills();
            HStack("Tweaks", 1,
                () => Toggle("Infinite Mercenary Rerolls", ref settings.toggleInfiniteArmyRerolls),
                () => {
                    Toggle("Experimental - Enable Large Player Armies", ref settings.toggleLargeArmies);
                    if (settings.toggleLargeArmies) {
                        BlueprintRoot.Instance.Kingdom.StartArmySquadsCount = 14;
                        BlueprintRoot.Instance.Kingdom.MaxArmySquadsCount = 14;
                    }
                    else {
                        BlueprintRoot.Instance.Kingdom.StartArmySquadsCount = 4;
                        BlueprintRoot.Instance.Kingdom.MaxArmySquadsCount = 7;
                    }
                },
                () => Slider("Recruitment Cost", ref settings.recruitmentCost, 0f, 1f, 1f, 2, "", AutoWidth()),
                () => LogSlider("Number of Recruits", ref settings.recruitmentMultiplier, 0f, 100, 1, 1, "",
                    AutoWidth()),
                () => LogSlider("Army Experience Multiplier", ref settings.armyExperienceMultiplier, 0f, 100, 1, 1, "",
                    AutoWidth()),
                () => LogSlider("After Army Battle Raise Multiplier", ref settings.postBattleSummonMultiplier, 0f, 100,
                    1, 1, "", AutoWidth()),
                () => Slider("Player Leader Ability Strength", ref settings.playerLeaderPowerMultiplier, 0f, 10f, 1f, 2, "", AutoWidth()),
                () => Slider("Enemy Leader Ability Strength", ref settings.enemyLeaderPowerMultiplier, 0f, 5f, 1f, 2, "", AutoWidth())
            );
            Div(0, 25);

            if (armies == null)
                UpdateArmies();
            if (playerArmies != null)
                ArmiesGUI("Player Armies", playerArmies);
            if (playerArmies != null && demonArmies != null) 
                Div(0, 25, 0);
            if (demonArmies != null)
                ArmiesGUI("Demon Armies", demonArmies);
        }
        public static void ArmiesGUI(string title, IEnumerable<(GlobalMapArmyState, float)> armies) {
            if (armies.Count() == 0) return;
            var selectedArmy = armySelection.GetValueOrDefault(title, null);
            using (VerticalScope()) {
                HStack(title, 1,
                    () => {
                        Label("Name", MinWidth(100), MaxWidth(250));
                        Label("Type", MinWidth(100), MaxWidth(250));
                        Label("Leader", Width(350));
                        Label("Squad Count", Width(150));
                        Space(55);
                        Label("Location", Width(400));
                        Space(25);
                        Label("Dist");
                    },
                    () => {
                        using (VerticalScope()) {
                            var last = armies.Last().Item1;
                            foreach (var armyEntry in armies) {
                                var showLeader = false;
                                var showSquads = false;
                                var showAllLeaderSkills = false;
                                var showAllRituals = false;
                                var army = armyEntry.Item1;
                                var leader = army.Data.Leader;
                                var distance = armyEntry.Item2;
                                var showAddSquad = false;
                                BlueprintUnit squadToAdd = null;

                                using (HorizontalScope()) {
                                    Label(army.Data.ArmyName.ToString().orange().bold(), MinWidth(100), MaxWidth(250));
                                    Label(army.ArmyType.ToString().cyan(), MinWidth(100), MaxWidth(250));
                                    if (leader != null) {
                                        showLeader = toggleStates.GetValueOrDefault(leader, false);
                                        if (DisclosureToggle(leader.LocalizedName, ref showLeader, 350)) {
                                            selectedArmy = army == selectedArmy ? null : army;
                                            toggleStates[leader] = showLeader;
                                        }
                                    }
                                    else Space(353);
                                    var squads = army.Data.Squads;
                                    Label(squads.Count.ToString().cyan(), Width(35));
                                    showSquads = toggleStates.GetValueOrDefault(squads, false);
                                    if (DisclosureToggle("Squads", ref showSquads, 125)) {
                                        selectedArmy = army == selectedArmy ? null : army;
                                        toggleStates[squads] = showSquads;

                                    }
                                    Space(50);
                                    var displayName = army.Location?.GetDisplayName() ?? "traveling on a path";
                                    Label(displayName.yellow(), Width(400));
                                    Space(25);
                                    var distStr = distance >= 0 ? $"{distance:0.#}" : "-";
                                    Label(distStr, Width(50));
                                    Space(50);
                                    ActionButton("Teleport", () => TeleportToArmy(army), Width(150));
                                    Space(25);
                                    if (GlobalMapView.Instance != null) {
                                        ActionButton("Summon", () => SummonArmy(army), Width(150));
                                    }
                                    Space(25);
                                    if (army.Data.Faction == ArmyFaction.Crusaders) {
                                        ActionButton("Full MP", () => {
                                            var additionalMP = army.Data.GetArmyBonusSkills().Select(a => a.DailyMovementPoints);
                                            army.RestoreMovementPoints(40 + additionalMP.Sum());
                                        }, Width(150));
                                    }
                                    Space(25);
                                    ActionButton("Destroy", () => {
                                        // army.Data.RemoveAllSquads();
                                        Game.Instance.Player.GlobalMap.LastActivated.DestroyArmy(army);
                                        UpdateArmies();
                                    }, Width(150));
                                }
                                if (showLeader) {
                                    Div(0, 10);
                                    showAllLeaderSkills = toggleStates.GetValueOrDefault(leader.Skills, false);
                                    showAllRituals = toggleStates.GetValueOrDefault(leader.m_RitualSlots, false);
                                    using (VerticalScope()) {
                                        using (HorizontalScope()) {
                                            Space(100);
                                            using (VerticalScope()) {
                                                Label("Stats".yellow());
                                                ValueAdjuster("Level".cyan(), () => leader.Level, (l) => leader.m_Level = l, 1, 0, 20, 375.width());
                                                var stats = leader.Stats;
                                                ValueAdjuster("Attack Bonus".cyan(), () => stats.AttackBonus.BaseValue, (v) => stats.AttackBonus.BaseValue = v, 1, stats.AttackBonus.MinValue, stats.AttackBonus.MaxValue, Width(375));
                                                ValueAdjuster("Defense Bonus".cyan(), () => stats.DefenseBonus.BaseValue, (v) => stats.DefenseBonus.BaseValue = v, 1, stats.DefenseBonus.MinValue, stats.DefenseBonus.MaxValue, Width(375));
                                                ValueAdjuster("Infirmary Size".cyan(), () => stats.InfirmarySize.BaseValue, (v) => stats.InfirmarySize.BaseValue = v, 25, stats.InfirmarySize.MinValue, stats.InfirmarySize.MaxValue, Width(375));
                                                ValueAdjuster("Max Mana".cyan(), () => stats.MaxMana.BaseValue, (v) => stats.MaxMana.BaseValue = v, 5, stats.MaxMana.MinValue, stats.MaxMana.MaxValue, Width(375));
                                                ValueAdjuster("Mana Regen".cyan(), () => stats.ManaRegeneration.BaseValue, (v) => stats.ManaRegeneration.BaseValue = v, 1, stats.ManaRegeneration.MinValue, stats.ManaRegeneration.MaxValue, Width(375));
                                                ValueAdjuster("Spell Strength".cyan(), () => stats.SpellStrength.BaseValue, (v) => stats.SpellStrength.BaseValue = v, 1, stats.SpellStrength.MinValue, stats.SpellStrength.MaxValue, Width(375));
                                            }
                                        }
                                        using (HorizontalScope()) {
                                            Space(100);
                                            Label("Skills".yellow(), Width(85));
                                            if (DisclosureToggle("Show All".orange().bold(), ref showAllLeaderSkills, 125)) {
                                                toggleStates[leader.Skills] = showAllLeaderSkills;
                                            }
                                            //UI.Space(285);
                                            //UI.Label("Action".yellow(), UI.Width(150));
                                        }
                                        using (HorizontalScope()) {
                                            Space(100);
                                            ActionTextField(ref skillsSearchText, "Search", (s) => { }, () => { }, 235.width());
                                        }
                                        var skills = showAllLeaderSkills ? GetAllLeaderSkills() : leader.Skills;
                                        if (skillsSearchText.Length > 0) {
                                            var searchText = skillsSearchText.ToLower();
                                            skills = skills.Where(
                                                skill => skill.LocalizedName.ToString().ToLower().Contains(searchText)
                                                         || skill.LocalizedDescription.ToString().ToLower().Contains(searchText));
                                        }
                                        BlueprintLeaderSkill skillToAdd = null;
                                        BlueprintLeaderSkill skillToRemove = null;
                                        if (skills != null)
                                            foreach (var skill in skills) {
                                                var leaderHasSkill = leader.Skills.Contains(skill);
                                                using (HorizontalScope()) {
                                                    Space(100);
                                                    var skillName = (string)skill.LocalizedName;
                                                    if (leaderHasSkill) skillName = skillName.cyan();
                                                    Label(skillName, Width(375));
                                                    Space(25);
                                                    if (leaderHasSkill)
                                                        ActionButton("Remove", () => { skillToRemove = skill; }, Width(150));
                                                    else
                                                        ActionButton("Add", () => { skillToAdd = skill; }, Width(150));
                                                    Space(100);
                                                    var description = (string)skill.LocalizedDescription;
                                                    Label(description.StripHTML().green());
                                                }
                                            }
                                        if (skillToAdd != null) leader.AddSkill(skillToAdd, true);
                                        if (skillToRemove != null) leader.RemoveSkill(skillToRemove);
#if false
                                        using (HorizontalScope()) {
                                            Space(100);
                                            Label("Rituals".yellow(), Width(85));
                                            if (DisclosureToggle("Show All".orange().bold(), ref showAllRituals, 125)) {
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
                                                using (HorizontalScope()) {
                                                    Space(100);
                                                    var name = (string)ability.name;
                                                    if (leaderHasAbility) name = name.cyan();
                                                    Label(name, Width(375));
                                                    Space(25);
                                                    if (leaderHasAbility)
                                                        ActionButton("Remove", () => { }, Width(150));
                                                    else if (canAdd)
                                                        ActionButton("Add", () => { }, Width(150));
                                                    else Space(153);
                                                    Space(100);
                                                    var description = (string)ability.GetDescription();
                                                    Label(description.StripHTML().green());
                                                }
                                            }
                                        }
#endif
                                        if (!showSquads)
                                            Div(0, 10);
                                    }
                                }
                                if (showSquads) {
                                    Div(0, 10);
                                    using (VerticalScope()) {
                                        using (HorizontalScope()) {
                                            Label("Squad Name".yellow(), Width(475));
                                            Space(25);
                                            Label("Unit Count".yellow(), Width(250));
                                        }
                                    }
                                    using (VerticalScope()) {
                                        var squads = army.Data.m_Squads;
                                        SquadState squadToRemove = null;
                                        showAddSquad = toggleShowSquadStates.GetValueOrDefault(squads, false);
                                        foreach (var squad in squads) {
                                            using (HorizontalScope()) {
                                                Label(squad.Unit.NameSafe(), Width(475));
                                                Space(25);
                                                var count = squad.Count;
                                                ActionIntTextField(ref count,
                                                    (value) => {
                                                        squad.SetCount(value);
                                                    }, Width(225)
                                                );
                                                Space(25);
                                                ActionButton("Remove", () => {
                                                    squadToRemove = squad;
                                                }, Width(150));
                                            }
                                        }
                                        if (title == "Player Armies") {
                                            if (DisclosureToggle("Add Squads", ref showAddSquad, 125)) {
                                                toggleShowSquadStates[squads] = showAddSquad;
                                            }
                                        }
                                        if(showAddSquad) {
                                            Div(0, 10);
                                            var count = 0;
                                            var kingdom = KingdomState.Instance;
                                            var mercenariesManager = kingdom.MercenariesManager;
                                            var mercenariesPool = mercenariesManager.Pool;
                                            var recruitManager = kingdom.RecruitsManager;
                                            var growthPool = recruitManager.Growth;
                                            using (VerticalScope()) {
                                                using (HorizontalScope()) { 
                                                Label("Unit Count".cyan(), AutoWidth());
                                                count = IntTextField(ref settings.unitCount, null, Width(150));
                                                }

                                                foreach (var poolInfo in mercenariesPool) {
                                                    var unit = poolInfo.Unit;
                                                    using (HorizontalScope()) {
                                                        Label(unit.NameSafe(), Width(520));
                                                        ActionButton("Add",
                                                            () => {
                                                                squadToAdd = unit;
                                                            }, Width(150));
                                                    }
                                                }
                                                foreach (var poolInfo in growthPool) {
                                                    var unit = poolInfo.Unit;
                                                    using (HorizontalScope()) {
                                                        Label(unit.NameSafe(), Width(520));
                                                        ActionButton("Add",
                                                            () => {
                                                                squadToAdd = unit;
                                                            }, Width(150));
                                                    }
                                                }
                                            }
                                            if (squadToAdd != null) {
                                                var merge = false;
                                                foreach(var squad in army.Data.Squads) {
                                                    if(squad.Unit.NameSafe() == squadToAdd.NameSafe()) {
                                                        merge = true;
                                                        break;
                                                    }
                                                }
                                                army.Data.Add(squadToAdd, count, merge, null);
                                            }
                                        }
                                       
                                        if (squadToRemove != null) {
                                            squadToRemove.Army.RemoveSquad(squadToRemove);
                                        }
                                    }
                                    if (army != last)
                                        Div();
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
            if (!Main.IsInGame || GlobalMapView.Instance is null) return null;
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
