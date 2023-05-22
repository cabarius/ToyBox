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
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Globalmap.Blueprints.SectorMap;

namespace ToyBox {

    public static partial class BlueprintActions {
        public static void InitializeActionsRT() {
            // Features
            BlueprintAction.Register<BlueprintFeature>("Add",
                                                       (bp, ch, n, index) => ch.Progression.Features.Add(bp),
                                                       (bp, ch, index) => !ch.Progression.Features.Contains(bp));

            BlueprintAction.Register<BlueprintFeature>("Remove",
                                                       (bp, ch, n, index) => ch.Progression.Features.Remove(bp),
                                                       (bp, ch, index) => ch.Progression.Features.Contains(bp));

            // Buffs
            BlueprintAction.Register<BlueprintBuff>("Add",
                                                    (bp, ch, n, index) => GameHelper.ApplyBuff(ch, bp),
                                                    (bp, ch, index) => !ch.Descriptor().Buffs.Contains(bp));

            BlueprintAction.Register<BlueprintBuff>("Remove",
                                                    (bp, ch, n, index) => ch.Descriptor().Facts.Remove(bp),
                                                    (bp, ch, index) => ch.Descriptor().Buffs.Contains(bp));
            // Abilities
            BlueprintAction.Register<BlueprintAbility>("Add",
                                                       (bp, ch, n, index) => ch.Abilities.Add(bp),
                                                       (bp, ch, index) => !ch.Abilities.Contains(bp));

            BlueprintAction.Register<BlueprintAbility>("Remove",
                                                       (bp, ch, n, index) => ch.Abilities.Remove(bp),
                                                       (bp, ch, index) => ch.Abilities.Contains(bp));


            // BlueprintActivatableAbility
            BlueprintAction.Register<BlueprintActivatableAbility>("Add",
                                                                  (bp, ch, n, index) => ch.Descriptor().AddFact(bp),
                                                                  (bp, ch, index) => !ch.Descriptor().Facts.Contains(bp));

            BlueprintAction.Register<BlueprintActivatableAbility>("Remove",
                                                                  (bp, ch, n, index) => ch.Descriptor().Facts.Remove(bp),
                                                                  (bp, ch, index) => ch.Descriptor().Facts.Contains(bp));

            // Teleport
            BlueprintAction.Register<BlueprintStarSystemMap>("Teleport", (map, ch, n, index) => Teleport.To(map));
            BlueprintAction.Register<BlueprintSectorMapPoint>("Teleport",
                                                              (globalMapPoint, ch, n, index) => { } //Teleport.To(globalMapPoint)
                                                                                                            );


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