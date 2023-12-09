// Copyright (c) 2018 fireundubh <fireundubh@gmail.com>
// This code is licensed under MIT license (see LICENSE for details)
// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker;
using Kingmaker.Code.UI.MVVM.View.SurfaceCombat.Console;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.InputSystems;
using Kingmaker.UI.Models.SettingsUI;
using Kingmaker.Utility.GameConst;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.SriptZones;
using Kingmaker.View.MapObjects.Traps;
using ModKit;
using Owlcat.Runtime.Visual.Highlighting;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.Highlighting;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ToyBox.classes.MonkeyPatchin {
    public class HighlightObjectToggle {
        [HarmonyPatch(typeof(KeyboardAccess))]
        private static class KeyboardAccess_Patch {
            public static bool justChanged = false;
            [HarmonyPatch(nameof(KeyboardAccess.OnCallbackByBinding))]
            [HarmonyPrefix]
            private static bool OnCallbackByBinding(KeyboardAccess.Binding binding) {
                if (!Main.Settings.highlightObjectsToggle) return true;
                if (Game.Instance?.Player?.IsInCombat ?? false) return true;
                if (binding.Name.StartsWith(UISettingsRoot.Instance.UIKeybindGeneralSettings.HighlightObjects.name)) {
                    if (binding.Name.EndsWith(UIConsts.SuffixOn) && binding.InputMatched() && !justChanged) {
                        justChanged = true;
                        InteractionHighlightController.Instance?.Highlight(!InteractionHighlightController.Instance?.IsHighlighting ?? false);
                        Task.Run(() => {
                            Thread.Sleep(250);
                            justChanged = false;
                        });
                    }
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Game))]
        private static class Game_Patch {
            private static HashSet<GameModeType> turnOffWhen = null;
            private static bool wasTurnedOffBefore = false;
            internal static bool wasTurnedOff = false;
            [HarmonyPatch(nameof(Game.DoStartMode))]
            [HarmonyPrefix]
            private static void DoStartMode(GameModeType type) {
                if (!Main.Settings.highlightObjectsToggle) return;
                if (Game.Instance.Player.IsInCombat) return;
                if (turnOffWhen == null) {
                    turnOffWhen = new() {
                        GameModeType.Dialog,
                        GameModeType.Cutscene
                    };
                }
                if (turnOffWhen.Contains(type)) {
                    if (InteractionHighlightController.Instance?.IsHighlighting ?? false) {
                        wasTurnedOffBefore = true;
                        wasTurnedOff = true;
                        InteractionHighlightController.Instance.HighlightOff();
                        wasTurnedOff = false;
                    }
                } else {
                    if (wasTurnedOffBefore && (!InteractionHighlightController.Instance?.IsHighlighting ?? false)) {
                        InteractionHighlightController.Instance.HighlightOn();
                        wasTurnedOffBefore = false;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Player))]
        private static class Player_Patch {
            private static bool interestingTick = false;
            private static bool wasOnBeforeFightIntern = false;
            internal static bool wasOnBeforeFight = false;
            [HarmonyPatch(nameof(Player.IsInCombat), MethodType.Setter)]
            [HarmonyPrefix]
            private static void setIsInCombatPre(bool value) {
                if (!Main.Settings.highlightObjectsToggle) return;
                interestingTick = value != Game.Instance.Player.IsInCombat;
                if (!interestingTick) return;
                if (InteractionHighlightController.Instance.IsHighlighting && value) {
                    wasOnBeforeFightIntern = true;
                    wasOnBeforeFight = true;
                    InteractionHighlightController.Instance.HighlightOff();
                    wasOnBeforeFight = false;
                }
            }
            [HarmonyPatch(nameof(Player.IsInCombat), MethodType.Setter)]
            [HarmonyPostfix]
            private static void SetIsInCombatPost(bool value) {
                if (!Main.Settings.highlightObjectsToggle) return;
                if (!interestingTick) return;
                if (wasOnBeforeFightIntern && !value) {
                    wasOnBeforeFightIntern = false;
                    InteractionHighlightController.Instance.HighlightOn();
                }
                interestingTick = false;
            }
        }
        [HarmonyPatch(typeof(InteractionHighlightController))]
        private static class InteractionHighlightController_Patch {
            [HarmonyPatch(nameof(InteractionHighlightController.HighlightOff))]
            [HarmonyPrefix]
            private static bool HighlightOff() {
                if (!Main.Settings.highlightObjectsToggle) return true;
                if (Game.Instance.Player.IsInCombat) return true;
                if (!Player_Patch.wasOnBeforeFight && !Game_Patch.wasTurnedOff && !KeyboardAccess_Patch.justChanged) {
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(MapObjectView))]
        private static class MapObjectView_Patch {
            [HarmonyPatch(nameof(MapObjectView.ShouldBeHighlighted))]
            [HarmonyPostfix]
            private static void UpdateHighlight(MapObjectView __instance, ref bool __result) {
                if (__instance == null) return;
                bool flag = __instance.Highlighted || __instance.m_ForcedHighlightOnReveal || (__instance.GlobalHighlighting && (!__instance.Data.IsInFogOfWar || Main.Settings.highlightHiddenObjectsInFog));
                if (Game.Instance.TurnController.TurnBasedModeActive) {
                    if (__instance.Data.Parts.GetAll<InteractionPart>().Any((InteractionPart i) => i is InteractionLootPart)) {
                        flag = false;
                    }
                }
                if (!flag || !__instance.HighlightOnHover || (!__instance.Data.IsRevealed && !Main.Settings.highlightHiddenObjects) || !__instance.Data.IsAwarenessCheckPassed) {
                    __result = __instance.Data.Parts.GetAll<InteractionPart>().Any((InteractionPart i) => i.HasVisibleTrap());
                }
                else {
                    __result = true;
                }
            }
        }
    }
}