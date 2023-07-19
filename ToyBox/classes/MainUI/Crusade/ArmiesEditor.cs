using Kingmaker;
using Kingmaker.Armies;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Armies.State;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Kingdom;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using ModKit;
using ModKit.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityModManagerNet;
using static ModKit.UI;

namespace ToyBox.classes.MainUI {
    public static class ArmiesEditor {
        public static Settings settings => Main.Settings;

        public static IEnumerable<(GlobalMapArmyState, float)> armies;
        public static IEnumerable<(GlobalMapArmyState, float)> playerArmies;
        public static IEnumerable<(GlobalMapArmyState, float)> demonArmies;
        public static string skillsSearchText = "";
        public static Browser<BlueprintUnit, BlueprintUnit> mercenaryBrowser = new(true, true);

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

        public static readonly object locker = new();
        public static Dictionary<int, bool> IsInMercenaryPool = new();
        public static Dictionary<int, bool> IsInRecruitPool = new();
        public static IEnumerable<BlueprintUnit> armyBlueprints;
        public static IEnumerable<BlueprintUnit> recruitPool;
        public static bool poolChanged = true;
        public static List<BlueprintUnit> mercenaryUnits;

        public static void LoadMercenaryData() {
            armyBlueprints = BlueprintExtensions.GetBlueprints<BlueprintUnit>().Where((u) => u.NameSafe().StartsWith("Army"));
            IEnumerable<BlueprintUnit> recruitPool = KingdomState.Instance.RecruitsManager.Pool.Select((r) => r.Unit);
            foreach (var entry in armyBlueprints) {
                IsInRecruitPool[entry.GetHashCode()] = recruitPool.Contains(entry);
                IsInMercenaryPool[entry.GetHashCode()] = KingdomState.Instance.MercenariesManager.HasUnitInPool(entry);
            }
            mercenaryBrowser?.ReloadData();
        }
        public static void AddAllCurrentUnits() {
            var armies = ArmiesByDistanceFromPlayer();
            if (armies is null) return;
            var playerArmies = from army in armies
                               where army.Item1.Data.Faction == ArmyFaction.Crusaders
                               select army;
            foreach (var army in playerArmies) {
                foreach (var squad in army.Item1.Data.Squads) {
                    var unit = squad.Unit.GetHashCode();
                    bool hasMercenary = IsInMercenaryPool.ContainsKey(unit) && IsInMercenaryPool[unit];
                    bool hasRecruit = IsInRecruitPool.ContainsKey(unit) && IsInRecruitPool[unit];
                    if (!hasMercenary && !hasRecruit) {
                        KingdomState.Instance.MercenariesManager.AddMercenary(squad.Unit, 1);
                    }
                }
                LoadMercenaryData();
            }
        }
        public static bool discloseMercenaryUnits = false;
        private const string RerollAll = "Reroll Mercs";
        private const string ApplyRecruitGrowth = "Add new recruits";

