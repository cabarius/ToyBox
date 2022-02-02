// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
//using Kingmaker.Controllers.GlobalMap;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using System.Collections.Generic;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;
using ModKit;

namespace ToyBox.BagOfPatches {
    internal static class ModUI {
        [HarmonyPatch(typeof(UnityModManager.UI), nameof(UnityModManager.UI.Update))]
        internal static class UnityModManager_UI_Update_Patch {
            private static readonly Dictionary<int, float> scrollOffsets = new() { };
            private static void Postfix(UnityModManager.UI __instance, ref Rect ___mWindowRect, ref Vector2[] ___mScrollPosition, ref int ___tabId) {
#if false
                // hack to fix mouse wheel which seems to gets de-magnified when the cursor is on the right side of the screen
                var scrollPosition = ___mScrollPosition[___tabId];
                var scrollOffset = scrollOffsets.GetValueOrDefault(___tabId, scrollPosition.y);
                var mouseDelta = UnityEngine.Input.mouseScrollDelta;
                if (mouseDelta.y != 0 || mouseDelta.x != 0) {
                    scrollOffset -= 10*mouseDelta.y;
                    scrollPosition.y = scrollOffset;
                    scrollOffsets[___tabId] = scrollOffset;
                    var str = "";
                    foreach (var pos in ___mScrollPosition) str += $"{pos} ";
                    Logger.Log($"scroll pos: {str} mouse delta: {mouseDelta}");
                }
                ___mScrollPosition[___tabId] = scrollPosition;
#endif
                // save these in case we need them inside the mod
                //Logger.Log($"Rect: {___mWindowRect}");
                UI.ummRect = ___mWindowRect;
                UI.ummWidth = ___mWindowRect.width;
                UI.ummScrollPosition = ___mScrollPosition;
                UI.ummTabID = ___tabId;
            }
        }
    }
}
