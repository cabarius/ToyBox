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
using Kingmaker.UI.FullScreenUITypes;

namespace ModKit {
    public partial class UI {
        public static string Name(this Alignment a) => UIUtility.GetAlignmentName(a);
        public static string Acronym(this Alignment a) => UIUtility.GetAlignmentAcronym(a);


        public static Alignment[] Alignments = new Alignment[] {
                    Alignment.LawfulGood,       Alignment.NeutralGood,      Alignment.ChaoticGood,
                    Alignment.LawfulNeutral,    Alignment.TrueNeutral,      Alignment.ChaoticNeutral,
                    Alignment.LawfulEvil,       Alignment.NeutralEvil,      Alignment.ChaoticEvil
        };
        public static RGBA Color(this Alignment a) => a switch {
            Alignment.LawfulGood => RGBA.aqua,
            Alignment.NeutralGood => RGBA.lime,
            Alignment.ChaoticGood => RGBA.yellow,
            Alignment.LawfulNeutral => RGBA.blue,
            Alignment.TrueNeutral => RGBA.white,
            Alignment.ChaoticNeutral => RGBA.orange,
            Alignment.LawfulEvil => RGBA.purple,
            Alignment.NeutralEvil => RGBA.fuchsia,
            Alignment.ChaoticEvil => RGBA.red,
            _ => RGBA.grey,
        };
        public static AlignmentMaskType[] AlignmentMasks = new AlignmentMaskType[] {
                    AlignmentMaskType.None,             AlignmentMaskType.Good,             AlignmentMaskType.Evil,
                    AlignmentMaskType.Any,              AlignmentMaskType.Lawful,           AlignmentMaskType.Chaotic,
                    AlignmentMaskType.LawfulGood,       AlignmentMaskType.NeutralGood,      AlignmentMaskType.ChaoticGood,
                    AlignmentMaskType.LawfulNeutral,    AlignmentMaskType.TrueNeutral,      AlignmentMaskType.ChaoticNeutral,
                    AlignmentMaskType.LawfulEvil,       AlignmentMaskType.NeutralEvil,      AlignmentMaskType.ChaoticEvil,
        };

        public static RGBA Color(this AlignmentMaskType a) {
            switch (a) {
                case AlignmentMaskType.None: return RGBA.grey;
                case AlignmentMaskType.Good: return RGBA.lime;
                case AlignmentMaskType.Evil: return RGBA.fuchsia;
                case AlignmentMaskType.Any: return RGBA.grey;
                case AlignmentMaskType.Lawful: return RGBA.blue; ;
                case AlignmentMaskType.Chaotic: return RGBA.orange;
                case AlignmentMaskType.LawfulGood: return RGBA.aqua;
                case AlignmentMaskType.NeutralGood: return RGBA.lime;
                case AlignmentMaskType.ChaoticGood: return RGBA.yellow;
                case AlignmentMaskType.LawfulNeutral: return RGBA.blue;
                case AlignmentMaskType.TrueNeutral: return RGBA.white;
                case AlignmentMaskType.ChaoticNeutral: return RGBA.orange;
                case AlignmentMaskType.LawfulEvil: return RGBA.purple;
                case AlignmentMaskType.NeutralEvil: return RGBA.fuchsia;
                case AlignmentMaskType.ChaoticEvil: return RGBA.red;
            }
            return RGBA.grey;
        }
        public static void AlignmentGrid(string title, Alignment alignment, Action<Alignment> action, params GUILayoutOption[] options) {
            using (HorizontalScope()) {
                if (title?.Length > 0) {
                    Label(title.cyan(), options);
                }
                var alignmentIndex = Array.IndexOf(Alignments, alignment);
                var titles = Alignments.Select(
                    a => a.Acronym().color(a.Color()).bold()).ToArray();
                if (SelectionGrid(ref alignmentIndex, titles, 3, Width(250f))) {
                    action(Alignments[alignmentIndex]);
                }
            }
        }
        public static void AlignmentGrid(Alignment alignment, Action<Alignment> action, params GUILayoutOption[] options)
            => AlignmentGrid(null, alignment, action, options);
    }
}

namespace ToyBox {
    public static class WrathExtensions {
        public static string HashKey(this UnitEntityData ch) => ch.CharacterName;  // + ch.UniqueId; }
        public static string HashKey(this UnitDescriptor ch) => ch.CharacterName;
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
                return associatedBlueprint?.Description.StripHTML();
                // Why did BoT do this instead of the above which is what MechanicsContext.SelectUIData() does for description
#if false
                var description = bp.Des
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
