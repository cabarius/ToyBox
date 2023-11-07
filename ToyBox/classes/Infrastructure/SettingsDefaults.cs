using Kingmaker.Enums.Damage;
using System;
using System.Collections.Generic;

namespace ToyBox {
    internal static class SettingsDefaults {
        public static readonly HashSet<string> DefaultBuffsToIgnoreForDurationMultiplier = new() {
            "24cf3deb078d3df4d92ba24b176bda97", //Prone
            "e6f2fc5d73d88064583cb828801212f4", //Fatigued
            "bb1b849f30e6464284c1efd0e812d626", //Army Nauseated
            "f59aa0658cda4c7b82bf73c632a39650", //Army Stinking Cloud 
            "6179bbe7a7b4b674c813dedbca121799", //Summoned Unit Appear Buff (causes inaction for summoned units)
            "12f2f2cf326dfd743b2cce5b14e99b3c", //Resurrection Buff
            "4e9ddf0456c4d65498ad90fe6e621c3b", //Ranged Legerdemain Buff
            /* Cooldown Buffs */
            "03b2f5afd3d54131967312a97fa5462e", // WitchHexIceTombCooldownBuff
            "75f14be8cb1d4cf6abe5ec4abd598381", // WitchHexWitheringCooldownBuff
            "e9a4c63018894996a2133b640d7c6dab", // WitchHexDireProphecyBuffCooldownBuff
            "d051ff5949794ca78ca3fbaeb26bd1a2", // TrickRidingCooldownBuff
            "4bc4450170dc4c2782ced03240e2009d", // ArmyBardAurasCooldownBuff
            "8328890b12294dd79c82f3818369e0a5", // ArmyLayOnHandsSelfCooldownBuff
            "3801e343e55b47ddbdce3b537c32eef6", // ArmyRetrieverEyerRay_CooldownBuff
            "fbfdca2aca3bb45458b6c0af4df7bb90", // ArmyChargeAbilityCooldown
            "a37592486280448dba8f78af33690ffe", // FighterAttackFormationCooldownBuff
            "178ed2b0b04b4f5cb890009638ade3ea", // FighterChargeFormationCooldownBuff
            "4840e9fb2bcd4c68b78e313d905e2132", // FighterDefenceFormationCooldownBuff
            "fa037b990d8c49ffa8977479a7a3eed7", // FighterFearsomeFormationCooldownBuff
            "a474c827d6e1bfd46956b8d841a496c7", // CompanionKTC_Cooldown
            "f469d4c7e7a34c3984ce15f0f4431bbc", // DogBark_Cooldown
            "222829a37640ceb47a810be9996b6159", // HepzamirahSummonsBE_Cooldown
            "13b6a92f2bc47da4994f09bdb2a4a9e0", // NocticulaSummonsBE_Cooldown
            "89b501348d054d6408a930efd6105200", // SpontaneousHealingCooldown
            "0a52573d3076420b8ccdde7db49b4fcb", // CelestialTotemLesserBuffCooldown
            "a115039fdc47c7848a05390a294dbcbf", // DeadlyPerformanceCooldownBuff
            "f6164abbbbe246d3b9ac4c8a93e52456", // FearsomeInspirationHiddenBuffCooldown
            "c3ac324ad4725434790d9544ab9d73ee", // ArmigerArdentRerollCooldown
            "65058aafc91a12042b158527f9d0506a", // TrueJudgmentCooldownBuff
            "f80bdf69ef8bc3743a9b18667ba9684e", // MasterSpyCooldownBuff
            "077f4430a10d3504b9078ab717334972", // MasterHunterCooldownBuff
            "6f33cd117281834468ee73e79a0367c6", // MasterStrikeCooldownBuff
            "faa0697caa542f34688f82244ccc2c5f", // ShamanHexAmelioratingCooldownBuffI
            "9906228821acb2a49ad1a013751de037", // ShamanHexAmelioratingCooldownBuffII
            "98fa71575440d8a4d8ce15b70ece5568", // ShamanHexDraconicResilienceCooldownBuff
            "6382987b74a30884e83197077b2a5148", // ShamanHexFortuneCooldownBuff
            "002b4f05558eec844ab1c0cf5d0e714a", // ShamanHexFuryCooldownBuff
            "efa4562d6a8161e48bf8b9498bac90f6", // ShamanHexHealingCooldownBuff
            "b20644cbc292adf4ca2007a5ccd27c65", // ShamanHexMisfortuneCooldownBuff
            "27ae9c5de3920794fbca334bd131f085", // ShamanHexSlumberCooldownBuff
            "20d695886f0c0f24185ebd645c110298", // ShamanHexBattleWardCooldownBuff
            "21ad57f770cad7c4eb5f91510120f170", // ShamanHexHamperingHexCooldownBuff
            "a0d09af0a86f78d4b9778e0b4ce0f069", // ShamanHexBoneWardCooldownBuff
            "22e2eb141d3925c419932407f2d61217", // ShamanHexFearfulGazeCooldownBuff
            "76bdae64447923a45853911f2abbdf05", // ShamanHexFireNimbusCooldownBuff
            "abb89285366b2c94898c1125a1a3747e", // ShamanHexFlameCurseCooldownBuff
            "61851fcdb9e95304e861e3843896282d", // ShamanHexWardOfFlamesCooldownBuff
            "547ec39778e37e3419ed67c887231241", // ShamanHexHypothermiaCooldownBuff
            "e6e98105f86eb904c9af0a2c1f5a6647", // ShamanHexEntanglingCurseCooldownBuff
            "cca470fbc764ba645b07d855c6af431b", // ShamanHexMetalCurseCooldownBuff
            "c2303ebc0d83671468644f7b42a420c0", // ShamanHexBeckoningChillCooldownBuff
            "80c3307481e58fc45a56f21e3b5ec70c", // ShamanHexWindWardCooldownBuff
            "2778a8154f1e4e3a87b5bce12ec35ac8", // GreaterWyrmshifterBreathWeaponCooldownBuff
            "beb1f5e3c7e84de08d095af1be154b47", // RageshaperGroundSlamCooldownBuff
            "e24693e33559eb8448ef27385e6c5a9c", // BloodlineCelestialHeavenlyFireCooldownBuff
            "34dad0518f8eb4040938346f7b78ff8e", // BloodlineFeyLaughingTouchCooldownBuff
            "d1a4d8957788d8f4ead60f09aa228382", // HagboundWitchVileCurseCooldownBuff
            "b00cd2d31d2d87148a6dad31caa697a8", // WitchHexAmelioratingCooldownBuffI
            "a94647a0e45c86445adcbc91ac25b0bf", // WitchHexAmelioratingCooldownBuffII
            "d03be82c4ffd14840a89e600e910ae7d", // WitchHexFortuneCooldownBuff
            "2a09996f7f5f9f845a1b58d0a759621d", // WitchHexHealingCooldownBuff
            "f3dced28095610f459fef4441012ffc1", // WitchHexMisfortuneCooldownBuff
            "1ad64500bb3364442a9c7d28cf9a24d3", // WitchHexSlumberCooldownBuff
            "6f3da77a44fa7304fac61c07a01964a5", // WitchHexVulnerabilityCurseCooldownBuff
            "c1cbd350af845214b8ef11b0da9dd304", // WitchHexAnimalServantCooldownBuff
            "ba2a266ffb7fb6246831ae72eee08d20", // WitchHexDeathCurseCooldownBuff
            "12fd1d05f54fe07479ac48eff49265dc", // WitchHexLayToRestCooldownBuff
            "73921d85ed0d684408c5b5f23b6f4360", // WitchHexAgonyCooldownBuff
            "69d4d478357f1fc49bb38c6c64b0bb48", // WitchHexHoarfrostCooldownBuff
            "a009621bc2d137c4d89f15eff9f10792", // WitchHexMajorAmelioratingCooldownBuffI
            "34e1e519e3dca424ab9914256acc79b1", // WitchHexMajorAmelioratingCooldownBuffII
            "245d0006a26235a4f9d8bfdb5d5e303d", // WitchHexMajorHealingCooldownBuff
            "6d1f2d9d46484ba4ab052f65f5d0b422", // WitchHexRegenerativeSinewCooldownBuff
            "05f4111baf9924a4c9efa35835c1e302", // WitchHexRestlessSlumberCooldownBuff
            "5616b7189359a784396275b470e6dd8b", // FaithStealingStrikeCooldownBuff
            "7e20d7440c9b4872b44fa3b9fd0d125e", // AnomalyDistortionCooldownBuff
            "24775ab6c0c7403082b3fedeff76d28d", // MaskOfTheFastBitesCooldownBuff
            "cfc52ecc6fb34cdfac78fa3986aeb221", // MaskOfAreshkagalHeadband_TabulaRasaCooldownBuff
            "d868546877b2de24388fbdd5741d0c95", // CleavingFinishCooldown
            "8b765e0ef53b4e8e9b495a685e00b8d7", // FrightfulShapeCooldownBuff
            "34762bab68ec86c45a15884b9a9929fc", // IndomitableMountCooldownBuff
            "5c9ef8224acdbab4fbaf59c710d0ef23", // MountedCombatCooldownBuff
            "0d02b41741498e8478695d77ab527b03", // VolleyFireCooldownBuff
            "a98394128e4c41509c1a873e4faf914a", // DragonAzataBreathCooldown
            "99d08a20ac1f4da3b9db2ed3db38a898", // DragonAzataFrightfulPresenceCooldownBuff
            "72f7f1a64a634b84b64c4cd59ef19ad5", // VavakiaAspectDamageCooldown
            "4e9ddf0456c4d65498ad90fe6e621c3b", // GoldenDragonBreathCooldown
            "2936f138e42c6bd459c4bbd300cddc78", // FearControlCooldown
            "6f47e92597a27384bbfe3bcdf2d05566", // LastStandBuffCooldown
            "d4fe4b361568cbf4c816fcd6f39b79be", // BeggarDialogueCooldown
            "7039b533f248a9a4aa3abffd6e83c9ca", // SoothingMudCooldownBuff
            "69bb26014fdb0484d9d3e711b874f853", // SerenityCooldownBuff
            "d50c08a3c00775241ac781932fc3aa02", // EyebiteCooldownBuff
            "a7a5d1143490dae49b8603810866cf4d", // FormOfTheDragonIIBreathWeaponCooldownBuff
            "4934c3a12cfa4f4488834bdcea3b6fbc", // JoltingPortentBuffCooldown
            "08af6d8af6241c84ea66baeafc8222b9", // FormOfTheDragonIIIFrightfulPresenceCooldownBuff
            "85e11efe92991694882cfdb417a941bd", // FrightfulAspectCooldownBuff
            "f4cb0148083231745a9cd971fc6ee9c9", // BlackDragonFrightfulPresenceCooldownBuff
            "e6f07f58f07b47b1b83d595c5a7a3baf", // TerendelevUndead_Buff_FrightfulPresenceCooldown
            "3e75d2d398aa60f44849757d011a27f8", // ZachariusFearAuraCooldown
            "2730acd272e412e47a32f680a68d0a1f" // WhispersOfMadnessEffectCooldown
        };

        public static void InitializeDefaultDamageTypes() {
        }
    }
}
