// borrowed shamelessly and enhanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Owlcat.Runtime.UniRx;
using Kingmaker;
using Kingmaker.Achievements;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums.Damage;
using Kingmaker.GameModes;
using Kingmaker.Items;
using Kingmaker.RuleSystem;
using Kingmaker.Settings;
//using Kingmaker.UI._ConsoleUI.Models;
using Kingmaker.UI.Common;
//using Kingmaker.UI.RestCamp;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Utility;
using Kingmaker.View;
using System;
using System.Collections.Generic;
using System.Linq;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using UnityEngine;
using UnityModManager = UnityModManagerNet.UnityModManager;
// using Steamworks;
using Kingmaker.Achievements.Platforms;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.MVVM._PCView.ServiceWindows.Inventory;
using Kingmaker.UI.MVVM._PCView.Slots;
using Kingmaker.UI.MVVM._VM.Common;
using Kingmaker.UI.MVVM._VM.CounterWindow;
using Kingmaker.UI.Loot;
using Kingmaker.UI.MVVM._PCView.Vendor;
using Kingmaker.UI.TurnBasedMode;
using Kingmaker.UI._ConsoleUI.CombatStartScreen;
using Kingmaker.Items.Slots;
using ModKit;
using Kingmaker.EntitySystem.Persistence;
using ToyBox.Multiclass;
using Kingmaker.UI.MVVM._PCView.ActionBar;

