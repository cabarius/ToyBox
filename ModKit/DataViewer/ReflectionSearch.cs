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
    public partial class ReflectionSearch : MonoBehaviour {
        public delegate void SearchProgress(int visitCount, int depth, int breadth);
        public bool isSearching { get { return searchCoroutine != null;  } }
        private static HashSet<int> VisitedInstanceIDs = new HashSet<int> { };
        public static int SequenceNumber = 0;
        private IEnumerator searchCoroutine;
        private static ReflectionSearch _shared;
        public static int maxSearchDepth = 1;

        public static ReflectionSearch Shared {
            get {
                if (_shared == null) {
                    _shared = new GameObject().AddComponent<ReflectionSearch>();
                    UnityEngine.Object.DontDestroyOnLoad(_shared.gameObject);
                }
                return _shared;
            }
        }

        public void StartSearch(Node node, String searchText, SearchProgress updator, ReflectionSearchResult resultRoot) {
            if (searchCoroutine != null) {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }
            VisitedInstanceIDs.Clear();
            resultRoot.Clear();
            resultRoot.Node = node;
            StopAllCoroutines();
            updator(0, 0, 1);
            if (node == null) return;
            SequenceNumber++;
            Mod.Log($"seq: {SequenceNumber} - search for: {searchText}");
            if (searchText.Length == 0) {
            }
            else {
                var todo = new List<Node> { node };
                searchCoroutine = Search(searchText, todo , 0, 0, SequenceNumber, updator, resultRoot);
                StartCoroutine(searchCoroutine);                
            }
        }
        public void Stop() {
            if (searchCoroutine != null) {
                StopCoroutine(searchCoroutine);
                searchCoroutine = null;
            }
            StopAllCoroutines();
        }
        private IEnumerator Search(String searchText, List<Node> todo, int depth, int visitCount, int sequenceNumber, SearchProgress updator, ReflectionSearchResult resultRoot) {
            yield return null;
            if (sequenceNumber != SequenceNumber) yield return null;
            var todoText = todo.Count > 0 ? todo.First().Name : "n/a";
            //Main.Log(depth, $"seq: {sequenceNumber} depth: {depth} - count: {todo.Count} - todo[0]: {todoText}");
            var newTodo = new List<Node> { };
            var breadth = todo.Count();
            foreach (var node in todo) {
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
                    if (node.Name.Matches(searchText) || node.ValueText.Matches(searchText)) {
                        foundMatch = true;
                        updator(visitCount, depth, breadth);
                        resultRoot.AddSearchResult(node);
                        Mod.Log(depth, $"matched: {node.GetPath()} - {node.ValueText}");
                        Mod.Log($"{resultRoot.ToString()}");
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
                    if (visitCount % 100 == 0) updator(visitCount, depth, breadth);

                }
                if (node.hasChildren && !alreadyVisted) {
                    if (node.InstanceID is int instID2 && instID2 == this.GetInstanceID()) break;
                    if (node.Name == "searchCoroutine") break;
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
                if (visitCount % 1000 == 0) yield return null;
            }
            if (newTodo.Count > 0 && depth < maxSearchDepth)
                yield return Search(searchText, newTodo, depth + 1, visitCount, sequenceNumber, updator, resultRoot);
            else
                Stop();
        }
    }
}
