using Kingmaker;
using Kingmaker.Blueprints;
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
        private static int _increase = 1;
        private static bool listPortraits = false;
        private static string newPortraitName = "";
        public static List<Action> OnStatsGUI(UnitEntityData ch) {
            List<Action> todo = new();
            Div(100, 20, 755);
            if (ch.UISettings.Portrait.IsCustom) {
                Label("Current Portrait ID: ".localize() + ch.UISettings.Portrait.CustomId);
            }
            else {
                Label("No Custom Portrait used!".localize());
            }
            using (HorizontalScope()) {
                Label("Enter the name of the new custom portrait you want to use: ".localize());
                TextField(ref newPortraitName);
            }
            ActionButton("Change Portrait", () => todo.Add(() => {
                if (CustomPortraitsManager.Instance.GetExistingCustomPortraitIds().Contains(newPortraitName)) {
                    ch.UISettings.SetPortraitUnsafe(null, new PortraitData(newPortraitName));
                    Mod.Debug($"Changed portrait of {ch.CharacterName} to {newPortraitName}");
                }
                else {
                    Mod.Log($"No portrait with name {newPortraitName}");
                }
            }));
            DisclosureToggle("List found Portraits", ref listPortraits);
            using (HorizontalScope()) {
                if (listPortraits) {
                    Space(15);
                    using (VerticalScope()) {
                        foreach (var customId in CustomPortraitsManager.Instance.GetExistingCustomPortraitIds()) {
                            Label(customId.ToString());
                        }
                    }
                }
            }
#if Wrath
            var alignment = ch.Descriptor().Alignment.ValueRaw;
            using (HorizontalScope()) {
                100.space();
                Label("Alignment".localize(), Width(425));
                Label($"{alignment.Name()}".color(alignment.Color()).bold(), Width(1250f));
            }
            using (HorizontalScope()) {
                528.space();
                AlignmentGrid(alignment, (a) => ch.Descriptor().Alignment.Set(a));
            }
            Div(100, 20, 755);
            using (HorizontalScope()) {
                var charAlignment = ch.Descriptor().Alignment;
                100.space();
                var text = "Shift Alignment % by".localize()?.Split('%');
                if (text.Length < 2) {
                    Label($"Shift Alignment {alignment.Acronym().color(alignment.Color()).bold()} {(charAlignment.VectorRaw * 50).ToString().Cyan()} by", 340.width());
                }
                else {
                    Label($"{text?[0]}{alignment.Acronym().color(alignment.Color()).bold()} {(charAlignment.VectorRaw * 50).ToString().Cyan()}{text?[1]}", 340.width());
                }
                5.space();
                var increment = IntTextField(ref Settings.alignmentIncrement, null, 55.width());
                var maskIndex = -1;
                20.space();
                var titles = AlignmentShiftDirections.Select(
                    a => $"{increment.ToString("+0;-#").orange()} {a.ToString().color(a.Color()).bold()}").ToArray();
                if (SelectionGrid(ref maskIndex, titles, 3, 650.width())) {
                    charAlignment.Shift(AlignmentShiftDirections[maskIndex], increment, ToyboxAlignmentProvider);
                }
            }
            Div(100, 20, 755);
            var alignmentMask = ch.Descriptor().Alignment.m_LockedAlignmentMask;
            using (HorizontalScope()) {
                100.space();
                Label("Alignment Lock".localize(), 425.width());
                //UI.Label($"{alignmentMask.ToString()}".color(alignmentMask.Color()).bold(), UI.Width(325));
                Label($"Experimental - this sets a mask on your alignment shifts. {"Warning".bold().orange()}{": Using this may change your alignment.".orange()}".localize().green());
            }
            using (HorizontalScope()) {
                528.space();
                var maskIndex = Array.IndexOf(AlignmentMasks, alignmentMask);
                var titles = AlignmentMasks.Select(
                    a => a.ToString().color(a.Color()).bold()).ToArray();
                if (SelectionGrid(ref maskIndex, titles, 3, 650.width())) {
                    ch.Descriptor().Alignment.LockAlignment(AlignmentMasks[maskIndex], new Alignment?());
                }
            }
            Div(100, 20, 755);
#elif RT
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
#endif
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
#if Wrath
            using (HorizontalScope()) {
                Space(100);
                Label("Size".localize(), Width(425));
                var size = ch.Descriptor().State.Size;
                Label($"{size}".orange().bold(), Width(175));
            }
            using (HorizontalScope()) {
                Space(528);
                EnumGrid(
                    () => ch.Descriptor().State.Size,
                    (s) => ch.Descriptor().State.Size = s,
                    3, Width(600));
            }
            using (HorizontalScope()) {
                Space(528);
                ActionButton("Reset".localize(), () => { ch.Descriptor().State.Size = ch.Descriptor().OriginalSize; }, Width(197));
            }
#endif
            using (HorizontalScope()) {
                if (ch != null && ch.HashKey() != null) {
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
#if Wrath
            if (ch.Descriptor().Progression.GetCurrentMythicClass()?.CharacterClass.Name == "Swarm That Walks") {
                UnitPartLocustSwarm SwarmPart = null;
                UnitPartLocustClonePets SwarmClones = null;
                bool found = false;
                foreach (var part in ch.Parts.Parts) {
                    var tmpPart = part as UnitPartLocustSwarm;
                    var tmpClone = part as UnitPartLocustClonePets;
                    if (tmpPart != null) {
                        found = true;
                        SwarmPart = tmpPart;
                    }
                    if (tmpClone != null) {
                        SwarmClones = tmpClone;
                    }
                }
                if (found) {
                    Div(100, 20, 755);
                    if (SwarmPart != null) {
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Swarm Power".localize(), Width(150));
                            Label("Currently:".localize() + $" {SwarmPart.CurrentStrength}/{SwarmPart.CurrentScale}".green());
                        }
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Warning:".localize().red().bold(), Width(150));
                            Label("This is not reversible.".localize().orange().bold(), Width(250));
                            Space(25);
                            ActionButton("Increase Swarm Power".localize(), () => todo.Add(() => SwarmPart.AddStrength(_increase)));
                            Space(10);
                            IntTextField(ref _increase, "", MinWidth(50), AutoWidth());
                            Space(25);
                            Label("This increases your Swarm Power by the provided value.".localize().green());
                        }
                    }
                    if (SwarmClones != null) {
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Swarm Clones".localize(), Width(150));
                            Label("Currently:".localize() + $" {SwarmClones?.m_SpawnedPetRefs?.Count}".green());
                        }
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Warning:".localize().red().bold(), Width(150));
                            Label("This is not reversible.".localize().orange().bold(), Width(250));
                            Space(25);
                            ActionButton("Remove all Clones".localize(), () => todo.Add(() => {
                                var toRemove = SwarmClones.m_SpawnedPetRefs.ToList();
                                SwarmClones.RemoveClones();
                                foreach (var clone in toRemove) {
                                    Game.Instance.Player.RemoveCompanion(clone.Value);
                                    Game.Instance.Player.DismissCompanion(clone.Value);
                                    Game.Instance.Player.DetachPartyMember(clone.Value);
                                    Game.Instance.Player.CrossSceneState.RemoveEntityData(clone.Value);

                                }

                                foreach (var buff in ch.Buffs.Enumerable.ToList()) {
                                    if (BlueprintExtensions.GetTitle(buff.Blueprint).ToLower().Contains("locustclone")) {
                                        ch.Buffs.RemoveFact(buff);
                                    }
                                }
                            }));
                        }
                    }
                }
            }
            Div(100, 20, 755);
#endif
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
#if Wrath
                    var modifiableValue = ch.Stats.GetStat(statType);
#elif RT
                    var modifiableValue = ch.Stats.GetStatOptional(statType);
#endif                    
                    if (modifiableValue == null) {
                        continue;
                    }

                    var key = $"{ch.CharacterName}-{statType}";
                    var storedValue = statEditorStorage.ContainsKey(key) ? statEditorStorage[key] : modifiableValue.BaseValue;
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
                                         storedValue = modifiableValue.BaseValue;
                                     },
                                     GUI.skin.box,
                                     AutoWidth());
                        Space(20);
                        Label($"{modifiableValue.BaseValue}".orange().bold(), Width(50f));
                        ActionButton(" > ",
                                     () => {
                                         modifiableValue.BaseValue += 1;
                                         storedValue = modifiableValue.BaseValue;
                                     },
                                     GUI.skin.box,
                                     AutoWidth());
                        Space(25);
                        ActionIntTextField(ref storedValue, (v) => { modifiableValue.BaseValue = v; }, Width(75));
                        statEditorStorage[key] = storedValue;
                    }
                }
                catch (Exception ex) {
                    //Mod.Error(ex);
                }
            }
            return todo;
        }
    }
}