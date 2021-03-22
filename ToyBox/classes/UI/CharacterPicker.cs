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
    public class CharacterPicker {
        public static int selectedIndex = 0;
        public static UnitEntityData selectedCharacter = null;

        public static void OnGUI(UnityModManager.ModEntry modEntry) {
            UI.Space(25);
            UI.ActionSelectionGrid(ref selectedIndex,
                PartyEditor.characterList.Select((ch) => ch.CharacterName).ToArray(),
                8,
                (index) => {  BlueprintBrowser.UpdateSearchResults(); },
                UI.MinWidth(200));
            selectedCharacter = PartyEditor.characterList[selectedIndex];
            UI.Space(10);
            UI.HStack(null, 0, () => {
                UI.Label($"{PartyEditor.characterList[CharacterPicker.selectedIndex].CharacterName}".orange().bold(), UI.AutoWidth());
                UI.Space(25);
                UI.Label("will be used for adding/remove features, buffs, etc in the search results below.".green());
            });
        }
    }
}