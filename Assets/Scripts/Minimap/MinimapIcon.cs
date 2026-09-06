using UnityEngine;

namespace SowurShield.Minimap
{

/// <summary>
/// Component for GameObjects to appear on the minimap
/// Attach to any object that should be visible on the minimap (NPCs, SellBox, Beds, etc.)
///
/// AUTOMATIC MARKERS ARE OFF — see <see cref="DrawsMarker"/>. The minimap reads as a plain
/// aerial photograph of the farm: no player arrow, no NPC or animal diamonds, no building
/// squares. The player's own pins are the one exception and still draw.
///
/// The component is deliberately still present and still runs even for the types that draw
/// nothing, because it does a second job the minimap depends on: taking the Minimap layer out
/// of every gameplay camera's culling mask. Delete these components and the painted minimap
/// ground starts rendering over the game world.
/// </summary>
public class MinimapIcon : MonoBehaviour
{
    /// <summary>
    /// Whether this icon draws a symbol on the minimap.
    ///
    /// Changed on 2026-09-06: the minimap is an aerial view of the farm, so the markers the
    /// *game* places (player, NPCs, animals, buildings, bed, sell box) draw nothing. Markers
    /// the *player* places — the pins dropped with right-click on the fullscreen map — still
    /// draw, because those are the player's own notes rather than clutter the game adds.
    ///
    /// To bring the automatic markers back, return true here unconditionally. Nothing else was
    /// removed: the drawing code, the marker shapes and the clusterer are all still in place.
    /// </summary>
    private bool DrawsMarker => iconType == MinimapIconType.Waypoint;

    [Header("Icon Settings")]
    [SerializeField] private MinimapIconType iconType = MinimapIconType.Generic;
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Color iconColor = Color.white;
    [SerializeField] private float iconSize = 1f;

    [Header("Visibility Settings")]
    [SerializeField] private bool alwaysVisible = true;
    [SerializeField] private float visibilityRange = 50f; // Only show if within this range of player
    [SerializeField] private bool rotateWithObject = false;

    [Header("Layer Settings")]
    [SerializeField] private string minimapLayerName = "Minimap";
    [SerializeField] private GameObject iconObject;
    [SerializeField] private SpriteRenderer iconRenderer;

    // Editor-only: its sole reader, LogDebug, is itself inside #if UNITY_EDITOR. Leaving the
    // field outside the guard means a player build compiles a field nothing ever reads (CS0414).
    #if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    #endif

    // State
    private Transform playerTransform;
    private int minimapLayer;
    private bool isInitialized = false;

    // Clustering state (driven by MinimapIconClusterer)
    private int clusterCount = 1;
    private float clusterScale = 1f;
    private bool hiddenByCluster = false;

    /// <summary>What this marker represents. Read by the clusterer to group like with like.</summary>
    public MinimapIconType IconType => iconType;

    /// <summary>
    /// How many markers this one currently stands for. 1 means itself alone; higher means it is
    /// the visible representative of a cluster.
    /// </summary>
    public int ClusterCount => clusterCount;

    /// <summary>
    /// Applied by <see cref="MinimapIconClusterer"/>. A count of 0 means "you were absorbed into
    /// another marker, hide"; 1 or more means "you represent this many", drawn at the given scale.
    /// </summary>
    public void ApplyClusterState(int count, float scale = 1f)
    {
        clusterCount = Mathf.Max(count, 0);
        clusterScale = scale;
        hiddenByCluster = count == 0;

        if (iconObject != null)
            iconObject.transform.localScale = Vector3.one * (iconSize * clusterScale);

        // Visibility is settled in UpdateVisibility so range-limiting and clustering cannot
        // fight over the renderer's enabled flag.
        if (iconRenderer != null && hiddenByCluster && iconRenderer.enabled)
            iconRenderer.enabled = false;
    }

    private void Awake()
    {
        minimapLayer = LayerMask.NameToLayer(minimapLayerName);
        HideMinimapLayerFromGameplayCameras();
    }

