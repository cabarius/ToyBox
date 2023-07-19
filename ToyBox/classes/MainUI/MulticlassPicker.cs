﻿// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using ModKit;
using System.Linq;
using System.Linq.Expressions;
#if Wrath
using ToyBox.Multiclass;
#endif

namespace ToyBox {
    public class MulticlassPicker {
        public static Settings settings => Main.Settings;

        public static void OnGUI(UnitEntityData ch, float indent = 100) {
            var targetString = ch == null
                                   ? ("creation of ".green() + "new characters" + "\nNote:".yellow().bold()
                                      + " This value applies to ".orange() + "all saves".yellow().bold() + " and in the main menu".orange()).localize()
                                   : "when leveling up ".localize().green() + ch.CharacterName.orange().bold() + ("\nNote:".yellow().bold()
                                                                                                                  + " This applies only to the ".orange() + "current save.".yellow().bold()).localize();
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                UI.Label("Configure multiclass classes and gestalt flags to use during ".localize() + $"{targetString}".green());
                UI.Space(25);
                UI.Toggle("Show Class Descriptions".localize(), ref settings.toggleMulticlassShowClassDescriptions);
            }
            UI.Space(15);
            MigrationOptions(indent);
            if (Game.Instance?.BlueprintRoot?.Progression == null) return;
            var options = MulticlassOptions.Get(ch);
            var classes = Game.Instance.BlueprintRoot.Progression.CharacterClasses;
            var mythicClasses = Game.Instance.BlueprintRoot.Progression.CharacterMythics;
            var showDesc = settings.toggleMulticlassShowClassDescriptions;
            if (ch != null) {
                using (UI.HorizontalScope()) {
                    UI.Space(indent);
                    UI.Label($"Character Level".localize().cyan().bold(), UI.Width(300));
                    UI.Space(25);
                    UI.Label(ch.Progression.CharacterLevel.ToString().orange().bold());
                }
                UI.Space(25);
            }
            foreach (var cl in classes) {
                if (PickerRow(ch, cl, options, indent)) {
                    MulticlassOptions.Set(ch, options);
                    Mod.Trace("MulticlassOptions.Set");
                }
            }
            UI.Space(10);
            UI.Div(indent);
            UI.Space(-3);
            if (showDesc) {
                using (UI.HorizontalScope()) {
                    UI.Space(indent); UI.Label("Mythic".localize().cyan());
                }
            }
            foreach (var mycl in mythicClasses) {
                if (PickerRow(ch, mycl, options, indent)) {
                    MulticlassOptions.Set(ch, options);
                    Mod.Trace("MulticlassOptions.Set");
                }
            }
        }

