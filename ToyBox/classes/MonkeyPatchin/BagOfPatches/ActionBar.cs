using HarmonyLib;
using Kingmaker;
using Kingmaker.UI.MVVM._PCView.ActionBar;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.UnitSettings;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace ToyBox.BagOfPatches {
    internal static class ActionBar {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;


        [HarmonyPatch(typeof(ActionBarGroupPCView), nameof(ActionBarGroupPCView.SetStatePosition))]
        public static class ActionBarGroupPCViewSetStatePosition_Patch {
            public static void Prefix(ActionBarGroupPCView __instance) {
                if (!settings.toggleWidenActionBarGroups) return;
                var isSpellGroup = __instance is ActionBarSpellGroupPCView;
                var rectTransform = __instance.RectTransform;
                var itemCount = __instance.m_SlotsList.Count(s => !s.ViewModel?.IsEmpty.Value ?? false);
                var columnCount = Math.Max(5, (int)(0.85 * Math.Sqrt(itemCount)));
                var rowCount = (int)(Math.Ceiling((float)itemCount / columnCount));
                if (isSpellGroup) {
                    //rowCount += 1; // nudge for spell group ??? TODO - why?
                    rowCount = Math.Max(rowCount, (int)Math.Ceiling((((__instance as ActionBarSpellGroupPCView).m_Levels.Count - 1) * 26)/ 53f)) + 1;
                }
                var width = columnCount * 53f;
                var xoffset = (columnCount - 5) * 53f;
                var height = rowCount * 53f + 40 + (isSpellGroup ? 5 : 0);
                var oldOffset = rectTransform.offsetMax;
                var oldSize = rectTransform.sizeDelta;
                //Mod.Debug($"ActionBarGroupPCViewSetStatePosition_Patch - itemCount:{itemCount} width:{oldSize.x} - > {width}");
                rectTransform.offsetMax = new Vector2(width, height);
                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }


        [HarmonyPatch(typeof(ActionBarBaseSlotPCView), nameof(ActionBarBaseSlotPCView.BindViewImplementation))]
        public static class ActionBarBaseSlotPCView_BindViewImplementation_Patch {
            public static void Postfix(ActionBarBaseSlotPCView __instance) {
                if (!settings.toggleShowAcronymsInSpellAndActionSlots) return;
                var viewModel = __instance.ViewModel;
                var icon = __instance.transform.Find("BackgroundIcon");
                var mechanicSlot = viewModel.MechanicActionBarSlot;
                var name = mechanicSlot.GetTitle();
                switch (mechanicSlot) {
                    case MechanicActionBarSlotAbility abilitySlot: name = abilitySlot.GetTitle(); break;
                    case MechanicActionBarSlotActivableAbility activatableSlot: name = activatableSlot.GetTitle(); break;
                    case MechanicActionBarSlotGlobalMagicSpell globalMagicSpellSlot: name = globalMagicSpellSlot.GetTitle(); break;
                    case MechanicActionBarSlotItem itemSlot: name = itemSlot.GetTitle(); break;
                    case MechanicActionBarSlotSpell spellSlot: name = spellSlot.GetTitle(); break;
                    case MechanicActionBarSlotSpontaneusConvertedSpell convSpellSlot: name = convSpellSlot.GetTitle(); break;
                }
                name = name.StripHTML();
                if (name?.Length <= 0) return;
                var title = string.Join("", name.Split(' ').Select(s => s[0]).Where(c => Char.IsLetter(c)).Take(4));
                //Mod.Debug($"mechanicSlot: {mechanicSlot} : {mechanicSlot.GetType()} - {name} => {title}");
                var acronym = __instance.transform.Find("BackgroundIcon/ActionBarAcronym-ToyBox");
                if (acronym == null) {
                    var count = __instance.transform.Find("BackgroundIcon/Count");
                    acronym = UnityEngine.Object.Instantiate(count, icon.transform);
                    acronym.transform.SetSiblingIndex(4);
                    acronym.name = "ActionBarAcronym-ToyBox";
                }
                var rectTransform = acronym.transform as RectTransform;
                var len = title.Length;
                rectTransform.anchorMin = new Vector2(.95f - 0.09f * Math.Max(0, 4 - len), 0.15f); // - 0.35f);
                rectTransform.anchorMax = new Vector2(1.0f - 0.09f * Math.Max(0, 4 - len), 0.35f); // - 0.35f);
                var percent = len <= 3 ? 100 : len < 4 ? 100 : len < 5 ? 83 : 75;
                acronym.GetComponentInChildren<TextMeshProUGUI>().text = $"<size={percent}%>{title}</size>";
                acronym.gameObject.SetActive(true);
                if (acronym.gameObject.GetComponent<UICornerCut>() == null) {
                    var cornerCut = acronym.gameObject.AddComponent<UICornerCut>();
                    cornerCut?.gameObject?.SetActive(true);
                }
            }
        }
    }
}
#if false 
                rectTransform.anchorMin = new Vector2(.95f, .15f);
                rectTransform.anchorMax = new Vector2(1.0f, .35f);
                var prototypeText = __instance.gameObject.transform.parent.transform.Find("Background/Header/HeaderText");
                var text = GameObject.Instantiate(prototypeText, __instance.transform);
                var mesh = text.GetComponent<TextMeshProUGUI>();

                var mechanicSlot = viewModel.MechanicActionBarSlot;
                var name = "";
                switch (mechanicSlot) {
                    case MechanicActionBarSlotAbility abilitySlot: name = abilitySlot.Ability.NameForAcronym; break;
                    case MechanicActionBarSlotActivableAbility activatableSlot: name = activatableSlot.ActivatableAbility.NameForAcronym; break;
                    case MechanicActionBarSlotGlobalMagicSpell globalMagicSpellSlot: name = globalMagicSpellSlot.SpellState.Blueprint.name; break;
                    case MechanicActionBarSlotItem itemSlot: name = itemSlot.Item.NameForAcronym; break;
                    case MechanicActionBarSlotSpell spellSlot: name = spellSlot.Spell.NameForAcronym; break;
                    case MechanicActionBarSlotSpontaneusConvertedSpell convSpellSlot: name = convSpellSlot.Spell.NameForAcronym; break;
                }
                string.Concat(name.Where(c => c >= 'A' && c <= 'Z'));
#endif
