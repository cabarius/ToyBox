// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using ModKit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ToyBox.BagOfPatches {
    public static class DiceRollsRT {
        //TODO: For each Dice Type; for Skillchecks, incombat, outofcombat etc.
        public class RollSaveEntry {
            public Roll roll;
            public RollSetup setup;
            internal RollSaveEntry() { }
            public RollSaveEntry(RollSetup setup) {
                this.setup = setup;
                roll = setup.CreateRoll();
            }
            public void setSetup(RollSetup setup) {
                this.setup = setup;
                roll = setup.CreateRoll();
            }
            public static RollSaveEntry example() {
                var tmpSetup = new RollSetup(DiceType.D100);
                var tmpRule1 = new RollRule(RollNum.Roll50, RuleType.RollAtMost, DiceType.D100);
                var tmpRule2 = new RollRule(RollNum.Roll1, RuleType.RollNever, DiceType.D100);
                tmpSetup.AddRule(tmpRule1);
                tmpSetup.AddRule(tmpRule2);
                return new RollSaveEntry(tmpSetup);
            }
        }
        public class Roll {
            public List<int> possible;
            public bool reroll;
            public bool takeBetter;
            public bool takeWorse;
            internal Roll() { }
            public Roll(List<int> possible, bool reroll, bool takeBetter, bool takeWorse) {
                this.possible = possible;
                this.reroll = reroll;
                this.takeBetter = takeBetter;
                this.takeWorse = takeWorse;
            }

            public int doRoll() {
                int result = UnityEngine.Random.Range(1, possible.Count);
                if (reroll) {
                    int tmp = UnityEngine.Random.Range(1, possible.Count);
                    if (takeBetter) {
                        result = Math.Max(result, tmp);
                    } // Should be useless but who knows
                    else if (takeWorse) {
                        result = Math.Min(result, tmp);
                    }
                }
                return possible.Get(result - 1);
            }
        }
        public class RollRule {
            public RollNum targetRoll;
            public RuleType ruletype;
            public DiceType dice;
            public int customRollValue;
            internal RollRule() { }
            public RollRule(RollNum targetRoll, RuleType ruletype, DiceType dice, int customRollValue = 0) {
                this.targetRoll = targetRoll;
                this.ruletype = ruletype;
                this.dice = dice;
                if (0 < customRollValue && customRollValue <= (int)dice) {
                    this.customRollValue = customRollValue;
                }
            }
            public bool IsIncompatibleWith(RollRule toCompare) {
                var toCompareType = toCompare.ruletype;
                if (toCompareType != RuleType.RollNever) {
                    if (ruletype == toCompareType) {
                        return true;
                    }
                }
                switch (ruletype) {
                    case RuleType.RollAdvantage:
                        return (toCompareType == RuleType.RollDisadvantage || toCompareType == RuleType.RollAlways);
                    case RuleType.RollDisadvantage:
                        return (toCompareType == RuleType.RollAdvantage || toCompareType == RuleType.RollAlways);
                    case RuleType.RollAlways:
                        return true;
                    case RuleType.RollNever:
                        return (toCompareType == RuleType.RollAlways);
                    case RuleType.RollAtMost:
                        return (toCompareType == RuleType.RollAlways);
                    case RuleType.RollAtLeast:
                        return (toCompareType == RuleType.RollAlways);
                    default:
                        return false;
                }
            }
        }
        public class RollSetup {
            public List<RollRule> activeRules;
            public DiceType dice;
            public RollSetup(DiceType dice) {
                activeRules = new();
                this.dice = dice;
            }
            public bool AddRule(RollRule newRule) {
                if (newRule.dice != dice) return false;
                if (activeRules.Any(rule => rule.IsIncompatibleWith(newRule))) return false;
                var backup = new List<RollRule>(activeRules);
                activeRules.Add(newRule);
                bool canRoll = CreateRoll().possible.Count > 0;
                if (!canRoll) {
                    activeRules = backup;
                }
                return canRoll;
            }
            public Roll CreateRoll() {
                bool reroll = false;
                bool takeBetter = false;
                bool takeWorse = false;
                int rollResult = -1;
                int rollNum;
                List<int> exclusions = new();
                foreach (var rule in activeRules) {
                    switch (rule.targetRoll) {
                        case RollNum.None: rollNum = 0; break;
                        case RollNum.Custom: rollNum = rule.customRollValue; break;
                        default: rollNum = (int)rule.targetRoll; break;
                    }
                    switch (rule.ruletype) {
                        case RuleType.RollAdvantage: reroll = true; takeBetter = true; break;
                        case RuleType.RollDisadvantage: reroll = true; takeWorse = true; break;
                        case RuleType.RollAlways: rollResult = rollNum; break;
                        case RuleType.RollNever: exclusions.Add(rollNum); break;
                        case RuleType.RollAtLeast: {
                                for (var i = 1; i < rollNum; i++) {
                                    exclusions.Add(i);
                                }
                            }; break;
                        case RuleType.RollAtMost: {
                                for (var i = 1 + rollNum; i <= (int)dice; i++) {
                                    exclusions.Add(i);
                                }
                            }; break;
                    }
                }
                List<int> valid = new();
                for (int i = 1; i <= (int)dice; i++) {
                    if (!exclusions.Contains(i)) {
                        valid.Add(i);
                    }
                }
                if (rollResult != -1) {
                    valid = new() { rollResult };
                }
                return new Roll(valid, reroll, takeBetter, takeWorse);
            }
        }
        public enum RollNum {
            None = -1,
            Custom = 0,
            Roll1 = 1,
            Roll25 = 25,
            Roll50 = 50,
            Roll100 = 100,
        }
        public enum RuleType {
            [Description("Role at least")]
            RollAtLeast,
            [Description("Role at most")]
            RollAtMost,
            [Description("Always role")]
            RollAlways,
            [Description("Never role")]
            RollNever,
            [Description("Roll with Advantage")]
            RollAdvantage,
            [Description("Roll with Disadvantage")]
            RollDisadvantage
        }

        private static bool changePolicy = true;
        public static Settings settings = Main.Settings;
        public static Player player = Game.Instance.Player;
        [HarmonyPatch(typeof(RulePerformAttackRoll))]
        private static class RulePerformAttackRollPatch {
            private static bool forceHit;
            private static bool forceCrit;
            [HarmonyPatch(nameof(RulePerformAttackRoll.OnTrigger))]
            [HarmonyPrefix]
            private static void OnTriggerPrefix(RulebookEventContext context) {
                if (context.Current.Initiator is BaseUnitEntity unit) {
                    forceCrit = UnitEntityDataUtils.CheckUnitEntityData(unit, settings.allHitsCritical);
                    forceHit = UnitEntityDataUtils.CheckUnitEntityData(unit, settings.allAttacksHit);
                    if (forceCrit || forceHit) {
                        changePolicy = true;
                    }
                }
            }
            [HarmonyPatch(nameof(RulePerformAttackRoll.OnTrigger))]
            [HarmonyPostfix]
            private static void OnTriggerPostfix(ref RulePerformAttackRoll __instance) {
                if (forceCrit) {
                    __instance.ResultIsRighteousFury = true;
                    __instance.Result = AttackResult.RighteousFury;
                }
            }
        }
        [HarmonyPatch(typeof(AttackHitPolicyContextData))]
        private static class AttackHitPolicyPatch {
            [HarmonyPatch(nameof(AttackHitPolicyContextData.Current), MethodType.Getter)]
            [HarmonyPostfix]
            private static void GetCurrent(ref AttackHitPolicyType __result) {
                if (changePolicy) {
                    __result = AttackHitPolicyType.AutoHit;
                    changePolicy = false;
                }
            }
        }
    }
}
