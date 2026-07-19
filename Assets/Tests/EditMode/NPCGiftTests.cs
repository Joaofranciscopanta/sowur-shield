using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for the NPC gifting API on ConversationMemory
/// (CanGiftToday / GiveGift), including the relationship change,
/// per-NPC daily cooldown, clamping, and save/load round-trip.
///
/// GameTimeController.instance is expected to be null in EditMode
/// (Awake never runs), so CanGiftToday/GiveGift fall back to day = 0.
/// This fallback is itself a legitimate case covered by these tests.
/// </summary>
public class NPCGiftTests
{
    private GameObject _go;
    private ConversationMemory _memory;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ConversationMemory_Test");
        _memory = _go.AddComponent<ConversationMemory>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
    }

    [Test]
    public void CanGiftToday_DefaultsToTrue_WhenNeverGifted()
    {
        Assert.IsTrue(_memory.CanGiftToday("Elara"));
    }

    [Test]
    public void GiveGift_AppliesRelationshipChange()
    {
        _memory.SetRelationship("Elara", 10f);
        _memory.GiveGift("Elara", 5f);

        Assert.AreEqual(15f, _memory.GetRelationshipLevel("Elara"), 0.01f);
    }

    [Test]
    public void GiveGift_RecordsLastGiftDay_BlocksSecondGiftSameDay()
    {
        Assert.IsTrue(_memory.CanGiftToday("Elara"));

        _memory.GiveGift("Elara", 5f);

        Assert.IsFalse(_memory.CanGiftToday("Elara"));
    }

    [Test]
    public void GiveGift_RelationshipClampedAt100()
    {
        _memory.SetRelationship("Bob", 98f);
        _memory.GiveGift("Bob", 10f);

        Assert.AreEqual(100f, _memory.GetRelationshipLevel("Bob"), 0.01f);
    }

    [Test]
    public void Gift_SaveLoad_RoundTrip()
    {
        _memory.GiveGift("Elara", 5f);

        GameData data = new GameData();
        _memory.SaveData(data);

        Assert.IsTrue(data.worldData.worldCounters.ContainsKey("lastgift_Elara"),
            "lastgift_Elara key missing after SaveData");

        GameObject go2 = new GameObject("Memory2");
        ConversationMemory mem2 = go2.AddComponent<ConversationMemory>();
        mem2.LoadData(data);

        Assert.AreEqual(_memory.CanGiftToday("Elara"), mem2.CanGiftToday("Elara"),
            "CanGiftToday result should match between original and loaded memory for the same day");

        Object.DestroyImmediate(go2);
    }

    [Test]
    public void GiveGift_MultipleNPCs_IndependentCooldowns()
    {
        _memory.GiveGift("Alice", 5f);

        Assert.IsTrue(_memory.CanGiftToday("Bob"));
    }

    [Test]
    public void GiveGift_NullOrEmptyNpcId_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _memory.GiveGift("", 5f));
        Assert.DoesNotThrow(() => _memory.GiveGift(null, 5f));

        Assert.IsFalse(_memory.CanGiftToday(""));
        Assert.IsFalse(_memory.CanGiftToday(null));
    }
}

} // namespace SowurShield.Tests
