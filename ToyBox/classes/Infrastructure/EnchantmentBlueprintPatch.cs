using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints.Items;
using UnityEngine;
using Kingmaker.Items;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Components;
using ModKit;
using ToyBox;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.UI.Common;

namespace ToyBox {

    public class EnchantmentBlueprintPatch {
        #region static aliases
        // alias enchantment GUIDs as strings. god help you if you need to edit something.

        // armour enchants
        const string poisonResistant = "30c370f2385b56045814e2f37b34cc96";
        const string fortLight = "1e69e9029c627914eb06608dad707b36";
        const string fortMed = "62ec0b22425fb424c82fd52d7f4c02a5";
        const string fortHeavy = "9b1538c732e06544bbd955fee570a2be";
        const string spellRes13 = "4bc20fd0e137e1645a18f030b961ef3d";
        const string spellRes15 = "ad0f81f6377180d4292a2316efb950f2";
        const string spellRes17 = "49fe9e1969afd874181ed7613120c250";
        const string spellRes19 = "583938eaafc820f49ad94eca1e5a98ca";
        const string shadow = "d64d7aa52626bc24da3906dce17dbc7d";
        const string shadowImp = "6b090a291c473984baa5b5bb07a1e300";
        const string invulnerability = "4ffa3c3d5f6cdfb4eaf15f11d8e55bd1";
        const string balanced = "53fba8eec3abd214b98a57b12d7ad0a7";
        const string resCold10 = "c872314ecfab32949ad2e0eebd834919";
        const string resCold20 = "510d87d2a949587469882061ee186522";
        const string resCold30 = "7ef70c319ca74fe4cb5eddea792bb353";
        const string resFire10 = "47f45701cc9545049b3745ef949d7446";
        const string resFire20 = "e7af6912cc308df4e9ee63c8824f2738";
        const string resFire30 = "0e98403449de8ce4c846361c6df30d1f";
        const string resAcid10 = "dd0e096412423d646929d9b945fd6d4c";
        const string resAcid20 = "1346633e0ff138148a9a925e330314b5";
        const string resAcid30 = "e6fa2f59c7f1bb14ebfc429f17d0a4c6";
        const string resSonic10 = "6e2dfcafe4faf8941b1426a86a76c368";
        const string resSonic30 = "8b940da1e47fb6843aacdeac9410ec41";
        const string resLightning10 = "1e4dcaf8ffa56c24788e392dae886166";
        const string resLightning20 = "fcfd9515adbd07a43b490280c06203f9";
        const string resLightning30 = "26b91513989a653458986fabce24ba95";

        // shield specific enchants
        const string arrowCatching = "2940acde07f54421b8bd137dc7b2a6fa";
        const string arrowDeflecting = "faf86f0f02b30884c860b675a0df8e2e";


        // weapon enchants
        const string baneAberration = "ee71cc8848219c24b8418a628cc3e2fa";
        const string baneAnimal = "78cf9fabe95d3934688ea898c154d904";
        const string baneConstruct = "73d30862f33cc754bb5a5f3240162ae6";
        const string baneDragon = "e5cb46a0a658b0a41854447bea32d2ee";
        const string baneFey = "b6948040cdb601242884744a543050d4";
        const string baneHumanoidGiant = "dcecb5f2ffacfd44ead0ed4f8846445d";
        const string baneHumanoidReptilian = "dcecb5f2ffacfd44ead0ed4f8846445d";
        const string baneMagicalBeast = "97d477424832c5144a9413c64d818659";
        const string baneMonstrousHumanoid = "c5f84a79ad154c84e8d2e9fe0dd49350";
        const string baneLawfulOutsider = "3a6f564c8ea2d1941a45b19fa16e59f5";
        const string baneChaoticOutsider = "234177d5807909f44b8c91ed3c9bf7ac";
        const string baneEvilOutsider = "20ba9055c6ae1e44ca270c03feacc53b";
        const string baneGoodOutsider = "a876de94b916b7249a77d090cb9be4f3";
        const string banePlant = "0b761b6ed6375114d8d01525d44be5a9";
        const string baneUndead = "eebb4d3f20b8caa43af1fed8f2773328";
        const string baneVermin = "c3428441c00354c4fabe27629c6c64dd";

