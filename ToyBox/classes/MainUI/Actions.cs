// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.View;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Utility;
using UnityModManagerNet;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.UI.ServiceWindow;

namespace ToyBox {
    public static class Actions {
        public static void UnlockAllMythicPaths() {
            var mythicInfos = BlueprintRoot.Instance.MythicsSettings.m_MythicsInfos;
            foreach (var infoRef in mythicInfos) {
                var info = infoRef.Get();
                var etudeGUID = info.EtudeGuid;
                var etudeBp = ResourcesLibrary.TryGetBlueprint<BlueprintEtude>(etudeGUID);
                Main.Log($"mythicInfo: {info} {etudeGUID} {etudeBp}");
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
                    Vector3 offset = 5f * UnityEngine.Random.insideUnitSphere;
                    Vector3 spawnPosition = new Vector3(
                        worldPosition.x + offset.x,
                        worldPosition.y,
                        worldPosition.z + offset.z);
                    Game.Instance.EntityCreator.SpawnUnit(unit, spawnPosition, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
                }
            }
        }

        public static void HandleChangeParty() {
            List<UnitReference> partyCharacters = Game.Instance.Player.Party.Select<UnitEntityData, UnitReference>((Func<UnitEntityData, UnitReference>)(u => (UnitReference)u)).ToList<UnitReference>(); ;
            if ((partyCharacters != null ? (partyCharacters.Select<UnitReference, UnitEntityData>((Func<UnitReference, UnitEntityData>)(r => r.Value)).SequenceEqual<UnitEntityData>((IEnumerable<UnitEntityData>)Game.Instance.Player.Party) ? 1 : 0) : 1) != 0)
                return;
            GlobalMapView.Instance.ChangePartyOnMap();
        }

        public static void ChangeParty() {
            GameModeType currentMode = Game.Instance.CurrentMode;

            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                UnityModManager.UI.Instance.ToggleWindow();
                EventBus.RaiseEvent<IGroupChangerHandler>((Action<IGroupChangerHandler>)(h => h.HandleCall(new Action(Actions.HandleChangeParty), (Action)null, true)));
            }
        }
        public static void IdentifyAll() {
            var inventory = Game.Instance?.Player?.Inventory;
            if (inventory == null) return;
            foreach (var item in inventory) {
                item.Identify();
            }
        }
        public static bool HasAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                if (PartyEditor.IsOnPartyEditor() && PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out Spellbook selectedSpellbook)) {
                    return UIUtilityUnit.SpellbookHasSpell(selectedSpellbook, ability);
                }
            }
            return ch.Spellbooks.Any(spellbook => spellbook.IsKnown(ability)) || ch.Descriptor.Abilities.HasFact(ability);
        }
        public static bool CanAddAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                if (PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out Spellbook selectedSpellbook)) {
                    return !selectedSpellbook.IsKnown(ability) &&
                           (ability.IsInSpellList(selectedSpellbook.Blueprint.SpellList) || Main.settings.showFromAllSpellbooks);
                }

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
                if (CanAddAbility(ch, ability)) {
                    if (PartyEditor.IsOnPartyEditor() && PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out Spellbook selectedSpellbook)) {
                            selectedSpellbook.AddKnown(PartyEditor.selectedSpellbookLevel, ability);
                            return;
                    }
                }

                Main.Log($"adding spell: {ability.Name}");
                foreach (var spellbook in ch.Spellbooks) {
                    var spellbookBP = spellbook.Blueprint;
                    var maxLevel = spellbookBP.MaxSpellLevel;
                    Main.Log($"checking {spellbook.Blueprint.Name} maxLevel: {maxLevel}");
                    for (int level = 0; level <= maxLevel; level++) {
                        var learnable = spellbookBP.SpellList.GetSpells(level);
                        var allowsSpell = learnable.Contains(ability);
                        var allowText = allowsSpell ? "FOUND" : "did not find";
                        Main.Log($"{allowText} spell {ability.Name} in {learnable.Count()} level {level} spells");
                        if (allowsSpell) {
                            Main.Log($"spell level = {level}");
                            spellbook.AddKnown(level, ability);
                        }

                    }
                }
            }
            else {
                ch.Descriptor.AddFact(ability);
            }
        }
        public static bool CanAddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability) {
            return ability.IsSpell && !ch.Descriptor.HasFact(ability) && !PartyEditor.IsOnPartyEditor();
        }
        public static void AddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability) {
            ch.Descriptor.AddFact(ability);
        }
        public static void RemoveAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                if (PartyEditor.IsOnPartyEditor() && PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out Spellbook selectedSpellbook)) {
                    if (UIUtilityUnit.SpellbookHasSpell(selectedSpellbook, ability)) {
                        selectedSpellbook.RemoveSpell(ability);
                        return;
                    }
                }
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
            int level = ch.Descriptor.Progression.MaxCharacterLevel;
            int xp = ch.Descriptor.Progression.Experience;
            BlueprintStatProgression xpTable = ch.Descriptor.Progression.ExperienceTable;

            for (int i = ch.Descriptor.Progression.MaxCharacterLevel; i >= 1; i--) {
                int xpBonus = xpTable.GetBonus(i);

                Main.Log(i + ": " + xpBonus + " | " + xp);

                if ((xp - xpBonus) >= 0) {
                    Main.Log(i + ": " + (xp - xpBonus));
                    level = i;
                    break;
                }
            }
            ch.Descriptor.Progression.CharacterLevel = level;
        }
        
        public static void CreateArmy(BlueprintArmyPreset bp) {
            var playerPosition = Game.Instance.Player.GlobalMap.CurrentPosition;
            Game.Instance.Player.GlobalMap.LastActivated.CreateArmy(ArmyFaction.Crusaders, bp, playerPosition);
        }
    }
}