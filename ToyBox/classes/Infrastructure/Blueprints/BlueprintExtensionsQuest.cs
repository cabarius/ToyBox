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
using Kingmaker;
using Kingmaker.EntitySystem;
using Kingmaker.View.MapObjects;
using Kingmaker.DialogSystem.Blueprints;

namespace ToyBox {

    public static partial class BlueprintExtensions {
        public class IntrestingnessEntry {
            public UnitEntityData unit { get; set; }
            public object source { get; set; }
            public ConditionsChecker checker { get; set; }
            public List<Element> elements { get; set; }
            public bool HasConditins => checker?.Conditions.Length > 0;
            public bool HasElements => elements?.Count > 0;
            public IntrestingnessEntry(UnitEntityData unit, object source, ConditionsChecker checker, List<Element> elements = null) {
                this.unit = unit;
                this.source = source;
                this.checker = checker;
                this.elements = elements;
            }
        }
        public static bool IsActive(this IntrestingnessEntry entry) => 
            (entry.checker?.IsActive() ?? false)
            || (entry?.elements.Any(element => element.IsActive()) ?? false)
            || (entry.elements?.Count > 0 && entry.source is ActionsHolder) // Kludge until we get more clever about analyzing dialog state.  This lets Lathimas show up as active
            ;
        public static bool IsActive(this Element element) => element switch {
            Conditional conditional => conditional.ConditionsChecker.Check(),
            Condition condition => condition.CheckCondition(),
            _ => false,
        };
        public static bool IsActive(this ConditionsChecker checker) => checker.Conditions.Any(c => c.CheckCondition());
        public static string CaptionString(this Condition condition) =>
            $"{condition.GetCaption().orange()} -> {(condition.CheckCondition() ? "True".green() : "False".yellow())}";
        public static string CaptionString(this Element element) => $"{element.GetCaption().orange()}";

        public static bool IsQuestRelated(this Element element) => element is GiveObjective
                                                                   || element is SetObjectiveStatus
                                                                   || element is StartEtude
                                                                   || element is CompleteEtude
                                                                   || element is UnlockFlag
                                                                   // || element is StartDialog
                                                                   || element is ObjectiveStatus
                                                                   || element is ItemsEnough
                                                                   || element is Conditional
                                                                   ;
        public static int InterestingnessCoefficent(this UnitEntityData unit) => unit.GetUnitInteractionConditions().Count(entry => entry.IsActive());
        public static List<BlueprintDialog> GetDialog(this UnitEntityData unit) {
            var dialogs = unit.Parts.Parts
                                         .OfType<UnitPartInteractions>()
                                         .SelectMany(p => p.m_Interactions)
                                         .OfType<Wrapper>()
                                         .Select(w => w.Source)
                                         .OfType<SpawnerInteractionDialog>()
                                         .Select(sid => sid.Dialog).ToList();
            return dialogs;
        }
        public static IEnumerable<IntrestingnessEntry> GetUnitInteractionConditions(this UnitEntityData unit) {
            var spawnInterations = unit.Parts.Parts
                               .OfType<UnitPartInteractions>()
                               .SelectMany(p => p.m_Interactions)
                               .OfType<Wrapper>()
                               .Select(w => w.Source);
            var result = new HashSet<IntrestingnessEntry>();
            var elements = new HashSet<IntrestingnessEntry>();
            
            // dialog
            var dialogInteractions = spawnInterations.OfType<SpawnerInteractionDialog>().ToList();
            // dialog interation conditions
            var dialogInteractionConditions = dialogInteractions
                                        .Where(di => di.Conditions?.Get() != null)
                                        .Select(di => new IntrestingnessEntry(unit, di.Dialog, di.Conditions.Get().Conditions));
            result.UnionWith(dialogInteractionConditions.ToHashSet());
            // dialog conditions
            var dialogConditions = dialogInteractions
                                   .Select(di => new IntrestingnessEntry(unit, di.Dialog, di.Dialog.Conditions));
            result.UnionWith(dialogConditions.ToHashSet());
            // dialog elements
            var dialogElements = dialogInteractions
                .Select(di => new IntrestingnessEntry(unit, di.Dialog, null, di.Dialog.ElementsArray.Where(e => e.IsQuestRelated()).ToList()));
            elements.UnionWith(dialogElements.ToHashSet());
            // dialog cue conditions
            var dialogCueConditions = dialogInteractions
                                      .Where(di => di.Dialog.FirstCue != null)
                                      .SelectMany(di => di.Dialog.FirstCue.Cues
                                                          .Where(cueRef => cueRef.Get() != null)
                                                          .Select(cueRef => new IntrestingnessEntry(unit, cueRef.Get(), cueRef.Get().Conditions)));
            result.UnionWith(dialogCueConditions.ToHashSet());
            
            // actions
            var actionInteractions = spawnInterations.OfType<SpawnerInteractionActions>();
            // action interaction conditions
            var actionInteractionConditions = actionInteractions
                                              .Where(ai => ai.Conditions?.Get() != null)
                                              .Select(ai => new IntrestingnessEntry(unit, ai, ai.Conditions.Get().Conditions));
            result.UnionWith(actionInteractionConditions.ToHashSet());
            // action conditions
            var actionConditions = actionInteractions
                                   .Where(ai => ai.Actions?.Get() != null)
                                   .SelectMany(ai => ai.Actions.Get().Actions.Actions
                                                   .Where(a => a is Conditional)
                                                   .Select(a =>  new IntrestingnessEntry(unit, ai.Actions.Get(), (a as Conditional).ConditionsChecker)));
            result.Union(actionConditions.ToHashSet());
            // action elements
            var actionElements = actionInteractions
                .Where(ai => ai.Actions?.Get() != null)
                .Select(ai => new IntrestingnessEntry(unit, ai.Actions.Get(), null, ai.Actions.Get().ElementsArray.Where(e => e.IsQuestRelated()).ToList()));
            elements.UnionWith(actionElements.ToHashSet());
            foreach (var entry in elements) {
                //Mod.Debug($"checking {entry}");
                var conditionals = entry.elements.OfType<Conditional>();
                if (conditionals.Any()) {
                    //Mod.Debug($"found {conditionals.Count()} Conditionals");
                    foreach (var conditional in conditionals) {
                        var newEntry = new IntrestingnessEntry(entry.unit, conditional, conditional.ConditionsChecker);
                        result.Add(newEntry);
                        //Mod.Debug($"    Added {conditional}");
                    }
                    var nonConditionals = entry.elements.Where(element => !(element is Conditional));
                    entry.elements = nonConditionals.ToList();
                }
            }
            result.UnionWith(elements);
            return result;
        }
        public static void RevealInterestingNPCs() {
            if (Game.Instance?.State?.Units is { } unitsPool) {
                var inerestingUnits = unitsPool.Where(u => u.InterestingnessCoefficent() > 0);
                foreach (var unit in inerestingUnits) {
                    Mod.Debug($"Revealing {unit.CharacterName}");
                    unit.SetIsRevealedSilent(true);
                }
            }
        }
    }
}