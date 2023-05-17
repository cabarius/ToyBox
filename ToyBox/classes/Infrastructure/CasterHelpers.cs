﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
#if Wrath
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
#elif RT
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Spells;
#endif
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
#if RT
using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
#endif
namespace ToyBox.classes.Infrastructure {
    public static class CasterHelpers {
        private static readonly Dictionary<string, List<int>> UnitSpellsKnown = new();
        // This is to figure out how many caster levels you actually have real levels in
        public static Dictionary<BlueprintSpellbook, int> GetOriginalCasterLevel(UnitDescriptor unit) {
            var mythicLevel = 0;
            BlueprintSpellbook mythicSpellbook = null;
            Dictionary<BlueprintSpellbook, int> casterLevelDictionary = new();
            foreach (var classInfo in unit.Progression.Classes) {
                if (classInfo.CharacterClass == BlueprintRoot.Instance.Progression.MythicStartingClass ||
                    classInfo.CharacterClass == BlueprintRoot.Instance.Progression.MythicCompanionClass) {
                    if (mythicSpellbook == null) {
                        mythicLevel += classInfo.Level;
                    }
                    else {
                        casterLevelDictionary[mythicSpellbook] += classInfo.Level;
                    }
                }

                if (classInfo.Spellbook == null) {
                    continue;
                }

                var casterLevel = classInfo.Level + classInfo.Spellbook.CasterLevelModifier;
                if (classInfo.CharacterClass.IsMythic) {
                    casterLevel += mythicLevel;
                    mythicSpellbook = classInfo.Spellbook;
                }

                var skipLevels = classInfo.CharacterClass.GetComponent<SkipLevelsForSpellProgression>();

                if (skipLevels?.Levels?.Length > 0) {
                    foreach (var skipLevelsLevel in skipLevels.Levels) {
                        if (classInfo.Level >= skipLevelsLevel) { --casterLevel; }
                    }
                }

                var levelToStartCountingFrom = classInfo.CharacterClass.PrestigeClass ? GetPrestigeCasterLevelStart(classInfo.CharacterClass.Progression) : 1;
                casterLevel += 1 - levelToStartCountingFrom;

                if (classInfo.Spellbook.IsMythic) {
                    casterLevel *= 2;
                }

                if (casterLevelDictionary.ContainsKey(classInfo.Spellbook)) {
                    casterLevelDictionary[classInfo.Spellbook] += casterLevel;
                }
                else {
                    casterLevelDictionary[classInfo.Spellbook] = casterLevel;
                }
            }
#if DEBUG
            foreach (var spellbook in casterLevelDictionary) {
                Mod.Trace($"spellbook - {spellbook.Key.Name}: {spellbook.Value}");
            }
#endif
            return casterLevelDictionary;
        }

        public static int GetRealCasterLevel(UnitDescriptor unit, BlueprintSpellbook spellbook) {
            var hasCasterLevel = GetOriginalCasterLevel(unit).TryGetValue(spellbook, out var level);
            return hasCasterLevel ? level : 0;
        }


        private static int GetPrestigeCasterLevelStart(BlueprintProgression progression) {
            foreach (var level in progression.LevelEntries) {
                if (level.Features.OfType<BlueprintFeatureSelection>().SelectMany(feature => feature.AllFeatures.OfType<BlueprintFeatureReplaceSpellbook>()).Any()) {
                    return level.Level;
                }
            }
            return 1;
        }

        public static void RemoveSpellsOfLevel(this Spellbook spellbook, int level) {
            var spells = new List<AbilityData>(spellbook.GetKnownSpells(level));
            // copy constructor is needed to avoid self mutation here
            spells.ForEach(x => spellbook.RemoveSpell(x.Blueprint));
        }

        public static void LowerCasterLevel(Spellbook spellbook) {
            var oldMaxSpellLevel = spellbook.MaxSpellLevel;
            spellbook.m_BaseLevelInternal--;
            var newMaxSpellLevel = spellbook.MaxSpellLevel;
            if (newMaxSpellLevel < oldMaxSpellLevel) {
                RemoveSpellsOfLevel(spellbook, oldMaxSpellLevel);
            }
        }

        public static void AddCasterLevel(Spellbook spellbook) {
            var oldMaxSpellLevel = spellbook.MaxSpellLevel;
            spellbook.m_BaseLevelInternal++;
            var newMaxSpellLevel = spellbook.MaxSpellLevel;
            if (newMaxSpellLevel > oldMaxSpellLevel) {
                spellbook.LearnSpellsOnRaiseLevel(oldMaxSpellLevel, newMaxSpellLevel, false);
            }
        }
        public static void AddIfUnknown(this Spellbook spellbook, int level, BlueprintAbility ability) {
            if (!spellbook.IsKnown(ability)) spellbook.AddKnown(level, ability);
        }

