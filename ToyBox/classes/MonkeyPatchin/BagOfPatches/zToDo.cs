// borrowed shamelessly and enchanced from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/26, which is under the MIT License

using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.AreaLogic.QuestSystem;
using Kingmaker.AreaLogic.SummonPool;
using Kingmaker.Assets.Controllers.GlobalMap;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Blueprints.Items.Components;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Shields;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.Cheats;
using Kingmaker.Controllers;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Combat;
//using Kingmaker.Controllers.GlobalMap;
using Kingmaker.Controllers.Rest;
using Kingmaker.Controllers.Rest.Cooking;
using Kingmaker.Controllers.Units;
using Kingmaker.Designers;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Dungeon;
using Kingmaker.Dungeon.Blueprints;
using Kingmaker.Dungeon.Units.Debug;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Formations;
using DG.Tweening;
using Kingmaker.GameModes;
using Kingmaker.Globalmap;
using Kingmaker.Items;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.Kingdom.Settlements;
using Kingmaker.Kingdom.Tasks;
using Kingmaker.Kingdom.UI;
using Kingmaker.PubSubSystem;
using Kingmaker.RandomEncounters;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.TextTools;
using Kingmaker.UI;
//using Kingmaker.UI._ConsoleUI.Models;
using Kingmaker.UI.Common;
using Kingmaker.UI.FullScreenUITypes;
using Kingmaker.UI.Group;
using Kingmaker.UI.IngameMenu;
using Kingmaker.UI.Kingdom;
using Kingmaker.UI.Log;
using Kingmaker.UI.MainMenuUI;
using Kingmaker.UI.MVVM;
using Kingmaker.UI.MVVM.CharGen;
using Kingmaker.UI.MVVM.CharGen.Phases;
using Kingmaker.UI.MVVM.CharGen.Phases.Mythic;
using Kingmaker.UI.RestCamp;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UI.ServiceWindow.LocalMap;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Class.LevelUp.Actions;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using Kingmaker.View.Spawners;
using Kingmaker.Visual;
using Kingmaker.Visual.Animation.Kingmaker.Actions;
using Kingmaker.Visual.HitSystem;
using Kingmaker.Visual.LocalMap;
using Kingmaker.Visual.Sound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
//using Kingmaker.UI._ConsoleUI.GroupChanger;
using Kingmaker.UI.ActionBar;
using Owlcat.Runtime.Visual.RenderPipeline.RendererFeatures.FogOfWar;
using TMPro;
using TurnBased.Controllers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Kingmaker.UnitLogic.Class.LevelUp.LevelUpState;
using UnityModManager = UnityModManagerNet.UnityModManager;

namespace ToyBox {
#if false
    static class HarmonyPatches_ToDo {
        public static Settings settings = Main.settings;
        public static UnityModManager.ModEntry.ModLogger modLogger = Logger.modLogger;
        public static Player player = Game.Instance.Player;

