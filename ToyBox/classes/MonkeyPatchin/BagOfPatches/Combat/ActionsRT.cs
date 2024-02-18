#nullable enable annotations
﻿// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.UnitLogic.Commands;



//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic.Parts;
using System.Linq;

//using Kingmaker.UI._ConsoleUI.GroupChanger;

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
            private static readonly string[] abilityGroupToDecooldownIds = new string[] {
                "1cf206b13141425491c379bc75ef0699", //WeaponAttackAbilityGroup
                "0a77ccc934d14b94b5171dc3faa531e4", //WeaponAttackAbilityGroup_PrimaryHand
                "109c045a43c84bfaa46ca3d0aadfbf3c", //WeaponAttackAbilityGroup_SecondaryHand
                "36fdf1bc96884a9e803dcbcc8e447785", //PsykerSpellsGroup
                "73f152d564dc482289fc8a753ab3d571", //PsykerStaffPowers
                "926c66e10782441bac49945d306697e1", //PsykerMinorPowers
                "ebb0aef8634845069b938c90b9d114aa", //PsykerMajorPowers

            };


            [HarmonyPatch(typeof(UnitUseAbilityParams))]
            public static class myPatch {
                [HarmonyPatch(nameof(UnitUseAbilityParams.IgnoreCooldown), MethodType.Getter)]
                [HarmonyPostfix]
                public static void Result(ref bool __result, UnitUseAbilityParams __instance) {

                    if (!__instance.Ability.Caster.IsInPlayerParty)
                        return;
                    if (Settings.toggleInfiniteAbilities ||
                        (Settings.toggleNoAttackCooldowns &&
                        __instance.Ability.AbilityGroups.Any(g => abilityGroupToDecooldownIds.Contains(g.AssetGuid)))
                        ) {
                        __result = true;
                    }
                }
            }
        }
    }
}