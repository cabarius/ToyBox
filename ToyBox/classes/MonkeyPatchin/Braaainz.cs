using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Items;
using Kingmaker.Items.Parts;
using Kingmaker.UI.MVVM._PCView.Loot;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.LocalMap.Markers;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._PCView.Tooltip.Bricks;
using Kingmaker.UI.MVVM._PCView.Vendor;
using Kingmaker.UI.MVVM._VM.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._VM.ServiceWindows.LocalMap.Utils;
using Kingmaker.UI.MVVM._VM.Slots;
using UniRx;
using Owlcat.Runtime.UI.MVVM;
using UnityEngine;
using UnityEngine.UI;
using System;
using Kingmaker.Items.Slots;
using Kingmaker.AI;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Alignments;
using ModKit;
using JetBrains.Annotations;
using Kingmaker.Utility;
using System.Linq;

namespace ToyBox.BagOfPatches {
#if DEBUG
    internal static class Braaainz {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(AiBrainController), nameof(AiBrainController.TickBrain))]
        private static class AiBrainController_TickBrain_patch {
            public static bool Prefix(AiBrainController __instance, UnitEntityData unit) {
                Mod.Log($"{unit?.CharacterName.orange()} - Maybe BrainTick");
                return true;
            }
        }
        [HarmonyPatch(typeof(AiBrainController), nameof(AiBrainController.SelectAction))]
        private static class AiBrainController_SelectAction_patch {
            public static bool Prefix(AiBrainController __instance, UnitEntityData unit, DecisionContext context,
      [CanBeNull] AIDebugScope debugScope) {
                Mod.Log($"{unit?.CharacterName.orange()} - SelectAction".cyan());
                return true;
            }
        }
#if true
        [HarmonyPatch(typeof(AiBrainController), nameof(AiBrainController.FindBestAction))]
        private static class AiBrainController_FindAction_patch {
            public static bool Prefix(
                    AiBrainController __instance,
                    UnitEntityData unit,
                    DecisionContext context,
                    out AiAction bestActionResult,
                    out UnitEntityData bestTargetResult,
                    out bool isAutoUseAbility
                ) {
                Mod.Log($"    FindBestAction - {unit.CharacterName}".yellow());
                bestActionResult = null;
                bestTargetResult = null;
                isAutoUseAbility = false;
                AiAction bestAction = bestActionResult = (AiAction)null;
                UnitEntityData bestTarget = bestTargetResult = (UnitEntityData)null;
                AiAction action = unit.IsPlayerFaction
                    ? unit.Brain.GetAvailableAutoUseAbility()?.DefaultAiAction 
                    : null;
                if (action != null) {
                    Mod.Log($"    found AutoUseAction {action.DebugName}");
                    using (AIDebugScope debugScope = AIDebugScope.Open((object)action))
                        AiBrainController.CalculateActionScore(context, action, ref bestAction, ref bestTarget, debugScope);
                }
                isAutoUseAbility = bestAction != null && context.CurrentScore >= 0.1M;
                if (!isAutoUseAbility) {
                    for (int index = 0; index < unit.Brain.CustomActions.Count; ++index) {
                        AiActionCustom customAction = unit.Brain.CustomActions[index];
                        customAction.ValidateAndFix();
                        using (AIDebugScope debugScope = AIDebugScope.Open((object)customAction)) {
                            if (customAction.IsValid) {
                                using (ProfileScope.New("One Action", (SimpleBlueprint)customAction.Blueprint)) {
                                    if (!(context.BestScore > (Decimal)customAction.BaseScore))
                                        AiBrainController.CalculateActionScore(context, (AiAction)customAction, ref bestAction, ref bestTarget, debugScope);
                                }
                            }
                        }
                    }
                    if (bestAction != null)
                        Mod.Log($"    found Custom Action {bestAction.DebugName} bestTarget: {bestTarget?.CharacterName}");
                }
                if (!isAutoUseAbility && (bestAction == null || context.CurrentScore < 0.1M)) {
                    for (int index = 0; index < unit.Brain.AvailableActions.Count; ++index) {
                        AiAction availableAction = unit.Brain.AvailableActions[index];
                        Mod.Log($"    checking {availableAction.DebugName.yellow()} - baseScore: {availableAction.BaseScore} best:{context.BestScore} context:{String.Join(",\n", context.ToDictionary().Select(p => $"{p.Key} : {p.Value}"))}");
                        using (AIDebugScope debugScope = AIDebugScope.Open((object)availableAction)) {
                            using (ProfileScope.New("One Action", (SimpleBlueprint)availableAction.Blueprint)) {
                                if (!(context.BestScore > (Decimal)availableAction.BaseScore))
                                    AiBrainController.CalculateActionScore(context, availableAction, ref bestAction, ref bestTarget, debugScope);
                            }
                        }
                    }
                    if (bestAction != null)
                        Mod.Log($"    found Brain.AvailableAction {bestAction?.DebugName} bestTarget: {bestTarget?.CharacterName}");
                }
                bestActionResult = bestAction;
                bestTargetResult = bestTarget;
                return false;
            }
        }
#endif
    }
#endif
}