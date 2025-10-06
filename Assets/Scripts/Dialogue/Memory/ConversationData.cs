using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ConversationData
{
    [Header("Save Metadata")]
    public string saveVersion = "1.0.0";
    public string saveTimestamp;
    public float totalPlayTime = 0f;
    
    [Header("Conversation Progress")]
    public List<string> completedConversations = new List<string>();
    public Dictionary<string, string> lastNodeReached = new Dictionary<string, string>();
    
    [Header("Choice History")]
    public List<ChoiceRecord> choiceHistory = new List<ChoiceRecord>();
    
    [Header("Relationships")]
    public Dictionary<string, float> relationshipLevels = new Dictionary<string, float>();
    
    [Header("Quest Status")]
    public Dictionary<string, string> questStatuses = new Dictionary<string, string>();
    
    [Header("Custom Variables")]
    public Dictionary<string, string> customVariables = new Dictionary<string, string>();
    
    [Header("Inventory Tracking")]
    public Dictionary<string, int> trackedItems = new Dictionary<string, int>();
    
    public ConversationData()
    {
        saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    /// <summary>
    /// Records that a conversation was completed
    /// </summary>
    public void CompleteConversation(string conversationId)
    {
        if (!string.IsNullOrEmpty(conversationId) && !completedConversations.Contains(conversationId))
        {
            completedConversations.Add(conversationId);
        }
    }
    
    /// <summary>
    /// Checks if a conversation was completed
    /// </summary>
    public bool HasCompletedConversation(string conversationId)
    {
        return !string.IsNullOrEmpty(conversationId) && completedConversations.Contains(conversationId);
    }
    
    /// <summary>
    /// Records the last node reached in a conversation
    /// </summary>
    public void SetLastNodeReached(string conversationId, string nodeId)
    {
        if (!string.IsNullOrEmpty(conversationId) && !string.IsNullOrEmpty(nodeId))
        {
            lastNodeReached[conversationId] = nodeId;
        }
    }
    
    /// <summary>
    /// Gets the last node reached in a conversation
    /// </summary>
    public string GetLastNodeReached(string conversationId)
    {
        return lastNodeReached.TryGetValue(conversationId ?? "", out string nodeId) ? nodeId : null;
    }
    
    /// <summary>
    /// Records a choice made by the player
    /// </summary>
    public void RecordChoice(string conversationId, string nodeId, string choiceText, string nextNodeId)
    {
        var choice = new ChoiceRecord
        {
            conversationId = conversationId,
            nodeId = nodeId,
            choiceText = choiceText,
            nextNodeId = nextNodeId,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        choiceHistory.Add(choice);
    }
    
    /// <summary>
    /// Checks if a specific choice was made
    /// </summary>
    public bool HasMadeChoice(string conversationId, string choiceText)
    {
        foreach (var choice in choiceHistory)
        {
            if (choice.conversationId == conversationId && 
                string.Equals(choice.choiceText, choiceText, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Gets all choices made in a specific conversation
    /// </summary>
    public List<ChoiceRecord> GetChoicesForConversation(string conversationId)
    {
        var result = new List<ChoiceRecord>();
        foreach (var choice in choiceHistory)
        {
            if (choice.conversationId == conversationId)
                result.Add(choice);
        }
        return result;
    }
    
    /// <summary>
    /// Sets relationship level with an NPC
    /// </summary>
    public void SetRelationshipLevel(string npcId, float level)
    {
        if (!string.IsNullOrEmpty(npcId))
        {
            relationshipLevels[npcId] = Mathf.Clamp(level, -100f, 100f);
        }
    }
    
    /// <summary>
    /// Modifies relationship level with an NPC
    /// </summary>
    public void ModifyRelationshipLevel(string npcId, float change)
    {
        if (!string.IsNullOrEmpty(npcId))
        {
            float currentLevel = GetRelationshipLevel(npcId);
            SetRelationshipLevel(npcId, currentLevel + change);
        }
    }
    
    /// <summary>
    /// Gets relationship level with an NPC
    /// </summary>
    public float GetRelationshipLevel(string npcId)
    {
        return relationshipLevels.TryGetValue(npcId ?? "", out float level) ? level : 0f;
    }
    
    /// <summary>
    /// Sets quest status
    /// </summary>
    public void SetQuestStatus(string questId, string status)
    {
        if (!string.IsNullOrEmpty(questId))
        {
            questStatuses[questId] = status ?? "";
        }
    }
    
    /// <summary>
    /// Gets quest status
    /// </summary>
    public string GetQuestStatus(string questId)
    {
        return questStatuses.TryGetValue(questId ?? "", out string status) ? status : "";
    }
    
    /// <summary>
    /// Sets a custom variable
    /// </summary>
    public void SetVariable(string key, string value)
    {
        if (!string.IsNullOrEmpty(key))
        {
            customVariables[key] = value ?? "";
        }
    }
    
    /// <summary>
    /// Gets a custom variable
    /// </summary>
    public string GetVariable(string key)
    {
        return customVariables.TryGetValue(key ?? "", out string value) ? value : "";
    }
    
    /// <summary>
    /// Sets tracked item count
    /// </summary>
    public void SetItemCount(string itemId, int count)
    {
        if (!string.IsNullOrEmpty(itemId))
        {
            trackedItems[itemId] = Mathf.Max(0, count);
        }
    }
    
    /// <summary>
    /// Modifies tracked item count
    /// </summary>
    public void ModifyItemCount(string itemId, int change)
    {
        if (!string.IsNullOrEmpty(itemId))
        {
            int currentCount = GetItemCount(itemId);
            SetItemCount(itemId, currentCount + change);
        }
    }
    
    /// <summary>
    /// Gets tracked item count
    /// </summary>
    public int GetItemCount(string itemId)
    {
        return trackedItems.TryGetValue(itemId ?? "", out int count) ? count : 0;
    }
}

[System.Serializable]
public class ChoiceRecord
{
    public string conversationId;
    public string nodeId;
    public string choiceText;
    public string nextNodeId;
    public string timestamp;
}