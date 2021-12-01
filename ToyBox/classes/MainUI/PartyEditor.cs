// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
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
    public class PartyEditor {
        public static Settings settings => Main.settings;

        private enum ToggleChoice {
            Classes,
            Stats,
            Facts,
            Features,
            Buffs,
            Abilities,
            Spells,
            None,
        };

        private static ToggleChoice selectedToggle = ToggleChoice.None;
        private static int selectedCharacterIndex = 0;
        private static UnitEntityData charToAdd = null;
        private static UnitEntityData charToRecruit = null;
        private static UnitEntityData charToRemove = null;
        private static UnitEntityData charToUnrecruit = null;
        private static bool editMultiClass = false;
        private static UnitEntityData multiclassEditCharacter = null;
        private static int respecableCount = 0;
        private static int recruitableCount = 0;
        private static int selectedSpellbook = 0;
        private static (string, string) nameEditState = (null, null);
        public static int selectedSpellbookLevel = 0;
        private static bool editSpellbooks = false;
        private static UnitEntityData spellbookEditCharacter = null;
        private static readonly Dictionary<string, int> statEditorStorage = new();
        public static Dictionary<string, Spellbook> SelectedSpellbook = new();

        public static List<UnitEntityData> GetCharacterList() {
            var partyFilterChoices = CharacterPicker.GetPartyFilterChoices();
            if (partyFilterChoices == null) { return null; }
            return partyFilterChoices[Main.settings.selectedPartyFilter].func();
        }

        private static UnitEntityData GetSelectedCharacter() {
            var characterList = GetCharacterList();
            if (characterList == null || characterList.Count == 0) return null;
            if (selectedCharacterIndex >= characterList.Count) selectedCharacterIndex = 0;
            return characterList[selectedCharacterIndex];
        }
        public static void ResetGUI() {
            selectedCharacterIndex = 0;
            selectedSpellbook = 0;
            selectedSpellbookLevel = 0;
            CharacterPicker.partyFilterChoices = null;
            Main.settings.selectedPartyFilter = 0;
        }

        // This bit of kludge is added in order to tell whether our generic actions are being accessed from this screen or the Search n' Pick
        public static bool IsOnPartyEditor() => Main.settings.selectedTab == 2;

        public static void ActionsGUI(UnitEntityData ch) {
            var player = Game.Instance.Player;
            Space(25);
            if (!player.PartyAndPets.Contains(ch) && player.AllCharacters.Contains(ch)) {
                ActionButton("Add", () => { charToAdd = ch; }, Width(150));
                Space(25);
            }
            else if (player.ActiveCompanions.Contains(ch)) {
                ActionButton("Remove", () => { charToRemove = ch; }, Width(150));
                Space(25);
            }
            else if (!player.AllCharacters.Contains(ch)) {
                recruitableCount++;
                ActionButton("Recruit".cyan(), () => { charToRecruit = ch; }, Width(150));
                Space(25);
            }
            if (player.AllCharacters.Contains(ch) && !ch.IsStoryCompanion()) {
                ActionButton("Unrecruit".cyan(), () => { charToUnrecruit = ch; charToRemove = ch; }, Width(150));
                Space(25);

            }
            else {
                Space(178);
            }
            if (RespecHelper.GetRespecableUnits().Contains(ch)) {
                respecableCount++;
                ActionButton("Respec".cyan(), () => { Actions.ToggleModWindow(); RespecHelper.Respec(ch); }, Width(150));
            }
            else {
                Space(153);
            }
#if DEBUG
            Space(25);
            ActionButton("Log Caster Info", () => CasterHelpers.GetOriginalCasterLevel(ch.Descriptor),
                AutoWidth());
#endif
        }
        public static void OnGUI() {
            var player = Game.Instance.Player;
            var filterChoices = CharacterPicker.GetPartyFilterChoices();
            if (filterChoices == null) { return; }

            charToAdd = null;
            charToRecruit = null;
            charToRemove = null;
            charToUnrecruit = null;
            var characterListFunc = TypePicker(
                null,
                ref Main.settings.selectedPartyFilter,
                filterChoices
                );
            var characterList = characterListFunc.func();
            var mainChar = GameHelper.GetPlayerCharacter();
            if (characterListFunc.name == "Nearby") {
                Slider("Nearby Distance", ref CharacterPicker.nearbyRange, 1f, 200, 25, 0, " meters", Width(250));
                characterList = characterList.OrderBy((ch) => ch.DistanceTo(mainChar)).ToList();
            }
            Space(20);
            var chIndex = 0;
            recruitableCount = 0;
            respecableCount = 0;
            var selectedCharacter = GetSelectedCharacter();
            var isWide = IsWide;
            if (Main.IsInGame) {
                using (HorizontalScope()) {
                    Label($"Party Level ".cyan() + $"{Game.Instance.Player.PartyLevel}".orange().bold(), AutoWidth());
                    Space(25);
#if false   // disabled until we fix performance
                    var encounterCR = CheatsCombat.GetEncounterCr();
                    if (encounterCR > 0) {
                        UI.Label($"Encounter CR ".cyan() + $"{encounterCR}".orange().bold(), UI.AutoWidth());
                    }
#endif
                }
            }
            List<Action> todo = new();
            foreach (var ch in characterList) {
                var classData = ch.Progression.Classes;
                // TODO - understand the difference between ch.Progression and ch.Descriptor.Progression
                var progression = ch.Descriptor.Progression;
                var xpTable = progression.ExperienceTable;
                var level = progression.CharacterLevel;
                var mythicLevel = progression.MythicLevel;
                var spellbooks = ch.Spellbooks;
                var spellCount = spellbooks.Sum((sb) => sb.GetAllKnownSpells().Count());
                var isOnTeam = player.AllCharacters.Contains(ch);
                using (HorizontalScope()) {
                    var name = ch.CharacterName;
                    if (Game.Instance.Player.AllCharacters.Contains(ch)) {
                        if (isWide) {
                            if (EditableLabel(ref name, ref nameEditState, 200, n => n.orange().bold(), MinWidth(100), MaxWidth(600))) {
                                ch.Descriptor.CustomName = name;
                                Main.SetNeedsResetGameUI();
                            }
                        }
                        else
                            if (EditableLabel(ref name, ref nameEditState, 200, n => n.orange().bold(), Width(230))) {
                            ch.Descriptor.CustomName = name;
                            Main.SetNeedsResetGameUI();
                        }
                    }
                    else {
                        if (isWide)
                            Label(ch.CharacterName.orange().bold(), MinWidth(100), MaxWidth(600));
                        else
                            Label(ch.CharacterName.orange().bold(), Width(230));
                    }
                    Space(5);
                    var distance = mainChar.DistanceTo(ch); ;
                    Label(distance < 1 ? "" : distance.ToString("0") + "m", Width(75));
                    Space(5);
                    int nextLevel;
                    for (nextLevel = level; progression.Experience >= xpTable.GetBonus(nextLevel + 1) && xpTable.HasBonusForLevel(nextLevel + 1); nextLevel++) { }
                    if (nextLevel <= level || !isOnTeam)
                        Label((level < 10 ? "   lvl" : "   lv").green() + $" {level}", Width(90));
                    else
                        Label((level < 10 ? "  " : "") + $"{level} > " + $"{nextLevel}".cyan(), Width(90));
                    // Level up code adapted from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/2
                    if (player.AllCharacters.Contains(ch)) {
                        if (xpTable.HasBonusForLevel(nextLevel + 1)) {
                            ActionButton("+1", () => {
                                progression.AdvanceExperienceTo(xpTable.GetBonus(nextLevel + 1), true);
                            }, Width(63));
                        }
                        else { Label("max", Width(63)); }
                    }
                    else { Space(66); }
                    Space(10);
                    var nextML = progression.MythicExperience;
                    if (nextML <= mythicLevel || !isOnTeam)
                        Label((mythicLevel < 10 ? "  my" : "  my").green() + $" {mythicLevel}", Width(90));
                    else
                        Label((level < 10 ? "  " : "") + $"{mythicLevel} > " + $"{nextML}".cyan(), Width(90));
                    if (player.AllCharacters.Contains(ch)) {
                        if (progression.MythicExperience < 10) {
                            ActionButton("+1", () => {
                                progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                            }, Width(63));
                        }
                        else { Label("max", Width(63)); }
                    }
                    else { Space(66); }
                    Space(30);
                    if (!isWide) ActionsGUI(ch);
                    Wrap(!IsWide, 283, 0);
                    var showClasses = ch == selectedCharacter && selectedToggle == ToggleChoice.Classes;
                    if (DisclosureToggle($"{classData.Count} Classes", ref showClasses)) {
                        if (showClasses) {
                            selectedCharacter = ch; selectedToggle = ToggleChoice.Classes; Mod.Trace($"selected {ch.CharacterName}");
                        }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    var showStats = ch == selectedCharacter && selectedToggle == ToggleChoice.Stats;
                    if (DisclosureToggle("Stats", ref showStats, 125)) {
                        if (showStats) { selectedCharacter = ch; selectedToggle = ToggleChoice.Stats; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    Wrap(IsNarrow, 279);
                    //var showFacts = ch == selectedCharacter && selectedToggle == ToggleChoice.Facts;
                    //if (UI.DisclosureToggle("Facts", ref showFacts, 125)) {
                    //    if (showFacts) { selectedCharacter = ch; selectedToggle = ToggleChoice.Facts; }
                    //    else { selectedToggle = ToggleChoice.None; }
                    //}
                    var showFeatures = ch == selectedCharacter && selectedToggle == ToggleChoice.Features;
                    if (DisclosureToggle("Features", ref showFeatures, 150)) {
                        if (showFeatures) { selectedCharacter = ch; selectedToggle = ToggleChoice.Features; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    var showBuffs = ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs;
                    if (DisclosureToggle("Buffs", ref showBuffs, 125)) {
                        if (showBuffs) { selectedCharacter = ch; selectedToggle = ToggleChoice.Buffs; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    Wrap(IsNarrow, 304);
                    var showAbilities = ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities;
                    if (DisclosureToggle("Abilities", ref showAbilities, 125)) {
                        if (showAbilities) { selectedCharacter = ch; selectedToggle = ToggleChoice.Abilities; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    Space(10);
                    if (spellbooks.Count() > 0) {
                        var showSpells = ch == selectedCharacter && selectedToggle == ToggleChoice.Spells;
                        if (DisclosureToggle($"{spellCount} Spells", ref showSpells)) {
                            if (showSpells) { selectedCharacter = ch; selectedToggle = ToggleChoice.Spells; }
                            else { selectedToggle = ToggleChoice.None; }
                        }
                    }
                    else { Space(180); }
                    if (isWide) ActionsGUI(ch);
                }
                //if (!UI.IsWide && (selectedToggle != ToggleChoice.Stats || ch != selectedCharacter)) {
                //    UI.Div(20, 20);
                //}
                if (selectedCharacter != spellbookEditCharacter) {
                    editSpellbooks = false;
                    spellbookEditCharacter = null;
                }
                if (selectedCharacter != multiclassEditCharacter) {
                    editMultiClass = false;
                    multiclassEditCharacter = null;
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Classes) {
#if true
                    Div(100, 20);
                    using (HorizontalScope()) {
                        Space(100);
                        Toggle("Multiple Classes On Level-Up", ref settings.toggleMulticlass);
                        if (settings.toggleMulticlass) {
                            Space(40);
                            if (DisclosureToggle("Config".orange().bold(), ref editMultiClass)) {
                                multiclassEditCharacter = selectedCharacter;
                            }
                            Space(53);
                            Label("Experimental - See 'Level Up + Multiclass' for more options and info".green());
                        }
                    }
                    using (HorizontalScope()) {
                        Space(100);
                        ActionToggle("Allow Levels Past 20",
                            () => {
                                var hasValue = settings.perSave.charIsLegendaryHero.TryGetValue(ch.HashKey(), out var isLegendaryHero);
                                return hasValue && isLegendaryHero;
                            },
                            (val) => {
                                if (settings.perSave.charIsLegendaryHero.ContainsKey(ch.HashKey())) {
                                    settings.perSave.charIsLegendaryHero[ch.HashKey()] = val;
                                    Settings.SavePerSaveSettings();
                                }
                                else {
                                    settings.perSave.charIsLegendaryHero.Add(ch.HashKey(), val);
                                    Settings.SavePerSaveSettings();
                                }
                            },
                            0f,
                            AutoWidth());
                        Space(380);
                        Label("Tick this to let your character exceed the level 20 level cap like the Legend mythic path".green());
                    }
#endif
                    Div(100, 20);
                    if (editMultiClass) {
                        MulticlassPicker.OnGUI(ch);
                    }
                    else {
                        var prog = ch.Descriptor.Progression;
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Character Level".cyan(), Width(250));
                            ActionButton("<", () => prog.CharacterLevel = Math.Max(0, prog.CharacterLevel - 1), AutoWidth());
                            Space(25);
                            Label("level".green() + $": {prog.CharacterLevel}", Width(100f));
                            ActionButton(">", () => prog.CharacterLevel = Math.Min(prog.MaxCharacterLevel, prog.CharacterLevel + 1), AutoWidth());
                            Space(25);
                            ActionButton("Reset", () => ch.resetClassLevel(), Width(125));
                            Space(23);
                            using (VerticalScope()) {
                                Label("This directly changes your character level but will not change exp or adjust any features associated with your character. To do a normal level up use +1 Lvl above.  This gets recalculated when you reload the game.  ".green() + "If you want to alter default character level mark classes you want to exclude from the calculation with ".orange() + "gestalt".orange().bold() + " which means those levels were added for multi-classing. See the link for more information on this campaign variant.".orange());
                                LinkButton("Gestalt Characters", "https://www.d20srd.org/srd/variant/classes/gestaltCharacters.htm");
                            }
                        }
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Experience".cyan(), Width(250));
                            Space(82);
                            Label($"{prog.Experience}", Width(150f));
                            Space(36);
                            ActionButton("Set", () => {
                                var newXP = prog.ExperienceTable.GetBonus(Mathf.RoundToInt(prog.CharacterLevel));
                                prog.Experience = newXP;
                            }, Width(125));
                            Space(23);
                            Label("This sets your experience to match the current value of character level.".green());
                        }
                        Div(100, 25);
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Mythic Level".cyan(), Width(250));
                            ActionButton("<", () => prog.MythicLevel = Math.Max(0, prog.MythicLevel - 1), AutoWidth());
                            Space(25);
                            Label("my lvl".green() + $": {prog.MythicLevel}", Width(100f));
                            ActionButton(">", () => prog.MythicLevel = Math.Min(10, prog.MythicLevel + 1), AutoWidth());
                            Space(175);
                            Label("This directly changes your mythic level but will not adjust any features associated with your character. To do a normal mythic level up use +1 my above".green());
                        }
                        using (HorizontalScope()) {
                            Space(100);
                            Label("Experience".cyan(), Width(250));
                            Space(82);
                            Label($"{prog.MythicExperience}", Width(150f));
                            Space(36);
                            ActionButton("Set", () => {
                                prog.MythicExperience = prog.MythicLevel;
                            }, Width(125));
                            Space(23);
                            Label("This sets your mythic experience to match the current value of mythic level. Note that mythic experience is 1 point per level".green());
                        }
                        var classCount = classData.Count(x => !x.CharacterClass.IsMythic);
                        var gestaltCount = classData.Count(cd => !cd.CharacterClass.IsMythic && ch.IsClassGestalt(cd.CharacterClass));
                        var mythicCount = classData.Count(x => x.CharacterClass.IsMythic);
                        var mythicGestaltCount = classData.Count(cd => cd.CharacterClass.IsMythic && ch.IsClassGestalt(cd.CharacterClass));
                        foreach (var cd in classData) {
                            var showedGestalt = false;
                            Div(100, 20);
                            using (HorizontalScope()) {
                                Space(100);
                                using (VerticalScope(Width(250))) {
                                    var className = cd.CharacterClass.Name;
                                    var archetype = cd.Archetypes.FirstOrDefault<BlueprintArchetype>();
                                    if (archetype != null) {
                                        var archName = archetype.Name;
                                        Label(archName.orange(), Width(250));
                                        if (!archName.Contains(className))
                                            Label(className.yellow(), Width(250));
                                    }
                                    else {
                                        Label(className.orange(), Width(250));
                                    }
                                }
                                ActionButton("<", () => cd.Level = Math.Max(0, cd.Level - 1), AutoWidth());
                                Space(25);
                                Label("level".green() + $": {cd.Level}", Width(100f));
                                var maxLevel = cd.CharacterClass.Progression.IsMythic ? 10 : 20;
                                ActionButton(">", () => cd.Level = Math.Min(maxLevel, cd.Level + 1), AutoWidth());
                                Space(23);
                                if (ch.IsClassGestalt(cd.CharacterClass)
                                    || !cd.CharacterClass.IsMythic && classCount - gestaltCount > 1
                                    || cd.CharacterClass.IsMythic && mythicCount - mythicGestaltCount > 1
                                    ) {
                                    ActionToggle(
                                        "gestalt".grey(),
                                        () => ch.IsClassGestalt(cd.CharacterClass),
                                        (v) => {
                                            ch.SetClassIsGestalt(cd.CharacterClass, v);
                                            ch.Progression.UpdateLevelsForGestalt();
                                        },
                                        125
                                        );
                                    showedGestalt = true;
                                }
                                else Space(125);
                                Space(27);
                                using (VerticalScope()) {
                                    if (showedGestalt) {
                                        if (showedGestalt) {
                                            Label("this flag lets you not count this class in computing character level".green());
                                            DivLast();
                                        }
                                    }
                                    Label(cd.CharacterClass.Description.StripHTML().green(), AutoWidth());
                                }
                            }
                        }
                    }
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Stats) {
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
                //if (ch == selectedCharacter && selectedToggle == ToggleChoice.Facts) {
                //    todo = FactsEditor.OnGUI(ch, ch.Facts.m_Facts);
                //}
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Features) {
                    todo = FactsEditor.OnGUI(ch, ch.Progression.Features.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs) {
                    todo = FactsEditor.OnGUI(ch, ch.Descriptor.Buffs.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities) {
                    todo = FactsEditor.OnGUI(ch, ch.Descriptor.Abilities.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Spells) {
                    Space(20);
                    var names = spellbooks.Select((sb) => sb.Blueprint.GetDisplayName()).ToArray();
                    var titles = names.Select((name, i) => $"{name} ({spellbooks.ElementAt(i).CasterLevel})").ToArray();
                    if (spellbooks.Any()) {
                        var spellbook = spellbooks.ElementAt(selectedSpellbook);
                        using (HorizontalScope()) {
                            SelectionGrid(ref selectedSpellbook, titles, Math.Min(titles.Length, 7), AutoWidth());
                            if (selectedSpellbook >= names.Length) selectedSpellbook = 0;
                            DisclosureToggle("Edit".orange().bold(), ref editSpellbooks);
                            Space(-50);
                            var mergableClasses = ch.MergableClasses();
                            if (spellbook.IsStandaloneMythic || mergableClasses.Count() == 0) {
                                Label($"Merge Mythic".cyan(), AutoWidth());
                                25.space();
                                Label("When you get standalone mythic spellbooks you can merge them here.".green());
                            }
                            else {
                                Label($"Merge Mythic:".cyan(), 175.width());
                                25.space();
                                foreach (var cl in mergableClasses) {
                                    ActionButton(cl.CharacterClass.LocalizedName.ToString(), () => spellbook.MergeMythicSpellbook(cl));
                                    15.space();
                                }
                                25.space();
                                using (VerticalScope()) {
                                    Label("Merging your mythic spellbook will cause you to transfer all mythic spells to your normal spellbook and gain caster levels equal to your mythic level. You will then be able to re-select spells on next level up or mythic level up. Merging a second mythic spellbook will transfer the spells but not increase your caster level further.  If you want more CL then increase it below.".green());
                                    Label("Warning: This is irreversible. Please save before continuing!".Orange());
                                }
                            }
                        }
                        spellbook = spellbooks.ElementAt(selectedSpellbook);
                        if (editSpellbooks) {
                            spellbookEditCharacter = ch;
                            var blueprints = BlueprintExensions.GetBlueprints<BlueprintSpellbook>().OrderBy((bp) => bp.GetDisplayName());
                            todo = BlueprintListUI.OnGUI(ch, blueprints, 100);
                        }
                        else {
                            var maxLevel = spellbook.Blueprint.MaxSpellLevel;
                            var casterLevel = spellbook.CasterLevel;
                            using (HorizontalScope()) {
                                EnumerablePicker(
                                    "Spells known",
                                    ref selectedSpellbookLevel,
                                    Enumerable.Range(0, spellbook.Blueprint.MaxSpellLevel + 1),
                                    0,
                                    (lvl) => {
                                        var levelText = spellbook.Blueprint.SpellsPerDay.GetCount(casterLevel, lvl) != null ? $"L{lvl}".bold() : $"L{lvl}".grey();
                                        var knownCount = spellbook.GetKnownSpells(lvl).Count;
                                        var countText = knownCount > 0 ? $" ({knownCount})".white() : "";
                                        return levelText + countText;
                                    },
                                    AutoWidth()
                                );
                                Space(20);
                                if (casterLevel > 0) {
                                    ActionButton("-1 CL", () => CasterHelpers.LowerCasterLevel(spellbook), AutoWidth());
                                }
                                if (casterLevel < 40) {
                                    ActionButton("+1 CL", () => CasterHelpers.AddCasterLevel(spellbook), AutoWidth());
                                }
                                // removes opposition schools; these are not cleared when removing facts; to add new opposition schools, simply add the corresponding fact again
                                if (spellbook.OppositionSchools.Any()) {
                                    ActionButton("Clear Opposition Schools", () => {
                                        spellbook.OppositionSchools.Clear();
                                        spellbook.ExOppositionSchools.Clear();
                                        ch.Facts.RemoveAll<UnitFact>(r => r.Blueprint.GetComponent<AddOppositionSchool>(), true);
                                    }, AutoWidth());
                                }
                                if (spellbook.OppositionDescriptors != 0) {
                                    ActionButton("Clear Opposition Descriptors", () => {
                                        spellbook.OppositionDescriptors = 0;
                                        ch.Facts.RemoveAll<UnitFact>(r => r.Blueprint.GetComponent<AddOppositionDescriptor>(), true);
                                    }, AutoWidth());
                                }
                            }
                            SelectedSpellbook[ch.HashKey()] = spellbook;
                            todo = FactsEditor.OnGUI(ch, spellbook, selectedSpellbookLevel);
                        }
                    }
#if false
                    else {
                        spellbookEditCharacter = ch;
                        editSpellbooks = true;
                        var blueprints = BlueprintExensions.GetBlueprints<BlueprintSpellbook>().OrderBy((bp) => bp.GetDisplayName());
                        todo = BlueprintListUI.OnGUI(ch, blueprints, 100);
                    }
#endif
                }
                if (selectedCharacter != GetSelectedCharacter()) {
                    selectedCharacterIndex = characterList.IndexOf(selectedCharacter);
                }
                chIndex += 1;
            }
            Space(25);
            if (recruitableCount > 0) {
                Label($"{recruitableCount} character(s) can be ".orange().bold() + " Recruited".cyan() + ". This allows you to add non party NPCs to your party as if they were mercenaries".green());
            }
            if (respecableCount > 0) {
                Label($"{respecableCount} character(s)  can be ".orange().bold() + "Respecced".cyan() + ". Pressing Respec will close the mod window and take you to character level up".green());
                Label("WARNING".yellow().bold() + " The Respec UI is ".orange() + "Non Interruptable".yellow().bold() + " please save before using".orange());
            }
            if (recruitableCount > 0 || respecableCount > 0) {
                Label("WARNING".yellow().bold() + " these features are ".orange() + "EXPERIMENTAL".yellow().bold() + " and uses unreleased and likely buggy code.".orange());
                Label("BACK UP".yellow().bold() + " before playing with this feature.You will lose your mythic ranks but you can restore them in this Party Editor.".orange());
            }
            Space(25);
            foreach (var action in todo)
                action();
            if (charToAdd != null) { UnitEntityDataUtils.AddCompanion(charToAdd); }
            if (charToRecruit != null) { UnitEntityDataUtils.RecruitCompanion(charToRecruit); }
            if (charToRemove != null) { UnitEntityDataUtils.RemoveCompanion(charToRemove); }
            if (charToUnrecruit != null) { charToUnrecruit.Ensure<UnitPartCompanion>().SetState(CompanionState.None); charToUnrecruit.Remove<UnitPartCompanion>(); }
        }
    }
}