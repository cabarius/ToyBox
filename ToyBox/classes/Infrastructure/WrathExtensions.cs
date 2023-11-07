// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using System;
using UnityEngine;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UI.Common;
//using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Alignment = Kingmaker.Enums.Alignment;
using ModKit;
using Kingmaker.UnitLogic.Alignments;
using System.Linq;
using Kingmaker;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.Mechanics;

namespace ToyBox {
    public static class WrathExtensions {
        public static string HashKey(this UnitEntityData ch) => ch.CharacterName;  // + ch.UniqueId; }
        public static string HashKey(this MechanicEntity entity) => 
            entity is UnitEntityData ch ? ch.CharacterName : entity.Name;
        public static string HashKey(this BlueprintCharacterClass cl) => cl.NameSafe();
        public static string HashKey(this BlueprintArchetype arch) => arch.NameSafe();

        public static string GetDescription(this SimpleBlueprint bp)
        // borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
        {
            try {
                // avoid exceptions on known broken items
                var guid = bp.AssetGuid;
                if (guid == "b60252a8ae028ba498340199f48ead67" || guid == "fb379e61500421143b52c739823b4082") return null;
                var associatedBlueprint = bp as IUIDataProvider;
                return associatedBlueprint?.Description?.StripHTML();
                // Why did BoT do this instead of the above which is what MechanicsContext.SelectUIData() does for description
#if false
                var description = associatedBlueprint.Description;
                UnitReference mainChar = Game.Instance.Player.MainCharacter;
                if (mainChar == null) { return ""; }
                MechanicsContext context = new MechanicsContext((UnitEntityData)null, mainChar.Value.Descriptor, bp, (MechanicsContext)null, (TargetWrapper)null);
                return context?.SelectUIData(UIDataType.Description)?.Description ?? "";
#endif
            }
            catch (Exception e) {
                Console.Write($"{e}");
#if DEBUG
                return "ERROR".red().bold() + $": caught exception {e}";
#else
                return "";
#endif
            }
        }
        public static UnitEntityData GetCurrentCharacter() {
            var firstSelectedUnit = Game.Instance.SelectionCharacter.FirstSelectedUnit;
            return (object)firstSelectedUnit != null ? firstSelectedUnit : (UnitEntityData)Game.Instance.Player.MainCharacter;
        }
    }
}
