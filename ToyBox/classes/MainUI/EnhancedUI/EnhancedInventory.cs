using HarmonyLib;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Abilities;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox {
    public enum InventoryType {
        InventoryStash,
        Vendor,
        LootCollector,
        LootInventoryStash
    }

    public static class EnhancedInventory {
        public static Settings Settings => Main.Settings;

        public static readonly Dictionary<ItemSortCategories, (int index, string title)> SorterCategoryMap = new Dictionary<ItemSortCategories, (int index, string title)> {
            [ItemSortCategories.NotSorted] = ((int)ItemsSorterType.NotSorted, null),
            [ItemSortCategories.TypeUp] = ((int)ItemsSorterType.TypeUp, null),
            [ItemSortCategories.TypeDown] = ((int)ItemsSorterType.TypeDown, null),
            [ItemSortCategories.NameUp] = ((int)ItemsSorterType.NameUp, null),
            [ItemSortCategories.NameDown] = ((int)ItemsSorterType.NameDown, null),
            [ItemSortCategories.DateUp] = ((int)ItemsSorterType.DateUp, null),
            [ItemSortCategories.DateDown] = ((int)ItemsSorterType.DateDown, null),
            [ItemSortCategories.WeightValueUp] = ((int)ExpandedSorterType.WeightValueUp, "Price / Weight (in ascending order)"),
            [ItemSortCategories.WeightValueDown] = ((int)ExpandedSorterType.WeightValueDown, "Price / Weight (in descending order)"),
            [ItemSortCategories.RarityUp] = ((int)ExpandedSorterType.RarityUp, "Rarity (ascending order)"),
            [ItemSortCategories.RarityDown] = ((int)ExpandedSorterType.RarityDown, "Rarity (descending order)")
        };

        public static readonly Dictionary<FilterCategories, (int index, string title)> FilterCategoryMap = new Dictionary<FilterCategories, (int index, string title)> {
            [FilterCategories.NoFilter] = ((int)ItemsFilterType.NoFilter, null),
            [FilterCategories.Weapon] = ((int)ItemsFilterType.Weapon, null),
            [FilterCategories.Armor] = ((int)ItemsFilterType.Armor, null),
            [FilterCategories.Accessories] = ((int)ItemsFilterType.Accessories, null),
            [FilterCategories.Usable] = ((int)ItemsFilterType.Usable, null),
            [FilterCategories.Notable] = ((int)ItemsFilterType.Notable, null),
            [FilterCategories.NonUsable] = ((int)ItemsFilterType.NonUsable, null),
            [FilterCategories.Scroll] = ((int)ItemsFilterType.Scroll, null),
            [FilterCategories.Wand] = ((int)ItemsFilterType.Wand, null),
            [FilterCategories.Utility] = ((int)ItemsFilterType.Utility, null),
            [FilterCategories.Potion] = ((int)ItemsFilterType.Potion, null),
            [FilterCategories.Recipe] = ((int)ItemsFilterType.Recipe, null),
            [FilterCategories.Unlearned] = ((int)ItemsFilterType.Unlearned, null),
            [FilterCategories.QuickslotUtils] = ((int)ExpandedFilterType.QuickslotUtilities, "Quickslot Usable"),
            [FilterCategories.UnlearnedRecipes] = ((int)ExpandedFilterType.UnlearnedRecipes, "Unlearned Recipes"),
            [FilterCategories.UnreadDocuments] = ((int)ExpandedFilterType.UnreadDocuments, "Unread Documents"),
            [FilterCategories.UsableWithoutUMD] = ((int)ExpandedFilterType.UsableWithoutUMD, "Usable Without Magic Device Check"),
            [FilterCategories.CurrentEquipped] = ((int)ExpandedFilterType.CurrentEquipped, "Can Equip (Current Char)"),
            [FilterCategories.NonZeroPW] = ((int)ExpandedFilterType.NonZeroPW, "Non-zero price and weight"),
            [FilterCategories.UnlearnedScrolls] = ((int)ExpandedFilterType.UnlearnedScrolls, "Unlearned Scrolls")
        };

        public static readonly RemappableInt FilterMapper = new RemappableInt();
        public static readonly RemappableInt SorterMapper = new RemappableInt();
        public static void OnLoad() {
            RefreshRemappers();
        }
        public static void OnUnload() {
        }
        public static void RefreshRemappers() {
            FilterMapper.Clear();
            SorterMapper.Clear();

            foreach (FilterCategories flag in EnumHelper.ValidFilterCategories) {
                if (Settings.SearchFilterCategories.HasFlag(flag)) {
                    FilterMapper.Add(FilterCategoryMap[flag].index);
                }
            }
            foreach (ItemSortCategories flag in EnumHelper.ValidSorterCategories) {
                if (Settings.InventoryItemSorterOptions.HasFlag(flag)) {
                    //Mod.Log($"{flag} {SorterCategoryMap[flag]}");
                    if (SorterCategoryMap.ContainsKey(flag))
                        SorterMapper.Add(SorterCategoryMap[flag].index);
                }
            }
            // TODO: bring this back once we implement this for RT
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
        Default = SpellName | SpellSaves | SpellSchool
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
        Scroll = 1 << 7,
        Wand = 1 << 8,
        Utility = 1 << 9,
        Potion = 1 << 10,
        Recipe = 1 << 11,
        Unlearned = 1 << 12,
        QuickslotUtils = 1 << 13,
        UnlearnedRecipes = 1 << 14,
        UnreadDocuments = 1 << 15,
        UsableWithoutUMD = 1 << 16,
        CurrentEquipped = 1 << 17,
        NonZeroPW = 1 << 18,
        UnlearnedScrolls = 1 << 19,

        Default = Weapon |
            Armor |
            Accessories |
            Usable |
            Notable |
            NonUsable |
            Scroll |
            Wand |
            Potion |
            Recipe |
            Unlearned |
            QuickslotUtils |
            UnlearnedRecipes |
            UnreadDocuments |
            UsableWithoutUMD |
            CurrentEquipped |
            NonZeroPW |
            UnlearnedScrolls,
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
    public enum ExpandedFilterType {
        QuickslotUtilities = 14,
        UnlearnedRecipes = 15,
        UnreadDocuments = 16,
        UsableWithoutUMD = 17,
        CurrentEquipped = 18,
        NonZeroPW = 19,
        UnlearnedScrolls = 20,
    }

    public enum ExpandedSorterType {
        WeightValueUp = 11,
        WeightValueDown = 12,
        RarityUp = 13,
        RarityDown = 14
    }

    public enum SpellbookFilter {
        NoFilter,
        AOE,
        Touch,
        TargetsFortitude,
        TargetsReflex,
        TargetsWill,
        SupportsMetamagic
    }

    public static class EnumHelper {
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
