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
    private string saveDirectoryPath;
    private string currentSaveFilePath;
    
    // Registered saveable objects
    private List<ISaveable> saveableObjects = new List<ISaveable>();
    
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
            LogDebug($"Created save directory: {saveDirectoryPath}");
        }
        
        LogDebug($"SaveManager initialized. Save path: {currentSaveFilePath}");
    }
    
    private void Start()
    {
        // Auto-load game on start if save exists
        if (HasSaveFile())
        {
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
            LogDebug($"Registered saveable object: {saveable.GetType().Name}");
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
            LogDebug($"Unregistered saveable object: {saveable.GetType().Name}");
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
        OnSaveStarted?.Invoke();
        LogDebug("Starting save operation...");
        
        try
        {
            // Create fresh game data
            currentGameData = new GameData();
            
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
            
            LogDebug($"Game saved successfully to: {currentSaveFilePath}");
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
            LogDebug($"Created backup save: {backupFileName}");
            
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
                LogDebug($"Removed old backup: {Path.GetFileName(backupFiles[i])}");
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
            foreach (var saveable in saveableObjects.ToList())
            {
                if (saveable != null)
                {
                    try
                    {
                        saveable.LoadData(currentGameData);
                    }
                    catch (System.Exception e)
                    {
                        LogError($"Error loading data into {saveable.GetType().Name}: {e.Message}");
                    }
                }
            }
            
            LogDebug($"Game loaded successfully from: {currentSaveFilePath}");
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
            LogDebug("Save file deleted");
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
        if (enableAutoSave)
        {
            LogDebug("Auto-save triggered by sleeping");
            SaveGame();
        }
    }
    
    // ============================================================================
    // DEBUG AND LOGGING
    // ============================================================================
    
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[SaveManager] {message}");
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
            Debug.Log($"Save File Info:\n" +
                     $"Name: {info.fileName}\n" +
                     $"Path: {info.filePath}\n" +
                     $"Created: {info.creationTime}\n" +
                     $"Modified: {info.lastWriteTime}\n" +
                     $"Size: {info.fileSizeBytes} bytes");
        }
        else
        {
            Debug.Log("No save file exists");
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