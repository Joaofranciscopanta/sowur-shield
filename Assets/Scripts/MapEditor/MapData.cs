using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "New Map", menuName = "Sowur Shield/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Map Information")]
    public string mapName = "Untitled Map";
    public string description = "";
    public Vector2Int mapSize = new Vector2Int(100, 100);
    public Vector3 playerSpawnPosition = Vector3.zero;
    
    [Header("Tile Data")]
    public List<TileDataEntry> tileData = new List<TileDataEntry>();
    
    [Header("Object Data")]
    public List<NPCSpawnData> npcSpawns = new List<NPCSpawnData>();
    public List<ObjectSpawnData> objectSpawns = new List<ObjectSpawnData>();
    public List<SpawnPointData> spawnPoints = new List<SpawnPointData>();
    public List<InteractionZoneData> interactionZones = new List<InteractionZoneData>();
    
    [Header("Map Settings")]
    public Color backgroundColor = Color.black;
    public bool enableDayNightCycle = true;
    public float ambientLightLevel = 1f;
    
    [Header("Metadata")]
    public string createdBy = "Unknown";
    public string creationDate = "";
    public string lastModified = "";
    public int versionNumber = 1;
    
    private void OnEnable()
    {
        if (string.IsNullOrEmpty(creationDate))
        {
            creationDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
    
    public void UpdateMetadata()
    {
        lastModified = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        versionNumber++;
    }
    
    public TileDataEntry GetTileAt(Vector3Int position)
    {
        return tileData.Find(t => t.position == position);
    }
    
    public void SetTileAt(Vector3Int position, ExtendedTileType tileType, int layer = 0)
    {
        var existingTile = GetTileAt(position);
        if (existingTile != null)
        {
            existingTile.tileType = tileType;
            existingTile.layer = layer;
        }
        else
        {
            tileData.Add(new TileDataEntry
            {
                position = position,
                tileType = tileType,
                layer = layer
            });
        }
        UpdateMetadata();
    }
    
    public void RemoveTileAt(Vector3Int position)
    {
        tileData.RemoveAll(t => t.position == position);
        UpdateMetadata();
    }
    
    public void AddNPCSpawn(Vector3 position, string npcId, string npcName, string[] dialogueIds)
    {
        npcSpawns.Add(new NPCSpawnData
        {
            position = position,
            npcId = npcId,
            npcName = npcName,
            dialogueIds = dialogueIds,
            rotation = 0f
        });
        UpdateMetadata();
    }
    
    public void RemoveNPCSpawn(Vector3 position)
    {
        npcSpawns.RemoveAll(npc => Vector3.Distance(npc.position, position) < 0.5f);
        UpdateMetadata();
    }
    
    public void ClearAllData()
    {
        tileData.Clear();
        npcSpawns.Clear();
        objectSpawns.Clear();
        spawnPoints.Clear();
        interactionZones.Clear();
        UpdateMetadata();
    }
}

[System.Serializable]
public class TileDataEntry
{
    public Vector3Int position;
    public ExtendedTileType tileType;
    public int layer = 0;
    public bool hasCollision = false;
    public string customData = "";
}

[System.Serializable]
public class NPCSpawnData
{
    public Vector3 position;
    public string npcId;
    public string npcName;
    public string[] dialogueIds;
    public float rotation = 0f;
    public bool isActive = true;
    public string npcPrefabPath = "";
}

[System.Serializable]
public class ObjectSpawnData
{
    public Vector3 position;
    public string objectId;
    public string prefabPath;
    public Vector3 rotation = Vector3.zero;
    public Vector3 scale = Vector3.one;
    public bool isActive = true;
    public string customData = "";
}

[System.Serializable]
public class SpawnPointData
{
    public Vector3 position;
    public string spawnId;
    public SpawnPointType spawnType;
    public bool isDefault = false;
    public string description = "";
}

[System.Serializable]
public class InteractionZoneData
{
    public Vector3 center;
    public Vector2 size;
    public InteractionZoneType zoneType;
    public string targetScene = "";
    public Vector3 targetPosition = Vector3.zero;
    public string customData = "";
}

public enum ExtendedTileType
{
    None,
    Grass,
    Dirt,
    Water,
    Stone,
    Sand,
    Wood,
    Brick,
    Ice,
    Lava,
    Custom1,
    Custom2,
    Custom3,
    Custom4,
    Custom5
}

public enum SpawnPointType
{
    Player,
    Enemy,
    Item,
    Custom
}

public enum InteractionZoneType
{
    SceneTransition,
    Shop,
    House,
    Farm,
    Custom
}