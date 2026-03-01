using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using SowurShield.Farming;

namespace SowurShield.MapEditor
{

public class ExtendedDualGridTilemap : MonoBehaviour
{
    protected static Vector3Int[] NEIGHBOURS = new Vector3Int[] {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0)
    };

    [Header("Tilemap References")]
    public Tilemap placeholderTilemap;
    public Tilemap displayTilemap;
    public Tilemap[] layerTilemaps; // Support for multiple layers

    [Header("Tile Libraries")]
    public TileLibrary[] tileLibraries;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Extended tile type mapping
    private Dictionary<ExtendedTileType, Tile> placeholderTiles;
    private Dictionary<ExtendedTileType, Dictionary<Tuple<ExtendedTileType, ExtendedTileType, ExtendedTileType, ExtendedTileType>, Tile>> tileRules;
    
    // Editor integration
    private RuntimeMapEditor mapEditor;
    
    public System.Action<Vector3Int, ExtendedTileType> OnTileChanged;
    
    void Start()
    {
        InitializeTileSystem();
        mapEditor = RuntimeMapEditor.Instance;
    }
    
    private void InitializeTileSystem()
    {
        placeholderTiles = new Dictionary<ExtendedTileType, Tile>();
        tileRules = new Dictionary<ExtendedTileType, Dictionary<Tuple<ExtendedTileType, ExtendedTileType, ExtendedTileType, ExtendedTileType>, Tile>>();
        
        // Initialize tile libraries
        foreach (var library in tileLibraries)
        {
            if (library != null)
            {
                InitializeTileLibrary(library);
            }
        }
        
        RefreshDisplayTilemap();

    }
    
    private void InitializeTileLibrary(TileLibrary library)
    {
        // Store placeholder tile
        if (library.placeholderTile != null)
        {
            placeholderTiles[library.tileType] = library.placeholderTile;
        }
        
        // Initialize autotiling rules for this tile type
        var rules = new Dictionary<Tuple<ExtendedTileType, ExtendedTileType, ExtendedTileType, ExtendedTileType>, Tile>();
        
        if (library.autoTileSet != null && library.autoTileSet.Length >= 16)
        {
            // Same autotiling logic as original, but for each tile type
            var tiles = library.autoTileSet;
            var tileType = library.tileType;
            var otherType = GetComplementaryTileType(tileType);
            
            rules.Add(new(tileType, tileType, tileType, tileType), tiles[6]); // All same type
            rules.Add(new(otherType, otherType, otherType, tileType), tiles[13]); // OUTER_BOTTOM_RIGHT
            rules.Add(new(otherType, otherType, tileType, otherType), tiles[0]); // OUTER_BOTTOM_LEFT
            rules.Add(new(otherType, tileType, otherType, otherType), tiles[8]); // OUTER_TOP_RIGHT
            rules.Add(new(tileType, otherType, otherType, otherType), tiles[15]); // OUTER_TOP_LEFT
            rules.Add(new(otherType, tileType, otherType, tileType), tiles[1]); // EDGE_RIGHT
            rules.Add(new(tileType, otherType, tileType, otherType), tiles[11]); // EDGE_LEFT
            rules.Add(new(otherType, otherType, tileType, tileType), tiles[3]); // EDGE_BOTTOM
            rules.Add(new(tileType, tileType, otherType, otherType), tiles[9]); // EDGE_TOP
            rules.Add(new(otherType, tileType, tileType, tileType), tiles[5]); // INNER_BOTTOM_RIGHT
            rules.Add(new(tileType, otherType, tileType, tileType), tiles[2]); // INNER_BOTTOM_LEFT
            rules.Add(new(tileType, tileType, otherType, tileType), tiles[10]); // INNER_TOP_RIGHT
            rules.Add(new(tileType, tileType, tileType, otherType), tiles[7]); // INNER_TOP_LEFT
            rules.Add(new(otherType, tileType, tileType, otherType), tiles[14]); // DUAL_UP_RIGHT
            rules.Add(new(tileType, otherType, otherType, tileType), tiles[4]); // DUAL_DOWN_RIGHT
            rules.Add(new(otherType, otherType, otherType, otherType), tiles[12]); // All other type
        }
        
        tileRules[library.tileType] = rules;
    }
    
