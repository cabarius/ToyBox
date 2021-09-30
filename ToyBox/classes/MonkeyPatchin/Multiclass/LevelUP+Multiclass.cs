// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
//using Kingmaker.UI.LevelUp.Phase;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox.Multiclass {
    internal static class LevelUp {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;
        public static UnityModManager.ModEntry.ModLogger modLogger = ModKit.Logger.modLogger;

        [HarmonyPatch(typeof(LevelUpController), MethodType.Constructor, new Type[] {
            typeof(UnitEntityData),
            typeof(bool),
            typeof(LevelUpState.CharBuildMode) })]
        static class LevelUpController_ctor_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            static bool Prefix(LevelUpController __instance) {
                if (Main.Enabled) {
                    MultipleClasses.levelUpController = __instance;
                }
                return true;
            }
        }
#if true
        /*     public static void UpdateProgression(
                                    [NotNull] LevelUpState state,
                                    [NotNull] UnitDescriptor unit,
                                    [NotNull] BlueprintProgression progression)
        */
        [HarmonyPatch(typeof(LevelUpHelper), "UpdateProgression")]
        [HarmonyPatch(new Type[] { typeof(LevelUpState), typeof(UnitDescriptor), typeof(BlueprintProgression) })]
        static class LevelUpHelper_UpdateProgression_Patch {
            public static bool Prefix([NotNull] LevelUpState state, [NotNull] UnitDescriptor unit, [NotNull] BlueprintProgression progression) {
                if (!settings.toggleMulticlass) return true;
                ProgressionData progressionData = unit.Progression.SureProgressionData(progression);
                int level = progressionData.Level;
                int nextLevel = progressionData.Blueprint.CalcLevel(unit);
                progressionData.Level = nextLevel;
                // TODO - this is from the mod but we need to figure out if max level 20 still makes sense with mythic levels
                // int maxLevel = 20 // unit.Progression.CharacterLevel;
                // if (nextLevel > maxLevel)
                //     nextLevel = maxLevel;
                if (level >= nextLevel || progression.ExclusiveProgression != null && state.SelectedClass != progression.ExclusiveProgression)
                    return false;
                if (!progression.GiveFeaturesForPreviousLevels)
                    level = nextLevel - 1;
                for (int lvl = level + 1; lvl <= nextLevel; ++lvl) {
                    //                    if (!AllowProceed(progression)) break;
                    LevelEntry levelEntry = progressionData.GetLevelEntry(lvl);
                    LevelUpHelper.AddFeaturesFromProgression(state, unit, levelEntry.Features, (FeatureSource)progression, lvl);
                }
                return false;
            }
            private static bool AllowProceed(BlueprintProgression progression) {
                // SpellSpecializationProgression << shouldn't be applied more than once per character level
                if (!Main.Enabled || Main.multiclassMod == null) return false;
                return Main.multiclassMod.UpdatedProgressions.Add(progression);
                // TODO - what is the following and does it still matter?
                // || progression.AssetGuid != "fe9220cdc16e5f444a84d85d5fa8e3d5";
            }
        }
