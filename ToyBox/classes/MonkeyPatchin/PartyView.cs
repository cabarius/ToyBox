using HarmonyLib;
using Kingmaker;
using Kingmaker.Assets.Code.UI.Overtip;
using Kingmaker.UI.MVVM._VM.Party;
using System;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.PubSubSystem;
using UniRx;
using Kingmaker.UI.MVVM._PCView.Party;
using Kingmaker.UI;
using Kingmaker.Blueprints.Root;
using Owlcat.Runtime.UI.Controls.Selectable;
using Owlcat.Runtime.UI.Controls.Other;
using Owlcat.Runtime.UI.Controls.Button;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using ModKit;
using UnityEngine.UI;

namespace ToyBox.BagOfPatches {
    internal static class PartyView {
        public static Settings settings => Main.Settings;
        public static Player player = Game.Instance.Player;

#if false
        static int SlotsInParty = 8;
        [HarmonyPatch(typeof(PartyVM), nameof(PartyVM.SetGroup))]
        public static class PartyVM_SetGroup_Patch {
            public static bool Prefix(ref PartyVM __instance) {
                Main.Log($"PartyVM_SetGroup_Patch");
                //for (int index = 6;index < SlotsInParty; ++index) {
                //    Main.Log($"PartyVM_SetGroup_Patch - adding slot: {index}");
                //    __instance.CharactersVM.Add(new PartyCharacterVM(new Action<bool>(__instance.NextPrev), index));
                //}

                __instance.m_ActualGroup = Game.Instance.SelectionCharacter.ActualGroup;
                __instance.StartIndex = 0;
                for (int index = 0;index < __instance.m_ActualGroup.Count;++index) {
                    if (__instance.FullCharactersVM.Count == index) {
                        __instance.FullCharactersVM.Add(new PartyCharacterVM(new Action<bool>(__instance.NextPrev), index));
                        __instance.CharactersVM.Add(new PartyCharacterVM(new Action<bool>(__instance.NextPrev), index));
                        Main.Log($"PartyVM_SetGroup_Patch - adding slot: {index}");
                    }
                    __instance.FullCharactersVM[index].SetUnitData(__instance.m_ActualGroup[index]);
                }
                while (__instance.FullCharactersVM.Count > __instance.m_ActualGroup.Count) {
                    Main.Log($"PartyVM_SetGroup_Patch - removing slot: {__instance.FullCharactersVM.Count - 1}");

                    PartyCharacterVM partyCharacterVm = __instance.FullCharactersVM[__instance.FullCharactersVM.Count - 1];
                    partyCharacterVm.Dispose();
                    __instance.FullCharactersVM.Remove(partyCharacterVm);
                }
                return false;
            }
        }
#endif

#if false
        public static class PartyVM_Construtor_Patch {
            public static void Postfix(PartyVM __instance) {
                Game.Instance.UI.PartyBarkManager = (Kingmaker.Assets.Code.UI.Overtip.IBarkPlayer)__instance;
                for (int index = 6; index < SlotsInParty; ++index)
                    __instance.CharactersVM.Add(new PartyCharacterVM(new Action<bool>(__instance.NextPrev), index));
                __instance.SetGroup();
            }
        }

        [HarmonyPatch(typeof(PartyVM), MethodType.Constructor)]
        public static class PartyVM_Construtor_Patch {
            public static bool Prefix(PartyVM __instance) {
                __instance.AddDisposable(EventBus.Subscribe((object)__instance));
                Game.Instance.UI.PartyBarkManager = (IBarkPlayer)__instance;
                for (int index = 0;index < 6;++index)
                    __instance.CharactersVM.Add(new PartyCharacterVM(new Action<bool>(__instance.NextPrev), index));
                __instance.AddDisposable(Game.Instance.SelectionCharacter.SelectionCharacterUpdated.Subscribe<UniRx.Unit>((Action<UniRx.Unit>)(_ => __instance.SetGroup())));
                __instance.SetGroup();
                return false;
            }
        }

