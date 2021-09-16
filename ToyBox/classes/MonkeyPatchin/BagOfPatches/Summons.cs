// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Achievements;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Achievements.Blueprints;
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
using Kingmaker.Modding;
using Kingmaker.PubSubSystem;
using Kingmaker.RandomEncounters;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Settings;
using Kingmaker.Settings.Difficulty;
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
using Kingmaker.UI.MVVM._PCView.CharGen;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Mythic;
using Kingmaker.UI.MVVM._PCView.Loot;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.Slots;
//using Kingmaker.UI.RestCamp;
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
using Owlcat.Runtime.UI.MVVM;
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

namespace ToyBox.BagOfPatches {
    static class Summons {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;
        public static Player player = Game.Instance.Player;


        static bool SummonedByPlayerFaction = false;

        [HarmonyPatch(typeof(SummonPool), "Register")]
        static class SummonPool_Register_Patch {
            static void Postfix(ref UnitEntityData unit) {
                //if (settings.toggleSetSpeedOnSummon) {
                //    unit.Descriptor.Stats.GetStat(StatType.Speed).BaseValue = settings.setSpeedOnSummonValue;
                //}

                if (settings.toggleMakeSummmonsControllable && SummonedByPlayerFaction) {
                    // modLogger.Log($"SummonPool.Register: Unit [{unit.CharacterName}] [{unit.UniqueId}]");
                    UnitEntityDataUtils.Charm(unit);
#if false
                    if (unit.Blueprint.AssetGuid == "6fdf7a3f850a1eb48bfbf44d9d0f45dd" && StringUtils.ToToggleBool(settings.toggleDisableWarpaintedSkullAbilityForSummonedBarbarians)) // WarpaintedSkullSummonedBarbarians
                    {
                        if (unit.Body.Head.HasItem && unit.Body.Head.Item?.Blueprint?.AssetGuid == "5d343648bb8887d42b24cbadfeb36991") // WarpaintedSkullItem
                        {
                            unit.Body.Head.Item.Ability.Deactivate();
                            Common.ModLoggerDebug(unit.Body.Head.Item.Name + "'s ability active: " + unit.Body.Head.Item.Ability.Active);
                        }
                    }
#endif
                    SummonedByPlayerFaction = false;
                }

#if false
                if (StringUtils.ToToggleBool(settings.toggleRemoveSummonsGlow)) {
                    unit.Buffs.RemoveFact(Utilities.GetBlueprintByGuid<BlueprintFact>("706c182e86d9be848b59ddccca73d13e")); // SummonedCreatureVisual
                    unit.Buffs.RemoveFact(Utilities.GetBlueprintByGuid<BlueprintFact>("e4b996b5168fe284ab3141a91895d7ea")); // NaturalAllyCreatureVisual
                }
#endif
            }
        }

        [HarmonyPatch(typeof(RuleSummonUnit), MethodType.Constructor, new Type[] { 
            typeof(UnitEntityData), 
            typeof(BlueprintUnit), 
            typeof(Vector3), 
            typeof(Rounds), 
            typeof(int) }
        )]
        public static class RuleSummonUnit_Constructor_Patch {
            public static void Prefix(UnitEntityData initiator, BlueprintUnit blueprint, Vector3 position, ref Rounds duration, ref int level, RuleSummonUnit __instance) {
                modLogger.Log($"old duration: {duration} level: {level} \n mult: {settings.summonDurationMultiplier1} levelInc: {settings.summonLevelModifier1}\n initiatior: {initiator} tweakTarget: {settings.summonTweakTarget1} shouldTweak: {UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.summonTweakTarget1)}");
                if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.summonTweakTarget1)) {
                    if (settings.summonDurationMultiplier1 != 1) {
                        duration = new Rounds(Convert.ToInt32(duration.Value * settings.summonDurationMultiplier1));
                    }
                    if (settings.summonLevelModifier1 != 0) {
                        level = Math.Max(0, Math.Min(level + (int)settings.summonLevelModifier1, 20));
                    }
                }
                else if (UnitEntityDataUtils.CheckUnitEntityData(initiator, settings.summonTweakTarget2)) {
                    if (settings.summonDurationMultiplier2 != 1) {
                        duration = new Rounds(Convert.ToInt32(duration.Value * settings.summonDurationMultiplier2));
                    }
                    if (settings.summonLevelModifier2 >= 0) {
                        level = Math.Max(0, Math.Min(level + (int)settings.summonLevelModifier1, 20));
                    }
                }
                modLogger.Log($"new duration: {duration} level: {level}");

                if (settings.toggleMakeSummmonsControllable) {
                    SummonedByPlayerFaction = initiator.IsPlayerFaction;
                }
                modLogger.Log("Initiator: " + initiator.CharacterName + $"(PlayerFaction : {initiator.IsPlayerFaction})" + "\nBlueprint: " + blueprint.CharacterName  + "\nDuration: " + duration.Value);
            }
        }

        [HarmonyPatch(typeof(ActionBarManager), "CheckTurnPanelView")]
        internal static class ActionBarManager_CheckTurnPanelView_Patch {
            private static void Postfix(ActionBarManager __instance) {
                if (settings.toggleMakeSummmonsControllable && CombatController.IsInTurnBasedCombat()) {
                    Traverse.Create((object)__instance).Method("ShowTurnPanel", Array.Empty<object>()).GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), "IsDirectlyControllable", MethodType.Getter)]
        public static class UnitEntityData_IsDirectlyControllable_Patch {
            public static void Postfix(UnitEntityData __instance, ref bool __result) {
                if (settings.toggleMakeSummmonsControllable && __instance.Descriptor.IsPartyOrPet() && !__result && __instance.Get<UnitPartSummonedMonster>() != null && !__instance.Descriptor.State.IsFinallyDead && !__instance.Descriptor.State.IsPanicked && !__instance.IsDetached && !__instance.PreventDirectControl) {
                    __result = true;
                }
            }
        }
    }
}
