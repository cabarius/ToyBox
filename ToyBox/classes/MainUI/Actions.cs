// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ToyBox
{
    public static class Actions
    {
        public static void UnlockAllMythicPaths()
        {
            var mythicInfos = BlueprintRoot.Instance.MythicsSettings.m_MythicsInfos;

            foreach (var infoRef in mythicInfos)
            {
                var info = infoRef.Get();
                var etudeGUID = info.EtudeGuid;
                var etudeBp = ResourcesLibrary.TryGetBlueprint<BlueprintEtude>(etudeGUID);
                Main.Log($"mythicInfo: {info} {etudeGUID} {etudeBp}");
                Game.Instance.Player.EtudesSystem.StartEtude(etudeBp);
            }
        }

        public static void ToggleModWindow()
        {
            UnityModManager.UI.Instance.ToggleWindow();
        }

        public static void RunPerceptionTriggers()
        {
            if (!Game.Instance.Player.Party.Any()) { return; }

            foreach (BlueprintComponent bc in Game.Instance.State.LoadedAreaState.Blueprint.CollectComponents())
            {
                if (bc.name.Contains("PerceptionTrigger"))
                {
                    PerceptionTrigger pt = (PerceptionTrigger)bc;
                    pt.OnSpotted.Run();
                }
            }
        }

        public static void TeleportPartyToPlayer()
        {
            GameModeType currentMode = Game.Instance.CurrentMode;
            var partyMembers = Game.Instance.Player.m_PartyAndPets;

            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause)
            {
                foreach (var unit in partyMembers)
                {
                    if (unit != Game.Instance.Player.MainCharacter.Value)
                    {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                    }
                }
            }
        }

        public static void TeleportEveryoneToPlayer()
        {
            GameModeType currentMode = Game.Instance.CurrentMode;

            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause)
            {
                foreach (var unit in Game.Instance.State.Units)
                {
                    if (unit != Game.Instance.Player.MainCharacter.Value)
                    {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;
                    }
                }
            }
        }

        public static void TeleportToGlobalMap(Action callback = null)
        {
            var globalMap = Game.Instance.BlueprintRoot.GlobalMap;
            var areaEnterPoint = globalMap.All.FindOrDefault(i => i.Get().GlobalMapEnterPoint != null)?.Get().GlobalMapEnterPoint;

            if (areaEnterPoint != null)
            {
                Game.Instance.LoadArea(areaEnterPoint.Area, areaEnterPoint, AutoSaveMode.None, callback: callback ?? (() => { }));
            }
        }

        public static bool TeleportToGlobalMapPoint(BlueprintGlobalMapPoint destination)
        {
            if (GlobalMapView.Instance != null)
            {
                var globalMapController = Game.Instance.GlobalMapController;
                GlobalMapUI globalMapUI = Game.Instance.UI.GlobalMapUI;
                GlobalMapView globalMapView = GlobalMapView.Instance;
                GlobalMapState globalMapState = Game.Instance.Player.GetGlobalMap(destination.GlobalMap);

                GlobalMapPointState pointState = Game.Instance.Player.GetGlobalMap(destination.GlobalMap).GetPointState(destination);
                pointState.EdgesOpened = true;
                pointState.Reveal();
                GlobalMapPointView pointView = globalMapView.GetPointView(destination);

                if ((bool)(Object)globalMapView)
                {
                    if ((bool)(Object)pointView)
                    {
                        globalMapView.RevealLocation(pointView);
                    }
                }

                foreach (var edge in pointState.Edges)
                {
                    edge.UpdateExplored(1f, 1);
                    globalMapView.GetEdgeView(edge.Blueprint).UpdateRenderers();
                }

                globalMapController.StartTravels();

                EventBus.RaiseEvent((Action<IGlobalMapPlayerTravelHandler>)(h => h.HandleGlobalMapPlayerTravelStarted(globalMapView.State.Player, false)));

                globalMapView.State.Player.SetCurrentPosition(new GlobalMapPosition(destination));
                globalMapView.GetPointView(destination)?.OpenOutgoingEdges(null);
                globalMapView.UpdatePawnPosition();
                globalMapController.Stop();

                EventBus.RaiseEvent((Action<IGlobalMapPlayerTravelHandler>)(h => h.HandleGlobalMapPlayerTravelStopped(globalMapView.State.Player)));

                globalMapView.PlayerPawn.m_Compass.TryClear();
                globalMapView.PlayerPawn.m_Compass.TrySet();

                return true;
            }

            return false;
        }

        public static void RemoveAllBuffs()
        {
            foreach (UnitEntityData target in Game.Instance.Player.Party)
            {
                foreach (Buff buff in new List<Buff>(target.Descriptor.Buffs.Enumerable))
                {
                    target.Descriptor.RemoveFact(buff);
                }
            }
        }

        public static void SpawnUnit(BlueprintUnit unit, int count)
        {
            Vector3 worldPosition = Game.Instance.ClickEventsController.WorldPosition;

            if (unit != null)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 offset = 5f * Random.insideUnitSphere;

                    Vector3 spawnPosition = new Vector3(
                        worldPosition.x + offset.x,
                        worldPosition.y,
                        worldPosition.z + offset.z);

                    Game.Instance.EntityCreator.SpawnUnit(unit, spawnPosition, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
                }
            }
        }

        public static void ChangeParty()
        {
            GameModeType currentMode = Game.Instance.CurrentMode;

            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause)
            {
                UnityModManager.UI.Instance.ToggleWindow();
                GlobalMapView.Instance.ChangePartyOnMap();
            }
        }

        public static bool HasAbility(this UnitEntityData ch, BlueprintAbility ability)
        {
            return ability.IsSpell && ch.Spellbooks.Any(spellbook => spellbook.IsKnown(ability)) || ch.Descriptor.Abilities.HasFact(ability);
        }

        public static bool CanAddAbility(this UnitEntityData ch, BlueprintAbility ability)
        {
            if (ability.IsSpell)
            {
                foreach (var spellbook in ch.Spellbooks)
                {
                    if (spellbook.IsKnown(ability))
                    {
                        return false;
                    }

                    var spellbookBP = spellbook.Blueprint;
                    int maxLevel = spellbookBP.MaxSpellLevel;

                    for (int level = 0; level <= maxLevel; level++)
                    {
                        var learnable = spellbookBP.SpellList.GetSpells(level);

                        if (learnable.Contains(ability))
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                if (!ch.Descriptor.Abilities.HasFact(ability))
                {
                    return true;
                }
            }

            return false;
        }

        public static void AddAbility(this UnitEntityData ch, BlueprintAbility ability)
        {
            if (ability.IsSpell)
            {
                Main.Log($"adding spell: {ability.Name}");

                foreach (var spellbook in ch.Spellbooks)
                {
                    var spellbookBP = spellbook.Blueprint;
                    int maxLevel = spellbookBP.MaxSpellLevel;
                    Main.Log($"checking {spellbook.Blueprint.Name} maxLevel: {maxLevel}");

                    for (int level = 0; level <= maxLevel; level++)
                    {
                        var learnable = spellbookBP.SpellList.GetSpells(level);
                        bool allowsSpell = learnable.Contains(ability);
                        string allowText = allowsSpell ? "FOUND" : "did not find";
                        Main.Log($"{allowText} spell {ability.Name} in {learnable.Count()} level {level} spells");

                        if (allowsSpell)
                        {
                            Main.Log($"spell level = {level}");
                            spellbook.AddKnown(level, ability);
                        }
                    }
                }
            }
            else
            {
                ch.Descriptor.AddFact(ability);
            }
        }

        public static bool CanAddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability)
        {
            return ability.IsSpell && !ch.Descriptor.HasFact(ability);
        }

        public static void AddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability)
        {
            ch.Descriptor.AddFact(ability);
        }

        public static void RemoveAbility(this UnitEntityData ch, BlueprintAbility ability)
        {
            if (ability.IsSpell)
            {
                foreach (Spellbook spellbook in ch.Spellbooks.Where(spellbook => UIUtilityUnit.SpellbookHasSpell(spellbook, ability)))
                {
                    spellbook.RemoveSpell(ability);
                }
            }

            var abilities = ch.Descriptor.Abilities;

            if (abilities.HasFact(ability))
            {
                abilities.RemoveFact(ability);
            }
        }

        public static void ResetMythicPath(this UnitEntityData ch)
        {
        }

        public static void resetClassLevel(this UnitEntityData ch)
        {
            // TODO - this doesn't seem to work in BoT either...
            int level = 21;
            int xp = ch.Descriptor.Progression.Experience;
            BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;

            for (int i = 20; i >= 1; i--)
            {
                int xpBonus = xpTable.GetBonus(i);

                Main.Log(i + ": " + xpBonus + " | " + xp);

                if ((xp - xpBonus) >= 0)
                {
                    Main.Log(i + ": " + (xp - xpBonus));
                    level = i;

                    break;
                }
            }

            ch.Descriptor.Progression.CharacterLevel = level;
        }
    }
}