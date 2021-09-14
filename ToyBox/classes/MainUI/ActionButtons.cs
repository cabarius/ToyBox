// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using ModKit;
using System;
using System.Collections.Generic;

namespace ToyBox {
    public class NamedTypeFilter {
        public String name { get; }
        public Type type { get; }
        public Func<SimpleBlueprint, bool> filter;
        public Func<SimpleBlueprint, String> collator;
        public Func<IEnumerable<SimpleBlueprint>> blueprintSource;
        protected NamedTypeFilter(String name, Type type, Func<SimpleBlueprint, bool> filter = null, Func<SimpleBlueprint, String> collator = null, Func<IEnumerable<SimpleBlueprint>> blueprintSource = null) {
            this.name = name;
            this.type = type;
            this.filter = filter ?? (_ => true);
            this.collator = collator;
            this.blueprintSource = blueprintSource;
        }
    }
    public class NamedTypeFilter<TBlueprint> : NamedTypeFilter where TBlueprint : SimpleBlueprint {
        public NamedTypeFilter(String name, Func<TBlueprint, bool> filter = null, Func<TBlueprint, String> collator = null, Func<IEnumerable<SimpleBlueprint>> blueprintSource = null)
            : base(name, typeof(TBlueprint), null, null, blueprintSource) {
            if (filter != null) this.filter = bp => filter((TBlueprint)bp);
            if (collator != null) this.collator = bp => collator((TBlueprint)bp);
        }
    }
    public static class ActionButtons {
        public static Settings settings => Main.settings;

        public static void ResetGUI() { }

        // convenience extensions for constructing UI for special types
        public static void ActionButton<T>(this NamedAction<T> namedAction, T value, Action buttonAction, float width = 0) {
            if (namedAction?.canPerform(value) == true) {
                UI.ActionButton(namedAction.name, buttonAction, width == 0 ? UI.AutoWidth() : UI.Width(width));
            }
            else {
                UI.Space(width + 3);
            }
        }
        public static void MutatorButton<U, T>(this NamedMutator<U, T> mutator, U unit, T value, Action buttonAction, float width = 0) {
            if (mutator?.canPerform(unit, value) == true) {
                UI.ActionButton(mutator.name, buttonAction, width == 0 ? UI.AutoWidth() : UI.Width(width));
            }
            else {
                UI.Space(width + 3);
            }
        }
        public static void BlueprintActionButton(this BlueprintAction action, UnitEntityData unit, SimpleBlueprint bp, Action buttonAction, float width) {
            if (action?.canPerform(bp, unit) == true) {
                UI.ActionButton(action.name, buttonAction, width == 0 ? UI.AutoWidth() : UI.Width(width));
            }
            else {
                UI.Space(width + 3);
            }
        }
    }
}
