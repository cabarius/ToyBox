// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace ToyBox {
    internal class PartyUtils {

        public static List<UnitEntityData> GetCustomCompanions() {
            var unitEntityData = Game.Instance.Player.AllCharacters;
            List<UnitEntityData> unitEntityDataNew = new();

            foreach (var unit in unitEntityData) {
                if (unit.IsCustomCompanion()) {
                    unitEntityDataNew.Add(unit);
                }
            }
            return unitEntityDataNew;
        }

        public static List<UnitEntityData> GetPets() {
            var unitEntityData = Game.Instance.Player.AllCharacters;
            List<UnitEntityData> unitEntityDataNew = new();
            foreach (var unit in unitEntityData) {
                if (unit.IsPet) {
                    unitEntityDataNew.Add(unit);
                }
            }
            return unitEntityDataNew;
        }
        public static List<UnitEntityData> GetRemoteCompanions() => Game.Instance.Player.AllCharacters.Except(Game.Instance.Player.Party).Where(unit => !unit.IsPet).ToList();
    }
}
