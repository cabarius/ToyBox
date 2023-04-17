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
using System.Collections.Generic;
using System.Reflection;
using ModKit.Utility;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.UnitLogic;
using UnityEngine.PlayerLoop;
using Kingmaker.Visual.Animation.Events;
using Kingmaker.Visual.Animation;
using System.Security.Policy;
using Kingmaker.UnitLogic.Groups;
using Kingmaker.View;
using Kingmaker.Visual.Animation.Kingmaker;
using Kingmaker.Controllers.Optimization;
using Kingmaker.UnitLogic.Commands;
using static Owlcat.QA.Validation.BlueprintValidationHelper;
using static RootMotion.FinalIK.InteractionTrigger;
using TurnBased.Controllers;

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
            private static string MaybeListToString(object o) {
                if (o is List<TargetInfo> list) return $"<{string.Join(", ", list.Select(ti => ti.Unit.CharacterName))}>";
                return o.ToString();
            }
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
                        using (AIDebugScope debugScope = AIDebugScope.Open((object)availableAction)) {
                            using (ProfileScope.New("One Action", (SimpleBlueprint)availableAction.Blueprint)) {
                                Mod.Log($"    checking {availableAction.DebugName.yellow()} - baseScore: {availableAction.BaseScore} best:{context.BestScore} context:{String.Join(",\n", context.ToDictionary().Select(p => $"{p.Key} : {MaybeListToString(p.Value)}"))}");
                                if (!(context.BestScore > (Decimal)availableAction.BaseScore))
                                    AiBrainController.CalculateActionScore(context, availableAction, ref bestAction, ref bestTarget, debugScope);
                            }
                        }
                    }
                    if (bestAction != null)
                        Mod.Log($"    {unit.CharacterName.orange()} found Brain.AvailableAction {bestAction?.DebugName.cyan()} bestTarget: {bestTarget?.CharacterName.yellow()}".green());
                }
                bestActionResult = bestAction;
                bestTargetResult = bestTarget;
                return false;
            }
        }
