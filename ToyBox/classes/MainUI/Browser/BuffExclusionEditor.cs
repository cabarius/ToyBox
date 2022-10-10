using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using ModKit;
using static ModKit.UI;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace ToyBox {
    public class BuffExclusionEditor {
        public delegate void NavigateTo(params string[] argv);
        public static Settings settings => Main.settings;

        //We'll use this to add new buffs
        private static string _userSuppliedGuid = "";
        private static string _lastValidatedGuid = "";
        private static List<BlueprintBuff> _buffExceptions;


        public static void OnGUI(
        ) {
            if (_buffExceptions == null) {
                _buffExceptions = BlueprintLoader.Shared.GetBlueprintsByGuids<BlueprintBuff>(settings.buffsToIgnoreForDurationMultiplier)?.ToList();
            }
            VStack(null,

                () => {
                    if (BlueprintLoader.Shared.IsLoading) {
                        Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
                    }
                    else Space(25);
                },
                () => {
                    if (BlueprintLoader.Shared.IsLoading || _buffExceptions == null) return;

                    using (VerticalScope()) {
                        AddBuffGui();
                        if (_buffExceptions != null) {
                            BuffList(_buffExceptions);
                        }
                    }
                },
                () => { }
            );

        }

        private static void AddBuffGui() {
            using (VerticalScope()) {
                Label("This section is for excluding (usually harmful) buffs from the buff duration multiplier, since not all harmful buffs are properly tagged as harmful. To add a buff, first find its GUID, which can be done either in-game (with the settings -> \"display guids in most tooltips\" option turned on) or by using Search n' Pick. Once you've added a GUID, click validate. This will verify that the GUID belongs to a valid buff, and reveal an \"Add\" button. Then just click the button to exclude the buff from the duration multiplier!"
                    .orange().bold());
                Space(10);
                Label("Note: the defaults for this list cannot be removed, and have their \"Remove\" buttons hidden."
                    .orange().bold());
                Space(25);
                HStack("Add a new buff", 1, () => {
                    Label("GUID of Buff:");
                    Space(25);
                    TextField(ref _userSuppliedGuid, null, Width(300));
                    ActionButton("Validate", () => {
                        if (IsValidBuff(_userSuppliedGuid)) _lastValidatedGuid = _userSuppliedGuid;
                    });
                    if (!string.IsNullOrEmpty(_lastValidatedGuid) && _userSuppliedGuid == _lastValidatedGuid) {
                        ActionButton("Add", () => {
                            AddABuff(_userSuppliedGuid);
                            _lastValidatedGuid = "";
                            _userSuppliedGuid = "";
                        });
                        Space(25);
                        Label("It's a valid Buff! Press \"Add\" to add it to the list.".green());
                    }

                });
            }

        }

        private static void BuffList(IEnumerable<BlueprintBuff> buffs) {
            var divisor = IsWide ? 6 : 4;
            var titleWidth = ummWidth / divisor;
            var complexNameWidth = ummWidth / divisor;
            VStack(null, buffs?.OrderBy(b => b.GetDisplayName()).Select<BlueprintBuff, Action>(bp => () => {
                using (HorizontalScope()) {
                    Label(bp.GetDisplayName().cyan().bold(), Width(titleWidth));
                    Label(bp.NameSafe().orange().bold(), Width(complexNameWidth));
                    Space(25);
                    GUILayout.TextField(bp.AssetGuidThreadSafe, ExpandWidth(false));
                    if (!SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe)) {
                        //It seems that if you specify defaults, saving settings without the defaults won't actually
                        //remove the items from the list. This just prevents confusion by removing the button altogether.
                        ActionButton("Remove", () => {
                            RemoveABuff(bp.AssetGuidThreadSafe);
                        });
                    }
                    Label(bp.GetDescription().green());
                }
                Space(25);
            })
            .Prepend(() => {
                using (HorizontalScope()) {
                    Label("In-Game Name".red().bold(), Width(titleWidth));
                    Label("Internal Name".red().bold(), Width(complexNameWidth));
                }
            })
            .Append(() => { })
            .ToArray());
        }

        private static void AddABuff(string buffGuid) {
            if (!IsValidBuff(buffGuid)) return;

            settings.buffsToIgnoreForDurationMultiplier.Add(buffGuid);
            TriggerReload();
#if DEBUG
            LogCurrentlyIgnoredBuffs();
#endif
        }

        private static void RemoveABuff(string buffGuid) {
            if (!settings.buffsToIgnoreForDurationMultiplier.Contains(buffGuid)) return;

            settings.buffsToIgnoreForDurationMultiplier.Remove(buffGuid);
            TriggerReload();
#if DEBUG
            LogCurrentlyIgnoredBuffs();
#endif
        }

        private static void TriggerReload() {
            _buffExceptions = null;
        }


        private static void LogCurrentlyIgnoredBuffs() => Mod.Log($"Currently ignored buffs: {string.Join(", ", settings.buffsToIgnoreForDurationMultiplier)}");


        public static bool IsValidBuff(string buffGuid) => BlueprintLoader.Shared.GetBlueprintsByGuids<BlueprintBuff>(new[] { buffGuid }).Count() > 0;

    }
}