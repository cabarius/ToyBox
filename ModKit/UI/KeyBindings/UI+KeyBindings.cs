using UnityEngine;
using System;
using GL = UnityEngine.GUILayout;
using System.Linq;

namespace ModKit {
    public static partial class UI {
        private static string selectedIdentifier = null;
        private static KeyBind oldValue = null;
        public static KeyBind EditKeyBind(string identifier, bool showHint = true, bool allowModifierOnly = false, params GUILayoutOption[] options) {
            if (Event.current.type == EventType.Layout)
                KeyBindings.OnGUI();
            var keyBind = KeyBindings.GetBinding(identifier);
            var isEditing = identifier == selectedIdentifier;
            var isEditingOther = selectedIdentifier != null && identifier != selectedIdentifier && oldValue != null;
            var label = keyBind.IsEmpty ? (isEditing ? "Cancel" : "Bind") : keyBind.ToString().orange().bold();
            showHint = showHint && isEditing;
            var conflicts = keyBind.Conflicts();
            using (VerticalScope(options)) {
                Space(3.point());
                if (GL.Button(label, hotkeyStyle, AutoWidth())) {
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
                if (conflicts.Count() > 0) {
                    Label("conflicts".orange().bold() + "\n" + string.Join("\n", conflicts));
                }
                if (showHint) {
                    var hint = "";
                    if (keyBind.IsEmpty)
                        hint = oldValue == null ? "set key binding".green() : "press key".green();
                    Label(hint);
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
                    Mod.Trace($"    currentEvent isKey - bind: {keyBind}");
                    KeyBindings.SetBinding(identifier, keyBind);
                    selectedIdentifier = null;
                    oldValue = null;
                    Input.ResetInputAxes();
                    return keyBind;
                }

                // Allow raw modifier keys as keybinds
                if (Event.current.isKey && keyCode.IsModifier() && allowModifierOnly) {
                    keyBind = new KeyBind(identifier, keyCode, false, false, false, false);
                    Mod.Trace($"    currentEvent isKey - bind: {keyBind}");
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
            using (HorizontalScope()) {
                Space(indent);
                Label(title.bold(), titleWidth == 0 ? ExpandWidth(false) : Width(titleWidth));
                Space(25);
                EditKeyBind(identifier, true);
            }
        }

        public static void ModifierPicker(string identifier, string title, float indent = 0, float titleWidth = 0) {
            using (HorizontalScope()) {
                Label(title.bold(), titleWidth == 0 ? ExpandWidth(false) : Width(titleWidth));
                Space(25);
                EditKeyBind(identifier, true, true);
            }
        }

        // One stop shopping for making an instant button that you want to let a player bind to a key in game
        public static void BindableActionButton(string title, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300) }; }
            var action = KeyBindings.GetAction(title);
            if (GL.Button(title, options)) { action(); }
            EditKeyBind(title, true, false, Width(200));
        }

        // Action button designed to live in a collection with a BindableActionButton
        public static void NonBindableActionButton(string title, Action action, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300) }; }
            if (GL.Button(title, options)) { action(); }
            Space(204);
            if (Event.current.type == EventType.Layout)
                KeyBindings.RegisterAction(title, action);
        }
    }
}
