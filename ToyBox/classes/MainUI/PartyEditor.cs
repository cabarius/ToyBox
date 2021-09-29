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
using ModKit.Utility;
using ToyBox.classes.Infrastructure;
using Kingmaker.PubSubSystem;
using Kingmaker.Blueprints;

namespace ToyBox {
    public class PartyEditor {
        public static Settings settings => Main.settings;

        enum ToggleChoice {
            Classes,
            Stats,
            Facts,
            Buffs,
            Abilities,
            Spells,
            None,
        };
        static ToggleChoice selectedToggle = ToggleChoice.None;
        static int selectedCharacterIndex = 0;
        static UnitEntityData charToAdd = null;
        static UnitEntityData charToRecruit = null;
        static UnitEntityData charToRemove = null;
        static bool editMultiClass = false;
        static UnitEntityData multiclassEditCharacter = null;
        static int respecableCount = 0;
        static int recruitableCount = 0;
        static int selectedSpellbook = 0;
        static (string, string) nameEditState = (null, null);
        public static int selectedSpellbookLevel = 0;
        static bool editSpellbooks = false;
        static UnitEntityData spellbookEditCharacter = null;
        static float nearbyRange = 25;
        static Dictionary<String, int> statEditorStorage = new Dictionary<String, int>();
        public static Dictionary<string, Spellbook> SelectedSpellbook = new Dictionary<string, Spellbook>();
        private static NamedFunc<List<UnitEntityData>>[] partyFilterChoices = null;
        private static Player partyFilterPlayer = null;
        public static NamedFunc<List<UnitEntityData>>[] GetPartyFilterChoices() {
            if (partyFilterPlayer != Game.Instance.Player) partyFilterChoices = null;
            if (Game.Instance.Player != null && partyFilterChoices == null) {
                partyFilterChoices = new NamedFunc<List<UnitEntityData>>[] {
                    new NamedFunc<List<UnitEntityData>>("Party", () => Game.Instance.Player.Party),
                    new NamedFunc<List<UnitEntityData>>("Party & Pets", () => Game.Instance.Player.m_PartyAndPets),
                    new NamedFunc<List<UnitEntityData>>("All", () => Game.Instance.Player.AllCharacters),
                    new NamedFunc<List<UnitEntityData>>("Active", () => Game.Instance.Player.ActiveCompanions),
                    new NamedFunc<List<UnitEntityData>>("Remote", () => Game.Instance.Player.m_RemoteCompanions),
                    new NamedFunc<List<UnitEntityData>>("Custom", PartyUtils.GetCustomCompanions),
                    new NamedFunc<List<UnitEntityData>>("Pets", PartyUtils.GetPets),
                    new NamedFunc<List<UnitEntityData>>("Nearby", () => {
                        var player = GameHelper.GetPlayerCharacter();
                        if (player == null) return new List<UnitEntityData> ();
                        return GameHelper.GetTargetsAround(GameHelper.GetPlayerCharacter().Position, nearbyRange , false, false).ToList();
                    }),
                    new NamedFunc<List<UnitEntityData>>("Friendly", () => Game.Instance.State.Units.Where((u) => u != null && !u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("Enemies", () => Game.Instance.State.Units.Where((u) => u != null && u.IsEnemy(GameHelper.GetPlayerCharacter())).ToList()),
                    new NamedFunc<List<UnitEntityData>>("All Units", () => Game.Instance.State.Units.ToList()),
               };
            }
            return partyFilterChoices;
        }
        public static List<UnitEntityData> GetCharacterList() {
            var partyFilterChoices = GetPartyFilterChoices();
            if (partyFilterChoices == null) { return null; }
            return partyFilterChoices[Main.settings.selectedPartyFilter].func();
        }
        static UnitEntityData GetSelectedCharacter() {
            var characterList = GetCharacterList();
            if (characterList == null || characterList.Count == 0) return null;
            if (selectedCharacterIndex >= characterList.Count) selectedCharacterIndex = 0;
            return characterList[selectedCharacterIndex];
        }
        public static void ResetGUI() {
            selectedCharacterIndex = 0;
            selectedSpellbook = 0;
            selectedSpellbookLevel = 0;
            partyFilterChoices = null;
            Main.settings.selectedPartyFilter = 0;
        }

        // This bit of kludge is added in order to tell whether our generic actions are being accessed from this screen or the Search n' Pick
        public static bool IsOnPartyEditor() {
            return Main.settings.selectedTab == 2;
        }

        public static void ActionsGUI(UnitEntityData ch) {
            var player = Game.Instance.Player;
            UI.Space(25);
            if (!player.PartyAndPets.Contains(ch) && player.AllCharacters.Contains(ch)) {
                UI.ActionButton("Add", () => { charToAdd = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else if (player.ActiveCompanions.Contains(ch)) {
                UI.ActionButton("Remove", () => { charToRemove = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else if (!player.AllCharacters.Contains(ch)) {
                recruitableCount++;
                UI.ActionButton("Recruit".cyan(), () => { charToRecruit = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else {
                UI.Space(178);
            }
            if (RespecHelper.GetRespecableUnits().Contains(ch)) {
                respecableCount++;
                UI.ActionButton("Respec".cyan(), () => { Actions.ToggleModWindow(); RespecHelper.Respec(ch); }, UI.Width(150));
            }
            else {
                UI.Space(153);
            }
#if DEBUG
            UI.Space(25);
            UI.ActionButton("Log Caster Info", () => CasterHelpers.GetOriginalCasterLevel(ch.Descriptor),
                UI.AutoWidth());
#endif
        }
        public static void OnGUI() {
            var player = Game.Instance.Player;
            var filterChoices = GetPartyFilterChoices();
            if (filterChoices == null) { return; }

            charToAdd = null;
            charToRecruit = null;
            charToRemove = null;
            var characterListFunc = UI.TypePicker<List<UnitEntityData>>(
                null,
                ref Main.settings.selectedPartyFilter,
                filterChoices
                );
            var characterList = characterListFunc.func();
            var mainChar = GameHelper.GetPlayerCharacter();
            if (characterListFunc.name == "Nearby") {
                UI.Slider("Nearby Distance", ref nearbyRange, 1f, 200, 25, 0, " meters", UI.Width(250));
                characterList = characterList.OrderBy((ch) => ch.DistanceTo(mainChar)).ToList();
            }
            UI.Space(20);
            int chIndex = 0;
            recruitableCount = 0;
            respecableCount = 0;
            var selectedCharacter = GetSelectedCharacter();
            bool isWide = UI.IsWide;
            if (Main.IsInGame) {
                using (UI.HorizontalScope()) {
                    UI.Label($"Party Level ".cyan() + $"{Game.Instance.Player.PartyLevel}".orange().bold(), UI.AutoWidth());
                    UI.Space(25);
#if false   // disabled until we fix performance
                    var encounterCR = CheatsCombat.GetEncounterCr();
                    if (encounterCR > 0) {
                        UI.Label($"Encounter CR ".cyan() + $"{encounterCR}".orange().bold(), UI.AutoWidth());
                    }
#endif
                }
            }
            foreach (UnitEntityData ch in characterList) {
                var classData = ch.Progression.Classes;
                // TODO - understand the difference between ch.Progression and ch.Descriptor.Progression
                UnitProgressionData progression = ch.Descriptor.Progression;
                BlueprintStatProgression xpTable = progression.ExperienceTable;
                int level = progression.CharacterLevel;
                int mythicLevel = progression.MythicLevel;
                var spellbooks = ch.Spellbooks;
                var spellCount = spellbooks.Sum((sb) => sb.GetAllKnownSpells().Count());
                bool isOnTeam = player.AllCharacters.Contains(ch);
                using (UI.HorizontalScope()) {
                    var name = ch.CharacterName;
                    if (Game.Instance.Player.AllCharacters.Contains(ch)) {
                        if (isWide) {
                            if (UI.EditableLabel(ref name, ref nameEditState, 200, n => n.orange().bold(), UI.MinWidth(100), UI.MaxWidth(600))) {
                                ch.Descriptor.CustomName = name;
                                // TODO - why does this cause a piece of the turn based UI come up?
                                // Game.Instance.ScheduleAction(() => Game.ResetUI());
                            }
                        }
                        else
                            if (UI.EditableLabel(ref name, ref nameEditState, 200, n => n.orange().bold(), UI.Width(230))) {
                            ch.Descriptor.CustomName = name;
                            // TODO - why does this cause a piece of the turn based UI come up?
                            //Game.Instance.ScheduleAction(() => Game.ResetUI());
                        }
                    }
                    else {
                        if (isWide)
                            UI.Label(ch.CharacterName.orange().bold(), UI.MinWidth(100), UI.MaxWidth(600));
                        else
                            UI.Label(ch.CharacterName.orange().bold(), UI.Width(230));
                    }
                    UI.Space(5);
                    float distance = mainChar.DistanceTo(ch); ;
                    UI.Label(distance < 1 ? "" : distance.ToString("0") + "m", UI.Width(75));
                    UI.Space(5);
                    int nextLevel;
                    for (nextLevel = level; progression.Experience >= xpTable.GetBonus(nextLevel+1) && xpTable.HasBonusForLevel(nextLevel+1); nextLevel++) { }
                    if (nextLevel <= level || !isOnTeam) 
                        UI.Label((level < 10 ? "   lvl" : "   lv").green() + $" {level}", UI.Width(90));
                    else
                        UI.Label((level < 10 ? "  " : "") + $"{level} > "+ $"{nextLevel}".cyan(), UI.Width(90));
                    // Level up code adapted from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/2
                    if (player.AllCharacters.Contains(ch)) {
                        if (xpTable.HasBonusForLevel(nextLevel + 1)) {
                            UI.ActionButton("+1", () => {
                                progression.AdvanceExperienceTo(xpTable.GetBonus(nextLevel + 1), true);
                            }, UI.Width(63));
                        }
                        else { UI.Label("max", UI.Width(63)); }
                    }
                    else { UI.Space(66); }
                    UI.Space(10);
                    int nextML = progression.MythicExperience;
                    if (nextML <= mythicLevel || !isOnTeam)
                        UI.Label((mythicLevel < 10 ? "  my" : "  my").green() + $" {mythicLevel}", UI.Width(90));
                    else
                        UI.Label((level < 10 ? "  " : "") + $"{mythicLevel} > " + $"{nextML}".cyan(), UI.Width(90));
                    if (player.AllCharacters.Contains(ch)) {
                        if (progression.MythicExperience < 10) {
                            UI.ActionButton("+1", () => {
                                progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                            }, UI.Width(63));
                        }
                        else { UI.Label("max", UI.Width(63)); }
                    }
                    else { UI.Space(66); }
                    UI.Space(30);
                    if (!isWide) ActionsGUI(ch);
                    UI.Wrap(!UI.IsWide, 283, 0);
                    bool showClasses = ch == selectedCharacter && selectedToggle == ToggleChoice.Classes;
                    if (UI.DisclosureToggle($"{classData.Count} Classes", ref showClasses)) {
                        if (showClasses) {
                            selectedCharacter = ch; selectedToggle = ToggleChoice.Classes; Main.Log($"selected {ch.CharacterName}");
                        }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    bool showStats = ch == selectedCharacter && selectedToggle == ToggleChoice.Stats;
                    if (UI.DisclosureToggle("Stats", ref showStats, 125)) {
                        if (showStats) { selectedCharacter = ch; selectedToggle = ToggleChoice.Stats; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    UI.Wrap(UI.IsNarrow, 279);
                    bool showFacts = ch == selectedCharacter && selectedToggle == ToggleChoice.Facts;
                    if (UI.DisclosureToggle("Facts", ref showFacts, 125)) {
                        if (showFacts) { selectedCharacter = ch; selectedToggle = ToggleChoice.Facts; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    bool showBuffs = ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs;
                    if (UI.DisclosureToggle("Buffs", ref showBuffs, 125)) {
                        if (showBuffs) { selectedCharacter = ch; selectedToggle = ToggleChoice.Buffs; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    UI.Wrap(UI.IsNarrow, 304);
                    bool showAbilities = ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities;
                    if (UI.DisclosureToggle("Abilities", ref showAbilities, 125)) {
                        if (showAbilities) { selectedCharacter = ch; selectedToggle = ToggleChoice.Abilities; }
                        else { selectedToggle = ToggleChoice.None; }
                    }
                    UI.Space(10);
                    if (spellbooks.Count() > 0) {
                        bool showSpells = ch == selectedCharacter && selectedToggle == ToggleChoice.Spells;
                        if (UI.DisclosureToggle($"{spellCount} Spells", ref showSpells)) {
                            if (showSpells) { selectedCharacter = ch; selectedToggle = ToggleChoice.Spells; }
                            else { selectedToggle = ToggleChoice.None; }
                        }
                    }
                    else { UI.Space(180); }
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
                    UI.Div(100, 20);
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Toggle("Multiple Classes On Level-Up", ref settings.toggleMulticlass, 0);
                        if (settings.toggleMulticlass) {
                            UI.Space(40);
                            if (UI.DisclosureToggle("Config".orange().bold(), ref editMultiClass)) {
                                multiclassEditCharacter = selectedCharacter;
                            }
                            UI.Space(53);
                            UI.Label("Experimental - See 'Level Up + Multiclass' for more options and info".green());
                        }
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.ActionToggle("Allow Levels Past 20",
                            () => {
                                bool hasValue = settings.charIsLegendaryHero.TryGetValue(ch.HashKey(), out bool isLegendaryHero);
                                return hasValue && isLegendaryHero;
                            },
                            (val) => {
                                if (settings.charIsLegendaryHero.ContainsKey(ch.HashKey())) {
                                    settings.charIsLegendaryHero[ch.HashKey()] = val;
                                }
                                else {
                                    settings.charIsLegendaryHero.Add(ch.HashKey(), val);
                                }
                            },
                            0f,
                            UI.AutoWidth());
                        UI.Space(380);
                        UI.Label("Tick this to let your character exceed the level 20 level cap like the Legend mythic path".green());
                    }
#endif
                    UI.Div(100, 20);
                    if (editMultiClass) {
                        var options = MulticlassOptions.Get(ch);
                        MulticlassPicker.OnGUI(options);
                        MulticlassOptions.Set(ch, options);
                    }
                    else {
                        var prog = ch.Descriptor.Progression;
                        using (UI.HorizontalScope()) {
                            UI.Space(100);
                            UI.Label("Character Level".cyan(), UI.Width(250));
                            UI.ActionButton("<", () => prog.CharacterLevel = Math.Max(0, prog.CharacterLevel - 1), UI.AutoWidth());
                            UI.Space(25);
                            UI.Label("level".green() + $": {prog.CharacterLevel}", UI.Width(100f));
                            UI.ActionButton(">", () => prog.CharacterLevel = Math.Min(prog.MaxCharacterLevel, prog.CharacterLevel + 1), UI.AutoWidth());
                            UI.Space(25);
                            UI.ActionButton("Reset", () => ch.resetClassLevel(), UI.Width(125));
                            UI.Space(23);
                            using (UI.VerticalScope()) {
                                UI.Label("This directly changes your character level but will not change exp or adjust any features associated with your character. To do a normal level up use +1 Lvl above.  This gets recalculated when you reload the game.  ".green() + "If you want to alter default character level mark classes you want to exclude from the calculation with ".orange() + "gestalt".orange().bold() + " which means those levels were added for multi-classing. See the link for more information on this campaign variant.".orange());
                                UI.LinkButton("Gestalt Characters", "https://www.d20srd.org/srd/variant/classes/gestaltCharacters.htm");
                            }
                        }
                        using (UI.HorizontalScope()) {
                            UI.Space(100);
                            UI.Label("Experience".cyan(), UI.Width(250));
                            UI.Space(82);
                            UI.Label($"{prog.Experience}", UI.Width(150f));
                            UI.Space(36);
                            UI.ActionButton("Set", () => {
                                int newXP = prog.ExperienceTable.GetBonus(Mathf.RoundToInt(prog.CharacterLevel));
                                prog.Experience = newXP;
                            }, UI.Width(125));
                            UI.Space(23);
                            UI.Label("This sets your experience to match the current value of character level.".green());
                        }
                        UI.Div(100, 25);
                        using (UI.HorizontalScope()) {
                            UI.Space(100);
                            UI.Label("Mythic Level".cyan(), UI.Width(250));
                            UI.ActionButton("<", () => prog.MythicLevel = Math.Max(0, prog.MythicLevel - 1), UI.AutoWidth());
                            UI.Space(25);
                            UI.Label("my lvl".green() + $": {prog.MythicLevel}", UI.Width(100f));
                            UI.ActionButton(">", () => prog.MythicLevel = Math.Min(10, prog.MythicLevel + 1), UI.AutoWidth());
                            UI.Space(175);
                            UI.Label("This directly changes your mythic level but will not adjust any features associated with your character. To do a normal mythic level up use +1 my above".green());
                        }
                        var classCount = classData.Count;
                        var gestaltCount = classData.Count(cd => ch.IsClassGestalt(cd.CharacterClass));
                        foreach (var cd in classData) {
                            UI.Div(100, 20);
                            using (UI.HorizontalScope()) {
                                UI.Space(100);
                                UI.Label(cd.CharacterClass.Name.orange(), UI.Width(250));
                                UI.ActionButton("<", () => cd.Level = Math.Max(0, cd.Level - 1), UI.AutoWidth());
                                UI.Space(25);
                                UI.Label("level".green() + $": {cd.Level}", UI.Width(100f));
                                var maxLevel = cd.CharacterClass.Progression.IsMythic ? 10 : 20;
                                UI.ActionButton(">", () => cd.Level = Math.Min(maxLevel, cd.Level + 1), UI.AutoWidth());
                                UI.Space(23);
                                if (classCount - gestaltCount > 1 || ch.IsClassGestalt(cd.CharacterClass) == true) {
                                    UI.ActionToggle(
                                        "gestalt".grey(),
                                        () => ch.IsClassGestalt(cd.CharacterClass),
                                        (v) => {
                                            ch.SetClassIsGestalt(cd.CharacterClass, v);
                                            ch.Progression.UpdateLevelsForGestalt();
                                        },
                                        125
                                        );
                                }
                                else UI.Space(125);
                                UI.Space(27);
                                UI.Label(cd.CharacterClass.Description.RemoveHtmlTags().green(), UI.AutoWidth());
                            }
                        }
                    }
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Stats) {
                    UI.Div(100, 20, 755);
                    var alignment = ch.Descriptor.Alignment.ValueRaw;
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Alignment", UI.Width(425));
                        UI.Label($"{alignment.Name()}".color(alignment.Color()).bold(), UI.Width(1250f));
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        int alignmentIndex = Array.IndexOf(WrathExtensions.Alignments, alignment);
                        var titles = WrathExtensions.Alignments.Select(
                            a => a.Acronym().color(a.Color()).bold()).ToArray();
                        if (UI.SelectionGrid(ref alignmentIndex, titles, 3, UI.Width(250f))) {
                            ch.Descriptor.Alignment.Set(WrathExtensions.Alignments[alignmentIndex]);
                        }
                    }
                    UI.Div(100, 20, 755);
                    var alignmentMask = ch.Descriptor.Alignment.m_LockedAlignmentMask;
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Alignment Lock", UI.Width(425));
                        //UI.Label($"{alignmentMask.ToString()}".color(alignmentMask.Color()).bold(), UI.Width(325));
                        UI.Label($"Experimental - this sets a mask on your alignment shifts. {"Warning".bold().orange()}{": Using this may change your alignment.".orange()}".green());
                    }

                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        int maskIndex = Array.IndexOf(WrathExtensions.AlignmentMasks, alignmentMask);
                        var titles = WrathExtensions.AlignmentMasks.Select(
                            a => a.ToString().color(a.Color()).bold()).ToArray();
                        if (UI.SelectionGrid(ref maskIndex, titles, 3, UI.Width(800))) {
                            ch.Descriptor.Alignment.LockAlignment(WrathExtensions.AlignmentMasks[maskIndex], new Alignment?());
                        }
                    }
                    UI.Div(100, 20, 755);
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Size", UI.Width(425));
                        var size = ch.Descriptor.State.Size;
                        UI.Label($"{size}".orange().bold(), UI.Width(175));
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        UI.EnumGrid(
                            () => ch.Descriptor.State.Size,
                            (s) => ch.Descriptor.State.Size = s,
                            3, UI.Width(600));
                    }
                    using (UI.HorizontalScope()) {
                        UI.Space(528);
                        UI.ActionButton("Reset", () => { ch.Descriptor.State.Size = ch.Descriptor.OriginalSize; }, UI.Width(197));
                    }
                    UI.Div(100, 20, 755);
                    using (UI.HorizontalScope()) {
                        UI.Space(100);
                        UI.Label("Gender", UI.Width(400));
                        UI.Space(25);
                        var gender = ch.Descriptor.CustomGender ?? ch.Descriptor.Gender;
                        bool isFemale = gender == Gender.Female;
                        using (UI.HorizontalScope(UI.Width(200))) {
                            if (UI.Toggle(isFemale ? "Female" : "Male", ref isFemale,
                                "♀".color(RGBA.magenta).bold(),
                                "♂".color(RGBA.aqua).bold(),
                                0, UI.largeStyle, GUI.skin.box, UI.Width(300), UI.Height(20))) {
                                ch.Descriptor.CustomGender = isFemale ? Gender.Female : Gender.Male;
                            }
                        }
                        UI.Label("Changing your gender may cause visual glitches".green());
                    }
                    UI.Space(10);
                    UI.Div(100, 20, 755);
                    foreach (StatType obj in HumanFriendly.StatTypes) {
                        StatType statType = (StatType)obj;
                        ModifiableValue modifiableValue = ch.Stats.GetStat(statType);
                        if (modifiableValue == null) {
                            continue;
                        }

                        string key = $"{ch.CharacterName}-{statType.ToString()}";
                        int storedValue = statEditorStorage.ContainsKey(key) ? statEditorStorage[key] : modifiableValue.BaseValue;
                        string statName = statType.ToString();
                        if (statName == "BaseAttackBonus" || statName == "SkillAthletics" || statName == "HitPoints") {
                            UI.Div(100, 20, 755);
                        }
                        using (UI.HorizontalScope()) {
                            UI.Space(100);
                            UI.Label(statName, UI.Width(400f));
                            UI.Space(25);
                            UI.ActionButton(" < ", () => {
                                modifiableValue.BaseValue -= 1;
                                storedValue = modifiableValue.BaseValue;
                            }, UI.AutoWidth());
                            UI.Space(20);
                            UI.Label($"{modifiableValue.BaseValue}".orange().bold(), UI.Width(50f));
                            UI.ActionButton(" > ", () => {
                                modifiableValue.BaseValue += 1;
                                storedValue = modifiableValue.BaseValue;
                            }, UI.AutoWidth());
                            UI.Space(25);
                            UI.ActionIntTextField(ref storedValue, statType.ToString(), (v) => {
                                modifiableValue.BaseValue = v;
                            }, null, UI.Width(75));
                            statEditorStorage[key] = storedValue;
                        }
                    }
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Facts) {
                    FactsEditor.OnGUI(ch, ch.Progression.Features.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Buffs) {
                    FactsEditor.OnGUI(ch, ch.Descriptor.Buffs.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Abilities) {
                    FactsEditor.OnGUI(ch, ch.Descriptor.Abilities.Enumerable.ToList());
                }
                if (ch == selectedCharacter && selectedToggle == ToggleChoice.Spells) {
                    UI.Space(20);
                    var names = spellbooks.Select((sb) => sb.Blueprint.GetDisplayName()).ToArray();
                    var titles = names.Select((name, i) => $"{name} ({spellbooks.ElementAt(i).CasterLevel})").ToArray();
                    if (spellbooks.Any()) {
                        using (UI.HorizontalScope()) {
                            UI.SelectionGrid(ref selectedSpellbook, titles, 7, UI.Width(1581));
                            if (selectedSpellbook >= names.Length) selectedSpellbook = 0;
                            UI.DisclosureToggle("Edit", ref editSpellbooks);
                        }
                        var spellbook = spellbooks.ElementAt(selectedSpellbook);
                        if (editSpellbooks) {
                            spellbookEditCharacter = ch;
                            var blueprints = BlueprintExensions.GetBlueprints<BlueprintSpellbook>().OrderBy((bp) => bp.GetDisplayName());
                            BlueprintListUI.OnGUI(ch, blueprints, 100);
                        }
                        else {
                            var maxLevel = spellbook.Blueprint.MaxSpellLevel;
                            var casterLevel = spellbook.CasterLevel;
                            using (UI.HorizontalScope()) {
                                UI.EnumerablePicker<int>(
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
                                    UI.AutoWidth()
                                );
                                UI.Space(20);
                                if (casterLevel > 0) {
                                    UI.ActionButton("-1 CL", () => CasterHelpers.LowerCasterLevel(spellbook), UI.AutoWidth());
                                }
                                if (casterLevel < 40) {
                                    UI.ActionButton("+1 CL", () => CasterHelpers.AddCasterLevel(spellbook), UI.AutoWidth());
                                }

                                UI.Space(20);
                                if (ch.Spellbooks.Where(x => x.IsStandaloneMythic && !spellbook.IsStandaloneMythic && x.Blueprint.CharacterClass != null).Any(y => y.Blueprint.CharacterClass == ch.Progression.GetMythicToMerge()?.CharacterClass)) {
                                    UI.ActionButton("Merge Mythic Levels and Selected Spellbook", () => CasterHelpers.ForceSpellbookMerge(spellbook), UI.AutoWidth());
                                    UI.Label("Warning: This is irreversible. Please save before continuing!".Orange());
                                }
                            }
                            SelectedSpellbook[ch.HashKey()] = spellbook;
                            FactsEditor.OnGUI(ch, spellbook, selectedSpellbookLevel);
                        }
                    }
#if false
                    else {
                        spellbookEditCharacter = ch;
                        editSpellbooks = true;
                        var blueprints = BlueprintExensions.GetBlueprints<BlueprintSpellbook>().OrderBy((bp) => bp.GetDisplayName());
                        BlueprintListUI.OnGUI(ch, blueprints, 100);
                    }
#endif
                }
                if (selectedCharacter != GetSelectedCharacter()) {
                    selectedCharacterIndex = characterList.IndexOf(selectedCharacter);
                }
                chIndex += 1;
            }
            UI.Space(25);
            if (recruitableCount > 0) {
                UI.Label($"{recruitableCount} character(s) can be ".orange().bold() + " Recruited".cyan() + ". This allows you to add non party NPCs to your party as if they were mercenaries".green());
            }
            if (respecableCount > 0) {
                UI.Label($"{respecableCount} character(s)  can be ".orange().bold() + "Respecced".cyan() + ". Pressing Respec will close the mod window and take you to character level up".green());
                UI.Label("WARNING".yellow().bold() + " The Respec UI is ".orange() + "Non Interruptable".yellow().bold() + " please save before using".orange());
            }
            if (recruitableCount > 0 || respecableCount > 0) {
                UI.Label("WARNING".yellow().bold() + " these features are ".orange() + "EXPERIMENTAL".yellow().bold() + " and uses unreleased and likely buggy code.".orange());
                UI.Label("BACK UP".yellow().bold() + " before playing with this feature.You will lose your mythic ranks but you can restore them in this Party Editor.".orange());
            }
            UI.Space(25);
            if (charToAdd != null) { UnitEntityDataUtils.AddCompanion(charToAdd); }
            if (charToRecruit != null) { UnitEntityDataUtils.AddCompanion(charToRecruit); }
            if (charToRemove != null) { UnitEntityDataUtils.RemoveCompanion(charToRemove); }
        }
    }
}