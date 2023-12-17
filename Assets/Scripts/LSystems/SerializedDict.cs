using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

[Serializable]
class SerializedDict<TKey, TValue>
{
    public List<SerializedTuple<TKey, TValue>> _values;

    public SerializedDict() {
        _values = new List<SerializedTuple<TKey, TValue>>();
    }

    public TValue this[TKey key] {
        get {
            foreach (SerializedTuple<TKey, TValue> tuple in _values) {
                if (tuple._item1.Equals(key)) return tuple._item2;
            }
            throw new Exception($"[KeyError] Key {key} wasn't found in the dictionary.");
        }
        set {
            if (ContainsKey(key)) {
                GetTupleFromKey(key)._item2 = value;
            } else {
                _values.Add(new SerializedTuple<TKey, TValue>(key, value));
            }
        }
    }

    public SerializedTuple<TKey, TValue> GetTupleFromKey(TKey key) {
        foreach (SerializedTuple<TKey, TValue> tuple in _values) {
            if (tuple._item1.Equals(key)) return tuple;
        }
        throw new Exception($"[KeyError] Key {key} wasn't found in the dictionary.");
    }

    public bool ContainsKey(TKey key) {
        foreach (SerializedTuple<TKey, TValue> tuple in _values) {
            if (tuple._item1.Equals(key)) return true;
        } 
        return false;
    }

    public bool ContainsValue(TValue value) {
        foreach (SerializedTuple<TKey, TValue> tuple in _values) {
            if (tuple._item2.Equals(value)) return true;
        } 
        return false;
    }

    public void RemoveByKey(TKey key) {
        for (int i = 0; i < _values.Count; ++i) {
            if (_values[i]._item1.Equals(key)) {
                _values.RemoveAt(i);
            }
        } 
    }
}