#endif

        // Do not proceed the spell selection if the caster level was not changed
        [HarmonyPatch(typeof(ApplySpellbook), "Apply")]
        [HarmonyPatch(new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        static class ApplySpellbook_Apply_Patch {
            public static bool Prefix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return true;
                if (state.SelectedClass == null)
                    return false;
                SkipLevelsForSpellProgression component1 = state.SelectedClass.GetComponent<SkipLevelsForSpellProgression>();
                if (component1 != null && ((IEnumerable<int>)component1.Levels).Contains<int>(state.NextClassLevel))
                    return false;
                ClassData classData = unit.Progression.GetClassData(state.SelectedClass);
                if (classData == null || !(classData.Spellbook != null))
                    return false;
                Spellbook spellbook1 = unit.DemandSpellbook(classData.Spellbook);
                if ((bool)state.SelectedClass.Spellbook && state.SelectedClass.Spellbook != classData.Spellbook) {
                    Spellbook spellbook2 = unit.Spellbooks.FirstOrDefault<Spellbook>((Func<Spellbook, bool>)(s => s.Blueprint == state.SelectedClass.Spellbook));
                    if (spellbook2 != null) {
                        foreach (AbilityData allKnownSpell in spellbook2.GetAllKnownSpells())
                            spellbook1.AddKnown(allKnownSpell.SpellLevel, allKnownSpell.Blueprint);
                        unit.DeleteSpellbook(state.SelectedClass.Spellbook);
                    }
                }
                int casterLevelBefore = spellbook1.CasterLevel;
                spellbook1.AddLevelFromClass(classData.CharacterClass);
                int casterLevelAfter = spellbook1.CasterLevel;
                if (casterLevelBefore == casterLevelAfter) return false; // Mod line
                SpellSelectionData spellSelectionData = state.DemandSpellSelection(spellbook1.Blueprint, spellbook1.Blueprint.SpellList);
                if (spellbook1.Blueprint.SpellsKnown != null) {
                    for (int index = 0; index <= 10; ++index) {
                        BlueprintSpellsTable spellsKnown = spellbook1.Blueprint.SpellsKnown;
                        int? count = spellsKnown.GetCount(casterLevelBefore, index);
                        int num1 = count ?? 0;
                        count = spellsKnown.GetCount(casterLevelAfter, index);
                        int num2 = count ?? 0;
                        spellSelectionData.SetLevelSpells(index, Math.Max(0, num2 - num1));
                    }
                }
                int maxSpellLevel = spellbook1.MaxSpellLevel;
                if (spellbook1.Blueprint.SpellsPerLevel > 0) {
                    if (casterLevelBefore == 0) {
                        spellSelectionData.SetExtraSpells(0, maxSpellLevel);
                        spellSelectionData.ExtraByStat = true;
                        spellSelectionData.UpdateMaxLevelSpells(unit);
                    }
                    else
                        spellSelectionData.SetExtraSpells(spellbook1.Blueprint.SpellsPerLevel, maxSpellLevel);
                }
                foreach (AddCustomSpells component2 in spellbook1.Blueprint.GetComponents<AddCustomSpells>())
                    ApplySpellbook.TryApplyCustomSpells(spellbook1, component2, state, unit);
                return false;
            }
        }

        // Fixed a vanilla PFK bug that caused dragon bloodline to be displayed in Magus' feats tree
        [HarmonyPatch(typeof(ApplyClassMechanics), "ApplyProgressions")]
        static class ApplyClassMechanics_ApplyProgressions_Patch {
            public static bool Prefix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return true;
                BlueprintCharacterClass blueprintCharacterClass = state.NextClassLevel <= 1 ? state.SelectedClass : (BlueprintCharacterClass)null;
                foreach (BlueprintProgression blueprintProgression in unit.Progression.Features.Enumerable.Select<Feature, BlueprintFeature>((Func<Feature, BlueprintFeature>)(f => f.Blueprint)).OfType<BlueprintProgression>().ToList<BlueprintProgression>()) {
                    BlueprintProgression p = blueprintProgression;
                    if (blueprintCharacterClass != null
                        // && p.Classes.Contains<BlueprintCharacterClass>(blueprintCharacterClass)) 
                        && p.IsChildProgressionOf(unit, blueprintCharacterClass) // Mod Line replacing above
                        )
                        unit.Progression.Features.Enumerable.FirstItem<Feature>(
                            (f => f.Blueprint == p))?.SetSource((FeatureSource)blueprintCharacterClass, 1
                            );
                    LevelUpHelper.UpdateProgression(state, unit, p);
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(UnitHelper))]
        [HarmonyPatch("CopyInternal")]
        static class UnitProgressionData_CopyFrom_Patch {
            static void Postfix(UnitEntityData unit, UnitEntityData __result) {
                if (!settings.toggleMulticlass) return;
                // When upgrading, this method will be used to copy a UnitEntityData, which involves copying UnitProgressionData
                // By default, the CharacterLevel of the copied UnitProgressionData is equal to the sum of all non-mythical class levels
                //  If the character level is not equal to this default value, there will be problems (for example, when it is lower than the default value, you may not be able to upgrade until you reach level 20, because the sum of non-mythical class levels has exceeded 20 in advance)
                // Fix this.

                var UnitProgressionData_CharacterLevel = AccessTools.Property(typeof(UnitProgressionData), nameof(UnitProgressionData.CharacterLevel));
                Main.Log($"UnitProgressionData_CopyFrom_Patch - {unit.CharacterName.orange()} - {UnitProgressionData_CharacterLevel}");

                UnitProgressionData_CharacterLevel.SetValue(__result.Descriptor.Progression, unit.Descriptor.Progression.CharacterLevel);
            }
        }
    }
}
