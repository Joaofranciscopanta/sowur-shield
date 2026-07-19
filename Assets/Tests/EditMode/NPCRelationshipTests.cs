using NUnit.Framework;
using UnityEngine;
using SowurShield.Core;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for the NPC relationship / ConversationMemory ISaveable bridge.
///
/// ConversationMemory is a MonoBehaviour singleton; we create it on a temporary
/// GameObject and bypass the DontDestroyOnLoad path by testing SaveData/LoadData
/// directly rather than going through the singleton registration.
/// </summary>
public class NPCRelationshipTests
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

    // ── Save / Load round-trips ───────────────────────────────────────────────

    [Test]
    public void Relationship_SaveLoad_RoundTrip()
    {
        _memory.SetRelationship("Elara", 45.5f);

        GameData data = new GameData();
        _memory.SaveData(data);

        // Expect key "rel_Elara" = 4550 (45.5 × 100)
        Assert.IsTrue(data.worldData.worldCounters.ContainsKey("rel_Elara"),
            "rel_Elara key missing after SaveData");
        Assert.AreEqual(4550, data.worldData.worldCounters["rel_Elara"]);

        // Now load into a fresh memory instance
        GameObject go2 = new GameObject("Memory2");
        ConversationMemory mem2 = go2.AddComponent<ConversationMemory>();
        mem2.LoadData(data);

        Assert.AreEqual(45.5f, mem2.GetRelationshipLevel("Elara"), 0.01f,
            "Relationship level not restored after LoadData");

        Object.DestroyImmediate(go2);
    }

    [Test]
    public void Relationship_DefaultsToZero_WhenNotSaved()
    {
        GameData data = new GameData();
        _memory.LoadData(data);

        Assert.AreEqual(0f, _memory.GetRelationshipLevel("Nobody"), 0.001f);
    }

    [Test]
    public void Relationship_ClampedAt100_WhenSaved()
    {
        _memory.SetRelationship("Bob", 150f); // internally clamped to 100 by ConversationData

        GameData data = new GameData();
        _memory.SaveData(data);

        // Should save 10000 (100.0 × 100), not 15000
        Assert.AreEqual(10000, data.worldData.worldCounters["rel_Bob"]);
    }

    [Test]
    public void Relationship_NegativeValues_SaveLoad()
    {
        _memory.SetRelationship("Villain", -30f);

        GameData data = new GameData();
        _memory.SaveData(data);

        Assert.AreEqual(-3000, data.worldData.worldCounters["rel_Villain"]);

        _memory.LoadData(data);
        Assert.AreEqual(-30f, _memory.GetRelationshipLevel("Villain"), 0.01f);
    }

    [Test]
    public void MultipleNPCs_AllSavedAndLoaded()
    {
        _memory.SetRelationship("Alice", 10f);
        _memory.SetRelationship("Bob", 25f);
        _memory.SetRelationship("Carol", 75f);

        GameData data = new GameData();
        _memory.SaveData(data);

        GameObject go2 = new GameObject("Memory2");
        ConversationMemory mem2 = go2.AddComponent<ConversationMemory>();
        mem2.LoadData(data);

        Assert.AreEqual(10f,  mem2.GetRelationshipLevel("Alice"), 0.01f);
        Assert.AreEqual(25f,  mem2.GetRelationshipLevel("Bob"),   0.01f);
        Assert.AreEqual(75f,  mem2.GetRelationshipLevel("Carol"), 0.01f);

        Object.DestroyImmediate(go2);
    }

    // ── Completed conversations ───────────────────────────────────────────────

    [Test]
    public void CompletedConversation_SaveLoad_RoundTrip()
    {
        _memory.CompleteConversation("intro_elara");
        _memory.CompleteConversation("market_quest");

        GameData data = new GameData();
        _memory.SaveData(data);

        Assert.IsTrue(data.worldData.worldFlags.ContainsKey("conv_done_intro_elara"));
        Assert.IsTrue(data.worldData.worldFlags["conv_done_intro_elara"]);
        Assert.IsTrue(data.worldData.worldFlags.ContainsKey("conv_done_market_quest"));

        GameObject go2 = new GameObject("Memory2");
        ConversationMemory mem2 = go2.AddComponent<ConversationMemory>();
        mem2.LoadData(data);

        Assert.IsTrue(mem2.HasCompletedConversation("intro_elara"));
        Assert.IsTrue(mem2.HasCompletedConversation("market_quest"));
        Assert.IsFalse(mem2.HasCompletedConversation("unknown_conv"));

        Object.DestroyImmediate(go2);
    }

    // ── Quest statuses ────────────────────────────────────────────────────────

    [Test]
    public void QuestStatus_SaveLoad_RoundTrip()
    {
        _memory.SetQuestStatus("missing_cat", "active");
        _memory.SetQuestStatus("herb_gathering", "completed");

        GameData data = new GameData();
        _memory.SaveData(data);

        Assert.AreEqual("active",    data.worldData.worldStrings["quest_missing_cat"]);
        Assert.AreEqual("completed", data.worldData.worldStrings["quest_herb_gathering"]);

        GameObject go2 = new GameObject("Memory2");
        ConversationMemory mem2 = go2.AddComponent<ConversationMemory>();
        mem2.LoadData(data);

        Assert.AreEqual("active",    mem2.GetQuestStatus("missing_cat"));
        Assert.AreEqual("completed", mem2.GetQuestStatus("herb_gathering"));

        Object.DestroyImmediate(go2);
    }

    // ── Custom variables ──────────────────────────────────────────────────────

    [Test]
    public void CustomVariable_SaveLoad_RoundTrip()
    {
        _memory.SetVariable("gave_flower", "true");

        GameData data = new GameData();
        _memory.SaveData(data);

        Assert.IsTrue(data.worldData.worldStrings.ContainsKey("dlgvar_gave_flower"));
        Assert.AreEqual("true", data.worldData.worldStrings["dlgvar_gave_flower"]);

        GameObject go2 = new GameObject("Memory2");
        ConversationMemory mem2 = go2.AddComponent<ConversationMemory>();
        mem2.LoadData(data);

        Assert.AreEqual("true", mem2.GetVariable("gave_flower"));

        Object.DestroyImmediate(go2);
    }

    // ── Null safety ───────────────────────────────────────────────────────────

    [Test]
    public void SaveData_NullGameData_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _memory.SaveData(null));
    }

    [Test]
    public void LoadData_NullGameData_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _memory.LoadData(null));
    }
}

} // namespace SowurShield.Tests
