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
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using ModKit;

namespace ToyBox.BagOfPatches {
    static class Multipliers {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(EncumbranceHelper), "GetHeavy")]
        static class EncumbranceHelper_GetHeavy_Patch {
            static void Postfix(ref int __result) {
                __result = Mathf.RoundToInt(__result * settings.encumberanceMultiplier);
            }
        }

        [HarmonyPatch(typeof(UnitPartWeariness), "GetFatigueHoursModifier")]
        static class EncumbranceHelper_GetFatigueHoursModifier_Patch {
            static void Postfix(ref float __result) {
                __result = __result * (float)Math.Round(settings.fatigueHoursModifierMultiplier, 1);
            }
        }

        [HarmonyPatch(typeof(Player), "GainPartyExperience")]
        public static class Player_GainPartyExperience_Patch {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref int gained) {
                gained = Mathf.RoundToInt(gained * (float)Math.Round(settings.experienceMultiplier, 1));
                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "GainMoney")]
        public static class Player_GainMoney_Patch {
            [HarmonyPrefix]
            public static bool Prefix(Player __instance, ref long amount) {
                amount = Mathf.RoundToInt(amount * (float)Math.Round(settings.moneyMultiplier, 1));
                return true;
            }
        }

        [HarmonyPatch(typeof(Spellbook), "GetSpellsPerDay")]
        static class Spellbook_GetSpellsPerDay_Patch {
            static void Postfix(ref int __result) {
                __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.spellsPerDayMultiplier, 1));
            }
        }

        [HarmonyPatch(typeof(Player), "GetCustomCompanionCost")]
        public static class Player_GetCustomCompanionCost_Patch {
            public static bool Prefix(ref bool __state) {
                return !__state;    // FIXME - why did Bag of Tricks do this?
            }

            public static void Postfix(ref int __result) {
                __result = Mathf.RoundToInt(__result * settings.companionCostMultiplier);
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), "GetRegionalModifier", new Type[] { })]
        public static class MovementSpeed_GetRegionalModifier_Patch1 {
            public static void Postfix(ref float __result) {
                float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        [HarmonyPatch(typeof(GlobalMapMovementController), "GetRegionalModifier", new Type[] { typeof(Vector3) })]
        public static class MovementSpeed_GetRegionalModifier_Patch2 {
            public static void Postfix(ref float __result) {
                float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                __result = speedMultiplier * __result;
            }
        }

        /**
            GlobalMapState state,
            GlobalMapView view,
            IGlobalMapTraveler traveler,
            float visualStepDistance)
        */
        [HarmonyPatch(typeof(GlobalMapMovementUtility), "MoveAlongEdge", new Type[] {
            typeof(GlobalMapState), typeof(GlobalMapView), typeof(IGlobalMapTraveler), typeof(float)
            })]
        public static class GlobalMapMovementUtility_MoveAlongEdge_Patch {
            public static void Prefix(
                GlobalMapState state,
                GlobalMapView view,
                IGlobalMapTraveler traveler,
                ref float visualStepDistance) {
                // TODO - can we get rid of the other map movement multipliers and do them all here?
                if (traveler is GlobalMapArmyState armyState && armyState.Data.Faction == Kingmaker.Armies.ArmyFaction.Crusaders) {
                    float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                    visualStepDistance = speedMultiplier * visualStepDistance;
                }
            }
        }

        [HarmonyPatch(typeof(GlobalMapArmyState), "SpendMovementPoints", new Type[] { typeof(float) })]
        public static class GlobalMapArmyState_SpendMovementPoints_Patch {
            public static void Prefix(GlobalMapArmyState __instance, ref float points) {
                if (__instance.Data.Faction == Kingmaker.Armies.ArmyFaction.Crusaders) {
                    float speedMultiplier = Mathf.Clamp(settings.travelSpeedMultiplier, 0.1f, 100f);
                    points = points / speedMultiplier;
                }
            }
        }

        /**
        public Buff AddBuff(
          BlueprintBuff blueprint,
          UnitEntityData caster,
          TimeSpan? duration,
          [CanBeNull] AbilityParams abilityParams = null) {
            MechanicsContext context = new MechanicsContext(caster, this.Owner, (SimpleBlueprint)blueprint);
            if (abilityParams != null)
                context.SetParams(abilityParams);
            return this.Manager.Add<Buff>(new Buff(blueprint, context, duration));
        }
        */
#if false
        [HarmonyPatch(typeof(Buff), "AddBuff")]
        [HarmonyPatch(new Type[] { typeof(BlueprintBuff), typeof(UnitEntityData), typeof(TimeSpan?), typeof(AbilityParams) })]
        public static class Buff_AddBuff_patch {
            public static void Prefix(BlueprintBuff blueprint, UnitEntityData caster, ref TimeSpan? duration, [CanBeNull] AbilityParams abilityParams = null) {
                try {
                    if (!caster.IsPlayersEnemy) {
                        if (duration != null) {
                            duration = TimeSpan.FromTicks(Convert.ToInt64(duration.Value.Ticks * settings.buffDurationMultiplierValue));
                        }
                    }
                }
                catch (Exception e) {
                    modLogger.Log(e.ToString());
                }

                Main.Debug("Initiator: " + caster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }
#endif
        [HarmonyPatch(typeof(BuffCollection), "AddBuff", new Type[] {
            typeof(BlueprintBuff),
            typeof(UnitEntityData),
            typeof(TimeSpan?),
            typeof(AbilityParams)
            })]
        public static class BuffCollection_AddBuff_patch {
            public static void Prefix(BlueprintBuff blueprint, UnitEntityData caster, ref TimeSpan? duration, [CanBeNull] AbilityParams abilityParams = null) {
                try {
                    if (!caster.IsPlayersEnemy) {
                        if (duration != null) {
                            duration = TimeSpan.FromTicks(Convert.ToInt64(duration.Value.Ticks * settings.buffDurationMultiplierValue));
                        }
                    }
                }
                catch (Exception e) {
                    modLogger.Log(e.ToString());
                }

                Main.Debug("Initiator: " + caster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }

        [HarmonyPatch(typeof(BuffCollection), "AddBuff", new Type[] {
            typeof(BlueprintBuff),
            typeof(MechanicsContext),
            typeof(TimeSpan?)
            })]
        public static class BuffCollection_AddBuff2_patch {
            public static void Prefix(BlueprintBuff blueprint, MechanicsContext parentContext, ref TimeSpan? duration) {
                try {
                    if (!parentContext.MaybeCaster.IsPlayersEnemy) {
                        if (duration != null) {
                            duration = TimeSpan.FromTicks(Convert.ToInt64(duration.Value.Ticks * settings.buffDurationMultiplierValue));
                        }
                    }
                }
                catch (Exception e) {
                    modLogger.Log(e.ToString());
                }

                Main.Debug("Initiator: " + parentContext.MaybeCaster.CharacterName + "\nBlueprintBuff: " + blueprint.Name + "\nDuration: " + duration.ToString());
            }
        }

        [HarmonyPatch(typeof(RandomEncounterUnitSelector), "PlaceUnits")]
        internal static class RandomEncounterUnitSelector_PlaceUnits_Patch {
            private static void Postfix(ref IList<UnitEntityData> units) {
                foreach (UnitEntityData unit in units) {
                    if (unit.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction)) {
                        Main.Debug("RandomEncounterUnitSelector.PlaceUnits: " + unit.CharacterName);
                        unit.Stats.HitPoints.BaseValue = Mathf.RoundToInt(unit.Stats.HitPoints.BaseValue * settings.enemyBaseHitPointsMultiplier);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnitSpawnerBase), "Spawn")]
        internal static class UnitSpawner_Spawn_Patch {
            private static void Postfix(ref UnitEntityData __result) {
                if (__result != null && __result.AttackFactions.Contains(Game.Instance.BlueprintRoot.PlayerFaction)) {
                    Main.Debug("UnitSpawner.Spawn: " + __result.CharacterName);
                    __result.Stats.HitPoints.BaseValue = Mathf.RoundToInt(__result.Stats.HitPoints.BaseValue * settings.enemyBaseHitPointsMultiplier);
                }
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemSellPrice", new Type[] { typeof(ItemEntity) })]
        static class VendorLogic_GetItemSellPrice_Patch {
            private static void Postfix(ref long __result) {
                __result = (long)(__result * settings.vendorSellPriceMultiplier);
            }
        }
        [HarmonyPatch(typeof(VendorLogic), "GetItemSellPrice", new Type[] { typeof(BlueprintItem) })]
        static class VendorLogic_GetItemSellPrice_Patch2 {
            private static void Postfix(ref long __result) {
                __result = (long)(__result * settings.vendorSellPriceMultiplier);
            }
        }

        [HarmonyPatch(typeof(VendorLogic), "GetItemBuyPrice", new Type[] { typeof(ItemEntity) })]
        static class VendorLogic_GetItemBuyPrice_Patch {
            private static void Postfix(ref long __result) {
                __result = (long)(__result * settings.vendorBuyPriceMultiplier);
            }
        }
        [HarmonyPatch(typeof(VendorLogic), "GetItemBuyPrice", new Type[] { typeof(BlueprintItem) })]
        static class VendorLogic_GetItemBuyPrice_Patc2h {
            private static void Postfix(ref long __result) {
                __result = (long)(__result * settings.vendorBuyPriceMultiplier);
            }
        }

        [HarmonyPatch(typeof(CameraZoom), "TickZoom")]
        static class CameraZoom_TickZoom {
            static bool firstCall = true;
            static float BaseFovMin = 17.5f;
            static float BaseFovMax = 30;
            public static bool Prefix(CameraZoom __instance) {
                if (firstCall) {
                    modLogger.Log($"baseMin/Max: {__instance.FovMin} {__instance.FovMax}");
                    if (__instance.FovMin != BaseFovMin) {
                        modLogger.Log($"Warning: game has changed FovMin to {__instance.FovMin} vs {BaseFovMin}. Toy Box should be updated to avoid stability issues when enabling and disabling the mod repeatedly".orange().bold());
                        //BaseFovMin = __instance.FovMin;
                    }
                    if (__instance.FovMax != BaseFovMax) {
                        modLogger.Log($"Warning: game has changed FovMax to {__instance.FovMax} vs {BaseFovMax}. Toy Box should be updated to avoid stability issues when enabling and disabling the mod repeatedly".orange().bold());
                        //BaseFovMax = __instance.FovMax;
                    }
                    firstCall = false;
                }
                __instance.FovMax = BaseFovMax * settings.fovMultiplier;
                __instance.FovMin = BaseFovMin / settings.fovMultiplier;
                if (__instance.m_ZoomRoutine != null)
                    return true;
                if (!__instance.IsScrollBusy && Game.Instance.IsControllerMouse)
                    __instance.m_PlayerScrollPosition += __instance.IsOutOfScreen ? 0.0f : Input.GetAxis("Mouse ScrollWheel");
                __instance.m_ScrollPosition = __instance.m_PlayerScrollPosition;
                __instance.m_ScrollPosition = Mathf.Clamp(__instance.m_ScrollPosition, 0.0f, __instance.m_ZoomLenght);
                __instance.m_SmoothScrollPosition = Mathf.Lerp(__instance.m_SmoothScrollPosition, __instance.m_ScrollPosition, Time.unscaledDeltaTime * __instance.m_Smooth);
                __instance.m_Camera.fieldOfView = Mathf.Lerp(__instance.FovMax, __instance.FovMin, __instance.CurrentNormalizePosition);
                __instance.m_PlayerScrollPosition = __instance.m_ScrollPosition;
                return true;
            }
        }
    }
}
