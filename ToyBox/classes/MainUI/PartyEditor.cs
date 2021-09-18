// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Root;
using Kingmaker.Designers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using ToyBox.Multiclass;
using Alignment = Kingmaker.Enums.Alignment;
using ModKit;

namespace ToyBox {
    public class PartyEditor {
        public static Settings settings { get { return Main.settings; } }
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
        static UnitEntityData charToRemove = null;
        static bool editMultiClass = false;
        static UnitEntityData multiclassEditCharacter = null;
        static int respecableCount = 0;
        static int selectedSpellbook = 0;
        static int selectedSpellbookLevel = 0;
        static bool editSpellbooks = false;
        static UnitEntityData spellbookEditCharacter = null;
        static float nearbyRange = 25;
        static Alignment[] alignments = new Alignment[] {
                    Alignment.LawfulGood,       Alignment.NeutralGood,      Alignment.ChaoticGood,
                    Alignment.LawfulNeutral,    Alignment.TrueNeutral,      Alignment.ChaoticNeutral,
                    Alignment.LawfulEvil,       Alignment.NeutralEvil,      Alignment.ChaoticEvil
        };
        static Dictionary<String, int> statEditorStorage = new Dictionary<String, int>();
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

        public static void ActionsGUI(UnitEntityData ch) {
            var player = Game.Instance.Player;
            UI.Space(25);
            if (!player.PartyAndPets.Contains(ch)) {
                UI.ActionButton("Add", () => { charToAdd = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else if (player.ActiveCompanions.Contains(ch)) {
                UI.ActionButton("Remove", () => { charToRemove = ch; }, UI.Width(150));
                UI.Space(25);
            }
            else {
                UI.Space(178);
            }
            if (player.Party.Contains(ch)) {
                respecableCount++;
                UI.ActionButton("Respec", () => { Actions.ToggleModWindow(); UnitHelper.Respec(ch); }, UI.Width(150));
            }
            else {
                UI.Space(170);
            }
        }
        public static void OnGUI() {
            var player = Game.Instance.Player;
            var filterChoices = GetPartyFilterChoices();
            if (filterChoices == null) { return; }

            charToAdd = null;
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
                BlueprintStatProgression xpTable = BlueprintRoot.Instance.Progression.XPTable;
                int level = progression.CharacterLevel;
                int mythicLevel = progression.MythicExperience;
                var spellbooks = ch.Spellbooks;
                var spellCount = spellbooks.Sum((sb) => sb.GetAllKnownSpells().Count());
                using (UI.HorizontalScope()) {
                    if (isWide)
                        UI.Label(ch.CharacterName.orange().bold(), UI.MinWidth(100), UI.MaxWidth(600));
                    else
                        UI.Label(ch.CharacterName.orange().bold(), UI.Width(230));
                    UI.Space(5);
                    float distance = mainChar.DistanceTo(ch); ;
                    UI.Label(distance < 1 ? "" : distance.ToString("0") + "m", UI.Width(75));
                    UI.Space(5);
                    UI.Label("lvl".green() + $": {level}", UI.Width(75));
                    // Level up code adapted from Bag of Tricks https://www.nexusmods.com/pathfinderkingmaker/mods/2
                    if (player.AllCharacters.Contains(ch)) {
                        if (progression.Experience < xpTable.GetBonus(level + 1) && level < 20) {
                            UI.ActionButton("+1", () => {
                                progression.AdvanceExperienceTo(xpTable.GetBonus(level + 1), true);
                            }, UI.Width(70));
                        }
                        else if (progression.Experience >= xpTable.GetBonus(level + 1) && level < 20) {
                            UI.Label("LvUp".cyan().italic(), UI.Width(70));
                        }
                        else { UI.Space(74); }
                    }
                    else { UI.Space(74); }
                    UI.Space(5);
                    UI.Label($"my".green() + $": {mythicLevel}", UI.Width(80));
                    if (player.AllCharacters.Contains(ch)) {
                        if (progression.MythicExperience < 10) {
                            UI.ActionButton("+1", () => {
                                progression.AdvanceMythicExperience(progression.MythicExperience + 1, true);
                            }, UI.Width(70));
                        }
                        else { UI.Label("max".cyan(), UI.Width(70)); }
                    }
                    else { UI.Space(74); }
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
                            UI.Space(50);
                            UI.Label("Experimental - See 'Level Up + Multiclass' for more options and info".green());
                        }
                        else { UI.Space(50);  UI.Label("Experimental Preview ".magenta());  }
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
                            UI.ActionButton(">", () => prog.CharacterLevel = Math.Min(20, prog.CharacterLevel + 1), UI.AutoWidth());
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
                                int newXP = BlueprintRoot.Instance.Progression.XPTable.GetBonus(Mathf.RoundToInt(prog.CharacterLevel));
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
                        int alignmentIndex = Array.IndexOf(alignments, alignment);
                        var titles = alignments.Select(
                            a => a.Acronym().color(a.Color()).bold()).ToArray();
                        if (UI.SelectionGrid(ref alignmentIndex, titles, 3, UI.Width(250f))) {
                            ch.Descriptor.Alignment.Set(alignments[alignmentIndex]);
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
                    foreach (StatType obj in Enum.GetValues(typeof(StatType))) {
                        StatType statType = (StatType)obj;
                        ModifiableValue modifiableValue = ch.Stats.GetStat(statType);
                        if (modifiableValue != null) {
                            String key = $"{ch.CharacterName}-{statType.ToString()}";
                            var storedValue = statEditorStorage.ContainsKey(key) ? statEditorStorage[key] : modifiableValue.BaseValue;
                            var statName = statType.ToString();
                            if (statName == "BaseAttackBonus" || statName == "SkillAthletics") {
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
                            if (selectedSpellbook > names.Count()) selectedSpellbook = 0;
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
                                        var knownCount = spellbook.GetKnownSpells(lvl).Count();
                                        var countText = knownCount > 0 ? $" ({knownCount})".white() : "";
                                        return levelText + countText;
                                    },
                                    UI.AutoWidth()
                                );
                                if (casterLevel < 20) {
                                    UI.ActionButton("+1 Caster Level", () => spellbook.AddBaseLevel());
                                }
                            }
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
            if (respecableCount > 0) {
                UI.Label($"{respecableCount} characters".yellow().bold() + " can be respecced. Pressing Respec will close the mod window and take you to character level up".orange());
                UI.Label("WARNING".yellow().bold() + " this feature is ".orange() + "EXPERIMENTAL".yellow().bold() + " and uses unreleased and likely buggy code.".orange());
                UI.Label("BACK UP".yellow().bold() + " before playing with this feature.You will lose your mythic ranks but you can restore them in this Party Editor.".orange());

            }
            UI.Space(25);
            if (charToAdd != null) { UnitEntityDataUtils.AddCompanion(charToAdd); }
            if (charToRemove != null) { UnitEntityDataUtils.RemoveCompanion(charToRemove); }
        }
    }
}