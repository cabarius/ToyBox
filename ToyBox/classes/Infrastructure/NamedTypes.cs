// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;

namespace ToyBox {
    public class NamedTypeFilter {
        public String name { get; }
        public Type type { get; }
        public NamedTypeFilter(String name, Type type) { this.name = name; this.type = type; }
    }

    public class NamedAction {
        public String name { get; }
        public Action action { get; }
        public NamedAction(String name, Action action) { this.name = name; this.action = action; }
    }
    public class NamedAction<T> {
        public String name { get; }
        public Action<T> action { get; }
        public NamedAction(String name, Action<T> action) { this.name = name; this.action = action; }
    }

    public class NamedFunc<T> {
        public String name { get; }
        public Func<T> func { get; }
        public NamedFunc(String name, Func<T> func) { this.name = name; this.func = func; }
    }

    public class NamedMutator<Target, T> {
        public String name { get; }
        public Type type { get; }
        public Action<Target, T> action { get; }
        public Func<Target, T, bool> canPerform { get; }
        public NamedMutator(
            String name,
            Type type,
            Action<Target, T> action,
            Func<Target, T, bool> canPerform = null
            ) {
            this.name = name;
            this.type = type;
            this.action = action;
            this.canPerform = canPerform != null ? canPerform : (target, value) => true;
        }
    }
}