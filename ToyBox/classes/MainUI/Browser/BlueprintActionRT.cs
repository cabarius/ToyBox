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
using Kingmaker.Cheats;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.ContextData;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.Blueprints.Colonization;
using Kingmaker.Globalmap.Blueprints.SectorMap;
using Kingmaker.Globalmap.Blueprints.SystemMap;
using Kingmaker.Globalmap.SectorMap;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Progression.Features.Advancements;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.Visual.CharacterSystem;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ToyBox {

    public static partial class BlueprintActions {
        public static void InitializeActionsRT() {
            // Features
            BlueprintAction.Register<BlueprintFeature>("Add".localize(),
                                                       (bp, ch, n, index) => {
                                                           ch.Progression.Features.Add(bp);
                                                           OwlLogging.Log($"Add Feature {bp} to {ch}");
                                                       },
                                                       (bp, ch, index) => !ch.Progression.Features.Contains(bp));

            BlueprintAction.Register<BlueprintFeature>("Remove".localize(),
                                                       (bp, ch, n, index) => { 
                                                           ch.Progression.Features.Remove(bp);
                                                           OwlLogging.Log($"Remove Feature {bp} from {ch}");
                                                       },
                                                       (bp, ch, index) => ch.Progression.Features.Contains(bp));
            BlueprintAction.Register<BlueprintFeature>("<".localize(),
                               (bp, ch, n, index) => {
                                   ch.Progression.Features.Get(bp)?.RemoveRank();
                                   OwlLogging.Log($"Remove rank from Feature {bp} for {ch}");
                               },
                               (bp, ch, index) => {
                                   var feature = ch.Progression.Features.Get(bp);
                                   return feature?.GetRank() > 1;
                               });

            BlueprintAction.Register<BlueprintFeature>(">".localize(),
                                                       (bp, ch, n, index) => {
                                                           ch.Progression.Features.Get(bp)?.AddRank();
                                                           OwlLogging.Log($"Add rank to Feature {bp} for {ch}");
                                                       },
                                                       (bp, ch, index) => {
                                                           var feature = ch.Progression.Features.Get(bp);
                                                           if (bp is BlueprintStatAdvancement) {
                                                               return feature != null;
                                                           }
                                                           return feature != null && feature.GetRank() < feature.Blueprint.Ranks;
                                                       });
            // Buffs
            BlueprintAction.Register<BlueprintBuff>("Add".localize(),
                                                    (bp, ch, n, index) => {
                                                        GameHelper.ApplyBuff(ch, bp);
                                                        OwlLogging.Log($"Add Buff {bp} to {ch}");
                                                    },
                                                    (bp, ch, index) => !ch.Descriptor().Buffs.Contains(bp));

            BlueprintAction.Register<BlueprintBuff>("Remove".localize(),
                                                    (bp, ch, n, index) => {
                                                        ch.Descriptor().Facts.Remove(bp);
                                                        OwlLogging.Log($"Remove Buff {bp} to {ch}");
                                                    },
                                                    (bp, ch, index) => ch.Descriptor().Buffs.Contains(bp));
            BlueprintAction.Register<BlueprintBuff>("<".localize(),
                                                    (bp, ch, n, index) => {
                                                        ch.Descriptor().Buffs.Get(bp)?.RemoveRank();
                                                        OwlLogging.Log($"Remove rank from Buff {bp} for {ch}");
                                                    },
                                                    (bp, ch, index) => {
                                                        var buff = ch.Descriptor().Buffs.Get(bp);
                                                        return buff?.GetRank() > 1;
                                                    });

            BlueprintAction.Register<BlueprintBuff>(">".localize(),
                                                    (bp, ch, n, index) => {
                                                        ch.Descriptor().Buffs.Get(bp)?.AddRank();
                                                        OwlLogging.Log($"Add rank to Buff {bp} for {ch}");
                                                    },
                                                    (bp, ch, index) => {
                                                        var buff = ch.Descriptor().Buffs.Get(bp);
                                                        return buff != null && buff?.GetRank() < buff.Blueprint.Ranks - 1;
                                                    });
            // Kingdom Bufs
            // Abilities
            BlueprintAction.Register<BlueprintAbility>("Add".localize(),
                                                       (bp, ch, n, index) => {
                                                           ch.Abilities.Add(bp);
                                                           OwlLogging.Log($"Add Ability {bp} to {ch}");
                                                       },
                                                       (bp, ch, index) => !ch.Abilities.Contains(bp));

            BlueprintAction.Register<BlueprintAbility>("Remove".localize(),
                                                       (bp, ch, n, index) => {
                                                           ch.Abilities.Remove(bp);
                                                           OwlLogging.Log($"Remove Ability {bp} to {ch}");
                                                       },
                                                       (bp, ch, index) => ch.Abilities.Contains(bp));


            // BlueprintActivatableAbility
            BlueprintAction.Register<BlueprintActivatableAbility>("Add".localize(),
                                                                  (bp, ch, n, index) => {
                                                                      ch.Descriptor().AddFact(bp);
                                                                      OwlLogging.Log($"Add ActivatableAbility {bp} to {ch}");
                                                                  },
                                                                  (bp, ch, index) => !ch.Descriptor().Facts.Contains(bp));

            BlueprintAction.Register<BlueprintActivatableAbility>("Remove".localize(),
                                                                  (bp, ch, n, index) => {
                                                                      ch.Descriptor().Facts.Remove(bp);
                                                                      OwlLogging.Log($"Remove ActivatableAbility {bp} to {ch}");
                                                                  },
                                                                  (bp, ch, index) => ch.Descriptor().Facts.Contains(bp));

            // Teleport
            BlueprintAction.Register<BlueprintStarSystemMap>("Teleport".localize(), (map, ch, n, index) => {
                Teleport.To(map);
                OwlLogging.Log($"Teleport to {map}");
            });
            BlueprintAction.Register<BlueprintSectorMapPoint>("Teleport".localize(),
                                                              (globalMapPoint, ch, n, index) => { }); //Teleport.To(globalMapPoint)
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
            BlueprintAction.Register<BlueprintPlanet>("Colonize".localize(), (bp, ch, n, index) => {
                try {
                    CheatsColonization.ColonizePlanet(bp);
                    OwlLogging.Log($"Colonize Planet {bp}");
                } catch (Exception ex) {
                    throw new Exception("Error trying to colonize Planet. Are you in the correct Star System?\n".localize().orange().bold() + ex.Message + ex.StackTrace.ToString());
                }
            }, (bp, ch, index) => {
                var system = bp.ConnectedAreas.FirstOrDefault(f => f is BlueprintStarSystemMap) as BlueprintStarSystemMap;
                return bp.GetComponent<ColonyComponent>() != null && Game.Instance.CurrentlyLoadedArea is BlueprintStarSystemMap && (system == null || Game.Instance.Player.CurrentStarSystem == system);

            });
            BlueprintAction.Register<BlueprintColony>("Colonize".localize(), (bp, ch, n, index) => {

                try {
                    CheatsColonization.ColonizePlanet(ColonyToPlanet[bp]);
                    OwlLogging.Log($"Colonize Colony {bp}");
                } catch (Exception ex) {
                    throw new Exception("Error trying to colonize Planet. Are you in the correct Star System?\n".localize().orange().bold() + ex.Message + ex.StackTrace.ToString());
                }
            }, (bp, ch, index) => {
                if (ColonyToPlanet == null) {
                    ColonyToPlanet = new();
                    foreach (var planet in BlueprintLoader.Shared.GetBlueprints<BlueprintPlanet>()) {
                        var colonyComponent = planet.GetComponent<ColonyComponent>();
                        if (colonyComponent != null) {
                            if (colonyComponent.ColonyBlueprint != null) {
                                ColonyToPlanet[colonyComponent.ColonyBlueprint] = planet;
                            }
                        }
                    }
                }
                var system = ColonyToPlanet[bp].ConnectedAreas.FirstOrDefault(f => f is BlueprintStarSystemMap) as BlueprintStarSystemMap;
                return ColonyToPlanet[bp] != null && Game.Instance.CurrentlyLoadedArea is BlueprintStarSystemMap && (system == null || Game.Instance.Player.CurrentStarSystem == system);
            });
        }
        private static Dictionary<BlueprintColony, BlueprintPlanet> ColonyToPlanet = null;
    }
}