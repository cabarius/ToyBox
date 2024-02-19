using ModKit.Utility;
using System;
using System.Reflection.Emit;

namespace ModKit.DataViewer {
    internal static class UnsafeForceCast {
        private static readonly DoubleDictionary<Type, Type, WeakReference> _cache = new();

        public static Func<TInput, TOutput>? GetDelegate<TInput, TOutput>() {
            Func<TInput, TOutput>? cache = default;
            if (_cache.TryGetValue(typeof(TInput), typeof(TOutput), out var weakRef))
                cache = weakRef.Target as Func<TInput, TOutput>;
            if (cache == null) {
                cache = CreateDelegate<TInput, TOutput>();
                _cache[typeof(TInput), typeof(TOutput)] = new WeakReference(cache);
            }
            return cache;
        }

        private static Func<TInput, TOutput>? CreateDelegate<TInput, TOutput>() {
            var method = new DynamicMethod(
                "UnsafeForceCast",
                typeof(TOutput),
                new[] { typeof(TInput) });

            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (typeof(TInput) == typeof(object) && typeof(TOutput).IsValueType)
                il.Emit(OpCodes.Unbox_Any, typeof(TOutput));
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(Func<TInput, TOutput>)) as Func<TInput, TOutput>;
        }
    }
}