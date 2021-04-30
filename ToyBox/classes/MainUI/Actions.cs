// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Armies.TacticalCombat.Blueprints;
using Kingmaker.Armies.TacticalCombat.Brain;
using Kingmaker.Armies.TacticalCombat.Brain.Considerations;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Credits;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Blueprints.Console;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.View;
using Kingmaker.Interaction;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Tutorial;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UI.IngameMenu;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;
using UnityModManagerNet;
using Kingmaker.Globalmap.State;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Globalmap;

namespace ToyBox {
    public static class Actions {
        public static void UnlockAllMythicPaths() {
            var mythicInfos = BlueprintRoot.Instance.MythicsSettings.m_MythicsInfos;
            foreach (var infoRef in mythicInfos) {
                var info = infoRef.Get();
                var etudeGUID = info.EtudeGuid;
                var etudeBp = ResourcesLibrary.TryGetBlueprint<BlueprintEtude>(etudeGUID);
                Game.Instance.Player.EtudesSystem.StartEtude(etudeBp);
            }
#if false
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("d85f7367b453b7b468b77e5e708297ae"));
                Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("e6669aad304206c4d969f6602e6b412e"));
                Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("8f2f0ea65ef3a3f48948d27a39b37db1"));
                Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("f6dce66b61f98eb4dbe6388e16b1de11"));
