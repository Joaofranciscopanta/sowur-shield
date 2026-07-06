using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SowurShield.Core
{
    public class SaveManager : MonoBehaviour
    {
        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "GameSave";
        [SerializeField] private string saveFileExtension = ".json";
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private bool enableBackupSaves = true;
        [SerializeField] private int maxBackupSaves = 5;
        [SerializeField] private int manualSlotCount = 3;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // Singleton instance
        public static SaveManager Instance { get; private set; }

        // Events
        public System.Action<bool> OnSaveCompleted;
        public System.Action<bool> OnLoadCompleted;
        public System.Action OnSaveStarted;
        public System.Action OnLoadStarted;

        // Save data
        private GameData currentGameData;
        public GameData CurrentGameData => currentGameData;
        private string saveDirectoryPath;

        // Slot management
        private const string AUTO_SAVE_SLOT_NAME = "AutoSave";
        private string activeSlotName = AUTO_SAVE_SLOT_NAME;
        public string ActiveSlotName => activeSlotName;

        // Registered saveable objects
        private List<ISaveable> saveableObjects = new List<ISaveable>();

        // Continue from start of day flag
        public static bool resetToStartOfDayAfterLoad = false;

        // New game initialization flag
        public static bool initializeNewGameAfterLoad = false;

        // Slot chosen in main menu before SaveManager was loaded
        public static string pendingActiveSlot = null;

        private void Awake()
        {
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
            saveDirectoryPath = Path.Combine(Application.persistentDataPath, "Saves");

            // Create saves directory if needed
            if (!Directory.Exists(saveDirectoryPath))
                Directory.CreateDirectory(saveDirectoryPath);

            // Create slot sub-directories
            CreateSlotDirectory(AUTO_SAVE_SLOT_NAME);
            for (int i = 1; i <= manualSlotCount; i++)
                CreateSlotDirectory($"Slot{i}");

            // Migrate legacy flat save file
            string legacySavePath = Path.Combine(saveDirectoryPath, saveFileName + saveFileExtension);
            if (File.Exists(legacySavePath))
            {
                string autoSavePath = GetSlotSaveFilePath(AUTO_SAVE_SLOT_NAME);
                if (!File.Exists(autoSavePath))
                {
                    File.Move(legacySavePath, autoSavePath);
                    LogDebug("Migrated legacy save file to AutoSave slot.");
                }
                else
                {
                    File.Delete(legacySavePath);
                }
            }
        }

        private void CreateSlotDirectory(string slotName)
        {
            string dir = GetSlotDirectory(slotName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private void Update()
        {
            // Accumulate play time only while the game scene is active
            if (currentGameData != null && GameTimeController.instance != null)
                currentGameData.totalPlayTime += Time.unscaledDeltaTime;
        }

        private void Start()
        {
            // Apply slot chosen in main menu before this instance was created
            if (!string.IsNullOrEmpty(pendingActiveSlot))
            {
                activeSlotName = pendingActiveSlot;
                pendingActiveSlot = null;
            }

            LogDebug($"SaveManager Start() - slot: {activeSlotName}, resetToStartOfDayAfterLoad: {resetToStartOfDayAfterLoad}, initializeNewGameAfterLoad: {initializeNewGameAfterLoad}");

            if (initializeNewGameAfterLoad)
            {
                if (GameTimeController.instance != null)
                    GameTimeController.instance.ResetForNewGame();

                currentGameData = new GameData();
                initializeNewGameAfterLoad = false;
                LogDebug("New game initialized successfully");

                // Start tutorial for new games
                TutorialManager.Instance?.StartTutorial();
            }
            else if (HasSaveFile())
            {
                LogDebug("Save file detected, calling LoadGame()");
                LoadGame();
            }
            else
            {
                currentGameData = new GameData();
                LogDebug("No save file found. Created new game data.");
            }
        }

        // ============================================================================
        // REGISTRATION SYSTEM
        // ============================================================================

        public void RegisterSaveable(ISaveable saveable)
        {
            if (saveable != null && !saveableObjects.Contains(saveable))
            {
                saveableObjects.Add(saveable);
            }
        }

        public void UnregisterSaveable(ISaveable saveable)
        {
            if (saveableObjects.Contains(saveable))
                saveableObjects.Remove(saveable);
        }

        /// <summary>
        /// Re-applies LoadData() to every currently registered ISaveable using the
        /// in-memory currentGameData, without re-reading the save file from disk.
        ///
        /// Needed when a scene is reloaded mid-session (e.g. returning from CombatScene
        /// to SampleScene) — newly instantiated objects like GroundItem register
        /// themselves but never receive their persisted state, because only
        /// LoadGame() (an explicit "load save" action) normally calls LoadData().
        /// </summary>
        public void ReapplyLoadedDataToRegisteredObjects()
        {
            if (currentGameData == null) return;

            foreach (var saveable in saveableObjects.ToList())
            {
                if (saveable != null)
                {
                    try { saveable.LoadData(currentGameData); }
                    catch (System.Exception e) { LogError($"Error re-applying data into {saveable.GetType().Name}: {e.Message}"); }
                }
            }
        }

        /// <summary>
        /// Calls SaveData() on every currently registered ISaveable into the in-memory
        /// currentGameData, without writing to disk. Needed before a scene reload (e.g.
        /// entering CombatScene) so runtime-only objects — like an animal bought from
        /// AnimalMarketUI — get their existence and state recorded even if the player never
        /// explicitly saved, otherwise AnimalPurchaseLoader has nothing to recreate them from
        /// when the farm scene reloads on return.
        /// </summary>
        public void CaptureRegisteredObjectsIntoCurrentGameData()
        {
            if (currentGameData == null)
                currentGameData = new GameData();

            foreach (var saveable in saveableObjects.ToList())
            {
                if (saveable != null)
                {
                    try { saveable.SaveData(currentGameData); }
                    catch (System.Exception e) { LogError($"Error capturing data from {saveable.GetType().Name}: {e.Message}"); }
                }
            }
        }

        // ============================================================================
        // SLOT MANAGEMENT
        // ============================================================================

        public void SetActiveSlot(string slotName)
        {
            activeSlotName = slotName;
            LogDebug($"Active slot set to: {slotName}");
        }

        /// <summary>Save to a specific slot without changing the activeSlotName.</summary>
        public void SaveToSlot(string slotName)
        {
            string prev = activeSlotName;
            activeSlotName = slotName;
            SaveGame();
            activeSlotName = prev;
        }

        /// <summary>Set active slot and load the game from it.</summary>
        public void LoadFromSlot(string slotName)
        {
            if (!HasSaveFile(slotName))
            {
                LogError($"LoadFromSlot('{slotName}') aborted — slot has no save file. Active slot unchanged.");
                OnLoadCompleted?.Invoke(false);
                return;
            }

            activeSlotName = slotName;
            LoadGame();
        }

        /// <summary>Delete all files in a slot directory. AutoSave cannot be deleted.</summary>
        public bool DeleteSlot(string slotName)
        {
            if (slotName == AUTO_SAVE_SLOT_NAME)
            {
                LogDebug("Cannot delete AutoSave slot.");
                return false;
            }

            string dir = GetSlotDirectory(slotName);
            if (!Directory.Exists(dir))
                return false;

            foreach (string file in Directory.GetFiles(dir))
                File.Delete(file);

            LogDebug($"Slot '{slotName}' deleted.");
            return true;
        }

        public SaveSlotInfo GetSlotInfo(string slotName)
        {
            return ReadSlotMeta(slotName);
        }

        public SaveSlotInfo[] GetAllSlotInfos()
        {
            var list = new List<SaveSlotInfo>();
            list.Add(ReadSlotMeta(AUTO_SAVE_SLOT_NAME));
            for (int i = 1; i <= manualSlotCount; i++)
                list.Add(ReadSlotMeta($"Slot{i}"));
            return list.ToArray();
        }

        /// <summary>Returns the slot name with the most recent saveTimestamp, or AUTO_SAVE_SLOT_NAME if all empty.</summary>
        public string GetMostRecentSlotName()
        {
            string best = AUTO_SAVE_SLOT_NAME;
            System.DateTime bestTime = System.DateTime.MinValue;

            foreach (var info in GetAllSlotInfos())
            {
                if (info.isEmpty) continue;
                if (System.DateTime.TryParse(info.saveTimestamp, out System.DateTime t) && t > bestTime)
                {
                    bestTime = t;
                    best = info.slotName;
                }
            }
            return best;
        }

        // ============================================================================
        // SAVE OPERATIONS
        // ============================================================================

        public void SaveGame()
        {
            // Demo builds ship without persistence, but the Editor must always be able to
            // save — otherwise QA on the WebGL target silently no-ops (see QA_UI_AUDIT_2026-07-05).
#if DEMO_BUILD && !UNITY_EDITOR
            OnSaveCompleted?.Invoke(false);
            return;
#endif
            OnSaveStarted?.Invoke();
            LogDebug($"SaveGame() to slot '{activeSlotName}' - registered objects: {saveableObjects.Count}");

            try
            {
                if (currentGameData == null)
                    currentGameData = new GameData();
                else
                {
                    currentGameData.saveTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    currentGameData.saveCount++;
                }

                foreach (var saveable in saveableObjects.ToList())
                {
                    if (saveable != null)
                    {
                        try { saveable.SaveData(currentGameData); }
                        catch (System.Exception e) { LogError($"Error saving data from {saveable.GetType().Name}: {e.Message}"); }
                    }
                }

                string savePath = CurrentSaveFilePath;

                if (enableBackupSaves && File.Exists(savePath))
                    CreateBackupSave();

                string jsonData = JsonUtility.ToJson(currentGameData, true);
                File.WriteAllText(savePath, jsonData);

                WriteSlotMeta(activeSlotName, currentGameData);

                OnSaveCompleted?.Invoke(true);
            }
            catch (System.Exception e)
            {
                LogError($"Failed to save game: {e.Message}");
                OnSaveCompleted?.Invoke(false);
            }
        }

        private void CreateBackupSave()
        {
            try
            {
                string slotDir = GetSlotDirectory(activeSlotName);
                string backupFileName = $"{saveFileName}_backup_{System.DateTime.Now:yyyyMMdd_HHmmss}{saveFileExtension}";
                string backupFilePath = Path.Combine(slotDir, backupFileName);
                File.Copy(CurrentSaveFilePath, backupFilePath);
                CleanOldBackups();
            }
            catch (System.Exception e)
            {
                LogError($"Failed to create backup: {e.Message}");
            }
        }

        private void CleanOldBackups()
        {
            try
            {
                string slotDir = GetSlotDirectory(activeSlotName);
                var backupFiles = Directory.GetFiles(slotDir, $"{saveFileName}_backup_*{saveFileExtension}")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToArray();

                for (int i = maxBackupSaves; i < backupFiles.Length; i++)
                    File.Delete(backupFiles[i]);
            }
            catch (System.Exception e)
            {
                LogError($"Failed to clean old backups: {e.Message}");
            }
        }

        // ============================================================================
        // LOAD OPERATIONS
        // ============================================================================

        public void LoadGame()
        {
#if DEMO_BUILD && !UNITY_EDITOR
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
            LogDebug($"Loading from slot '{activeSlotName}'...");

            try
            {
                string jsonData = File.ReadAllText(CurrentSaveFilePath);
                currentGameData = JsonUtility.FromJson<GameData>(jsonData);

                if (currentGameData == null)
                    throw new System.Exception("Failed to parse save file JSON");

                if (currentGameData.saveVersion < GameData.CURRENT_SAVE_VERSION)
                    currentGameData = MigrateSave(currentGameData);

                LogDebug($"Loading data into {saveableObjects.Count} registered objects");
                foreach (var saveable in saveableObjects.ToList())
                {
                    if (saveable != null)
                    {
                        try { saveable.LoadData(currentGameData); }
                        catch (System.Exception e) { LogError($"Error loading data into {saveable.GetType().Name}: {e.Message}"); }
                    }
                }

                if (resetToStartOfDayAfterLoad)
                {
                    if (GameTimeController.instance != null)
                        GameTimeController.instance.ResetToStartOfDay();
                    else
                    {
                        GameTimeController tc = Object.FindFirstObjectByType<GameTimeController>();
                        tc?.ResetToStartOfDay();
                    }
                    resetToStartOfDayAfterLoad = false;
                }

                OnLoadCompleted?.Invoke(true);
            }
            catch (System.Exception e)
            {
                LogError($"Failed to load game: {e.Message}");
                OnLoadCompleted?.Invoke(false);
                currentGameData = new GameData();
            }
        }

        // ============================================================================
        // SAVE MIGRATION
        // ============================================================================

        private GameData MigrateSave(GameData data)
        {
            int fromVersion = data.saveVersion;

            while (data.saveVersion < GameData.CURRENT_SAVE_VERSION)
            {
                switch (data.saveVersion)
                {
                    // case 1: MigrateV1ToV2(data); break; // example for the future
                    default:
                        break; // no-op for versions with no transformation needed
                }

                data.saveVersion++;
            }

            if (fromVersion != data.saveVersion)
                Debug.LogWarning($"[SaveManager] Migrated save from v{fromVersion} to v{data.saveVersion}");

            return data;
        }

        // ============================================================================
        // UTILITY METHODS
        // ============================================================================

        public bool HasSaveFile() => HasSaveFile(activeSlotName);

        public bool HasSaveFile(string slotName)
        {
#if DEMO_BUILD && !UNITY_EDITOR
            return false;
#endif
            return File.Exists(GetSlotSaveFilePath(slotName));
        }

        public void DeleteSaveFile()
        {
            string path = CurrentSaveFilePath;
            if (File.Exists(path))
                File.Delete(path);

            // Also remove the meta file
            string metaPath = GetSlotMetaFilePath(activeSlotName);
            if (File.Exists(metaPath))
                File.Delete(metaPath);
        }

        public SaveFileInfo GetSaveFileInfo()
        {
            if (!HasSaveFile()) return null;

            FileInfo fileInfo = new FileInfo(CurrentSaveFilePath);
            return new SaveFileInfo
            {
                fileName = Path.GetFileName(CurrentSaveFilePath),
                filePath = CurrentSaveFilePath,
                creationTime = fileInfo.CreationTime,
                lastWriteTime = fileInfo.LastWriteTime,
                fileSizeBytes = fileInfo.Length
            };
        }

        public GameData GetCurrentGameData() => currentGameData;

        // ============================================================================
        // AUTO SAVE SYSTEM
        // ============================================================================

        public void TriggerAutoSave()
        {
            LogDebug("TriggerAutoSave() called");

            if (enableAutoSave)
            {
                string prev = activeSlotName;
                activeSlotName = AUTO_SAVE_SLOT_NAME;
                SaveGame();
                activeSlotName = prev;
            }
            else
            {
                LogDebug("Auto save is disabled - skipping save");
            }
        }

        // ============================================================================
        // PRIVATE HELPERS
        // ============================================================================

        private string CurrentSaveFilePath => GetSlotSaveFilePath(activeSlotName);

        private string GetSlotDirectory(string slotName) =>
            Path.Combine(saveDirectoryPath, slotName);

        private string GetSlotSaveFilePath(string slotName) =>
            Path.Combine(GetSlotDirectory(slotName), saveFileName + saveFileExtension);

        private string GetSlotMetaFilePath(string slotName) =>
            Path.Combine(GetSlotDirectory(slotName), "SlotMeta.json");

        private void WriteSlotMeta(string slotName, GameData data)
        {
            try
            {
                var info = new SaveSlotInfo
                {
                    slotName = slotName,
                    isAutoSave = slotName == AUTO_SAVE_SLOT_NAME,
                    isEmpty = false,
                    currentDay = data.timeData?.currentDay ?? 1,
                    season = data.timeData?.season ?? "Spring",
                    year = data.timeData?.year ?? 1,
                    money = data.playerData?.money ?? 0,
                    totalPlayTime = data.totalPlayTime,
                    saveTimestamp = data.saveTimestamp,
                    fileSizeBytes = new FileInfo(GetSlotSaveFilePath(slotName)).Length
                };

                string json = JsonUtility.ToJson(info, true);
                File.WriteAllText(GetSlotMetaFilePath(slotName), json);
            }
            catch (System.Exception e)
            {
                LogError($"Failed to write slot meta for '{slotName}': {e.Message}");
            }
        }

        private SaveSlotInfo ReadSlotMeta(string slotName)
        {
            try
            {
                string metaPath = GetSlotMetaFilePath(slotName);
                if (File.Exists(metaPath))
                {
                    string json = File.ReadAllText(metaPath);
                    var info = JsonUtility.FromJson<SaveSlotInfo>(json);
                    if (info != null)
                    {
                        info.slotName = slotName;
                        info.isAutoSave = slotName == AUTO_SAVE_SLOT_NAME;
                        return info;
                    }
                }
            }
            catch (System.Exception e)
            {
                LogError($"Failed to read slot meta for '{slotName}': {e.Message}");
            }

            return new SaveSlotInfo
            {
                slotName = slotName,
                isAutoSave = slotName == AUTO_SAVE_SLOT_NAME,
                isEmpty = true
            };
        }

        // ============================================================================
        // DEBUG AND LOGGING
        // ============================================================================

        private void LogDebug(string message)
        {
            if (enableDebugLogs) { }
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
        public void DebugSaveGame() => SaveGame();

        [ContextMenu("Force Load Game")]
        public void DebugLoadGame() => LoadGame();

        [ContextMenu("Delete Save File")]
        public void DebugDeleteSave() => DeleteSaveFile();

        [ContextMenu("Show Save File Info")]
        public void DebugShowSaveInfo()
        {
            GetSaveFileInfo();
        }
#endif
    }

    // ============================================================================
    // DATA STRUCTURES
    // ============================================================================

    public interface ISaveable
    {
        void SaveData(GameData gameData);
        void LoadData(GameData gameData);
    }

    [System.Serializable]
    public class SaveFileInfo
    {
        public string fileName;
        public string filePath;
        public System.DateTime creationTime;
        public System.DateTime lastWriteTime;
        public long fileSizeBytes;
    }

} // namespace SowurShield.Core
