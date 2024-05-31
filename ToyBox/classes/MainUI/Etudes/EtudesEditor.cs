using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;
using ModKit;
using ModKit.DataViewer;
using ModKit.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static ModKit.UI;
using Application = UnityEngine.Application;

namespace ToyBox {
    public static class EtudesEditor {

        private static BlueprintGuid _parent;
        private static BlueprintGuid _selected;
        private static Dictionary<BlueprintGuid, EtudeInfo> loadedEtudes => EtudesTreeModel.Instance.loadedEtudes;
        private static Dictionary<BlueprintGuid, EtudeInfo> _filteredEtudes = new();

        // TODO: is this still the right root etude?
        internal static BlueprintGuid rootEtudeId =
            BlueprintGuid.Parse("f0e6f6b732c40284ab3c103cad2455cc");

        public static string searchText = "";
        public static string searchTextInput = "";
        private static bool _showOnlyFlagLikes;
        private static bool showComments => Main.Settings.showEtudeComments;

        private static List<BlueprintArea> _areas;
        private static BlueprintArea _selectedArea;
        private static string _areaSearchText = "";
        //private EtudeChildrenDrawer etudeChildrenDrawer;

        public static Dictionary<string?, SimpleBlueprint> toValues = new();
        public static Dictionary<string?, BlueprintAction> actionLookup = new();
        public static void OnShowGUI() => UpdateEtudeStates();
        public static int lineNumber = 0;
        public static Rect firstRect;
        private static void Update() {
            //etudeChildrenDrawer?.Update();
        }

        private static void ReloadEtudes() {
            EtudesTreeModel.Instance.ReloadBlueprintsTree();
            //etudeChildrenDrawer = new EtudeChildrenDrawer(loadedEtudes, this);
            //etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
            ApplyFilter();
        }

