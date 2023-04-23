using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using ModKit;
using ToyBox.Inventory;

namespace ToyBox {
    public static class EnhancedInventory {
        public static Settings Settings => Main.Settings;
        private static Harmony m_harmony;
        private static OnAreaLoad m_area_load_handler;

        public static readonly Dictionary<ItemSortCategories, (int, string)> SorterCategoryMap = new Dictionary<ItemSortCategories, (int index, string title)> {
            [ItemSortCategories.NotSorted] = ((int)ItemsFilter.SorterType.NotSorted, null),
            [ItemSortCategories.TypeUp] = ((int)ItemsFilter.SorterType.TypeUp, null),
            [ItemSortCategories.TypeDown] = ((int)ItemsFilter.SorterType.TypeDown, null),
            [ItemSortCategories.PriceUp] = ((int)ItemsFilter.SorterType.PriceUp, null),
            [ItemSortCategories.PriceDown] = ((int)ItemsFilter.SorterType.PriceDown, null),
            [ItemSortCategories.NameUp] = ((int)ItemsFilter.SorterType.NameUp, null),
            [ItemSortCategories.NameDown] = ((int)ItemsFilter.SorterType.NameDown, null),
            [ItemSortCategories.DateUp] = ((int)ItemsFilter.SorterType.DateUp, null),
            [ItemSortCategories.DateDown] = ((int)ItemsFilter.SorterType.DateDown, null),
            [ItemSortCategories.WeightUp] = ((int)ItemsFilter.SorterType.WeightUp, null),
            [ItemSortCategories.WeightDown] = ((int)ItemsFilter.SorterType.WeightDown, null),
            [ItemSortCategories.WeightValueUp] = ((int)ExpandedSorterType.WeightValueUp, "Price / Weight (in ascending order)"),
            [ItemSortCategories.WeightValueDown] = ((int)ExpandedSorterType.WeightValueDown, "Price / Weight (in descending order)"),
            [ItemSortCategories.RarityUp] = ((int)ExpandedSorterType.RarityUp, "Rarity (ascending order)"),
            [ItemSortCategories.RarityDown] = ((int)ExpandedSorterType.RarityDown, "Rarity (descending order)")
        };
        public static readonly RemappableInt FilterMapper = new RemappableInt();
        public static readonly RemappableInt SorterMapper = new RemappableInt();
        public static void OnLoad() {
            m_area_load_handler = new OnAreaLoad();
            EventBus.Subscribe(m_area_load_handler);
            RefreshRemappers();
        }
        public static void OnUnLoad() {
            EventBus.Unsubscribe(m_area_load_handler);
        }
        public static void RefreshRemappers() {
            FilterMapper.Clear();
            SorterMapper.Clear();

#if false
            foreach (FilterCategories flag in EnumHelper.ValidFilterCategories) {
                if (Settings.FilterOptions.HasFlag(flag) || !Settings.EnableInventorySearchBar) {
                    FilterMapper.Add(FilterCategoryMap[flag].Item1);
                }
            }
#endif
            foreach (ItemSortCategories flag in EnumHelper.ValidSorterCategories) {
                if (Settings.InventoryItemSorterOptions.HasFlag(flag)) {
                    //Mod.Log($"{flag} {SorterCategoryMap[flag]}");
                    if (SorterCategoryMap.ContainsKey(flag))
                        SorterMapper.Add(SorterCategoryMap[flag].Item1);
                }
            }
            ItemsFilterPCView_.ReloadFilterViews();
        }
    }

    [Flags]
    public enum InventorySearchCriteria {
        ItemName = 1 << 0,
        ItemType = 1 << 1,
        ItemSubtype = 1 << 2,
        ItemDescription = 1 << 3,

        Default = ItemName | ItemType | ItemSubtype
    }

    [Flags]
    public enum SpellbookSearchCriteria {
        SpellName = 1 << 0,
        SpellDescription = 1 << 1,
        SpellSaves = 1 << 2,
        SpellSchool = 1 << 3,

        Default = SpellName | SpellSaves | SpellSchool,
    }

