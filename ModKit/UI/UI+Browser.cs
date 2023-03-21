using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using ModKit.Utility;
using ModKit;

namespace ModKit {

    public static partial class UI {
        public static class Browser<T, TItem, TDef> // for many things the item will be the definition
        {
            // ReSharper disable file StaticMemberInGenericType
            private static IEnumerable<TDef> _filteredDefinitions;
            private static readonly Dictionary<string, bool> DisclosureStates = new();
            private static string _prevCallerKey = "";
            private static string _searchText = "";
            private static int _searchLimit = 100;
            private static int _matchCount;
            private static bool _showAll;

            internal static void OnGUI(
                string callerKey, ref bool changed,
                T target,
                IEnumerable<TItem> current,
                IEnumerable<TDef> available,
                Func<TItem, TDef> definition,
                Func<TDef, string> searchAndSortKey,
                Func<TDef, string> title,
                Func<TDef, string> description = null,
                Func<TItem, string> value = null,
                Action<T, TItem, string> setValue = null,
                // The following functors take an target and item and will return an action to perform it if it can, null otherwise
                Func<T, TItem, Action> incrementValue = null,
                Func<T, TItem, Action> decrementValue = null,
                Func<T, TDef, Action> addItem = null,
                Func<T, TItem, Action> removeItem = null,
                // ReSharper disable once UnusedParameter.Global
                Func<T, TItem, Action> childrenOnGUI = null
            ) {
                // ReSharper disable once NotAccessedVariable
                var searchChanged = false;
                //            var refreshTree = false;
                if (callerKey != _prevCallerKey) {
                    searchChanged = true;
                    _showAll = false;
                    DisclosureStates.Clear();
                }

                _prevCallerKey = callerKey;
                using (HorizontalScope()) {
                    100.space();
                    ActionTextField(ref _searchText, "searchText", null, () => { searchChanged = true; }, width(320));
                    25.space();
                    Label("Limit", ExpandWidth(false));
                    ActionIntTextField(ref _searchLimit, "searchLimit", null, () => { searchChanged = true; }, width(175));
                    if (_searchLimit > 1000) { _searchLimit = 1000; }

                    25.space();
                    searchChanged |= DisclosureToggle("Show All".Orange().Bold(), ref _showAll);
                }

                using (HorizontalScope()) {
                    space(100);
                    ActionButton("Search", () => { searchChanged = true; }, AutoWidth());
                    space(25);
                    if (_matchCount > 0 && _searchText.Length > 0) {
                        var matchesText = "Matches: ".Green().Bold() + $"{_matchCount}".Orange().Bold();
                        if (_matchCount > _searchLimit) { matchesText += " => ".Cyan() + $"{_searchLimit}".Cyan().Bold(); }

                        Label(matchesText, ExpandWidth(false));
                    }
                }

                var currentDict = current.ToDictionary(definition, c => c);
                List<TDef> definitions;
                if (_showAll) {
                    UpdateSearchResults(_searchText, available, searchAndSortKey);
                    definitions = _filteredDefinitions?.ToList();
                }
                else {
                    definitions = currentDict.Keys.ToList();
                }

                var terms = _searchText.Split(' ').Select(s => s.ToLower()).ToHashSet();

                var sorted = definitions?.OrderBy(title);
                _matchCount = 0;
                Div(100);
                foreach (var def in sorted) {
                    var name = title(def);
                    var nameLower = name.ToLower();
                    if (name is not { Length: > 0 } ||
                        (_searchText.Length != 0 && !terms.All(term => nameLower.Contains(term)))) {
                        continue;
                    }

                    currentDict.TryGetValue(def, out var item);
                    OnRowGUI(callerKey, ref changed, target, def, item, title, description, value, setValue, incrementValue,
                        decrementValue,
                        addItem, removeItem);
                }
            }

            private static void OnRowGUI(
                string callerKey, ref bool changed,
                T target,
                TDef definition,
                TItem item,
                Func<TDef, string> title,
                Func<TDef, string> description = null,
                Func<TItem, string> value = null,
                // ReSharper disable once UnusedParameter.Local
                Action<T, TItem, string> setValue = null,
                Func<T, TItem, Action> incrementValue = null,
                Func<T, TItem, Action> decrementValue = null,
                Func<T, TDef, Action> addItem = null,
                Func<T, TItem, Action> removeItem = null,
                Func<T, TItem, Action> childrenOnGUI = null
            ) {
                // var remwidth = Ummwidth;
                _matchCount++;
                using (HorizontalScope()) {
                    space(100);
                    // remwidth -= 100;
                    var titlewidth = ((int)(ummWidth / (IsWide ? 3.0f : 4.0f))).point();
                    var text = title(definition);
                    var titleKey = $"{callerKey}-{text}";
                    if (item != null) {
                        text = text.Cyan().Bold();
                    }

                    if (childrenOnGUI == null) {
                        Label(text, width(titlewidth));
                        // remwidth -= titlewidth;
                    }
                    else {
                        DisclosureStates.TryGetValue(titleKey, out var show);
                        if (DisclosureToggle(text, ref show, titlewidth)) {
                            DisclosureStates[titleKey] = show;
                        }
                    }

                    space(10);
                    // remwidth -= 10;
                    if (item != null && value?.Invoke(item) is { } stringValue) {
                        if (decrementValue?.Invoke(target, item) is { } decrementAction) {
                            if (ActionButton("<", () => decrementAction(), 60.width())) {
                                changed = true;
                            }
                        }
                        else {
                            63.space();
                        }

                        space(10);
                        Label($"{stringValue}".Orange().Bold(), width(100));
                        if (incrementValue?.Invoke(target, item) is { } incrementer) {
                            if (ActionButton(">", incrementer, 60.width())) {
                                changed = true;
                            }
                        }
                        else {
                            63.space();
                        }
                    }

                    30.space();
                    if (addItem?.Invoke(target, definition) is { } add) {
                        if (ActionButton("Add", add, 150.width())) {
                            changed = true;
                        }
                    }

                    space(10);
                    // remwidth -= 10;
                    if (item != null && removeItem?.Invoke(target, item) is { } remove) {
                        if (ActionButton("Remove", remove, 175.width())) {
                            changed = true;
                        }
                    }

                    // remwidth -= 178;
                    space(20);
                    // remwidth -= 20;
                    using (VerticalScope()) {
                        if (description != null) {
                            Label(description(definition).StripHTML().Green(), AutoWidth());
                        }
                    }
                }

                Div(100);
            }

            [UsedImplicitly]
            public static void UpdateSearchResults(string searchTextParam, IEnumerable<TDef> definitions,
                Func<TDef, string> searchAndSortKey) {
                if (definitions == null) {
                    return;
                }

                var terms = searchTextParam.Split(' ').Select(s => s.ToLower()).ToHashSet();
                var filtered = new List<TDef>();
                foreach (var def in definitions) {
                    if (def.GetType().ToString().Contains(searchTextParam)
                       ) {
                        filtered.Add(def);
                    }
                    else {
                        var name = searchAndSortKey(def).ToLower();
                        if (terms.All(term => name.Matches(term))) {
                            filtered.Add(def);
                        }
                    }
                }

                _matchCount = filtered.Count;
                _filteredDefinitions = filtered.OrderBy(searchAndSortKey).Take(_searchLimit).ToArray();
            }
        }
    }
}