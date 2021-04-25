using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ModMaker.Utility
{
    public static partial class ReflectionCache
    {
        private static readonly DoubleDictionary<Type, string, WeakReference> _fieldCache = new DoubleDictionary<Type, string, WeakReference>();

        private static CachedField<TField> GetFieldCache<T, TField>(string name)
        {
            object cache = default;
            if (_fieldCache.TryGetValue(typeof(T), name, out WeakReference weakRef))
                cache = weakRef.Target;
            if (cache == null)
            {
                if (typeof(T).IsValueType)
                    cache = new CachedFieldOfStruct<T, TField>(name);
                else
                    cache = new CachedFieldOfClass<T, TField>(name);
                _fieldCache[typeof(T), name] = new WeakReference(cache);
                EnqueueCache(cache);
            }
            return cache as CachedField<TField>;
        }

        private static CachedField<TField> GetFieldCache<TField>(Type type, string name)
        {
            object cache = null;
            if (_fieldCache.TryGetValue(type, name, out WeakReference weakRef))
                cache = weakRef.Target;
            if (cache == null)
            {
                cache = 
                    IsStatic(type) ?
                    new CachedFieldOfStatic<TField>(type, name) :
                    type.IsValueType ?
                    Activator.CreateInstance(typeof(CachedFieldOfStruct<,>).MakeGenericType(type, typeof(TField)), name) :
                    Activator.CreateInstance(typeof(CachedFieldOfClass<,>).MakeGenericType(type, typeof(TField)), name);
                _fieldCache[type, name] = new WeakReference(cache);
                EnqueueCache(cache);
            }
            return cache as CachedField<TField>;
        }

        public static FieldInfo GetFieldInfo<T, TField>(string name)
        {
            return GetFieldCache<T, TField>(name).Info;
        }

        public static FieldInfo GetFieldInfo<TField>(this Type type, string name)
        {
            return GetFieldCache<TField>(type, name).Info;
        }

        public static ref TField GetFieldRef<T, TField>(this ref T instance, string name) where T : struct
        {
            return ref (GetFieldCache<T, TField>(name) as CachedFieldOfStruct<T, TField>).GetRef(ref instance);
        }

        public static ref TField GetFieldRef<T, TField>(this T instance, string name) where T : class
        {
            return ref (GetFieldCache<T, TField>(name) as CachedFieldOfClass<T, TField>).GetRef(instance);
        }

        public static TField GetFieldValue<T, TField>(this ref T instance, string name) where T : struct
        {
            return (GetFieldCache<T, TField>(name) as CachedFieldOfStruct<T, TField>).Get(ref instance);
        }

        public static TField GetFieldValue<T, TField>(this T instance, string name) where T : class
        {
            return (GetFieldCache<T, TField>(name) as CachedFieldOfClass<T, TField>).Get(instance);
        }

        public static TField GetFieldValue<T, TField>(string name)
        {
            return GetFieldCache<T, TField>(name).Get();
        }

        public static TField GetFieldValue<TField>(this Type type, string name)
        {
            return GetFieldCache<TField>(type, name).Get();
        }

        public static void SetFieldValue<T, TField>(this ref T instance, string name, TField value) where T : struct
        {
            (GetFieldCache<T, TField>(name) as CachedFieldOfStruct<T, TField>).Set(ref instance, value);
        }

        public static void SetFieldValue<T, TField>(this T instance, string name, TField value) where T : class
        {
            (GetFieldCache<T, TField>(name) as CachedFieldOfClass<T, TField>).Set(instance, value);
        }

        public static void SetFieldValue<T, TField>(string name, TField value)
        {
            GetFieldCache<T, TField>(name).Set(value);
        }

        public static void SetFieldValue<TField>(this Type type, string name, TField value)
        {
            GetFieldCache<TField>(type, name).Set(value);
        }

        private abstract class CachedField<TField>
        {
            public readonly FieldInfo Info;

            public CachedField(Type type, string name)
            {
                Info = type.GetFields(ALL_FLAGS).FirstOrDefault(item => item.Name == name);

                if (Info == null || Info.FieldType != typeof(TField))
                    throw new InvalidOperationException();
            }

            // for static field
            public abstract TField Get();

            // for static field
            public abstract void Set(TField value);

            protected Delegate CreateGetter(Type delType, bool isInstByRef)
            {
                DynamicMethod method = new DynamicMethod(
                    name: "get_" + Info.Name,
                    returnType: Info.FieldType,
                    parameterTypes: new[] { isInstByRef ? Info.DeclaringType.MakeByRefType() : Info.DeclaringType },
                    owner: typeof(CachedField<TField>),
                    skipVisibility: true);
                method.DefineParameter(1, ParameterAttributes.In, "instance");

                ILGenerator il = method.GetILGenerator();
                if (Info.IsStatic)
                {
                    il.Emit(OpCodes.Ldsfld, Info);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, Info);
                }
                il.Emit(OpCodes.Ret);
                
                return method.CreateDelegate(delType);
            }

            protected Delegate CreateRefGetter(Type delType, bool isInstByRef)
            {
                // DynamicMethod does not allow ref return type before .Net Core 2.1
                TypeBuilder typeBuilder = RequestTypeBuilder();
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    name: "getRef_" + Info.Name,
                    attributes: MethodAttributes.Static | MethodAttributes.Public,
                    returnType: Info.FieldType.MakeByRefType(),
                    parameterTypes: new[] { isInstByRef? Info.DeclaringType.MakeByRefType() : Info.DeclaringType });
                methodBuilder.DefineParameter(1, ParameterAttributes.In, "instance");

                ILGenerator il = methodBuilder.GetILGenerator();
                if (Info.IsStatic)
                {
                    il.Emit(OpCodes.Ldsflda, Info);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldflda, Info);
                }
                il.Emit(OpCodes.Ret);

                Type t = typeBuilder.CreateType();
                MethodInfo method = t.GetMethod(methodBuilder.Name);

                return method.CreateDelegate(delType);
            }

            protected Delegate CreateSetter(Type delType, bool isInstByRef)
            {
                DynamicMethod method = new DynamicMethod(
                    name: "set_" + Info.Name,
                    returnType: null,
                    parameterTypes: new[] { isInstByRef ? Info.DeclaringType.MakeByRefType() : Info.DeclaringType, Info.FieldType },
                    owner: typeof(CachedField<TField>),
                    skipVisibility: true);
                method.DefineParameter(1, ParameterAttributes.In, "instance");
                method.DefineParameter(2, ParameterAttributes.In, "value");

                ILGenerator il = method.GetILGenerator();
                if (Info.IsStatic)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Stsfld, Info);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Stfld, Info);
                }
                il.Emit(OpCodes.Ret);

                return method.CreateDelegate(delType);
            }
        }

        private class CachedFieldOfStruct<T, TField> : CachedField<TField>
        {
            private delegate TField Getter(ref T instance);
            private delegate ref TField RefGetter(ref T instance);
            private delegate void Setter(ref T instance, TField value);

            private T _dummy = default;
            private Getter _getter;
            private RefGetter _refGetter;
            private Setter _setter;

            public CachedFieldOfStruct(string name) : base(typeof(T), name) { }

            public override TField Get()
            {
                return (_getter ?? (_getter = CreateGetter(typeof(Getter), true) as Getter))(ref _dummy);
            }

            public TField Get(ref T instance)
            {
                return (_getter ?? (_getter = CreateGetter(typeof(Getter), true) as Getter))(ref instance);
            }

            public ref TField GetRef(ref T instance)
            {
                return ref (_refGetter ?? (_refGetter = CreateRefGetter(typeof(RefGetter), true) as RefGetter))(ref instance);
            }

            public override void Set(TField value)
            {
                (_setter ?? (_setter = CreateSetter(typeof(Setter), true) as Setter))(ref _dummy, value);
            }

            public void Set(ref T instance, TField value)
            {
                (_setter ?? (_setter = CreateSetter(typeof(Setter), true) as Setter))(ref instance, value);
            }
        }

        private class CachedFieldOfClass<T, TField> : CachedField<TField>
        {
            private delegate TField Getter(T instance);
            private delegate ref TField RefGetter(T instance);
            private delegate void Setter(T instance, TField value);

            private T _dummy = default;
            private Getter _getter;
            private RefGetter _refGetter;
            private Setter _setter;

            public CachedFieldOfClass(string name) : base(typeof(T), name) { }

            public override TField Get()
            {
                return (_getter ?? (_getter = CreateGetter(typeof(Getter), false) as Getter))(_dummy);
            }

            public TField Get(T instance)
            {
                return (_getter ?? (_getter = CreateGetter(typeof(Getter), false) as Getter))(instance);
            }

            public ref TField GetRef(T instance)
            {
                return ref (_refGetter ?? (_refGetter = CreateRefGetter(typeof(RefGetter), false) as RefGetter))(instance);
            }

            public override void Set(TField value)
            {
                (_setter ?? (_setter = CreateSetter(typeof(Setter), false) as Setter))(_dummy, value);
            }

            public void Set(T instance, TField value)
            {
                (_setter ?? (_setter = CreateSetter(typeof(Setter), false) as Setter))(instance, value);
            }
        }

        private class CachedFieldOfStatic<TField> : CachedField<TField>
        {
            private delegate TField Getter();
            private delegate void Setter(TField value);

            private Getter _getter;
            private Setter _setter;

            public CachedFieldOfStatic(Type type, string name) : base(type, name)
            {
                //if (!IsStatic(type))
                //    throw new InvalidOperationException();
            }

            public override TField Get()
            {
                return (_getter ?? (_getter = CreateGetter()))();
            }

            public override void Set(TField value)
            {
                (_setter ?? (_setter = CreateSetter()))(value);
            }

            private Getter CreateGetter()
            {
                DynamicMethod method = new DynamicMethod(
                    name: "get_" + Info.Name,
                    returnType: Info.FieldType,
                    parameterTypes: null,
                    owner: typeof(CachedField<TField>),
                    skipVisibility: true);

                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldsfld, Info);
                il.Emit(OpCodes.Ret);

                return method.CreateDelegate(typeof(Getter)) as Getter;
            }

            private Setter CreateSetter()
            {
                DynamicMethod method = new DynamicMethod(
                    name: "set_" + Info.Name,
                    returnType: null,
                    parameterTypes: new[] { Info.FieldType },
                    owner: typeof(CachedField<TField>),
                    skipVisibility: true);
                method.DefineParameter(1, ParameterAttributes.In, "value");

                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stsfld, Info);
                il.Emit(OpCodes.Ret);

                return method.CreateDelegate(typeof(Setter)) as Setter;
            }
        }
    }
}
