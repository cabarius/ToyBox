// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic.Commands.Base;
using System;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.BagOfPatches {
    internal static class Actions {
        public static Settings Settings = Main.Settings;
        public static Player player = Game.Instance.Player;


        [HarmonyPatch(typeof(PartUnitCombatState))]
        public static class PartUnitCombatStatePatch {
            [HarmonyPatch(nameof(PartUnitCombatState.SpendActionPoints))]
            [HarmonyPrefix]
            public static bool SpendActionPoints(PartUnitCombatState __instance, int? yellow = null, float? blue = null) {
                if (!Settings.toggleUnlimitedActionsPerTurn) return true;
                return false;
            }
        }
    }
}