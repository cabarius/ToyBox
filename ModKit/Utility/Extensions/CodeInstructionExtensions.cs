using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ModKit.Utility {
    public static class CodeInstructionExtensions {
        #region Debug

        public static IEnumerable<CodeInstruction> Complete(this IEnumerable<CodeInstruction> codes) => codes;

        public static IEnumerable<CodeInstruction> Dump(this IEnumerable<CodeInstruction> codes, Action<string> print) {
            Dictionary<Label, string> labels = new();
            var ie = codes.GetEnumerator();
            for (var i = 0; ie.MoveNext(); i++)
                foreach (var l in ie.Current.labels)
                    labels[l] = $"L_{i:X4}";

            print("==== Begin Dumping Instructions ====");
            ie = codes.GetEnumerator();
            for (var i = 0; ie.MoveNext(); i++)
                print($"L_{i:X4}: {ie.Current.opcode}" +
                    (ie.Current.operand == null ?
                    string.Empty : $" {(ie.Current.operand is Label l ? labels[l] : ie.Current.operand)}"));
            print("==== End Dumping Instructions ====");

            return codes;
        }

        #endregion

        #region Preset Logic

        public static IEnumerable<CodeInstruction> Patch<TState>(this IEnumerable<CodeInstruction> codes, ILGenerator il,
            Func<TState> prefix, Action<TState> postfix) {
            // TState state = prefix();
            // try
            // {
            //     originalMethod();
            // }
            // finally
            // {
            //     postfix(state);
            // }

            var state = il.DeclareLocal(typeof(TState));
            var ret = il.DefineLabel();

            return codes
                .ReplaceAll(new CodeInstruction(OpCodes.Ret), new CodeInstruction(OpCodes.Leave, ret), true)
                .AddRange(new CodeInstruction[] {
                    //new CodeInstruction(OpCodes.Pop) { blocks = Blocks(ExceptionBlockType.BeginCatchBlock) },
                    //new CodeInstruction(OpCodes.Rethrow),
                    new CodeInstruction(OpCodes.Ldloc, state).BeginFinallyBlock(),
                    new CodeInstruction(OpCodes.Call, postfix.Method),
                    new CodeInstruction(OpCodes.Endfinally).EndExceptionBlock(),
                    new CodeInstruction(OpCodes.Ret).MarkLabel(ret)
                })
                .InsertRange(0, new CodeInstruction[] {
                    new CodeInstruction(OpCodes.Call, prefix.Method),
                    new CodeInstruction(OpCodes.Stloc, state),
                    new CodeInstruction(OpCodes.Nop).BeginExceptionBlock()
                }, false);
        }

        public static IEnumerable<CodeInstruction> Patch<TInstance, TState>(this IEnumerable<CodeInstruction> codes, ILGenerator il,
            Func<TInstance, TState> prefix, Action<TInstance, TState> postfix) {
            // TState state = prefix(this);
            // try
            // {
            //     originalMethod();
            // }
            // finally
            // {
            //     postfix(this, state);
            // }

            var state = il.DeclareLocal(typeof(TState));
            var ret = il.DefineLabel();

            return codes
                .ReplaceAll(new CodeInstruction(OpCodes.Ret), new CodeInstruction(OpCodes.Leave, ret), true)
                .AddRange(new CodeInstruction[] {
                    //new CodeInstruction(OpCodes.Pop) { blocks = Blocks(ExceptionBlockType.BeginCatchBlock) },
                    //new CodeInstruction(OpCodes.Rethrow),
                    new CodeInstruction(OpCodes.Ldarg_0).BeginFinallyBlock(),
                    new CodeInstruction(OpCodes.Ldloc, state),
                    new CodeInstruction(OpCodes.Call, postfix.Method),
                    new CodeInstruction(OpCodes.Endfinally).EndExceptionBlock(),
                    new CodeInstruction(OpCodes.Ret).MarkLabel(ret)
                })
                .InsertRange(0, new CodeInstruction[] {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, prefix.Method),
                    new CodeInstruction(OpCodes.Stloc, state),
                    new CodeInstruction(OpCodes.Nop).BeginExceptionBlock()
                }, false);
        }

        #endregion

        #region Collection Modifying

        public static IEnumerable<CodeInstruction> Add(this IEnumerable<CodeInstruction> codes,
            CodeInstruction newCode) => codes.Concat(new CodeInstruction[] { newCode });

        public static IEnumerable<CodeInstruction> AddRange(this IEnumerable<CodeInstruction> codes,
            IEnumerable<CodeInstruction> newCodes) => codes.Concat(newCodes);

        public static IEnumerable<CodeInstruction> Insert(this IEnumerable<CodeInstruction> codes,
            int index, CodeInstruction newCode, bool moveLabelsAtIndex = false) => codes.InsertRange(index, new CodeInstruction[] { newCode }, moveLabelsAtIndex);

        public static IEnumerable<CodeInstruction> InsertRange(this IEnumerable<CodeInstruction> codes,
            int index, IEnumerable<CodeInstruction> newCodes, bool moveLabelsFromIndex = false) {
            if (moveLabelsFromIndex)
                codes.MoveLabels(index, newCodes, 0, newCodes.Where(code => code.operand is Label).Select(code => (Label)code.operand));
            return codes.Take(index).Concat(newCodes).Concat(codes.Skip(index));
        }

        public static IEnumerable<CodeInstruction> Remove(this IEnumerable<CodeInstruction> codes,
            int index, bool moveLabelsFromIndex = false) => codes.RemoveRange(index, 1, moveLabelsFromIndex);

        public static IEnumerable<CodeInstruction> RemoveRange(this IEnumerable<CodeInstruction> codes,
            int index, int count, bool moveLabelsFromIndex = false) {
            if (moveLabelsFromIndex)
                codes.MoveLabels(index, codes, index + count);
            return codes.Take(index).Concat(codes.Skip(index + count));
        }

        public static IEnumerable<CodeInstruction> Replace(this IEnumerable<CodeInstruction> codes,
            int index, CodeInstruction newCode, bool moveLabelsFromIndex = false) => codes.ReplaceRange(index, 1, new CodeInstruction[] { newCode }, moveLabelsFromIndex);

        public static IEnumerable<CodeInstruction> ReplaceRange(this IEnumerable<CodeInstruction> codes,
            int index, int count, IEnumerable<CodeInstruction> newCodes, bool moveLabelsFromIndex = false) {
            if (moveLabelsFromIndex)
                codes.MoveLabels(index, newCodes, 0, newCodes.Where(code => code.operand is Label).Select(code => (Label)code.operand));
            return codes.Take(index).Concat(newCodes).Concat(codes.Skip(index + count));
        }

        public static IEnumerable<CodeInstruction> ReplaceAll(this IEnumerable<CodeInstruction> codes,
            CodeInstruction findingCode, CodeInstruction newCode,
            bool moveLabelsFromIndex = false, IEqualityComparer<CodeInstruction>? comparer = null) => codes.ReplaceAll(findingCode, newCode, out _, moveLabelsFromIndex, comparer);

        public static IEnumerable<CodeInstruction> ReplaceAll(this IEnumerable<CodeInstruction> codes,
            CodeInstruction findingCode, CodeInstruction newCode,
            out int replaced, bool moveLabelsFromIndex = false, IEqualityComparer<CodeInstruction>? comparer = null) => codes.ReplaceAll(new CodeInstruction[] { findingCode }, new CodeInstruction[] { newCode }, out replaced, moveLabelsFromIndex, comparer);

        public static IEnumerable<CodeInstruction> ReplaceAll(this IEnumerable<CodeInstruction> codes,
            IEnumerable<CodeInstruction> findingCodes, IEnumerable<CodeInstruction> newCodes,
            bool moveLabelsFromIndex = false, IEqualityComparer<CodeInstruction>? comparer = null) => codes.ReplaceAll(findingCodes, newCodes, out _, moveLabelsFromIndex, comparer);

        public static IEnumerable<CodeInstruction> ReplaceAll(this IEnumerable<CodeInstruction> codes,
            IEnumerable<CodeInstruction> findingCodes, IEnumerable<CodeInstruction> newCodes,
            out int replaced, bool moveLabelsFromIndex = false, IEqualityComparer<CodeInstruction>? comparer = null) {
            replaced = 0;
            if (comparer == null)
                comparer = new CodeInstructionMatchComparer();
            var findingCodesCount = findingCodes.Count();
            var newCodesCount = newCodes.Count();
            if (findingCodesCount > 0) {
                var i = codes.Count() - findingCodesCount;
                while (i >= 0) {
                    if (codes.MatchCodes(i, findingCodes, comparer)) {
                        codes = (newCodesCount > 0) ?
                            codes.ReplaceRange(i, findingCodesCount, (moveLabelsFromIndex && replaced > 0) ?
                                new CodeInstruction[] { newCodes.First().Clone() }.Concat(newCodes.Skip(1)) : newCodes,
                                moveLabelsFromIndex) :
                            codes.RemoveRange(i, findingCodesCount, moveLabelsFromIndex);
                        replaced++;
                        i -= findingCodesCount;
                    } else {
                        i--;
                    }
                }
            }
            return codes;
        }

        #endregion 

        public static CodeInstruction Item(this IEnumerable<CodeInstruction> codes, int index) => codes.ElementAt(index);

        #region Label

        public static Label NewLabel(this CodeInstruction code, ILGenerator il) {
            var label = il.DefineLabel();
            code.MarkLabel(label);
            return label;
        }

        public static Label NewLabel(this IEnumerable<CodeInstruction> codes, int index, ILGenerator il) => codes.Item(index).NewLabel(il);

        public static CodeInstruction MarkLabel(this CodeInstruction code, Label newLabel) {
            code.labels.Add(newLabel);
            return code;
        }

        public static CodeInstruction MarkLabel(this CodeInstruction code, IEnumerable<Label> newLabel) {
            code.labels.AddRange(newLabel);
            return code;
        }

        public static void MoveLabels(this IEnumerable<CodeInstruction> codes,
            int index, IEnumerable<CodeInstruction> targetCodes, int targetIndex) {
            var labels = codes.Item(index).labels;
            targetCodes.Item(targetIndex).MarkLabel(labels);
            labels.Clear();
        }

        public static void MoveLabels(this IEnumerable<CodeInstruction> codes,
            int index, IEnumerable<CodeInstruction> targetCodes, int targetIndex, IEnumerable<Label> skipLabels) {
            var source = codes.Item(index).labels;
            var target = targetCodes.Item(targetIndex).labels;
            HashSet<Label> skip = new(skipLabels);
            var i = 0;
            while (i < source.Count) {
                if (skip.Contains(source[i])) {
                    i++;
                } else {
                    target.Add(source[i]);
                    source.RemoveAt(i);
                }
            }
        }

        public static void RemoveLabel(this IEnumerable<CodeInstruction> codes, int index, Label label) => codes.Item(index).labels.RemoveAll(item => item == label);

        public static void RemoveLabel(this IEnumerable<CodeInstruction> codes, int index, IEnumerable<Label> labels) {
            labels = new HashSet<Label>(labels);
            codes.Item(index).labels.RemoveAll(item => labels.Contains(item));
        }

        #endregion

        #region Exception Block

        public static CodeInstruction BeginCatchBlock(this CodeInstruction code, Type? catchType = null) {
            code.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, catchType ?? typeof(object)));
            return code;
        }

        public static CodeInstruction BeginExceptionBlock(this CodeInstruction code, Type? catchType = null) {
            code.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, catchType ?? typeof(object)));
            return code;
        }

        public static CodeInstruction BeginFinallyBlock(this CodeInstruction code, Type? catchType = null) {
            code.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock, catchType ?? typeof(object)));
            return code;
        }

        public static CodeInstruction EndExceptionBlock(this CodeInstruction code, Type? catchType = null) {
            code.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock, catchType ?? typeof(object)));
            return code;
        }

        #endregion

        #region Collection Searching

        public static int FindCodes(this IEnumerable<CodeInstruction> codes, IEnumerable<CodeInstruction> findingCodes) => codes.FindCodes(findingCodes, new CodeInstructionMatchComparer());

        public static int FindCodes(this IEnumerable<CodeInstruction> codes,
            int startIndex, IEnumerable<CodeInstruction> findingCodes) => codes.FindCodes(startIndex, findingCodes, new CodeInstructionMatchComparer());

        public static int FindCodes(this IEnumerable<CodeInstruction> codes,
            IEnumerable<CodeInstruction> findingCodes, IEqualityComparer<CodeInstruction> comparer) => codes.FindCodes(0, findingCodes, comparer);

        public static int FindCodes(this IEnumerable<CodeInstruction> codes,
            int startIndex, IEnumerable<CodeInstruction> findingCodes, IEqualityComparer<CodeInstruction> comparer) {
            if (findingCodes.Any()) {
                var ubound = codes.Count() - findingCodes.Count();
                for (var i = startIndex; i <= ubound; i++) {
                    if (codes.MatchCodes(i, findingCodes, comparer))
                        return i;
                }
            }
            return -1;
        }

        public static int FindLastCodes(this IEnumerable<CodeInstruction> codes,
            IEnumerable<CodeInstruction> findingCodes) => codes.FindLastCodes(findingCodes, new CodeInstructionMatchComparer());

        public static int FindLastCodes(this IEnumerable<CodeInstruction> codes,
            IEnumerable<CodeInstruction> findingCodes, IEqualityComparer<CodeInstruction> comparer) {
            if (findingCodes.Any()) {
                var ubound = codes.Count() - findingCodes.Count();
                for (var i = ubound; i >= 0; i--) {
                    if (codes.MatchCodes(i, findingCodes, comparer))
                        return i;
                }
            }
            return -1;
        }

        public static bool MatchCodes(this IEnumerable<CodeInstruction> codes,
            int startIndex, IEnumerable<CodeInstruction> matchingCodes) => codes.MatchCodes(startIndex, matchingCodes, new CodeInstructionMatchComparer());

        public static bool MatchCodes(this IEnumerable<CodeInstruction> codes,
            int startIndex, IEnumerable<CodeInstruction> matchingCodes, IEqualityComparer<CodeInstruction> comparer) => codes.Skip(startIndex).Take(matchingCodes.Count()).SequenceEqual(matchingCodes, comparer);

        public class CodeInstructionMatchComparer : IEqualityComparer<CodeInstruction> {
            public bool Equals(CodeInstruction x, CodeInstruction y) {
                if (y == null)
                    return true;
                else if (x == null)
                    return false;
                else if ((y.opcode == default || OpCodeEquals(y.opcode, x.opcode)) &&
                        (y.operand == null || (y.operand is ValueType ? y.operand.Equals(x.operand) : y.operand == x.operand)) &&
                        (y.labels.Count == 0 || y.labels.TrueForAll(label => x.labels.Contains(label))))
                    return true;
                else
                    return false;
            }

            public int GetHashCode(CodeInstruction obj) => throw new NotImplementedException();

            private bool OpCodeEquals(OpCode x, OpCode y) {
                if (x == OpCodes.Br || x == OpCodes.Br_S)
                    return y == OpCodes.Br || y == OpCodes.Br_S;
                else if (x == OpCodes.Brtrue || x == OpCodes.Brtrue_S)
                    return y == OpCodes.Brtrue || y == OpCodes.Brtrue_S;
                else if (x == OpCodes.Brfalse || x == OpCodes.Brfalse_S)
                    return y == OpCodes.Brfalse || y == OpCodes.Brfalse_S;
                else
                    return x == y;
            }
        }

        #endregion 
    }
}
