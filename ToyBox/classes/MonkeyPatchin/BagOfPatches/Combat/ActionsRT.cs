// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;



//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;

//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Actions {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;


        [HarmonyPatch(typeof(PartUnitCombatState))]
        public static class PartUnitCombatStatePatch {
            [HarmonyPatch(nameof(PartUnitCombatState.SpendActionPoints))]
            [HarmonyPrefix]
            public static bool SpendActionPoints(PartUnitCombatState __instance, int? yellow = null, float? blue = null) {
                if (!Settings.toggleUnlimitedActionsPerTurn) return true;
                if (__instance.Owner.IsPartyOrPet()) {
                    return false;
                }
                else {
                    return true;
                }
            }
            [HarmonyPatch(nameof(PartUnitCombatState.SpendActionPointsAll))]
            [HarmonyPrefix]
            public static bool SpendActionPointsAll(PartUnitCombatState __instance) {
                if (!Settings.toggleReallyUnlimitedActionsPerTurn) return true;
                if (__instance.Owner.IsPartyOrPet()) {
                    return false;
                }
                else {
                    return true;
                }
            }
        }


        [HarmonyPatch(typeof(PartAbilityCooldowns))]
        public static class PartAbilityCooldownsPatch {
            [HarmonyPatch(nameof(PartAbilityCooldowns.StartCooldown))]
            [HarmonyPrefix]
            public static bool StartCooldown(AbilityData ability) {
                if (!Settings.toggleReallyUnlimitedActionsPerTurn) return true;
                if (ability.Caster.IsInPlayerParty)
                    return false;
                return true;
            }

            [HarmonyPatch(nameof(PartAbilityCooldowns.IsIgnoredByComponent))]
            [HarmonyPrefix]
            public static bool IsIgnoredByComponent(ref bool __result, BlueprintAbilityGroup group, AbilityData ability) {
                if (!Settings.toggleReallyUnlimitedActionsPerTurn) return true;
                if (ability.Caster.IsInPlayerParty) {
                    __result = true;
                    return false;
                }
                return true;
            }

        }
    }
}