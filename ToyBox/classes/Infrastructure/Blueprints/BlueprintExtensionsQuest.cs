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
using static ToyBox.BlueprintExtensions;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace ToyBox {

    public static partial class BlueprintExtensions {
        public class UnitInteractionConditionsCheckerEntry {
            public UnitEntityData unit { get; set; }
            public object source { get; set; }
            public ConditionsChecker checker { get; set; }

            public UnitInteractionConditionsCheckerEntry(UnitEntityData unit, object source, ConditionsChecker checker) {
                this.unit = unit;
                this.source = source;
                this.checker = checker;
            }
        }
        public static string CaptionString(this Condition condition) =>
            $"{condition.GetCaption().orange()} -> {(condition.CheckCondition() ? "True".green() : "False".yellow())}";
        

        public static int GetUnitIterestingnessCoefficent(this UnitEntityData unit) => unit.GetUnitInteractionConditions().Count(c => c.checker.Conditions.Any(c => c.CheckCondition()));
    
        public static IEnumerable<UnitInteractionConditionsCheckerEntry> GetUnitInteractionConditions(this UnitEntityData unit) {
            var spawnInterations = unit.Parts.Parts
                               .OfType<UnitPartInteractions>()
                               .SelectMany(p => p.m_Interactions)
                               .OfType<Wrapper>()
                               .Select(w => w.Source);
            IEnumerable<UnitInteractionConditionsCheckerEntry> result = new List<UnitInteractionConditionsCheckerEntry>();
            var dialogInteractions = spawnInterations.OfType<SpawnerInteractionDialog>().ToList();
            var interactionConditions = dialogInteractions
                                   .Where(di => di.Conditions?.Get() != null)
                                   .Select(di => new UnitInteractionConditionsCheckerEntry(unit, di, di.Conditions.Get().Conditions));
            result = result.Union(interactionConditions);
            var dialogConditions = dialogInteractions
                                   .Select(di => new UnitInteractionConditionsCheckerEntry(unit, di, di.Dialog.Conditions));
            result = result.Union(dialogConditions);
            var actionInteractions = spawnInterations.OfType<SpawnerInteractionActions>();
            var actionInteractionConditions = actionInteractions
                                              .Where(ai => ai.Conditions?.Get() != null)
                                              .Select(ai => new UnitInteractionConditionsCheckerEntry(unit, ai, ai.Conditions.Get().Conditions));
            result = result.Union(actionInteractionConditions);
            var actionConditions = actionInteractions
                                   .Where(ai => ai.Actions?.Get() != null)
                                   .SelectMany(ai => ai.Actions.Get().Actions.Actions
                                                   .Where(a => a is Conditional)
                                                   .Select(a =>  new UnitInteractionConditionsCheckerEntry(unit, ai, (a as Conditional).ConditionsChecker)));
            result = result.Union(actionConditions);
            return result;
        }
    }
}