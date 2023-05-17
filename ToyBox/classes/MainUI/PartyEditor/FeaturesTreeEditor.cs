using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using ModKit;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if Wrath
using Kingmaker.Blueprints.Classes.Selection;
#endif

namespace ToyBox {
    public class FeaturesTreeEditor {
        private UnitEntityData _selectedCharacter = null;
        private FeaturesTree _featuresTree;

        private GUIStyle _buttonStyle;

        public string Name => "Features Tree";

        public int Priority => 500;

        public void OnGUI(UnitEntityData character, bool refresh) {
            if (!Main.IsInGame) return;
            var activeScene = SceneManager.GetActiveScene().name;
            if (Game.Instance?.Player == null || activeScene == "MainMenu" || activeScene == "Start") {
                UI.Label(" * Please start or load the game first.".color(RGBA.yellow));
                return;
            }
            if (_buttonStyle == null)
                _buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = true };

            try {
                if (character != _selectedCharacter || refresh) {
                    _selectedCharacter = character;
#if Wrath
                    _featuresTree = new FeaturesTree(_selectedCharacter.Descriptor.Progression);
#elif RT
                    _featuresTree = new FeaturesTree(_selectedCharacter.Progression);
#endif
                }
                using (UI.HorizontalScope()) {
                    // features tree
                    if (_featuresTree != null)
                        using (UI.VerticalScope()) {
                            var expandAll = false;
                            var collapseAll = false;

                            // draw tool bar
                            using (UI.HorizontalScope()) {
                                UI.ActionButton("Refresh", () => _featuresTree = 
                                                                     new FeaturesTree(_selectedCharacter
#if Wrath
                                                                                      .Descriptor
#endif
                                                                                      .Progression), UI.Width(200));
                                UI.Button("Expand All", ref expandAll, UI.Width(200));
                                UI.Button("Collapse All", ref collapseAll, UI.Width(200));
                            }

                            UI.Space(10f);

                            // draw tree
                            foreach (var node in _featuresTree.RootNodes) {
                                draw(node);
                            }

                            void draw(FeaturesTree.FeatureNode node) {
                                using (UI.HorizontalScope()) {
                                    var levelText = node.Level == 0 ? "" : $" {node.Level} - ";
                                    var blueprintName =  $"[{node.Blueprint.name}]".color(node.IsMissing ? RGBA.maroon : RGBA.aqua);
                                    var titleText = $"{levelText}{node.Name.Bold()} {blueprintName}";
                                    if (node.ChildNodes.Count > 0) {
                                        if (node.Expanded == ToggleState.None) {
                                            node.Expanded = ToggleState.Off;
                                        }
                                        node.Expanded = expandAll ? ToggleState.On : collapseAll ? ToggleState.Off : node.Expanded;
                                    }
                                    else {
                                        node.Expanded = ToggleState.None;
                                    }
                                    Mod.Trace($"{node.Expanded} {titleText}");
                                    UI.ToggleButton(ref node.Expanded, titleText, _buttonStyle);
                                    if (node.Expanded.IsOn()) {
                                        using (UI.VerticalScope(UI.ExpandWidth(false))) {
                                            foreach (var child in node.ChildNodes.OrderBy(n => n.Level))
                                                draw(child);
                                        }
                                    }
                                    else {
                                        GUILayout.FlexibleSpace();
                                    }
                                }
                            }
                        }
                }
            }
            catch (Exception e) {
                _selectedCharacter = null;
                _featuresTree = null;
                Mod.Error(e);
                throw e;
            }
        }

        private class FeaturesTree {
            public readonly List<FeatureNode> RootNodes = new();

#if Wrath
            public FeaturesTree(UnitProgressionData progression) {
#elif RT
            public FeaturesTree(PartUnitProgression progression) {
#endif
                Dictionary<BlueprintScriptableObject, FeatureNode> normalNodes = new();
                List<FeatureNode> parametrizedNodes = new();

                //Main.Log($"prog: {progression}");
                // get nodes (features / race)
                foreach (var feature in progression.Features.Enumerable) {
                    var name = feature.Name;
                    if (name == null || name.Length == 0)
                        name = feature.Blueprint.name;
                    //Main.Log($"feature: {name}");
#if Wrath
                    var source = feature.m_Source;
                    //Main.Log($"source: {source}");
                    if (feature.Blueprint is BlueprintParametrizedFeature)
                        parametrizedNodes.Add(new FeatureNode(name, feature.SourceLevel, feature.Blueprint, source));
                    else
                        normalNodes.Add(feature.Blueprint, new FeatureNode(name, feature.SourceLevel, feature.Blueprint, source));
#endif
                }

                // get nodes (classes)
                foreach (var characterClass in progression.Classes.Select(item => item.CharacterClass)) {
                    normalNodes.Add(characterClass, new FeatureNode(characterClass.Name, 0, characterClass, null));
                }

#if Wrath
                // set source selection
                var selectionNodes = normalNodes.Values
                    .Where(item => item.Blueprint is BlueprintFeatureSelection).ToList();
                for (var level = 0; level <= 100; level++) {
                    foreach (var selection in selectionNodes) {
                        foreach (var feature in progression.GetSelections(selection.Blueprint as BlueprintFeatureSelection, level)) {
                            FeatureNode node = default;
                            if (feature is BlueprintParametrizedFeature) {
                                node = parametrizedNodes
                                    .FirstOrDefault(item => item.Source != null && item.Source == selection.Source);
                            }

                            if (node != null || normalNodes.TryGetValue(feature, out node)) {
                                node.Source = selection.Blueprint;
                                node.Level = level;
                            }
                            else {
                                // missing child
                                normalNodes.Add(feature,
                                    new FeatureNode(string.Empty, level, feature, selection.Blueprint) { IsMissing = true });
                            }
                        }
                    }
                }
#endif

                // build tree
                foreach (var node in normalNodes.Values.Concat(parametrizedNodes).ToList()) {
                    if (node.Source == null) {
                        RootNodes.Add(node);
                    }
                    else if (normalNodes.TryGetValue(node.Source, out var parent)) {
                        parent.ChildNodes.Add(node);
                    }
                    else {
                        // missing parent
                        parent = new FeatureNode(string.Empty, 0, node.Source, null) { IsMissing = true };
                        parent.ChildNodes.Add(node);
                        normalNodes.Add(parent.Blueprint, parent);
                        RootNodes.Add(parent);
                    }
                }
            }

            public class FeatureNode {
                internal bool IsMissing;
                internal BlueprintScriptableObject Source;

                public readonly string Name;
                public int Level;
                public readonly BlueprintScriptableObject Blueprint;
                public readonly List<FeatureNode> ChildNodes = new();

                public ToggleState Expanded;

                internal FeatureNode(string name, int level, BlueprintScriptableObject blueprint, BlueprintScriptableObject source) {
                    Name = name;
                    Level = level;
                    Blueprint = blueprint;
                    Source = source;
                }
            }
        }
    }
}
