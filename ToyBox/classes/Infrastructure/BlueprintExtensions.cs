﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Craft;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ToyBox {
    public static class BlueprintExensions {
        private static ConditionalWeakTable<object, string> cachedCollationNames = new ConditionalWeakTable<object, string>();

        public static string GetDisplayName(this SimpleBlueprint bp) { return bp.name; }

        public static string GetDisplayName(this BlueprintSpellbook bp) {
            string name = bp.DisplayName;
            if (string.IsNullOrEmpty(name)) {
                name = bp.name.Replace("Spellbook", "");
            }

            return name;
        }

        public static string CollationName(this SimpleBlueprint bp) {
            cachedCollationNames.TryGetValue(bp, out string collationName);

            if (collationName != null) {
                return collationName;
            }

            string typeName = bp.GetType().ToString();
            int stripIndex = typeName.LastIndexOf("Blueprint", StringComparison.Ordinal);
            if (stripIndex > 0) {
                typeName = typeName.Substring(stripIndex + "Blueprint".Length);
            }

            cachedCollationNames.Add(bp, typeName);

            return typeName;
        }

        public static string CollationName(this BlueprintSpellbook bp) {
            if (bp.IsMythic) {
                return "Mythic";
            }

            if (bp.IsAlchemist) {
                return "Alchemist";
            }

            if (bp.IsArcane) {
                return "Arcane";
            }

            if (bp.IsSinMagicSpecialist) {
                return "Specialist";
            }

            if (bp.CharacterClass.IsDivineCaster) {
                return "Divine";
            }

            return bp.GetType().ToString();
        }

        public static string CollationName(this BlueprintBuff bp) {
            if (bp.IsClassFeature) {
                return "Class Feature";
            }

            if (bp.IsFromSpell) {
                return "From Spell";
            }

            if (bp.Harmful) {
                return "Harmful";
            }

            if (bp.RemoveOnRest) {
                return "Rest Removes";
            }

            if (bp.RemoveOnResurrect) {
                return "Res Removes";
            }

            if (bp.Ranks > 0) {
                return $"{bp.Ranks} Ranks";
            }

            return bp.GetType().ToString();
        }

        public static string CollationName(this BlueprintIngredient bp) {
            if (bp.IsNotable) {
                return "Notable";
            }

            //if (bp.AllowMakeStackable) return "Stackable";
            if (bp.Destructible) {
                return "Destructible";
            }

            if (bp.FlavorText != null) {
                return bp.FlavorText;
            }

            return bp.NonIdentifiedName;
        }

        public static string CollationName(this BlueprintArea bp) {
            string typeName = bp.GetType().Name.Replace("Blueprint", "");

            if (typeName == "Area") {
                return $"Area CR{bp.CR}";
            }

            if (bp.IsGlobalMap) {
                return "GlobalMap";
            }

            if (bp.IsIndoor) {
                return "Indoor";
            }

            return typeName;
        }

        static Dictionary<Type, List<SimpleBlueprint>> blueprintsByType = new Dictionary<Type, List<SimpleBlueprint>>();

        public static IEnumerable<SimpleBlueprint> BlueprintsOfType(Type type) {
            if (blueprintsByType.ContainsKey(type)) {
                return blueprintsByType[type];
            }

            var blueprints = BlueprintBrowser.GetBlueprints();

            if (blueprints == null) {
                return new List<SimpleBlueprint>();
            }

            var filtered = blueprints.Where(bp => bp.GetType().IsKindOf(type)).ToList();
            blueprintsByType[type] = filtered;

            return filtered;
        }

        public static IEnumerable<SimpleBlueprint> BlueprintsOfType<BPType>() where BPType : SimpleBlueprint {
            var type = typeof(BPType);

            if (blueprintsByType.ContainsKey(type)) {
                return blueprintsByType[type];
            }

            var blueprints = BlueprintBrowser.GetBlueprints();

            if (blueprints == null) {
                return new List<SimpleBlueprint>();
            }

            var filtered = blueprints.Where(bp => (bp is BPType) ? true : false).ToList();
            blueprintsByType[type] = filtered;

            return filtered;
        }

        public static IEnumerable<SimpleBlueprint> GetBlueprints<T>() where T : SimpleBlueprint {
            return BlueprintsOfType<T>();
        }

        public static int GetSelectableFeaturesCount(this BlueprintFeatureSelection selection, UnitDescriptor unit) {
            int count = 0;
            NoSelectionIfAlreadyHasFeature component = selection.GetComponent<NoSelectionIfAlreadyHasFeature>();

            if (component == null) {
                return count;
            }

            if (component.AnyFeatureFromSelection) {
                foreach (BlueprintFeature allFeature in selection.AllFeatures) {
                    if (!unit.Progression.Features.HasFact(allFeature)) {
                        count++;
                    }
                }
            }

            foreach (BlueprintFeature feature in component.Features) {
                if (!unit.Progression.Features.HasFact(feature)) {
                    count++;
                }
            }

            return count;
        }
    }
}