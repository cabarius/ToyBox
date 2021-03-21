using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

using GL = UnityEngine.GUILayout;

namespace ToyBox
{
    public class UI
    {
        /*** ToyBox UI
         * 
         * This is a simple UI framework that simulates the style of SwiftUI.  
         * 
         * Usage - these are intended to be called from any OnGUI render path usedd in your mod
         * 
         * Elements will be defined like this
                UI.Section("Cheap Tricks", () =>
                {
                    UI.HStack("Combat", 4,
                        () => { UI.ActionButton("Rest All", () => { CheatsCombat.RestAll(); }); },
                        () => { UI.ActionButton("Empowered", () => { CheatsCombat.Empowered(""); }); },
                        () => { UI.ActionButton("Full Buff Please", () => { CheatsCombat.RestAll(); }); },
                        () => { UI.ActionButton("Remove Death's Door", () => { CheatsCombat.Empowered(""); }); },
                        () => { UI.ActionButton("Kill All Enemies", () => { CheatsCombat.KillAll(); }); },
                        () => { UI.ActionButton("Summon Zoo", () => { CheatsCombat.SpawnInspectedEnemiesUnderCursor(""); }); }
                     );
                    UI.Space(10);
                    UI.HStack("Common", 4,
                        () => { UI.ActionButton("Change Weather", () => { CheatsCommon.ChangeWeather(""); }); },
                        () => { UI.ActionButton("Set Perception to 40", () => { CheatsCommon.StatPerception(); }); }
                     );
                    UI.Space(10);
                    UI.HStack("Unlocks", 4,
                        () => { UI.ActionButton("Give All Items", () => { CheatsUnlock.CreateAllItems(""); }); }
                     );
                });
        */

        public const string onMark = "<color=green><b>✔</b></color>";
        public const string offMark = "<color=red><b>✖</b></color>";
        public const string disclosureArrowOn = "<color=orange><b>▶</b></color>";
        public const string disclosureArrowOff = "<color=white><b>▲</b></color>";

        // UI Elements

        public static void Space(float size = 150f) { GL.Space(size); }

        public static void Label(String title, params GUILayoutOption[] options)
        {
            // var content = tooltip == null ? new GUIContent(title) : new GUIContent(title, tooltip);
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(150f) }; }
            GL.Label(title, options);
        }

        public static void ActionButton(String title, Action action, params GUILayoutOption[] options)
        {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300f) }; }
            if (GL.Button(title, options)) { action(); }
        }

        static void TogglePrivate(
            String title,
            ref bool value,
            bool disclosureStyle = true,
            params GUILayoutOption[] options)
        {
            if (!disclosureStyle)
            {
                if (GL.Button(title + " " + (value ? onMark : offMark), GL.ExpandWidth(false))) { value = !value; }
            }
            else
            {
                UI.Label(title, GL.ExpandWidth(false));
                GL.Space(10);
                if (GL.Button(value ? disclosureArrowOn : disclosureArrowOff, GL.ExpandWidth(false))) { value = !value; }
            }
        }

        public static void Toggle(
            String title,
            ref bool value,
            params GUILayoutOption[] options)
        {
            TogglePrivate(title, ref value, false, options);
        }

        public static void BitFieldToggle(
            String title,
            ref int bitfield,
            int offset,
            params GUILayoutOption[] options)
        {
            bool bit = ((1 << offset) & bitfield) != 0;
            bool newBit = bit;
            TogglePrivate(title, ref newBit, false, options);
            if (bit != newBit) { bitfield ^= 1 << offset; }
        }

        public static void DisclosureToggle(String title, ref bool value, params Action[] actions)
        {
            UI.TogglePrivate(title, ref value, true, GL.ExpandWidth(false));
            UI.If(value, actions);
        }

        public static void DisclosureBitFieldToggle(String title, ref int bitfield, int offset, params Action[] actions)
        {

            bool bit = ((1 << offset) & bitfield) != 0;
            bool newBit = bit;
            TogglePrivate(title, ref newBit, true, GL.ExpandWidth(false));
            if (bit != newBit) { bitfield ^= (1 << offset); }
            UI.If(newBit, actions);
        }

        public static T TypePicker<T>(String title, ref int selectedIndex, List<NamedFunc<T>> items) where T : class
        {
            var titles = items.Select((item) => item.name).ToArray();
            if  (title?.Length > 0) { Label(title); }
            selectedIndex = GL.SelectionGrid(selectedIndex, titles, 6);
            return items[selectedIndex].func();
        }

        // UI Builders

        public static void If(bool value, params Action[] actions)
        {
            if (value)
            {
                foreach (var action in actions)
                {
                    action();
                }
            }
        }

        public static void Group(params Action[] actions)
        {
            foreach (var action in actions)
            {
                action();
            }
        }

        public static void HStack(String title = null, int stride = 0, params Action[] actions)
        {
            var length = actions.Length;
            if (stride < 1) { stride = length; }
            for (int ii = 0; ii < actions.Length; ii += stride)
            {
                bool hasTitle = title != null;
                GL.BeginHorizontal();
                if (hasTitle)
                {
                    if (ii == 0) { UI.Label(title, GL.Width(150f)); }
                    else { UI.Space(153);  }
                }
                UI.Group(actions.Skip(ii).Take(stride).ToArray());
                GL.EndHorizontal();
            }
        }

        public static void VStack(String title = null, params Action[] actions)
        {
            GL.BeginVertical();
            if (title != null) { UI.Label(title); }
            UI.Group(actions);
            GL.EndVertical();
        }

        public static void Section(String title, params Action[] actions)
        {
            Space(25);
            Label($"====== {title} ======".bold(), GL.ExpandWidth(true));
            Space(25);
            foreach (Action action in actions) { action(); }
            Space(10);
        }
    }
}