    /// <summary>
    /// Takes the minimap layer out of every gameplay camera's culling mask.
    ///
    /// Markers are drawn as real world-space sprites so the minimap camera can photograph them,
    /// which means any camera that renders their layer draws them too. SampleScene's main camera
    /// had a culling mask of -1 (everything), so the markers were always being drawn over the
    /// game — harmless while there were five small ones, and a screen full of giant diamonds the
    /// moment the scene gained thirty.
    ///
    /// Fixing the scene alone would leave the same trap for the next camera anyone adds, so the
    /// icons enforce it themselves. The minimap camera is exempt: it is the one that must see
    /// them, identified by carrying a MinimapCamera component.
    /// </summary>
    private void HideMinimapLayerFromGameplayCameras()
    {
        if (minimapLayer < 0) return;

        // The sweep is identical for every icon, but it ran in each one's Awake — 35 icons in
        // SampleScene meant 35 full passes over Camera.allCameras during load. One pass fixes
        // every camera, so the rest are pure waste.
        if (layerHiddenFromCameras) return;
        layerHiddenFromCameras = true;

        int bit = 1 << minimapLayer;

        foreach (var cam in Camera.allCameras)
        {
            if (cam == null) continue;
            if (cam.GetComponent<MinimapCamera>() != null) continue;
            if ((cam.cullingMask & bit) == 0) continue;
            cam.cullingMask &= ~bit;
        }
    }

    /// <summary>
    /// Static state survives both scene loads and (with domain reload disabled) entering Play
    /// Mode, so it is re-armed on each. A camera belonging to the next scene has its own culling
    /// mask and would never be stripped if this stayed latched from the previous one.
    /// </summary>
    private static bool layerHiddenFromCameras = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        layerHiddenFromCameras = false;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedResetSweep;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedResetSweep;
    }

    private static void OnSceneLoadedResetSweep(
        UnityEngine.SceneManagement.Scene scene,
        UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        layerHiddenFromCameras = false;
    }

    private void Start()
    {
        // Find player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Create icon representation. Skipped for the automatic marker types: Awake has
        // already done the culling-mask sweep, which is the part the rest of the minimap
        // relies on, and that runs regardless.
        if (DrawsMarker)
            CreateIconObject();

        isInitialized = true;
    }

    private void LateUpdate()
    {
        if (!isInitialized || iconObject == null)
            return;

        // Update visibility based on range
        UpdateVisibility();

        // Update rotation if needed
        if (rotateWithObject && iconRenderer != null)
        {
            iconRenderer.transform.rotation = transform.rotation;
        }
    }