        public static void AddAllSpellsOfSelectedLevel(Spellbook spellbook, int level) {
            List<BlueprintAbility> toLearn;
            if (Main.Settings.showFromAllSpellbooks) {
                var normal = BlueprintExtensions.GetBlueprints<BlueprintSpellbook>()
                    .Where(x => ((BlueprintSpellbook)x).SpellList != null)
                    .SelectMany(x => ((BlueprintSpellbook)x).SpellList.GetSpells(level));
                var mythic = BlueprintExtensions.GetBlueprints<BlueprintSpellbook>()
                    .Where(x => ((BlueprintSpellbook)x).MythicSpellList != null)
                    .SelectMany(x => ((BlueprintSpellbook)x).MythicSpellList.GetSpells(level));
                toLearn = normal.Concat(mythic).Distinct().ToList();
            }
            else {
                toLearn = spellbook.Blueprint.SpellList.GetSpells(level);
            }

            toLearn.ForEach(x => spellbook.AddIfUnknown(level, x));
        }

        public static IEnumerable<BlueprintAbility> GetSpellsLearnableOfLevel(this Spellbook sb, int lvl) {
            foreach (var s in sb.Blueprint.SpellList.GetSpells(lvl))
                if (!sb.IsKnown(s))
                    yield return s;
        }

        public static void HandleAddAllSpellsOnPartyEditor(UnitDescriptor unit, List<BlueprintAbility> abilities) {
            if (!PartyEditor.SelectedSpellbook.TryGetValue(unit.HashKey(), out var selectedSpellbook)) {
                return;
            }
            var level = PartyEditor.selectedSpellbookLevel;
            if (level == selectedSpellbook.Blueprint.MaxSpellLevel + 1)
                level = PartyEditor.newSpellLvl;
            if (abilities != null) {
                abilities.ForEach(x => selectedSpellbook.AddIfUnknown(level, x));
            }
            else {
                AddAllSpellsOfSelectedLevel(selectedSpellbook, level);
            }
        }

        public static void HandleAddAllSpellsOnPartyEditor(UnitDescriptor unit) {
            if (!PartyEditor.SelectedSpellbook.TryGetValue(unit.HashKey(), out var selectedSpellbook)) {
                return;
            }
            var level = PartyEditor.selectedSpellbookLevel;
            if (level == selectedSpellbook.Blueprint.MaxSpellLevel + 1)
                level = PartyEditor.newSpellLvl;
            selectedSpellbook.RemoveSpellsOfLevel(level);
        }

        public static int GetActualSpellsLearnedForClass(UnitDescriptor unit, Spellbook spellbook, int level) {
            Mod.Trace($"GetActualSpellsLearnedForClass - unit: {unit?.CharacterName} spellbook: {spellbook?.Blueprint.DisplayName} level:{level}");
            // Get all +spells known facts for this spellbook's class so we can ignore them when getting spell counts
            var spellsToIgnore = unit.Facts.List.SelectMany(x =>
                x.BlueprintComponents.Where(y => y is AddKnownSpell)).Select(z => z as AddKnownSpell)
                .Where(x => x.CharacterClass == spellbook.Blueprint.CharacterClass && (x.Archetype == null || unit.Progression.IsArchetype(x.Archetype))).Select(y => y.Spell)
                .ToList();
            Spellbook spellbookOfNormalUnit = null;
            if (unit.TryGetPartyMemberForLevelUpVersion(out var ch)) { // get the real units spellbook, the levelup version does not contain flags like CopiedFromScroll
                if (ch?.Spellbooks?.Count() > 0)
                    spellbookOfNormalUnit = ch.Spellbooks.First(s => s.Blueprint == spellbook.Blueprint);
            }
            return GetActualSpellsLearned(spellbook, level, spellsToIgnore, spellbookOfNormalUnit);
        }

