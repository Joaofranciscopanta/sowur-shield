using UnityEngine;
using System.Collections.Generic;
using SowurShield.Animals;
using SowurShield.Combat;
using SowurShield.Dialogue;

namespace SowurShield.Core
{

/// <summary>
/// Singleton that owns achievement unlock state. Loads all AchievementData from
/// Resources/Achievements, subscribes to the game's global unlock-condition events
/// (quest completion, crop harvest, animal purchase, stage clear, item sale), and fires
/// OnAchievementUnlocked for UI (e.g. a Steam-style corner notification) to react to.
/// Persists via ISaveable into ProgressGameData.achievementsUnlocked.
/// </summary>
public class AchievementManager : MonoBehaviour, ISaveable
{
    public static AchievementManager Instance { get; private set; }

    /// <summary>Fired once, the moment an achievement is newly unlocked (never re-fires for an already-unlocked one).</summary>
    public System.Action<AchievementData> OnAchievementUnlocked;

    private readonly Dictionary<string, AchievementData> _allAchievements = new Dictionary<string, AchievementData>();
    private readonly HashSet<string> _unlockedIds = new HashSet<string>();

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllAchievementAssets();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (Instance != this) return;

        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);

        SubscribeToHooks();

        // QuestManager/AnimalRoster may not have initialized their singleton yet this frame
        // (Awake/Start ordering across GameObjects isn't guaranteed) — retry once next frame.
        if (QuestManager.Instance == null || AnimalRoster.Instance == null)
            StartCoroutine(RetrySubscriptionsNextFrame());
    }

    private System.Collections.IEnumerator RetrySubscriptionsNextFrame()
    {
        yield return null;
        SubscribeToHooks(); // safe to call twice — events are unsubscribed first
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.UnregisterSaveable(this);

        if (Instance == this)
        {
            UnsubscribeFromHooks();
            Instance = null;
        }
    }

    private void LoadAllAchievementAssets()
    {
        AchievementData[] all = Resources.LoadAll<AchievementData>("Achievements");
        foreach (var a in all)
        {
            if (!string.IsNullOrEmpty(a.achievementId))
                _allAchievements[a.achievementId] = a;
        }
    }

    // =========================================================================
    // Hook subscriptions
    // =========================================================================

    private void SubscribeToHooks()
    {
        // Unsubscribe first so this is safe to call more than once (e.g. the next-frame
        // retry for singletons that weren't ready yet) without double-firing handlers.
        UnsubscribeFromHooks();

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;

        CropGrowthManager.OnAnyCropHarvested += OnCropHarvested;
        StageManager.OnStageCompleted += OnStageCompleted;
        SellBox.OnAnyItemsSold += OnItemsSold;

        if (AnimalRoster.Instance != null)
            AnimalRoster.Instance.OnAnimalRegistered += OnAnimalRegistered;
    }

    private void UnsubscribeFromHooks()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;

        CropGrowthManager.OnAnyCropHarvested -= OnCropHarvested;
        StageManager.OnStageCompleted -= OnStageCompleted;
        SellBox.OnAnyItemsSold -= OnItemsSold;

        if (AnimalRoster.Instance != null)
            AnimalRoster.Instance.OnAnimalRegistered -= OnAnimalRegistered;
    }

    private void OnQuestCompleted(QuestData quest)
    {
        UnlockMatching(AchievementTriggerType.QuestCompleted, quest.questId);
    }

    private void OnCropHarvested(CropGrowthManager manager)
    {
        UnlockMatching(AchievementTriggerType.CropHarvested, null);
    }

    private void OnAnimalRegistered(Animal animal)
    {
        UnlockMatching(AchievementTriggerType.AnimalPurchased, null);
    }

    private void OnStageCompleted(StageData stage)
    {
        UnlockMatching(AchievementTriggerType.StageCompleted, stage.stageName);
    }

    private void OnItemsSold(int totalEarnings)
    {
        UnlockMatching(AchievementTriggerType.ItemSold, null);
    }

    /// <summary>
    /// Unlocks every not-yet-unlocked achievement of the given trigger type whose targetId
    /// is blank (matches anything) or equals matchId (case-insensitive).
    /// </summary>
    private void UnlockMatching(AchievementTriggerType triggerType, string matchId)
    {
        foreach (var achievement in _allAchievements.Values)
        {
            if (achievement.triggerType != triggerType) continue;
            if (_unlockedIds.Contains(achievement.achievementId)) continue;

            bool targetsAnything = string.IsNullOrEmpty(achievement.targetId);
            bool matches = targetsAnything ||
                string.Equals(achievement.targetId, matchId, System.StringComparison.OrdinalIgnoreCase);
            if (!matches) continue;

            Unlock(achievement);
        }
    }

    private void Unlock(AchievementData achievement)
    {
        if (achievement == null || _unlockedIds.Contains(achievement.achievementId)) return;

        _unlockedIds.Add(achievement.achievementId);
        OnAchievementUnlocked?.Invoke(achievement);
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public bool IsUnlocked(string achievementId) => _unlockedIds.Contains(achievementId);
    public IEnumerable<string> GetUnlockedIds() => _unlockedIds;
    public AchievementData GetAchievementData(string achievementId) =>
        _allAchievements.TryGetValue(achievementId, out var a) ? a : null;

    // =========================================================================
    // ISaveable
    // =========================================================================

    public void SaveData(GameData gameData)
    {
        if (gameData?.progressData == null) return;

        foreach (string id in _allAchievements.Keys)
            gameData.progressData.achievementsUnlocked[id] = _unlockedIds.Contains(id);
    }

    public void LoadData(GameData gameData)
    {
        if (gameData?.progressData == null) return;

        _unlockedIds.Clear();
        foreach (var kv in gameData.progressData.achievementsUnlocked)
        {
            if (kv.Value)
                _unlockedIds.Add(kv.Key);
        }
    }
}

} // namespace SowurShield.Core
