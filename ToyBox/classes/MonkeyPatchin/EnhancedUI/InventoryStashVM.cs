using HarmonyLib;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.Slots;
using Kingmaker.UI.Vendor;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UniRx;
using static ToyBox.BlueprintExtensions;

namespace ToyBox.Inventory {
    // Handles both adding selected sorters to the sorter dropdowns and making sure that the dropdown is properly updates to match the selected sorter.
    [HarmonyPatch(typeof(SlotsGroupVM<ItemSlotVM>))]
    public static class SlotsGroupVM_ {
        [HarmonyPatch(nameof(SlotsGroupVM<ItemSlotVM>.UpdateVisibleCollection), new Type[] {typeof(bool), typeof(bool)})]
        [HarmonyPostfix]
        public static void UpdateVisibleCollection(SlotsGroupVM<ItemSlotVM> __instance, bool force = false, bool forceSetIndex = false) {
            // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/ServiceWindowsPCView/Background/Windows/InventoryPCView/Inventory/Stash/StashContainer/
            // GlobalMapPCView(Clone)/StaticCanvas/ServiceWindowsConfig/Background/Windows/InventoryPCView/Inventory/Stash/StashContainer/
            try {
                var inventoryScreen = UIHelpers.InventoryScreen;
                var inventoryView = inventoryScreen.GetComponent<InventoryStashPCView>();
                var stashHeader = inventoryScreen.Find("Inventory/Stash/StashContainer/StashHeader");
                var stashHeaderLabel = stashHeader.GetComponent<TextMeshProUGUI>();
                if (Main.Settings.toggleEnhancedInventory) {
                    var count = __instance.VisibleCollection.Sum(vm => vm.HasItem ? vm.ItemEntity.Count : 0);
//                    var distinctCount = __instance.VisibleCollection.Count(vm => vm.HasItem);
                    stashHeaderLabel.AddSuffix($" ({count} items)".size(25), '(');
 //                  stashHeaderLabel.AddSuffix($" ({count}{(count != distinctCount ? $" ({distinctCount})" : "")} items)".size(25), '(');
                }
                // Cleanup modified text if enhanced inventory gets turned off
                else if (stashHeaderLabel.text.IndexOf('(') != -1)
                    stashHeaderLabel.AddSuffix(null, '(');
            }
            catch { }
        }
        // Player side
        // VendorPCView - InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/NestedCanvas1/VendorPCView/
        // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/NestedCanvas1/VendorPCView/MainContent/PlayerStash/

        // Vendor side
        // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/NestedCanvas1/VendorPCView/MainContent/VendorBlock/VendorHeader
        // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/NestedCanvas1/VendorPCView/MainContent/VendorBlock/PC_FilterBlock/FilterPCView
    }
}