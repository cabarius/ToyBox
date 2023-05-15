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
using Kingmaker.UI.MVVM._VM.ServiceWindows.CharacterInfo.Sections.Abilities;

namespace ToyBox {
    public static class SaveLoadViews {
        public static Settings Settings => Main.Settings;

        [HarmonyPatch(typeof(SaveLoadPCView))]
        public static class SaveLoadPCViewPatch {
            public static string SaveFileSearchText = "";
            private static SearchBar _searchBar = null;
            private static SaveSlotCollectionVirtualView _collectionVirtualView;
            public static void UpdateSearch(SaveLoadPCView saveLoadView) {
                SaveFileSearchText = _searchBar.InputField.text;
                Mod.Debug($"searchText Changed => {SaveFileSearchText}");
                _collectionVirtualView.m_VirtualList.ScrollController.ForceScrollToTop();
                var saveLoadVM = saveLoadView.ViewModel;
                saveLoadView.ViewModel.UpdateSavesCollection();
                #if false
                saveLoadView.m_SlotCollectionView.m_VirtualList.Elements.Sort((e1, e2) => {
                    switch (e1.Data) {
                        case SaveSlotGroupVM ssgVMa1 when e2.Data is SaveSlotGroupVM ssgVMa2: 
                            return ssgVMa1.CharacterName.CompareTo(ssgVMa2.CharacterName);
                        case SaveSlotGroupVM ssgVMb1 when e2.Data is SaveSlotVM ssVMb2:
                        case SaveSlotVM ssVMc1 when e2.Data is SaveSlotGroupVM ssgVMc2: 
                            return 1;
                        case SaveSlotVM ssVMd1 when e2.Data is SaveSlotVM ssVMd2: 
                            return -ssVMd1.Reference.SystemSaveTime.CompareTo(ssVMd2.Reference.SystemSaveTime);
                        default: 
                            return 0;
                    }
                });
                #endif
                //m_scroll_bar.ScrollToTop();

            }

            [HarmonyPatch(nameof(SaveLoadPCView.BindViewImplementation))]
            [HarmonyPrefix]
            public static bool BindViewImplementationPrefix(SaveLoadPCView __instance) {
                Mod.Debug($"SaveLoadPCView.BindViewImplementation");
                // if (_searchBar?.InputField is TMP_InputField inputField) inputField.text = SaveFileSearchText;
                if (!Settings.toggleEnhancedLoadSave) {
                    if (_searchBar != null) {
                        Destroy(UIHelpers.SaveLoadScreen.Find("ToyBoxLoadSaveSearchBar")?.gameObject);
                        _searchBar = null;
                    }
                    return true;
                }
                // grab some stuff from the scene graph
                var saveLoadScreen = UIHelpers.SaveLoadScreen;
                _collectionVirtualView = saveLoadScreen.Find("SaveLoadScreen/SaveSlotCollectionPlace/SaveSlotVirtualCollectionView").GetComponent<SaveSlotCollectionVirtualView>();
                var topMenu = saveLoadScreen.Find("SaveLoadScreen/Top");
                // get rid of old version of the search bar if it is there
                if (_searchBar == null) {
                    // make new search bar
                    _searchBar = new SearchBar(saveLoadScreen, null, false, "ToyBoxLoadSaveSearchBar");
                    var searchBarTransform = _searchBar.GameObject.transform;

                    Destroy(searchBarTransform.Find("Background").gameObject); // get rid of the fringe backgroundY cuz it looks weird here

                    searchBarTransform.localScale = new Vector3(0.85f, 0.85f, 1.0f);
                    searchBarTransform.localPosition = new Vector2(-677.0f, 462.0f);
                    //searchBarTransform.localPosition = new Vector2(-107.0f, 505.0f);
                    _searchBar.DropdownIconObject?.SetActive(false);
                }
                SaveFileSearchText = "";
                _searchBar.InputField.text = SaveFileSearchText;
                _searchBar.InputField.onValueChanged.AddListener(val => { UpdateSearch(__instance); });
                return true;
            }

            [HarmonyPatch(nameof(SaveLoadPCView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void BindViewImplementation(SaveLoadPCView __instance) {
                if (!Settings.toggleEnhancedLoadSave) return;
                if (_searchBar == null) return;
                _searchBar.FocusSearchBar();
                _collectionVirtualView.m_VirtualList.ScrollController.ForceScrollToTop();
                //FixupSaveSlotCollectionVM(__instance.ViewModel.SaveSlotCollectionVm);
                __instance.ViewModel.UpdateSavesCollection();
            }
        }

        [HarmonyPatch(typeof(SaveLoadVM))]
        public static class SaveLoadVMPatch {
            [HarmonyPatch(MethodType.Constructor, new Type[] {typeof(SaveLoadMode), typeof(bool), typeof(Action), typeof(IUILoadService)})]
            [HarmonyPrefix]
            public static void SaveLoadVMConstructor(           
                SaveLoadVM __instance,
                SaveLoadMode mode,
                bool singleMode,
                Action onClose,
                IUILoadService loadService) {
                Mod.Debug($"SaveLoadVM.Constructor ");
                SaveLoadPCViewPatch.SaveFileSearchText = "";
            }

            [HarmonyPatch(nameof(SaveLoadVM.UpdateSavesCollection))]
            [HarmonyPrefix]
            public static bool UpdateSavesCollection(SaveLoadVM __instance) {
                if (!Settings.toggleEnhancedLoadSave) return true;
                Mod.Debug($"UpdateSavesCollection");
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
                //__instance.m_SaveSlotVMs.Sort((vm1, vm2) => -vm1.SaveTime.Value.CompareTo(vm2.SaveTime.Value));
                foreach (var groupVM in __instance.SaveSlotCollectionVm.SaveSlotGroups) {
                    var items = groupVM.SaveLoadSlots;
                    if (items?.Count() == 0) continue;
                    items.Sort((vm1, vm2) =>  -vm1.Reference.SystemSaveTime.CompareTo(vm2.Reference.SystemSaveTime));
                    Mod.Debug(groupVM.CharacterName.orange());
                    var sortedStrings = items.Select(i => $"{i.Reference.SystemSaveTime} - {i.GameName}");
                    //Mod.Debug($"sorted: {string.Join("\n    ", sortedStrings)}");
                }
                return false;
            }
        }
    }
}