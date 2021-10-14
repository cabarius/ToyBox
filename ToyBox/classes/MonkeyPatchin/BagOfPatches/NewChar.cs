// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class NewChar {
        public static Settings settings = Main.settings;
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
                        var isCustom = unit.Blueprint.ToString() == "CustomCompanion";
                        //Logger.Log($"unit.Blueprint: {unit.Blueprint.ToString()}");
                        //Logger.Log($"not pregen - isCust: {isCustom}");
                        var pointCount = Math.Max(0, __instance.m_Unit.IsCustomCompanion() ? settings.characterCreationAbilityPointsMerc : settings.characterCreationAbilityPointsPlayer);

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
                var attributeMax = settings.characterCreationAbilityPointsMax;
                if (!__instance.Available) {
                    __result = false;
                }
                else {
                    if (attributeMax <= 18) {
                        attributeMax = 18;
                    }
                    var attributeValue = __instance.StatValues[attribute];
                    __result = attributeValue < attributeMax && __instance.GetAddCost(attribute) <= __instance.Points;
                }
            }
        }
        [HarmonyPatch(typeof(StatsDistribution), nameof(StatsDistribution.GetAddCost))]
        public static class StatsDistribution_GetAddCost_Patch {
            public static bool Prefix(StatsDistribution __instance, StatType attribute) {
                var attributeValue = __instance.StatValues[attribute];
                return attributeValue > 7 && attributeValue < 17;
            }
            public static void Postfix(StatsDistribution __instance, ref int __result, StatType attribute) {
                var attributeValue = __instance.StatValues[attribute];
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
                var attributeValue = __instance.StatValues[attribute];
                return attributeValue > 7 && attributeValue < 17;
            }
            public static void Postfix(StatsDistribution __instance, ref int __result, StatType attribute) {
                var attributeValue = __instance.StatValues[attribute];
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
