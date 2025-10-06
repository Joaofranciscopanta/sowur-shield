using UnityEngine;

/// <summary>
/// Manages saving and loading of player-specific data like position, stats, and progress
/// </summary>
public class PlayerDataManager : MonoBehaviour, ISaveable
{
    [Header("Components")]
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private PlayerStats playerStats;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();
            
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
            
        // Register with SaveManager (before SaveManager.Start() runs)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
            #if UNITY_EDITOR
            LogDebug("PlayerDataManager registered with SaveManager in Awake");
            #endif
        }
    }
    
    private void OnDestroy()
    {
        // Unregister from SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }
    
    // ============================================================================
    // ISAVEABLE IMPLEMENTATION
    // ============================================================================
    
    public void SaveData(GameData gameData)
    {
        // Save player position
        gameData.playerData.position = transform.position;
        gameData.playerData.rotation = transform.eulerAngles.z;
        gameData.playerData.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Save player stats if available
        if (playerStats != null)
        {
            gameData.playerData.health = playerStats.CurrentHealth;
            gameData.playerData.maxHealth = playerStats.MaxHealth;
            gameData.playerData.energy = playerStats.CurrentEnergy;
            gameData.playerData.maxEnergy = playerStats.MaxEnergy;
            gameData.playerData.money = playerStats.Money;
            gameData.playerData.playerLevel = playerStats.PlayerLevel;
            gameData.playerData.experience = playerStats.Experience;
            gameData.playerData.experienceToNextLevel = playerStats.ExperienceToNextLevel;
        }
        else
        {
            // Default values if PlayerStats component is missing
        }
    }
    
    public void LoadData(GameData gameData)
    {
        // Load player position - spawn at bed if they've slept in one
        Vector3 spawnPosition = gameData.playerData.position;

        if (gameData.playerData.hasSleptInBed && gameData.playerData.lastBedPosition != Vector3.zero)
        {
            spawnPosition = gameData.playerData.lastBedPosition;
        }

        transform.position = spawnPosition;
        transform.rotation = Quaternion.Euler(0, 0, gameData.playerData.rotation);
        
        // Load player stats if available
        if (playerStats != null)
        {
            playerStats.SetHealth(gameData.playerData.health);
            playerStats.SetMaxHealth(gameData.playerData.maxHealth);
            playerStats.SetEnergy(gameData.playerData.energy);
            playerStats.SetMaxEnergy(gameData.playerData.maxEnergy);
            playerStats.SetMoney(gameData.playerData.money);
            playerStats.SetPlayerLevel(gameData.playerData.playerLevel);
            playerStats.SetExperience(gameData.playerData.experience);
            playerStats.SetExperienceToNextLevel(gameData.playerData.experienceToNextLevel);
        }
        
        // Note: Scene loading should be handled separately by a SceneManager
    }
    
    // ============================================================================
    // UTILITY METHODS
    // ============================================================================
    
    /// <summary>
    /// Manually save player position (useful for checkpoints)
    /// </summary>
    public void SavePosition()
    {
        if (SaveManager.Instance != null)
        {
            var gameData = SaveManager.Instance.GetCurrentGameData();
            if (gameData != null)
            {
                gameData.playerData.position = transform.position;
                gameData.playerData.rotation = transform.eulerAngles.z;
                gameData.playerData.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
        }
    }
    
    /// <summary>
    /// Get current player position data
    /// </summary>
    public PlayerGameData GetPlayerData()
    {
        var data = new PlayerGameData();
        data.position = transform.position;
        data.rotation = transform.eulerAngles.z;
        data.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        if (playerStats != null)
        {
            data.health = playerStats.CurrentHealth;
            data.maxHealth = playerStats.MaxHealth;
            data.energy = playerStats.CurrentEnergy;
            data.maxEnergy = playerStats.MaxEnergy;
            data.money = playerStats.Money;
            data.playerLevel = playerStats.PlayerLevel;
            data.experience = playerStats.Experience;
            data.experienceToNextLevel = playerStats.ExperienceToNextLevel;
        }
        
        return data;
    }
    
    #if UNITY_EDITOR
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PlayerDataManager] {message}");
        }
    }
    #endif
    
    // ============================================================================
    // EDITOR/DEBUG METHODS
    // ============================================================================
    
    #if UNITY_EDITOR
    [ContextMenu("Save Current Position")]
    public void DebugSavePosition()
    {
        SavePosition();
    }
    
    [ContextMenu("Show Player Data")]
    public void DebugShowPlayerData()
    {
        var data = GetPlayerData();
    }
    #endif
}