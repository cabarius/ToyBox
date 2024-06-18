using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ModKit.Utility {
    public static partial class ReflectionCache {
        private static readonly DoubleDictionary<Type, string?, WeakReference> _propertieCache = new();

        private static CachedProperty<TProperty> GetPropertyCache<T, TProperty>(string? name) {
            object cache = null;
            if (_propertieCache.TryGetValue(typeof(T), name, out var weakRef))
                cache = weakRef.Target;
            if (cache == null) {
                if (typeof(T).IsValueType)
                    cache = new CachedPropertyOfStruct<T, TProperty>(name);
                else
                    cache = new CachedPropertyOfClass<T, TProperty>(name);
                _propertieCache[typeof(T), name] = new WeakReference(cache);
                EnqueueCache(cache);
            }
            return cache as CachedProperty<TProperty>;
        }

        private static CachedProperty<TProperty> GetPropertyCache<TProperty>(Type type, string? name) {
            object cache = null;
            if (_propertieCache.TryGetValue(type, name, out var weakRef))
                cache = weakRef.Target;
            if (cache == null) {
                cache =
                    IsStatic(type) ?
                    new CachedPropertyOfStatic<TProperty>(type, name) :
                    type.IsValueType ?
                    Activator.CreateInstance(typeof(CachedPropertyOfStruct<,>).MakeGenericType(type, typeof(TProperty)), name) :
                    Activator.CreateInstance(typeof(CachedPropertyOfClass<,>).MakeGenericType(type, typeof(TProperty)), name);
                _propertieCache[type, name] = new WeakReference(cache);
                EnqueueCache(cache);
            }
            return cache as CachedProperty<TProperty>;
        }

        public static PropertyInfo GetPropertyInfo<T, TProperty>(string? name) => GetPropertyCache<T, TProperty>(name).Info;

        public static PropertyInfo GetPropertyInfo<TProperty>(this Type type, string? name) => GetPropertyCache<TProperty>(type, name).Info;

        public static TProperty GetPropertyValue<T, TProperty>(this ref T instance, string? name) where T : struct => (GetPropertyCache<T, TProperty>(name) as CachedPropertyOfStruct<T, TProperty>).Get(ref instance);

        public static TProperty GetPropertyValue<T, TProperty>(this T instance, string? name) where T : class => (GetPropertyCache<T, TProperty>(name) as CachedPropertyOfClass<T, TProperty>).Get(instance);

        public static TProperty GetPropertyValue<T, TProperty>(string? name) => GetPropertyCache<T, TProperty>(name).Get();

        public static TProperty GetPropertyValue<TProperty>(this Type type, string? name) => GetPropertyCache<TProperty>(type, name).Get();

        public static void SetPropertyValue<T, TProperty>(this ref T instance, string? name, TProperty value) where T : struct => (GetPropertyCache<T, TProperty>(name) as CachedPropertyOfStruct<T, TProperty>).Set(ref instance, value);

        public static void SetPropertyValue<T, TProperty>(this T instance, string? name, TProperty value) where T : class => (GetPropertyCache<T, TProperty>(name) as CachedPropertyOfClass<T, TProperty>).Set(instance, value);

        public static void SetPropertyValue<T, TProperty>(string? name, TProperty value) => GetPropertyCache<T, TProperty>(name).Set(value);

        public static void SetPropertyValue<TProperty>(this Type type, string? name, TProperty value) => GetPropertyCache<TProperty>(type, name).Set(value);

        private abstract class CachedProperty<TProperty> {
            public readonly PropertyInfo Info;

            protected CachedProperty(Type type, string? name) {
                Info = type.GetProperties(ALL_FLAGS).FirstOrDefault(item => item.Name == name);

                if (Info == null || Info.PropertyType != typeof(TProperty))
                    throw new InvalidOperationException();
                else if (Info.DeclaringType != type)
                    Info = Info.DeclaringType.GetProperties(ALL_FLAGS).FirstOrDefault(item => item.Name == name);
            }

            // for static property
            public abstract TProperty Get();

            // for static property
            public abstract void Set(TProperty value);

            protected Delegate CreateGetter(Type delType, MethodInfo getter, bool isInstByRef) {
                if (getter.IsStatic) {
                    DynamicMethod method = new(
                    name: "get_" + Info.Name,
                    returnType: Info.PropertyType,
                    parameterTypes: new[] { isInstByRef ? Info.DeclaringType.MakeByRefType() : Info.DeclaringType },
                    owner: typeof(CachedProperty<TProperty>),
                    skipVisibility: true);
                    method.DefineParameter(1, ParameterAttributes.In, "instance");
                    var il = method.GetILGenerator();
                    il.Emit(OpCodes.Call, getter);
                    il.Emit(OpCodes.Ret);
                    return method.CreateDelegate(delType);
                } else {
                    return Delegate.CreateDelegate(delType, getter);
                }
            }

            protected Delegate CreateSetter(Type delType, MethodInfo setter, bool isInstByRef) {
                if (setter.IsStatic) {
                    DynamicMethod method = new(
                    name: "set_" + Info.Name,
                    returnType: null,
                    parameterTypes: new[] { isInstByRef ? Info.DeclaringType.MakeByRefType() : Info.DeclaringType, Info.PropertyType },
                    owner: typeof(CachedProperty<TProperty>),
                    skipVisibility: true);
                    method.DefineParameter(1, ParameterAttributes.In, "instance");
                    method.DefineParameter(2, ParameterAttributes.In, "value");
                    var il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, setter);
                    il.Emit(OpCodes.Ret);
                    return method.CreateDelegate(delType);
                } else {
                    return Delegate.CreateDelegate(delType, setter);
                }
            }
        }

        private class CachedPropertyOfStruct<T, TProperty> : CachedProperty<TProperty> {
            private delegate TProperty Getter(ref T instance);
            private delegate void Setter(ref T instance, TProperty value);

            private T _dummy = default;
            private Getter _getter;
            private Setter _setter;

            public CachedPropertyOfStruct(string? name) : base(typeof(T), name) { }

            public override TProperty Get() => (_getter ??= CreateGetter(typeof(Getter), Info.GetMethod, true) as Getter)(ref _dummy);

            public TProperty Get(ref T instance) => (_getter ??= CreateGetter(typeof(Getter), Info.GetMethod, true) as Getter)(ref instance);

            public override void Set(TProperty value) => (_setter ??= CreateSetter(typeof(Setter), Info.SetMethod, true) as Setter)(ref _dummy, value);

            public void Set(ref T instance, TProperty value) => (_setter ??= CreateSetter(typeof(Setter), Info.SetMethod, true) as Setter)(ref instance, value);
        }

        private class CachedPropertyOfClass<T, TProperty> : CachedProperty<TProperty> {
            private delegate TProperty Getter(T instance);
            private delegate void Setter(T instance, TProperty value);

            private readonly T _dummy = default;
            private Getter _getter;
            private Setter _setter;

            public CachedPropertyOfClass(string? name) : base(typeof(T), name) { }

            public override TProperty Get() => (_getter ??= CreateGetter(typeof(Getter), Info.GetMethod, false) as Getter)(_dummy);

            public TProperty Get(T instance) => (_getter ??= CreateGetter(typeof(Getter), Info.GetMethod, false) as Getter)(instance);

            public override void Set(TProperty value) => (_setter ??= CreateSetter(typeof(Setter), Info.SetMethod, false) as Setter)(_dummy, value);

            public void Set(T instance, TProperty value) => (_setter ??= CreateSetter(typeof(Setter), Info.SetMethod, false) as Setter)(instance, value);
        }

        private class CachedPropertyOfStatic<TProperty> : CachedProperty<TProperty> {
            private delegate TProperty Getter();
            private delegate void Setter(TProperty value);

            private Getter _getter;
            private Setter _setter;

            public CachedPropertyOfStatic(Type type, string? name) : base(type, name) {
                //if (!IsStatic(type))
                //    throw new InvalidOperationException();
            }

            public override TProperty Get() => (_getter ??= CreateGetter())();

            public override void Set(TProperty value) => (_setter ??= CreateSetter())(value);

            private Getter CreateGetter() => Delegate.CreateDelegate(typeof(Getter), Info.GetMethod) as Getter;

            private Setter CreateSetter() => Delegate.CreateDelegate(typeof(Setter), Info.SetMethod) as Setter;
        }
    }
}
