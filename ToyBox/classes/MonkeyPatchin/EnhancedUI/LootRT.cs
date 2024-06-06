using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.BundlesLoading;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence.Scenes;
using Kingmaker.Items;
using Kingmaker.Mechanics.Entities;
using Kingmaker.Modding;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects;
using ModKit;
using ModKit.Utility;
using Owlcat.Runtime.Core.Utility;
using Owlcat.Runtime.UI.MVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using static ToyBox.BlueprintExtensions;
using ItemSlot = Kingmaker.Items.Slots.ItemSlot;

namespace ToyBox.Inventory {
    internal static class Loot {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;
#if false
        public static HashSet<EquipSlotType> SelectedLootSlotFilters = new();
        public static InventoryEquipSlotPCView selectedSlot = null;
        public static bool ToggleSelectedLootFilter(EquipSlotType slotType, bool? forceState = null) {
            var becomeActive = forceState ?? !SelectedLootSlotFilters.Contains(slotType);
            if (becomeActive && !SelectedLootSlotFilters.Contains(slotType)) {
                SelectedLootSlotFilters.Add(slotType);
            }
            else if (SelectedLootSlotFilters.Contains(slotType))
                SelectedLootSlotFilters.Remove(slotType);
            return becomeActive;
        }
        public static void ShowLootFilterFeedback(this InventoryEquipSlotPCView equipSlotView, bool active) {
            if (equipSlotView.gameObject?.transform is { } slotView
                && slotView.Find("CanInsert") is { } canInsert
                && slotView.Find("ChangeVisual") is { } changeVisual) {
                canInsert.gameObject.SetActive(active);
                changeVisual.gameObject.SetActive(active);
                if (active)
                    selectedSlot = equipSlotView;
            }
        }
        public static void SyncLootFilterFeedback(this InventoryEquipSlotView equipSlotView) {
            if (equipSlotView is InventoryEquipSlotPCView slotPCView) {
                var viewModel = equipSlotView.ViewModel;
                var isActive = SelectedLootSlotFilters.Contains(viewModel.SlotType);
                slotPCView.ShowLootFilterFeedback(isActive);
                if (isActive)
                    selectedSlot = slotPCView;
            }
        }
        public static void ClearSelectedLootSlotFilters() {
            SelectedLootSlotFilters.Clear();
            selectedSlot = null;
            SyncSelectedLootSlotFilters();
        }
        public static void SyncSelectedLootSlotFilters() {
            var inventoryView = UIHelpers.InventoryScreen;
            if (inventoryView?.Find("Inventory/Doll")?.GetComponent<InventoryDollPCView>() is InventoryDollPCView dollView) {
                dollView.m_Armor.SyncLootFilterFeedback();
                dollView.m_Belt.SyncLootFilterFeedback();
                dollView.m_Feet.SyncLootFilterFeedback();
                dollView.m_Glasses.SyncLootFilterFeedback();
                dollView.m_Gloves.SyncLootFilterFeedback();
                dollView.m_Head.SyncLootFilterFeedback();
                dollView.m_Neck.SyncLootFilterFeedback();
                dollView.m_Ring1.SyncLootFilterFeedback();
                dollView.m_Ring2.SyncLootFilterFeedback();
                dollView.m_Shirt.SyncLootFilterFeedback();
                dollView.m_Shoulders.SyncLootFilterFeedback();
                dollView.m_Wrist.SyncLootFilterFeedback();
                dollView.m_QuickSlots.ForEach(slot => slot.SyncLootFilterFeedback());
            }
        }
        internal static void SelectedCharacterDidChange() {
            //SyncSelectedLootSlotFilters();
        }
        [HarmonyPatch(typeof(InventoryEquipSlotPCView))]
        private static class InventoryEquipSlotPCViewPatch {
            // Modifies equipment slot background to work with rarity coloring
            [HarmonyPatch(nameof(InventoryEquipSlotPCView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void BindViewImplementation(InventoryEquipSlotPCView __instance) {
                if (Settings.togglEquipSlotInventoryFiltering)
                    __instance.SyncLootFilterFeedback();
                if (!Settings.UsingLootRarity) return;
                var backfill = __instance.gameObject?
                    .transform.Find("Backfill");
                var image = backfill?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredEquipSlotBackgroundColor;
                }
            }
            // Handles Clicks for Equipment Slot -> Inventory Filtering
            [HarmonyPatch(nameof(InventoryEquipSlotPCView.OnClick))]
            [HarmonyPostfix]
            public static void OnClick(InventoryEquipSlotPCView __instance) {
                if (!Settings.togglEquipSlotInventoryFiltering) return;
                TogglEquipSlotInventoryFiltering(__instance);
            }
            [HarmonyPatch(nameof(InventoryEquipSlotPCView.OnDoubleClick))]
            [HarmonyPostfix]
            // This sounds weird but we want double click to reselect it so when you double click to remove something the slot filter stays selected
            public static void OnDoubleClick(InventoryEquipSlotPCView __instance) {
                if (!Settings.togglEquipSlotInventoryFiltering) return;
                TogglEquipSlotInventoryFiltering(__instance);
            }
            internal static void TogglEquipSlotInventoryFiltering(InventoryEquipSlotPCView __instance) {
                var equipSlotVM = __instance.ViewModel;
                var slotType = equipSlotVM.SlotType;
                var isSelected = SelectedLootSlotFilters.Contains(slotType);
                SelectedLootSlotFilters.Clear();
                if (!isSelected) {
                    SelectedLootSlotFilters.Add(slotType);
                }
                SyncSelectedLootSlotFilters();
                EventBus.RaiseEvent((Action<IInventoryHandler>)(h => h.Refresh()));
                //__instance.ShowLootFilterFeedback(show);
                InventoryPCViewPatch.SavedInventoryVM?.StashVM.CollectionChanged();
            }
        }

        // Highlight copyable scolls
        [HarmonyPatch(typeof(LootSlotPCView))]
        private static class LootSlotPCViewPatch {
            [HarmonyPatch(nameof(LootSlotPCView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void BindViewImplementation(ViewBase<ItemSlotVM> __instance) {
                if (__instance is LootSlotPCView itemSlotPCView) {
                    if (itemSlotPCView.ViewModel.HasItem && itemSlotPCView.ViewModel.IsScroll && Settings.toggleHighlightCopyableScrolls) {
                        //                            modLogger.Log($"found {itemSlotPCView.ViewModel}");
                        itemSlotPCView.m_Icon.CrossFadeColor(new Color(0.5f, 1.0f, 0.5f, 1.0f), 0.2f, true, true);
                    }
                    else {
                        itemSlotPCView.m_Icon.CrossFadeColor(Color.white, 0.2f, true, true);
                    }
                }
            }
        }

        // Adds Rarity color circles to items in inventory
        [HarmonyPatch(typeof(ItemSlotView<EquipSlotVM>))]
        private static class ItemSlotViewPatch {
            [HarmonyPatch(nameof(ItemSlotView<EquipSlotVM>.RefreshItem))]
            [HarmonyPostfix]
            public static void RefreshItem(InventoryEquipSlotView __instance) {
                if (__instance.ViewModel.HasItem && __instance.ViewModel.IsScroll && Settings.toggleHighlightCopyableScrolls) {
                    //                            modLogger.Log($"found {itemSlotPCView.ViewModel}");
                    __instance.m_Icon.CrossFadeColor(new Color(0.5f, 1.0f, 0.5f, 1.0f), 0.2f, true, true);
                }
                else {
                    __instance.m_Icon.CrossFadeColor(Color.white, 0.2f, true, true);
                }
                var item = __instance.Item;
                if (Settings.togglEquipSlotInventoryFiltering) {
                    try {
                        if (__instance.gameObject?.transform is { } inventorySlotView
                            && inventorySlotView.Find("Item/NeedCheckLayer") is { } conflictFeedback) {
                            var unit = SelectedCharacterObserver.Shared.SelectedUnit ?? WrathExtensions.GetCurrentCharacter();
                            if (unit != null && item != null && SelectedLootSlotFilters.Any()) {
                                //Mod.Debug($"Unit: {unit.CharacterName}");
                                //Mod.Debug($"Item: {item.Blueprint.GetDisplayName()}");
                                var hasConflicts = unit.HasModifierConflicts(item);
                                conflictFeedback.gameObject.SetActive(hasConflicts);
                                var icon = conflictFeedback.GetComponent<Image>();
                                icon.color = new Color(1.0f, 0.8f, 0.3f, 0.75f);
                            }
                            else 
                                conflictFeedback.gameObject.SetActive(false);
                        }
                    }
                    catch (Exception e) {
                        Mod.Error(e);
                    }
                }
                if (Settings.UsingLootRarity && item != null) {
                    _ = item.Blueprint.GetComponent<AddItemShowInfoCallback>();
                    var cb = item.Get<ItemPartShowInfoCallback>();
                    if (cb != null && (!cb.m_Settings.Once || !cb.m_Triggered)) {
                        // This forces the item to display as notable
                        __instance.SlotVM.IsNotable.SetValueAndForceNotify(true);
                    }
                    var rarity = item.Rarity();
                    var color = rarity.Color();
                    //Main.Log($"ItemSlotView_RefreshItem_Patch - {item.Name} - {color}");
                    if (rarity == RarityType.Notable && __instance.m_NotableLayer != null) {
                        var objFX = __instance.m_NotableLayer.Find("NotableLayerFX");
                        if (objFX != null && objFX.TryGetComponent<Image>(out var image)) image.color = color;
                    }
                    else if (rarity != RarityType.Notable) {
                        if (rarity >= RarityType.Uncommon) // Make sure things uncommon or better get their color circles despite not being magic. Colored loot offers sligtly different UI assumptions
                            __instance.SlotVM.IsMagic.SetValueAndForceNotify(true);

                        if (__instance.m_MagicLayer != null) {
                            var ratingAlpha = (float)Math.Min(120, item.Rating() + 20) / 120;
                            var colorTranslucent = new Color(color.r, color.g, color.b, color.a * ratingAlpha); // 0.45f);
                            var obj = __instance.m_MagicLayer.gameObject;
                            obj.GetComponent<Image>().color = colorTranslucent;
                            var objFX = __instance.m_MagicLayer.Find("MagicLayerFX");
                            if (objFX != null && objFX.TryGetComponent<Image>(out var image)) image.color = color;
                        }
                    }
                }
            }
        }

        // Adds Rarity tags/colors to item names
        [HarmonyPatch(typeof(ItemEntity), nameof(ItemEntity.Name), MethodType.Getter)]
        private static class ItemEntity_Name_Patch {
            public static void Postfix(ItemEntity __instance, ref string __result) {
                if (!Settings.UsingLootRarity && !Settings.togglePuzzleRelief && __result == null && __result.Length == 0) return;
                var bp = __instance.Blueprint;
                if (Settings.togglePuzzleRelief && bp is BlueprintItem bpItem && bpItem.NameForAcronym.Contains("Domino")) {
                    var text = bpItem.NameForAcronym.Replace("Domino", "");
                    var truncateIndex = text.IndexOf("_Slot");
                    if (truncateIndex >= 0)
                        text = text.Substring(0, truncateIndex);
                    if (Settings.toggleColorLootByRarity) {
                        var rarity = __instance.Rarity();
                        __result = __result.Rarity(rarity);
                    }
                    __result = __result + $"\n[Puzzle Piece: {text}]";
                    return;
                }
                if (Settings.UsingLootRarity) {
                    var rarity = __instance.Rarity();
                    if (rarity < Settings.minRarityToColor) return;
                    if (bp is BlueprintItemWeapon bpWeap && !bpWeap.IsMagic && rarity < RarityType.Uncommon) return;
                    if (bp is BlueprintItemArmor bpArmor && !bpArmor.IsMagic && rarity < RarityType.Uncommon) return;
                    var result = __result.RarityInGame(rarity);
                    //Main.Log($"ItemEntity_Name_Patch - Name: {__result} type:{__instance.GetType().FullName} - {rarity.ToString()} -> {result}");
                    __result = result;
                }

            }
        }

        internal static Color ColoredLootBackgroundColor = new(1f, 1f, 1f, 0.25f);
        internal static Color ColoredEquipSlotBackgroundColor = new(1f, 1f, 1f, 0.45f);

        // Modifies inventory slot background to work with rarity coloring
        [HarmonyPatch(typeof(InventoryPCView))]
        private static class InventoryPCViewPatch {
            public static InventoryVM SavedInventoryVM = null;
            [HarmonyPatch(nameof(InventoryPCView.BindViewImplementation))]
            [HarmonyPostfix]
            public static void BindViewImplementation(InventoryPCView __instance) {
                SavedInventoryVM = __instance.ViewModel;
                if (Settings.togglEquipSlotInventoryFiltering) {
                    ClearSelectedLootSlotFilters();
                    SavedInventoryVM.StashVM.CollectionChanged();
                    SelectedCharacterObserver.Shared.Notifiers -= SelectedCharacterDidChange;
                    SelectedCharacterObserver.Shared.Notifiers += SelectedCharacterDidChange;
                }
                if (!Settings.UsingLootRarity) return;
                var decoration = __instance.gameObject?
                    .transform.Find("Inventory/Stash/StashContainer/StashScrollView/decoration");
                var image = decoration?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredLootBackgroundColor;
                }
            }
            [HarmonyPatch(nameof(InventoryPCView.HideWindow))]
            [HarmonyPrefix]
            public static void OnHide() {
                Mod.Log("InventoryPCView.HideWindow");
                ClearSelectedLootSlotFilters();
                SelectedCharacterObserver.Shared.Notifiers -= SelectedCharacterDidChange;
            }
        }
        // modifies weapon slot backgrounds to work with rarity coloring
        [HarmonyPatch(typeof(WeaponSetPCView), nameof(WeaponSetPCView.BindViewImplementation))]
        private static class WeaponSetPCView_BindViewImplementation_Patch {
            public static void Postfix(WeaponSetPCView __instance) {
                if (!Settings.UsingLootRarity) return;
                var selected = __instance.gameObject?
                    .transform.Find("SelectedObject");
                var image = selected?.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = ColoredLootBackgroundColor;
                }
            }
        }

        // modifies slot backgrounds in chests and such to work with rarity coloring
        [HarmonyPatch(typeof(LootCollectorPCView), nameof(VendorPCView.BindViewImplementation))]
        private static class LootCollectorPCView_BindViewImplementation_Patch {
            public static void Postfix(LootCollectorPCView __instance) {
                if (!Settings.UsingLootRarity) return;
                var image = __instance.gameObject?
                    .transform.Find("Collector/StashScrollView/Decoration")?
                    .GetComponent<UnityEngine.UI.Image>();
                image.color = ColoredLootBackgroundColor;

            }
        }

        // modify vender slot backgrounds to work with rarity coloring
        [HarmonyPatch(typeof(VendorPCView), nameof(VendorPCView.BindViewImplementation))]
        private static class VendorPCView_BindViewImplementation_Patch {
            public static void Postfix(VendorPCView __instance) {
                if (!Settings.UsingLootRarity) return;
                var vendorImage = __instance.gameObject?
                    .transform.Find("MainContent/VendorBlock/VendorStashScrollView/decoration")?
                    .GetComponent<UnityEngine.UI.Image>();
                vendorImage.color = ColoredLootBackgroundColor;
                var playerImage = __instance.gameObject?
                    .transform.Find("MainContent/PlayerStash/StashScrollView/decoration")?
                    .GetComponent<UnityEngine.UI.Image>();
                playerImage.color = ColoredLootBackgroundColor;
            }
        }

        // modifies tooltip title background color
        [HarmonyPatch(typeof(TooltipBrickEntityHeaderView), nameof(TooltipBrickEntityHeaderView.BindViewImplementation))]
        private static class TooltipBrickEntityHeaderView_BindViewImplementation_Patch {
            public static void Postfix(TooltipBrickEntityHeaderView __instance) {
                if (!Settings.UsingLootRarity) return;
                var image = __instance.gameObject?
                    .transform.Find("TextBlock/Back/ItemBackContainer")?
                    .GetComponent<UnityEngine.UI.Image>();
                image.color = ColoredLootBackgroundColor;

#if false
                __instance.m_MainTitle.color = (Color32)Color.green;
                __instance.m_MainTitle.overrideColorTags = true;
                __instance.m_MainTitle.outlineColor = (Color32)Color.magenta;
                __instance.m_MainTitle.outlineWidth = 5;
#endif
            }
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.CanRemoveItem))]
        private static class ItemSlot_CanRemoveItem_Patch {
            public static void Postfix(ref bool __result) {
                if (Settings.toggleOverrideLockedItems)
                    __result = true;
            }
        }
#endif
        [HarmonyPatch(typeof(MassLootHelper), nameof(MassLootHelper.GetMassLootFromCurrentArea))]
        public static class PatchLootEverythingOnLeave_Patch {
            public static bool Prefix(ref IEnumerable<LootWrapper> __result) {
                if (!Settings.toggleMassLootEverything) return true;
                IEnumerable<BaseUnitEntity> all_units = Shodan.AllBaseUnits.All;
                if (Settings.toggleLootAliveUnits) {
                    all_units = all_units.Where(unit => unit.IsInGame && (unit.IsDeadAndHasLoot || unit.Inventory.HasLoot));
                } else {
                    all_units = all_units.Where(unit => unit.IsInGame && unit.IsDeadAndHasLoot);
                }

                var result_units = all_units.Select(unit => new LootWrapper { Unit = unit });

                var all_entities = Game.Instance.State.Entities.All.Where(w => w.IsInGame);
                var all_chests = all_entities.SelectMany(s => s.GetAll<InteractionLootPart>()).Where(i => i?.Loot != Game.Instance.Player.SharedStash).NotNull();

                var tmp = TempList.Get<InteractionLootPart>();

                foreach (var i in all_chests) {
                    //if (i.Owner.IsRevealed
                    //    && i.Loot.HasLoot
                    //    && (i.LootViewed
                    //        || (i.View is DroppedLoot && !i.Owner.Get<DroppedLoot.EntityPartBreathOfMoney>())
                    //        || i.View.GetComponent<SkinnedMeshRenderer>()))
                    if (i.Loot.HasLoot) {
                        tmp.Add(i);
                    }
                }

                var result_chests = tmp.Distinct(new MassLootHelper.LootDuplicateCheck()).Select(i => new LootWrapper { InteractionLoot = i });

                __result = result_units.Concat(result_chests);
#if false
                foreach (var loot in __result) // showing inventories from living enemies makes the items invisible (also they can still be looted with the Get All option)
                {
                    if (loot.Unit != null)
                    ;
                    if (loot.InteractionLoot != null)
                    ;
                }
#endif
                return false;
            }
        }
#if false
        [HarmonyPatch(typeof(SceneLoader), nameof(SceneLoader.MatchStateWithScene))]
        static class SceneLoader_MatchStateWithScene_Patch
        {
            const string BundleName = "dungeons_areshkagal.worldtex";
            static readonly HashSet<string> areshKagalNames = new HashSet<string> {
                "areshkagal_puzzle_cian_d", "areshkagal_puzzle_cian_dark_d",  "areshkagal_puzzle_cian_m",
                "areshkagal_puzzle_green_d", "areshkagal_puzzle_green_dark_d",
                "areshkagal_puzzle_purple_d", "areshkagal_puzzle_purple_dark_d",
                "areshkagal_puzzle_red_d", "areshkagal_puzzle_red_dark_d",
                "areshkagal_puzzle_yellow_d", "areshkagal_puzzle_yellow_dark_d"
            };

