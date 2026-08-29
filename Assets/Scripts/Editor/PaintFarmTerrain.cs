using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using SowurShield.Farming;

namespace SowurShield.Editor
{

/// <summary>
/// Paints the farm's dirt areas onto the dual-grid placeholder tilemap.
///
/// The world shipped as a single tile — TilesDemo_6, the all-grass case — repeated 10,201
/// times, because nothing was ever painted onto PlaceholderTilemap. The dual-grid system
/// itself was complete the whole time: 16 rule tiles wired up, grass and dirt placeholders
/// assigned. It just had no input, so every cell resolved to "grass on all four corners"
/// and the farm read as a flat green rectangle.
///
/// This is deliberately a tool rather than a runtime script. Terrain is level design: it
/// should live in the scene file, be visible in the editor, and be editable by hand
/// afterwards. Running it again re-paints the same layout, so it is safe to re-run after
/// tweaking the shapes below.
///
/// Layout follows what is actually in the scene (positions read from SampleScene):
///   bed (-1.8, 1.7) and selling box (-1.4, 5.5)  -> homestead yard
///   feeding trough (6.0, 0.5), animals around x 7..10  -> paddock
///   NPCs spread from (-8,-4) to (9,3)  -> paths connect the two hubs
///
/// Menu: Sowur Shield > Terrain > Paint Farm Terrain
/// </summary>
public static class PaintFarmTerrain
{
    [MenuItem("Sowur Shield/Terrain/Paint Farm Terrain")]
    public static void Paint()
    {
        var grid = Object.FindObjectOfType<DualGridTilemap>();
        if (grid == null)
        {
            Debug.LogError("[PaintFarmTerrain] No DualGridTilemap in the open scene. " +
                           "Open SampleScene first.");
            return;
        }

        if (grid.placeholderTilemap == null || grid.dirtPlaceholderTile == null)
        {
            Debug.LogError("[PaintFarmTerrain] DualGridTilemap is missing its placeholder " +
                           "tilemap or dirt tile reference.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(grid.placeholderTilemap.gameObject,
                                             "Paint farm terrain");

        // Start from a clean slate so re-running does not accumulate old shapes.
        grid.placeholderTilemap.ClearAllTiles();

        int painted = 0;

        // ── Homestead yard: bed at (-1.8, 1.7), selling box at (-1.4, 5.5) ───────────
        painted += FillRect(grid, -5, 0, 2, 7);

        // ── Paddock: feeding trough at (6.0, 0.5), livestock spread x 7..10, y -6..4 ─
        painted += FillRect(grid, 5, -7, 11, 5);

        // ── Paths ───────────────────────────────────────────────────────────────────
        // Homestead -> paddock, along the row the player already walks.
        painted += FillRect(grid, 2, 0, 5, 2);
        // South spur towards Rui (-2,-6), Bento (2,-8) and the animal market (2,-3).
        painted += FillRect(grid, 0, -8, 2, 0);
        // West spur towards Isabela (-6,2) and Elias (-8,-4).
        painted += FillRect(grid, -9, 1, -5, 3);

        // ── A few clearings, so the grass is not one unbroken sheet ─────────────────
        painted += FillRect(grid, -11, 8, -8, 11);   // north-west
        painted += FillRect(grid, 8, 7, 12, 10);     // north-east
        painted += FillRect(grid, -12, -12, -9, -9); // south-west

        // DualGridTilemap builds its neighbour->tile rule table in Start(), which never runs
        // in edit mode, so RefreshDisplayTilemap() would dereference a null dictionary here.
        // Build the same table first.
        EnsureRuleTable(grid);

        grid.RefreshDisplayTilemap();

        EditorUtility.SetDirty(grid.placeholderTilemap);
        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);

        Debug.Log($"[PaintFarmTerrain] Painted {painted} dirt cells. " +
                  "Save the scene to keep it.");
    }

