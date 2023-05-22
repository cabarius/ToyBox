// global statics
global using static ModKit.UI;
#if RT
// common alternate using
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
global using Owlcat.Runtime.Core.Utility;
global using static Kingmaker.Utility.MassLootHelper;
#elif Wrath
global using Epic.OnlineServices.Lobby;
global using Owlcat.Runtime.Core.Utils;
#endif

// Type Aliases
#if RT
global using BlueprintGuid = System.String;
global using BlueprintFeatureSelection = Kingmaker.UnitLogic.Levelup.Obsolete.Blueprints.Selection.BlueprintFeatureSelection_Obsolete;
global using UnitEntityData = Kingmaker.EntitySystem.Entities.BaseUnitEntity;
global using UnitProgressionData = Kingmaker.UnitLogic.PartUnitProgression;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Localization;
using UniRx;
#endif

#if RT
// Local using
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Levelup.Components;
using Kingmaker.GameCommands;
#elif Wrath
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.Cheats;
using Kingmaker.EntitySystem;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Localization;
using Kingmaker.UnitLogic;
using ModKit;
using ModKit.Utility;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.Cheats;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI;
using RewiredConsts;


namespace ToyBox {
    public static partial class Shodan {

        // General Stuff
#if RT
        public static string StringValue(this LocalizedString locStr) => locStr.Text;
        public static bool IsNullOrEmpty(this string str) => str == null || str.Length == 0;
        public static UnitEntityData Descriptor(this UnitEntityData entity) => entity;
        public static float GetCost(this BlueprintItem item) => item.ProfitFactorCost;
        public static Etude GetFact(this EtudesTree tree, BlueprintEtude blueprint) => tree.RawFacts.FirstItem(i => i.Blueprint == blueprint);

        // Hacks to get around ambiguity in Description due to te ILootable interface in BaseUnitEntity
        public static Gender? GetCustomGender(this UnitEntityData unit) {
            return null;
            var unitType = unit.GetType();
            var description = unitType.GetPropValue<PartUnitDescription>("Description");
            return description.CustomGender;
        }
        public static void SetCustomGender(this UnitEntityData unit, Gender gender) {
            return;
            var unitType = unit.GetType();
            var description = unitType.GetPropValue<PartUnitDescription>("Description");
            description.CustomGender = gender;
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
#elif Wrath
        public static string StringValue(this LocalizedString locStr) => locStr.ToString();
        public static UnitDescriptor Descriptor(this UnitEntityData entity) => entity.Descriptor;
        public static float GetCost(this BlueprintItem item) => item.Cost;
        public static Gender? GetCustomGender(this UnitDescriptor descriptor) => descriptor.CustomGender;
        public static void SetCustomGender(this UnitDescriptor descriptor, Gender gender) => descriptor.CustomGender = gender;
        public static Dictionary<string, object> GetInGameSettingsList() => Game.Instance?.Player?.SettingsList;
#endif

        // Unit Entity Utils
#if RT
        public static UnitEntityData MainCharacter => Game.Instance.Player.MainCharacter.Entity;
        public static EntityPool<UnitEntityData> AllUnits => Game.Instance?.State?.AllUnits;
        public static List<UnitEntityData> SelectedUnits => UIAccess.SelectionManager.SelectedUnits.ToList();
        public static ReactiveCollection<UnitEntityData> SelectedUnitsReactive() => UIAccess.SelectionManager.SelectedUnits;
        public static bool IsEnemy(UnitEntityData unit) => unit.CombatGroup.IsEnemy(GameHelper.GetPlayerCharacter())  && unit != GameHelper.GetPlayerCharacter();
        public static void KillUnit(UnitEntityData unit) => CheatsCombat.KillUnit(unit);
#elif Wrath
        public static UnitEntityData MainCharacter => Game.Instance.Player.MainCharacter.Value;
        public static EntityPool<UnitEntityData> AllUnits => Game.Instance?.State?.Units;
        public static List<UnitEntityData> SelectedUnits => Game.Instance.UI.SelectionManager.SelectedUnits;
        public static bool IsEnemy(UnitEntityData unit) => unit.IsPlayersEnemy && unit != GameHelper.GetPlayerCharacter();
        public static void KillUnit(UnitEntityData unit) => GameHelper.KillUnit(unit);

        // Teleport and Travel
#endif
#if RT
        public static void EnterToArea(BlueprintAreaEnterPoint enterPoint) => Game.Instance.GameCommandQueue.AreaTransition(enterPoint, AutoSaveMode.None, false);
#elif Wrath
        public static void EnterToArea(BlueprintAreaEnterPoint enterPoint) => GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
#endif

#if RT
        // disabled for now in beta
        public static bool CanRespec(this BaseUnitEntity ch) {
            return false;
            if (ch == null)
                return false;
            var component = ch.OriginalBlueprint.GetComponent<CharacterLevelLimit>();
            var levelLimit = component != null ? component.LevelLimit : 0;
            return !ch.LifeState.IsDead && !ch.IsPet && ch.Progression.CharacterLevel > levelLimit;
        }
        public static void DoRespec(this BaseUnitEntity ch) {
            Game.Instance.Player.RespecCompanion(ch);
        }
#elif Wrath
        public static bool CanRespec(this UnitEntityData ch) {
            return RespecHelper.GetRespecableUnits().Contains(ch);
        }
        public static void DoRespec(this UnitEntityData ch) {
            RespecHelper.Respec(ch);
        }
#endif
    }
}
