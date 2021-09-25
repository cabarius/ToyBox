using UnityEngine;
using Kingmaker;
using Kingmaker.UI;
using Kingmaker.Settings;
using System;
using System.Collections.Generic;
using GL = UnityEngine.GUILayout;
using ModKit;
using ModKit.Utility;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;
using Newtonsoft.Json;

namespace ModKit {
    static partial class UI {
#if DEBUG
        private const bool debugKeyBind = false;
#else
        private const bool debugKeyBind = false;
#endif
        private const float V = 10f;
        static private HashSet<KeyCode> allowedMouseButtons = new HashSet<KeyCode> { KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };
        public static bool IsModifier(this KeyCode code)
            => code == KeyCode.LeftControl || code == KeyCode.RightControl
            || code == KeyCode.LeftAlt || code == KeyCode.RightAlt
            || code == KeyCode.LeftShift || code == KeyCode.RightShift
            || code == KeyCode.LeftCommand || code == KeyCode.RightCommand;
        public static bool IsControl(this KeyCode code)
            => code == KeyCode.LeftControl || code == KeyCode.RightControl;
        public static bool IsAlt(this KeyCode code)
            => code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        public static bool IsCommand(this KeyCode code)
            => code == KeyCode.LeftCommand || code == KeyCode.RightCommand;
        public static bool IsShift(this KeyCode code)
            => code == KeyCode.LeftShift || code == KeyCode.RightShift;

