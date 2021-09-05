// Copyright (c) 2018 fireundubh <fireundubh@gmail.com>
// This code is licensed under MIT license (see LICENSE for details)
// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using System;

namespace ToyBox.classes.MonkeyPatchin {
    public class HighlightObjectToggle {
        [HarmonyPatch(typeof(InteractionHighlightController), "HighlightOn")]
        class InteractionHighlightController_Activate_Patch {
            static AccessTools.FieldRef<InteractionHighlightController, bool> m_IsHighlightingRef;

            static bool Prepare() {
                m_IsHighlightingRef = Accessors.CreateFieldRef<InteractionHighlightController, bool>("m_IsHighlighting");

                return true;
            }

            static bool Prefix(InteractionHighlightController __instance, bool ___m_Inactive) {
                try {
                    if (!Main.Enabled) {
                        return true;
                    }

                    if (!Main.settings.highlightObjectsToggle) {
                        return true;
                    }

                    if (m_IsHighlightingRef(__instance) & !___m_Inactive) {
                        m_IsHighlightingRef(__instance) = false;

                        foreach (MapObjectEntityData mapObjectEntityData in Game.Instance.State.MapObjects) {
                            mapObjectEntityData.View.UpdateHighlight();
                        }

                        foreach (UnitEntityData unitEntityData in Game.Instance.State.Units) {
                            unitEntityData.View.UpdateHighlight(false);
                        }

                        EventBus.RaiseEvent((IInteractionHighlightUIHandler h) => h.HandleHighlightChange(false));

                        return false;
                    }
                }
                catch (Exception ex) {
                    Main.Error(ex);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(InteractionHighlightController), "HighlightOff")]
        class InteractionHighlightController_Deactivate_Patch {
            static bool Prefix(InteractionHighlightController __instance) {
                try {
                    if (!Main.Enabled) {
                        return true;
                    }

                    if (Main.settings.highlightObjectsToggle) {
                        return false;
                    }
                }
                catch (Exception ex) {
                    Main.Error(ex);
                }

                return true;
            }
        }
    }
}