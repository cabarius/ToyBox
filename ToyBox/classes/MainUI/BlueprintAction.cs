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

namespace ToyBox {
    public class BlueprintAction : NamedMutator<UnitEntityData, BlueprintScriptableObject> {
        public BlueprintAction(
            String name,
            Type type,
            Action<UnitEntityData, BlueprintScriptableObject, int> action,
            Func<UnitEntityData, BlueprintScriptableObject, bool> canPerform = null,
            bool isRepeatable = false
            ) : base(name, type, action, canPerform, isRepeatable) { }


        public static BlueprintAction[] globalActions = new BlueprintAction[] {
            new BlueprintAction("Add", typeof(BlueprintItem),
                (ch, bp , n) => { Game.Instance.Player.Inventory.Add((BlueprintItem)bp, n, null); },
                null,
                true
                ),
            new BlueprintAction("Remove", typeof(BlueprintItem),
                (ch, bp, n) => { Game.Instance.Player.Inventory.Remove((BlueprintItem)bp, n); },
                (ch, bp) => { return Game.Instance.Player.Inventory.Contains((BlueprintItem)bp); },
                true
                ),
            new BlueprintAction("Spawn", typeof(BlueprintUnit),
                (ch, bp, n) => { Actions.SpawnUnit((BlueprintUnit)bp, n); },
                null,
                true
                ),
            new BlueprintAction("Teleport", typeof(BlueprintAreaEnterPoint),
                (ch, bp, n) => {
                    var enterPoint = (BlueprintAreaEnterPoint)bp;
                    GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
                }),
            //new BlueprintAction("Teleport", typeof(BlueprintArea),
            //    (ch, bp) => {
            //        var area = (BlueprintArea)bp;
            //        var enterPoint = Utilities.GetEnterPoint(area);
            //        GameHelper.EnterToArea(enterPoint, AutoSaveMode.None);
            //    }),
#if false
            new BlueprintAction("Kill", typeof(BlueprintUnit),
                (ch, bp) => { Actions.SpawnUnit((BlueprintUnit)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintUnit),
                (ch, bp) => {CheatsCombat.Kill((BlueprintUnit)bp); },
                (ch, bp) => { return ch.Inventory.Contains((BlueprintUnit)bp);  }
                ),
#endif
        };

        public static BlueprintAction[] characterActions = new BlueprintAction[] {
            // Features
            new BlueprintAction("Add", typeof(BlueprintFeature),
                (ch, bp, n) => { ch.Descriptor.AddFact((BlueprintUnitFact)bp); },
                (ch, bp) => { return !ch.Progression.Features.HasFact((BlueprintUnitFact)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintFeature),
                (ch, bp, n) => { ch.Progression.Features.RemoveFact((BlueprintUnitFact)bp); },
                (ch, bp) => { return ch.Progression.Features.HasFact((BlueprintUnitFact)bp);  }
                ),
            new BlueprintAction("<", typeof(BlueprintFeature),
                (ch, bp, n) => { try { ch.Progression.Features.GetFact((BlueprintUnitFact)bp).RemoveRank(); } catch (Exception e) { Logger.Log(e); } },
                (ch, bp) => {
                    var feature = ch.Progression.Features.GetFact((BlueprintUnitFact)bp);
                    return feature != null && feature.GetRank() > 1;
                }),
            new BlueprintAction(">", typeof(BlueprintFeature),
                (ch, bp, n) => { ch.Progression.Features.GetFact((BlueprintUnitFact)bp).AddRank(); },
                (ch, bp) => {
                    var feature = ch.Progression.Features.GetFact((BlueprintUnitFact)bp);
                    return feature != null && feature.GetRank() < feature.Blueprint.Ranks;
                }),

            // Spellbooks
            new BlueprintAction("Add", typeof(BlueprintSpellbook),
                (ch, bp, n) => { ch.Descriptor.DemandSpellbook(((BlueprintSpellbook)bp).CharacterClass); },
                (ch, bp) => { return !ch.Descriptor.Spellbooks.Any((sb) => sb.Blueprint == (BlueprintSpellbook)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintSpellbook),
                (ch, bp, n) => { ch.Descriptor.DeleteSpellbook((BlueprintSpellbook)bp); },
                (ch, bp) => { return ch.Descriptor.Spellbooks.Any((sb) => sb.Blueprint == (BlueprintSpellbook)bp);  }
                ),
            new BlueprintAction(">", typeof(BlueprintSpellbook),
                (ch, bp, n) => {
                    try {
                        var spellbook = ch.Descriptor.Spellbooks.First((sb) => sb.Blueprint == (BlueprintSpellbook)bp);
                        if (spellbook.IsMythic) spellbook.AddMythicLevel();
                        else spellbook.AddBaseLevel();
                    }
                    catch (Exception e) { Logger.Log(e); }
                },
                (ch, bp) => ch.Descriptor.Spellbooks.Any((sb) => sb.Blueprint == (BlueprintSpellbook)bp && sb.CasterLevel < ((BlueprintSpellbook)bp).MaxSpellLevel)
                ),

            // Buffs
            new BlueprintAction("Add", typeof(BlueprintBuff),
                (ch, bp, n) => { GameHelper.ApplyBuff(ch,(BlueprintBuff)bp); },
                (ch, bp) => { return !ch.Descriptor.Buffs.HasFact((BlueprintUnitFact)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintBuff),
                (ch, bp, n) => { ch.Descriptor.RemoveFact((BlueprintUnitFact)bp); },
                (ch, bp) => { return ch.Descriptor.Buffs.HasFact((BlueprintBuff)bp);  }
                ),
            new BlueprintAction("<", typeof(BlueprintBuff),
                (ch, bp, n) => { ch.Descriptor.Buffs.GetFact((BlueprintBuff)bp).RemoveRank(); },
                (ch, bp) => {
                    var buff = ch.Descriptor.Buffs.GetFact((BlueprintBuff)bp);
                    return buff != null && buff.GetRank() > 1;
                }),
            new BlueprintAction(">", typeof(BlueprintBuff),
                (ch, bp, n) => { ch.Descriptor.Buffs.GetFact((BlueprintUnitFact)bp).AddRank(); },
                (ch, bp) => {
                    var buff = ch.Descriptor.Buffs.GetFact((BlueprintUnitFact)bp);
                    return buff != null && buff.GetRank() < buff.Blueprint.Ranks - 1;
                }),

            // Abilities
            new BlueprintAction("Add", typeof(BlueprintAbility),
                (ch, bp, n) => { ch.AddAbility((BlueprintAbility)bp); },
                (ch, bp) => { return ch.CanAddAbility((BlueprintAbility)bp); }
                ),
            new BlueprintAction("At Will", typeof(BlueprintAbility),
                (ch, bp, n) => { ch.AddSpellAsAbility((BlueprintAbility)bp); },
                (ch, bp) => { return ch.CanAddSpellAsAbility((BlueprintAbility)bp); }
                ),
            new BlueprintAction("Remove", typeof(BlueprintAbility),
                (ch, bp, n) => { ch.RemoveAbility((BlueprintAbility)bp); },
                (ch, bp) => { return ch.HasAbility((BlueprintAbility)bp); }
                ),
        };

        public static int maxActions() { return globalActions.Count() + characterActions.Count(); }
        public static int maxCharacterActions() { return characterActions.Count(); }
    }
}