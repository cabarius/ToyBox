using System;
using System.Collections.Generic;
using System.IO;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;
using System.Linq;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using ModKit;
using Kingmaker;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace ToyBox {
    public static class EtudesEditor {

        private static BlueprintGuid parent;
        private static BlueprintGuid selected;
        private static Dictionary<BlueprintGuid, EtudeInfo> loadedEtudes => EtudesTreeModel.Instance.loadedEtudes;
        private static Dictionary<BlueprintGuid, EtudeInfo> filteredEtudes = new();
        private static readonly BlueprintGuid rootEtudeId = BlueprintGuid.Parse("f0e6f6b732c40284ab3c103cad2455cc");
        private static bool showOnlyFlagLikes;

        private static BlueprintEtude selectedEtude;

        private static List<BlueprintArea> areas;
        private static BlueprintArea selectedArea;
        private static string areaSearchText = "";
        //private EtudeChildrenDrawer etudeChildrenDrawer;

        public static string searchText = "";
        public static Dictionary<string, SimpleBlueprint> toValues = new();
        public static Dictionary<string, BlueprintAction> actionLookup = new();
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
            if (areas == null) areas = BlueprintLoader.Shared.GetBlueprints<BlueprintArea>()?.OrderBy(a => a.name).ToList();
            if (areas == null) return;
            if (parent == BlueprintGuid.Empty) {
                parent = rootEtudeId;
                selected = parent;
            }
            using (UI.HorizontalScope()) {
                if (parent == BlueprintGuid.Empty)
                    return;
                UI.Label("Search");
                UI.Space(25);
                UI.TextField(ref searchText, "Find", UI.Width(200));
                UI.Space(25);
                if (UI.Toggle("Flags Only", ref showOnlyFlagLikes)) ApplyFilter();
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
            using (UI.HorizontalScope(GUI.skin.box, UI.AutoWidth())) {
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
            var remainingWidth = UI.ummWidth;
            using (UI.HorizontalScope()) {
                UI.Label(""); firstRect = GUILayoutUtility.GetLastRect();
                using (UI.VerticalScope(GUI.skin.box)) {
                    if (UI.VPicker<BlueprintArea>("Areas".orange().bold(), ref selectedArea, areas, "All", bp => {
                        var name = bp.name; // bp.AreaDisplayName;
                        if (name?.Length == 0) name = bp.AreaName;
                        if (name?.Length == 0) name = bp.NameSafe();
                        return name;
                    }, ref areaSearchText,
                    () => { },
                    UI.rarityButtonStyle,
                    UI.Width(300))) {
                        ApplyFilter();
                    }
                }
                remainingWidth -= 300;
                using (UI.VerticalScope(GUI.skin.box, UI.Width(remainingWidth))) {
                    //using (var scope = UI.ScrollViewScope(m_ScrollPos, GUI.skin.box)) {
                    //UI.Label($"Hierarchy tree : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}", UI.MinHeight(50));

                    if (filteredEtudes.Count == 0) {
                        UI.Label("No Etudes", UI.AutoWidth());
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
            UI.ActionButton("Generate Comment Translation Table", () => { });
#endif
            foreach (var item in toValues) {
                var mutator = actionLookup[item.Key];
                if (mutator != null)
                    try { mutator.action(item.Value, null); }
                    catch (Exception e) { Mod.Error(e); }
            }
            if (toValues.Count > 0) {
                UpdateEtudeStates();
            }
            toValues.Clear();
        }
        private static void DrawEtude(BlueprintGuid etudeID, EtudeInfo etude, int indent) {
            var viewPort = UI.ummRect;
            var topLines = firstRect.y / 30;
            var linesVisible = 1 + viewPort.height / 30;
            var scrollOffset = UI.ummScrollPosition[0].y / 30 - topLines;
            var viewPortLine = lineNumber - scrollOffset;
            var isVisible = viewPortLine >= 0 && viewPortLine < linesVisible;
#if DEBUG
            Mod.Log($"line: {lineNumber} - topLines: {topLines} scrollOffset: {scrollOffset} - {Event.current.type} - isVisible: {isVisible}");
#endif
            if (true || isVisible) {
                var etudeInfo = loadedEtudes[etudeID];
                var name = etude.Name;
                if (etude.hasSearchResults || searchText.Length == 0 || name.ToLower().Contains(searchText.ToLower())) {
                    using (UI.HorizontalScope()) {
                        using (UI.HorizontalScope(UI.Width(310))) {
                            var actions = etude.Blueprint.GetActions().Where(action => action.canPerform(etude.Blueprint, null));
                            foreach (var action in actions) {
                                actionLookup[action.name] = action;
                                UI.ActionButton(action.name, () => toValues[action.name] = etude.Blueprint, UI.Width(150));
                            }
                        }
                        UI.Indent(indent);
                        var style = GUIStyle.none;
                        style.fontStyle = FontStyle.Normal;
                        if (selected == etudeID) name = name.orange().bold();

                        using (UI.HorizontalScope(UI.Width(500))) {
                            if (etudeInfo.ChildrenId.Count == 0) etudeInfo.ShowChildren = ToggleState.None;
                            UI.ToggleButton(ref etudeInfo.ShowChildren, name.orange().bold(), (state) => OpenCloseAllChildren(etudeInfo, state));
                        }
                        //UI.ActionButton(UI.DisclosureGlyphOff + ">", () => OpenCloseAllChildren(etudeEntry, !etudeEntry.Foldout), GUI.skin.box, UI.AutoWidth());
                        UI.Space(25);
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
                        UI.Space(25);
                        if (etude.Blueprint.m_AllElements.Count > 0) {
                            UI.ToggleButton(ref etude.ShowElements, etude.State.ToString().yellow());
                        }
                        else
                            UI.Label(etude.State.ToString().yellow(), UI.AutoWidth());
                        UI.Space(25);
                        if (EtudeValidationProblem(etudeID, etude)) {
                            UI.Label("ValidationProblem".yellow(), UI.AutoWidth());
                            UI.Space(25);
                        }

                        if (etude.LinkedArea != BlueprintGuid.Empty)
                            UI.Label("🔗", UI.AutoWidth());
                        if (etude.CompleteParent)
                            UI.Label("⎌", UI.AutoWidth());
                        if (etude.AllowActionStart) {
                            UI.Space(25);
                            UI.Label("Can Start", UI.AutoWidth());
                        }
                        if (!string.IsNullOrEmpty(etude.Comment)) {
                            UI.Space(25);
                            var comment = Translater.cachedTranslations.GetValueOrDefault(etude.Comment, etude.Comment);
                            UI.Label(comment.green());
                        }
                    }
                    if (etude.ShowElements.IsOn()) {
                        using (UI.HorizontalScope()) {
                            UI.Space(310);
                            UI.Indent(indent + 2);
                            using (UI.VerticalScope()) {
                                foreach (var element in etude.Blueprint.m_AllElements) {
                                    using (UI.HorizontalScope()) {
                                        // UI.Label(element.NameSafe().orange()); -- this is useless at the moment
                                        UI.Label(element.ToString().yellow() ?? "?", UI.Width(450));
                                        UI.Space(25);
                                        UI.Label(element.GetType().Name.cyan(), UI.Width(250));
                                        UI.Space(25);
                                        UI.Label(element.GetDescription().green());
                                    }
                                    if (element is StartEtude started) {
                                        DrawEtude(started.Etude.Guid, loadedEtudes[started.Etude.Guid], indent + 2);
                                    }
                                    if (element is EtudeStatus status) {
                                        DrawEtude(status.m_Etude.Guid, loadedEtudes[status.m_Etude.Guid], indent + 2);
                                    }
                                    if (element is CompleteEtude completed) {
                                        DrawEtude(completed.Etude.Guid, loadedEtudes[completed.Etude.Guid], indent + 2);
                                    }
                                    UI.Div();
                                }
                            }
                        }
                    }
                }
            }
            lineNumber += 1;
        }
        private static void ShowBlueprintsTree() {
            using (UI.VerticalScope()) {
                DrawEtude(rootEtudeId, loadedEtudes[rootEtudeId], 0);
                using (UI.VerticalScope(GUI.skin.box)) {
                    ShowParentTree(loadedEtudes[rootEtudeId], 1);
                }
            }
        }
        private static void ShowParentTree(EtudeInfo etude, int indent) {
            foreach (var childrenEtude in etude.ChildrenId) {
                if (!filteredEtudes.ContainsKey(childrenEtude))
                    continue;
                DrawEtude(childrenEtude, loadedEtudes[childrenEtude], indent);

                if ((loadedEtudes[childrenEtude].ChildrenId.Count == 0) || (loadedEtudes[childrenEtude].ShowChildren.IsOff()))
                    continue;

                ShowParentTree(loadedEtudes[childrenEtude], indent + 1);
            }
        }

        private static void ApplyFilter() {
            var etudesOfArea = new Dictionary<BlueprintGuid, EtudeInfo>();

            filteredEtudes = loadedEtudes;

            if (selectedArea != null) {
                etudesOfArea = GetAreaEtudes();
                filteredEtudes = etudesOfArea;
            }

            var flaglikeEtudes = new Dictionary<BlueprintGuid, EtudeInfo>();

            if (showOnlyFlagLikes) {
                flaglikeEtudes = GetFlaglikeEtudes();
                filteredEtudes = filteredEtudes.Keys.Intersect(flaglikeEtudes.Keys)
                    .ToDictionary(t => t, t => filteredEtudes[t]);
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
                if (etude.Value.LinkedArea == selectedArea.AssetGuid) {
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
            }
            else {
                etudeIdReferences.State = EtudeInfo.EtudeState.Started;
            }
        }

        private static bool EtudeValidationProblem(BlueprintGuid etudeID, EtudeInfo etude) {
            if (etude.ChainedTo != BlueprintGuid.Empty && etude.LinkedTo != BlueprintGuid.Empty)
                return true;

            foreach (var chained in etude.ChainedId) {
                if (loadedEtudes[chained].ParentId != etude.ParentId)
                    return true;
            }

            foreach (var linked in etude.LinkedId) {
                if (loadedEtudes[linked].ParentId != etude.ParentId && loadedEtudes[linked].ParentId != etudeID)
                    return true;
            }

            return false;
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
        private static void RevealMatches(string searchText) {

        }
    }
}
