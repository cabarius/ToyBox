using ModKit.Utility;
using System;
using System.Linq;
using ModKit;
using ToyBox.classes.Models;
using UnityEngine;
using static ModKit.UI;

namespace ToyBox {
    internal static class BulkSell {
        private static readonly BulkSellSettings _settings = Main.Settings.bulkSellSettings;

        public static void OnGUI() {
            void BonusItemOptions(string itemTypeName, string bonusType, ref int enchantLevel, ref int stackSize) {
                    Label($"{itemTypeName.Cyan()}:", 150.width());
                    Label($"{bonusType.orange()}", 150.width() );
                    Space(-250);
                    Slider("", ref enchantLevel, 0, 20, 0, "", 300.width());
                    Space(-270);
                    Slider("", ref stackSize, 0, 20, 1, "", 300.width());
            }

            void ConsumableOptions(string itemTypeName, ref bool sellToggle, ref int stackSize) {
                    Toggle($"Sell {itemTypeName}", ref sellToggle, AutoWidth());
                    Space(0);
                    Slider("Minimum amount (per type) to leave in inventory", ref stackSize, 0, 200, 1, "", AutoWidth());
            }

            void DamageTypeOptions<T>(string title, SerializableDictionary<T, bool> settings) where T : Enum {
                using (HorizontalScope()) {
                    Label(title.orange(), 220.width());
                    using (VerticalScope()) {
                        settings.Keys
                                .Select((type, index) => new { type, index })
                                .GroupBy(g => g.index / 5)
                                .ToList()
                                .ForEach(group => {
                                    using (HorizontalScope()) {
                                        group.ToList().ForEach(type => {
                                            ActionToggle(type.type.ToString(), () => settings[type.type], b => settings[type.type] = b, 150);
                                            Space(10);
                                        });
                                    }
                                });
                    }
                }
            }

            using (VerticalScope()) {
                // create GUI sections
                using (HorizontalScope()) {
                    Label("Category".Cyan(),150.width());
                    Label("Type".Cyan(), 150.width());
                    80.space();
                    Label("Max Modifier".Cyan(), 300.width());
                    165.space();
                    Label("Amount to Keep Around".Cyan(), 300.width());
                }

                using (HorizontalScope()) {
                    BonusItemOptions("Armors", "enchantment", ref _settings.armorEnchantLevel, ref _settings.armorStackSize);
                    25.space();
                    Toggle("Sell unique armors", ref _settings.sellUniqueArmors, AutoWidth());
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Shields", "enchantment", ref _settings.shieldEnchantLevel, ref _settings.shieldStackSize);
                    25.space();
                    Toggle("Sell unique shields", ref _settings.sellUniqueShields, AutoWidth());
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Belts", "attribute", ref _settings.maxAttributeBonusForBelt, ref _settings.beltStackSize);
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Head items", "attribute", ref _settings.maxAttributeBonusForHead, ref _settings.headStackSize);
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Cloaks", "save", ref _settings.maxSaveBonusForCloaks, ref _settings.cloakStackSize);
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Bracers", "AC", ref _settings.maxACBonusForBracers, ref _settings.bracerStackSize);
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Amulets", "AC", ref _settings.maxACBonusForNeck, ref _settings.neckStackSize);
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Rings", "AC", ref _settings.maxACBonusForRings, ref _settings.ringStackSize);
                }
                using (HorizontalScope()) {
                    BonusItemOptions("Weapons", "enchantment", ref _settings.weaponEnchantLevel, ref _settings.weaponStackSize);
                    25.space();
                    Toggle("Sell unique weapons", ref _settings.sellUniqueWeapons, AutoWidth());
                }
                using (HorizontalScope()) {
                    150.space();
                    DisclosureToggle("Damage Types".Cyan(), ref _settings.showWeaponEnergyTypes, 240f);
                    if (_settings.showWeaponEnergyTypes) {
                        using (VerticalScope()) {
                            DamageTypeOptions("Elemental:", _settings.damageEnergy);
                            DamageTypeOptions("Alignment:", _settings.damageAlignment);
                            DamageTypeOptions("Materials:", _settings.damageMaterial);
                            DamageTypeOptions("Other:", _settings.damageReality);
                        }
                    }
                }
                Label("Consumables:", AutoWidth());
                Space(10);
                ConsumableOptions("Potions", ref _settings.sellPotions, ref _settings.potionStackSize);
                Space(10);
                ConsumableOptions("Scrolls", ref _settings.sellScrolls, ref _settings.scrollStackSize);
                Space(10);
                ConsumableOptions("Ingredients", ref _settings.sellIngredients, ref _settings.ingredientStackSize);
                Space(10);

                Div(0, 25);
                Slider("Change all enhancement modifiers", ref _settings.globalModifier, 0, 10, 0, "", AutoWidth());
                ActionButton("Apply", () => {
                    _settings.maxAttributeBonusForBelt = _settings.globalModifier;
                    _settings.maxAttributeBonusForHead = _settings.globalModifier;
                    _settings.maxSaveBonusForCloaks = _settings.globalModifier;
                    _settings.maxACBonusForRings = _settings.globalModifier;
                    _settings.maxACBonusForBracers = _settings.globalModifier;
                    _settings.maxACBonusForNeck = _settings.globalModifier;
                    _settings.weaponEnchantLevel = _settings.globalModifier;
                    _settings.armorEnchantLevel = _settings.globalModifier;
                    _settings.shieldEnchantLevel = _settings.globalModifier;
                }, AutoWidth());
            }
        }
    }
}
