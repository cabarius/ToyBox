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
        public static Settings settings => Main.Settings;

        //We'll use this to add new buffs
        private static List<BlueprintBuff> _buffExceptions;
        private static List<BlueprintBuff> _allBuffs;
        private static string _searchString;
        private static IEnumerable<BlueprintBuff> _searchResults;
        private static IEnumerable<BlueprintBuff> _displayedBuffs;
        private static int _pageSize = 10;
        private static int _currentPage = 0;
        private static string _paginationString;
        private static string _goToPage = "1";
        private static bool _showCurrentExceptions = true;
        private static bool _showBuffsToAdd = false;


        public static void OnGUI(
        ) {
            if (_buffExceptions == null) {
                _buffExceptions = BlueprintLoader.Shared.GetBlueprintsByGuids<BlueprintBuff>(settings.buffsToIgnoreForDurationMultiplier)
                    ?.OrderBy(b => b.GetDisplayName())
                    ?.ToList();
                _allBuffs = BlueprintLoader.Shared.GetBlueprints<BlueprintBuff>()
                    ?.Where(bp => !bp.IsHiddenInUI && !bp.HiddenInInspector && !bp.GetDisplayName().StartsWith("[unknown key") && !bp.IsClassFeature && !bp.Harmful)
                    ?.OrderBy(b => b.GetDisplayName())
                    ?.ToList();
                _searchResults = GetValidBuffsToAdd();
                _displayedBuffs = GetPaginatedBuffs();
                SetPaginationString();
            }
            VStack(null,

                () => {
                    if (BlueprintLoader.Shared.IsLoading) {
                        Label("Blueprints".orange().bold() + " loading: " + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
                    }
                    else Space(25);
                },
                () => {
                    if (BlueprintLoader.Shared.IsLoading || _searchResults == null) return;

                    using (VerticalScope()) {
                        Func<bool, string> hideOrShowString = (bool isShown) => isShown ? "Hide" : "Show";

                        DisclosureToggle($"{hideOrShowString(_showCurrentExceptions)} current list", ref _showCurrentExceptions);
                        if (_showCurrentExceptions) {
                            BuffList(_buffExceptions);
                        }

                        DisclosureToggle($"{hideOrShowString(_showBuffsToAdd)} buffs to add to list", ref _showBuffsToAdd, 175, () => FilterBuffList(_searchString));
                        if (_showBuffsToAdd) {
                            using (HorizontalScope()) {
                                Label("Search");
                                Space(25);
                                ActionTextField(ref _searchString, search => FilterBuffList(search), 300.width());
                            }
                            PaginationControl();
                            BuffList(_displayedBuffs);
                            PaginationControl();
                        }
                    }
                },
                () => { }
            );

        }

        private static void PaginationControl() {
            using (HorizontalScope()) {
                ActionButton("<", () => SetCurrentPage(_currentPage - 1));
                Space(25);
                Label(_paginationString);
                Space(25);
                ActionButton(">", () => SetCurrentPage(_currentPage + 1));
                Space(25);
                Label("Go to page: ");
                TextField(ref _goToPage, "goToPage", 40.width());
                ActionButton("Go!", () => {
                    if (int.TryParse(_goToPage, out int result)) {
                        SetCurrentPage(result - 1);
                    }
                });
            }
        }

        private static void FilterBuffList(string search) {
            var searchLower = !string.IsNullOrEmpty(search) ? search.ToLowerInvariant() : string.Empty;
            var buffList = GetValidBuffsToAdd();
            _searchResults = string.IsNullOrEmpty(_searchString)
                ? buffList
                : buffList.Where(b => 
                    b.AssetGuidThreadSafe.ToLowerInvariant() == searchLower || 
                    b.GetDisplayName().ToLowerInvariant().Contains(searchLower) ||
                    b.NameSafe().ToLowerInvariant().Contains(searchLower));
            _displayedBuffs = GetPaginatedBuffs();
            SetPaginationString();
            //This will clamp down to the range of pages, so if you search while on the last page, for example, it will place you on the max page after the search is executed.
            SetCurrentPage(_currentPage);
        }

        private static IEnumerable<BlueprintBuff> GetValidBuffsToAdd() => _allBuffs?.Where(b => !settings.buffsToIgnoreForDurationMultiplier.Contains(b.AssetGuidThreadSafe));
        private static IEnumerable<BlueprintBuff> GetPaginatedBuffs() => _searchResults?.Skip(_pageSize * _currentPage)?.Take(_pageSize);

        private static void SetPaginationString() {
            if (_searchResults == null) _paginationString = string.Empty;
            _paginationString = $"Page {_currentPage + 1} of {GetMaxPages()}";
        }

        private static void SetCurrentPage(int newPageNumber) {
            if (newPageNumber < 0) newPageNumber = 0;
            if (newPageNumber > GetMaxPages() - 1) newPageNumber = GetMaxPages() - 1;
            _currentPage = newPageNumber;
            _displayedBuffs = GetPaginatedBuffs();
            SetPaginationString();
        }

        private static int GetMaxPages() {
            if (_searchResults == null) return 1;

            return (int)Math.Ceiling((decimal)_searchResults.Count() / _pageSize);
        }

        private static void BuffList(IEnumerable<BlueprintBuff> buffs) {
            if (buffs == null) return;
            var divisor = IsWide ? 6 : 4;
            var titleWidth = ummWidth / divisor;
            var complexNameWidth = ummWidth / divisor;
            var guidWidth = ummWidth / divisor / 2;
            VStack(null, buffs?.OrderBy(b => b.GetDisplayName()).Select<BlueprintBuff, Action>(bp => () => {
                using (HorizontalScope()) {
                    Label(bp.GetDisplayName().cyan().bold(), Width(titleWidth));
                    Label(bp.NameSafe().orange().bold(), Width(complexNameWidth));
                    if (settings.showAssetIDs) {
                        ClipboardLabel(bp.AssetGuidThreadSafe, ExpandWidth(false), Width(guidWidth));
                    }
                    //It seems that if you specify defaults, saving settings without the defaults won't actually
                    //remove the items from the list. This just prevents confusion by removing the button altogether.
                    if (!settings.buffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe)) {
                        ActionButton("Add", () => {
                            AddBuff(bp.AssetGuidThreadSafe);
                        });
                    }
                    if (settings.buffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe)
                        && !SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe)) {
                        ActionButton("Remove", () => {
                            RemoveBuff(bp.AssetGuidThreadSafe);
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
                    if (settings.showAssetIDs) {
                        Label("Guid".red().bold(), Width(guidWidth));
                    }
                    Label("Description".red().bold());
                }
            })
            .Append(() => { })
            .ToArray());
        }

        private static void AddBuff(string buffGuid) {
            if (!IsValidBuff(buffGuid)) return;

            settings.buffsToIgnoreForDurationMultiplier.Add(buffGuid);
            TriggerReload();
#if DEBUG
            LogCurrentlyIgnoredBuffs();
#endif
        }

        private static void RemoveBuff(string buffGuid) {
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


        private static void LogCurrentlyIgnoredBuffs() => Mod.Log($"Currently ignored buffs: {string.Join(", ", settings.buffsToIgnoreForDurationMultiplier)}. There are {_allBuffs.Count} total buffs.");


        public static bool IsValidBuff(string buffGuid) => BlueprintLoader.Shared.GetBlueprintsByGuids<BlueprintBuff>(new[] { buffGuid }).Count() > 0;

    }
}