namespace ToyBox.BagOfPatches {
    internal static class Misc {
        public static Settings settings = Main.settings;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(Player), nameof(Player.OnAreaLoaded))]
        internal static class Player_OnAreaLoaded_Patch {
            private static void Postfix() {
                Mod.Debug("Player_OnAreaLoaded_Patch");
                Settings.ClearCachedPerSave();
                MultipleClasses.SyncAllGestaltState();
            }
        }

        [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRoutine))]
        internal static class SaveManager_SaveRoutine_Patch {
            private static void Postfix() {
                Mod.Log("SaveManager_SaveRoutine_Patch");
                Settings.SavePerSaveSettings();
            }
        }

        public static BlueprintAbility ExtractSpell([NotNull] ItemEntity item) {
            var itemEntityUsable = item as ItemEntityUsable;
            if (itemEntityUsable?.Blueprint.Type != UsableItemType.Scroll) {
                return null;
            }
            return itemEntityUsable.Blueprint.Ability.Parent ? itemEntityUsable.Blueprint.Ability.Parent : itemEntityUsable.Blueprint.Ability;
        }

        public static string GetSpellbookActionName(string actionName, ItemEntity item, UnitEntityData unit) {
            if (actionName != LocalizedTexts.Instance.Items.CopyScroll) {
                return actionName;
            }

            var spell = ExtractSpell(item);
            if (spell == null) {
                return actionName;
            }

            var spellbooks = unit.Descriptor.Spellbooks.Where(x => x.Blueprint.SpellList.Contains(spell)).ToList();

            var count = spellbooks.Count;

            if (count <= 0) {
                return actionName;
            }

            var actionFormat = "{0} <{1}>";

            return string.Format(actionFormat, actionName, count == 1 ? spellbooks.First().Blueprint.Name : "Multiple");
        }


        [HarmonyPatch(typeof(Kingmaker.UI.ServiceWindow.ItemSlot), nameof(Kingmaker.UI.ServiceWindow.ItemSlot.ScrollContent), MethodType.Getter)]
        public static class ItemSlot_ScrollContent_Patch {
            [HarmonyPostfix]
            private static void Postfix(Kingmaker.UI.ServiceWindow.ItemSlot __instance, ref string __result) {
                var currentCharacter = UIUtility.GetCurrentCharacter();
                var component = __instance.Item.Blueprint.GetComponent<CopyItem>();
                var actionName = component?.GetActionName(currentCharacter) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(actionName)) {
                    actionName = GetSpellbookActionName(actionName, __instance.Item, currentCharacter);
                }
                __result = actionName;
            }
        }

        // Disables the lockout for reporting achievements
        [HarmonyPatch(typeof(AchievementEntity), nameof(AchievementEntity.IsDisabled), MethodType.Getter)]
        public static class AchievementEntity_IsDisabled_Patch {
            private static void Postfix(ref bool __result, AchievementEntity __instance) {
                //modLogger.Log("AchievementEntity.IsDisabled");
                if (settings.toggleAllowAchievementsDuringModdedGame) {
                    //modLogger.Log($"AchievementEntity.IsDisabled - {__result}");
                    __result = Game.Instance.Player.StartPreset.Or<BlueprintAreaPreset>((BlueprintAreaPreset)null)?.DlcCampaign != null || !__instance.Data.OnlyMainCampaign && __instance.Data.SpecificDlc != null && Game.Instance.Player.StartPreset.Or<BlueprintAreaPreset>((BlueprintAreaPreset)null)?.DlcCampaign != __instance.Data.SpecificDlc?.Get() || (UnityEngine.Object)__instance.Data.MinDifficulty != (UnityEngine.Object)null && Game.Instance.Player.MinDifficultyController.MinDifficulty.CompareTo(__instance.Data.MinDifficulty.Preset) < 0 || __instance.Data.IronMan && !(bool)(SettingsEntity<bool>)SettingsRoot.Difficulty.OnlyOneSave;
                    // || (Game.Instance.Player.ModsUser || OwlcatModificationsManager.Instance.IsAnyModActive)
                    //modLogger.Log($"AchievementEntity.IsDisabled - {__result}");
                }
            }
        }
        // Removes the flag that taints the save file of a user who mods their game
        [HarmonyPatch(typeof(Player), nameof(Player.ModsUser), MethodType.Getter)]
        public static class Player_ModsUser_Patch {
            public static bool Prefix(ref bool __result) {
                if (settings.toggleAllowAchievementsDuringModdedGame) {
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        // SPIDERS

        internal static class ModelReplacers {

            public static bool spidersBegone = false;
            public static bool vescavorsBegone = false;
            public static bool retrieversBegone = false;
            public static bool derakniBegone = false;
            public static bool deskariBegone = false;

            public static void CheckAndReplace(ref UnitEntityData unitEntityData) {
                var type = unitEntityData.Blueprint.Type;

                // spider checks
                if (spidersBegone) {
                    var isASpider = IsSpiderType(type?.AssetGuidThreadSafe);
                    var isASpiderSwarm = IsSpiderSwarmType(type?.AssetGuidThreadSafe);
                    var isOtherSpiderUnit = IsSpiderBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    var isOtherSpiderSwarmUnit = IsSpiderSwarmBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    if (isASpider || isOtherSpiderUnit) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintWolfStandardGUID;
                        return;
                    }
                    else if (isASpiderSwarm || isOtherSpiderSwarmUnit) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintCR2RatSwarmGUID;
                        return;
                    }
                }

                // vescavor checks
                if (vescavorsBegone) {
                    var isAVescavorGuard = IsVescavorGuardType(type?.AssetGuidThreadSafe);
                    var isAVescavorQueen = IsVescavorQueenType(type?.AssetGuidThreadSafe);
                    var isAVescavorSwarm = IsVescavorSwarmType(type?.AssetGuidThreadSafe);
                    var isOtherVescavorGuardUnit = IsVescavorGuardBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    var isOtherVescavorQueenUnit = IsVescavorQueenBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    var isOtherVescavorSwarmUnit = IsVescavorSwarmBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    if (isAVescavorGuard || isOtherVescavorGuardUnit) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintWolfStandardGUID;
                        return;
                    }
                    else if (isAVescavorSwarm || isOtherVescavorSwarmUnit) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintCR2RatSwarmGUID;
                        return;
                    }
                    else if (isAVescavorQueen || isOtherVescavorQueenUnit) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintDireWolfStandardGUID;
                        return;
                    }
                }

                // retriever checks
                if (retrieversBegone) {
                    var isARetriever = IsRetrieverType(type?.AssetGuidThreadSafe);
                    var isAAreshkagelRetriever = IsRetrieverAreshkagelType(type?.AssetGuidThreadSafe);
                    var isOtherRetrieverUnit = IsRetrieverBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    var isAreshkagelRetrieverUnit = IsRetrieverAreshkagelBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    if (isARetriever || isOtherRetrieverUnit) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintBearStandardGUID;
                        return;
                    }
                    else if (isAAreshkagelRetriever || isAreshkagelRetrieverUnit) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintOwlBearStandardGUID;
                        return;
                    }
                }

                // derakni checks
                if (derakniBegone) {
                    var isADemonDerakni = IsDemonDerakniType(type?.AssetGuidThreadSafe);
                    if (isADemonDerakni) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintTriceratopsStandarGUID;
                        return;
                    }
                }

                // Deskari checks
                if (deskariBegone) {
                    var isADeskari = IsDeskariBlueprintUnit(unitEntityData.Blueprint.AssetGuidThreadSafe);
                    if (isADeskari) {
                        unitEntityData.Descriptor.CustomPrefabGuid = blueprintMastodonStandarGUID;
                        return;
                    }
                }
            }

            public static void CheckAndReplace(ref BlueprintUnit blueprintUnit) {
                var type = blueprintUnit.Type;

                // spider checks
                if (spidersBegone) {
                    var isASpider = IsSpiderType(type?.AssetGuidThreadSafe);
                    var isASpiderSwarm = IsSpiderSwarmType(type?.AssetGuidThreadSafe);
                    var isOtherSpiderUnit = IsSpiderBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);
                    var isOtherSpiderSwarmUnit = IsSpiderSwarmBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);

                    if (isASpider || isOtherSpiderUnit) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintWolfStandardGUID).Prefab;
                        return;
                    }
                    else if (isASpiderSwarm || isOtherSpiderSwarmUnit) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintCR2RatSwarmGUID).Prefab;
                        return;
                    }
                }

                // vescavor checks
                if (vescavorsBegone) {
                    var isAVescavorGuard = IsVescavorGuardType(type?.AssetGuidThreadSafe);
                    var isAVescavorQueen = IsVescavorQueenType(type?.AssetGuidThreadSafe);
                    var isAVescavorSwarm = IsVescavorSwarmType(type?.AssetGuidThreadSafe);
                    var isOtherVescavorGuardUnit = IsVescavorGuardBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);
                    var isOtherVescavorQueenUnit = IsVescavorQueenBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);
                    var isOtherVescavorSwarmUnit = IsVescavorSwarmBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);

                    if (isAVescavorGuard || isOtherVescavorGuardUnit) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintWolfStandardGUID).Prefab;
                        return;
                    }
                    else if (isAVescavorSwarm || isOtherVescavorSwarmUnit) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintCR2RatSwarmGUID).Prefab;
                        return;
                    }
                    else if (isAVescavorQueen || isOtherVescavorQueenUnit) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintDireWolfStandardGUID).Prefab;
                        return;
                    }
                }

                // retriever checks
                if (retrieversBegone) {
                    var isARetriever = IsRetrieverType(type?.AssetGuidThreadSafe);
                    var isAAreshkagelRetriever = IsRetrieverAreshkagelType(type?.AssetGuidThreadSafe);
                    var isOtherRetrieverUnit = IsRetrieverBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);
                    var isAreshkagelRetrieverUnit = IsRetrieverAreshkagelBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);

                    if (isARetriever || isOtherRetrieverUnit) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintBearStandardGUID).Prefab;
                        return;
                    }
                    else if (isAAreshkagelRetriever || isAreshkagelRetrieverUnit) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintOwlBearStandardGUID).Prefab;
                        return;
                    }
                }

                // derakni checks
                if (derakniBegone) {
                    var isADemonDerakni = IsDemonDerakniType(type?.AssetGuidThreadSafe);
                    if (isADemonDerakni) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintTriceratopsStandarGUID).Prefab;
                        return;
                    }
                }


                // Deskari checks
                if (deskariBegone) {
                    var isADeskari = IsDeskariBlueprintUnit(blueprintUnit.AssetGuidThreadSafe);
                    if (isADeskari) {
                        blueprintUnit.Prefab = Utilities.GetBlueprintByGuid<BlueprintUnit>(blueprintMastodonStandarGUID).Prefab;
                        return;
                    }
                }
            }
            // Spider check methods
            private static bool IsSpiderType(string typeGuid) => typeGuid == spiderTypeGUID;
            private static bool IsSpiderSwarmType(string typeGuid) => typeGuid == spiderSwarmTypeGUID;
            private static bool IsSpiderBlueprintUnit(string blueprintUnitGuid) => spiderGuids.Contains(blueprintUnitGuid);
            private static bool IsSpiderSwarmBlueprintUnit(string blueprintUnitGuid) => spiderSwarmGuids.Contains(blueprintUnitGuid);

            // Vescavor check methods
            private static bool IsVescavorGuardType(string typeGuid) => typeGuid == vescavorGuardTypeGUID;
            private static bool IsVescavorQueenType(string typeGuid) => typeGuid == vescavorQueenTypeGUID;
            private static bool IsVescavorSwarmType(string typeGuid) => typeGuid == vescavorSwarmTypeGUID;
            private static bool IsVescavorGuardBlueprintUnit(string blueprintUnitGuid) => vescavorGuardGuids.Contains(blueprintUnitGuid);
            private static bool IsVescavorQueenBlueprintUnit(string blueprintUnitGuid) => vescavorQueenGuids.Contains(blueprintUnitGuid);
            private static bool IsVescavorSwarmBlueprintUnit(string blueprintUnitGuid) => vescavorSwarmGuids.Contains(blueprintUnitGuid);

            // Retriever check methods
            private static bool IsRetrieverType(string typeGuid) => typeGuid == RetrieverTypeGUID;
            private static bool IsRetrieverAreshkagelType(string typeGuid) => typeGuid == RetrieverAreshkagelTypeGUID;
            private static bool IsRetrieverBlueprintUnit(string blueprintUnitGuid) => RetrieverGuids.Contains(blueprintUnitGuid);
            private static bool IsRetrieverAreshkagelBlueprintUnit(string blueprintUnitGuid) => RetrieverAreshkagelGuids.Contains(blueprintUnitGuid);

            // Derakni check method
            private static bool IsDemonDerakniType(string typeGuid) => typeGuid == demonDerakniTypeGUID;

            // Deskari check method
            private static bool IsDeskariBlueprintUnit(string blueprintUnitGuid) => blueprintUnitGuid == blueprintDeskariGUID;

            private const string spiderTypeGUID = "243702bdc53e2574aaa34d1e3eafe6aa";
            private const string spiderSwarmTypeGUID = "0fd1473096fbdda4db770cca8366c5e1";

            private const string vescavorGuardTypeGUID = "6cc8fb5ba241e9340adfb908b5d0ef85";
            private const string vescavorQueenTypeGUID = "c73d6ef065a177c4d89b251000192025";
            private const string vescavorSwarmTypeGUID = "7885004e5fe98d044b279637976299cc";

            private const string RetrieverTypeGUID = "92ab3c61406a420288f6277cae48efdf";
            private const string RetrieverAreshkagelTypeGUID = "a1f8e3fdf9288e342b70b44e7cb67b0f";

            private const string demonDerakniTypeGUID = "f57d863656bcfd4449a2fc743c3e895c";

            private const string blueprintDeskariGUID = "5a75db49bf7aeaf4c9f0264cac3eed5c";

            private const string blueprintCR2RatSwarmGUID = "12a5944fa27307e4e8b6f56431d5cc8c";
            private const string blueprintWolfStandardGUID = "ea610d9e540af4243b1310a3e6833d9f";
            private const string blueprintDireWolfStandardGUID = "87b83e0e06432a44eb50fb03c71bc8f5";
            private const string blueprintBearStandardGUID = "cbaf7673c1c75a746b195af100bfab32";
            private const string blueprintOwlBearStandardGUID = "d6e0acbdbdb56114898922063ae2cba0";
            private const string blueprintTriceratopsStandarGUID = "429171c659daac44689a34d3b7771140";
            private const string blueprintMastodonStandarGUID = "028cc6f46e7998f46855a33ffde89567";

            private static readonly string[] spiderSwarmGuids = new string[]
            {
                "a28e944558ed5b64790c3701e8c89d75",
                "da2f152d19ce4d54e8c17da91f01fabd",
                "f2327e24765fb6342975b6216bfb307b"
            };
            private static readonly string[] spiderGuids = new string[]
            {
                "272f71e982166934182d51b4e03e400e",
                "d95785c3853077a4599e0cbe8874703f",
                "48f0c472e5cd4beda4afdb1b6c39c344",
                "ae2806b1e73ed7b4e9e9ae966de4dad6",
                "b048bb08e51492a4092063026282fa93",
                "00f6b260b3727b44ba30a9e51abf3b11",
                "6eb8f96ee587cc24ba375f082b2ecdbc",
                "b69082d0bfe9e9446b00363d617b7473",
                "d0e28afa4e4c0994cb6deae66612445a",
                "c4b33e5fd3d3a6f46b2aade647b0bf25",
                "457be920f33d9ee42b697f64a076ba98",
                "38a5be8e3d104fa28bdb39450cf80858",
                "63897b4df57da2f4396ca8a6f34723e7",
                "a21493b15142420bb7623cf97ebad1c9",
                "e9c1c68972cc4904dacdf2df9acf6730",
                "84d46dae0fbd4dfba7d85d2bd4d6648c",
                "f560cc7976d44bbc99c51eef867abc4a",
                "18a3ceeb3fb44f24ea6d3035a5f05a8c",
                "30e473f4deea1d34caac26be7836f166",
                "ba9451623f3f13742a8bd12da4822d2b",
                "1be1454f47c246419f0b410ab451d749",
                "0db7fc0d547b43668d6eb9be0cb1725a",
                "a813d907bc55e734584d99a038c9211e",
                "51c66b0783a748c4b9538f0f0678c4d7",
                "07467e9a29a215346ab66fec7963eb62",
                "4622aca7715007147b26b7fc26db3df8",
                "9e120b5e0ad3c794491c049aa24b9fde",
                "d7af2cc1ac8611c4c9abec7be93b0e12",
                "a027b1b189e95c64a9323da021bd7a9a",
            };

            private static readonly string[] vescavorSwarmGuids = new string[]
             {
                 "c148c12cb7914a50b2fccc39fa880b73",
                 "f03d262634c93a340b85c4a93cd0ffe4",
                 "204a57cdfd30fdc4da930a05f87b5a0b",
                 "d1add298a78c9744c89c9b4f87df5316",
                 "39ea2dcdc362421f94643abe52de9aed",
                 
                 //Daeran's Other Swarm is considered a vescavor and has this ID - replace this as well?
                 "0264a9119a0737447a226cdd4ba1f79b"
             };
            private static readonly string[] vescavorGuardGuids = new string[]
            {
                "17a0d2b9a532ff641bc122778fa80e05",
                "0413e0164ae24d9d9d78348a186ce375"
            };
            private static readonly string[] vescavorQueenGuids = new string[]
            {
                "3d59b2d00f92a244ea887bd74f96dd85",
                "e3cbfef493c4a3f4fa2abb660ba6aad6"
            };

            private static readonly string[] RetrieverGuids = new string[]
            {
                // Standard Units
                "5b6a2b0c6c8aa28438a4b65b3afb02c1",
                "2f79139e8f8f3514d8f751975cca5a29",
                "d0f630b8893f35843b44f81e1d78e55c",
                "427ae028e138789438a4688d579bed90",
                "e40064497ffc3a749a6e5e6ac5f2666a",
                "683e76fa7ae24d9899ab64c41d2878be",
                "fde2aabdc2dd1e74ebf045cd34183b62",
                "81115449d26349d4ca1a0066b94efe46",
                "76ff7605e93d426bb841a9f44dc527f5",
                "90c9f482ed41458da2c203ca604a6614",

                // Army Units
                "b4633d6d8ca7e95479cc156808b0da3e",
            };
            private static readonly string[] RetrieverAreshkagelGuids = new string[]
            {
                // Areshkagel Version
                "9876513c09509954bb3330dc650fb9ae",
                "1e4cafbd06b16cb4c9ba27538203a42d"
            };
        }

        [HarmonyPatch(typeof(UnitEntityData), nameof(UnitEntityData.CreateView))]
        public static class UnitEntityData_CreateView_Patch {
            public static void Prefix(ref UnitEntityData __instance) {
                ModelReplacers.spidersBegone = settings.toggleSpiderBegone;
                ModelReplacers.vescavorsBegone = settings.toggleVescavorsBegone;
                ModelReplacers.retrieversBegone = settings.toggleRetrieversBegone;
                ModelReplacers.derakniBegone = settings.toggleDeraknisBegone;
                ModelReplacers.deskariBegone = settings.toggleDeskariBegone;

                ModelReplacers.CheckAndReplace(ref __instance);
            }
        }

        [HarmonyPatch(typeof(BlueprintUnit), nameof(BlueprintUnit.PreloadResources))]
        public static class BlueprintUnit_PreloadResources_Patch {
            public static void Prefix(ref BlueprintUnit __instance) {
                ModelReplacers.spidersBegone = settings.toggleSpiderBegone;
                ModelReplacers.vescavorsBegone = settings.toggleVescavorsBegone;
                ModelReplacers.retrieversBegone = settings.toggleRetrieversBegone;
                ModelReplacers.derakniBegone = settings.toggleDeraknisBegone;
                ModelReplacers.deskariBegone = settings.toggleDeskariBegone;

                ModelReplacers.CheckAndReplace(ref __instance);
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), nameof(EntityCreationController.SpawnUnit))]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState), typeof(string) })]
        public static class EntityCreationControllert_SpawnUnit_Patch1 {
            public static void Prefix(ref BlueprintUnit unit) {
                ModelReplacers.spidersBegone = settings.toggleSpiderBegone;
                ModelReplacers.vescavorsBegone = settings.toggleVescavorsBegone;
                ModelReplacers.retrieversBegone = settings.toggleRetrieversBegone;
                ModelReplacers.derakniBegone = settings.toggleDeraknisBegone;
                ModelReplacers.deskariBegone = settings.toggleDeskariBegone;

                ModelReplacers.CheckAndReplace(ref unit);
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), nameof(EntityCreationController.SpawnUnit))]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(UnitEntityView), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState), typeof(string) })]
        public static class EntityCreationControllert_SpawnUnit_Patch2 {
            public static void Prefix(ref BlueprintUnit unit) {
                ModelReplacers.spidersBegone = settings.toggleSpiderBegone;
                ModelReplacers.vescavorsBegone = settings.toggleVescavorsBegone;
                ModelReplacers.retrieversBegone = settings.toggleRetrieversBegone;
                ModelReplacers.derakniBegone = settings.toggleDeraknisBegone;
                ModelReplacers.deskariBegone = settings.toggleDeskariBegone;

                ModelReplacers.CheckAndReplace(ref unit);
            }
        }
        [HarmonyPatch(typeof(Kingmaker.Items.Slots.ItemSlot), nameof(Kingmaker.Items.Slots.ItemSlot.RemoveItem), new Type[] { typeof(bool), typeof(bool) })]
        private static class ItemSlot_RemoveItem_Patch {
            private static void Prefix(Kingmaker.Items.Slots.ItemSlot __instance, ref ItemEntity __state) {
                if (Game.Instance.CurrentMode == GameModeType.Default && settings.togglAutoEquipConsumables) {
                    __state = null;
                    var slot = __instance.Owner.Body.QuickSlots.FindOrDefault(s => s.HasItem && s.Item == __instance.m_ItemRef);
                    if (slot != null) {
                        __state = __instance.m_ItemRef;
                    }
                }
            }

            private static void Postfix(Kingmaker.Items.Slots.ItemSlot __instance, ItemEntity __state) {
                if (Game.Instance.CurrentMode == GameModeType.Default && settings.togglAutoEquipConsumables) {
                    if (__state != null) {
                        var blueprint = __state.Blueprint;
                        var item = Game.Instance.Player.Inventory.Items.FindOrDefault(i => i.Blueprint.ItemType == ItemsFilter.ItemType.Usable && i.Blueprint == blueprint);
                        if (item != null) {
                            Game.Instance.ScheduleAction(() => {
                                try {
                                    Mod.Debug($"refill {item.m_Blueprint.Name.cyan()}");
                                    __instance.InsertItem(item);
                                }
                                catch (Exception e) { Mod.Error($"{e}"); }
                            });
                        }
                        __state = null;
                    }
                }
            }
        }

        // To eliminate some log spam
        /*[HarmonyPatch(typeof(SteamAchievementsManager), nameof(SteamAchievementsManager.OnUserStatsStored), new Type[] { typeof(UserStatsStored_t) })]
        public static class SteamAchievementsManager_OnUserStatsStored_Patch {
            public static bool Prefix(ref SteamAchievementsManager __instance, UserStatsStored_t pCallback) {
                if ((long)(ulong)__instance.m_GameId != (long)pCallback.m_nGameID)
                    return false;
                if (EResult.k_EResultOK == pCallback.m_eResult) { }
                //Debug.Log((object)"StoreStats - success");
                else if (EResult.k_EResultInvalidParam == pCallback.m_eResult) {
                    Debug.Log((object)"StoreStats - some failed to validate");
                    __instance.OnUserStatsReceived(new UserStatsReceived_t() {
                        m_eResult = EResult.k_EResultOK,
                        m_nGameID = (ulong)__instance.m_GameId
                    });
                }
                else
                    Debug.Log((object)("StoreStats - failed, " + (object)pCallback.m_eResult));
                return false;
            }
        */

        // Turnbased Combat Start Delay
        [HarmonyPatch(typeof(TurnBasedModeUIController), nameof(TurnBasedModeUIController.ShowCombatStartWindow))]
        private static class Difficulty_Override_Patch {
            private static bool Prefix(TurnBasedModeUIController __instance) {
                if (settings.turnBasedCombatStartDelay == 4f) return true;
                if (__instance.m_CombatStartWindowVM == null) {
                    __instance.HideTurnPanel();
                    __instance.m_CombatStartWindowVM = new CombatStartWindowVM(new Action(__instance.HideCombatStartWindow));
                    __instance.m_Config.CombatStartWindowView.Bind(__instance.m_CombatStartWindowVM);
                    object p = DelayedInvoker.InvokeInTime(new Action(__instance.HideCombatStartWindow), settings.turnBasedCombatStartDelay, true);
                }
                return false;
            }
        }
        // Shift + Click Inventory Tweaks
        [HarmonyPatch(typeof(CommonVM), nameof(CommonVM.HandleOpen), new Type[] {
            typeof(CounterWindowType), typeof(ItemEntity), typeof(Action<int>)
        })]
        public static class CommonVM_HandleOpen_Patch {
            public static bool Prefix(CounterWindowType type, ItemEntity item, Action<int> command) {
                if (settings.toggleShiftClickToFastTransfer && UI.KeyBindings.GetBinding("ClickToTransferModifier").IsModifierActive) {
                    command.Invoke(item.Count);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ItemSlotPCView), nameof(ItemSlotPCView.OnClick))]
        public static class ItemSlotPCView_OnClick_Patch {
            public static bool Prefix(ItemSlotPCView __instance) {
                if (settings.toggleShiftClickToFastTransfer && UI.KeyBindings.GetBinding("ClickToTransferModifier").IsModifierActive) {
                    __instance.OnDoubleClick();
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(InventorySlotPCView), nameof(InventorySlotPCView.OnClick))]
        public static class InventorySlotPCView_OnClick_Patch {
            public static bool Prefix(InventorySlotPCView __instance) {
                if (settings.toggleShiftClickToFastTransfer && UI.KeyBindings.GetBinding("ClickToTransferModifier").IsModifierActive) {
                    __instance.OnDoubleClick();
                    return false;
                }
                if (__instance.UsableSource != UsableSourceType.Inventory) return true;
                if (!settings.toggleShiftClickToUseInventorySlot) return true;
                if (UI.KeyBindings.GetBinding("InventoryUseModifier").IsModifierActive) {
                    var item = __instance.Item;
                    Mod.Debug($"InventorySlotPCView_OnClick_Patch - Using {item.Name}");
                    try {
                        item.TryUseFromInventory(item.GetBestAvailableUser(), (TargetWrapper)UIUtility.GetCurrentCharacter());
                    }
                    catch (Exception e) {
                        Mod.Error($"InventorySlotPCView_OnClick_Patch - {e}");
                    }
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(VendorSlotPCView), nameof(VendorSlotPCView.OnClick))]
        public static class VendorSlotPCView_OnClick_Patch {
            public static bool Prefix(VendorSlotPCView __instance) {
                if (settings.toggleShiftClickToFastTransfer && UI.KeyBindings.GetBinding("ClickToTransferModifier").IsModifierActive) {
                    __instance.OnDoubleClick();
                    return false;
                }
                return true;
            }
        }
    }
}