#if false
        [HarmonyPatch(typeof(ProfileScope), nameof(ProfileScope.New), new Type[] { typeof(string), typeof(UnityEngine.Object) })]
        private static class ProfileScope_New_Patch {
            public static void Postfix(ref IDisposable __result, string text, UnityEngine.Object ctx = null) {
                if (text.Matches("Intersecting")
                    || text.Equals("some type name")
                    || text.Equals("BlueprintSpellsTable")
                    || text.Equals("Spellbook")
                    || text.Equals("UpdateEvents")
                    || text.Equals("Update dead bodies")
                    || text.Equals("Units")
                    || text.Equals("EntityBoundsHelper.FindEntitiesInRange")
                    || text.Equals("GetDataList")
                    || text.Equals("Update Revealer")
                    || text.Equals("Playing")
                    || text.Equals("Animation")
                    || text.Equals("Mixer")
                    || text.Equals("Scale")
                    || text.Equals("GetWeight")
                    || text.Equals("UpdateSkeleton")
                    || text.Equals("One Controller")
                    || text.Equals("EarlyOuts")
                    || text.Equals("GetParticles")
                    || text.Equals("UpdateMap")
                    || text.Equals("UpdateUnitEncumbrance")
                    || text.Equals("Revealers")
                    || text.Equals("GetSpeed")
                    || text.Equals("ActionBarVM")
                    || text.Equals("UO")
                    || text.Equals("VM visibility")
                    || text.Equals("View update")
                    || text.Equals("Update AkSoundEngineController")
                    || text.Equals("Update Objects")
                    || text.Equals("Dummy Listener")
                    || text.Equals("UpdateListenerPosition")
                    || text.Equals("FxAoeSpawner")
                    || text.Equals("ApplyResults")
                    || text.Equals("Groups")
                    || text.Equals("add")
                    || text.Equals("remove")
                    || text.Equals("FxAoeSpawner")
                    || text.Equals("SetParticles")
                    || text.Equals("UpdatePartyEncumbrance")
                    || text.Equals("Encumbrance.GetPartyCarryingCapacity")
                    || text.Equals("PartyCapacity")
                    || text.Equals("DetachedCapacity")
                    || text.Equals("EquipmentWeight")
                    || text.Equals("Additional")
                    || text.Equals("PrepareEntities")
                    || text.Equals("UpdateBorders")
                    || text.Equals("PathVisualizer Update")
                    || text.Equals("Mixer.NormalizeWeights")
                    || text.Equals("AnimationManager.UpdateAnimations")
                    || text.Equals("Animation.UpdateInternal")
                    || text.Equals("Mixer.NormalizeWeights")
                    || text.Equals("AnimationManager.UpdateActions")
                    || text.Equals("Cleanup Revealers")
                    || text.Equals("Collect Revealers")
                    || text.Equals("UpdateRendererRevealers")
                    || text.Equals("AnimationClipWrapperStateMachineBehaviour.OnStateUpdate")
                    || text.Equals("GetSizeScale")
                    || text.Equals("ActionBarVM OnUpdateHandler")
                    || text.Equals("Localized string")
                    || text.Equals("TransitioningIn")
                    || text.Equals("UpdateEntity")
                    || text.Equals("Update Object Position")
                    || text.Equals("Update Object Zone")
                    || text.Equals("ActionBarVM UpdateSelection")
                    || text.Equals("TransitioningIn")
                    || text.Equals("Tick Movement")
                    || text.Equals("ObstacleAnalyzer.GetNearestNode")
                    || text.Equals("Before Avoidance")
                    || text.Equals("Avoidance")
                    || text.Equals("Init")
                    || text.Equals("Navmesh Avoidance")
                    || text.Equals("Units Avoidance")
                    || text.Equals("UnitGroup.IsEnemy")
                    || text.Equals("UnitGroup.IsEnemy")
                    || text.Equals("UnitGroup.IsEnemy")
                    || text.Equals("UnitGroup.IsEnemy")
                    || text.Equals("Calc Direction")
                    || text.Equals("After avoidance")
                    || text.Equals("Physics move")
                    || text.Equals("Navmesh clamp")
                    || text.Equals("ObstacleAnalyzer.GetNearestNode")
                    || text.Equals("Physics")
                    || text.Equals("Combat")
                    || text.Equals("State")
                    || text.Equals("MicroIdle")
                    || text.Equals("UnitAnimationManager.Tick()")
                    || text.Equals("Tick Animator")
                    || text.Equals("Check Sleeping")
                    || text.Equals("Set Variables")
                    || text.Equals("Movement")
                    || text.Equals("Speed")
                    || text.Equals("TBM")
                    || text.Equals("UnitCommands.RemoveFinishedAndUpdateQueue")
                    || text.Equals("Tick one shape")
                    || text.Equals("CopyUnitsInside")
                    || text.Equals("EndIfNecessary")
                    || text.Equals("UpdatePosition")
                    || text.Equals("UpdateUnits")
                    || text.Equals("EntityBoundsHelper.FindUnitsInShape")
                    || text.Equals("UpdateUnit")
                    || text.Equals("IsUnitInside")
                    || text.Equals("SearchUnitInside")
                    || text.Equals("HandleTick")
                    || text.Equals("SetLifeState")
                    || text.Equals("Check Should Pause Cutscene")
                    || text.Equals("CombatController Change turn")
                    || text.Equals("Mounted crap")
                    || text.Equals("Vision Range")
                    || text.Equals("Collect SpottedBy")
                    || text.Equals("Should Be In Stealth")
                    || text.Matches("ShouldBeInStealth")
                    || text.Equals("Memory Cleanup")
                    || text.Equals("TickCommand")
                    || text.Equals("TickApproaching")
                    || text.Equals("TransitionOut")
                    || text.Matches("Animator")
                    || text.Equals("Finishing")
                    || text.Equals("HandleRound")
                    || text.Equals("Check Condition")
                    || text.Equals("Action")
                    || text.Equals("Cooldown")
                    || text.Equals("HP")
                    || text.Equals("HP Short")
                    || text.Equals("SpellHandle")
                    || text.Equals("Find animation")
                    )
                    return;
                Mod.Log($"ProfileScope: {text ?? "null scope"}".pink());
            }
         }
#endif
#if false
        [HarmonyPatch(typeof(ProfileScope), nameof(ProfileScope.New), new Type[] { typeof(string), typeof(SimpleBlueprint) })]
        private static class ProfileScope_New_Patch2 {
            public static void Postfix(ref IDisposable __result, string text, SimpleBlueprint _) {
                Mod.Log($"ProfileScope: {text ?? "null scope"}".pink());
            }
        }
        [HarmonyPatch]
        static class ProfileScope_New_Patch {
            static IEnumerable<MethodInfo> TargetMethods()
                => typeof(ProfileScope).GetMethods(nameof(ProfileScope.New)); //not sure if you need binding flags here

            static void Postfix(string text)
                => Mod.Log($"ProfileScope: {text ?? "null scope"}".pink());
        }
#endif

#endif
    }
#endif
}