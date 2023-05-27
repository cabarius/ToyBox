using JetBrains.Annotations;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ModKit {

    public static partial class UI {
        public class Browser {

            private static readonly Dictionary<object, object> ShowDetails = new();
            public static void ClearDetails() => ShowDetails.Clear();
            public static bool DetailToggle(string title, object key, object target = null, int width = 600) {
                var changed = false;
                if (target == null) target = key;
                var expanded = ShowDetails.ContainsKey(key);
                if (DisclosureToggle(title, ref expanded, width)) {
                    changed = true;
                    ShowDetails.Clear();
                    if (expanded) {
                        ShowDetails[key] = target;
                    }
                }
                return changed;
            }
            public static bool OnDetailGUI(object key, Action<object> onDetailGUI) {

                ShowDetails.TryGetValue(key, out var target);
                if (target != null) {
                    onDetailGUI(target);
                    return true;
                }
                else {
                    return false;
                }
            }
        }
        public class Browser<Definition, Item> : Browser {
            // Simple browser that displays a searchable collection of items along with a collection of available definitions.
            // It provides a toggle to show the definitions mixed in with the items. 
            // By default it shows just the title but you can provide an optional RowUI to show more complex row UI including buttons to add and remove items and so forth. This will be layed out after the title
            // You an also provide UI that renders children below the row
            public ModKitSettings Settings => Mod.ModKitSettings;
            private IEnumerable<Definition> _pagedResults = new List<Definition>();
            private Queue<Definition> cachedSearchResults;
            public List<Definition> filteredDefinitions;
            private Dictionary<Definition, Item> _currentDict;

            private CancellationTokenSource _cancellationTokenSource;
            private string _searchText = "";
            public string SearchText => _searchText;
            public bool SearchAsYouType;
            public bool ShowAll;
            public bool IsDetailBrowser;

            public int SearchLimit {
                get => IsDetailBrowser ? Settings.browserDetailSearchLimit : Settings.browserSearchLimit;
                set {
                    var oldValue = SearchLimit;
                    if (IsDetailBrowser)
                        Settings.browserDetailSearchLimit = value;
                    else
                        Settings.browserSearchLimit = value;
                    if (value != oldValue) ModKitSettings.Save();
                }
            }
            private int _pageCount;
            private int _matchCount;
            private int _currentPage = 1;
            private bool _searchQueryChanged = true;
            public void ResetSearch() {
                _searchQueryChanged = true;
                ReloadData();
            }
            public bool needsReloadData = true;
            public void ReloadData() => needsReloadData = true;
            private bool _updatePages = false;
            private bool _finishedSearch = false;
            public bool isSearching = false;
            public bool startedLoadingAvailable = false;
            public bool availableIsStatic { get; private set; }
            private List<Definition> _availableCache;
            public void OnShowGUI() => needsReloadData = true;
            public Browser(bool searchAsYouType = true, bool availableIsStatic = false, bool isDetailBrowser = false) {
                SearchAsYouType = searchAsYouType;
                this.availableIsStatic = availableIsStatic;
                IsDetailBrowser = isDetailBrowser;
                Mod.NotifyOnShowGUI += OnShowGUI;
            }

            public void OnGUI(
                IEnumerable<Item> current,
                Func<IEnumerable<Definition>> available,    // Func because available may be slow
                Func<Item, Definition> definition,
                Func<Definition, string> searchKey,
                Func<Definition, IComparable[]> sortKeys,
                Action onHeaderGUI = null,
                Action<Definition, Item> onRowGUI = null,
                Action<Definition, Item> onDetailGUI = null,
                int indent = 50,
                bool showDiv = true,
                bool search = true,
                float titleMinWidth = 100,
                float titleMaxWidth = 300,
                string searchTextPassedFromParent = "",
                bool showItemDiv = false
                ) {
                current ??= new List<Item>();
                List<Definition> definitions = Update(current, available, search, searchKey, sortKeys, definition);
                if (search || SearchLimit < _matchCount) {
                    if (search) {
                        using (HorizontalScope()) {
                            indent.space();
                            ActionTextField(ref _searchText, "searchText", (text) => {
                                if (!SearchAsYouType) return;
                                needsReloadData = true;
                                _searchQueryChanged = true;
                            }, () => { needsReloadData = true; }, MinWidth(320), AutoWidth());
                            25.space();
                            Label("Limit".localize(), ExpandWidth(false));
                            var searchLimit = SearchLimit;
                            ActionIntTextField(ref searchLimit, "Search Limit", (i) => { _updatePages = true; }, () => { _updatePages = true; }, width(175));
                            if (searchLimit > 1000) { searchLimit = 1000; }
                            SearchLimit = searchLimit;
                            25.space();
                            if (DisclosureToggle("Show All".localize().Orange().Bold(), ref ShowAll)) {
                                startedLoadingAvailable |= ShowAll;
                                ResetSearch();
                            }
                            25.space();
                            //                            if (isSearching && false) { // ADDB - Please add a delay timer before this appears because having it flash on very short searches is distracting or let's just get rid of it
                            //                                                        // It was helpful for debugging but I don't think we need it anymore?
                            //                                Label("Searching...", AutoWidth());
                            //                                25.space();
                            //                            }
                        }
                    }
                    else {
                        if (_searchText != searchTextPassedFromParent) {
                            needsReloadData = true;
                            _searchText = searchTextPassedFromParent;
                            if (_searchText == null) {
                                _searchText = "";
                            }
                        }
                    }
                    using (HorizontalScope()) {
                        if (search) {
                            space(indent);
                            ActionButton("Search".localize(), () => { needsReloadData = true; }, AutoWidth());
                        }
                        space(25);
                        if (_matchCount > 0 || _searchText.Length > 0) {
                            var matchesText = "Matches: ".localize().Green().Bold() + $"{_matchCount}".Orange().Bold();
                            if (_matchCount > SearchLimit) { matchesText += " => ".Cyan() + $"{SearchLimit}".Cyan().Bold(); }

                            Label(matchesText, ExpandWidth(false));
                        }
                        if (_matchCount > SearchLimit) {
                            string pageLabel = "Page: ".localize().orange() + _currentPage.ToString().cyan() + " / " + _pageCount.ToString().cyan();
                            25.space();
                            Label(pageLabel, ExpandWidth(false));
                            ActionButton("-", () => {
                                if (_currentPage >= 1) {
                                    if (_currentPage == 1) {
                                        _currentPage = _pageCount;
                                    }
                                    else {
                                        _currentPage -= 1;
                                    }
                                    _updatePages = true;
                                }
                            }, AutoWidth());
                            ActionButton("+", () => {
                                if (_currentPage > _pageCount) return;
                                if (_currentPage == _pageCount) {
                                    _currentPage = 1;
                                }
                                else {
                                    _currentPage += 1;
                                }
                                _updatePages = true;
                            }, AutoWidth());
                        }
                    }
                }
                if (showDiv)
                    Div(indent);
                if (onHeaderGUI != null) {
                    using (HorizontalScope(AutoWidth())) {
                        space(indent);
                        onHeaderGUI();
                    }
                }
                foreach (var def in definitions) {
                    if (showItemDiv) {
                        Div(indent);
                    }
                    _currentDict.TryGetValue(def, out var item);
                    if (onRowGUI != null) {
                        using (HorizontalScope(AutoWidth())) {
                            space(indent);
                            onRowGUI(def, item);
                        }
                    }
                    onDetailGUI?.Invoke(def, item);
                }
            }

            private List<Definition> Update(
                IEnumerable<Item> current,
                Func<IEnumerable<Definition>> available,
                bool search,
                Func<Definition, string> searchKey,
                Func<Definition, IComparable[]> sortKeys,
                Func<Item, Definition> definition
                ) {
                if (Event.current.type == EventType.Layout) {
                    if (startedLoadingAvailable) {
                        _availableCache = available()?.ToList();
                        if (_availableCache?.Count() > 0) {
                            startedLoadingAvailable = false;
                            needsReloadData = true;
                            if (!availableIsStatic) {
                                _availableCache = null;
                            }
                        }
                    }
                    if (_finishedSearch || isSearching) {
                        bool nothingToSearch = (!ShowAll && current.Count() == 0) || (ShowAll && (availableIsStatic ? _availableCache : available()).Count() == 0);
                        // If the search has at least one result
                        if ((cachedSearchResults.Count > 0 || nothingToSearch) && (_searchQueryChanged || _finishedSearch)) {
                            if (_finishedSearch && !_searchQueryChanged) {
                                filteredDefinitions = new List<Definition>();
                            }
                            // Lock the search results
                            lock (cachedSearchResults) {
                                // Go through every item in the queue
                                while (cachedSearchResults.Count > 0) {
                                    // Add the item into the OrderedSet filteredDefinitions
                                    filteredDefinitions.Add(cachedSearchResults.Dequeue());
                                }
                            }
                            filteredDefinitions.Sort(Comparer<Definition>.Create((x, y) => {
                                var xKeys = sortKeys(x);
                                var yKeys = sortKeys(y);
                                var zipped = xKeys.Zip(yKeys, (x, y) => (x: x, y: y));
                                foreach (var pair in zipped) {
                                    var compare = pair.x.CompareTo(pair.y);
                                    if (compare != 0) return compare;
                                }
                                return xKeys.Length > yKeys.Length ? -1 : 1;
                            }));
                        }
                        _matchCount = filteredDefinitions.Count;
                        UpdatePageCount();
                        UpdatePaginatedResults();
                        if (_finishedSearch) {
                            isSearching = false;
                            _updatePages = false;
                            _finishedSearch = false;
                            _searchQueryChanged = false;
                            cachedSearchResults = null;
                        }
                    }
                    if (needsReloadData) {
                        _currentDict = current.ToDictionaryIgnoringDuplicates(definition, c => c);
                        IEnumerable<Definition> definitions;
                        if (ShowAll) {
                            if (startedLoadingAvailable) {
                                definitions = _currentDict.Keys.ToList();
                            }
                            else if (availableIsStatic) {
                                definitions = _availableCache;
                            }
                            else {
                                definitions = available();
                            }
                        }
                        else {
                            definitions = _currentDict.Keys.ToList();
                        }
                        if (!isSearching) {
                            _cancellationTokenSource = new();
                            Task.Run(() => UpdateSearchResults(_searchText, definitions, searchKey, sortKeys, search));
                            if (_searchQueryChanged) {
                                filteredDefinitions = new List<Definition>();
                            }
                            isSearching = true;
                            needsReloadData = false;
                        }
                        else {
                            _cancellationTokenSource.Cancel();
                        }
                    }
                    if (_updatePages) {
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
                Func<Definition, IComparable[]> sortKey,
                bool search
                ) {
                if (definitions == null) {
                    return;
                }
                cachedSearchResults = new();
                var terms = searchTextParam.Split(' ').Select(s => s.ToLower()).ToHashSet();
                if (search) {
                    foreach (var def in definitions) {
                        if (_cancellationTokenSource.IsCancellationRequested) {
                            isSearching = false;
                            return;
                        }
                        if (def.GetType().ToString().Contains(searchTextParam)
                           ) {
                            lock (cachedSearchResults) {
                                cachedSearchResults.Enqueue(def);
                            }
                        }
                        else if (searchKey != null) {
                            var text = searchKey(def).ToLower();
                            if (terms.All(term => text.Matches(term))) {
                                lock (cachedSearchResults) {
                                    cachedSearchResults.Enqueue(def);
                                }
                            }
                        }
                    }
                }
                else {
                    lock (cachedSearchResults) {
                        cachedSearchResults = new Queue<Definition>(definitions);
                    }
                }
                _finishedSearch = true;
            }
            public void UpdatePageCount() {
                if (SearchLimit > 0) {
                    _pageCount = (int)Math.Ceiling((double)_matchCount / SearchLimit);
                    _currentPage = Math.Min(_currentPage, _pageCount);
                    _currentPage = Math.Max(1, _currentPage);
                }
                else {
                    _pageCount = 1;
                    _currentPage = 1;
                }
            }
            public void UpdatePaginatedResults() {
                var limit = SearchLimit;
                var count = _matchCount;
                var offset = Math.Min(count, (_currentPage - 1) * limit);
                limit = Math.Min(limit, Math.Max(count, count - limit));
                Mod.Trace($"{_currentPage} / {_pageCount} count: {count} => offset: {offset} limit: {limit} ");
                _pagedResults = filteredDefinitions.Skip(offset).Take(limit).ToArray();
            }
        }
    }
}