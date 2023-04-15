using JetBrains.Annotations;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
//using ToyBox;
using UnityEngine;
//using static Kingmaker.Blueprints.Classes.StatsDistributionPreset;

namespace ModKit {

    public static partial class UI {

        public class Browser<Item, Definition> {
            // Simple browser that displays a searchable collection of items along with a collection of available definitions.
            // It provides a toggle to show the definitions mixed in with the items. 
            // By default it shows just the title but you can provide an optional RowUI to show more complex row UI including buttons to add and remove items and so forth. This will be layed out after the title
            // You an also provide UI that renders children below the row

            private IEnumerable<Definition> _pagedResults = new List<Definition>();
            private IEnumerable<Definition> _filteredOrderedDefinitions;
            private Dictionary<Definition, Item> _currentDict;
            private readonly Dictionary<string, bool> DisclosureStates = new();
            private string _searchText = "";
            private int _searchLimit = 100;
            private int _pageCount;
            private int _matchCount;
            private int _currentPage = 1;
            private DateTime lockedUntil;
            public bool searchChanged = true;
            public bool SearchAsYouType = true;
            private bool _showAll;
            private bool _updatePages = false;
            private bool _startedLoading = false;
            private bool _availableIsStatic;
            private IEnumerable<Definition> _availableCache;
            public void OnShowGUI() {
                searchChanged = true;
            }
            public Browser(bool searchAsYouType = true, bool availableIsStatic = false) {
                SearchAsYouType = searchAsYouType;
                _availableIsStatic = availableIsStatic;
                Mod.NotifyOnShowGUI += OnShowGUI;
            }

