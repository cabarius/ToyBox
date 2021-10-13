using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {

    public class EtudeChildrenDrawer {
        private Dictionary<BlueprintGuid, EtudeInfo> loadedEtudes = new Dictionary<BlueprintGuid, EtudeInfo>();
        private Dictionary<BlueprintGuid, EtudeDrawerData> etudeDrawerData = new Dictionary<BlueprintGuid, EtudeDrawerData>();
        private BlueprintGuid parentEtude;
        //private float chainedShift = 40;
        //private float linkedShift = 20;
        //private float verticalShift = 20;
        //private float lastRectMaxY;
        //private Rect workspaceRect;
        private int maxDepthToDefaultShow = 0;
        private bool FirstLayoutProcess;
        public float DefaultExpandedNodeWidth = 600;

        public ReferenceGraph ReferenceGraph;
        private ReferenceGraph.Entry selectedEntry;
        private List<ReferenceGraph.Ref> startReferences = new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> completeReferences = new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> checkReferences = new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> synchronizedReferences = new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> otherReferences = new List<ReferenceGraph.Ref>();
        private List<BlueprintGuid> conflictingGroupReferences = new List<BlueprintGuid>();
        private bool startFoldout = false;
        private bool completeFoldout = false;
        private bool checkFoldout = false;
        private bool synchronizedFoldout = false;
        private bool otherFoldout = false;
        private bool conflictingGroupFoldout = false;

        private string oldFind = "";
        private Dictionary<BlueprintGuid, EtudeInfo> foundedEtudes = new Dictionary<BlueprintGuid, EtudeInfo>();

        public static bool newParentFromContestComand = false;
        public static BlueprintGuid newParentID;

        private BlueprintGuid SelectedId;
        private string SelectedName = "";

        private EtudeChildrenDrawer() {
        }

        public EtudeChildrenDrawer(Dictionary<BlueprintGuid, EtudeInfo> etudes) {
            loadedEtudes = etudes;
        }

        public void SetParent(BlueprintGuid parent) {
            parentEtude = parent;
            etudeDrawerData = new Dictionary<BlueprintGuid, EtudeDrawerData>();
            FirstLayoutProcess = true;
        }

        public static void TryToSetParent(BlueprintGuid parent) {
            newParentFromContestComand = true;
            newParentID = parent;
        }


        public void Update() {
            var oldSelectedEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(SelectedId);
            if (oldSelectedEtude == null) {
                EtudesTreeModel.Instance.RemoveEtudeData(SelectedId);
                return;
            }

            if (oldSelectedEtude.name != SelectedName)
                EtudesTreeModel.Instance.UpdateEtude(oldSelectedEtude);
        }
        public void OnGUI() {
            if (parentEtude == BlueprintGuid.Empty)
                return;

            //HandleEvents();

            UI.Label($"Child Etudes: {loadedEtudes[parentEtude].Name}", UI.AutoWidth());

            //GUI.DrawTextureWithTexCoords(workspaceRect, etudeViewer.grid,
            //    new Rect(_zoomCoordsOrigin.x / 30, -_zoomCoordsOrigin.y / 30, workspaceRect.width / (30 * _zoom),
            //        workspaceRect.height / (30 * _zoom)));

#if false
            PrepareLayout();
            DrawSelection();
            DrawLines();
            DrawEtudes();
            DrawReferences();
            DrawFind();
            GUILayout.EndArea();
            EditorZoomArea.End();

            if (newParentFromContestComand) {
                if (loadedEtudes.ContainsKey(newParentID)) {
                    BlueprintEtude clickedEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(newParentID);
                    Selection.activeObject = BlueprintEditorWrapper.Wrap(clickedEtude);

                    if (clickedEtude.Parent.IsEmpty()) {
                        parentEtude = clickedEtude.AssetGuid;
                    }
                    else {
                        parentEtude = clickedEtude.Parent.GetBlueprint().AssetGuid;
                    }

                    etudeDrawerData = new Dictionary<BlueprintGuid, EtudeDrawerData>();
                    _zoomCoordsOrigin = Vector2.zero;
                    FirstLayoutProcess = true;
                }

                newParentFromContestComand = false;
            }
#endif
        }
    }
}
