using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.EntitySystem.Stats.Base;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.Sound.Base;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM.VM.Tooltip.Templates;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Levelup;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility.Random;
using Kingmaker.Visual.LightSelector;
using Kingmaker.Visual.Sound;
using ModKit;
using ModKit.Utility;
using Owlcat.Runtime.Core.Physics.PositionBasedDynamics.Bodies;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using ToyBox.classes.Infrastructure;
using UnityEngine;
using UnityEngine.Profiling;
using static Kingmaker.Visual.Sound.UnitAsksComponent;
using static ModKit.UI;
using static UnityModManagerNet.UnityModManager;

namespace ToyBox {
    public partial class PartyEditor {
        public class ToyBoxAlignmentProvider : IAlignmentShiftProvider {
            AlignmentShift IAlignmentShiftProvider.AlignmentShift => new() { Description = "ToyBox Party Editor".LocalizedStringInGame() };
        }
        public static IAlignmentShiftProvider ToyboxAlignmentProvider => new ToyBoxAlignmentProvider();

        public static Dictionary<string, float> lastScaleSize = new();
        private static readonly Dictionary<string, PortraitData> _portraitsByID = new();
        private static bool _portraitsLoaded = false;
        private static int _increase = 1;
        private static Browser<string, string> portraitBrowser;
        private static Browser<BlueprintPortrait, BlueprintPortrait> blueprintPortraitBrowser;
        private static Browser<BlueprintUnitAsksList, BlueprintUnitAsksList> blueprintVoiceBrowser;
        private static bool listCustomPortraits = false;
        private static bool listCustomVoices = false;
        private static bool listBlueprintPortraits = false;
        private static List<BlueprintPortrait> blueprintPortraitBps = null;
        private static List<BlueprintUnitAsksList> blueprintVoiceBps = null;
        private static string newPortraitName = "";
        private static BlueprintPortrait newBlueprintPortrait = null;
        private static bool unknownID = false;

