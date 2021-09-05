// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Achievements;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.Settings;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._PCView.Loot;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.Slots;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Owlcat.Runtime.UI.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
using Logger = ModKit.Logger;

namespace ToyBox.BagOfPatches
{
    static class Misc
    {
        public static Settings settings = Main.settings;

        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;

        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(Spellbook), "GetSpellsPerDay")]
        static class Spellbook_GetSpellsPerDay_Patch
        {
            static void Postfix(ref int __result)
            {
                __result = Mathf.RoundToInt(__result * (float)Math.Round(settings.spellsPerDayMultiplier, 1));
            }
        }

        public static BlueprintAbility ExtractSpell([NotNull] ItemEntity item)
        {
            ItemEntityUsable itemEntityUsable = item as ItemEntityUsable;

            if (itemEntityUsable?.Blueprint.Type != UsableItemType.Scroll)
            {
                return null;
            }

            return itemEntityUsable.Blueprint.Ability.Parent ? itemEntityUsable.Blueprint.Ability.Parent : itemEntityUsable.Blueprint.Ability;
        }

        public static string GetSpellbookActionName(string actionName, ItemEntity item, UnitEntityData unit)
        {
            if (actionName != LocalizedTexts.Instance.Items.CopyScroll)
            {
                return actionName;
            }

            BlueprintAbility spell = ExtractSpell(item);

            if (spell == null)
            {
                return actionName;
            }

            List<Spellbook> spellbooks = unit.Descriptor.Spellbooks.Where(x => x.Blueprint.SpellList.Contains(spell)).ToList();

            int count = spellbooks.Count;

            return count <= 0 ? actionName : string.Format("{0} <{1}>", actionName, count == 1 ? spellbooks.First().Blueprint.Name : "Multiple");
        }


        [HarmonyPatch(typeof(ItemSlot), "ScrollContent", MethodType.Getter)]
        public static class ItemSlot_ScrollContent_Patch
        {
            [HarmonyPostfix]
            static void Postfix(ItemSlot __instance, ref string __result)
            {
                UnitEntityData currentCharacter = UIUtility.GetCurrentCharacter();

                CopyItem component = __instance.Item.Blueprint.GetComponent<CopyItem>();

                string actionName = component?.GetActionName(currentCharacter) ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(actionName))
                {
                    actionName = GetSpellbookActionName(actionName, __instance.Item, currentCharacter);
                }

                __result = actionName;
            }
        }

        [HarmonyPatch(typeof(AchievementEntity), "IsDisabled", MethodType.Getter)]
        public static class AchievementEntity_IsDisabled_Patch
        {
            private static void Postfix(ref bool __result, AchievementEntity __instance)
            {
                modLogger.Log("AchievementEntity.IsDisabled");

                if (settings.toggleAllowAchievementsDuringModdedGame)
                {
                    modLogger.Log($"AchievementEntity.IsDisabled - {__result}");

                    __result = Game.Instance.Player.StartPreset.Or(null)?.DlcCampaign != null ||
                               !__instance.Data.OnlyMainCampaign && __instance.Data.SpecificDlc != null &&
                               Game.Instance.Player.StartPreset.Or(null)?.DlcCampaign !=
                               __instance.Data.SpecificDlc?.Get() ||
                               __instance.Data.MinDifficulty != null &&
                               Game.Instance.Player.MinDifficultyController.MinDifficulty.CompareTo(__instance.Data.MinDifficulty.Preset) < 0 ||
                               __instance.Data.IronMan && !(bool)(SettingsEntity<bool>)SettingsRoot.Difficulty.OnlyOneSave;

                    modLogger.Log($"AchievementEntity.IsDisabled - {__result}");
                }
            }
        }

        [HarmonyPatch(typeof(LootSlotPCView), "BindViewImplementation")]
        static class ItemSlot_IsUsable_Patch
        {
            public static void Postfix(ViewBase<ItemSlotVM> __instance)
            {
                if (__instance is LootSlotPCView itemSlotPCView)
                {
                    if (itemSlotPCView.ViewModel.HasItem && itemSlotPCView.ViewModel.IsScroll && settings.toggleHighlightCopyableScrolls)
                    {
                        itemSlotPCView.m_Icon.CrossFadeColor(new Color(0.5f, 1.0f, 0.5f, 1.0f), 0.2f, true, true);
                    }
                    else
                    {
                        itemSlotPCView.m_Icon.CrossFadeColor(Color.white, 0.2f, true, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ItemSlotView<EquipSlotVM>), "RefreshItem")]
        static class ItemSlotView_RefreshItem_Patch
        {
            public static void Postfix(InventoryEquipSlotView __instance)
            {
                if (__instance.SlotVM.HasItem && __instance.SlotVM.IsScroll && settings.toggleHighlightCopyableScrolls)
                {
                    __instance.m_Icon.canvasRenderer.SetColor(new Color(0.5f, 1.0f, 0.5f, 1.0f));
                }
                else
                {
                    __instance.m_Icon.canvasRenderer.SetColor(Color.white);
                }
            }
        }
    }
}