        [HarmonyPatch(typeof(PartyVM), nameof(PartyVM.StartIndex) , MethodType.Setter)]
        public static class PartyVM_StartIndex_Patch {
            public static bool Prefix(ref PartyVM __instance, int value) {
                int max = __instance.m_ActualGroup.Count - SlotsInParty;
                if (max < 0)
                    max = 0;
                value = Mathf.Clamp(value, 0, max);
                __instance.m_StartIndex = value;
                __instance.PrevEnable.Value = value > 0;
                __instance.NextEnable.Value = __instance.m_ActualGroup.Count > __instance.m_StartIndex + SlotsInParty;
                for (int index1 = 0; index1 < __instance.CharactersVM.Count; ++index1) {
                    int index2 = __instance.m_StartIndex + index1;
                    __instance.CharactersVM[index1].SetUnitData(__instance.m_ActualGroup.Count > index2 ? __instance.m_ActualGroup[index2] : (UnitEntityData)null);
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(PartyVM), nameof(PartyVM.SetGroup))]
        public static class PartyVM_SetGroup_Patch {
            public static bool Prefix(ref PartyVM __instance) {
                __instance.m_ActualGroup = Game.Instance.SelectionCharacter.ActualGroup;
                __instance.StartIndex = 0;
                for (int index = 0; index < __instance.m_ActualGroup.Count; ++index) {
                    if (__instance.FullCharactersVM.Count == index) {
                        __instance.FullCharactersVM.Add(new PartyCharacterVM(new Action<bool>(__instance.NextPrev), index));
                    }
                    __instance.FullCharactersVM[index].SetUnitData(__instance.m_ActualGroup[index]);
                }
                while (__instance.FullCharactersVM.Count > __instance.m_ActualGroup.Count) {
                    PartyCharacterVM partyCharacterVm = __instance.FullCharactersVM[__instance.FullCharactersVM.Count - 1];
                    partyCharacterVm.Dispose();
                    __instance.FullCharactersVM.Remove(partyCharacterVm);
                }
                __instance.CharactersVM.Clear();
                foreach (var ch in __instance.FullCharactersVM) {
                    __instance.CharactersVM.Add(ch);
                }
                Main.Log($"after: Characters: {__instance.CharactersVM.Count()}");
                Main.Log($"after: FullCharacters: {__instance.FullCharactersVM.Count()}");
                return true;
            }
        }
#endif
#if false
        [HarmonyPatch(typeof(PartyPCView))]
        internal static class PartyPCView_Patches {
            private static Sprite hudBackground8;
            private static int leadingEdge;
            private static float itemWidth;
            private static float firstX;
            private static float scale;

            [HarmonyPatch("Initialize"), HarmonyPrefix]
            static void Initialize(PartyPCView __instance) {
                if (PartyVM_Patches.SupportedSlots == 6)
                    return;

                try {


                    Mod.Log("INSTALLING BUBBLE GROUP PANEL");

                    if (hudBackground8 == null) {
                        hudBackground8 = AssetLoader.LoadInternal("sprites", "UI_HudBackgroundCharacter_8.png", new Vector2Int(1746, 298));
                    }

                    __instance.transform.Find("Background").GetComponent<Image>().sprite = hudBackground8;

                    scale = 6 / (float)8;
                    itemWidth = 94.5f;
                    __instance.m_Shift = itemWidth;

                    var currentViews = __instance.GetComponentsInChildren<PartyCharacterPCView>(true);
                    List<GameObject> toTweak = new(currentViews.Select(view => view.gameObject));
                    firstX = toTweak[0].transform.localPosition.x - 9.5f;

                    UpdateCharacterBindings(__instance);

                }
                catch (Exception e) {
                    Mod.Error(e);
                }
            }

            [HarmonyPatch(nameof(PartyPCView.UpdateCharacterBindings)), HarmonyPostfix]
            static void UpdateCharacterBindings(PartyPCView __instance) {
                if (PartyVM_Patches.SupportedSlots == 6) return;

                var currentViews = __instance.GetComponentsInChildren<PartyCharacterPCView>(true);
                List<GameObject> toTweak = new(currentViews.Select(view => view.gameObject));
                firstX = toTweak[0].transform.localPosition.x - 9.5f;

                for (int i = 0; i < toTweak.Count; i++) {
                    GameObject view = toTweak[i];

                    TweakPCView(__instance, i, view);
                }
            }

