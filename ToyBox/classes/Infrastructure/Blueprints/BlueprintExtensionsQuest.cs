// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using HarmonyLib;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Craft;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.UnitLogic.Parts;
using static Kingmaker.UnitLogic.Interaction.SpawnerInteractionPart;
using Kingmaker.UnitLogic.Interaction;

namespace ToyBox {

    public static partial class BlueprintExtensions {
        public class QuestObjectiveStatusEntry {
            public UnitEntityData unit { get; set; }
            public object source { get; set; }
            public ObjectiveStatus objectiveStatus { get; set; }

            public QuestObjectiveStatusEntry(UnitEntityData unit, object source, ObjectiveStatus objectiveStatus) {
                this.unit = unit;
                this.source = source;
                this.objectiveStatus = objectiveStatus;
            }
        }

        public static int GetDialogAndActionCounts(this UnitEntityData unit) {
            var spawnerInteractions = unit.Parts.Parts
                               .OfType<UnitPartInteractions>()
                               .SelectMany(p => p.m_Interactions)
                               .OfType<Wrapper>()
                               .Select(w => w.Source);
            var count = 0;
            var dialogs = spawnerInteractions.OfType<SpawnerInteractionDialog>().ToList();
            var dialogElements = dialogs.SelectMany(d => d.Dialog.ElementsArray);
            count += dialogElements.Count();
            var dialogConditions = dialogs
                                   .Where(d => d.Conditions?.Get() != null)
                                   .SelectMany(d => d.Conditions.Get().ElementsArray);
            count += dialogConditions.Count();
            var actions = spawnerInteractions.OfType<SpawnerInteractionActions>();
            var actionConditions = actions
                                   .Where(a => a.Conditions?.Get() != null)
                                   .SelectMany(a => a.Conditions.Get().ElementsArray.OfType<ObjectiveStatus>());
            count += actionConditions.Count();
            return count;
        }
        public static IEnumerable<QuestObjectiveStatusEntry> GetQuestObjectives(this UnitEntityData unit) {
            var spawnInterations = unit.Parts.Parts
                               .OfType<UnitPartInteractions>()
                               .SelectMany(p => p.m_Interactions)
                               .OfType<Wrapper>()
                               .Select(w => w.Source);
            IEnumerable<QuestObjectiveStatusEntry> result = new List<QuestObjectiveStatusEntry>();
            var dialogs = spawnInterations.OfType<SpawnerInteractionDialog>().ToList();
            var dialogObjectives = dialogs
                .SelectMany(d => d.Dialog.ElementsArray
                .OfType<ObjectiveStatus>()
                .Select(o => new QuestObjectiveStatusEntry(unit, d.Dialog, o)));
            result = result.Union(dialogObjectives);
            var dialogConditions = dialogs
                                   .Where(d => d.Conditions?.Get() != null)
                                   .SelectMany(d => d.Conditions.Get().ElementsArray
                                   .OfType<ObjectiveStatus>()
                                   .Select(o => new QuestObjectiveStatusEntry(unit, d, o)));
            result = result.Union(dialogConditions);
            var actions = spawnInterations.OfType<SpawnerInteractionActions>();
            var actionConditions = actions
                                   .Where(a => a.Conditions?.Get() != null)
                                   .SelectMany(a => a.Conditions.Get()
                                                            .ElementsArray.OfType<ObjectiveStatus>()
                                                            .Select(o => new QuestObjectiveStatusEntry(unit, a.name, o))
                                                            );
            result = result.Union(actionConditions);
            return result;
        }

    }
}