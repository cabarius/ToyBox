using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static ModKit.UI;
using static ModKit.Utility.StringExtensions;
using Object = System.Object;

namespace ModKit.DataViewer {
    public class ReflectionTreeView {
#if false
        private static readonly ConditionalWeakTable<object, ReflectionTreeView> _cachedReflectionTrees = new() { };
        private static void AddOrUpdateCachedReflectionTree(object key, ReflectionTreeView tree) {
            if (_cachedReflectionTrees.TryGetValue(key, out _)) {
                _cachedReflectionTrees.Remove(key);
                //Mod.Log($"removing: {bp.NameSafe()}");
            }
            _cachedReflectionTrees.Add(key, tree);
        }
#endif
        private static readonly Dictionary<object, ReflectionTreeView> ExpandedObjects = new();
        public static void ClearExpanded() => ExpandedObjects.Clear();
        public static void DetailToggle(string title, object key, object target = null, int width = 600) {
            target ??= key;
            var expanded = ExpandedObjects.ContainsKey(key);
            if (DisclosureToggle(title, ref expanded, width)) {
                ExpandedObjects.Clear();
                if (expanded) {
                    ExpandedObjects[key] = new ReflectionTreeView(target);
                }
            }
        }
        public static bool OnDetailGUI(object key, int indent = 50) {
            ExpandedObjects.TryGetValue(key, out var reflectionTreeView);
            if (reflectionTreeView != null) {
                reflectionTreeView.Indent = indent;
                using (HorizontalScope()) {
                    Space(indent);
                    Label("Inspecting: ".grey() + reflectionTreeView.Root.ToString().orange().bold());
                }
                reflectionTreeView.OnGUI(false);
                return true;
            } else {
                return false;
            }
        }


        private ReflectionTree _tree;
        private ReflectionSearchResult _searchResults = new ReflectionSearchResult();
        private float _height;
        private bool _mouseOver;
        private GUIStyle _buttonStyle;
        private GUIStyle _valueStyle;
        private int _totalNodeCount;
        private int _nodesCount;
        private int _startIndex;
        private int _skipLevels;
        private string _searchText = "";

        internal string[] SearchTerms => _searchText.Length == 0
                                             ? Array.Empty<string>()
                                             : _searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        private bool enableCopy = false;
        private int visitCount = 0;
        private int searchDepth = 0;
        private int searchBreadth = 0;
        private void updateCounts(int visitCount, int depth, int breadth) {
            this.visitCount = visitCount;
            this.searchDepth = depth;
            this.searchBreadth = breadth;
        }

        private Rect _viewerRect;

        public int Indent { get; set; } = 0;
        public float DepthDelta { get; set; } = 30f;

        public int MaxRows => 1000; // { get { return Main.settings.maxRows; } }

        public object Root => _tree.Root;

        public float TitleMinWidth { get; set; } = 300f;

        public ReflectionTreeView() { }
        public ReflectionTreeView(object root) {
            SetRoot(root);
        }

        public void Clear() {
            _tree = null;
            lock (_searchResults) {
                _searchResults.Clear();
            }
        }

        public void SetRoot(object root) {
            if (_tree != null)
                _tree.SetRoot(root);
            else
                _tree = new ReflectionTree(root);
            lock (_searchResults) {
                _searchResults.Node = null;
            }
            _tree.RootNode.Expanded = ToggleState.On;
            //            ReflectionSearch.Shared.StartSearch(_tree.RootNode, searchText, updateCounts, _searchResults);
        }

        public void OnGUI(bool drawRoot = true, bool collapse = false) {
            if (_tree == null)
                return;
            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button)
                    { alignment = TextAnchor.MiddleLeft, stretchHeight = false };
            if (_valueStyle == null)
                _valueStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, stretchHeight = false };
            if (Event.current.type == EventType.Layout) {
                var count = ReflectionSearch.ApplyUpdates();
                if (count > 0) Mod.Log($"ReflectionTreeView.OnGUI - {count} Search Updates Applied");
            }

            int startIndexUBound = Math.Max(0, _nodesCount - MaxRows);

