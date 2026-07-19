using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace SowurShield.Dialogue
{

/// <summary>
/// ScriptableObject defining a quest: objectives, rewards, and prerequisites.
/// Quests are started/tracked by QuestManager; status persists via SaveManager ISaveable.
///
/// CREATE: Assets > Create > SowurShield > Quest Data
/// </summary>
[CreateAssetMenu(menuName = "SowurShield/Quest Data", fileName = "NewQuest")]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID used as save key — never change after shipping.")]
    public string questId;
    public LocalizedString questTitle; // table "Dialogue", key "quest.<questId>.title"
    public LocalizedString questDescription; // table "Dialogue", key "quest.<questId>.description"

    [Header("Objectives")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    [Header("Prerequisites")]
    [Tooltip("These quest IDs must be completed before this quest can be offered.")]
    public List<string> prerequisiteQuestIds = new List<string>();

    [Header("Rewards")]
    public int rewardGold = 0;
    public List<QuestItemReward> rewardItems = new List<QuestItemReward>();
    public List<QuestRelationshipReward> rewardRelationships = new List<QuestRelationshipReward>();
}

[System.Serializable]
public class QuestObjective
{
    public LocalizedString description; // table "Dialogue", key "quest.<questId>.objective<N>"
    public QuestObjectiveType type;
    [Tooltip("Match key. CollectItem→item name; TalkToNPC→conversation id (DialogueTree.conversationId); " +
             "HarvestCrop→crop name (CropData.cropName); CompleteBattle→stage name. " +
             "Leave blank to match any target of this type.")]
    public string targetId;
    [Min(1)]
    public int requiredCount = 1;
}

[System.Serializable]
public class QuestItemReward
{
    public string itemName;
    [Min(1)]
    public int quantity = 1;
}

[System.Serializable]
public class QuestRelationshipReward
{
    public string npcId;
    public float amount = 5f;
}

public enum QuestObjectiveType
{
    CollectItem,     // Auto: Inventory.AddItem → count of targetId reaches requiredCount
    TalkToNPC,       // Auto: ConversationMemory.CompleteConversation(targetId)
    HarvestCrop,     // Auto: CropGrowthManager.HarvestCrop of crop named targetId
    CompleteBattle,  // Auto: StageManager.CompleteStage of stage named targetId
    Custom           // Manually advanced via QuestManager.AdvanceObjective
}

} // namespace SowurShield.Dialogue
