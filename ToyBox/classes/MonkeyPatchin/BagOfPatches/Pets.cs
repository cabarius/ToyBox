using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Parts;
using System;

namespace ToyBox.BagOfPatches {
    internal static class Pets {
        public static Settings settings = Main.settings;


        [HarmonyPatch(typeof(AbilityTargetIsSuitableMountSize), nameof(AbilityTargetIsSuitableMountSize.CanMount))]
        private static class AbilityTargetIsSuitableMountSize_CanMount_Patch {
            private static bool Prefix(UnitEntityData master, UnitEntityData pet, ref bool __result) {
                if (!settings.toggleMakePetsRidable) return true;
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(AbilityTargetIsSuitableMount), nameof(AbilityTargetIsSuitableMount.CanMount))]
        private static class AbilityTargetIsSuitableMount_CanMount_Patch {
            private static bool Prefix(UnitEntityData master, UnitEntityData pet, ref bool __result) {
                if (!settings.toggleRideAnything) return true;
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(UnitProgressionData), nameof(UnitProgressionData.GainMythicExperience))]
        private static class UnitProgressionData_GainMythicExperience_Patch {

            private static bool Prefix(UnitProgressionData __instance, int experience) {
                if (!settings.toggleAllowMythicPets) return true;
                if (experience < 1) {
                    PFLog.Default.Error(string.Format("Current mythic level of {0} is {1}, trying to raise to {2}! Aborting", (object)__instance.Owner, (object)__instance.MythicLevel, (object)(__instance.MythicExperience + experience)));
                }
                else {
                    __instance.MythicExperience += experience;
                    if (__instance.MythicExperience > 10)
                        PFLog.Default.Error(string.Format("Current mythic level of {0} is {1}, trying to raise to {2}! Can't do this", (object)__instance.Owner, (object)__instance.MythicLevel, (object)(__instance.MythicExperience + experience)));
                    var pair = UnitPartDualCompanion.GetPair(__instance.Owner.Unit);
                    if (pair != (UnitDescriptor)null)
                        pair.Descriptor.Progression.MythicExperience = __instance.MythicExperience;
                    EventBus.RaiseEvent<IUnitGainMythicExperienceHandler>((Action<IUnitGainMythicExperienceHandler>)(h => h.HandleUnitGainMythicExperience(__instance.Owner, experience)));
                }
                return false;
            }
        }
    }
}