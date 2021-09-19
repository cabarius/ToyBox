using UnityEngine;
using Kingmaker;
using Kingmaker.UI;
using Kingmaker.Settings;
using System;
using System.Collections.Generic;
using GL = UnityEngine.GUILayout;
using ModKit;
using ModKit.Utility;

namespace ToyBox {

    static class Keybinding {
        static private KeyCode[] mouseButtonsValid = { KeyCode.Mouse3, KeyCode.Mouse4, KeyCode.Mouse5, KeyCode.Mouse6 };
        public static void SetKeyBinding(string title, ref KeyCode keyCode, float indent = 0, float titleWidth = 0) {
            string label = (keyCode == KeyCode.None) ? "Press Key" : keyCode.ToString();
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                UI.Label(title.bold(), titleWidth == 0 ? UI.ExpandWidth(false) : UI.Width(titleWidth));
                UI.Space(25);
                if (GL.Button(label, UI.Width(200))) {
                    keyCode = KeyCode.None;
                }
                if (keyCode != KeyCode.None) {
                    UI.Space(25);
                    UI.Label("press to reassign".green());
                }
                if (keyCode == KeyCode.None && Event.current != null) {
                    if (Event.current.isKey) {
                        keyCode = Event.current.keyCode;
                        Input.ResetInputAxes();
                    }
                    else {
                        foreach (KeyCode mouseButton in mouseButtonsValid) {
                            if (Input.GetKey(mouseButton)) {
                                keyCode = mouseButton;
                                Input.ResetInputAxes();
                            }
                        }
                    }
                }
            }
        }
    }
}
