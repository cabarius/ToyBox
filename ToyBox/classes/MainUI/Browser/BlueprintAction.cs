﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

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
using Kingmaker.Blueprints.Facts;
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
    public abstract class BlueprintAction {
        public delegate void Perform(SimpleBlueprint bp, UnitEntityData ch = null, int count = 1, int listValue = 0);

        public delegate bool CanPerform(SimpleBlueprint bp, UnitEntityData ch = null, int listValue = 0);

        private static Dictionary<Type, BlueprintAction[]> actionsForType;

        public static BlueprintAction[] ActionsForType(Type type) {
            if (actionsForType == null) {
                actionsForType = new Dictionary<Type, BlueprintAction[]>();
                BlueprintActions.InitializeActions();
            }

            actionsForType.TryGetValue(type, out var result);

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

        public static IEnumerable<BlueprintAction> ActionsForBlueprint(SimpleBlueprint bp) => ActionsForType(bp.GetType());
        public static void Register<T>(string name, BlueprintAction<T>.Perform perform, BlueprintAction<T>.CanPerform canPerform = null, bool isRepeatable = false) where T : SimpleBlueprint {
            var action = new BlueprintAction<T>(name, perform, canPerform, isRepeatable);
            var type = action.BlueprintType;
            actionsForType.TryGetValue(type, out var existing);
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
            this.action = (bp, ch, n, index) => action((BPType)bp, ch, n, index);
            this.canPerform = (bp, ch, index) => Main.IsInGame && bp is BPType bpt && (canPerform?.Invoke(bpt, ch, index) ?? true);
        }

        public override Type BlueprintType => typeof(BPType);
    }

    public static class BlueprintActions {
        public static IEnumerable<BlueprintAction> GetActions(this SimpleBlueprint bp) => BlueprintAction.ActionsForBlueprint(bp);
        private static Dictionary<BlueprintParametrizedFeature, IFeatureSelectionItem[]> parametrizedSelectionItems = new();
        public static IFeatureSelectionItem ParametrizedSelectionItems(this BlueprintParametrizedFeature feature, int index) {
            if (parametrizedSelectionItems.TryGetValue(feature, out var value)) return index < value.Length ? value[index] : null ;
            value = feature.Items.OrderBy(x => x.Name).ToArray();
            if (value == null) return null;
            parametrizedSelectionItems[feature] = value;
            return index < value.Length ? value[index] : null;
        }
        private static Dictionary<BlueprintFeatureSelection, BlueprintFeature[]> featureSelectionItems = new();
        public static BlueprintFeature FeatureSelectionItems(this BlueprintFeatureSelection feature, int index) {
            if (featureSelectionItems.TryGetValue(feature, out var value)) return index < value.Length ? value[index] : null;
            value = feature.AllFeatures.OrderBy(x => x.Name).ToArray();
            if (value == null) return null;
            featureSelectionItems[feature] = value;
            return index < value.Length ? value[index] : null;
        }
        public static void InitializeActions() {
            var flags = Game.Instance.Player.UnlockableFlags;
            BlueprintAction.Register<BlueprintItem>("Add",
                                                    (bp, ch, n, index) => Game.Instance.Player.Inventory.Add(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintItem>("Remove",
                                                    (bp, ch, n, index) => Game.Instance.Player.Inventory.Remove(bp, n),
                                                    (bp, ch, index) => Game.Instance.Player.Inventory.Contains(bp), true);

            BlueprintAction.Register<BlueprintUnit>("Spawn",
                                                    (bp, ch, n, index) => Actions.SpawnUnit(bp, n), isRepeatable: true);

            // Features
            BlueprintAction.Register<BlueprintFeature>("Add",
                                                       (bp, ch, n, index) => ch.Progression.Features.AddFeature(bp),
                                                       (bp, ch, index) => !ch.Progression.Features.HasFact(bp));

            BlueprintAction.Register<BlueprintFeature>("Remove",
                                                       (bp, ch, n, index) => ch.Progression.Features.RemoveFact(bp),
                                                       (bp, ch, index) => ch.Progression.Features.HasFact(bp));
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
            // Paramaterized Feature
            BlueprintAction.Register<BlueprintParametrizedFeature>("Add",
                 (bp, ch, n, index) => {
                     var value = bp.ParametrizedSelectionItems(BlueprintListUI.ParamSelected[index])?.Param;
                     ch?.Descriptor?.AddFact<UnitFact>(bp, null, value);
                 },
                (bp, ch, index) => {
                    var value = bp.ParametrizedSelectionItems(BlueprintListUI.ParamSelected[index])?.Param;
                    var existing = ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == value);
                    return existing == null;
                });
            BlueprintAction.Register<BlueprintParametrizedFeature>("Remove",
                (bp, ch, n, index) => {
                    var value = bp.ParametrizedSelectionItems(BlueprintListUI.ParamSelected[index])?.Param;
                    var fact = ch.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == value);
                    ch?.Progression?.Features?.RemoveFact(fact);
                },
                (bp, ch, index) => {
                    if (bp.Items.Count() == 0) return false;
                    var value = bp.ParametrizedSelectionItems(BlueprintListUI.ParamSelected[index])?.Param;
                    var existing = ch?.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == value);
                    return existing != null;
                });
            // Feature Selection
            BlueprintAction.Register<BlueprintFeatureSelection>("Add",
                 (bp, ch, n, index) => {
                     var value = bp.FeatureSelectionItems(BlueprintListUI.ParamSelected[index]);
                     var source = new FeatureSource();
                     ch?.Descriptor?.Progression.Features.AddFeature(bp).SetSource(source, 1);
                     ch?.Progression?.AddSelection(bp, source, 0, value);

                 },
                (bp, ch, index) => {
                    var progression = ch?.Descriptor?.Progression;
                    if (progression == null) return false;
                    if (!progression.Features.HasFact(bp)) return true;
                    var value = bp.FeatureSelectionItems(BlueprintListUI.ParamSelected[index]);
                    if (progression.Selections.TryGetValue(bp, out var selection)) {
                        if (selection.SelectionsByLevel.Values.Any(l => l.Any(f => f == value))) return false;
                    }
                    return true;
                });
            BlueprintAction.Register<BlueprintFeatureSelection>("Rem. All",
                (bp, ch, n, index) => {
                    var progression = ch?.Descriptor?.Progression;
                    var value = bp.FeatureSelectionItems(BlueprintListUI.ParamSelected[index]);
                    //Feature fact = progression.Features.GetFact(bp);
                    var fact = ch.Descriptor?.Unit?.Facts?.Get<Feature>(i => i.Blueprint == bp && i.Param == value);
                    var selections = ch?.Descriptor?.Progression.Selections;
                    BlueprintFeatureSelection featureSelection = null;
                    FeatureSelectionData featureSelectionData = null;
                    int level = -1;
                    foreach (var selection in selections) {
                        foreach (var keyValuePair in selection.Value.SelectionsByLevel) {
                            if (keyValuePair.Value.HasItem<BlueprintFeature>(bp)) {
                                featureSelection = selection.Key;
                                featureSelectionData = selection.Value;
                                level = keyValuePair.Key;
                                break;
                            }
                        }
                        if (level >= 0)
                            break;
                    }
                    featureSelectionData?.RemoveSelection(level, value);
                    progression.Features.RemoveFact(bp);
                },
                (bp, ch, index) => {
                    var progression = ch?.Descriptor?.Progression;
                    if (progression == null) return false;
                    if (!progression.Features.HasFact(bp)) return false;
                    var value = bp.FeatureSelectionItems(BlueprintListUI.ParamSelected[index]);
                    if (progression.Selections.TryGetValue(bp, out var selection)) {
                        if (selection.SelectionsByLevel.Values.Any(l => l.Any(f => f == value))) return true;
                    }
                    return false;
                });

            // Facts
            BlueprintAction.Register<BlueprintUnitFact>("Add",
                                                       (bp, ch, n, index) => ch.AddFact(bp),
                                                       (bp, ch, index) => !ch.Facts.List.Select(f => f.Blueprint).Contains(bp));

            BlueprintAction.Register<BlueprintUnitFact>("Remove",
                                                       (bp, ch, n, index) => ch.RemoveFact(bp),
                                                       (bp, ch, index) => ch.Facts.List.Select(f => f.Blueprint).Contains(bp));

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
            BlueprintAction.Register<BlueprintAreaEnterPoint>("Teleport", (enterPoint, ch, n, index) => Teleport.To(enterPoint));
            BlueprintAction.Register<BlueprintGlobalMap>("Teleport", (map, ch, n, index) => Teleport.To(map));
            BlueprintAction.Register<BlueprintArea>("Teleport", (area, ch, n, index) => Teleport.To(area));
            BlueprintAction.Register<BlueprintGlobalMapPoint>("Teleport", (globalMapPoint, ch, n, index) => Teleport.To(globalMapPoint));

            //Army
            BlueprintAction.Register<BlueprintArmyPreset>("Create Friendly", (bp, ch, n, l) => {
                Actions.CreateArmy(bp,true);
            });
            BlueprintAction.Register<BlueprintArmyPreset>("Create Hostile", (bp, ch, n, l) => {
                Actions.CreateArmy(bp,false);
            });

            //ArmyGeneral
            BlueprintAction.Register<BlueprintLeaderSkill>("Add",
                (bp, ch, n, l) => Actions.AddSkillToLeader(bp),
                (bp, ch, index) => Actions.LeaderSelected(bp) && !Actions.LeaderHasSkill(bp));

            //ArmyGeneral
            BlueprintAction.Register<BlueprintLeaderSkill>("Remove",
                (bp, ch, n, l) => Actions.RemoveSkillFromLeader(bp),
                (bp, ch, index) => Actions.LeaderSelected(bp) && Actions.LeaderHasSkill(bp));
        }
    }
}