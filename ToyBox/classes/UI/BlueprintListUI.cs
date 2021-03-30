// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections;
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
    public class BlueprintListUI {
        public static void OnGUI(UnitEntityData ch, IEnumerable<BlueprintScriptableObject> blueprints, float indent = 0, Func<String,String> titleFormater = null) {
            if (titleFormater == null) titleFormater = (t) => t.orange().bold();
            int index = 0;
            int maxActions = 0;
            foreach (BlueprintScriptableObject blueprint in blueprints) {
                var actions = blueprint.ActionsForUnit(ch);
                maxActions = Math.Max(actions.Count, maxActions);
            }

            foreach (BlueprintScriptableObject blueprint in blueprints) {
                UI.BeginHorizontal();
                UI.Space(indent);
                var actions = blueprint.ActionsForUnit(ch);
                var titles = actions.Select((a) => a.name);
                var title = blueprint.name;
                if (titles.Contains("Remove")) {
                    title = title.cyan().bold();
                }
                else {
                    title = titleFormater(title);
                }

                UI.Label(title, UI.Width(650 - indent));
                int actionCount = actions != null ? actions.Count() : 0;
                for (int ii = 0; ii < maxActions; ii++) {
                    if (ii < actionCount) {
                        BlueprintAction action = actions[ii];
                        // TODO -don't show increase or decrease actions until we redo actions into a proper value editor that gives us Add/Remove and numeric item with the ability to show values.  For now users can edit ranks in the Facts Editor
                        if (action.name == "<" || action.name == ">") continue;
                            UI.ActionButton(action.name, () => { action.action(ch, blueprint); }, UI.Width(140));
                        UI.Space(10);
                    }
                    else {
                        UI.Space(154);
                    }
                }
                UI.Space(30);
                UI.Label($"{blueprint.GetType().Name.cyan()}" + " " + blueprint.GetDescription().green().bold()); //, UI.Width(400));
                UI.EndHorizontal();
#if false
                String description = blueprint.GetDescription();
                if (description.Length > 0) {
                    UI.BeginHorizontal();
                    UI.Space(684 + maxActions * 154);
                    UI.Label($"{description.green()}");
                    UI.EndHorizontal();
                }
#endif
                index++;
            }
        }
    }
}