            static void Prefix(SceneEntitiesState state) {
                if (!Settings.togglePuzzleRelief) return;
                Mod.Debug("SceneLoader_MatchStateWithScene_Patch");
                string sceneName = state.SceneName;
                Mod.Debug(sceneName);
                string sceneBundleName = BundledSceneLoader.GetBundleName(sceneName);
                DependencyData dependency = OwlcatModificationsManager.Instance.GetDependenciesForBundle(sceneBundleName) ?? BundlesLoadService.Instance.m_DependencyData;
                dependency.BundleToDependencies.TryGetValue(sceneBundleName, out var list);
                Mod.Debug((list.Any(d => d.Equals(BundleName))).ToString());

                string[] s = state.SceneName.Split('_');
#if false
                if (   !s.Any(p => p.Equals("GlobalPuzzle"))
                       || !s.Any(p => p.Equals("Mechanics"))
                       ||  s.Any(p => p.Equals("Cave"))
                       ||  s.Any(p => p.Equals("Puzzle")))
                    return;
#endif
                var textures = BundlesLoadService.Instance.RequestBundle(BundleName)?.LoadAllAssets<Texture2D>();
                if (textures is null) {
                    Mod.Log($"Failed to load the {BundleName} bundle.");
                    return;
                }
                Mod.Debug($"Found Asset Bundle named {BundleName}.");
                Texture2D[] matches = textures.Where(t => areshKagalNames.Contains(t.name)).ToArray();
                Mod.Debug($"Found {matches.Length} matches");
                var assembly = Assembly.GetExecutingAssembly();
                foreach (var t in matches) {
#if false
                    var name = "ToyBox.Art.Texture2D." + t.name + ".png";
                    t.LoadImage(File.ReadAllBytes(Path.Combine(Main.modPath, "Icons", textureName + ".png")));

#else
                    var name = "ToyBox.Art.Texture2D." + t.name + ".png";
                    try {
                        var stream = assembly.GetManifestResourceStream(name);
                        if (stream != null) {
                            var buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            t.LoadImage(buffer);
                        }
                        else {
                            Mod.Error($"BundlesLoadService_RequestBundle_Patch - failed to load {name} from {assembly.FullName}");
                            var resourceNames = assembly.GetManifestResourceNames();
                            Mod.Log(string.Join("\n", resourceNames));
                        }
                    }
                    catch (Exception e) {
                        Mod.Error(e);
                    }
                }
#endif
            }
        }
#if false
        [HarmonyPatch(typeof(BundlesLoadService), nameof(BundlesLoadService.RequestBundle))]
        static class BundlesLoadService_RequestBundle_Patch {
            const string BundleName = "dungeons_areshkagal.worldtex";
            static readonly HashSet<string> areshKagalNames = new HashSet<string> {
                "areshkagal_puzzle_cian_d", "areshkagal_puzzle_cian_dark_d",
                "areshkagal_puzzle_green_d", "areshkagal_puzzle_green_dark_d",
                "areshkagal_puzzle_purple_d", "areshkagal_puzzle_purple_dark_d",
                "areshkagal_puzzle_red_d", "areshkagal_puzzle_red_dark_d",
                "areshkagal_puzzle_yellow_d", "areshkagal_puzzle_yellow_dark_d"
            };

            static void Postfix(ref AssetBundle __result) {
                if (!Settings.togglePuzzleRelief) return;
                if (!__result.name.Contains(BundleName)) return;
                Mod.Log($"Found Asset Bundle named {BundleName}.");
                Texture2D[] textures = __result.LoadAllAssets<Texture2D>();
                Texture2D[] matches = textures.Where(t => areshKagalNames.Contains(t.name)).ToArray();
                Mod.Log($"Found {matches.Length} matches");
                var assembly = Assembly.GetExecutingAssembly();
                foreach (var t in matches) {
                    var name = "ToyBox.Art.Texture2D." + t.name + ".png";
                    try {
                        var stream = assembly.GetManifestResourceStream(name);
                        if (stream != null) {
                            var buffer = new byte[stream.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            t.LoadImage(buffer);
                        }
                        else {
                            Mod.Error($"BundlesLoadService_RequestBundle_Patch - failed to load {name} from {assembly.FullName}");
                            var resourceNames = assembly.GetManifestResourceNames();
                            Mod.Log(string.Join("\n", resourceNames));
                        }
                    }
                    catch (Exception e) {
                        Mod.Error(e);
                    }
                }
            }
        }
#endif
#endif
    }
}

