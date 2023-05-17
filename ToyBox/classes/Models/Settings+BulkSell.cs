using Kingmaker.Enums.Damage;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {
    [Serializable]
    public class BulkSellSettings {
        public int weaponEnchantLevel = 1;
        public int weaponStackSize = 0;
        public int armorEnchantLevel = 1;
        public int armorStackSize = 0;
        public int shieldEnchantLevel = 1;
        public int shieldStackSize = 0;
        public int beltStackSize = 0;
        public int cloakStackSize = 0;
        public int ringStackSize = 0;
        public int headStackSize = 0;
        public int bracerStackSize = 0;
        public int neckStackSize = 0;
        public int potionStackSize = 50;
        public int scrollStackSize = 50;
        public int ingredientStackSize = 50;

        public SerializableDictionary<DamageRealityType, bool> damageReality = new();
        public SerializableDictionary<DamageAlignment, bool> damageAlignment = new();
        public SerializableDictionary<PhysicalDamageMaterial, bool> damageMaterial = new();
        public SerializableDictionary<DamageEnergyType, bool> damageEnergy = new();

        public bool showWeaponEnergyTypes = false;

        public bool sellIngredients = true;
        public bool sellPotions = true;
        public bool sellScrolls = true;

        public bool sellUniqueWeapons = false;
        public bool sellUniqueArmors = false;
        public bool sellUniqueShields = false;

        public int maxAttributeBonusForBelt = 2;
        public int maxAttributeBonusForHead = 2;
        public int maxSaveBonusForCloaks = 2;
        public int maxACBonusForRings = 2;
        public int maxACBonusForBracers = 2;
        public int maxACBonusForNeck = 2;

        public int globalModifier = 1;

    }
}
