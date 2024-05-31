using Kingmaker.Blueprints;
using ModKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using static ModKit.Utility.ReflectionCache;

namespace ModKit.DataViewer {
    public enum NodeType {
        Root,
        Component,
        Item,
        Field,
        Property
    }

    // This structure has evolved into a reflection graph or DAG but for the sake of continuity we will stick with calling it a tree
    public class ReflectionTree : ReflectionTree<object> {
        public ReflectionTree(object root) : base(root) { }
    }

    public class ReflectionTree<TRoot> {
        private RootNode<TRoot>? _root;

        public TRoot Root => _root!.Value;

        public Node? RootNode => _root;

        public ReflectionTree(TRoot root) => SetRoot(root);

        public void SetRoot(TRoot root) {
            if (_root != null)
                _root.SetValue(root);
            else
                _root = new RootNode<TRoot>("<Root>", root);
        }
    }

    public abstract class Node {
        // this allows us to avoid duplicated nodes for the same value
        //public static ConditionalWeakTable<object, Node> ValueToNodeLookup = new ConditionalWeakTable<object, Node>();

        protected const BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        public readonly NodeType NodeType;
        public readonly Type Type;
        public readonly bool IsNullable;
        protected Node(Type type, NodeType nodeType) {
            NodeType = nodeType;
            Type = type;
            IsNullable = Type.IsGenericType && !Type.IsGenericTypeDefinition && Type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        //[ObsoleteAttribute("TODO - move this into a proper view model", false)]
        public ToggleState Expanded { get; set; }

        //[ObsoleteAttribute("TODO - move this into a proper view model", false)]
        public bool Matches { get; set; }

        public string NodeTypePrefix {
            get {
                switch (NodeType) {
                    case NodeType.Component:
                        return "c";
                    case NodeType.Item:
                        return "i";
                    case NodeType.Field:
                        return "f";
                    case NodeType.Property:
                        return "p";
                    default:
                        return string.Empty;
                }
            }
        }

        public int ExpandedNodeCount {
            get {
                var count = 1;
                if (IsBaseType) return count;
                if (Expanded == ToggleState.On) {
                    foreach (var child in GetItemNodes()) count += child.ExpandedNodeCount;
                    foreach (var child in GetComponentNodes()) count += child.ExpandedNodeCount;
                    foreach (var child in GetPropertyNodes()) count += child.ExpandedNodeCount;
                    foreach (var child in GetFieldNodes()) count += child.ExpandedNodeCount;
                }
                return Count;
            }
        }

        public int ChildrenCount {
            get {
                if (IsBaseType) return 0;
                return GetItemNodes().Count + GetComponentNodes().Count + GetFieldNodes().Count + GetPropertyNodes().Count;
            }
        }

        public bool hasChildren {
            get {
                if (IsBaseType) return false;
                return ChildrenCount > 0;
            }
        }

        public string? Name { get; protected set; }
        public abstract string ValueText { get; }
        public abstract Type InstType { get; }
        public abstract bool IsBaseType { get; }
        public abstract bool IsEnumerable { get; }
        public abstract int EnumerableCount { get; }
        public abstract bool IsException { get; }
        public abstract bool IsGameObject { get; }
        public abstract bool IsNull { get; }
        public abstract int? InstanceID { get; }
        public static IEnumerable<FieldInfo> GetFields(Type type) {
            var names = new HashSet<string>();
            foreach (var field in (Nullable.GetUnderlyingType(type) ?? type).GetFields(AllFlags))
                if (!field.IsStatic
                    && !field.IsDefined(typeof(CompilerGeneratedAttribute), false)
                    && // ignore backing field
                    names.Add(field.Name))
                    yield return field;
        }
        public static IEnumerable<PropertyInfo> GetProperties(Type type) {
            var names = new HashSet<string>();
            foreach (var property in (Nullable.GetUnderlyingType(type) ?? type).GetProperties(AllFlags))
                if (property.GetMethod != null && !property.GetMethod.IsStatic && property.GetMethod.GetParameters().Length == 0 && names.Add(property.Name))
                    yield return property;
        }
        public abstract IReadOnlyCollection<Node> GetItemNodes();
        public abstract IReadOnlyCollection<Node> GetComponentNodes();
        public abstract IReadOnlyCollection<Node> GetPropertyNodes();
        public abstract IReadOnlyCollection<Node> GetFieldNodes();
        public abstract Node GetParent();
        private void AppendPathFromRoot(StringBuilder sb) {
            var parent = GetParent();
            if (parent != null) {
                parent.AppendPathFromRoot(sb);
                sb.Append(".");
            }
            sb.Append(Name);
        }
        public string GetPath() {
            var sb = new StringBuilder();
            AppendPathFromRoot(sb);
            return sb.ToString();
        }
        public abstract void SetDirty();
        public abstract bool IsDirty();
        internal abstract void SetValue(object value);
        protected abstract void UpdateValue();
    }

