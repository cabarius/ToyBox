// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Mechanics.Entities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics.Blueprints;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
namespace ToyBox {
    public abstract class BlueprintAction {
        public delegate void Perform(SimpleBlueprint bp, BaseUnitEntity? ch = null, int count = 1, int listValue = 0);

        public delegate bool CanPerform(SimpleBlueprint bp, BaseUnitEntity? ch = null, int listValue = 0);

        private static Dictionary<Type, BlueprintAction[]> actionsForType;

        public static BlueprintAction[] ActionsForType(Type type) {
            if (actionsForType == null) {
                actionsForType = new Dictionary<Type, BlueprintAction[]>();
                BlueprintActions.InitializeActions();
                BlueprintActions.InitializeActionsRT();
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
        public static void Register<T>(string? name, BlueprintAction<T>.Perform perform, BlueprintAction<T>.CanPerform? canPerform = null, bool isRepeatable = false) where T : SimpleBlueprint {
            var action = new BlueprintAction<T>(name, perform, canPerform, isRepeatable);
            var type = action.BlueprintType;
            actionsForType.TryGetValue(type, out var existing);
            existing ??= new BlueprintAction[] { };
            var list = existing.ToList();
            list.Add(action);
            actionsForType[type] = list.ToArray();
        }

        public string? name { get; protected set; }

        public Perform action;

        public CanPerform canPerform;

        protected BlueprintAction(string? name, bool isRepeatable) {
            this.name = name;
            this.isRepeatable = isRepeatable;
        }

        public bool isRepeatable;

        public abstract Type BlueprintType { get; }
    }

    public class BlueprintAction<BPType> : BlueprintAction where BPType : SimpleBlueprint {
        public new delegate void Perform(BPType bp, BaseUnitEntity ch, int count = 1, int listValue = 0);

        public new delegate bool CanPerform(BPType bp, BaseUnitEntity ch, int listValue = 0);

        public BlueprintAction(string? name, Perform action, CanPerform? canPerform = null, bool isRepeatable = false) : base(name, isRepeatable) {
            this.action = (bp, ch, n, index) => action((BPType)bp, ch, n, index);
            this.canPerform = (bp, ch, index) => Main.IsInGame && bp is BPType bpt && (canPerform?.Invoke(bpt, ch, index) ?? true);
        }

        public override Type BlueprintType => typeof(BPType);
    }

    public static partial class BlueprintActions {
        public static IEnumerable<BlueprintAction> GetActions(this SimpleBlueprint bp) => BlueprintAction.ActionsForBlueprint(bp);

        private static Dictionary<BlueprintFeatureSelection, BlueprintFeature[]> featureSelectionItems = new();
        public static BlueprintFeature FeatureSelectionItems(this BlueprintFeatureSelection feature, int index) {
            if (featureSelectionItems.TryGetValue(feature, out var value)) return index < value.Length ? value[index] : null;
            value = feature.AllFeatures.OrderBy(x => x.NameSafe()).ToArray();
            if (value == null) return null;
            featureSelectionItems[feature] = value;
            return index < value.Length ? value[index] : null;
        }

        public static void InitializeActions() {
            var flags = Game.Instance.Player.UnlockableFlags;
            BlueprintAction.Register<BlueprintItem>("Add".localize(),
                                                    (bp, ch, n, index) => Game.Instance.Player.Inventory.Add(bp, n), isRepeatable: true);

            BlueprintAction.Register<BlueprintItem>("Remove".localize(),
                                                    (bp, ch, n, index) => Game.Instance.Player.Inventory.Remove(bp, n),
                                                    (bp, ch, index) => Game.Instance.Player.Inventory.Contains(bp), true);

            BlueprintAction.Register<BlueprintUnit>("Spawn".localize(),
                                                    (bp, ch, n, index) => Actions.SpawnUnit(bp, n), isRepeatable: true);

            // Facts
            BlueprintAction.Register<BlueprintMechanicEntityFact>("Add".localize(),
                                                       (bp, ch, n, index) => ch.AddFact(bp),
                                                       (bp, ch, index) => !ch.Facts.List.Select(f => f.Blueprint).Contains(bp));

            BlueprintAction.Register<BlueprintMechanicEntityFact>("Remove".localize(),
                                                        (bp, ch, n, index) => ch.Facts.Remove(bp),
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

            // Teleport
            BlueprintAction.Register<BlueprintAreaEnterPoint>("Teleport".localize(), (enterPoint, ch, n, index) => Teleport.To(enterPoint));
            BlueprintAction.Register<BlueprintArea>("Teleport".localize(), (area, ch, n, index) => Teleport.To(area));

            // Quests
            BlueprintAction.Register<BlueprintQuest>("Start".localize(),
                                                     (bp, ch, n, index) => Game.Instance.Player.QuestBook.GiveObjective(bp.Objectives.First()),
                                                     (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp) == null);

            BlueprintAction.Register<BlueprintQuest>("Complete".localize(),
                                                     (bp, ch, n, index) => {
                                                         foreach (var objective in bp.Objectives) {
                                                             Game.Instance.Player.QuestBook.CompleteObjective(objective);
                                                         }
                                                     }, (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp)?.State == QuestState.Started);

            // Quests Objectives
            BlueprintAction.Register<BlueprintQuestObjective>("Start".localize(),
                                                              (bp, ch, n, index) => Game.Instance.Player.QuestBook.GiveObjective(bp),
                                                              (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest) == null);

            BlueprintAction.Register<BlueprintQuestObjective>("Complete".localize(),
                                                              (bp, ch, n, index) => Game.Instance.Player.QuestBook.CompleteObjective(bp),
                                                              (bp, ch, index) => Game.Instance.Player.QuestBook.GetQuest(bp.Quest)?.State == QuestState.Started);

            // Etudes
            BlueprintAction.Register<BlueprintEtude>("Start".localize(),
                                                     (bp, ch, n, index) => Game.Instance.Player.EtudesSystem.StartEtude(bp),
                                                     (bp, ch, index) => Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));
            BlueprintAction.Register<BlueprintEtude>("Complete".localize(),
                                                     (bp, ch, n, index) => Game.Instance.Player.EtudesSystem.MarkEtudeCompleted(bp),
                                                     (bp, ch, index) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp) &&
                                                                        !Game.Instance.Player.EtudesSystem.EtudeIsCompleted(bp));
            BlueprintAction.Register<BlueprintEtude>("Unstart".localize(),
                                         (bp, ch, n, index) =>
                                             Game.Instance.Player.EtudesSystem.UnstartEtude(bp),
                                         (bp, ch, index) => !Game.Instance.Player.EtudesSystem.EtudeIsNotStarted(bp));
            // Flags
            BlueprintAction.Register<BlueprintUnlockableFlag>("Unlock".localize(),
                (bp, ch, n, index) => flags.Unlock(bp),
                (bp, ch, index) => !flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>("Lock".localize(),
                (bp, ch, n, index) => flags.Lock(bp),
                (bp, ch, index) => flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>(">".localize(),
                (bp, ch, n, index) => flags.SetFlagValue(bp, flags.GetFlagValue(bp) + n),
                (bp, ch, index) => flags.IsUnlocked(bp));

            BlueprintAction.Register<BlueprintUnlockableFlag>("<".localize(),
                (bp, ch, n, index) => flags.SetFlagValue(bp, flags.GetFlagValue(bp) - n),
                (bp, ch, index) => flags.IsUnlocked(bp));
            // Cutscenes
            BlueprintAction.Register<Cutscene>("Play".localize(), (bp, ch, n, index) => {
                Actions.ToggleModWindow();
                var cutscenePlayerData = CutscenePlayerData.Queue.FirstOrDefault(c => c.PlayActionId == bp.name);

                if (cutscenePlayerData != null) {
                    cutscenePlayerData.PreventDestruction = true;
                    cutscenePlayerData.Stop();
                    cutscenePlayerData.PreventDestruction = false;
                }
                SceneEntitiesState state = null; // TODO: do we need this?
                CutscenePlayerView.Play(bp, null, true, state).PlayerData.PlayActionId = bp.name;
            });
        }
    }
}