        public static void OnGUI() {
            if (loadedEtudes?.Count == 0) {
                ReloadEtudes();
            }
            if (_areas == null) _areas = BlueprintLoader.Shared.GetBlueprints<BlueprintArea>()?.OrderBy(a => a.name).ToList();
            if (_areas == null) return;
            if (_parent == BlueprintGuid.Empty) {
                _parent = rootEtudeId;
                _selected = _parent;
            }
            Label(("Note".orange().bold() + " this is a new and exciting feature that allows you to see for the first time the structure and some basic relationships of ".green() + "Etudes".cyan().bold() + " and other ".green() + "Elements".cyan().bold() + " that control the progression of your game story. Etudes are hierarchical in structure and additionally contain a set of ".green() + "Elements".cyan().bold() + " that can both conditions to check and actions to execute when the etude is started. As you browe you will notice there is a disclosure triangle next to the name which will show the children of the Etude.  Etudes that have ".green() + "Elements".cyan().bold() + " will offer a second disclosure triangle next to the status that will show them to you.".green()).localize());
            Label(("WARNING".yellow().bold() + " this tool can both miraculously fix your broken progression or it can break it even further. Save and back up your save before using.".orange()).localize());
            using (HorizontalScope(AutoWidth())) {
                if (_parent == BlueprintGuid.Empty)
                    return;
                Label("Search".localize());
                Space(25);
                ActionTextField(ref searchTextInput, "Search", (s) => { }, () => { searchText = searchTextInput; UpdateSearchResults(); }, 400.width());
                Space(25);
                if (Toggle("Flags Only".localize(), ref _showOnlyFlagLikes)) ApplyFilter();
                25.space();
                Toggle("Show GUIDs".localize(), ref Main.Settings.showAssetIDs);
                25.space();
                Toggle("Show Comments (some in Russian)".localize(), ref Main.Settings.showEtudeComments);
                //UI.Label($"Etude Hierarchy : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}", UI.AutoWidth());
                //UI.Label($"H : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[selected].Name)}");

                //if (loadedEtudes.Count != 0) {
                //    UI.ActionButton("Refresh", () => ReloadEtudes(), UI.AutoWidth());
                //}

                //if (UI.Button("Update DREAMTOOL Index", UI.MinWidth(300), UI.MaxWidth(300))) {
                //    ReferenceGraph.CollectMenu();
                //    etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
                //    etudeChildrenDrawer.ReferenceGraph.AnalyzeReferencesInBlueprints();
                //    etudeChildrenDrawer.ReferenceGraph.Save();
                //}
            }
            using (HorizontalScope(GUI.skin.box, AutoWidth())) {
                //if (etudeChildrenDrawer != null) {
                //    using (UI.VerticalScope(GUI.skin.box, UI.MinHeight(60),
                //        UI.MinWidth(300))) {
                //        etudeChildrenDrawer.DefaultExpandedNodeWidth = UI.Slider("Ширина раскрытия нода", etudeChildrenDrawer.DefaultExpandedNodeWidth, 200, 2000);
                //    }
                //}

                //if (etudeChildrenDrawer != null && !etudeChildrenDrawer.BlockersInfo.IsEmpty) {
                //    using (UI.VerticalScope(GUI.skin.box, UI.MinHeight(60),
                //        UI.MinWidth(350))) {
                //        var info = etudeChildrenDrawer.BlockersInfo;
                //        var lockSelf = info.Blockers.Contains(info.Owner);
                //        if (lockSelf) {
                //            UI.Label("Completion блокируется условиями самого этюда");
                //        }

                //        if (info.Blockers.Count > 1 || !lockSelf) {
                //            UI.Label("Completion блокируется условиями детей: ");
                //            foreach (var blocker in info.Blockers) {
                //                var bluprint = blocker.Blueprint;
                //                if (UI.Button(bluprint.name)) {
                //                    Selection.activeObject = BlueprintEditorWrapper.Wrap(bluprint);
                //                }
                //            }
                //        }
                //    }
                //}
            }
            var remainingWidth = ummWidth;
            using (HorizontalScope()) {
                Label(""); firstRect = GUILayoutUtility.GetLastRect();
                using (VerticalScope(GUI.skin.box)) {
                    if (VPicker<BlueprintArea>("Areas".localize().orange().bold(), ref _selectedArea, _areas, "All".localize(), bp => {
                        var name = bp.name; // bp.AreaDisplayName;
                        if (name?.Length == 0) name = bp.AreaName;
                        if (name?.Length == 0) name = bp.NameSafe();
                        return name;
                    }, ref _areaSearchText,
                    () => { },
                    rarityButtonStyle,
                    Width(300))) {
                        ApplyFilter();
                    }
                }
                remainingWidth -= 300;
                using (VerticalScope(GUI.skin.box)) { //, UI.Width(remainingWidth))) {
                    //using (var scope = UI.ScrollViewScope(m_ScrollPos, GUI.skin.box)) {
                    //UI.Label($"Hierarchy tree : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}", UI.MinHeight(50));

                    if (_filteredEtudes.Count == 0) {
                        Label("No Etudes".localize(), AutoWidth());
                        //UI.ActionButton("Refresh", () => ReloadEtudes(), UI.AutoWidth());
                        return;
                    }

                    if (Application.isPlaying) {
                        foreach (var etude in Game.Instance.Player.EtudesSystem.Etudes.RawFacts) {
                            FillPlaymodeEtudeData(etude);
                        }
                    }
                    lineNumber = 0;
                    ShowBlueprintsTree();

                    //m_ScrollPos = scope.scrollPosition;
                }

                //using (UI.VerticalScope(GUI.skin.box, UI.ExpandWidth(true), UI.ExpandHeight(true))) {
                //    UI.Label("", UI.ExpandWidth(true), UI.ExpandHeight(true));

                //if (Event.current.type == EventType.Repaint) {
                //    workspaceRect = GUILayoutUtility.GetLastRect();
                //    etudeChildrenDrawer?.SetWorkspaceRect(workspaceRect);
                //}
                //etudeChildrenDrawer.OnGUI();
                //}
            }
#if DEBUG
            ActionButton("Generate Comment Translation Table".localize(), () => { });
#endif
            foreach (var item in toValues) {
                var mutator = actionLookup[item.Key];
                if (mutator != null)
                    try { mutator.action(item.Value, null); } catch (Exception e) { Mod.Error(e); }
            }
            if (toValues.Count > 0) {
                UpdateEtudeStates();
            }
            toValues.Clear();
        }

