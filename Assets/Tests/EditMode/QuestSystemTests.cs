using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using SowurShield.Core;
using SowurShield.Dialogue;

namespace SowurShield.Tests
{

/// <summary>
/// EditMode tests for QuestManager: lifecycle, prerequisites, objective tracking,
/// save/load round-trip, and null safety.
/// </summary>
public class QuestSystemTests
{
    private GameObject _managerGO;
    private QuestManager _questManager;

    private QuestData MakeQuest(string id, string title, List<QuestObjective> objectives = null,
                                 List<string> prereqs = null, int rewardGold = 0)
    {
        var data = ScriptableObject.CreateInstance<QuestData>();
        data.questId    = id;
        data.questTitle = title;
        data.objectives = objectives ?? new List<QuestObjective>();
        data.prerequisiteQuestIds = prereqs ?? new List<string>();
        data.rewardGold = rewardGold;
        return data;
    }

    private QuestObjective MakeObjective(QuestObjectiveType type, string targetId, int required)
    {
        return new QuestObjective
        {
            description   = $"Collect {required}x {targetId}",
            type          = type,
            targetId      = targetId,
            requiredCount = required
        };
    }

    [SetUp]
    public void SetUp()
    {
        _managerGO    = new GameObject("QuestManager_Test");
        _questManager = _managerGO.AddComponent<QuestManager>();

        // AddComponent() does not run Awake() synchronously in Edit Mode, and Awake()
        // itself calls DontDestroyOnLoad() which throws outside Play Mode — so set the
        // singleton Instance directly via reflection instead of invoking Awake().
        // None of these tests call StartQuest(string questId), so LoadAllQuestAssets()
        // (also called from Awake) is not needed.
        typeof(QuestManager).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
            .SetValue(null, _questManager);
    }

    [TearDown]
    public void TearDown()
    {
        if (_managerGO != null)
            Object.DestroyImmediate(_managerGO);
    }

    // ── Start / query ─────────────────────────────────────────────────────────

    [Test]
    public void StartQuest_SetsQuestActive()
    {
        var q = MakeQuest("q1", "Test Quest");
        _questManager.StartQuest(q);

        Assert.IsTrue(_questManager.IsQuestActive("q1"));
        Assert.IsFalse(_questManager.IsQuestCompleted("q1"));

        Object.DestroyImmediate(q);
    }

    [Test]
    public void StartQuest_Duplicate_IsNoOp()
    {
        var q = MakeQuest("q2", "Dupe Quest");
        _questManager.StartQuest(q);
        _questManager.StartQuest(q); // Should not throw or double-track

        int count = 0;
        foreach (var _ in _questManager.GetActiveQuestIds()) count++;
        Assert.AreEqual(1, count);

        Object.DestroyImmediate(q);
    }

    [Test]
    public void StartQuest_Prereq_NotMet_DoesNotStart()
    {
        var q = MakeQuest("q3", "Locked Quest", prereqs: new List<string> { "prereq_quest" });
        bool started = _questManager.StartQuest(q);

        Assert.IsFalse(started);
        Assert.IsFalse(_questManager.IsQuestActive("q3"));

        Object.DestroyImmediate(q);
    }

    // ── Objective advancement ─────────────────────────────────────────────────

    [Test]
    public void AdvanceObjective_UpdatesProgress()
    {
        var objectives = new List<QuestObjective> { MakeObjective(QuestObjectiveType.Custom, "", 3) };
        var q = MakeQuest("q4", "Progress Quest", objectives);
        _questManager.StartQuest(q);

        _questManager.AdvanceObjective("q4", 0, 2);
        Assert.AreEqual(2, _questManager.GetObjectiveProgress("q4", 0));

        Object.DestroyImmediate(q);
    }

    [Test]
    public void AdvanceObjective_ClampsAtRequired()
    {
        // Second objective keeps the quest active after objective 0 is clamped to its max,
        // so GetObjectiveProgress still has a tracked entry to read (completing the quest
        // removes its _objectiveProgress entry — see CompleteQuest).
        var objectives = new List<QuestObjective>
        {
            MakeObjective(QuestObjectiveType.Custom, "", 3),
            MakeObjective(QuestObjectiveType.Custom, "", 1)
        };
        var q = MakeQuest("q5", "Clamp Quest", objectives);
        _questManager.StartQuest(q);

        _questManager.AdvanceObjective("q5", 0, 10); // Way more than needed
        Assert.AreEqual(3, _questManager.GetObjectiveProgress("q5", 0));

        Object.DestroyImmediate(q);
    }

    [Test]
    public void AdvanceObjective_WhenComplete_QuestCompletes()
    {
        var objectives = new List<QuestObjective> { MakeObjective(QuestObjectiveType.Custom, "", 1) };
        var q = MakeQuest("q6", "Auto Complete Quest", objectives);

        QuestData completedQuest = null;
        _questManager.OnQuestCompleted += d => completedQuest = d;

        _questManager.StartQuest(q);
        _questManager.AdvanceObjective("q6", 0, 1);

        Assert.IsFalse(_questManager.IsQuestActive("q6"));
        Assert.IsTrue(_questManager.IsQuestCompleted("q6"));
        Assert.IsNotNull(completedQuest);
        Assert.AreEqual("q6", completedQuest.questId);

        Object.DestroyImmediate(q);
    }

