using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace SowurShield.MapEditor
{

public class MapSerializer : MonoBehaviour
{
    public static MapSerializer Instance { get; private set; }
    
    [Header("Save Settings")]
    // Resources/, e nao Assets/Maps: `Assets/` e pasta de PROJETO e nao existe no
    // jogo compilado, entao um mapa salvo ali nunca poderia ser lido pelo jogo. O
    // que esta em Resources/ e empacotado no build e sai por Resources.Load, que e
    // como todo o resto deste projeto (GameBalance, AnimalData, UITheme...) carrega.
    [SerializeField] private string mapsFolder = "Assets/Resources/Maps";
    // FORA de Resources/: backup nenhum precisa ir para o build do jogo, e cada
    // copia ali dentro seria peso morto no download.
    [SerializeField] private string backupFolder = "Assets/MapBackups";

    /// <summary>Copias em JSON, tambem fora de Resources/ e do build.</summary>
    [SerializeField] private string jsonFolder = "Assets/MapBackups/json";
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
        if (!Directory.Exists(jsonFolder))
        {
            Directory.CreateDirectory(jsonFolder);
        }

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
            
            // As pastas eram criadas so no Start(), que nao rodou ainda quando este
            // componente e adicionado por AddComponent e usado no mesmo frame — o
            // primeiro save falhava em silencio e o usuario perdia o trabalho.
            EnsureDirectoriesExist();

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
            
            // O JSON e copia de conveniencia (ler/diffar fora do Unity), e vai para
            // FORA de Resources/: desde que os mapas passaram a ser carregados pelo
            // jogo, tudo o que esta em Resources/ e empacotado no build -- o .json
            // seria uma segunda copia de cada mapa no download, sem nenhum uso.
            string jsonPath = Path.Combine(
                jsonFolder, Path.GetFileNameWithoutExtension(fileName) + ".json");
            SaveAsJSON(mapData, jsonPath);
            
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
        catch (System.Exception)
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
        catch (System.Exception)
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
        catch (System.Exception)
        {

        }
    }

    /// <summary>
    /// Copia de seguranca periodica enquanto o editor esta aberto.
    ///
    /// Vai para a pasta de BACKUP, nunca para mapsFolder. Desde que os mapas passaram
    /// a viver em Resources/ (para o jogo poder carrega-los), tudo o que e escrito la
    /// entra no build -- e o autosave dispara sozinho a cada intervalo, entao uma
    /// tarde de trabalho poria dezenas de copias do mapa dentro do jogo. Ja tinha
    /// acontecido: 5 dos 7 arquivos em Assets/Maps eram autosave.
    /// </summary>
    private void AutoSaveCurrentMap()
    {
        if (mapEditor?.CurrentMapData == null) return;
        
        string autoSaveFileName = $"{mapEditor.CurrentMapData.mapName}_autosave_{System.DateTime.Now:HH-mm}";
        SaveAutoSaveOutsideResources(mapEditor.CurrentMapData, autoSaveFileName);
        
        // Clean up old auto saves
        CleanupOldAutoSaves();
        

    }
    
    /// <summary>Grava o autosave como JSON na pasta de backup, fora do build.</summary>
    private void SaveAutoSaveOutsideResources(MapData mapData, string fileName)
    {
        try
        {
            EnsureDirectoriesExist();
            string caminho = Path.Combine(backupFolder, fileName + ".json");
            SaveAsJSON(mapData, caminho);
        }
        catch (System.Exception e)
        {
            OnError?.Invoke($"Falha no autosave: {e.Message}");
        }
    }

    private void CleanupOldAutoSaves()
    {
        try
        {
            // Os autosaves agora sao .json na pasta de backup, nao .asset em
            // mapsFolder: procurar no lugar antigo faria a limpeza nunca achar nada
            // e as copias se acumularem para sempre.
            if (!Directory.Exists(backupFolder)) return;
            string[] autoSaveFiles = Directory.GetFiles(backupFolder, "*_autosave_*.json");
            
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
        catch (System.Exception)
        {

        }
    }

    private void SaveAsScriptableObject(MapData mapData, string path)
    {
#if UNITY_EDITOR
        // CreateAsset num caminho ocupado nao substitui: o Unity renomeia para
        // "Mapa 1.asset", "Mapa 2.asset"... e cada save vira um asset novo. Quem
        // constroi salvaria dez vezes e teria dez mapas, sem saber qual e o bom.
        var existente = UnityEditor.AssetDatabase.LoadAssetAtPath<MapData>(path);
        if (existente != null)
        {
            // Copia os campos para o asset que ja esta no disco, preservando o GUID
            // (uma referencia a este mapa noutro lugar continua valendo).
            UnityEditor.EditorUtility.CopySerialized(mapData, existente);
            UnityEditor.EditorUtility.SetDirty(existente);
        }
        else
        {
            UnityEditor.AssetDatabase.CreateAsset(Object.Instantiate(mapData), path);
        }
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
        catch (System.Exception)
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
        catch (System.Exception)
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
} // namespace SowurShield.MapEditor