// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox {
    public abstract class BlueprintAction {
        public delegate void Perform(SimpleBlueprint bp, UnitEntityData ch = null, int count = 1);

        public delegate bool CanPerform(SimpleBlueprint bp, UnitEntityData ch = null);

        private static Dictionary<Type, BlueprintAction[]> actionsForType;

        public static BlueprintAction[] ActionsForType(Type type) {
            if (actionsForType == null) {
                actionsForType = new Dictionary<Type, BlueprintAction[]>();
                BlueprintActions.InitializeActions();
            }

            actionsForType.TryGetValue(type, out BlueprintAction[] result);

            if (result == null) {
                var baseType = type.BaseType;

                if (baseType != null) {
                    result = ActionsForType(baseType);
                }

                result ??= new BlueprintAction[] { };

                actionsForType[type] = result;
            }

            return result;
        }

        public static IEnumerable<BlueprintAction> ActionsForBlueprint(SimpleBlueprint bp) {
            return ActionsForType(bp.GetType());
        }

        public static void Register<T>(string name, BlueprintAction<T>.Perform perform, BlueprintAction<T>.CanPerform canPerform = null, bool isRepeatable = false) where T : SimpleBlueprint {
            var action = new BlueprintAction<T>(name, perform, canPerform, isRepeatable);
            var type = action.BlueprintType;
            actionsForType.TryGetValue(type, out BlueprintAction[] existing);
            existing ??= new BlueprintAction[] { };
            var list = existing.ToList();
            list.Add(action);
            actionsForType[type] = list.ToArray();
        }

        public string name { get; protected set; }

        public Perform action;

        public CanPerform canPerform;

        protected BlueprintAction(string name, bool isRepeatable) {
            this.name = name;
            this.isRepeatable = isRepeatable;
        }

        public bool isRepeatable;

        public abstract Type BlueprintType { get; }
    }

    public class BlueprintAction<BPType> : BlueprintAction where BPType : SimpleBlueprint {
        public new delegate void Perform(BPType bp, UnitEntityData ch, int count = 1);

        public new delegate bool CanPerform(BPType bp, UnitEntityData ch);

        public BlueprintAction(string name, Perform action, CanPerform canPerform = null, bool isRepeatable = false) : base(name, isRepeatable) {
            this.action = (bp, ch, n) => action((BPType)bp, ch, n);
            this.canPerform = (bp, ch) => Main.IsInGame && bp is BPType bpt && (canPerform?.Invoke(bpt, ch) ?? true);
        }

        public override Type BlueprintType => typeof(BPType);
    }

    public static class BlueprintActions {
        public static IEnumerable<BlueprintAction> GetActions(this SimpleBlueprint bp) {
            return BlueprintAction.ActionsForBlueprint(bp);
        }

        public static void InitializeActions() {
            BlueprintAction.Register<BlueprintItem>("Add",
                                                    (bp, ch, n) => Game.Instance.Player.Inventory.Add(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintItem>("Remove",
                                                    (bp, ch, n) => Game.Instance.Player.Inventory.Remove(bp, n),
                                                    (bp, ch) => Game.Instance.Player.Inventory.Contains(bp), true);

            BlueprintAction.Register<BlueprintUnit>("Spawn",
                                                    (bp, ch, n) => Actions.SpawnUnit(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintFeature>("Add",
                                                       (bp, ch, n) => ch.Descriptor.AddFact(bp),
                                                       (bp, ch) => !ch.Progression.Features.HasFact(bp));

            BlueprintAction.Register<BlueprintFeature>("Remove",
                                                       (bp, ch, n) => ch.Progression.Features.RemoveFact(bp),
                                                       (bp, ch) => ch.Progression.Features.HasFact(bp));

            BlueprintAction.Register<BlueprintFeature>("<",
                                                       (bp, ch, n) => ch.Progression.Features.GetFact(bp)?.RemoveRank(),
                                                       (bp, ch) => {
                                                           var feature = ch.Progression.Features.GetFact(bp);

                                                           return feature?.GetRank() > 1;
                                                       });

            BlueprintAction.Register<BlueprintFeature>(">",
                                                       (bp, ch, n) => ch.Progression.Features.GetFact(bp)?.AddRank(),
                                                       (bp, ch) => {
                                                           var feature = ch.Progression.Features.GetFact(bp);

                                                           return feature != null && feature.GetRank() < feature.Blueprint.Ranks;
                                                       });

            // Spellbooks
            BlueprintAction.Register<BlueprintSpellbook>("Add",
                                                         (bp, ch, n) => ch.Descriptor.DemandSpellbook(bp.CharacterClass),
                                                         (bp, ch) => ch.Descriptor.Spellbooks.All(sb => sb.Blueprint != bp));

            BlueprintAction.Register<BlueprintSpellbook>("Remove",
                                                         (bp, ch, n) => ch.Descriptor.DeleteSpellbook(bp),
                                                         (bp, ch) => ch.Descriptor.Spellbooks.Any(sb => sb.Blueprint == bp));

            BlueprintAction.Register<BlueprintSpellbook>(">",
                                                         (bp, ch, n) => {
                                                             try {
                                                                 var spellbook = ch.Descriptor.Spellbooks.First(sb => sb.Blueprint == bp);

                                                                 if (spellbook.IsMythic) {
                                                                     spellbook.AddMythicLevel();
                                                                 }
                                                                 else {
                                                                     spellbook.AddBaseLevel();
                                                                 }
                                                             }
                                                             catch (Exception e) { Main.Error(e); }
                                                         },
                                                         (bp, ch) => ch.Descriptor.Spellbooks.Any(sb => sb.Blueprint == bp && sb.CasterLevel < bp.MaxSpellLevel));

            // Buffs
            BlueprintAction.Register<BlueprintBuff>("Add",
                                                    (bp, ch, n) => GameHelper.ApplyBuff(ch, bp),
                                                    (bp, ch) => !ch.Descriptor.Buffs.HasFact(bp));

            BlueprintAction.Register<BlueprintBuff>("Remove",
                                                    (bp, ch, n) => ch.Descriptor.RemoveFact(bp),
                                                    (bp, ch) => ch.Descriptor.Buffs.HasFact(bp));

            BlueprintAction.Register<BlueprintBuff>("<",
                                                    (bp, ch, n) => ch.Descriptor.Buffs.GetFact(bp)?.RemoveRank(),
                                                    (bp, ch) => {
                                                        var buff = ch.Descriptor.Buffs.GetFact(bp);

                                                        return buff?.GetRank() > 1;
                                                    });

            BlueprintAction.Register<BlueprintBuff>(">",
                                                    (bp, ch, n) => ch.Descriptor.Buffs.GetFact(bp)?.AddRank(),
                                                    (bp, ch) => {
                                                        var buff = ch.Descriptor.Buffs.GetFact(bp);

                                                        return buff != null && buff.GetRank() < buff.Blueprint.Ranks - 1;
                                                    });

            // Abilities
            BlueprintAction.Register<BlueprintAbility>("Add",
                                                       (bp, ch, n) => ch.AddAbility(bp),
                                                       (bp, ch) => ch.CanAddAbility(bp));

            BlueprintAction.Register<BlueprintAbility>("At Will",
                                                       (bp, ch, n) => ch.AddSpellAsAbility(bp),
                                                       (bp, ch) => ch.CanAddSpellAsAbility(bp));

            BlueprintAction.Register<BlueprintAbility>("Remove",
                                                       (bp, ch, n) => ch.RemoveAbility(bp),
                                                       (bp, ch) => ch.HasAbility(bp));

            // BlueprintActivatableAbility
            BlueprintAction.Register<BlueprintActivatableAbility>("Add",
                                                                  (bp, ch, n) => ch.Descriptor.AddFact(bp),
                                                                  (bp, ch) => !ch.Descriptor.HasFact(bp));

            BlueprintAction.Register<BlueprintActivatableAbility>("Remove",
                                                                  (bp, ch, n) => ch.Descriptor.RemoveFact(bp),
                                                                  (bp, ch) => ch.Descriptor.HasFact(bp));

            // Quests
            BlueprintAction.Register<BlueprintQuest>("Start",
                                                     (bp, ch, n) => Game.Instance.Player.QuestBook.GiveObjective(bp.Objectives.First()),
                                                     (bp, ch) => Game.Instance.Player.QuestBook.GetQuest(bp) == null);

            BlueprintAction.Register<BlueprintQuest>("Complete",
                                                     (bp, ch, n) => {
                                                         foreach (var objective in bp.Objectives) {
                                                             Game.Instance.Player.QuestBook.CompleteObjective(objective);
                                                         }
                                                     }, (bp, ch) => Game.Instance.Player.QuestBook.GetQuest(bp)?.State == QuestState.Started);

            // Quests Objectives
            BlueprintAction.Register<BlueprintQuestObjective>("Start",
                                                              (bp, ch, n) => Game.Instance.Player.QuestBook.GiveObjective(bp),
                                                              (bp, ch) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest) == null);

            BlueprintAction.Register<BlueprintQuestObjective>("Complete",
                                                              (bp, ch, n) => Game.Instance.Player.QuestBook.CompleteObjective(bp),
                                                              (bp, ch) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest)?.State == QuestState.Started);

            // Etudes
            BlueprintAction.Register<BlueprintEtude>("Start",
                                                     (bp, ch, n) => Game.Instance.Player.EtudesSystem.StartEtude(bp),
                                                     (bp, ch) => Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));

            BlueprintAction.Register<BlueprintEtude>("Complete",
                                                     (bp, ch, n) => Game.Instance.Player.EtudesSystem.MarkEtudeCompleted(bp),
                                                     (bp, ch) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp) &&
                                                                 !Game.Instance.Player.EtudesSystem.EtudeIsCompleted(bp));

            // Cutscenes
            BlueprintAction.Register<Cutscene>("Play", (bp, ch, n) => {
                                                           Actions.ToggleModWindow();
                                                           CutscenePlayerData cutscenePlayerData = CutscenePlayerData.Queue.FirstOrDefault(c => c.PlayActionId == bp.name);

                                                           if (cutscenePlayerData != null) {
                                                               cutscenePlayerData.PreventDestruction = true;
                                                               cutscenePlayerData.Stop();
                                                               cutscenePlayerData.PreventDestruction = false;
                                                           }

                                                           var state = ContextData<SpawnedUnitData>.Current?.State;
                                                           CutscenePlayerView.Play(bp, null, true, state).PlayerData.PlayActionId = bp.name;
                                                       });

            // Teleport
            BlueprintAction.Register<BlueprintAreaEnterPoint>("Teleport", (bp, ch, n) => GameHelper.EnterToArea(bp, AutoSaveMode.None));
            BlueprintAction.Register<BlueprintGlobalMap>("Teleport", (bp, ch, n) => GameHelper.EnterToArea(bp.GlobalMapEnterPoint, AutoSaveMode.None));

            BlueprintAction.Register<BlueprintArea>("Teleport", (area, ch, n) => {
                                                                    var areaEnterPoints = BlueprintExensions.BlueprintsOfType<BlueprintAreaEnterPoint>();
                                                                    var blueprint = areaEnterPoints.FirstOrDefault(bp => bp is BlueprintAreaEnterPoint ep && ep.Area == area);

                                                                    if (blueprint is BlueprintAreaEnterPoint enterPoint) {
                                                                        GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
                                                                    }
                                                                });

            BlueprintAction.Register<BlueprintGlobalMapPoint>("Teleport", (globalMapPoint, ch, n) => {
                                                                              if (!Actions.TeleportToGlobalMapPoint(globalMapPoint)) {
                                                                                  Actions.TeleportToGlobalMap(() => Actions.TeleportToGlobalMapPoint(globalMapPoint));
                                                                              }
                                                                          });
        }
    }
}