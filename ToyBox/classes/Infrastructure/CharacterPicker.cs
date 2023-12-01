// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModKit.UI;

namespace ToyBox {
    public class CharacterPicker {
        public static NamedFunc<List<BaseUnitEntity>>[] PartyFilterChoices = null;
        private static readonly Player partyFilterPlayer = null;
        public static float nearbyRange = 25;
        public static IEnumerable<BaseUnitEntity> GetTargetsAround(Vector3 point, int radius, bool checkLOS = true, bool includeDead = false) {
            foreach (AbstractUnitEntity abstractUnitEntity in Game.Instance.State.AllUnits.OfType<BaseUnitEntity>()) {
                BaseUnitEntity baseUnitEntity = (BaseUnitEntity)abstractUnitEntity;
                if ((!baseUnitEntity.LifeState.IsDead || includeDead) && !baseUnitEntity.Features.IsUntargetable && baseUnitEntity.IsUnitInRangeCells(point, radius, checkLOS)) {
                    yield return baseUnitEntity;
                }
            }
            yield break;
        }
        public static NamedFunc<List<BaseUnitEntity>>[] GetPartyFilterChoices() {
            if (partyFilterPlayer != Game.Instance.Player) PartyFilterChoices = null;
            if (Game.Instance.Player != null && PartyFilterChoices == null) {
                PartyFilterChoices = new NamedFunc<List<BaseUnitEntity>>[] {
                    new NamedFunc<List<BaseUnitEntity>>("Party".localize(), () => Game.Instance.Player.Party),
                    new NamedFunc<List<BaseUnitEntity>>("Party & Pets".localize(), () => Game.Instance.Player.m_PartyAndPets),
                    new NamedFunc<List<BaseUnitEntity>>("All".localize(), () => Game.Instance.Player.AllCharactersAndStarships.ToList()),
                    new NamedFunc<List<BaseUnitEntity>>("Active".localize(), () => Game.Instance.Player.ActiveCompanions),
                    new NamedFunc<List<BaseUnitEntity>>("Remote".localize(), () => Game.Instance.Player.m_RemoteCompanions),
                    new NamedFunc<List<BaseUnitEntity>>("Custom".localize(), PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<BaseUnitEntity>>("Pets".localize(), PartyUtils.GetPets),
                    new NamedFunc<List<BaseUnitEntity>>("Starships".localize(), () => Game.Instance.Player.AllStarships.ToList()),
                    //new NamedFunc<List<UnitEntityData>>("Familiars", Game.Instance.Player.Party.SelectMany(ch => ch.Familiars),
                    new NamedFunc<List<BaseUnitEntity>>("Nearby".localize(), () => {
                        var player = GameHelper.GetPlayerCharacter();
                        return (player == null) ? new() : GetTargetsAround(player.Position, (int)nearbyRange , false, false).ToList();
                    }),
                    new NamedFunc<List<BaseUnitEntity>>("Friendly".localize(), () => Shodan.AllBaseUnits.Where((u) => u != null && !u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<BaseUnitEntity>>("Enemies".localize(), () => Shodan.AllBaseUnits.Where((u) => u != null && u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<BaseUnitEntity>>("All Units".localize(), () => Shodan.AllBaseUnits.ToList()),
               };
            }
            return PartyFilterChoices;
        }
        public static List<BaseUnitEntity> GetCharacterList() {
            var partyFilterChoices = GetPartyFilterChoices();
            return partyFilterChoices?[Main.Settings.selectedPartyFilter].func();
        }

        private static int _selectedIndex = 0;
        public static BaseUnitEntity GetSelectedCharacter() {
            var characters = GetCharacterList();
            if (characters == null || characters.Count == 0) {
                return Game.Instance.Player.MainCharacterEntity;
            }
            if (_selectedIndex >= characters.Count) _selectedIndex = 0;
            return characters[_selectedIndex];
        }
        public static void ResetGUI() => _selectedIndex = 0;

        public static NamedFunc<List<BaseUnitEntity>> OnFilterPickerGUI() {
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