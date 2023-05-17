// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT Licenseusing Kingmaker;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Cheats;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.ElementsSystem;
using ModKit;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Items;
using Kingmaker.Controllers.Combat;
using Utilities = Kingmaker.Cheats.Utilities;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints;
using ModKit.Utility;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Enums;
using Kingmaker.UI.Common;
using Kingmaker.Designers.Mechanics.EquipmentEnchants;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.Mechanics.Facts;
using Owlcat.Runtime.Core.Utils;
using static Kingmaker.EntitySystem.Stats.ModifiableValue;
#if Wrath
using ToyBox.Inventory;
using Kingmaker.Blueprints.Classes.Selection;
using static Kingmaker.EntitySystem.EntityDataBase;
#endif
#if Wrath
namespace ToyBox {
    public static class ItemEntityUtils {
        public static bool HasModifierConflicts(this UnitEntityData unit, ItemEntity item) {
            var itemModifiers = item.GetModifierDescriptors();
            return unit.Stats.AllStats.SelectMany(stat => stat.Modifiers)
                       .Any(m => !m.Stacks 
#if Wrath
                                 && m.ItemSource != (Loot.selectedSlot?.Item ?? null)
#elif RT
                                 && m.SourceItem != (Loot.selectedSlot?.Item ?? null)
#endif
                                 && itemModifiers.Contains(m.ModDescriptor));
        }
        public static HashSet<ModifierDescriptor> GetNonStackingModifiers(this UnitEntityData unit) {
            return unit.Stats.AllStats.SelectMany(stat => stat.Modifiers).Where(m => !m.Stacks).Select(m => m.ModDescriptor).ToHashSet();
        }
        public static HashSet<ModifierDescriptor> GetModifierDescriptors(this ItemEntity item) {
            return item.Enchantments.SelectMany(e => e.Blueprint.GetComponents<AddStatBonusEquipment>()).Select(c => c.Descriptor).ToHashSet();
        }
        public static bool HasModifierConflicts(this ItemEntity item, HashSet<ModifierDescriptor> modifiers) {
            var components = item.Enchantments.SelectMany(e => e.Blueprint.GetComponents<AddStatBonusEquipment>());
            Mod.Log($"components: {string.Join(", ", components.Select(c => c.Descriptor.ToString()))}");

            if (components.Any(c => modifiers.Contains(c.Descriptor))) return true;
            switch (item) {
                case ItemEntityWeapon weapon:
                    var weaponEnchantments = weapon.Enchantments.SelectMany((ItemEnchantment p) => p.SelectComponents<WeaponEnhancementBonus>()).ToList();
                    return weaponEnchantments.Any(e => modifiers.Contains(e.Descriptor));
                case ItemEntityArmor armor:
                    var armorEnchantments = armor.Enchantments.SelectMany(p => p.SelectComponents<WeaponEnhancementBonus>());
                    return armorEnchantments.Any(e => modifiers.Contains(e.Descriptor));
                case ItemEntityShield shield:
                    var shieldAsWeaponEnchantments = shield.WeaponComponent.Enchantments.SelectMany((ItemEnchantment p) => p.SelectComponents<WeaponEnhancementBonus>()).ToList();
                    var shieldAsArmorEnchantments = shield.ArmorComponent.Enchantments.SelectMany(p => p.SelectComponents<WeaponEnhancementBonus>());
                    return
                        shieldAsWeaponEnchantments.Any(e => modifiers.Contains(e.Descriptor))
                        || shieldAsArmorEnchantments.Any(e => modifiers.Contains(e.Descriptor));
                default:
                    break;

            }
            return false;
        }
    }
}
#endif