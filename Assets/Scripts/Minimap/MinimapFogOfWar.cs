using UnityEngine;
using SowurShield.Core;

namespace SowurShield.Minimap
{

/// <summary>
/// Hides the parts of the map the player has never walked near, and reveals them permanently as
/// they explore.
///
/// The minimap shows the whole farm from the first second of a new game, which gives away the
/// shape of the world before the player has seen any of it and leaves the map with nothing to
/// earn. Terraria and Don't Starve both make the map a record of where you have been; that turns
/// walking around into progress you can look at.
///
/// Implemented as a second texture drawn over the painted ground: fully opaque where unexplored,
/// transparent where revealed, with a soft edge so the frontier does not look like a stencil.
/// Revealing writes into a low-resolution mask (one texel covers a good fraction of a tile) —
/// exploration is coarse by nature and a 128px mask costs almost nothing to update or store.
///
/// The mask persists per save slot. Without that, every load would re-fog ground the player had
/// already cleared, which reads as the game forgetting.
/// </summary>
[DefaultExecutionOrder(-45)]
public class MinimapFogOfWar : MonoBehaviour, ISaveable
{
    [Header("Enable")]
    [Tooltip("Turn off to show the whole map from the start.")]
    [SerializeField] private bool fogEnabled = true;

    [Header("Reveal")]
    [Tooltip("Radius around the player revealed as they walk, in world units.")]
    [SerializeField] private float revealRadius = 7f;

    [Tooltip("How much of the outer edge fades rather than cutting hard, 0..1 of the radius.")]
    [Range(0f, 1f)]
    [SerializeField] private float edgeSoftness = 0.45f;

    [Tooltip("Seconds between reveal passes. Walking speed makes anything under ~0.1s wasted work.")]
    [SerializeField] private float revealInterval = 0.15f;

    [Header("Appearance")]
    [Tooltip("Colour of unexplored ground.")]
    [SerializeField] private Color fogColor = new Color(0.13f, 0.15f, 0.17f, 1f);

    [Tooltip("Mask resolution. Exploration is coarse; 128 is plenty and keeps saves small.")]
    [SerializeField] private int maskResolution = 128;

    [Header("Rendering")]
    [Tooltip("Must sort above the ground (0) and below the markers (100+).")]
    [SerializeField] private int fogSortingOrder = 50;

    // Runtime
    private GameObject fogObject;
    private SpriteRenderer fogRenderer;
    private Texture2D fogTexture;
    private Color32[] fogPixels;
    private Bounds worldBounds;
    private bool built = false;
    private bool dirty = false;
    private float nextRevealTime;
    private Transform player;

    // Alpha 255 = fully fogged, 0 = fully revealed. Stored as the alpha channel of fogPixels.
    private const byte Fogged = 255;

    /// <summary>Side length of the placeholder area used when the world cannot be measured.</summary>
    private const float FallbackWorldSize = 64f;

    private const string SaveKeyMask = "minimap_fog_mask";
    private const string SaveKeyResolution = "minimap_fog_res";

    // ============================================================================
    // LIFECYCLE
    // ============================================================================

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;

        // Registering in Start rather than Awake is deliberate and still safe: SaveManager catches
        // up objects that register after its initial load. Awake would run before the terrain
        // painter has measured the world, and the fog must match its bounds exactly.
        SaveManager.Instance?.RegisterSaveable(this);

        // One frame later, so MinimapTerrainPainter has produced the ground this fog covers.
        StartCoroutine(BuildNextFrame());
    }

    private void OnEnable()
    {
        // A neblina e dimensionada pelos bounds do painter. Quando o mundo e editado e o
        // chao redesenhado, esses bounds podem crescer (um predio colocado longe alarga a
        // medicao) — sem reconstruir, a neblina ficaria deslocada do mapa que cobre.
        // Build() preserva a mascara ja revelada, entao isto nao devolve o jogador ao
        // escuro: so realinha.
        // OnWorldChanged, nao OnRepainted: com o minimapa como camera aerea o painter esta
        // desligado e nunca repinta, mas o mundo continua a mudar quando se edita o mapa.
        MinimapTerrainPainter.OnWorldChanged += HandleTerrainRepainted;
    }

    private void OnDisable()
    {
        MinimapTerrainPainter.OnWorldChanged -= HandleTerrainRepainted;
    }

    private void HandleTerrainRepainted()
    {
        if (built) Build();
    }

    private void OnDestroy()
    {
        SaveManager.Instance?.UnregisterSaveable(this);

        if (fogTexture != null) Destroy(fogTexture);
        if (fogObject != null) Destroy(fogObject);
    }

    private System.Collections.IEnumerator BuildNextFrame()
    {
        yield return null;
        Build();
    }

