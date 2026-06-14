using NUnit.Framework;
using UnityEngine;
using SowurShield.Combat;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Combat Phase 2 difficulty-based accuracy and skill-use-chance
/// scaling (EnemyData.GetScaledAccuracy / GetScaledSkillUseChance), and the optional
/// accuracy parameter on CombatUnit.InitializeAsEnemy.
/// </summary>
public class CombatPhase2AccuracyTests
{
    private System.Collections.Generic.List<Object> _cleanupList = new System.Collections.Generic.List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        _cleanupList.Add(obj);
        return obj;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in _cleanupList)
        {
            if (obj != null) Object.DestroyImmediate(obj);
        }
        _cleanupList.Clear();
    }

    private EnemyData CreateEnemyData(float baseAccuracy = 1.0f, float skillUseChance = 0.3f)
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        Track(data);
        data.baseAccuracy = baseAccuracy;
        data.skillUseChance = skillUseChance;
        return data;
    }

    // =========================================================================
    // GetScaledAccuracy
    // =========================================================================

    [Test]
    public void GetScaledAccuracy_AtDifficultyOne_ReturnsBaseAccuracy()
    {
        var data = CreateEnemyData(baseAccuracy: 0.8f);

        Assert.AreEqual(0.8f, data.GetScaledAccuracy(1), 0.001f, "At difficulty 1, accuracy should equal the base value.");
    }

    [Test]
    public void GetScaledAccuracy_IncreasesWithDifficulty()
    {
        var data = CreateEnemyData(baseAccuracy: 0.8f);

        float accuracyAtOne = data.GetScaledAccuracy(1);
        float accuracyAtFive = data.GetScaledAccuracy(5);

        Assert.Greater(accuracyAtFive, accuracyAtOne, "Accuracy should increase with difficulty level.");
    }

    [Test]
    public void GetScaledAccuracy_CappedAtNinetyFivePercent()
    {
        var data = CreateEnemyData(baseAccuracy: 1.0f);

        float scaled = data.GetScaledAccuracy(10);

        Assert.AreEqual(0.95f, scaled, 0.001f, "Accuracy should be capped at 0.95 even at high difficulty.");
    }

    // =========================================================================
    // GetScaledSkillUseChance
    // =========================================================================

    [Test]
    public void GetScaledSkillUseChance_AtDifficultyOne_ReturnsBaseChance()
    {
        var data = CreateEnemyData(skillUseChance: 0.3f);

        Assert.AreEqual(0.3f, data.GetScaledSkillUseChance(1), 0.001f, "At difficulty 1, skill-use chance should equal the base value.");
    }

    [Test]
    public void GetScaledSkillUseChance_IncreasesWithDifficulty()
    {
        var data = CreateEnemyData(skillUseChance: 0.3f);

        float chanceAtOne = data.GetScaledSkillUseChance(1);
        float chanceAtFive = data.GetScaledSkillUseChance(5);

        Assert.Greater(chanceAtFive, chanceAtOne, "Skill-use chance should increase with difficulty level.");
    }

    [Test]
    public void GetScaledSkillUseChance_CappedAtSixtyPercent()
    {
        var data = CreateEnemyData(skillUseChance: 0.5f);

        float scaled = data.GetScaledSkillUseChance(10);

        Assert.AreEqual(0.6f, scaled, 0.001f, "Skill-use chance should be capped at 0.6 even at high difficulty.");
    }

    // =========================================================================
    // CombatUnit.InitializeAsEnemy — optional accuracy parameter
    // =========================================================================

    [Test]
    public void InitializeAsEnemy_WithoutAccuracyParam_DefaultsToFullAccuracy()
    {
        var go = new GameObject("TestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = false;

        unit.InitializeAsEnemy("TestUnit", 100f, 10f, 5f, 10f);

        Assert.AreEqual(1.0f, unit.GetAccuracy(), 0.001f, "InitializeAsEnemy without an accuracy argument should default to 1.0 (existing behavior).");
    }

    [Test]
    public void InitializeAsEnemy_WithAccuracyParam_SetsAccuracy()
    {
        var go = new GameObject("TestUnit");
        Track(go);
        var unit = go.AddComponent<CombatUnit>();
        unit.isPlayerUnit = false;

        unit.InitializeAsEnemy("TestUnit", 100f, 10f, 5f, 10f, 0.85f);

        Assert.AreEqual(0.85f, unit.GetAccuracy(), 0.001f, "InitializeAsEnemy should apply the provided accuracy value.");
    }
}

} // namespace SowurShield.Tests