        public static bool PickerRow(UnitEntityData ch, BlueprintCharacterClass cl, MulticlassOptions options, float indent = 100) {
            var changed = false;
            var showDesc = settings.toggleMulticlassShowClassDescriptions;
            if (showDesc) UI.Div(indent, 15);
            var cd = ch?.Progression.GetClassData(cl);
            var chArchetype = cd?.Archetypes.FirstOrDefault<BlueprintArchetype>();
            var archetypeOptions = options.ArchetypeOptions(cl);
            var showGestaltToggle = false;
            if (ch != null && cd != null) {
                var classes = ch?.Progression.Classes;
                var classCount = classes?.Count(x => !x.CharacterClass.IsMythic);
                var gestaltCount = classes?.Count(cd => !cd.CharacterClass.IsMythic && ch.IsClassGestalt(cd.CharacterClass));
                var mythicCount = classes.Count(x => x.CharacterClass.IsMythic);
                var mythicGestaltCount = classes.Count(cd => cd.CharacterClass.IsMythic && ch.IsClassGestalt(cd.CharacterClass));

                showGestaltToggle = ch.IsClassGestalt(cd.CharacterClass)
                                    || !cd.CharacterClass.IsMythic && classCount - gestaltCount > 1
                                    || cd.CharacterClass.IsMythic && mythicCount - mythicGestaltCount > 1;
            }
            var charHasClass = cd != null && chArchetype == null;
            // Class Toggle
            var canSelectClass = MulticlassOptions.CanSelectClassAsMulticlass(ch, cl);
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                var optionsHasClass = options.Contains(cl);
                UI.ActionToggle(
                     charHasClass ? cl.Name.orange() + $" ({cd.Level})".orange() : cl.Name,
                    () => optionsHasClass,
                    (v) => {
                        if (v) {
                            archetypeOptions = options.Add(cl);
                            if (chArchetype != null) {
                                archetypeOptions.Add(chArchetype);
                                options.SetArchetypeOptions(cl, archetypeOptions);
                            }
                        }
                        else options.Remove(cl);
                        var action = v ? "Add".localize().green() : "Del".localize().yellow();
                        Mod.Trace($"PickerRow - {action} class: {cl.HashKey()} - {options} -> {options.Contains(cl)}");
                        changed = true;
                    },
                    () => !canSelectClass,
                    350);
                UI.Space(247);
                using (UI.VerticalScope()) {
                    if (!canSelectClass)
                        UI.Label("to select this class you must unselect at least one of your other existing classes".localize().orange());
                    if (optionsHasClass && chArchetype != null && archetypeOptions.Empty()) {
                        try {
                            var text = "due to existing archetype, %, this multiclass option will only be applied during respec.".localize().Split('%');
                            UI.Label($"{text[0]}{chArchetype.Name.yellow()}{text[1]}".orange());
                        }
                        catch {
                            UI.Label($"due to existing archetype, {chArchetype.Name.yellow()}, this multiclass option will only be applied during respec.".orange());
                        }
                    }
                    if (showGestaltToggle && chArchetype == null) {
                        using (UI.HorizontalScope()) {
                            UI.Space(-150);
                            UI.ActionToggle("gestalt".localize().grey(), () => ch.IsClassGestalt(cd.CharacterClass),
                                (v) => {
                                    ch.SetClassIsGestalt(cd.CharacterClass, v);
                                    ch.Progression.UpdateLevelsForGestalt();
                                    changed = true;
                                }, 125);
                            UI.Space(25);
                            UI.Label("this flag lets you not count this class in computing character level".localize().green());
                        }
                    }
                    if (showDesc) {
                        using (UI.HorizontalScope()) {
                            UI.Label(cl.Description.StripHTML().green());
                        }
                    }
                }
            }
            // Archetypes
            using (UI.HorizontalScope()) {
                var showedGestalt = false;
                UI.Space(indent);
                var archetypes = cl.Archetypes;
                if (options.Contains(cl) && archetypes.Any() || chArchetype != null || charHasClass) {
                    UI.Space(50);
                    using (UI.VerticalScope()) {
                        foreach (var archetype in cl.Archetypes) {
                            if (showDesc) UI.Div();
                            using (UI.HorizontalScope()) {
                                var hasArch = archetypeOptions.Contains(archetype);
                                UI.ActionToggle(
                                    archetype == chArchetype ? cd.ArchetypesName().orange() + $" ({cd.Level})".orange() : archetype.Name,
                                    () => hasArch,
                                    (v) => {
                                        if (v) archetypeOptions.AddExclusive(archetype);
                                        else archetypeOptions.Remove(archetype);
                                        options.SetArchetypeOptions(cl, archetypeOptions);
                                        var action = v ? "Add".localize().green() : "Del".localize().yellow();
                                        Mod.Trace($"PickerRow -  {action}  - arch: {archetype.HashKey()} - {archetypeOptions}");
                                        changed = true;
                                    },
                                    () => !canSelectClass,
                                    300);
                                UI.Space(250);
                                using (UI.VerticalScope()) {

                                    if (hasArch && archetype != chArchetype && (chArchetype != null || charHasClass)) {
                                        if (chArchetype != null) {
                                            try {
                                                var text = "due to existing archetype, %, this multiclass option will only be applied during respec.".localize().Split('%');
                                                UI.Label($"{text[0]}{chArchetype.Name.yellow()}{text[1]}".orange());
                                            }
                                            catch {
                                                UI.Label($"due to existing archetype, {chArchetype.Name.yellow()}, this multiclass option will only be applied during respec.".orange());
                                            }
                                        }
                                        else {
                                            try {
                                                var text = "due to existing class, %, this multiclass option will only be applied during respec.".localize().Split('%');
                                                UI.Label($"{text[0]}{cd.CharacterClass.Name.yellow()}{text[1]}".orange());
                                            }
                                            catch {
                                                UI.Label($"due to existing class, {chArchetype.Name.yellow()}, this multiclass option will only be applied during respec.".orange());
                                            }
                                        }
                                    }
                                    else if (showGestaltToggle && archetype == chArchetype) {
                                        using (UI.HorizontalScope()) {
                                            UI.Space(-155);
                                            UI.ActionToggle("gestalt".localize().grey(), () => ch.IsClassGestalt(cd.CharacterClass),
                                                (v) => {
                                                    ch.SetClassIsGestalt(cd.CharacterClass, v);
                                                    ch.Progression.UpdateLevelsForGestalt();
                                                    changed = true;
                                                }, 125);
                                            UI.Space(25);
                                            UI.Label("this flag lets you not count this class in computing character level".localize().green());
                                            showedGestalt = true;
                                        }
                                    }
                                    if (showDesc) {
                                        using (UI.VerticalScope()) {
                                            if (showedGestalt) {
                                                UI.Label("this flag lets you not count this class in computing character level".localize().green());
                                                UI.DivLast();
                                            }
                                            UI.Label(archetype.Description.StripHTML().green());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return changed;
        }
        public static bool areYouSure1 = false;
        public static bool areYouSure2 = false;
        public static bool areYouSure3 = false;
        public static void MigrationOptions(float indent) {
            if (!Main.IsInGame) return;
            var hasMulticlassMigration = settings.multiclassSettings.Count > 0
                && (settings.toggleAlwaysShowMigration || settings.perSave.multiclassSettings.Count == 0);
            var hasGestaltMigration = settings.excludeClassesFromCharLevelSets.Count > 0
                && (settings.toggleAlwaysShowMigration || settings.perSave.excludeClassesFromCharLevelSets.Count == 0);
            var hasLevelAsLegendMigration = settings.perSave.charIsLegendaryHero.Count > 0
                && (settings.toggleAlwaysShowMigration || settings.perSave.charIsLegendaryHero.Count == 0);
            var hasAvailableMigrations = hasMulticlassMigration || hasGestaltMigration || hasLevelAsLegendMigration;
            var migrationCount = settings.multiclassSettings.Count + settings.excludeClassesFromCharLevelSets.Count + settings.charIsLegendaryHero.Count;
            if (migrationCount > 0) {
                using (UI.HorizontalScope()) {
                    UI.Space(indent);
                    UI.Toggle("Show Migrations".localize(), ref settings.toggleAlwaysShowMigration);
                    UI.Space(25);
                    UI.Label(("toggle this if you want show older ToyBox settings for ".green() + "Multi-class selections, Gestalt Flags and Allow Levels Past 20 ".cyan()).localize());
                }
            }
            if (migrationCount > 0) {
                UI.Div(indent);
                if (hasAvailableMigrations) {
                    using (UI.HorizontalScope()) {
                        UI.Space(indent);
                        using (UI.VerticalScope()) {
                            UI.Label(("the following options allow you to migrate previous settings that were stored in toybox to the new per setting save mechanism for ".green() + "Multi-class selections, Gestalt Flags and Allow Levels Past 20 ".cyan() + "\nNote:".orange() + "you may have configured this for a different save so use care in doing this migration".green()).localize());
                            if (hasMulticlassMigration)
                                using (UI.HorizontalScope()) {
                                    UI.Label("Multi-class settings".localize(), UI.Width(300));
                                    UI.Space(25);
                                    UI.Label($"{settings.multiclassSettings.Count}".cyan());
                                    UI.Space(25);
                                    UI.ActionButton("Migrate".localize(), () => { settings.perSave.multiclassSettings = settings.multiclassSettings; Settings.SavePerSaveSettings(); });
                                    UI.Space(25);
                                    UI.DangerousActionButton("Remove".localize(), "this will remove your old multiclass settings from ToyBox settings but does not affect any other saves that have already migrated them".localize(), ref areYouSure1, () => settings.multiclassSettings.Clear());
                                }
                            if (hasGestaltMigration)
                                using (UI.HorizontalScope()) {
                                    UI.Label("Gestalt Flags".localize(), UI.Width(300));
                                    UI.Space(25);
                                    UI.Label($"{settings.excludeClassesFromCharLevelSets.Count}".cyan());
                                    UI.Space(25);
                                    UI.ActionButton("Migrate".localize(), () => {
                                        settings.perSave.excludeClassesFromCharLevelSets = settings.excludeClassesFromCharLevelSets; Settings.SavePerSaveSettings();
                                        MultipleClasses.SyncAllGestaltState();
                                    });
                                    UI.Space(25);
                                    UI.DangerousActionButton("Remove".localize(), "this will remove your old gestalt flags from ToyBox settings but does not affect any other saves that have already migrated them".localize(), ref areYouSure2, () => settings.excludeClassesFromCharLevelSets.Clear());
                                }
                            if (hasLevelAsLegendMigration)
                                using (UI.HorizontalScope()) {
                                    UI.Label("Chars Able To Exceed Level 20".localize(), UI.Width(300));
                                    UI.Space(25);
                                    UI.Label($"{settings.charIsLegendaryHero.Count}".cyan());
                                    UI.Space(25);
                                    UI.ActionButton("Migrate".localize(), () => { settings.perSave.charIsLegendaryHero = settings.charIsLegendaryHero; Settings.SavePerSaveSettings(); });
                                    UI.Space(25);
                                    UI.DangerousActionButton("Remove".localize(), "this will remove your old Allow Level Past 20 flags from ToyBox settings but does not affect any other saves that have already migrated them".localize(), ref areYouSure3, () => settings.charIsLegendaryHero.Clear());
                                }
                        }
                    }
                    UI.Div(indent);
                }
            }
        }
    }
}