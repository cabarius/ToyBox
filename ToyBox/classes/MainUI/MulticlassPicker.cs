// Copyright < 2021 > Narria (github user Cabarius) - License: MIT
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Utility;
using ModKit;
using ToyBox.Multiclass;

namespace ToyBox {
    public class MulticlassPicker {
        public static Settings settings => Main.settings;

        public static void OnGUI(UnitEntityData ch, float indent = 100) {
            var targetString = ch == null
                    ? "creation of ".green() + "new characters" + "\nNote:".yellow().bold()
                        + " This value applies to ".orange() + "all saves".yellow().bold() + " and in the main menu".orange()
                   : $"when leveling up ".green() + ch.CharacterName.orange().bold() + "\nNote:".yellow().bold()
                        + " This applies only to the ".orange() + "current save.".yellow().bold();
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                UI.Label($"Configure multiclass classes and gestalt flags to use during {targetString}".green());
                UI.Space(25);
                UI.Toggle("Show Class Descriptions", ref settings.toggleMulticlassShowClassDescriptions);
            }
            UI.Space(15);
            var hasMulticlassMigration = settings.perSave.multiclassSettings.Count == 0 && settings.multiclassSettings.Count > 0;
            var hasGestaltMigration = settings.perSave.excludeClassesFromCharLevelSets.Count >= 0 && settings.excludeClassesFromCharLevelSets.Count > 0;
            var hasLevelAsLegendMigration = settings.perSave.charIsLegendaryHero.Count == 0 && settings.perSave.charIsLegendaryHero.Count > 0;
            if (hasMulticlassMigration || hasGestaltMigration || hasLevelAsLegendMigration) {
                UI.Div(indent);
                using (UI.HorizontalScope()) {
                    UI.Space(indent);
                    using (UI.VerticalScope()) {
                        UI.Label("the following options allow you to migrate previous settings that were stored in toybox to the new per setting save mechanism for ".green() + "Multi-class selections, Gestalt Flags and Allow Levels Past 20 ".cyan() + "\nNote:".orange() + "you may have configured this for a different save so use care in doing this migration".green());
                        if (hasMulticlassMigration)
                            using (UI.HorizontalScope()) {
                                UI.Label("Multi-class settings", UI.Width(300));
                                UI.Space(25);
                                UI.Label($"{settings.multiclassSettings.Count}".cyan());
                                UI.Space(25);
                                UI.ActionButton("Migrate", () => { settings.perSave.multiclassSettings = settings.multiclassSettings; Settings.SavePerSaveSettings(); });
                            }
                        if (hasGestaltMigration)
                            using (UI.HorizontalScope()) {
                                UI.Label("Gesalt Flags", UI.Width(300));
                                UI.Space(25);
                                UI.Label($"{settings.excludeClassesFromCharLevelSets.Count}".cyan());
                                UI.Space(25);
                                UI.ActionButton("Migrate", () => { settings.perSave.excludeClassesFromCharLevelSets = settings.excludeClassesFromCharLevelSets; Settings.SavePerSaveSettings(); });
                            }
                        if (hasLevelAsLegendMigration)
                            using (UI.HorizontalScope()) {
                                UI.Label("Chars Able To Exceed Level 20", UI.Width(300));
                                UI.Space(25);
                                UI.Label($"{settings.charIsLegendaryHero.Count}".cyan());
                                UI.Space(25);
                                UI.ActionButton("Migrate", () => { settings.perSave.charIsLegendaryHero = settings.charIsLegendaryHero; Settings.SavePerSaveSettings(); });
                            }
                    }
                }
                UI.Div(indent);
            }
            var options = MulticlassOptions.Get(ch);
            var classes = Game.Instance.BlueprintRoot.Progression.CharacterClasses;
            var mythicClasses = Game.Instance.BlueprintRoot.Progression.CharacterMythics;
            var showDesc = settings.toggleMulticlassShowClassDescriptions;
            foreach (var cl in classes) {
                if (PickerRow(ch, cl, options, indent)) {
                    MulticlassOptions.Set(ch, options);
                    Mod.Log("MulticlassOptions.Set");
                }
            }
            UI.Div(indent);
            if (showDesc) {
                using (UI.HorizontalScope()) {
                    UI.Space(indent); UI.Label("Mythic".cyan());
                }
            }
            foreach (var mycl in mythicClasses) {
                if (PickerRow(ch, mycl, options, indent)) {
                    MulticlassOptions.Set(ch, options);
                    Mod.Log("MulticlassOptions.Set");
                }
            }
        }

