// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace ToyBox {
    public class PartyEditor {
        enum ToggleChoice {
            Classes,
            Stats,
            Facts,
            Buffs,
            Abilities,
            Spells,
            None,
        };

        static ToggleChoice selectedToggle = ToggleChoice.None;
        static int selectedCharacterIndex = 0;

        static int selectedSpellbook = 0;
        static int selectedSpellbookLevel = 0;
        static float nearByRange = 25;
        private static NamedFunc<List<UnitEntityData>>[] partyFilterChoices = null;
        public static NamedFunc<List<UnitEntityData>>[] GetPartyFilterChoices() {
            if (Game.Instance.Player != null && partyFilterChoices == null) {
            return new NamedFunc<List<UnitEntityData>>[] {
                    new NamedFunc<List<UnitEntityData>>("Party", () => Game.Instance.Player.Party),
                    new NamedFunc<List<UnitEntityData>>("Party & Pets", () => Game.Instance.Player.m_PartyAndPets),
                    new NamedFunc<List<UnitEntityData>>("All Characters", () => Game.Instance.Player.AllCharacters),
                    new NamedFunc<List<UnitEntityData>>("Active Companions", () => Game.Instance.Player.ActiveCompanions),
                    new NamedFunc<List<UnitEntityData>>("Remote Companions", () => Game.Instance.Player.m_RemoteCompanions),
                    new NamedFunc<List<UnitEntityData>>("Custom (Mercs)", PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<UnitEntityData>>("Pets", PartyUtils.GetPets),
                    new NamedFunc<List<UnitEntityData>>("Nearby", () => {
                        var player = GameHelper.GetPlayerCharacter();
                        if (player == null) return new List<UnitEntityData> ();
                        return GameHelper.GetTargetsAround(GameHelper.GetPlayerCharacter().Position, nearByRange , false, false).ToList();
                    }),
                    new NamedFunc<List<UnitEntityData>>("Friendly", () => Game.Instance.State.Units.Where((u) => u != null && !u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("Enemies", () => Game.Instance.State.Units.Where((u) => u != null && u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("All Units", () => Game.Instance.State.Units.ToList()),
               };
            }
            return partyFilterChoices;
        }
        public static List<UnitEntityData> GetCharacterList() {
            var partyFilterChoices = GetPartyFilterChoices();
            if (partyFilterChoices == null) { return null; }
            return partyFilterChoices[Main.settings.selectedPartyFilter].func();
        }
        static UnitEntityData GetSelectedCharacter() {
            var characterList = GetCharacterList();
            if (characterList == null || characterList.Count == 0) return null;
            if (selectedCharacterIndex >= characterList.Count) selectedCharacterIndex = 0;
            return characterList[selectedCharacterIndex];
        }
        public static void ResetGUI() {
            selectedCharacterIndex = 0;
            selectedSpellbook = 0;
            selectedSpellbookLevel = 0;
            partyFilterChoices = null;
            Main.settings.selectedPartyFilter = 0;
        }
        public static void OnGUI() {
            var player = Game.Instance.Player;
            var filterChoices = GetPartyFilterChoices();
            if (filterChoices == null) { return; }

            UnitEntityData charToAdd = null;
            UnitEntityData charToRemove = null;
            var characterListFunc = UI.TypePicker<List<UnitEntityData>>(
                null,
                ref Main.settings.selectedPartyFilter,
                filterChoices
                );
            var characterList = characterListFunc.func();
            var mainChar = GameHelper.GetPlayerCharacter();
            if (characterListFunc.name == "Nearby") {
                UI.Slider("Nearby Distance", ref nearByRange, 1f, 200, 25, 0, " meters", UI.Width(250)); 
                characterList = characterList.OrderBy((ch) => ch.DistanceTo(mainChar)).ToList();
            }
            UI.Space(20);
            int chIndex = 0;
            int respecableCount = 0;
            var selectedCharacter = GetSelectedCharacter();
            foreach (UnitEntityData ch in characterList) {
                UnitProgressionData progression = ch.Descriptor.Progression;
                BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;
                int level = progression.CharacterLevel;
                int mythicLevel = progression.MythicExperience;
                UI.BeginHorizontal();

                UI.Label(ch.CharacterName.orange().bold(), UI.Width(200));
                UI.Space(25);
                float distance = mainChar.DistanceTo(ch); ;
                UI.Label(distance < 1 ? "" : distance.ToString("0") + "m", UI.Width(75));
                UI.Space(25);
                UI.Label("lvl".green() + $": {level}", UI.Width(75));
                // Level up code adapted from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/2
                if (player.AllCharacters.Contains(ch)) {
                    if (progression.Experience < xpTable.GetBonus(level + 1) && level < 20) {
                        UI.ActionButton("+1 Lvl", () => {
                            progression.AdvanceExperienceTo(xpTable.GetBonus(level + 1), true);
                        }, UI.Width(110));
                    }
                    else if (progression.Experience >= xpTable.GetBonus(level + 1) && level < 20) {
                        UI.Label("Level Up".cyan().italic(), UI.Width(110));
                    }
                    else { UI.Space(113); }
                }
                else { UI.Space(113); }
                UI.Space(25);
                UI.Label($"my".green() + $": {mythicLevel}", UI.Width(100));
                if (player.AllCharacters.Contains(ch)) {
                    if (progression.MythicExperience < 10) {
                        UI.ActionButton("+1 My", () => {
                            progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                        }, UI.Width(100));
                    }
                    else { UI.Label("Max", UI.Width(100)); }
                }
                else { UI.Space(103); }
                var classData = ch.Progression.Classes;
                UI.Space(35);

                bool showClasses = ch == selectedCharacter && selectedToggle == ToggleChoice.Classes;
                if (UI.DisclosureToggle($"{classData.Count} Classes", ref showClasses)) {
                    if (showClasses) { selectedCharacter = ch; selectedToggle = ToggleChoice.Classes; Logger.Log($"selected {ch.CharacterName}");
                    }
                    else { selectedToggle = ToggleChoice.None; }
                }
                bool showStats = ch == selectedCharacter && selectedToggle == ToggleChoice.Stats;
                if (UI.DisclosureToggle("Stats", ref showStats, true, 150)) {
                    if (showStats) { selectedCharacter = ch; selectedToggle = ToggleChoice.Stats; }
                    else { selectedToggle = ToggleChoice.None; }
                }
                bool showFacts = ch == selectedCharacter && selectedToggle == ToggleChoice.Facts;
                if (UI.DisclosureToggle("Facts", ref showFacts, true, 150)) {
                    if (showFacts) { selectedCharacter = ch; selectedToggle = ToggleChoice.Facts; }
                    else { selectedToggle = ToggleChoice.None; }
                }
                bool showBuffs = ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs;
                if (UI.DisclosureToggle("Buffs", ref showBuffs, true, 150)) {
                    if (showBuffs) { selectedCharacter = ch; selectedToggle = ToggleChoice.Buffs; }
                    else { selectedToggle = ToggleChoice.None; }
                }
                bool showAbilities = ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities;
                if (UI.DisclosureToggle("Abilities", ref showAbilities, true)) {
                    if (showAbilities) { selectedCharacter = ch; selectedToggle = ToggleChoice.Abilities; }
                    else { selectedToggle = ToggleChoice.None; }
                }
                UI.Space(25);
                var spellbooks = ch.Spellbooks;
                var spellCount = spellbooks.Sum((sb) => sb.GetAllKnownSpells().Count());
                if (spellCount > 0) {
                    bool showSpells = ch == selectedCharacter && selectedToggle == ToggleChoice.Spells;
                    if (UI.DisclosureToggle($"{spellCount} Spells", ref showSpells, true)) {
                        if (showSpells) { selectedCharacter = ch; selectedToggle = ToggleChoice.Spells; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                }
                else { UI.Space(180); }
                UI.Space(25);
                if (player.Party.Contains(ch)) {
                    respecableCount++;
                    UI.ActionButton("Respec", () => { Actions.ToggleModWindow(); UnitHelper.Respec(ch); }, UI.Width(150));
                }
                else {
                    UI.Space(155);
                }
                UI.Space(25);
                if (!player.PartyAndPets.Contains(ch)) {
                    UI.ActionButton("Add To Party", () => { charToAdd = ch; }, UI.AutoWidth());
                }
                else if (player.ActiveCompanions.Contains(ch)) {
                    UI.ActionButton("Remove From Party", () => { charToRemove = ch; }, UI.AutoWidth());
                }
                UI.EndHorizontal();

                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Classes) {
                    foreach (var cd in classData) {
                        UI.BeginHorizontal();
                        UI.Space(253);
                        UI.Label(cd.CharacterClass.Name.orange(), UI.Width(250));
                        UI.Label("level".green() + $": {cd.Level}", UI.Width(125f));
                        UI.Label(cd.CharacterClass.Description.green(), UI.AutoWidth());
                        UI.EndHorizontal();
                    }
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Stats) {
                    foreach (StatType obj in Enum.GetValues(typeof(StatType))) {
                        StatType statType = (StatType)obj;
                        ModifiableValue modifiableValue = ch.Stats.GetStat(statType);
                        if (modifiableValue != null) {
                            UI.BeginHorizontal();
                            UI.Space(69);   // the best number...
                            UI.Label(statType.ToString().green().bold(), UI.Width(400f));
                            UI.Space(25);
                            UI.ActionButton(" < ", () => { modifiableValue.BaseValue -= 1; }, UI.AutoWidth());
                            UI.Space(20);
                            UI.Label($"{modifiableValue.BaseValue}".orange().bold(), UI.Width(50f));
                            UI.ActionButton(" > ", () => { modifiableValue.BaseValue += 1; }, UI.AutoWidth());
                            UI.EndHorizontal();
                        }
                    }

                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Facts) {
                    FactsEditor.OnGUI(ch, ch.Progression.Features.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs) {
                    FactsEditor.OnGUI(ch, ch.Descriptor.Buffs.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities) {
                    FactsEditor.OnGUI(ch, ch.Descriptor.Abilities.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Spells) {
                    UI.Space(20);
                    var names = spellbooks.Select((sb) => sb.Blueprint.Name.ToString()).ToArray();
                    var titles = names.Select((name, i) => $"{name} ({spellbooks.ElementAt(i).CasterLevel})").ToArray();
                    if (spellbooks.Any()) {
                        UI.SelectionGrid(ref selectedSpellbook, titles, 7, UI.Width(1581));
                        if (selectedSpellbook > names.Count()) selectedSpellbook = 0;
                        var spellbook = spellbooks.ElementAt(selectedSpellbook);
                        
                        var casterLevel = spellbook.CasterLevel;
                        UI.EnumerablePicker<int>(
                            "Spell Level".bold() + " (count)",
                            ref selectedSpellbookLevel,
                            Enumerable.Range(0, casterLevel + 1),
                            0,
                            (l) => $"L{l}".bold() + $" ({spellbook.GetKnownSpells(l).Count()})".white(),
                            UI.AutoWidth()
                        );
                        FactsEditor.OnGUI(ch, spellbook, selectedSpellbookLevel);
                    }
                }
                if (selectedCharacter != GetSelectedCharacter()) {
                    selectedCharacterIndex = characterList.IndexOf(selectedCharacter);
                }
                chIndex += 1;
            }
            UI.Space(25);
            if (respecableCount > 0) {
                UI.Label($"{respecableCount} characters".yellow().bold() + " can be respecced. Pressing Respec will close the mod window and take you to character level up".orange());
                UI.Label("WARNING".yellow().bold() + " this feature is ".orange() + "EXPERIMENTAL".yellow().bold() + " and uses unreleased and likely buggy code.".orange());
                UI.Label("BACK UP".yellow().bold() + " before playing with this feature.You will lose your mythic ranks but you can restore them in this Party Editor.".orange());

            }
            UI.Space(25);
            if (charToAdd != null) { UnitEntityDataUtils.AddCompanion(charToAdd); }
            if (charToRemove != null) { UnitEntityDataUtils.RemoveCompanion(charToRemove); }
        }
    }
}