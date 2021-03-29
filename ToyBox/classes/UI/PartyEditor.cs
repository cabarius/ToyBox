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
        // toggle bitfields
        
        static int showClassesBitfield = 0;
        static int showStatsBitfield = 0;
        static int showFactsBitfield = 0;
        static int showAbilitiesBitfield = 0;
        static int showSpellsBitfield = 0;

        static int selectedSpellbook = 0;
        static int selectedSpellbookLevel = 0;
        private static NamedFunc<List<UnitEntityData>>[] _partyFilterChoices = null;
        public static NamedFunc<List<UnitEntityData>>[] GetPartyFilterChoices() {
            var player = Game.Instance.Player;
            var palyerData = GameHelper.GetPlayerCharacter();
            if (player != null && _partyFilterChoices == null) {
                _partyFilterChoices = new NamedFunc<List<UnitEntityData>>[] {
                    new NamedFunc<List<UnitEntityData>>("Party", () => player.Party),
                    new NamedFunc<List<UnitEntityData>>("Party & Pets", () => player.m_PartyAndPets),
                    new NamedFunc<List<UnitEntityData>>("All Characters", () => player.AllCharacters),
                    new NamedFunc<List<UnitEntityData>>("Active Companions", () => player.ActiveCompanions),
                    new NamedFunc<List<UnitEntityData>>("Remote Companions", () => player.m_RemoteCompanions),
                    new NamedFunc<List<UnitEntityData>>("Custom (Mercs)", PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<UnitEntityData>>("Pets", PartyUtils.GetPets),
                    new NamedFunc<List<UnitEntityData>>("Friendly", () => Game.Instance.State.Units.Where((u) => !u.IsEnemy(palyerData)).ToList()),
                    new NamedFunc<List<UnitEntityData>>("Enemies", () => Game.Instance.State.Units.Where((u) => u.IsEnemy(palyerData)).ToList()),
                    new NamedFunc<List<UnitEntityData>>("All Units", () => Game.Instance.State.Units.ToList()),
               };
            }
            return _partyFilterChoices;
        }
        static List<UnitEntityData> characterList = null;
        public static List<UnitEntityData> GetCharacterList() {
            var partyFilterChoices = GetPartyFilterChoices();
            if (partyFilterChoices == null) { return null; }
            return partyFilterChoices[Main.settings.selectedPartyFilter].func();
        }
        public static void OnGUI() {
            var player = Game.Instance.Player;
            var filterChoices = GetPartyFilterChoices();
            if (filterChoices == null) { return; }

            UnitEntityData charToAdd = null;
            UnitEntityData charToRemove = null;
            characterList = UI.TypePicker<List<UnitEntityData>>(
                null,
                ref Main.settings.selectedPartyFilter,
                filterChoices
                );
            UI.Space(20);
            int chIndex = 0;
            int respecableCount = 0;
            foreach (UnitEntityData ch in characterList) {
                UnitProgressionData progression = ch.Descriptor.Progression;
                BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;
                int level = progression.CharacterLevel;
                int mythicLevel = progression.MythicExperience;
                UI.BeginHorizontal();

                UI.Label(ch.CharacterName.orange().bold(), UI.Width(200));
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
                UI.Label($"mythic".green() + $": {mythicLevel}", UI.Width(125));
                if (player.AllCharacters.Contains(ch)) {
                    if (progression.MythicExperience < 10) {
                        UI.ActionButton("+1 ML", () => {
                            progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                        }, UI.Width(100));
                    }
                    else { UI.Label("Max", UI.Width(100)); }
                }
                else { UI.Space(103); }
                var classData = ch.Progression.Classes;
                UI.Space(35);
                UI.DisclosureBitFieldToggle($"{classData.Count} Classes", ref showClassesBitfield, chIndex, true, false);
                UI.DisclosureBitFieldToggle("Stats", ref showStatsBitfield, chIndex, true, false, 150);
                UI.DisclosureBitFieldToggle("Facts", ref showFactsBitfield, chIndex, true, false, 150);
                UI.DisclosureBitFieldToggle("Abilities", ref showAbilitiesBitfield, chIndex, true, false);
                UI.Space(25);
                var spellbooks = ch.Spellbooks;
                var spellCount = spellbooks.Sum((sb) => sb.GetAllKnownSpells().Count());
                if (spellCount > 0) {
                    UI.DisclosureBitFieldToggle($"{spellCount} Spells", ref showSpellsBitfield, chIndex, true, false);
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

                if (((1 << chIndex) & showClassesBitfield) != 0) {
                    foreach (var cd in classData) {
                        UI.BeginHorizontal();
                        UI.Space(253);
                        UI.Label(cd.CharacterClass.Name.orange(), UI.Width(250));
                        UI.Label("level".green() + $": {cd.Level}", UI.Width(125f));
                        UI.Label(cd.CharacterClass.Description.green(), UI.AutoWidth());
                        UI.EndHorizontal();
                    }
                }
                if (((1 << chIndex) & showStatsBitfield) != 0) {
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
                if (((1 << chIndex) & showFactsBitfield) != 0) {
                    FactsEditor.OnGUI(ch, ch.Progression.Features.Enumerable.ToList());
                }
                if (((1 << chIndex) & showAbilitiesBitfield) != 0) {
                    FactsEditor.OnGUI(ch, ch.Descriptor.Abilities.Enumerable.ToList());
                }
                if (((1 << chIndex) & showSpellsBitfield) != 0) {
                    UI.Space(20);
                    var names = spellbooks.Select((sb) => sb.Blueprint.Name.ToString()).ToArray();
                    var titles = names.Select((name, i) => $"{name} ({spellbooks.ElementAt(i).CasterLevel})").ToArray();
                    if (spellbooks.Any()) {
                        UI.SelectionGrid(ref selectedSpellbook, titles, 7, UI.Width(1581));
                        if (selectedSpellbook > names.Count()) selectedSpellbook = 0;
                        var spellbook = spellbooks.ElementAt(selectedSpellbook);
                        var casterLevel = spellbook.CasterLevel;
                        UI.EnumerablePicker<int>(
                            "Spell Level".bold() +" (count)",
                            ref selectedSpellbookLevel,
                            Enumerable.Range(0, casterLevel + 1),
                            0,
                            (l) => $"L{l}".bold() + $" ({spellbook.GetKnownSpells(l).Count()})".white(),
                            UI.AutoWidth()
                        );
                        FactsEditor.OnGUI(ch, spellbook, selectedSpellbookLevel);
                    }
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