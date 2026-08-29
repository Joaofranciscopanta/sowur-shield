using UnityEngine;

namespace SowurShield.Core
{

/// <summary>
/// Draws a sprite in front of whatever stands further up the screen, so a character walking
/// below a tree covers its trunk instead of being swallowed by it.
///
/// The scene shipped with all 144 sprites on the Default sorting layer at hand-picked orders
/// 0-5, which is a fixed stack: a tree at order 5 drew over the player at order 3 no matter
/// where either of them stood. The eight sorting layers that Phase 1 of the visual audit
/// created were never assigned to anything.
///
/// This converts world Y into sortingOrder rather than switching the camera to
/// <c>TransparencySortMode.CustomAxis</c>. The camera setting would sort *everything* it
/// renders, including the minimap icons that sit at orders 100-130 and the fog at 50, and
/// those rely on their fixed stack. A per-object component only touches what it is put on.
///
/// <para><b>The pivot is the anchor.</b> Sorting uses the sprite's transform position, so a
/// sprite pivoted at its centre sorts by its middle and appears to sink into objects it should
/// stand in front of. Pivot standing sprites at their feet.</para>
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class YSortSprite : MonoBehaviour
{
    [Tooltip("Sprites higher up the screen get a lower order. 100 keeps a whole 200-unit " +
             "map inside the band below, at 1cm of vertical resolution.")]
    [SerializeField] private float precision = 100f;

    [Tooltip("Leave on for anything that moves. Static scenery only needs sorting once, and " +
             "an Update that never changes anything is wasted on 100+ objects.")]
    [SerializeField] private bool continuous = true;

    [Tooltip("Nudges the sort point up or down without moving the object. Use it for art " +
             "with empty space under it, or to force a prop to read as behind its neighbours.")]
    [SerializeField] private float yOffset;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (continuous) Apply();
    }

    /// <summary>
    /// Recomputes the sorting order from the current position. Public so a script that
    /// teleports a static object can resort it without turning <see cref="continuous"/> on.
    /// </summary>
    public void Apply()
    {
        // Resolve lazily rather than trusting Awake. An editor tool that adds this component
        // from script gets no Awake or OnEnable callback until play mode, so a cached-in-Awake
        // reference is still null when the tool calls Apply() -- and the null guard below then
        // silently did nothing, leaving every order at its old hand-picked value.
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        // The bottom of the drawn sprite, which is where the object visually meets the
        // ground, whatever its pivot. Falls back to the transform if the sprite is missing.
        float y = (spriteRenderer.sprite != null ? spriteRenderer.bounds.min.y
                                                 : transform.position.y) + yOffset;

        int order = -Mathf.RoundToInt(y * precision);

        // Skip the assignment when the result would not change: sortingOrder is a native
        // setter that dirties the renderer, and this runs every frame on every character.
        // Compare the computed order rather than the input Y -- caching Y alone made Apply()
        // a no-op whenever something else had reset sortingOrder underneath us, which is
        // exactly what leaving play mode does to a scene object.
        if (order == spriteRenderer.sortingOrder) return;

        spriteRenderer.sortingOrder = order;
    }
}

}
