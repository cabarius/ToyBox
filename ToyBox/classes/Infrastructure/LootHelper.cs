﻿using Kingmaker;
using Kingmaker.Blueprints.Loot;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using Kingmaker.View.MapObjects;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ToyBox {
    public static class LootHelper {

        public static bool IsLootable(this ItemEntity item, RarityType filter = RarityType.None) {
            var rarity = item.Blueprint.Rarity();
            if ((int)rarity < (int)filter) return false;
            return item.IsLootable;
        }
        public static List<ItemEntity> Lootable(this List<ItemEntity> loots, RarityType filter = RarityType.None) {
            return loots.Where(l => l.IsLootable(filter)).ToList();
        }
        public static string GetName(this LootWrapper present) {
            if (present.InteractionLoot != null) {
//                var name = present.InteractionLoot.Owner.View.name;
                var name = present.InteractionLoot.Source.name;
                if (name == null || name.Length == 0) name = "Ground";
                return name;
            }
            if (present.Unit != null) return present.Unit.CharacterName;
            return null;
        }

        public static List<ItemEntity> GetInteraction(this LootWrapper present) {
            if (present.InteractionLoot != null) return present.InteractionLoot.Loot.Items; ;
            if (present.Unit != null) return present.Unit.Inventory.Items;
            return null;
        }
        public static List<ItemEntity> GetLewtz(this LootWrapper present) {
            if (present.InteractionLoot != null) return present.InteractionLoot.Loot.Items; ;
            if (present.Unit != null) return present.Unit.Inventory.Items;
            return null;
        }
        public static IEnumerable<LootWrapper> GetMassLootFromCurrentArea() {
            List<LootWrapper> lootWrapperList = new List<LootWrapper>();
            var units = Game.Instance.State.Units.All
                .Where<UnitEntityData>((Func<UnitEntityData, bool>)(u => u.IsInGame && !u.Descriptor.IsPartyOrPet()));
                //.Where<UnitEntityData>((Func<UnitEntityData, bool>)(u => u.IsRevealed && u.IsDeadAndHasLoot));
            foreach (var unitEntityData in units)
                lootWrapperList.Add(new LootWrapper() {
                    Unit = unitEntityData
                });
            var interactionLootParts = Game.Instance.State.Entities.All
                .Where<EntityDataBase>(e => e.IsInGame)
                .Select<EntityDataBase, InteractionLootPart>(i => i.Get<InteractionLootPart>())
                .Where<InteractionLootPart>(i => i?.Loot != Game.Instance.Player.SharedStash)
                .NotNull<InteractionLootPart>();
            var source = TempList.Get<InteractionLootPart>();
            foreach (var interactionLootPart in interactionLootParts) {
                if (// interactionLootPart.Owner.IsRevealed && 
                    interactionLootPart.Loot.HasLoot 
                    //&& (
                    //    interactionLootPart.LootViewed || interactionLootPart.View is DroppedLoot && !(bool)(EntityPart)interactionLootPart.Owner.Get<DroppedLoot.EntityPartBreathOfMoney>() || (bool)(UnityEngine.Object)interactionLootPart.View.GetComponent<SkinnedMeshRenderer>()
                    //    )
                    )
                    source.Add(interactionLootPart);
            }
            IEnumerable<LootWrapper> collection = source.Distinct<InteractionLootPart>((IEqualityComparer<InteractionLootPart>)new MassLootHelper.LootDuplicateCheck()).Select<InteractionLootPart, LootWrapper>((Func<InteractionLootPart, LootWrapper>)(i => new LootWrapper() {
                InteractionLoot = i
            }));
            lootWrapperList.AddRange(collection);
            return (IEnumerable<LootWrapper>)lootWrapperList;
        }
    }
}