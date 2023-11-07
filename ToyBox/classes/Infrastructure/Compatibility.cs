// global statics
// common alternate using
global using Kingmaker.Blueprints.Base;
global using Kingmaker.Code.UI.MVVM;
global using Kingmaker.Code.UI.MVVM.VM.Loot;
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
global using Owlcat.Runtime.Core;
global using Owlcat.Runtime.Core.Utility;
global using static Kingmaker.Utility.MassLootHelper;
global using static ModKit.UI;
global using UnitOvertipView = Kingmaker.Code.UI.MVVM.View.Overtips.Unit.OvertipUnitView;
global using EntityOvertipVM = Kingmaker.Code.UI.MVVM.VM.Overtips.Unit.OvertipEntityUnitVM;
global using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
global using UnitDescriptor = Kingmaker.EntitySystem.Entities.MechanicEntity;
global using BlueprintFeatureSelection = Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection.BlueprintFeatureSelection_Obsolete;
global using UnitProgressionData = Kingmaker.UnitLogic.PartUnitProgression;
// Type Aliases
global using BlueprintGuid = System.String;
using JetBrains.Annotations;
using Kingmaker;
// Local using
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameCommands;
using Kingmaker.Localization;
using Kingmaker.Localization;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Levelup.Components;
using Kingmaker.UnitLogic.Parts;
using ModKit;
using ModKit.Utility;
using RewiredConsts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniRx;

namespace ToyBox {
    public static partial class Shodan {

        // General Stuff
        public static string StringValue(this LocalizedString locStr) => locStr.Text;
        public static bool IsNullOrEmpty(this string str) => str == null || str.Length == 0;
        public static UnitEntityData Descriptor(this UnitEntityData entity) => entity;
        public static float GetCost(this BlueprintItem item) => item.ProfitFactorCost;
        public static Etude GetFact(this EtudesTree tree, BlueprintEtude blueprint) => tree.RawFacts.FirstItem(i => i.Blueprint == blueprint);

        // Hacks to get around ambiguity in Description due to te ILootable interface in BaseUnitEntity
        public static Gender? GetCustomGender(this UnitEntityData unit) {
            return unit.Description.CustomGender;
        }
        public static void SetCustomGender(this UnitEntityData unit, Gender gender) {
            unit.Description.CustomGender = gender;
        }
        public static bool CanPlay(this BlueprintEtude etudeBP) {
            try {
                var etudesTree = Game.Instance.Player.EtudesSystem.Etudes;
                var etude = etudesTree.Get(etudeBP);
                if (etude != null)
                    return etudesTree.EtudeCanPlay(etude);
            }
            catch (Exception ex) {
                Mod.Error(ex);
            }
            return true;
        }
        public static Dictionary<string, object> GetInGameSettingsList() => Game.Instance?.State?.InGameSettings?.List;

        // Unit Entity Utils
        public static UnitEntityData MainCharacter => Game.Instance.Player.MainCharacterEntity;
        public static EntityPool<AbstractUnitEntity>? AllUnits => Game.Instance?.State?.AllUnits;
        public static EntityPool<UnitEntityData>? AllBaseUnits => Game.Instance?.State?.AllBaseUnits;
        public static List<UnitEntityData> SelectedUnits => UIAccess.SelectionManager.SelectedUnits.ToList();
        public static ReactiveCollection<UnitEntityData> SelectedUnitsReactive() => UIAccess.SelectionManager.SelectedUnits;
        public static bool IsEnemy(this UnitEntityData unit) {
            PartFaction factionOptional = unit.GetFactionOptional();
            return factionOptional != null && factionOptional.IsPlayerEnemy;
        }
        public static bool IsPlayerFaction(this UnitEntityData unit) {
            PartFaction factionOptional = unit.GetFactionOptional();
            return factionOptional != null && factionOptional.IsPlayer;
        }
        public static void KillUnit(UnitEntityData unit) => CheatsCombat.KillUnit(unit);
        public static bool ToyBoxIsPartyOrPet(this MechanicEntity entity) => Game.Instance.Player.PartyAndPets.Contains(entity);
        public static bool HasBonusForLevel(this BlueprintStatProgression xpTable, int level) => level >= 0 && level < xpTable.Bonuses.Length;
        public static float GetMaxSpeed(List<UnitEntityData> data) => data.Select(u => u.OwnerEntity.Movable.ModifiedSpeedMps).Max();

        public static void EnterToArea(BlueprintAreaEnterPoint enterPoint) => Game.Instance.LoadArea(enterPoint, AutoSaveMode.None, null);

        // disabled for now in beta
        public static bool CanRespec(this BaseUnitEntity ch) {
            if (ch == null)
                return false;
            var component = ch.OriginalBlueprint.GetComponent<CharacterLevelLimit>();
            var levelLimit = component != null ? component.LevelLimit : 0;
            return !ch.LifeState.IsDead && !ch.IsPet && ch.Progression.CharacterLevel > levelLimit;
        }
        public static void DoRespec(this BaseUnitEntity ch) {
            Game.Instance.Player.RespecCompanion(ch);
        }
    }
}
