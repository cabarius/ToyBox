// Copyright (c) 2018 fireundubh <fireundubh@gmail.com>
// This code is licensed under MIT license (see LICENSE for details)
// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using System;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using ModKit;

namespace ToyBox.classes.MonkeyPatchin {
    public class HighlightObjectToggle {
        [HarmonyPatch(typeof(InteractionHighlightController), "HighlightOn")]
        private class InteractionHighlightController_Activate_Patch {
            //static TimeSpan m_LastTickTime;
            private static AccessTools.FieldRef<InteractionHighlightController, bool> m_IsHighlightingRef;

            //static FastGetter<InteractionHighlightController, bool> IsHighlightingGet;
            //static FastSetter<InteractionHighlightController, bool> IsHighlightingSet;
            private static bool Prepare() {
                // Accessors.CreateFieldRef<KingdomEvent, int>("m_StartedOn");
                m_IsHighlightingRef = Accessors.CreateFieldRef<InteractionHighlightController, bool>("m_IsHighlighting");
                //IsHighlightingGet = Accessors.CreateFieldRe<InteractionHighlightController, bool>("IsHighlighting");
                //IsHighlightingSet = Accessors.CreateSetter<InteractionHighlightController, bool>("IsHighlighting");
                return true;
            }

            private static bool Prefix(InteractionHighlightController __instance, bool ___m_Inactive) {
                try {
                    if (!Main.Enabled) return true;
                    if (!Main.settings.highlightObjectsToggle) return true;
                    //var isInCutScene = Game.Instance.State.Cutscenes.ToList().Count() > 0;
                    //if (isInCutScene) return true;
                    if (m_IsHighlightingRef(__instance) & !___m_Inactive) {
                        m_IsHighlightingRef(__instance) = false;
                        foreach (var mapObjectEntityData in Game.Instance.State.MapObjects) {
                            mapObjectEntityData.View.UpdateHighlight();
                        }
                        foreach (var unitEntityData in Game.Instance.State.Units) {
                            unitEntityData.View.UpdateHighlight(false);
                        }
                        EventBus.RaiseEvent<IInteractionHighlightUIHandler>(delegate (IInteractionHighlightUIHandler h) {
                            h.HandleHighlightChange(false);
                        });
                        return false;
                    }
                }
                catch (Exception ex) {
                    Mod.Error(ex);
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(InteractionHighlightController), "HighlightOff")]
        private class InteractionHighlightController_Deactivate_Patch {
            private static bool Prefix(InteractionHighlightController __instance) {
                try {
                    if (!Main.Enabled) return true;
                    if (Main.settings.highlightObjectsToggle) {
                        return false;
                    }
                }
                catch (Exception ex) {
                    Mod.Error(ex);
                }
                return true;
            }
        }
    }
}
