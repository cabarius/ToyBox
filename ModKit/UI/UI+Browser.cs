using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using ModKit.Utility;
using ModKit;
using UnityEngine;
//using static Kingmaker.Blueprints.Classes.StatsDistributionPreset;

namespace ModKit {

    public static partial class UI {

        public static class Browser<Item, Definition> {
            // Simple browser that displays a searchable collection of items along with a collection of available definitions.
            // It provides a toggle to show the definitions mixed in with the items. 
            // By default it shows just the title but you can provide an optional RowUI to show more complex row UI including buttons to add and remove items and so forth. This will be layed out after the title
            // You an also provide UI that renders children below the row

            private static IEnumerable<Definition> _filteredDefinitions;
            private static readonly Dictionary<string, bool> DisclosureStates = new();
            private static string _prevCallerKey = "";
            private static string _searchText = "";
            private static int _searchLimit = 100;
            private static int _matchCount;
            private static bool _showAll;

            public static void OnGUI(
                string callerKey,
                ref bool changed,
                IEnumerable<Item> current,
                Func<IEnumerable<Definition>> available,    // Func because available may be slow
                Func<Item, Definition> definition,
                Func<Definition, string> identifier,
                Func<Definition, string> title,
                Func<Definition, string> searchKey = null,
                Func<Definition, string> sortKey = null,
                Action OnHeaderGUI = null,
                Action<Item, Definition> OnRowGUI = null,
                Action<Item, Definition> OnChildrenGUI = null,
                int indent = 50,
                bool showDiv = true,
                bool search = true,
                float titleMinWidth = 100,
                float titleMaxWidth = 300
                ) {
                if (searchKey == null) searchKey = title;
                if (sortKey == null) sortKey = title;
                var searchChanged = false;
                //            var refreshTree = false;
                if (callerKey != _prevCallerKey) {
                    searchChanged = true;
                    _showAll = false;
                    DisclosureStates.Clear();
                }

                _prevCallerKey = callerKey;
                if (search) {
                    using (HorizontalScope()) {
                        indent.space();
                        ActionTextField(ref _searchText, "searchText", null, () => { searchChanged = true; }, width(320));
                        25.space();
                        Label("Limit", ExpandWidth(false));
                        ActionIntTextField(ref _searchLimit, "searchLimit", null, () => { searchChanged = true; }, width(175));
                        if (_searchLimit > 1000) { _searchLimit = 1000; }

                        25.space();
                        searchChanged |= DisclosureToggle("Show All".Orange().Bold(), ref _showAll);
                    }

                    using (HorizontalScope()) {
                        space(indent);
                        ActionButton("Search", () => { searchChanged = true; }, AutoWidth());
                        space(25);
                        if (_matchCount > 0 && _searchText.Length > 0) {
                            var matchesText = "Matches: ".Green().Bold() + $"{_matchCount}".Orange().Bold();
                            if (_matchCount > _searchLimit) { matchesText += " => ".Cyan() + $"{_searchLimit}".Cyan().Bold(); }

                            Label(matchesText, ExpandWidth(false));
                        }
                    }
                }
                var currentDict = current.ToDictionary(definition, c => c);
                List<Definition> definitions;
                if (_showAll && search) {
                    UpdateSearchResults(_searchText, available(), searchKey, sortKey);
                    definitions = _filteredDefinitions?.ToList();
                } else {
                    definitions = currentDict.Keys.ToList();
                }

                var terms = _searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

                var sorted = definitions?.OrderBy(title);
                _matchCount = 0;
                if (showDiv)
                    Div(indent);
                if (OnHeaderGUI != null) {
                    using (HorizontalScope(AutoWidth())) {
                        space(indent);
                        OnHeaderGUI();
                    }
                }
                foreach (var def in sorted) {
                    var name = title(def);
                    var nameLower = name.ToLower();
                    if (name is not { Length: > 0 } ||
                        (_searchText.Length != 0 && !terms.All(term => nameLower.Contains(term)))) {
                        continue;
                    }

                    currentDict.TryGetValue(def, out var item);
                    _matchCount++;
                    var remainingWidth = ummWidth;
                    bool showChildren = false;
                    using (HorizontalScope(AutoWidth())) {
                        space(indent);
                        remainingWidth -= indent;
                        var titleWidth = (remainingWidth / (IsWide ? 3.0f : 4.0f)) - 100;
                        var text = title(def);
                        var titleKey = $"{callerKey}-{text}";
                        if (item != null) {
                            text = text.Cyan().Bold();
                        }
                        if (OnChildrenGUI == null) {
                            Label(text, width((int)titleWidth));
                            // remwidth -= titlewidth;
                        } else {
                            DisclosureStates.TryGetValue(titleKey, out showChildren);
                            if (DisclosureToggle(text, ref showChildren, titleWidth)) {
                                DisclosureStates[titleKey] = showChildren;
                            }
                        }
                        var lastRect = GUILayoutUtility.GetLastRect();
                        remainingWidth -= lastRect.width;
                        10.space();
                        if (OnRowGUI != null)
                            OnRowGUI(item, def);
                    }
                    if (showChildren) {
                        OnChildrenGUI(item, def);
                    }
                }
            }
            [UsedImplicitly]
            public static void UpdateSearchResults(string searchTextParam,
                IEnumerable<Definition> definitions,
                Func<Definition, string> searchKey,
                Func<Definition, string> sortKey
                ) {
                if (definitions == null) {
                    return;
                }

                var terms = searchTextParam.Split(' ').Select(s => s.ToLower()).ToHashSet();
                var filtered = new List<Definition>();
                foreach (var def in definitions) {
                    if (def.GetType().ToString().Contains(searchTextParam)
                       ) {
                        filtered.Add(def);
                    } else {
                        var name = searchKey(def).ToLower();
                        if (terms.All(term => name.Matches(term))) {
                            filtered.Add(def);
                        }
                    }
                }

                _matchCount = filtered.Count;
                _filteredDefinitions = filtered.OrderBy(sortKey).Take(_searchLimit).ToArray();
            }

        }
    }
}