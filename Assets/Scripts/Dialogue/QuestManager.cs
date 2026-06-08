using UnityEngine;
using System.Collections.Generic;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Dialogue
{

/// <summary>
/// Singleton that owns the runtime quest state.
/// - Starts, tracks, and completes quests.
/// - Grants rewards via PlayerStats + Inventory.
/// - Persists to GameData via ISaveable (worldFlags + worldCounters).
/// - Fires events that QuestTrackerUI and dialogue conditions subscribe to.
///
/// Quest status strings written to ConversationMemory for dialogue conditions:
///   "active", "completed", "" (not started).
/// </summary>
public class QuestManager : MonoBehaviour, ISaveable
{
    public static QuestManager Instance { get; private set; }

    // Runtime state
    private readonly Dictionary<string, int[]> _objectiveProgress = new Dictionary<string, int[]>();
    private readonly HashSet<string> _activeQuestIds   = new HashSet<string>();
    private readonly HashSet<string> _completedQuestIds = new HashSet<string>();

    // Lookup: loaded QuestData ScriptableObjects by id
    private readonly Dictionary<string, QuestData> _allQuests = new Dictionary<string, QuestData>();

    // Events
    public System.Action<QuestData>              OnQuestStarted;
    public System.Action<QuestData, int, int>    OnObjectiveUpdated;  // quest, objIndex, newCount
    public System.Action<QuestData>              OnQuestCompleted;

    // Save key prefixes
    private const string ACTIVE_PREFIX    = "qst_active_";
    private const string DONE_PREFIX      = "qst_done_";
    private const string PROG_PREFIX      = "qst_prog_";  // PROG_{questId}_{objIndex}

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllQuestAssets();

            if (SaveManager.Instance != null)
                SaveManager.Instance.RegisterSaveable(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (Instance == this && SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.UnregisterSaveable(this);
    }

    private void LoadAllQuestAssets()
    {
        QuestData[] all = Resources.LoadAll<QuestData>("Quests");
        foreach (var q in all)
        {
            if (!string.IsNullOrEmpty(q.questId))
                _allQuests[q.questId] = q;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Start a quest. No-ops if already active or completed.</summary>
    public bool StartQuest(string questId)
    {
        if (!_allQuests.TryGetValue(questId, out QuestData data))
        {
            Debug.LogWarning($"[QuestManager] StartQuest: unknown questId '{questId}'.");
            return false;
        }
        return StartQuest(data);
    }

    public bool StartQuest(QuestData data)
    {
        if (data == null || string.IsNullOrEmpty(data.questId)) return false;
        if (_activeQuestIds.Contains(data.questId) || _completedQuestIds.Contains(data.questId))
            return false;

        // Check prerequisites
        if (data.prerequisiteQuestIds != null)
        {
            foreach (string prereq in data.prerequisiteQuestIds)
            {
                if (!_completedQuestIds.Contains(prereq))
                {
                    Debug.LogWarning($"[QuestManager] Cannot start '{data.questId}' — prereq '{prereq}' not done.");
                    return false;
                }
            }
        }

        _activeQuestIds.Add(data.questId);
        _objectiveProgress[data.questId] = new int[data.objectives?.Count ?? 0];

        // Inform ConversationMemory so dialogue conditions work
        ConversationMemory.Instance?.SetQuestStatus(data.questId, "active");

        OnQuestStarted?.Invoke(data);
        return true;
    }

    /// <summary>
    /// Advance a CollectItem or Custom objective.
    /// Pass objectiveIndex=-1 to auto-detect first incomplete matching objective of that type.
    /// </summary>
    public void AdvanceObjective(string questId, int objectiveIndex, int amount = 1)
    {
        if (!_activeQuestIds.Contains(questId)) return;
        if (!_allQuests.TryGetValue(questId, out QuestData data)) return;
        if (!_objectiveProgress.TryGetValue(questId, out int[] prog)) return;
        if (objectiveIndex < 0 || objectiveIndex >= prog.Length) return;

        prog[objectiveIndex] = Mathf.Min(prog[objectiveIndex] + amount,
                                          data.objectives[objectiveIndex].requiredCount);

        OnObjectiveUpdated?.Invoke(data, objectiveIndex, prog[objectiveIndex]);

        CheckQuestCompletion(data, prog);
    }

    /// <summary>Advance all matching objectives of type + targetId across all active quests.</summary>
    public void NotifyObjective(QuestObjectiveType type, string targetId, int amount = 1)
    {
        // Snapshot — CheckQuestCompletion may remove from _activeQuestIds mid-loop.
        foreach (string qId in new List<string>(_activeQuestIds))
        {
            if (!_allQuests.TryGetValue(qId, out QuestData data)) continue;
            if (!_objectiveProgress.TryGetValue(qId, out int[] prog)) continue;

            for (int i = 0; i < data.objectives.Count; i++)
            {
                var obj = data.objectives[i];
                if (obj.type != type) continue;
                if (!string.IsNullOrEmpty(obj.targetId) &&
                    !string.Equals(obj.targetId, targetId, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (prog[i] >= obj.requiredCount) continue;

                prog[i] = Mathf.Min(prog[i] + amount, obj.requiredCount);
                OnObjectiveUpdated?.Invoke(data, i, prog[i]);
            }

            CheckQuestCompletion(data, prog);
        }
    }

    public bool IsQuestActive(string questId)     => _activeQuestIds.Contains(questId);
    public bool IsQuestCompleted(string questId)  => _completedQuestIds.Contains(questId);

    /// <summary>Returns 0–1 completion ratio across all objectives, or 0 if quest not active.</summary>
    public float GetQuestProgress(string questId)
    {
        if (!_activeQuestIds.Contains(questId)) return 0f;
        if (!_allQuests.TryGetValue(questId, out QuestData data)) return 0f;
        if (!_objectiveProgress.TryGetValue(questId, out int[] prog)) return 0f;
        if (data.objectives == null || data.objectives.Count == 0) return 1f;

        int total = 0, done = 0;
        for (int i = 0; i < data.objectives.Count; i++)
        {
            total += data.objectives[i].requiredCount;
            done  += prog[i];
        }
        return total > 0 ? (float)done / total : 1f;
    }

    public int GetObjectiveProgress(string questId, int objIndex)
    {
        if (!_objectiveProgress.TryGetValue(questId, out int[] prog)) return 0;
        if (objIndex < 0 || objIndex >= prog.Length) return 0;
        return prog[objIndex];
    }

    public QuestData GetQuestData(string questId) =>
        _allQuests.TryGetValue(questId, out var d) ? d : null;

    public IEnumerable<string> GetActiveQuestIds()    => _activeQuestIds;
    public IEnumerable<string> GetCompletedQuestIds() => _completedQuestIds;

    // =========================================================================
    // CollectItem Auto-Check
    // =========================================================================

    /// <summary>
    /// Call this after an item is added to inventory to auto-advance CollectItem objectives.
    /// Pass the current count the player has of this item.
    /// </summary>
    public void OnInventoryItemCountChanged(string itemName, int newCount)
    {
        // Snapshot — CheckQuestCompletion may remove from _activeQuestIds mid-loop.
        foreach (string qId in new List<string>(_activeQuestIds))
        {
            if (!_allQuests.TryGetValue(qId, out QuestData data)) continue;
            if (!_objectiveProgress.TryGetValue(qId, out int[] prog)) continue;

            for (int i = 0; i < data.objectives.Count; i++)
            {
                var obj = data.objectives[i];
                if (obj.type != QuestObjectiveType.CollectItem) continue;
                if (!string.Equals(obj.targetId, itemName, System.StringComparison.OrdinalIgnoreCase)) continue;

                int clamped = Mathf.Min(newCount, obj.requiredCount);
                if (clamped == prog[i]) continue;
                prog[i] = clamped;
                OnObjectiveUpdated?.Invoke(data, i, prog[i]);
            }

            CheckQuestCompletion(data, prog);
        }
    }

    // =========================================================================
    // Completion & Rewards
    // =========================================================================

    private void CheckQuestCompletion(QuestData data, int[] prog)
    {
        if (data.objectives == null || data.objectives.Count == 0) return;
        for (int i = 0; i < data.objectives.Count; i++)
        {
            if (prog[i] < data.objectives[i].requiredCount)
                return; // Not all done
        }
        CompleteQuest(data);
    }

    private void CompleteQuest(QuestData data)
    {
        if (!_activeQuestIds.Contains(data.questId)) return;

        _activeQuestIds.Remove(data.questId);
        _completedQuestIds.Add(data.questId);
        _objectiveProgress.Remove(data.questId);

        ConversationMemory.Instance?.SetQuestStatus(data.questId, "completed");

        GrantRewards(data);
        OnQuestCompleted?.Invoke(data);
    }

    private void GrantRewards(QuestData data)
    {
        // Gold
        if (data.rewardGold > 0)
        {
            PlayerStats stats = Object.FindFirstObjectByType<PlayerStats>();
            stats?.AddMoney(data.rewardGold);
        }

        // Items
        if (data.rewardItems != null && data.rewardItems.Count > 0)
        {
            Inventory inv = Object.FindFirstObjectByType<Inventory>();
            if (inv != null)
            {
                foreach (var reward in data.rewardItems)
                {
                    Item item = ItemDatabase.GetItem(reward.itemName);
                    if (item != null)
                        inv.AddItem(item, reward.quantity);
                    else
                        Debug.LogWarning($"[QuestManager] Reward item '{reward.itemName}' not found in ItemDatabase.");
                }
            }
        }

        // Relationships
        if (data.rewardRelationships != null)
        {
            foreach (var rel in data.rewardRelationships)
                ConversationMemory.Instance?.ModifyRelationship(rel.npcId, rel.amount);
        }
    }

    // =========================================================================
    // ISaveable
    // =========================================================================

    public void SaveData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        var flags    = gameData.worldData.worldFlags;
        var counters = gameData.worldData.worldCounters;

        foreach (string id in _activeQuestIds)
            flags[ACTIVE_PREFIX + id] = true;

        foreach (string id in _completedQuestIds)
        {
            flags[DONE_PREFIX + id] = true;
            flags.Remove(ACTIVE_PREFIX + id); // Cleanup: done trumps active
        }

        foreach (var kv in _objectiveProgress)
        {
            for (int i = 0; i < kv.Value.Length; i++)
                counters[PROG_PREFIX + kv.Key + "_" + i] = kv.Value[i];
        }
    }

    public void LoadData(GameData gameData)
    {
        if (gameData?.worldData == null) return;

        _activeQuestIds.Clear();
        _completedQuestIds.Clear();
        _objectiveProgress.Clear();

        var flags    = gameData.worldData.worldFlags;
        var counters = gameData.worldData.worldCounters;

        // Restore completed first
        foreach (var kv in flags)
        {
            if (!kv.Value) continue;

            if (kv.Key.StartsWith(DONE_PREFIX))
                _completedQuestIds.Add(kv.Key.Substring(DONE_PREFIX.Length));
            else if (kv.Key.StartsWith(ACTIVE_PREFIX))
            {
                string id = kv.Key.Substring(ACTIVE_PREFIX.Length);
                if (!_completedQuestIds.Contains(id))
                    _activeQuestIds.Add(id);
            }
        }

        // Restore objective progress for active quests
        foreach (string id in _activeQuestIds)
        {
            if (!_allQuests.TryGetValue(id, out QuestData data)) continue;
            int objCount = data.objectives?.Count ?? 0;
            int[] prog = new int[objCount];
            for (int i = 0; i < objCount; i++)
            {
                string key = PROG_PREFIX + id + "_" + i;
                if (counters.TryGetValue(key, out int val))
                    prog[i] = val;
            }
            _objectiveProgress[id] = prog;
        }
    }
}

} // namespace SowurShield.Dialogue
