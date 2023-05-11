using ModKit.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using static ModKit.Utility.ReflectionCache;
using static ModKit.Utility.StringExtensions;
using static ModKit.Utility.RichTextExtensions;

namespace ModKit.DataViewer {

    /**
     * Strategy For Async Deep Search
     * 
     * --- update
     * 
     * duh can't do real async/Task need to use unity coroutines ala https://docs.unity3d.com/ScriptReference/MonoBehaviour.StartCoroutine.html
     * 
     * two coroutines implemented with Task() and async/await
     *      Render Path - regular OnGUI on the main thread
     *      Search Loop
     *          background thread posting updates using IProgress on the main thread using something like
     *              private async void Button_Click(object sender, EventArgs e) 
     *              here https://stackoverflow.com/questions/12414601/async-await-vs-backgroundworker
     * 
     * Store Node.searchText as a static
     * 
     * Add to node
     *      HashSet<String> matches
     *      searchText
     * 
     * Node.Render(depth) - main thread (UI)
     *      if (!autoExpandKeys.IsEmpty), foreach (key, value) display {key}, {value | Render(children+1) )
     *      if (isExpanded) foreach (key, value) display {key}, {value | Render(children+1) )
      *     yield
     * 
     * Node.Search(string[] keyPath, Func<Node,Bool> matches, int depth) - background thread
     *      autoMatchKeys.Clear()
     *      foreach (key, value) 
     *          if (matches(key) matches += key
     *          if (value.isAtomic && matches(value))  matches += key
     *          if we added any keys to matches then {
     *              foreach parent = Node.parent until Node.IsRoot {
     *                  depth -= 1
     *                  parKey = keyPath[depth]
     *                  if parent.autoMatchKeys.Contains(parKey) done // another branch populated through that key
     *                  parent.matches += parKey
     *              }
     *          }
     *          else (value as Node).Search(keyPath + key, matches)
     *          
     *          
     * Bool Matches(text)
     *      if (text.contains(searchText) return true
     * 
     * On User click expand for Node, Node.isExpanded = !Node.isExpanded
     *      
     * On searchText change
     *      foreach Node in Tree, this.matches.Clear()
     *      
     */
    public partial class ReflectionSearch {
        public delegate void SearchProgress(int visitCount, int depth, int breadth);
        private CancellationTokenSource _cancellationTokenSource;
        public bool isSearching { get; private set; } = false;
        private static HashSet<int> VisitedInstanceIDs = new HashSet<int> { };
        public static int SequenceNumber = 0;
        private static ReflectionSearch _shared;
        public static int maxSearchDepth = 1;
        private static Queue<Action> _updates = new();
        public static int ApplyUpdates() {
            lock (_updates) {
                int count = _updates.Count;
                foreach (var update in _updates) update.Invoke();
                _updates.Clear();
                return count;
            }
        }
        public static void AddUpdate(Action update) {
            lock (_updates) {
                _updates.Enqueue(update);
            }
        }
        public static void ClearUpdates() {
            lock (_updates) {
                _updates.Clear();
            }
        }
        public static ReflectionSearch Shared {
            get {
                if (_shared == null) {
                    _shared = new ReflectionSearch();
                }
                return _shared;
            }
        }
        // Task.Run(() => UpdateSearchResults(_searchText, definitions, searchKey, sortKey, search));
        public void StartSearch(Node node, string[] searchTerms, SearchProgress updater, ReflectionSearchResult resultRoot) {
            if (isSearching) {
                _cancellationTokenSource.Cancel();
                isSearching = false;
            }
            VisitedInstanceIDs.Clear();
            lock (resultRoot) {
                resultRoot.Clear();
                resultRoot.Node = node;
            }
            _cancellationTokenSource = new();
            isSearching = true;
            AddUpdate(() => updater(0, 0, 1));
            if (node == null) return;
            SequenceNumber++;
            Mod.Log($"seq: {SequenceNumber} - search for: {searchTerms}");
            if (searchTerms.Length != 0) {
                var todo = new List<Node> { node };
                Task.Run(() => Search(searchTerms, todo , 0, 0, SequenceNumber, updater, resultRoot));
            }
        }
        public void Stop() {
            if (isSearching) {
                isSearching = false;
                _cancellationTokenSource.Cancel();
            }
        }
        private void Search(string[] searchTerms, List<Node> todo, int depth, int visitCount, int sequenceNumber, SearchProgress updater, ReflectionSearchResult resultRoot) {
            if (_cancellationTokenSource.IsCancellationRequested) {
                isSearching = false;
                return;
            }
            if (sequenceNumber != SequenceNumber) {
                Stop();
                return;
            }
            var todoText = todo.Count > 0 ? todo.First().Name : "n/a";
            //Main.Log(depth, $"seq: {sequenceNumber} depth: {depth} - count: {todo.Count} - todo[0]: {todoText}");
            var newTodo = new List<Node> { };
            var breadth = todo.Count();
            var termCount = searchTerms.Length;
            foreach (var node in todo) {
                if (_cancellationTokenSource.IsCancellationRequested || isSearching == false) {
                    isSearching = false;
                    return;
                }
                bool foundMatch = false;
                var instanceID = node.InstanceID;
                bool alreadyVisted = false;
                if (instanceID is int instID) {
                    if (VisitedInstanceIDs.Contains(instID))
                        alreadyVisted = true;
                    else {
                        VisitedInstanceIDs.Add(instID);
                    }
                }
                visitCount++;
                //Main.Log(depth, $"node: {node.Name} - {node.GetPath()}");
                try {
                    var matchCount = 0;
                    foreach (var term in searchTerms) {
                        var nodeToCheck = node;
                        bool found = false;
                        while (nodeToCheck != null && !found) {
                            if (nodeToCheck.Name.Matches(term) || nodeToCheck.ValueText.Matches(term)) {
                                found = true;
                                break;
                            }
                            nodeToCheck = nodeToCheck.GetParent();
                        }
                        if (found) matchCount++;
                    }
                    if (matchCount >= termCount) {
                        foundMatch = true;
                        AddUpdate(() => {
                            updater(visitCount, depth, breadth);
                            lock (resultRoot) {
                                resultRoot.AddSearchResult(node);
                                Mod.Log(depth, $"matched: {node.GetPath()} - {node.ValueText}");
                                Mod.Log($"{resultRoot.ToString()}");
                            }
                        });
                    }
                }
                catch (Exception e) {
                    Mod.Log(depth, $"caught - {e}");
                }
                node.Matches = foundMatch;
                if (!foundMatch) {
                    //Main.Log(depth, $"NOT matched: {node.Name} - {node.ValueText}");
                    //if (node.Expanded == ToggleState.On && node.GetParent() != null) {
                    //    node.Expanded = ToggleState.Off;
                    //}
                    if (visitCount % 100 == 0) AddUpdate(() => updater(visitCount, depth, breadth));

                }
                if (node.hasChildren && !alreadyVisted) {
                    //if (node.Name == "SyncRoot") break;
                    //if (node.Name == "normalized") break;

                    try {
                        foreach (var child in node.GetItemNodes()) {
                            //Main.Log(depth + 1, $"item: {child.Name}"); 
                            newTodo.Add(child);
                        }
                    }
                    catch (Exception e) {
                        Mod.Log(depth, $"caught - {e}");
                    }
                    try {
                        foreach (var child in node.GetComponentNodes()) {
                            //Main.Log(depth + 1, $"comp: {child.Name}"); 
                            newTodo.Add(child);
                        }
                    }
                    catch (Exception e) {
                        Mod.Log(depth, $"caught - {e}");
                    }
                    try {
                        foreach (var child in node.GetPropertyNodes()) {
                            //Main.Log(depth + 1, $"prop: {child.Name}");
                            newTodo.Add(child);
                        }
                    }
                    catch (Exception e) {
                        Mod.Log(depth, $"caught - {e}");
                    }
                    try {
                        foreach (var child in node.GetFieldNodes()) {
                            //Main.Log(depth + 1, $"field: {child.Name}");
                            newTodo.Add(child);
                        }
                    }
                    catch (Exception e) {
                        Mod.Log(depth, $"caught - {e}");
                    }
                }
            }
            if (newTodo.Count > 0 && depth < maxSearchDepth)
                Search(searchTerms, newTodo, depth + 1, visitCount, sequenceNumber, updater, resultRoot);
            else
                Stop();
        }
    }
}
