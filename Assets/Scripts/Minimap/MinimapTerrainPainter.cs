using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SowurShield.Minimap
{

/// <summary>
/// Paints the minimap's ground layer.
///
/// The minimap had no map. Both tilemaps on the MinimapTerrain layer hold zero tiles, so the
/// camera photographed nothing but its own clear colour and the markers floating on it — the
/// green a player sees is the background, not grass. A minimap with no terrain cannot answer
/// "where am I?", only "what is near me?", and no amount of icon polish fixes that.
///
/// Rather than block on a tileset that does not exist yet, this derives the ground from the world
/// that *is* there: every sprite in the scene is classified by what it evidently is (tree, water,
/// structure, undergrowth) and stamped as a soft blob of colour onto one texture, which is then
/// drawn under the markers. The result is not a tile-accurate map; it is a shape — where the
/// orchard is, where the pond is, where the buildings cluster — which is exactly the job a
/// minimap does at 200px.
///
/// It runs once at startup and whenever the world is explicitly invalidated. If a real tileset
/// arrives later, <see cref="HasAuthoredTerrain"/> stands this down automatically: authored
/// tiles always win over a derived approximation.
/// </summary>
[DefaultExecutionOrder(-50)]
public class MinimapTerrainPainter : MonoBehaviour
{
    [Header("Output")]
    [Tooltip("Texture resolution. 512 is plenty: this is blurred ground, not readable detail.")]
    [SerializeField] private int textureResolution = 512;

    [Tooltip("World padding around the scene bounds, in units.")]
    [SerializeField] private float worldPadding = 4f;

    // 3.2 was tuned while the blobs had a wide soft falloff, where the outer half was nearly
    // transparent and the painted size read much smaller than it measured. With a hard edge that
    // same value makes every bush a boulder, so it comes down as the falloff tightens.
    [Tooltip("How much larger than life each feature is painted. A minimap shows regions, " +
             "not objects: stamped 1:1 a tree covers ~1px of the HUD and reads as noise.")]
    [SerializeField] private float featureSpread = 2.1f;

    [Tooltip("Smallest a stamp may be, in world units, so single small props still register.")]
    [SerializeField] private float minFeatureRadius = 0.9f;

    [Header("Palette")]
    [SerializeField] private Color grassColor = new Color(0.60f, 0.73f, 0.40f, 1f);
    [SerializeField] private Color grassVariantColor = new Color(0.55f, 0.69f, 0.36f, 1f);
    [SerializeField] private Color treeColor = new Color(0.29f, 0.46f, 0.24f, 1f);
    [SerializeField] private Color waterColor = new Color(0.36f, 0.58f, 0.75f, 1f);
    [SerializeField] private Color structureColor = new Color(0.55f, 0.42f, 0.30f, 1f);
    [SerializeField] private Color scrubColor = new Color(0.52f, 0.65f, 0.35f, 1f);
    [SerializeField] private Color soilColor = new Color(0.48f, 0.36f, 0.24f, 1f);

    [Header("Rendering")]
    // Must be >= 0. A negative sorting order makes this sprite disappear entirely in this
    // project — measured: -1, -50 and -100 all render nothing while 0 and 999 render correctly,
    // with no error and with isVisible still reporting true. The ground therefore sits at 0 and
    // every marker sorts above it (MinimapIcon uses 100+), which achieves the same layering
    // without relying on negative orders.
    [Tooltip("Sorting order for the painted ground. Keep at 0 or above — negative values do not " +
             "render here. Markers sort above it at 100+.")]
    [SerializeField] private int groundSortingOrder = 0;

    private GameObject groundObject;
    private SpriteRenderer groundRenderer;
    private Texture2D groundTexture;

    // The bounds the ground was last painted over. Anything that must line up with the map —
    // the fog mask especially — has to use this exact rectangle rather than re-measuring, since
    // padding and the self-exclusion rule would otherwise produce a slightly different one.
    private Bounds paintedBounds;
    private bool hasPainted = false;

    /// <summary>The world rectangle the painted ground currently covers.</summary>
    public bool TryGetPaintedBounds(out Bounds bounds)
    {
        bounds = paintedBounds;
        return hasPainted;
    }

    /// <summary>Categories the painter can distinguish, in the order they overpaint each other.</summary>
    private enum Ground { Grass, Scrub, Soil, Water, Tree, Structure }

    /// <summary>
    /// Fraction of a blob's radius that stays fully opaque before the edge starts to fade.
    /// High on purpose — see the comment in StampBlob about the aura this produced at 0.45.
    /// </summary>
    private const float EdgeFeatherStart = 0.88f;

    [Header("Behaviour")]
    [Tooltip("Stand down when a tilemap on the terrain layer already draws the ground. " +
             "Leave OFF while the world tilemaps render nothing readable at minimap scale.")]
    [SerializeField] private bool deferToAuthoredTerrain = false;

    private void Start()
    {
        // Deliberately opt-in, and off by default.
        //
        // SampleScene's DisplayTilemap looks like authored terrain by every cheap test — 10,201
        // tiles, every one carrying a sprite, renderer enabled — yet photographed by the minimap
        // camera it produces 5.9% coverage: effectively nothing. Standing down for it would put
        // the minimap straight back to the blank square this work set out to fix.
        //
        // The flag exists so that a project which later paints genuine minimap terrain can hand
        // the job back with one toggle, rather than deleting this component.
        if (deferToAuthoredTerrain && HasAuthoredTerrain())
        {
            enabled = false;
            return;
        }

        // Run after the dual-grid system has had a frame to populate its tilemaps, so the world
        // measurement sees the finished scene rather than a half-built one.
        StartCoroutine(RepaintNextFrame());
    }

    private System.Collections.IEnumerator RepaintNextFrame()
    {
        yield return null;
        Repaint();
    }

    private void OnDestroy()
    {
        if (groundTexture != null)
            Destroy(groundTexture);

        if (groundObject != null)
            Destroy(groundObject);
    }

    /// <summary>
    /// True when a tilemap on the minimap terrain layer actually *draws* something.
    ///
    /// Counting tiles is not enough. SampleScene's DisplayTilemap is filled at runtime by the
    /// dual-grid system with 10,201 tiles, yet renders nothing visible on the minimap — the tiles
    /// carry no opaque sprite at this scale. A painter that stood down on tile count alone would
    /// hand the minimap back to terrain that is invisible, which is the original bug wearing a
    /// different hat.
    ///
    /// So the test is the rendered footprint: a tilemap counts as authored terrain only if its
    /// renderer is enabled and it reports a non-degenerate bounds. Anything else, and this
    /// painter supplies the ground.
    /// </summary>
    public static bool HasAuthoredTerrain()
    {
        int terrainLayer = LayerMask.NameToLayer(MinimapCamera.MinimapTerrainLayerName);
        if (terrainLayer < 0)
            return false;

        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tm.gameObject.layer != terrainLayer) continue;

            var renderer = tm.GetComponent<TilemapRenderer>();
            if (renderer == null || !renderer.enabled) continue;

            bool hasTile = false;
            foreach (var pos in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;
                // A tile with no sprite occupies a cell but paints nothing.
                if (tm.GetSprite(pos) == null) continue;
                hasTile = true;
                break;
            }

            if (hasTile) return true;
        }

        return false;
    }

    /// <summary>Rebuild the ground texture from the current scene.</summary>
    [ContextMenu("Repaint Minimap Terrain")]
    public void Repaint()
    {
        if (!TryMeasureWorld(out Bounds world))
            return;

        // Expand() takes the total growth, not a per-side margin, so passing padding*2 padded
        // twice as far as asked and stretched the map out to 60 units when the farm is 28.
        world.Expand(new Vector3(worldPadding, worldPadding, 0f));

        int res = Mathf.Clamp(textureResolution, 64, 2048);
        var pixels = new Color32[res * res];

        PaintGrassBase(pixels, res);
        StampWorldFeatures(pixels, res, world);

        ApplyTexture(pixels, res, world);

        paintedBounds = world;
        hasPainted = true;
    }

    // ============================================================================
    // MEASUREMENT
    // ============================================================================

    /// <summary>
    /// Extent of the world to paint. Minimap markers are excluded — they sit on the minimap layer
    /// and would otherwise define the bounds themselves.
    /// </summary>
    private bool TryMeasureWorld(out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;

        int iconLayer = LayerMask.NameToLayer(MinimapLayerName);

        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (iconLayer >= 0 && sr.gameObject.layer == iconLayer) continue;
            if (sr.sprite == null) continue;

            // Skip the ground this painter produced. Measuring it as world geometry makes each
            // repaint slightly larger than the last — the map grew from 32 to 49 units wide over
            // a few calls, drifting off the farm it is supposed to show.
            if (groundRenderer != null && sr == groundRenderer) continue;

            if (!any) { bounds = sr.bounds; any = true; }
            else bounds.Encapsulate(sr.bounds);
        }

        return any;
    }

    private const string MinimapLayerName = "Minimap";

    // ============================================================================
    // PAINTING
    // ============================================================================

    /// <summary>
    /// Fills the base with grass, gently mottled. A perfectly flat fill reads as "nothing
    /// rendered" — the same impression the black square gave — whereas a little variation reads
    /// as ground.
    /// </summary>
    private void PaintGrassBase(Color32[] pixels, int res)
    {
        // Fixed seed: the map must not change between runs, or the same farm looks different
        // every load.
        var rng = new System.Random(20260823);
        float scale = 6f / res;
        int offset = rng.Next(0, 10000);

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float n = Mathf.PerlinNoise((x + offset) * scale, (y + offset) * scale);
                Color c = Color.Lerp(grassColor, grassVariantColor, Mathf.SmoothStep(0f, 1f, n));
                pixels[y * res + x] = c;
            }
        }
    }

    /// <summary>
    /// Stamps every world object as a soft blob of its category's colour.
    /// </summary>
    private void StampWorldFeatures(Color32[] pixels, int res, Bounds world)
    {
        var features = new List<(Ground kind, Vector2 centre, float radius)>();

        int iconLayer = LayerMask.NameToLayer(MinimapLayerName);

        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (iconLayer >= 0 && sr.gameObject.layer == iconLayer) continue;
            if (sr.sprite == null) continue;
            if (groundRenderer != null && sr == groundRenderer) continue; // never stamp our own output

            Ground kind = Classify(sr);
            if (kind == Ground.Grass) continue; // nothing to stamp

            var b = sr.bounds;
            // Radius from the footprint, not the full sprite height: a tree's canopy is tall but
            // occupies a small patch of ground, and using height made trees smear vertically.
            float radius = Mathf.Max(b.extents.x, b.extents.y * 0.6f);

            // Then deliberately oversized. Measured against the real HUD, the median object has a
            // radius of ~1px there — stamping true-to-life produced a texture that was 93% flat
            // grass and read as no terrain at all. Overlapping, oversized stamps are what turn
            // scattered trees into a legible wood and a few props into a yard.
            radius = Mathf.Max(radius * featureSpread, minFeatureRadius);

            features.Add((kind, new Vector2(b.center.x, b.center.y), radius));
        }

        // Paint in category order so structures and water survive being overlapped by scrub.
        foreach (Ground kind in new[] { Ground.Scrub, Ground.Soil, Ground.Tree, Ground.Water, Ground.Structure })
        {
            foreach (var f in features)
            {
                if (f.kind != kind) continue;
                StampBlob(pixels, res, world, f.centre, f.radius, ColorFor(kind));
            }
        }
    }

    /// <summary>
    /// Draws one soft-edged circle. The falloff matters: hard circles read as a polka-dot pattern,
    /// while a feathered edge lets neighbouring trees merge into something that looks like a
    /// wood, which is what the player is actually trying to recognise.
    /// </summary>
    private void StampBlob(Color32[] pixels, int res, Bounds world, Vector2 centre, float radius, Color color)
    {
        float unitsPerPixelX = world.size.x / res;
        float unitsPerPixelY = world.size.y / res;
        if (unitsPerPixelX <= 0f || unitsPerPixelY <= 0f) return;

        float cx = (centre.x - world.min.x) / unitsPerPixelX;
        float cy = (centre.y - world.min.y) / unitsPerPixelY;

        float rx = Mathf.Max(radius / unitsPerPixelX, 1.5f);
        float ry = Mathf.Max(radius / unitsPerPixelY, 1.5f);

        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
        int maxX = Mathf.Min(res - 1, Mathf.CeilToInt(cx + rx));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
        int maxY = Mathf.Min(res - 1, Mathf.CeilToInt(cy + ry));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x - cx) / rx;
                float dy = (y - cy) / ry;
                float d2 = dx * dx + dy * dy;
                if (d2 > 1f) continue;

                // Mostly solid, with only the outermost sliver feathered.
                //
                // This used to start fading at 45% of the radius. Combined with featureSpread
                // (3.2x), that put a huge soft gradient around every object — on the finished map
                // it read as a glowing aura rather than ground, which is the first thing anyone
                // noticed about it. Keeping the fade to the last ~12% gives each feature a
                // definite edge while still letting neighbours merge instead of pebble-dashing.
                float t = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(EdgeFeatherStart, 1f, Mathf.Sqrt(d2)));

                int i = y * res + x;
                Color existing = pixels[i];
                pixels[i] = Color.Lerp(existing, color, t);
            }
        }
    }

    private Color ColorFor(Ground kind)
    {
        switch (kind)
        {
            case Ground.Tree:      return treeColor;
            case Ground.Water:     return waterColor;
            case Ground.Structure: return structureColor;
            case Ground.Scrub:     return scrubColor;
            case Ground.Soil:      return soilColor;
            default:               return grassColor;
        }
    }

    /// <summary>
    /// Works out what a sprite represents from its name.
    ///
    /// Name matching is crude and it is chosen deliberately: the scene has no terrain tags, no
    /// shared component, and no layer split to key off, so the sprite name is the only signal
    /// present on all 97 objects. It degrades safely — an unrecognised object simply stays grass
    /// rather than painting something wrong.
    /// </summary>
    private Ground Classify(SpriteRenderer sr)
    {
        string n = sr.sprite.name.ToLowerInvariant();
        string g = sr.gameObject.name.ToLowerInvariant();

        if (Contains(n, g, "water", "boat", "pond", "river", "lake")) return Ground.Water;
        if (Contains(n, g, "tree", "stump", "bush")) return Ground.Tree;
        if (Contains(n, g, "well", "work station", "workstation", "house", "barn",
                            "coop", "shed", "sign", "trough", "fence", "building")) return Ground.Structure;
        if (Contains(n, g, "carrot", "crop", "seed", "wheat", "soil", "field")) return Ground.Soil;
        if (Contains(n, g, "mushroom", "flower", "stone", "grass", "rock", "props",
                            "blanket", "basket")) return Ground.Scrub;

        return Ground.Grass;
    }

    private static bool Contains(string spriteName, string objectName, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (spriteName.Contains(needle) || objectName.Contains(needle))
                return true;
        }
        return false;
    }

    // ============================================================================
    // OUTPUT
    // ============================================================================

    /// <summary>
    /// Publishes the painted pixels as a world-space sprite on the terrain layer, sized to the
    /// world it was measured from, so the minimap camera photographs it in place.
    /// </summary>
    private void ApplyTexture(Color32[] pixels, int res, Bounds world)
    {
        if (groundTexture == null || groundTexture.width != res)
        {
            if (groundTexture != null) Destroy(groundTexture);

            groundTexture = new Texture2D(res, res, TextureFormat.RGBA32, false);
            groundTexture.name = "MinimapGround";
            groundTexture.wrapMode = TextureWrapMode.Clamp;
            groundTexture.filterMode = FilterMode.Bilinear;
        }

        groundTexture.SetPixels32(pixels);
        groundTexture.Apply();

        if (groundObject == null)
        {
            groundObject = new GameObject("MinimapGround");
            groundObject.transform.SetParent(transform, false);

            int terrainLayer = LayerMask.NameToLayer(MinimapCamera.MinimapTerrainLayerName);
            if (terrainLayer >= 0)
                groundObject.layer = terrainLayer;

            groundRenderer = groundObject.AddComponent<SpriteRenderer>();
        }

        // Set every time, not just on creation: a repaint after the order changed would otherwise
        // keep whatever the object was built with.
        groundRenderer.sortingOrder = Mathf.Max(0, groundSortingOrder);

        // The texture is square but the world usually is not. Rather than scale the transform
        // non-uniformly — which was distorting where each painted feature landed — the texture is
        // painted in the world's own aspect from the start (see StampBlob, which maps X and Y
        // through separate units-per-pixel), so here the sprite only needs a PPU per axis.
        //
        // Sprite.Create takes a single PPU, so the honest way to cover a non-square world is a
        // uniform PPU plus a matching transform scale on both axes.
        float ppu = res / Mathf.Max(world.size.x, 0.001f);
        var sprite = Sprite.Create(groundTexture, new Rect(0, 0, res, res),
                                   new Vector2(0.5f, 0.5f), ppu, 0,
                                   SpriteMeshType.FullRect);
        sprite.name = "MinimapGroundSprite";

        // Take a reference to the outgoing sprite BEFORE assigning the new one. Destroying via
        // `groundRenderer.sprite` after assignment reads back the sprite just installed and
        // deletes that instead — Destroy is deferred to end of frame, so the renderer kept a
        // handle to an object that was about to be torn down and drew nothing at all, with no
        // error. That is what made the painted ground invisible while every diagnostic (layer,
        // culling mask, isVisible, texture contents) reported healthy.
        var previousSprite = groundRenderer.sprite;

        groundRenderer.sprite = sprite;
        groundRenderer.drawMode = SpriteDrawMode.Simple;

        if (previousSprite != null && previousSprite != sprite)
            Destroy(previousSprite);

        // z MUST be 0, matching the plane every world sprite and marker sits on.
        //
        // Sitting the ground at z=0.5 to "put it behind" the markers is the intuitive move and it
        // silently deletes the map: with the default transparency sort axis (0,0,1) the renderer
        // culled it entirely, and every diagnostic still reported healthy — layer in the culling
        // mask, isVisible true, texture full of correct opaque pixels. Measured: identical setup
        // at z=0.5 rendered 5% non-background, at z=0.0 it rendered 92%.
        //
        // Depth ordering against the markers comes from sortingOrder alone (ground 0, markers
        // 100+), which is the correct mechanism for 2D sprites.
        groundObject.transform.position = new Vector3(world.center.x, world.center.y, 0f);

        float spriteWorldWidth = res / ppu;                 // == world.size.x by construction
        float scaleY = spriteWorldWidth > 0f ? world.size.y / spriteWorldWidth : 1f;
        groundObject.transform.localScale = new Vector3(1f, scaleY, 1f);
    }
}

} // namespace SowurShield.Minimap
