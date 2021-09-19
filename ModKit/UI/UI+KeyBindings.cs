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
        static private KeyCode[] mouseButtonsValid = { KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };
        public static bool IsModifier(this KeyCode code)
            => code == KeyCode.LeftControl || code == KeyCode.RightControl
            || code == KeyCode.LeftAlt || code == KeyCode.RightAlt
            || code == KeyCode.LeftShift || code == KeyCode.RightShift
            || code == KeyCode.LeftCommand || code == KeyCode.RightCommand;
        public static bool IsControl(this KeyCode code)
            => code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        public static bool IsCommand(this KeyCode code)
            => code == KeyCode.LeftCommand || code == KeyCode.RightCommand;
        public static bool IsAlt(this KeyCode code)
            => code == KeyCode.LeftControl || code == KeyCode.RightControl;
        public static bool IsShift(this KeyCode code)
            => code == KeyCode.LeftShift || code == KeyCode.RightShift;

        [Serializable]
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
            public bool IsEmpty { get { return Key == KeyCode.None && !Ctrl && !Alt && !Shift; } }
            public bool IsActive {
                get {
                    var keyCode = Event.current.keyCode;
                    var ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                    var altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                    var cmdDown = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
                    var shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    return keyCode == Key
                        && ctrlDown == Ctrl
                        && altDown == Alt
                        && cmdDown == Cmd
                        && shiftDown == Shift;
                }
            }
            public override string ToString() {
                var result = "";
                if (Ctrl)
                    result += "Ctrl+";
                if (Alt)
                    result += "Alt+";
                if (Cmd)
                    result += "Cmd+";
                if (Shift)
                    result += "Shift+";
                return result + Key.ToString();
            }
        }
        public static class KeyBindings {
            static ModEntry modEntry = null;
            static SerializableDictionary<string, KeyBind> bindings = null;
            static Dictionary<string, Action> actions = new Dictionary<string, Action> { };
            public static bool IsActive(string identifier) {
                return GetBinding(identifier).IsActive;
            }
            internal static void RegisterAction(string identifier, Action action) {
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
            public static void OnGUI() {

                foreach (var item in bindings) {
                    var identifier = item.Key;
                    var binding = item.Value;
                    if (binding.IsActive && actions.ContainsKey(identifier)) {
                        Action action;
                        actions.TryGetValue(identifier, out action);
                        action();
                    }
                }
                if (Event.current.type != EventType.Layout)
                    return;
                // actions are registered on each render loop by BindableActionButton so we clear them here so to support disabling keybindings in the UI
                actions.Clear();
            }
        }
        static string selectedIdentifier = null;
        static KeyBind oldValue = null;
        public static KeyBind EditKeyBind(string identifier) {
            var keyBind = KeyBindings.GetBinding(identifier);
            bool isEditing = identifier == selectedIdentifier;
            bool isEditingOther = selectedIdentifier != null && identifier != selectedIdentifier && oldValue != null;
            string label = keyBind.IsEmpty ? (isEditing ? "Cancel" : "Bind") : "Hotkey:".cyan() + " " + keyBind.ToString().orange().bold();
            if (GL.Button(label, UI.AutoWidth())) {
                if (isEditing  || isEditingOther) {
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
            UI.Space(25);
            UI.Label(keyBind.IsEmpty ? (oldValue == null ? "push to pick key binding".green() : "type key combo to assign".cyan()) : "");
            if (isEditing && keyBind.IsEmpty && Event.current != null) {
                var IsCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                var IsAltDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                var IsCmdDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                var IsShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var keyCode = Event.current.keyCode;
                if (Event.current.isKey && !keyCode.IsModifier()) {
                    keyBind = new KeyBind(identifier, keyCode, IsCtrlDown, IsAltDown, IsCmdDown, IsShiftDown);
                    KeyBindings.SetBinding(identifier, keyBind);
                    selectedIdentifier = null;
                    oldValue = null;
                    Input.ResetInputAxes();
                    return keyBind;
                }

                foreach (var mouseButton in mouseButtonsValid) {
                    if (Input.GetKey(mouseButton)) {
                        var mouseCode = mouseButton;
                        keyBind = new KeyBind(identifier, mouseCode, IsCtrlDown, IsAltDown, IsShiftDown);
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
                var keyBind = EditKeyBind(identifier);
            }
        }
        public static void BindableActionButton(String title, Action action, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300f) }; }
            if (GL.Button(title, options)) { action(); }
            EditKeyBind(title);
            if (Event.current.type == EventType.Layout)
                KeyBindings.RegisterAction(title, action);
        }
    }
}