    /// <summary>
    /// Mirrors the rule table DualGridTilemap.Start() builds, so the display tilemap can be
    /// resolved from the editor. Kept in the same tile-index order as the runtime version;
    /// if that table changes, this one has to change with it.
    /// </summary>
    private static void EnsureRuleTable(DualGridTilemap grid)
    {
        var field = typeof(DualGridTilemap).GetField(
            "neighbourTupleToTile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (field == null || field.GetValue(null) != null)
            return;

        var t = grid.tiles;
        var table = new System.Collections.Generic.Dictionary<
            System.Tuple<TileType, TileType, TileType, TileType>, Tile>
        {
            { Key(TileType.Grass, TileType.Grass, TileType.Grass, TileType.Grass), t[6] },
            { Key(TileType.Dirt,  TileType.Dirt,  TileType.Dirt,  TileType.Grass), t[13] },
            { Key(TileType.Dirt,  TileType.Dirt,  TileType.Grass, TileType.Dirt),  t[0] },
            { Key(TileType.Dirt,  TileType.Grass, TileType.Dirt,  TileType.Dirt),  t[8] },
            { Key(TileType.Grass, TileType.Dirt,  TileType.Dirt,  TileType.Dirt),  t[15] },
            { Key(TileType.Dirt,  TileType.Grass, TileType.Dirt,  TileType.Grass), t[1] },
            { Key(TileType.Grass, TileType.Dirt,  TileType.Grass, TileType.Dirt),  t[11] },
            { Key(TileType.Dirt,  TileType.Dirt,  TileType.Grass, TileType.Grass), t[3] },
            { Key(TileType.Grass, TileType.Grass, TileType.Dirt,  TileType.Dirt),  t[9] },
            { Key(TileType.Dirt,  TileType.Grass, TileType.Grass, TileType.Grass), t[5] },
            { Key(TileType.Grass, TileType.Dirt,  TileType.Grass, TileType.Grass), t[2] },
            { Key(TileType.Grass, TileType.Grass, TileType.Dirt,  TileType.Grass), t[10] },
            { Key(TileType.Grass, TileType.Grass, TileType.Grass, TileType.Dirt),  t[7] },
            { Key(TileType.Dirt,  TileType.Grass, TileType.Grass, TileType.Dirt),  t[14] },
            { Key(TileType.Grass, TileType.Dirt,  TileType.Dirt,  TileType.Grass), t[4] },
            { Key(TileType.Dirt,  TileType.Dirt,  TileType.Dirt,  TileType.Dirt),  t[12] },
        };

        field.SetValue(null, table);
    }

    private static System.Tuple<TileType, TileType, TileType, TileType> Key(
        TileType a, TileType b, TileType c, TileType d) =>
        new System.Tuple<TileType, TileType, TileType, TileType>(a, b, c, d);

    /// <summary>
    /// The placeholder tile that makes a cell read as Dirt — which is
    /// <c>grassPlaceholderTile</c>, not <c>dirtPlaceholderTile</c>.
    ///
    /// DualGridTilemap.getPlaceholderTileTypeAt is inverted relative to its field names:
    ///
    ///     if (placeholderTilemap.GetTile(coords) == grassPlaceholderTile)
    ///         return Dirt;
    ///     else
    ///         return Grass;
    ///
    /// So an empty cell and a cell painted with dirtPlaceholderTile both resolve to Grass,
    /// and only grassPlaceholderTile yields Dirt. Painting the obvious field produced 249
    /// placeholder tiles and zero visible change, because every cell still resolved to the
    /// all-grass rule. Renaming the fields would be the real fix, but they are serialized on
    /// the scene object, so this wraps the quirk instead of risking the references.
    /// </summary>
    private static Tile DirtMarker(DualGridTilemap grid) => grid.grassPlaceholderTile;

    /// <summary>
    /// Inclusive on both corners — the rectangles above read as map coordinates, so
    /// FillRect(-5, 0, 2, 7) covers x -5..2 and y 0..7.
    /// </summary>
    private static int FillRect(DualGridTilemap grid, int x0, int y0, int x1, int y1)
    {
        int count = 0;
        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                grid.placeholderTilemap.SetTile(new Vector3Int(x, y, 0), DirtMarker(grid));
                count++;
            }
        }
        return count;
    }
}

}
