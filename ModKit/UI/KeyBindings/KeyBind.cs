using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ModKit {
    public static partial class UI {
        // Contains low level classes that describe a key binding as a KeyCode and modifiers that are associated with an identifier. These can be written out to a file.
        public enum ClickModifier {
            Disabled,
            Shift,
            Ctrl,
            Alt,
            Command
        }

        public static bool IsActive(this ClickModifier modifier) => modifier switch {
            ClickModifier.Disabled => false,
            ClickModifier.Shift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
            ClickModifier.Ctrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
            ClickModifier.Alt => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt),
            ClickModifier.Command => Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand),
            _ => false
        };

        private static readonly HashSet<KeyCode> allowedMouseButtons = new() { KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };
        public static bool IsModifier(this KeyCode code) =>
            code == KeyCode.LeftControl
            || code == KeyCode.RightControl
            || code == KeyCode.LeftAlt
            || code == KeyCode.RightAlt
            || code == KeyCode.LeftShift
            || code == KeyCode.RightShift
            || code == KeyCode.LeftCommand
            || code == KeyCode.RightCommand;
        public static bool IsControl(this KeyCode code) => code == KeyCode.LeftControl || code == KeyCode.RightControl;

        public static bool IsAlt(this KeyCode code) => code == KeyCode.LeftAlt || code == KeyCode.RightAlt;

        public static bool IsCommand(this KeyCode code) => code == KeyCode.LeftCommand || code == KeyCode.RightCommand;

        public static bool IsShift(this KeyCode code) => code == KeyCode.LeftShift || code == KeyCode.RightShift;

        private static GUIStyle _hotkeyStyle;

        public static GUIStyle hotkeyStyle {
            get {
                if (_hotkeyStyle == null)
                    _hotkeyStyle = new GUIStyle(GUI.skin.textArea) {
                        margin = new RectOffset(3, 3, 3, 3),
                        richText = true
                    };
                _hotkeyStyle.fontSize = 11.point();
                _hotkeyStyle.fixedHeight = 17.point();

                return _hotkeyStyle;
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class KeyBind {
            [JsonProperty] public string? ID;
            [JsonProperty] public KeyCode Key;
            [JsonProperty] public bool Ctrl;
            [JsonProperty] public bool Alt;
            [JsonProperty] public bool Cmd;
            [JsonProperty] public bool Shift;
            [JsonProperty] public bool IsModifierOnly;
            public KeyBind(string? identifer, KeyCode key = KeyCode.None, bool ctrl = false, bool alt = false, bool cmd = false, bool shift = false, bool isModifierOnly = false) {
                ID = identifer;
                Key = key;
                Ctrl = ctrl;
                Alt = alt;
                Cmd = cmd;
                Shift = shift;
                IsModifierOnly = isModifierOnly;
            }
            public bool Conflicts(KeyBind kb) {
                Mod.Log($"kb: {this} {IsModifierOnly} vs {kb} {kb.IsModifierOnly}");
                if (IsModifierOnly || kb.IsModifierOnly) return false;
                return Key == kb.Key
                       && Ctrl == kb.Ctrl
                       && Alt == kb.Alt
                       && Cmd == kb.Cmd
                       && Shift == kb.Shift;
            }
            public override bool Equals(object o) {
                if (o is KeyBind kb)
                    return ID == kb.ID && Conflicts(kb);
                else
                    return false;
            }
            public override int GetHashCode() =>
                ID.GetHashCode()
                + (int)Key
                + (Ctrl ? 1 : 0)
                + (Cmd ? 1 : 0)
                + (Shift ? 1 : 0);

            [JsonIgnore] public bool IsEmpty => Key == KeyCode.None;

            [JsonIgnore]
            public bool IsKeyCodeActive {
                get {
                    if (Key == KeyCode.None) return false;
                    if (allowedMouseButtons.Contains(Key)) return Input.GetKey(Key);
                    var active = Key == Event.current.keyCode;
                    return active;
                }
            }

            [JsonIgnore]
            public bool IsActive {
                get {
                    if (Event.current == null) return false;
                    if (!IsKeyCodeActive) return false;
                    var ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                    var altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                    var cmdDown = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
                    var shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    // note we already checked Key above
                    var active = ctrlDown == Ctrl
                                 && altDown == Alt
                                 && cmdDown == Cmd
                                 && shiftDown == Shift;
                    return active;
                }
            }

            public bool IsModifierActive {
                get {
                    if (Event.current == null) return false;
                    return Input.GetKey(Key);
                }
            }

            public string bindCode => ToString();
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