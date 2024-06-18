// global statics
global using static ModKit.UI;
global using Epic.OnlineServices.Lobby;
global using Owlcat.Runtime.Core.Utils;

// Type Aliases

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;
using Kingmaker.EntitySystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
using Kingmaker.Blueprints.Root;

namespace ToyBox {
    public static partial class Shodan {

        // General Stuff
        public static string StringValue(this LocalizedString locStr) => locStr.ToString();
        public static bool IsNullOrEmpty(this string str) => str == null || str.Length == 0;
        public static UnitDescriptor Descriptor(this UnitEntityData entity) => entity.Descriptor;
        public static float GetCost(this BlueprintItem item) => item.Cost;
        public static Gender? GetCustomGender(this UnitDescriptor descriptor) => descriptor.CustomGender;
        public static void SetCustomGender(this UnitDescriptor descriptor, Gender gender) => descriptor.CustomGender = gender;
        public static Dictionary<string, object>? GetInGameSettingsList() => Game.Instance?.Player?.SettingsList;

        // Unit Entity Utils
        public static UnitEntityData MainCharacter => Game.Instance.Player.MainCharacter.Value;
        public static EntityPool<UnitEntityData>? AllUnits => Game.Instance?.State?.Units;
        public static List<UnitEntityData> SelectedUnits => Game.Instance.UI.SelectionManager.SelectedUnits;
        public static bool IsEnemy(this UnitEntityData unit) {
            UnitAttackFactions uaf = unit.Descriptor.AttackFactions;
            return uaf.m_Owner.Faction.EnemyForEveryone || uaf.m_Factions.Contains(BlueprintRoot.Instance.PlayerFaction);
        }
        public static bool IsPlayerFaction(this UnitEntityData unit) {
            UnitDescriptor ud = unit.Descriptor;
            if (ud.m_IsPlayerFactionCached == null) {
                ud.m_IsPlayerFactionCached = new bool?(ud.Faction == BlueprintRoot.Instance.PlayerFaction);
            }
            return ud.m_IsPlayerFactionCached.Value && !ud.AttackFactions.IsPlayerEnemy;
        }
        public static void KillUnit(UnitEntityData unit) => GameHelper.KillUnit(unit);
        public static bool IsPartyOrPet(this UnitEntityData entity) => entity.Descriptor.IsPartyOrPet();
        public static float GetMaxSpeed(List<UnitEntityData> data) => data.Select(u => u.ModifiedSpeedMps).Max();

        // Teleport and Travel
        public static void EnterToArea(BlueprintAreaEnterPoint enterPoint) => GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);

        public static bool CanRespec(this UnitEntityData ch) {
            return RespecHelper.GetRespecableUnits().Contains(ch);
        }
        public static void DoRespec(this UnitEntityData ch) {
            RespecHelper.Respec(ch);
        }
    }
}