    private void Update()
    {
        if (!built || !fogEnabled || player == null)
            return;

        if (Time.time < nextRevealTime)
            return;

        nextRevealTime = Time.time + Mathf.Max(0.05f, revealInterval);

        RevealAround(player.position);

        if (dirty)
        {
            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false);
            dirty = false;
        }
    }

    // ============================================================================
    // BUILD
    // ============================================================================

    private void Build()
    {
        if (!TryMeasureWorld(out worldBounds))
        {
            // No terrain painter and no minimap camera to measure against — a scene that has not
            // set the minimap up, or a test host. Bailing out entirely left fogPixels null, so
            // RevealAll/GetExploredFraction silently did nothing and reported 0% explored, which
            // is indistinguishable from "nothing has been explored yet".
            //
            // A fallback square is better than a null mask: the component stays queryable and
            // save/load keeps working, and a real Build() replaces these bounds as soon as the
            // painter reports in.
            worldBounds = new Bounds(Vector3.zero, new Vector3(FallbackWorldSize, FallbackWorldSize, 1f));
        }

        int res = Mathf.Clamp(maskResolution, 32, 512);

        // Preserve an already-loaded mask (a save may have restored one before Build ran).
        bool hadMask = fogPixels != null && fogPixels.Length == res * res;

        if (!hadMask)
        {
            fogPixels = new Color32[res * res];
            var start = (Color32)fogColor;
            start.a = fogEnabled ? Fogged : (byte)0;
            for (int i = 0; i < fogPixels.Length; i++) fogPixels[i] = start;
        }

        if (fogTexture == null || fogTexture.width != res)
        {
            if (fogTexture != null) Destroy(fogTexture);
            fogTexture = new Texture2D(res, res, TextureFormat.RGBA32, false);
            fogTexture.name = "MinimapFog";
            fogTexture.wrapMode = TextureWrapMode.Clamp;
            fogTexture.filterMode = FilterMode.Bilinear; // soft frontier for free
        }

        fogTexture.SetPixels32(fogPixels);
        fogTexture.Apply(false);

        if (fogObject == null)
        {
            fogObject = new GameObject("MinimapFog");
            fogObject.transform.SetParent(transform, false);

            // Gerado, nao autorado: sem DontSave ficaria gravado na cena se esta for salva
            // durante o Play Mode, e reapareceria dimensionado para o mundo antigo.
            fogObject.hideFlags = HideFlags.DontSave;

            // The icon layer, NOT MinimapTerrain. MinimapTerrain is deliberately shared with the
            // main camera so the ground reads on both (see MinimapCamera.MinimapTerrainLayerName);
            // putting the fog there meant the main camera drew it too, and a ~30x31 unit dark
            // sprite at sortingOrder 50 covered most of the play area. It looked like night
            // lighting, so nothing was ever reported as broken. Fog is a minimap artefact and
            // belongs on the layer only the minimap camera renders.
            int iconLayer = LayerMask.NameToLayer(MinimapCamera.MinimapIconLayerName);
            if (iconLayer >= 0) fogObject.layer = iconLayer;

            fogRenderer = fogObject.AddComponent<SpriteRenderer>();
        }

        // z MUST be 0 — a sprite pushed back in z is culled outright by 2D transparency sorting,
        // which is what silently deleted the painted ground before this was understood. Depth
        // ordering comes from sortingOrder alone.
        float ppu = res / Mathf.Max(worldBounds.size.x, 0.001f);
        var sprite = Sprite.Create(fogTexture, new Rect(0, 0, res, res),
                                   new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        sprite.name = "MinimapFogSprite";

        var previous = fogRenderer.sprite;
        fogRenderer.sprite = sprite;
        fogRenderer.sortingOrder = Mathf.Max(0, fogSortingOrder);
        if (previous != null && previous != sprite) Destroy(previous);

        fogObject.transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, 0f);

        float spriteWorldWidth = res / ppu;
        float scaleY = spriteWorldWidth > 0f ? worldBounds.size.y / spriteWorldWidth : 1f;
        fogObject.transform.localScale = new Vector3(1f, scaleY, 1f);

        fogRenderer.enabled = fogEnabled;
        built = true;

        // Clear whatever is under the player right now, so a new game does not start with the
        // player themselves sitting inside a black patch.
        if (fogEnabled && player != null)
        {
            RevealAround(player.position);
            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false);
            dirty = false;
        }
    }

    /// <summary>
    /// Matches <see cref="MinimapTerrainPainter"/>'s measurement so fog and ground line up.
    /// A mismatch here would offset the fog against the map it covers.
    /// </summary>
    private bool TryMeasureWorld(out Bounds bounds)
    {
        bounds = new Bounds();

        var painter = GetComponent<MinimapTerrainPainter>();
        if (painter != null && painter.TryGetPaintedBounds(out bounds))
            return true;

        // Fall back to the camera's own world measurement.
        var cam = FindFirstObjectByType<MinimapCamera>();
        return cam != null && cam.TryGetWorldBounds(out bounds);
    }

    // ============================================================================
    // REVEAL
    // ============================================================================

    /// <summary>
    /// Clears fog in a soft-edged circle. Only ever lowers alpha, so revealed ground can never
    /// re-fog — exploration is a ratchet.
    /// </summary>
    public void RevealAround(Vector3 worldPosition)
    {
        if (fogPixels == null || fogTexture == null) return;

        int res = fogTexture.width;
        float unitsPerPixelX = worldBounds.size.x / res;
        float unitsPerPixelY = worldBounds.size.y / res;
        if (unitsPerPixelX <= 0f || unitsPerPixelY <= 0f) return;

        float cx = (worldPosition.x - worldBounds.min.x) / unitsPerPixelX;
        float cy = (worldPosition.y - worldBounds.min.y) / unitsPerPixelY;

        float rx = revealRadius / unitsPerPixelX;
        float ry = revealRadius / unitsPerPixelY;

        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
        int maxX = Mathf.Min(res - 1, Mathf.CeilToInt(cx + rx));
        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
        int maxY = Mathf.Min(res - 1, Mathf.CeilToInt(cy + ry));

        float inner = Mathf.Clamp01(1f - edgeSoftness);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x - cx) / Mathf.Max(rx, 0.0001f);
                float dy = (y - cy) / Mathf.Max(ry, 0.0001f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 1f) continue;

                // Fully clear inside `inner`, fading to untouched at the rim.
                float clear = d <= inner ? 1f : Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(inner, 1f, d));

                int i = y * res + x;
                byte current = fogPixels[i].a;
                byte target = (byte)Mathf.RoundToInt(Mathf.Lerp(current, 0f, clear));

                if (target < current)
                {
                    var p = fogPixels[i];
                    p.a = target;
                    fogPixels[i] = p;
                    dirty = true;
                }
            }
        }
    }

    /// <summary>Clear the entire map — for debugging, or a "reveal map" item.</summary>
    [ContextMenu("Reveal Entire Map")]
    public void RevealAll()
    {
        if (fogPixels == null) return;

        for (int i = 0; i < fogPixels.Length; i++)
        {
            var p = fogPixels[i];
            p.a = 0;
            fogPixels[i] = p;
        }

        if (fogTexture != null)
        {
            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false);
        }
        dirty = false;
    }

    /// <summary>Fraction of the map explored, 0..1. Useful for a completion stat.</summary>
    public float GetExploredFraction()
    {
        if (fogPixels == null || fogPixels.Length == 0) return 0f;

        int clear = 0;
        for (int i = 0; i < fogPixels.Length; i++)
            if (fogPixels[i].a < 128) clear++;

        return (float)clear / fogPixels.Length;
    }

    // ============================================================================
    // SAVE / LOAD
    // ============================================================================

    /// <summary>
    /// The mask is stored as one Base64 string of its alpha channel — one byte per texel, so a
    /// 128px mask is 16KB raw and well under that once Base64'd and JSON-compressed. Storing RGB
    /// too would triple it for no gain: the fog colour is a setting, not per-texel data.
    /// </summary>
    public void SaveData(GameData gameData)
    {
        // Keyed off the mask alone, not the texture: the mask is the data, and a scene where the
        // texture failed to build should still persist whatever exploration it recorded.
        if (fogPixels == null || fogPixels.Length == 0) return;

        int res = Mathf.RoundToInt(Mathf.Sqrt(fogPixels.Length));
        var alpha = new byte[fogPixels.Length];
        for (int i = 0; i < fogPixels.Length; i++) alpha[i] = fogPixels[i].a;

        gameData.worldData.worldStrings[SaveKeyMask] = System.Convert.ToBase64String(alpha);
        gameData.worldData.worldCounters[SaveKeyResolution] = res;
    }

    public void LoadData(GameData gameData)
    {
        if (!gameData.worldData.worldStrings.ContainsKey(SaveKeyMask))
            return;

        int savedRes = gameData.worldData.worldCounters.ContainsKey(SaveKeyResolution)
            ? gameData.worldData.worldCounters[SaveKeyResolution]
            : 0;

        // A mask saved at a different resolution cannot be reinterpreted safely — dropping it
        // re-fogs the map, which is worse than ignoring the setting change, so it is kept only
        // when the sizes agree.
        if (savedRes <= 0 || savedRes != Mathf.Clamp(maskResolution, 32, 512))
            return;

        byte[] alpha;
        try { alpha = System.Convert.FromBase64String(gameData.worldData.worldStrings[SaveKeyMask]); }
        catch (System.FormatException) { return; }

        if (alpha.Length != savedRes * savedRes)
            return;

        if (fogPixels == null || fogPixels.Length != alpha.Length)
        {
            fogPixels = new Color32[alpha.Length];
            for (int i = 0; i < fogPixels.Length; i++) fogPixels[i] = fogColor;
        }

        for (int i = 0; i < alpha.Length; i++)
        {
            var p = fogPixels[i];
            p.a = alpha[i];
            fogPixels[i] = p;
        }

        if (fogTexture != null && fogTexture.width == savedRes)
        {
            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false);
        }
    }
}

} // namespace SowurShield.Minimap