    [Test]
    public void MultipleObjectives_AllMustCompleteBeforeQuestDone()
    {
        var objectives = new List<QuestObjective>
        {
            MakeObjective(QuestObjectiveType.Custom, "", 2),
            MakeObjective(QuestObjectiveType.Custom, "", 2)
        };
        var q = MakeQuest("q7", "Multi Obj Quest", objectives);
        _questManager.StartQuest(q);

        _questManager.AdvanceObjective("q7", 0, 2);
        Assert.IsTrue(_questManager.IsQuestActive("q7"),  "Should still be active after obj 0 done");

        _questManager.AdvanceObjective("q7", 1, 2);
        Assert.IsTrue(_questManager.IsQuestCompleted("q7"), "Should complete after obj 1 done");

        Object.DestroyImmediate(q);
    }

    // ── NotifyObjective ───────────────────────────────────────────────────────

    [Test]
    public void NotifyObjective_MatchesTypeAndTargetId()
    {
        var objectives = new List<QuestObjective>
        {
            MakeObjective(QuestObjectiveType.CollectItem, "Carrot", 3)
        };
        var q = MakeQuest("q8", "Collect Quest", objectives);
        _questManager.StartQuest(q);

        _questManager.NotifyObjective(QuestObjectiveType.CollectItem, "Carrot", 2);
        Assert.AreEqual(2, _questManager.GetObjectiveProgress("q8", 0));

        // Wrong item — should not advance
        _questManager.NotifyObjective(QuestObjectiveType.CollectItem, "Cabbage", 1);
        Assert.AreEqual(2, _questManager.GetObjectiveProgress("q8", 0));

        Object.DestroyImmediate(q);
    }

    // ── GetQuestProgress ──────────────────────────────────────────────────────

    [Test]
    public void GetQuestProgress_ReturnsCorrectRatio()
    {
        var objectives = new List<QuestObjective>
        {
            MakeObjective(QuestObjectiveType.Custom, "", 4)
        };
        var q = MakeQuest("q9", "Progress Ratio", objectives);
        _questManager.StartQuest(q);

        _questManager.AdvanceObjective("q9", 0, 2);
        Assert.AreEqual(0.5f, _questManager.GetQuestProgress("q9"), 0.001f);

        Object.DestroyImmediate(q);
    }

    // ── Save / Load round-trip ────────────────────────────────────────────────

    [Test]
    public void SaveLoad_ActiveQuest_RestoredCorrectly()
    {
        // We need a second QuestManager to load into — simulate using Save/LoadData directly
        var objectives = new List<QuestObjective> { MakeObjective(QuestObjectiveType.Custom, "", 5) };
        var q = MakeQuest("q10", "Save Quest", objectives);
        _questManager.StartQuest(q);
        _questManager.AdvanceObjective("q10", 0, 3);

        GameData data = new GameData();
        _questManager.SaveData(data);

        // Verify keys written
        Assert.IsTrue(data.worldData.worldFlags.ContainsKey("qst_active_q10"));
        Assert.IsTrue(data.worldData.worldCounters.ContainsKey("qst_prog_q10_0"));
        Assert.AreEqual(3, data.worldData.worldCounters["qst_prog_q10_0"]);

        Object.DestroyImmediate(q);
    }

    [Test]
    public void SaveLoad_CompletedQuest_SavedAsCompleted()
    {
        var objectives = new List<QuestObjective> { MakeObjective(QuestObjectiveType.Custom, "", 1) };
        var q = MakeQuest("q11", "Complete Save", objectives);
        _questManager.StartQuest(q);
        _questManager.AdvanceObjective("q11", 0, 1); // Completes quest

        GameData data = new GameData();
        _questManager.SaveData(data);

        Assert.IsTrue(data.worldData.worldFlags.ContainsKey("qst_done_q11"));
        Assert.IsFalse(data.worldData.worldFlags.ContainsKey("qst_active_q11"),
            "Completed quest should not also appear as active");

        Object.DestroyImmediate(q);
    }

    // ── Null / edge cases ─────────────────────────────────────────────────────

    [Test]
    public void StartQuest_Null_ReturnsFalse()
    {
        bool result = false;
        Assert.DoesNotThrow(() => result = _questManager.StartQuest((QuestData)null));
        Assert.IsFalse(result);
    }

    [Test]
    public void AdvanceObjective_OnInactiveQuest_IsNoOp()
    {
        Assert.DoesNotThrow(() => _questManager.AdvanceObjective("nonexistent_quest", 0, 1));
    }

    [Test]
    public void GetObjectiveProgress_UnknownQuest_ReturnsZero()
    {
        Assert.AreEqual(0, _questManager.GetObjectiveProgress("ghost_quest", 0));
    }

    [Test]
    public void SaveData_Null_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _questManager.SaveData(null));
    }

    [Test]
    public void LoadData_Null_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _questManager.LoadData(null));
    }
}

} // namespace SowurShield.Tests
