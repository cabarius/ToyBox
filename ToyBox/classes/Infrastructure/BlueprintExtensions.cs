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

namespace ToyBox {

    public static partial class BlueprintExensions {
        public static Settings settings => Main.settings;

        private static ConditionalWeakTable<object, List<string>> cachedCollationNames = new() { };
        private static HashSet<BlueprintGuid> badList = new();
        public static void ResetCollationCache() => cachedCollationNames = new() { };
        private static void AddOrUpdateCachedNames(SimpleBlueprint bp, List<string> names) {
            if (cachedCollationNames.TryGetValue(bp, out _)) {
                cachedCollationNames.Remove(bp);
            }
            cachedCollationNames.Add(bp, names);
        }

        public static string GetDisplayName(this SimpleBlueprint bp) => bp switch {
            BlueprintAbilityResource abilityResource => abilityResource.Name,
            BlueprintArchetype archetype => archetype.Name,
            BlueprintCharacterClass charClass => charClass.Name,
            BlueprintItem item => item.Name,
            BlueprintItemEnchantment enchant => enchant.Name,
            BlueprintUnitFact fact => fact.Name,
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
                cachedCollationNames.Add(bp, names);
            }

            if (extras != null) names = names.Concat(extras).ToList();
            return names;
        }
        public static List<string> CollationNames(this SimpleBlueprint bp, params string[] extras) => DefaultCollationNames(bp, extras);
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

        private static readonly Dictionary<Type, IEnumerable<SimpleBlueprint>> blueprintsByType = new();
        public static IEnumerable<SimpleBlueprint> BlueprintsOfType(Type type) {
            if (blueprintsByType.ContainsKey(type)) return blueprintsByType[type];
            var blueprints = BlueprintLoader.Shared.GetBlueprints();
            if (blueprints == null) return new List<SimpleBlueprint>();
            var filtered = blueprints.Where((bp) => bp.GetType().IsKindOf(type)).ToList();
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
    }
}