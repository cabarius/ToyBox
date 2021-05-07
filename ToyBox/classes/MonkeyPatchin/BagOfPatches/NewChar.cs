// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

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
using Kingmaker.UI.MVVM._PCView.CharGen;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases;
using Kingmaker.UI.MVVM._PCView.CharGen.Phases.Mythic;
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
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.ResourceLinks;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.AbilityScores;
using System.Diagnostics;

namespace ToyBox.BagOfPatches {
    static class NewChar {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;
        public static Player player = Game.Instance.Player;

        //     public LevelUpState([NotNull] UnitEntityData unit, LevelUpState.CharBuildMode mode, bool isPregen)
        [HarmonyPatch(typeof(LevelUpState), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(LevelUpState.CharBuildMode), typeof(bool) })]
        public static class LevelUpState_Patch {
            [HarmonyPriority(Priority.Low)]
            public static void Postfix(UnitDescriptor unit, LevelUpState.CharBuildMode mode, ref LevelUpState __instance, bool isPregen) {
                if (__instance.IsFirstCharacterLevel) {
                    if (!__instance.IsPregen) {
                        // Kludge - there is some weirdness where the unit in the character generator does not return IsCustomCharacter() as true during character creation so I have to check the blueprint. The thing is if I actually try to get the blueprint name the game crashes so I do this kludge calling unit.Blueprint.ToString()
                        bool isCustom = unit.Blueprint.ToString() == "CustomCompanion";
                        //Logger.Log($"unit.Blueprint: {unit.Blueprint.ToString()}");
                        //Logger.Log($"not pregen - isCust: {isCustom}");
                        int pointCount = Math.Max(0, isCustom ? settings.characterCreationAbilityPointsMerc : settings.characterCreationAbilityPointsPlayer);
                        //Logger.Log($"points: {pointCount}");

                        __instance.StatsDistribution.Start(pointCount);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.CanRemove))]
        public static class StatsDistribution_CanRemove_Patch {
            public static void Postfix(ref bool __result, StatType attribute, StatsDistribution __instance) {
                if (settings.characterCreationAbilityPointsMin != 7) {
                    __result = __instance.Available && __instance.StatValues[attribute] > settings.characterCreationAbilityPointsMin;
                }
            }
        }

        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.CanAdd))]
        public static class StatsDistribution_CanAdd_Patch {
            public static void Prefix() {
 
            }
            public static void Postfix(ref bool __result, StatType attribute, StatsDistribution __instance) {
                int attributeMax = settings.characterCreationAbilityPointsMax;
                if (!__instance.Available) {
                    __result = false;
                }
                else {
                    if (attributeMax <= 18) {
                        attributeMax = 18;
                    }
                    int attributeValue = __instance.StatValues[attribute];
                    __result = attributeValue < attributeMax && __instance.GetAddCost(attribute) <= __instance.Points;
                }
            }
        }
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.GetAddCost))]
        public static class StatsDistribution_GetAddCost_Patch {
            public static bool Prefix(StatsDistribution __instance, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                return (attributeValue > 7 && attributeValue < 17);
            }
            public static void Postfix(StatsDistribution __instance, ref int __result, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                if (attributeValue <= 7) {
                    __result = 2;
                }
                if (attributeValue >= 17) {
                    __result = 4;
                }
            }
        }
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.GetRemoveCost))]
        public static class StatsDistribution_GetRemoveCost_Patch {
            public static bool Prefix(StatsDistribution __instance, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                return (attributeValue > 7 && attributeValue < 17);
            }
            public static void Postfix(StatsDistribution __instance, ref int __result, StatType attribute) {
                int attributeValue = __instance.StatValues[attribute];
                if (attributeValue <= 7) {
                    __result = -2;
                }
                else if (attributeValue >= 17) {
                    __result = -4;
                }
            }
        }

    }
}
