using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "GameSave";
    [SerializeField] private string saveFileExtension = ".json";
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private bool enableBackupSaves = true;
    [SerializeField] private int maxBackupSaves = 5;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // Singleton instance
    public static SaveManager Instance { get; private set; }
    
    // Events
    public System.Action<bool> OnSaveCompleted; // bool indicates success
    public System.Action<bool> OnLoadCompleted; // bool indicates success
    public System.Action OnSaveStarted;
    public System.Action OnLoadStarted;
    
    // Save data
    private GameData currentGameData;
    public GameData CurrentGameData => currentGameData;
    private string saveDirectoryPath;
    private string currentSaveFilePath;
    
    // Registered saveable objects
    private List<ISaveable> saveableObjects = new List<ISaveable>();
    
    // Continue from start of day flag
    public static bool resetToStartOfDayAfterLoad = false;
    
    // New game initialization flag
    public static bool initializeNewGameAfterLoad = false;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSaveManager();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeSaveManager()
    {
        // Set up save directory path
        saveDirectoryPath = Path.Combine(Application.persistentDataPath, "Saves");
        currentSaveFilePath = Path.Combine(saveDirectoryPath, saveFileName + saveFileExtension);
        
        // Create saves directory if it doesn't exist
        if (!Directory.Exists(saveDirectoryPath))
        {
            Directory.CreateDirectory(saveDirectoryPath);
        }
    }
    
    private void Start()
    {
        LogDebug($"SaveManager Start() - resetToStartOfDayAfterLoad: {resetToStartOfDayAfterLoad}, initializeNewGameAfterLoad: {initializeNewGameAfterLoad}");
        LogDebug($"Registered saveable objects count: {saveableObjects.Count}");
        
        // Check if we're starting a new game
        if (initializeNewGameAfterLoad)
        {
            LogDebug("New game initialization requested - resetting TimeController");
            
            // Reset TimeController for new game
            if (GameTimeController.instance != null)
            {
                GameTimeController.instance.ResetForNewGame();
            }
            
            // Create fresh game data
            currentGameData = new GameData();
            initializeNewGameAfterLoad = false;
            LogDebug("New game initialized successfully");
        }
        // Auto-load game on start if save exists (and not starting new game)
        else if (HasSaveFile())
        {
            LogDebug("Save file detected, calling LoadGame()");
            LoadGame();
        }
        else
        {
            // Create new game data
            currentGameData = new GameData();
            LogDebug("No save file found. Created new game data.");
        }
    }
    
    // ============================================================================
    // REGISTRATION SYSTEM
    // ============================================================================
    
    /// <summary>
    /// Register an object that implements ISaveable to be included in save/load operations
    /// </summary>
    public void RegisterSaveable(ISaveable saveable)
    {
        if (saveable != null && !saveableObjects.Contains(saveable))
        {
            saveableObjects.Add(saveable);
            string objectName = "Unknown";
            if (saveable is MonoBehaviour mb)
            {
                objectName = mb.gameObject.name;
            }
            LogDebug($"RegisterSaveable: Added {saveable.GetType().Name} from GameObject '{objectName}' - Total registered: {saveableObjects.Count}");
        }
        else
        {
            string objectName = "Unknown";
            if (saveable is MonoBehaviour mb)
            {
                objectName = mb.gameObject.name;
            }
            LogDebug($"RegisterSaveable: SKIPPED duplicate {saveable?.GetType().Name} from GameObject '{objectName}'");
        }
    }
    
    /// <summary>
    /// Unregister a saveable object
    /// </summary>
    public void UnregisterSaveable(ISaveable saveable)
    {
        if (saveableObjects.Contains(saveable))
        {
            saveableObjects.Remove(saveable);
        }
    }
    
    // ============================================================================
    // SAVE OPERATIONS
    // ============================================================================
    
    /// <summary>
    /// Save the current game state
    /// </summary>
    public void SaveGame()
    {
#if DEMO_BUILD
        OnSaveCompleted?.Invoke(false);
        return;
#endif
        OnSaveStarted?.Invoke();
        LogDebug($"SaveGame() called - registered objects: {saveableObjects.Count}");
        
        try
        {
            // Preserve existing game data or create fresh if none exists
            if (currentGameData == null)
            {
                currentGameData = new GameData();
            }
            else
            {
                // Update metadata for existing data
                currentGameData.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                currentGameData.saveCount++;
            }
            
            // Collect data from all registered saveable objects
            foreach (var saveable in saveableObjects.ToList()) // ToList to avoid modification during iteration
            {
                if (saveable != null)
                {
                    try
                    {
                        saveable.SaveData(currentGameData);
                    }
                    catch (System.Exception e)
                    {
                        LogError($"Error saving data from {saveable.GetType().Name}: {e.Message}");
                    }
                }
            }
            
            // Create backup if enabled
            if (enableBackupSaves && File.Exists(currentSaveFilePath))
            {
                CreateBackupSave();
            }
            
            // Convert to JSON and save
            string jsonData = JsonUtility.ToJson(currentGameData, true);
            File.WriteAllText(currentSaveFilePath, jsonData);
            
            OnSaveCompleted?.Invoke(true);
        }
        catch (System.Exception e)
        {
            LogError($"Failed to save game: {e.Message}");
            OnSaveCompleted?.Invoke(false);
        }
    }
    
    /// <summary>
    /// Create a backup of the current save file
    /// </summary>
    private void CreateBackupSave()
    {
        try
        {
            string backupFileName = $"{saveFileName}_backup_{System.DateTime.Now:yyyyMMdd_HHmmss}{saveFileExtension}";
            string backupFilePath = Path.Combine(saveDirectoryPath, backupFileName);
            
            File.Copy(currentSaveFilePath, backupFilePath);
            
            // Clean old backups if necessary
            CleanOldBackups();
        }
        catch (System.Exception e)
        {
            LogError($"Failed to create backup: {e.Message}");
        }
    }
    
    /// <summary>
    /// Remove old backup files to maintain max backup count
    /// </summary>
    private void CleanOldBackups()
    {
        try
        {
            var backupFiles = Directory.GetFiles(saveDirectoryPath, $"{saveFileName}_backup_*{saveFileExtension}")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToArray();
            
            // Remove excess backups
            for (int i = maxBackupSaves; i < backupFiles.Length; i++)
            {
                File.Delete(backupFiles[i]);
            }
        }
        catch (System.Exception e)
        {
            LogError($"Failed to clean old backups: {e.Message}");
        }
    }
    
    // ============================================================================
    // LOAD OPERATIONS
    // ============================================================================
    
    /// <summary>
    /// Load the saved game state
    /// </summary>
    public void LoadGame()
    {
#if DEMO_BUILD
        OnLoadCompleted?.Invoke(false);
        return;
#endif
        if (!HasSaveFile())
        {
            LogError("No save file found to load!");
            OnLoadCompleted?.Invoke(false);
            return;
        }
        
        OnLoadStarted?.Invoke();
        LogDebug("Starting load operation...");
        
        try
        {
            // Read and parse JSON data
            string jsonData = File.ReadAllText(currentSaveFilePath);
            currentGameData = JsonUtility.FromJson<GameData>(jsonData);
            
            if (currentGameData == null)
            {
                throw new System.Exception("Failed to parse save file JSON");
            }
            
            // Load data into all registered saveable objects
            LogDebug($"Loading data into {saveableObjects.Count} registered objects");
            foreach (var saveable in saveableObjects.ToList())
            {
                if (saveable != null)
                {
                    try
                    {
                        LogDebug($"Loading data into: {saveable.GetType().Name}");
                        saveable.LoadData(currentGameData);
                    }
                    catch (System.Exception e)
                    {
                        LogError($"Error loading data into {saveable.GetType().Name}: {e.Message}");
                    }
                }
            }
            
            // Check if we should reset to start of day (for Continue button)
            if (resetToStartOfDayAfterLoad)
            {
                LogDebug("resetToStartOfDayAfterLoad flag is TRUE - processing day reset");
                
                // Find the TimeController and reset it
                if (GameTimeController.instance != null)
                {
                    GameTimeController.instance.ResetToStartOfDay();
                    LogDebug("Reset day progress to start of day for Continue operation");
                }
                else
                {
                    // Try to find it if instance is null
                    GameTimeController timeController = FindObjectOfType<GameTimeController>();
                    if (timeController != null)
                    {
                        timeController.ResetToStartOfDay();
                        LogDebug("Reset day progress to start of day for Continue operation (found via FindObjectOfType)");
                    }
                    else
                    {
                        LogError("Could not find GameTimeController to reset day progress");
                    }
                }
                
                // Reset the flag after use
                resetToStartOfDayAfterLoad = false;
                LogDebug("resetToStartOfDayAfterLoad flag reset to FALSE");
            }
            else
            {
                LogDebug("resetToStartOfDayAfterLoad flag is FALSE - no day reset needed");
            }
            
            // Game loaded successfully
            OnLoadCompleted?.Invoke(true);
        }
        catch (System.Exception e)
        {
            LogError($"Failed to load game: {e.Message}");
            OnLoadCompleted?.Invoke(false);
            
            // Create new game data as fallback
            currentGameData = new GameData();
        }
    }
    
    // ============================================================================
    // UTILITY METHODS
    // ============================================================================
    
    /// <summary>
    /// Check if a save file exists
    /// </summary>
    public bool HasSaveFile()
    {
#if DEMO_BUILD
        return false; // No save files in demo build
#endif
        return File.Exists(currentSaveFilePath);
    }
    
    /// <summary>
    /// Delete the current save file
    /// </summary>
    public void DeleteSaveFile()
    {
        if (File.Exists(currentSaveFilePath))
        {
            File.Delete(currentSaveFilePath);
        }
    }
    
    /// <summary>
    /// Get save file information
    /// </summary>
    public SaveFileInfo GetSaveFileInfo()
    {
        if (!HasSaveFile())
            return null;
        
        FileInfo fileInfo = new FileInfo(currentSaveFilePath);
        return new SaveFileInfo
        {
            fileName = Path.GetFileName(currentSaveFilePath),
            filePath = currentSaveFilePath,
            creationTime = fileInfo.CreationTime,
            lastWriteTime = fileInfo.LastWriteTime,
            fileSizeBytes = fileInfo.Length
        };
    }
    
    /// <summary>
    /// Get current game data (read-only)
    /// </summary>
    public GameData GetCurrentGameData()
    {
        return currentGameData;
    }
    
    // ============================================================================
    // AUTO SAVE SYSTEM
    // ============================================================================
    
    /// <summary>
    /// Called by BedInteractable when player sleeps - triggers auto save
    /// </summary>
    public void TriggerAutoSave()
    {
        LogDebug("TriggerAutoSave() called");
        
        if (enableAutoSave)
        {
            SaveGame();
        }
        else
        {
            LogDebug("Auto save is disabled - skipping save");
        }
    }
    
    // ============================================================================
    // DEBUG AND LOGGING
    // ============================================================================
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
        }
    }
    
    private void LogError(string message)
    {
    }
    
    // ============================================================================
    // EDITOR/DEBUG METHODS
    // ============================================================================
    
    #if UNITY_EDITOR
    [ContextMenu("Force Save Game")]
    public void DebugSaveGame()
    {
        SaveGame();
    }
    
    [ContextMenu("Force Load Game")]
    public void DebugLoadGame()
    {
        LoadGame();
    }
    
    [ContextMenu("Delete Save File")]
    public void DebugDeleteSave()
    {
        DeleteSaveFile();
    }
    
    [ContextMenu("Show Save File Info")]
    public void DebugShowSaveInfo()
    {
        var info = GetSaveFileInfo();
        if (info != null)
        {
        }
        else
        {
        }
    }
    #endif
}

// ============================================================================
// DATA STRUCTURES
// ============================================================================

/// <summary>
/// Interface for objects that can be saved and loaded
/// </summary>
public interface ISaveable
{
    void SaveData(GameData gameData);
    void LoadData(GameData gameData);
}

/// <summary>
/// Information about a save file
/// </summary>
[System.Serializable]
public class SaveFileInfo
{
    public string fileName;
    public string filePath;
    public System.DateTime creationTime;
    public System.DateTime lastWriteTime;
    public long fileSizeBytes;
}