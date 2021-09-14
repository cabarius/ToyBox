// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands.Base;
using ModKit;
using TurnBased.Controllers;
using UnityModManagerNet;

namespace ToyBox.BagOfPatches {
    static class ACtions {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;
        public static Player player = Game.Instance.Player;


        [HarmonyPatch(typeof(UnitCombatState), "HasCooldownForCommand")]
        [HarmonyPatch(new[] { typeof(UnitCommand) })]
        public static class UnitCombatState_HasCooldownForCommand_Patch1 {
            public static void Postfix(ref bool __result, UnitCombatState __instance) {
                if (settings.toggleInstantCooldown && __instance.Unit.IsDirectlyControllable) {
                    __result = false;
                }
                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(UnitCombatState), "HasCooldownForCommand")]
        [HarmonyPatch(new[] { typeof(UnitCommand.CommandType) })]
        public static class UnitCombatState_HasCooldownForCommand_Patch2 {
            public static void Postfix(ref bool __result, UnitCombatState __instance) {
                if (settings.toggleInstantCooldown && __instance.Unit.IsDirectlyControllable) {
                    __result = false;
                }
                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(UnitCombatState), "OnNewRound")]
        public static class UnitCombatState_OnNewRound_Patch {
            public static bool Prefix(UnitCombatState __instance) {
                if (__instance.Unit.IsDirectlyControllable && settings.toggleInstantCooldown) {
                    __instance.Cooldown.Initiative = 0f;
                    __instance.Cooldown.StandardAction = 0f;
                    __instance.Cooldown.MoveAction = 0f;
                    __instance.Cooldown.SwiftAction = 0f;
                    __instance.Cooldown.AttackOfOpportunity = 0f;
                }
                if (CombatController.IsInTurnBasedCombat() && settings.toggleUnlimitedActionsPerTurn) {
                    __instance.Cooldown.Initiative = 0f;
                    __instance.Cooldown.StandardAction = 0f;
                    __instance.Cooldown.MoveAction = 0f;
                    __instance.Cooldown.SwiftAction = 0f;
                    __instance.Cooldown.AttackOfOpportunity = 0f;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), "SpendAction")]
        public static class UnitEntityData_SpendAction_Patch {

            public static bool Prefix(UnitCommand.CommandType type, bool isFullRound, float timeSinceCommandStart, UnitEntityData __instance) {
                if (!__instance.IsInCombat) return true;
                if (!settings.toggleUnlimitedActionsPerTurn) return true;

                if (CombatController.IsInTurnBasedCombat()) {
                    return false;
                }
                return true;
            }
        }
    }
}