        private static GUIStyle _hotkeyStyle;
        public static GUIStyle hotkeyStyle {
            get {
                if (_hotkeyStyle == null)
                    _hotkeyStyle = new GUIStyle(GUI.skin.textArea) {
                        margin = new RectOffset(3, 3, 3, 3),
                        richText = true
                    };
                _hotkeyStyle.fontSize = UnityModManager.UI.Scale(11);
                _hotkeyStyle.fixedHeight = UnityModManager.UI.Scale(17);

                return _hotkeyStyle;
            }
        }
        [JsonObject(MemberSerialization.OptIn)]
        public class KeyBind {
            [JsonProperty]
            public string ID;
            [JsonProperty]
            public KeyCode Key;
            [JsonProperty]
            public bool Ctrl;
            [JsonProperty]
            public bool Alt;
            [JsonProperty]
            public bool Cmd;
            [JsonProperty]
            public bool Shift;
            public KeyBind(string identifer, KeyCode key = KeyCode.None, bool ctrl = false, bool alt = false, bool cmd = false, bool shift = false) {
                ID = identifer;
                Key = key;
                Ctrl = ctrl;
                Alt = alt;
                Cmd = cmd;
                Shift = shift;
            }
            [JsonIgnore]
            public bool IsEmpty => Key == KeyCode.None;
            [JsonIgnore]
            public bool IsKeyCodeActive {
                get {
                    if (Key == KeyCode.None) {
                        //if (debugKeyBind) Logger.Log($"        keyCode: {Key} --> not active");
                        return false;
                    }
                    if (allowedMouseButtons.Contains(Key)) {
                        //if (debugKeyBind && Input.GetKey(Key)) Logger.Log($"        mouseKey: {Key} --> active");
                        return Input.GetKey(Key);
                    }
                    bool active = Key == Event.current.keyCode; // && Input.GetKey(Key);
                        //if (debugKeyBind) Logger.Log($"        keyCode: {Key} --> {active}");
                    return active;
                }
            }
            [JsonIgnore]
            public bool IsActive {
                get {
                    if (Event.current == null) {
                        //Logger.Log("        Event.current == null -> inactive");
                        return false;
                    }
                    if (!IsKeyCodeActive) {
                        //Logger.Log("        IsKeyCodeActive == false -> inactive");
                        return false;
                    }
                    var ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                    var altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                    var cmdDown = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
                    var shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    // note we already checked Key above
                    bool active = ctrlDown == Ctrl
                                && altDown == Alt
                                && cmdDown == Cmd
                                && shiftDown == Shift;
                    //if (debugKeyBind) Logger.Log($"        ctrl: {ctrlDown == Ctrl} shift: {altDown == Alt} cmd: {cmdDown == Cmd} Alt: {shiftDown == Shift} --> {(ctrlDown ? "Active".cyan() : "inactive")}");
                    return active;
                }
            }
            public override string ToString() { // Why can't Unity display these ⌥⌃⇧⌘ ???  ⌗⌃⌥⇧⇑⌂©ăåâÂ
                var result = "";
                if (Ctrl)
                    result += "^".cyan();
                if (Shift)
                    result += "⇑".cyan();
                if (Alt || Cmd)
                    result += "Alt".cyan();
                return result + (Ctrl || Shift || Alt ? "+".cyan() : "") + Key.ToString();
            }
        }
        public static class KeyBindings {
            static ModEntry modEntry = null;
            static SerializableDictionary<string, KeyBind> bindings = null;
            static Dictionary<string, Action> actions = new Dictionary<string, Action> { };
            public static bool IsActive(string identifier) {
                return GetBinding(identifier).IsActive;
            }
            public static Action GetAction(string identifier) {
                return actions.GetValueOrDefault(identifier, null);
            }
            public static void RegisterAction(string identifier, Action action) {
                actions[identifier] = action;
            }
            internal static KeyBind GetBinding(string identifier) {
                return bindings.GetValueOrDefault(identifier, new KeyBind(identifier));
            }
            internal static void SetBinding(string identifier, KeyBind binding) {
                bindings[identifier] = binding;
                ModSettings.SaveSettings(modEntry, "bindings.json", bindings);
            }
            public static void OnLoad(ModEntry modEntry) {
                if (KeyBindings.modEntry == null)
                    KeyBindings.modEntry = modEntry;
                if (bindings == null) {
                    ModSettings.LoadSettings(modEntry, "bindings.json", ref bindings);
                }
            }
            static KeyBind lastTriggered = null;
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
                            Action action;
                            actions.TryGetValue(identifier, out action);
                            action();
                            lastTriggered = binding;
                        }
                    }
                }
            }
        }

        static string selectedIdentifier = null;
        static KeyBind oldValue = null;
        public static KeyBind EditKeyBind(string identifier, bool showHint = true, params GUILayoutOption[] options) {
            var keyBind = KeyBindings.GetBinding(identifier);
            bool isEditing = identifier == selectedIdentifier;
            bool isEditingOther = selectedIdentifier != null && identifier != selectedIdentifier && oldValue != null;
            string label = keyBind.IsEmpty ? (isEditing ? "Cancel" : "Bind") : keyBind.ToString().orange().bold();
            showHint = showHint && isEditing;
            using (UI.VerticalScope(options)) {
                UI.Space(UnityModManager.UI.Scale(3));
                if (GL.Button(label, hotkeyStyle, UI.AutoWidth())) {
                    if (isEditing || isEditingOther) {
                        KeyBindings.SetBinding(selectedIdentifier, oldValue);
                        if (isEditing) {
                            selectedIdentifier = null;
                            oldValue = null;
                            return KeyBindings.GetBinding(identifier);
                        }
                    }
                    selectedIdentifier = identifier;
                    oldValue = keyBind;
                    keyBind = new KeyBind(identifier);
                    KeyBindings.SetBinding(identifier, keyBind);
                }
                if (showHint) {
                    var hint = "";
                    if (keyBind.IsEmpty)
                        hint = oldValue == null ? "set key binding".green() : "press key".green();
                    UI.Label(hint);
                }
            }
            if (isEditing && keyBind.IsEmpty && Event.current != null) {
                var isCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                var isAltDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                var isCmdDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                var isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var keyCode = Event.current.keyCode;
                //Logger.Log($"    {keyCode.ToString()} ctrl:{isCtrlDown} alt:{isAltDown} cmd: {isCmdDown} shift: {isShiftDown}");
                if (keyCode == KeyCode.Escape || keyCode == KeyCode.Backspace) {
                    selectedIdentifier = null;
                    oldValue = null;
                    //Logger.Log("   unbound");
                    return KeyBindings.GetBinding(identifier);
                }
                if (Event.current.isKey && !keyCode.IsModifier()) {
                    keyBind = new KeyBind(identifier, keyCode, isCtrlDown, isAltDown, isCmdDown, isShiftDown);
                    Logger.Log($"    currentEvent isKey - bind: {keyBind}");
                    KeyBindings.SetBinding(identifier, keyBind);
                    selectedIdentifier = null;
                    oldValue = null;
                    Input.ResetInputAxes();
                    return keyBind;
                }

                foreach (var mouseButton in allowedMouseButtons) {
                    if (Input.GetKey(mouseButton)) {
                        keyBind = new KeyBind(identifier, mouseButton, isCtrlDown, isAltDown, isCmdDown, isShiftDown);
                        KeyBindings.SetBinding(identifier, keyBind);
                        selectedIdentifier = null;
                        oldValue = null;
                        Input.ResetInputAxes();
                        return keyBind;
                    }
                }
            }
            return keyBind;
        }
        public static void KeyBindPicker(string identifier, string title, float indent = 0, float titleWidth = 0) {
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                UI.Label(title.bold(), titleWidth == 0 ? UI.ExpandWidth(false) : UI.Width(titleWidth));
                UI.Space(25);
                var keyBind = EditKeyBind(identifier, true);
            }
        }

        // One stop shop for making an instant button that you want to let a player bind to a key in game
        public static void BindableActionButton(String title, bool showHint = false, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300) }; }
            var action = KeyBindings.GetAction(title);
            if (GL.Button(title, options)) { action(); }
            EditKeyBind(title, true, UI.Width(200));
        }

        // Action button designed to live in a collection with a BindableActionButton
        public static void NonBindableActionButton(String title, Action action, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300) }; }
            if (GL.Button(title, options)) { action(); }
            UI.Space(204);
            if (Event.current.type == EventType.Layout)
                KeyBindings.RegisterAction(title, action);
        }
    }
}
