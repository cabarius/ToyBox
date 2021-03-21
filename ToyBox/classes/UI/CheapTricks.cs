using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

namespace ToyBox {
    public class CheapTricks {

        public static void OnGUI(UnityModManager.ModEntry modEntry) {
            UI.Section("Cheap Tricks", () => {
                UI.HStack("Combat", 4,
                    () => { UI.ActionButton("Rest All", () => { CheatsCombat.RestAll(); }); },
                    () => { UI.ActionButton("Empowered", () => { CheatsCombat.Empowered(""); }); },
                    () => { UI.ActionButton("Full Buff Please", () => { CheatsCombat.FullBuffPlease(""); }); },
                    () => { UI.ActionButton("Remove Death's Door", () => { CheatsCombat.DetachDebuff(); }); },
                    () => { UI.ActionButton("Kill All Enemies", () => { CheatsCombat.KillAll(); }); },
                    () => { UI.ActionButton("Summon Zoo", () => { CheatsCombat.SpawnInspectedEnemiesUnderCursor(""); }); }
                 );
                UI.Space(10);
                UI.HStack("Common", 4,
                    () => { UI.ActionButton("Teleport Party To You", () => { Actions.TeleportPartyToPlayer(); }); },
                    () => { UI.ActionButton("Perception Checks", () => { Actions.RunPerceptionTriggers(); }); },
                    () => {
                        UI.ActionButton("Set Perception to 40", () => {
                            CheatsCommon.StatPerception();
                            Actions.RunPerceptionTriggers();
                        });
                    },
                    () => { UI.ActionButton("Change Weather", () => { CheatsCommon.ChangeWeather(""); }); },
                    () => { UI.ActionButton("Give All Items", () => { CheatsUnlock.CreateAllItems(""); }); }
                    );
            });
            UI.HStack("Flags", 1,
                () => { UI.Toggle("Object Highlight Toggle Mode", ref Main.settings.highlightObjectsToggle, UI.AutoWidth()); }
                );
        }
    }
}