            // mouse wheel & fix scroll position
            if (Event.current.type == EventType.Layout) {
                _totalNodeCount = _tree.RootNode.ChildrenCount;
                if (startIndexUBound > 0) {
                    if (_mouseOver) {
                        var delta = Input.mouseScrollDelta;
                        if (delta.y > 0 && _startIndex > 0)
                            _startIndex--;
                        else if (delta.y < 0 && _startIndex < startIndexUBound)
                            _startIndex++;
                    }
                    if (_startIndex > startIndexUBound) {
                        _startIndex = startIndexUBound;
                    }
                }
                else {
                    _startIndex = 0;
                }
            }
            using (new GUILayout.VerticalScope()) {
                // tool-bar
                using (HorizontalScope()) {
                    Space(Indent);
                    ActionButton("Refresh", () => _tree.RootNode.SetDirty());
                    15.space();
                    ActionTextField(ref _searchText,
                                    "searhText",
                                    (text) => { },
                                    () => {
                                        _searchText = _searchText.Trim();
                                        ReflectionSearch.Shared.StartSearch(_tree.RootNode,
                                                                            SearchTerms,
                                                                            updateCounts,
                                                                            _searchResults);
                                    },
                                    Width(250));
                    GUILayout.Space(10f);
                    bool isSearching = ReflectionSearch.Shared.isSearching;
                    ActionButton(isSearching ? "Stop" : "Search",
                                 () => {
                                     if (isSearching) {
                                         ReflectionSearch.Shared.Stop();
                                     }
                                     else {
                                         _searchText = _searchText.Trim();
                                         ReflectionSearch.Shared.StartSearch(_tree.RootNode,
                                                                             SearchTerms,
                                                                             updateCounts,
                                                                             _searchResults);
                                     }
                                 },
                                 AutoWidth());
                    10.space();
                    if (ValueAdjuster("Max Depth:", ref ReflectionSearch.maxSearchDepth)) {
                        ReflectionSearch.Shared.StartSearch(_tree.RootNode, SearchTerms, updateCounts, _searchResults);
                    }
                    10.space();
                    if (visitCount > 0) {
                        Label($"found {_searchResults.Count}".Cyan()
                              + $" visited: {visitCount} (d: {searchDepth} b: {searchBreadth})".Orange());
                    }
                    GUILayout.FlexibleSpace();
                    //                  10.space();
                    //                    Toggle("Enable Value Selection For Copy", ref enableCopy);
                }
#if false
                using (new GUILayout.HorizontalScope()) {
                    if (GUILayout.Button("Collapse", GUILayout.ExpandWidth(false))) {
                        collapse = true;
                        _skipLevels = 0;
                    }
                    if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false)))
                        _tree.RootNode.SetDirty();
                    GUILayout.Space(10f);
                    //GUIHelper.AdjusterButton(ref _skipLevels, "Skip Levels:", 0);
                    //GUILayout.Space(10f);
                    UI.ValueAdjuster("Max Rows:", ref Main.settings.maxRows);
                    GUILayout.Space(10f);
#if false
                    GUILayout.Label("Title Width:", GUILayout.ExpandWidth(false));
                    TitleMinWidth =
 GUILayout.HorizontalSlider(TitleMinWidth, 0f, Screen.width / 2, GUILayout.Width(100f));

                    GUILayout.Space(10f);
#endif
                    GUILayout.Label($"Scroll: {_startIndex} / {_totalNodeCount}", GUILayout.ExpandWidth(false));
                    GUILayout.Space(10f);
                    UI.ActionTextField(ref searchText, "searhText", (text) => { }, () => {
                        searchText = searchText.Trim();
                        ReflectionSearch.Shared.StartSearch(_tree.RootNode, searchText, updateCounts, _searchResults);
                    }, UI.Width(250));
                    GUILayout.Space(10f);
                    bool isSearching = ReflectionSearch.Shared.isSearching;
                    UI.ActionButton(isSearching ? "Stop" : "Search", () => {
                        if (isSearching) {
                            ReflectionSearch.Shared.Stop();
                        }
                        else {
                            searchText = searchText.Trim();
                            ReflectionSearch.Shared.StartSearch(_tree.RootNode, searchText, updateCounts, _searchResults);
                        }
                    }, UI.AutoWidth());
                    GUILayout.Space(10f);
                    if (UI.ValueAdjuster("Max Depth:", ref Main.settings.maxSearchDepth)) {
                        ReflectionSearch.Shared.StartSearch(_tree.RootNode, searchText, updateCounts, _searchResults);
                    }
                    GUILayout.Space(10f);
                    if (visitCount > 0) {
                        GUILayout.Label($"found {_searchResults.Count}".Cyan() + $" visited: {visitCount} (d: {searchDepth} b: {searchBreadth})".Orange());
                    }
                    GUILayout.FlexibleSpace();
                }
