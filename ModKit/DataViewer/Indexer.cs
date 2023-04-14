using System;
using System.Collections.Generic;
using System.Text;

namespace ModKit.DataViewer {
    public static class Extensions {
    }
    public class Indexer<TKey, TItem> { // TKey must be unique or crashy crashy
        public  static Func<TItem, TKey> GetKey { get; set; }
        public static TKey Key(TItem item) => GetKey(item);
        public static TItem Item(TKey key) =>  _lookup.GetValueOrDefault(key).Item;

        private static readonly Dictionary<TKey, Entry> _lookup = new();

        public class KeyPath {
            public object Instance { get; private set; }
            public string[] Path { get; private set; }
            public KeyPath(object instance, params string[] path) {
                Instance = instance;
                this.Path = path;
            }
            public KeyPath(object instance, string path) {
                Instance = instance;
                this.Path = path.Split('.');
            }
        }
        public class Entry {
            public Entry(TItem i) {
                Item = i;
            }

            public TItem Item { get; private set; } = default(TItem);
            public HashSet<KeyPath> ReferencedBy { get; private set; } = new HashSet<KeyPath> { };
            public HashSet<KeyPath> SubItems { get; private set; } = new HashSet<KeyPath> { };
        }

        public void Add(TItem item) {
            var key = Key(item);
            _lookup[key] = new Entry(item);
        }
    }
}
