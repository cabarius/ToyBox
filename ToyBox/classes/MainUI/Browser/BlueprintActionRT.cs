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
using Kingmaker.Globalmap.Blueprints.SectorMap;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Kingmaker.Visual.CharacterSystem;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox {

    public static partial class BlueprintActions {
        public static void InitializeActionsRT() {
            // Features
            BlueprintAction.Register<BlueprintFeature>("Add".localize(),
                                                       (bp, ch, n, index) => ch.Progression.Features.Add(bp),
                                                       (bp, ch, index) => !ch.Progression.Features.Contains(bp));

            BlueprintAction.Register<BlueprintFeature>("Remove".localize(),
                                                       (bp, ch, n, index) => ch.Progression.Features.Remove(bp),
                                                       (bp, ch, index) => ch.Progression.Features.Contains(bp));
            BlueprintAction.Register<BlueprintFeature>("<".localize(),
                               (bp, ch, n, index) => ch.Progression.Features.Get(bp)?.RemoveRank(),
                               (bp, ch, index) => {
                                   var feature = ch.Progression.Features.Get(bp);
                                   return feature?.GetRank() > 1;
                               });

            BlueprintAction.Register<BlueprintFeature>(">".localize(),
                                                       (bp, ch, n, index) => ch.Progression.Features.Get(bp)?.AddRank(),
                                                       (bp, ch, index) => {
                                                           var feature = ch.Progression.Features.Get(bp);
                                                           return feature != null && feature.GetRank() < feature.Blueprint.Ranks;
                                                       });
            // Buffs
            BlueprintAction.Register<BlueprintBuff>("Add".localize(),
                                                    (bp, ch, n, index) => GameHelper.ApplyBuff(ch, bp),
                                                    (bp, ch, index) => !ch.Descriptor().Buffs.Contains(bp));

            BlueprintAction.Register<BlueprintBuff>("Remove".localize(),
                                                    (bp, ch, n, index) => ch.Descriptor().Facts.Remove(bp),
                                                    (bp, ch, index) => ch.Descriptor().Buffs.Contains(bp));
            BlueprintAction.Register<BlueprintBuff>("<".localize(),
                                                    (bp, ch, n, index) => ch.Descriptor().Buffs.Get(bp)?.RemoveRank(),
                                                    (bp, ch, index) => {
                                                        var buff = ch.Descriptor().Buffs.Get(bp);
                                                        return buff?.GetRank() > 1;
                                                    });

            BlueprintAction.Register<BlueprintBuff>(">".localize(),
                                                    (bp, ch, n, index) => ch.Descriptor().Buffs.Get(bp)?.AddRank(),
                                                    (bp, ch, index) => {
                                                        var buff = ch.Descriptor().Buffs.Get(bp);
                                                        return buff != null && buff?.GetRank() < buff.Blueprint.Ranks - 1;
                                                    });
            // Kingdom Bufs
            // Abilities
            BlueprintAction.Register<BlueprintAbility>("Add".localize(),
                                                       (bp, ch, n, index) => ch.Abilities.Add(bp),
                                                       (bp, ch, index) => !ch.Abilities.Contains(bp));

            BlueprintAction.Register<BlueprintAbility>("Remove".localize(),
                                                       (bp, ch, n, index) => ch.Abilities.Remove(bp),
                                                       (bp, ch, index) => ch.Abilities.Contains(bp));


            // BlueprintActivatableAbility
            BlueprintAction.Register<BlueprintActivatableAbility>("Add".localize(),
                                                                  (bp, ch, n, index) => ch.Descriptor().AddFact(bp),
                                                                  (bp, ch, index) => !ch.Descriptor().Facts.Contains(bp));

            BlueprintAction.Register<BlueprintActivatableAbility>("Remove".localize(),
                                                                  (bp, ch, n, index) => ch.Descriptor().Facts.Remove(bp),
                                                                  (bp, ch, index) => ch.Descriptor().Facts.Contains(bp));

            // Teleport
            BlueprintAction.Register<BlueprintStarSystemMap>("Teleport".localize(), (map, ch, n, index) => Teleport.To(map));
            BlueprintAction.Register<BlueprintSectorMapPoint>("Teleport".localize(),
                                                              (globalMapPoint, ch, n, index) => { } //Teleport.To(globalMapPoint)
                                                                                                            );
            BlueprintAction.Register<KingmakerEquipmentEntity>("Dress".localize(), (bp, ch, n, index) => {
                IEnumerable<EquipmentEntity> enumerable = bp.Load(ch.Gender, ch.ViewSettings.Doll.RacePreset.RaceId);
                ch.View.CharacterAvatar.AddEquipmentEntities(enumerable);
                foreach (var ee in bp.GetLinks(ch.Gender, ch.ViewSettings.Doll.RacePreset.RaceId)) {
                    if (!ch.View.CharacterAvatar.m_SavedEquipmentEntities.Contains(ee)) {
                        ch.View.CharacterAvatar.m_SavedEquipmentEntities.Add(ee);
                    }
                }
                if (!Main.Settings.perSave.doOverrideOutfit.TryGetValue(ch.HashKey(), out var valuePair)) {
                    valuePair = new(false, new());
                }
                valuePair.Item2.Add(bp.AssetGuid);
                Main.Settings.perSave.doOverrideOutfit[ch.HashKey()] = valuePair;
                Settings.SavePerSaveSettings();
            }, (bp, ch, index) => {
                if (ch.IsInGame && ch.View != null && ch.View.CharacterAvatar != null) {
                    IEnumerable<EquipmentEntity> enumerable = bp.Load(ch.Gender, ch.ViewSettings.Doll.RacePreset.RaceId);
                    foreach (var ee in enumerable) {
                        if (ch.View.CharacterAvatar.EquipmentEntities.Contains(ee)) {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            });
            BlueprintAction.Register<KingmakerEquipmentEntity>("Undress".localize(), (bp, ch, n, index) => {
                IEnumerable<EquipmentEntity> enumerable = bp.Load(ch.Gender, ch.ViewSettings.Doll.RacePreset.RaceId);
                ch.View.CharacterAvatar.RemoveEquipmentEntities(enumerable);
                var links = bp.GetLinks(ch.Gender, ch.ViewSettings.Doll.RacePreset.RaceId);
                ch.View.CharacterAvatar.m_SavedEquipmentEntities.RemoveAll(s => links.Contains(s));
                if (!Main.Settings.perSave.doOverrideOutfit.TryGetValue(ch.HashKey(), out var valuePair)) {
                    valuePair = new(false, new());
                }
                valuePair.Item2.Remove(bp.AssetGuid);
                Main.Settings.perSave.doOverrideOutfit[ch.HashKey()] = valuePair;
                Settings.SavePerSaveSettings();
            }, (bp, ch, index) => {
                if (ch.IsInGame && ch.View != null && ch.View.CharacterAvatar != null) {
                    IEnumerable<EquipmentEntity> enumerable = bp.Load(ch.Gender, ch.ViewSettings.Doll.RacePreset.RaceId);
                    foreach (var ee in enumerable) {
                        if (ch.View.CharacterAvatar.EquipmentEntities.Contains(ee)) {
                            return true;
                        }
                    }
                }
                return false;
            });


#if false   // TODO: implement this
            // Teleport
            BlueprintAction.Register<BlueprintAreaEnterPoint>("Teleport", (enterPoint, ch, n, index) => Teleport.To(enterPoint));
            BlueprintAction.Register<BlueprintGlobalMap>("Teleport", (map, ch, n, index) => Teleport.To(map));
            BlueprintAction.Register<BlueprintArea>("Teleport", (area, ch, n, index) => Teleport.To(area));
            BlueprintAction.Register<BlueprintGlobalMapPoint>("Teleport", (globalMapPoint, ch, n, index) => Teleport.To(globalMapPoint));

            //Army
            BlueprintAction.Register<BlueprintArmyPreset>("Add Friendly", (bp, ch, n, l) => {
                Actions.CreateArmy(bp,true);
            });
            BlueprintAction.Register<BlueprintArmyPreset>("Add Hostile", (bp, ch, n, l) => {
                Actions.CreateArmy(bp,false);
            });
#endif
        }
    }
}