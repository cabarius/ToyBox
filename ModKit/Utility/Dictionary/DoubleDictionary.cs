using System;
using System.Collections.Generic;

namespace ModKit.Utility {
    public class DoubleDictionary<TKey1, TKey2, TValue> {
        private readonly Dictionary<TKey1, Dictionary<TKey2, TValue>> _dictionary
            = new();

        public TValue this[TKey1 key1, TKey2 key2] {
            get {
                return _dictionary[key1][key2];
            }
            set {
                _dictionary[key1][key2] = value;
            }
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value) {
            if (!_dictionary.TryGetValue(key1, out var innerDictionary)) {
                _dictionary.Add(key1, innerDictionary = new Dictionary<TKey2, TValue>());
            }
            innerDictionary.Add(key2, value);
        }

        public void Clear() => _dictionary.Clear();

        public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value) {
            if (!_dictionary.TryGetValue(key1, out var innerDictionary)) {
                lock (_dictionary) {
                    if (!_dictionary.TryGetValue(key1, out innerDictionary)) {
                        _dictionary.Add(key1, innerDictionary = new Dictionary<TKey2, TValue>());
                    }
                }
            }
            return innerDictionary.TryGetValue(key2, out value);
        }

        public TValue GetValueOrDefault(TKey1 key1, TKey2 key2, Func<TValue> getDefault) {
            lock (_dictionary) {
                if (!_dictionary.TryGetValue(key1, out var innerDictionary)) {
                    _dictionary.Add(key1, innerDictionary = new Dictionary<TKey2, TValue>());
                }
                if (!innerDictionary.TryGetValue(key2, out var value)) {
                    innerDictionary.Add(key2, value = getDefault());
                }
                return value;
            }
        }
    }
}
