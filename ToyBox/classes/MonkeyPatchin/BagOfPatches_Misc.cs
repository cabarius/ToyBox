// borrowed shamelessly and enchanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.Controllers.Rest;
using Kingmaker.Controllers.Rest.Cooking;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.Dungeon.Units.Debug;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Formations;
using DG.Tweening;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.PubSubSystem;
using Kingmaker.RandomEncounters;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.TextTools;
using Kingmaker.UI;
//using Kingmaker.UI._ConsoleUI.Models;
using Kingmaker.UI.Common;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Group;
using Kingmaker.UI.IngameMenu;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.Log;
using Kingmaker.UI.MainMenuUI;
using Kingmaker.UI.MVVM;
using Kingmaker.UI.MVVM.CharGen;
using Kingmaker.UI.MVVM.CharGen.Phases;
using Kingmaker.UI.MVVM.CharGen.Phases.Mythic;
using Kingmaker.UI.RestCamp;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.ServiceWindow.LocalMap;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using Kingmaker.View.Spawners;
using Kingmaker.Visual;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.HitSystem;
using Kingmaker.Visual.LocalMap;
using Kingmaker.Visual.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using Kingmaker.UI.ActionBar;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using TMPro;
using TurnBased.Controllers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Kingmaker.UnitLogic.Class.LevelUp.LevelUpState;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox {
    static class HarmonyPatches_Misc {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;
        public static Player player = Game.Instance.Player;


        [HarmonyPatch(typeof(Spellbook), "GetSpellsPerDay")]
        static class Spellbook_GetSpellsPerDay_Patch {
            static void Postfix(ref int __result) {
                __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.spellsPerDayMultiplier, 1));
            }
        }

        public static BlueprintAbility ExtractSpell([NotNull] ItemEntity item) {
            ItemEntityUsable itemEntityUsable = item as ItemEntityUsable;
            if (itemEntityUsable?.Blueprint.Type != UsableItemType.Scroll) {
                return null;
            }
            return itemEntityUsable.Blueprint.Ability.Parent ? itemEntityUsable.Blueprint.Ability.Parent : itemEntityUsable.Blueprint.Ability;
        }

        public static string GetSpellbookActionName(string actionName, ItemEntity item, UnitEntityData unit) {
            if (actionName != LocalizedTexts.Instance.Items.CopyScroll) {
                return actionName;
            }

            BlueprintAbility spell = ExtractSpell(item);
            if (spell == null) {
                return actionName;
            }

            List<Spellbook> spellbooks = unit.Descriptor.Spellbooks.Where(x => x.Blueprint.SpellList.Contains(spell)).ToList();

            int count = spellbooks.Count;

            if (count <= 0) {
                return actionName;
            }

            string actionFormat = "{0} <{1}>";

            return string.Format(actionFormat, actionName, count == 1 ? spellbooks.First().Blueprint.Name : "Multiple");
        }


        [HarmonyPatch(typeof(Kingmaker.UI.ServiceWindow.ItemSlot), "ScrollContent", MethodType.Getter)]
        public static class ItemSlot_ScrollContent_Patch {
            [HarmonyPostfix]
            static void Postfix(Kingmaker.UI.ServiceWindow.ItemSlot __instance, ref string __result) {
                UnitEntityData currentCharacter = UIUtility.GetCurrentCharacter();
                CopyItem component = __instance.Item.Blueprint.GetComponent<CopyItem>();
                string actionName = component?.GetActionName(currentCharacter) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(actionName)) {
                    actionName = GetSpellbookActionName(actionName, __instance.Item, currentCharacter);
                }
                __result = actionName;
            }
        }
    }
}
