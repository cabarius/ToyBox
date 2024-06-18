using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModKit.Utility;
#if false
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
        private SerializableDictionary<string, string[]> Tags; // indexed by category
        public Entry(Type[] inheritance, string title, string subtitle, string description, string identifier, SerializableDictionary<string, string> extras, string searchKey, string sortKey, string[] categories, SerializableDictionary<string, string[]> tags) {
            Inheritance = inheritance;
            Title = title;
            Subtitle = subtitle;
            Description = description;
            Identifier = identifier;
            this.extras = extras;
            SearchKey = searchKey;
            SortKey = sortKey;
            Categories = categories;
            Tags = tags;
        }
    }

    public class Category<T> {
        private string? Name;
        public delegate bool CategoryChecker(T obj);
        public delegate string[] Tagger(T obj);
        public CategoryChecker? IsCategory;
        public Tagger? GetTags;
    }

    public abstract class DataSource<Data> {
        public delegate void Updater(List<Entry> entries, int total, bool done = false);
        public delegate Entry DataTransformer(Data data);
        public Updater? UpdateProgress;
        public DataTransformer? Transformer;
        public bool IsLoading { get; private set; } = false;
        public bool IsLoaded { get; private set; } = false;

        private CancellationTokenSource? _cancelToken;

        public void Start() {
            if (IsLoading) {
                _cancelToken?.Cancel();
                IsLoading = false;
            }
            _cancelToken = new CancellationTokenSource();
            IsLoading = true;
            Task.Run(LoadData);
        }

        public void Stop() {
            if (IsLoading) {
                IsLoading = false;
                _cancelToken?.Cancel();
            }
        }

        protected abstract void LoadData();
    }

    internal interface IBrowserViewModel {
        public List<Entry> Entries { get; }
        public HashSet<string> Tags { get; }
    }

    public class BrowserModel<Item, Def> : IBrowserViewModel {
        public class Category<TItem, TDef> : Category<TDef> {
            public delegate void OnGUI(TItem item, TDef def);

            public OnGUI? OnHeaderGUI;
            public OnGUI? OnRowGUI;
            public OnGUI? OnDetailGUI;
        }

        public List<Category<Item, Def>> Categories;
        public BrowserModel(List<Category<Item, Def>> categories, List<Entry> entries, HashSet<string> tags) {
            Categories = categories;
            Entries = entries;
            Tags = tags;
        }
        public List<Entry> Entries { get; }
        public HashSet<string> Tags { get; }
    }

    public class BrowserViewModel<Item, Def> : IBrowserViewModel {
        public BrowserViewModel(HashSet<string> selectedTags, BrowserModel<Item, Def>.Category<Item, Def> selectedCategory, string searchText, List<Entry> entries, HashSet<string> tags) {
            SelectedTags = selectedTags;
            SelectedCategory = selectedCategory;
            SearchText = searchText;
            Entries = entries;
            Tags = tags;
        }
        public BrowserModel<Item, Def>.Category<Item, Def> SelectedCategory { get; }
        public HashSet<string> SelectedTags;
        public string SearchText { get; }
        public List<Entry> Entries { get; }
        public HashSet<string> Tags { get; }
    }
}
#endif