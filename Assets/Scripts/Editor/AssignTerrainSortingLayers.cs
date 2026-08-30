using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SowurShield.Editor
{

/// <summary>
/// Moves the ground tilemaps onto the Ground sorting layer, so nothing standing on the world
/// can be drawn underneath it.
///
/// <para><b>The bug this fixes.</b> DisplayTilemap and PlaceholderTilemap sat on the Default
/// sorting layer at orders 0 and -1 -- the same layer as every character and prop. That was
/// harmless while sprites used hand-picked orders 1-5, because all of them were above 0. Once
/// <c>YSortSprite</c> started deriving order from world position, anything north of the map's
/// origin got a negative order and fell behind the ground: 47 of the 87 sorted sprites,
/// including the player, who simply vanished when walking up the map.</para>
///
/// <para>Sorting layers, not orders, are the right tool here. Bumping the tilemap to a large
/// negative order would work until someone walked far enough north to beat it, and the eight
/// layers created by Phase 1 of the visual audit exist precisely so ground, objects and
/// characters never have to compete on the same axis. Ground is -2 against Default's 0, so
/// every Y-sorted sprite now wins regardless of its order.</para>
///
/// Menu: Sowur Shield > Rendering > Assign Terrain Sorting Layers
/// </summary>
public static class AssignTerrainSortingLayers
{
    private const string GroundLayer = "Ground";

    /// <summary>
    /// Tilemaps that make up the walkable ground. Order within the layer is preserved so the
    /// display tilemap keeps drawing over its placeholder.
    /// </summary>
    private static readonly string[] GroundTilemaps = { "DisplayTilemap", "PlaceholderTilemap" };

    [MenuItem("Sowur Shield/Rendering/Assign Terrain Sorting Layers")]
    public static void Assign()
    {
        if (SortingLayer.layers.All(l => l.name != GroundLayer))
        {
            Debug.LogError($"[AssignTerrainSortingLayers] No '{GroundLayer}' sorting layer. " +
                           "Create it in Project Settings > Tags and Layers first.");
            return;
        }

        int moved = 0;

        foreach (TilemapRenderer renderer in Object.FindObjectsByType<TilemapRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!GroundTilemaps.Contains(renderer.gameObject.name)) continue;

            // No Unity-layer exclusion here on purpose. Both ground tilemaps sit on the
            // MinimapTerrain layer, which the Main Camera and the minimap camera BOTH render
            // -- one tilemap deliberately serves as the world's floor and the minimap's. An
            // earlier version of this tool skipped that layer and so skipped the very objects
            // it was written to fix. The named list above is what limits the scope.

            if (renderer.sortingLayerName == GroundLayer) continue;

            // Deliberately no Undo.RecordObject: recording an object whose property this tool
            // then writes snapshots the old value, and closing the menu item's undo group
            // restores it. That silently reverted 26 of 90 writes in ApplyYSorting.
            renderer.sortingLayerName = GroundLayer;

            if (PrefabUtility.IsPartOfPrefabInstance(renderer))
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);

            EditorUtility.SetDirty(renderer);
            moved++;

            Debug.Log($"[AssignTerrainSortingLayers] {renderer.gameObject.name} -> " +
                      $"{GroundLayer} (order {renderer.sortingOrder} kept)");
        }

        if (moved > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[AssignTerrainSortingLayers] {moved} tilemap(s) moved to '{GroundLayer}'. " +
                  "Save the scene.");
    }
}

}