            public void OnGUI(
                string callerKey,
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
                float titleMaxWidth = 300,
                string searchTextPassedFromParent = "",
                bool showItemDiv = false
                ) {
                if (searchKey == null) searchKey = title;
                if (sortKey == null) sortKey = title;
                if (current == null) current = new List<Item>();
                List<Definition> definitions = update(current, available, title, search, searchKey, sortKey, definition);
                if (search || _searchLimit < _matchCount) {
                    if (search) {
                        using (HorizontalScope()) {
                            indent.space();
                            ActionTextField(ref _searchText, "searchText", (text) => {
                                if (SearchAsYouType) {
                                    lockedUntil = DateTime.Now.AddMilliseconds(250);
                                    searchChanged = true;
                                }
                            }, () => { searchChanged = true; }, width(320));
                            25.space();
                            Label("Limit", ExpandWidth(false));
                            ActionIntTextField(ref _searchLimit, "searchLimit", (i) => { _updatePages = true; }, () => { _updatePages = true; }, width(175));
                            if (_searchLimit > 1000) { _searchLimit = 1000; }
                            25.space();
                            _startedLoading |= DisclosureToggle("Show All".Orange().Bold(), ref _showAll);
                            25.space();
                        }
                    } else {
                        if (_searchText != searchTextPassedFromParent) {
                            searchChanged = true;
                            _searchText = searchTextPassedFromParent;
                            if (_searchText == null) {
                                _searchText = "";
                            }
                        }
                    }
                    using (HorizontalScope()) {
                        if (search) {
                            space(indent);
                            ActionButton("Search", () => { searchChanged = true; }, AutoWidth());
                        }
                        space(25);
                        if (_matchCount > 0 || _searchText.Length > 0) {
                            var matchesText = "Matches: ".Green().Bold() + $"{_matchCount}".Orange().Bold();
                            if (_matchCount > _searchLimit) { matchesText += " => ".Cyan() + $"{_searchLimit}".Cyan().Bold(); }

                            Label(matchesText, ExpandWidth(false));
                        }
                        if (_matchCount > _searchLimit) {
                            string pageLabel = "Page: ".orange() + _currentPage.ToString().cyan() + " / " + _pageCount.ToString().cyan();
                            25.space();
                            Label(pageLabel, ExpandWidth(false));
                            ActionButton("-", () => {
                                if (_currentPage > 1) {
                                    _currentPage -= 1;
                                    _updatePages = true;
                                }
                            }, AutoWidth());
                            ActionButton("+", () => {
                                if (_currentPage < _pageCount) {
                                    _currentPage += 1;
                                    _updatePages = true;
                                }
                            }, AutoWidth());
                        }
                    }
                }
                if (showDiv)
                    Div(indent);
                if (OnHeaderGUI != null) {
                    using (HorizontalScope(AutoWidth())) {
                        space(indent);
                        OnHeaderGUI();
                    }
                }
                foreach (var def in definitions) {
                    if (showItemDiv) {
                        Div(indent);
                    }
                    var name = title(def);
                    var nameLower = name.ToLower();
                    _currentDict.TryGetValue(def, out var item);
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

            private List<Definition> update(IEnumerable<Item> current, Func<IEnumerable<Definition>> available, Func<Definition, string> title, bool search,
                Func<Definition, string> searchKey, Func<Definition, string> sortKey, Func<Item, Definition> definition) {
                if (Event.current.type == EventType.Layout) {
                    if (_startedLoading) {
                        _availableCache = available();
                        if (_availableCache?.Count() > 0) {
                            _startedLoading = false;
                            searchChanged = true;
                            if (!_availableIsStatic) {
                                _availableCache = null;
                            }
                        }
                    }
                    if (searchChanged) {
                        if (DateTime.Now.CompareTo(lockedUntil) >= 0) {
                            _currentDict = current.ToDictionaryIgnoringDuplicates(definition, c => c);
                            var defs = (_showAll) ? ((_availableIsStatic) ? _availableCache : available()) : _currentDict.Keys.ToList();
                            UpdateSearchResults(_searchText, defs, searchKey, sortKey, title, search);
                            searchChanged = false;
                        }
                    } else if (_updatePages) {
                        _updatePages = false;
                        UpdatePageCount();
                        UpdatePaginatedResults();
                    }
                }
                return _pagedResults?.ToList();
            }

            [UsedImplicitly]
            public void UpdateSearchResults(string searchTextParam,
                IEnumerable<Definition> definitions,
                Func<Definition, string> searchKey,
                Func<Definition, string> sortKey,
                Func<Definition, string> title,
                bool search
                ) {
                if (definitions == null) {
                    return;
                }
                var filtered = new List<Definition>();
                var terms = searchTextParam.Split(' ').Select(s => s.ToLower()).ToHashSet();

                if (search) {
                    foreach (var def in definitions) {
                        var name = title(def);
                        var nameLower = name.ToLower();
                        // Should items without name still be supported?
                        if (name is not { Length: > 0 }) {
                            continue;
                        }
                        if (def.GetType().ToString().Contains(searchTextParam)
                           ) {
                            filtered.Add(def);
                        } else if (searchKey != null) {
                            var name2 = searchKey(def).ToLower();
                            if (terms.All(term => name2.Matches(term))) {
                                filtered.Add(def);
                            }
                        }
                    }
                } else {
                    filtered = definitions.ToList();
                }
                _matchCount = filtered.Count;
                _filteredOrderedDefinitions = filtered.OrderBy(sortKey);
                UpdatePageCount();
                UpdatePaginatedResults();
                _updatePages = false;
            }
            public void UpdatePageCount() {
                if (_searchLimit > 0) {
                    _pageCount = (int)Math.Ceiling((double)_matchCount / _searchLimit);
                    _currentPage = Math.Min(_currentPage, _pageCount);
                    _currentPage = Math.Max(1, _currentPage);
                } else {
                    _pageCount = 1;
                    _currentPage = 1;
                }
            }
            public void UpdatePaginatedResults() {
                var limit = _searchLimit;
                var count = _matchCount;
                var offset = Math.Min(count, (_currentPage - 1) * limit);
                limit = Math.Min(limit, Math.Max(count, count - limit));
                Mod.Trace($"{_currentPage} / {_pageCount} count: {count} => offset: {offset} limit: {limit} ");
                _pagedResults = _filteredOrderedDefinitions.Skip(offset).Take(limit).ToArray();
            }
        }
    }
}