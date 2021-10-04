// Copyright (c) 2018 fireundubh <fireundubh@gmail.com>
// This code is licensed under MIT license (see LICENSE for details)
// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using System;
using HarmonyLib;
using ModKit;
using Kingmaker;
using Kingmaker.Controllers.MapObjects;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.SriptZones;
using Kingmaker.View.MapObjects.Traps;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.Highlighting;
using UnityEngine;

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

    [HarmonyPatch(typeof(MapObjectView), "UpdateHighlight")]
    class HighlightHiddenObjects {
        static string ObjName = "ToyBox.HiddenHighlighter";
        static string DecalName = "ToyBox.DecalHiddenHighlighter";
        static Color HighlightColor0 = new Color(1.0f, 0.0f, 1.0f, 0.8f);
        static Color HighlightColor1 = new Color(0.0f, 0.0f, 1.0f, 1.0f);

        static void Postfix(MapObjectView __instance) {
            MapObjectEntityData data = __instance.Data;
            if(data == null) return;

            PerceptionCheckComponent pcc = __instance.GetComponent<PerceptionCheckComponent>();

            if (!data.IsPerceptionCheckPassed && pcc != null) {
                bool? is_highlighting = Game.Instance?.InteractionHighlightController?.IsHighlighting;
                bool should_highlight = (is_highlighting ?? false) && Main.settings.highlightHiddenObjects;

                if (!Main.settings.highlightHiddenObjectsInFog && data.IsInFogOfWar) {
                    should_highlight = false;
                }

                if (should_highlight) {
                    HighlightOn(__instance);
                } else {
                    HighlightOff(__instance);
                }
            } else {
                HighlightDestroy(__instance);
            }
        }

        static void HighlightCreate(MapObjectView view) {
            if (view.transform.Find(ObjName)) return;
            GameObject obj = new GameObject(ObjName);
            Main.Objects.Add(obj);
            obj.transform.parent = view.transform;
            Highlighter highlighter = obj.AddComponent<Highlighter>();

            foreach (ScriptZonePolygon polygon in view.transform.GetComponentsInChildren<ScriptZonePolygon>()) {
                MeshFilter mesh = polygon.DecalMeshObject;
                if (mesh == null) continue;

                MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
                if (renderer == null) continue;

                GameObject decal = GameObject.Instantiate(renderer.gameObject, renderer.transform.parent);
                decal.name = DecalName;
                Main.Objects.Add(decal);

                MeshRenderer decal_renderer = decal.GetComponent<MeshRenderer>();
                decal_renderer.enabled = false;
                decal_renderer.forceRenderingOff = true;
            }

            foreach (Renderer renderer in view.transform.GetComponentsInChildren<Renderer>()) {
                highlighter.AddExtraRenderer(renderer);
            }
        }

        static void HighlightDestroy(MapObjectView view) {
            GameObject decal = view?.transform?.Find(DecalName)?.gameObject;
            if (decal != null) {
                GameObject.Destroy(decal);
            }

            GameObject obj = view?.transform?.Find(ObjName)?.gameObject;
            if (obj != null) {
                GameObject.Destroy(obj);
            }
        }

        static void HighlightOn(MapObjectView view) {
            GameObject obj = view.transform.Find(ObjName)?.gameObject;
            if (obj == null) {
                HighlightCreate(view);
                obj = view.transform.Find(ObjName)?.gameObject;
            }

            Highlighter highlighter = obj.GetComponent<Highlighter>();
            if (highlighter != null) {
                highlighter.ConstantOnImmediate(HighlightColor0);
                highlighter.FlashingOn(HighlightColor0, HighlightColor1, 1.0f);

                GameObject decal = view?.transform?.Find(DecalName)?.gameObject;
                if (decal == null) return;
                MeshRenderer renderer = decal.GetComponent<MeshRenderer>();
                if (renderer == null) return;
                renderer.enabled = true;
                renderer.forceRenderingOff = true;
            }
        }

        static void HighlightOff(MapObjectView view) {
            GameObject obj = view.transform.Find(ObjName)?.gameObject;
            if (obj == null) return;

            Highlighter highlighter = obj.GetComponent<Highlighter>();
            if (highlighter != null) {
                highlighter.ConstantOff(0.0f);
                highlighter.FlashingOff();

                GameObject decal = view?.transform?.Find(DecalName)?.gameObject;
                if (decal == null) return;
                MeshRenderer renderer = decal.GetComponent<MeshRenderer>();
                if (renderer == null) return;
                renderer.enabled = false;
                renderer.forceRenderingOff = true;
            }
        }
    }
}
