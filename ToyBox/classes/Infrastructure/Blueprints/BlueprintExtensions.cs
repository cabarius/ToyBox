// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Craft;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using System.Runtime.CompilerServices;
using ModKit;
using Kingmaker.Blueprints.Items.Weapons;
using HarmonyLib;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;

namespace ToyBox {

    public static partial class BlueprintExtensions {
        public static Settings settings => Main.settings;

        private static ConditionalWeakTable<object, List<string>> cachedCollationNames = new() { };
        private static HashSet<BlueprintGuid> badList = new();
        public static void ResetCollationCache() => cachedCollationNames = new() { };
        private static void AddOrUpdateCachedNames(SimpleBlueprint bp, List<string> names) {
            names = names.Distinct().ToList();
            if (cachedCollationNames.TryGetValue(bp, out _)) {
                cachedCollationNames.Remove(bp);
                //Mod.Log($"removing: {bp.NameSafe()}");
            }
            cachedCollationNames.Add(bp, names);
            //Mod.Log($"adding: {bp.NameSafe()} - {names.Count} - {String.Join(", ", names)}");
        }

        public static string GetDisplayName(this SimpleBlueprint bp) => bp switch {
            BlueprintAbilityResource abilityResource => abilityResource.Name,
            BlueprintArchetype archetype => archetype.Name,
            BlueprintCharacterClass charClass => charClass.Name,
            BlueprintItem item => item.Name,
            BlueprintItemEnchantment enchant => enchant.Name,
            BlueprintUnitFact fact => fact.NameSafe(),
            _ => bp.name
        };
        public static string GetDisplayName(this BlueprintSpellbook bp) {
            var name = bp.DisplayName;
            if (name == null || name.Length == 0) name = bp.name.Replace("Spellbook", "");
            return name;
        }
        public static IEnumerable<string> Attributes(this SimpleBlueprint bp) {
            List<string> modifers = new();
            if (badList.Contains(bp.AssetGuid)) return modifers;
            var traverse = Traverse.Create(bp);
            foreach (var property in Traverse.Create(bp).Properties()) {
                if (property.StartsWith("Is")) {
                    try {
                        var value = traverse.Property<bool>(property)?.Value;
                        if (value.HasValue && value.GetValueOrDefault()) {
                            modifers.Add(property); //.Substring(2));
                        }
                    }
                    catch (Exception e) {
                        Mod.Warn($"${bp.name}.{property} thew an exception: {e.Message}");
                        badList.Add(bp.AssetGuid);
                        break;
                    }
                }
            }
            return modifers;
        }
        private static List<string> DefaultCollationNames(this SimpleBlueprint bp, string[] extras) {
            cachedCollationNames.TryGetValue(bp, out var names);
            if (names == null) {
                names = new List<string> { };
                var typeName = bp.GetType().Name.Replace("Blueprint", "");
                //var stripIndex = typeName.LastIndexOf("Blueprint");
                //if (stripIndex > 0) typeName = typeName.Substring(stripIndex + "Blueprint".Length);
                names.Add(typeName);
                foreach (var attribute in bp.Attributes())
                    names.Add(attribute.orange());
                cachedCollationNames.Add(bp, names.Distinct().ToList());
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
        public static List<string> CollationNames(this BlueprintIngredient bp, params string[] extras) {
            var names = DefaultCollationNames(bp, extras);
            if (bp.Destructible) names.Add("Destructible");
            if (bp.FlavorText != null) names.Add(bp.FlavorText);
            AddOrUpdateCachedNames(bp, names);
            return names;
        }
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
        public static IEnumerable<InfoBoxAttribute> GetInfoBoxes(this SimpleBlueprint bp) => bp.GetAttributes<InfoBoxAttribute>();

        public static string GetInfoBoxDescription(this SimpleBlueprint bp) => String.Join("\n", bp.GetInfoBoxes().Select(attr => attr.Text));

        private static readonly Dictionary<Type, IEnumerable<SimpleBlueprint>> blueprintsByType = new();
        public static IEnumerable<SimpleBlueprint> BlueprintsOfType(Type type) {
            if (blueprintsByType.ContainsKey(type)) return blueprintsByType[type];
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
            if (blueprintsByType.ContainsKey(type)) return blueprintsByType[type].OfType<BPType>();
            var blueprints = BlueprintLoader.Shared.GetBlueprints<BPType>();
            if (blueprints == null) return new List<BPType>();
            var filtered = blueprints.Where((bp) => bp is BPType).ToList();
            // FIXME - why do we get inconsistent partial results if we cache here
            //if (filtered.Count > 0)
            //    blueprintsByType[type] = filtered;
            return filtered;
        }

        public static IEnumerable<T> GetBlueprints<T>() where T : SimpleBlueprint => BlueprintsOfType<T>();
        public static int GetSelectableFeaturesCount(this BlueprintFeatureSelection selection, UnitDescriptor unit) {
            var count = 0;
            var component = selection.GetComponent<NoSelectionIfAlreadyHasFeature>();
            if (component == null)
                return count;
            if (component.AnyFeatureFromSelection) {
                foreach (var allFeature in selection.AllFeatures) {
                    if (!unit.Progression.Features.HasFact((BlueprintFact)allFeature)) {
                        count++;
                    }
                }
            }
            foreach (var feature in component.Features) {
                if (!unit.Progression.Features.HasFact((BlueprintFact)feature)) {
                    count++;
                }
            }
            return count;
        }

        // BlueprintFeatureSelection Helpers
        public static bool HasFeatureSelection(this UnitEntityData ch, BlueprintFeatureSelection bp, BlueprintFeature feature) {
            var progression = ch?.Descriptor?.Progression;
            if (progression == null) return false;
            if (!progression.Features.HasFact(bp)) return false;
            if (progression.Selections.TryGetValue(bp, out var selection)) {
                if (selection.SelectionsByLevel.Values.Any(l => l.Any(f => f == feature))) return true;
            }
            return false;
        }
        public static List<BlueprintFeature> FeatureSelectionValues(this UnitEntityData ch, BlueprintFeatureSelection bp) => bp.AllFeatures.Where(f => ch.HasFeatureSelection(bp, f)).ToList();
        public static void AddFeatureSelection(this UnitEntityData ch, BlueprintFeatureSelection bp, BlueprintFeature feature) {
            var source = new FeatureSource();
            ch?.Descriptor?.Progression.Features.AddFeature(bp).SetSource(source, 1);
            ch?.Progression?.AddSelection(bp, source, 0, feature);
        }
        public static void RemoveFeatureSelection(this UnitEntityData ch, BlueprintFeatureSelection bp, BlueprintFeature feature) {
            // FIXME - fix this
#if false
            var progression = ch?.Descriptor?.Progression;
            var fact = ch.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == feature);
            var selections = ch?.Descriptor?.Progression.Selections;
            BlueprintFeatureSelection featureSelection = null;
            FeatureSelectionData featureSelectionData = null;
            var level = -1;
            foreach (var selection in selections) {
                foreach (var keyValuePair in selection.Value.SelectionsByLevel) {
                    if (keyValuePair.Value.HasItem<BlueprintFeature>(bp)) {
                        featureSelection = selection.Key;
                        featureSelectionData = selection.Value;
                        level = keyValuePair.Key;
                        break;
                    }
                }
                if (level >= 0)
                    break;
            }
            featureSelectionData?.RemoveSelection(level, feature);
            progression.Features.RemoveFact(bp);
#endif
        }

        // BlueprintParametrizedFeature Helpers
        public static bool HasParamemterizedFeatureItem(this UnitEntityData ch, BlueprintParametrizedFeature bp, IFeatureSelectionItem item) {
            if (bp.Items.Count() == 0) return false;
            var existing = ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == item.Param);
            return existing != null;
        }
        public static List<IFeatureSelectionItem> ParamterizedFeatureItems(this UnitEntityData ch, BlueprintParametrizedFeature bp) => bp.Items.Where(f => ch.HasParamemterizedFeatureItem(bp, f)).ToList();
        public static void AddParameterizedFeatureItem(this UnitEntityData ch, BlueprintParametrizedFeature bp, IFeatureSelectionItem item) {
            ch?.Descriptor?.AddFact<UnitFact>(bp, null, item.Param);
        }
        public static void RemoveParameterizedFeatureItem(this UnitEntityData ch, BlueprintParametrizedFeature bp, IFeatureSelectionItem item) {
            var fact = ch.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == item.Param);
            ch?.Progression?.Features?.RemoveFact(fact);
        }

    }
}