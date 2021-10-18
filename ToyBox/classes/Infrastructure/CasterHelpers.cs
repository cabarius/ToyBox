using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using ModKit;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox.classes.Infrastructure {
    public static class CasterHelpers {
        private static readonly Dictionary<string, List<int>> UnitSpellsKnown = new();
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
            if (!spellbook.IsKnown(ability))spellbook.AddKnown(level, ability); 
        }

        public static void AddAllSpellsOfSelectedLevel(Spellbook spellbook, int level) {
            List<BlueprintAbility> toLearn;
            if (Main.settings.showFromAllSpellbooks) {
                var normal = BlueprintExensions.GetBlueprints<BlueprintSpellbook>()
                    .Where(x => ((BlueprintSpellbook)x).SpellList != null)
                    .SelectMany(x => ((BlueprintSpellbook)x).SpellList.GetSpells(level));
                var mythic = BlueprintExensions.GetBlueprints<BlueprintSpellbook>()
                    .Where(x => ((BlueprintSpellbook)x).MythicSpellList != null)
                    .SelectMany(x => ((BlueprintSpellbook)x).MythicSpellList.GetSpells(level));
                toLearn = normal.Concat(mythic).Distinct().ToList();
            }
            else {
                toLearn = spellbook.Blueprint.SpellList.GetSpells(level);
            }

            toLearn.ForEach(x => spellbook.AddIfUnknown(level, x));
        }

        public static void HandleAddAllSpellsOnPartyEditor(UnitDescriptor unit, List<BlueprintAbility> abilities) {
            if (!PartyEditor.SelectedSpellbook.TryGetValue(unit.HashKey(), out var selectedSpellbook)) {
                return;
            }

            if (abilities != null) {
                abilities.ForEach(x => selectedSpellbook.AddIfUnknown(PartyEditor.selectedSpellbookLevel, x));
            }
            else {
                AddAllSpellsOfSelectedLevel(selectedSpellbook, PartyEditor.selectedSpellbookLevel);
            }
        }
        
        public static void HandleAddAllSpellsOnPartyEditor(UnitDescriptor unit) {
            if (!PartyEditor.SelectedSpellbook.TryGetValue(unit.HashKey(), out var selectedSpellbook)) {
                return;
            }
            selectedSpellbook.RemoveSpellsOfLevel(PartyEditor.selectedSpellbookLevel);
        }

        public static int GetCachedSpellsKnown(UnitDescriptor unit, Spellbook spellbook, int level) {
            var key = $"{unit.CharacterName}.{spellbook.Blueprint.Name}";
            if (!UnitSpellsKnown.TryGetValue(key, out var spellsKnownList)) {
                //Mod.Trace($"Can't find cached spells known data for character {unit.CharacterName} with key: {key}");
                return level > 1 ? GetCachedSpellsKnown(unit, spellbook, level - 1) : 0;
            }

            return spellsKnownList.Count > level ? spellsKnownList[level] : spellsKnownList.LastOrDefault();
        }

        public static void CacheSpellsLearned(UnitEntityData unit) {
            var addedSpellComponents = unit.Facts.List.SelectMany(x =>
                x.BlueprintComponents.Where(y => y is AddKnownSpell)).Select(z => z as AddKnownSpell).ToList();
            foreach (var unitSpellbook in unit.Spellbooks) {
                var spellsToIgnore = addedSpellComponents
                    .Where(x => x.CharacterClass == unitSpellbook.Blueprint.CharacterClass && (x.Archetype == null || unit.Progression.IsArchetype(x.Archetype))).Select(y => y.Spell).ToList();
                var key = $"{unit.CharacterName}.{unitSpellbook.Blueprint.Name}";
                if (UnitSpellsKnown.ContainsKey(key)) {
                    continue;
                }
                var spellsLearnedList = new List<int>();
                for (var i = 0; i < 10; i++) {
                    spellsLearnedList.Add(GetActualSpellsLearned(unitSpellbook, i, spellsToIgnore));
                }
                UnitSpellsKnown.Add(key, spellsLearnedList);
                var list = string.Join(", ", spellsLearnedList);
                Mod.Trace($"Caching {list} for {key}");
            }
        }

        public static void ClearCachedSpellsLearned(UnitEntityData unit) {
            foreach (var unitSpellbook in unit.Spellbooks) {
                var key = $"{unit.CharacterName}.{unitSpellbook.Blueprint.Name}";
                if (UnitSpellsKnown.ContainsKey(key)) {
                    Mod.Trace($"Clearing cached value for {key}");
                    UnitSpellsKnown.Remove(key);
                }
            }
        }

        public static int GetActualSpellsLearned(Spellbook spellbook, int level, List<BlueprintAbility> spellsToIgnore) {
            var known = spellbook.SureKnownSpells(level)
                .Where(x => !x.IsTemporary)
                .Where(x => !x.CopiedFromScroll)
                .Where(x => !x.IsFromMythicSpellList)
                .Where(x => x.SourceItem == null)
                .Where(x => x.SourceItemEquipmentBlueprint == null)
                .Where(x => x.SourceItemUsableBlueprint == null)
                .Where(x => !x.IsMysticTheurgeCombinedSpell)
                .Where(x => !spellsToIgnore.Contains(x.Blueprint))
                .Distinct()
                .ToList();

            return known.Count;
        }

        public static ClassData GetMythicToMerge(this UnitProgressionData unit) {
            var list = unit.Classes.Where(cls => cls.CharacterClass.IsMythic).ToList();
            if (!list.Any()) {
                return null;
            }

            return list.Count == 1 ? list.First() : list.FirstOrDefault(mythic => mythic.CharacterClass != BlueprintRoot.Instance.Progression.MythicStartingClass && mythic.CharacterClass != BlueprintRoot.Instance.Progression.MythicCompanionClass);
        }

        public static void ForceSpellbookMerge(Spellbook spellbook) {
            var unit = spellbook.Owner;
            var classData = unit.Progression.GetMythicToMerge();
            var oldMythicSpellbookBp = classData?.Spellbook;
            if (classData == null || oldMythicSpellbookBp == null || !oldMythicSpellbookBp.IsMythic) {
                Mod.Warn("Can't merge because you don't have a mythic class / mythic spellbook!");
                return;
            }

            classData.Spellbook = spellbook.Blueprint;
            spellbook.m_Type = SpellbookType.Mythic;
            spellbook.AddSpecialList(oldMythicSpellbookBp.MythicSpellList);
            for (var i = 0; i < unit.Progression.MythicLevel; i++) {
                spellbook.AddMythicLevel();
            }

            unit.DeleteSpellbook(oldMythicSpellbookBp);
        }

        private static Dictionary<int, List<BlueprintAbility>> AllSpellsCache = new();
        public static List<BlueprintAbility> GetAllSpells(int level) {
            if (AllSpellsCache.TryGetValue(level, out var spells))
                return spells;
            else {
                var spellbooks = BlueprintExensions.GetBlueprints<BlueprintSpellbook>();
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
                if (spells.Count() > 0)
                    AllSpellsCache[level] = spells;
                return spells;
            }
        }
    }
}
