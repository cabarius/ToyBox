#if RT
global using BlueprintGuid = System.String;
global using BlueprintFeatureSelection = Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection.BlueprintFeatureSelection_Obsolete;
global using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
    public static class Compatibility {
#if RT
        public static bool IsNullOrEmpty(this string str) => str == null || str.Length == 0;
#endif
    }
}
