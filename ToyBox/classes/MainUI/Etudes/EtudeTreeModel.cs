using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox {

    public class EtudesTreeLoader {
        public IEnumerable<BlueprintEtude> etudes;
        public NamedTypeFilter<BlueprintEtude> etudeFilter = new("Etudes", null, bp => bp.CollationNames(bp.Parent?.GetBlueprint().NameSafe() ?? ""));
        public Dictionary<BlueprintGuid, EtudeIdReferences> LoadedEtudes = new();
        public Dictionary<BlueprintGuid, ConflictingGroupIdReferences> ConflictingGroups = new();

        private EtudesTreeLoader() {
            ReloadBlueprintsTree();
        }

        private static EtudesTreeLoader instance;

        public static EtudesTreeLoader Instance {
            get {
                if (instance == null) {
                    instance = new EtudesTreeLoader();

                }
                return instance;
            }
        }

        public void ReloadBlueprintsTree() {
            if (etudes == null) etudes = BlueprintLoader.Shared.GetBlueprints<BlueprintEtude>();
            LoadedEtudes = new Dictionary<BlueprintGuid, EtudeIdReferences>();
            var filtedEtudes = from bp in etudes
                               where etudeFilter.filter(bp)
                               select bp;
            foreach (var etude in filtedEtudes) {
                AddEtudeToLoaded(etude);
            }

            foreach (var loadedEtude in LoadedEtudes) {
                foreach (var etude in loadedEtude.Value.ChainedId) {
                    LoadedEtudes[etude].ChainedTo = loadedEtude.Key;
                }

                foreach (var etude in loadedEtude.Value.LinkedId) {
                    LoadedEtudes[etude].LinkedTo = loadedEtude.Key;
                }
            }
        }

        public void UpdateEtude(BlueprintEtude blueprintEtude) {
            if (LoadedEtudes.ContainsKey(blueprintEtude.AssetGuid)) {
                UpdateEtudeData(blueprintEtude);
            }
            else {
                AddEtudeToLoaded(blueprintEtude);
            }
        }

        private void UpdateEtudeData(BlueprintEtude blueprintEtude) {
            var etudeIdReference = PrepareNewEtudeData(blueprintEtude);
            var oldEtude = LoadedEtudes[blueprintEtude.AssetGuid];
            //Remove old data
            if (etudeIdReference.ChainedTo != oldEtude.ChainedTo && oldEtude.ChainedTo != BlueprintGuid.Empty && LoadedEtudes[oldEtude.ChainedTo].ChainedId.Contains(blueprintEtude.AssetGuid))
                LoadedEtudes[oldEtude.ChainedTo].ChainedId.Remove(blueprintEtude.AssetGuid);
            if (etudeIdReference.LinkedTo != oldEtude.LinkedTo && oldEtude.LinkedTo != BlueprintGuid.Empty && LoadedEtudes[oldEtude.LinkedTo].LinkedId.Contains(blueprintEtude.AssetGuid))
                LoadedEtudes[oldEtude.LinkedTo].LinkedId.Remove(blueprintEtude.AssetGuid);
            if (etudeIdReference.ParentId != oldEtude.ParentId && oldEtude.ParentId != BlueprintGuid.Empty && LoadedEtudes[oldEtude.ParentId].ChildrenId.Contains(blueprintEtude.AssetGuid))
                LoadedEtudes[oldEtude.ParentId].ChildrenId.Remove(blueprintEtude.AssetGuid);

            foreach (var etude in oldEtude.ChainedId) {
                if (!etudeIdReference.ChainedId.Contains(etude)) {
                    LoadedEtudes[etude].ChainedTo = BlueprintGuid.Empty;
                }
            }

            foreach (var etude in oldEtude.LinkedId) {
                if (!etudeIdReference.LinkedId.Contains(etude)) {
                    LoadedEtudes[etude].LinkedTo = BlueprintGuid.Empty;
                }
            }

            //Add new data

            etudeIdReference.ChildrenId = oldEtude.ChildrenId;
            etudeIdReference.ChainedTo = oldEtude.ChainedTo;
            etudeIdReference.LinkedTo = oldEtude.LinkedTo;
            if (oldEtude.ChainedTo != BlueprintGuid.Empty)
                LoadedEtudes[etudeIdReference.ChainedTo].ChainedId.Add(blueprintEtude.AssetGuid);

            if (oldEtude.LinkedTo != BlueprintGuid.Empty)
                LoadedEtudes[etudeIdReference.LinkedTo].LinkedId.Add(blueprintEtude.AssetGuid);

            LoadedEtudes[blueprintEtude.AssetGuid] = etudeIdReference;

            foreach (var etude in LoadedEtudes[blueprintEtude.AssetGuid].ChainedId) {
                LoadedEtudes[etude].ChainedTo = blueprintEtude.AssetGuid;
            }

            foreach (var etude in LoadedEtudes[blueprintEtude.AssetGuid].LinkedId) {
                LoadedEtudes[etude].LinkedTo = blueprintEtude.AssetGuid;
            }
        }

        private void AddEtudeToLoaded(BlueprintEtude blueprintEtude) {
            if (!LoadedEtudes.ContainsKey(blueprintEtude.AssetGuid)) {
                var etudeIdReference = PrepareNewEtudeData(blueprintEtude);

                LoadedEtudes.Add(blueprintEtude.AssetGuid, etudeIdReference);
            }
        }

        public void RemoveEtudeData(BlueprintGuid SelectedId) {
            if (!LoadedEtudes.ContainsKey(SelectedId))
                return;

            var etudeToRemove = LoadedEtudes[SelectedId];
            LoadedEtudes[etudeToRemove.ParentId].ChildrenId.Remove(SelectedId);

            if (etudeToRemove.LinkedTo != BlueprintGuid.Empty) {
                LoadedEtudes[etudeToRemove.LinkedTo].LinkedId.Remove(SelectedId);
            }

            if (etudeToRemove.ChainedTo != BlueprintGuid.Empty) {
                LoadedEtudes[etudeToRemove.ChainedTo].ChainedId.Remove(SelectedId);
            }

            foreach (var linkedTo in etudeToRemove.LinkedId) {
                LoadedEtudes[linkedTo].LinkedTo = BlueprintGuid.Empty;
            }

            foreach (var chainedTo in etudeToRemove.ChainedId) {
                LoadedEtudes[chainedTo].ChainedTo = BlueprintGuid.Empty;
            }

            LoadedEtudes.Remove(SelectedId);
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

            foreach (var conflictingGroup in blueprintEtude.ConflictingGroups) {
                var conflictingGroupBlueprint = conflictingGroup.GetBlueprint();

                if (conflictingGroupBlueprint == null)
                    continue;

                etudeIdReference.ConflictingGroups.Add(conflictingGroupBlueprint.AssetGuid);

                if (!ConflictingGroups.ContainsKey(conflictingGroupBlueprint.AssetGuid))
                    ConflictingGroups.Add(conflictingGroupBlueprint.AssetGuid, new ConflictingGroupIdReferences());

                ConflictingGroups[conflictingGroupBlueprint.AssetGuid].Name = conflictingGroupBlueprint.name;

                if (!ConflictingGroups[conflictingGroupBlueprint.AssetGuid].Etudes.Contains(blueprintEtude.AssetGuid))
                    ConflictingGroups[conflictingGroupBlueprint.AssetGuid].Etudes.Add(blueprintEtude.AssetGuid);
            }

            if (blueprintEtude.LinkedAreaPart != null) {
                etudeIdReference.LinkedArea = blueprintEtude.LinkedAreaPart.AssetGuid;
            }

            if (etudeIdReference.ParentId != BlueprintGuid.Empty) {
                if (!LoadedEtudes.ContainsKey(blueprintEtude.Parent.Get().AssetGuid))
                    AddEtudeToLoaded(blueprintEtude.Parent.Get());

                if (!LoadedEtudes[etudeIdReference.ParentId].ChildrenId.Contains(blueprintEtude.AssetGuid))
                    LoadedEtudes[etudeIdReference.ParentId].ChildrenId.Add(blueprintEtude.AssetGuid);
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
