﻿using UnityEngine;
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
using System.Linq;

namespace ModKit {
    public static partial class UI {
        private static string selectedIdentifier = null;
        private static KeyBind oldValue = null;
        public static KeyBind EditKeyBind(string identifier, bool showHint = true, params GUILayoutOption[] options) {
            if (Event.current.type == EventType.Layout)
                KeyBindings.OnGUI();
            var keyBind = KeyBindings.GetBinding(identifier);
            bool isEditing = identifier == selectedIdentifier;
            bool isEditingOther = selectedIdentifier != null && identifier != selectedIdentifier && oldValue != null;
            string label = keyBind.IsEmpty ? (isEditing ? "Cancel" : "Bind") : keyBind.ToString().orange().bold();
            showHint = showHint && isEditing;
            var conflicts = keyBind.Conflicts();
            using (VerticalScope(options)) {
                Space(UnityModManager.UI.Scale(3));
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

        // One stop shopping for making an instant button that you want to let a player bind to a key in game
        public static void BindableActionButton(string title, params GUILayoutOption[] options) {
            if (options.Length == 0) { options = new GUILayoutOption[] { GL.Width(300) }; }
            var action = KeyBindings.GetAction(title);
            if (GL.Button(title, options)) { action(); }
            EditKeyBind(title, true, Width(200));
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