        private static HashSet<BlueprintGuid> enclosingEtudes = new();
        private static void DrawEtude(BlueprintGuid etudeID, EtudeInfo etude, int indent) {
            if (enclosingEtudes.Contains(etudeID)) return;
            var viewPort = ummRect;
            var topLines = firstRect.y / 30;
            var linesVisible = 1 + viewPort.height / 30;
            var scrollOffset = ummScrollPosition[0].y / 30 - topLines;
            var viewPortLine = lineNumber - scrollOffset;
            var isVisible = viewPortLine >= 0 && viewPortLine < linesVisible;
#if false
            Mod.Log($"line: {lineNumber} - topLines: {topLines} scrollOffset: {scrollOffset} - {Event.current.type} - isVisible: {isVisible}");
#endif
            if (true || isVisible) {
                var name = etude.Name;
                if (etude.hasSearchResults || searchText.Length == 0 || name.ToLower().Contains(searchText.ToLower())) {
                    enclosingEtudes.Add(etudeID);
                    var components = etude.Blueprint.Components;
                    //var gameActions = etude.Blueprint.ComponentsArray.SelectMany(c => {
                    //    var actionsField = c.GetType().GetField("Actions");
                    //    var actionList = (ActionList)actionsField?.GetValue(c);
                    //    return actionList?.Actions ?? new GameAction[] { };
                    //}).ToList();
                    var conflicts = EtudesTreeModel.Instance.GetConflictingEtudes(etudeID);
                    var conflictCount = conflicts.Count - 1;
                    using (HorizontalScope(ExpandWidth(true))) {
                        using (HorizontalScope(Width(310))) {
                            var actions = etude.Blueprint.GetActions().Where(action => action.canPerform(etude.Blueprint, null));
                            foreach (var action in actions) {
                                actionLookup[action.name] = action;
                                ActionButton(action.name, () => toValues[action.name] = etude.Blueprint, Width(150));
                            }
                        }
                        Indent(indent);
                        var style = GUIStyle.none;
                        style.fontStyle = FontStyle.Normal;
                        if (_selected == etudeID) name = name.orange().bold();

                        using (HorizontalScope(Width(825))) {
                            if (etude.ChildrenId.Count == 0) etude.ShowChildren = ToggleState.None;
                            ToggleButton(ref etude.ShowChildren, name.orange().bold(), (state) => OpenCloseAllChildren(etude, state));
                            Space(25);
                            var eltCount = etude.Blueprint.m_AllElements.Count;
                            if (eltCount > 0)
                                ToggleButton(ref etude.ShowElements, eltCount.ToString() + " " + "elements".localize(), Width(175));
                            else
                                Space(178);
                            //UI.Space(126);
                            if (conflictCount > 0)
                                ToggleButton(ref etude.ShowConflicts, conflictCount.ToString() + " " + "conflicts".localize(), Width(175));
                            else
                                Space(178);

                            //UI.Space(126);
                            //if (gameActions.Count > 0)
                            //    UI.ToggleButton(ref etude.ShowActions, $"{gameActions.Count} actions", UI.Width(75));
                            //else
                            //    UI.Space(78);
                        }
                        //UI.ActionButton(UI.DisclosureGlyphOff + ">", () => OpenCloseAllChildren(etudeEntry, !etudeEntry.Foldout), GUI.skin.box, UI.AutoWidth());
                        //if (GUILayout.Button("Select", GUI.skin.box, UI.Width(100))) {
                        //    if (selected != etudeID) {
                        //        selected = etudeID;
                        //    }
                        //    else {
                        //        parent = etudeID;
                        //        //etudeChildrenDrawer.SetParent(parent, workspaceRect);
                        //    }
                        //    selectedEtude = ResourcesLibrary.TryGetBlueprint<BlueprintEtude>(etudeID);
                        //}

                        Space(100);
                        Label(etude.State.ToString().yellow(), Width(125));
                        Space(-2);
                        Space(25);
                        if (EtudeValidationProblem(etudeID, etude) is { } reason) {
                            UI.Label($"{reason.cyan()}".yellow(), 300.width());
                            UI.Space(25);
                        }
                        Label("🔗", AutoWidth());
                        if (etude.CompleteParent)
                            Label("⎌", AutoWidth());
                        if (etude.AllowActionStart) {
                            Space(25);
                            Label("Can Start".localize(), 100.width());
                        }
                        ReflectionTreeView.DetailToggle("Inspect".localize(), etude, etude, 100);
                        if (Main.Settings.showAssetIDs) {
                            var guid = etudeID.ToString();
                            TextField(ref guid);
                        }
                        if (showComments && !Main.Settings.showAssetIDs && !string.IsNullOrEmpty(etude.Comment)) {
                            Label(etude.Comment.green(), ExpandWidth(true));
                        }
                        Label("", AutoWidth());
                    }
                    ReflectionTreeView.OnDetailGUI(etude);
                    if (showComments && Main.Settings.showAssetIDs && !string.IsNullOrEmpty(etude.Comment)) {
                        Space(-15);
                        using (HorizontalScope(Width(200))) {
                            Space(310);
                            Indent(indent);
                            Space(933);
                            Label(etude.Comment.green(), ExpandWidth(true));
                            Label("", AutoWidth());
                        }
                    }
                    indent += 2;
                    if (etude.ShowElements.IsOn()) {
                        using (HorizontalScope(ExpandWidth(true))) {
                            Space(310);
                            Indent(indent);
                            using (VerticalScope()) {
                                foreach (var element in etude.Blueprint.m_AllElements) {
                                    using (HorizontalScope(Width(10000))) {
                                        // UI.Label(element.NameSafe().orange()); -- this is useless at the moment
                                        using (HorizontalScope(450)) {
                                            if (element is GameAction gameAction) {
                                                try {
                                                    ActionButton(gameAction.GetCaption().yellow(), gameAction.RunAction);
                                                } catch (Exception e) {
                                                    Mod.Warn($"{gameAction.GetCaption()} failed to run {e.ToString().yellow()}");
                                                }
                                            } else
                                                Label(element.GetCaption().yellow() ?? "?");
                                            Space(25);
                                            ReflectionTreeView.DetailToggle("Inspect".localize(), element, element, 100);
                                            Space(0);
                                        }
                                        Space(25);
                                        if (element is Condition condition)
                                            Label($"{element.GetType().Name.cyan()} : {condition.CheckCondition().ToString().orange()}", Width(250));
                                        else if (element is Conditional conditional)
                                            Label($"{element.GetType().Name.cyan()} : {conditional.ConditionsChecker.Check().ToString().orange()} - {string.Join(", ", conditional.ConditionsChecker.Conditions.Select(c => c.GetCaption())).yellow()}", Width(250));
                                        else
                                            Label(element.GetType().Name.cyan(), Width(250));
                                        if (element is AnotherEtudeOfGroupIsPlaying otherGroup)
                                            Label($"{conflictCount}", Width(50));
                                        else
                                            Width(53);
                                        Space(25);
                                        if (showComments)
                                            Label(element.GetDescription().green());

                                    }
                                    if (element is StartEtude started) {
                                        DrawEtudeTree(started.Etude.Guid, 2, true);
                                    }
                                    if (element is EtudeStatus status) {
                                        DrawEtudeTree(status.m_Etude.Guid, 2, true);
                                    }
                                    if (element is CompleteEtude completed) {
                                        DrawEtudeTree(completed.Etude.Guid, 2, true);
                                    }
                                    Div();
                                }
                            }
                        }
                    }

                    if (etude.ShowConflicts.IsOn()) {
                        using (HorizontalScope(Width(10000))) {
                            Space(310);
                            Indent(indent);
                            using (VerticalScope()) {
                                foreach (var conflict in conflicts) {
                                    DrawEtudeTree(conflict, 2, true);
                                }
                            }
                        }
                    }
                    //if (etude.ShowActions.IsOn()) {
                    //    foreach (var action in gameActions) {
                    //        using (UI.HorizontalScope()) {
                    //            UI.Space(310);
                    //            UI.Indent(indent);
                    //            UI.ActionButton(action.GetCaption(), action.RunAction);
                    //            UI.Space(25);
                    //            UI.Label(action.GetDescription().green());
                    //        }
                    //    }
                    //}
                    lineNumber += 1;
                }
                enclosingEtudes.Remove(etudeID);
            }
        }
        private static void ShowBlueprintsTree() {
            using (VerticalScope()) {
                DrawEtude(rootEtudeId, loadedEtudes[rootEtudeId], 0);
                using (VerticalScope(GUI.skin.box)) {
                    ShowParentTree(loadedEtudes[rootEtudeId], 1);
                }
            }
        }

