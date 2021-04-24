//
// TO DO:
// 1. ref ReturnType
// 2. Nullable<T> support
//

using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace ModMaker.Utility
{
    public static partial class ReflectionCache
    {
        private const BindingFlags ALL_FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic /*| BindingFlags.FlattenHierarchy*/;

        private static readonly Queue _cache = new Queue();

        public static int Count => _cache.Count;

        public static int SizeLimit { get; set;} = 1000;

        public static void Clear()
        {
            _fieldCache.Clear();
            _propertieCache.Clear();
            _methodCache.Clear();
            _cache.Clear();
        }

        private static void EnqueueCache(object obj)
        {
            while (_cache.Count >= SizeLimit && _cache.Count > 0)
                _cache.Dequeue();
            _cache.Enqueue(obj);
        }

        private static bool IsStatic(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        private static TypeBuilder RequestTypeBuilder()
        {
            AssemblyName asmName = new AssemblyName(nameof(ReflectionCache) + "." + Guid.NewGuid().ToString());
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = asmBuilder.DefineDynamicModule("<Module>");
            return moduleBuilder.DefineType("<Type>");
        }
    }
}
