// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox {
    internal class PartyUtils {

        public static List<BaseUnitEntity> GetCustomCompanions() {
            var unitEntityData = Game.Instance.Player.AllCharacters;
            List<BaseUnitEntity> unitEntityDataNew = new();

            foreach (var unit in unitEntityData) {
                if (unit.IsCustomCompanion()) {
                    unitEntityDataNew.Add(unit);
                }
            }
            return unitEntityDataNew;
        }

        public static List<BaseUnitEntity> GetPets() {
            var unitEntityData = Game.Instance.Player.AllCharacters;
            List<BaseUnitEntity> unitEntityDataNew = new();
            foreach (var unit in unitEntityData) {
                if (unit.IsPet) {
                    unitEntityDataNew.Add(unit);
                }
            }
            return unitEntityDataNew;
        }
        public static List<BaseUnitEntity> GetRemoteCompanions() => Game.Instance.Player.AllCharacters.Except(Game.Instance.Player.Party).Where(unit => !unit.IsPet).ToList();
    }
}
