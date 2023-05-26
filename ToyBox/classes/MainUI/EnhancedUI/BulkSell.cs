using ModKit;
using ModKit.Utility;
using System;
using System.Linq;
using UnityEngine;
using static ModKit.UI;

namespace ToyBox {
    internal static class BulkSell {
        private static readonly BulkSellSettings _settings = Main.Settings.bulkSellSettings;

        public static void OnGUI() {
            void BonusItemOptions(string itemTypeName, string bonusType, ref int enchantLevel, ref int stackSize, Action accessory = null) {
                using (HorizontalScope()) {
                    Label($"{itemTypeName.Cyan()}:", 180.width());
                    Label($"{bonusType.orange()}", 150.width());
                    Slider(ref enchantLevel, 0, 20, 0, "", 300.width());
                    Slider(ref stackSize, 1, 20, 1, "", 300.width());
                    accessory?.Invoke();
                }
            }

            void ConsumableOptions(string itemTypeName, ref bool sellToggle, ref int stackSize) {
                using (HorizontalScope()) {
                    Toggle("Sell".localize() + $" {itemTypeName}", ref sellToggle, 150.width());
                    Space(25);
                    Label("Amount To Keep".localize().Cyan(), 150.width());
                    Slider(ref stackSize, 1, 200, 1, "", AutoWidth());
                }
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
                                            ActionToggle(type.type.ToString().localize(), () => settings[type.type], b => settings[type.type] = b, 150);
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
                    TitleLabel("Category".localize().Cyan(), 178.width());
                    TitleLabel("Type".localize().Cyan(), 150.width());
                    5.space();
                    TitleLabel("Max Modifier".localize().Cyan(), 300.width());
                    190.space();
                    TitleLabel("Amount To Keep".localize().Cyan(), 300.width());
                }
                BonusItemOptions("Armors".localize(),
                                 "enchantment".localize(),
                                 ref _settings.armorEnchantLevel,
                                 ref _settings.armorStackSize,
                                 () => Toggle("Sell unique armors".localize(), ref _settings.sellUniqueArmors)
                                 );
                BonusItemOptions("Shields".localize(),
                                 "enchantment".localize(),
                                 ref
                                 _settings.shieldEnchantLevel,
                                 ref _settings.shieldStackSize,
                                 () => Toggle("Sell unique shields".localize(), ref _settings.sellUniqueShields));
                BonusItemOptions("Belts".localize(), "attribute".localize(), ref _settings.maxAttributeBonusForBelt, ref _settings.beltStackSize);
                BonusItemOptions("Head items".localize(), "attribute".localize(), ref _settings.maxAttributeBonusForHead, ref _settings.headStackSize);
                BonusItemOptions("Cloaks".localize(), "save".localize(), ref _settings.maxSaveBonusForCloaks, ref _settings.cloakStackSize);
                BonusItemOptions("Bracers".localize(), "AC".localize(), ref _settings.maxACBonusForBracers, ref _settings.bracerStackSize);
                BonusItemOptions("Amulets".localize(), "AC".localize(), ref _settings.maxACBonusForNeck, ref _settings.neckStackSize);
                BonusItemOptions("Rings".localize(), "AC".localize(), ref _settings.maxACBonusForRings, ref _settings.ringStackSize);
                BonusItemOptions("Weapons".localize(),
                                 "enchantment".localize(),
                                 ref _settings.weaponEnchantLevel,
                                 ref _settings.weaponStackSize,
                                 () => Toggle("Sell unique weapons".localize(), ref _settings.sellUniqueWeapons)
                                 );
                using (HorizontalScope()) {
                    180.space();
                    DisclosureToggle("Damage Types".localize().Cyan(), ref _settings.showWeaponEnergyTypes, 240f);
                    if (_settings.showWeaponEnergyTypes) {
                        using (VerticalScope()) {
                            DamageTypeOptions("Elemental:".localize(), _settings.damageEnergy);
                            DamageTypeOptions("Alignment:".localize(), _settings.damageAlignment);
                            DamageTypeOptions("Materials:".localize(), _settings.damageMaterial);
                            DamageTypeOptions("Other:".localize(), _settings.damageReality);
                        }
                    }
                }
                Label("Consumables".localize(), AutoWidth());
                DivLast();
                ConsumableOptions("Potions".localize(), ref _settings.sellPotions, ref _settings.potionStackSize);
                ConsumableOptions("Scrolls".localize(), ref _settings.sellScrolls, ref _settings.scrollStackSize);
                ConsumableOptions("Ingredients".localize(), ref _settings.sellIngredients, ref _settings.ingredientStackSize);

                Div(0, 25);
                Slider("Change all enhancement modifiers".localize(), ref _settings.globalModifier, 0, 10, 0, "", AutoWidth());
                ActionButton("Apply".localize(), () => {
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
