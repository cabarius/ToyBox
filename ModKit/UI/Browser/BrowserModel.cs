using System;
using System.Collections.Generic;
using System.Text;
using ModKit.Utility;

namespace ModKit {

    class ModelActionUI {
        delegate bool Filter(DefinitionEntry entry);
        private Filter ExtraFilter; // used to restrict actions to certain subtypes of Definitions
        float OnGUI() => 0.0f; // renders actions, returns the space required
    }
    class BrowserModel {
        private SerializableDictionary<Type, DefinitionEntry> entries; // type => entry
        private SerializableDictionary<Type, HashSet<string>> collations; // collation keys (type => key set)
        private Dictionary<Type, List<ModelActionUI>> actions = new();
    }
    class DefinitionEntry {
        private Type[] inheritance;
        private string title;
        private string subtitle;
        private string description;
        private string identifier;
        private SerializableDictionary<string, string> extras;
        private string searchKey;
        private string sortKey;
        private string[] categories;
    }
}
