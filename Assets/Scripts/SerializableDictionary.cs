using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// Drop-in replacement for Dictionary&lt;TKey,TValue&gt; that actually survives JsonUtility's
/// ToJson/FromJson round-trip. JsonUtility silently ignores plain Dictionary fields — it only
/// understands primitives, [Serializable] types, arrays, and List&lt;T&gt; — so every
/// Dictionary field in GameData (worldFlags, achievementsUnlocked, etc.) never actually
/// persisted between sessions. This stores the same data as two parallel serialized lists and
/// rebuilds the lookup dictionary via ISerializationCallbackReceiver, while exposing the same
/// indexer/ContainsKey/TryGetValue/Keys/Values API so no calling code needs to change.
/// </summary>
[System.Serializable]
public class SerializableDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    private Dictionary<TKey, TValue> map = new Dictionary<TKey, TValue>();

    public TValue this[TKey key]
    {
        get => map[key];
        set => map[key] = value;
    }

    public int Count => map.Count;
    public ICollection<TKey> Keys => map.Keys;
    public ICollection<TValue> Values => map.Values;

    public bool ContainsKey(TKey key) => map.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => map.TryGetValue(key, out value);
    public bool Remove(TKey key) => map.Remove(key);
    public void Clear() => map.Clear();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var kvp in map)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        map.Clear();
        int count = Mathf.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
            map[keys[i]] = values[i];
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => map.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

} // namespace SowurShield.Core
