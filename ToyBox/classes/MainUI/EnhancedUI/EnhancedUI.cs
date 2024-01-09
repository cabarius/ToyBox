using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.View.MapObjects;
using Kingmaker.View.MapObjects.InteractionRestrictions;
using ModKit;
using ModKit.Utility;
using System;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;
using static ModKit.UI;
#if Wrath
using ToyBox.Multiclass;
#endif

namespace ToyBox {
    public static class EnhancedUI {
        public static Settings Settings => Main.Settings;
        public static void ResetGUI() { }
        public static void OnGUI() {
            HStack("Common Tweaks".localize(),
                   1,
                   () => {
                       ActionButton("Maximize Window".localize(), Actions.MaximizeModWindow, 200.width());
                       300.space();
                       HelpLabel("Maximize the ModManager window for best ToyBox user experience".localize());
                   },
                   () => {
                       Toggle("Enhanced Map View".localize(), ref Settings.toggleZoomableLocalMaps, 500.width());
                       HelpLabel("Makes mouse zoom work for the local map (cities, dungeons, etc). Game restart required if you turn it off".localize());
                   },
                   () => {
                       Toggle("Click On Equip Slots To Filter Inventory".localize(), ref Settings.togglEquipSlotInventoryFiltering, 500.width());
                       HelpLabel($"If you tick this you can click on equipment slots to filter the inventory for items that fit in it.\nFor more {"Enhanced Inventory".orange()} and {"Spellbook".orange()} check out the {"Loot & Spellbook Tab".orange().bold()}".localize());
                   },
#if Wrath                   
                   () => {
                       Toggle("Auto Follow While Holding Camera Follow Key".localize(), ref Settings.toggleAutoFollowHold, 400.width());
                       100.space();
                       HelpLabel("When enabled and you hold down the camera follow key (usually f) the camera will keep following the unit until you release it".localize());
                   },
#endif
                   () => Toggle("Highlight Copyable Scrolls".localize(), ref Settings.toggleHighlightCopyableScrolls),
                   () => {
                       var modifier = KeyBindings.GetBinding("InventoryUseModifier");
                       var modifierText = modifier.Key == KeyCode.None ? "Modifer" : modifier.ToString();
                       Toggle("Allow ".localize() + $"{modifierText}".cyan() + (" + Click".cyan() + " To Use Items In Inventory").localize(), ref Settings.toggleShiftClickToUseInventorySlot, 470.width());
                       if (Settings.toggleShiftClickToUseInventorySlot) {
                           ModifierPicker("InventoryUseModifier", "", 0);
                       }
                   },
                   () => {
                       var modifier = KeyBindings.GetBinding("ClickToTransferModifier");
                       var modifierText = modifier.Key == KeyCode.None ? "Modifer" : modifier.ToString();
                       Toggle("Allow ".localize() + $"{modifierText}".cyan() + (" + Click".cyan() + " To Transfer Entire Stack").localize(), ref Settings.toggleShiftClickToFastTransfer, 470.width());
                       if (Settings.toggleShiftClickToFastTransfer) {
                           ModifierPicker("ClickToTransferModifier", "", 0);
                       }
                   },
                   () => {
                       Toggle("Enhanced Load/Save".localize(), ref Settings.toggleEnhancedLoadSave, 500.width());
                       HelpLabel("Adds a search field to Load/Save screen (in game only)".localize());
                   },
                   () => Toggle("Object Highlight Toggle Mode".localize(), ref Settings.highlightObjectsToggle),
                   () => {
                       Toggle("Mark Interesting NPCs".localize(), ref Settings.toggleShowInterestingNPCsOnLocalMap, 500.width());
                       HelpLabel("This will change the color of NPC names on the highlike makers and change the color map markers to indicate that they have interesting or conditional interactions".localize());
                   },
                   () => Toggle("Make Spell/Ability/Item Pop-Ups Wider ".localize(), ref Settings.toggleWidenActionBarGroups),
                   () => {
                       if (Toggle("Show Acronyms in Spell/Ability/Item Pop-Ups".localize(), ref Settings.toggleShowAcronymsInSpellAndActionSlots)) {
                           Main.SetNeedsResetGameUI();
                       }
                   },
                   () => {
                       Toggle("Make Puzzle Symbols More Clear".localize(), ref Settings.togglePuzzleRelief);
                       25.space();
                       HelpLabel(("ToyBox Archeologists can tag confusing puzzle pieces with green numbers in the game world and for inventory tool tips it will show text like this: " + "[PuzzlePiece Green3x1]".yellow().bold() + "\nNOTE: ".orange().bold() + "Needs game restart to take efect".orange()).localize());
                   },
#if Wrath
                   () => {
                       ActionButton("Clear Action Bar".localize(), () => Actions.ClearActionBar());
                       50.space();
                       Label("Make sure you have auto-fill turned off in settings or else this will just reset to default".localize().green());
                   },
#endif
                   () => ActionButton("Fix Incorrect Main Character".localize(),
                                      () => {
                                          var probablyPlayer = Game.Instance.Player?.Party?
                                                                   .Where(x => !x.IsCustomCompanion())
                                                                   .Where(x => !x.IsStoryCompanion()).ToList();
                                          if (probablyPlayer is { Count: 1 }) {
                                              var newMainCharacter = probablyPlayer.First();
                                              Mod.Warn($"Promoting {newMainCharacter.CharacterName} to main character!");
                                              if (Game.Instance != null) Game.Instance.Player.MainCharacter = newMainCharacter;
                                          }
                                      },
                                      AutoWidth()),
                   () => { }
                );
            Div(0, 25);
            HStack("Loot Rarity Coloring".localize(),
                   1,
                   () => {
                       using (VerticalScope(300.width())) {
                           Toggle("Show Rarity Tags".localize(), ref Settings.toggleShowRarityTags);
                           Toggle("Color Item Names".localize(), ref Settings.toggleColorLootByRarity);
                       }
                       using (VerticalScope()) {
                           Label($"This makes loot function like Diablo or Borderlands. {"Note: turning this off requires you to save and reload for it to take effect.".orange()}".localize()
                                     .green());
                       }
                   },
                   () => {
                       if (Settings.UsingLootRarity) {
                           using (VerticalScope(400.width())) {
                               Label("Minimum Rarity For Loot Rarity Tags/Colors".localize().cyan(), AutoWidth());
                               RarityGrid(ref Settings.minRarityToColor, 4, AutoWidth());
                           }
                       }
                   },
                   () => {
                       if (Settings.UsingLootRarity) {
                           using (VerticalScope(300)) {
                               using (HorizontalScope(300)) {
                                   using (VerticalScope()) {
                                       Label("Maximum Rarity To Hide:".localize().cyan(), AutoWidth());
                                       RarityGrid(ref Settings.maxRarityToHide, 4, AutoWidth());
                                   }
                               }
                           }
                           50.space();
                           using (VerticalScope()) {
                               Label("");
                               HelpLabel($"This hides map pins of loot containers containing at most the selected rarity. {"Note: Changing settings requires reopening the map.".orange()}".localize());
                           }
                       }
                   },
                   () => { }
                );
            Div(0, 25);
            EnhancedCamera.OnGUI();
            Div(0, 25);
            // TODO: Update EnumHelper.ValidFilterCategories for RT
#if Wrath
            HStack("Enhanced Inventory".localize(),
                   1,
                   () => {
                       using (VerticalScope()) {
                           using (HorizontalScope()) {
                               if (Toggle("Enable Enhanced Inventory".localize(), ref Settings.toggleEnhancedInventory, 300.width()))
                                   EnhancedInventory.RefreshRemappers();
                               25.space();
                               Label("Selected features revived from Xenofell's excellent mod".localize().green());
                           }
                       }
                   },
                   () => {
                       if (!Settings.toggleEnhancedInventory) return;
                       using (VerticalScope()) {
                           Rect divRect;
                           using (HorizontalScope()) {
                               Toggle("Always Keep Search Filter Active".localize(), ref Settings.toggleDontClearSearchWhenLoseFocus, 300.width());
                               25.space();
                               HelpLabel(("When ticked, this keeps your search active when you click to dismiss the Search Bar. This allows you to apply the search to different item categories.\n" + "Untick this if you wish for the standard game behavior where it clears your search".orange()).localize());
                           }
                           using (HorizontalScope()) {
                               Label("Enabled Sort Categories".localize().Cyan(), 300.width());
                               25.space();
                               HelpLabel("Here you can choose which Sort Options appear in the popup menu".localize());
                               divRect = DivLastRect();
                           }
                           var hscopeRect = DivLastRect();
                           Div(hscopeRect.x, 0, divRect.x + divRect.width - hscopeRect.x);
                           ItemSortCategories new_options = ItemSortCategories.NotSorted;
                           var selectableCategories = EnumHelper.ValidSorterCategories.Where(i => i != ItemSortCategories.NotSorted).ToList();
                           var changed = false;
                           Table(selectableCategories,
                                 (flag) => {
                                     //Mod.Log($"            {flag.ToString()}");
                                     if (flag == ItemSortCategories.NotSorted || flag == ItemSortCategories.Default)
                                         return;
                                     bool isSet = Settings.InventoryItemSorterOptions.HasFlag(flag);
                                     using (HorizontalScope(250)) {
                                         30.space();
                                         if (Toggle($"{(EnhancedInventory.SorterCategoryMap[flag].Item2 ?? flag.ToString()).localize()}", ref isSet)) changed = true;
                                     }
                                     if (isSet) {
                                         new_options |= flag;
                                     }
                                 },
                                 2,
                                 null,
                                 375.width());
                           65.space(() => ActionButton("Use Default".localize(), () => new_options = ItemSortCategories.Default));
                           Settings.InventoryItemSorterOptions = new_options;
                           if (changed) EnhancedInventory.RefreshRemappers();
                       }
                   },
                   () => {
                       if (!Settings.toggleEnhancedInventory) return;
                       using (VerticalScope()) {
                           Rect divRect;
                           using (HorizontalScope()) {
                               Label("Enabled Search Filters".localize().Cyan(), 300.width());
                               25.space();
                               HelpLabel("Here you can choose which Search filters appear in the popup menu".localize());
                               divRect = DivLastRect();
                           }
                           var hscopeRect = DivLastRect();
                           Div(hscopeRect.x, 0, divRect.x + divRect.width - hscopeRect.x);
                           FilterCategories new_options = default;
                           var selectableFilters = EnumHelper.ValidFilterCategories.Where(i => i != FilterCategories.NoFilter).ToList();
                           var changed = false;
                           Table(selectableFilters,
                                 (flag) => {
                                     //Mod.Log($"            {flag.ToString()}");
                                     bool isSet = Settings.SearchFilterCategories.HasFlag(flag);
                                     using (HorizontalScope(250)) {
                                         30.space();
                                         if (Toggle($"{(EnhancedInventory.FilterCategoryMap[flag].Item2 ?? flag.ToString()).localize()}", ref isSet)) changed = true;
                                     }
                                     if (isSet) {
                                         new_options |= flag;
                                     }
                                 },
                                 2,
                                 null,
                                 375.width());
                           65.space(() => ActionButton("Use Default".localize(), () => new_options = FilterCategories.Default));
                           Settings.SearchFilterCategories = new_options;
                           if (changed) EnhancedInventory.RefreshRemappers();
                       }
                   });
            Div(0, 25);
            HStack("Spellbook".localize(),
                   1,
                   () => {
                       if (Toggle("Enable Enhanced Spellbook".localize(), ref Settings.toggleEnhancedSpellbook, 300.width()))
                           EnhancedInventory.RefreshRemappers();
                       25.space();
                       Label("Various spellbook enhancements revived from Xenofell's excellent mod".localize().green());
                   },
                   () => {
                       if (Settings.toggleEnhancedSpellbook) {
                           using (VerticalScope()) {
                               Toggle("Give the search bar focus when opening the spellbook screen".localize(), ref Settings.toggleSpellbookSearchBarFocusWhenOpening);
                               Toggle("Show all spell levels by default".localize(), ref Settings.toggleSpellbookShowAllSpellsByDefault);
                               //Toggle("Show metamagic by default", ref Settings.toggleSpellbookShowMetamagicByDefault);
                               Toggle("Show the empty grey metamagic circles above spells".localize(), ref Settings.toggleSpellbookShowEmptyMetamagicCircles);
                               Toggle("Show level of the spell when the spellbook is showing all spell levels".localize(), ref Settings.toggleSpellbookShowLevelWhenViewingAllSpells);
                               Toggle("After creating a metamagic spell, switch to the metamagic tab".localize(), ref Settings.toggleSpellbookAutoSwitchToMetamagicTab);
                               15.space();
                               Rect divRect;
                               using (HorizontalScope()) {
                                   Label("Spellbook Search Criteria".localize().Cyan(), 300.width());
                                   25.space();
                                   HelpLabel("Here you can choose which Search filters appear in the spellbook search popup menu".localize());
                                   divRect = DivLastRect();
                               }
                               var hscopeRect = DivLastRect();
                               Div(hscopeRect.x, 0, divRect.x + divRect.width - hscopeRect.x);
                               SpellbookSearchCriteria new_options = default;
                               var changed = false;
                               var spellbookFilterCategories = EnumHelper.ValidSpellbookSearchCriteria.ToList();
                               Table(spellbookFilterCategories,
                                     (flag) => {
                                         //Mod.Log($"            {flag.ToString()}");
                                         bool isSet = Settings.SpellbookSearchCriteria.HasFlag(flag);
                                         using (HorizontalScope(250)) {
                                             30.space();
                                             if (Toggle($"{flag.ToString().localize()}", ref isSet)) changed = true;
                                         }
                                         if (isSet) {
                                             new_options |= flag;
                                         }
                                     },
                                     2,
                                     null,
                                     375.width());
                               65.space(() => ActionButton("Use Default".localize(), () => new_options = SpellbookSearchCriteria.Default));
                               Settings.SpellbookSearchCriteria = new_options;
                               if (changed) EnhancedInventory.RefreshRemappers();
                           }
                       }
                   },
                   () => { });
#endif
        }
    }

}