        const string corrosive = "633b38ff1d11de64a91d490c683ab1c8";
        const string corrosiveBurst = "2becfef47bec13940b9ee71f1b14d2dd";
        const string flaming = "30f90becaaac51f41bf56641966c4121";
        const string flamingBurst = "3f032a3cd54e57649a0cdad0434bf221";
        const string frost = "421e54078b7719d40915ce0672511d0b";
        const string icyBurst = "564a6924b246d254c920a7c44bf2a58b";
        const string shock = "7bda5277d36ad114f9f9fd21d0dab658";
        const string shockingBurst = "914d7ee77fb09d846924ca08bccee0ff";
        const string thundering = "690e762f7704e1f4aa1ac69ef0ce6a96";
        const string thunderingBurst = "83bd616525288b34a8f34976b2759ea1";
        const string furious = "b606a3f5daa76cc40add055613970d2a";
        const string ghosttouch = "47857e1a5a3ec1a46adf6491b1423b4f";
        const string heartseeker = "e252b26686ab66241afdf33f2adaead6";
        const string keen = "102a9c8c9b7a75e4fb5844e79deaf4c0";
        const string vicious = "a1455a289da208144981e4b1ef92cc56";
        const string anarchic = "57315bc1e1f62a741be0efde688087e9";
        const string axiomatic = "0ca43051edefcad4b9b2240aa36dc8d4";
        const string disruption = "0f20d79b7049c0f4ca54ca3d1ea44baa";
        const string furyborn = "091e2f6b2fad84a45ae76b8aac3c55c3";
        const string holy = "28a9964d81fedae44bae3ca45710c140";
        const string unholy = "28a9964d81fedae44bae3ca45710c140";
        const string igniting = "cd344d5e4cdd8254e97943b2dd358ce5";
        const string nullifying = "efbe3a35fc7349845ac9f96b4c63312e";
        const string speed = "f1c0c50108025d546b2554674ea1c006";
        const string brilliantEnergy = "66e9e299c9002ea4bb65b6f300e43770";
        const string vorpal = "2f60bfcba52e48a479e4a69868e24ebc";
        const string cruel = "629c383ffb407224398bb71d1bd95d14";
        const string secondChance = "a63292054cf307d479bebbff768f5d0e";

        #endregion

        #region Enchantment GUIDs by equivalent +bonus
        public static string[] P1ArmorEnchants =  {
            poisonResistant,
            balanced,
            fortLight
        };

        public static string[] P2ArmorEnchants = {
            shadow,
            spellRes13
        };

        public static string[] P3ArmorEnchants = {
            fortMed,
            spellRes15,
            invulnerability
        };

        public static string[] P4ArmorEnchants = {
            shadowImp,
            spellRes17
        };

        public static string[] P5ArmorEnchants = {
            spellRes19,
            fortHeavy
        };


        public static string[] P1ShieldEnchants = {
            poisonResistant,
            arrowCatching,
            fortLight
        };

        public static string[] P2ShieldEnchants = {
            arrowDeflecting,
            spellRes13
        };

        public static string[] P3ShieldEnchants = {
            spellRes15,
            fortMed
        };

        public static string[] P4ShieldEnchants = {
            spellRes17,
        };

        public static string[] P5ShieldEnchants = {
            spellRes19,
            fortHeavy,
        };


        public static string[] P1MeleeWeapEnchants = {
            corrosive,
            cruel,
            flaming,
            frost,
            furious,
            ghosttouch,
            heartseeker,
            keen,
            shock,
            thundering,
            vicious
        };

        public static string[] P2MeleeWeapEnchants = {
            anarchic,
            axiomatic,
            corrosiveBurst,
            disruption,
            flamingBurst,
            furyborn,
            holy,
            icyBurst,
            igniting,
            shockingBurst,
            thunderingBurst,
            unholy
        };

        public static string[] P3MeleeWeapEnchants = {
            nullifying,
            speed
        };

        public static string[] P4MeleeWeapEnchants = {
            brilliantEnergy
        };

        public static string[] P5MeleeWeapEnchants = {
            vorpal
        };


        public static string[] P1RangedWeapEnchants = {

            corrosive,
            flaming,
            frost,
            thundering,
            shock
        };

