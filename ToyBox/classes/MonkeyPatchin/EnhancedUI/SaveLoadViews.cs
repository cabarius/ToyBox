using HarmonyLib;
using Kingmaker.Blueprints.Root;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.SaveLoad;
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
using Kingmaker.UI.MVVM._VM.SaveLoad;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static ToyBox.BlueprintExtensions;
using static UnityEngine.Object;
using Kingmaker;
using Kingmaker.Utility;
using Kingmaker.EntitySystem.Persistence;
using ModKit.Utility;
using Owlcat.Runtime.UI.SelectionGroup;

namespace ToyBox {
    public static class SaveLoadViews {
        public static Settings Settings => Main.Settings;

        [HarmonyPatch(typeof(SaveLoadPCView))]
        public static class SaveLoadPCViewPatch {
            public static string SaveFileSearchText = "";
            private static SearchBar _searchBar;
            private static SaveSlotCollectionVirtualView _collectionVirtualView;

            [HarmonyPatch(nameof(SaveLoadPCView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void BindViewImplementation(SaveLoadPCView __instance) {
                if (!Settings.toggleEnhancedLoadSave) {
                    if (_searchBar != null) {
                        Destroy(UIHelpers.SaveLoadScreen.Find("ToyBoxLoadSaveSearchBar")?.gameObject);
                        _searchBar = null;
                    }
                    return;
                }
                // grab some stuff from the scene graph
                var saveLoadScreen = UIHelpers.SaveLoadScreen;
                _collectionVirtualView = saveLoadScreen.Find("SaveLoadScreen/SaveSlotCollectionPlace/SaveSlotVirtualCollectionView").GetComponent<SaveSlotCollectionVirtualView>();
                var topMenu = saveLoadScreen.Find("SaveLoadScreen/Top");
                
                // get rid of old version of the search bar if it is there
                Destroy(saveLoadScreen.Find("ToyBoxLoadSaveSearchBar")?.gameObject);

                // make new search bar
                _searchBar = new SearchBar(saveLoadScreen, null, false, "ToyBoxLoadSaveSearchBar");
                var searchBarTransform = _searchBar.GameObject.transform;

                Destroy(searchBarTransform.Find("Background").gameObject);                 // get rid of the fringe background cuz it looks weird here

                searchBarTransform.localScale = new Vector3(0.85f, 0.85f, 1.0f);
                searchBarTransform.localPosition = new Vector2(-677.0f, 442.0f);
                _searchBar.DropdownIconObject?.SetActive(false);

                _searchBar.InputField.onValueChanged.AddListener(val => {
                    SaveFileSearchText = _searchBar.InputField.text;
                    Mod.Debug($"searchText Changed => {SaveFileSearchText}");
                    _collectionVirtualView.m_VirtualList.ScrollController.ForceScrollToTop();
                    __instance.ViewModel.UpdateSavesCollection();
                    //m_scroll_bar.ScrollToTop();
                });
                _searchBar.FocusSearchBar();
                SaveFileSearchText = "";
                _searchBar.InputField.text = SaveFileSearchText;
            }
            // Kingmaker.UI.MVVM._PCView.SaveLoad.SaveLoadPCView
            // CommonPCView(Clone)/FadeCanvas/SaveLoadView

            // Good place for search field
            // CommonPCView(Clone)/FadeCanvas/SaveLoadView/SaveLoadScreen/Top

            // Kingmaker.UI.MVVM._PCView.SaveLoad.SaveSlotCollectionVirtualView
            // CommonPCView(Clone)/FadeCanvas/SaveLoadView/SaveLoadScreen/SaveSlotCollectionPlace/SaveSlotVirtualCollectionView

            // Kingmaker.UI.MVVM._ConsoleView.ServiceWindows.CharacterInfo.Sections.Abilities.ExpandableTitleView
            // CommonPCView(Clone)/FadeCanvas/SaveLoadView/SaveLoadScreen/SaveSlotCollectionPlace/SaveSlotVirtualCollectionView/Viewport/Content/ExpandableTitleView
        }
        //[HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.SendOnValueChangedAndUpdateLabel))]

        [HarmonyPatch(typeof(SaveLoadVM))]
        public static class SaveLoadVMPatch {
            [HarmonyPatch(nameof(SaveLoadVM.UpdateSavesCollection))]
            [HarmonyPrefix]
            public static bool UpdateSavesCollection(SaveLoadVM __instance) {
                if (!Settings.toggleEnhancedLoadSave) return true;
                Game.Instance.SaveManager.UpdateSaveListIfNeeded(BuildModeUtility.IsDevelopment);
                var referenceCollection = new List<SaveInfo>(Game.Instance.SaveManager);
                var searchText = SaveLoadPCViewPatch.SaveFileSearchText;
                if (!searchText.IsNullOrEmpty())
                    referenceCollection = referenceCollection.Where(rc =>
                                                                        rc.Name.Matches(searchText)
                                                                        || rc.Description.Matches(searchText)
                                                                        || rc.Area.name.Matches(searchText)
                                                                        // || rc.Campaign.GetDisplayName().Matches(searchText)
                        ).ToList();
                referenceCollection.Sort((s1, s2) => -s1.SystemSaveTime.CompareTo(s2.SystemSaveTime));
                __instance.ShowCorruptionDialog = false;
                foreach (var saveInfo1 in referenceCollection) {
                    var saveInfo = saveInfo1;
                    if (saveInfo.Type != SaveInfo.SaveType.ForImport && !__instance.m_SaveSlotVMs.Any(vm => vm.ReferenceSaveEquals(saveInfo))) {
                        var slot = new SaveSlotVM(saveInfo,
                                                  __instance.Mode,
                                                  __instance.RequestSaveOrLoad,
                                                  __instance.RequestDeleteSaveInfo);
                        __instance.AddDisposable(slot);
                        __instance.SaveSlotCollectionVm.HandleNewSave(slot);
                        __instance.m_SaveSlotVMs.Add(slot);
                    }
                }

                var saveSlotVmList = new List<SaveSlotVM>();
                foreach (var saveSlotVm in __instance.m_SaveSlotVMs.Where(saveSlotVm => !referenceCollection.Any(saveSlotVm.ReferenceSaveEquals))) {
                    saveSlotVm.Dispose();
                    saveSlotVmList.Add(saveSlotVm);
                }

                foreach (var slot in saveSlotVmList) {
                    __instance.SaveSlotCollectionVm.HandleDeleteSave(slot);
                    __instance.m_SaveSlotVMs.Remove(slot);
                }

                __instance.AddDisposable(__instance.m_SelectionGroup = new SelectionGroupRadioVM<SaveSlotVM>(__instance.m_SaveSlotVMs, __instance.SelectedSaveSlot));
                return false;
            }
        }
    }
}