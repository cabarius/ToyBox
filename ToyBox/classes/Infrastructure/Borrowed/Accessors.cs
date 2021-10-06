// some stuff borrowed shamelessly and enhanced from Kingdom Resolution Mod
//   "Author": "spacehamster",
//   "HomePage": "https://www.nexusmods.com/pathfinderkingmaker/mods/36",
//   "Repository": "https://raw.githubusercontent.com/spacehamster/KingmakerKingdomResolutionMod/master/KingdomResolution/Repository.json"

using HarmonyLib;
using System;
using System.Linq;

#pragma warning disable 618

namespace ToyBox {
    public delegate TResult FastGetter<TClass, TResult>(TClass source);
    public delegate object FastGetter(object source);
    public delegate void FastSetter(object source, object value);
    public delegate void FastSetter<TClass, TValue>(TClass source, TValue value);
    public delegate object FastInvoker(object target, params object[] paramters);
    public delegate TResult FastInvoker<TClass, TResult>(TClass target);
    public delegate TResult FastInvoker<TClass, T1, TResult>(TClass target, T1 arg1);
    public delegate TResult FastInvoker<TClass, T1, T2, TResult>(TClass target, T1 arg1, T2 arg2);
    public delegate TResult FastInvoker<TClass, T1, T2, T3, TResult>(TClass target, T1 arg1, T2 arg2, T3 arg);
    public delegate object FastStaticInvoker(params object[] parameters);
    public delegate TResult FastStaticInvoker<out TResult>();
    public delegate TResult FastStaticInvoker<in T1, out TResult>(T1 arg1);
    public delegate TResult FastStaticInvoker<in T1, in T2, out TResult>(T1 arg1, T2 arg2);
    public delegate TResult FastStaticInvoker<in T1, in T2, in T3, out TResult>(T1 arg1, T2 arg2, T3 arg3);
    public class Accessors {
        public static AccessTools.FieldRef<TClass, TResult> CreateFieldRef<TClass, TResult>(string name) {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var fieldInfo = AccessTools.Field(classType, name);
            if (fieldInfo == null) {
                throw new Exception($"{classType} does not contain field {name}");
            }
            if (!resultType.IsAssignableFrom(fieldInfo.FieldType)) {
                throw new InvalidCastException($"Cannot cast field type {resultType} as {fieldInfo.FieldType} for class {classType} field {name}");
            }
            return AccessTools.FieldRefAccess<TClass, TResult>(name);
        }
        public static FastGetter<TClass, TResult> CreateGetter<TClass, TResult>(string name) {
            var fieldInfo = AccessTools.Field(typeof(TClass), name);
            var propInfo = AccessTools.Property(typeof(TClass), name);
            if (fieldInfo == null && propInfo == null) {
                throw new Exception($"{typeof(TClass).Name} does not contain field or property {name}");
            }
            var isProp = propInfo != null;
            var memberType = isProp ? propInfo.PropertyType : fieldInfo.FieldType;
            var memberTypeName = isProp ? "property" : "field";
            if (!typeof(TResult).IsAssignableFrom(memberType)) {
                throw new InvalidCastException($"Cannot cast field type {typeof(TResult).Name} as {memberType} for class {typeof(TClass).Name} {memberTypeName} {name}");
            }
            var handler = isProp ?
                FastAccess.CreateGetterHandler<TClass, TResult>(propInfo) :
                FastAccess.CreateGetterHandler<TClass, TResult>(fieldInfo);
            return new FastGetter<TClass, TResult>(handler);
        }
        public static FastSetter<TClass, TValue> CreateSetter<TClass, TValue>(string name) {
            var propertyInfo = AccessTools.Property(typeof(TClass), name);
            var fieldInfo = AccessTools.Field(typeof(TClass), name);
            if (propertyInfo == null && fieldInfo == null) {
                throw new Exception($"{typeof(TClass).Name} does not contain a field or property {name}");
            }
            var isProperty = propertyInfo != null;
            var memberType = isProperty ? propertyInfo.PropertyType : fieldInfo.FieldType;
            var memberTypeName = isProperty ? "property" : "field";
            if (!typeof(TValue).IsAssignableFrom(memberType)) {
                throw new Exception($"Cannot cast property type {typeof(TValue).Name} as {memberType} for class {typeof(TClass).Name} {memberTypeName} {name}");
            }
            var handler = isProperty ?
                FastAccess.CreateSetterHandler<TClass, TValue>(propertyInfo) :
                FastAccess.CreateSetterHandler<TClass, TValue>(fieldInfo);
            return new FastSetter<TClass, TValue>(handler);
        }
        public static FastInvoker CreateInvoker(Type classType, string name, Type resultType, params Type[] parameters) {
            var methodInfo = AccessTools.Method(classType, name, parameters);
            if (methodInfo == null) {
                var argString = string.Join(", ", parameters.Select(t => t.ToString()));
                throw new Exception($"{classType} does not contain method {name} with arguments {argString}");
            }
            if (!resultType.IsAssignableFrom(methodInfo.ReturnType)) {
                throw new Exception($"Cannot cast return type {resultType} as {methodInfo.ReturnType} for class {classType} method {name}");
            }
            var _parameters = methodInfo.GetParameters();
            if (_parameters.Length != parameters.Length) {
                throw new Exception($"Expected {parameters.Length} paramters for class {classType} method {name}");
            }
            for (var i = 0; i < parameters.Length; i++) {
                if (!parameters[i].IsAssignableFrom(_parameters[i].ParameterType)) {
                    throw new Exception($"Cannot cast paramter type {parameters[i]} as {_parameters[i].ParameterType} for class {classType} method {name}");
                }
            }
            return new FastInvoker(MethodInvoker.GetHandler(methodInfo));
        }
        public static FastInvoker<TClass, TResult> CreateInvoker<TClass, TResult>(string name) {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, TResult>((instance) => (TResult)invoker.Invoke(instance));
        }
        public static FastInvoker<TClass, T1, TResult> CreateInvoker<TClass, T1, TResult>(string name) {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { typeof(T1) };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, T1, TResult>((instance, arg1) => (TResult)invoker.Invoke(instance, new object[] { arg1 }));
        }
        public static FastInvoker<TClass, T1, T2, TResult> CreateInvoker<TClass, T1, T2, TResult>(string name) {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { typeof(T1), typeof(T2) };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, T1, T2, TResult>((instance, arg1, arg2) => (TResult)invoker.Invoke(instance, new object[] { arg1, arg2 }));
        }
        public static FastInvoker<TClass, T1, T2, T3, TResult> CreateInvoker<TClass, T1, T2, T3, TResult>(string name) {
            var classType = typeof(TClass);
            var resultType = typeof(TResult);
            var args = new Type[] { typeof(T1), typeof(T2), typeof(T3) };
            var invoker = CreateInvoker(classType, name, resultType, args);
            return new FastInvoker<TClass, T1, T2, T3, TResult>((instance, arg1, arg2, arg3) => (TResult)invoker.Invoke(instance, new object[] { arg1, arg2, arg3 }));
        }
        private class StaticFastInvokeHandler {
            private readonly Type classType;
            private readonly FastInvokeHandler invoker;

