using HarmonyLib;
using Kingmaker.Blueprints.Root;
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
        [HarmonyPatch(typeof(UnitHelper), nameof(UnitHelper.CreateMoveCommandParamsRT))]
        private static class UnitHelper_CreateMoveCommandParamsRT_Patch {
            private static int GetMaxWalkDistance() {
                return (int)(Settings.walkRangeMultiplier * BlueprintRoot.Instance.MaxWalkDistance);
            }
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> CreateMoveCommandParamsRT(IEnumerable<CodeInstruction> instructions) {
                var fieldInfo = AccessTools.Field(typeof(BlueprintRoot), nameof(BlueprintRoot.MaxWalkDistance));
                var methodInfo = AccessTools.Method(typeof(UnitHelper_CreateMoveCommandParamsRT_Patch), nameof(GetMaxWalkDistance));

                foreach (var instruction in instructions) {
                    if (instruction.opcode == OpCodes.Ldfld && instruction.operand as FieldInfo == fieldInfo) {
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Call, methodInfo);
                        continue;
                    }
                    yield return instruction;
                }
            }
        }
    }
}