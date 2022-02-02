// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers.Mechanics.Facts;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem.Entities;
//using Kingmaker.UI._ConsoleUI.Models;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Linq;
using ToyBox.classes.Infrastructure;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
//using Kingmaker.UI.LevelUp.Phase;

namespace ToyBox.Multiclass {
    internal static class LevelUp {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(LevelUpController), MethodType.Constructor, new Type[] {
            typeof(UnitEntityData),
            typeof(bool),
            typeof(LevelUpState.CharBuildMode) })]
        private static class LevelUpController_ctor_Patch {
            [HarmonyPrefix, HarmonyPriority(Priority.First)]
            private static bool Prefix(LevelUpController __instance) {
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
        [HarmonyPatch(typeof(LevelUpHelper), nameof(LevelUpHelper.UpdateProgression))]
        [HarmonyPatch(new Type[] { typeof(LevelUpState), typeof(UnitDescriptor), typeof(BlueprintProgression) })]
        private static class LevelUpHelper_UpdateProgression_Patch {
            public static bool Prefix([NotNull] LevelUpState state, [NotNull] UnitDescriptor unit, [NotNull] BlueprintProgression progression) {
                if (!settings.toggleMulticlass) return true;
                var progressionData = unit.Progression.SureProgressionData(progression);
                var level = progressionData.Level;
                var nextLevel = progressionData.Blueprint.CalcLevel(unit);
                progressionData.Level = nextLevel;
                // TODO - this is from the mod but we need to figure out if max level 20 still makes sense with mythic levels
                // int maxLevel = 20 // unit.Progression.CharacterLevel;
                // if (nextLevel > maxLevel)
                //     nextLevel = maxLevel;
                Mod.Debug($"LevelUpHelper_UpdateProgression_Patch - {unit.CharacterName.orange()} - class: {state.SelectedClass} level: {level} nextLvl: {nextLevel}");
                if (level >= nextLevel || progression.ExclusiveProgression != null && state.SelectedClass != progression.ExclusiveProgression)
                    return false;
                if (!progression.GiveFeaturesForPreviousLevels)
                    level = nextLevel - 1;
                for (var lvl = level + 1; lvl <= nextLevel; ++lvl) {
                    //                    if (!AllowProceed(progression)) break;
                    var levelEntry = progressionData.GetLevelEntry(lvl);
                    Mod.Debug($"    LevelUpHelper_UpdateProgression_Patch - {string.Join(", ", levelEntry.Features.Select(f => f.name.yellow()))}");

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

        // This is to grab the spells known on load
        [HarmonyPatch(typeof(UnitDescriptor), nameof(UnitDescriptor.PostLoad))]
        public static class UnitDescriptor_PostLoad_Hook {
            private static void Postfix(UnitEntityData entity) => CasterHelpers.CacheSpellsLearned(entity);
        }

        [HarmonyPatch(typeof(LevelUpController), nameof(LevelUpController.Commit))]
        private static class LevelUpController_Commit_Posthook {
            private static void Postfix(LevelUpController __instance) {
                Mod.Debug("Clearing Cache because LevelUpController.Commit called");
                CasterHelpers.ClearCachedSpellsLearned(__instance.Unit);
                CasterHelpers.CacheSpellsLearned(__instance.Unit);
            }
        }

        // Do not proceed the spell selection if the caster level was not changed
        [HarmonyPatch(typeof(ApplySpellbook), nameof(ApplySpellbook.Apply))]
        [HarmonyPatch(new Type[] { typeof(LevelUpState), typeof(UnitDescriptor) })]
        private static class ApplySpellbook_Apply_Patch {
            public static bool Prefix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return true;

                if (state.SelectedClass == null) {
                    return false;
                }

                var component1 = state.SelectedClass.GetComponent<SkipLevelsForSpellProgression>();
                if (component1 != null && component1.Levels.Contains(state.NextClassLevel)) {
                    return false;
                }

                var classData = unit.Progression.GetClassData(state.SelectedClass);
                if (classData?.Spellbook == null) {
                    return false;
                }

                var spellbook1 = unit.DemandSpellbook(classData.Spellbook);
                if (state.SelectedClass.Spellbook && state.SelectedClass.Spellbook != classData.Spellbook) {
                    var spellbook2 = unit.Spellbooks.FirstOrDefault(s => s.Blueprint == state.SelectedClass.Spellbook);
                    if (spellbook2 != null) {
                        foreach (var allKnownSpell in spellbook2.GetAllKnownSpells()) {
                            spellbook1.AddKnown(allKnownSpell.SpellLevel, allKnownSpell.Blueprint);
                        }

                        unit.DeleteSpellbook(state.SelectedClass.Spellbook);
                    }
                }
                var casterLevelAfter = CasterHelpers.GetRealCasterLevel(unit, spellbook1.Blueprint); // Calculates based on progression which includes class selected in level up screen
                spellbook1.AddLevelFromClass(classData.CharacterClass); // This only adds one class at a time and will only ever increase by 1 or 2
                var casterLevelBefore = casterLevelAfter - (classData.CharacterClass.IsMythic ? 2 : 1); // Technically only needed to see if this is our first level of a casting class
                var spellSelectionData = state.DemandSpellSelection(spellbook1.Blueprint, spellbook1.Blueprint.SpellList);
                if (spellbook1.Blueprint.SpellsKnown != null) {
                    for (var index = 0; index <= 10; ++index) {
                        var spellsKnown = spellbook1.Blueprint.SpellsKnown;
                        var count = spellsKnown.GetCount(casterLevelAfter, index);
                        var expectedCount = count ?? 0;
                        var actual = CasterHelpers.GetCachedSpellsKnown(unit, spellbook1, index);
#if DEBUG
                        //Mod.Trace($"Spellbook {spellbook1.Blueprint.Name}: Granting {expectedCount-actual} spells of spell level:{index} based on expected={expectedCount} and actual={actual}");
#endif
                        spellSelectionData.SetLevelSpells(index, Math.Max(0, expectedCount - actual));
                    }
                }
                var maxSpellLevel = spellbook1.MaxSpellLevel;
                if (spellbook1.Blueprint.SpellsPerLevel > 0) {
                    if (casterLevelBefore == 0) {
                        spellSelectionData.SetExtraSpells(0, maxSpellLevel);
                        spellSelectionData.ExtraByStat = true;
                        spellSelectionData.UpdateMaxLevelSpells(unit);
                    }
                    else {
                        spellSelectionData.SetExtraSpells(spellbook1.Blueprint.SpellsPerLevel, maxSpellLevel);
                    }
                }
                foreach (var component2 in spellbook1.Blueprint.GetComponents<AddCustomSpells>()) {
                    ApplySpellbook.TryApplyCustomSpells(spellbook1, component2, state, unit);
                }

                return false;
            }
        }

        // Fixed a vanilla PFK bug that caused dragon bloodline to be displayed in Magus' feats tree
        [HarmonyPatch(typeof(ApplyClassMechanics), nameof(ApplyClassMechanics.ApplyProgressions))]
        private static class ApplyClassMechanics_ApplyProgressions_Patch {
            public static bool Prefix(LevelUpState state, UnitDescriptor unit) {
                if (!settings.toggleMulticlass) return true;
                Mod.Debug($"ApplyClassMechanics_ApplyProgressions_Patch - {unit.CharacterName.orange()} - class: {state.SelectedClass} nextLevel: {state.NextClassLevel}");
                BlueprintCharacterClass blueprintCharacterClass = null;
                if (unit.TryGetPartyMemberForLevelUpVersion(out var ch)
                    && ch.TryGetClass(state.SelectedClass, out var cl)
                    && state.NextClassLevel >= cl.Level
                    )
                    blueprintCharacterClass = state.SelectedClass;
                //var blueprintCharacterClass = state.NextClassLevel <= 1 ? state.SelectedClass : (BlueprintCharacterClass)null;
                var features = unit.Progression.Features.Enumerable;
                var progressions = features.Select(f => f.Blueprint).OfType<BlueprintProgression>().ToList();  // this ToList is important because it prevents mutation exceptions
                foreach (var blueprintProgression in progressions) {
                    var p = blueprintProgression;
                    Mod.Debug($"    prog: {p.name.yellow()}");
                    if (blueprintCharacterClass != null
                        // && p.Classes.Contains<BlueprintCharacterClass>(blueprintCharacterClass)) 
                        && p.IsChildProgressionOf(unit, blueprintCharacterClass) // Mod Line replacing above
                        ) {
                        var feature = unit.Progression.Features.Enumerable.FirstItem(f => f.Blueprint == p);
                        feature?.SetSource(blueprintCharacterClass, state.NextClassLevel);
                        Mod.Debug($"    feature: {feature.Name.cyan()} - levlel: {state.NextClassLevel}");
                        //feature?.SetSource(blueprintCharacterClass, 1);
                    }
                    LevelUpHelper.UpdateProgression(state, unit, p);
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(UnitHelper), nameof(UnitHelper.CopyInternal))]
        private static class UnitProgressionData_CopyFrom_Patch { 
            private static void Postfix(UnitEntityData unit, UnitEntityData __result) {
                if (!settings.toggleMulticlass) return;
                // When upgrading, this method will be used to copy a UnitEntityData, which involves copying UnitProgressionData
                // By default, the CharacterLevel of the copied UnitProgressionData is equal to the sum of all non-mythical class levels
                //  If the character level is not equal to this default value, there will be problems (for example, when it is lower than the default value, you may not be able to upgrade until you reach level 20, because the sum of non-mythical class levels has exceeded 20 in advance)
                // Fix this.

                var UnitProgressionData_CharacterLevel = AccessTools.Property(typeof(UnitProgressionData), nameof(UnitProgressionData.CharacterLevel));
                Mod.Trace($"UnitProgressionData_CopyFrom_Patch - {unit.CharacterName.orange()} - {UnitProgressionData_CharacterLevel}");

                UnitProgressionData_CharacterLevel.SetValue(__result.Descriptor.Progression, unit.Descriptor.Progression.CharacterLevel);
                __result.Descriptor.Progression.MythicLevel = unit.Descriptor.Progression.MythicLevel;
            }
        }
    }
}