            private static void TweakPCView(PartyPCView __instance, int i, GameObject view) {
                var viewRect = view.transform as RectTransform;

                if (viewRect.localScale.x <= (scale + 0.01f)) return;

                var pos = viewRect.localPosition;
                pos.x = firstX + (i * itemWidth);
                viewRect.localPosition = pos;
                viewRect.localScale = new Vector3(scale, scale, 1);

                var portraitRect = view.transform.Find("Portrait") as RectTransform;
                const float recaleFactor = 1.25f;
                portraitRect.localScale = new Vector3(recaleFactor, recaleFactor, 1);

                var frameRect = view.transform.Find("Frame") as RectTransform;
                frameRect.pivot = new Vector2(.5f, 1);
                frameRect.anchoredPosition = new Vector2(0, 23);
                frameRect.sizeDelta = new Vector2(0, 47);

                var healthBarRect = view.transform.Find("Health") as RectTransform;
                healthBarRect.pivot = new Vector2(0, 1);
                healthBarRect.anchoredPosition = new Vector2(0, -2);
                healthBarRect.anchorMin = new Vector2(0, 1);
                healthBarRect.anchorMax = new Vector2(0, 1);
                healthBarRect.localScale = new Vector2(recaleFactor, recaleFactor);

                var hitpointRect = view.transform.Find("HitPoint") as RectTransform;
                var hpPos = hitpointRect.anchoredPosition;
                hpPos.y -= 20;
                hitpointRect.anchoredPosition = hpPos;

                view.transform.Find("PartBuffView").gameObject.SetActive(false);

                (view.transform.Find("Frame/Selected/Mark") as RectTransform).anchoredPosition = new Vector2(0, 94);

                var buffRect = view.transform.Find("BuffMain") as RectTransform;

                buffRect.sizeDelta = new Vector2(-8, 24);
                buffRect.pivot = new Vector2(0, 0);
                buffRect.anchorMin = new Vector2(0, 1);
                buffRect.anchorMax = new Vector2(1, 1);
                buffRect.anchoredPosition = new Vector2(4, -4);
                buffRect.Edit<GridLayoutGroupWorkaround>(g => {
                    g.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    g.padding.top = 2;
                });
                buffRect.gameObject.AddComponent<Image>().color = new Color(.05f, .05f, .05f);

                var buffHover = buffRect.Find("BuffTriggerNotification").GetComponent<OwlcatSelectable>();

                __instance.AddDisposable(buffHover.OnHoverAsObservable().Subscribe<bool>(selected => {
                    viewRect.SetAsLastSibling();
                }));

                buffRect.Find("BuffTriggerNotification/BuffAdditional/").localScale = new Vector2(1.25f, 1.25f);
            }
        }

        //[HarmonyPatch(typeof(UnitBuffPartPCView))]
        //static class UnitBuffPartPCView_Patches {

        //    private static HashSet<Guid> Dreamwork = new() {
        //        // Teamwork buffs
        //        Guid.Parse("44569e9e95364bf42b1071382a8a89da"),
        //        Guid.Parse("a6298b0f87fc7694086cd8eac9d6a2aa"),
        //        Guid.Parse("cc26546e4f73fe142b606b4759b4eb18"),
        //        Guid.Parse("e5079510480031146992dafde835c3b8"),
        //        Guid.Parse("3de0359d9480cb549ab6cf1eac51f9dc"),
        //        Guid.Parse("2f5768f642de59f40acd5211a627a237"),
        //        Guid.Parse("965ea9716b87f4b46a6a8f50523393bd"),
        //        Guid.Parse("693964e674883e74b8d0005dbf4a4e6b"),
        //        Guid.Parse("731a11dcc952e744f8a88768e07a0542"),
        //        Guid.Parse("953c3dbda63dcdb4aad6c54c1a4590d0"),
        //        Guid.Parse("9c179de4894c295499822714878f3590"),
        //        Guid.Parse("c7223802e54e8524c8b1e5c71df22f7b"),

        //        // Toggles (power attack, rapid shot)
        //        Guid.Parse("0f310c1e709e15e4fa693db15a4baeb4"),
        //        Guid.Parse("f958ef62eea5050418fb92dfa944c631"),
        //        Guid.Parse("8af258b1dd322874ba6047b0c24660c7"),
        //        Guid.Parse("bf3b19ed9c919464aa2a741271718542"),

        //    };


        //    [HarmonyPatch("DrawBuffs"), HarmonyPostfix]
        //    static void DrawBuffs(UnitBuffPartPCView __instance) {
        //        try {
        //            if (PartyVM_Patches.SupportedSlots == 6)
        //                return;

        //            if (__instance.ViewModel.Buffs.Count <= 6)
        //                return;

        //            var main = __instance.m_MainContainer.transform;
        //            var overflow = __instance.m_AdditionalContainer.transform;

        //            int[] badButShown = new int[5];
        //            int[] goodButHidden = new int[5];
        //            int nextShown = 0;
        //            int nextHidden = 0;

        //            for (int i = 0; i < __instance.m_BuffList.Count; i++) {
        //                var buff = __instance.m_BuffList[i].ViewModel.Buff;
        //                if (nextShown < 5 && Dreamwork.Contains(buff.Blueprint.AssetGuid.m_Guid)) {
        //                    badButShown[nextShown++] = i;
        //                } else if (nextHidden < 5 && __instance.m_BuffList[i].transform.parent == overflow) {
        //                    goodButHidden[nextHidden++] = i;
        //                }
        //            }

