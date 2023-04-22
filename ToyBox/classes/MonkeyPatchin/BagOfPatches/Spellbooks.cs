using HarmonyLib;
using Kingmaker.UnitLogic;

namespace ToyBox.BagOfPatches {
    internal static class Spellbooks {
        public static Settings settings = Main.Settings;

#if false
        [HarmonyPatch(typeof(Spellbook), nameof(Spellbook.GetMaxSpellLevel))]
        public static class Spellbook_GetMaxSpellLevel_Patch {
            public static bool Prefix(Spellbook __instance, ref int __result) {
                int num = -1;
                for (var spellLevel = 0; spellLevel <= 10; ++spellLevel) {
                    if (__instance.Blueprint.SpellsPerDay.GetCount(__instance.CasterLevel, spellLevel).HasValue)
                        num = spellLevel;
                }
                __result = num != 9 || !(bool)__instance.Owner.State.Features.EnableSpellLevel10 ? num : 10;
                return true;
            }
        }
#endif
    }
}