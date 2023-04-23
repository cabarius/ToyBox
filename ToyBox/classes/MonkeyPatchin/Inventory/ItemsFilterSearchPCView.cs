using HarmonyLib;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Root;
using Kingmaker.Items;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.Slots;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UniRx;
using static ToyBox.BlueprintExtensions;

namespace ToyBox.Inventory {
    [HarmonyPatch(typeof(ItemsFilterSearchPCView))]
    public static class ItemsFilterSearchPCView_Initialize_Patch {
        private static readonly HashSet<ItemsFilterSearchPCView> KnownFilterViews = new();
        public static void ReloadFilterViews() {
            foreach (var filterView in KnownFilterViews) {
                filterView.ReloadFilterOptions();
            }
        }
        private static void ReloadFilterOptions(this ItemsFilterSearchPCView filterView) {
            Mod.Log("hi");
            filterView.m_DropdownValues.Clear();
            List<string> options = new List<string>();

            foreach (var flag in EnumHelper.ValidFilterCategories) {
                if (Main.Settings.SearchFilterCategories.HasFlag(flag)
                    && EnhancedInventory.FilterCategoryMap.TryGetValue(flag, out var entry)
                   ) {
                    (int index, string text) = entry;
                    if (text == null) {
                        Mod.Log($"adding {flag} : {text}");
                        text = LocalizedTexts.Instance.ItemsFilter.GetText((ItemsFilter.FilterType)index);
                        EnhancedInventory.FilterCategoryMap[flag] = (index, text);
                    }
                    Mod.Log($"flag: {flag} - text: {text}");
                    options.Add(text);
                    filterView.m_DropdownValues.Add(text);
                }
            }
            filterView.SetupDropdown();
        }
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemsFilterSearchPCView.Initialize), new Type[] { })]
        public static void Initialize(ItemsFilterSearchPCView __instance) {
            if (!KnownFilterViews.Contains(__instance))
                KnownFilterViews.Add(__instance);
            __instance.ReloadFilterOptions();
        }
    }
}