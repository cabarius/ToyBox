using ModKit;
using ModKit.Utility;
using System;
using System.Linq;
using UnityEngine;
using static ModKit.UI;
#nullable enable annotations

namespace ToyBox {
    public static class EnhancedCamera {
        public static Settings Settings => Main.Settings;
        internal const string? ResetAdditionalCameraAngles = "Fix Camera";
        public static void OnLoad() {
            KeyBindings.RegisterAction(ResetAdditionalCameraAngles, () => {
                Main.resetExtraCameraAngles = true;
            });
        }
        public static void ResetGUI() { }
        public static void OnGUI() {
            HStack("Camera".localize(),
                   1,
                   () => Toggle("Enable Zoom on all maps and cutscenes".localize(), ref Settings.toggleZoomOnAllMaps),
                   () => {
                       Toggle("Enable Rotate on all maps and cutscenes".localize(), ref Settings.toggleRotateOnAllMaps, 400.width());
                       153.space();
                       Label(("Note:".orange() + " For cutscenes and some situations the rotation keys are disabled so you have to hold down Mouse3 to drag in order to get rotation".green()).localize());
                   },
                   () => {
                       if (Toggle("Enable Mouse3 Dragging To Aim The Camera".localize(), ref Settings.toggleCameraPitch, 400.width())) {
                           Main.resetExtraCameraAngles = true;
                       }
                       153.space();

                       HelpLabel("This allows you to adjust pitch (Camera Tilt) by holding down Mouse3 (which previously just rotated)".localize());
                   },
                   () => {
                       Toggle("Ctrl + Mouse3 Drag To Adjust Camera Elevation".localize(), ref Settings.toggleCameraElevation);
                       25.space();
                       Toggle("Free Camera".localize(), ref Settings.toggleFreeCamera);
                   },
                   () => Label("Rotation Options".localize().cyan()),
                   () => {
                       50.space();
                       Label("Mouse:".localize().cyan(), 125.width());
                       25.space();
                       Toggle("Invert X Axis".localize(), ref Settings.toggleInvertXAxis);
                       if (Settings.toggleCameraPitch) {
                           25.space();
                           Toggle("Invert Y Axis".localize(), ref Settings.toggleInvertYAxis);
                       }
                   },
                   () => {
                       50.space();
                       Label("Keyboard:".localize().cyan(), 125.width());
                       25.space();
                       Toggle("Invert X Axis".localize(), ref Settings.toggleInvertKeyboardXAxis);
                   },
                   () => {
                       50.space();
                       BindableActionButton(ResetAdditionalCameraAngles, true);
                   },
                   () => LogSlider("Field Of View".localize(), ref Settings.fovMultiplier, 0.4f, 5.0f, 1, 2, "", AutoWidth()),
                   () => { }
                );
        }
    }

}