using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModKit.DataViewer {
    public abstract class ResultNode {
        public virtual string Name { get; }
        public virtual Type Type { get; }
        public virtual string NodeTypePrefix { get;  }
        public virtual string ValueText { get; }
    }
    public class ResultNode<TNode> : ResultNode where TNode : class {
        protected const BindingFlags ALL_FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public delegate bool TraversalCallback(ResultNode<TNode> node, int depth);
        public TNode Node;
        public List<ResultNode<TNode>> children = new List<ResultNode<TNode>>();
        public HashSet<TNode> matches { get { return children.Select(c => c.Node).ToHashSet(); } }
        public ToggleState ToggleState = ToggleState.Off;
        public ToggleState ShowSiblings = ToggleState.Off;
        public bool isMatch;    // flag indicating that this node is an actual matching node and not a parent of a matching node
        public int Count;
        public void Traverse(TraversalCallback callback, int depth = 0) {
            if (callback(this, depth)) {
                foreach (var child in children) {
                    child.Traverse(callback, depth + 1);
                }
            }
        }
        public ResultNode<TNode> FindChild(TNode node) {
            return children.Find(rn => rn.Node == node);
        }
        public ResultNode<TNode> FindOrAddChild(TNode node) {
            var rnode = children.Find(rn => rn.Node == node);
            if (rnode == null) {
                rnode = Activator.CreateInstance(this.GetType(), ALL_FLAGS, null, null, null) as ResultNode<TNode>;
                rnode.Node = node;
                children.Add(rnode);
            }
            return rnode;
        }
        public void AddSearchResult(IEnumerable<TNode> path) {
            var rnode = this;
            Count++;
            foreach (var node in path) {
                rnode = rnode.FindOrAddChild(node);
                rnode.Count++;
            }
        }
        public void Clear() {
            Node = null;
            Count = 0;
            children.Clear();
        }
        private StringBuilder BuildString(StringBuilder builder, int depth) {
            builder.Append($"{NodeTypePrefix} {Name}:{Type.ToString()} - {ValueText}\n".Indent(depth));
            foreach (var child in children) {
                builder = child.BuildString(builder, depth + 1);
            }
            return builder;
        }
        override public string ToString() {
            return BuildString(new StringBuilder().Append("\n"), 0).ToString();
        }
    }
    public class ReflectionSearchResult : ResultNode<Node> {
        public override string Name { get { return Node.Name; } }
        public override Type Type {  get { return Node.Type; } }
        public override string NodeTypePrefix { get { return Node.NodeTypePrefix; } }
        public override string ValueText { get { return Node.ValueText; } }

        public void AddSearchResult(Node node) {
            if (node == null) return;
            var path = new List<Node>();
            for (var n = node; node != null && node != this.Node; node = node.GetParent()) {
                path.Add(node);
            }
            AddSearchResult(path.Reverse<Node>());
        }
    }
}

