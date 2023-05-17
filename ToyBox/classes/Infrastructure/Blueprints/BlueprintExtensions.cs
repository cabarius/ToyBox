// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#if Wrath
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Craft;
#elif RT
using Kingmaker.UI.Models.Tooltip.Base;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection;
using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Spells;
using Kingmaker.UnitLogic.Progression.Features;
using Owlcat.Runtime.Core;
#endif
namespace ToyBox {

    public static partial class BlueprintExtensions {
        public static Settings Settings => Main.Settings;

        private static ConditionalWeakTable<object, List<string>> _cachedCollationNames = new() { };
        private static readonly HashSet<BlueprintGuid> BadList = new();
        public static void ResetCollationCache() => _cachedCollationNames = new ConditionalWeakTable<object, List<string>> { };
        private static void AddOrUpdateCachedNames(SimpleBlueprint bp, List<string> names) {
            names = names.Distinct().ToList();
            if (_cachedCollationNames.TryGetValue(bp, out _)) {
                _cachedCollationNames.Remove(bp);
                //Mod.Log($"removing: {bp.NameSafe()}");
            }
            _cachedCollationNames.Add(bp, names);
            //Mod.Log($"adding: {bp.NameSafe()} - {names.Count} - {String.Join(", ", names)}");
        }

