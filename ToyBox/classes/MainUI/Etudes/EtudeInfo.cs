using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingmaker.Blueprints;
using ModKit;

namespace ToyBox {
        public class ConflictingGroupIdReferences
    {
        public string Name;
        public List<BlueprintGuid> Etudes = new();
    }
    public class EtudeInfo {
        public enum EtudeState {
            NotStated = 0,
            Started = 1,
            Active = 2,
            CompleteBeforeActive = 3,
            Completed = 4,
            ComplitionBlocked = 5
        }

        public string Name;
        public BlueprintGuid ParentId;
        public List<BlueprintGuid> LinkedId = new();
        public List<BlueprintGuid> ChainedId = new();
        public BlueprintGuid LinkedTo;
        public BlueprintGuid ChainedTo;
        public List<BlueprintGuid> ChildrenId = new();
        public bool AllowActionStart;
        public EtudeState State;
        public BlueprintGuid LinkedArea;
        public bool CompleteParent;
        public string Comment;
        public ToggleState ShowChildren;
        bool hasSearchResults;
        public List<BlueprintGuid> ConflictingGroups = new();
        public int Priority;
    }
    public class EtudeDrawerData {
        public bool ShowChildren;
        public Dictionary<BlueprintGuid, EtudeInfo> ChainStarts = new Dictionary<BlueprintGuid, EtudeInfo>();
        public bool NeedToPaint;
        public int Depth;
    }
}