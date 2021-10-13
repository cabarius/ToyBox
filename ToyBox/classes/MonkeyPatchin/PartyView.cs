using HarmonyLib;
using Kingmaker;
using Kingmaker.Assets.Code.UI.Overtip;
using Kingmaker.UI.MVVM._VM.Party;
using System;
using UnityModManager = UnityModManagerNet.UnityModManager;
using Kingmaker.PubSubSystem;
using UniRx;

namespace ToyBox.BagOfPatches {
    internal static class PartyView {
        public static Settings settings = Main.settings;
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
    }
}
