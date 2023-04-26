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
    [HarmonyPatch(typeof(InventoryStashVM))]

#if false
    public static class InventoryStashVM_ {
        public static Settings Settings = Main.Settings;
        [HarmonyPatch(nameof(InventoryStashVM.UpdateValues), new Type[] { })]
        [HarmonyPostfix]
        public static void UpdateValues(InventoryStashVM __instance) {
            // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/ServiceWindowsPCView/Background/Windows/InventoryPCView/Inventory/Stash/StashContainer/
            var inventoryScreen = UIHelpers.InventoryScreen;
            var stashHeader = inventoryScreen.Find("Inventory/Stash/StashContainer/StashHeader");
            var stashHeaderLabel = stashHeader.GetComponent<TextMeshProUGUI>();
            var items = __instance.ItemsCollection;
            var slotsGroupVM = __instance.ItemSlotsGroup;
            var total = __instance.ItemsCollection.Count();
            var count = slotsGroupVM.VisibleCollection.Count();
            var text = stashHeaderLabel.text.Split('\n').FirstOrDefault();
            text += $"\n{count}/{total} items".size(50);
            stashHeaderLabel.text = text;
        }
    }
#endif

    [HarmonyPatch(typeof(SlotsGroupVM<ItemSlotVM>))]
    public static class SlotsGroupVM_ {
        [HarmonyPatch(nameof(SlotsGroupVM<ItemSlotVM>.UpdateVisibleCollection), new Type[] {typeof(bool), typeof(bool)})]
        [HarmonyPostfix]
        public static void UpdateVisibleCollection(SlotsGroupVM<ItemSlotVM> __instance, bool force = false, bool forceSetIndex = false) {
            // InGamePCView(Clone)/InGameStaticPartPCView/StaticCanvas/ServiceWindowsPCView/Background/Windows/InventoryPCView/Inventory/Stash/StashContainer/
            var inventoryScreen = UIHelpers.InventoryScreen;
            var inventoryView = inventoryScreen.GetComponent<InventoryStashPCView>();
            var stashHeader = inventoryScreen.Find("Inventory/Stash/StashContainer/StashHeader");
            var stashHeaderLabel = stashHeader.GetComponent<TextMeshProUGUI>();
            if (Main.Settings.toggleEnhancedInventory) {
                var count = __instance.VisibleCollection.Count(vm => vm.HasItem);
                stashHeaderLabel.AddSuffix($" ({count} items)".size(25), '(');
            }
            // Cleanup modified text if enhanced inventory gets turned off
            else if (stashHeaderLabel.text.IndexOf('(') != -1)
                stashHeaderLabel.AddSuffix(null, '(');
        }
    }
}