        public static void OnLoad() {
            KeyBindings.RegisterAction(RerollAll, () => {
                var mercenaryManager = KingdomState.Instance.MercenariesManager;
                mercenaryManager.CurrentSlots.RemoveAll((v) => true);
                mercenaryManager.RollSlots(mercenaryManager.MaxAllowedSlots - mercenaryManager.CurrentSlots.Count);
                EventBus.RaiseEvent<IArmyMercenarySlotsHandler>(delegate (IArmyMercenarySlotsHandler h) {
                    h.HandleSlotsRerolled();
                });
            });
            KeyBindings.RegisterAction(ApplyRecruitGrowth, () => {
                KingdomState.Instance.RecruitsManager.ApplyGrowth();
            });
        }
        public static void OnGUI() {
            if (Game.Instance?.Player == null) return;
            var kingdom = KingdomState.Instance;
            if (kingdom == null) {
                Label("You must unlock the crusade before you can access these toys.".localize().yellow().bold());
                return;
            }
            HStack("Tweaks".localize(), 1,
                () => Toggle("Infinite Mercenary Rerolls".localize(), ref settings.toggleInfiniteArmyRerolls),
                () => {
                    Toggle("Experimental - Enable Large Player Armies".localize(), ref settings.toggleLargeArmies);
                    if (settings.toggleLargeArmies) {
                        BlueprintRoot.Instance.Kingdom.StartArmySquadsCount = 14;
                        BlueprintRoot.Instance.Kingdom.MaxArmySquadsCount = 14;
                    }
                    else {
                        BlueprintRoot.Instance.Kingdom.StartArmySquadsCount = 4;
                        BlueprintRoot.Instance.Kingdom.MaxArmySquadsCount = 7;
                    }
                },
                () => Slider("Recruitment Cost".localize(), ref settings.recruitmentCost, 0f, 1f, 1f, 2, "", AutoWidth()),
                () => LogSlider("Number of Recruits at Refresh".localize(), ref settings.recruitmentMultiplier, 0f, 100, 1, 1, "",
                    AutoWidth()),
                () => LogSlider("Army Experience Multiplier".localize(), ref settings.armyExperienceMultiplier, 0f, 100, 1, 1, "",
                    AutoWidth()),
                () => LogSlider("After Army Battle Raise Multiplier".localize(), ref settings.postBattleSummonMultiplier, 0f, 100,
                    1, 1, "", AutoWidth()),
                () => Slider("Player Leader Ability Strength".localize(), ref settings.playerLeaderPowerMultiplier, 0f, 10f, 1f, 2, "", AutoWidth()),
                () => Slider("Enemy Leader Ability Strength".localize(), ref settings.enemyLeaderPowerMultiplier, 0f, 5f, 1f, 2, "", AutoWidth())
            );
            Div(0, 25);
            var mercenaryManager = KingdomState.Instance.MercenariesManager;
            var recruitsManager = KingdomState.Instance.RecruitsManager;
            if (poolChanged) {
                mercenaryUnits = mercenaryManager.Pool.Select((u) => u.Unit).ToList();
                foreach (var unit in mercenaryUnits) {
                    IsInMercenaryPool[unit.GetHashCode()] = true;
                }
                recruitPool = KingdomState.Instance.RecruitsManager.Pool.Select((r) => r.Unit);
                mercenaryUnits.AddRange(recruitPool);
            }
            HStack("Mercenaries".localize(),
                   1,
                   () => {
                       if (playerArmies != null) {
                           ActionButton("Add All Current Units".localize(), () => AddAllCurrentUnits(), 200.width());
                           110.space();
                           Label("Adds all currently active friendly units that are neither recruitable nor Mercanries to Mercenary units.".localize().green());
                       }
                       else {
                           Label("Need Armies To Add All Current Units".localize().yellow());
                           25.space();
                           HelpLabel("You should be on the global map and have the Crusade active".localize());
                       }
                   },
                   () => {
                       BindableActionButton(RerollAll, true, 200.width());
                       Space(-93);
                       Label("Rerolls Mercenary Units for free.".localize().green());
                   },
                   () => {
                       BindableActionButton(ApplyRecruitGrowth, true, 200.width());
                       Space(-93);
                       Label("Applies the regular recruits growth directly.".localize().green());
                   },
                   () => {
                       ValueAdjustorEditable("Mercenary Slots".localize(),
                                             () => mercenaryManager.MaxAllowedSlots,
                                             v => mercenaryManager.AddSlotsCount(v - mercenaryManager.MaxAllowedSlots),
                                             1,
                                             0,
                                             200);
                   },
                   () => {
                       using (VerticalScope()) {
                           Toggle("Add new units in friendly armies to Mercenary Pool if not Recruitable.".localize().cyan(), ref settings.toggleAddNewUnitsAsMercenaries, AutoWidth());
                           10.space();
                           Div();
                           15.space();
                       }
                   },
                   () => DisclosureToggle("Show Recruitment Pools".localize().Orange(), ref discloseMercenaryUnits),
                   () => {
                       if (discloseMercenaryUnits) {
                           using (VerticalScope()) {
                               mercenaryBrowser.OnGUI(
                                   mercenaryUnits,
                                   () => {
                                       if (armyBlueprints == null || armyBlueprints?.Count() == 0) {
                                           LoadMercenaryData();
                                       }
                                       return armyBlueprints;
                                   },
                                   (unit) => unit,
                                   (unit) => IsInRecruitPool.GetValueOrDefault(unit.GetHashCode(), false) ? unit.GetDisplayName().orange().bold() : unit.GetDisplayName(),
                                   (unit) => new[] { $"{unit.NameSafe()} {unit.GetDisplayName()} {unit.Description}" },
                                   () => {
                                       var bluh = ummWidth - 50;
                                       var titleWidth = (bluh / (IsWide ? 3.0f : 4.0f)) - 100;
                                       TitleLabel("Unit".localize(), Width((int)titleWidth));
                                       125.space();
                                       TitleLabel("Action".localize(), Width(210));
                                       20.space();
                                       TitleLabel("Pool".localize(), Width(200));
                                       20.space();
                                       TitleLabel("Recruitment Weight (Mercenary only)".localize(), AutoWidth());
                                   },
                                   (unit, _) => {
                                       var bluh = ummWidth - 50;
                                       var titleWidth = (bluh / (IsWide ? 3.0f : 4.0f)) - 100;
                                       bool isInMercPool = IsInMercenaryPool.GetValueOrDefault(unit.GetHashCode(), false);
                                       bool isInKingdomPool = IsInRecruitPool.GetValueOrDefault(unit.GetHashCode(), recruitPool.Contains(unit));
                                       var title = unit.GetDisplayName();
                                       if (isInKingdomPool)
                                           title = title.orange().bold();
                                       else if (isInMercPool)
                                           title = title.cyan().bold();
                                       Label(title, Width((int)titleWidth));
                                       ActionButton(isInMercPool ? "Rem Merc".localize() : "Add Merc".localize(),
                                                    () => {
                                                        mercenaryBrowser.needsReloadData = true;
                                                        if (isInMercPool) {
                                                            mercenaryManager.RemoveMercenary(unit);
                                                            isInMercPool = false;
                                                        }
                                                        else {
                                                            mercenaryManager.AddMercenary(unit, 1);
                                                            isInMercPool = true;
                                                        }
                                                        IsInMercenaryPool[unit.GetHashCode()] = isInMercPool;
                                                    },
                                                    150.width());
                                       10.space();
                                       ActionButton(isInKingdomPool ? "Rem Recruit".localize() : "Add Recruit".localize(),
                                                    () => {
                                                        mercenaryBrowser.needsReloadData = true;
                                                        if (isInKingdomPool) {
                                                            var count = recruitsManager.GetCountInPool(unit);
                                                            recruitsManager.DecreasePool(unit, count);
                                                            isInKingdomPool = false;
                                                        }
                                                        else {
                                                            var pool = recruitsManager.Pool;
                                                            var count = pool.Sum(r => r.Count) / pool.Count;
                                                            recruitsManager.IncreasePool(unit, count);
                                                            isInKingdomPool = true;
                                                        }
                                                        IsInRecruitPool[unit.GetHashCode()] = isInKingdomPool;
                                                    },
                                                    150.width());
                                       var poolText = $"{(isInMercPool ? "Merc".localize().cyan() : "")} {(isInKingdomPool ? ("Recruit".localize() + $" ({recruitsManager.GetCountInPool(unit)})").orange() : "")}".Trim();
                                       50.space();
                                       Label(poolText, Width(200));
                                       25.space();
                                       if (isInMercPool) {
                                           var poolInfo = mercenaryManager.Pool.FirstOrDefault(pi => pi.Unit == unit);
                                           if (poolInfo != null) {
                                               var weight = poolInfo.Weight;
                                               if (LogSliderCustomLabelWidth("Weight".localize(), ref weight, 0.01f, 1000, 1, 2, "", 70, AutoWidth())) {
                                                   poolInfo.UpdateWeight(weight);
                                               }
                                           }
                                           else {
                                               Label("Weird", AutoWidth());
                                           }
                                       }
                                   });
                           }
                       }
                   });

            Div(0, 25);

            if (armies == null)
                UpdateArmies();
            if (playerArmies != null)
                ArmiesGUI("Player Armies".localize(), playerArmies);
            if (playerArmies != null && demonArmies != null)
                Div(0, 25, 0);
            if (demonArmies != null)
                ArmiesGUI("Demon Armies".localize(), demonArmies);
        }
        public static void ArmiesGUI(string title, IEnumerable<(GlobalMapArmyState, float)> armies) {
            if (armies.Count() == 0) return;
            var selectedArmy = armySelection.GetValueOrDefault(title, null);
            using (VerticalScope()) {
                HStack(title, 1,
                    () => {
                        Label("Name".localize(), MinWidth(100), MaxWidth(250));
                        Label("Type".localize(), MinWidth(100), MaxWidth(250));
                        Label("Leader".localize(), Width(350));
                        Label("Squad Count".localize(), Width(150));
                        Space(55);
                        Label("Location".localize(), Width(400));
                        Space(25);
                        Label("Dist".localize());
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
                                    if (DisclosureToggle("Squads".localize(), ref showSquads, 125)) {
                                        selectedArmy = army == selectedArmy ? null : army;
                                        toggleStates[squads] = showSquads;

                                    }
                                    Space(50);
                                    var displayName = army.Location?.GetDisplayName() ?? "traveling on a path".localize();
                                    Label(displayName.yellow(), Width(400));
                                    Space(25);
                                    var distStr = distance >= 0 ? $"{distance:0.#}" : "-";
                                    Label(distStr, Width(50));
                                    Space(50);
                                    ActionButton("Teleport".localize(), () => TeleportToArmy(army), Width(150));
                                    Space(25);
                                    if (GlobalMapView.Instance != null) {
                                        ActionButton("Summon".localize(), () => SummonArmy(army), Width(150));
                                    }
                                    Space(25);
                                    if (army.Data.Faction == ArmyFaction.Crusaders) {
                                        ActionButton("Full MP".localize(), () => {
                                            var additionalMP = army.Data.GetArmyBonusSkills().Select(a => a.DailyMovementPoints);
                                            army.RestoreMovementPoints(40 + additionalMP.Sum());
                                        }, Width(150));
                                    }
                                    Space(25);
                                    ActionButton("Destroy".localize(), () => {
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
                                                Label("Stats".localize().yellow());
                                                ValueAdjuster("Level".localize().cyan(), () => leader.Level, (l) => leader.m_Level = l, 1, 0, 20, 375.width());
                                                ValueAdjustorEditable("Experience".localize().cyan(), () => leader.Experience, (e) => leader.m_Experience = e, 100, 0, int.MaxValue, 375.width());
                                                var stats = leader.Stats;
                                                ValueAdjuster("Attack Bonus".localize().cyan(),
                                                    () => stats.AttackBonus.BaseValue,
                                                    (v) => stats.AttackBonus.BaseValue = v, 1,
                                                    stats.AttackBonus.MinValue,
                                                    stats.AttackBonus.MaxValue,
                                                    Width(375));
                                                ValueAdjuster("Defense Bonus".localize().cyan(),
                                                    () => stats.DefenseBonus.BaseValue,
                                                    (v) => stats.DefenseBonus.BaseValue = v, 1,
                                                    stats.DefenseBonus.MinValue,
                                                    stats.DefenseBonus.MaxValue,
                                                    Width(375));
                                                ValueAdjuster("Infirmary Size".localize().cyan(),
                                                    () => stats.InfirmarySize.BaseValue,
                                                    (v) => stats.InfirmarySize.BaseValue = v, 25,
                                                    stats.InfirmarySize.MinValue,
                                                    stats.InfirmarySize.MaxValue, Width(375));
                                                ValueAdjuster("Mana".localize().cyan(),
                                                    () => stats.CurrentMana,
                                                    (v) => stats.CurrentMana = v, 5,
                                                    stats.MaxMana.MinValue,
                                                    stats.MaxMana.MaxValue,
                                                    Width(375));
                                                ValueAdjuster("Max Mana".localize().cyan(),
                                                    () => stats.MaxMana.BaseValue,
                                                    (v) => stats.MaxMana.BaseValue = v, 5,
                                                    stats.MaxMana.MinValue,
                                                    stats.MaxMana.MaxValue,
                                                    Width(375));
                                                ValueAdjuster("Mana Regen".localize().cyan(),
                                                    () => stats.ManaRegeneration.BaseValue,
                                                    (v) => stats.ManaRegeneration.BaseValue = v, 1,
                                                    stats.ManaRegeneration.MinValue,
                                                    stats.ManaRegeneration.MaxValue,
                                                    Width(375));
                                                ValueAdjuster("Spell Strength".localize().cyan(),
                                                    () => stats.SpellStrength.BaseValue,
                                                    (v) => stats.SpellStrength.BaseValue = v, 1,
                                                    stats.SpellStrength.MinValue,
                                                    stats.SpellStrength.MaxValue, Width(375));
                                            }
                                        }
                                        using (HorizontalScope()) {
                                            Space(100);
                                            Label("Skills".localize().yellow(), Width(85));
                                            if (DisclosureToggle("Show All".localize().orange().bold(), ref showAllLeaderSkills, 125)) {
                                                toggleStates[leader.Skills] = showAllLeaderSkills;
                                            }
                                            //UI.Space(285);
                                            //UI.Label("Action".yellow(), UI.Width(150));
                                        }
                                        using (HorizontalScope()) {
                                            Space(100);
                                            ActionTextField(ref skillsSearchText, "Search".localize(), (s) => { }, () => { }, 235.width());
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
                                                        ActionButton("Remove".localize(), () => { skillToRemove = skill; }, Width(150));
                                                    else
                                                        ActionButton("Add".localize(), () => { skillToAdd = skill; }, Width(150));
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
                                            Label("Squad Name".localize().yellow(), Width(475));
                                            Space(25);
                                            Label("Unit Count".localize().yellow(), Width(250));
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
                                                ActionButton("Remove".localize(), () => {
                                                    squadToRemove = squad;
                                                }, Width(150));
                                            }
                                        }
                                        if (title == "Player Armies".localize()) {
                                            if (DisclosureToggle("Add Squads".localize().Yellow(), ref showAddSquad, 125)) {
                                                toggleShowSquadStates[squads] = showAddSquad;
                                            }
                                        }
                                        if (showAddSquad) {
                                            Div(0, 10);
                                            var count = 0;
                                            var kingdom = KingdomState.Instance;
                                            var mercenariesManager = kingdom.MercenariesManager;
                                            var mercenariesPool = mercenariesManager.Pool;
                                            var recruitManager = kingdom.RecruitsManager;
                                            var growthPool = recruitManager.Growth;
                                            using (VerticalScope()) {
                                                using (HorizontalScope()) {
                                                    Label("Unit Count".localize().cyan(), AutoWidth());
                                                    count = IntTextField(ref settings.unitCount, null, Width(150));
                                                }

                                                foreach (var poolInfo in mercenariesPool) {
                                                    var unit = poolInfo.Unit;
                                                    using (HorizontalScope()) {
                                                        Label(unit.NameSafe(), Width(520));
                                                        ActionButton("Add".localize(),
                                                            () => {
                                                                squadToAdd = unit;
                                                            }, Width(150));
                                                    }
                                                }
                                                foreach (var poolInfo in growthPool) {
                                                    var unit = poolInfo.Unit;
                                                    using (HorizontalScope()) {
                                                        Label(unit.NameSafe(), Width(520));
                                                        ActionButton("Add".localize(),
                                                            () => {
                                                                squadToAdd = unit;
                                                            }, Width(150));
                                                    }
                                                }
                                            }
                                            if (squadToAdd != null) {
                                                var merge = false;
                                                foreach (var squad in army.Data.Squads) {
                                                    if (squad.Unit.NameSafe() == squadToAdd.NameSafe()) {
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
            results = results.OrderBy(r => r.Item2).ThenBy(r => r.army.Location?.GetDisplayName() ?? "traveling on a path".localize());
            return results;
        }
    }
}
