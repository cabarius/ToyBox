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

namespace ToyBox.BagOfPatches {
    internal static class ActionBar {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(ActionBarBaseSlotPCView), nameof(ActionBarBaseSlotPCView.BindViewImplementation))]
        public static class ActionBarBaseSlotPCView_BindViewImplementation_Patch {
            public static void Postfix(ActionBarBaseSlotPCView __instance) {
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
                var title = string.Join("", name.Split(' ').Select(s => s[0]).Where(c => Char.IsLetter(c)).Take(4));
                //Mod.Debug($"mechanicSlot: {mechanicSlot} : {mechanicSlot.GetType()} - {name} => {title}");
                var acronym = __instance.transform.Find("BackgroundIcon/ActionBarAcronym-ToyBox");
                if (acronym == null) {
                    var count = __instance.transform.Find("BackgroundIcon/Count");
                    acronym = GameObject.Instantiate(count, icon.transform);
                    acronym.transform.SetSiblingIndex(4);
                    acronym.name = "ActionBarAcronym-ToyBox";
                }
                var rectTransform = acronym.transform as RectTransform;
                var len = title.Length;
                rectTransform.anchorMin = new Vector2(.95f - 0.09f*(4 - len) , 0.15f);
                rectTransform.anchorMax = new Vector2(1.0f - 0.09f*(4 - len), 0.35f);
                var percent = len <= 3 ? 100 : len < 4 ? 100 : len < 5 ? 83 : 75;
                acronym.GetComponentInChildren<TextMeshProUGUI>().text = $"<size={percent}%>{title}</size>";
                acronym.gameObject.SetActive(true);
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