    private ExtendedTileType GetComplementaryTileType(ExtendedTileType tileType)
    {
        // For now, use Grass as default "other" type
        // This could be made more sophisticated based on biome or context
        switch (tileType)
        {
            case ExtendedTileType.Grass: return ExtendedTileType.Dirt;
            case ExtendedTileType.Dirt: return ExtendedTileType.Grass;
            case ExtendedTileType.Water: return ExtendedTileType.Sand;
            case ExtendedTileType.Sand: return ExtendedTileType.Water;
            case ExtendedTileType.Stone: return ExtendedTileType.Dirt;
            default: return ExtendedTileType.Grass;
        }
    }
    
    public void SetCell(Vector3Int coords, ExtendedTileType tileType, int layer = 0)
    {
        // Set placeholder tile
        if (placeholderTiles.ContainsKey(tileType))
        {
            Tilemap targetTilemap = GetTilemapForLayer(layer);
            targetTilemap.SetTile(coords, placeholderTiles[tileType]);
            
            // Update display
            SetDisplayTile(coords);
            
            // Notify editor
            OnTileChanged?.Invoke(coords, tileType);
            
            if (showDebugInfo)
            {

            }
        }
        else
        {

        }
    }
    
    public void SetCell(Vector3Int coords, Tile tile)
    {
        // Legacy method for compatibility with original DualGridTilemap
        placeholderTilemap.SetTile(coords, tile);
        SetDisplayTile(coords);
    }
    
    public ExtendedTileType GetTileTypeAt(Vector3Int coords, int layer = 0)
    {
        Tilemap targetTilemap = GetTilemapForLayer(layer);
        Tile tileAtPosition = targetTilemap.GetTile<Tile>(coords);
        
        // Find which tile type this represents
        foreach (var kvp in placeholderTiles)
        {
            if (kvp.Value == tileAtPosition)
            {
                return kvp.Key;
            }
        }
        
        return ExtendedTileType.None;
    }
    
    private Tilemap GetTilemapForLayer(int layer)
    {
        if (layer == 0 || layerTilemaps == null || layer - 1 >= layerTilemaps.Length)
        {
            return placeholderTilemap; // Default to main tilemap
        }
        
        return layerTilemaps[layer - 1];
    }
    
    private Tile CalculateDisplayTile(Vector3Int coords, ExtendedTileType centerTileType)
    {
        if (!tileRules.ContainsKey(centerTileType))
        {
            // Return a default tile or the placeholder
            return placeholderTiles.ContainsKey(centerTileType) ? placeholderTiles[centerTileType] : null;
        }
        
        // Get 4 neighbours
        ExtendedTileType topRight = GetTileTypeAt(coords - NEIGHBOURS[0]);
        ExtendedTileType topLeft = GetTileTypeAt(coords - NEIGHBOURS[1]);
        ExtendedTileType botRight = GetTileTypeAt(coords - NEIGHBOURS[2]);
        ExtendedTileType botLeft = GetTileTypeAt(coords - NEIGHBOURS[3]);
        
        var neighbourTuple = new Tuple<ExtendedTileType, ExtendedTileType, ExtendedTileType, ExtendedTileType>(
            topLeft, topRight, botLeft, botRight);
        
        var rules = tileRules[centerTileType];
        
        if (rules.ContainsKey(neighbourTuple))
        {
            return rules[neighbourTuple];
        }
        
        // Fallback to placeholder tile
        return placeholderTiles.ContainsKey(centerTileType) ? placeholderTiles[centerTileType] : null;
    }
    
    protected void SetDisplayTile(Vector3Int pos)
    {
        for (int i = 0; i < NEIGHBOURS.Length; i++)
        {
            Vector3Int newPos = pos + NEIGHBOURS[i];
            ExtendedTileType tileType = GetTileTypeAt(newPos);
            
            if (tileType != ExtendedTileType.None)
            {
                Tile displayTile = CalculateDisplayTile(newPos, tileType);
                if (displayTile != null)
                {
                    displayTilemap.SetTile(newPos, displayTile);
                }
            }
        }
    }
    