    [Flags]
    public enum HighlightLootableOptions {
        UnlearnedScrolls = 1 << 0,
        UnlearnedRecipes = 1 << 1,
        UnreadDocuments = 1 << 2,

        Default = UnlearnedScrolls | UnlearnedRecipes | UnreadDocuments
    }

    [Flags]
    public enum FilterCategories {
        NoFilter = 0,
        Weapon = 1 << 0,
        Armor = 1 << 1,
        Accessories = 1 << 2,
        Ingredients = 1 << 3,
        Usable = 1 << 4,
        Notable = 1 << 5,
        NonUsable = 1 << 6,
        QuickslotUtils = 1 << 7,
        UnlearnedScrolls = 1 << 8,
        UnlearnedRecipes = 1 << 9,
        UnreadDocuments = 1 << 10,
        UsableWithoutUMD = 1 << 11,
        CurrentEquipped = 1 << 12,
        NonZeroPW = 1 << 13,

        Default = Weapon |
            Armor |
            Accessories |
            Ingredients |
            Usable |
            Notable |
            NonUsable |
            QuickslotUtils |
            UnlearnedScrolls |
            UnlearnedRecipes |
            UnreadDocuments |
            UsableWithoutUMD |
            CurrentEquipped |
            NonZeroPW,
    }

    [Flags]
    public enum ItemSortCategories {
        NotSorted = 0,
        TypeUp = 1 << 0,
        TypeDown = 1 << 1,
        PriceUp = 1 << 2,
        PriceDown = 1 << 3,
        NameUp = 1 << 4,
        NameDown = 1 << 5,
        DateUp = 1 << 6,
        DateDown = 1 << 7,
        WeightUp = 1 << 8,
        WeightDown = 1 << 9,
        WeightValueUp = 1 << 10,
        WeightValueDown = 1 << 11,
        RarityUp = 1 << 12,
        RarityDown = 1 << 13,

        Default = TypeUp |
            PriceDown |
            DateDown |
            WeightDown |
            WeightValueUp |
            RarityDown
    }
    public enum ExpandedFilterType
    {
        QuickslotUtilities = 8,
        UnlearnedScrolls = 9,
        UnlearnedRecipes = 10,
        UnreadDocuments = 11,
        UsableWithoutUMD = 12,
        CurrentEquipped = 13,
        NonZeroPW = 14,
    }

    public enum ExpandedSorterType
    {
        WeightValueUp = 11,
        WeightValueDown = 12,
        RarityUp = 13,
        RarityDown = 14
    }

    public enum SpellbookFilter
    {
        NoFilter,
        TargetsFortitude,
        TargetsReflex,
        TargetsWill
    }

    public static class EnumHelper
    {
        public static IEnumerable<InventorySearchCriteria> ValidInventorySearchCriteria
            = Enum.GetValues(typeof(InventorySearchCriteria)).Cast<InventorySearchCriteria>().Where(i => i != InventorySearchCriteria.Default);

        public static IEnumerable<SpellbookSearchCriteria> ValidSpellbookSearchCriteria
            = Enum.GetValues(typeof(SpellbookSearchCriteria)).Cast<SpellbookSearchCriteria>().Where(i => i != SpellbookSearchCriteria.Default);

        public static IEnumerable<HighlightLootableOptions> ValidHighlightLootableOptions
            = Enum.GetValues(typeof(HighlightLootableOptions)).Cast<HighlightLootableOptions>().Where(i => i != HighlightLootableOptions.Default);

        public static IEnumerable<FilterCategories> ValidFilterCategories
            = Enum.GetValues(typeof(FilterCategories)).Cast<FilterCategories>().Where(i => i != FilterCategories.Default);
        public static bool IsRarityCategory(this ItemSortCategories category) => category == ItemSortCategories.RarityUp || category == ItemSortCategories.RarityDown;
        public static bool IsValid(this ItemSortCategories category) => category != ItemSortCategories.Default && (Main.Settings.UsingLootRarity || !category.IsRarityCategory());

        public static IEnumerable<ItemSortCategories> ValidSorterCategories 
            = Enum.GetValues(typeof(ItemSortCategories)).Cast<ItemSortCategories>().Where(category => category.IsValid());
    }
}
