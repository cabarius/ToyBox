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
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.State;
using Kingmaker.Globalmap.View;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using UnityModManagerNet;
using ToyBox.BagOfPatches;
using ModKit;
using Owlcat.Runtime.Core.Utils;
using ToyBox;

namespace ToyBox {
    public static class Actions {
        public static Settings settings => Main.settings;

        public static void UnlockAllBasicMythicPaths() {
            // TODO - do this right once I build the etude browser and understand this better
            UnlockAeon();
            UnlockAzata();
            UnlockLich();
            UnlockTrickster();
            // The following two block progression so better not to
            //UnlockDevil();
            //UnockSwarm();
            UnlockGoldDragon();
#if false
            var mythicInfos = BlueprintRoot.Instance.MythicsSettings.m_MythicsInfos;
            foreach (var infoRef in mythicInfos) {
                var info = infoRef.Get();
                var etudeGUID = info.EtudeGuid;
                var etudeBp = ResourcesLibrary.TryGetBlueprint<BlueprintEtude>(etudeGUID);
                Main.Log($"mythicInfo: {info} {etudeGUID} {etudeBp}");
                Game.Instance.Player.EtudesSystem.StartEtude(etudeBp);
            }
#endif
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockAngel() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("d85f7367b453b7b468b77e5e708297ae"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockDemon() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("e6669aad304206c4d969f6602e6b412e"));
            Main.SetNeedsResetGameUI();
        }

