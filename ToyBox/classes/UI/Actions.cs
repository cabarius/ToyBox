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
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
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
using Kingmaker.EntitySystem.Stats;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Interaction;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Tutorial;
using Kingmaker.UI;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Customization;
using Kingmaker.Utility;
using Kingmaker.Visual.Sound;

namespace ToyBox
{
    public class Actions
    {
        public static void RunPerceptionTriggers()
        {
            foreach (BlueprintComponent bc in Game.Instance.State.LoadedAreaState.Blueprint.CollectComponents())
            {
                if (bc.name.Contains("PerceptionTrigger"))
                {
                    PerceptionTrigger pt = (PerceptionTrigger)bc;
                    pt.OnSpotted.Run();
                }
            }
        }

        public static void TeleportPartyToPlayer()
        {
            GameModeType currentMode = Game.Instance.CurrentMode;
            var partyMembers = Game.Instance.Player.m_PartyAndPets;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause)
            {
                foreach (var unit in partyMembers)
                {
                    if (unit != Game.Instance.Player.MainCharacter.Value)
                    {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;

                    }

                }
            }
        }

        public static void TeleportEveryoneToPlayer()
        {
            GameModeType currentMode = Game.Instance.CurrentMode;
            if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause)
            {
                foreach (var unit in Game.Instance.State.Units)
                {
                    if (unit != Game.Instance.Player.MainCharacter.Value)
                    {
                        unit.Commands.InterruptMove();
                        unit.Commands.InterruptMove();
                        unit.Position = Game.Instance.Player.MainCharacter.Value.Position;

                    }

                }

            }
        }

    }

    public class BlueprintAction : ToyBox.NamedAction<BlueprintScriptableObject>
    {

        public static HashSet<Type> ignoredBluePrintTypes = new HashSet<Type> {
                typeof(BlueprintAiCastSpell),
                typeof(BlueprintAnswer),
                typeof(BlueprintAnswersList),
                typeof(BlueprintAreaEnterPoint),
                typeof(BlueprintBarkBanter),
                typeof(BlueprintCue),
                typeof(BlueprintCueSequence),
                typeof(BlueprintDialog),
                typeof(BlueprintGlobalMapEdge),
                typeof(BlueprintGlobalMapPoint),
                typeof(BlueprintQuest),
                typeof(BlueprintSequenceExit),
                typeof(BlueprintTutorial),
                typeof(BlueprintTacticalCombatObstaclesMap),
                typeof(BlueprintCheck),
                typeof(BlueprintScriptZone),
                typeof(BlueprintAreaMechanics),
                typeof(BlueprintTacticalCombatAiCastSpell),
                typeof(TacticalCombatTagConsideration),
                typeof(BlueprintUnitAsksList),
                typeof(BlueprintCreditsRoles),
                typeof(CommandCooldownConsideration),
                typeof(LifeStateConsideration),
                typeof(BlueprintAiAttack),
                typeof(BlueprintAiSwitchWeapon),
                typeof(BlueprintCompanionStory),
                typeof(CanMakeFullAttackConsideration),
                typeof(DistanceConsideration),
                typeof(FactConsideration),
                typeof(NotImpatientConsideration),
                typeof(BlueprintGlobalMapPointVariation),
                typeof(BlueprintControllableProjectile),
                typeof(BlueprintInteractionRoot),
                typeof(UnitsThreateningConsideration),
                typeof(BlueprintTacticalCombatAiAttack),
                typeof(UnitsAroundConsideration),
                typeof(LastTargetConsideration),
                typeof(TargetSelfConsideration),
                typeof(BlueprintAiFollow),
                typeof(TargetClassConsideration),
                typeof(HealthConsideration),
                typeof(LineOfSightConsideration),
                typeof(RaceGenderDistribution),
                typeof(ArmorTypeConsideration),
                typeof(ComplexConsideration),
                typeof(ManualTargetConsideration),
                typeof(BlueprintDungeonLocalizedStrings),
                typeof(DistanceRangeConsideration),
                typeof(AlignmentConsideration),
                typeof(GamePadIcons),
                typeof(GamePadTexts),
                typeof(CasterClassConsideration),
                typeof(StatConsideration),
                typeof(BlueprintAiTouch),
                typeof(ActiveCommandConsideration),
                typeof(ArmyHealthConsideration),
                typeof(BuffsAroundConsideration),
                typeof(BuffConsideration),
                typeof(HealthAroundConsideration),
                typeof(HitThisRoundConsideration),
                typeof(ThreatedByConsideration),
                typeof(CanUseSpellCombatConsideration),
                typeof(CustomAiConsiderationsRoot),
                typeof(BlueprintQuestObjective),
                typeof(BlueprintScriptZone),
                typeof(BlueprintQuestGroups),
                typeof(Cutscene),
                typeof(Consideration),
                typeof(BlueprintEtude),
                typeof(BlueprintSummonPool),
                typeof(BlueprintUnit),
                typeof(BlueprintArea),
                typeof(BlueprintArmorEnchantment),
                typeof(BlueprintWeaponEnchantment),
                typeof(BlueprintEquipmentEnchantment),
                typeof(BlueprintEncyclopediaPage),
                typeof(BlueprintAreaPart),
                typeof(BlueprintLogicConnector),
                typeof(BlueprintKingdomBuff),
                typeof(BlueprintSettlementBuilding),
            };

        public static Action<BlueprintScriptableObject> addFact = bp => (Utilities.GetUnitUnderMouse() ?? GameHelper.GetPlayerCharacter()).Descriptor.AddFact((BlueprintUnitFact)bp);
        public static Action<BlueprintScriptableObject> removeFact = bp => (Utilities.GetUnitUnderMouse() ?? GameHelper.GetPlayerCharacter()).Progression.Features.RemoveFact((BlueprintUnitFact)bp);

        public static Action<BlueprintScriptableObject> addItem = bp => GameHelper.GetPlayerCharacter().Inventory.Add((BlueprintItem)bp, 1, null);

        static BlueprintAction[] itemActions = new BlueprintAction[] {
            new  BlueprintAction { name = "Add", action = addItem }
        };

        static BlueprintAction[] factActions = new BlueprintAction[] {
            new  BlueprintAction { name = "Add", action = addFact },
            new  BlueprintAction { name = "Remove", action = removeFact },
        };

        public static BlueprintAction[] ActionsForBlueprint(BlueprintScriptableObject bp)
        {
            Type type = bp.GetType();
            if (type.IsKindOf(typeof(BlueprintItem))) { return itemActions; }
            if (type.IsKindOf(typeof(BlueprintUnitFact))) { return factActions; }
            return null;
        }
    }
}