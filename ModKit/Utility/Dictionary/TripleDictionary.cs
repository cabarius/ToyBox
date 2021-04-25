using System;
using System.Collections.Generic;
using System.Text;

namespace ModMaker.Utility
{
    public class TripleDictionary<TKey1, TKey2, TKey3, TValue>
    {
        private readonly Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TValue>>> _dictionary
            = new Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TValue>>>();

        public TValue this[TKey1 key1, TKey2 key2, TKey3 key3] {
            get {
                return _dictionary[key1][key2][key3];
            }
            set {
                _dictionary[key1][key2][key3] = value;
            }
        }

        public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TValue value)
        {
            if (!_dictionary.TryGetValue(key1, out Dictionary<TKey2, Dictionary<TKey3, TValue>> innerDictionary1))
            {
                _dictionary.Add(key1, innerDictionary1 = new Dictionary<TKey2, Dictionary<TKey3, TValue>>());
            }
            if (!innerDictionary1.TryGetValue(key2, out Dictionary<TKey3, TValue> innerDictionary2))
            {
                innerDictionary1.Add(key2, innerDictionary2 = new Dictionary<TKey3, TValue>());
            }
            innerDictionary2.Add(key3, value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, out TValue value)
        {
            if (!_dictionary.TryGetValue(key1, out Dictionary<TKey2, Dictionary<TKey3, TValue>> innerDictionary1))
            {
                _dictionary.Add(key1, innerDictionary1 = new Dictionary<TKey2, Dictionary<TKey3, TValue>>());
            }
            if (!innerDictionary1.TryGetValue(key2, out Dictionary<TKey3, TValue> innerDictionary2))
            {
                innerDictionary1.Add(key2, innerDictionary2 = new Dictionary<TKey3, TValue>());
            }
            return innerDictionary2.TryGetValue(key3, out value);
        }

        public TValue GetValueOrDefault(TKey1 key1, TKey2 key2, TKey3 key3, Func<TValue> getDefault)
        {
            if (!_dictionary.TryGetValue(key1, out Dictionary<TKey2, Dictionary<TKey3, TValue>> innerDictionary1))
            {
                _dictionary.Add(key1, innerDictionary1 = new Dictionary<TKey2, Dictionary<TKey3, TValue>>());
            }
            if (!innerDictionary1.TryGetValue(key2, out Dictionary<TKey3, TValue> innerDictionary2))
            {
                innerDictionary1.Add(key2, innerDictionary2 = new Dictionary<TKey3, TValue>());
            }
            if (!innerDictionary2.TryGetValue(key3, out TValue value))
            {
                innerDictionary2.Add(key3, value = getDefault());
            }
            return value;
        }
    }
}
