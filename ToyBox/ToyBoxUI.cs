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
        public static void Section(String title, params Action[] actions)
        {
            GL.Space(25);
            GL.Label("====== title ======".bold());
            GL.Space(25);
            foreach (Action action in actions) { action(); }
        }

        // button with a title that calls an action

        public static void ActionButton(String name, Action action) { if (GL.Button(name, GL.Width(300f))) { action(); } }
        public static void Actic(params Actions[] actions)
        {
            foreach (NamedAction action in actions)
            {
                GL.Space(10);
                if (GL.Button(action.name, GL.Width(300f))) { action.action(); }
            }
        }

        public static void HStack(String title = null, int stride = 0, params NamedAction[] actions)
        {
            for (int ii = 0; ii < actions.Length; ii += stride)
            {
                bool hasTitle = title != null;
                if (ii == 0 && hasTitle)
                {
                    GL.BeginHorizontal();
                    GL.Label(title, GL.Width(150f));
                }
                else if (ii % stride == 0 && ii > 0) { GL.EndHorizontal(); GL.BeginHorizontal(); }
                if (hasTitle) { GL.Space(153);  }
                Quickies(actions.Skip(ii).Take(stride).ToArray());
                GL.EndHorizontal();
            }
        }




        public static void temp()
        {

            GL.BeginHorizontal();
            GL.Label("Combat", GL.Width(150f));
            if (GL.Button("Empowered", GL.Width(300f)))
            {
                CheatsCombat.Empowered("");
            }
            if (GL.Button("Full Buff Please", GL.Width(300f)))
            {
                CheatsCombat.FullBuffPlease("");
            }
            if (GL.Button("Remove Death's Door", GL.Width(300f)))
            {
                CheatsCombat.DetachDebuff();
            }
            GL.EndHorizontal();
            GL.BeginHorizontal();
            GL.Space(153);
            if (GL.Button("Kill All Enemies", GL.Width(300f)))
            {
                CheatsCombat.KillAll();
            }
            if (GL.Button("Summon Zoo", GL.Width(300f)))
            {
                CheatsCombat.SpawnInspectedEnemiesUnderCursor("");
            }
            GL.EndHorizontal();

            GL.Space(10);

        }

    }
}
