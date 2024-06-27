using HarmonyLib;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Units;
using Kingmaker.UnitLogic;
using Kingmaker.View.MapObjects.Traps;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ToyBox.BagOfPatches {
    [HarmonyPatch]
    internal static partial class Tweaks {
        [HarmonyPatch(typeof(TrapObjectData), nameof(TrapObjectData.TryTriggerTrap)), HarmonyPrefix]
        private static bool TryTriggerTrap() {
            if (Settings.disableTraps) {
                return false;
            }
            return true;
        }
        [HarmonyPatch]
        private static class UnitHelper_CreateMoveCommandParamsRT_Patch {
            private static int GetMaxWalkDistance() {
                return (int)(Settings.walkRangeMultiplier * BlueprintRoot.Instance.MaxWalkDistance);
            }
            private static int GetMinSprintDistance() {
                return (int)(Settings.sprintRangeMultiplier * BlueprintRoot.Instance.MinSprintDistance);
            }
            [HarmonyTargetMethods]
            public static IEnumerable<MethodInfo> GetMethods() {
                yield return AccessTools.Method(typeof(UnitHelper), nameof(UnitHelper.CreateMoveCommandParamsRT));
                yield return AccessTools.Method(typeof(UnitCommandsRunner), nameof(UnitCommandsRunner.TryApproachAndInteract));
            }
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> CreateMoveCommandParamsRT(IEnumerable<CodeInstruction> instructions) {
                var fieldInfo = AccessTools.Field(typeof(BlueprintRoot), nameof(BlueprintRoot.MaxWalkDistance));
                var methodInfo = AccessTools.Method(typeof(UnitHelper_CreateMoveCommandParamsRT_Patch), nameof(GetMaxWalkDistance));

                var fieldInfo2 = AccessTools.Field(typeof(BlueprintRoot), nameof(BlueprintRoot.MinSprintDistance));
                var methodInfo2 = AccessTools.Method(typeof(UnitHelper_CreateMoveCommandParamsRT_Patch), nameof(GetMinSprintDistance));
                foreach (var instruction in instructions) {
                    if (instruction.opcode == OpCodes.Ldfld && instruction.operand as FieldInfo == fieldInfo) {
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Call, methodInfo);
                        continue;
                    }
                    if (instruction.opcode == OpCodes.Ldfld && instruction.operand as FieldInfo == fieldInfo2) {
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Call, methodInfo2);
                        continue;
                    }
                    yield return instruction;
                }
            }
        }
    }
}