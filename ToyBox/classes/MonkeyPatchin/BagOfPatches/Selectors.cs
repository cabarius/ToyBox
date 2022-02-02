// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Selectors {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;
        [HarmonyPatch(typeof(UnitCombatState), nameof(UnitCombatState.AttackOfOpportunity))]
        private static class UnitCombatState_AttackOfOpportunity_Patch {
            private static bool Prefix(UnitEntityData target) {
                if (settings.toggleAttacksofOpportunity && target.IsPlayerFaction) {
                    return false;
                }
                if (UnitEntityDataUtils.CheckUnitEntityData(target, settings.noAttacksOfOpportunitySelection)) {
                    return false;
                }
                return true;
            }
        }
    }
}