    internal abstract class GenericNode<TNode> : Node {
        // the graph will not show any child nodes of following types
        private static readonly HashSet<Type> BaseTypes = new() {
            typeof(object),
            typeof(DBNull),
            typeof(bool),
            typeof(char),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            //typeof(DateTime),
            typeof(string),
            typeof(IntPtr),
            typeof(UIntPtr)
        };

        private Type? _instType;
        private bool? _isBaseType;
        private bool? _isEnumerable;
        private bool? _isGameObject;

        private TNode? _value;
        private int _enumerableCount = -1;
        private bool _valueIsDirty = true;

        private List<Node>? _componentNodes;
        private List<Node>? _itemNodes;
        private List<Node>? _fieldNodes;
        private List<Node>? _propertyNodes;
        private bool _componentIsDirty;
        private bool _itemIsDirty;
        private bool _fieldIsDirty;
        private bool _propertyIsDirty;
        protected GenericNode(NodeType nodeType) : base(typeof(TNode), nodeType) {
            if (Type.IsValueType && !IsNullable) _instType = Type;
        }

        public TNode Value {
            get {
                UpdateValue();
                return _value;
            }
            protected set {
                if (!value?.Equals(_value) ?? _value != null) {
                    _value = value;
                    if (value is BlueprintReferenceBase { Cached: null } bpRefBase)
                        bpRefBase.GetBlueprint();
                    _enumerableCount = -1;
                    if (!Type.IsValueType || IsNullable) {
                        var oldType = _instType;
                        _instType = value?.GetType();
                        if (_instType != oldType) {
                            _isBaseType = null;
                            _isEnumerable = null;
                            _isGameObject = null;
                            _fieldIsDirty = true;
                            _propertyIsDirty = true;
                        }
                    }
                }
            }
        }

        public override string ValueText => IsException
                                                ? "<exception>"
                                                : IsNull
                                                    ? "<null>"
                                                    : Value.ToString();

        public override Type InstType {
            get {
                UpdateValue();
                return _instType;
            }
        }

        public override bool IsBaseType {
            get {
                UpdateValue();
                return _isBaseType ?? (_isBaseType = BaseTypes.Contains(Nullable.GetUnderlyingType(InstType ?? Type) ?? InstType ?? Type)).Value;
            }
        }

        public override bool IsEnumerable {
            get {
                UpdateValue();
                return _isEnumerable ?? (_isEnumerable = (InstType ?? Type).GetInterfaces().Contains(typeof(IEnumerable))).Value;
            }
        }

        public override int EnumerableCount {
            get {
                UpdateValue();
                return _enumerableCount;
            }
        }

        public override bool IsGameObject {
            get {
                UpdateValue();
                return _isGameObject ?? (_isGameObject = typeof(GameObject).IsAssignableFrom(InstType ?? Type)).Value;
            }
        }

        public override int? InstanceID {
            get {
                int? result = null;
                if (Value is UnityEngine.Object unityObject) result = unityObject?.GetInstanceID();
                if (Value is object obj) return obj.GetHashCode();
                return result;
            }
        }

