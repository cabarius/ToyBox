using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using ModKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ToyBox {
    public class EtudesTreeModel {
        public List<BlueprintEtude> etudes;
        public NamedTypeFilter<BlueprintEtude> etudeFilter = new("Etudes", null, bp => bp.CollationNames(bp.Parent?.GetBlueprint().NameSafe() ?? ""));
        public Dictionary<BlueprintGuid, EtudeIdReferences> loadedEtudes = new();
        public Dictionary<BlueprintGuid, ConflictingGroupIdReferences> conflictingGroups = new();
        public HashSet<string> commentKeys = new();
        private EtudesTreeModel() {
            //ReloadBlueprintsTree(bps => { });
        }

        private static EtudesTreeModel instance;

        public static EtudesTreeModel Instance {
            get {
                if (instance == null) {
                    instance = new EtudesTreeModel();

                }
                return instance;
            }
        }

        public void ReloadBlueprintsTree() {
            etudes = BlueprintLoader.Shared.GetBlueprints<BlueprintEtude>();
            if (etudes == null) return;
            loadedEtudes = new Dictionary<BlueprintGuid, EtudeIdReferences>();
            var filteredEtudes = (from bp in etudes
                                  where etudeFilter.filter(bp)
                                  select bp).ToList();
            foreach (var etude in filteredEtudes) {
                AddEtudeToLoaded(etude);
            }

            foreach (var loadedEtude in loadedEtudes) {
                foreach (var etude in loadedEtude.Value.ChainedId) {
                    loadedEtudes[etude].ChainedTo = loadedEtude.Key;
                }

                foreach (var etude in loadedEtude.Value.LinkedId) {
                    loadedEtudes[etude].LinkedTo = loadedEtude.Key;
                }
            }
            //Translater.MassTranslate(commentKeys.ToList());
        }

        public void UpdateEtude(BlueprintEtude blueprintEtude) {
            if (loadedEtudes.ContainsKey(blueprintEtude.AssetGuid)) {
                UpdateEtudeData(blueprintEtude);
            }
            else {
                AddEtudeToLoaded(blueprintEtude);
            }
        }

        private void UpdateEtudeData(BlueprintEtude blueprintEtude) {
            var etudeIdReference = PrepareNewEtudeData(blueprintEtude);
            var oldEtude = loadedEtudes[blueprintEtude.AssetGuid];
            //Remove old data
            if (etudeIdReference.ChainedTo != oldEtude.ChainedTo && oldEtude.ChainedTo != BlueprintGuid.Empty && loadedEtudes[oldEtude.ChainedTo].ChainedId.Contains(blueprintEtude.AssetGuid))
                loadedEtudes[oldEtude.ChainedTo].ChainedId.Remove(blueprintEtude.AssetGuid);
            if (etudeIdReference.LinkedTo != oldEtude.LinkedTo && oldEtude.LinkedTo != BlueprintGuid.Empty && loadedEtudes[oldEtude.LinkedTo].LinkedId.Contains(blueprintEtude.AssetGuid))
                loadedEtudes[oldEtude.LinkedTo].LinkedId.Remove(blueprintEtude.AssetGuid);
            if (etudeIdReference.ParentId != oldEtude.ParentId && oldEtude.ParentId != BlueprintGuid.Empty && loadedEtudes[oldEtude.ParentId].ChildrenId.Contains(blueprintEtude.AssetGuid))
                loadedEtudes[oldEtude.ParentId].ChildrenId.Remove(blueprintEtude.AssetGuid);

            foreach (var etude in oldEtude.ChainedId) {
                if (!etudeIdReference.ChainedId.Contains(etude)) {
                    loadedEtudes[etude].ChainedTo = BlueprintGuid.Empty;
                }
            }

            foreach (var etude in oldEtude.LinkedId) {
                if (!etudeIdReference.LinkedId.Contains(etude)) {
                    loadedEtudes[etude].LinkedTo = BlueprintGuid.Empty;
                }
            }

            //Add new data

            etudeIdReference.ChildrenId = oldEtude.ChildrenId;
            etudeIdReference.ChainedTo = oldEtude.ChainedTo;
            etudeIdReference.LinkedTo = oldEtude.LinkedTo;
            if (oldEtude.ChainedTo != BlueprintGuid.Empty)
                loadedEtudes[etudeIdReference.ChainedTo].ChainedId.Add(blueprintEtude.AssetGuid);

            if (oldEtude.LinkedTo != BlueprintGuid.Empty)
                loadedEtudes[etudeIdReference.LinkedTo].LinkedId.Add(blueprintEtude.AssetGuid);

            loadedEtudes[blueprintEtude.AssetGuid] = etudeIdReference;

            foreach (var etude in loadedEtudes[blueprintEtude.AssetGuid].ChainedId) {
                loadedEtudes[etude].ChainedTo = blueprintEtude.AssetGuid;
            }

            foreach (var etude in loadedEtudes[blueprintEtude.AssetGuid].LinkedId) {
                loadedEtudes[etude].LinkedTo = blueprintEtude.AssetGuid;
            }
        }

        private void AddEtudeToLoaded(BlueprintEtude blueprintEtude) {
            if (!loadedEtudes.ContainsKey(blueprintEtude.AssetGuid)) {
                var etudeIdReference = PrepareNewEtudeData(blueprintEtude);

                loadedEtudes.Add(blueprintEtude.AssetGuid, etudeIdReference);
            }
        }

        public void RemoveEtudeData(BlueprintGuid SelectedId) {
            if (!loadedEtudes.ContainsKey(SelectedId))
                return;

            var etudeToRemove = loadedEtudes[SelectedId];
            loadedEtudes[etudeToRemove.ParentId].ChildrenId.Remove(SelectedId);

            if (etudeToRemove.LinkedTo != BlueprintGuid.Empty) {
                loadedEtudes[etudeToRemove.LinkedTo].LinkedId.Remove(SelectedId);
            }

            if (etudeToRemove.ChainedTo != BlueprintGuid.Empty) {
                loadedEtudes[etudeToRemove.ChainedTo].ChainedId.Remove(SelectedId);
            }

            foreach (var linkedTo in etudeToRemove.LinkedId) {
                loadedEtudes[linkedTo].LinkedTo = BlueprintGuid.Empty;
            }

            foreach (var chainedTo in etudeToRemove.ChainedId) {
                loadedEtudes[chainedTo].ChainedTo = BlueprintGuid.Empty;
            }

            loadedEtudes.Remove(SelectedId);
        }

        private EtudeIdReferences PrepareNewEtudeData(BlueprintEtude blueprintEtude) {
            var etudeIdReference = new EtudeIdReferences {
                Name = blueprintEtude.name,
                ParentId = blueprintEtude.Parent?.Get()?.AssetGuid ?? BlueprintGuid.Empty,
                AllowActionStart = blueprintEtude.AllowActionStart,
                CompleteParent = blueprintEtude.CompletesParent,
                Comment = blueprintEtude.Comment,
                Priority = blueprintEtude.Priority
            };

            if (blueprintEtude.Comment.Length > 0)
                commentKeys.Add(blueprintEtude.Comment);

            foreach (var conflictingGroup in blueprintEtude.ConflictingGroups) {
                var conflictingGroupBlueprint = conflictingGroup.GetBlueprint();

                if (conflictingGroupBlueprint == null)
                    continue;

                etudeIdReference.ConflictingGroups.Add(conflictingGroupBlueprint.AssetGuid);

                if (!conflictingGroups.ContainsKey(conflictingGroupBlueprint.AssetGuid))
                    conflictingGroups.Add(conflictingGroupBlueprint.AssetGuid, new ConflictingGroupIdReferences());

                conflictingGroups[conflictingGroupBlueprint.AssetGuid].Name = conflictingGroupBlueprint.name;

                if (!conflictingGroups[conflictingGroupBlueprint.AssetGuid].Etudes.Contains(blueprintEtude.AssetGuid))
                    conflictingGroups[conflictingGroupBlueprint.AssetGuid].Etudes.Add(blueprintEtude.AssetGuid);
            }

            if (blueprintEtude.LinkedAreaPart != null) {
                etudeIdReference.LinkedArea = blueprintEtude.LinkedAreaPart.AssetGuid;
            }

            if (etudeIdReference.ParentId != BlueprintGuid.Empty) {
                if (!loadedEtudes.ContainsKey(blueprintEtude.Parent.Get().AssetGuid))
                    AddEtudeToLoaded(blueprintEtude.Parent.Get());

                if (!loadedEtudes[etudeIdReference.ParentId].ChildrenId.Contains(blueprintEtude.AssetGuid))
                    loadedEtudes[etudeIdReference.ParentId].ChildrenId.Add(blueprintEtude.AssetGuid);
            }

            foreach (var chainedStart in blueprintEtude.StartsOnComplete) {
                if (chainedStart.Get() == null)
                    continue;

                etudeIdReference.ChainedId.Add(chainedStart.Get().AssetGuid);
            }

            foreach (var linkedStart in blueprintEtude.StartsWith) {
                if (linkedStart.Get() == null)
                    continue;


                etudeIdReference.LinkedId.Add(linkedStart.Get().AssetGuid);
            }

            return etudeIdReference;
        }
    }
}
