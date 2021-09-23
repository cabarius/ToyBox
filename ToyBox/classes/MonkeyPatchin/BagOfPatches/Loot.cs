using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Items;
using Kingmaker.Items.Parts;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.Loot;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.Slots;
using Owlcat.Runtime.UI.MVVM;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;

namespace ToyBox.BagOfPatches {
    static class Loot {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;
        public static Player player = Game.Instance.Player;

        //        [HarmonyPatch(typeof(ItemSlot), "SetupEquipPossibility")]

        [HarmonyPatch(typeof(LootSlotPCView), "BindViewImplementation")]
        static class ItemSlot_IsUsable_Patch {
            public static void Postfix(ViewBase<ItemSlotVM> __instance) {
                if (__instance is LootSlotPCView itemSlotPCView) {
                    //                        modLogger.Log($"checking  {itemSlotPCView.ViewModel.Item}");
                    if (itemSlotPCView.ViewModel.HasItem && itemSlotPCView.ViewModel.IsScroll && settings.toggleHighlightCopyableScrolls) {
                        //                            modLogger.Log($"found {itemSlotPCView.ViewModel}");
                        itemSlotPCView.m_Icon.CrossFadeColor(new Color(0.5f, 1.0f, 0.5f, 1.0f), 0.2f, true, true);
                    } else {
                        itemSlotPCView.m_Icon.CrossFadeColor(Color.white, 0.2f, true, true);
                    }
                    if (settings.toggleColorLootByRarity) {
                        // TODO - figure this out for loot containers
#if false
                        var magicGO = __instance.m_MagicLayer.gameObject;
                        var color = __instance.Item.Blueprint.Rarity().Color();
                        var colorOpaque = new Color(color.r * 0.9f, color.g * 0.9f, color.b * 0.9f, color.a * 0.9f);
                        var colorTranslucent = new Color(color.r, color.g, color.b, color.a * 0.65f);
                        magicGO.GetComponent<Image>().color = colorTranslucent;
                        var magicFXGO = __instance.m_MagicLayer.FindChild("MagicLayerFX");
                        magicFXGO.GetComponent<Image>().color = color;
#endif
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlotView<EquipSlotVM>), "RefreshItem")]
        static class ItemSlotView_RefreshItem_Patch {
            public static void Postfix(InventoryEquipSlotView __instance) {
                if (!__instance.SlotVM.HasItem || !__instance.SlotVM.IsScroll) {
                    __instance.m_Icon.canvasRenderer.SetColor(Color.white);
                } else if (__instance.SlotVM.IsScroll) {
                    __instance.m_Icon.canvasRenderer.SetColor(new Color(0.5f, 1.0f, 0.5f, 1.0f));
                }
                if (settings.toggleColorLootByRarity && __instance.Item != null) { 
                    var component = __instance.Item.Blueprint.GetComponent<AddItemShowInfoCallback>();
                    ItemPartShowInfoCallback cb = __instance.Item.Get<ItemPartShowInfoCallback>();
                    if (cb != null && (!cb.m_Settings.Once || !cb.m_Triggered)) {
                        // This forces the item to display as notable
                        __instance.SlotVM.IsNotable.SetValueAndForceNotify(true);
                    } else {

                    }
                    var rarity = __instance.Item.Blueprint.Rarity();
                    var color = rarity.Color();
                    var colorTranslucent = new Color(color.r, color.g, color.b, color.a * 0.65f);
                    if (rarity == RarityType.Notable) {
                        var objFX = __instance.m_NotableLayer.Find("NotableLayerFX");
                        objFX.GetComponent<Image>().color = color;
                    } else {
                        var obj = __instance.m_MagicLayer.gameObject;
                        obj.GetComponent<Image>().color = colorTranslucent;
                        var objFX = __instance.m_MagicLayer.Find("MagicLayerFX");
                        objFX.GetComponent<Image>().color = color;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintItem), nameof(BlueprintItem.Name), MethodType.Getter)]
        static class BlueprintItem_Name_Patch {
            public static void Postfix(BlueprintItem __instance, ref string __result) {
                if (settings.toggleColorLootByRarity && __result != null && __result.Length > 0) {
                    if (__instance is BlueprintItemWeapon bpWeap && !bpWeap.IsMagic) return;
                    if (__instance is BlueprintItemArmor bpArmor && !bpArmor.IsMagic) return;
                    var rarity = __instance.Rarity();
                    var result = __result.Rarity(rarity);
                    //Main.Log($"BlueprintItem - Name: {__result} - {rarity.ToString()} -> {result}");
                    __result = result;
                }
            }
        }
#if false
        [HarmonyPatch(typeof(UIUtility), nameof(UIUtility.IsMagicItem))]
        static class UIUtility_IsMagicItem_Patch {
            public static void PostFix(ItemEntity item, ref bool __result) {
                if (settings.toggleColorLootByRarity) {
                    var component = item.Blueprint.GetComponent<AddItemShowInfoCallback>();
                    Main.Log($"BlueprintItem - IsMagicItem: {item.Name} - {component}");
                    __result = __result || component != null;
                }
            }
        }
#endif
    }
}