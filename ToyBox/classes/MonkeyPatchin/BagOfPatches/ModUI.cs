// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;
using ModKit;

namespace ToyBox.BagOfPatches
{
    static class ModUI
    {
        [HarmonyPatch(typeof(UnityModManager.UI), "Update")]
        internal static class UnityModManager_UI_Update_Patch
        {
            static Dictionary<int, float> scrollOffsets = new Dictionary<int, float> { };

            private static void Postfix(UnityModManager.UI __instance, ref Rect ___mWindowRect, ref Vector2[] ___mScrollPosition, ref int ___tabId)
            {
                // save these in case we need them inside the mod
                UI.ummRect = ___mWindowRect;
                UI.ummWidth = ___mWindowRect.width;
                UI.ummScrollPosition = ___mScrollPosition;
                UI.ummTabID = ___tabId;
            }
        }
    }
}