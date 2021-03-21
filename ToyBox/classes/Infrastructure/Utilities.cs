using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using UnityEditor;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;
using Kingmaker.Assets.UI;

namespace ToyBox {
    public class Utilties {
        public static string RemoveHtmlTags(string s) {
            return Regex.Replace(s, "<.*?>", String.Empty);
        }
        public static string UnityRichTextToHtml(string s) {
            s = s.Replace("<color=", "<font color=");
            s = s.Replace("</color>", "</font>");
            s = s.Replace("<size=", "<size size=");
            s = s.Replace("</size>", "</font>");
            s += "<br/>";

            return s;
        }

        public static string[] getObjectInfo(object o) {

            string fields = "";
            foreach (string field in Traverse.Create(o).Fields()) {
                fields = fields + field + ", ";
            }
            string methods = "";
            foreach (string method in Traverse.Create(o).Methods()) {
                methods = methods + method + ", ";
            }
            string properties = "";
            foreach (string property in Traverse.Create(o).Properties()) {
                properties = properties + property + ", ";
            }
            return new string[] { fields, methods, properties };
        }
    }

    public class NamedTypeFilter {
        public String name { get; set; }
        public Type type { get; set; }
        public NamedTypeFilter() { }
        public NamedTypeFilter(String name, Type type) { this.name = name; this.type = type; }
    }

    public class NamedAction {
        public String name { get; set; }
        public Action action { get; set; }
        public NamedAction() { }
        public NamedAction(String name, Action action) { this.name = name; this.action = action; }
    }
    public class NamedAction<T> {
        public String name { get; set; }
        public Action<T> action { get; set; }
        public NamedAction() { }
        public NamedAction(String name, Action<T> action) { this.name = name; this.action = action; }
    }

    public class NamedFunc<T> {
        public String name { get; set; }
        public Func<T> func { get; set; }
        public NamedFunc() { }
        public NamedFunc(String name, Func<T> func) { this.name = name; this.func = func; }
    }

    public static class TB {
        public static bool IsKindOf(this Type type, Type baseType) {
            return type.IsSubclassOf(baseType) || type == baseType;
        }
    }

    public static class BlueprintScriptableObjectUtils {
        public static string GetDescription(this BlueprintScriptableObject bpObejct)
        // borrowed shamelessly and enchanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License
        {
            try {
                UnitReference mainChar = Game.Instance.Player.MainCharacter;
                if (mainChar == null) { return "n/a"; }
                MechanicsContext context = new MechanicsContext((UnitEntityData)null, mainChar.Value.Descriptor, bpObejct, (MechanicsContext)null, (TargetWrapper)null);
                return context?.SelectUIData(UIDataType.Description)?.Description ?? "";
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
