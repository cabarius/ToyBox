// Copyright < 2021 > Narria(github user Cabarius) - License: MIT
using UnityEngine;
using UnityModManagerNet;
using UnityEngine.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.AI.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Armies;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.Armies.TacticalCombat.Blueprints;
using Kingmaker.Armies.TacticalCombat.Brain;
using Kingmaker.Armies.TacticalCombat.Brain.Considerations;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.CharGen;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Credits;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Blueprints.Console;
using Kingmaker.Controllers.Rest;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Events;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Interaction;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Tutorial;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;
using Kingmaker.UnitLogic.ActivatableAbilities;

namespace ToyBox {
    public abstract class BlueprintAction {
        public delegate void Perform(BlueprintScriptableObject bp, UnitEntityData ch = null, int count = 1);
        public delegate bool CanPerform(BlueprintScriptableObject bp, UnitEntityData ch = null);
        private static Dictionary<Type, BlueprintAction[]> actionsForType = null;
        public static BlueprintAction[] ActionsForType(Type type) {
            if (actionsForType == null) {
                actionsForType = new Dictionary<Type, BlueprintAction[]> { };
                BlueprintActions.InitializeActions();
            }
            BlueprintAction[] result;
            actionsForType.TryGetValue(type, out result);
            if (result == null) {
                var baseType = type.BaseType;
                if (baseType != null)
                    result = ActionsForType(baseType);
                if (result == null) {
                    result = new BlueprintAction[] { };
                }
                actionsForType[type] = result;
            }
            return result;
        }
        public static BlueprintAction[] ActionsForBlueprint(BlueprintScriptableObject bp) {
            return ActionsForType(bp.GetType());
        }
        public static void Register(params BlueprintAction[] actions) {
            foreach (var action in actions) {
                var type = action.BlueprintType;
                BlueprintAction[] existing;
                actionsForType.TryGetValue(type, out existing);
                if (existing == null) {
                    existing = new BlueprintAction[] { };
                }
                var list = existing.ToList();
                list.Add(action);
                actionsForType[type] = list.ToArray();
            }
        }

        public String name { get; protected set; }
        public Perform action;
        public CanPerform canPerform;
        protected BlueprintAction(String name, bool isRepeatable) { this.name = name; this.isRepeatable = isRepeatable; }
        public bool isRepeatable;
        abstract public Type BlueprintType { get; }
    }
    public class BlueprintAction<BPType> : BlueprintAction where BPType : BlueprintScriptableObject {
        public delegate void Perform(BPType bp, UnitEntityData ch, int count = 1);
        public delegate bool CanPerform(BPType bp, UnitEntityData ch);

