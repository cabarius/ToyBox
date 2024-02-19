using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GL = UnityEngine.GUILayout;

namespace ModKit {
    public static partial class UI {
        // Here we provide UI elements for managing KeyBinds.  We provide a low level UI to set the keys for a key binding as well as some built in controls. 
        private static string? selectedIdentifier = null;
        private static KeyBind oldValue = null;
        public static KeyBind EditKeyBind(string? identifier, bool showHint = true, bool allowModifierOnly = false, params GUILayoutOption[] options) {
            if (Event.current.type == EventType.Layout)
                KeyBindings.OnGUI();
            var keyBind = KeyBindings.GetBinding(identifier);
            var isEditing = identifier == selectedIdentifier;
            var isEditingOther = selectedIdentifier != null && identifier != selectedIdentifier && oldValue != null;
            var label = keyBind.IsEmpty ? isEditing ? "Cancel".localize() : "Bind".localize() : keyBind.ToString().orange().bold();
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
                var bind = keyBind;
                if (conflicts.Count() > 0) {
                    ActionButton("Replace".localize(), () => { bind.RemoveConflicts(); });
                    ActionButton("Clear".localize(), () => { KeyBindings.ClearBinding(bind.ID); });
                    Label("conflicts".localize().orange().bold() + "\n" + string.Join("\n", conflicts));
                }
                if (showHint) {
                    var hint = "";
                    if (keyBind.IsEmpty)
                        hint = oldValue == null ? "set key binding".localize().green() : "press key".localize().green();
                    Label(hint);
                    if (oldValue != null)
                        ActionButton("Clear".localize(),
                                     () => {
                                         KeyBindings.ClearBinding(oldValue.ID);
                                         selectedIdentifier = null;
                                     });
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
                    keyBind = new KeyBind(identifier, keyCode, false, false, false, false, true);
                    Mod.Trace($"    currentEvent isKey - bind: {keyBind} isModifierOnly:{keyBind.IsModifierOnly}");
                    KeyBindings.SetBinding(identifier, keyBind);
                    selectedIdentifier = null;
                    oldValue = null;
                    Input.ResetInputAxes();
                    return keyBind;
                }

                foreach (var mouseButton in allowedMouseButtons)
                    if (Input.GetKey(mouseButton)) {
                        keyBind = new KeyBind(identifier, mouseButton, isCtrlDown, isAltDown, isCmdDown, isShiftDown);
                        KeyBindings.SetBinding(identifier, keyBind);
                        selectedIdentifier = null;
                        oldValue = null;
                        Input.ResetInputAxes();
                        return keyBind;
                    }
            }
            return keyBind;
        }
        // Standalone control that lets the user set and edit a KeyBind associated with an identifier. Note you must call KeyBindings.RegisterAction to have this keybind fire an action. You also need to call KeyBindings.OnUpdate from your mod's OnUpdate delegate in order for ModKit to detect the keys and fire the action
        public static void KeyBindPicker(string? identifier, string? title, float indent = 0, float titleWidth = 0) {
            using (HorizontalScope()) {
                Space(indent);
                Label(title.bold(), titleWidth == 0 ? ExpandWidth(false) : Width(titleWidth));
                Space(25);
                EditKeyBind(identifier, true);
            }
        }
        // This is a special helper that lets you choose only modifiers like ctrl, shift, alt, etc.  This is useful for checking for modifiers on a mouse click. To check for the presence of a registered set of modifiers do this 
        // UI.KeyBindings.GetBinding(<identifier>).IsModifierActive
        public static void ModifierPicker(string? identifier, string title, float indent = 0, float titleWidth = 0) {
            using (HorizontalScope()) {
                Label(title.bold(), titleWidth == 0 ? ExpandWidth(false) : Width(titleWidth));
                Space(25);
                EditKeyBind(identifier, true, true);
            }
        }

        // One stop shopping for making controls that allow the player to bind to a key in game. Note you must call KeyBindings.RegisterAction with the title of this control to have this keybind fire an action. You also need to call KeyBindings.OnUpdate from your mod's OnUpdate delegate in order for ModKit to detect the keys and fire the action

        // Basic Action Button with HotKey
        public static void BindableActionButton(string? title, params GUILayoutOption[] options) => BindableActionButton(title, false, options);
        public static void BindableActionButton(string? title, bool shouldLocalize = false, params GUILayoutOption[] options) {
            if (options.Length == 0) options = new GUILayoutOption[] { GL.Width(300) };
            var actionEntry = KeyBindings.GetAction(title);
            if (GL.Button(shouldLocalize ? title.localize() : title, options)) actionEntry?.action();
            EditKeyBind(title, true, false, Width(200));
        }

        // Action button designed to live in a collection with a BindableActionButton
        public static void NonBindableActionButton(string? title, Action action, params GUILayoutOption[] options) {
            if (options.Length == 0) options = new GUILayoutOption[] { GL.Width(300) };
            if (GL.Button(title, options)) action();
            Space(204);
            if (Event.current.type == EventType.Layout)
                KeyBindings.RegisterAction(title, action);
        }
    }
}