    public void RefreshDisplayTilemap()
    {
        if (displayTilemap == null) return;
        
        // Clear display tilemap
        displayTilemap.SetTilesBlock(new BoundsInt(-100, -100, 0, 200, 200, 1), new TileBase[200 * 200]);
        
        // Rebuild from placeholder data
        for (int i = -50; i < 50; i++)
        {
            for (int j = -50; j < 50; j++)
            {
                Vector3Int pos = new Vector3Int(i, j, 0);
                ExtendedTileType tileType = GetTileTypeAt(pos);
                
                if (tileType != ExtendedTileType.None)
                {
                    Tile displayTile = CalculateDisplayTile(pos, tileType);
                    if (displayTile != null)
                    {
                        displayTilemap.SetTile(pos, displayTile);
                    }
                }
            }
        }
        

    }
    
    public void ClearArea(BoundsInt area, int layer = 0)
    {
        Tilemap targetTilemap = GetTilemapForLayer(layer);
        
        TileBase[] emptyTiles = new TileBase[area.size.x * area.size.y];
        targetTilemap.SetTilesBlock(area, emptyTiles);
        
        // Update display for affected area
        for (int x = area.xMin - 1; x <= area.xMax + 1; x++)
        {
            for (int y = area.yMin - 1; y <= area.yMax + 1; y++)
            {
                SetDisplayTile(new Vector3Int(x, y, 0));
            }
        }
    }
    
    public void FillArea(BoundsInt area, ExtendedTileType tileType, int layer = 0)
    {
        for (int x = area.xMin; x < area.xMax; x++)
        {
            for (int y = area.yMin; y < area.yMax; y++)
            {
                SetCell(new Vector3Int(x, y, 0), tileType, layer);
            }
        }
    }
    
    // Integration with MapEditor
    public void LoadFromMapData(MapData mapData)
    {
        if (mapData == null) return;
        
        // Clear existing tiles
        ClearArea(new BoundsInt(-100, -100, 0, 200, 200, 1));
        
        // Apply tile data
        foreach (var tileEntry in mapData.tileData)
        {
            SetCell(tileEntry.position, tileEntry.tileType, tileEntry.layer);
        }
        
        RefreshDisplayTilemap();

    }
    
    public void SaveToMapData(MapData mapData)
    {
        if (mapData == null) return;
        
        mapData.tileData.Clear();
        
        // Scan all tiles and save to map data
        for (int x = -50; x < 50; x++)
        {
            for (int y = -50; y < 50; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                ExtendedTileType tileType = GetTileTypeAt(pos);
                
                if (tileType != ExtendedTileType.None)
                {
                    mapData.tileData.Add(new TileDataEntry
                    {
                        position = pos,
                        tileType = tileType,
                        layer = 0 // TODO: Support multiple layers
                    });
                }
            }
        }
        

    }
    
    // Utility methods
    public bool HasTileLibraryFor(ExtendedTileType tileType)
    {
        return placeholderTiles.ContainsKey(tileType);
    }
    
    public void RegisterTileLibrary(TileLibrary library)
    {
        if (library != null)
        {
            InitializeTileLibrary(library);
        }
    }
}

[System.Serializable]
public class TileLibrary
{
    [Header("Tile Type")]
    public ExtendedTileType tileType;
    
    [Header("Tiles")]
    public Tile placeholderTile;
    public Tile[] autoTileSet = new Tile[16]; // 16 tiles for autotiling
    
    [Header("Properties")]
    public bool hasCollision = false;
    public bool isWalkable = true;
    public float movementSpeedModifier = 1f;
    
    [Header("Visual")]
    public Color tintColor = Color.white;
    public Sprite previewSprite;
    
    [Header("Audio")]
    public AudioClip stepSound;
    public AudioClip interactSound;
}
} // namespace SowurShield.MapEditor