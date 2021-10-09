using System;
using System.Collections.Generic;
using ModKit.Utility;
using static UnityModManagerNet.UnityModManager;
using System.Linq;

namespace ModKit {
    public static partial class UI {
        public static IEnumerable<string> Conflicts(this KeyBind keyBind) => KeyBindings.conflicts.GetValueOrDefault(keyBind.bindCode, new List<string> { }).Where(id => id != keyBind.ID);

        public static class KeyBindings {
            private static ModEntry modEntry = null;
            private static SerializableDictionary<string, KeyBind> bindings = null;
            private static readonly Dictionary<string, Action> actions = new() { };
            internal static Dictionary<string, List<string>> conflicts = new() { };
            internal static bool BindingsDidChange = false;
            public static bool IsActive(string identifier) => GetBinding(identifier).IsActive;
            public static Action GetAction(string identifier) => actions.GetValueOrDefault(identifier, null);
            public static void RegisterAction(string identifier, Action action) => actions[identifier] = action;
            internal static KeyBind GetBinding(string identifier) {
                BindingsDidChange = true;
                return bindings.GetValueOrDefault(identifier, new KeyBind(identifier));
            }
            internal static void SetBinding(string identifier, KeyBind binding) {
                bindings[identifier] = binding;
                ModSettings.SaveSettings(modEntry, "bindings.json", bindings);
                BindingsDidChange = true;
            }
            public static void UpdateConflicts() {
                conflicts.Clear();
                foreach (var binding in bindings) {
                    var keyBind = binding.Value;
                    if (!keyBind.IsEmpty) {
                        var identifier = binding.Key;
                        var bindCode = keyBind.ToString();
                        var conflict = conflicts.GetValueOrDefault(bindCode, new List<string> { });
                        conflict.Add(identifier);
                        conflicts[bindCode] = conflict;
                    }
                }
                conflicts = conflicts.Filter(kvp => kvp.Value.Count > 1);
                //Logger.Log($"conflicts: {String.Join(", ", conflicts.Select(kvp => $"{kvp.Key.orange()} : {kvp.Value.Count}".cyan())).yellow()}");
            }
            public static void OnLoad(ModEntry modEntry) {
                if (KeyBindings.modEntry == null)
                    KeyBindings.modEntry = modEntry;
                if (bindings == null) {
                    ModSettings.LoadSettings(modEntry, "bindings.json", ref bindings);
                    BindingsDidChange = true;
                }
            }

            public static void OnGUI() {
                if (BindingsDidChange) {
                    UpdateConflicts();
                    BindingsDidChange = false;
                }
            }

            private static KeyBind lastTriggered = null;
            public static void OnUpdate() {
                if (lastTriggered != null) {
                    //if (debugKeyBind)
                    //    Logger.Log($"    lastTriggered: {lastTriggered} - IsActive: {lastTriggered.IsActive}");
                    if (!lastTriggered.IsActive) {
                        //if (debugKeyBind)
                        //    Logger.Log($"    lastTriggered: {lastTriggered} - Finished".green());
                        lastTriggered = null;
                    }
                }
                //if (debugKeyBind)
                //    Logger.Log($"looking for {Event.current.keyCode}");
                foreach (var item in bindings) {
                    var identifier = item.Key;
                    var binding = item.Value;
                    var active = binding.IsActive;
                    //if (debugKeyBind)
                    //    Logger.Log($"    checking: {binding.ToString()} - IsActive: {(active ? "True".cyan() : "False")} action: {actions.ContainsKey(identifier)}");
                    if (active && actions.ContainsKey(identifier)) {
                        //if (debugKeyBind)
                        //    Logger.Log($"    binding: {binding.ToString()} - lastTriggered: {lastTriggered}");
                        if (binding != lastTriggered) {
                            //if (debugKeyBind)
                            //    Logger.Log($"    firing action: {identifier}".cyan());
                            actions.TryGetValue(identifier, out var action);
                            action();
                            lastTriggered = binding;
                        }
                    }
                }
            }
        }
    }
}
