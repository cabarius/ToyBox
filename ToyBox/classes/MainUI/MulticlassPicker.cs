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
            var options = MulticlassOptions.Get(ch);
            var classes = Game.Instance.BlueprintRoot.Progression.CharacterClasses;
            var mythicClasses = Game.Instance.BlueprintRoot.Progression.CharacterMythics;
            var showDesc = settings.toggleMulticlassShowClassDescriptions;
            foreach (var cl in classes) {
                PickerRow(ch, cl, options, indent);
            }
            UI.Div(indent);
            if (showDesc) {
                using (UI.HorizontalScope()) {
                    UI.Space(indent); UI.Label("Mythic".cyan());
                }
            }
            foreach (var mycl in mythicClasses) {
                PickerRow(ch, mycl, options, indent);
            }
            MulticlassOptions.Set(ch, options);
        }

        public static bool PickerRow(UnitEntityData ch, BlueprintCharacterClass cl, MulticlassOptions options, float indent = 100) {
            var changed = false;
            var showDesc = settings.toggleMulticlassShowClassDescriptions;
            if (showDesc) UI.Div(indent);
            var cd = ch?.Progression.GetClassData(cl);
            var chArchetype = cd?.Archetypes.FirstOrDefault<BlueprintArchetype>();
            var showGestaltToggle = false;
            if (ch != null && cd != null) {
                var classes = ch?.Progression.Classes;
                var classCount = classes?.Count(x => !x.CharacterClass.IsMythic);
                var gestaltCount = classes?.Count(cd => !cd.CharacterClass.IsMythic && ch.IsClassGestalt(cd.CharacterClass));
                showGestaltToggle = !cd.CharacterClass.IsMythic && classCount - gestaltCount > 1 || ch.IsClassGestalt(cd.CharacterClass) || cd.CharacterClass.IsMythic;
            }
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                UI.ActionToggle(
                    cd != null && chArchetype == null ? cl.Name.orange() : cl.Name,
                    () => options.Contains(cl),
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
                        }, 125);
                    UI.Space(25);
                }
                else UI.Space(157);
                if (showDesc) UI.Label(cl.Description.StripHTML().green());
            }
            using (UI.HorizontalScope()) {
                UI.Space(indent);
                var archetypes = cl.Archetypes;
                if (options.Contains(cl) && archetypes.Any() || chArchetype != null) {
                    UI.Space(50);
                    using (UI.VerticalScope()) {
                        var archetypeOptions = options.ArchetypeOptions(cl);
                        foreach (var archetype in cl.Archetypes) {
                            if (chArchetype == null || chArchetype == archetype) {
                                if (showDesc) UI.Div();
                                using (UI.HorizontalScope()) {
                                    UI.ActionToggle(
                                    chArchetype != null ? archetype.Name.orange() : archetype.Name,
                                    () => archetypeOptions.Contains(archetype),
                                    (v) => {
                                        if (v) archetypeOptions.AddExclusive(archetype);
                                        else archetypeOptions.Remove(archetype);
                                        Mod.Trace($"PickerRow - archetypeOptions - {{{archetypeOptions}}}");
                                    }, 300);
                                    options.SetArchetypeOptions(cl, archetypeOptions);
                                    if (showGestaltToggle && chArchetype != null) {
                                        UI.ActionToggle("gestalt".grey(), () => ch.IsClassGestalt(cd.CharacterClass),
                                            (v) => {
                                                ch.SetClassIsGestalt(cd.CharacterClass, v);
                                                ch.Progression.UpdateLevelsForGestalt();
                                            }, 125);
                                        UI.Space(25);
                                    }
                                    else UI.Space(157);
                                    if (showDesc) UI.Label(archetype.Description.StripHTML().green());
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
