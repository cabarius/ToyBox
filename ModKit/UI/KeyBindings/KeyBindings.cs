using System;
using System.Collections.Generic;
using ModKit.Utility;
using static ModKit.UI;
using static UnityModManagerNet.UnityModManager;
using System.Linq;

namespace ModKit {
    public static partial class UI {
        public static IEnumerable<string?> Conflicts(this KeyBind keyBind) => KeyBindings.conflicts
                                                                                         .GetValueOrDefault(keyBind.bindCode, new List<string> { }).Where(id => id != keyBind.ID);
        public static void RemoveConflicts(this KeyBind keyBind) => KeyBindings.RemoveConflicts(keyBind);
        public static string ToggleTranscriptForState(string identifier, bool state)
            => $"Toggle: {identifier.blue()} -> {(state ? "True".blue() : "False".red())}";

        // This maintains the association of actions and KeyBinds associate with a specific identifier. Since we can not persist the action we persist the keybind and the client needs to register the action with the identifier each time the mod is initialized. This also contains logic to detect conflicts.  
        // NOTE: This also provides an OnUpdate call and any client of this must manually call it during an OnUpdate block in their mod for KeyBindings to function correctly
        public static class KeyBindings {
            private static ModEntry modEntry = null;
            private static SerializableDictionary<string?, KeyBind> bindings = null;
            private static readonly Dictionary<string?, (Action action, Func<string, string> description)> actions = new() { };
            internal static Dictionary<string, List<string>> conflicts = new() { };
            internal static bool BindingsDidChange = false;
            public static bool IsActive(string? identifier) => GetBinding(identifier).IsActive;
            public static (Action action, Func<string, string> description)? GetAction(string? identifier) {
                if (actions.ContainsKey(identifier))
                    return actions[identifier];
                return null;
            }
            public static void RegisterAction(string? identifier, Action action, Func<string, string>? description = null)
                => actions[identifier] = (action, description);
            internal static KeyBind GetBinding(string? identifier) {
                BindingsDidChange = true;
                return bindings.GetValueOrDefault(identifier, new KeyBind(identifier));
            }
            internal static void SetBinding(string? identifier, KeyBind binding) {
                bindings[identifier] = binding;
                modEntry.SaveSettings("bindings.json", bindings);
                BindingsDidChange = true;
            }
            internal static void ClearBinding(string? identifier) {
                if (bindings.ContainsKey(identifier))
                    bindings.Remove(identifier);
                modEntry.SaveSettings("bindings.json", bindings);
                BindingsDidChange = true;
            }
            public static void UpdateConflicts() {
                conflicts.Clear();
                foreach (var binding in bindings) {
                    var keyBind = binding.Value;
                    if (!keyBind.IsEmpty && !keyBind.IsModifierOnly) {
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
            public static void RemoveConflicts(KeyBind keyBind) {
                UpdateConflicts();
                var doomed = keyBind.Conflicts();
                foreach (var condemnedIdentifier in doomed) ClearBinding(condemnedIdentifier);
            }
            public static void OnLoad(ModEntry modEntry) {
                if (KeyBindings.modEntry == null)
                    KeyBindings.modEntry = modEntry;
                if (bindings == null) {
                    SettingsController.LoadSettings(modEntry, "bindings.json", ref bindings);
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
                if (lastTriggered != null)
                    //if (debugKeyBind)
                    //    Logger.Log($"    lastTriggered: {lastTriggered} - IsActive: {lastTriggered.IsActive}");
                    if (!lastTriggered.IsActive)
                        //if (debugKeyBind)
                        //    Logger.Log($"    lastTriggered: {lastTriggered} - Finished".green());
                        lastTriggered = null;
                //if (debugKeyBind)
                //    Logger.Log($"looking for {Event.current.keyCode}");
                foreach (var item in bindings) {
                    var identifier = item.Key;
                    var binding = item.Value;
                    var active = binding.IsActive;
                    //if (debugKeyBind)
                    //    Logger.Log($"    checking: {binding.ToString()} - IsActive: {(active ? "True".cyan() : "False")} action: {actions.ContainsKey(identifier)}");
                    if (active && actions.ContainsKey(identifier))
                        //if (debugKeyBind)
                        //    Logger.Log($"    binding: {binding.ToString()} - lastTriggered: {lastTriggered}");
                        if (binding != lastTriggered) {
                            //if (debugKeyBind)
                            //    Logger.Log($"    firing action: {identifier}".cyan());
                            actions.TryGetValue(identifier, out var entry);
                            entry.action();
                            lastTriggered = binding;
                            if (!Mod.ModKitSettings.toggleKeyBindingsOutputToTranscript) continue;
                            Mod.InGameTranscriptLogger?.Invoke(entry.description != null ? entry.description(identifier) : $"Action " + identifier.blue());
                        }
                }
            }
        }
    }
}