        public static void UnlockAeon() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("d85f7367b453b7b468b77e5e708297ae"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockAzata() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("e6669aad304206c4d969f6602e6b412e"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockLich() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("8f2f0ea65ef3a3f48948d27a39b37db1"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockTrickster() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("f6dce66b61f98eb4dbe6388e16b1de11"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockLegend() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("2943a647eb4017c49b4c121b15841d07"));
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("230552776ff941e1b054596bf589f9a9"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockDevil() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("26028ff893925ef44aa1179906ac9265"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockSwarm() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("6248db4784b301945b67b52143386b55"));
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("544443943d917e14ca72583d0357d4ad"));
            Main.SetNeedsResetGameUI();
        }
        public static void UnlockGoldDragon() {
            Game.Instance.Player.EtudesSystem.StartEtude(ResourcesLibrary.TryGetBlueprint<BlueprintEtude>("067212d277e846a4f9ff96aee6138f0b"));
            Main.SetNeedsResetGameUI();
        }

        public static void ToggleModWindow() => UnityModManager.UI.Instance.ToggleWindow();
        public static void RunPerceptionTriggers() {
            foreach (var obj in Game.Instance.State.MapObjects) {
                obj.LastPerceptionRollRank = new Dictionary<UnitReference, int>();
            }

            Tweaks.UnitEntityData_CanRollPerception_Extension.TriggerReroll = true;
        }
        public static void RemoveAllBuffs() {
            foreach (var target in Game.Instance.Player.Party) {
                foreach (var buff in new List<Buff>(target.Descriptor.Buffs.Enumerable)) {
                    target.Descriptor.RemoveFact(buff);
                }
            }
        }
        public static void SpawnUnit(BlueprintUnit unit, int count) {
            var worldPosition = Game.Instance.ClickEventsController.WorldPosition;
            //           var worldPosition = Game.Instance.Player.MainCharacter.Value.Position;
            if (!(unit == null)) {
                for (var i = 0; i < count; i++) {
                    var offset = 5f * UnityEngine.Random.insideUnitSphere;
                    Vector3 spawnPosition = new(
                        worldPosition.x + offset.x,
                        worldPosition.y,
                        worldPosition.z + offset.z);
                    Game.Instance.EntityCreator.SpawnUnit(unit, spawnPosition, Quaternion.identity, Game.Instance.State.LoadedAreaState.MainState);
                }
            }
        }
        public static void HandleChangeParty() {
            if (Game.Instance.CurrentMode == GameModeType.GlobalMap) {
                var partyCharacters = Game.Instance.Player.Party.Select(u => (UnitReference)u).ToList(); ;
                if ((partyCharacters != null ? (partyCharacters.Select(r => r.Value).SequenceEqual(Game.Instance.Player.Party) ? 1 : 0) : 1) != 0)
                    return;
                GlobalMapView.Instance.ChangePartyOnMap();
            }
            else {
                foreach (var temp in Game.Instance.Player.RemoteCompanions.ToTempList())
                    temp.IsInGame = false;
                Game.Instance.Player.FixPartyAfterChange();
                Game.Instance.UI.SelectionManager.UpdateSelectedUnits();
                var tempList = Game.Instance.Player.Party.Select(character => character.View).ToTempList<UnitEntityView>();
                if (Game.Instance.UI.SelectionManager is SelectionManagerPC selectionManager) 
                    selectionManager.MultiSelect((IEnumerable<UnitEntityView>)tempList);
            }
        }
        public static void ChangeParty() {
            var currentMode = Game.Instance.CurrentMode;

            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause || currentMode == GameModeType.GlobalMap) {
                if (Main.IsModGUIShown) UnityModManager.UI.Instance.ToggleWindow();
                EventBus.RaiseEvent<IGroupChangerHandler>(h => h.HandleCall(new Action(HandleChangeParty), (Action)null, true));
            }
        }
        public static void IdentifyAll() {
            var inventory = Game.Instance?.Player?.Inventory;
            if (inventory == null) return;
            foreach (var item in inventory) {
                item.Identify();
            }
            foreach (var ch in Game.Instance.Player.AllCharacters) {
                foreach (var item in ch.Body.GetAllItemsInternal()) {
                    item.Identify();
                    //Main.Log($"{ch.CharacterName} - {item.Name} - {item.IsIdentified}");
                }
            }
        }
        public static void ClearActionBar() {
            var selectedChar = Game.Instance?.SelectionCharacter?.CurrentSelectedCharacter;
            var uiSettings = selectedChar?.UISettings;
            uiSettings?.CleanupSlots();
            uiSettings.Dirty = true;
        }
        public static bool HasAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                if (PartyEditor.IsOnPartyEditor() && PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out var selectedSpellbook)) {
                    return UIUtilityUnit.SpellbookHasSpell(selectedSpellbook, ability);
                }
            }
            return ch.Spellbooks.Any(spellbook => spellbook.IsKnown(ability)) || ch.Descriptor.Abilities.HasFact(ability);
        }
        public static bool CanAddAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                if (PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out var selectedSpellbook)) {
                    return !selectedSpellbook.IsKnown(ability) &&
                           (ability.IsInSpellList(selectedSpellbook.Blueprint.SpellList) || Main.settings.showFromAllSpellbooks);
                }

                foreach (var spellbook in ch.Spellbooks) {
                    if (spellbook.IsKnown(ability)) return false;
                    var spellbookBP = spellbook.Blueprint;
                    var maxLevel = spellbookBP.MaxSpellLevel;
                    for (var level = 0; level <= maxLevel; level++) {
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
                    if (PartyEditor.IsOnPartyEditor() && PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out var selectedSpellbook)) {
                        selectedSpellbook.AddKnown(PartyEditor.selectedSpellbookLevel, ability);
                        return;
                    }
                }

                Mod.Trace($"adding spell: {ability.Name}");
                foreach (var spellbook in ch.Spellbooks) {
                    var spellbookBP = spellbook.Blueprint;
                    var maxLevel = spellbookBP.MaxSpellLevel;
                    Mod.Trace($"checking {spellbook.Blueprint.Name} maxLevel: {maxLevel}");
                    for (var level = 0; level <= maxLevel; level++) {
                        var learnable = spellbookBP.SpellList.GetSpells(level);
                        var allowsSpell = learnable.Contains(ability);
                        var allowText = allowsSpell ? "FOUND" : "did not find";
                        Mod.Trace($"{allowText} spell {ability.Name} in {learnable.Count()} level {level} spells");
                        if (allowsSpell) {
                            Mod.Trace($"spell level = {level}");
                            spellbook.AddKnown(level, ability);
                        }
                    }
                }
            }
            else {
                ch.Descriptor.AddFact(ability);
            }
        }
        public static bool CanAddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability) => ability.IsSpell && !ch.Descriptor.HasFact(ability) && !PartyEditor.IsOnPartyEditor();
        public static void AddSpellAsAbility(this UnitEntityData ch, BlueprintAbility ability) => ch.Descriptor.AddFact(ability);
        public static void RemoveAbility(this UnitEntityData ch, BlueprintAbility ability) {
            if (ability.IsSpell) {
                if (PartyEditor.IsOnPartyEditor() && PartyEditor.SelectedSpellbook.TryGetValue(ch.HashKey(), out var selectedSpellbook)) {
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
            var level = ch.Descriptor.Progression.MaxCharacterLevel;
            var xp = ch.Descriptor.Progression.Experience;
            var xpTable = ch.Descriptor.Progression.ExperienceTable;

            for (var i = ch.Descriptor.Progression.MaxCharacterLevel; i >= 1; i--) {
                var xpBonus = xpTable.GetBonus(i);

                Mod.Trace(i + ": " + xpBonus + " | " + xp);

                if ((xp - xpBonus) >= 0) {
                    Mod.Trace(i + ": " + (xp - xpBonus));
                    level = i;
                    break;
                }
            }
            ch.Descriptor.Progression.CharacterLevel = level;
        }

        public static void CreateArmy(BlueprintArmyPreset bp, bool friendlyorhostile) {
            var playerPosition = Game.Instance.Player.GlobalMap.CurrentPosition;
            if (friendlyorhostile) {
                Game.Instance.Player.GlobalMap.LastActivated.CreateArmy(ArmyFaction.Crusaders, bp, playerPosition);
            }
            else {
                Game.Instance.Player.GlobalMap.LastActivated.CreateArmy(ArmyFaction.Demons, bp, playerPosition);
            }

        }

        public static void AddSkillToLeader(BlueprintLeaderSkill bp) {
            var selectedArmy = Game.Instance.GlobalMapController.SelectedArmy;
            if (selectedArmy == null || selectedArmy.Data.Leader == null) {
                Mod.Trace($"Choose an army with a leader!");
                return;
            }
            var leader = selectedArmy.Data.Leader;
            leader.AddSkill(bp, true);
        }

        public static void RemoveSkillFromLeader(BlueprintLeaderSkill bp) {
            var selectedArmy = Game.Instance.GlobalMapController.SelectedArmy;
            if (selectedArmy == null || selectedArmy.Data.Leader == null) {
                Mod.Trace($"Choose an army with a leader!");
                return;
            }
            var leader = selectedArmy.Data.Leader;
            leader.RemoveSkill(bp);
        }

        public static bool LeaderHasSkill(BlueprintLeaderSkill bp) {
            var selectedArmy = Game.Instance.GlobalMapController.SelectedArmy;
            if (selectedArmy == null || selectedArmy.Data.Leader == null) {
                Mod.Trace($"Choose an army with a leader!");
                return false;
            }
            var leader = selectedArmy.Data.Leader;
            return leader.m_Skills.Contains(bp);
        }

        public static bool LeaderSelected(BlueprintLeaderSkill bp) {
            var selectedArmy = Game.Instance.GlobalMapController.SelectedArmy;
            if (selectedArmy == null || selectedArmy.Data.Leader == null) {
                return false;
            }
            return true;
        }
        public static void ApplyTimeScale() {
            var timeScale = settings.useAlternateTimeScaleMultiplier
                ? settings.alternateTimeScaleMultiplier
                : settings.timeScaleMultiplier;
            Game.Instance.TimeController.DebugTimeScale = timeScale;
        }
        public static void LobotomizeAllEnemies() {
            foreach (var unit in Game.Instance.State.Units) {
                if (unit.CombatState.IsInCombat &&
                    unit.IsPlayersEnemy &&
                    unit != Kingmaker.Designers.GameHelper.GetPlayerCharacter()) {
                    var descriptor = unit.Descriptor;
                    if (descriptor != null) {
                        // removing the brain works better in RTWP, but gets stuck in turn based
                        //AccessTools.DeclaredProperty(descriptor.GetType(), "Brain")?.SetValue(descriptor, null);

                        // add a bunch of conditions and hope for the best
                        descriptor.State.AddCondition(UnitCondition.DisableAttacksOfOpportunity);
                        descriptor.State.AddCondition(UnitCondition.CantAct);
                        descriptor.State.AddCondition(UnitCondition.CanNotAttack);
                        descriptor.State.AddCondition(UnitCondition.CantMove);
                        descriptor.State.AddCondition(UnitCondition.MovementBan);
                    }
                }
            }
        }
        // can potentially go back in time but some parts of the game don't expect it
        public static void KingdomTimelineAdvanceDays(int days) {
            var kingdom = KingdomState.Instance;
            var timelineManager = kingdom.TimelineManager;

            // from KingdomState.SkipTime
            foreach (var kingdomTask in kingdom.ActiveTasks) {
                if (!kingdomTask.IsFinished && !kingdomTask.IsStarted && !kingdomTask.NeedsCommit && kingdomTask.HasAssignedLeader) {
                    kingdomTask.Start(true);
                }
            }

            // from KingdomTimelineManager.Advance
            if (!KingdomTimelineManager.CanAdvanceTime()) {
                return;
            }
            Game.Instance.AdvanceGameTime(TimeSpan.FromDays(days));
            if (Game.Instance.IsModeActive(GameModeType.Kingdom)) {
                foreach (var unitEntityData in Game.Instance.Player.AllCharacters) {
                    RestController.ApplyRest(unitEntityData.Descriptor);
                }
            }

            timelineManager.UpdateTimeline();
        }

        // called when changing highlight settings so they take immediate effect
        public static void UpdateHighlights(bool on) {
            foreach (var mapObjectEntityData in Game.Instance.State.MapObjects) {
                mapObjectEntityData.View.UpdateHighlight();
            }
            foreach (var unitEntityData in Game.Instance.State.Units) {
                unitEntityData.View.UpdateHighlight(false);
            }
        }

        public static void RerollInteractionSkillChecks() {
            foreach (var obj in Game.Instance.State.MapObjects) {
                foreach (var part in obj.Parts.GetAll<InteractionSkillCheckPart>()) {
                    if (part.AlreadyUsed && !part.CheckPassed) {
                        part.AlreadyUsed = false;
                        part.Enabled = true;
                    }
                }
            }
        }
    }
}