    private void OnDestroy()
    {
        // Clean up icon object
        if (iconObject != null)
        {
            Destroy(iconObject);
        }
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private void CreateIconObject()
    {
        // Create a child object that will be on the minimap layer
        iconObject = new GameObject($"{gameObject.name}_MinimapIcon");
        iconObject.transform.SetParent(transform);

        // For 2D XY plane: position at Z=0 so camera at Z=-100 can see it
        // This ensures the icon is visible to the minimap camera
        iconObject.transform.localPosition = new Vector3(0, 0, -transform.position.z);
        iconObject.transform.localRotation = Quaternion.identity;

        // Set to minimap layer
        iconObject.layer = minimapLayer;

        // Add sprite renderer
        iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = iconSprite != null ? iconSprite : GetDefaultIconSprite();
        iconRenderer.color = iconColor;
        // Every icon used a flat 100, so overlapping markers drew in arbitrary order — the
        // player's arrow ended up hidden behind the bed's square, which is exactly backwards
        // for the one marker the player looks for first.
        iconRenderer.sortingOrder = SortingOrderFor(iconType);

        // Scale based on icon size
        iconObject.transform.localScale = Vector3.one * iconSize;

        // For 2D XY plane games, sprites naturally face the camera at (0,0,0)
        // No rotation needed - sprites are already in the correct orientation
        if (!rotateWithObject)
        {
            iconRenderer.transform.localRotation = Quaternion.identity; // (0, 0, 0) for 2D XY plane
        }
    }

    /// <summary>
    /// Draws the marker for this icon type.
    ///
    /// This used to `return null` with a comment saying Unity would fall back to a white square.
    /// It does not — a SpriteRenderer with no sprite draws nothing at all, so every icon whose
    /// `iconSprite` was left unassigned (which was all of them) was simply invisible, with no
    /// error. Three of the five icons in SampleScene had never rendered.
    ///
    /// Shapes are deliberately not the world art: a minimap is read at ~200px, where a detailed
    /// character sprite reduces to unreadable confetti. Each type gets a distinct silhouette so
    /// it survives at that size — the player a chevron, buildings a square, NPCs a diamond,
    /// animals a small dot.
    /// </summary>
    private Sprite GetDefaultIconSprite()
    {
        return MinimapIconSprites.ForType(iconType);
    }

    /// <summary>
    /// Draw order for overlapping markers: the more urgent the marker, the higher it sits.
    /// The player is always on top — losing your own position behind a building marker defeats
    /// the point of the minimap.
    /// </summary>
    private static int SortingOrderFor(MinimapIconType type)
    {
        switch (type)
        {
            case MinimapIconType.Player:      return 130;
            case MinimapIconType.Quest:       return 125;
            case MinimapIconType.Enemy:       return 120;
            case MinimapIconType.NPC:         return 115;
            case MinimapIconType.SellBox:
            case MinimapIconType.Bed:
            case MinimapIconType.Building:
            case MinimapIconType.CropField:   return 105;
            default:                          return 100;
        }
    }

    // ============================================================================
    // VISIBILITY MANAGEMENT
    // ============================================================================

    private void UpdateVisibility()
    {
        // A marker absorbed into a cluster stays hidden regardless of range: another marker is
        // already standing for it, and showing both would double-count.
        if (hiddenByCluster)
        {
            if (iconRenderer.enabled)
                iconRenderer.enabled = false;
            return;
        }

        if (alwaysVisible)
        {
            if (!iconRenderer.enabled)
                iconRenderer.enabled = true;
            return;
        }

        // Check distance to player
        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool shouldBeVisible = distance <= visibilityRange;

            if (iconRenderer.enabled != shouldBeVisible)
            {
                iconRenderer.enabled = shouldBeVisible;
            }
        }
    }