            public StaticFastInvokeHandler(Type classType, FastInvokeHandler invoker) {
                this.classType = classType;
                this.invoker = invoker;
            }

            public object Invoke(params object[] args) => invoker.Invoke(classType, args);
        }
        public static FastStaticInvoker CreateStateInvoker(Type classType, string name, Type resultType, params Type[] parameters) {
            var methodInfo = AccessTools.Method(classType, name, parameters);
            if (methodInfo == null) {
                var argString = string.Join(", ", parameters.Select(t => t.ToString()));
                throw new Exception($"{classType} does not contain method {name} with arguments {argString}");
            }
            if (!resultType.IsAssignableFrom(methodInfo.ReturnType)) {
                throw new Exception($"Cannot cast return type {resultType} as {methodInfo.ReturnType} for class {classType} method {name}");
            }
            var _parameters = methodInfo.GetParameters();
            if (_parameters.Length != parameters.Length) {
                throw new Exception($"Expected {parameters.Length} paramters for class {classType} method {name}");
            }
            for (var i = 0; i < parameters.Length; i++) {
                if (!parameters[i].IsAssignableFrom(_parameters[i].ParameterType)) {
                    throw new Exception($"Cannot cast paramter type {parameters[i]} as {_parameters[i].ParameterType} for class {classType} method {name}");
                }
            }
            return new StaticFastInvokeHandler(classType, MethodInvoker.GetHandler(methodInfo)).Invoke;
        }
    }
}