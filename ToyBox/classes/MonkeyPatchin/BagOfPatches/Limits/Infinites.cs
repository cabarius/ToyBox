// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints.Items.Equipment;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.ActivatableAbilities;
using System;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Infinites {
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.SpendCharges), new Type[] { typeof(UnitDescriptor) })]
        public static class ItemEntity_SpendCharges_Patch {
            public static bool Prefix(ref bool __result, UnitDescriptor user, ItemEntity __instance) {
                if (settings.toggleInfiniteItems && user.IsPartyOrPet()) {
                    var blueprintItemEquipment = __instance.Blueprint as BlueprintItemEquipment;
                    __result = blueprintItemEquipment && blueprintItemEquipment.GainAbility; // Don't skip the check about being a valid item and having an ability to use
                    return false; // We're skipping spend charges because even if someone else has logic to sometimes not spend charges, we don't care. We said "infinite" use.
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(AbilityResourceLogic), nameof(AbilityResourceLogic.Spend))]
        public static class AbilityResourceLogic_Spend_Patch {
            public static bool Prefix(AbilityData ability) {
                var unit = ability.Caster
                    ;
                if (unit?.IsPartyOrPet() == true && settings.toggleInfiniteAbilities) {

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ActivatableAbilityResourceLogic), nameof(ActivatableAbilityResourceLogic.SpendResource))]
        public static class ActivatableAbilityResourceLogic_SpendResource_Patch {
            public static bool Prefix() => !settings.toggleInfiniteAbilities;
        }

    }
}