        [HarmonyPatch(typeof(UnitEntityData), "CreateView")]
        public static class UnitEntityData_CreateView_Patch {
            public static void Prefix(ref UnitEntityData __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref __instance);
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintUnit), "PreloadResources")]
        public static class BlueprintUnit_PreloadResources_Patch {
            public static void Prefix(ref BlueprintUnit __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref __instance);
                }
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), "SpawnUnit")]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState) })]
        public static class EntityCreationControllert_SpawnUnit_Patch1 {
            public static void Prefix(ref BlueprintUnit unit) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref unit);
                }
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), "SpawnUnit")]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(UnitEntityView), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState) })]
        public static class EntityCreationControllert_SpawnUnit_Patch2 {
            public static void Prefix(ref BlueprintUnit unit) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref unit);
                }
            }

        }

        [HarmonyPatch(typeof(ContextConditionAlignment), "CheckCondition")]
        public static class ContextConditionAlignment_CheckCondition_Patch {
            public static void Postfix(ref bool __result, ContextConditionAlignment __instance) {
                if (StringUtils.ToToggleBool(settings.toggleReverseCasterAlignmentChecks)) {
                    if (__instance.CheckCaster) {
                        __result = !__result;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RuleSummonUnit), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(BlueprintUnit), typeof(Vector3), typeof(Rounds), typeof(int) })]
        public static class RuleSummonUnit_Constructor_Patch {
            public static void Prefix(UnitEntityData initiator, BlueprintUnit blueprint, Vector3 position, ref Rounds duration, ref int level, RuleSummonUnit __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSummonDurationMultiplier) && UnitEntityDataUtils.CheckUnitEntityData(initiator, (UnitSelectType)settings.indexSummonDurationMultiplier)) {
                    duration = new Rounds(Convert.ToInt32(duration.Value * settings.finalSummonDurationMultiplierValue)); ;
                }

                if (StringUtils.ToToggleBool(settings.toggleSetSummonLevelTo20) && UnitEntityDataUtils.CheckUnitEntityData(initiator, (UnitSelectType)settings.indexSetSummonLevelTo20)) {
                    level = 20;
                }

                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable)) {
                    Storage.SummonedByPlayerFaction = initiator.IsPlayerFaction;
                }

                Logger.ModLoggerDebug("Initiator: " + initiator.CharacterName + $"(PlayerFaction : {initiator.IsPlayerFaction})" + "\nBlueprint: " + blueprint.CharacterName + "\nPosition: " + position.ToString() + "\nDuration: " + duration.Value + "\nLevel: " + level);
            }
        }

        [HarmonyPatch(typeof(ActionBarManager), "CheckTurnPanelView")]
        internal static class ActionBarManager_CheckTurnPanelView_Patch {
            private static void Postfix(ActionBarManager __instance) {
                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable) && CombatController.IsInTurnBasedCombat()) {
                    Traverse.Create((object)__instance).Method("ShowTurnPanel", Array.Empty<object>()).GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(UberLogger.Logger), "ForwardToUnity")]
        static class UberLoggerLogger_ForwardToUnity_Patch {
            static void Prefix(ref object message) {
                if (StringUtils.ToToggleBool(settings.toggleUberLoggerForwardPrefix)) {
                    string message1 = "[UberLogger] " + message as string;
                    message = message1 as object;
                }
            }
        }


        [HarmonyPatch(typeof(DungeonStageInitializer), "Initialize")]
        static class DungeonStageInitializer_Initialize_Patch {
            static void Prefix(BlueprintDungeonArea area) {
                Logger.ModLoggerDebug("Game.Instance.Player.DungeonState.Stage: " + Game.Instance.Player.DungeonState.Stage);
            }
        }
        [HarmonyPatch(typeof(DungeonDebug), "SaveStage")]
        static class DungeonDebug_SaveStage_Patch_Pre {
            static void Prefix(string filename) {
                Logger.ModLoggerDebug("DungeonDebug.SaveStage filename: " + filename);
                Logger.ModLoggerDebug("DungeonDebug.SaveStage Path: " + Path.Combine(Application.persistentDataPath, "DungeonStages"));
            }
        }
        [HarmonyPatch(typeof(DungeonDebug), "SaveStage")]
        static class DungeonDebug_SaveStage_Patch_Post {
            static void Postfix(string filename) {
                if (settings.settingShowDebugInfo) {
                    try {
                        string str = File.ReadAllText(Path.Combine(Application.persistentDataPath, $"DungeonStages\\{filename}"));
                        modLogger.Log($"START---{filename}---START\n" + str + $"\nEND---{filename}---END");
                    }
                    catch (Exception e) {
                        modLogger.Log(e.ToString());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RuleCalculateArmorCheckPenalty), "OnTrigger")]
        public static class RuleCalculateArmorCheckPenalty_OnTrigger_Patch {
            private static bool Prefix(RuleCalculateArmorCheckPenalty __instance) {
                if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0)) {
                    if (!StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        Traverse.Create(__instance).Property("Penalty").SetValue(0);
                        return false;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly) && !__instance.Armor.Wielder.Unit.IsInCombat) {
                        Traverse.Create(__instance).Property("Penalty").SetValue(0);
                        return false;
                    }

                }
                return true;
            }
        }
        [HarmonyPatch(typeof(UIUtilityItem), "GetArmorData")]
        public static class UIUtilityItem_GetArmorData_Patch {
            private static void Postfix(ref UIUtilityItem.ArmorData __result, ref ItemEntityArmor armor) {
                if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0)) {
                    if (!StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        UIUtilityItem.ArmorData armorData = __result;
                        armorData.ArmorCheckPenalty = 0;
                        __result = armorData;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        Logger.ModLoggerDebug(armor.Name);
                        if (armor.Wielder != null) {
                            Logger.ModLoggerDebug(armor.Name + ": " + armor.Wielder.CharacterName);
                            if (!armor.Wielder.Unit.IsInCombat) {
                                Logger.ModLoggerDebug(armor.Name + ": " + armor.Wielder.CharacterName + " - " + armor.Wielder.Unit.IsInCombat);
                                UIUtilityItem.ArmorData armorData = __result;
                                armorData.ArmorCheckPenalty = 0;
                                __result = armorData;
                            }
                        }
                        else {
                            Logger.ModLoggerDebug("!" + armor.Name);
                            UIUtilityItem.ArmorData armorData = __result;
                            armorData.ArmorCheckPenalty = 0;
                            __result = armorData;
                        }

                    }
                }
            }
        }


        [HarmonyPatch(typeof(BlueprintAbility), "GetRange")]
        static class BlueprintAbility_GetRange_Patch_Pre {

            private static Feet defaultClose = 30.Feet();
            private static Feet defaultMedium = 40.Feet();
            private static Feet defaultlong = 50.Feet();
            private static Feet cotwMedium = 60.Feet();
            private static Feet cotwLong = 100.Feet();
            [HarmonyPriority(Priority.Low)]
            static void Postfix(ref Feet __result) {
                if (StringUtils.ToToggleBool(settings.toggleTabletopSpellAbilityRange)) {
                    if (Main.callOfTheWild.ModIsActive()) {
                        if (__result == defaultClose) {
                            __result = 25.Feet();
                        }
                        else if (__result == cotwMedium) {
                            __result = 100.Feet();
                        }
                        else if (__result == cotwLong) {
                            __result = 400.Feet();
                        }
                    }
                    else {
                        if (__result == defaultClose) {
                            __result = 25.Feet();
                        }
                        else if (__result == defaultMedium) {
                            __result = 100.Feet();
                        }
                        else if (__result == defaultlong) {
                            __result = 400.Feet();
                        }
                    }
                }
                if (StringUtils.ToToggleBool(settings.toggleCustomSpellAbilityRange)) {
                    if (Main.callOfTheWild.ModIsActive()) {
                        if (__result == defaultClose) {
                            __result = settings.customSpellAbilityRangeClose.Feet();
                        }
                        else if (__result == cotwMedium) {
                            __result = settings.customSpellAbilityRangeMedium.Feet();
                        }
                        else if (__result == cotwLong) {
                            __result = settings.customSpellAbilityRangeLong.Feet();
                        }
                    }
                    else {
                        if (__result == defaultClose) {
                            __result = settings.customSpellAbilityRangeClose.Feet();
                        }
                        else if (__result == defaultMedium) {
                            __result = settings.customSpellAbilityRangeMedium.Feet();
                        }
                        else if (__result == defaultlong) {
                            __result = settings.customSpellAbilityRangeLong.Feet();
                        }
                    }

                }



                if (StringUtils.ToToggleBool(settings.toggleSpellAbilityRangeMultiplier)) {
                    if (settings.useCustomSpellAbilityRangeMultiplier) {
                        __result = __result * settings.customSpellAbilityRangeMultiplier;
                    }
                    else {
                        __result = __result * settings.spellAbilityRangeMultiplier;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), "IsDirectlyControllable", MethodType.Getter)]
        public static class UnitEntityData_IsDirectlyControllable_Patch {
            public static void Postfix(UnitEntityData __instance, ref bool __result) {
                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable) && __instance.IsPlayerFaction && !__result && __instance.Get<UnitPartSummonedMonster>() != null && !__instance.Descriptor.State.IsFinallyDead && !__instance.Descriptor.State.IsPanicked && !__instance.IsDetached && !__instance.PreventDirectControl) {
                    __result = true;
                }
            }
        }
        // (KingmakerUIExtensionsMod\UIExtensions\Features\AlwaysDisplayPetsPortrait.cs) force the pets to be displayed
        [HarmonyPatch(typeof(GroupController), "WithPet", MethodType.Getter)]
        static class GroupController_get_WithPet_Patch {
            [HarmonyPostfix]
            static void Postfix(ref bool __result) {
                __result = settings.toggleShowAllPartyPortraits;
            }
        }

        // (KingmakerUIExtensionsMod\UIExtensions\Features\AlwaysDisplayPetsPortrait.cs) fix the navigation block isn't refreshed after loading a save
        [HarmonyPatch(typeof(GroupController), "Kingmaker.UI.IUIElement.Initialize")]
        static class GroupController_Initialize_Patch {
            [HarmonyPostfix]
            static void Postfix() {
                if (settings.toggleShowAllPartyPortraits) {
                    GroupControllerUtils.NaviBlockShowAllPartyMembers();
                }
            }
        }

        // (KingmakerUIExtensionsMod\UIExtensions\Features\AlwaysDisplayPetsPortrait.cs) fix the game could duplicately bind a hotkey to character slots multiple times
        [HarmonyPatch(typeof(GroupCharacter), nameof(GroupCharacter.Initialize), typeof(UnitEntityData), typeof(int))]
        static class GroupCharacter_Initialize_Patch {
            [HarmonyPrefix]
            static void Prefix(GroupCharacter __instance) {
                __instance.Bind(false);
            }
        }

        [HarmonyPatch(typeof(GroupController), nameof(GroupController.OnScroll), typeof(PointerEventData))]
        static class GroupController_OnScroll_Patch {
            [HarmonyPrefix]
            static bool Prefix(GroupController __instance, PointerEventData eventData) {
                if (StringUtils.ToToggleBool(settings.toggleShowAllPartyPortraits)) {
                    float y = eventData.scrollDelta.y;
                    if ((double)Math.Abs(y) < (double)Mathf.Epsilon || !__instance.NavigateCharacters((double)y < 0.0)) {
                        return false;
                    }
                    Game.Instance.UI.Common.UISound.Play(UISoundType.ButtonClick);
                    return false;
                }
                return true;
            }
        }

        // (KingmakerUIExtensionsMod\UIExtensions\Features\AlwaysDisplayPetsPortrait.cs) update after gain a pet (after leveling up)
        [HarmonyPatch(typeof(SceneEntitiesState), nameof(SceneEntitiesState.AddEntityData), typeof(EntityDataBase))]
        static class SceneEntitiesState_AddEntityData_Patch {
            [HarmonyPostfix]
            static void Postfix(EntityDataBase data) {
                if (StringUtils.ToToggleBool(settings.toggleShowAllPartyPortraits)) {
                    if (data is UnitEntityData unit && unit.IsPlayerFaction && unit.Descriptor.IsPet) {
                        GroupControllerUtils.NaviBlockShowAllPartyMembers();
                    }
                }
            }
        }

        // (KingmakerUIExtensionsMod\UIExtensions\Features\AlwaysDisplayPetsPortrait.cs) fixed bugs on dragging portraits
        [HarmonyPatch(typeof(GroupController), nameof(GroupController.DragCharacter), typeof(GroupCharacter))]
        static class GroupController_DragCharacter_Patch {
            [HarmonyPrefix]
            static bool Prefix(GroupController __instance, GroupCharacter groupCharacter) {
                if (StringUtils.ToToggleBool(settings.toggleShowAllPartyPortraits)) {
                    List<GroupCharacter> characters = GroupControllerUtils.GetCharacters();
                    UnitEntityData targetUnit = GetTargetToSwap(groupCharacter, characters);
                    if (targetUnit != null) {
                        SwapUnitsIndex(groupCharacter.Unit, targetUnit);
                        SortCharacters(groupCharacter, characters);
                        EventBus.RaiseEvent<IPartyChangedUIHandler>(h => h.HandlePartyChanged());
                    }
                    return false;
                }
                return true;
            }

            static UnitEntityData GetTargetToSwap(GroupCharacter source, List<GroupCharacter> characters) {
                float halfWidth = source.TargetPanel.sizeDelta.x / 2f;
                Vector3 sourcePosition = source.TargetPanel.localPosition;
                bool sourceIsPet = source.Unit.Descriptor.IsPet;
                bool IsValidTarget(GroupCharacter targetCharacter) {
                    UnitEntityData target = targetCharacter.Unit;
                    return target != null && (sourceIsPet ? target.Descriptor.Pet == null : !target.Descriptor.IsPet);
                }

                // check the next unit
                if (source.Index < 5) {
                    GroupCharacter targetCharacter = characters.Skip(source.Index + 1).FirstOrDefault(IsValidTarget);
                    if (targetCharacter != null && sourcePosition.x > targetCharacter.BasePosition.x - halfWidth) {
                        return targetCharacter.Unit;
                    }
                }

                // check the prior unit
                if (source.Index > 0) {
                    GroupCharacter targetCharacter = characters.Take(source.Index).LastOrDefault(IsValidTarget);
                    if (targetCharacter != null && sourcePosition.x < targetCharacter.BasePosition.x + halfWidth) {
                        return targetCharacter.Unit;
                    }
                }

                return null;
            }

            static void SwapUnitsIndex(UnitEntityData source, UnitEntityData target) {
                void EnsureNotPet(ref UnitEntityData unit)
                    => unit = unit.Descriptor.IsPet ? unit.Descriptor.Master.Value : unit;
                EnsureNotPet(ref source);
                EnsureNotPet(ref target);

                // get index
                List<UnitReference> characters = Game.Instance.Player.PartyCharacters;
                int sourceIndex = characters.FindIndex((UnitReference pc) => pc.Value == source);
                int targetIndex = characters.FindIndex((UnitReference pc) => pc.Value == target);

                // swap
                UnitReference sourceUnit = characters[sourceIndex];
                characters[sourceIndex] = characters[targetIndex];
                characters[targetIndex] = sourceUnit;
                Game.Instance.Player.InvalidateCharacterLists();
            }

            static void SortCharacters(GroupCharacter source, List<GroupCharacter> characters) {
                List<UnitEntityData> sortedUnits =
                    UIUtility.GetGroup(GroupControllerUtils.GetWithRemote(), GroupControllerUtils.GetWithPet())
                    .Skip(GroupControllerUtils.GetStartIndex()).Take(6).ToList();
                List<Vector3> positions = characters.Select(item => item.BasePosition).ToList();

                for (int i = 0; i < 6 && i < sortedUnits.Count; i++) {
                    UnitEntityData unit = sortedUnits[i];
                    GroupCharacter character = characters.Find(c => c.Unit == unit);
                    if (character == null) {
                        character = characters.Find(c => !sortedUnits.Contains(c.Unit));
                        GroupControllerUtils.SetCharacter(unit, character.Index);
                    }
                    if (character.Index != i) {
                        DoMoveCharacter(character, i, positions[i], character != source);
                    }
                }

                characters.Sort((x, y) => x.Index - y.Index);
            }

            static void DoMoveCharacter(GroupCharacter character, int newIndex, Vector3 newPosition, bool doLocalMove) {
                character.Bind(false);
                character.Index = newIndex;
                character.Bind(true);
                character.BasePosition = newPosition;
                if (doLocalMove) {
                    character.transform.DOLocalMove(newPosition, 0.2f, false).SetUpdate(true);
                }
            }
        }
        [HarmonyPatch(typeof(GlobalMapLocation), "HandleHoverChange", new Type[] { typeof(bool) })]
        internal static class GlobalMapLocation_HandleHoverChange_Patch {
            private static void Postfix(GlobalMapLocation __instance, ref bool isHover) {
                if (Main.enabled && isHover)
                    Storage.lastHoveredLocation = __instance;
                else
                    Storage.lastHoveredLocation = null;
            }
        }

        // TODO - This may contain  fix for mouse wheel getting lost...
        [HarmonyPatch(typeof(UnityModManager.UI), "Update")]
        internal static class UnityModManager_UI_Update_Patch {
            private static void Postfix(UnityModManager.UI __instance, ref Rect ___mWindowRect, ref Vector2[] ___mScrollPosition, ref int ___tabId) {
                Storage.ummRect = ___mWindowRect;
                //Storage.ummWidth = ___mWindowWidth; float ___mWindowWidth
                Storage.ummScrollPosition = ___mScrollPosition;
                Storage.ummTabId = ___tabId;

                if (Main.enabled) {
                    GameModeType currentGameMode = Game.Instance.CurrentMode;

                    if ((currentGameMode == GameModeType.Default || currentGameMode == GameModeType.EscMode) && StringUtils.ToToggleBool(settings.toggleShowAreaName) && !Game.Instance.IsPaused) {
                        Main.sceneAreaInfo.On();
                        Main.sceneAreaInfo.Text("<b>" + Game.Instance.CurrentlyLoadedArea.AreaName.ToString() + "</b>");
                    }
                    else {
                        Main.sceneAreaInfo.Off();
                    }

                    if (currentGameMode != GameModeType.None && StringUtils.ToToggleBool(settings.toggleDisplayObjectInfo)) {
                        if (Main.sceneAreaInfo.baseGameObject.activeSelf) {
                            Vector3 temp = Main.sceneAreaInfo.baseGameObject.GetComponent<RectTransform>().position;
                            temp.y = Main.sceneAreaInfo.baseGameObject.GetComponent<RectTransform>().position.y - Main.sceneAreaInfo.baseGameObject.GetComponent<RectTransform>().sizeDelta.y;
                            Main.objectInfo.baseGameObject.GetComponent<RectTransform>().position = temp;
                        }
                        else {
                            Main.objectInfo.baseGameObject.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2f, Screen.height * 0.95f, 0);
                        }
                        Main.objectInfo.On();
                        BlueprintScriptableObject blueprint;
                        UnitEntityData unitUnderMouse = Common.GetUnitUnderMouse();
                        BlueprintScriptableObject[] scriptableObjectArray = ActionKey.Tooltip();
                        if (unitUnderMouse != null) {
                            blueprint = (BlueprintScriptableObject)unitUnderMouse.Blueprint;
                        }
                        else {
                            if (scriptableObjectArray == null) {
                                goto DisplayObjectInfoEnd;
                            }
                            blueprint = scriptableObjectArray[0];
                        }
                        Main.objectInfo.Text($"<b>{blueprint.AssetGuid}\n{Utilities.GetBlueprintName(blueprint)}</b>");
                    }
                    else {
                        Main.objectInfo.Off();
                    }
                DisplayObjectInfoEnd:

                    if (settings.toggleEnableFocusCamera && Game.Instance?.UI?.GetCameraRig() != null && (currentGameMode == GameModeType.Default || currentGameMode == GameModeType.Pause)) {
                        List<UnitEntityData> partyMembers = Game.Instance.Player?.ControllableCharacters;

                        if (partyMembers != Storage.partyMembersFocusUnits) {
                            Storage.partyMembersFocusUnits = partyMembers;
                        }

                        if (settings.partyMembersFocusPositionCounter >= Storage.partyMembersFocusUnits.Count) {
                            settings.partyMembersFocusPositionCounter = Storage.partyMembersFocusUnits.Count - 1;
                        }

                        if (Input.GetKeyDown(settings.focusCameraKey)) {
                            if (settings.focusCameraToggle) {
                                settings.focusCameraToggle = false;
                            }
                            else {
                                settings.focusCameraToggle = true;
                            }
                        }

                        if (settings.focusCameraToggle && !Input.GetKey(KeyCode.Mouse2)) {
                            Game.Instance.UI.GetCameraRig().ScrollTo(Storage.partyMembersFocusUnits[settings.partyMembersFocusPositionCounter].Position);
                        }

                        if (Input.GetKeyDown(settings.focusCylceKey)) {

                            if (settings.partyMembersFocusPositionCounter < Storage.partyMembersFocusUnits.Count) {
                                if (settings.partyMembersFocusPositionCounter == Storage.partyMembersFocusUnits.Count - 1) {
                                    settings.partyMembersFocusPositionCounter = 0;
                                }
                                else {
                                    settings.partyMembersFocusPositionCounter++;
                                }
                            }
                            else {
                                settings.partyMembersFocusPositionCounter = 0;

                            }
                        }
                    }

                    if (Input.GetKeyDown(settings.togglePartyAlwaysRoll20Key) && settings.toggleEnablePartyAlwaysRoll20Hotkey) {
                        if (settings.togglePartyAlwaysRoll20) {
                            settings.togglePartyAlwaysRoll20 = Storage.isTrueString;
                            Common.AddLogEntry(Strings.GetText("buttonToggle_PartyAlwaysRolls20") + ": " + Strings.GetText("logMessage_Enabled"), Color.black);

                        }

                        else if (settings.togglePartyAlwaysRoll20) {
                            settings.togglePartyAlwaysRoll20 = Storage.isFalseString;
                            Common.AddLogEntry(Strings.GetText("buttonToggle_PartyAlwaysRolls20") + ": " + Strings.GetText("logMessage_Disabled"), Color.black);
                        }
                    }

                    if (Input.GetKeyDown(settings.resetCutsceneLockKey) && settings.toggleEnableResetCutsceneLockHotkey) {
                        Game.Instance.CheatResetCutsceneLock();
                        Common.AddLogEntry(Strings.GetText("button_ResetCutsceneLock") + ": " + Strings.GetText("logMessage_Enabled"), Color.black);
                    }


                    if (Input.GetKeyDown(settings.actionKey) && settings.toggleEnableActionKey && settings.actionKeyIndex == 6 && ActionKey.teleportUnit != null && ActionKey.teleportUnit.IsInGame) {
                        GameModeType currentMode = Game.Instance.CurrentMode;
                        if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                            Vector3 mousePosition = Common.MousePositionLocalMap();
                            Common.TeleportUnit(ActionKey.teleportUnit, mousePosition);
                        }
                        ActionKey.teleportUnit = null;
                    }

                    if (Input.GetKeyDown(settings.actionKey) && settings.toggleEnableActionKey && settings.actionKeyIndex == 8 && ActionKey.rotateUnit != null && ActionKey.rotateUnit.IsInGame) {
                        GameModeType currentMode = Game.Instance.CurrentMode;
                        if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                            Vector3 mousePosition = Common.MousePositionLocalMap();
                            Common.RotateUnit(ActionKey.rotateUnit, mousePosition);
                        }
                        ActionKey.rotateUnit = null;
                    }

                    if (Input.GetKeyDown(settings.actionKey) && StringUtils.ToToggleBool(settings.toggleEnableActionKey) && settings.actionKeyIndex != 0) {
                        ActionKey.Functions(settings.actionKeyIndex);
                    }

                    if (StringUtils.ToToggleBool(settings.toggleHUDToggle) && Game.Instance.Player.ControllableCharacters.Any() && Input.GetKeyDown(settings.hudToggleKey) && Game.Instance.CurrentMode != GameModeType.None) {
                        Common.ToggleHUD();
                    }

                    if (Input.GetKey(settings.teleportKey) && settings.toggleEnableTeleport) {
                        GameModeType currentMode = Game.Instance.CurrentMode;
                        if (currentMode == GameModeType.Default || currentMode == GameModeType.Pause) {
                            List<UnitEntityData> selectedUnits = Game.Instance.UI.SelectionManagerPC.GetSelectedUnits();
                            Vector3 mousePosition = Common.MousePositionLocalMap();
                            foreach (UnitEntityData unit in selectedUnits) {
                                Common.TeleportUnit(unit, mousePosition);
                            }
                        }
                        else if (currentMode == GameModeType.GlobalMap && Storage.lastHoveredLocation != null) {
                            GlobalMapRules.Instance.TeleportParty(Storage.lastHoveredLocation.Blueprint);
                            Storage.lastHoveredLocation = null;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RandomEncountersController), "GetAvoidanceCheckResult")]
        internal static class RandomEncountersControllern_GetAvoidanceCheckResult_Patch {
            private static void Postfix(ref RandomEncounterAvoidanceCheckResult __result) {
                if (StringUtils.ToToggleBool(settings.toggleEnableAvoidanceSuccess) && StringUtils.ToToggleBool(settings.toggleEnableRandomEncounterSettings)) {
                    __result = RandomEncounterAvoidanceCheckResult.Success;
                }
            }
        }

        [HarmonyPatch(typeof(GroupCharacterPortraitController), "OnUnitSelectionAdd")]
        internal static class GroupCharacterPortraitController_OnUnitSelectionAdd_Patch {
            private static void Postfix(UnitEntityData selected) {
                if (StringUtils.ToToggleBool(settings.toggleEnableFocusCamera) && StringUtils.ToToggleBool(settings.toggleEnableFocusCameraSelectedUnit)) {
                    settings.partyMembersFocusPositionCounter = Storage.partyMembersFocusUnits.FindIndex(a => a == selected);
                }
            }
        }

        [HarmonyPatch(typeof(UnityModManager), "SaveSettingsAndParams")]
        internal static class UnityModManager_SaveSettingsAndParams_Patch {
            private static void Postfix(UnityModManager __instance) {
                FavouritesFactory.SerializeFavourites();
                if (settings.toggleEnableTaxCollector) {
                    TaxCollector.Serialize(Main.taxSettings, Storage.modEntryPath + Storage.taxCollectorFolder + "\\" + Storage.taxCollectorFile);
                }
            }
        }

        [HarmonyPatch(typeof(CameraRig), "SetRotation")]
        static class CameraRig_SetRotation_Patch {
            static bool Prefix(ref float cameraRotation) {
                if (settings.toggleEnableCameraRotation) {
                    if (cameraRotation != settings.defaultRotation) {
                        // If we enter a new area with a different default camera angle, reset the rotation to 0
                        settings.defaultRotation = cameraRotation;
                        settings.cameraRotation = 0;
                    }
                    cameraRotation += settings.cameraRotation;
                    Main.rotationChanged = true;
                    if (Main.localMap) {
                        // If the local map is open, call the Set method to redraw things
                        Traverse.Create(Main.localMap).Method("Set").GetValue();
                    }
                    return true;
                }
                return true;
            }
        }



        [HarmonyPatch(typeof(CameraRig), "TickScroll")]
        static class CameraKey_Patch {
            static bool Prefix(ref CameraRig __instance) {

                if (StringUtils.ToToggleBool(settings.toggleEnableCameraScrollSpeed)) {
                    Traverse.Create(__instance).Field("m_ScrollSpeed").SetValue(settings.cameraScrollSpeed);
                }

                if (settings.toggleEnableCameraRotation) {
                    if (Input.GetKey(settings.cameraTurnLeft)) {
                        settings.cameraRotation -= settings.cameraTurnRate;
                        if (settings.cameraRotation < -180) {
                            settings.cameraRotation += 360;
                        }
                    }
                    else if (Input.GetKey(settings.cameraTurnRight)) {
                        settings.cameraRotation += settings.cameraTurnRate;
                        if (settings.cameraRotation >= 180) {
                            settings.cameraRotation -= 360;
                        }
                    }
                    else if (Input.GetKey(settings.cameraReset)) {
                        settings.cameraRotation = 0;
                    }
                    else {
                        return true;
                    }
                    HarmonyInstance.Create("kingmaker.camerarotation").Patch(AccessTools.Method(typeof(CameraRig), "SetRotation"), new HarmonyMethod(typeof(BagOfTricks.Main).GetMethod("CameraRig_SetRotation_Patch")), null);
                    Game.Instance.UI.GetCameraRig().SetRotation(settings.defaultRotation);
                    return true;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(LocalMap), "OnShow")]
        static class LocalMap_OnShow_Patch {
            static void Prefix(LocalMap __instance) {
                Main.localMap = __instance;
            }

        }

        [HarmonyPatch(typeof(LocalMap), "OnHide")]
        static class LocalMap_OnHide_Patch {
            static void Postfix() {
                Main.localMap = null;
            }
        }

        [HarmonyPatch(typeof(LocalMapRenderer), "IsDirty")]
        static class LocalMapRenderer_IsDirty_Patch {
            static void Postfix(ref bool __result) {
                if (Main.rotationChanged) {
                    // If rotation has changed since drawing the map image, return that it's dirty.
                    __result = true;
                    Main.rotationChanged = false;
                }
            }
        }
        [HarmonyPatch(typeof(CameraZoom))]
        [HarmonyPatch("TickSmoothZoomToTargetValue")]
        public static class CameraZoom_TickSmoothZoomToTargetValue_Patch {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                var codes = new List<CodeInstruction>(instructions);
                if (settings.toggleEnableCameraZoom) {

                    int foundFovMin = -1;
                    int foundFovMax = -1;

                    for (int i = 0; i < codes.Count; i++) {
                        if (codes[i].opcode == OpCodes.Callvirt && codes[i - 1].opcode == OpCodes.Call && codes[i - 2].opcode == OpCodes.Call) {
                            foundFovMin = i - 4;
                            foundFovMax = i - 6;
                            break;
                        }
                    }

                    codes[foundFovMin - 1].opcode = OpCodes.Nop;
                    codes[foundFovMin].opcode = OpCodes.Ldc_R4;
                    codes[foundFovMin].operand = float.Parse(settings.savedFovMin);

                    codes[foundFovMax - 1].opcode = OpCodes.Nop;
                    codes[foundFovMax].opcode = OpCodes.Ldc_R4;
                    codes[foundFovMax].operand = float.Parse(settings.savedFovMax);

                    return codes.AsEnumerable();
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(CameraRig))]
        [HarmonyPatch("SetMapMode")]
        public static class CameraRig_SetMapMode_Patch {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                var codes = new List<CodeInstruction>(instructions);
                if (settings.toggleEnableCameraZoom) {
                    int foundFovGlobalMap = -1;

                    for (int i = 0; i < codes.Count; i++) {
                        if (codes[i].opcode == OpCodes.Br && codes[i - 1].opcode == OpCodes.Ldfld && codes[i - 2].opcode == OpCodes.Call && codes[i - 3].opcode == OpCodes.Ldarg_0) {
                            foundFovGlobalMap = i - 1;
                            break;
                        }
                    }

                    codes[foundFovGlobalMap - 2].opcode = OpCodes.Nop;
                    codes[foundFovGlobalMap - 1].opcode = OpCodes.Nop;
                    codes[foundFovGlobalMap].opcode = OpCodes.Ldc_R4;
                    codes[foundFovGlobalMap].operand = float.Parse(settings.savedFovGlobalMap, Storage.cultureEN);

                    return codes.AsEnumerable();
                }
                return codes.AsEnumerable();
            }
        }

        //UI start
        [HarmonyPatch(typeof(EscMenuWindow), "Initialize")]
        internal static class EscMenuWindow_Initialize_Patch {
            private static void Postfix(EscMenuWindow __instance) {
                if (StringUtils.ToToggleBool(settings.toggleUnityModManagerButton) && Traverse.Create((object)__instance).Field("UnityModManager_Button").GetValue<Button>() == null) {
                    try {
                        Button saveButton = Traverse.Create((object)__instance).Field("ButtonSave").GetValue<Button>();
                        Transform saveButtonParent = saveButton.transform.parent;
                        Button ummButton = UnityEngine.Object.Instantiate<Button>(saveButton);
                        ummButton.name = "UnityModManager_Button";
                        ummButton.transform.SetParent(saveButtonParent, false);
                        ummButton.onClick = new Button.ButtonClickedEvent();
                        ummButton.onClick.AddListener((UnityAction)(() => OnClick()));
                        ummButton.GetComponentInChildren<TextMeshProUGUI>().text = Strings.GetText("misc_UnityModManager");
                        ummButton.transform.SetSiblingIndex(ummButton.transform.GetSiblingIndex() - Mathf.RoundToInt(settings.unityModManagerButtonIndex));
                    }
                    catch (Exception exception) {
                        modLogger.Log(exception.ToString());
                    }
                }

            }
            public static void OnClick() {
                try {
                    UnityModManager.UI.Instance.ToggleWindow(true);
                }
                catch (Exception e) {

                    modLogger.Log(e.ToString()); ;
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), "UpdatePlayerMoneyAndInventoryWeight")]
        internal static class Inventory_UpdatePlayerMoneyAndInventoryWeight_Patch {
            private static void Postfix(Inventory __instance, TextMeshProUGUI ___PlayerMoneyNow) {
                if (StringUtils.ToToggleBool(settings.toggleScaleInventoryMoney)) {
                    try {
                        int digits = Math.Abs((int)Math.Floor(Math.Log10(player.Money)) + 1);
                        String s = player.Money.ToString();
                        switch (digits) {
                            case 8:
                                ___PlayerMoneyNow.text = RichTextUtils.SizePercent(s, 80);
                                break;
                            case 9:
                                ___PlayerMoneyNow.text = RichTextUtils.SizePercent(s, 75);
                                break;
                            case 10:
                                ___PlayerMoneyNow.text = RichTextUtils.SizePercent(s, 70);
                                break;
                            case 11:
                                ___PlayerMoneyNow.text = RichTextUtils.SizePercent(s, 65);
                                break;
                            case 12:
                                ___PlayerMoneyNow.text = RichTextUtils.SizePercent(s, 60);
                                break;
                        }
                    }
                    catch (Exception exception) {
                        modLogger.Log(exception.ToString());
                    }
                }

            }
        }
        //UI end

        [HarmonyPatch(typeof(Player), "OnAreaLoaded")]
        internal static class Player_OnAreaLoaded_Patch {
            private static void Postfix() {
                Main.ReloadPartyState();
                ActionKey.teleportUnit = null;
                ActionKey.rotateUnit = null;
            }
        }
        [HarmonyPatch(typeof(Player), "AttachPartyMember")]
        internal static class Player_AttachPartyMember_Patch {
            private static void Postfix() {
                Main.ReloadPartyState();
            }
        }

        [HarmonyPatch(typeof(Player), "AddCompanion")]
        internal static class Player_AddCompanion_Patch {
            private static void Postfix() {
                Main.ReloadPartyState();
                if (StringUtils.ToToggleBool(settings.toggleShowAllPartyPortraits)) {
                    GroupControllerUtils.NaviBlockShowAllPartyMembers();
                }

            }
        }
        [HarmonyPatch(typeof(Player), "RemoveCompanion")]
        internal static class Player_RemoveCompanion_Patch {
            private static void Postfix() {
                Main.ReloadPartyState();
            }
        }
        [HarmonyPatch(typeof(Player), "DismissCompanion")]
        internal static class Player_DismissCompanion_Patch {
            private static void Postfix() {
                Main.ReloadPartyState();
            }
        }
        [HarmonyPatch(typeof(Player), "SwapAttachedAndDetachedPartyMembers")]
        internal static class Player_SwapAttachedAndDetachedPartyMembers_Patch {
            private static void Postfix() {
                Main.ReloadPartyState();
            }
        }

        [HarmonyPatch(typeof(EscMenuWindow), "OnHotKeyEscPressed")]
        internal static class EscMenuWindow_OnHotKeyEscPressed_Patch {
            private static void Postfix() {
                if (settings.toggleEnableTaxCollector) {
                    try {
                        TaxCollector.Serialize(Main.taxSettings, Storage.modEntryPath + Storage.taxCollectorFolder + "\\" + Storage.taxCollectorFile);

                    }
                    catch (Exception e) {

                        modLogger.Log(e.ToString());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Initialize")]
        internal static class Player_Initialize_Patch {
            private static void Postfix() {
                //settings.defaultVendorSellPriceMultiplier = (float)Game.Instance.BlueprintRoot.Vendors.SellModifier;
                Main.CheckRandomEncounterSettings();
                if (settings.artisanMasterpieceChance != Defaults.artisanMasterpieceChance && KingdomRoot.Instance != null) {
                    KingdomRoot.Instance.ArtisanMasterpieceChance = settings.artisanMasterpieceChance;
                }
                if (StringUtils.ToToggleBool(settings.toggleNoResourcesClaimCost) && KingdomRoot.Instance != null) {
                    KingdomRoot.Instance.DefaultMapResourceCost = 0;
                }
                //Game.Instance.BlueprintRoot.Vendors.SellModifier = settings.vendorSellPriceMultiplier;
            }
        }

        [HarmonyPatch(typeof(Game), "LoadGame")]
        public static class Game_LoadGame_Patch {
            private static void Postfix(SaveInfo saveInfo) {
                Game.Instance.BlueprintRoot.Vendors.SellModifier = settings.vendorSellPriceMultiplier;
            }
        }

                [HarmonyPatch(typeof(TimeController), "Tick")]
        static class TimeController_Tick_Patch {
            public static bool Prefix() {
                if (settings.debugTimeMultiplier != Defaults.debugTimeScale && settings.useCustomDebugTimeMultiplier == false && Game.Instance.TimeController.DebugTimeScale != settings.debugTimeMultiplier) {
                    Game.Instance.TimeController.DebugTimeScale = settings.debugTimeMultiplier;
                }

                if (settings.finalCustomDebugTimeMultiplier != Defaults.debugTimeScale && settings.useCustomDebugTimeMultiplier == true && Game.Instance.TimeController.DebugTimeScale != settings.finalCustomDebugTimeMultiplier) {
                    Game.Instance.TimeController.DebugTimeScale = settings.finalCustomDebugTimeMultiplier;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(TimeController), "Tick")]
        public static class TimeController_Tick_Patch2 {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
                var codes = new List<CodeInstruction>(instructions);
                int found = -1;

                for (int i = 0; i < codes.Count; i++) {
                    if (codes[i].opcode == OpCodes.Stfld && codes[i - 1].opcode == OpCodes.Call && codes[i - 2].opcode == OpCodes.Call && codes[i - 3].opcode == OpCodes.Conv_R8 && codes[i - 4].opcode == OpCodes.Call && codes[i - 5].opcode == OpCodes.Ldarg_0 && codes[i - 6].opcode == OpCodes.Ldfld && codes[i - 7].opcode == OpCodes.Dup) {
                        found = i;
                        break;
                    }
                }


                if (found != -1 && StringUtils.ToToggleBool(settings.toggleStopGameTime)) {
                    codes[found - 5].opcode = OpCodes.Ldc_I4_0;
                    codes[found - 4].opcode = OpCodes.Nop;
                    return codes.AsEnumerable();
                }
                else {
                    return codes.AsEnumerable();
                }


            }
        }
        [HarmonyPatch(typeof(GlobalMapTeleport), "RunAction")]
        public static class GlobalMapTeleport_RunAction_Patch {
            public static bool Prefix(ref float ___SkipHours) {
                if (StringUtils.ToToggleBool(settings.toggleStopGameTime)) {
                    ___SkipHours = 0f;

                }
                return true;
            }
        }
        [HarmonyPatch(typeof(GlobalMapTeleportLast), "RunAction")]
        public static class GlobalMapTeleportLast_RunAction_Patch {
            public static bool Prefix(ref float ___SkipHours) {
                if (StringUtils.ToToggleBool(settings.toggleStopGameTime)) {
                    ___SkipHours = 0f;

                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Game), "AdvanceGameTime")]
        public static class Game_AdvanceGameTime_Patch {
            public static bool Prefix(ref TimeSpan delta) {
                if (StringUtils.ToToggleBool(settings.toggleStopGameTime)) {
                    delta = TimeSpan.Zero;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RuleDealDamage), "ApplyDifficultyModifiers")]
        public static class Game_RollDamage_PrePatch {
            public static bool Prefix(RuleDealDamage __instance, ref int damage) {
                if (StringUtils.ToToggleBool(settings.toggleNoDamageFromEnemies) && __instance.Initiator.IsPlayersEnemy) {
                    damage = 0;
                }
                if (StringUtils.ToToggleBool(settings.togglePartyOneHitKills) && __instance.Initiator.IsPlayerFaction) {
                    UnitEntityData unit = __instance.Target;
                    damage = unit.Descriptor.Stats.HitPoints.ModifiedValue + unit.Descriptor.Stats.TemporaryHitPoints.ModifiedValue + 1;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(RuleDealDamage), "ApplyDifficultyModifiers")]
        static class RuleDealDamage_ApplyDifficultyModifiers_PostPatch {
            private static void Postfix(RuleDealDamage __instance, ref int __result) {
                if (StringUtils.ToToggleBool(settings.toggleDamageDealtMultipliers)) {
                    if (StringUtils.ToToggleBool(settings.toggleEnemiesDamageDealtMultiplier) && __instance.Initiator.IsPlayersEnemy) {
                        __result = Mathf.RoundToInt(__result * settings.enemiesDamageDealtMultiplier);
                    }
                    if (StringUtils.ToToggleBool(settings.togglePartyDamageDealtMultiplier) && __instance.Initiator.IsPlayerFaction) {
                        __result = Mathf.RoundToInt(__result * settings.partyDamageDealtMultiplier);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(DisableDeviceRestriction), "CheckRestriction")]
        public static class DisableDeviceRestriction_CheckRestriction_Patch {
            public static bool Prefix(DisableDeviceRestriction __instance, ref UnitEntityData user) {
                if (StringUtils.ToToggleBool(settings.toggleAllDoorContainersUnlocked)) {
                    DisableDeviceRestriction.DisableDeviceRestrictionData data = (DisableDeviceRestriction.DisableDeviceRestrictionData)__instance.Data;
                    data.Unlocked = true;
                    __instance.Data = data;
                }
                if (StringUtils.ToToggleBool(settings.toggleRepeatableLockPicking)) {
                    DisableDeviceRestriction.DisableDeviceRestrictionData data = (DisableDeviceRestriction.DisableDeviceRestrictionData)__instance.Data;
                    data.LastSkillRank.Clear();
                    __instance.Data = data;
                    if (StringUtils.ToToggleBool(settings.toggleRepeatableLockPickingWeariness)) {
                        user.Ensure<UnitPartWeariness>().AddWearinessHours(settings.finalRepeatableLockPickingWeariness);
                    }
                    if (StringUtils.ToToggleBool(settings.toggleRepeatableLockPickingLockPicks)) {
                        Storage.unitLockPick = user;
                        Storage.checkLockPick = true;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RuleSkillCheck), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(StatType), typeof(int) })]
        public static class RuleSkillCheck_RollResult_Patch {
            private static void Prefix([NotNull] UnitEntityData unit, StatType statType, ref int dc) {
                if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividual) && StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividualDC99)) {
                    for (int i = 0; i < settings.togglePassSkillChecksIndividualArray.Count(); i++) {
                        if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividualArray[i]) && Storage.statsSkillsDict.Union(Storage.statsSocialSkillsDict).ToDictionary(d => d.Key, d => d.Value)[Storage.individualSkillsArray[i]] == statType) {
                            if (UnitEntityDataUtils.CheckUnitEntityData(unit, (UnitSelectType)settings.indexPassSkillChecksIndividual)) {
                                dc = -99;
                            }
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(RulePartySkillCheck), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(StatType), typeof(int) })]
        public static class RulePartySkillCheckk_RollResult_Patch {
            private static void Prefix(StatType statType, ref int difficultyClass) {
                if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividual) && StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividualDC99)) {
                    for (int i = 0; i < settings.togglePassSkillChecksIndividualArray.Count(); i++) {
                        if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividualArray[i]) && Storage.statsSkillsDict.Union(Storage.statsSocialSkillsDict).ToDictionary(d => d.Key, d => d.Value)[Storage.individualSkillsArray[i]] == statType) {
                            difficultyClass = -99;
                        }
                    }
                }
            }
        }
                [HarmonyPatch(typeof(StaticEntityData), "IsPerceptionCheckPassed", MethodType.Getter)]
        public static class StaticEntityData_IsPerceptionCheckPassed_Patch {
            private static void Postfix(ref bool __result, StaticEntityData __instance) {
                if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividual)) {
                    if (StringUtils.ToToggleBool(settings.togglePassSkillChecksIndividualArray[6])) {
                        __result = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RestController), "PerformCampingChecks")]
        public static class RestController_PerformCampingChecks__Patch {
            private static void Postfix(RestController __instance) {
                if (StringUtils.ToToggleBool(settings.toggleRepeatableLockPickingLockPicks)) {
                    int newLockPicks = Storage.lockPicks;
                    int limit = 0;
                    List<UnitEntityData> partyMembers = Game.Instance.Player.Party;
                    foreach (UnitEntityData controllableCharacter in partyMembers) {
                        int baseValue = controllableCharacter.Stats.SkillThievery.BaseValue;
                        if (baseValue > 0) {
                            RuleSkillCheck skillCheck = new RuleSkillCheck(controllableCharacter, StatType.SkillThievery, Storage.lockPicksCreationDC);
                            int result = skillCheck.BaseRollResult;
                            if (result < 5) {
                                newLockPicks += 1;
                            }
                            else if (result > 5 && result < 10) {
                                newLockPicks += 2;
                            }
                            else if (result > 10 && result < 15) {
                                newLockPicks += 3;
                            }
                            else if (result > 15 && result < 20) {
                                newLockPicks += 4;
                            }
                            else if (result > 20) {
                                newLockPicks += 5;
                            }
                            limit += 5;
                        }
                    }
                    if (newLockPicks > 0) {
                        Storage.lockPicks = Mathf.Clamp(newLockPicks, 1, limit);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(BlueprintCookingRecipe), "CheckIngredients")]
        public static class BlueprintCookingRecipe_CheckIngredients_Patch {
            private static void Postfix(ref bool __result) {
                if (StringUtils.ToToggleBool(settings.toggleNoIngredientsRequired)) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(BlueprintCookingRecipe), "SpendIngredients")]
        public static class BlueprintCookingRecipe_SpendIngredients_Patch {
            public static bool Prefix(BlueprintCookingRecipe __instance) {
                if (StringUtils.ToToggleBool(settings.toggleNoIngredientsRequired)) {
                    __instance.Ingredients = new BlueprintCookingRecipe.ItemEntry[0];
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(KineticistAbilityBurnCost), "GetTotal")]
        public static class KineticistAbilityBurnCost_GetTotal_Patch {
            private static void Postfix(ref int __result) {
                if (StringUtils.ToToggleBool(settings.toggleNoBurnKineticist)) {
                    __result = 0;

                }
            }
        }
        [HarmonyPatch(typeof(AbilityAcceptBurnOnCast), "OnCast")]
        public static class UnitPartKineticistt_AcceptBurn_Patch {
            public static bool Prefix(ref int ___BurnValue) {
                if (StringUtils.ToToggleBool(settings.toggleNoBurnKineticist)) {
                    ___BurnValue = 0;
                }
                return true;

            }
        }

        [HarmonyPatch(typeof(SaveManager), "PrepareSave")]
        public static class SaveManager_PrepareSave_Patch {
            private static void Postfix(SaveInfo save) {
                if (StringUtils.ToToggleBool(settings.toggleRepeatableLockPickingLockPicks)) {
                    SaveTools.SaveFile(save);
                }
            }
        }
            [HarmonyPatch(typeof(Game), "LoadGame")]
            public static class Game_LoadGame_Patch {
                private static void Postfix(SaveInfo saveInfo) {
                    Game.Instance.BlueprintRoot.Vendors.SellModifier = settings.vendorSellPriceMultiplier;
                }
            }
        }
            [HarmonyPatch(typeof(SteamSavesReplicator), "DeleteSave")]
        public static class SteamSavesReplicator_DeleteSave_Patch {
            private static void Postfix(SaveInfo saveInfo) {
                if (StringUtils.ToToggleBool(settings.toggleRepeatableLockPickingLockPicks)) {
                    SaveTools.DeleteFile(saveInfo);
                }
            }
        }

        [HarmonyPatch(typeof(UnitPartKineticist), "HealBurn")]
        public static class UnitPartKineticist_HealBurn_Patch {
            private static void Postfix(UnitPartKineticist __instance) {
                if (StringUtils.ToToggleBool(settings.toggleMaximiseAcceptedBurn)) {
                    for (int i = __instance.AcceptedBurn; i < __instance.MaxBurn; i++) {
                        AbilityData abilityData = new AbilityData(Utilities.GetBlueprintByGuid<BlueprintAbility>("a5631955254ae5c4d9cc2d16870448a2"), __instance.Owner);
                        __instance.AcceptBurn(__instance.MaxBurn, abilityData);
                    }


                }
            }
        }
        [HarmonyPatch(typeof(UnitPartKineticist), "ClearAcceptedBurn")]
        public static class UnitPartKineticist_ClearAcceptedBurn_Patch {
            private static void Postfix(UnitPartKineticist __instance) {
                if (StringUtils.ToToggleBool(settings.toggleMaximiseAcceptedBurn)) {
                    for (int i = __instance.AcceptedBurn; i < __instance.MaxBurn; i++) {
                        AbilityData abilityData = new AbilityData(Utilities.GetBlueprintByGuid<BlueprintAbility>("a5631955254ae5c4d9cc2d16870448a2"), __instance.Owner);
                        __instance.AcceptBurn(__instance.MaxBurn, abilityData);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Inventory), "SetCharacter")]
        public static class Inventory_SetCharacter_Patch {
            private static void Postfix(Inventory __instance) {
                if (StringUtils.ToToggleBool(settings.toggleShowPetInventory)) {
                    if (GroupController.Instance.GetCurrentCharacter().Descriptor.IsPet) {
                        __instance.Placeholder.gameObject.SetActive(false);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintArmorType), "HasDexterityBonusLimit", MethodType.Getter)]
        public static class BlueprintArmorType_HasDexterityBonusLimit_Patch {
            private static void Postfix(ref bool __result) {
                if (StringUtils.ToToggleBool(settings.toggleDexBonusLimit99)) {
                    __result = false;
                }
            }
        }
        [HarmonyPatch(typeof(BlueprintArmorType), "MaxDexterityBonus", MethodType.Getter)]
        public static class BlueprintArmorType_MaxDexterityBonus_Patch {
            private static void Postfix(ref int __result) {
                if (StringUtils.ToToggleBool(settings.toggleDexBonusLimit99)) {
                    __result = 99;
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintItem), "Weight", MethodType.Getter)]
        public static class BlueprintItem_Weight_Patch {
            private static void Postfix(ref float __result) {
                if (StringUtils.ToToggleBool(settings.toggleItemsWeighZero)) {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(BlueprintArmorType), "Weight", MethodType.Getter)]
        public static class BlueprintArmorType_Weight_Patch {
            private static void Postfix(ref float __result) {
                if (StringUtils.ToToggleBool(settings.toggleItemsWeighZero)) {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(BlueprintItemArmor), "Weight", MethodType.Getter)]
        public static class BlueprintItemArmor_Weight_Patch {
            private static void Postfix(ref float __result) {
                if (StringUtils.ToToggleBool(settings.toggleItemsWeighZero)) {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(BlueprintItemShield), "Weight", MethodType.Getter)]
        public static class BlueprintItemShield_Weight_Patch {
            private static void Postfix(ref float __result) {
                if (StringUtils.ToToggleBool(settings.toggleItemsWeighZero)) {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(BlueprintItemWeapon), "Weight", MethodType.Getter)]
        public static class BlueprintItemWeapon_Weight_Patch {
            private static void Postfix(ref float __result) {
                if (StringUtils.ToToggleBool(settings.toggleItemsWeighZero)) {
                    __result = 0f;
                }
            }
        }
        [HarmonyPatch(typeof(BlueprintWeaponType), "Weight", MethodType.Getter)]
        public static class BlueprintWeaponType_Weight_Patch {
            private static void Postfix(ref float __result) {
                if (StringUtils.ToToggleBool(settings.toggleItemsWeighZero)) {
                    __result = 0f;
                }
            }
        }

        [HarmonyPatch(typeof(RuleCalculateAttacksCount), "OnTrigger")]
        public static class RuleCalculateAttacksCount_OnTrigger_Patch {
            private static void Prefix(ref RuleCalculateAttacksCount.AttacksCount ___PrimaryHand, ref RuleCalculateAttacksCount.AttacksCount ___SecondaryHand, RuleCalculateAttacksCount __instance) {
                if (__instance.Initiator.IsPlayerFaction && StringUtils.ToToggleBool(settings.toggleExtraAttacksParty)) {
                    ___PrimaryHand.AdditionalAttacks += settings.extraAttacksPartyPrimaryHand;
                    if (!__instance.Initiator.Body.SecondaryHand.HasShield) {
                        ___SecondaryHand.AdditionalAttacks += settings.extraAttacksPartySecondaryHand;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnitAlignment), "GetDirection")]
        static class UnitAlignment_GetDirection_Patch {
            static void Postfix(UnitAlignment __instance, ref Vector2 __result, AlignmentShiftDirection direction) {
                if (!Main.enabled) return;
                if (StringUtils.ToToggleBool(settings.toggleAlignmentFix)) {
                    if (direction == AlignmentShiftDirection.NeutralGood) __result = new Vector2(0, 1);
                    if (direction == AlignmentShiftDirection.NeutralEvil) __result = new Vector2(0, -1);
                    if (direction == AlignmentShiftDirection.LawfulNeutral) __result = new Vector2(-1, 0);
                    if (direction == AlignmentShiftDirection.ChaoticNeutral) __result = new Vector2(1, 0);
                }
            }
        }
        [HarmonyPatch(typeof(UnitAlignment), "Set", new Type[] { typeof(Alignment), typeof(bool) })]
        static class UnitAlignment_Set_Patch {
            static void Prefix(UnitAlignment __instance, ref Alignment alignment) {
                if (StringUtils.ToToggleBool(settings.togglePreventAlignmentChanges)) {
                    alignment = __instance.Value;
                }
            }
        }
        [HarmonyPatch(typeof(UnitAlignment), "Shift", new Type[] { typeof(AlignmentShiftDirection), typeof(int), typeof(IAlignmentShiftProvider) })]
        static class UnitAlignment_Shift_Patch {
            static bool Prefix(UnitAlignment __instance, AlignmentShiftDirection direction, ref int value, IAlignmentShiftProvider provider) {
                try {
                    if (!Main.enabled) return true;

                    if (StringUtils.ToToggleBool(settings.togglePreventAlignmentChanges)) {
                        value = 0;
                    }

                    if (StringUtils.ToToggleBool(settings.toggleAlignmentFix)) {
                        if (value == 0) {
                            return false;
                        }
                        Vector2 vector = __instance.Vector;
                        float num = (float)value / 50f;
                        var directionVector = Traverse.Create(__instance).Method("GetDirection", new object[] { direction }).GetValue<Vector2>();
                        Vector2 newAlignment = __instance.Vector + directionVector * num;
                        if (newAlignment.magnitude > 1f) {
                            //Instead of normalizing towards true neutral, normalize opposite to the alignment vector
                            //to prevent sliding towards neutral
                            newAlignment -= (newAlignment.magnitude - newAlignment.normalized.magnitude) * directionVector;
                        }
                        if (direction == AlignmentShiftDirection.TrueNeutral && (Vector2.zero - __instance.Vector).magnitude < num) {
                            newAlignment = Vector2.zero;
                        }
                        Traverse.Create(__instance).Property<Vector2>("Vector").Value = newAlignment;
                        Traverse.Create(__instance).Method("UpdateValue").GetValue();
                        //Traverse requires the parameter types to find interface parameters
                        Traverse.Create(__instance).Method("OnChanged",
                            new Type[] { typeof(AlignmentShiftDirection), typeof(Vector2), typeof(IAlignmentShiftProvider), typeof(bool) },
                            new object[] { direction, vector, provider, true }).GetValue();
                        return false;
                    }
                }
                catch (Exception e) {
                    modLogger.Log(e.ToString());
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ForbidSpellbookOnAlignmentDeviation), "CheckAlignment")]
        static class ForbidSpellbookOnAlignmentDeviation_CheckAlignment_Patch {
            static bool Prefix(ForbidSpellbookOnAlignmentDeviation __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSpellbookAbilityAlignmentChecks)) {
                    __instance.Alignment = __instance.Owner.Alignment.Value.ToMask();
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(AbilityCasterAlignment), "CorrectCaster")]
        static class AbilityCasterAlignment_CheckAlignment_Patch {
            static void Postfix(ref bool __result) {
                if (StringUtils.ToToggleBool(settings.toggleSpellbookAbilityAlignmentChecks)) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SummonPool), "Register")]
        static class SummonPool_Register_Patch {
            static void Postfix(ref UnitEntityData unit) {
                if (StringUtils.ToToggleBool(settings.toggleSetSpeedOnSummon)) {
                    unit.Descriptor.Stats.GetStat(StatType.Speed).BaseValue = settings.setSpeedOnSummonValue;
                }

                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable) && Storage.SummonedByPlayerFaction) {
                    Logger.ModLoggerDebug($"SummonPool.Register: Unit [{unit.CharacterName}] [{unit.UniqueId}]");
                    UnitEntityDataUtils.Charm(unit);

                    if (unit.Blueprint.AssetGuid == "6fdf7a3f850a1eb48bfbf44d9d0f45dd" && StringUtils.ToToggleBool(settings.toggleDisableWarpaintedSkullAbilityForSummonedBarbarians)) // WarpaintedSkullSummonedBarbarians
                    {
                        if (unit.Body.Head.HasItem && unit.Body.Head.Item?.Blueprint?.AssetGuid == "5d343648bb8887d42b24cbadfeb36991") // WarpaintedSkullItem
                        {
                            unit.Body.Head.Item.Ability.Deactivate();
                            Logger.ModLoggerDebug(unit.Body.Head.Item.Name + "'s ability active: " + unit.Body.Head.Item.Ability.Active);
                        }
                    }
                    Storage.SummonedByPlayerFaction = false;
                }

                if (StringUtils.ToToggleBool(settings.toggleRemoveSummonsGlow)) {
                    unit.Buffs.RemoveFact(Utilities.GetBlueprintByGuid<BlueprintFact>("706c182e86d9be848b59ddccca73d13e")); // SummonedCreatureVisual
                    unit.Buffs.RemoveFact(Utilities.GetBlueprintByGuid<BlueprintFact>("e4b996b5168fe284ab3141a91895d7ea")); // NaturalAllyCreatureVisual
                }
            }
        }

        [HarmonyPatch(typeof(Quest), "TimeToFail", MethodType.Getter)]
        static class Quest_HandleTimePassed_Patch {
            static void Postfix(ref TimeSpan? __result) {
                if (__result != null && StringUtils.ToToggleBool(settings.toggleFreezeTimedQuestAt90Days)) {
                    __result = TimeSpan.FromDays(90);
                }
            }
        }
        [HarmonyPatch(typeof(QuestObjective), "TimeToFail", MethodType.Getter)]
        static class QuestObjective_HandleTimePassed_Patch {
            static void Postfix(ref TimeSpan? __result) {
                if (__result != null && StringUtils.ToToggleBool(settings.toggleFreezeTimedQuestAt90Days)) {
                    __result = TimeSpan.FromDays(90);
                }
            }
        }

        [HarmonyPatch(typeof(QuestObjective), "Fail")]
        static class QuestObjective_Fail_Patch {
            static bool Prefix() {
                if (StringUtils.ToToggleBool(settings.togglePreventQuestFailure)) {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(RuleCheckCastingDefensively), "Success", MethodType.Getter)]
        static class RuleCheckCastingDefensively_Success_Patch {
            static void Postfix(ref bool __result, RuleCheckCastingDefensively __instance) {
                if (StringUtils.ToToggleBool(settings.toggleAlwaysSucceedCastingDefensively) && __instance.Initiator.IsPlayerFaction) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(RuleCheckConcentration), "Success", MethodType.Getter)]
        static class RuleCheckConcentration_Success_Patch {
            static void Postfix(ref bool __result, RuleCheckConcentration __instance) {
                if (StringUtils.ToToggleBool(settings.toggleAlwaysSucceedConcentration) && __instance.Initiator.IsPlayerFaction) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(SpellBookToggle), "SetUp")]
        static class SpellBookToggle_SetUp_Patch {
            static bool Prefix(UnitEntityData unit, SpellBookToggle __instance, List<SpellbookClassTab> ___m_Tabs) {
                if (StringUtils.ToToggleBool(settings.toggleSortSpellbooksAlphabetically)) {
                    __instance.Initialize();
                    foreach (SpellbookClassTab tab in ___m_Tabs)
                        tab.SetActive(false);
                    __instance.Spellbooks = new List<Spellbook>();
                    int i = 0;
                    __instance.Spellbooks = unit.Descriptor.Spellbooks.OrderBy(d => d.Blueprint.DisplayName).ToList();
                    foreach (Spellbook spellbook in __instance.Spellbooks) {
                        spellbook.UpdateAllSlotsSize(false);
                        if (i < ___m_Tabs.Count) {
                            ___m_Tabs[i].SetName(spellbook.Blueprint.DisplayName);
                            ___m_Tabs[i].SetIndex(i);
                            ___m_Tabs[i].Toggle.onValueChanged.AddListener(new UnityAction<bool>(__instance.OnToggle));
                            ___m_Tabs[i].SetLevel(spellbook.CasterLevel);
                            ___m_Tabs[i].SetActive(true);
                            ++i;
                        }
                        else
                            break;
                    }
                    if (__instance.Spellbooks.Count > 0)
                        __instance.SelectSpellbookByIndex(0);
                    __instance.gameObject.SetActive(__instance.Spellbooks.Count > 1);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(SpellBookView), "GetSpellsForLevel")]
        static class SpellBookView_GetSpellsForLevel_Patch {
            static void Postfix(ref List<AbilityData> __result) {
                if (StringUtils.ToToggleBool(settings.toggleSortSpellsAlphabetically)) {
                    __result = __result.OrderBy(d => d.Name).ToList();
                }
            }
        }

        [HarmonyPatch(typeof(HitPlayer), "CalcHitLevel")]
        static class HitPlayer_CalcHitLevel_Patch {
            static void Postfix(ref HitLevel __result) {
                if (StringUtils.ToToggleBool(settings.toggleSortSpellsAlphabetically)) {
                    __result = HitLevel.Crit;
                }
            }
        }

    }
        [HarmonyPatch(typeof(CampingSettings), "IsDungeon", MethodType.Getter)]
        static class CampingSettings_IsDungeon_Patch {
            static void Postfix(ref bool __result) {
                if (!Main.Enabled) {
                    return;
                }
                if (StringUtils.ToToggleBool(settings.toggleCookingAndHuntingInDungeons)) {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(SoundState), "OnAreaLoadingComplete")]
        public static class SoundState_OnAreaLoadingComplete_Patch {
            private static void Postfix() {
                Logger.ModLoggerDebug(Game.Instance.CurrentlyLoadedArea.AreaName.ToString());
                Logger.ModLoggerDebug(Game.Instance.CurrentlyLoadedArea.AssetGuid);
                Logger.ModLoggerDebug(SceneManager.GetActiveScene().name);

                if (StringUtils.ToToggleBool(settings.toggleUnlimitedCasting) && SceneManager.GetActiveScene().name == "HouseAtTheEdgeOfTime_Courtyard_Light") {
                    UIUtility.ShowMessageBox(Strings.GetText("warning_UnlimitedCasting"), DialogMessageBoxBase.BoxType.Message, new Action<DialogMessageBoxBase.BoxButton>(Common.CloseMessageBox));
                }
                if (StringUtils.ToToggleBool(settings.toggleNoDamageFromEnemies) && Game.Instance.CurrentlyLoadedArea.AssetGuid == "0ba5b24abcd5523459e54cd5877cb837") {
                    UIUtility.ShowMessageBox(Strings.GetText("warning_NoDamageFromEnemies"), DialogMessageBoxBase.BoxType.Message, new Action<DialogMessageBoxBase.BoxButton>(Common.CloseMessageBox));
                }

            }
        }

        [HarmonyPatch(typeof(EncumbranceHelper.CarryingCapacity), "GetEncumbrance")]
        [HarmonyPatch(new Type[] { typeof(float) })]
        static class EncumbranceHelperCarryingCapacity_GetEncumbrance_Patch {
            static void Postfix(ref Encumbrance __result) {
                if (StringUtils.ToToggleBool(settings.toggleSetEncumbrance)) {
                    __result = Common.IntToEncumbrance(settings.setEncumbrancIndex);
                }
            }
        }

        [HarmonyPatch(typeof(PartyEncumbranceController), "UpdateUnitEncumbrance")]
        static class PartyEncumbranceController_UpdateUnitEncumbrance_Patch {
            static void Postfix(UnitDescriptor unit) {
                if (StringUtils.ToToggleBool(settings.toggleSetEncumbrance)) {
                    unit.Encumbrance = Common.IntToEncumbrance(settings.setEncumbrancIndex);
                    unit.Remove<UnitPartEncumbrance>();
                }
            }
        }
        [HarmonyPatch(typeof(PartyEncumbranceController), "UpdatePartyEncumbrance")]
        static class PartyEncumbranceController_UpdatePartyEncumbrance_Patch {
            static bool Prefix() {
                if (StringUtils.ToToggleBool(settings.toggleSetEncumbrance)) {
                    player.Encumbrance = Common.IntToEncumbrance(settings.setEncumbrancIndex);
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(GlobalMapRules), "ChangePartyOnMap")]
        static class GlobalMapRules_ChangePartyOnMap_Patch {
            static bool Prefix() {
                if (StringUtils.ToToggleBool(settings.toggleInstantPartyChange)) {
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(IngameMenuManager), "OpenGroupManager")]
        static class IngameMenuManager_OpenGroupManager_Patch {
            static bool Prefix(IngameMenuManager __instance) {
                if (StringUtils.ToToggleBool(settings.toggleInstantPartyChange)) {
                    MethodInfo startChangedPartyOnGlobalMap = __instance.GetType().GetMethod("StartChangedPartyOnGlobalMap", BindingFlags.NonPublic | BindingFlags.Instance);
                    startChangedPartyOnGlobalMap.Invoke(__instance, new object[] { });
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BuildModeUtility), "IsDevelopment", MethodType.Getter)]
        static class BuildModeUtility_IsDevelopment_Patch {
            static void Postfix(ref bool __result) {
                if (StringUtils.ToToggleBool(settings.toggleDevTools)) {
                    __result = true;
                }
            }
        }
        [HarmonyPatch(typeof(SmartConsole), "WriteLine")]
        static class SmartConsole_WriteLine_Patch {
            static void Postfix(string message) {
                if (StringUtils.ToToggleBool(settings.toggleDevTools)) {
                    modLogger.Log(message);
                    UberLoggerAppWindow.Instance.Log(new UberLogger.LogInfo((UnityEngine.Object)null, nameof(SmartConsole), UberLogger.LogSeverity.Message, new List<UberLogger.LogStackFrame>(), (object)message, (object[])Array.Empty<object>()));

                }
            }
        }
        [HarmonyPatch(typeof(SmartConsole), "Initialise")]
        static class SmartConsole_Initialise_Patch {
            static void Postfix() {
                if (StringUtils.ToToggleBool(settings.toggleDevTools)) {
                    SmartConsoleCommands.Register();
                }
            }
        }
        [HarmonyPatch(typeof(Kingmaker.MainMenu), "Start")]
        static class MainMenu_Start_Patch {
            static void Postfix() {
                ModifiedBlueprintTools.Patch();

                if (StringUtils.ToToggleBool(settings.toggleNoTempHPKineticist)) {
                    Cheats.PatchBurnEffectBuff(0);
                }
            }
        }
        [HarmonyPatch(typeof(MainMenuButtons), "Update")]
        static class MainMenuButtons_Update_Patch {
            static void Postfix() {
                if (StringUtils.ToToggleBool(settings.toggleAutomaticallyLoadLastSave) && Storage.firstStart) {
                    Storage.firstStart = false;
                    EventBus.RaiseEvent<IUIMainMenu>((Action<IUIMainMenu>)(h => h.LoadLastGame()));
                }
                Storage.firstStart = false;
            }
        }


        [HarmonyPatch(typeof(UnitPartNegativeLevels), "Add")]
        static class UnitPartNegativeLevels_Add_Patch {
            static bool Prefix(UnitPartNegativeLevels __instance) {
                if (StringUtils.ToToggleBool(settings.toggleNoNegativeLevels) && __instance.Owner.IsPlayerFaction) {
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Kingmaker.Items.Slots.ItemSlot), "RemoveItem")]
        [HarmonyPatch(new Type[] { typeof(bool) })]
        static class ItemSlot_RemoveItem_Patch {
            static void Prefix(Kingmaker.Items.Slots.ItemSlot __instance, ItemEntity ___m_Item, UnitDescriptor ___Owner, ref ItemEntity __state) {
                if (Game.Instance.CurrentMode == GameModeType.Default && StringUtils.ToToggleBool(settings.togglAutoEquipConsumables)) {
                    __state = null;
                    if (___Owner.Body.QuickSlots.Any(x => x.HasItem && x.Item == ___m_Item)) {
                        __state = ___m_Item;
                    }
                }
            }
            static void Postfix(Kingmaker.Items.Slots.ItemSlot __instance, ItemEntity ___m_Item, UnitDescriptor ___Owner, ItemEntity __state) {
                if (Game.Instance.CurrentMode == GameModeType.Default && StringUtils.ToToggleBool(settings.togglAutoEquipConsumables)) {
                    if (__state != null) {
                        BlueprintItem blueprint = __state.Blueprint;
                        foreach (ItemEntity item in Game.Instance.Player.Inventory.Items) {
                            if (item.Blueprint.ItemType == ItemsFilter.ItemType.Usable && item.Blueprint == blueprint) {
                                __instance.InsertItem(item);
                                break;
                            }
                        }
                        __state = null;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UnitEntityData), "CreateView")]
        public static class UnitEntityData_CreateView_Patch {
            public static void Prefix(ref UnitEntityData __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref __instance);
                }
            }
        }

        [HarmonyPatch(typeof(BlueprintUnit), "PreloadResources")]
        public static class BlueprintUnit_PreloadResources_Patch {
            public static void Prefix(ref BlueprintUnit __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref __instance);
                }
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), "SpawnUnit")]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState) })]
        public static class EntityCreationControllert_SpawnUnit_Patch1 {
            public static void Prefix(ref BlueprintUnit unit) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref unit);
                }
            }
        }

        [HarmonyPatch(typeof(EntityCreationController), "SpawnUnit")]
        [HarmonyPatch(new Type[] { typeof(BlueprintUnit), typeof(UnitEntityView), typeof(Vector3), typeof(Quaternion), typeof(SceneEntitiesState) })]
        public static class EntityCreationControllert_SpawnUnit_Patch2 {
            public static void Prefix(ref BlueprintUnit unit) {
                if (StringUtils.ToToggleBool(settings.toggleSpiderBegone)) {
                    SpidersBegone.CheckAndReplace(ref unit);
                }
            }

        }

        [HarmonyPatch(typeof(ContextConditionAlignment), "CheckCondition")]
        public static class ContextConditionAlignment_CheckCondition_Patch {
            public static void Postfix(ref bool __result, ContextConditionAlignment __instance) {
                if (StringUtils.ToToggleBool(settings.toggleReverseCasterAlignmentChecks)) {
                    if (__instance.CheckCaster) {
                        __result = !__result;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RuleSummonUnit), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(UnitEntityData), typeof(BlueprintUnit), typeof(Vector3), typeof(Rounds), typeof(int) })]
        public static class RuleSummonUnit_Constructor_Patch {
            public static void Prefix(UnitEntityData initiator, BlueprintUnit blueprint, Vector3 position, ref Rounds duration, ref int level, RuleSummonUnit __instance) {
                if (StringUtils.ToToggleBool(settings.toggleSummonDurationMultiplier) && UnitEntityDataUtils.CheckUnitEntityData(initiator, (UnitSelectType)settings.indexSummonDurationMultiplier)) {
                    duration = new Rounds(Convert.ToInt32(duration.Value * settings.finalSummonDurationMultiplierValue)); ;
                }

                if (StringUtils.ToToggleBool(settings.toggleSetSummonLevelTo20) && UnitEntityDataUtils.CheckUnitEntityData(initiator, (UnitSelectType)settings.indexSetSummonLevelTo20)) {
                    level = 20;
                }

                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable)) {
                    Storage.SummonedByPlayerFaction = initiator.IsPlayerFaction;
                }

                Logger.ModLoggerDebug("Initiator: " + initiator.CharacterName + $"(PlayerFaction : {initiator.IsPlayerFaction})" + "\nBlueprint: " + blueprint.CharacterName + "\nPosition: " + position.ToString() + "\nDuration: " + duration.Value + "\nLevel: " + level);
            }
        }

        [HarmonyPatch(typeof(ActionBarManager), "CheckTurnPanelView")]
        internal static class ActionBarManager_CheckTurnPanelView_Patch {
            private static void Postfix(ActionBarManager __instance) {
                if (StringUtils.ToToggleBool(settings.toggleMakeSummmonsControllable) && CombatController.IsInTurnBasedCombat()) {
                    Traverse.Create((object)__instance).Method("ShowTurnPanel", Array.Empty<object>()).GetValue();
                }
            }
        }

        [HarmonyPatch(typeof(UberLogger.Logger), "ForwardToUnity")]
        static class UberLoggerLogger_ForwardToUnity_Patch {
            static void Prefix(ref object message) {
                if (StringUtils.ToToggleBool(settings.toggleUberLoggerForwardPrefix)) {
                    string message1 = "[UberLogger] " + message as string;
                    message = message1 as object;
                }
            }
        }


        [HarmonyPatch(typeof(DungeonStageInitializer), "Initialize")]
        static class DungeonStageInitializer_Initialize_Patch {
            static void Prefix(BlueprintDungeonArea area) {
                Logger.ModLoggerDebug("Game.Instance.Player.DungeonState.Stage: " + Game.Instance.Player.DungeonState.Stage);
            }
        }
        [HarmonyPatch(typeof(DungeonDebug), "SaveStage")]
        static class DungeonDebug_SaveStage_Patch_Pre {
            static void Prefix(string filename) {
                Logger.ModLoggerDebug("DungeonDebug.SaveStage filename: " + filename);
                Logger.ModLoggerDebug("DungeonDebug.SaveStage Path: " + Path.Combine(Application.persistentDataPath, "DungeonStages"));
            }
        }
        [HarmonyPatch(typeof(DungeonDebug), "SaveStage")]
        static class DungeonDebug_SaveStage_Patch_Post {
            static void Postfix(string filename) {
                if (settings.settingShowDebugInfo) {
                    try {
                        string str = File.ReadAllText(Path.Combine(Application.persistentDataPath, $"DungeonStages\\{filename}"));
                        modLogger.Log($"START---{filename}---START\n" + str + $"\nEND---{filename}---END");
                    }
                    catch (Exception e) {
                        modLogger.Log(e.ToString());
                    }
                }
            }
        }

        [HarmonyPatch(typeof(RuleCalculateArmorCheckPenalty), "OnTrigger")]
        public static class RuleCalculateArmorCheckPenalty_OnTrigger_Patch {
            private static bool Prefix(RuleCalculateArmorCheckPenalty __instance) {
                if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0)) {
                    if (!StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        Traverse.Create(__instance).Property("Penalty").SetValue(0);
                        return false;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly) && !__instance.Armor.Wielder.Unit.IsInCombat) {
                        Traverse.Create(__instance).Property("Penalty").SetValue(0);
                        return false;
                    }

                }
                return true;
            }
        }
        [HarmonyPatch(typeof(UIUtilityItem), "GetArmorData")]
        public static class UIUtilityItem_GetArmorData_Patch {
            private static void Postfix(ref UIUtilityItem.ArmorData __result, ref ItemEntityArmor armor) {
                if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0)) {
                    if (!StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        UIUtilityItem.ArmorData armorData = __result;
                        armorData.ArmorCheckPenalty = 0;
                        __result = armorData;
                    }
                    else if (StringUtils.ToToggleBool(settings.toggleArmourChecksPenalty0OutOfCombatOnly)) {
                        Logger.ModLoggerDebug(armor.Name);
                        if (armor.Wielder != null) {
                            Logger.ModLoggerDebug(armor.Name + ": " + armor.Wielder.CharacterName);
                            if (!armor.Wielder.Unit.IsInCombat) {
                                Logger.ModLoggerDebug(armor.Name + ": " + armor.Wielder.CharacterName + " - " + armor.Wielder.Unit.IsInCombat);
                                UIUtilityItem.ArmorData armorData = __result;
                                armorData.ArmorCheckPenalty = 0;
                                __result = armorData;
                            }
                        }
                        else {
                            Logger.ModLoggerDebug("!" + armor.Name);
                            UIUtilityItem.ArmorData armorData = __result;
                            armorData.ArmorCheckPenalty = 0;
                            __result = armorData;
                        }

                    }
                }
            }
        }


        [HarmonyPatch(typeof(BlueprintAbility), "GetRange")]
        static class BlueprintAbility_GetRange_Patch_Pre {

            private static Feet defaultClose = 30.Feet();
            private static Feet defaultMedium = 40.Feet();
            private static Feet defaultlong = 50.Feet();
            private static Feet cotwMedium = 60.Feet();
            private static Feet cotwLong = 100.Feet();
            [HarmonyPriority(Priority.Low)]
            static void Postfix(ref Feet __result) {
                if (StringUtils.ToToggleBool(settings.toggleTabletopSpellAbilityRange)) {
                    if (Main.callOfTheWild.ModIsActive()) {
                        if (__result == defaultClose) {
                            __result = 25.Feet();
                        }
                        else if (__result == cotwMedium) {
                            __result = 100.Feet();
                        }
                        else if (__result == cotwLong) {
                            __result = 400.Feet();
                        }
                    }
                    else {
                        if (__result == defaultClose) {
                            __result = 25.Feet();
                        }
                        else if (__result == defaultMedium) {
                            __result = 100.Feet();
                        }
                        else if (__result == defaultlong) {
                            __result = 400.Feet();
                        }
                    }
                }
                if (StringUtils.ToToggleBool(settings.toggleCustomSpellAbilityRange)) {
                    if (Main.callOfTheWild.ModIsActive()) {
                        if (__result == defaultClose) {
                            __result = settings.customSpellAbilityRangeClose.Feet();
                        }
                        else if (__result == cotwMedium) {
                            __result = settings.customSpellAbilityRangeMedium.Feet();
                        }
                        else if (__result == cotwLong) {
                            __result = settings.customSpellAbilityRangeLong.Feet();
                        }
                    }
                    else {
                        if (__result == defaultClose) {
                            __result = settings.customSpellAbilityRangeClose.Feet();
                        }
                        else if (__result == defaultMedium) {
                            __result = settings.customSpellAbilityRangeMedium.Feet();
                        }
                        else if (__result == defaultlong) {
                            __result = settings.customSpellAbilityRangeLong.Feet();
                        }
                    }

                }



                if (StringUtils.ToToggleBool(settings.toggleSpellAbilityRangeMultiplier)) {
                    if (settings.useCustomSpellAbilityRangeMultiplier) {
                        __result = __result * settings.customSpellAbilityRangeMultiplier;
                    }
                    else {
                        __result = __result * settings.spellAbilityRangeMultiplier;
                    }
                }
            }
        }
#endif
}
