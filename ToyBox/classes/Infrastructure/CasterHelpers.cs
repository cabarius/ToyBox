﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using ModKit;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox.classes.Infrastructure {
    public static class CasterHelpers {
        public static Dictionary<UnitDescriptor, Dictionary<BlueprintSpellbook, int>> UnitBonusSpellLevels =
            new Dictionary<UnitDescriptor, Dictionary<BlueprintSpellbook, int>>();
        public static Dictionary<BlueprintSpellbook, int> GetOriginalCasterLevel(UnitDescriptor unit) {
            int mythicLevel = 0;
            BlueprintSpellbook mythicSpellbook = null;
            Dictionary<BlueprintSpellbook, int> casterLevelDictionary = new Dictionary<BlueprintSpellbook, int>();
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

                int casterLevel = classInfo.Level + classInfo.Spellbook.CasterLevelModifier;
                if (classInfo.CharacterClass.IsMythic) {
                    casterLevel += mythicLevel;
                    mythicSpellbook = classInfo.Spellbook;
                }

                var skipLevels = classInfo.CharacterClass.GetComponent<SkipLevelsForSpellProgression>();
                
                if (skipLevels?.Levels?.Length > 0) {
                    foreach (int skipLevelsLevel in skipLevels.Levels) {
                        if (classInfo.Level >= skipLevelsLevel) { --casterLevel; }
                    }
                }

                int levelToStartCountingFrom = classInfo.CharacterClass.PrestigeClass ? GetPrestigeCasterLevelStart(classInfo.CharacterClass.Progression) : 1;
                casterLevel += 1 - levelToStartCountingFrom;

                if (classInfo.Spellbook.IsMythic) {
                    casterLevel *= 2;
                }

                if (casterLevelDictionary.ContainsKey(classInfo.Spellbook)) {
                    casterLevelDictionary[classInfo.Spellbook] += casterLevel;
                } else {
                    casterLevelDictionary[classInfo.Spellbook] = casterLevel;
                }
            }
#if DEBUG
            foreach (var spellbook in casterLevelDictionary) {
                Mod.Trace($"{spellbook.Key.Name}: {spellbook.Value}");
            }
#endif
            return casterLevelDictionary;
        }

        public static int GetRealCasterLevel(UnitDescriptor unit, BlueprintSpellbook spellbook) {
            bool hasCasterLevel = GetOriginalCasterLevel(unit).TryGetValue(spellbook, out int level);
            return hasCasterLevel ? level : 0;
        }

        public static void FindBonusLevels(UnitDescriptor unit) {
            var resultsDictionary = new Dictionary<BlueprintSpellbook, int>();
            var calculatedResults = GetOriginalCasterLevel(unit);
            var actual = unit.m_Spellbooks;
            foreach (var spellbookTypePair in calculatedResults) {
                actual.TryGetValue(spellbookTypePair.Key, out var actualSpellbook);
                if (actualSpellbook != null) {
                    int bonus = actualSpellbook.CasterLevel - spellbookTypePair.Value;
                    resultsDictionary.Add(spellbookTypePair.Key, bonus);
                }
            }
            UnitBonusSpellLevels.Add(unit, resultsDictionary);
        }

        private static int GetPrestigeCasterLevelStart(BlueprintProgression progression) {
            foreach (var level in progression.LevelEntries) {
                if (level.Features.OfType<BlueprintFeatureSelection>().SelectMany(feature => feature.AllFeatures.OfType<BlueprintFeatureReplaceSpellbook>()).Any()) {
                    return level.Level;
                }
            }
            return 1;
        }

        public static void RemoveSpellsOfLevel(Spellbook spellbook, int level) {
            var spells = spellbook.GetKnownSpells(level);
            spells.ForEach(x => spellbook.RemoveSpell(x.Blueprint));
        }

        public static void LowerCasterLevel(Spellbook spellbook) {
            int oldMaxSpellLevel = spellbook.MaxSpellLevel;
            spellbook.m_BaseLevelInternal--;
            int newMaxSpellLevel = spellbook.MaxSpellLevel;
            if (newMaxSpellLevel < oldMaxSpellLevel) {
                RemoveSpellsOfLevel(spellbook, oldMaxSpellLevel);
            }
        }

        public static void AddCasterLevel(Spellbook spellbook) {
            int oldMaxSpellLevel = spellbook.MaxSpellLevel;
            spellbook.m_BaseLevelInternal++;
            int newMaxSpellLevel = spellbook.MaxSpellLevel;
            if (newMaxSpellLevel > oldMaxSpellLevel) {
                spellbook.LearnSpellsOnRaiseLevel(oldMaxSpellLevel, newMaxSpellLevel, false);
            }
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

            toLearn.ForEach(x => spellbook.AddKnown(level, x));
        }

        public static void HandleAddAllSpellsOnPartyEditor(UnitDescriptor unit, List<BlueprintAbility> abilities) {
            if (!PartyEditor.SelectedSpellbook.TryGetValue(unit.HashKey(), out var selectedSpellbook)) {
                return;
            }

            if (abilities != null) {
                abilities.ForEach(x => selectedSpellbook.AddKnown(PartyEditor.selectedSpellbookLevel, x));
            }
            else {
                AddAllSpellsOfSelectedLevel(selectedSpellbook, PartyEditor.selectedSpellbookLevel);
            }
        }

        public static ClassData GetMythicToMerge(this UnitProgressionData unit) {
            List<ClassData> list = unit.Classes.Where(cls => cls.CharacterClass.IsMythic).ToList();
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
                Mod.Warning("Can't merge because you don't have a mythic class / mythic spellbook!");
                return;
            }

            classData.Spellbook = spellbook.Blueprint;
            spellbook.m_Type = SpellbookType.Mythic;
            spellbook.AddSpecialList(oldMythicSpellbookBp.MythicSpellList);
            for (int i = 0; i < unit.Progression.MythicLevel; i++) {
                spellbook.AddMythicLevel();
            }

            unit.DeleteSpellbook(oldMythicSpellbookBp);
        }
    }
}
