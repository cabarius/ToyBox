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
        private BlueprintGuid parentEtude;
        public float DefaultExpandedNodeWidth = 600;

        public ReferenceGraph ReferenceGraph;

        public static bool newParentFromContestComand = false;
        public static BlueprintGuid newParentID;

        private readonly BlueprintGuid SelectedId;
        private string SelectedName = "";

        private EtudeChildrenDrawer() {
        }

        public EtudeChildrenDrawer(Dictionary<BlueprintGuid, EtudeInfo> etudes) {
            loadedEtudes = etudes;
        }

        public void SetParent(BlueprintGuid parent) {
            parentEtude = parent;
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
