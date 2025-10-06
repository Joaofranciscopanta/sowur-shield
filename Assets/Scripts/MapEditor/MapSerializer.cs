using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class MapSerializer : MonoBehaviour
{
    public static MapSerializer Instance { get; private set; }
    
    [Header("Save Settings")]
    [SerializeField] private string mapsFolder = "Assets/Maps";
    [SerializeField] private string backupFolder = "Assets/Maps/Backups";
    [SerializeField] private int maxBackups = 10;
    [SerializeField] private bool createBackupOnSave = true;
    
    [Header("Auto Save")]
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private int maxAutoSaves = 5;
    
    // Runtime variables
    private float lastAutoSaveTime;
    private RuntimeMapEditor mapEditor;
    
    // Events
    public System.Action<string> OnMapSaved;
    public System.Action<string> OnMapLoaded;
    public System.Action<string> OnBackupCreated;
    public System.Action<string> OnError;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        mapEditor = RuntimeMapEditor.Instance;
        EnsureDirectoriesExist();
        lastAutoSaveTime = Time.time;
    }
    
    private void Update()
    {
        HandleAutoSave();
    }
    
    private void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(mapsFolder))
        {
            Directory.CreateDirectory(mapsFolder);

        }
        
        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);

        }
    }
    
    private void HandleAutoSave()
    {
        if (!enableAutoSave || mapEditor == null || !mapEditor.IsEditorActive) return;
        
        if (Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            AutoSaveCurrentMap();
            lastAutoSaveTime = Time.time;
        }
    }
    
    public void SaveMapData(MapData mapData, string fileName = null)
    {
        if (mapData == null)
        {
            OnError?.Invoke("Cannot save null map data");
            return;
        }
        
        try
        {
            // Generate filename if not provided
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = SanitizeFileName(mapData.mapName);
            }
            
            // Ensure .asset extension
            if (!fileName.EndsWith(".asset"))
            {
                fileName += ".asset";
            }
            
            string fullPath = Path.Combine(mapsFolder, fileName);
            
            // Create backup if enabled
            if (createBackupOnSave && File.Exists(fullPath))
            {
                CreateBackup(fullPath);
            }
            
            // Update metadata
            mapData.UpdateMetadata();
            
            // Save as ScriptableObject asset
            SaveAsScriptableObject(mapData, fullPath);
            
            // Also save as JSON for external use/backup
            SaveAsJSON(mapData, fullPath.Replace(".asset", ".json"));
            
            OnMapSaved?.Invoke(fileName);

            
        }
        catch (System.Exception e)
        {
            string errorMsg = $"Failed to save map: {e.Message}";
            OnError?.Invoke(errorMsg);

        }
    }
    
    public MapData LoadMapData(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            OnError?.Invoke("Filename cannot be empty");
            return null;
        }
        
        try
        {
            // Ensure .asset extension
            if (!fileName.EndsWith(".asset"))
            {
                fileName += ".asset";
            }
            
            string fullPath = Path.Combine(mapsFolder, fileName);
            
            if (!File.Exists(fullPath))
            {
                OnError?.Invoke($"Map file not found: {fileName}");
                return null;
            }
            
            // Load ScriptableObject
            MapData mapData = LoadScriptableObject(fullPath);
            
            if (mapData != null)
            {
                OnMapLoaded?.Invoke(fileName);

            }
            
            return mapData;
        }
        catch (System.Exception e)
        {
            string errorMsg = $"Failed to load map: {e.Message}";
            OnError?.Invoke(errorMsg);

            return null;
        }
    }
    
    public List<string> GetAvailableMaps()
    {
        List<string> mapFiles = new List<string>();
        
        try
        {
            if (Directory.Exists(mapsFolder))
            {
                string[] files = Directory.GetFiles(mapsFolder, "*.asset");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    mapFiles.Add(fileName);
                }
            }
        }
        catch (System.Exception e)
        {

        }
        
        return mapFiles;
    }
    
    public void DeleteMapData(string fileName)
    {
        try
        {
            if (!fileName.EndsWith(".asset"))
            {
                fileName += ".asset";
            }
            
            string fullPath = Path.Combine(mapsFolder, fileName);
            string jsonPath = fullPath.Replace(".asset", ".json");
            
            if (File.Exists(fullPath))
            {
                // Create backup before deleting
                CreateBackup(fullPath);
                File.Delete(fullPath);
            }
            
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
            

        }
        catch (System.Exception e)
        {
            string errorMsg = $"Failed to delete map: {e.Message}";
            OnError?.Invoke(errorMsg);

        }
    }
    
    private void CreateBackup(string originalPath)
    {
        try
        {
            string fileName = Path.GetFileNameWithoutExtension(originalPath);
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string backupFileName = $"{fileName}_backup_{timestamp}.asset";
            string backupPath = Path.Combine(backupFolder, backupFileName);
            
            File.Copy(originalPath, backupPath);
            
            // Clean up old backups
            CleanupOldBackups(fileName);
            
            OnBackupCreated?.Invoke(backupFileName);

        }
        catch (System.Exception e)
        {

        }
    }
    
    private void CleanupOldBackups(string mapName)
    {
        try
        {
            if (!Directory.Exists(backupFolder)) return;
            
            string[] backupFiles = Directory.GetFiles(backupFolder, $"{mapName}_backup_*.asset");
            
            if (backupFiles.Length > maxBackups)
            {
                // Sort by creation time and delete oldest
                System.Array.Sort(backupFiles, (x, y) => File.GetCreationTime(x).CompareTo(File.GetCreationTime(y)));
                
                int toDelete = backupFiles.Length - maxBackups;
                for (int i = 0; i < toDelete; i++)
                {
                    File.Delete(backupFiles[i]);
                }
            }
        }
        catch (System.Exception e)
        {

        }
    }
    
    private void AutoSaveCurrentMap()
    {
        if (mapEditor?.CurrentMapData == null) return;
        
        string autoSaveFileName = $"{mapEditor.CurrentMapData.mapName}_autosave_{System.DateTime.Now:HH-mm}";
        SaveMapData(mapEditor.CurrentMapData, autoSaveFileName);
        
        // Clean up old auto saves
        CleanupOldAutoSaves();
        

    }
    
    private void CleanupOldAutoSaves()
    {
        try
        {
            string[] autoSaveFiles = Directory.GetFiles(mapsFolder, "*_autosave_*.asset");
            
            if (autoSaveFiles.Length > maxAutoSaves)
            {
                System.Array.Sort(autoSaveFiles, (x, y) => File.GetCreationTime(x).CompareTo(File.GetCreationTime(y)));
                
                int toDelete = autoSaveFiles.Length - maxAutoSaves;
                for (int i = 0; i < toDelete; i++)
                {
                    File.Delete(autoSaveFiles[i]);
                }
            }
        }
        catch (System.Exception e)
        {

        }
    }
    
    private void SaveAsScriptableObject(MapData mapData, string path)
    {
#if UNITY_EDITOR
        // In editor, use Unity's asset system
        UnityEditor.AssetDatabase.CreateAsset(Object.Instantiate(mapData), path);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#else
        // In build, save as JSON (ScriptableObject assets can't be created at runtime)
        SaveAsJSON(mapData, path.Replace(".asset", ".json"));
#endif
    }
    
    private MapData LoadScriptableObject(string path)
    {
#if UNITY_EDITOR
        // In editor, load as Unity asset
        return UnityEditor.AssetDatabase.LoadAssetAtPath<MapData>(path);
#else
        // In build, load from JSON
        return LoadFromJSON(path.Replace(".asset", ".json"));
#endif
    }
    
    private void SaveAsJSON(MapData mapData, string path)
    {
        try
        {
            MapDataJSON jsonData = ConvertToJSON(mapData);
            string json = JsonUtility.ToJson(jsonData, true);
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {

        }
    }
    
    private MapData LoadFromJSON(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            
            string json = File.ReadAllText(path);
            MapDataJSON jsonData = JsonUtility.FromJson<MapDataJSON>(json);
            return ConvertFromJSON(jsonData);
        }
        catch (System.Exception e)
        {

            return null;
        }
    }
    
    private MapDataJSON ConvertToJSON(MapData mapData)
    {
        return new MapDataJSON
        {
            mapName = mapData.mapName,
            description = mapData.description,
            mapSize = mapData.mapSize,
            playerSpawnPosition = mapData.playerSpawnPosition,
            tileData = mapData.tileData,
            npcSpawns = mapData.npcSpawns,
            objectSpawns = mapData.objectSpawns,
            spawnPoints = mapData.spawnPoints,
            interactionZones = mapData.interactionZones,
            backgroundColor = mapData.backgroundColor,
            enableDayNightCycle = mapData.enableDayNightCycle,
            ambientLightLevel = mapData.ambientLightLevel,
            createdBy = mapData.createdBy,
            creationDate = mapData.creationDate,
            lastModified = mapData.lastModified,
            versionNumber = mapData.versionNumber
        };
    }
    
    private MapData ConvertFromJSON(MapDataJSON jsonData)
    {
        MapData mapData = ScriptableObject.CreateInstance<MapData>();
        
        mapData.mapName = jsonData.mapName;
        mapData.description = jsonData.description;
        mapData.mapSize = jsonData.mapSize;
        mapData.playerSpawnPosition = jsonData.playerSpawnPosition;
        mapData.tileData = jsonData.tileData;
        mapData.npcSpawns = jsonData.npcSpawns;
        mapData.objectSpawns = jsonData.objectSpawns;
        mapData.spawnPoints = jsonData.spawnPoints;
        mapData.interactionZones = jsonData.interactionZones;
        mapData.backgroundColor = jsonData.backgroundColor;
        mapData.enableDayNightCycle = jsonData.enableDayNightCycle;
        mapData.ambientLightLevel = jsonData.ambientLightLevel;
        mapData.createdBy = jsonData.createdBy;
        mapData.creationDate = jsonData.creationDate;
        mapData.lastModified = jsonData.lastModified;
        mapData.versionNumber = jsonData.versionNumber;
        
        return mapData;
    }
    
    private string SanitizeFileName(string fileName)
    {
        string sanitized = fileName;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        
        foreach (char c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        
        return sanitized;
    }
    
    // Export/Import for sharing maps
    public void ExportMap(MapData mapData, string exportPath)
    {
        try
        {
            MapDataJSON jsonData = ConvertToJSON(mapData);
            string json = JsonUtility.ToJson(jsonData, true);
            File.WriteAllText(exportPath, json);
            

        }
        catch (System.Exception e)
        {
            OnError?.Invoke($"Export failed: {e.Message}");
        }
    }
    
    public MapData ImportMap(string importPath)
    {
        try
        {
            if (!File.Exists(importPath))
            {
                OnError?.Invoke($"Import file not found: {importPath}");
                return null;
            }
            
            return LoadFromJSON(importPath);
        }
        catch (System.Exception e)
        {
            OnError?.Invoke($"Import failed: {e.Message}");
            return null;
        }
    }
}

[System.Serializable]
public class MapDataJSON
{
    public string mapName;
    public string description;
    public Vector2Int mapSize;
    public Vector3 playerSpawnPosition;
    public List<TileDataEntry> tileData;
    public List<NPCSpawnData> npcSpawns;
    public List<ObjectSpawnData> objectSpawns;
    public List<SpawnPointData> spawnPoints;
    public List<InteractionZoneData> interactionZones;
    public Color backgroundColor;
    public bool enableDayNightCycle;
    public float ambientLightLevel;
    public string createdBy;
    public string creationDate;
    public string lastModified;
    public int versionNumber;
}