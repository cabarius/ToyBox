using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
    //Fix for enchants added with toybox getting erased by stack merges - EX: If you add mighty fists to an amulet of natural armor, then stick it in inventory where you have another natural armor amulet of the same type, they're merged into a stack and the mighty fists enchant vanishes
    class CustomEnchantmentStackingFix {
        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.CanBeMerged), new Type[] { typeof(ItemEntity) })]
        static class Mergeupgrade {
            public static void Postfix(ref bool __result, ItemEntity __instance, ItemEntity other) {
                if (__result) {
                    if (!(__instance is ItemEntityUsable) && !(other is ItemEntityUsable)) {

                        if (__instance.Enchantments.Count != other.Enchantments.Count)//This catches every case but same number of new enchants added
                        {

                            __result = false;
                            return;
                        }
                        else if (__instance.Enchantments.Select(x => x.Blueprint.ToReference<BlueprintItemEnchantmentReference>()).Except(other.Enchantments.Select(x => x.Blueprint.ToReference<BlueprintItemEnchantmentReference>())).Any()) //And this catches the rest
                            {

                            return;
                        }
                    }
                }

            }
        }
    }
}
