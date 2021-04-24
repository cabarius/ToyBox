// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
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
using ModMaker;

namespace ToyBox {
    public class CharacterPicker {
        static int selectedIndex = 0;
        static UnitEntityData selectedCharacter = null;
        static public UnitEntityData GetSelectedCharacter() {
            var characters = PartyEditor.GetCharacterList();
            if (characters == null || characters.Count == 0) {
                return Game.Instance.Player.MainCharacter;
            }
            if (selectedIndex > characters.Count) {
                selectedIndex = 0;
            }
            return characters[selectedIndex];
        }
        public static void ResetGUI() {
            selectedIndex = 0;
            selectedCharacter = null;
        }

        public static void OnGUI() {

            var characters = PartyEditor.GetCharacterList();
            if (characters == null) { return; }
            UI.ActionSelectionGrid(ref selectedIndex,
                characters.Select((ch) => ch.CharacterName).ToArray(),
                8,
                (index) => {  BlueprintBrowser.UpdateSearchResults(); },
                UI.MinWidth(200));
            var selectedCharacter = GetSelectedCharacter();
            if (selectedCharacter != null) {
                UI.Space(10);
                UI.HStack(null, 0, () => {
                    UI.Label($"{GetSelectedCharacter().CharacterName}".orange().bold(), UI.AutoWidth());
                    UI.Space(5);
                    UI.Label("will be used for adding/remove features, buffs, etc ".green());
                });
            }
        }
    }
}