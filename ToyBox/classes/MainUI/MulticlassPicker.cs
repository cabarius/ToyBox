// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Utility;
using ModKit;
using System.Collections.Generic;
using System.Linq;

namespace ToyBox
{
    public class MulticlassPicker
    {
        public static void OnGUI(HashSet<string> multiclassSet, float indent = 100)
        {
            var classes = Game.Instance.BlueprintRoot.Progression.CharacterClasses;
            var mythicClasses = Game.Instance.BlueprintRoot.Progression.CharacterMythics;

            foreach (var cl in classes)
            {
                PickerRow(cl, multiclassSet, indent);
            }

            UI.Div(indent, 20);

            foreach (var mycl in mythicClasses)
            {
                using (UI.HorizontalScope())
                {
                    PickerRow(mycl, multiclassSet, indent);
                }
            }
        }

        public static bool PickerRow(BlueprintCharacterClass cl, HashSet<string> multiclassSet, float indent = 100)
        {
            bool changed = false;

            using (UI.HorizontalScope())
            {
                UI.Space(indent);

                UI.ActionToggle(
                    cl.Name,
                    () => multiclassSet.Contains(cl.AssetGuid.ToString()),
                    v =>
                    {
                        if (v)
                        {
                            multiclassSet.Add(cl.AssetGuid.ToString());
                        }
                        else
                        {
                            multiclassSet.Remove(cl.AssetGuid.ToString());
                        }

                        changed = true;
                    },
                    350
                );

                var archetypes = cl.Archetypes;

                if (multiclassSet.Contains(cl.AssetGuid.ToString()) && archetypes.Any())
                {
                    UI.Space(50);
                    int originalArchetype = 0;
                    int selectedArchetype = originalArchetype = archetypes.FindIndex(archetype => multiclassSet.Contains(archetype.AssetGuid.ToString())) + 1;
                    string[] choices = new[] { cl.Name }.Concat(archetypes.Select(a => a.Name)).ToArray();

                    UI.ActionSelectionGrid(ref selectedArchetype, choices, 6,
                                           sel =>
                                           {
                                               if (originalArchetype > 0)
                                               {
                                                   multiclassSet.Remove(
                                                       archetypes[originalArchetype - 1].AssetGuid.ToString());
                                               }

                                               // ReSharper disable once AccessToModifiedClosure
                                               if (selectedArchetype > 0)
                                               {
                                                   // ReSharper disable once AccessToModifiedClosure
                                                   multiclassSet.Add(archetypes[selectedArchetype - 1].AssetGuid.ToString());
                                               }
                                           }, UI.AutoWidth());
                }
            }

            return changed;
        }
    }
}