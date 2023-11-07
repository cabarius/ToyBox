using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Visual.LightSelector;
using ModKit;
using ModKit.Utility;
using Owlcat.Runtime.Core.Physics.PositionBasedDynamics.Bodies;
using System;
using System.Collections.Generic;
using System.Linq;
using ToyBox.classes.Infrastructure;
using UnityEngine;
using static ModKit.UI;

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
        private static bool listCustomPortraits = false;
        private static bool listBlueprintPortraits = false;
        private static List<BlueprintPortrait> blueprintBps = null;
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
        public static void OnPortraitGUI(string customID, float scaling = 0.5f, bool isButton = true, int targetWidth = 0) {
            PortraitData portraitData = null;
            bool loaded = true;
            if (!_portraitsByID.TryGetValue(customID, out portraitData)) {
                portraitData = new PortraitData(customID);
                if (portraitData.DirectoryExists()) {
                    _portraitsByID[customID] = CustomPortraitsManager.CreatePortraitData(customID);
                }
                else {
                    loaded = false;
                }
            }
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
                    Label(BlueprintExtensions.GetTitle(portrait), MinWidth(200), AutoWidth());
                }
            }
        }
        public static List<Action> OnStatsGUI(UnitEntityData ch) {
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
                        if (Event.current.type == EventType.Layout && blueprintBps == null) {
                            blueprintBps = BlueprintLoader.Shared.GetBlueprints<BlueprintPortrait>();
                        }
                        if (blueprintBps != null) {
                            if (blueprintPortraitBrowser == null) {
                                blueprintPortraitBrowser = new(true, true, false, true);
                                blueprintPortraitBrowser.SearchLimit = 18;
                                blueprintPortraitBrowser.DisplayShowAllGUI = false;
                            }
                            blueprintPortraitBrowser.OnGUI(blueprintBps, () => blueprintBps, ID => ID, ID => BlueprintExtensions.GetSearchKey(ID), ID => new[] { BlueprintExtensions.GetSortKey(ID) }, null, null, null, 0, true, true, 100, 300, "", false, null,
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
                }
            }
            Div(100, 20, 755);
            var soulMarks = ch.GetSoulMarks();
            using (HorizontalScope()) {
                100.space();
                Label("Soul Marks".localize(), Width(200));
                using (VerticalScope()) {
                    var names = Enum.GetNames(typeof(SoulMarkDirection));
                    var index = 0;
                    foreach (var name in names) {
                        if (name == "None") continue;
                        var soulMarkDirection = (SoulMarkDirection)index;
                        var soulMark = SoulMarkShiftExtension.GetSoulMarkFor(ch, soulMarkDirection);
                        using (HorizontalScope()) {
                            Label(name.localize().orange(), 200.width());
                            var oldRank = soulMark?.GetRank() - 1 ?? 0;
                            ValueAdjuster(
                                "Rank".localize(), () => oldRank,
                                v => {
                                    var change = v - oldRank;
                                    if (Math.Abs(change) > 0) {
                                        var soulMarkShift = new SoulMarkShift {
                                            Direction = soulMarkDirection,
                                            Value = change
                                        };
                                        new BlueprintAnswer {
                                            SoulMarkShift = soulMarkShift
                                        }.ApplyShiftDialog();

                                    }
                                }, 1, 0, 120);
                        }
                        index++;
                    }
                }
            }
            using (HorizontalScope()) {
                528.space();
                //                AlignmentGrid(alignment, (a) => ch.Descriptor().Alignment.Set(a));
            }
            Div(100, 20, 755);
#if false
                                    var soulMark = SoulMarkShiftExtension.GetSoulMarkFor(ch, (SoulMarkDirection)index);
                        using (HorizontalScope()) {
                            Label(name.orange(), 200.width());
                            if (soulMark == null) continue;
                            ValueAdjuster(
                                "Rank", soulMark.GetRank,
                                v => {
                                    var oldRank = soulMark.Rank;
                                    if (v > oldRank) {
                                        while (soulMark.GetRank() < v)
                                            soulMark.AddRank();
                                    }
                                    else if (v < oldRank) {
                                        while (soulMark.GetRank() > v)
                                            soulMark.RemoveRank();

                                    }
                                }, 1, 1, 5);
                        }
#endif
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
                                         modifiableValue.UpdateValue();
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
                                         modifiableValue.UpdateValue();
                                         storedValue = modifiableValue.ModifiedValue;
                                     },
                                     GUI.skin.box,
                                     AutoWidth());
                        Space(25);
                        ActionIntTextField(ref storedValue, (v) => {

                            modifiableValue.BaseValue += v - modifiableValue.ModifiedValue;
                            storedValue = modifiableValue.ModifiedValue;
                            modifiableValue.UpdateValue();
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
    }
}