        private static void DrawEtudeTree(BlueprintGuid etudeID, int indent, bool ignoreFilter = false) {
            var etude = loadedEtudes[etudeID];
            DrawEtude(etudeID, etude, indent);

            if (etude.ChildrenId.Count > 0 && (etude.ShowChildren.IsOn() || etude.hasSearchResults)) {
                ShowParentTree(etude, indent + 1, ignoreFilter);
            }
        }
        private static void ShowParentTree(EtudeInfo etude, int indent, bool ignoreFilter = false) {
            foreach (var childID in etude.ChildrenId) {
                if (!ignoreFilter && !_filteredEtudes.ContainsKey(childID))
                    continue;
                DrawEtudeTree(childID, indent, ignoreFilter);
            }
        }
        private static void UpdateSearchResults() {
            foreach (var entry in loadedEtudes)
                entry.Value.hasSearchResults = false;
            if (searchText.Length != 0) {
                foreach (var entry in loadedEtudes) {
                    var etude = entry.Value;
                    if (etude.Name.Matches(searchText)
                        || etude.Blueprint.AssetGuid.ToString().Matches(searchText)) {
                        etude.hasSearchResults = true;
                        etude.TraverseParents(e => e.hasSearchResults = true);
                    }
                }
            }
        }
        private static void ApplyFilter() {
            UpdateSearchResults();
            var etudesOfArea = new Dictionary<BlueprintGuid, EtudeInfo>();

            _filteredEtudes = loadedEtudes;

            if (_selectedArea != null) {
                etudesOfArea = GetAreaEtudes();
                _filteredEtudes = etudesOfArea;
            }

            var flaglikeEtudes = new Dictionary<BlueprintGuid, EtudeInfo>();

            if (_showOnlyFlagLikes) {
                flaglikeEtudes = GetFlaglikeEtudes();
                _filteredEtudes = _filteredEtudes.Keys.Intersect(flaglikeEtudes.Keys)
                    .ToDictionary(t => t, t => _filteredEtudes[t]);
            }
        }

