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
    public class FactsEditor<T> where T : UnitFact {
        static String searchText = "";
        static public void OnGUI(UnitLogicCollection<T> facts) {
            UI.BeginHorizontal();
            UI.Space(100);
            UI.TextField(ref searchText, null, UI.Width(200));
            UI.EndHorizontal();

            var addRankMethod = typeof(UnitLogicCollection<T>).GetMethod("AddRank");
            var removeRankMethod = typeof(UnitLogicCollection<T>).GetMethod("RemoveRank");

            T toRemove = null;
            T toRankDown = null;
            T toRankUp = null;
            foreach (var fact in facts.Enumerable) {
                String name = fact.Name;
                if (name == null) { name = $"{fact.Blueprint.name}"; }
                if (name != null && name.Length > 0 && (searchText.Length == 0 || name.Contains(searchText))) {
                    UI.BeginHorizontal();
                    UI.Space(100);
                    UI.Label($"{fact.Name}".cyan().bold(), UI.Width(400));
                    UI.Space(30);
                    try {
                        var rank = fact.GetRank();
                        var max = fact.Blueprint.GetPropValue<int>("Ranks");
                        if (removeRankMethod != null && rank > 1) {
                            UI.ActionButton("<", () => { toRankDown = fact; }, UI.Width(50));
                        }
                        else { UI.Space(53); }
                        UI.Space(10f);
                        UI.Label($"{rank}".orange().bold(), UI.Width(30f));
                        if (addRankMethod != null && rank < max) {
                            UI.ActionButton(">", () => { toRankUp = fact; }, UI.Width(50));
                        }
                        else { UI.Space(53); }
                    }
                    catch { }
                    UI.Space(30);
                    UI.ActionButton("Remove", () => { toRemove = fact; }, UI.Width(150));
                    String description = fact.Description;
                    if (description != null) {
                        UI.Space(30);
                        UI.Label(description.green(), UI.AutoWidth());
                    }
                    UI.EndHorizontal();
                }
            }
            if (toRankDown != null) { try { removeRankMethod.Invoke(toRankDown, new object[] { }); } catch { } }
            if (toRankUp != null) { try { addRankMethod.Invoke(toRankDown, new object[] { }); ; } catch { } }
            if (toRemove != null) {
                facts.RemoveFact(toRemove);
            }
        }
    }
}