        //            if (nextHidden == 0 || nextShown == 0)
        //                return;

        //            Vector3 overflowScale = __instance.m_BuffList[goodButHidden[0]].transform.localScale;
        //            Vector3 mainScale = __instance.m_BuffList[badButShown[0]].transform.localScale;


        //            while (nextHidden > 0 && nextShown > 0) {
        //                nextHidden--;
        //                nextShown--;

        //                __instance.m_BuffList[badButShown[nextShown]].transform.SetParent(overflow);
        //                __instance.m_BuffList[badButShown[nextShown]].transform.localScale = overflowScale;
        //                __instance.m_BuffList[goodButHidden[nextHidden]].transform.SetParent(main);
        //                __instance.m_BuffList[goodButHidden[nextHidden]].transform.localScale = mainScale;
        //            }

        //            while (main.childCount > 6) {
        //                var toSwap = main.transform.GetChild(6);
        //                toSwap.transform.SetParent(overflow);
        //                toSwap.localScale = overflowScale;
        //            }
        //        } catch (Exception ex) {
        //            Main.Error(ex, "buffling");
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(PartyVM))]
        public static class PartyVM_Patches {

            public static int SupportedSlots = 6;

            private static int WantedSlots = 6;

            //[HarmonyTranspiler]
            //[HarmonyPatch("set_StartIndex")]
            static IEnumerable<CodeInstruction> UpdateStartValue(IEnumerable<CodeInstruction> instructions) {
                return ConvertConstants(instructions, 8);
            }

            //[HarmonyTranspiler]
            //[HarmonyPatch(MethodType.Constructor)]
            static IEnumerable<CodeInstruction> _ctor(IEnumerable<CodeInstruction> instructions) {
                return ConvertConstants(instructions, 8);
            }

            private static OpCode[] LdConstants = {
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8,
        };

            private static IEnumerable<CodeInstruction> ConvertConstants(IEnumerable<CodeInstruction> instructions, int to) {
                Func<CodeInstruction> makeReplacement;
                if (to <= 8)
                    makeReplacement = () => new CodeInstruction(LdConstants[to]);
                else
                    makeReplacement = () => new CodeInstruction(OpCodes.Ldc_I4_S, to);

                foreach (var ins in instructions) {
                    if (ins.opcode == OpCodes.Ldc_I4_6)
                        yield return makeReplacement();
                    else
                        yield return ins;
                }
            }


            private static readonly BubblePatch Patch_ctor = BubblePatch.FirstConstructor(typeof(PartyVM), typeof(PartyVM_Patches));
            private static readonly BubblePatch Patch_set_StartIndex = BubblePatch.Method(typeof(PartyVM), typeof(PartyVM_Patches), "UpdateStartValue");

            public static void Repatch() {
                if (settings.toggleExpandedPartyView)
                    WantedSlots = 8;
                else
                    WantedSlots = 6;

                if (SupportedSlots == WantedSlots)
                    return;

                Patch_ctor.Revert();
                Patch_set_StartIndex.Revert();

                if (WantedSlots == 6)
                    return;

                SupportedSlots = 8;

                Patch_ctor.Apply();
                Patch_set_StartIndex.Apply();
            }
        }

        public class BubblePatch {
            public MethodBase Original;
            public HarmonyMethod Patch;
            public bool IsPatched = false;

            public BubblePatch(MethodBase target, Type patcher, string name) {
                Patch = new HarmonyMethod(patcher, name);
                Original = target;
            }

            public static BubblePatch FirstConstructor(Type target, Type patcher) {
                return new BubblePatch(target.GetConstructors().First(), patcher, "_ctor");
            }
            public static BubblePatch Setter(Type target, Type patcher, string propertyName) {
                return new BubblePatch(target.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetMethod, patcher, $"set_{propertyName}");
            }
            public static BubblePatch Getter(Type target, Type patcher, string propertyName) {
                return new BubblePatch(target.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetMethod, patcher, $"get_{propertyName}");
            }
            public static BubblePatch Method(Type target, Type patcher, string name) {
                return new BubblePatch(target.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), patcher, name);
            }

            public void Revert() {
                if (!IsPatched)
                    return;

                Main.HarmonyInstance.Unpatch(Original, Patch.method);
                IsPatched = false;
            }
            public void Apply() {
                if (IsPatched)
                    throw new Exception("Trying to apply a patch that is already applied");
                Main.HarmonyInstance.Patch(Original, null, null, Patch, null);
                IsPatched = true;
            }
        }
#endif
    }
}
