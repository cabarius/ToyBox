// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using ModKit;

namespace ToyBox {
    public class CharacterPicker {
        static int selectedIndex = 0;
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