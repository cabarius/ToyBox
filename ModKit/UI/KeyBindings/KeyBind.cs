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
using System.Linq;

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
            public bool Conflicts(KeyBind kb) {
                return Key == kb.Key
                    && Ctrl == kb.Ctrl
                    && Alt == kb.Alt
                    && Cmd == kb.Cmd
                    && Shift == kb.Shift;

            }
            public override bool Equals(object o) {
                if (o is KeyBind kb) {
                    return ID == kb.ID && Conflicts(kb);
                }
                else
                    return false;
            }
            public override int GetHashCode() {
                return ID.GetHashCode() 
                    + (int)Key 
                    + (Ctrl ? 1 : 0) 
                    + (Cmd ? 1 : 0)
                    + (Shift ? 1 : 0);
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
            public string bindCode => this.ToString();
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
    }
}
