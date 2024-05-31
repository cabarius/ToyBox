using JetBrains.Annotations;
using ModKit.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToyBox;
using UnityEngine;

namespace ModKit {

    public static partial class UI {
        public class Browser {
            public enum sortDirection {
                Ascending = 1,
                Descending = -1
            };

            private static readonly Dictionary<object, object> ShowDetails = new();
            public static void ClearDetails() => ShowDetails.Clear();
            public static bool DetailToggle(string? title, object key, object? target = null, int width = 600) {
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
                } else {
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
            public List<Definition> filteredDefinitions = new();
            public List<Definition> tempFilteredDefinitions;
            public Dictionary<string, List<Definition>> collatedDefinitions = new();
            private Dictionary<Definition, Item> _currentDict;
            public string prevCollationKey;
            public string collationKey;
            private CancellationTokenSource _searchCancellationTokenSource;
            private CancellationTokenSource _collationCancellationTokenSource;
            private string _searchText = "";
            public string SearchText => _searchText;
            public sortDirection SortDirection = sortDirection.Ascending;
            public bool doCollation = false;
            private bool _collationKeyIsNullOrAllOrDoesNotExist => collationKey == null || collationKey?.ToLower() == "all" || (!collatedDefinitions?.ContainsKey(collationKey) ?? true);
            public bool SearchAsYouType;
            public bool ShowAll;
            public bool DisplayShowAllGUI = true;
            public bool IsDetailBrowser;
            public bool _doCopyToEnd = false;
            public bool _finishedCopyToEnd = false;

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
            public bool searchQueryChanged = true;
            public void ResetSearch() {
                searchQueryChanged = true;
                ReloadData();
            }
            public bool needsReloadData = true;
            internal bool _needsRedoCollation = true;
            internal bool _collationFinished = false;
            public void RedoCollation() {
                _needsRedoCollation = true;
                _collationFinished = false;
                _searchCancellationTokenSource?.Cancel();
                _collationCancellationTokenSource?.Cancel();
                ResetSearch();
            }
            public void ReloadData() => needsReloadData = true;
            private bool _updatePages = false;
            private bool _finishedSearch = false;
            public bool isCollating = false;
            public bool isSearching = false;
            public bool startedLoadingAvailable = false;
            public bool availableIsStatic { get; private set; }
            public bool useCustomNotRowGUI;
            private List<Definition> _availableCache;
            public void OnShowGUI() => RedoCollation();
            public Browser(bool searchAsYouType, bool availableIsStatic = false, bool isDetailBrowser = false, bool useCustomNotRowGUI = false) {
                SearchAsYouType = searchAsYouType;
                this.availableIsStatic = availableIsStatic;
                IsDetailBrowser = isDetailBrowser;
                this.useCustomNotRowGUI = useCustomNotRowGUI;
                Mod.NotifyOnShowGUI += OnShowGUI;
            }

            public void OnGUI(
                IEnumerable<Item> current,
                Func<IEnumerable<Definition>> available,    // Func because available may be slow
                Func<Item, Definition> definition,
                Func<Definition, string?> searchKey,
                Func<Definition, IComparable?[]> sortKeys,
                Action? onHeaderGUI = null,
                Action<Definition, Item>? onRowGUI = null,
                Action<Definition, Item>? onDetailGUI = null,
                int indent = 50,
                bool showDiv = true,
                bool search = true,
                float titleMinWidth = 100,
                float titleMaxWidth = 300,
                string searchTextPassedFromParent = "",
                bool showItemDiv = false,
                Func<Definition, IEnumerable<string>> collator = null,
                Action<List<Definition>, Dictionary<Definition, Item>> customNotRowGUI = null
                ) {
                current ??= new List<Item>();
                if (collationKey != prevCollationKey) {
                    prevCollationKey = collationKey;
                    ResetSearch();
                }
                List<Definition> definitions = Update(current, available, search, searchKey, sortKeys, definition, collator);
                if (search || SearchLimit < _matchCount) {
                    if (search) {
                        using (HorizontalScope()) {
                            indent.space();
                            ActionTextField(ref _searchText, "searchText", (text) => {
                                if (!SearchAsYouType) return;
                                needsReloadData = true;
                                searchQueryChanged = true;
                            }, () => { needsReloadData = true; }, MinWidth(320), AutoWidth());
                            25.space();
                            Label("Limit".localize(), ExpandWidth(false));
                            var searchLimit = SearchLimit;
                            ActionIntTextField(ref searchLimit, "Search Limit", (i) => { _updatePages = true; }, () => { _updatePages = true; }, width(175));
                            if (searchLimit > 1000) { searchLimit = 1000; }
                            SearchLimit = searchLimit;
                            25.space();
                            if (DisplayShowAllGUI) {
                                if (DisclosureToggle("Show All".localize().Orange().Bold(), ref ShowAll)) {
                                    startedLoadingAvailable |= ShowAll;
                                    RedoCollation();
                                }
                                25.space();
                            }
                            if (isCollating) {
                                Label("Collating...".localize().cyan().bold(), AutoWidth());
                                25.space();
                            } else if (_doCopyToEnd) {
                                Label("Copying...".localize().cyan().bold(), AutoWidth());
                                25.space();
                            }
                        }
                    } else {
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
                            string? matchesText = "Matches: ".localize().Green().Bold() + $"{_matchCount}".Orange().Bold();
                            if (_matchCount > SearchLimit) { matchesText += " => ".Cyan() + $"{SearchLimit}".Cyan().Bold(); }

                            Label(matchesText, ExpandWidth(false));
                        }
                        if (_matchCount > SearchLimit) {
                            string? pageLabel = "Page: ".localize().orange() + _currentPage.ToString().cyan() + " / " + _pageCount.ToString().cyan();
                            25.space();
                            Label(pageLabel, ExpandWidth(false));
                            ActionButton("-", () => {
                                if (_currentPage >= 1) {
                                    if (_currentPage == 1) {
                                        _currentPage = _pageCount;
                                    } else {
                                        _currentPage -= 1;
                                    }
                                    _updatePages = true;
                                }
                            }, AutoWidth());
                            ActionButton("+", () => {
                                if (_currentPage > _pageCount) return;
                                if (_currentPage == _pageCount) {
                                    _currentPage = 1;
                                } else {
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
                if (useCustomNotRowGUI) {
                    customNotRowGUI(definitions, _currentDict);
                } else {
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
            }

            private List<Definition> Update(
                IEnumerable<Item> current,
                Func<IEnumerable<Definition>> available,
                bool search,
                Func<Definition, string?> searchKey,
                Func<Definition, IComparable?[]> sortKeys,
                Func<Item, Definition> definition,
                Func<Definition, IEnumerable<string>> collator
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
                        if ((cachedSearchResults.Count > 0 || nothingToSearch) && (searchQueryChanged || _finishedSearch)) {
                            Comparer<Definition> comparer = Comparer<Definition>.Create((x, y) => {
                                var xKeys = sortKeys(x);
                                var yKeys = sortKeys(y);
                                var zipped = xKeys.Zip(yKeys, (x, y) => (x: x, y: y));
                                foreach (var pair in zipped) {
                                    var compare = pair.x.CompareTo(pair.y);
                                    if (compare != 0) return (int)SortDirection * compare;
                                }
                                return (int)SortDirection * (xKeys.Length > yKeys.Length ? -1 : 1);
                            });
                            if (_finishedSearch && !searchQueryChanged) {
                                filteredDefinitions = new List<Definition>();
                            }
                            if (_doCopyToEnd && _finishedCopyToEnd) {
                                _doCopyToEnd = false;
                                _finishedCopyToEnd = false;
                                cachedSearchResults.Clear();
                                filteredDefinitions = tempFilteredDefinitions;
                            } else {
                                // If the search already finished we want to copy all results as fast as possible
                                if (_finishedSearch && cachedSearchResults.Count < 1000) {
                                    filteredDefinitions.AddRange(cachedSearchResults);
                                    cachedSearchResults.Clear();
                                    filteredDefinitions.Sort(comparer);
                                } // If it's too much then even the above approach will take up to ~10 seconds on decent setups
                                else if (_finishedSearch && !_doCopyToEnd) {
                                    _doCopyToEnd = true;
                                    _finishedCopyToEnd = false;
                                    Task.Run(() => CopyToEnd(filteredDefinitions, cachedSearchResults, comparer));
                                } // If it's not finished then we shouldn't have too many results anyway
                                else if (!_doCopyToEnd) {
                                    // Lock the search results
                                    lock (cachedSearchResults) {
                                        // Go through every item in the queue
                                        while (cachedSearchResults.Count > 0) {
                                            // Add the item into the OrderedSet filteredDefinitions
                                            filteredDefinitions.Add(cachedSearchResults.Dequeue());
                                        }
                                    }
                                    filteredDefinitions.Sort(comparer);
                                }
                            }
                        }
                        _matchCount = filteredDefinitions.Count;
                        UpdatePageCount();
                        UpdatePaginatedResults();
                        if (_finishedSearch && cachedSearchResults?.Count == 0) {
                            isSearching = false;
                            _updatePages = false;
                            _finishedSearch = false;
                            searchQueryChanged = false;
                            cachedSearchResults = null;
                        }
                    }
                    if (_needsRedoCollation && doCollation && collator != null) {
                        _collationFinished = false;
                        _currentDict = current.ToDictionaryIgnoringDuplicates(definition, c => c);
                        IEnumerable<Definition> definitions;
                        if (ShowAll) {
                            if (startedLoadingAvailable) {
                                definitions = _currentDict.Keys.ToList();
                            } else if (availableIsStatic) {
                                definitions = _availableCache;
                            } else {
                                definitions = available();
                            }
                        } else {
                            definitions = _currentDict.Keys.ToList();
                        }
                        if (!isCollating) {
                            _collationCancellationTokenSource = new();
                            _searchCancellationTokenSource?.Cancel();
                            isCollating = true;
                            _needsRedoCollation = false;
                            Task.Run(() => Collate(definitions, collator, sortKeys));
                        } else {
                            _collationCancellationTokenSource.Cancel();
                        }
                    }
                    if (needsReloadData && !isCollating && ((doCollation && _collationFinished) || !doCollation || collator != null)) {
                        _currentDict = current.ToDictionaryIgnoringDuplicates(definition, c => c);
                        IEnumerable<Definition> definitions;
                        if (doCollation && !_collationKeyIsNullOrAllOrDoesNotExist) {
                            definitions = collatedDefinitions[collationKey];
                        } else {
                            if (ShowAll) {
                                if (startedLoadingAvailable) {
                                    definitions = _currentDict.Keys.ToList();
                                } else if (availableIsStatic) {
                                    definitions = _availableCache;
                                } else {
                                    definitions = available();
                                }
                            } else {
                                definitions = _currentDict.Keys.ToList();
                            }
                        }
                        if (!isSearching) {
                            _searchCancellationTokenSource = new();
                            Task.Run(() => UpdateSearchResults(_searchText, definitions, searchKey, search));
                            if (searchQueryChanged) {
                                filteredDefinitions = new List<Definition>();
                            }
                            isSearching = true;
                            needsReloadData = false;
                        } else {
                            _searchCancellationTokenSource.Cancel();
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

            public void CopyToEnd(List<Definition> filteredDefinitions, Queue<Definition> cachedSearchResults, Comparer<Definition> comparer) {
                tempFilteredDefinitions = filteredDefinitions.Concat(cachedSearchResults).ToList();
                if ((_collationCancellationTokenSource?.IsCancellationRequested ?? false) || (_searchCancellationTokenSource?.IsCancellationRequested ?? false)) {
                    tempFilteredDefinitions.Clear();
                }
                tempFilteredDefinitions.Sort(comparer);
                _finishedCopyToEnd = true;
            }

            public void UpdateSearchResults(string searchTextParam,
                IEnumerable<Definition> definitions,
                Func<Definition, string>? searchKey,
                bool search
                ) {
                if (definitions == null) {
                    return;
                }
                cachedSearchResults = new();
                var terms = searchTextParam.Split(' ').Select(s => s.ToLower()).ToHashSet();
                if (search && !string.IsNullOrEmpty(searchTextParam)) {
                    foreach (var def in definitions) {
                        if (_searchCancellationTokenSource.IsCancellationRequested) {
                            isSearching = false;
                            return;
                        }
                        if (def.GetType().ToString().Contains(searchTextParam)
                           ) {
                            lock (cachedSearchResults) {
                                cachedSearchResults.Enqueue(def);
                            }
                        } else if (searchKey != null) {
                            var text = searchKey(def).ToLower();
                            if (terms.All(term => text.Matches(term))) {
                                lock (cachedSearchResults) {
                                    cachedSearchResults.Enqueue(def);
                                }
                            }
                        }
                    }
                } else {
                    lock (cachedSearchResults) {
                        cachedSearchResults = new Queue<Definition>(definitions);
                    }
                }
                _finishedSearch = true;
            }
            public void Collate(IEnumerable<Definition> definitions,
                Func<Definition, IEnumerable<string>> collator,
                Func<Definition, IComparable[]> sortKeys) {
                collatedDefinitions = new();
                foreach (var definition in definitions) {
                    if (_collationCancellationTokenSource.IsCancellationRequested) {
                        isCollating = false;
                        return;
                    }
                    foreach (var key in collator(definition)) {
                        var group = collatedDefinitions.GetValueOrDefault(key, new());
                        group.Add(definition);
                        collatedDefinitions[key] = group;
                    }
                }
                foreach (var group in collatedDefinitions.Values) {
                    group.Sort(Comparer<Definition>.Create((x, y) => {
                        var xKeys = sortKeys(x);
                        var yKeys = sortKeys(y);
                        var zipped = xKeys.Zip(yKeys, (x, y) => (x: x, y: y));
                        foreach (var pair in zipped) {
                            var compare = pair.x.CompareTo(pair.y);
                            if (compare != 0) return (int)SortDirection * compare;
                        }
                        return (int)SortDirection * (xKeys.Length > yKeys.Length ? -1 : 1);
                    }));
                }
                _collationFinished = true;
                isCollating = false;
            }
            public void UpdatePageCount() {
                if (SearchLimit > 0) {
                    _pageCount = (int)Math.Ceiling((double)_matchCount / SearchLimit);
                    _currentPage = Math.Min(_currentPage, _pageCount);
                    _currentPage = Math.Max(1, _currentPage);
                } else {
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