    /// <summary>
    /// Manually set icon visibility
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (iconRenderer != null)
        {
            iconRenderer.enabled = visible;
        }
    }

    // ============================================================================
    // CUSTOMIZATION API
    // ============================================================================

    /// <summary>
    /// Change the icon sprite
    /// </summary>
    public void SetIconSprite(Sprite sprite)
    {
        iconSprite = sprite;
        if (iconRenderer != null)
        {
            iconRenderer.sprite = sprite;
        }
    }

    /// <summary>
    /// Change the icon color
    /// </summary>
    public void SetIconColor(Color color)
    {
        iconColor = color;
        if (iconRenderer != null)
        {
            iconRenderer.color = color;
        }
    }

    /// <summary>
    /// Change the icon size
    /// </summary>
    public void SetIconSize(float size)
    {
        iconSize = size;
        if (iconObject != null)
        {
            // Keep any active cluster scale folded in, or resizing an icon would silently reset
            // a cluster back to single-marker size.
            iconObject.transform.localScale = Vector3.one * (size * clusterScale);
        }
    }

    /// <summary>
    /// Set the icon type (updates color/sprite based on type)
    /// </summary>
    public void SetIconType(MinimapIconType type)
    {
        iconType = type;
        ApplyIconTypeDefaults();

        // A pin is built with AddComponent and only then told what it is, so Start() already
        // ran with the default type and skipped creating the sprite. Create it now that the
        // type says this marker should draw. Without this the player's pins were placed,
        // saved and counted — and invisible.
        if (isInitialized && DrawsMarker && iconObject == null)
            CreateIconObject();
    }

    private void ApplyIconTypeDefaults()
    {
        // Apply default colors based on icon type
        switch (iconType)
        {
            case MinimapIconType.Player:
                SetIconColor(Color.green);
                break;

            case MinimapIconType.NPC:
                SetIconColor(Color.blue);
                break;

            case MinimapIconType.SellBox:
                SetIconColor(Color.yellow);
                break;

            case MinimapIconType.Bed:
                SetIconColor(new Color(1f, 0.5f, 0f)); // Orange
                break;

            case MinimapIconType.CropField:
                SetIconColor(new Color(0.5f, 1f, 0.5f)); // Light green
                break;

            case MinimapIconType.Building:
                SetIconColor(new Color(0.7f, 0.7f, 0.7f)); // Gray
                break;

            case MinimapIconType.Waypoint:
                SetIconColor(Color.cyan);
                break;

            case MinimapIconType.Quest:
                SetIconColor(Color.magenta);
                break;

            case MinimapIconType.Generic:
            default:
                SetIconColor(Color.white);
                break;
        }
    }

    /// <summary>
    /// Set whether this icon should always be visible or range-limited
    /// </summary>
    public void SetAlwaysVisible(bool always)
    {
        alwaysVisible = always;
    }

    /// <summary>
    /// Set the visibility range (only used if alwaysVisible is false)
    /// </summary>
    public void SetVisibilityRange(float range)
    {
        visibilityRange = range;
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    /// <summary>
    /// Flash the icon (useful for drawing attention)
    /// </summary>
    public void Flash(float duration = 1f)
    {
        if (iconRenderer != null)
        {
            StartCoroutine(FlashCoroutine(duration));
        }
    }

    private System.Collections.IEnumerator FlashCoroutine(float duration)
    {
        Color originalColor = iconRenderer.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float alpha = Mathf.PingPong(elapsed * 4f, 1f);
            iconRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        iconRenderer.color = originalColor;
    }

    /// <summary>
    /// Pulse the icon size (useful for important markers)
    /// </summary>
    public void Pulse(float duration = 1f, float scaleMultiplier = 1.5f)
    {
        if (iconObject != null)
        {
            StartCoroutine(PulseCoroutine(duration, scaleMultiplier));
        }
    }

    private System.Collections.IEnumerator PulseCoroutine(float duration, float scaleMultiplier)
    {
        Vector3 originalScale = iconObject.transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float scale = 1f + (scaleMultiplier - 1f) * Mathf.PingPong(elapsed * 2f, 1f);
            iconObject.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        iconObject.transform.localScale = originalScale;
    }

    // ============================================================================
    // DEBUG & LOGGING
    // ============================================================================

    #if UNITY_EDITOR
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MinimapIcon] {message}", this);
        }
    }
    #endif

    // ============================================================================
    // EDITOR HELPERS
    // ============================================================================

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Update icon appearance when values change in inspector
        if (Application.isPlaying && iconRenderer != null)
        {
            iconRenderer.color = iconColor;
            if (iconSprite != null)
                iconRenderer.sprite = iconSprite;

            if (iconObject != null)
                iconObject.transform.localScale = Vector3.one * iconSize;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw visibility range in editor
        if (!alwaysVisible)
        {
            Gizmos.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visibilityRange);
        }

        // Draw icon representation
        Gizmos.color = iconColor;
        Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
    }
#endif
}

/// <summary>
/// Types of minimap icons for easy categorization
/// </summary>
public enum MinimapIconType
{
    Generic,
    Player,
    NPC,
    SellBox,
    Bed,
    CropField,
    Building,
    Waypoint,
    Quest,
    Enemy,
    Collectible
}

} // namespace SowurShield.Minimap
