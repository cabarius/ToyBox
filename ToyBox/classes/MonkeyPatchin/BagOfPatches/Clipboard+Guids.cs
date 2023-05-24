﻿using HarmonyLib;
using Kingmaker.PubSubSystem;
using Owlcat.Runtime.UI.Tooltips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModKit;
using UnityEngine;
using ModKit.Utility;
#if Wrath
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._VM.ActionBar;
using Kingmaker.UI.MVVM._VM.Tooltip.Bricks;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.UnitSettings;
#elif RT
using Kingmaker.Code.UI.MVVM.VM.Tooltip.Templates;
using Kingmaker.Code.UI.MVVM.View.Slots;
using Kingmaker.Code.UI.MVVM.VM.Tooltip.Bricks;
using Kingmaker.UI.Models.UnitSettings;
using Kingmaker.Code.UI.MVVM.VM.ActionBar;
#endif

namespace ToyBox.classes.MonkeyPatchin.BagOfPatches {
    public static class GUIDTooltipStyes {
#if Wrath
        public static string GUIDTooltipStyle(this string guid) => $"<color=grey>guid: {guid}</color>";
#elif RT
        public static string GUIDTooltipStyle(this string guid) => $"<color=grey>guid: {guid}</color>".sizePercent(85);
#endif
    }

    [HarmonyPatch]
    public class Clipboard_Guids {
        public static Settings settings = Main.Settings;
#if Wrath
        private const TooltipTextType GUIDTooltipTextTypes = TooltipTextType.Small | TooltipTextType.Italic;
#elif RT
        const TooltipTextType GUIDTooltipTextTypes = TooltipTextType.Simple | TooltipTextType.Italic;
#endif

        public static void CopyToClipboard(string guid) {
            GUIUtility.systemCopyBuffer = guid;
            EventBus.RaiseEvent<IWarningNotificationUIHandler>(h => h.HandleWarning("Copied Guid to clipboard: " + guid, false));
        }

        [HarmonyPatch(typeof(TooltipTemplateAbility), nameof(TooltipTemplateAbility.GetBody))]
        [HarmonyPostfix]
        public static void Tooltip1(TooltipTemplateAbility __instance, ref IEnumerable<ITooltipBrick> __result) {
            if (!settings.toggleGuidsClipboard)
                return;

            var list = __result.ToList();

            var guid = __instance.BlueprintAbility?.AssetGuidThreadSafe;
            list.Insert(0, new TooltipBrickText(guid.GUIDTooltipStyle(), GUIDTooltipTextTypes));

            __result = list;
        }

        [HarmonyPatch(typeof(TooltipTemplateActivatableAbility), nameof(TooltipTemplateActivatableAbility.GetBody))]
        [HarmonyPostfix]
        public static void Tooltip2(TooltipTemplateActivatableAbility __instance, ref IEnumerable<ITooltipBrick> __result) {
            if (!settings.toggleGuidsClipboard)
                return;

            var list = __result.ToList();

            var guid = __instance.BlueprintActivatableAbility?.AssetGuidThreadSafe;
            var guid2 = __instance.BlueprintActivatableAbility?.m_Buff?.Guid.ToString();
            list.Insert(0, new TooltipBrickText($"{guid}\nbuff: {guid2}".GUIDTooltipStyle(), GUIDTooltipTextTypes));

            __result = list;
        }

        [HarmonyPatch(typeof(TooltipTemplateItem), nameof(TooltipTemplateItem.GetBody))]
        [HarmonyPostfix]
        public static void Tooltip3(TooltipTemplateItem __instance, ref IEnumerable<ITooltipBrick> __result) {
            if (!settings.toggleGuidsClipboard)
                return;

            var list = __result.ToList();

            var guid = __instance.m_BlueprintItem?.AssetGuidThreadSafe ?? __instance.m_Item?.Blueprint?.AssetGuidThreadSafe;
            list.Insert(0, new TooltipBrickText(guid.GUIDTooltipStyle(), GUIDTooltipTextTypes));

            __result = list;
        }

        [HarmonyPatch(typeof(TooltipTemplateBuff), nameof(TooltipTemplateBuff.GetBody))]
        [HarmonyPostfix]
        public static void Tooltip4(TooltipTemplateBuff __instance, ref IEnumerable<ITooltipBrick> __result) {
            if (!settings.toggleGuidsClipboard)
                return;

            var list = __result.ToList();

            var guid = __instance.Buff?.Blueprint?.AssetGuidThreadSafe;
            list.Insert(0, new TooltipBrickText(guid.GUIDTooltipStyle(), GUIDTooltipTextTypes));

            __result = list;
        }

        [HarmonyPatch(typeof(ActionBarSlotVM), nameof(ActionBarSlotVM.OnMainClick))]
        [HarmonyPrefix]
        [HarmonyPriority(390)]
        public static bool LeftClickToolbar(ActionBarSlotVM __instance) {
            if (!settings.toggleGuidsClipboard)
                return true;

            if (Input.GetMouseButtonUp(0) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                switch (__instance.MechanicActionBarSlot) {
                    case MechanicActionBarSlotAbility ab:
                        CopyToClipboard(ab.Ability.Blueprint.AssetGuidThreadSafe);
                        return false;
                    case MechanicActionBarSlotActivableAbility act:
                        CopyToClipboard(act.ActivatableAbility.Blueprint.AssetGuidThreadSafe);
                        return false;
                    case MechanicActionBarSlotItem item:
                        CopyToClipboard(item.Item.Blueprint.AssetGuidThreadSafe);
                        return false;
                    case MechanicActionBarSlotSpell spell:
                        CopyToClipboard(spell.Spell.Blueprint.AssetGuidThreadSafe);
                        return false;
                    case MechanicActionBarSlotSpontaneusConvertedSpell cspell:
                        CopyToClipboard(cspell.Spell.Blueprint.AssetGuidThreadSafe);
                        return false;
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(ItemSlotPCView), nameof(ItemSlotPCView.OnClick))]
        [HarmonyPrefix]
        public static void LeftClickItem(ItemSlotPCView __instance) {
            if (!settings.toggleGuidsClipboard)
                return;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                string guid = __instance.ViewModel?.Item?.Value?.Blueprint?.AssetGuidThreadSafe;
                if (guid != null)
                    CopyToClipboard(guid);
            }
        }
    }
}
