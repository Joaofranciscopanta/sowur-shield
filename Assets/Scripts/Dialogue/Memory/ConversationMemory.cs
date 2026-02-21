using UnityEngine;
using System.IO;

public class ConversationMemory : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "ConversationData";
    [SerializeField] private string saveFileExtension = ".json";
    [SerializeField] private bool autoSaveOnChanges = true;
    [SerializeField] private float autoSaveInterval = 30f; // Save every 30 seconds
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Singleton instance
    public static ConversationMemory Instance { get; private set; }
    
    // Events
    public System.Action<string> OnConversationCompleted;
    public System.Action<string, string, string, string> OnChoiceMade; // conversationId, nodeId, choiceText, nextNodeId
    public System.Action<string, float> OnRelationshipChanged; // npcId, newLevel
    public System.Action<bool> OnSaveCompleted; // success
    
    // Data
    private ConversationData conversationData;
    private string saveFilePath;
    private float lastAutoSaveTime;
    private bool hasUnsavedChanges = false;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMemorySystem();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Handle auto-save
        if (autoSaveOnChanges && hasUnsavedChanges && 
            Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            SaveData();
        }
    }
    
    private void InitializeMemorySystem()
    {
        // Setup save path
        string saveDirectory = Path.Combine(Application.persistentDataPath, "DialogueData");
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        
        saveFilePath = Path.Combine(saveDirectory, saveFileName + saveFileExtension);
        
        // Load existing data or create new
        LoadData();
    }
    
    private void LoadData()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string jsonData = File.ReadAllText(saveFilePath);
                conversationData = JsonUtility.FromJson<ConversationData>(jsonData);
                
            }
            else
            {
                conversationData = new ConversationData();
            }
        }
        catch (System.Exception e)
        {
            conversationData = new ConversationData();
        }
        
        hasUnsavedChanges = false;
    }
    
    private void SaveData()
    {
        try
        {
            conversationData.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string jsonData = JsonUtility.ToJson(conversationData, true);
            File.WriteAllText(saveFilePath, jsonData);
            
            hasUnsavedChanges = false;
            lastAutoSaveTime = Time.time;
            
                
            OnSaveCompleted?.Invoke(true);
        }
        catch (System.Exception e)
        {
            OnSaveCompleted?.Invoke(false);
        }
    }
    
    // Public API methods
    
    /// <summary>
    /// Marks a conversation as completed
    /// </summary>
    public void CompleteConversation(string conversationId)
    {
        if (conversationData == null) return;
        
        conversationData.CompleteConversation(conversationId);
        MarkDataChanged();

        OnConversationCompleted?.Invoke(conversationId);
    }
    
    /// <summary>
    /// Checks if a conversation was completed
    /// </summary>
    public bool HasCompletedConversation(string conversationId)
    {
        return conversationData?.HasCompletedConversation(conversationId) ?? false;
    }
    
    /// <summary>
    /// Records a choice made by the player
    /// </summary>
    public void RecordChoice(string conversationId, string nodeId, string choiceText, string nextNodeId)
    {
        if (conversationData == null) return;
        
        conversationData.RecordChoice(conversationId, nodeId, choiceText, nextNodeId);
        MarkDataChanged();
        
        OnChoiceMade?.Invoke(conversationId, nodeId, choiceText, nextNodeId);
        
    }
    
    /// <summary>
    /// Checks if a specific choice was made
    /// </summary>
    public bool HasMadeChoice(string conversationId, string choiceText)
    {
        return conversationData?.HasMadeChoice(conversationId, choiceText) ?? false;
    }
    
    /// <summary>
    /// Sets relationship level with an NPC
    /// </summary>
    public void SetRelationship(string npcId, float level)
    {
        if (conversationData == null) return;
        
        float oldLevel = conversationData.GetRelationshipLevel(npcId);
        conversationData.SetRelationshipLevel(npcId, level);
        MarkDataChanged();
        
        OnRelationshipChanged?.Invoke(npcId, level);
        
    }
    
    /// <summary>
    /// Modifies relationship level with an NPC
    /// </summary>
    public void ModifyRelationship(string npcId, float change)
    {
        if (conversationData == null) return;
        
        float currentLevel = conversationData.GetRelationshipLevel(npcId);
        SetRelationship(npcId, currentLevel + change);
    }
    
    /// <summary>
    /// Gets relationship level with an NPC
    /// </summary>
    public float GetRelationshipLevel(string npcId)
    {
        return conversationData?.GetRelationshipLevel(npcId) ?? 0f;
    }
    
    /// <summary>
    /// Sets quest status
    /// </summary>
    public void SetQuestStatus(string questId, string status)
    {
        if (conversationData == null) return;
        
        conversationData.SetQuestStatus(questId, status);
        MarkDataChanged();
        
    }
    
    /// <summary>
    /// Gets quest status
    /// </summary>
    public string GetQuestStatus(string questId)
    {
        return conversationData?.GetQuestStatus(questId) ?? "";
    }
    
    /// <summary>
    /// Sets a custom variable
    /// </summary>
    public void SetVariable(string key, string value)
    {
        if (conversationData == null) return;
        
        conversationData.SetVariable(key, value);
        MarkDataChanged();
        
    }
    
    /// <summary>
    /// Gets a custom variable
    /// </summary>
    public string GetVariable(string key)
    {
        return conversationData?.GetVariable(key) ?? "";
    }
    
    /// <summary>
    /// Gives items to player (tracked in memory)
    /// </summary>
    public void GiveItem(string itemId, int count)
    {
        if (conversationData == null) return;
        
        conversationData.ModifyItemCount(itemId, count);
        MarkDataChanged();
        
    }
    
    /// <summary>
    /// Takes items from player (tracked in memory)
    /// </summary>
    public void TakeItem(string itemId, int count)
    {
        if (conversationData == null) return;
        
        conversationData.ModifyItemCount(itemId, -count);
        MarkDataChanged();
        
    }
    
    /// <summary>
    /// Gets tracked item count
    /// </summary>
    public int GetInventoryItemCount(string itemId)
    {
        return conversationData?.GetItemCount(itemId) ?? 0;
    }
    
    /// <summary>
    /// Records last node reached in a conversation
    /// </summary>
    public void SetLastNodeReached(string conversationId, string nodeId)
    {
        if (conversationData == null) return;
        
        conversationData.SetLastNodeReached(conversationId, nodeId);
        MarkDataChanged();
    }
    
    /// <summary>
    /// Gets last node reached in a conversation
    /// </summary>
    public string GetLastNodeReached(string conversationId)
    {
        return conversationData?.GetLastNodeReached(conversationId);
    }
    
    /// <summary>
    /// Manually saves data
    /// </summary>
    public void ForceSave()
    {
        SaveData();
    }
    
    /// <summary>
    /// Clears all conversation data (for testing/debugging)
    /// </summary>
    public void ClearAllData()
    {
        conversationData = new ConversationData();
        MarkDataChanged();
        SaveData();
        
    }
    
    private void MarkDataChanged()
    {
        hasUnsavedChanges = true;
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && hasUnsavedChanges)
        {
            SaveData();
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && hasUnsavedChanges)
        {
            SaveData();
        }
    }
    
    private void OnDestroy()
    {
        if (hasUnsavedChanges)
        {
            SaveData();
        }
    }
}