#endif
                // view
                using (VerticalScope()) {
                    using (HorizontalScope()) {
                        Space(Indent);
                        using (VerticalScope()) {
                            //                    using (new GUILayout.ScrollViewScope(new Vector2(), GUIStyle.none, GUIStyle.none, GUILayout.Height(_height))) {
                            using (HorizontalScope(GUI.skin.box)) {
                                // nodes
                                using (new GUILayout.VerticalScope()) {
                                    _nodesCount = 0;
                                    if (SearchTerms.Length > 0) {
                                        Div();
                                        lock (_searchResults) {
                                            _searchResults.Traverse((node, depth) => {
                                                if (node.Node == null) return true;
                                                var toggleState = node.ToggleState;
                                                if (!node.Node.hasChildren)
                                                    toggleState = ToggleState.None;
                                                else if (node.ToggleState == ToggleState.None)
                                                    toggleState = ToggleState.Off;
                                                if (node.Node.NodeType == NodeType.Root) {
                                                    if (node.matches.Count == 0) return false;
                                                    Label("Search Results".Cyan().Bold());
                                                }
                                                else
                                                    DrawNodePrivate(node.Node, depth, ref toggleState);
                                                if (node.ToggleState != toggleState) {
                                                    Mod.Log(node.ToString());
                                                }
                                                node.ToggleState = toggleState;
                                                if (toggleState.IsOn()) {
                                                    DrawChildren(node.Node, depth + 1, collapse);
                                                }
                                                return true; // toggleState == ToggleState.On;
                                            }, 0);
                                        }
                                        Div();
                                    }
                                    if (drawRoot)
                                        DrawNode(_tree.RootNode, 0, collapse);
                                    else
                                        DrawChildren(_tree.RootNode, 0, collapse);
                                }

                                // scrollbar
                                //                            if (startIndexUBound > 0)
                                //                            _startIndex = (int)GUILayout.VerticalScrollbar(_startIndex, MaxRows, 0f, Math.Max(MaxRows, _totalNodeCount), GUILayout.ExpandHeight(true));
                            }

                            // cache height
                            if (Event.current.type == EventType.Repaint) {
                                var mousePos = Event.current.mousePosition;
                                _mouseOver = _viewerRect.Contains(Event.current.mousePosition);
                                //Main.Log($"mousePos: {mousePos} Rect: {_viewerRect} --> {_mouseOver}");
                                _viewerRect = GUILayoutUtility.GetLastRect();
                                _height = _viewerRect.height + 5f;
                            }
                            //                  }
                        }
                    }
                }
            }
        }
        private void DrawNodePrivate(Node node, int depth, ref ToggleState expanded) {
            _nodesCount++;

            if (_nodesCount > _startIndex && _nodesCount <= _startIndex + MaxRows) {

                using (HorizontalScope()) {
                    // title
                    Space(DepthDelta * (depth - _skipLevels));
                    var name = node.Name;
                    var instText = "";  // if (node.InstanceID is int instID) instText = "@" + instID.ToString();
                    name = name.MarkedSubstring(SearchTerms);
                    var enumerableCount = node.EnumerableCount;
                    if (enumerableCount == 0 || node.IsNull) return; // TODO - make this a config option
                    if (enumerableCount >= 0) name = name + $"[{enumerableCount}]".yellow();
                    var typeName = node.InstType?.Name ?? node.Type?.Name;
                    ToggleButton(ref expanded,
                        $"[{node.NodeTypePrefix}] ".color(RGBA.grey) +
                        name + " : " + typeName.color(
                            node.IsBaseType ? RGBA.grey :
                            node.IsGameObject ? RGBA.magenta :
                            node.IsEnumerable ? RGBA.cyan : RGBA.orange)
                        + instText,
                        _buttonStyle, GUILayout.ExpandWidth(false), GUILayout.MinWidth(TitleMinWidth));

                    // value
                    Color originalColor = GUI.contentColor;
                    GUI.contentColor = node.IsException ? Color.red : node.IsNull ? Color.grey : originalColor;
                    var valueText = node.ValueText;
                    if (SearchTerms.Length == 0 || !SearchTerms.Any(term => valueText.Matches(term)))
                        ClipboardLabel(valueText); // + " " + node.GetPath().green(), _valueStyle);
                    else {
                        //if (valueText.Matches("mor"))
                        //    Mod.Log($"{valueText}/[{string.Join(", ", SearchTerms)}]");
                        Label(valueText.MarkedSubstring(SearchTerms), ExpandWidth(true));
                    }
                    GUI.contentColor = originalColor;

                    // instance type
                    var text = "";
                    var style = GUI.skin.label;
                    if (node.InstType != null && node.InstType != node.Type) {
                        text = node.InstType.Name.color(RGBA.yellow);
                        style = _buttonStyle;
                        Label(text, _buttonStyle, GUILayout.ExpandWidth(false));
                    }
                    else 
                        Label("", ExpandWidth(false));
                }
            }
        }
        private void DrawNode(Node node, int depth, bool collapse) {
            try {
                ToggleState expanded = node.Expanded;
                if (depth >= _skipLevels && !(collapse && depth > 0)) {
                    if (!node.hasChildren)
                        expanded = ToggleState.None;
                    else if (node.Expanded == ToggleState.None)
                        expanded = ToggleState.Off;
                    DrawNodePrivate(node, depth, ref expanded);
                    node.Expanded = expanded;
                }
                if (collapse)
                    node.Expanded = ToggleState.Off;

                // children
                if (expanded.IsOn()) {
                    DrawChildren(node, depth + 1, collapse);
                }
            } catch (Exception e) {
            }
        }

        private void DrawChildren(Node node, int depth, bool collapse, Func<Node, bool> hoist = null) {
            if (node.IsBaseType)
                return;
            if (hoist == null) hoist = (n) => n.Matches;
            var toHoist = new List<Node>();
            var others = new List<Node>();
            var nodesCount = _nodesCount;
            var maxNodeCount = _startIndex + MaxRows * 2;
            foreach (var child in node.GetItemNodes()) {
                if (nodesCount > maxNodeCount) break; nodesCount++;
                if (hoist(child)) toHoist.Add(child); else others.Add(child);
            }
            foreach (var child in node.GetComponentNodes()) {
                if (nodesCount > maxNodeCount) break; nodesCount++;
                if (hoist(child)) toHoist.Add(child); else others.Add(child);
            }
            foreach (var child in node.GetPropertyNodes()) {
                if (nodesCount > maxNodeCount) break; nodesCount++;
                if (hoist(child)) toHoist.Add(child); else others.Add(child);
            }
            foreach (var child in node.GetFieldNodes()) {
                if (nodesCount > maxNodeCount) break; nodesCount++;
                if (hoist(child)) toHoist.Add(child); else others.Add(child);
            }
            foreach (var child in toHoist) { DrawNode(child, depth, collapse); }
            foreach (var child in others) { DrawNode(child, depth, collapse); }
            _totalNodeCount = Math.Max(_nodesCount, _totalNodeCount);
        }
    }
}

#if false
                    using (new GUILayout.HorizontalScope()) {
                        // parent
                        try {
                            GUILayout.Space(DepthDelta * (depth - _skipLevels));
                            var parent = node.GetParent();
                            var parentText = "";
                            while (parent != null) {
                                parentText = parentText + parent.Name + " : " + parent.Type.Name;
                                try {
                                    parent = parent.GetParent();
                                }
                                catch (Exception e) {
                                    parentText += e.ToString();
                                    parent = null;
                                }

                            }
                            GUILayout.Label(parentText);
                        }
                        catch (Exception e) { }
                    }
#endif
