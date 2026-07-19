using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// ScriptableObject defining a single achievement: what unlocks it, and what to show in
/// the Steam-style unlock notification. AchievementManager owns the runtime unlock state.
///
/// CREATE: Assets > Create > SowurShield > Achievement Data
/// </summary>
[CreateAssetMenu(menuName = "SowurShield/Achievement Data", fileName = "NewAchievement")]
public class AchievementData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID used as save key — never change after shipping.")]
    public string achievementId;
    public string title = "New Achievement";
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Unlock Condition")]
    public AchievementTriggerType triggerType;
    [Tooltip("Match key. QuestCompleted→QuestData.questId; StageCompleted→StageData.stageName. " +
             "Leave blank for CropHarvested/AnimalPurchased/ItemSold, which trigger on ANY occurrence.")]
    public string targetId;
}

public enum AchievementTriggerType
{
    QuestCompleted,     // Fires when QuestManager.OnQuestCompleted matches targetId (questId)
    CropHarvested,      // Fires on CropGrowthManager.OnAnyCropHarvested (any crop)
    AnimalPurchased,     // Fires on AnimalRoster.OnAnimalRegistered (any animal)
    StageCompleted,     // Fires when StageManager.OnStageCompleted matches targetId (stageName)
    ItemSold            // Fires on SellBox.OnAnyItemsSold (any sale)
}

} // namespace SowurShield.Core