        public BlueprintAction(
            String name,
            Perform action,
            CanPerform canPerform = null,
            bool isRepeatable = false
            ) : base(name, isRepeatable) {
            this.action = (bp, ch, n) => action((BPType)bp, ch, n);
            this.canPerform = (bp, ch) => (bp is BPType bpt) ? (canPerform != null ? canPerform(bpt, ch) : true) : false;
        }
        override public Type BlueprintType { get { return typeof(BPType); } }
    }
    public static class BlueprintActions {
        public static IEnumerable<BlueprintAction> GetActions(this BlueprintScriptableObject bp) {
            return BlueprintAction.ActionsForBlueprint(bp);
        }
        public static void InitializeActions() {
            BlueprintAction.Register(
                new BlueprintAction<BlueprintItem>("Add",
                (bp, ch, n) => Game.Instance.Player.Inventory.Add(bp, n, null),
                null,
                true
                ),
            new BlueprintAction<BlueprintItem>("Remove",
                (bp, ch, n) => Game.Instance.Player.Inventory.Remove(bp, n),
                (bp, ch) => Game.Instance.Player.Inventory.Contains(bp),
                true
                ),
            new BlueprintAction<BlueprintUnit>("Spawn",
                (bp, ch, n) => Actions.SpawnUnit(bp, n),
                null,
                true
                ),
#if false
            new BlueprintAction("Kill", typeof(BlueprintUnit),
                (bp, ch) => { Actions.SpawnUnit((BlueprintUnit)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintUnit),
                (bp, ch) => {CheatsCombat.Kill((BlueprintUnit)bp); },
                (bp, ch) => { return ch.Inventory.Contains((BlueprintUnit)bp);  }
                ),
#endif
            new BlueprintAction<BlueprintAreaEnterPoint>("Teleport",
                (bp, ch, n) => GameHelper.EnterToArea(bp, AutoSaveMode.None)
                ),
            new BlueprintAction<BlueprintArea>("Teleport",
                (area, ch, n) => {
                    var areaEnterPoints = BlueprintExensions.BlueprintsOfType<BlueprintAreaEnterPoint>();
                    var blueprint = areaEnterPoints.Where(bp => (bp is BlueprintAreaEnterPoint ep) ? ep.Area == area : false).FirstOrDefault();
                    if (blueprint is BlueprintAreaEnterPoint enterPoint)
                        GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
                }),
            new BlueprintAction<BlueprintFeature>("Add",
                (bp, ch, n) => ch.Descriptor.AddFact(bp),
                (bp, ch) => !ch.Progression.Features.HasFact(bp)
                ),
            new BlueprintAction<BlueprintFeature>("Remove",
                (bp, ch, n) => ch.Progression.Features.RemoveFact(bp),
                (bp, ch) => ch.Progression.Features.HasFact(bp)
                ),
            new BlueprintAction<BlueprintFeature>("<",
                (bp, ch, n) => { try { ch.Progression.Features.GetFact(bp).RemoveRank(); } catch (Exception e) { Logger.Log(e); } },
                (bp, ch) => {
                    var feature = ch.Progression.Features.GetFact(bp);
                    return feature != null && feature.GetRank() > 1;
                }),
            new BlueprintAction<BlueprintFeature>(">",
                (bp, ch, n) => ch.Progression.Features.GetFact(bp).AddRank(),
                (bp, ch) => {
                    var feature = ch.Progression.Features.GetFact(bp);
                    return feature != null && feature.GetRank() < feature.Blueprint.Ranks;
                }),

            // Spellbooks
            new BlueprintAction<BlueprintSpellbook>("Add",
                (bp, ch, n) => { ch.Descriptor.DemandSpellbook(bp.CharacterClass); },
                (bp, ch) => !ch.Descriptor.Spellbooks.Any((sb) => sb.Blueprint == bp)
                ),
            new BlueprintAction<BlueprintSpellbook>("Remove",
                (bp, ch, n) => ch.Descriptor.DeleteSpellbook(bp),
                (bp, ch) => ch.Descriptor.Spellbooks.Any((sb) => sb.Blueprint == bp)
                ),
            new BlueprintAction<BlueprintSpellbook>(">",
                (bp, ch, n) => {
                    try {
                        var spellbook = ch.Descriptor.Spellbooks.First((sb) => sb.Blueprint == bp);
                        if (spellbook.IsMythic) spellbook.AddMythicLevel();
                        else spellbook.AddBaseLevel();
                    }
                    catch (Exception e) { Logger.Log(e); }
                },
                (bp, ch) => ch.Descriptor.Spellbooks.Any((sb) => sb.Blueprint == bp && sb.CasterLevel < bp.MaxSpellLevel)
                ),

            // Buffs
            new BlueprintAction<BlueprintBuff>("Add",
                (bp, ch, n) => GameHelper.ApplyBuff(ch, bp),
                (bp, ch) => !ch.Descriptor.Buffs.HasFact(bp)
                ),
            new BlueprintAction<BlueprintBuff>("Remove",
                (bp, ch, n) => ch.Descriptor.RemoveFact(bp),
                (bp, ch) => ch.Descriptor.Buffs.HasFact(bp)
                ),
            new BlueprintAction<BlueprintBuff>("<",
                (bp, ch, n) => ch.Descriptor.Buffs.GetFact(bp).RemoveRank(),
                (bp, ch) => {
                    var buff = ch.Descriptor.Buffs.GetFact(bp);
                    return buff != null && buff.GetRank() > 1;
                }),
            new BlueprintAction<BlueprintBuff>(">",
                (bp, ch, n) => ch.Descriptor.Buffs.GetFact(bp).AddRank(),
                (bp, ch) => {
                    var buff = ch.Descriptor.Buffs.GetFact(bp);
                    return buff != null && buff.GetRank() < buff.Blueprint.Ranks - 1;
                }),

            // Abilities
            new BlueprintAction<BlueprintAbility>("Add",
                (bp, ch, n) => ch.AddAbility(bp),
                (bp, ch) => ch.CanAddAbility(bp)
                ),
            new BlueprintAction<BlueprintAbility>("At Will",
                (bp, ch, n) => ch.AddSpellAsAbility(bp),
                (bp, ch) => ch.CanAddSpellAsAbility(bp)
                ),
            new BlueprintAction<BlueprintAbility>("Remove",
                (bp, ch, n) => ch.RemoveAbility(bp),
                (bp, ch) => ch.HasAbility(bp)
                ),
            // BlueprintActivatableAbility
            new BlueprintAction<BlueprintActivatableAbility>("Add",
                (bp, ch, n) => ch.Descriptor.AddFact(bp),
                (bp, ch) => !ch.Descriptor.HasFact(bp)
            ),
            new BlueprintAction<BlueprintActivatableAbility>("Remove",
                (bp, ch, n) => ch.Descriptor.RemoveFact(bp),
                (bp, ch) => ch.Descriptor.HasFact(bp)
                )
            );
        }
    }
}
