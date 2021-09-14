// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.QuestSystem;
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox {
    public abstract class BlueprintAction {
        public delegate void Perform(SimpleBlueprint bp, UnitEntityData ch = null, int count = 1, int listValue = 0);

        public delegate bool CanPerform(SimpleBlueprint bp, UnitEntityData ch = null, int listValue = 0);

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
        public new delegate void Perform(BPType bp, UnitEntityData ch, int count = 1, int listValue = 0);

        public new delegate bool CanPerform(BPType bp, UnitEntityData ch, int listValue = 0);

        public BlueprintAction(string name, Perform action, CanPerform canPerform = null, bool isRepeatable = false) : base(name, isRepeatable) {
            this.action = (bp, ch, n, l) => action((BPType)bp, ch, n, l);
            this.canPerform = (bp, ch, l) => Main.IsInGame && bp is BPType bpt && (canPerform?.Invoke(bpt, ch, l) ?? true);
        }

        public override Type BlueprintType => typeof(BPType);
    }

    public static class BlueprintActions {
        public static IEnumerable<BlueprintAction> GetActions(this SimpleBlueprint bp) {
            return BlueprintAction.ActionsForBlueprint(bp);
        }

        public static void InitializeActions() {
            var flags = Game.Instance.Player.UnlockableFlags;
            BlueprintAction.Register<BlueprintItem>("Add",
                                                    (bp, ch, n, l) => Game.Instance.Player.Inventory.Add(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintItem>("Remove",
                                                    (bp, ch, n, l) => Game.Instance.Player.Inventory.Remove(bp, n),
                                                    (bp, ch, l) => Game.Instance.Player.Inventory.Contains(bp), true);

            BlueprintAction.Register<BlueprintUnit>("Spawn",
                                                    (bp, ch, n, l) => Actions.SpawnUnit(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintFeature>("Add",
                                                       (bp, ch, n, l) => ch.Descriptor.AddFact(bp),
                                                       (bp, ch, l) => !ch.Progression.Features.HasFact(bp));

            BlueprintAction.Register<BlueprintFeature>("Remove",
                                                       (bp, ch, n, l) => ch.Progression.Features.RemoveFact(bp),
                                                       (bp, ch, l) => ch.Progression.Features.HasFact(bp));

            BlueprintAction.Register<BlueprintParametrizedFeature>("Add", (bp, ch, n, l) => ch?.Descriptor?.AddFact<UnitFact>(bp, null, bp.Items.ToArray()[BlueprintListUI.ParamSelected[l]].Param),
                                                       (bp, ch, l) => ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == bp.Items.ToArray()[BlueprintListUI.ParamSelected[l]].Param) == null);

            BlueprintAction.Register<BlueprintParametrizedFeature>("Remove", (bp, ch, n, l) => ch?.Progression?.Features?.RemoveFact(ch.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == bp.Items.ToArray()[BlueprintListUI.ParamSelected[l]].Param)),
                                                       (bp, ch, l) => ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == bp.Items.ToArray()[BlueprintListUI.ParamSelected[l]].Param) != null);

            BlueprintAction.Register<BlueprintFeature>("<",
                                                       (bp, ch, n, l) => ch.Progression.Features.GetFact(bp)?.RemoveRank(),
                                                       (bp, ch, l) => {
                                                           var feature = ch.Progression.Features.GetFact(bp);
                                                           return feature?.GetRank() > 1;
                                                       });

            BlueprintAction.Register<BlueprintFeature>(">",
                                                       (bp, ch, n, l) => ch.Progression.Features.GetFact(bp)?.AddRank(),
                                                       (bp, ch, l) => {
                                                           var feature = ch.Progression.Features.GetFact(bp);
                                                           return feature != null && feature.GetRank() < feature.Blueprint.Ranks;
                                                       });
            //BlueprintAction.Register<BlueprintArchetype>(
            //    "Add",
            //    (bp, ch, n, l) => ch.Progression.AddArchetype(ch.Progression.Classes.First().CharacterClass, bp),
            //    (bp, ch, l) => ch.Progression.CanAddArchetype(ch.Progression.Classes.First().CharacterClass, bp)
            //    );
            //BlueprintAction.Register<BlueprintArchetype>("Remove",
            //    (bp, ch, n, l) => ch.Progression.AddArchetype(ch.Progression.Classes.First().CharacterClass, bp),
            //    (bp, ch, l) => ch.Progression.Classes.First().Archetypes.Contains(bp)
            //    );

            // Spellbooks
            BlueprintAction.Register<BlueprintSpellbook>("Add",
                                                         (bp, ch, n, l) => ch.Descriptor.DemandSpellbook(bp.CharacterClass),
                                                         (bp, ch, l) => ch.Descriptor.Spellbooks.All(sb => sb.Blueprint != bp));

            BlueprintAction.Register<BlueprintSpellbook>("Remove",
                                                         (bp, ch, n, l) => ch.Descriptor.DeleteSpellbook(bp),
                                                         (bp, ch, l) => ch.Descriptor.Spellbooks.Any(sb => sb.Blueprint == bp));

            BlueprintAction.Register<BlueprintSpellbook>(">",
                                                         (bp, ch, n, l) => {
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
                                                         (bp, ch, l) => ch.Descriptor.Spellbooks.Any(sb => sb.Blueprint == bp && sb.CasterLevel < bp.MaxSpellLevel));

            // Buffs
            BlueprintAction.Register<BlueprintBuff>("Add",
                                                    (bp, ch, n, l) => GameHelper.ApplyBuff(ch, bp),
                                                    (bp, ch, l) => !ch.Descriptor.Buffs.HasFact(bp));

            BlueprintAction.Register<BlueprintBuff>("Remove",
                                                    (bp, ch, n, l) => ch.Descriptor.RemoveFact(bp),
                                                    (bp, ch, l) => ch.Descriptor.Buffs.HasFact(bp));

            BlueprintAction.Register<BlueprintBuff>("<",
                                                    (bp, ch, n, l) => ch.Descriptor.Buffs.GetFact(bp)?.RemoveRank(),
                                                    (bp, ch, l) => {
                                                        var buff = ch.Descriptor.Buffs.GetFact(bp);

                                                        return buff?.GetRank() > 1;
                                                    });

            BlueprintAction.Register<BlueprintBuff>(">",
                                                    (bp, ch, n, l) => ch.Descriptor.Buffs.GetFact(bp)?.AddRank(),
                                                    (bp, ch, l) => {
                                                        var buff = ch.Descriptor.Buffs.GetFact(bp);

                                                        return buff != null && buff.GetRank() < buff.Blueprint.Ranks - 1;
                                                    });

            // Abilities
            BlueprintAction.Register<BlueprintAbility>("Add",
                                                       (bp, ch, n, l) => ch.AddAbility(bp),
                                                       (bp, ch, l) => ch.CanAddAbility(bp));

            BlueprintAction.Register<BlueprintAbility>("At Will",
                                                       (bp, ch, n, l) => ch.AddSpellAsAbility(bp),
                                                       (bp, ch, l) => ch.CanAddSpellAsAbility(bp));

            BlueprintAction.Register<BlueprintAbility>("Remove",
                                                       (bp, ch, n, l) => ch.RemoveAbility(bp),
                                                       (bp, ch, l) => ch.HasAbility(bp));
            // Ability Resources

            BlueprintAction.Register<BlueprintAbilityResource>("Add",
                (bp, ch, n, l) => ch.Resources.Add(bp, true),
                (bp, ch, l) => !ch.Resources.ContainsResource(bp));

            BlueprintAction.Register<BlueprintAbilityResource>("Remove",
                (bp, ch, n, l) => ch.Resources.Remove(bp),
                (bp, ch, l) => ch.Resources.ContainsResource(bp));

            // Spellbooks


            // BlueprintActivatableAbility
            BlueprintAction.Register<BlueprintActivatableAbility>("Add",
                                                                  (bp, ch, n, l) => ch.Descriptor.AddFact(bp),
                                                                  (bp, ch, l) => !ch.Descriptor.HasFact(bp));

            BlueprintAction.Register<BlueprintActivatableAbility>("Remove",
                                                                  (bp, ch, n, l) => ch.Descriptor.RemoveFact(bp),
                                                                  (bp, ch, l) => ch.Descriptor.HasFact(bp));

            // Quests
            BlueprintAction.Register<BlueprintQuest>("Start",
                                                     (bp, ch, n, l) => Game.Instance.Player.QuestBook.GiveObjective(bp.Objectives.First()),
                                                     (bp, ch, l) => Game.Instance.Player.QuestBook.GetQuest(bp) == null);

            BlueprintAction.Register<BlueprintQuest>("Complete",
                                                     (bp, ch, n, l) => {
                                                         foreach (var objective in bp.Objectives) {
                                                             Game.Instance.Player.QuestBook.CompleteObjective(objective);
                                                         }
                                                     }, (bp, ch, l) => Game.Instance.Player.QuestBook.GetQuest(bp)?.State == QuestState.Started);

            // Quests Objectives
            BlueprintAction.Register<BlueprintQuestObjective>("Start",
                                                              (bp, ch, n, l) => Game.Instance.Player.QuestBook.GiveObjective(bp),
                                                              (bp, ch, l) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest) == null);

            BlueprintAction.Register<BlueprintQuestObjective>("Complete",
                                                              (bp, ch, n, l) => Game.Instance.Player.QuestBook.CompleteObjective(bp),
                                                              (bp, ch, l) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest)?.State == QuestState.Started);

            // Etudes
            BlueprintAction.Register<BlueprintEtude>("Start",
                                                     (bp, ch, n, l) => Game.Instance.Player.EtudesSystem.StartEtude(bp),
                                                     (bp, ch, l) => Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));

            BlueprintAction.Register<BlueprintEtude>("Complete",
                                                     (bp, ch, n, l) => Game.Instance.Player.EtudesSystem.MarkEtudeCompleted(bp),
                                                     (bp, ch, l) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp) &&
                                                                 !Game.Instance.Player.EtudesSystem.EtudeIsCompleted(bp));
            // Flags
            BlueprintAction.Register<BlueprintUnlockableFlag>("Unlock",
                (bp, ch, n, l) => flags.Unlock(bp),
                (bp, ch, l) => !flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>("Lock",
                (bp, ch, n, l) => flags.Lock(bp),
                (bp, ch, l) =>flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>(">",
                (bp, ch, n, l) => flags.SetFlagValue(bp, flags.GetFlagValue(bp) + n),
                (bp, ch, l) => flags.IsUnlocked(bp) );

            BlueprintAction.Register<BlueprintUnlockableFlag>("<",
                (bp, ch, n, l) => flags.SetFlagValue(bp, flags.GetFlagValue(bp) - n),
                (bp, ch, l) => flags.IsUnlocked(bp));

            // Cutscenes
            BlueprintAction.Register<Cutscene>("Play", (bp, ch, n, l) => {
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
            BlueprintAction.Register<BlueprintAreaEnterPoint>("Teleport", (bp, ch, n, l) => GameHelper.EnterToArea(bp, AutoSaveMode.None));
            BlueprintAction.Register<BlueprintGlobalMap>("Teleport", (bp, ch, n, l) => GameHelper.EnterToArea(bp.GlobalMapEnterPoint, AutoSaveMode.None));

            BlueprintAction.Register<BlueprintArea>("Teleport", (area, ch, n, l) => {
                var areaEnterPoints = BlueprintExensions.BlueprintsOfType<BlueprintAreaEnterPoint>();
                var blueprint = areaEnterPoints.FirstOrDefault(bp => bp is BlueprintAreaEnterPoint ep && ep.Area == area);

                if (blueprint is BlueprintAreaEnterPoint enterPoint) {
                    GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
                }
            });

            BlueprintAction.Register<BlueprintGlobalMapPoint>("Teleport", (globalMapPoint, ch, n, l) => {
                if (!Actions.TeleportToGlobalMapPoint(globalMapPoint)) {
                    Actions.TeleportToGlobalMap(() => Actions.TeleportToGlobalMapPoint(globalMapPoint));
                }
            });
        }
    }
}