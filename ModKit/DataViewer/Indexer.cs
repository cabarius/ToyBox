using System;
using System.Collections.Generic;
using System.Text;

namespace ModKit.DataViewer {
    public static class Extensions {
        public static Key IndexerKey<Key,Value>(this Value v) => Indexer<Key, Value>.IndexerKey(v);
        public static Value IndexerValue<Key, Value>(this Key k) => Indexer<Key, Value>.IndexerValue(k);
    }
    public class Indexer<Key, Value> { // TKey must be unique or crashy crashy
        public  static Func<Value, Key> GetKey { get; set; }
        public static Key IndexerKey(Value value) => GetKey(value);
        public static Value IndexerValue(Key key) =>  _lookup.GetValueOrDefault(key).Value;

        private static readonly Dictionary<Key, Entry<Value>> _lookup = new();

        public class Entry<TValue> {
            public TValue Value;
            public HashSet<Key> ReferencedBy { get; private set; } = new HashSet<Key>();
        }

        public void Add(Value item) {

        }

    }
}