        //[MenuItem("CONTEXT/BlueprintEtude/Open in EtudeViewer")]
        //public static void OpenAssetInEtudeViewer() {
        //    BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);
        //    if (blueprint == null)
        //        return;

        //    EtudeChildrenDrawer.TryToSetParent(blueprint.AssetGuid);

        //}

        private static Dictionary<BlueprintGuid, EtudeInfo> GetFlaglikeEtudes() {
            var etudesFlaglike = new Dictionary<BlueprintGuid, EtudeInfo>();

            foreach (var etude in loadedEtudes) {
                var flaglike = etude.Value.ChainedTo == BlueprintGuid.Empty &&
                                // (etude.Value.ChainedId.Count == 0) &&
                                etude.Value.LinkedTo == BlueprintGuid.Empty &&
                                etude.Value.LinkedArea == BlueprintGuid.Empty && !ParentHasArea(etude.Value);

                if (flaglike) {
                    etudesFlaglike.Add(etude.Key, etude.Value);
                    AddParentsToDictionary(etudesFlaglike, etude.Value);
                }
            }

            return etudesFlaglike;
        }

        public static bool ParentHasArea(EtudeInfo etude) {
            if (etude.ParentId == BlueprintGuid.Empty)
                return false;

            if (loadedEtudes[etude.ParentId].LinkedArea == BlueprintGuid.Empty) {
                return ParentHasArea(loadedEtudes[etude.ParentId]);
            }

            return true;
        }

        private static Dictionary<BlueprintGuid, EtudeInfo> GetAreaEtudes() {
            var etudesWithAreaLink = new Dictionary<BlueprintGuid, EtudeInfo>();

            foreach (var etude in loadedEtudes) {
                if (etude.Value.LinkedArea == _selectedArea.AssetGuid) {
                    if (!etudesWithAreaLink.ContainsKey(etude.Key))
                        etudesWithAreaLink.Add(etude.Key, etude.Value);

                    AddChildsToDictionary(etudesWithAreaLink, etude.Value);
                    AddParentsToDictionary(etudesWithAreaLink, etude.Value);

                }
            }

            return etudesWithAreaLink;
        }

        private static void AddChildsToDictionary(Dictionary<BlueprintGuid, EtudeInfo> dictionary, EtudeInfo etude) {
            foreach (var children in etude.ChildrenId) {
                if (dictionary.ContainsKey(children))
                    continue;

                dictionary.Add(children, loadedEtudes[children]);
                AddChildsToDictionary(dictionary, loadedEtudes[children]);
            }
        }

