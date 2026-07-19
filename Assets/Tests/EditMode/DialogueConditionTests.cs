using NUnit.Framework;
using UnityEngine;
using System.Reflection;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for DialogueCondition.IsConditionMet() across condition types and operators.
/// ConversationMemory is a MonoBehaviour singleton; we create a fresh instance per test so
/// ConversationMemory.Instance is populated for the condition's internal lookups.
/// </summary>
public class DialogueConditionTests
{
    private GameObject _go;
    private ConversationMemory _memory;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("ConversationMemory_Test");
        _memory = _go.AddComponent<ConversationMemory>();

        // AddComponent() does not run Awake() synchronously in Edit Mode, and Awake()
        // itself calls DontDestroyOnLoad() which throws outside Play Mode — so set the
        // singleton Instance directly via reflection instead of invoking Awake().
        // The conversationData property lazily initializes itself on first access.
        typeof(ConversationMemory).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            .SetValue(null, _memory);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null)
            Object.DestroyImmediate(_go);
    }

    // ── Always ───────────────────────────────────────────────────────────────

    [Test]
    public void Always_ReturnsTrue()
    {
        var condition = new DialogueCondition { conditionType = ConditionType.Always };

        Assert.IsTrue(condition.IsConditionMet());
    }

    [Test]
    public void Always_Inverted_ReturnsFalse()
    {
        var condition = new DialogueCondition { conditionType = ConditionType.Always, invertCondition = true };

        Assert.IsFalse(condition.IsConditionMet());
    }

    // ── RelationshipLevel ────────────────────────────────────────────────────

    [Test]
    public void RelationshipLevel_GreaterOrEqual_TrueWhenAboveThreshold()
    {
        _memory.SetRelationship("Elara", 50f);

        var condition = new DialogueCondition
        {
            conditionType = ConditionType.RelationshipLevel,
            conditionKey = "Elara",
            conditionOperator = ConditionOperator.GreaterOrEqual,
            conditionValue = "40"
        };

        Assert.IsTrue(condition.IsConditionMet());
    }

    [Test]
    public void RelationshipLevel_LessThan_FalseWhenAboveThreshold()
    {
        _memory.SetRelationship("Elara", 50f);

        var condition = new DialogueCondition
        {
            conditionType = ConditionType.RelationshipLevel,
            conditionKey = "Elara",
            conditionOperator = ConditionOperator.LessThan,
            conditionValue = "40"
        };

        Assert.IsFalse(condition.IsConditionMet());
    }

    // ── QuestStatus ──────────────────────────────────────────────────────────

    [Test]
    public void QuestStatus_Equals_TrueWhenStatusMatches()
    {
        _memory.SetQuestStatus("q1", "completed");

        var condition = new DialogueCondition
        {
            conditionType = ConditionType.QuestStatus,
            conditionKey = "q1",
            conditionOperator = ConditionOperator.Equals,
            conditionValue = "completed"
        };

        Assert.IsTrue(condition.IsConditionMet());
    }

    [Test]
    public void QuestStatus_Equals_IsCaseInsensitive()
    {
        _memory.SetQuestStatus("q1", "Completed");

        var condition = new DialogueCondition
        {
            conditionType = ConditionType.QuestStatus,
            conditionKey = "q1",
            conditionOperator = ConditionOperator.Equals,
            conditionValue = "completed"
        };

        Assert.IsTrue(condition.IsConditionMet());
    }

    // ── VariableCheck ────────────────────────────────────────────────────────

    [Test]
    public void VariableCheck_Equals_TrueWhenVariableMatches()
    {
        _memory.SetVariable("metBlacksmith", "true");

        var condition = new DialogueCondition
        {
            conditionType = ConditionType.VariableCheck,
            conditionKey = "metBlacksmith",
            conditionOperator = ConditionOperator.Equals,
            conditionValue = "true"
        };

        Assert.IsTrue(condition.IsConditionMet());
    }

    [Test]
    public void VariableCheck_NotEquals_TrueWhenVariableDiffers()
    {
        _memory.SetVariable("metBlacksmith", "false");

        var condition = new DialogueCondition
        {
            conditionType = ConditionType.VariableCheck,
            conditionKey = "metBlacksmith",
            conditionOperator = ConditionOperator.NotEquals,
            conditionValue = "true"
        };

        Assert.IsTrue(condition.IsConditionMet());
    }

    // ── ConversationCompleted / ChoiceMade ──────────────────────────────────

    [Test]
    public void ConversationCompleted_FalseWhenNeverCompleted()
    {
        var condition = new DialogueCondition
        {
            conditionType = ConditionType.ConversationCompleted,
            conditionKey = "intro_conversation"
        };

        Assert.IsFalse(condition.IsConditionMet());
    }

    [Test]
    public void ChoiceMade_FalseWhenNeverMade()
    {
        var condition = new DialogueCondition
        {
            conditionType = ConditionType.ChoiceMade,
            conditionKey = "intro_conversation",
            conditionValue = "Hello!"
        };

        Assert.IsFalse(condition.IsConditionMet());
    }

    // ── InventoryItem ────────────────────────────────────────────────────────

    [Test]
    public void InventoryItem_GreaterOrEqual_FalseWhenNoInventoryInScene()
    {
        // No Inventory exists in the test scene, and "NonExistentItemXYZ" is not in
        // ItemDatabase — both early-out to currentAmount = 0.
        var condition = new DialogueCondition
        {
            conditionType = ConditionType.InventoryItem,
            conditionKey = "NonExistentItemXYZ",
            conditionOperator = ConditionOperator.GreaterOrEqual,
            conditionValue = "1"
        };

        Assert.IsFalse(condition.IsConditionMet());
    }
}

} // namespace SowurShield.Tests