        public static string[] P2RangedWeapEnchants = {
            anarchic,
            axiomatic,
            holy,
            unholy,
            corrosiveBurst,
            flamingBurst,
            icyBurst,
            thunderingBurst,
            shockingBurst,
            igniting
        };

        public static string[] P3RangedWeapEnchants = {
            speed
        };

        public static string[] P4RangedWeapEnchants = {
            brilliantEnergy,
            secondChance
        };


        public static string[] bane = {
            baneAberration,
            baneAnimal,
            baneChaoticOutsider,
            baneConstruct,
            baneDragon,
            baneEvilOutsider,
            baneFey,
            baneGoodOutsider,
            baneHumanoidGiant,
            baneHumanoidReptilian,
            baneLawfulOutsider,
            baneMagicalBeast,
            baneMonstrousHumanoid,
            banePlant,
            baneUndead,
            baneVermin,
        };

        public static string[] resistEnergy = {
            resAcid10,
            resCold10,
            resLightning10,
            resFire10,
            resSonic10,
        };

        public static string[] resistEnergyImp = {
            resFire20,
            resCold20,
            resAcid20,
            resLightning20,
        };

        public static string[] resistEnergyGreat = {
            resFire30,
            resCold30,
            resAcid30,
            resSonic30,
            resLightning30
        };

        #endregion

        #region random enchantment methods

        /// <summary>
        /// Generates a random melee weapon enchantment with a given equivalent bonus.
        /// 
        /// If Elemental Burst enchantments are chosen, they need the base
        /// Elemental damage enchantment on the item, too (e.g. Flaming Burst requires
        /// Flaming).
        /// 
        /// Certain bonus values only have one possiblility to be generated.
        /// </summary>
        /// <param name="bonus">+x bonus to generate a random enchantment for. Value from 1-5.</param>
        /// <returns>A random melee weapon enchantment. Null if the method fails.</returns>
        public static BlueprintWeaponEnchantment GetRandomMeleeWeaponEnchantment(int bonus) {
            Random rand = new Random();
            int dieResult;
            string enchantGUID = null;

            switch (bonus) {
                case 1:
                    dieResult = rand.Next(101);
                    if (1 <= dieResult && dieResult <= 9) {
                        enchantGUID = bane[rand.Next(bane.Length + 1) - 1];
                    }
                    else if (10 <= dieResult && dieResult <= 18) {
                        enchantGUID = corrosive;
                    }
                    else if (19 <= dieResult && dieResult <= 23) {
                        enchantGUID = cruel;
                    }
                    else if (24 <= dieResult && dieResult <= 32) {
                        enchantGUID = flaming;
                    }
                    else if (33 <= dieResult && dieResult <= 41) {
                        enchantGUID = frost;
                    }
                    else if (42 <= dieResult && dieResult <= 45) {
                        enchantGUID = furious;
                    }
                    else if (46 <= dieResult && dieResult <= 51) {
                        enchantGUID = ghosttouch;
                    }
                    else if (52 <= dieResult && dieResult <= 56) {
                        enchantGUID = heartseeker;
                    }
                    else if (57 <= dieResult && dieResult <= 77) {
                        enchantGUID = keen;
                    }
                    else if (78 <= dieResult && dieResult <= 85) {
                        enchantGUID = shock;
                    }
                    else if (86 <= dieResult && dieResult <= 94) {
                        enchantGUID = thundering;
                    }
                    else {
                        enchantGUID = vicious;
                    }
                    break;
                case 2:
                    dieResult = rand.Next(101) / 9; // integer division here gives us pretty accurate results
                    switch (dieResult) {
                        case 0:
                            enchantGUID = anarchic;
                            break;
                        case 1:
                            enchantGUID = axiomatic;
                            break;
                        case 2:
                            enchantGUID = corrosiveBurst;
                            break;
                        case 3:
                            enchantGUID = flamingBurst;
                            break;
                        case 4:
                            enchantGUID = holy;
                            break;
                        case 5:
                            enchantGUID = unholy;
                            break;
                        case 6:
                            enchantGUID = icyBurst;
                            break;
                        case 7:
                            enchantGUID = igniting;
                            break;
                        case 8:
                            enchantGUID = disruption;
                            break;
                        case 9:
                            enchantGUID = shockingBurst;
                            break;
                        case 10:
                            enchantGUID = thunderingBurst;
                            break;
                        case 11:
                            enchantGUID = furyborn;
                            break;
                    }
                    break;
                case 3:
                    dieResult = rand.Next(4) - 1;
                    if (dieResult == 0) {
                        enchantGUID = nullifying;
                    } else {
                        enchantGUID = speed;
                    }
                    break;
                case 4:
                    enchantGUID = brilliantEnergy;
                    break;
                case 5:
                    enchantGUID = vorpal;
                    break;
                default:
                    return null;
            }

            SimpleBlueprint holder = ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(System.Guid.Parse(enchantGUID)));
            return holder as BlueprintWeaponEnchantment;
        }

