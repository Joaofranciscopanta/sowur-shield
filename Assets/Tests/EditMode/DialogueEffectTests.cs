using NUnit.Framework;
using UnityEngine;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for DialogueEffect.Execute() across effect types.
/// ConversationMemory is a MonoBehaviour singleton; we create a fresh instance per test so
/// ConversationMemory.Instance is populated for the effect's internal writes.
/// </summary>
public class DialogueEffectTests
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

    // ── SetVariable ──────────────────────────────────────────────────────────

    [Test]
    public void SetVariable_RoundTripsThroughVariableCheckCondition()
    {
        var effect = new DialogueEffect
        {
            effectType = EffectType.SetVariable,
            effectKey = "metBlacksmith",
            effectValue = "true"
        };

        effect.Execute();

        var condition = new DialogueCondition
        {
            conditionType = ConditionType.VariableCheck,
            conditionKey = "metBlacksmith",
            conditionOperator = ConditionOperator.Equals,
            conditionValue = "true"
        };

        Assert.IsTrue(condition.IsConditionMet());
    }

    // ── ModifyRelationship ───────────────────────────────────────────────────

    [Test]
    public void ModifyRelationship_ChangesRelationshipByExpectedAmount()
    {
        _memory.SetRelationship("Elara", 10f);

        var effect = new DialogueEffect
        {
            effectType = EffectType.ModifyRelationship,
            effectKey = "Elara",
            numericValue = 15f
        };

        effect.Execute();

        Assert.AreEqual(25f, _memory.GetRelationshipLevel("Elara"), 0.01f);
    }

    [Test]
    public void ModifyRelationship_ClampsToPositive100()
    {
        _memory.SetRelationship("Elara", 90f);

        var effect = new DialogueEffect
        {
            effectType = EffectType.ModifyRelationship,
            effectKey = "Elara",
            numericValue = 50f
        };

        effect.Execute();

        Assert.AreEqual(100f, _memory.GetRelationshipLevel("Elara"), 0.01f);
    }

    [Test]
    public void ModifyRelationship_ClampsToNegative100()
    {
        _memory.SetRelationship("Elara", -90f);

        var effect = new DialogueEffect
        {
            effectType = EffectType.ModifyRelationship,
            effectKey = "Elara",
            numericValue = -50f
        };

        effect.Execute();

        Assert.AreEqual(-100f, _memory.GetRelationshipLevel("Elara"), 0.01f);
    }

    // ── SetQuestStatus ───────────────────────────────────────────────────────

    [Test]
    public void SetQuestStatus_UpdatesConversationMemory()
    {
        var effect = new DialogueEffect
        {
            effectType = EffectType.SetQuestStatus,
            effectKey = "q1",
            effectValue = "completed"
        };

        effect.Execute();

        Assert.AreEqual("completed", _memory.GetQuestStatus("q1"));
    }

    // ── GiveItem / TakeItem ──────────────────────────────────────────────────

    [Test]
    public void GiveItem_InvalidItemName_DoesNotThrow()
    {
        var effect = new DialogueEffect
        {
            effectType = EffectType.GiveItem,
            effectKey = "NonExistentItemXYZ",
            numericValue = 1f
        };

        Assert.DoesNotThrow(() => effect.Execute());
    }

    [Test]
    public void TakeItem_InvalidItemName_DoesNotThrow()
    {
        var effect = new DialogueEffect
        {
            effectType = EffectType.TakeItem,
            effectKey = "NonExistentItemXYZ",
            numericValue = 1f
        };

        Assert.DoesNotThrow(() => effect.Execute());
    }

    [Test]
    public void GiveItem_ZeroCount_IsNoOp()
    {
        var effect = new DialogueEffect
        {
            effectType = EffectType.GiveItem,
            effectKey = "Carrot",
            numericValue = 0f
        };

        Assert.DoesNotThrow(() => effect.Execute());
    }
}

} // namespace SowurShield.Tests
