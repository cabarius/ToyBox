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

namespace ToyBox {

    public static class BlueprintExensions {
        private static ConditionalWeakTable<object, string> cachedCollationNames = new ConditionalWeakTable<object, string> { };
        public static String GetDisplayName(this SimpleBlueprint bp) { return bp.name; }
        public static String GetDisplayName(this BlueprintSpellbook bp) {
            var name = bp.DisplayName;
            if (name == null || name.Length == 0) name = bp.name.Replace("Spellbook", "");
            return name;
        }
        public static String CollationName(this SimpleBlueprint bp) {
            string collationName;
            cachedCollationNames.TryGetValue(bp, out collationName);
            if (collationName != null) return collationName;
            var typeName = bp.GetType().ToString();
            var stripIndex = typeName.LastIndexOf("Blueprint");
            if (stripIndex > 0) typeName = typeName.Substring(stripIndex + "Blueprint".Length);
            cachedCollationNames.Add(bp, typeName);
            return typeName;
        }
        public static String CollationName(this BlueprintSpellbook bp) {
            if (bp.IsMythic) return "Mythic";
            if (bp.IsAlchemist) return "Alchemist";
            if (bp.IsArcane) return "Arcane";
            if (bp.IsSinMagicSpecialist) return "Specialist";
            if (bp.CharacterClass.IsDivineCaster) return "Divine";
            return bp.GetType().ToString();
        }
        public static String CollationName(this BlueprintBuff bp) {
            if (bp.IsClassFeature) return "Class Feature";
            if (bp.IsFromSpell) return "From Spell";
            if (bp.Harmful) return "Harmful";
            if (bp.RemoveOnRest) return "Rest Removes";
            if (bp.RemoveOnResurrect) return "Res Removes";
            if (bp.Ranks > 0) return $"{bp.Ranks} Ranks";
            return bp.GetType().ToString();
        }

        public static String CollationName(this BlueprintIngredient bp) {
            if (bp.IsNotable) return "Notable";
            //if (bp.AllowMakeStackable) return "Stackable";
            if (bp.Destructible) return "Destructible";
            if (bp.FlavorText != null) return bp.FlavorText;
            return bp.NonIdentifiedName;
        }
        public static String CollationName(this BlueprintArea bp) {
            var typeName = bp.GetType().Name.Replace("Blueprint", "");
            if (typeName == "Area") return $"Area CR{bp.CR}";
            if (bp.IsGlobalMap) return $"GlobalMap";
            if (bp.IsIndoor) return "Indoor";
            return typeName;
        }

        static Dictionary<Type, List<SimpleBlueprint>> blueprintsByType = new Dictionary<Type, List<SimpleBlueprint>>();
        public static List<SimpleBlueprint> BlueprintsOfType(Type type) {
            if (blueprintsByType.ContainsKey(type)) return blueprintsByType[type];
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return new List<SimpleBlueprint>();
            var filtered = blueprints.Where((bp) => bp.GetType().IsKindOf(type)).ToList();
            blueprintsByType[type] = filtered;
            return filtered;
        }

        public static List<SimpleBlueprint> BlueprintsOfType<BPType>() where BPType : SimpleBlueprint {
            var type = typeof(BPType);
            if (blueprintsByType.ContainsKey(type)) return blueprintsByType[type];
            var blueprints = BlueprintBrowser.GetBlueprints();
            if (blueprints == null) return new List<SimpleBlueprint>();
            var filtered = blueprints.Where((bp) => (bp is BPType) ? true : false).ToList();
            blueprintsByType[type] = filtered;
            return filtered;
        }

        public static List<SimpleBlueprint> GetBlueprints<T>() where T : SimpleBlueprint {
            return BlueprintsOfType<T>();
        }
        public static int GetSelectableFeaturesCount(this BlueprintFeatureSelection selection, UnitDescriptor unit) {
            int count = 0;
            NoSelectionIfAlreadyHasFeature component = selection.GetComponent<NoSelectionIfAlreadyHasFeature>();
            if (component == null)
                return count;
            if (component.AnyFeatureFromSelection) {
                foreach (BlueprintFeature allFeature in selection.AllFeatures) {
                    if (!unit.Progression.Features.HasFact((BlueprintFact)allFeature)) {
                        count++;
                    }
                }
            }
            foreach (BlueprintFeature feature in component.Features) {
                if (!unit.Progression.Features.HasFact((BlueprintFact)feature)) {
                    count++;
                }
            }
            return count;
        }
    }
}