        public static void UnloadPortraits(bool force = false) {
            if (!force && !_portraitsLoaded) return;
            _portraitsByID.Clear();
            _portraitsLoaded = false;
            portraitBrowser = null;
            blueprintPortraitBrowser = null;
            CustomPortraitsManager.Instance.Cleanup();
        }
        public static PortraitData LoadCustomPortrait(string customID, out bool loaded) {
            try {
                PortraitData portraitData;
                loaded = true;
                if (!_portraitsByID.TryGetValue(customID, out portraitData)) {
                    portraitData = new PortraitData(customID);
                    if (portraitData.DirectoryExists()) {
                        _portraitsByID[customID] = CustomPortraitsManager.CreatePortraitData(customID);
                        return _portraitsByID[customID];
                    }
                }
            }
            catch (Exception e) {
                Mod.Log(e.ToString());
            }
            loaded = false;
            return null;
        }
        public static void OnPortraitGUI(string customID, float scaling = 0.5f, bool isButton = true, int targetWidth = 0) {
            PortraitData portraitData = LoadCustomPortrait(customID, out var loaded);
            if (loaded) {
                var sprite = portraitData.FullLengthPortrait;
                int w, h;
                if (targetWidth == 0) {
                    w = (int)(sprite.rect.width * scaling);
                    h = (int)(sprite.rect.height * scaling);
                }
                else {
                    w = targetWidth;
                    h = (int)(targetWidth * (sprite.rect.height / sprite.rect.width));
                }
                using (VerticalScope((w + 10).width())) {
                    if (isButton) {
                        if (GUILayout.Button(sprite.texture, rarityStyle, w.width(), h.height())) {
                            newPortraitName = customID;
                        }
                    }
                    else {
                        GUILayout.Label(sprite.texture, rarityStyle, w.width(), h.height());
                    }
                    Label(customID);
                }
            }
        }
        public static void OnPortraitGUI(BlueprintPortrait portrait, float scaling = 0.5f, bool isButton = true, int targetWidth = 0) {
            if (portrait != null) {
                var sprite = portrait.FullLengthPortrait;
                if (sprite == null) return;
                int w, h;
                if (targetWidth == 0) {
                    w = (int)(sprite.rect.width * scaling);
                    h = (int)(sprite.rect.height * scaling);
                }
                else {
                    w = targetWidth;
                    h = (int)(targetWidth * (sprite.rect.height / sprite.rect.width));
                }
                using (VerticalScope((w + 10).width())) {
                    if (isButton) {
                        if (GUILayout.Button(sprite.texture, rarityStyle, w.width(), h.height())) {
                            newBlueprintPortrait = portrait;
                        }
                    }
                    else {
                        GUILayout.Label(sprite.texture, rarityStyle, w.width(), h.height());
                    }
                    ActionButton("Save as png".localize(), () => {
                        try {
                            var portraitDir = new DirectoryInfo(Path.Combine(Main.path, "Portraits", portrait.name));
                            if (!portraitDir.Exists) {
                                portraitDir.Create();
                            }
                            var outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitSmallName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                            portrait.SmallPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, UnityExtensions.SaveTextureFileFormat.PNG, 100, false);
                            outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitMediumName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                            portrait.HalfLengthPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, UnityExtensions.SaveTextureFileFormat.PNG, 100, false);
                            outFile = new FileInfo(Path.Combine(portraitDir.FullName, BlueprintRoot.Instance.CharGenRoot.PortraitBigName + BlueprintRoot.Instance.CharGenRoot.PortraitsFormat));
                            portrait.FullLengthPortrait.texture.SaveTextureToFile(outFile.FullName, -1, -1, UnityExtensions.SaveTextureFileFormat.PNG, 100, false);
                            Process.Start(portraitDir.FullName);
                        }
                        catch (Exception ex) {
                            Mod.Error(ex.ToString());
                        }
                    });
                    Label(BlueprintExtensions.GetTitle(portrait), MinWidth(200), AutoWidth());
                }
            }
        }
        public static List<Action> OnStatsGUI(BaseUnitEntity ch) {
            List<Action> todo = new();
            using (HorizontalScope()) {
                100.space();
                using (VerticalScope()) {
                    if (ch.UISettings.Portrait.IsCustom) {
                        Label("Current Custom Portrait".localize());
                        OnPortraitGUI(ch.UISettings.Portrait.CustomId, 0.25f, false);
                    }
                    else {
                        Label("Current Blueprint Portrait".localize());
                        OnPortraitGUI(ch.UISettings.PortraitBlueprint, 0.25f, false, (int)(0.25f * 692));
                    }
                    Div(0, 20, 755);
                    DisclosureToggle("Show Custom Portrait Picker".localize(), ref listCustomPortraits);
                    if (listCustomPortraits) {
                        using (HorizontalScope()) {
                            Label("Name of the new Custom Portrait: ".localize(), Width(425));
                            TextField(ref newPortraitName, null, MinWidth(200), AutoWidth());
                            ActionButton("Change Portrait".localize(), () => todo.Add(() => {
                                if (CustomPortraitsManager.Instance.GetExistingCustomPortraitIds().Contains(newPortraitName)) {
                                    ch.UISettings.SetPortrait(new PortraitData(newPortraitName));
                                    Mod.Debug($"Changed portrait of {ch.CharacterName} to {newPortraitName}");
                                    unknownID = false;
                                }
                                else {
                                    Mod.Warn($"No portrait with name {newPortraitName}");
                                    unknownID = true;
                                }
                            }));
                            if (unknownID) {
                                25.space();
                                Label("Unknown ID!".localize().Red());
                            }
                        }
                        if (CustomPortraitsManager.Instance.GetExistingCustomPortraitIds() is string[] customIDs) {
                            if (portraitBrowser == null) {
                                portraitBrowser = new(true, true, false, true);
                                _portraitsLoaded = true;
                                portraitBrowser.SearchLimit = 18;
                                portraitBrowser.DisplayShowAllGUI = false;
                            }
                            portraitBrowser.OnGUI(customIDs, () => customIDs, ID => ID, ID => ID, ID => new[] { ID }, null, null, null, 0, true, true, 100, 300, "", false, null,
                                (definitions, _currentDict) => {
                                    var count = definitions.Count;
                                    using (VerticalScope()) {
                                        for (var ii = 0; ii < count;) {
                                            var tmp = ii;
                                            using (HorizontalScope()) {
                                                for (; ii < Math.Min(tmp + 6, count); ii++) {
                                                    var customID = definitions[ii];
                                                    // 6 Portraits per row; 692px per image + buffer
                                                    OnPortraitGUI(customID, (ummWidth - 100) / (6 * 780));
                                                }
                                            }
                                        }
                                    }
                                });
                        }
                    }
                    DisclosureToggle("Show Blueprint Portrait Picker".localize(), ref listBlueprintPortraits);
                    if (listBlueprintPortraits) {
                        using (HorizontalScope()) {
                            Label("Name of the new Blueprintportrait: ".localize(), Width(425));
                            if (newBlueprintPortrait != null)
                                Label(BlueprintExtensions.GetTitle(newBlueprintPortrait), MinWidth(200), AutoWidth());
                            else
                                200.space();
                            ActionButton("Change Portrait".localize(), () => todo.Add(() => {
                                if (newBlueprintPortrait != null) {
                                    ch.UISettings.SetPortrait(newBlueprintPortrait);
                                    Mod.Debug($"Changed portrait of {ch.CharacterName} to {BlueprintExtensions.GetTitle(newBlueprintPortrait)}");
                                }
                            }));
                        }
                        if (Event.current.type == EventType.Layout && blueprintPortraitBps == null) {
                            blueprintPortraitBps = BlueprintLoader.Shared.GetBlueprints<BlueprintPortrait>();
                        }
                        if (blueprintPortraitBps != null) {
                            if (blueprintPortraitBrowser == null) {
                                blueprintPortraitBrowser = new(true, true, false, true);
                                blueprintPortraitBrowser.SearchLimit = 18;
                                blueprintPortraitBrowser.DisplayShowAllGUI = false;
                            }
                            blueprintPortraitBrowser.OnGUI(blueprintPortraitBps, () => blueprintPortraitBps, ID => ID, ID => BlueprintExtensions.GetSearchKey(ID), ID => new[] { BlueprintExtensions.GetSortKey(ID) }, null, null, null, 0, true, true, 100, 300, "", false, null,
                                (definitions, _currentDict) => {
                                    var count = definitions.Count;
                                    using (VerticalScope()) {
                                        for (var ii = 0; ii < count;) {
                                            var tmp = ii;
                                            using (HorizontalScope()) {
                                                for (; ii < Math.Min(tmp + 6, count); ii++) {
                                                    // 6 Portraits per row; 692px per image + buffer
                                                    OnPortraitGUI(definitions[ii], 3.76f * ((ummWidth - 100) / (6 * 780)), true, (int)(692 * (ummWidth - 100) / (6 * 780)));
                                                }
                                            }
                                        }
                                    }
                                });
                        }
                    }
                    DisclosureToggle("Show Blueprint Voice Picker".localize(), ref listCustomVoices);
                    if (listCustomVoices) {
                        if (!(ch.IsCustomCompanion() || ch.IsMainCharacter)) {
                            Label("You're about to change the voice of a non-custom character. That's untested.".localize().red().bold());
                        } else if (!BlueprintExtensions.GetTitle(ch.Asks.List).StartsWith("RT")) {
                            Label("You have given a custom character a non-default voice. That's untested.".localize().red().bold());
                        }
                        if (blueprintVoiceBrowser?.ShowAll ?? false) {
                            Label("Giving characters voices besides the default ones is untested.".localize().red().bold());
                        }
                        if (Event.current.type == EventType.Layout && blueprintVoiceBps == null) {
                            blueprintVoiceBps = BlueprintLoader.Shared.GetBlueprints<BlueprintUnitAsksList>();
                        }
                        if (blueprintVoiceBps != null) {
                            if (blueprintVoiceBrowser == null) {
                                blueprintVoiceBrowser = new(true, true);
                                blueprintVoiceBrowser.SearchLimit = 18;
                            }
                            blueprintVoiceBrowser.OnGUI(blueprintVoiceBps.Where(v => BlueprintExtensions.GetTitle(v).StartsWith("RT")).ToList(), () => blueprintVoiceBps, ID => ID, ID => BlueprintExtensions.GetSearchKey(ID), ID => new[] { BlueprintExtensions.GetSortKey(ID) }, null,
                            (definition, _currentDict) => {
                                bool isCurrentVoice = definition == ch.Asks.List;
                                if (isCurrentVoice) {
                                    Label(BlueprintExtensions.GetTitle(definition).green(), 500.width());
                                    ActionButton("Play Example".localize(), () => {
                                        new BarkWrapper(definition.GetComponent<UnitAsksComponent>().PartyMemberUnconscious, ch.View.Asks).Schedule();
                                    }, 150.width());
                                }
                                else {
                                    Label(BlueprintExtensions.GetTitle(definition), 500.width());
                                    Space(150);
                                }
                                Space(200);
                                if (isCurrentVoice) {
                                    Label("This is the current voice!".localize());
                                }
                                else {
                                    ActionButton("Change Voice".localize(), () => {
                                        if (definition != null) {
                                            todo.Add(() => {
                                                ch.Asks.SetCustom(definition);
                                                ch.View.UpdateAsks();
                                            });
                                            Mod.Debug($"Changed voice of {ch.CharacterName} to {BlueprintExtensions.GetTitle(definition)}");
                                        }
                                    });
                                }
                            });
                        }
                    }
                }
            }
            Div(100, 20, 755);
            var soulMarks = ch.GetSoulMarks();
            using (HorizontalScope()) {
                100.space();
                Label("Soul Marks".localize(), Width(200));
                // TODO: Actually implement this for companions.
                using (VerticalScope()) {
                    foreach (SoulMarkDirection dir in Enum.GetValues(typeof(SoulMarkDirection))) {
                        if (dir == SoulMarkDirection.None || dir == SoulMarkDirection.Reason) continue;
                        SoulMark soulMark = null;
                        try {
                            soulMark = SoulMarkShiftExtension.GetSoulMarkFor(ch, dir);
                            if (soulMark == null) continue;
                            /*{        
                            var f = Shodan.MainCharacter.Blueprint.m_AddFacts.Select(f => f.Get()).OfType<BlueprintSoulMark>().Where(f => f == SoulMarkShiftExtension.GetBaseSoulMarkFor(dir)).First();
                            ch.AddFact(f);
                            soulMark = SoulMarkShiftExtension.GetSoulMarkFor(ch, dir);
                            }*/
                        }
                        catch (Exception ex) {
                            Mod.Error(ex);
                            continue;
                        }
                        using (HorizontalScope()) {
                            Label(dir.ToString().localize().orange(), 200.width());
                            ActionButton(" < ",
                                         () => modifySoulmark(dir, soulMark, ch, soulMark.Rank - 1, soulMark.Rank - 2),
                                         GUI.skin.box,
                                         AutoWidth());
                            Space(20);
                            var val = soulMark.Rank - 1;
                            Label($"{val}".orange().bold(), Width(50f));
                            ActionButton(" > ",
                                         () => modifySoulmark(dir, soulMark, ch, soulMark.Rank - 1, soulMark.Rank),
                                         GUI.skin.box,
                                         AutoWidth());
                            Space(25);
                            val = soulMark.Rank - 1;
                            ActionIntTextField(ref val, (v) => {
                                if (v > 0) {
                                    modifySoulmark(dir, soulMark, ch, soulMark.Rank - 1, v);
                                }
                            },
                                Width(75));
                        }
                    }
                }
            }
            Div(100, 20, 755);
            if (ch != null && ch.HashKey() != null) {
                using (HorizontalScope()) {
                    Space(100);
                    if (ch.View?.gameObject?.transform?.localScale[0] is float scaleMultiplier) {
                        var lastScale = lastScaleSize.GetValueOrDefault(ch.HashKey(), 1);
                        if (lastScale != scaleMultiplier) {
                            ch.View.gameObject.transform.localScale = new Vector3(lastScale, lastScale, lastScale);
                        }
                        if (LogSliderCustomLabelWidth("Visual Character Size Multiplier".localize().color(RGBA.none) + " (This setting is per-save)".localize(), ref lastScale, 0.01f, 40f, 1, 2, "", 400, AutoWidth())) {
                            Main.Settings.perSave.characterModelSizeMultiplier[ch.HashKey()] = lastScale;
                            ch.View.gameObject.transform.localScale = new Vector3(lastScale, lastScale, lastScale);
                            lastScaleSize[ch.HashKey()] = lastScale;
                            Settings.SavePerSaveSettings();
                        }
                    }
                }
            }
            using (HorizontalScope()) {
                Space(100);
                Label("Gender".localize(), Width(400));
                Space(25);
                var gender = ch.Descriptor().GetCustomGender() ?? ch.Descriptor().Gender;
                var isFemale = gender == Gender.Female;
                using (HorizontalScope(Width(200))) {
                    if (Toggle(isFemale ? "Female".localize() : "Male".localize(), ref isFemale,
                        "♀".color(RGBA.magenta).bold(),
                        "♂".color(RGBA.aqua).bold(),
                        0, largeStyle, GUI.skin.box, Width(300), Height(20))) {
                        ch.Descriptor().SetCustomGender(isFemale ? Gender.Female : Gender.Male);
                    }
                }
                Label("Changing your gender may cause visual glitches".localize().green());
            }
            Space(10);
            Div(100, 20, 755);
            foreach (var obj in HumanFriendlyStats.StatTypes) {
                try {
                    var statType = (StatType)obj;
                    Mod.Debug($"stat: {statType}");
                    var modifiableValue = ch.Stats.GetStatOptional(statType);
                    if (modifiableValue == null) {
                        continue;
                    }

                    var key = $"{ch.CharacterName}-{statType}";
                    var storedValue = statEditorStorage.ContainsKey(key) ? statEditorStorage[key] : modifiableValue.ModifiedValue;
                    var statName = statType.ToString();
                    if (statName == "BaseAttackBonus" || statName == "SkillAthletics" || statName == "HitPoints") {
                        Div(100, 20, 755);
                    }
                    using (HorizontalScope()) {
                        Space(100);
                        Label(statName.localize(), Width(400f));
                        Space(25);
                        ActionButton(" < ",
                                     () => {
                                         modifiableValue.BaseValue -= 1;
                                         storedValue = modifiableValue.ModifiedValue;
                                     },
                                     GUI.skin.box,
                                     AutoWidth());
                        Space(20);
                        var val = modifiableValue.ModifiedValue;
                        Label($"{val}".orange().bold(), Width(50f));
                        ActionButton(" > ",
                                     () => {
                                         modifiableValue.BaseValue += 1;
                                         storedValue = modifiableValue.ModifiedValue;
                                     },
                                     GUI.skin.box,
                                     AutoWidth());
                        Space(25);
                        ActionIntTextField(ref storedValue, (v) => {

                            modifiableValue.BaseValue += v - modifiableValue.ModifiedValue;
                            storedValue = modifiableValue.ModifiedValue;
                        }, Width(75));
                        statEditorStorage[key] = storedValue;
                    }
                }
                catch (Exception ex) {
                    // Mod.Error(ex);
                }
            }
            return todo;
        }

        private static void modifySoulmark(SoulMarkDirection dir, SoulMark soulMark, BaseUnitEntity ch, int oldRank, int v) {
            var change = v - oldRank;
            if (change > 0) {
                var soulMarkShift = new SoulMarkShift() { CheckByRank = false, Direction = dir, Value = change };
                new BlueprintAnswer() { SoulMarkShift = soulMarkShift }.ApplyShiftDialog();
            }
            else if (change < 0) {
                var soulMarkShift = new SoulMarkShift() { CheckByRank = false, Direction = dir, Value = change };
                var provider = new BlueprintAnswer() { SoulMarkShift = soulMarkShift };
                var source = provider as BlueprintScriptableObject;
                if (source != null) {
                    EntityFactSource entityFactSource = new EntityFactSource(source, new int?(change));
                    if (!soulMark.Sources.ToList().HasItem(entityFactSource)) {
                        soulMark.AddSource(source, change);
                        soulMark.RemoveRank(-change);
                    }
                }
                Game.Instance.DialogController.SoulMarkShifts.Add(provider.SoulMarkShift);
                EventBus.RaiseEvent<ISoulMarkShiftHandler>(delegate (ISoulMarkShiftHandler h) {
                    h.HandleSoulMarkShift(provider);
                }, true);
            }
        }
    }
}
