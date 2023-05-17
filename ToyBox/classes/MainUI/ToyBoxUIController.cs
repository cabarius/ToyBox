using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker;
using Kingmaker.UI.SettingsUI;
using ModKit;
using UnityEngine;

namespace ToyBox {
    internal class ToyBoxUIController : MonoBehaviour {
        private static UISettingsEntityKeyBinding _followKeyBinding = null;
        private static GameObject _sharedController;
        public static void OnLoad() {
            Mod.Debug("ToyBoxUIController - Starting");
            _sharedController = new("SharedController", typeof(ToyBoxUIController));
        }
        public static void OnAreaLoad() {
            _followKeyBinding = null;
        }
        public void Awake() { }
        public void OnEnable() { }
        public void OnDestroy() {
            _followKeyBinding = null;
        }
        public void Update() {
            //Mod.Debug("ToyBoxUIController - Update");
            if (Main.Settings.toggleAutoFollowHold) {
                if (_followKeyBinding == null) {
                    var controlSettingsGroup = Game.Instance?.UISettingsManager?.m_ControlSettingsList?.First(g => g.name == "KeybindingsGeneral");
                    if (controlSettingsGroup == null)
                        return;
                    _followKeyBinding = controlSettingsGroup.SettingsList
                                                            .OfType<UISettingsEntityKeyBinding>()
                                                            .First(item => item.name == "FollowUnit");
                }
                if (_followKeyBinding?.IsDown ?? false) {
                    var selectedUnit = WrathExtensions.GetCurrentCharacter();
                    if (selectedUnit != null)
                        Game.Instance.CameraController?.Follower.Follow(selectedUnit);
                }
            }
        }
    }
}
