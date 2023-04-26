using HarmonyLib;
using Kingmaker.Blueprints.Root;
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
    // Handles both adding selected sorters to the sorter dropdowns and making sure that the dropdown is properly updates to match the selected sorter.
    [HarmonyPatch(typeof(ItemsFilterPCView))]
    public static class ItemsFilterPCView_ {
        public static Settings Settings = Main.Settings;

        private static readonly MethodInfo[] _methodInfosToTranspile = new MethodInfo[] {
            AccessTools.Method(typeof(ItemsFilterPCView_), nameof(SetDropdown)),
            AccessTools.Method(typeof(ItemsFilterPCView_), nameof(SetSorter)),
            AccessTools.Method(typeof(ItemsFilterPCView_), nameof(ObserveFilterChange)),
        };

        private static void SetDropdown(ItemsFilterPCView __instance, ItemsFilter.SorterType val) {
            if (!KnownFilterViews.Contains(__instance))
                KnownFilterViews.Add(__instance);
            if (Settings.toggleEnhancedInventory) {
                if (!ItemsFilterSearchPCView_Initialize_Patch.KnownFilterViews.Contains(__instance.m_SearchView))
                    ItemsFilterSearchPCView_Initialize_Patch.KnownFilterViews.Add(__instance.m_SearchView);
                __instance.m_Sorter.value = EnhancedInventory.SorterMapper.From((int)val);
            }
            else
                __instance.m_Sorter.value = (int)val;
        }

        private static void SetSorter(ItemsFilterPCView instance, int val) {
            if (Settings.toggleEnhancedInventory)
                instance.ViewModel.SetCurrentSorter((ItemsFilter.SorterType)EnhancedInventory.SorterMapper.To(val));
            else
                instance.ViewModel.SetCurrentSorter((ItemsFilter.SorterType)val);
        }

        private static ItemsFilter.FilterType _last_filter;

        private static void ObserveFilterChange(ItemsFilterPCView instance, ItemsFilter.FilterType filter) {
            if (_last_filter != filter) {
                _last_filter = filter;
                instance.ScrollToTop();
            }
        }

        // In BindViewImplementation, there are two inline delegates; we replace both of those in order with our own.
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(ItemsFilterPCView.BindViewImplementation))]
        public static IEnumerable<CodeInstruction> BindViewImplementation(IEnumerable<CodeInstruction> instructions) {
            List<CodeInstruction> il = instructions.ToList();

            int ldftn_count = 0;

            for (int i = 0; i < il.Count && ldftn_count < _methodInfosToTranspile.Length; ++i) {
                if (il[i].opcode == OpCodes.Ldftn) {
                    il[i].operand = _methodInfosToTranspile[ldftn_count++];
                }
            }

            return il.AsEnumerable();
        }
        private static readonly HashSet<ItemsFilterPCView> KnownFilterViews = new();
        public static void ReloadFilterViews() {
            foreach (var filterView in KnownFilterViews) {
                filterView.ReloadSorterOptions();
            }
        }
        private static void ReloadSorterOptions(this ItemsFilterPCView filterView) {
            if (!Settings.toggleEnhancedInventory) return;
            filterView.m_Sorter.ClearOptions();
            List<string> options = new List<string>();

            foreach (var flag in EnumHelper.ValidSorterCategories) {
                if (Settings.InventoryItemSorterOptions.HasFlag(flag) 
                    && EnhancedInventory.SorterCategoryMap.TryGetValue(flag, out var entry)
                    ) { 
                    (int  index, string text) = entry;
                    if (text == null) {
                        //Mod.Log($"adding {flag} : {text}");
                        text = LocalizedTexts.Instance.ItemsFilter.GetText((ItemsFilter.SorterType)index);
                        EnhancedInventory.SorterCategoryMap[flag] = (index, text);
                    }
                    //Mod.Log($"flag: {flag} - text: {text}");
                    options.Add(text);
                }
            }

            filterView.m_Sorter.AddOptions(options);
        }

        // Adds the sorters to the dropdown.
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ItemsFilterPCView.Initialize), new Type[] { })]
        public static void Initialize(ItemsFilterPCView __instance) {
            if (!Settings.toggleEnhancedInventory) return;
            if (!KnownFilterViews.Contains(__instance))
                KnownFilterViews.Add(__instance);
            __instance.ReloadSorterOptions();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(ItemsFilterPCView.Initialize), new Type[] { typeof(bool) })]
        public static void Initialize_Prefix(ref bool needReset) {
            needReset = false;
        }
    }
}