using HarmonyLib;
using Kingmaker.UnitLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.BagOfPatches {
    [HarmonyPatch(typeof(Spellbook), "BaseLevel", MethodType.Getter)]
    public static class Spellbook_BaseLevel_Getter_Patch {
        public static bool Prefix(Spellbook __instance, ref int ___m_BaseLevelInternal, ref int __result) {

            if (Main.settings.toggleUnlockTheClFromClass) {
                __result = Math.Max(0, ___m_BaseLevelInternal + __instance.Blueprint.CasterLevelModifier);
                return false;
            }
            return true;
        }

    }
}
