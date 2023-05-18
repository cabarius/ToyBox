#if RT
// common alternate usings
global using Kingmaker.Blueprints.Base;
global using Kingmaker.EntitySystem;
global using Kingmaker.EntitySystem.Entities.Base;
global using Kingmaker.EntitySystem.Stats.Base;
global using Kingmaker.PubSubSystem.Core;
global using Kingmaker.UI.Models.Tooltip.Base;
global using Kingmaker.UnitLogic.Levelup.Obsolete;
global using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints;
global using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection;
global using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Spells;
global using Kingmaker.UnitLogic.Progression.Features;
global using Kingmaker.Utility.DotNetExtensions;
global using Kingmaker.Utility.UnityExtensions;
global using Kingmaker.Code.UI.MVVM;
global using Kingmaker.Code.UI.MVVM.VM.Loot;
global using Owlcat.Runtime.Core;
global using static Kingmaker.Utility.MassLootHelper;

// Type Aliases
global using BlueprintGuid = System.String;
global using BlueprintFeatureSelection = Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection.BlueprintFeatureSelection_Obsolete;
global using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
global using UnitProgressionData = Kingmaker.UnitLogic.PartUnitProgression;

using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic.Parts;
#elif Wrath
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using ModKit;
using ModKit.Utility;

namespace ToyBox {
    public static class Compatibility {
#if RT
        public static bool IsNullOrEmpty(this string str) => str == null || str.Length == 0;
        public static UnitEntityData Descriptor(this UnitEntityData entity) => entity;
        public static float GetCost(this BlueprintItem item) => item.ProfitFactorCost;
        public static Etude GetFact(this EtudesTree tree, BlueprintEtude blueprint) => tree.RawFacts.FirstItem(i => i.Blueprint == blueprint);

        // Hacks to get around ambiguity in Description due to te ILootable interface in BaseUnitEntity
        public static Gender? GetCustomGender(this UnitEntityData unit) {
            var unitType = unit.GetType();
            var description = unitType.GetPropValue<PartUnitDescription>("Description");
            return description.CustomGender;
        }
        public static void SetCustomGender(this UnitEntityData unit, Gender gender) {
            var unitType = unit.GetType();
            var description = unitType.GetPropValue<PartUnitDescription>("Description");
            description.CustomGender = gender;
        }
#elif Wrath
        public static UnitDescriptor Descriptor(this UnitEntityData entity) => entity.Descriptor;
        public static float GetCost(this BlueprintItem item) => item.Cost;
        public static Gender? GetCustomGender(this UnitDescriptor descriptor) => descriptor.CustomGender;
        public static void SetCustomGender(this UnitDescriptor descriptor, Gender gender) => descriptor.CustomGender = gender;
#endif
    }
}
