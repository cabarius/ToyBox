#if RT
// common alternate usings
global using Kingmaker.EntitySystem.Entities.Base;
global using Kingmaker.EntitySystem.Stats.Base;
global using Kingmaker.PubSubSystem.Core;
global using Kingmaker.UI.Models.Tooltip.Base;
global using Kingmaker.UnitLogic.Levelup.Obsolete;
global using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints;
global using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection;
global using Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Spells;
global using Kingmaker.UnitLogic.Progression.Features;
global using Kingmaker.Utility.UnityExtensions;
global using Owlcat.Runtime.Core;
global using Owlcat.Runtime.UI.Utility;

// Type Aliases
global using BlueprintGuid = System.String;
global using BlueprintFeatureSelection = Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection.BlueprintFeatureSelection_Obsolete;
global using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
global using UnitProgressionData = Kingmaker.UnitLogic.PartUnitProgression;
using Kingmaker.EntitySystem;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;

namespace ToyBox {
    public static class Compatibility {
#if RT
        public static bool IsNullOrEmpty(this string str) => str == null || str.Length == 0;
        public static UnitEntityData Descriptor(this UnitEntityData entity) => entity;
#elif Wrath
        public static UnitDescriptor Descriptor(this UnitEntityData entity) => entity.Descriptor;
#endif
    }
}