        public static string GetDisplayName(this SimpleBlueprint bp) => bp switch {
            BlueprintAbilityResource abilityResource => abilityResource.Name,
            BlueprintArchetype archetype => archetype.Name,
            BlueprintCharacterClass charClass => charClass.Name,
            BlueprintItem item => item.Name,
            BlueprintItemEnchantment enchant => enchant.Name,
            BlueprintUnitFact fact => fact.NameSafe(),
            SimpleBlueprint blueprint => blueprint.name,
            _ => "n/a"
        };
        public static string GetDisplayName(this BlueprintSpellbook bp) {
            var name = bp.DisplayName;
            if (string.IsNullOrEmpty(name)) name = bp.name.Replace("Spellbook", "");
            return name;
        }
        public static string GetTitle(SimpleBlueprint blueprint) {
            if (blueprint is IUIDataProvider uiDataProvider) {
                string name;
                var isEmpty = uiDataProvider.Name.IsNullOrEmpty();
                if (isEmpty) {
                    name = blueprint.name;
                }
                else {
                    if (blueprint is BlueprintSpellbook spellbook)
                        return $"{spellbook.Name} - {spellbook.name}";
                    name = uiDataProvider.Name;
                    if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                        name = blueprint.name;
                    }
                    else if (Settings.showDisplayAndInternalNames) {
                        name += $" : {blueprint.name.color(RGBA.darkgrey)}";
                    }
                }
                return name;
            }
            return blueprint.name;
        }
        public static string GetSearchKey<Definition>(Definition feature) where Definition : SimpleBlueprint, IUIDataProvider {
            string name;
            var isEmpty = feature.Name.IsNullOrEmpty();
            if (isEmpty) {
                name = feature.name;
            }
            else {
                if (feature is BlueprintSpellbook spellbook)
                    return $"{spellbook.Name} {spellbook.name}";
                name = feature.Name;
                if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                    name = feature.name;
                }
                else if (Settings.showDisplayAndInternalNames) {
                    name += $" : {feature.name}";
                }
            }
            return name.StripHTML(); // can we get rid of this?
        }
        public static string GetSortKey(SimpleBlueprint blueprint) {
            if (blueprint is IUIDataProvider uiDataProvider) {
                string name;
                var isEmpty = uiDataProvider.Name.IsNullOrEmpty();
                if (isEmpty) {
                    name = blueprint.name;
                }
                else {
                    if (blueprint is BlueprintSpellbook spellbook)
                        return $"{spellbook.Name} - {spellbook.name}";
                    name = uiDataProvider.Name;
                    if (name == "<null>" || name.StartsWith("[unknown key: ")) {
                        name = blueprint.name;
                    }
                    else if (Settings.showDisplayAndInternalNames) {
                        name += blueprint.name;
                    }
                }
                return name;
            }
            return blueprint.name;
        }
        public static IEnumerable<string> Attributes(this SimpleBlueprint bp) {
            List<string> modifiers = new();
            if (BadList.Contains(bp.AssetGuid)) return modifiers;
            var traverse = Traverse.Create(bp);
            foreach (var property in Traverse.Create(bp).Properties().Where(property => property.StartsWith("Is"))) {
                try {
                    var value = traverse.Property<bool>(property)?.Value;
                    if (value.HasValue && value.GetValueOrDefault()) {
                        modifiers.Add(property); //.Substring(2));
                    }
                }
                catch (Exception e) {
                    Mod.Warn($"${bp.name}.{property} thew an exception: {e.Message}");
                    BadList.Add(bp.AssetGuid);
                    break;
                }
            }
            return modifiers;
        }
        private static List<string> DefaultCollationNames(this SimpleBlueprint bp, string[] extras) {
            _cachedCollationNames.TryGetValue(bp, out var names);
            if (names == null) {
                names = new List<string> { };
                var typeName = bp.GetType().Name.Replace("Blueprint", "");
                //var stripIndex = typeName.LastIndexOf("Blueprint");
                //if (stripIndex > 0) typeName = typeName.Substring(stripIndex + "Blueprint".Length);
                names.Add(typeName);
                foreach (var attribute in bp.Attributes())
                    names.Add(attribute.orange());
                _cachedCollationNames.Add(bp, names.Distinct().ToList());
            }

            if (extras != null) names = names.Concat(extras).ToList();
            return names;
        }
        public static List<string> CollationNames(this SimpleBlueprint bp, params string[] extras) => DefaultCollationNames(bp, extras);
        public static List<string> CollationNames(this BlueprintCharacterClass bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.IsArcaneCaster) names.Add("Arcane");
            if (bp.IsDivineCaster) names.Add("Divine");
            if (bp.IsMythic) names.Add("Mythic");
            return names;
        }
        public static List<string> CollationNames(this BlueprintSpellbook bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.CharacterClass.IsDivineCaster) names.Add("Divine");
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        public static List<string> CollationNames(this BlueprintBuff bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.Harmful) names.Add("Harmful");
            if (bp.RemoveOnRest) names.Add("Rest Removes");
            if (bp.RemoveOnResurrect) names.Add("Res Removes");
            if (bp.Ranks > 0) names.Add($"{bp.Ranks} Ranks");

            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        #if Wrath
        public static List<string> CollationNames(this BlueprintIngredient bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.Destructible) names.Add("Destructible");
            if (bp.FlavorText != null) names.Add(bp.FlavorText);
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        #endif
        public static List<string> CollationNames(this BlueprintArea bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            var typeName = bp.GetType().Name.Replace("Blueprint", "");
            if (typeName == "Area") names.Add($"Area CR{bp.CR}");
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        public static List<string> CollationNames(this BlueprintEtude bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            //foreach (var item in bp.ActivationCondition) {
            //    names.Add(item.name.yellow());
            //}
            //names.Add(bp.ValidationStatus.ToString().yellow());
            //if (bp.HasParent) names.Add($"P:".yellow() + bp.Parent.NameSafe());
            //foreach (var sibling in bp.StartsWith) {
            //    names.Add($"W:".yellow() + bp.Parent.NameSafe());
            //}
            //if (bp.HasLinkedAreaPart) names.Add($"area {bp.LinkedAreaPart.name}".yellow());
            //foreach (var condition in bp.ActivationCondition?.Conditions)
            //    names.Add(condition.GetCaption().yellow());
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
        public static string[] CaptionNames(this SimpleBlueprint bp) => bp.m_AllElements?.OfType<Condition>()?.Select(e => e.GetCaption() ?? "")?.ToArray() ?? new string[] { };
        public static List<String> CaptionCollationNames(this SimpleBlueprint bp) => bp.CollationNames(bp.CaptionNames());
        // Custom Attributes that Owlcat uses 
        #if Wrath
        public static IEnumerable<InfoBoxAttribute> GetInfoBoxes(this SimpleBlueprint bp) => bp.GetAttributes<InfoBoxAttribute>();
        public static string GetInfoBoxDescription(this SimpleBlueprint bp) => string.Join("\n", bp.GetInfoBoxes().Select(attr => attr.Text));
        #endif

        private static readonly Dictionary<Type, IEnumerable<SimpleBlueprint>> blueprintsByType = new();
        public static IEnumerable<SimpleBlueprint> BlueprintsOfType(Type type) {
            if (blueprintsByType.TryGetValue(type, out var ofType)) return ofType;
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<SimpleBlueprint>();
            var filtered = blueprints.Where((bp) => bp?.GetType().IsKindOf(type) == true).ToList();
            // FIXME - why do we get inconsistent partial results if we cache here
            //if (filtered.Count > 0)
            //    blueprintsByType[type] = filtered;
            return filtered;
        }

        public static IEnumerable<BPType> BlueprintsOfType<BPType>() where BPType : SimpleBlueprint {
            var type = typeof(BPType);
            if (blueprintsByType.TryGetValue(type, out var value)) return value.OfType<BPType>();
            var blueprints = BlueprintLoader.Shared.GetBlueprints<BPType>();
            if (blueprints == null) return new List<BPType>();
            var filtered = blueprints.Where((bp) => bp != null).ToList();
            // FIXME - why do we get inconsistent partial results if we cache here
            //if (filtered.Count > 0)
            //    blueprintsByType[type] = filtered;
            return filtered;
        }

        public static IEnumerable<T> GetBlueprints<T>() where T : SimpleBlueprint => BlueprintsOfType<T>();
#if Wrath        
        public static int GetSelectableFeaturesCount(this BlueprintFeatureSelection selection, UnitDescriptor unit) {
            var count = 0;
            var component = selection.GetComponent<NoSelectionIfAlreadyHasFeature>();
            if (component == null)
                return count;
            if (component.AnyFeatureFromSelection) {
                count += selection.AllFeatures.Count(allFeature => !unit.Progression.Features.HasFact((BlueprintFact)allFeature));
            }
            count += component.Features.Count(feature => !unit.Progression.Features.HasFact((BlueprintFact)feature));
            return count;
        }

        // BlueprintFeatureSelection Helpers
        public class FeatureSelectionEntry {
            public BlueprintFeature feature = null;
            public int level = 0;
            public FeatureSelectionData data;
        }
        public static bool HasFeatureSelection(this UnitEntityData ch, BlueprintFeatureSelection bp, BlueprintFeature feature) {
            var progression = ch?.Descriptor?.Progression;
            if (progression == null) return false;
            if (!progression.Features.HasFact(bp)) return false;
            return progression.Selections.TryGetValue(bp, out var selection) 
                   && selection.SelectionsByLevel.Values.Any(l => l.Any(f => f == feature));
        }
        public static List<BlueprintFeature> FeatureSelectionValues(this UnitEntityData ch, BlueprintFeatureSelection bp) => bp.AllFeatures.Where(f => ch.HasFeatureSelection(bp, f)).ToList();
        public static List<FeatureSelectionEntry> FeatureSelectionEntries(this UnitEntityData ch, BlueprintFeatureSelection bp)
            => (from pair in ch.Descriptor.Progression.Selections
                where pair.Key == bp
                from byLevelPair in pair.Value.SelectionsByLevel
                from feature in byLevelPair.Value
                select new FeatureSelectionEntry { feature = feature, level = byLevelPair.Key, data = pair.Value }).ToList();
        public static void AddFeatureSelection(this UnitEntityData ch, BlueprintFeatureSelection bp, BlueprintFeature feature, int level = 1) {
            var source = new FeatureSource();
            ch?.Progression?.AddSelection(bp, source, level, feature);
            var featureCollection = ch?.Progression?.Features;
            if (featureCollection == null) return;
            var fact = new Feature(feature, featureCollection.Owner, null);
            fact = featureCollection.Manager.Add<Feature>(fact);
            fact?.SetSource(source, level);
            // ch?.Progression?.Features.AddFeature(bp).SetSource(source, 1);
        }
        public static void RemoveFeatureSelection(this UnitEntityData ch, BlueprintFeatureSelection bp, FeatureSelectionData data, BlueprintFeature feature) {
            var progression = ch?.Descriptor?.Progression;
            if (progression == null) return;
            var fact = progression.Features.GetFact(feature);
            BlueprintFeatureSelection featureSelection = null;
            FeatureSelectionData featureSelectionData = null;
            var level = -1;
            foreach (var selection in progression.Selections) {
                foreach (var keyValuePair in selection.Value.SelectionsByLevel.Where(keyValuePair => keyValuePair.Value.HasItem<BlueprintFeature>(feature))) {
                    featureSelection = selection.Key;
                    featureSelectionData = selection.Value;
                    level = keyValuePair.Key;
                    break;
                }
                if (level >= 0)
                    break;
            }
            if (featureSelection != null) {
                featureSelectionData.RemoveSelection(level, feature);
            }
            if (fact == null) return;
            progression.Features.RemoveFact(fact);
        }
        // BlueprintParametrizedFeature Helpers
        public static bool HasParameterizedFeatureItem(this UnitEntityData ch, BlueprintParametrizedFeature bp, IFeatureSelectionItem item) {
            if (!bp.Items.Any()) return false;
            var existing = ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == item.Param);
            return existing != null;
        }
        public static List<IFeatureSelectionItem> ParameterizedFeatureItems(this UnitEntityData ch, BlueprintParametrizedFeature bp) => bp.Items.Where(f => ch.HasParameterizedFeatureItem(bp, f)).ToList();
        public static void AddParameterizedFeatureItem(this UnitEntityData ch, BlueprintParametrizedFeature bp, IFeatureSelectionItem item) => ch?.Descriptor?.AddFact<UnitFact>(bp, null, item.Param);
        public static void RemoveParameterizedFeatureItem(this UnitEntityData ch, BlueprintParametrizedFeature bp, IFeatureSelectionItem item) {
            var fact = ch.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == item.Param);
            ch?.Progression?.Features?.RemoveFact(fact);
        }
#endif
    }
}