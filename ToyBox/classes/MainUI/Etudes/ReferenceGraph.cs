using System.Collections.Generic;
using System.Linq;

namespace ToyBox {
    public class ReferenceGraph
    {
        public enum ValidationStateType
        {
            Normal,
            Warning,
            Error
        }

        public class Entry
        {
            public string ObjectGuid;
            public string ObjectName;
            public string ObjectType;
            public string OwnerName;

            public List<Ref> References;

            public int FullReferencesMask => References == null || References.Count == 0
                ? 0
                : References.Select(r => r.ReferenceTypeMask).Aggregate((a, b) => a | b);

            public string ValidationResult;
            public ValidationStateType ValidationState;
        }

        public class SceneEntity
        {
            public string GUID;
            public List<Ref> Refs = new List<Ref>();
        }

        public class Ref
        {
            public string AssetPath;
            public string AssetType;
            public string ReferencingObjectName;
            public string ReferencingObjectType;
            //public string TransformPath; // for scene references, path to referencing obj transform
            public int ReferenceTypeMask;
            public string RefChasingAssetGuid;
            public bool IsScene => AssetPath.EndsWith(".unity");

#if false
            public string AssetGuid
                => AssetPath.StartsWith("Assets")
                    ? AssetDatabase.AssetPathToGUID(AssetPath)
                    : BlueprintsDatabase.PathToId(AssetPath).ToString();
#endif
        }

        public class EntityRef
        {
            public string AssetPath;
            public string AssetName;
            public string UsagesType;
        }

        public readonly List<Entry> Entries = new List<Entry>();
        public readonly List<SceneEntity> SceneEntitys = new List<SceneEntity>();
        private List<string> m_ReferencingBlueprintPaths;
        private List<string> m_ReferencingScenesPaths;
        private Dictionary<string, Entry> m_EntriesByGuid;
        private Dictionary<string, SceneEntity> m_SceneObjectRefs;
        private readonly Dictionary<string, string> m_TypeNamesByGuid = new Dictionary<string, string>();

    }
}
