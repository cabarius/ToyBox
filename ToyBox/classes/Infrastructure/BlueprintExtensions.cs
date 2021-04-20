// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Armies.TacticalCombat.Blueprints;
using Kingmaker.Armies.TacticalCombat.Brain;
using Kingmaker.Armies.TacticalCombat.Brain.Considerations;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Credits;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Blueprints.Console;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Interaction;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Tutorial;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;

namespace ToyBox {

    public static class BlueprintExensions {

        public static String GetDisplayName(this BlueprintScriptableObject bp) { return bp.name; }
        public static String GetDisplayName(this BlueprintSpellbook bp) {
            var name = bp.DisplayName;
            if (name == null || name.Length == 0) name = bp.name.Replace("Spellbook", "");
            return name;
        }
        public static String CollationName(this BlueprintScriptableObject bp) {
            var typeName = bp.GetType().ToString();
            var stripIndex = typeName.LastIndexOf("Blueprint");
            if (stripIndex > 0) typeName = typeName.Substring(stripIndex + "Blueprint".Length);
            return typeName;
        }
        static Dictionary<Type, List<BlueprintAction>> actionsByType = new Dictionary<Type, List<BlueprintAction>>();
        public static List<BlueprintAction> BlueprintActions(this UnitEntityData ch, Type type) {
            if (ch == null) { return new List<BlueprintAction>(); }
            var results = new List<BlueprintAction>();
            if (actionsByType.ContainsKey(type)) return actionsByType[type];
            foreach (var action in BlueprintAction.globalActions) {
                if (type.IsKindOf(action.type)) { results.Add(action); }
            }
            foreach (var action in BlueprintAction.characterActions) {
                if (type.IsKindOf(action.type)) { results.Add(action); }
            }
            actionsByType[type] = results;
            return results;
        }
        public static List<BlueprintAction> ActionsForUnit(this BlueprintScriptableObject bp, UnitEntityData ch) {
            if (ch == null) { return new List<BlueprintAction>(); }
            Type type = bp.GetType();
            var actions = ch.BlueprintActions(type);
            var results = new List<BlueprintAction>();
            foreach (var action in actions) {
                if (action.canPerform(ch, bp)) { results.Add(action); }
            }
            return results;
        }

        static Dictionary<Type, List<BlueprintScriptableObject>> blueprintsByType = new Dictionary<Type, List<BlueprintScriptableObject>>();
        public static List<BlueprintScriptableObject> BlueprintsOfType(Type type) {
            if (blueprintsByType.ContainsKey(type)) return blueprintsByType[type];
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return new List<BlueprintScriptableObject>();
            var filtered = blueprints.Where((bp) => bp.GetType().IsKindOf(type)).ToList();
            blueprintsByType[type] = filtered;
            return filtered;
        }

        public static List<BlueprintScriptableObject> GetBlueprints<T>() where T : BlueprintScriptableObject {
            return BlueprintsOfType(typeof(T));
        }
        public static int GetSelectableFeaturesCount(this BlueprintFeatureSelection selection, UnitDescriptor unit) {
            int count = 0;
            NoSelectionIfAlreadyHasFeature component = selection.GetComponent<NoSelectionIfAlreadyHasFeature>();
            if ((UnityEngine.Object)component == (UnityEngine.Object)null)
                return count;
            if (component.AnyFeatureFromSelection) {
                foreach (BlueprintFeature allFeature in selection.AllFeatures) {
                    if (!unit.Progression.Features.HasFact((BlueprintFact)allFeature)) {
                        count++;
                    }
                }
            }
            foreach (BlueprintFeature feature in component.Features) {
                if (!unit.Progression.Features.HasFact((BlueprintFact)feature)) {
                    count++;
                }
            }
            return count;
        }
    }
}