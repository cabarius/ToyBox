#if RT
// common alternate usings
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
//global using Owlcat.Runtime.UI.Utility;
global using static Kingmaker.Utility.MassLootHelper;
// Type Aliases
global using BlueprintGuid = System.String;
global using BlueprintFeatureSelection = Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection.BlueprintFeatureSelection_Obsolete;
global using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
global using UnitProgressionData = Kingmaker.UnitLogic.PartUnitProgression;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem;
#elif Wrath
using Kingmaker.Blueprints.Items;
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
        public static float GetCost(this BlueprintItem item) => item.ProfitFactorCost;
        public static Etude GetFact(EtudesTree tree, BlueprintEtude blueprint) => tree.RawFacts.FirstItem(i => i.Blueprint == blueprint);
#elif Wrath
        public static UnitDescriptor Descriptor(this UnitEntityData entity) => entity.Descriptor;
        public static float GetCost(this BlueprintItem item) => item.Cost;
#endif
    }
}