        private static void AddParentsToDictionary(Dictionary<BlueprintGuid, EtudeInfo> dictionary, EtudeInfo etude) {
            if (etude.ParentId == BlueprintGuid.Empty)
                return;

            if (dictionary.ContainsKey(etude.ParentId))
                return;

            dictionary.Add(etude.ParentId, loadedEtudes[etude.ParentId]);
            AddParentsToDictionary(dictionary, loadedEtudes[etude.ParentId]);
        }

        private static void FillPlaymodeEtudeData(Etude etude) {
            var etudeIdReferences = loadedEtudes[etude.Blueprint.AssetGuid];
            UpdateStateInRef(etude, etudeIdReferences);
        }

        private static void UpdateStateInRef(Etude etude, EtudeInfo etudeIdReferences) {
            if (etude.IsCompleted) {
                etudeIdReferences.State = EtudeInfo.EtudeState.Completed;
                return;
            }

            if (etude.CompletionInProgress) {
                etudeIdReferences.State = EtudeInfo.EtudeState.CompletionBlocked;
                return;
            }

            if (etude.IsPlaying) {
                etudeIdReferences.State = EtudeInfo.EtudeState.Active;
            } else {
                etudeIdReferences.State = EtudeInfo.EtudeState.Started;
            }
        }

        // Not localizing this beacuse I doubt doing that is meaningful in any way.
        private static string EtudeValidationProblem(BlueprintGuid etudeID, EtudeInfo etude) {
            if (etude.ChainedTo == BlueprintGuid.Empty && etude.LinkedTo == BlueprintGuid.Empty)
                return "Chained/Linked to Nothing";

            foreach (var chained in etude.ChainedId) {
                var chainedEtude = loadedEtudes[chained];
                if (chainedEtude.ParentId != etude.ParentId)
                    return $"Chained etude {chainedEtude.Name} ({chainedEtude.Blueprint.AssetGuid}) has different parent: {chainedEtude.ParentId} than {etude.Name} parent: {etude.ParentId}";
            }

            foreach (var linked in etude.LinkedId) {
                var linkedEtude = loadedEtudes[linked];
                if (linkedEtude.ParentId != etude.ParentId && loadedEtudes[linked].ParentId != etudeID)
                    return $"Linked to child {linkedEtude.Name} ({linkedEtude.Blueprint.AssetGuid}) with different parent: {linkedEtude.ParentId} than {etude.Name} parent {etude.ParentId}";
            }

            return null;
        }
        private static void UpdateEtudeStates() {
            if (Application.isPlaying) {
                foreach (var etude in loadedEtudes)
                    UpdateEtudeState(etude.Key, etude.Value);
            }
        }
        public static void UpdateEtudeState(BlueprintGuid etudeID, EtudeInfo etude) {
            var blueprintEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(etudeID);

            var item = Game.Instance.Player.EtudesSystem.Etudes.GetFact(blueprintEtude);
            if (item != null)
                UpdateStateInRef(item, etude);
            else if (Game.Instance.Player.EtudesSystem.EtudeIsPreCompleted(blueprintEtude))
                etude.State = EtudeInfo.EtudeState.CompleteBeforeActive;
            else if (Game.Instance.Player.EtudesSystem.EtudeIsCompleted(blueprintEtude))
                etude.State = EtudeInfo.EtudeState.Completed;
        }
        private static void Traverse(this EtudeInfo etude, Action<EtudeInfo> action) {
            action(etude);
            foreach (var cildrenID in etude.ChildrenId) {
                Traverse(loadedEtudes[cildrenID], action);
            }
        }
        private static void TraverseParents(this EtudeInfo etude, Action<EtudeInfo> action) {
            while (loadedEtudes.TryGetValue(etude.ParentId, out var parent)) {
                action(parent);
                etude = parent;
            }
        }
        private static void OpenCloseAllChildren(this EtudeInfo etude, ToggleState state)
            => etude.Traverse((e) => e.ShowChildren = state);
        private static void OpenCloseParents(this EtudeInfo etude, ToggleState state)
            => etude.TraverseParents((e) => e.ShowChildren = state);
    }
}
