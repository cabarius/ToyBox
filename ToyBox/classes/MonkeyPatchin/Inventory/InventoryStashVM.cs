using HarmonyLib;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.Slots;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UniRx;
using static ToyBox.BlueprintExtensions;

namespace ToyBox.Inventory {
    // Handles both adding selected sorters to the sorter dropdowns and making sure that the dropdown is properly updates to match the selected sorter.
    [HarmonyPatch(typeof(InventoryStashVM))]

    public static class InventoryStashVM_ {
        public static Settings Settings = Main.Settings;
        [HarmonyPatch(nameof(InventoryStashVM.UpdateValues), new Type[] { })]
        [HarmonyPostfix]
        public static void UpdateValues(InventoryStashVM __instance) {
            // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/ServiceWindowsPCView/Background/Windows/InventoryPCView/Inventory/Stash/StashContainer/

        }

    }
}