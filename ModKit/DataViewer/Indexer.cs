using System;
using System.Collections.Generic;
using System.Text;

namespace ModKit.DataViewer {
    public static class Extensions {
    }
    public class Indexer<TKey, TItem> { // TKey must be unique or crashy crashy
        public static Func<TItem, TKey> GetKey { get; set; }
        public static Func<TKey, TItem> GetItem { get; set; }
        public static TKey Key(TItem item) => GetKey(item);
        public static TItem Item(TKey key) => GetItem(key);
        public static Entry GetEntry(TKey key) =>  _lookup.GetValueOrDefault(key);

        private static readonly Dictionary<TKey, Entry> _lookup = new();

        public class KeyPath {
            public TKey Key { get; private set; }
            public string[] Path { get; private set; }
            public KeyPath(TKey key, params string[] path) {
                Key = key;
                this.Path = path;
            }
            public KeyPath(TKey key, string path) {
                Key = key;
                this.Path = path.Split('.');
            }
            public KeyPath(TItem item, params string[] path) {
                Key = Key(item);
                this.Path = path;
            }
            public KeyPath(TItem item, string path) {
                Key = Key(item);
                this.Path = path.Split('.');
            }
        }
        public class Entry {
            public TKey Key { get; private set; }
            public HashSet<KeyPath> ReferencedBy { get; private set; } = new HashSet<KeyPath> { };
            public HashSet<KeyPath> SubItems { get; private set; } = new HashSet<KeyPath> { };
            public Dictionary<KeyPath, string> Properties { get; private set; }
            private TItem _item { get; set; } = default;
            public TItem Item {
                get {
                    _item ??= GetItem(Key);
                    return _item;
                }
            }
            public Entry(TKey key) {
                Key = key;
            }
            public Entry(TItem item, TKey key) {
                _item = item;
                Key = key;
            }
        }

        public void Add(TItem item) {
            var key = Key(item);
            _lookup[key] = new Entry(item, key);
        }
    }
}
