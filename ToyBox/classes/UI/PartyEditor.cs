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
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace ToyBox {
    public class PartyEditor {
        static int showStatsBitfield = 0;
        static int showDetailsBitfield = 0;
        static String playerDetailsSearchText = "";
        static List<UnitEntityData> characterList = null;
        public static void OnGUI(UnityModManager.ModEntry modEntry) {
            var player = Game.Instance.Player;
            var partyFilterChoices = new List<NamedFunc<List<UnitEntityData>>>() {
                    new NamedFunc<List<UnitEntityData>>("Party", () => player.Party),
                    new NamedFunc<List<UnitEntityData>>("Party & Pets", () => player.m_PartyAndPets),
                    new NamedFunc<List<UnitEntityData>>("All Characters", () => player.AllCharacters),
                    new NamedFunc<List<UnitEntityData>>("Active Companions", () => player.ActiveCompanions),
                    new NamedFunc<List<UnitEntityData>>("Remote Companions", () => player.m_RemoteCompanions),
                    new NamedFunc<List<UnitEntityData>>("Custom (Mercs)", PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<UnitEntityData>>("Pets",  PartyUtils.GetPets)
                };

            UI.Section("Party Editor", () => {
                UnitEntityData charToAdd = null;
                UnitEntityData charToRemove = null;
                characterList = UI.TypePicker<List<UnitEntityData>>(
                    null,
                    ref Main.settings.selectedPartyFilter,
                    partyFilterChoices
                    );

                int chIndex = 0;
                foreach (UnitEntityData ch in characterList) {
                    UnitProgressionData progression = ch.Descriptor.Progression;
                    BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;
                    int level = progression.CharacterLevel;
                    int mythicLevel = progression.MythicExperience;
                    UI.BeginHorizontal();

                    UI.Label(ch.CharacterName.orange().bold(), UI.Width(400f));
                    UI.Label("level".green() + $": {level}", UI.Width(125f));
                    // Level up code adapted from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/2
                    if (progression.Experience < xpTable.GetBonus(level + 1) && level < 20) {
                        UI.ActionButton(" +1 Level", () => {
                            progression.AdvanceExperienceTo(xpTable.GetBonus(level + 1), true);
                        }, UI.Width(150));
                    }
                    else if (progression.Experience >= xpTable.GetBonus(level + 1) && level < 20) {
                        UI.Label("Level Up".cyan().italic(), UI.Width(150));
                    }
                    else {
                        UI.Space(153);
                    }
                    UI.Space(30);
                    UI.Label($"mythic".green() + $": {mythicLevel}", UI.Width(125));
                    if (progression.MythicExperience < 10) {
                        UI.ActionButton(" +1 Mythic", () => {
                            progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                        }, UI.Width(150));
                    }
                    else {
                        UI.Label("Max", UI.Width(150));
                    }
                    UI.Space(25);
                    UI.DisclosureBitFieldToggle("Stats", ref showStatsBitfield, chIndex);
                    UI.Space(25);
                    UI.DisclosureBitFieldToggle("Details", ref showDetailsBitfield, chIndex);
                    UI.Space(80);
                    if (!player.PartyAndPets.Contains(ch)) {
                        UI.ActionButton("Add To Party", () => { charToAdd = ch; }, UI.AutoWidth());
                    }
                    else if (player.ActiveCompanions.Contains(ch)) {
                        UI.ActionButton("Remove From Party", () => { charToRemove = ch; }, UI.AutoWidth());
                    }
                    UI.EndHorizontal();

                    if (((1 << chIndex) & showStatsBitfield) != 0) {
                        foreach (object obj in Enum.GetValues(typeof(StatType))) {
                            StatType statType = (StatType)obj;
                            ModifiableValue modifiableValue = ch.Stats.GetStat(statType);
                            if (modifiableValue != null) {
                                UI.BeginHorizontal();
                                UI.Space(69);   // the best number...
                                UI.Label(statType.ToString().green().bold(), UI.Width(400f));
                                UI.Space(25f);
                                UI.ActionButton(" < ", () => { modifiableValue.BaseValue -= 1; }, UI.AutoWidth());
                                UI.Space(20f);
                                UI.Label($"{modifiableValue.BaseValue}".orange().bold(), UI.Width(50f));
                                UI.ActionButton(" > ", () => { modifiableValue.BaseValue += 1; }, UI.AutoWidth());
                                UI.EndHorizontal();
                            }
                        }

                    }

                    if (((1 << chIndex) & showDetailsBitfield) != 0) {
                        UI.BeginHorizontal();
                        UI.Space(100);
                        UI.TextField(ref playerDetailsSearchText, null, UI.Width(200));
                        UI.EndHorizontal();
                        FeatureCollection features = ch.Descriptor.Progression.Features;
                        EntityFact featureToRemove = null;
                        foreach (Feature fact in features) {
                            String name = fact.Name;
                            if (name == null) { name = $"{fact.Blueprint.name}"; }
                            if (name != null && name.Length > 0 && (playerDetailsSearchText.Length == 0 || name.Contains(playerDetailsSearchText))) {
                                UI.BeginHorizontal();
                                UI.Space(100);
                                UI.Label($"{fact.Name}".cyan().bold(), UI.Width(400));
                                UI.Space(30);
                                var rank = fact.GetRank();
                                UI.ActionButton("<", () => { fact.AddRank(); }, UI.AutoWidth());
                                UI.Space(10f);
                                UI.Label($"{fact.GetRank()}".orange().bold(), UI.Width(30f));
                                UI.ActionButton(">", () => { fact.RemoveRank(); }, UI.AutoWidth());
                                UI.Space(30);
                                UI.ActionButton("Remove", () => { featureToRemove = fact; }, UI.Width(150));
                                String description = fact.Description;
                                if (description != null) {
                                    UI.Space(30);
                                    UI.Label(description.green(), UI.AutoWidth());
                                }
                                UI.EndHorizontal();
                            }
                        }
                        if (featureToRemove != null) {
                            ch.Descriptor.Progression.Features.RemoveFact(featureToRemove);
                        }
                    }
                    chIndex += 1;
                }
                if (charToAdd != null) { UnitEntityDataUtils.AddCompanion(charToAdd); }
                if (charToRemove != null) { UnitEntityDataUtils.RemoveCompanion(charToRemove); }
            });
        }
    }
}