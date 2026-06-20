using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;

/// <summary>
/// Edit Mode tests for SerializableDictionary. The critical behavior to verify is the
/// JsonUtility round-trip — plain Dictionary&lt;,&gt; fields are silently dropped by
/// JsonUtility.ToJson/FromJson, which is the bug this class exists to fix.
/// </summary>
public class SerializableDictionaryTests
{
    [System.Serializable]
    private class Wrapper
    {
        public SerializableDictionary<string, int> map = new SerializableDictionary<string, int>();
    }

    // =========================================================================
    // BASIC DICTIONARY-LIKE API
    // =========================================================================

    [Test]
    public void Indexer_SetThenGet_ReturnsValue()
    {
        var dict = new SerializableDictionary<string, int>();
        dict["gold"] = 100;
        Assert.AreEqual(100, dict["gold"]);
    }

    [Test]
    public void Indexer_Overwrite_ReplacesValue()
    {
        var dict = new SerializableDictionary<string, int>();
        dict["gold"] = 100;
        dict["gold"] = 250;
        Assert.AreEqual(250, dict["gold"]);
    }

    [Test]
    public void ContainsKey_ReturnsTrue_AfterSet()
    {
        var dict = new SerializableDictionary<string, bool>();
        dict["flag_a"] = true;
        Assert.IsTrue(dict.ContainsKey("flag_a"));
    }

    [Test]
    public void ContainsKey_ReturnsFalse_ForMissingKey()
    {
        var dict = new SerializableDictionary<string, bool>();
        Assert.IsFalse(dict.ContainsKey("missing"));
    }

    [Test]
    public void TryGetValue_ReturnsTrueAndValue_WhenPresent()
    {
        var dict = new SerializableDictionary<string, float>();
        dict["rel_maren"] = 12.5f;

        bool found = dict.TryGetValue("rel_maren", out float value);

        Assert.IsTrue(found);
        Assert.AreEqual(12.5f, value, 0.001f);
    }

    [Test]
    public void TryGetValue_ReturnsFalse_WhenMissing()
    {
        var dict = new SerializableDictionary<string, float>();
        bool found = dict.TryGetValue("missing", out float value);

        Assert.IsFalse(found);
        Assert.AreEqual(0f, value);
    }

    [Test]
    public void Remove_DeletesKey()
    {
        var dict = new SerializableDictionary<string, int>();
        dict["a"] = 1;
        dict.Remove("a");
        Assert.IsFalse(dict.ContainsKey("a"));
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
        var dict = new SerializableDictionary<string, int>();
        dict["a"] = 1;
        dict["b"] = 2;
        dict.Clear();
        Assert.AreEqual(0, dict.Count);
    }

    [Test]
    public void Count_ReflectsNumberOfEntries()
    {
        var dict = new SerializableDictionary<string, int>();
        dict["a"] = 1;
        dict["b"] = 2;
        dict["c"] = 3;
        Assert.AreEqual(3, dict.Count);
    }

    [Test]
    public void ForEach_YieldsAllKeyValuePairs()
    {
        var dict = new SerializableDictionary<string, int>();
        dict["a"] = 1;
        dict["b"] = 2;

        int sum = 0;
        foreach (var kv in dict)
            sum += kv.Value;

        Assert.AreEqual(3, sum);
    }

    // =========================================================================
    // JSONUTILITY ROUND-TRIP — the actual bug this class fixes
    // =========================================================================

    [Test]
    public void JsonRoundTrip_PreservesEntries()
    {
        var wrapper = new Wrapper();
        wrapper.map["cropsHarvested"] = 5;
        wrapper.map["animalsOwned"] = 2;

        string json = JsonUtility.ToJson(wrapper);
        var restored = JsonUtility.FromJson<Wrapper>(json);

        Assert.AreEqual(5, restored.map["cropsHarvested"]);
        Assert.AreEqual(2, restored.map["animalsOwned"]);
    }

    [Test]
    public void JsonRoundTrip_PreservesCount()
    {
        var wrapper = new Wrapper();
        wrapper.map["a"] = 1;
        wrapper.map["b"] = 2;
        wrapper.map["c"] = 3;

        string json = JsonUtility.ToJson(wrapper);
        var restored = JsonUtility.FromJson<Wrapper>(json);

        Assert.AreEqual(3, restored.map.Count);
    }

    [Test]
    public void JsonRoundTrip_EmptyDictionary_StaysEmpty()
    {
        var wrapper = new Wrapper();

        string json = JsonUtility.ToJson(wrapper);
        var restored = JsonUtility.FromJson<Wrapper>(json);

        Assert.AreEqual(0, restored.map.Count);
    }

    [Test]
    public void JsonRoundTrip_OverwrittenValue_PersistsLatestOnly()
    {
        var wrapper = new Wrapper();
        wrapper.map["gold"] = 100;
        wrapper.map["gold"] = 250;

        string json = JsonUtility.ToJson(wrapper);
        var restored = JsonUtility.FromJson<Wrapper>(json);

        Assert.AreEqual(1, restored.map.Count);
        Assert.AreEqual(250, restored.map["gold"]);
    }

    [Test]
    public void RawDictionary_DoesNotSurviveJsonRoundTrip_DemonstratingTheBug()
    {
        // Sanity check proving the bug SerializableDictionary fixes actually exists —
        // if this test ever fails, JsonUtility started supporting Dictionary natively
        // and SerializableDictionary may no longer be necessary.
        var wrapper = new RawDictionaryWrapper();
        wrapper.map["gold"] = 100;

        string json = JsonUtility.ToJson(wrapper);
        var restored = JsonUtility.FromJson<RawDictionaryWrapper>(json);

        Assert.AreEqual(0, restored.map.Count,
            "JsonUtility was expected to silently drop plain Dictionary fields.");
    }

    [System.Serializable]
    private class RawDictionaryWrapper
    {
        public System.Collections.Generic.Dictionary<string, int> map =
            new System.Collections.Generic.Dictionary<string, int>();
    }
}
