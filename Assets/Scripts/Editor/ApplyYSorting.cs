using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Editor
{

/// <summary>
/// Puts <see cref="YSortSprite"/> on the scene sprites that stand on the ground, so they
/// occlude each other by depth instead of by a fixed order.
///
/// The scene had all 144 sprites on the Default layer at hand-picked orders 0-5. That is a
/// fixed stack: the player at order 3 was drawn under every tree at order 5 regardless of who
/// stood in front. The eight sorting layers the audit's Phase 1 created were never assigned.
///
/// <para>Two groups are deliberately left alone:</para>
/// <list type="bullet">
/// <item>Anything on the <c>Minimap</c> or <c>MinimapTerrain</c> layer. Those icons rely on a
/// fixed stack at orders 50-130, and the Main Camera does not render them anyway.</item>
/// <item>Flat things that live on the floor -- the cursor, trigger zones, fishing spots,
/// dropped items. A footprint has nothing to stand in front of, and giving it a Y order only
/// risks it flickering above the character walking over it.</item>
/// </list>
///
/// Menu: Sowur Shield > Rendering > Apply Y Sorting
/// </summary>
public static class ApplyYSorting
{
    /// <summary>
    /// Objects that lie flat on the ground and must keep their fixed order. Matched as a
    /// substring of the GameObject name, case-insensitively.
    /// </summary>
    private static readonly string[] FlatNames =
    {
        "Cursor", "TriggerZone", "FishingSpot", "Blanket", "Square",
        "MinimapGround", "MinimapFog",
        // Ambient overlays: a bird in flight, a cat curled on the floor, drifting smoke and
        // water sparkle. They are named individually rather than caught by height, because
        // "Decor" also covers HomeTreeDecor, which is a 2.37-unit tree that must sort.
        "CatDecor", "BirdDecor", "WaterDecor", "SmokeDecor",
    };

    /// <summary>
    /// Sprites shorter than this are treated as ground markings rather than standing objects.
    /// Height, not a name, decides it: "Decor" covers both a 0.18-unit bird and the 2.37-unit
    /// HomeTreeDecor, and excluding the tree by name left the player drawing through it.
    ///
    /// <para>Kept low on purpose. At 0.5 this silently skipped 25 sprites that genuinely do
    /// need sorting -- chicks (0.44), eggs (0.40), small stones (0.495) and dropped tools
    /// (0.17) all stand on the ground and can be walked behind. Only the ambient overlays that
    /// are painted flat on the floor belong below the line.</para>
    /// </summary>
    private const float StandingHeight = 0.16f;

    private static readonly string[] SkipLayers = { "Minimap", "MinimapTerrain" };

    [MenuItem("Sowur Shield/Rendering/Apply Y Sorting")]
    public static void Apply()
    {
        var renderers = Object.FindObjectsByType<SpriteRenderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        int added = 0, updated = 0, skipped = 0, removed = 0;
        var touched = new List<string>();

        foreach (SpriteRenderer sr in renderers)
        {
            if (!ShouldSort(sr))
            {
                // Strip a component left by an earlier run with different rules, so the scene
                // cannot drift into objects that carry a sorter the tool no longer maintains.
                var stale = sr.GetComponent<YSortSprite>();
                if (stale != null) { Undo.DestroyObjectImmediate(stale); removed++; }
                skipped++;
                continue;
            }

            var sorter = sr.GetComponent<YSortSprite>();
            if (sorter == null)
            {
                sorter = Undo.AddComponent<YSortSprite>(sr.gameObject);
                added++;
                touched.Add(sr.gameObject.name);
            }
            else updated++;

            // Static scenery does not need to re-sort every frame; only things that move do.
            // Getting this wrong costs a LateUpdate on ~90 objects that never change.
            var so = new SerializedObject(sorter);
            so.FindProperty("continuous").boolValue = MovesAtRuntime(sr.gameObject);
            so.ApplyModifiedProperties();

            // Apply() writes sortingOrder, then the write is registered as a prefab property
            // override -- most of these renderers are prefab instances, and without that Unity
            // reverts them to the prefab's value.
            //
            // Deliberately NOT wrapped in Undo.RecordObject: recording the renderer snapshots
            // the pre-change sortingOrder, and closing the menu item's undo group restored it,
            // so 26 of the 90 sprites silently kept their old hand-picked orders across three
            // runs of this tool. Adding the component is still undoable; the order is not.
            sorter.Apply();
            if (PrefabUtility.IsPartOfPrefabInstance(sr))
                PrefabUtility.RecordPrefabInstancePropertyModifications(sr);

            EditorUtility.SetDirty(sr);
            EditorUtility.SetDirty(sr.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[ApplyYSorting] {added} added, {updated} already had it, {skipped} left " +
                  $"alone (minimap + flat ground). Save the scene.\n" +
                  string.Join(", ", touched.Take(40)));
    }

    private static bool ShouldSort(SpriteRenderer sr)
    {
        if (sr == null || sr.sprite == null) return false;

        string layer = LayerMask.LayerToName(sr.gameObject.layer);
        if (SkipLayers.Contains(layer)) return false;

        string name = sr.gameObject.name;
        foreach (string flat in FlatNames)
            if (name.IndexOf(flat, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

        // Too short to stand in front of anything, so a Y order would only let it flicker
        // above the character walking over it.
        if (sr.bounds.size.y < StandingHeight) return false;

        return true;
    }

    /// <summary>
    /// True for anything that can change position while the game runs. Everything else is
    /// scenery that only needs sorting once, when the component is enabled.
    /// </summary>
    private static bool MovesAtRuntime(GameObject go)
    {
        if (go.CompareTag("Player")) return true;
        if (go.GetComponent<Rigidbody2D>() != null) return true;

        // Animals wander, NPCs can be walked around, dropped items are spawned mid-play.
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            string t = mb.GetType().Name;
            if (t == "Animal" || t == "PlayerMove" || t == "GroundItem"
                || t.Contains("NPC") || t.Contains("Enemy"))
                return true;
        }
        return false;
    }
}

}
