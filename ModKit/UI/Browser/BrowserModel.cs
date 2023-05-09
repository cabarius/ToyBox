using System;
using System.Collections.Generic;
using System.Text;
using ModKit.Utility;

namespace ModKit {
    public class Entry {
        private Type[] Inheritance; // this may be able to become type
        private string Title;
        private string Subtitle;
        private string Description;
        private string Identifier;
        private SerializableDictionary<string, string> extras;
        private string SearchKey;
        private string SortKey;
        private string[] Categories;
        private SerializableDictionary<string, string[]> Tags;  // indexed by category
    }
    public class Category<T> {
        private string Name;
        public delegate bool CategoryChecker(T obj);
        public delegate string[] Tagger(T obj);
        public CategoryChecker IsCategory;
        public Tagger GetTags;
    }
    
    public class DataSource {
        delegate void Updater(List<Entry> entries, int total);
        public bool IsLoading { get; private set; }
        public bool IsLoaded { get; private set; }
        public void Start() { }
        public void Cancel() { }

    }

    interface IBrowserViewModel {
        public List<Entry> Entries { get; }
        public HashSet<string> Tags { get; }
    }
    public class BrowserModel<Item, Def> : IBrowserViewModel {
        public class Category<Item, Def> : Category<Def> {
            public delegate void OnGUI(Item item, Def def);

            public OnGUI OnHeaderGUI;
            public OnGUI OnRowGUI;
            public OnGUI OnDetailGUI;
        }
        public List<Category<Item, Def>> Categories;
        public List<Entry> Entries { get;  }
        public HashSet<string> Tags { get; }
    }

    public class BrowserViewModel<Item, Def> : IBrowserViewModel {
        public BrowserModel<Item, Def>.Category<Item, Def> SelectedCategory { get; }
        public HashSet<string> SelectedTags;
        public string SearchText { get; }
        public List<Entry> Entries { get;  }
        public HashSet<string> Tags { get; }

    }
}
