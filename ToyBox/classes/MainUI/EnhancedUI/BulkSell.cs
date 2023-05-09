using ModKit.Utility;
using System;
using System.Linq;
using ToyBox.classes.Models;
using UnityEngine;
using static ModKit.UI;

namespace ToyBox
{
    internal static class BulkSell
    {
        private static readonly BulkSellSettings _settings = Main.Settings.bulkSellSettings;

        public static void OnGUI()
        {
            void bonusItemOptions(string itemTypeName, string bonusType, ref int enchantLevel, ref int stackSize)
            {
                using (VerticalScope())
                {
                    Label($"{itemTypeName}:");
                    Slider($"Highest {bonusType} bonus for bulk selling", ref enchantLevel, 0, 20, 0, "", AutoWidth());
                    Slider("Minimum amount (per type) to leave in inventory", ref stackSize, 0, 20, 1, "", AutoWidth());
                }
            }

            void consumableOptions(string itemTypeName, ref bool sellToggle, ref int stackSize)
            {
                using (VerticalScope())
                {
                    Toggle($"Sell {itemTypeName}", ref sellToggle, AutoWidth());
                    Slider("Minimum amount (per type) to leave in inventory", ref stackSize, 0, 200, 1, "", AutoWidth());
                }
            }

            void damageTypeOptions<T>(SerializableDictionary<T, bool> settings) where T : Enum
            {
                settings.Keys
                    .Select((type, index) => new { type, index })
                    .GroupBy(g => g.index / 4)
                    .ToList()
                    .ForEach(group =>
                    {
                        using (HorizontalScope())
                        {
                            group.ToList().ForEach(type =>
                            {
                                ActionToggle(type.type.ToString(), () => settings[type.type], b => settings[type.type] = b, 150);
                                Space(10);
                            });
                        }
                    });
                Space(20);
            }

            using (VerticalScope())
            {
                // create GUI sections
                bonusItemOptions("Weapons", "enchantment", ref _settings.weaponEnchantLevel, ref _settings.weaponStackSize);
                Toggle("Sell unique weapons", ref _settings.sellUniqueWeapons, AutoWidth());
                DisclosureToggle("Show additional damage types to sell", ref _settings.showWeaponEnergyTypes);

                if (_settings.showWeaponEnergyTypes)
                {
                    Div(0, 25);
                    using (VerticalScope())
                    {
                        Label("Elemental Damage:");
                        damageTypeOptions(_settings.damageEnergy);
                        Label("Reality Damage:");
                        damageTypeOptions(_settings.damageReality);
                        Label("Alignment Damage:");
                        damageTypeOptions(_settings.damageAlignment);
                        Label("Materials:");
                        damageTypeOptions(_settings.damageMaterial);
                    }
                }
                Div(0, 25);
                
                bonusItemOptions("Armors", "enchantment", ref _settings.armorEnchantLevel, ref _settings.armorStackSize);
                Toggle("Sell unique armors", ref _settings.sellUniqueArmors, AutoWidth());
                Div(0, 25);
                bonusItemOptions("Shields", "enchantment", ref _settings.shieldEnchantLevel, ref _settings.shieldStackSize);
                Toggle("Sell unique shields", ref _settings.sellUniqueShields, AutoWidth());
                Div(0, 25);
                bonusItemOptions("Belts", "attribute", ref _settings.maxAttributeBonusForBelt, ref _settings.beltStackSize);
                Div(0, 25);
                bonusItemOptions("Head items", "attribute", ref _settings.maxAttributeBonusForHead, ref _settings.headStackSize);
                Div(0, 25);
                bonusItemOptions("Cloaks", "save", ref _settings.maxSaveBonusForCloaks, ref _settings.cloakStackSize);
                Div(0, 25);
                bonusItemOptions("Bracers", "AC", ref _settings.maxACBonusForBracers, ref _settings.bracerStackSize);
                Div(0, 25);
                bonusItemOptions("Amulets", "AC", ref _settings.maxACBonusForNeck, ref _settings.neckStackSize);
                Div(0, 25);
                bonusItemOptions("Rings", "AC", ref _settings.maxACBonusForRings, ref _settings.ringStackSize);
                Div(0, 25);

                Label("Consumables:", AutoWidth());
                Space(10);
                consumableOptions("Potions", ref _settings.sellPotions, ref _settings.potionStackSize);
                Space(10);
                consumableOptions("Scrolls", ref _settings.sellScrolls, ref _settings.scrollStackSize);
                Space(10);
                consumableOptions("Ingredients", ref _settings.sellIngredients, ref _settings.ingredientStackSize);
                Space(10);

                Div(0, 25);
                Slider("Change all enhancement modifiers", ref _settings.globalModifier, 0, 10, 0, "", AutoWidth());
                ActionButton("Apply", () =>
                {
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
