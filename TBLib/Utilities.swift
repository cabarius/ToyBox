import System
import System.Collections.Generic
import System.Linq
import System.Text
import System.Threading.Tasks
import UnityEngine
import UnityModManagerNet
import UnityEngine.UI
import HarmonyLib
import Kingmaker
import Kingmaker.AI.Blueprints
import Kingmaker.AI.Blueprints.Considerations
import Kingmaker.AreaLogic.Cutscenes
import Kingmaker.AreaLogic.Etudes
import Kingmaker.Armies
import Kingmaker.Armies.TacticalCombat
import Kingmaker.Armies.TacticalCombat.Blueprints
import Kingmaker.Armies.TacticalCombat.Brain
import Kingmaker.Armies.TacticalCombat.Brain.Considerations
import Kingmaker.BarkBanters
import Kingmaker.Blueprints
import Kingmaker.Blueprints.Area
import Kingmaker.Blueprints.CharGen
import Kingmaker.Blueprints.Classes
import Kingmaker.Blueprints.Credits
import Kingmaker.Blueprints.Encyclopedia
import Kingmaker.Blueprints.Facts
import Kingmaker.Blueprints.Items
import Kingmaker.Blueprints.Items.Ecnchantments
import Kingmaker.Blueprints.Items.Armors
import Kingmaker.Blueprints.Items.Components
import Kingmaker.Blueprints.Items.Equipment
import Kingmaker.Blueprints.Items.Shields
import Kingmaker.Blueprints.Items.Weapons
import Kingmaker.Kingdom.Blueprints
import Kingmaker.Kingdom.Settlements
import Kingmaker.Blueprints.Quests
import Kingmaker.Blueprints.Root
import Kingmaker.Cheats
import Kingmaker.Blueprints.Console
import Kingmaker.Controllers.Rest
import Kingmaker.Designers
import Kingmaker.DialogSystem.Blueprints
import Kingmaker.Dungeon.Blueprints
import Kingmaker.EntitySystem.Entities
import Kingmaker.EntitySystem.Stats
import Kingmaker.GameModes
import Kingmaker.Globalmap.Blueprints
import Kingmaker.Interaction
import Kingmaker.Items
import Kingmaker.PubSubSystem
import Kingmaker.RuleSystem
import Kingmaker.RuleSystem.Rules.Damage
import Kingmaker.Tutorial
import Kingmaker.UI
import Kingmaker.UI.Common
import Kingmaker.UnitLogic
import Kingmaker.UnitLogic.Buffs
import Kingmaker.UnitLogic.Buffs.Blueprints
import Kingmaker.UnitLogic.Customization
import Kingmaker.Utility
import Kingmaker.Visual.Sound

public class NamedTypeFilter {
    public var name: String
    public var type: Type

    public init(_ name: String, type: Type) {
        self.name = name
        self.type = type
    }
}