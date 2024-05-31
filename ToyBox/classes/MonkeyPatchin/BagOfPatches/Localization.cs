using HarmonyLib;
using Kingmaker.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.BagOfPatches {
    internal static class Localization {

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.OnLocaleChanged))]
        public class Patch_LocalizationChanged {
            [HarmonyPatch]
            [HarmonyPrefix]
            public static void Prefix() {
                "ToyBox Party Editor".AddLocalizedString();
                // how to overwrite existing string
                //var unit = ResourcesLibrary.TryGetBlueprint<BlueprintUnit>("afa0eb762a9c4093b8ab29db4d905a13");
                //var localizedString = unit.LocalizedName.String;
                //LocalizationManager.CurrentPack.PutString(localizedString.m_Key, "Some Fancy New Name");

                // how to create a new string and assign it to something
                //var localizedString2 = new LocalizedString() { m_Key = "this can be any string. just needs to be unique" };
                //LocalizationManager.CurrentPack.PutString(localizedString2.m_Key, "Some Fancy New Name");
                //var unit2 = ResourcesLibrary.TryGetBlueprint<BlueprintUnit>("afa0eb762a9c4093b8ab29db4d905a13");
                //unit2.LocalizedName.String = localizedString;
            }
        }

        [HarmonyPatch(typeof(LocalizedString), nameof(LocalizedString.GetActualKey))]
        public class Patch_ActualKeyBug {
            [HarmonyPatch]
            [HarmonyPostfix]
            public static string Postfix(string original) => original ?? "";
        }
    }
}
