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
using UnityEngine;
using static ToyBox.BlueprintExtensions;

namespace ToyBox.Inventory {
    [HarmonyPatch(typeof(ItemsFilterSearchPCView))]
    public static class ItemsFilterSearchPCViewPatch {
        public static Settings Settings = Main.Settings;
        public static readonly HashSet<ItemsFilterSearchPCView> KnownFilterViews = new();
        public static void ReloadFilterViews() {
            foreach (var filterView in KnownFilterViews) {
                filterView.ReloadFilterOptions();
            }
        }
        private static void ReloadFilterOptions(this ItemsFilterSearchPCView filterView) {
            if (!Settings.toggleEnhancedInventory) return;
            filterView.m_DropdownValues.Clear();
            List<string> options = new List<string>();

            foreach (var flag in EnumHelper.ValidFilterCategories) {
                if (Main.Settings.SearchFilterCategories.HasFlag(flag)
                    && EnhancedInventory.FilterCategoryMap.TryGetValue(flag, out var entry)
                   ) {
                    (int index, string text) = entry;
                    if (text == null) {
                        //Mod.Log($"adding {flag} : {text}");
                        text = LocalizedTexts.Instance.ItemsFilter.GetText((ItemsFilter.FilterType)index);
                        EnhancedInventory.FilterCategoryMap[flag] = (index, text);
                    }
                    //Mod.Log($"flag: {flag} - text: {text}");
                    options.Add(text);
                    filterView.m_DropdownValues.Add(text);
                }
            }
            filterView.SetupDropdown();
        }
        [HarmonyPatch(nameof(ItemsFilterSearchPCView.Initialize), new Type[] { })]
        [HarmonyPostfix]
        public static void Initialize(ItemsFilterSearchPCView __instance) {
            if (!Settings.toggleEnhancedInventory) return;
            if (!KnownFilterViews.Contains(__instance))
                KnownFilterViews.Add(__instance);
            __instance.ReloadFilterOptions();
        }
        [HarmonyPatch(nameof(ItemsFilterSearchPCView.SetActive), new Type[] { typeof(bool) })]
        [HarmonyPrefix]
        public static bool SetActive(ItemsFilterSearchPCView __instance, bool value) {
            if (!Settings.toggleEnhancedInventory || !Settings.toggleDontClearSearchWhenLoseFocus) return true;
            __instance.gameObject.SetActive(value);
            __instance.m_Dropdown.Hide();
            if (!value)
                return false;
            __instance.m_InputField.Select();
            return false;
        }
    }
}