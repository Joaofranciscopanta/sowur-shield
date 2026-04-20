using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SowurShield.Combat;
using SowurShield.Worldmap;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for Phase 2 World Map features:
///   - BiomeUnlockChecker: IsBiomeComplete, IsBiomeUnlocked, GetCompletedCount
///   - StageManager: CompleteStage, IsStageCompleted, save/load round-trip, idempotency
///
/// StageData / EnemyData are ScriptableObjects instantiated in-memory via
/// ScriptableObject.CreateInstance — no disk I/O required.
///
/// StageManager is a static class; TearDown calls ResetProgress() to prevent
/// test pollution between runs.
/// </summary>
public class WorldMapPhase2Tests
{
    // ── cleanup helpers ──────────────────────────────────────────────────────

    private List<Object> _cleanupList = new List<Object>();

    private T Track<T>(T obj) where T : Object
    {
        _cleanupList.Add(obj);
        return obj;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in _cleanupList)
            if (obj != null) Object.DestroyImmediate(obj);
        _cleanupList.Clear();

        // Reset static StageManager state so tests don't bleed into each other
        StageManager.ResetProgress();
    }

    // ── factory helpers ──────────────────────────────────────────────────────

    private StageData CreateStage(string name, StageTheme theme = StageTheme.Meadow,
        bool unlockedByDefault = false, EnemyData boss = null, StageData prerequisite = null)
    {
        var stage = ScriptableObject.CreateInstance<StageData>();
        Track(stage);
        stage.stageName = name;
        stage.theme = theme;
        stage.unlockedByDefault = unlockedByDefault;
        stage.bossEnemy = boss;
        stage.prerequisiteStage = prerequisite;
        return stage;
    }

    private EnemyData CreateEnemy(string name)
    {
        var enemy = ScriptableObject.CreateInstance<EnemyData>();
        Track(enemy);
        enemy.enemyName = name;
        return enemy;
    }

    // =========================================================================
    // BiomeUnlockChecker.IsBiomeComplete
    // =========================================================================

    [Test]
    public void IsBiomeComplete_BossStageCompleted_ReturnsTrue()
    {
        var boss = CreateEnemy("BigBoss");
        var bossStage = CreateStage("Meadow_Boss", StageTheme.Meadow, boss: boss);
        var normalStage = CreateStage("Meadow_1", StageTheme.Meadow);

        var stages = new List<StageData> { normalStage, bossStage };
        var completed = new HashSet<string> { "Meadow_Boss" };

        Assert.IsTrue(BiomeUnlockChecker.IsBiomeComplete(StageTheme.Meadow, stages, completed));
    }

    [Test]
    public void IsBiomeComplete_BossStageNotCompleted_ReturnsFalse()
    {
        var boss = CreateEnemy("BigBoss");
        var bossStage = CreateStage("Meadow_Boss", StageTheme.Meadow, boss: boss);

        var stages = new List<StageData> { bossStage };
        var completed = new HashSet<string>();

        Assert.IsFalse(BiomeUnlockChecker.IsBiomeComplete(StageTheme.Meadow, stages, completed));
    }

    [Test]
    public void IsBiomeComplete_NoBossStageExists_ReturnsFalse()
    {
        // bossEnemy is null — stage should never satisfy boss-completion check
        var normalStage = CreateStage("Meadow_1", StageTheme.Meadow);

        var stages = new List<StageData> { normalStage };
        var completed = new HashSet<string> { "Meadow_1" };

        Assert.IsFalse(BiomeUnlockChecker.IsBiomeComplete(StageTheme.Meadow, stages, completed),
            "Completing a non-boss stage must not satisfy IsBiomeComplete");
    }

    [Test]
    public void IsBiomeComplete_WrongThemeBossCompleted_ReturnsFalse()
    {
        var boss = CreateEnemy("CaveBoss");
        var bossStage = CreateStage("Cave_Boss", StageTheme.Cave, boss: boss);

        var stages = new List<StageData> { bossStage };
        var completed = new HashSet<string> { "Cave_Boss" };

        // Querying Meadow — theme mismatch must yield false
        Assert.IsFalse(BiomeUnlockChecker.IsBiomeComplete(StageTheme.Meadow, stages, completed));
    }

    [Test]
    public void IsBiomeComplete_NullStageList_ReturnsFalse()
    {
        Assert.IsFalse(BiomeUnlockChecker.IsBiomeComplete(
            StageTheme.Meadow, null, new HashSet<string>()));
    }

    [Test]
    public void IsBiomeComplete_NullCompletedSet_ReturnsFalse()
    {
        var bossStage = CreateStage("Meadow_Boss", StageTheme.Meadow,
            boss: CreateEnemy("Boss"));

        Assert.IsFalse(BiomeUnlockChecker.IsBiomeComplete(
            StageTheme.Meadow, new List<StageData> { bossStage }, null));
    }

    // =========================================================================
    // BiomeUnlockChecker.IsBiomeUnlocked
    // =========================================================================

    [Test]
    public void IsBiomeUnlocked_StageUnlockedByDefault_ReturnsTrue()
    {
        var stage = CreateStage("Meadow_1", unlockedByDefault: true);
        var stages = new List<StageData> { stage };

        Assert.IsTrue(BiomeUnlockChecker.IsBiomeUnlocked(stages, new HashSet<string>()));
    }

    [Test]
    public void IsBiomeUnlocked_PrerequisiteCompleted_ReturnsTrue()
    {
        var prereq = CreateStage("Meadow_0");
        var stage = CreateStage("Meadow_1");
        stage.prerequisiteStage = prereq;

        var stages = new List<StageData> { stage };
        var completed = new HashSet<string> { "Meadow_0" };

        Assert.IsTrue(BiomeUnlockChecker.IsBiomeUnlocked(stages, completed));
    }

    [Test]
    public void IsBiomeUnlocked_PrerequisiteNotCompleted_ReturnsFalse()
    {
        var prereq = CreateStage("Meadow_0");
        var stage = CreateStage("Meadow_1");
        stage.prerequisiteStage = prereq;

        var stages = new List<StageData> { stage };
        var completed = new HashSet<string>(); // prereq not done

        Assert.IsFalse(BiomeUnlockChecker.IsBiomeUnlocked(stages, completed));
    }

    [Test]
    public void IsBiomeUnlocked_NoDefaultNoPrereq_ReturnsFalse()
    {
        var stage = CreateStage("Meadow_1"); // unlockedByDefault=false, no prereq

        Assert.IsFalse(BiomeUnlockChecker.IsBiomeUnlocked(
            new List<StageData> { stage }, new HashSet<string>()));
    }

    [Test]
    public void IsBiomeUnlocked_NullStageList_ReturnsFalse()
    {
        Assert.IsFalse(BiomeUnlockChecker.IsBiomeUnlocked(null, new HashSet<string>()));
    }

    [Test]
    public void IsBiomeUnlocked_NullCompletedSet_ReturnsFalse()
    {
        var stage = CreateStage("Meadow_1", unlockedByDefault: true);
        Assert.IsFalse(BiomeUnlockChecker.IsBiomeUnlocked(
            new List<StageData> { stage }, null));
    }

    // =========================================================================
    // BiomeUnlockChecker.GetCompletedCount
    // =========================================================================

    [Test]
    public void GetCompletedCount_TwoOfThreeCompleted_ReturnsTwo()
    {
        var s1 = CreateStage("Stage_1");
        var s2 = CreateStage("Stage_2");
        var s3 = CreateStage("Stage_3");

        var stages = new List<StageData> { s1, s2, s3 };
        var completed = new HashSet<string> { "Stage_1", "Stage_3" };

        Assert.AreEqual(2, BiomeUnlockChecker.GetCompletedCount(stages, completed));
    }

    [Test]
    public void GetCompletedCount_NoneCompleted_ReturnsZero()
    {
        var s1 = CreateStage("Stage_1");

        Assert.AreEqual(0, BiomeUnlockChecker.GetCompletedCount(
            new List<StageData> { s1 }, new HashSet<string>()));
    }

    [Test]
    public void GetCompletedCount_AllCompleted_ReturnsFullCount()
    {
        var s1 = CreateStage("Stage_A");
        var s2 = CreateStage("Stage_B");

        var stages = new List<StageData> { s1, s2 };
        var completed = new HashSet<string> { "Stage_A", "Stage_B" };

        Assert.AreEqual(2, BiomeUnlockChecker.GetCompletedCount(stages, completed));
    }

    [Test]
    public void GetCompletedCount_NullStageList_ReturnsZero()
    {
        Assert.AreEqual(0, BiomeUnlockChecker.GetCompletedCount(
            null, new HashSet<string> { "Stage_1" }));
    }

    [Test]
    public void GetCompletedCount_NullCompletedSet_ReturnsZero()
    {
        var s1 = CreateStage("Stage_1");
        Assert.AreEqual(0, BiomeUnlockChecker.GetCompletedCount(
            new List<StageData> { s1 }, null));
    }

    // =========================================================================
    // StageManager — completion tracking
    // =========================================================================

    [Test]
    public void StageManager_CompleteStage_IsStageCompletedReturnsTrue()
    {
        var stage = CreateStage("TestStage_A");
        StageManager.CompleteStage(stage);

        Assert.IsTrue(StageManager.IsStageCompleted(stage));
    }

    [Test]
    public void StageManager_IsStageCompleted_ByName_ReturnsTrue()
    {
        var stage = CreateStage("TestStage_B");
        StageManager.CompleteStage(stage);

        Assert.IsTrue(StageManager.IsStageCompleted("TestStage_B"));
    }

    [Test]
    public void StageManager_UncompletedStage_ReturnsFalse()
    {
        var stage = CreateStage("TestStage_C");
        // NOT completed
        Assert.IsFalse(StageManager.IsStageCompleted(stage));
    }

    [Test]
    public void StageManager_CompleteStage_NullSafe()
    {
        Assert.DoesNotThrow(() => StageManager.CompleteStage(null));
    }

    [Test]
    public void StageManager_GetCompletedCount_MatchesCompletedStages()
    {
        var s1 = CreateStage("TestStage_D");
        var s2 = CreateStage("TestStage_E");

        StageManager.CompleteStage(s1);
        StageManager.CompleteStage(s2);

        Assert.AreEqual(2, StageManager.GetCompletedCount());
    }

    [Test]
    public void StageManager_CompleteStage_Idempotent()
    {
        var stage = CreateStage("Idempotent_Stage");
        StageManager.CompleteStage(stage);
        StageManager.CompleteStage(stage); // second call must be a no-op

        Assert.AreEqual(1, StageManager.GetCompletedCount(),
            "Completing the same stage twice must not inflate the count");
    }

    // =========================================================================
    // StageManager — save / load round-trip
    // =========================================================================

    [Test]
    public void StageManager_SaveLoad_RoundTrip_PreservesCompletedStages()
    {
        var s1 = CreateStage("Save_Stage_1");
        var s2 = CreateStage("Save_Stage_2");

        StageManager.CompleteStage(s1);
        StageManager.CompleteStage(s2);

        StageProgressData saveData = StageManager.GetSaveData();

        // Wipe and reload
        StageManager.ResetProgress();
        Assert.AreEqual(0, StageManager.GetCompletedCount(), "Progress should be zero after reset");

        StageManager.LoadSaveData(saveData);

        Assert.IsTrue(StageManager.IsStageCompleted("Save_Stage_1"), "Stage 1 should be restored");
        Assert.IsTrue(StageManager.IsStageCompleted("Save_Stage_2"), "Stage 2 should be restored");
        Assert.AreEqual(2, StageManager.GetCompletedCount());
    }

    [Test]
    public void StageManager_LoadSaveData_Null_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => StageManager.LoadSaveData(null));
    }

    [Test]
    public void StageManager_LoadSaveData_EmptyList_ClearsExistingProgress()
    {
        var stage = CreateStage("ToClear");
        StageManager.CompleteStage(stage);
        Assert.AreEqual(1, StageManager.GetCompletedCount());

        StageManager.LoadSaveData(new StageProgressData
        {
            completedStages = new List<string>()
        });

        Assert.AreEqual(0, StageManager.GetCompletedCount(),
            "Loading an empty list should clear all previous progress");
    }

    [Test]
    public void StageManager_GetSaveData_CompletedListMatchesInternalSet()
    {
        var s1 = CreateStage("Export_Stage_1");
        var s2 = CreateStage("Export_Stage_2");
        StageManager.CompleteStage(s1);
        StageManager.CompleteStage(s2);

        StageProgressData saved = StageManager.GetSaveData();

        Assert.AreEqual(2, saved.completedStages.Count);
        Assert.IsTrue(saved.completedStages.Contains("Export_Stage_1"));
        Assert.IsTrue(saved.completedStages.Contains("Export_Stage_2"));
    }

    // =========================================================================
    // StageManager — GetCompletedStages returns a copy (encapsulation)
    // =========================================================================

    [Test]
    public void StageManager_GetCompletedStages_ReturnsCopy_NotLiveSet()
    {
        var stage = CreateStage("Copy_Stage");
        StageManager.CompleteStage(stage);

        HashSet<string> copy = StageManager.GetCompletedStages();
        copy.Clear(); // mutate the returned copy

        Assert.AreEqual(1, StageManager.GetCompletedCount(),
            "Mutating the returned HashSet must not affect StageManager's internal state");
    }

    // =========================================================================
    // StageManager — ResetProgress
    // =========================================================================

    [Test]
    public void StageManager_ResetProgress_ClearsAllCompletedStages()
    {
        var s1 = CreateStage("Reset_S1");
        var s2 = CreateStage("Reset_S2");
        StageManager.CompleteStage(s1);
        StageManager.CompleteStage(s2);

        StageManager.ResetProgress();

        Assert.AreEqual(0, StageManager.GetCompletedCount());
        Assert.IsFalse(StageManager.IsStageCompleted(s1));
        Assert.IsFalse(StageManager.IsStageCompleted(s2));
    }
}

} // namespace SowurShield.Tests
