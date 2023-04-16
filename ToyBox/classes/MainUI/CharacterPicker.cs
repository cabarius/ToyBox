﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using static ModKit.UI;

namespace ToyBox {
    public class CharacterPicker {
        public static NamedFunc<List<UnitEntityData>>[] partyFilterChoices = null;
        private static readonly Player partyFilterPlayer = null;
        public static float nearbyRange = 25;

        public static NamedFunc<List<UnitEntityData>>[] GetPartyFilterChoices() {
            if (partyFilterPlayer != Game.Instance.Player) partyFilterChoices = null;
            if (Game.Instance.Player != null && partyFilterChoices == null) {
                partyFilterChoices = new NamedFunc<List<UnitEntityData>>[] {
                    new NamedFunc<List<UnitEntityData>>("Party", () => Game.Instance.Player.Party),
                    new NamedFunc<List<UnitEntityData>>("Party & Pets", () => Game.Instance.Player.m_PartyAndPets),
                    new NamedFunc<List<UnitEntityData>>("All", () => Game.Instance.Player.AllCharacters),
                    new NamedFunc<List<UnitEntityData>>("Active", () => Game.Instance.Player.ActiveCompanions),
                    new NamedFunc<List<UnitEntityData>>("Remote", () => Game.Instance.Player.m_RemoteCompanions),
                    new NamedFunc<List<UnitEntityData>>("Custom", PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<UnitEntityData>>("Pets", PartyUtils.GetPets),
                    //new NamedFunc<List<UnitEntityData>>("Familiers", Game.Instance.Player.Party.SelectMany(ch => ch.Familiars),
                    new NamedFunc<List<UnitEntityData>>("Nearby", () => {
                        var player = GameHelper.GetPlayerCharacter();
                        if (player == null) return new List<UnitEntityData> ();
                        return GameHelper.GetTargetsAround(GameHelper.GetPlayerCharacter().Position, nearbyRange , false, false).ToList();
                    }),
                    new NamedFunc<List<UnitEntityData>>("Friendly", () => Game.Instance.State.Units.Where((u) => u != null && !u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("Enemies", () => Game.Instance.State.Units.Where((u) => u != null && u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("All Units", () => Game.Instance.State.Units.ToList()),
               };
            }
            return partyFilterChoices;
        }
        public static List<UnitEntityData> GetCharacterList() {
            var partyFilterChoices = GetPartyFilterChoices();
            if (partyFilterChoices == null) { return null; }
            return partyFilterChoices[Main.settings.selectedPartyFilter].func();
        }

        private static int selectedIndex = 0;
        public static UnitEntityData GetSelectedCharacter() {
            var characters = GetCharacterList();
            if (characters == null || characters.Count == 0) {
                return Game.Instance.Player.MainCharacter;
            }
            if (selectedIndex >= characters.Count) selectedIndex = 0;
            return characters[selectedIndex];
        }
        public static void ResetGUI() => selectedIndex = 0;

        public static NamedFunc<List<UnitEntityData>> OnFilterPickerGUI() {
            var filterChoices = GetPartyFilterChoices();
            if (filterChoices == null) { return null; }

            var characterListFunc = TypePicker(
                null,
                ref Main.settings.selectedPartyFilter,
                filterChoices
                );
            return characterListFunc;
        }
        public static void OnCharacterPickerGUI(float indent = 0) {

            var characters = GetCharacterList();
            if (characters == null) { return; }
            using (HorizontalScope(AutoWidth())) {
                Space(indent);
                ActionSelectionGrid(ref selectedIndex,
                    characters.Select((ch) => ch.CharacterName).ToArray(),
                    8,
                    (index) => { BlueprintBrowser.UpdateSearchResults(); },
                    AutoWidth());
            }
            var selectedCharacter = GetSelectedCharacter();
            if (selectedCharacter != null) {
                using (HorizontalScope(AutoWidth())) {
                    Space(indent);
                    Label($"{GetSelectedCharacter().CharacterName}".orange().bold(), AutoWidth());
                    Space(5);
                    Label("will be used for editing ".green());
                }
            }
        }
    }
}