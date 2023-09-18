// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Quests;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.Utility;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.AreaLogic.Cutscenes;
using ModKit;
using static ModKit.UI;
using ModKit.Utility;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.AI.Blueprints;
using ModKit.DataViewer;
using Kingmaker;
using ModKit.Utility.Extensions;
using Kingmaker.UnitLogic;
#if Wrath
using Kingmaker.Armies.Blueprints;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Craft;
using Kingmaker.Kingdom.Blueprints;
using Kingmaker.AI.Blueprints.Considerations;
using Kingmaker.Crusade.GlobalMagic;
#elif RT 
using Kingmaker.Globalmap.Blueprints.SectorMap;
#endif
using static ToyBox.BlueprintExtensions;

namespace ToyBox {
    public static class SearchAndPick {
        public static Settings Settings => Main.Settings;

        public static IEnumerable<SimpleBlueprint> bps = null;
        public static bool hasRepeatableAction;
        public static int maxActions = 0;
        public static int collationPickerPageSize = 30;
        // Need to cache the collators; if not then certain Category changes can lead to Cast Errors
        // Example All -> Spellbooks
        public static Dictionary<Type, Func<SimpleBlueprint, List<string>>> collatorCache = new();
        public static Dictionary<string, string> keyToDisplayName = new();
        public static int collationPickerPageCount => (int)Math.Ceiling((double)collationKeys?.Count / collationPickerPageSize);
        public static int collationPickerCurrentPage = 1;
        public static int repeatCount = 1;
        public static int selectedCollationIndex = 0;
        public static string collationSearchText = "";
        public static string parameter = "";
        public static bool needsRedoKeys = true;
        public static int bpCount = 0;
        public static List<string> collationKeys;
        public static UnitReference selectedUnit;
        public static Browser<SimpleBlueprint, SimpleBlueprint> SearchAndPickBrowser = new(Mod.ModKitSettings.searchAsYouType);
        public static int[] ParamSelected = new int[1000];
#if Wrath
        public static Dictionary<BlueprintParametrizedFeature, string[]> paramBPValueNames = new() { };
#endif
        public static Dictionary<BlueprintFeatureSelection, string[]> selectionBPValuesNames = new() { };