#endif
        }
        public static void ToggleModWindow() {
            UnityModManager.UI.Instance.ToggleWindow();
        }
        public static void RunPerceptionTriggers() {
            if (!Game.Instance.Player.Party.Any()) { return; }
            foreach (BlueprintComponent bc in Game.Instance.State.LoadedAreaState.Blueprint.CollectComponents()) {
                if (bc.name.Contains("PerceptionTrigger")) {
                    PerceptionTrigger pt = (PerceptionTrigger)bc;
                    pt.OnSpotted.Run();
                }
            }
        }
        public static void TeleportPartyToPlayer() {
            GameModeType currentMode = Game.Instance.CurrentMode;
            var partyMembers = Game.Instance.Player.m_PartyAndPets;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                foreach (var unit in partyMembers) {
                    if (unit != Game.Instance.Player.MainCharacter.Value) {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                    }
                }
            }
        }
        public static void TeleportEveryoneToPlayer() {
            GameModeType currentMode = Game.Instance.CurrentMode;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                foreach (var unit in Game.Instance.State.Units) {
                    if (unit != Game.Instance.Player.MainCharacter.Value) {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                    }
                }
            }
        }
        public static void TeleportToGlobalMap(Action callback = null) {
            var globalMap = Game.Instance.BlueprintRoot.GlobalMap;
            var areaEnterPoint = globalMap.All.FindOrDefault(i => i.Get().GlobalMapEnterPoint != null)?.Get().GlobalMapEnterPoint;
            Game.Instance.LoadArea(areaEnterPoint.Area, areaEnterPoint, AutoSaveMode.None, callback: callback != null ? callback : () => { });
        }
        public static bool TeleportToGlobalMapPoint(BlueprintGlobalMapPoint destination) {
            if (GlobalMapView.Instance != null) {
                var globalMapController = Game.Instance.GlobalMapController;
                GlobalMapUI globalMapUI = Game.Instance.UI.GlobalMapUI;
                GlobalMapView globalMapView = GlobalMapView.Instance;
                GlobalMapState globalMapState = Game.Instance.Player.GetGlobalMap(destination.GlobalMap);

                GlobalMapPointState pointState = Game.Instance.Player.GetGlobalMap(destination.GlobalMap).GetPointState(destination);
                pointState.EdgesOpened = true;
                pointState.Reveal();
                GlobalMapPointView pointView = globalMapView.GetPointView(destination);
                if ((bool)(UnityEngine.Object)globalMapView) {
                    if ((bool)(UnityEngine.Object)pointView)
                        globalMapView.RevealLocation(pointView);
                }
                foreach (var edge in pointState.Edges) {
                    edge.UpdateExplored(1f, 1);
                    globalMapView.GetEdgeView(edge.Blueprint).UpdateRenderers();

                }
                globalMapController.StartTravels();
                EventBus.RaiseEvent<IGlobalMapPlayerTravelHandler>((Action<IGlobalMapPlayerTravelHandler>)(h => h.HandleGlobalMapPlayerTravelStarted((IGlobalMapTraveler)globalMapView.State.Player)));
                globalMapView.State.Player.SetCurrentPosition(new GlobalMapPosition(destination));
                globalMapView.GetPointView(destination)?.OpenOutgoingEdges((GlobalMapPointView)null);
                globalMapView.UpdatePawnPosition();
                globalMapController.Stop();
                EventBus.RaiseEvent<IGlobalMapPlayerTravelHandler>((Action<IGlobalMapPlayerTravelHandler>)(h => h.HandleGlobalMapPlayerTravelStopped((IGlobalMapTraveler)globalMapView.State.Player)));
                globalMapView.PlayerPawn.m_Compass.TryClear();
                globalMapView.PlayerPawn.m_Compass.TrySet();
#if false
                globalMapView.TeleportParty(globalMapPoint);
                globalMapUI.HandleGlobalMapPlayerTravelStopped(globalMapState.Player);
                     GlobalMapPointState pointState = Game.Instance.Player.GlobalMap.GetPointState(globalMapPoint);
                pointState.EdgesOpened = true;
                pointState.Reveal();
                GlobalMapPointView pointView = globalMapView.GetPointView(globalMapPoint);
                pointView?.OpenOutgoingEdges((GlobalMapPointView)null);
                globalMapView.RevealLocation(pointView);
                if ((bool)(UnityEngine.Object)globalMapView) {
                    if ((bool)(UnityEngine.Object)pointView)
                        globalMapView.RevealLocation(pointView);
                }
                globalMapView.UpdatePawnPosition();
                pointState.LastVisited = Game.Instance.TimeController.GameTime;
#endif
                return true;
            }
            return false;
        }
        public static void RemoveAllBuffs() {
            foreach (UnitEntityData target in Game.Instance.Player.Party) {
                foreach (Buff buff in new List<Buff>(target.Descriptor.Buffs.Enumerable)) {
                    target.Descriptor.RemoveFact(buff);
                }
            }
        }
        public static void SpawnUnit(BlueprintUnit unit, int count) {
            Vector3 worldPosition = Game.Instance.ClickEventsController.WorldPosition;
            //           var worldPosition = Game.Instance.Player.MainCharacter.Value.Position;
            if (!(unit == null)) {
                for (int i = 0; i < count; i++) {
                    Vector3 offset = 5f*UnityEngine.Random.insideUnitSphere;
                    Vector3 spawnPosition = new Vector3(
                        worldPosition.x + offset.x,
                        worldPosition.y,
                        worldPosition.z + offset.z);
                    Game.Instance.EntityCreator.SpawnUnit(unit, spawnPosition, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
                }
            }
        }
        public static void ChangeParty() {
            GameModeType currentMode = Game.Instance.CurrentMode;

            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                UnityModManager.UI.Instance.ToggleWindow();
                GlobalMapView.Instance.ChangePartyOnMap();
            }
        }
        public static bool HasAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                foreach (var spellbook in ch.Spellbooks) {
                    if (spellbook.IsKnown(ability)) return true;
                }
            }
            if (ch.Descriptor.Abilities.HasFact(ability)) return true;
            return false;
        }
        public static bool CanAddAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                foreach (var spellbook in ch.Spellbooks) {
                    if (spellbook.IsKnown(ability)) return false;
                    var spellbookBP = spellbook.Blueprint;
                    var maxLevel = spellbookBP.MaxSpellLevel;
                    for (int level = 0; level <= maxLevel; level++) {
                        var learnable = spellbookBP.SpellList.GetSpells(level);
                        if (learnable.Contains(ability)) {
//                            Logger.Log($"found spell {ability.Name} in {learnable.Count()} level {level} spells");
                            return true; ;
                        }
                    }
                }
            }
            else {
                if (!ch.Descriptor.Abilities.HasFact(ability)) return true;
            }
            return false;
        }
        public static void AddAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                Logger.Log($"adding spell: {ability.Name}");
                foreach (var spellbook in ch.Spellbooks) {
                    var spellbookBP = spellbook.Blueprint;
                    var maxLevel = spellbookBP.MaxSpellLevel;
                    Logger.Log($"checking {spellbook.Blueprint.Name} maxLevel: {maxLevel}");
                    for (int level = 0; level <= maxLevel; level++) {
                        var learnable = spellbookBP.SpellList.GetSpells(level);
                        var allowsSpell = learnable.Contains(ability);
                        var allowText = allowsSpell ? "FOUND" : "did not find";
                        Logger.Log($"{allowText} spell {ability.Name} in {learnable.Count()} level {level} spells");
                        if (allowsSpell) {
                            Logger.Log($"spell level = {level}");
                            spellbook.AddKnown(level, ability);
                        }

                    }
                }
            }
            else {
                ch.Descriptor.AddFact(ability);
            }
        }
        static public bool CanAddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability) {
            return ability.IsSpell && !ch.Descriptor.HasFact(ability);
        }
        public static void AddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability) {
            ch.Descriptor.AddFact(ability);
        }
        public static void RemoveAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                foreach (var spellbook in ch.Spellbooks) {
                    if (UIUtilityUnit.SpellbookHasSpell(spellbook, ability)) {
                        spellbook.RemoveSpell(ability);
                    }
                }
            }
            var abilities = ch.Descriptor.Abilities;
            if (abilities.HasFact(ability)) abilities.RemoveFact(ability);
        }
        public static void ResetMythicPath(this UnitEntityData ch) {
//            ch.Descriptor.Progression.RemoveMythicLevel
        }
        public static void resetClassLevel(this UnitEntityData ch) {
            // TODO - this doesn't seem to work in BoT either...
            int level = 21;
            int xp = ch.Descriptor.Progression.Experience;
            BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;

            for (int i = 20; i >= 1; i--) {
                int xpBonus = xpTable.GetBonus(i);

                Logger.Log(i + ": " + xpBonus + " | " + xp);

                if ((xp - xpBonus) >= 0) {
                    Logger.Log(i + ": " + (xp - xpBonus));
                    level = i;
                    break;
                }
            }
            ch.Descriptor.Progression.CharacterLevel = level;
        }
    }
}