using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using ToyBox.Multiclass;
using Alignment = Kingmaker.Enums.Alignment;
using ModKit;
using static ModKit.UI;
using ModKit.Utility;
using ToyBox.classes.Infrastructure;
using Kingmaker.PubSubSystem;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Parts;

namespace ToyBox {
    public partial class PartyEditor {
        public static Kingmaker.UnitLogic.Alignments.IAlignmentShiftProvider ToyboxAlignmentProvider { get; private set; } //Will remember to set custom description... Eventually

        public static void OnStatsGUI(UnitEntityData ch) {
            Div(100, 20, 755);
            var alignment = ch.Descriptor.Alignment.ValueRaw;
            using (HorizontalScope()) {
                Space(100);
                Label("Alignment", Width(425));
                Label($"{alignment.Name()}".color(alignment.Color()).bold(), Width(1250f));
            }
            using (HorizontalScope()) {
                Space(528);
                AlignmentGrid(alignment, (a) => ch.Descriptor.Alignment.Set(a));
            }
            Div(100, 20, 755);
            var alignmentMask = ch.Descriptor.Alignment.m_LockedAlignmentMask;
            using (HorizontalScope()) {
                Space(100);
                Label("Alignment Lock", Width(425));
                //UI.Label($"{alignmentMask.ToString()}".color(alignmentMask.Color()).bold(), UI.Width(325));
                Label($"Experimental - this sets a mask on your alignment shifts. {"Warning".bold().orange()}{": Using this may change your alignment.".orange()}".green());
            }

            using (HorizontalScope()) {
                Space(528);
                var maskIndex = Array.IndexOf(AlignmentMasks, alignmentMask);
                var titles = AlignmentMasks.Select(
                    a => a.ToString().color(a.Color()).bold()).ToArray();
                if (SelectionGrid(ref maskIndex, titles, 3, Width(800))) {
                    ch.Descriptor.Alignment.LockAlignment(AlignmentMasks[maskIndex], new Alignment?());
                }
            }
            Div(100, 20, 755);
            using (HorizontalScope()) {
                Space(100);
                Label("Alignment Value", AutoWidth());
                Space(25);
                var increment = IntTextField(ref settings.increment, null, Width(55));
                Space(150);
                ActionButton($"Add {increment}" + " Law".cyan(), () => ch.Descriptor.Alignment.Shift(Kingmaker.UnitLogic.Alignments.AlignmentShiftDirection.Lawful, increment, ToyboxAlignmentProvider), AutoWidth());
                Space(10);
                ActionButton($"Add {increment}" + " Chaos".pink(), () => ch.Descriptor.Alignment.Shift(Kingmaker.UnitLogic.Alignments.AlignmentShiftDirection.Chaotic, increment, ToyboxAlignmentProvider), AutoWidth());
                Space(10);
                ActionButton($"Add {increment}" + " Good".green(), () => ch.Descriptor.Alignment.Shift(Kingmaker.UnitLogic.Alignments.AlignmentShiftDirection.Good, increment, ToyboxAlignmentProvider), AutoWidth());
                Space(10);
                ActionButton($"Add {increment}" + " Evil".red(), () => ch.Descriptor.Alignment.Shift(Kingmaker.UnitLogic.Alignments.AlignmentShiftDirection.Evil, increment, ToyboxAlignmentProvider), AutoWidth());
            }
            Div(100, 20, 755);
            using (HorizontalScope()) {
                Space(100);
                Label("Size", Width(425));
                var size = ch.Descriptor.State.Size;
                Label($"{size}".orange().bold(), Width(175));
            }
            using (HorizontalScope()) {
                Space(528);
                EnumGrid(
                    () => ch.Descriptor.State.Size,
                    (s) => ch.Descriptor.State.Size = s,
                    3, Width(600));
            }
            using (HorizontalScope()) {
                Space(528);
                ActionButton("Reset", () => { ch.Descriptor.State.Size = ch.Descriptor.OriginalSize; }, Width(197));
            }
            Div(100, 20, 755);
            using (HorizontalScope()) {
                Space(100);
                Label("Gender", Width(400));
                Space(25);
                var gender = ch.Descriptor.CustomGender ?? ch.Descriptor.Gender;
                var isFemale = gender == Gender.Female;
                using (HorizontalScope(Width(200))) {
                    if (Toggle(isFemale ? "Female" : "Male", ref isFemale,
                        "♀".color(RGBA.magenta).bold(),
                        "♂".color(RGBA.aqua).bold(),
                        0, largeStyle, GUI.skin.box, Width(300), Height(20))) {
                        ch.Descriptor.CustomGender = isFemale ? Gender.Female : Gender.Male;
                    }
                }
                Label("Changing your gender may cause visual glitches".green());
            }
            Space(10);
            Div(100, 20, 755);
            foreach (var obj in HumanFriendly.StatTypes) {
                var statType = (StatType)obj;
                var modifiableValue = ch.Stats.GetStat(statType);
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
                    Label(statName, Width(400f));
                    Space(25);
                    ActionButton(" < ", () => {
                        modifiableValue.BaseValue -= 1;
                        storedValue = modifiableValue.BaseValue;
                    }, GUI.skin.box, AutoWidth());
                    Space(20);
                    Label($"{modifiableValue.BaseValue}".orange().bold(), Width(50f));
                    ActionButton(" > ", () => {
                        modifiableValue.BaseValue += 1;
                        storedValue = modifiableValue.BaseValue;
                    }, GUI.skin.box, AutoWidth());
                    Space(25);
                    ActionIntTextField(ref storedValue, (v) => {
                        modifiableValue.BaseValue = v;
                    }, Width(75));
                    statEditorStorage[key] = storedValue;
                }
            }
        }

    }
}