using System;
using System.Collections.Generic;
using System.Text;

namespace ModKit.DataViewer {
    public static class Extensions {
        public static Key IndexerKey<Key,Item>(this Item item) => Indexer<Key, Item>.IndexerKey(item);
        public static Item IndexerItem<Key, Item>(this Key k) => Indexer<Key, Item>.IndexerItem(k);
    }
    public class Indexer<Key, Item> { // TKey must be unique or crashy crashy
        public  static Func<Item, Key> GetKey { get; set; }
        public static Key IndexerKey(Item item) => GetKey(item);
        public static Item IndexerItem(Key key) =>  _lookup.GetValueOrDefault(key).item;

        private static readonly Dictionary<Key, Entry<Item>> _lookup = new();

        public class KeyPath {
            public object Instance { get; private set; }
            public string[] path { get; private set; }
            public KeyPath(object instance, params string[] path) {
                Instance = instance;
                this.path = path;
            }
            public KeyPath(object instance, string path) {
                Instance = instance;
                this.path = path.Split('.');
            }
        }
        public class Entry<Item> {
            public Entry(Item i) {
                item = i;
            }

            public Item item { get; private set; } = default(Item);
            public HashSet<KeyPath> ReferencedBy { get; private set; } = new HashSet<KeyPath> { };
            public HashSet<KeyPath> SubItems { get; private set; } = new HashSet<KeyPath> { };
        }

        public void Add(Item item) {
            var key = IndexerKey(item);
            _lookup[key] = new Entry<Item>(item);
        }
    }
}
