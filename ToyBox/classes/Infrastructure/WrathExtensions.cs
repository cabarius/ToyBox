// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using UnityEditor;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Armies.TacticalCombat.Blueprints;
using Kingmaker.Armies.TacticalCombat.Brain;
using Kingmaker.Armies.TacticalCombat.Brain.Considerations;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Credits;
using Kingmaker.Blueprints.Encyclopedia;
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
using Kingmaker.Blueprints.Console;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Interaction;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Tutorial;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.LevelUp;
//using Kingmaker.UI.LevelUp.Phase;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;
using Kingmaker.Assets.UI;
using ModKit.Utility;
using static ModKit.Utility.ReflectionCache;
using Alignment = Kingmaker.Enums.Alignment;
using ModKit;

namespace ToyBox {
    public static class WrathExtensions {
        public static string HashKey(this UnitEntityData ch) { return ch.CharacterName; } // + ch.UniqueId; }
        public static string HashKey(this UnitDescriptor ch) { return ch.CharacterName; }
        public static string Name(this Alignment a) { return UIUtility.GetAlignmentName(a); }
        public static string Acronym(this Alignment a) { return UIUtility.GetAlignmentAcronym(a); }
        public static RGBA Color(this Alignment a) {
            switch (a) {
                case Alignment.LawfulGood: return RGBA.aqua;
                case Alignment.NeutralGood: return RGBA.lime;
                case Alignment.ChaoticGood: return RGBA.yellow;
                case Alignment.LawfulNeutral: return RGBA.blue;
                case Alignment.TrueNeutral: return RGBA.white;
                case Alignment.ChaoticNeutral: return RGBA.orange;
                case Alignment.LawfulEvil: return RGBA.purple;
                case Alignment.NeutralEvil: return RGBA.fuchsia;
                case Alignment.ChaoticEvil: return RGBA.red;
            }
            return RGBA.grey;
        }
        public static string GetDescription(this SimpleBlueprint   bp)
        // borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
        {
            try {
                // avoid exceptions on known broken items
                var guid = bp.AssetGuid;
                if (guid == "b60252a8ae028ba498340199f48ead67" || guid == "fb379e61500421143b52c739823b4082") return null;
                IUIDataProvider associatedBlueprint = bp as IUIDataProvider;
                return associatedBlueprint?.Description.RemoveHtmlTags();
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
    }
}
