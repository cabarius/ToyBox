// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Armies.Blueprints;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox {
    public static class EditorActions {

        public static void InitializeActions() {
#if false
            var flags = Game.Instance.Player.UnlockableFlags;
            BlueprintAction.Register<BlueprintItem>("Add",
                                                    (bp, ch, n, index) => Game.Instance.Player.Inventory.Add(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintItem>("Remove",
                                                    (bp, ch, n, index) => Game.Instance.Player.Inventory.Remove(bp, n),
                                                    (bp, ch, index) => Game.Instance.Player.Inventory.Contains(bp), true);

            BlueprintAction.Register<BlueprintUnit>("Spawn",
                                                    (bp, ch, n, index) => Actions.SpawnUnit(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintFeature>("Add",
                                                       (bp, ch, n, index) => ch.Descriptor.AddFact(bp),
                                                       (bp, ch, index) => !ch.Progression.Features.HasFact(bp));

            BlueprintAction.Register<BlueprintFeature>("Remove",
                                                       (bp, ch, n, index) => ch.Progression.Features.RemoveFact(bp),
                                                       (bp, ch, index) => ch.Progression.Features.HasFact(bp));

            BlueprintAction.Register<BlueprintParametrizedFeature>("Add",
                (bp, ch, n, index) => ch?.Descriptor?.AddFact<UnitFact>(bp, null, bp.Items.OrderBy(x => x.Name).ElementAt(BlueprintListUI.ParamSelected[index]).Param),
                (bp, ch, index) => ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == bp.Items.OrderBy(x => x.Name).ElementAt(BlueprintListUI.ParamSelected[index]).Param) == null);

            BlueprintAction.Register<BlueprintParametrizedFeature>("Remove", (bp, ch, n, index) => ch?.Progression?.Features?.RemoveFact(ch.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == bp.Items.OrderBy(x => x.Name).ToArray()[BlueprintListUI.ParamSelected[index]].Param)),
                                                       (bp, ch, index) => ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == bp.Items.OrderBy(x => x.Name).ToArray()[BlueprintListUI.ParamSelected[index]].Param) != null);

            BlueprintAction.Register<BlueprintFeature>("<",
                                                       (bp, ch, n, index) => ch.Progression.Features.GetFact(bp)?.RemoveRank(),
                                                       (bp, ch, index) => {
                                                           var feature = ch.Progression.Features.GetFact(bp);
                                                           return feature?.GetRank() > 1;
                                                       });

            BlueprintAction.Register<BlueprintFeature>(">",
                                                       (bp, ch, n, index) => ch.Progression.Features.GetFact(bp)?.AddRank(),
                                                       (bp, ch, index) => {
                                                           var feature = ch.Progression.Features.GetFact(bp);
                                                           return feature != null && feature.GetRank() < feature.Blueprint.Ranks;
                                                       });
            //BlueprintAction.Register<BlueprintArchetype>(
            //    "Add",
            //    (bp, ch, n, index) => ch.Progression.AddArchetype(ch.Progression.Classes.First().CharacterClass, bp),
            //    (bp, ch, index) => ch.Progression.CanAddArchetype(ch.Progression.Classes.First().CharacterClass, bp)
            //    );
            //BlueprintAction.Register<BlueprintArchetype>("Remove",
            //    (bp, ch, n, index) => ch.Progression.AddArchetype(ch.Progression.Classes.First().CharacterClass, bp),
            //    (bp, ch, index) => ch.Progression.Classes.First().Archetypes.Contains(bp)
            //    );

            // Spellbooks
            BlueprintAction.Register<BlueprintSpellbook>("Add",
                                                         (bp, ch, n, index) => ch.Descriptor.DemandSpellbook(bp.CharacterClass),
                                                         (bp, ch, index) => ch.Descriptor.Spellbooks.All(sb => sb.Blueprint != bp));

            BlueprintAction.Register<BlueprintSpellbook>("Remove",
                                                         (bp, ch, n, index) => ch.Descriptor.DeleteSpellbook(bp),
                                                         (bp, ch, index) => ch.Descriptor.Spellbooks.Any(sb => sb.Blueprint == bp));

            BlueprintAction.Register<BlueprintSpellbook>(">",
                                                         (bp, ch, n, index) => {
                                                             try {
                                                                 var spellbook = ch.Descriptor.Spellbooks.First(sb => sb.Blueprint == bp);

                                                                 if (spellbook.IsMythic) {
                                                                     spellbook.AddMythicLevel();
                                                                 }
                                                                 else {
                                                                     spellbook.AddBaseLevel();
                                                                 }
                                                             }
                                                             catch (Exception e) { Mod.Error(e); }
                                                         },
                                                         (bp, ch, index) => ch.Descriptor.Spellbooks.Any(sb => sb.Blueprint == bp && sb.CasterLevel < bp.MaxSpellLevel));

            // Buffs
            BlueprintAction.Register<BlueprintBuff>("Add",
                                                    (bp, ch, n, index) => GameHelper.ApplyBuff(ch, bp),
                                                    (bp, ch, index) => !ch.Descriptor.Buffs.HasFact(bp));

            BlueprintAction.Register<BlueprintBuff>("Remove",
                                                    (bp, ch, n, index) => ch.Descriptor.RemoveFact(bp),
                                                    (bp, ch, index) => ch.Descriptor.Buffs.HasFact(bp));

            BlueprintAction.Register<BlueprintBuff>("<",
                                                    (bp, ch, n, index) => ch.Descriptor.Buffs.GetFact(bp)?.RemoveRank(),
                                                    (bp, ch, index) => {
                                                        var buff = ch.Descriptor.Buffs.GetFact(bp);

                                                        return buff?.GetRank() > 1;
                                                    });

            BlueprintAction.Register<BlueprintBuff>(">",
                                                    (bp, ch, n, index) => ch.Descriptor.Buffs.GetFact(bp)?.AddRank(),
                                                    (bp, ch, index) => {
                                                        var buff = ch.Descriptor.Buffs.GetFact(bp);

                                                        return buff != null && buff.GetRank() < buff.Blueprint.Ranks - 1;
                                                    });

            // Abilities
            BlueprintAction.Register<BlueprintAbility>("Add",
                                                       (bp, ch, n, index) => ch.AddAbility(bp),
                                                       (bp, ch, index) => ch.CanAddAbility(bp));

            BlueprintAction.Register<BlueprintAbility>("At Will",
                                                       (bp, ch, n, index) => ch.AddSpellAsAbility(bp),
                                                       (bp, ch, index) => ch.CanAddSpellAsAbility(bp));

            BlueprintAction.Register<BlueprintAbility>("Remove",
                                                       (bp, ch, n, index) => ch.RemoveAbility(bp),
                                                       (bp, ch, index) => ch.HasAbility(bp));
            // Ability Resources

            BlueprintAction.Register<BlueprintAbilityResource>("Add",
                (bp, ch, n, index) => ch.Resources.Add(bp, true),
                (bp, ch, index) => !ch.Resources.ContainsResource(bp));

            BlueprintAction.Register<BlueprintAbilityResource>("Remove",
                (bp, ch, n, index) => ch.Resources.Remove(bp),
                (bp, ch, index) => ch.Resources.ContainsResource(bp));

            // Spellbooks


            // BlueprintActivatableAbility
            BlueprintAction.Register<BlueprintActivatableAbility>("Add",
                                                                  (bp, ch, n, index) => ch.Descriptor.AddFact(bp),
                                                                  (bp, ch, index) => !ch.Descriptor.HasFact(bp));

            BlueprintAction.Register<BlueprintActivatableAbility>("Remove",
                                                                  (bp, ch, n, index) => ch.Descriptor.RemoveFact(bp),
                                                                  (bp, ch, index) => ch.Descriptor.HasFact(bp));

            // Quests
            BlueprintAction.Register<BlueprintQuest>("Start",
                                                     (bp, ch, n, index) => Game.Instance.Player.QuestBook.GiveObjective(bp.Objectives.First()),
                                                     (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp) == null);

            BlueprintAction.Register<BlueprintQuest>("Complete",
                                                     (bp, ch, n, index) => {
                                                         foreach (var objective in bp.Objectives) {
                                                             Game.Instance.Player.QuestBook.CompleteObjective(objective);
                                                         }
                                                     }, (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp)?.State == QuestState.Started);

            // Quests Objectives
            BlueprintAction.Register<BlueprintQuestObjective>("Start",
                                                              (bp, ch, n, index) => Game.Instance.Player.QuestBook.GiveObjective(bp),
                                                              (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest) == null);

            BlueprintAction.Register<BlueprintQuestObjective>("Complete",
                                                              (bp, ch, n, index) => Game.Instance.Player.QuestBook.CompleteObjective(bp),
                                                              (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest)?.State == QuestState.Started);

            // Etudes
            BlueprintAction.Register<BlueprintEtude>("Start",
                                                     (bp, ch, n, index) => Game.Instance.Player.EtudesSystem.StartEtude(bp),
                                                     (bp, ch, index) => Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));
            BlueprintAction.Register<BlueprintEtude>("Unstart",
                                                     (bp, ch, n, index) => Game.Instance.Player.EtudesSystem.UnstartEtude(bp),
                                                     (bp, ch, index) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));
            BlueprintAction.Register<BlueprintEtude>("Complete",
                                                     (bp, ch, n, index) => Game.Instance.Player.EtudesSystem.MarkEtudeCompleted(bp),
                                                     (bp, ch, index) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp) &&
                                                                 !Game.Instance.Player.EtudesSystem.EtudeIsCompleted(bp));
            // Flags
            BlueprintAction.Register<BlueprintUnlockableFlag>("Unlock",
                (bp, ch, n, index) => flags.Unlock(bp),
                (bp, ch, index) => !flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>("Lock",
                (bp, ch, n, index) => flags.Lock(bp),
                (bp, ch, index) => flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>(">",
                (bp, ch, n, index) => flags.SetFlagValue(bp, flags.GetFlagValue(bp) + n),
                (bp, ch, index) => flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>("<",
                (bp, ch, n, index) => flags.SetFlagValue(bp, flags.GetFlagValue(bp) - n),
                (bp, ch, index) => flags.IsUnlocked(bp));

            // Cutscenes
            BlueprintAction.Register<Cutscene>("Play", (bp, ch, n, index) => {
                Actions.ToggleModWindow();
                var cutscenePlayerData = CutscenePlayerData.Queue.FirstOrDefault(c => c.PlayActionId == bp.name);

                if (cutscenePlayerData != null) {
                    cutscenePlayerData.PreventDestruction = true;
                    cutscenePlayerData.Stop();
                    cutscenePlayerData.PreventDestruction = false;
                }

                var state = ContextData<SpawnedUnitData>.Current?.State;
                CutscenePlayerView.Play(bp, null, true, state).PlayerData.PlayActionId = bp.name;
            });

            // Teleport
            BlueprintAction.Register<BlueprintAreaEnterPoint>("Teleport", (bp, ch, n, index) => GameHelper.EnterToArea(bp, AutoSaveMode.None));
            BlueprintAction.Register<BlueprintGlobalMap>("Teleport", (bp, ch, n, index) => GameHelper.EnterToArea(bp.GlobalMapEnterPoint, AutoSaveMode.None));

            BlueprintAction.Register<BlueprintArea>("Teleport", (area, ch, n, index) => {
                var areaEnterPoints = BlueprintExensions.BlueprintsOfType<BlueprintAreaEnterPoint>();
                var blueprint = areaEnterPoints.FirstOrDefault(bp => bp is BlueprintAreaEnterPoint ep && ep.Area == area);

                if (blueprint is BlueprintAreaEnterPoint enterPoint) {
                    GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
                }
            });

            BlueprintAction.Register<BlueprintGlobalMapPoint>("Teleport", (globalMapPoint, ch, n, index) => {
                if (!Teleport.TeleportToGlobalMapPoint(globalMapPoint)) {
                    Teleport.TeleportToGlobalMap(() => Teleport.TeleportToGlobalMapPoint(globalMapPoint));
                }
            });

            //Army
            BlueprintAction.Register<BlueprintArmyPreset>("Add", (bp, ch, n, l) => {
                Actions.CreateArmy(bp);
            });

            //ArmyGeneral
            BlueprintAction.Register<BlueprintLeaderSkill>("Add",
                (bp, ch, n, l) => Actions.AddSkillToLeader(bp),
                (bp, ch, index) => Actions.LeaderSelected(bp) && !Actions.LeaderHasSkill(bp));

            //ArmyGeneral
            BlueprintAction.Register<BlueprintLeaderSkill>("Remove",
                (bp, ch, n, l) => Actions.RemoveSkillFromLeader(bp),
                (bp, ch, index) => Actions.LeaderSelected(bp) && Actions.LeaderHasSkill(bp));
#endif
        }
    }
}