// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
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
using ModKit;

namespace ToyBox {
    public class MulticlassPicker {

        public static void OnGUI(HashSet<string> multiclassSet, float indent = 100) {
            var classes = Game.Instance.BlueprintRoot.Progression.CharacterClasses;
            var mythicClasses = Game.Instance.BlueprintRoot.Progression.CharacterMythics;

            foreach (var cl in classes) {
                PickerRow(cl, multiclassSet, indent);
            }
            UI.Div(indent, 20);
            foreach (var mycl in mythicClasses) {
                using (UI.HorizontalScope()) {
                    PickerRow(mycl, multiclassSet, indent);
                }
            }
        }

        public static bool PickerRow(BlueprintCharacterClass cl, HashSet<string> multiclassSet, float indent = 100) {
            bool changed = false;
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                UI.ActionToggle(
                    cl.Name,
                    () => multiclassSet.Contains(cl.AssetGuid.ToString()),
                    (v) => {
                        if (v) multiclassSet.Add(cl.AssetGuid.ToString()); 
                        else multiclassSet.Remove(cl.AssetGuid.ToString());
                        Main.Log($"multiclassSet - class: {cl.AssetGuid.ToString()}- <{String.Join(", ", multiclassSet)}>");

                        changed = true; 
                    },
                    350
                    ); 
                var archetypes = cl.Archetypes;
                if (multiclassSet.Contains(cl.AssetGuid.ToString()) && archetypes.Any()) {
                    UI.Space(50);
                    int originalArchetype = 0;
                    int selectedArchetype = originalArchetype = archetypes.FindIndex(archetype => multiclassSet.Contains(archetype.AssetGuid.ToString())) + 1;
                    var choices = new String[] { cl.Name }.Concat(archetypes.Select(a => a.Name)).ToArray();
                    UI.ActionSelectionGrid(ref selectedArchetype, choices, 6, (sel) => {
                        if (originalArchetype > 0)
                            multiclassSet.Remove(archetypes[originalArchetype - 1].AssetGuid.ToString());
                        if (selectedArchetype > 0)
                            multiclassSet.Add(archetypes[selectedArchetype - 1].AssetGuid.ToString());
                        Main.Log($"multiclassSet - archetype - <{String.Join(", ", multiclassSet)}>");
                    }, UI.AutoWidth());
                }
            }

            return changed;
        }
    }
}