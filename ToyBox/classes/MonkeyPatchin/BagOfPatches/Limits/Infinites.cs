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
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using System;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Infinites {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(AbilityResourceLogic), nameof(AbilityResourceLogic.Spend))]
        public static class AbilityResourceLogic_Spend_Patch {
            public static bool Prefix(AbilityData ability) {
                var unit = ability.Caster.Unit;
                if (unit?.Descriptor.IsPartyOrPet() == true && settings.toggleInfiniteAbilities) {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ActivatableAbilityResourceLogic), nameof(ActivatableAbilityResourceLogic.SpendResource))]
        public static class ActivatableAbilityResourceLogic_SpendResource_Patch {
            public static bool Prefix() => !settings.toggleInfiniteAbilities;
        }

        [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.SpellSlotCost), MethodType.Getter)]
        public static class AbilityData_SpellSlotCost_Patch {
            public static bool Prefix() => !settings.toggleInfiniteSpellCasts;
        }

        [HarmonyPatch(typeof(SpendSkillPoint), nameof(SpendSkillPoint.Apply))]
        public static class SpendSkillPoint_Apply_Patch {
            public static bool Prefix(ref bool __state) {
                __state = settings.toggleInfiniteSkillpoints;
                return !__state;
            }

            public static void Postfix(ref bool __state, LevelUpState state, UnitDescriptor unit, StatType ___Skill) {
                if (__state) {
                    unit.Stats.GetStat(___Skill).BaseValue++;
                }
            }
        }

        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.SpendCharges), new Type[] { typeof(UnitDescriptor) })]
        public static class ItemEntity_SpendCharges_Patch {
            public static bool Prefix(ref bool __state) {
                __state = settings.toggleInfiniteItems;
                return !__state;
            }
            public static void Postfix(bool __state, ItemEntity __instance, ref bool __result, UnitDescriptor user) {
                if (__state) {
                    var blueprintItemEquipment = __instance.Blueprint as BlueprintItemEquipment;
                    if (!blueprintItemEquipment || !blueprintItemEquipment.GainAbility) {
                        __result = false;
                        return;
                    }
                    if (!__instance.IsSpendCharges) {
                        __result = true;
                        return;
                    }
                    var hasNoCharges = false;
                    if (__instance.Charges > 0) {
                        ItemEntityUsable itemEntityUsable = new((BlueprintItemEquipmentUsable)__instance.Blueprint);
                        if (user.State.Features.HandOfMagusDan && itemEntityUsable.Blueprint.Type == UsableItemType.Scroll) {
                            RuleRollDice ruleRollDice = new(user.Unit, new DiceFormula(1, DiceType.D100));
                            Rulebook.Trigger(ruleRollDice);
                            if (ruleRollDice.Result <= 25) {
                                __result = true;
                                return;
                            }
                        }

                        if (user.IsPartyOrPet()) {
                            __result = true;
                            return;
                        }

                        --__instance.Charges;
                    }
                    else {
                        hasNoCharges = true;
                    }

                    if (__instance.Charges >= 1 || blueprintItemEquipment.RestoreChargesOnRest) {
                        __result = !hasNoCharges;
                        return;
                    }

                    if (__instance.Count > 1) {
                        __instance.DecrementCount(1);
                        __instance.Charges = 1;
                    }
                    else {
                        var collection = __instance.Collection;
                        collection?.Remove(__instance);
                    }

                    __result = !hasNoCharges;
                }
            }
        }
    }
}