        public override bool IsNull => Value == null || (Value is UnityEngine.Object unityObject && !unityObject);
        public override IReadOnlyCollection<Node> GetComponentNodes() {
            UpdateComponentNodes();
            return _componentNodes.AsReadOnly();
        }
        public override IReadOnlyCollection<Node> GetItemNodes() {
            UpdateItemNodes();
            return _itemNodes.AsReadOnly();
        }
        public override IReadOnlyCollection<Node> GetFieldNodes() {
            UpdateFieldNodes();
            return _fieldNodes.AsReadOnly();
        }
        public override IReadOnlyCollection<Node> GetPropertyNodes() {
            UpdatePropertyNodes();
            return _propertyNodes.AsReadOnly();
        }
        public override Node GetParent() => null;
        public override void SetDirty() => _valueIsDirty = true;
        public override bool IsDirty() => _valueIsDirty;
        private Node FindOrCreateChildForValue(object item, Type type, params object[] childArgs) {
            Node node = null;
            //if (item != null)
            //    ValueToNodeLookup.TryGetValue(item, out node);
            if (node == null) node = Activator.CreateInstance(type, AllFlags, null, childArgs, null) as Node;
            //if (item != null) 
            //    ValueToNodeLookup.Add(item, node);
            return node;
        }
        private void UpdateComponentNodes() {
            UpdateValue();
            if (!_componentIsDirty && _componentNodes != null) return;

            _componentIsDirty = false;

            if (_componentNodes == null) _componentNodes = new List<Node>();
            _componentNodes.Clear();
            if (IsException || IsNull || !IsGameObject) return;

            var nodeType = typeof(ComponentNode);
            var i = 0;
            foreach (var item in (Value as GameObject).GetComponents<Component>()) {
                _componentNodes.Add(FindOrCreateChildForValue(item, nodeType, this, "<component_" + i + ">", item));
                i++;
            }
            _enumerableCount = i;
        }
        private void UpdateItemNodes() {
            UpdateValue();

            if (!_itemIsDirty && _itemNodes != null) return;

            _itemIsDirty = false;

            if (_itemNodes == null) _itemNodes = new List<Node>();

            _itemNodes.Clear();
            if (IsException || IsNull || !IsEnumerable) return;

            var itemTypes = InstType.GetInterfaces()
                                    .Where(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(item => item.GetGenericArguments()[0]);
            var itemType = itemTypes.Count() == 1 ? itemTypes.First() : typeof(object);
            if (itemType == typeof(BlueprintReferenceBase)
                || itemType.IsSubclassOf(typeof(BlueprintReferenceBase)))
                itemType = typeof(BlueprintScriptableObject);
            var nodeType = typeof(ItemNode<>).MakeGenericType(itemType);
            var i = 0;
            foreach (var item in Value as IEnumerable) {
                var resolvedItem = item;
                if (item is BlueprintReferenceBase bpRefBase) resolvedItem = bpRefBase.GetBlueprint();
                _itemNodes.Add(FindOrCreateChildForValue(resolvedItem, nodeType, this, "<item_" + i + ">", resolvedItem));
                i++;
            }
            _enumerableCount = i;
        }
        private void UpdateFieldNodes() {
            UpdateValue();

            if (!_fieldIsDirty && _fieldNodes != null) return;

            _fieldIsDirty = false;

            if (_fieldNodes == null)
                _fieldNodes = new List<Node>();
            _fieldNodes.Clear();
            if (IsException || IsNull) return;

            var nodeType = InstType.IsValueType ? !IsNullable ? typeof(FieldOfStructNode<,,>) : typeof(FieldOfNullableNode<,,>) : typeof(FieldOfClassNode<,,>);

            _fieldNodes = GetFields(InstType).Select(child => FindOrCreateChildForValue(child, nodeType.MakeGenericType(Type, InstType, child.FieldType), this, child.Name)).ToList();
            //_fieldNodes = GetFields(InstType).Select(child => Activator.CreateInstance(
            //    nodeType.MakeGenericType(Type, InstType, child.FieldType),
            //    ALL_FLAGS, null, new object[] { this, child.Name }, null) as Node).ToList();

            _fieldNodes.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
        private void UpdatePropertyNodes() {
            UpdateValue();

            if (!_propertyIsDirty && _propertyNodes != null) return;

            _propertyIsDirty = false;

            if (_propertyNodes == null)
                _propertyNodes = new List<Node>();
            _propertyNodes.Clear();
            if (IsException || IsNull) return;
            var nodeType = InstType.IsValueType ? !IsNullable ? typeof(PropertyOfStructNode<,,>) : typeof(PropertyOfNullableNode<,,>) : typeof(PropertyOfClassNode<,,>);

            _propertyNodes = GetProperties(InstType).Select(child => FindOrCreateChildForValue(child,
                                                                                               nodeType.MakeGenericType(Type, InstType, child.PropertyType),
                                                                                               this,
                                                                                               child.Name)).ToList();
            // TODO: generalize this and implement custom data extractors
            if (Value is BlueprintReferenceBase bpRefBase) {
                var customNode = new CustomNode<SimpleBlueprint>("Cached", bpRefBase.GetBlueprint(), NodeType.Property);
                _propertyNodes.Add(customNode);
            }
            _propertyNodes.Sort((x, y) => x.Name.CompareTo(y.Name));
        }
        protected override void UpdateValue() {
            if (_valueIsDirty) {
                _valueIsDirty = false;

                _componentIsDirty = true;
                _itemIsDirty = true;

                if (_fieldNodes != null)
                    foreach (var child in _fieldNodes)
                        child.SetDirty();

                if (_propertyNodes != null)
                    foreach (var child in _propertyNodes)
                        child.SetDirty();
                UpdateValueImpl();
            }
        }
        protected abstract void UpdateValueImpl();
    }

    internal abstract class PassiveNode<TNode> : GenericNode<TNode> {
        public override bool IsException => false;

        public PassiveNode(string? name, TNode value, NodeType nodeType) : base(nodeType) {
            Name = name;
            Value = value;
        }
        internal override void SetValue(object value) {
            SetDirty();
            Value = (TNode)value;
        }
        internal void SetValue(TNode value) {
            SetDirty();
            Value = value;
        }
        protected override void UpdateValueImpl() { }
    }

    internal class RootNode<TNode> : PassiveNode<TNode> {
        public RootNode(string? name, TNode value) : base(name, value, NodeType.Root) { }
    }

    // This is a node that was created by some custom data extraction mechanism
    internal class CustomNode<TNode> : PassiveNode<TNode> {
        public CustomNode(string? name, TNode value, NodeType nodeType) : base(name, value, nodeType) { }
    }

    internal class ComponentNode : PassiveNode<Component> {
        protected readonly WeakReference<Node> _parentNode;
        protected ComponentNode(Node parentNode, string? name, Component value) : base(name, value, NodeType.Component) => _parentNode = new WeakReference<Node>(parentNode);
        public override Node GetParent() {
            if (_parentNode.TryGetTarget(out var parent)) return parent;
            return null;
        }
    }

    internal class ItemNode<TNode> : PassiveNode<TNode> {
        protected readonly WeakReference<Node> _parentNode;
        protected ItemNode(Node parentNode, string? name, TNode value) : base(name, value, NodeType.Item) => _parentNode = new WeakReference<Node>(parentNode);
        public override Node GetParent() {
            if (_parentNode.TryGetTarget(out var parent)) return parent;
            return null;
        }
    }

    internal abstract class ChildNode<TParent, TNode> : GenericNode<TNode> {
        protected bool _isException;
        protected readonly WeakReference<GenericNode<TParent>> _parentNode;

        public override bool IsException {
            get {
                UpdateValue();
                return _isException;
            }
        }

        protected ChildNode(GenericNode<TParent> parentNode, string? name, NodeType nodeType) : base(nodeType) {
            _parentNode = new WeakReference<GenericNode<TParent>>(parentNode);
            Name = name;
        }
        internal override void SetValue(object value) => throw new NotImplementedException();
        public override Node GetParent() {
            if (_parentNode.TryGetTarget(out var parent)) return parent;
            return null;
        }
    }

    internal abstract class ChildOfStructNode<TParent, TParentInst, TNode> : ChildNode<TParent, TNode>
        where TParentInst : struct {
        private readonly Func<TParent, TParentInst> _forceCast = UnsafeForceCast.GetDelegate<TParent, TParentInst>();
        protected ChildOfStructNode(GenericNode<TParent> parentNode, string? name, NodeType nodeType) : base(parentNode, name, nodeType) { }
        protected bool TryGetParentValue(out TParentInst value) {
            if (_parentNode.TryGetTarget(out var parent) && parent.InstType == typeof(TParentInst)) {
                value = _forceCast(parent.Value);
                return true;
            }
            value = default;
            return false;
        }
    }

    internal abstract class ChildOfNullableNode<TParent, TUnderlying, TNode> : ChildNode<TParent, TNode>
        where TUnderlying : struct {
        private readonly Func<TParent, TUnderlying?> _forceCast = UnsafeForceCast.GetDelegate<TParent, TUnderlying?>();
        protected ChildOfNullableNode(GenericNode<TParent> parentNode, string? name, NodeType nodeType) : base(parentNode, name, nodeType) { }
        protected bool TryGetParentValue(out TUnderlying value) {
            if (_parentNode.TryGetTarget(out var parent)) {
                var parentValue = _forceCast(parent.Value);
                if (parentValue.HasValue) {
                    value = parentValue.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }

    internal abstract class ChildOfClassNode<TParent, TParentInst, TNode> : ChildNode<TParent, TNode>
        where TParentInst : class {
        protected ChildOfClassNode(GenericNode<TParent> parentNode, string? name, NodeType nodeType) : base(parentNode, name, nodeType) { }
        protected bool TryGetParentValue(out TParentInst value) {
            if (_parentNode.TryGetTarget(out var parent) && (value = parent.Value as TParentInst) != null) return true;
            value = null;
            return false;
        }
    }

    internal class FieldOfStructNode<TParent, TParentInst, TNode> : ChildOfStructNode<TParent, TParentInst, TNode>
        where TParentInst : struct {
        protected FieldOfStructNode(GenericNode<TParent> parentNode, string? name) : base(parentNode, name, NodeType.Field) { }
        protected override void UpdateValueImpl() {
            if (TryGetParentValue(out var parentValue)) {
                _isException = false;
                Value = parentValue.GetFieldValue<TParentInst, TNode>(Name);
            } else {
                _isException = true;
                Value = default;
            }
        }
    }

    internal class PropertyOfStructNode<TParent, TParentInst, TNode> : ChildOfStructNode<TParent, TParentInst, TNode>
        where TParentInst : struct {
        protected PropertyOfStructNode(GenericNode<TParent> parentNode, string? name) : base(parentNode, name, NodeType.Property) { }
        protected override void UpdateValueImpl() {
            if (TryGetParentValue(out var parentValue))
                try {
                    _isException = false;
                    Value = parentValue.GetPropertyValue<TParentInst, TNode>(Name);
                } catch {
                    _isException = true;
                    Value = default;
                }
            else {
                _isException = true;
                Value = default;
            }
        }
    }

    internal class FieldOfNullableNode<TParent, TUnderlying, TNode> : ChildOfNullableNode<TParent, TUnderlying, TNode>
        where TUnderlying : struct {
        protected FieldOfNullableNode(GenericNode<TParent> parentNode, string? name) : base(parentNode, name, NodeType.Field) { }
        protected override void UpdateValueImpl() {
            if (TryGetParentValue(out var parentValue)) {
                _isException = false;
                Value = parentValue.GetFieldValue<TUnderlying, TNode>(Name);
            } else {
                _isException = true;
                Value = default;
            }
        }
    }

    internal class PropertyOfNullableNode<TParent, TUnderlying, TNode> : ChildOfNullableNode<TParent, TUnderlying, TNode>
        where TUnderlying : struct {
        protected PropertyOfNullableNode(GenericNode<TParent> parentNode, string? name) : base(parentNode, name, NodeType.Property) { }
        protected override void UpdateValueImpl() {
            if (TryGetParentValue(out var parentValue))
                try {
                    _isException = false;
                    Value = parentValue.GetPropertyValue<TUnderlying, TNode>(Name);
                } catch {
                    _isException = true;
                    Value = default;
                }
            else {
                _isException = true;
                Value = default;
            }
        }
    }

    internal class FieldOfClassNode<TParent, TParentInst, TNode> : ChildOfClassNode<TParent, TParentInst, TNode>
        where TParentInst : class {
        protected FieldOfClassNode(GenericNode<TParent> parentNode, string? name) : base(parentNode, name, NodeType.Field) { }
        protected override void UpdateValueImpl() {
            if (TryGetParentValue(out var parentValue)) {
                _isException = false;
                Value = parentValue.GetFieldValue<TParentInst, TNode>(Name);
            } else {
                _isException = true;
                Value = default;
            }
        }
    }

    internal class PropertyOfClassNode<TParent, TParentInst, TNode> : ChildOfClassNode<TParent, TParentInst, TNode>
        where TParentInst : class {
        protected PropertyOfClassNode(GenericNode<TParent> parentNode, string? name) : base(parentNode, name, NodeType.Property) { }
        protected override void UpdateValueImpl() {
            if (TryGetParentValue(out var parentValue))
                try {
                    _isException = false;
                    Value = parentValue.GetPropertyValue<TParentInst, TNode>(Name);
                } catch {
                    _isException = true;
                    Value = default;
                }
            else {
                _isException = true;
                Value = default;
            }
        }
    }
}