using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using static ModKit.UI;

namespace ToyBox {
    public class BuffExclusionEditor {
        public delegate void NavigateTo(params string[] argv);
        public static Settings settings => Main.Settings;

        //We'll use this to add new buffs
        private static List<BlueprintBuff> _defaultBuffExceptions;
        private static List<BlueprintBuff> _buffExceptions;
        private static List<BlueprintBuff> _allBuffs;
        private static string _searchString;
        private static IEnumerable<BlueprintBuff> _searchResults;
        private static IEnumerable<BlueprintBuff> _displayedBuffs;
        private static int _pageSize = 10;
        private static int _currentPage = 0;
        private static string? _paginationString;
        private static string _goToPage = "1";
        private static bool _showCurrentExceptions = true;
        private static bool _showDefaultExceptions = false;
        private static bool _showBuffsToAdd = false;


        public static void OnGUI(
        ) {
            if (_buffExceptions == null) {
                _buffExceptions = BlueprintLoader.Shared.GetBlueprintsByGuids<BlueprintBuff>(settings.buffsToIgnoreForDurationMultiplier)
                    ?.OrderBy(b => b.GetDisplayName())
                    ?.ToList();
                _defaultBuffExceptions = BlueprintLoader.Shared.GetBlueprintsByGuids<BlueprintBuff>(SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier)
                    ?.OrderBy(b => b.GetDisplayName())
                    ?.ToList();
                _allBuffs = BlueprintLoader.Shared.GetBlueprints<BlueprintBuff>()
                    ?.Where(bp => !bp.IsHiddenInUI
                                  && !bp.IsClassFeature
                                  && !bp.Harmful)
                    ?.OrderBy(b => b.GetDisplayName())
                    ?.ToList();
                _searchResults = GetValidBuffsToAdd();
                _displayedBuffs = GetPaginatedBuffs();
                SetPaginationString();
            }
            VStack(null,

                () => {
                    if (BlueprintLoader.Shared.IsLoading) {
                        Label(("Blueprints".orange().bold() + " loading: ").localize() + BlueprintLoader.Shared.progress.ToString("P2").cyan().bold());
                    }
                    else Space(25);
                },
                () => {
                    if (BlueprintLoader.Shared.IsLoading || _searchResults == null) return;

                    using (VerticalScope()) {
                        Func<bool, string?> hideOrShowString = (bool isShown) => isShown ? "Hide".localize() : "Show".localize();

                        DisclosureToggle($"{hideOrShowString(_showCurrentExceptions)} " + "custom exceptions".localize(), ref _showCurrentExceptions);
                        if (_showCurrentExceptions) {
                            BuffList(_buffExceptions);
                        }
                        DisclosureToggle($"{hideOrShowString(_showDefaultExceptions)} " + "default exceptions".localize(), ref _showDefaultExceptions);
                        if (_showDefaultExceptions) {
                            BuffList(_defaultBuffExceptions, true);
                        }
                        DisclosureToggle($"{hideOrShowString(_showBuffsToAdd)} " + "buffs to add to list".localize(), ref _showBuffsToAdd, 175, () => FilterBuffList(_searchString));
                        if (_showBuffsToAdd) {
                            using (HorizontalScope()) {
                                Label("Search".localize());
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
                Label("Go to page: ".localize());
                TextField(ref _goToPage, "goToPage", 40.width());
                ActionButton("Go!".localize(), () => {
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
                    BlueprintExtensions.GetSearchKey(b, true).ToLowerInvariant().Contains(searchLower) ||
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
            var text = "Page % of %".localize().Split('%');
            _paginationString = $"{text?[0]}{_currentPage + 1}{text?[1]}{GetMaxPages()}";
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

        private static void BuffList(IEnumerable<BlueprintBuff> buffs, bool showDefaults = false) {
            if (buffs == null) return;
            var divisor = IsWide ? 6 : 4;
            var titleWidth = ummWidth / divisor;
            var complexNameWidth = ummWidth / divisor;
            var guidWidth = ummWidth / divisor / 2;
            VStack(null, buffs?.Where(bp => showDefaults || !SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe))
                ?.OrderBy(BlueprintExtensions.GetSortKey).Select<BlueprintBuff, Action>(bp => () => {
                    using (HorizontalScope()) {
                        Label(BlueprintExtensions.GetTitle(bp).cyan().bold(), Width(titleWidth));
                        Label(bp.NameSafe().orange().bold(), Width(complexNameWidth));
                        if (settings.showAssetIDs) {
                            ClipboardLabel(bp.AssetGuidThreadSafe, ExpandWidth(false), Width(guidWidth));
                        }
                        //It seems that if you specify defaults, saving settings without the defaults won't actually
                        //remove the items from the list. This just prevents confusion by removing the button altogether.
                        if (!settings.buffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe)) {
                            ActionButton("Add".localize(), () => {
                                AddBuff(bp.AssetGuidThreadSafe);
                            });
                        }
                        if (settings.buffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe)
                            && !SettingsDefaults.DefaultBuffsToIgnoreForDurationMultiplier.Contains(bp.AssetGuidThreadSafe)) {
                            ActionButton("Remove".localize(), () => {
                                RemoveBuff(bp.AssetGuidThreadSafe);
                            });
                        }
                        Label(bp.GetDescription().green());
                    }
                    Space(25);
                })
            .Prepend(() => {
                using (HorizontalScope()) {
                    Label("In-Game Name".localize().red().bold(), Width(titleWidth));
                    Label("Internal Name".localize().red().bold(), Width(complexNameWidth));
                    if (settings.showAssetIDs) {
                        Label("Guid".localize().red().bold(), Width(guidWidth));
                    }
                    Label("Description".localize().red().bold());
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