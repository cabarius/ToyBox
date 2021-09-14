// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using UnityModManagerNet;

namespace ToyBox.BagOfPatches {
    static class Selectors {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;
        public static Player player = Game.Instance.Player;
        [HarmonyPatch(typeof(UnitCombatState), "AttackOfOpportunity")]
        static class UnitCombatState_AttackOfOpportunity_Patch {
            static bool Prefix(UnitEntityData target) {
                if (UnitEntityDataUtils.CheckUnitEntityData(target, settings.noAttacksOfOpportunitySelection)) {
                    return false;
                }
                return true;
            }
        }
    }
}