        /// <summary>
        /// Generates a random ranged weapon enchantment with a given equivalent bonus.
        /// 
        /// If Elemental Burst enchantments are chosen, they need the base
        /// Elemental damage enchantment on the item, too (e.g. Flaming Burst requires
        /// Flaming).
        /// 
        /// Certain bonus values only have one possiblility to be generated.
        /// </summary>
        /// <param name="bonus">+x bonus to generate a random enchantment for. Value 1-4.</param>
        /// <returns>A random ranged weapon enchantment. Null if the method fails.</returns>
        public static BlueprintWeaponEnchantment GetRandomRangedWeaponEnchantment(int bonus) {
            Random rand = new Random();
            int dieResult;
            string enchantGUID = null;

            switch (bonus) {
                case 1:
                    dieResult = rand.Next(101);
                    if (1 <= dieResult && dieResult <= 9) {
                        enchantGUID = bane[rand.Next(bane.Length + 1) - 1];
                    }
                    else if (10 <= dieResult && dieResult <= 18) {
                        enchantGUID = corrosive;
                    }
                    else if (19 <= dieResult && dieResult <= 23) {
                        enchantGUID = cruel;
                    }
                    else if (24 <= dieResult && dieResult <= 36) {
                        enchantGUID = flaming;
                    }
                    else if (37 <= dieResult && dieResult <= 48) {
                        enchantGUID = frost;
                    }
                    else if (49 <= dieResult && dieResult <= 54) {
                        enchantGUID = ghosttouch;
                    }
                    else if (55 <= dieResult && dieResult <= 69) {
                        enchantGUID = heartseeker;
                    }
                    else if (70 <= dieResult && dieResult <= 85) {
                        enchantGUID = shock;
                    }
                    else {
                        enchantGUID = thundering;
                    }
                    break;
                case 2:
                    dieResult = rand.Next(11);
                    switch (dieResult) {
                        case 1:
                            enchantGUID = anarchic;
                            break;
                        case 2:
                            enchantGUID = axiomatic;
                            break;
                        case 3:
                            enchantGUID = corrosiveBurst;
                            break;
                        case 4:
                            enchantGUID = flamingBurst;
                            break;
                        case 5:
                            enchantGUID = icyBurst;
                            break;
                        case 6:
                            enchantGUID = shockingBurst;
                            break;
                        case 7:
                            enchantGUID = thunderingBurst;
                            break;
                        case 8:
                            enchantGUID = holy;
                            break;
                        case 9:
                            enchantGUID = unholy;
                            break;
                        case 10:
                            enchantGUID = igniting;
                            break;
                    }
                    break;
                case 3:
                    enchantGUID = speed;
                    break;
                case 4:
                    dieResult = rand.Next(12);
                    if (dieResult <= 2) {
                        enchantGUID = brilliantEnergy;
                    } else {
                        enchantGUID = secondChance;
                    }
                    break;
                default:
                    return null;
            }

            SimpleBlueprint holder = ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(System.Guid.Parse(enchantGUID)));
            return holder as BlueprintWeaponEnchantment;
        }

        /// <summary>
        /// Generates a random main armour enchantment with a given equivalent bonus.
        /// </summary>
        /// <param name="bonus">+x bonus to generate a random enchantment for. Value from 1-5.</param>
        /// <returns>A random main armor enchantment. Null if the method fails.</returns>
        public static BlueprintArmorEnchantment GetRandomArmorEnchantment(int bonus) {
            Random rand = new Random();
            int dieResult;
            string enchantGUID = null;

            switch (bonus) {
                case 1:
                    dieResult = rand.Next(4);
                    if(dieResult == 1) {
                        enchantGUID = balanced;
                    } else if (dieResult == 2) {
                        enchantGUID = poisonResistant;
                    } else {
                        enchantGUID = fortLight;
                    }
                    break;
                case 2:
                    dieResult = rand.Next(2);
                    if (dieResult == 1) {
                        enchantGUID = spellRes13;
                    } else {
                        enchantGUID = shadow;
                    }
                    break;
                case 3:
                    dieResult = rand.Next(4);
                    if (dieResult == 1) {
                        enchantGUID = invulnerability;
                    }
                    else if (dieResult == 2) {
                        enchantGUID = spellRes15;
                    }
                    else {
                        enchantGUID = fortMed;
                    }
                    break;
                case 4:
                    dieResult = rand.Next(4);
                    if (dieResult == 1) {
                        enchantGUID = resistEnergy[rand.Next(resistEnergy.Length + 1) - 1];
                    }
                    else if (dieResult == 2) {
                        enchantGUID = spellRes17;
                    }
                    else {
                        enchantGUID = shadowImp;
                    }
                    break;
                case 5:
                    dieResult = rand.Next(5);
                    if (dieResult == 1) {
                        enchantGUID = resistEnergyImp[rand.Next(resistEnergy.Length + 1) - 1];
                    }
                    else if (dieResult == 2) {
                        enchantGUID = resistEnergyGreat[rand.Next(resistEnergy.Length + 1) - 1];
                    }
                    else if (dieResult == 3) {
                        enchantGUID = spellRes19;
                    }
                    else {
                        enchantGUID = fortHeavy;
                    }
                    break;
                default:
                    return null;
            }

            SimpleBlueprint holder = ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(System.Guid.Parse(enchantGUID)));
            return holder as BlueprintArmorEnchantment;
        }

        /// <summary>
        /// Generates a random shield armour enchantment with a given equivalent bonus.
        /// </summary>
        /// <param name="bonus">+x bonus to generate a random enchantment for. Value from 1-5.</param>
        /// <returns>A random shield armor enchantment. Null if the method fails.</returns>
        public static BlueprintArmorEnchantment GetRandomShieldEnchantment(int bonus) {
            Random rand = new Random();
            int dieResult;
            string enchantGUID = null;

            switch (bonus) {
                case 1:
                    dieResult = rand.Next(5);
                    if (dieResult == 1) {
                        enchantGUID = balanced;
                    }
                    else if (dieResult == 2) {
                        enchantGUID = poisonResistant;
                    } else if (dieResult == 3) {
                        enchantGUID = arrowCatching;
                    }
                    else {
                        enchantGUID = fortLight;
                    }
                    break;
                case 2:
                    dieResult = rand.Next(2);
                    if (dieResult == 1) {
                        enchantGUID = spellRes13;
                    }
                    else {
                        enchantGUID = arrowDeflecting;
                    }
                    break;
                case 3:
                    dieResult = rand.Next(3);
                    if (dieResult == 1) {
                        enchantGUID = spellRes15;
                    }
                    else {
                        enchantGUID = fortMed;
                    }
                    break;
                case 4:
                    dieResult = rand.Next(3);
                    if (dieResult == 1) {
                        enchantGUID = resistEnergy[rand.Next(resistEnergy.Length + 1) - 1];
                    }
                    else {
                        enchantGUID = spellRes17;
                    }
                    break;
                case 5:
                    dieResult = rand.Next(5);
                    if (dieResult == 1) {
                        enchantGUID = resistEnergyImp[rand.Next(resistEnergy.Length + 1) - 1];
                    }
                    else if (dieResult == 2) {
                        enchantGUID = resistEnergyGreat[rand.Next(resistEnergy.Length + 1) - 1];
                    }
                    else if (dieResult == 3) {
                        enchantGUID = spellRes19;
                    }
                    else {
                        enchantGUID = fortHeavy;
                    }
                    break;
                default:
                    return null;
            }

            SimpleBlueprint holder = ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(System.Guid.Parse(enchantGUID)));
            return holder as BlueprintArmorEnchantment;
        }

        #endregion
    }
}