        private static readonly NamedTypeFilter[] blueprintTypeFilters = new NamedTypeFilter[] {
            new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(
#if DEBUG
                // Whatever is collated here results in roughly 14k Collation Keys in Wrath.
                bp.m_AllElements?.OfType<Condition>()?.Select(e => e.GetCaption() ?? "")?.ToArray() ?? new string[] {}
#endif
                )),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.Collat`ionNames(bp.m_AllElements?.Select(e => e.GetType().Name).ToArray() ?? new string[] {})),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(bp.m_AllElements?.Select(e => e.ToString().TrimEnd(digits)).ToArray() ?? new string[] {})),
            //new NamedTypeFilter<SimpleBlueprint>("All", null, bp => bp.CollationNames(bp.m_AllElements?.Select(e => e.name.Split('$')[1].TrimEnd(digits)).ToArray() ?? new string[] {})),
            new NamedTypeFilter<BlueprintFact>("Facts", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintFeature>("Features", null, bp => bp.CollationNames( 
#if Wrath
                                                      bp.Groups.Select(g => g.ToString()).ToArray()
#endif
                                                      )),
#if Wrath
            new NamedTypeFilter<BlueprintParametrizedFeature>("ParamFeatures", null, bp => new List<string?> {bp.ParameterType.ToString() }),
            new NamedTypeFilter<BlueprintFeatureSelection>("Feature Selection", null, bp => bp.CollationNames(bp.Group.ToString(), bp.Group2.ToString())),
#endif
            new NamedTypeFilter<BlueprintCharacterClass>("Classes", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintProgression>("Progression", null, bp => bp.Classes.Select(cl => cl.Name).ToList()),
            new NamedTypeFilter<BlueprintArchetype>("Archetypes", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Abilities", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintAbility>("Spells", bp => bp.IsSpell, bp => bp.CollationNames(bp.School.ToString())),
#if Wrath

            new NamedTypeFilter<BlueprintGlobalMagicSpell>("Global Spells", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintBrain>("Brains", null, bp => bp.CollationNames()),
#if Wrath
            new NamedTypeFilter<BlueprintAiAction>("AIActions", null, bp => bp.CollationNames()),
            new NamedTypeFilter<Consideration>("Considerations", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintAbilityResource>("Ability Rsrc", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintSpellbook>("Spellbooks", null, bp => bp.CollationNames(bp.CharacterClass.Name.ToString())),
            new NamedTypeFilter<BlueprintBuff>("Buffs", null, bp => bp.CollationNames()),
#if Wrath
            new NamedTypeFilter<BlueprintKingdomBuff>("Kingdom Buffs", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintItem>("Item", null,  (bp) => {
                if (bp.m_NonIdentifiedNameText?.ToString().Length > 0) return bp.CollationNames(bp.m_NonIdentifiedNameText);
                return bp.CollationNames(bp.ItemType.ToString());
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equipment", null, (bp) =>  bp.CollationNames(bp.ItemType.ToString(), $"{bp.GetCost().ToBinString("⊙".yellow())}")),
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (rarity)", null, (bp) => new List<string?> {bp.Rarity().GetString() }),
            new NamedTypeFilter<BlueprintItemWeapon>("Weapons", null, (bp) => {
#if Wrath
                var type = bp.Type;
                var category = type?.Category;
                if (category != null) return bp.CollationNames(category.ToString(), $"{bp.GetCost().ToBinString("⊙".yellow())}");
                if (type != null) return bp.CollationNames(type.NameSafe(), $"{bp.GetCost().ToBinString("⊙".yellow())}");
#elif RT
                var family = bp.Family;
                var category = bp.Category;
                return bp.CollationNames(family.ToString(), category.ToString(), $"{bp.GetCost().ToBinString("⊙".yellow())}");
#endif
                return bp.CollationNames("?", $"{bp.GetCost().ToBinString("⊙".yellow())}");
                }),
            new NamedTypeFilter<BlueprintItemArmor>("Armor", null, (bp) => {
                var type = bp.Type;
                if (type != null) return bp.CollationNames(type.DefaultName, $"{bp.GetCost().ToBinString("⊙".yellow())}");
                return bp.CollationNames("?", $"{bp.GetCost().ToBinString("⊙".yellow())}");
                }),
            new NamedTypeFilter<BlueprintItemEquipmentUsable>("Usable", null, bp => bp.CollationNames(bp.SubtypeName, $"{bp.GetCost().ToBinString("⊙".yellow())}")),
#if Wrath
            new NamedTypeFilter<BlueprintIngredient>("Ingredient", null, bp => bp.CollationNames()),
#endif
            new NamedTypeFilter<BlueprintUnit>("Units", null, bp => bp.CollationNames(bp.Type?.Name ?? bp.Race?.Name ?? "?", $"CR{bp.CR}")),
            new NamedTypeFilter<BlueprintUnit>("Units CR", null, bp => bp.CollationNames($"CR {bp.CR}")),
            new NamedTypeFilter<BlueprintRace>("Races", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintArea>("Areas", null, bp => bp.CollationNames()),
            //new NamedTypeFilter<BlueprintAreaPart>("Area Parts", null, bp => bp.CollationName()),
            new NamedTypeFilter<BlueprintAreaEnterPoint>("Area Entry", null, bp =>bp.CollationNames(bp.m_Area.NameSafe())),
            //new NamedTypeFilter<BlueprintAreaEnterPoint>("AreaEntry ", null, bp => bp.m_Tooltip.ToString()),
#if Wrath
            new NamedTypeFilter<BlueprintGlobalMapPoint>("Map Points", null, bp => bp.CollationNames(bp.GlobalMapZone.ToString())),
            new NamedTypeFilter<BlueprintGlobalMap>("Global Map"),
#elif RT
            new NamedTypeFilter<BlueprintStarSystemMap>(
                    "System Map", null,
                    bp => {
                        var starNames = bp.Stars.Select(r => $"☀ {r.Star.Get().NameForAcronym}");
                        var planetNames = bp.Planets.Select(p => $"◍ {p.Get().Name}");
                        return bp.CollationNames(starNames.Concat(planetNames).ToArray());
                    }
                ),
            new NamedTypeFilter<BlueprintSectorMapPoint>("Sector Map Points", null,  bp => bp.CollationNames(bp.Name.Split(' '))),

#endif
            new NamedTypeFilter<Cutscene>("Cut Scenes", null, bp => bp.CollationNames(bp.Priority.ToString())),
            //new NamedTypeFilter<BlueprintMythicInfo>("Mythic Info"),
            new NamedTypeFilter<BlueprintQuest>("Quests", null, bp => bp.CollationNames(bp.m_Type.ToString())),
            new NamedTypeFilter<BlueprintQuestObjective>("QuestObj", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintEtude>("Etudes", null, bp =>bp.CollationNames(bp.Parent?.GetBlueprint().NameSafe() ?? "" )),
            new NamedTypeFilter<BlueprintUnlockableFlag>("Flags", null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintDialog>("Dialog",null, bp => bp.CaptionCollationNames()),
            new NamedTypeFilter<BlueprintCue>("Cues", null, bp => {
                if (bp.Conditions.HasConditions) {
                    return bp.CollationNames(bp.Conditions.Conditions.First().NameSafe().SubstringBetweenCharacters('$', '$'));
                }
                return new List<string?> { "-" };
                }),
            new NamedTypeFilter<BlueprintAnswer>("Answer", null, bp => bp.CaptionCollationNames()),
#if Wrath
            new NamedTypeFilter<BlueprintCrusadeEvent>("Crusade Events", null, bp => bp.CollationNames()),

            new NamedTypeFilter<BlueprintArmyPreset>("Armies", null, bp => bp.CollationNames()),
            new NamedTypeFilter<BlueprintLeaderSkill>("ArmyGeneralSkill", null, bp =>  bp.CollationNames()),
#endif
#if false
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (ench)", null, (bp) => {
                try {
                    var enchants = bp.CollectEnchantments();
                    var value = enchants.Sum((e) => e.EnchantmentCost);
                    return new List<string> { value.ToString() };
                }
                catch {
                    return new List<string> { "0" };
                }
            }),
            new NamedTypeFilter<BlueprintItemEquipment>("Equip (cost)", null, (bp) => new List<string> {bp.Cost.ToBinString() }),  
#endif
            //new NamedTypeFilter<SimpleBlueprint>("In Memory", null, bp => bp.CollationName(), () => ResourcesLibrary.s_LoadedBlueprints.Values.Where(bp => bp != null)),

        };

        public static NamedTypeFilter selectedTypeFilter = null;

        public static IEnumerable<SimpleBlueprint> blueprints = null;

        public static void RedoLayout() {
            if (bps == null) return;
            foreach (var blueprint in bps) {
                var actions = blueprint.GetActions();
                if (actions.Any(a => a.isRepeatable)) hasRepeatableAction = true;
                if (selectedUnit != null) {
                    // FIXME - perf bottleneck 
                    var actionCount = actions.Sum(action => action.canPerform(blueprint, selectedUnit) ? 1 : 0);
                    maxActions = Math.Max(actionCount, maxActions);
                }
            }
        }

        public static void OnGUI() {
            if (Event.current.type == EventType.Layout && (SearchAndPickBrowser.isCollating || needsRedoKeys)) {
                needsRedoKeys = SearchAndPickBrowser.isCollating;
                var count = SearchAndPickBrowser.collatedDefinitions.Keys.Count;
                var tmp = new string[(int)(1.1 * count) + 10];
                SearchAndPickBrowser.collatedDefinitions.Keys.CopyTo(tmp, 0);
                collationKeys = tmp.Where(s => !string.IsNullOrEmpty(s) && SearchAndPickBrowser.collatedDefinitions[s].Count > 0).ToList();
                if (Settings.sortCollationByEntries) {
                    collationKeys.Sort(Comparer<string>.Create((x, y) => {
                        return SearchAndPickBrowser.collatedDefinitions[y].Count.CompareTo(SearchAndPickBrowser.collatedDefinitions[x].Count);
                    }));
                }
                else {
                    collationKeys.Sort(Comparer<string>.Create((x, y) => {
                        if (char.IsNumber(x[x.Length - 1]) && char.IsNumber(y[y.Length - 1])) {
                            int numberOfDigitsAtEndx = 0;
                            int numberOfDigitsAtEndy = 0;
                            for (var i = x.Length - 1; i >= 0; i--) {
                                if (!char.IsNumber(x[i])) {
                                    break;
                                }

                                numberOfDigitsAtEndx++;
                            }
                            for (var i = y.Length - 1; i >= 0; i--) {
                                if (!char.IsNumber(y[i])) {
                                    break;
                                }

                                numberOfDigitsAtEndy++;
                            }
                            var result = x.Take(x.Length - numberOfDigitsAtEndx).ToString().CompareTo(y.Take(y.Length - numberOfDigitsAtEndy).ToString());
                            if (result != 0) return result;
                            var resultx = int.Parse(string.Join("", x.TakeLast(numberOfDigitsAtEndx)));
                            var resulty = int.Parse(string.Join("", y.TakeLast(numberOfDigitsAtEndy)));
                            return resultx.CompareTo(resulty);

                        }
                        return x.CompareTo(y);
                    }));
                }
                keyToDisplayName.Clear();
                collationKeys.ForEach(s => keyToDisplayName[s] = $"{s} ({SearchAndPickBrowser.collatedDefinitions[s]?.Count})");
                collationKeys.Insert(0, "All");
                keyToDisplayName["All"] = $"All ({bpCount})";
            }
            if (blueprints == null) {
                SearchAndPickBrowser.DisplayShowAllGUI = false;
                SearchAndPickBrowser.doCollation = true;
                blueprints = BlueprintLoader.Shared.GetBlueprints();
                if (blueprints != null) {
                    InitType();
                }
            }
            using (HorizontalScope(Width(350))) {
                var remainingWidth = ummWidth;
                using (VerticalScope(GUI.skin.box)) {
                    ActionSelectionGrid(ref Settings.selectedBPTypeFilter,
                        blueprintTypeFilters.Select(tf => tf.name.localize()).ToArray(),
                        1,
                        (selected) => {
                            InitType();
                        },
                        buttonStyle,
                        Width(200));
                }
                remainingWidth -= 350;
                using (VerticalScope()) {
                    if (Toggle("Sort By Count".localize(), ref Settings.sortCollationByEntries)) {
                        needsRedoKeys = true;
                    }
                    if (collationKeys?.Count > 0) {
                        if (PagedVPicker("Categories".localize(), ref SearchAndPickBrowser.collationKey, collationKeys.ToList(), null, s => keyToDisplayName[s], ref collationSearchText, ref collationPickerPageSize, ref collationPickerCurrentPage, Width(300))) {
                            Mod.Debug($"collationKey: {SearchAndPickBrowser.collationKey}");
                        }
                        remainingWidth -= 450;
                    }
                }
                using (VerticalScope(MinWidth(remainingWidth))) {
                    List<Action> todo = new();
                    int count = 0;
                    if (selectedTypeFilter != null) {
                        collatorCache[selectedTypeFilter.type] = selectedTypeFilter.collator;
                    }
                    using (HorizontalScope()) {
                        50.space();
                        using (VerticalScope()) {
                            CharacterPicker.OnCharacterPickerGUI();
                            var tmp = CharacterPicker.GetSelectedCharacter();
                            if (tmp != selectedUnit) {
                                selectedUnit = tmp;
                                RedoLayout();
                            }
                        }
                    }
                    SearchAndPickBrowser.OnGUI(bps, () => bps, bp => bp, bp => GetSearchKey(bp) + (Settings.searchDescriptions ? bp.GetDescription() : ""), bp => new[] { GetSortKey(bp) },
                        () => {
                            using (VerticalScope()) {
                                using (HorizontalScope()) {
                                    if (hasRepeatableAction) {
                                        Label("Parameter".localize().cyan() + ": ", ExpandWidth(false));
                                        ActionIntTextField(
                                            ref repeatCount,
                                            "repeatCount",
                                            (limit) => { },
                                            () => { },
                                            Width(100));
                                        Space(40);
                                        repeatCount = Math.Max(1, repeatCount);
                                        repeatCount = Math.Min(100, repeatCount);
                                    }
                                    25.space();
                                    if (Toggle("Search Descriptions".localize(), ref Settings.searchDescriptions, AutoWidth())) SearchAndPickBrowser.ReloadData();
                                    25.space();
                                    if (Toggle("Attributes".localize(), ref Settings.showAttributes, AutoWidth())) SearchAndPickBrowser.ReloadData();
                                    25.space();
                                    Toggle("Show GUIDs".localize(), ref Settings.showAssetIDs, AutoWidth());
                                    25.space();
                                    Toggle("Components".localize(), ref Settings.showComponents, AutoWidth());
                                    25.space();
                                    Toggle("Elements".localize(), ref Settings.showElements, AutoWidth());
                                    25.space();
                                    if (Toggle("Show Display & Internal Names".localize(), ref Settings.showDisplayAndInternalNames, AutoWidth())) SearchAndPickBrowser.ReloadData();
                                }
                            }
                        },
                        (bp, maybeBP) => {
                            GetTitle(bp);
                            Func<string, string> titleFormatter = (t) => t.orange().bold();
                            if (remainingWidth == 0) remainingWidth = ummWidth;
                            var description = bp.GetDescription().MarkedSubstring(Settings.searchText);
                            if (bp is BlueprintItem itemBlueprint && itemBlueprint.FlavorText?.Length > 0)
                                description = $"{itemBlueprint.FlavorText.StripHTML().color(RGBA.notable).MarkedSubstring(Settings.searchText)}\n{description}";
                            float titleWidth = 0;
                            var remWidth = remainingWidth;
                            using (HorizontalScope()) {
                                var actions = bp.GetActions()
                                    .Where(action => action.canPerform(bp, selectedUnit));
                                var titles = actions.Select(a => a.name);
                                string title = null;

                                // FIXME - perf bottleneck 
                                var actionCount = actions != null ? actions.Count() : 0;
                                // FIXME - horrible perf bottleneck 
                                // I mean it's an improvement?
                                int removeIndex = -1;
                                int lockIndex = -1;
                                int actionIndex = 0;
                                foreach (var action in titles) {
                                    if (action == "Remove".localize()) {
                                        removeIndex = actionIndex;
                                    }
                                    if (action == "Lock".localize()) {
                                        lockIndex = actionIndex;
                                    }
                                    actionIndex++;
                                }
                                // var removeIndex = titles.IndexOf("Remove".localize());
                                // var lockIndex = titles.IndexOf("Lock".localize());
                                if (removeIndex > -1 || lockIndex > -1) {
                                    title = GetTitle(bp, name => name.cyan().bold());
                                }
                                else {
                                    title = GetTitle(bp, name => name.orange().bold());
                                }
                                titleWidth = (remainingWidth / (IsWide ? 3 : 4));
                                var text = title.MarkedSubstring(Settings.searchText);
                                if (bp is BlueprintFeatureSelection featureSelection
#if Wrath
                                || bp is BlueprintParametrizedFeature parametrizedFeature
#endif
    ) {
                                    if (Browser.DetailToggle(text, bp, bp, (int)titleWidth))
                                        SearchAndPickBrowser.ReloadData();
                                }
                                else
                                    Label(text, Width((int)titleWidth));
                                remWidth -= titleWidth;

                                if (bp is BlueprintUnlockableFlag flagBP) {
                                    // special case this for now
                                    if (lockIndex >= 0) {
                                        var flags = Game.Instance.Player.UnlockableFlags;
                                        var lockAction = actions.ElementAt(lockIndex);
                                        ActionButton("<", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) - 1); }, Width(50));
                                        Space(25);
                                        Label($"{flags.GetFlagValue(flagBP)}".orange().bold(), MinWidth(50));
                                        ActionButton(">", () => { flags.SetFlagValue(flagBP, flags.GetFlagValue(flagBP) + 1); }, Width(50));
                                        Space(50);
                                        ActionButton(lockAction.name, () => { lockAction.action(bp, selectedUnit, repeatCount); }, Width(120));
                                        Space(100);
#if DEBUG
                                        Label(flagBP.GetDescription().green());
#endif
                                    }
                                    else {
                                        // FIXME - perf bottleneck 
                                        var unlockIndex = titles.IndexOf("Unlock".localize());
                                        if (unlockIndex >= 0) {
                                            var unlockAction = actions.ElementAt(unlockIndex);
                                            Space(240);
                                            ActionButton(unlockAction.name, () => { unlockAction.action(bp, selectedUnit, repeatCount); }, Width(120));
                                            Space(100);
                                        }
                                    }
                                    remWidth -= 300;
                                }
                                else {
                                    for (var ii = 0; ii < maxActions; ii++) {
                                        if (ii < actionCount) {
                                            var action = actions.ElementAt(ii);
                                            // TODO -don't show increase or decrease actions until we redo actions into a proper value editor that gives us Add/Remove and numeric item with the ability to show values.  For now users can edit ranks in the Facts Editor
                                            if (action.name == "<" || action.name == ">") {
                                                Space(174); continue;
                                            }
                                            var actionName = action.name;
                                            float extraSpace = 0;
                                            if (action.isRepeatable) {
                                                actionName += action.isRepeatable ? $" {repeatCount}" : "";
                                                extraSpace = 20 * (float)Math.Ceiling(Math.Log10((double)repeatCount));
                                            }
                                            ActionButton(actionName, () => todo.Add(() => action.action(bp, selectedUnit, repeatCount)), Width(160 + extraSpace));
                                            Space(10);
                                            remWidth -= 174.0f + extraSpace;

                                        }
                                        else {
                                            Space(174);
                                        }
                                    }
                                }
                                Space(10);
                                var type = bp.GetType();
                                var typeString = type.Name;
                                Func<SimpleBlueprint, List<string>> collator;
                                collatorCache.TryGetValue(type, out collator);
                                if (collator != null) {
                                    var names = collator(bp);
                                    if (names.Count > 0) {
                                        var collatorString = names.First();
                                        if (bp is BlueprintItem itemBP) {
                                            var rarity = itemBP.Rarity();
                                            typeString = $"{typeString} - {rarity}".Rarity(rarity);
                                        }
                                        if (!typeString.Contains(collatorString)) {
                                            typeString += $" : {collatorString}".yellow();
                                        }
                                    }
                                }
                                var attributes = "";
                                if (Settings.showAttributes) {
                                    var attr = string.Join(" ", bp.Attributes());
                                    if (!typeString.Contains(attr))
                                        attributes = attr;
                                }

                                if (attributes.Length > 1) typeString += $" - {attributes.orange()}";

                                if (description != null && description.Length > 0) description = $"{description}";
                                else description = "";
                                if (bp is BlueprintScriptableObject bpso) {
                                    if (Settings.showComponents && bpso.ComponentsArray?.Length > 0) {
                                        var componentStr = string.Join<object>(", ", bpso.ComponentsArray).color(RGBA.brown);
                                        if (description.Length == 0) description = componentStr;
                                        else description = description + "\n" + componentStr;
                                    }
                                    if (Settings.showElements && bpso.ElementsArray?.Count > 0) {
                                        var elementsStr = string.Join<object>("\n", bpso.ElementsArray.Select(e => $"{e.GetType().Name.cyan()} {e.GetCaption()}")).yellow();
                                        if (description.Length == 0) description = elementsStr;
                                        else description = description + "\n" + elementsStr;
                                    }
                                }
                                using (VerticalScope(Width(remWidth))) {
                                    using (HorizontalScope(Width(remWidth))) {
                                        ReflectionTreeView.DetailToggle("", bp, bp, 0);
                                        Space(-17);
                                        if (Settings.showAssetIDs) {
                                            Label(typeString, rarityButtonStyle);
                                            ClipboardLabel(bp.AssetGuid.ToString(), ExpandWidth(false));
                                        }
                                        else Label(typeString, rarityButtonStyle);
                                        Space(17);
                                    }
                                    if (description.Length > 0) Label(description.green(), Width(remWidth));
                                }
                            }
                            count++;
                        },
                        (bp, maybeBP) => {
                            ReflectionTreeView.OnDetailGUI(bp);
                            if (bp is BlueprintUnitFact buf) {
                                FactsEditor.BlueprintDetailGUI<UnitFact, BlueprintUnitFact, SimpleBlueprint, SimpleBlueprint>(buf, null, selectedUnit, SearchAndPickBrowser);
                            }
                        }, 50, true, true, 100, 300, "", false, selectedTypeFilter?.collator);
                    foreach (var action in todo) {
                        action();
                    }
                }
                Space(25);
            }
        }

        public static void InitType() {
            collationPickerCurrentPage = 1;
            selectedTypeFilter = blueprintTypeFilters[Settings.selectedBPTypeFilter];
            if (selectedTypeFilter.blueprintSource != null) bps = selectedTypeFilter.blueprintSource();
            else bps = from bp in BlueprintsOfType(selectedTypeFilter.type)
                       where selectedTypeFilter.filter(bp)
                       select bp;
            RedoLayout();
            bpCount = bps.Count();
            SearchAndPickBrowser.RedoCollation();
        }

        public static void ResetGUI() {
            RedoLayout();
            SearchAndPickBrowser.RedoCollation();
        }
    }
}