        public static bool PickerRow(UnitEntityData ch, BlueprintCharacterClass cl, MulticlassOptions options, float indent = 100) {
            var changed = false;
            var showDesc = settings.toggleMulticlassShowClassDescriptions;
            if (showDesc) UI.Div(indent);
            var cd = ch?.Progression.GetClassData(cl);
            var chArchetype = cd?.Archetypes.FirstOrDefault<BlueprintArchetype>();
            var archetypeOptions = options.ArchetypeOptions(cl);
            var showGestaltToggle = false;
            if (ch != null && cd != null) {
                var classes = ch?.Progression.Classes;
                var classCount = classes?.Count(x => !x.CharacterClass.IsMythic);
                var gestaltCount = classes?.Count(cd => !cd.CharacterClass.IsMythic && ch.IsClassGestalt(cd.CharacterClass));
                showGestaltToggle = !cd.CharacterClass.IsMythic && classCount - gestaltCount > 1 || ch.IsClassGestalt(cd.CharacterClass) || cd.CharacterClass.IsMythic;
            }
            var hasClass = cd != null && chArchetype == null;
            using (UI.HorizontalScope()) {
                bool showedGestalt = false;
                UI.Space(indent);
                UI.ActionToggle(
                     hasClass ? cl.Name.orange() : cl.Name,
                    () => options.Contains(cl) && (chArchetype == null || archetypeOptions.Contains(chArchetype)),
                    (v) => {
                        if (v) options.Add(cl);
                        else options.Remove(cl);
                        Mod.Trace($"PickerRow - multiclassOptions - class: {cl.HashKey()} - {options}>");
                        changed = true;
                    }, 350);
                if (showGestaltToggle && chArchetype == null) {
                    UI.ActionToggle("gestalt".grey(), () => ch.IsClassGestalt(cd.CharacterClass),
                        (v) => {
                            ch.SetClassIsGestalt(cd.CharacterClass, v);
                            ch.Progression.UpdateLevelsForGestalt();
                            changed = true;
                        }, 125);
                    UI.Space(25);
                    if (!showDesc)
                        UI.Label("this flag lets you not count this class in computing character level".green());
                    showedGestalt = true;
                }
                else UI.Space(157);
                if (showDesc) {
                    using (UI.VerticalScope()) {
                        if (showedGestalt) {
                            UI.Label("this flag lets you not count this class in computing character level".green());
                            UI.Div();
                        }
                        UI.Label(cl.Description.StripHTML().green());
                    }
                }
            }
            using (UI.HorizontalScope()) {
                bool showedGestalt = false;
                UI.Space(indent);
                var archetypes = cl.Archetypes;
                if (options.Contains(cl) && archetypes.Any() || chArchetype != null) {
                    UI.Space(50);
                    using (UI.VerticalScope()) {
                        foreach (var archetype in cl.Archetypes) {
                            if (!hasClass && chArchetype == null || chArchetype == archetype) {
                                if (showDesc) UI.Div();
                                using (UI.HorizontalScope()) {
                                    UI.ActionToggle(
                                    chArchetype != null ? archetype.Name.orange() : archetype.Name,
                                    () => archetypeOptions.Contains(archetype),
                                    (v) => {
                                        if (v) archetypeOptions.AddExclusive(archetype);
                                        else archetypeOptions.Remove(archetype);
                                        Mod.Trace($"PickerRow - archetypeOptions - {{{archetypeOptions}}}");
                                        changed = true;
                                    }, 300);
                                    options.SetArchetypeOptions(cl, archetypeOptions);
                                    if (showGestaltToggle && chArchetype != null) {
                                        UI.ActionToggle("gestalt".grey(), () => ch.IsClassGestalt(cd.CharacterClass),
                                            (v) => {
                                                ch.SetClassIsGestalt(cd.CharacterClass, v);
                                                ch.Progression.UpdateLevelsForGestalt();
                                                changed = true;
                                            }, 125);
                                        UI.Space(25);
                                        if (!showDesc)
                                            UI.Label("this flag lets you not count this class in computing character level".green());
                                        showedGestalt = true;
                                    }
                                    else UI.Space(157);
                                    if (showDesc) {
                                        using (UI.VerticalScope()) {
                                            if (showedGestalt) {
                                                UI.Label("this flag lets you not count this class in computing character level".green());
                                                UI.Div();
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
    }
#if false
    public class MulticlassPickerOld {

        public static void OnGUI(HashSet<string> multiclassSet, float indent = 100) {
            var classes = Game.Instance.BlueprintRoot.Progression.CharacterClasses;
            var mythicClasses = Game.Instance.BlueprintRoot.Progression.CharacterMythics;

            foreach (var cl in classes) {
                PickerRow(cl, multiclassSet, indent);
            }
            UI.Div(indent, 20);
            foreach (var mycl in mythicClasses) {
                using (UI.HorizontalScope()) {
                    PickerRow(mycl, multiclassSet, indent);
                }
            }
        }

        public static bool PickerRow(BlueprintCharacterClass cl, HashSet<string> multiclassSet, float indent = 100) {
            bool changed = false;
            bool showDesc = settings.toggleMulticlassShowClassDescriptions;
            if (showDesc) UI.Div(indent);
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                UI.ActionToggle(
                    cl.Name,
                    () => multiclassSet.Contains(cl.AssetGuid.ToString()),
                    (v) => {
                        if (v) multiclassSet.Add(cl.AssetGuid.ToString());
                        else multiclassSet.Remove(cl.AssetGuid.ToString());
                        Main.Log($"multiclassSet - class: {cl.AssetGuid.ToString()}- <{String.Join(", ", multiclassSet)}>");

                        changed = true;
                    },
                    350
                    );
                var archetypes = cl.Archetypes;
                if (multiclassSet.Contains(cl.AssetGuid.ToString()) && archetypes.Any()) {
                    UI.Space(50);
                    using (UI.VerticalScope()) {
                        var archetypeOptions = options.ArchetypeOptions(cl);
                        foreach (var archetype in cl.Archetypes) {
                            if (showDesc) UI.Div();
                            using (UI.HorizontalScope()) {
                                UI.ActionToggle(
                                archetype.Name,
                                () => archetypeOptions.Contains(archetype),
                                (v) => {
                                    if (v) archetypeOptions.AddExclusive(archetype);
                                    else archetypeOptions.Remove(archetype);
                                    Main.Log($"PickerRow - archetypeOptions - {{{archetypeOptions}}}");
                                },
                                350
                                );
                                options.SetArchetypeOptions(cl, archetypeOptions);
                                if (showDesc) UI.Label(archetype.Description.RemoveHtmlTags().green());
                            }
                        }
                    }
                }
            }
            return changed;
        }
    }
#endif
}
