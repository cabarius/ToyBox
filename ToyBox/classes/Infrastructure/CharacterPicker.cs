// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using System.Collections.Generic;
using System.Linq;
using static ModKit.UI;

namespace ToyBox {
    public class CharacterPicker {
        public static NamedFunc<List<UnitEntityData>>[] PartyFilterChoices = null;
        private static readonly Player partyFilterPlayer = null;
        public static float nearbyRange = 25;

        public static NamedFunc<List<UnitEntityData>>[] GetPartyFilterChoices() {
            if (partyFilterPlayer != Game.Instance.Player) PartyFilterChoices = null;
            if (Game.Instance.Player != null && PartyFilterChoices == null) {
                PartyFilterChoices = new NamedFunc<List<UnitEntityData>>[] {
                    new NamedFunc<List<UnitEntityData>>("Party".localize(), () => Game.Instance.Player.Party),
                    new NamedFunc<List<UnitEntityData>>("Party & Pets".localize(), () => Game.Instance.Player.m_PartyAndPets),
#if Wrath
                    new NamedFunc<List<UnitEntityData>>("All".localize(), () => Game.Instance.Player.AllCharacters),
#elif RT
                    new NamedFunc<List<UnitEntityData>>("All".localize(), () => Game.Instance.Player.AllCharactersAndStarships.ToList()),
#endif
                    new NamedFunc<List<UnitEntityData>>("Active".localize(), () => Game.Instance.Player.ActiveCompanions),
                    new NamedFunc<List<UnitEntityData>>("Remote".localize(), () => Game.Instance.Player.m_RemoteCompanions),
                    new NamedFunc<List<UnitEntityData>>("Custom".localize(), PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<UnitEntityData>>("Pets".localize(), PartyUtils.GetPets),
#if RT
                    new NamedFunc<List<UnitEntityData>>("Starships".localize(), () => Game.Instance.Player.AllStarships.ToList()),
#endif
                    //new NamedFunc<List<UnitEntityData>>("Familiars", Game.Instance.Player.Party.SelectMany(ch => ch.Familiars),
                    new NamedFunc<List<UnitEntityData>>("Nearby".localize(), () => {
                        var player = GameHelper.GetPlayerCharacter();
                        return player == null
                                   ? new List<UnitEntityData> ()
                                   : GameHelper.GetTargetsAround(GameHelper.GetPlayerCharacter().Position, (int)nearbyRange , false, false).ToList();
                    }),
                    new NamedFunc<List<UnitEntityData>>("Friendly".localize(), () => Shodan.AllUnits.Where((u) => u != null && !u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("Enemies".localize(), () => Shodan.AllUnits.Where((u) => u != null && u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("All Units".localize(), () => Shodan.AllUnits.ToList()),
               };
            }
            return PartyFilterChoices;
        }
        public static List<UnitEntityData> GetCharacterList() {
            var partyFilterChoices = GetPartyFilterChoices();
            return partyFilterChoices?[Main.Settings.selectedPartyFilter].func();
        }

        private static int _selectedIndex = 0;
        public static UnitEntityData GetSelectedCharacter() {
            var characters = GetCharacterList();
            if (characters == null || characters.Count == 0) {
                return Game.Instance.Player.MainCharacter;
            }
            if (_selectedIndex >= characters.Count) _selectedIndex = 0;
            return characters[_selectedIndex];
        }
        public static void ResetGUI() => _selectedIndex = 0;

        public static NamedFunc<List<UnitEntityData>> OnFilterPickerGUI() {
            var filterChoices = GetPartyFilterChoices();
            if (filterChoices == null) { return null; }

            var characterListFunc = TypePicker(
                null,
                ref Main.Settings.selectedPartyFilter,
                filterChoices,
                true
                );
            return characterListFunc;
        }
        public static void OnCharacterPickerGUI(float indent = 0) {

            var characters = GetCharacterList();
            if (characters == null) { return; }
            using (HorizontalScope(AutoWidth())) {
                Space(indent);
                ActionSelectionGrid(ref _selectedIndex,
                    characters.Select((ch) => ch.CharacterName).ToArray(),
                    8,
                    null,
                    AutoWidth());
            }
            var selectedCharacter = GetSelectedCharacter();
            if (selectedCharacter != null) {
                using (HorizontalScope(AutoWidth())) {
                    Space(indent);
                    Label($"{selectedCharacter.CharacterName}".orange().bold(), AutoWidth());
                    Space(5);
                    Label("will be used for editing ".localize().green());
                }
            }
        }
    }
}