        /// <summary>
        /// Calculates the number of spells selected via levelup, excluding spells from items, learned from scrolls and similar.
        /// If the spellbook comes from a UnitDescriptor thats part of a levelup, you need to specify spellbookOfNormalUnit as the base units spellbook.
        /// (Because levelup logic does not copy any AbilityData flags.) (see GetActualSpellsLearnedForClass as example.)
        /// </summary>
        /// <param name="spellbook"></param>
        /// <param name="level"></param>
        /// <param name="spellsToIgnore"></param>
        /// <param name="spellbookOfNormalUnit"></param>
        /// <returns></returns>
        public static int GetActualSpellsLearned(Spellbook spellbook, int level, List<BlueprintAbility> spellsToIgnore, Spellbook spellbookOfNormalUnit = null) {
            Mod.Trace($"GetActualSpellsLearned - spellbook: {spellbook?.Blueprint.DisplayName} level:{level}");

            Func<AbilityData, bool> normalSpellbookCondition = x => true;
            if (spellbookOfNormalUnit != null) {
                var normalSpellsOfLevel = spellbookOfNormalUnit.SureKnownSpells(level);
                normalSpellbookCondition = x => {
                    var sp = normalSpellsOfLevel.First(a => a.Blueprint == x.Blueprint);
                    if (sp == null)
                        return true;
                    return !sp.IsTemporary
                        && !sp.CopiedFromScroll
                        && !sp.IsFromMythicSpellList
                        && sp.SourceItem == null
                        && sp.SourceItemEquipmentBlueprint == null
                        && sp.SourceItemUsableBlueprint == null
                        && !sp.IsMysticTheurgeCombinedSpell;
                };
            }
            var known = spellbook.SureKnownSpells(level)
                .Where(x => !x.IsTemporary
                && !x.CopiedFromScroll
                && !x.IsFromMythicSpellList
                && x.SourceItem == null
                && x.SourceItemEquipmentBlueprint == null
                && x.SourceItemUsableBlueprint == null
                && !x.IsMysticTheurgeCombinedSpell
                && !spellsToIgnore.Contains(x.Blueprint)
                && normalSpellbookCondition(x))
                .Distinct()
                .ToList();

            return known.Count;
        }

        public static IEnumerable<ClassData> MergableClasses(this UnitEntityData unit) {
            var spellbookCandidates = unit.Spellbooks
                                          .Where(sb => sb.IsStandaloneMythic && sb.Blueprint.CharacterClass != null)
                                          .Select(sb => sb.Blueprint).ToHashSet();
            //Mod.Log($"{unit.CharacterName} - spellbookCandidates: {string.Join(", ", spellbookCandidates.Select(sb => sb.DisplayName))}");
            var classCandidates = unit.Progression.Classes
                                      .Where(cl => cl.Spellbook != null && spellbookCandidates.Contains(cl.Spellbook));
            //Mod.Log($"{unit.CharacterName} - classCandidates: {string.Join(", ", classCandidates.Select(cl => cl.CharacterClass.Name))}");
            return classCandidates;
        }
        public static void MergeMythicSpellbook(this Spellbook targetSpellbook, ClassData fromClass) {
            var unit = targetSpellbook.Owner;
            var oldMythicSpellbookBp = fromClass?.Spellbook;
            if (fromClass == null || oldMythicSpellbookBp == null || !oldMythicSpellbookBp.IsMythic) {
                Mod.Warn("Can't merge because you don't have a mythic class / mythic spellbook!");
                return;
            }

            fromClass.Spellbook = targetSpellbook.Blueprint;
            targetSpellbook.m_Type = SpellbookType.Mythic;
            targetSpellbook.AddSpecialList(oldMythicSpellbookBp.MythicSpellList);
            for (var i = targetSpellbook.MythicLevel; i < unit.Progression.MythicLevel; i++) {
                targetSpellbook.AddMythicLevel();
            }
            unit.DeleteSpellbook(oldMythicSpellbookBp);
        }
        private static readonly Dictionary<int, List<BlueprintAbility>> AllSpellsCache = new();
        public static List<BlueprintAbility> GetAllSpells(int level) {
            if (AllSpellsCache.TryGetValue(level, out var spells)) {
                return spells;
            }
            else {
                if (level == -1) {
                    var abilities = BlueprintExtensions.GetBlueprints<BlueprintAbility>();
                    spells = new List<BlueprintAbility>();
                    foreach (var ability in abilities) {
                        if (ability.IsSpell) {
                            spells.Add(ability);
                        }
                    }
                }
                else {
                    var spellbooks = BlueprintExtensions.GetBlueprints<BlueprintSpellbook>();
                    if (spellbooks == null) return null;
                    Mod.Log($"spellbooks: {spellbooks.Count()}");

                    var normal = from spellbook in spellbooks
                                 where spellbook.SpellList != null
                                 from spell in spellbook.SpellList.GetSpells(level)
                                 select spell;
                    Mod.Log($"normal: {normal.Count()}");
                    var mythic = from spellbook in spellbooks
                                 where spellbook.MythicSpellList != null
                                 from spell in spellbook.MythicSpellList.GetSpells(level)
                                 select spell;
                    Mod.Log($"mythic: {mythic.Count()}");
                    spells = normal.Concat(mythic).Distinct().ToList();
                }
                if (spells.Count() > 0)
                    AllSpellsCache[level] = spells;
                return spells;
            }
        }
    }
}
