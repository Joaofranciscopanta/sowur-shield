using UnityEngine;
using DG.Tweening;
using SowurShield.Core;

namespace SowurShield.Minimap
{

/// <summary>
/// Manages the minimap camera behavior for 2D games (XY plane)
/// Handles player following, zoom levels, panning, and rendering configuration
/// </summary>
[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera minimapCam;
    [SerializeField] private float defaultOrthographicSize = 10f;
    [SerializeField] private LayerMask minimapLayers;
    [SerializeField] private float cameraDistance = 100f; // Distance in front of objects (Z axis)

    [Header("Follow Settings")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private float followSmoothness = 5f;
    [SerializeField] private Vector3 followOffset = Vector3.zero;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomTransitionDuration = 0.3f;
    [SerializeField] private Ease zoomEase = Ease.InOutQuad;
    [SerializeField] private float minOrthographicSize = 4f;
    // Must exceed defaultOrthographicSize * the largest view scale, or the widest zoom step
    // silently does nothing. At 16 default and a 3.5 top scale that needs 56; the old 30 clamped
    // the 2.0 step (32) down to 30, so the last zoom-out was a no-op nobody noticed.
    [SerializeField] private float maxOrthographicSize = 60f;

    [Header("Render Settings")]
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private int renderTextureSize = 1024;
    // This is the minimap's ground colour, not a void colour. SampleScene has no terrain to
    // photograph — both tilemaps are empty and there is no ground sprite — so the camera's clear
    // colour IS the field the markers sit on. Near-black (the old default) read as "the minimap
    // is switched off"; a field green reads as the farm.
    [SerializeField] private Color backgroundColor = new Color(0.62f, 0.74f, 0.42f, 1f);

    // Editor-only: its sole reader, LogDebug, is itself inside #if UNITY_EDITOR. Leaving the
    // field outside the guard means a player build compiles a field nothing ever reads (CS0414).
    #if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    #endif

    // State
    private Vector3 panOffset = Vector3.zero;
    private float currentZoomLevel = 1f;
    private Tween currentZoomTween;

    // Cached world extent (see TryGetWorldBounds)
    private Bounds cachedWorldBounds;
    private bool worldBoundsCached = false;
    private bool worldBoundsValid = false;

    private void Awake()
    {
        // Get camera component if not assigned
        if (minimapCam == null)
            minimapCam = GetComponent<Camera>();

        // Auto-find player if not assigned
        if (playerTarget == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTarget = player.transform;
        }

        SetupCamera();
        CreateRenderTexture();
    }

    private void Start()
    {
        InitializePosition();
    }

    private void LateUpdate()
    {
        if (followPlayer && playerTarget != null)
        {
            UpdateCameraPosition();
        }
        else if (!followPlayer)
        {
            // When not following, apply manual pan offset
            UpdateManualPosition();
        }
    }

    private void OnDestroy()
    {
        // Clean up tweens
        if (currentZoomTween != null && currentZoomTween.IsActive())
        {
            currentZoomTween.Kill();
        }

        // Clean up render texture
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private void SetupCamera()
    {
        if (minimapCam == null)
            return;

        // Configure camera for 2D minimap (XY plane)
        minimapCam.orthographic = true;
        minimapCam.orthographicSize = defaultOrthographicSize;
        minimapCam.cullingMask = ResolveCullingMask();
        minimapCam.backgroundColor = backgroundColor;
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.depth = 10; // Render after main camera

        // Set near/far clip planes for 2D
        minimapCam.nearClipPlane = 0.1f;
        minimapCam.farClipPlane = 1000f;
    }

    /// <summary>
    /// The layers the minimap renders: the terrain layer plus the icon layer.
    ///
    /// In SampleScene `minimapLayers` was serialized to the Minimap layer alone, which holds only
    /// the icon sprites — the terrain tilemaps and all 107 world sprites live on Default — so the
    /// camera rendered its clear colour plus three dots and the panel read as a black square.
    /// Nothing errored; it simply looked switched off, and shipped that way.
    ///
    /// Pointing it at Default instead is the obvious fix and the wrong one: it draws every world
    /// sprite at full detail, which at the HUD's ~200px reduces to unreadable confetti. A minimap
    /// wants ground plus markers, so the terrain gets its own layer (see
    /// <see cref="MinimapTerrainLayerName"/>) that both this camera and the main camera render.
    ///
    /// A mask that cannot show terrain is treated as unconfigured. An explicit mask that already
    /// includes something beyond the icon layer is honoured, so deliberate setups still work.
    /// </summary>
    private int ResolveCullingMask()
    {
        int iconBit = LayerBit(MinimapLayerName);
        int terrainBit = LayerBit(MinimapTerrainLayerName);
        int configured = minimapLayers.value;

        // Icons-only (or nothing) cannot draw a map.
        bool cannotShowGround = configured == 0 || (configured & ~iconBit) == 0;
        if (!cannotShowGround)
            return configured;

        // Fall back to terrain + icons; if the terrain layer does not exist in this project,
        // Default at least shows something rather than a blank square.
        int fallback = (terrainBit != 0 ? terrainBit : 1) | iconBit;
        minimapLayers = fallback;
        return fallback;
    }

    private static int LayerBit(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? (1 << layer) : 0;
    }

    private const string MinimapLayerName = "Minimap";

    /// <summary>
    /// Layer holding the ground tilemaps. Rendered by the minimap camera *and* the main camera —
    /// it exists to separate "ground" from "props", not to hide anything from the player.
    /// </summary>
    public const string MinimapTerrainLayerName = "MinimapTerrain";

    private void CreateRenderTexture()
    {
        if (minimapCam == null)
            return;

        // Create render texture if not assigned
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16);
            renderTexture.name = "MinimapRenderTexture";
            renderTexture.format = RenderTextureFormat.ARGB32;
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.antiAliasing = 1; // No AA for performance
        }

        minimapCam.targetTexture = renderTexture;
    }

    private void InitializePosition()
    {
        if (playerTarget != null)
        {
            // Position camera in front of player (for 2D XY plane, camera looks along -Z axis)
            Vector3 initialPos = playerTarget.position + followOffset;
            initialPos.z = playerTarget.position.z - cameraDistance; // Camera in front (negative Z)
            transform.position = initialPos;
        }
        else
        {
            // Default position - in front of origin
            transform.position = new Vector3(0, 0, -cameraDistance);
        }

        // Point camera forward (looking at XY plane from -Z direction)
        // For 2D games, camera should have rotation (0, 0, 0) to look forward
        transform.rotation = Quaternion.identity; // (0, 0, 0)
    }

    // ============================================================================
    // CAMERA MOVEMENT
    // ============================================================================

    private void UpdateCameraPosition()
    {
        if (playerTarget == null)
            return;

        // Calculate target position (following player on XY plane)
        Vector3 targetPosition = playerTarget.position + followOffset;
        targetPosition.z = playerTarget.position.z - cameraDistance; // Keep camera in front

        // Smooth follow
        if (followSmoothness > 0)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime * followSmoothness
            );
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private void UpdateManualPosition()
    {
        // In manual mode, apply pan offset relative to player position
        if (playerTarget == null)
            return;

        Vector3 basePosition = playerTarget.position + followOffset;
        basePosition.z = playerTarget.position.z - cameraDistance;

        Vector3 targetPosition = basePosition + panOffset;
        transform.position = targetPosition;
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Enable/disable automatic player following
    /// </summary>
    public void SetFollowPlayer(bool follow)
    {
        followPlayer = follow;

        if (follow)
        {
            // Reset pan offset when returning to follow mode
            panOffset = Vector3.zero;
        }
    }

    /// <summary>
    /// Set manual pan offset (used in fullscreen mode)
    /// </summary>
    public void SetPanOffset(Vector3 offset)
    {
        panOffset = offset;
    }

    /// <summary>
    /// Set zoom level (0.5 = zoomed in, 2.0 = zoomed out)
    /// </summary>
    public void SetZoomLevel(float zoomLevel, bool immediate = false)
    {
        if (minimapCam == null)
            return;

        currentZoomLevel = zoomLevel;
        float targetSize = defaultOrthographicSize * zoomLevel;

        // Clamp to min/max
        targetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);

        if (immediate)
        {
            minimapCam.orthographicSize = targetSize;
        }
        else
        {
            // Kill existing tween
            if (currentZoomTween != null && currentZoomTween.IsActive())
            {
                currentZoomTween.Kill();
            }

            // Animate zoom
            currentZoomTween = DOTween.To(
                () => minimapCam.orthographicSize,
                x => minimapCam.orthographicSize = x,
                targetSize,
                zoomTransitionDuration
            ).SetEase(zoomEase);
        }
    }

    /// <summary>
    /// Reset zoom to default level
    /// </summary>
    public void ResetZoom(bool immediate = false)
    {
        SetZoomLevel(1f, immediate);
    }

    /// <summary>
    /// Set the player target for following
    /// </summary>
    public void SetPlayerTarget(Transform target)
    {
        playerTarget = target;
    }

    /// <summary>
    /// Get the camera's render texture
    /// </summary>
    public RenderTexture GetRenderTexture()
    {
        return renderTexture;
    }

    /// <summary>
    /// Get the camera component
    /// </summary>
    public Camera GetCamera()
    {
        return minimapCam;
    }

    /// <summary>
    /// Update the culling mask (which layers the minimap renders)
    /// </summary>
    public void SetCullingMask(LayerMask mask)
    {
        if (minimapCam != null)
        {
            minimapCam.cullingMask = mask;
            minimapLayers = mask;
        }
    }

    /// <summary>
    /// Get current world position of camera (2D position on XY plane)
    /// </summary>
    public Vector3 GetWorldPosition()
    {
        return new Vector3(transform.position.x, transform.position.y, 0);
    }

    /// <summary>Half-height of the current view, in world units.</summary>
    public float CurrentOrthographicSize()
    {
        return minimapCam != null ? minimapCam.orthographicSize : defaultOrthographicSize;
    }

    /// <summary>Width/height ratio of the render target.</summary>
    public float CurrentAspect()
    {
        return minimapCam != null && minimapCam.aspect > 0f ? minimapCam.aspect : 1f;
    }

    /// <summary>
    /// Extent of the playable world, used to stop panning past the map's edge.
    ///
    /// Measured once from every SpriteRenderer and Tilemap that is not a minimap marker, then
    /// cached — this walks the whole scene, so it must not run per-frame from a pan handler.
    /// Markers are excluded because they sit on the minimap layer and would otherwise define the
    /// bounds by themselves.
    /// </summary>
    public bool TryGetWorldBounds(out Bounds bounds)
    {
        if (worldBoundsCached)
        {
            bounds = cachedWorldBounds;
            return worldBoundsValid;
        }

        worldBoundsCached = true;
        worldBoundsValid = false;
        cachedWorldBounds = new Bounds();

        int iconLayer = LayerMask.NameToLayer(MinimapLayerName);
        bool any = false;

        int terrainLayer = LayerMask.NameToLayer(MinimapTerrainLayerName);

        foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (iconLayer >= 0 && sr.gameObject.layer == iconLayer) continue;

            // The painted minimap ground is derived FROM these bounds and is deliberately padded
            // beyond them, so measuring it here inflates the limit a little more every time —
            // the pan clamp drifted out to 101 units around a 28-unit farm.
            if (terrainLayer >= 0 && sr.gameObject.layer == terrainLayer) continue;

            if (!any) { cachedWorldBounds = sr.bounds; any = true; }
            else cachedWorldBounds.Encapsulate(sr.bounds);
        }

        foreach (var tm in FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            // SampleScene's two tilemaps hold zero tiles, and an empty Tilemap does NOT report an
            // empty localBounds — it reports the default 100x100 cell grid. Trusting that stretched
            // the pan limit to 101 units around a farm that is 28 wide. cellBounds is likewise
            // non-empty here, so the only reliable test is counting actual tiles.
            if (CountTiles(tm) == 0) continue;

            var b = tm.localBounds;
            b.center = tm.transform.TransformPoint(b.center);
            if (!any) { cachedWorldBounds = b; any = true; }
            else cachedWorldBounds.Encapsulate(b);
        }

        worldBoundsValid = any;
        bounds = cachedWorldBounds;
        return any;
    }

    /// <summary>
    /// Number of tiles actually placed. Walked rather than inferred from cellBounds, which stays
    /// non-empty on a tilemap that has never been painted.
    /// </summary>
    private static int CountTiles(UnityEngine.Tilemaps.Tilemap tilemap)
    {
        int count = 0;
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos)) count++;
        }
        return count;
    }

    /// <summary>Drop the cached bounds, e.g. after the world changes size.</summary>
    public void InvalidateWorldBounds()
    {
        worldBoundsCached = false;
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    /// <summary>
    /// Convert screen point to world point on the minimap
    /// Useful for minimap interactions
    /// </summary>
    public Vector3 ScreenToWorldPoint(Vector2 screenPoint)
    {
        if (minimapCam == null)
            return Vector3.zero;

        Vector3 worldPoint = minimapCam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, minimapCam.nearClipPlane));
        return worldPoint;
    }

    /// <summary>
    /// Check if a world position is currently visible in the minimap
    /// </summary>
    public bool IsPositionVisible(Vector3 worldPosition)
    {
        if (minimapCam == null)
            return false;

        Vector3 viewportPoint = minimapCam.WorldToViewportPoint(worldPosition);
        return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
               viewportPoint.y >= 0 && viewportPoint.y <= 1 &&
               viewportPoint.z > 0; // Must be in front of camera
    }

    /// <summary>
    /// Force the camera to update immediately (useful when teleporting)
    /// </summary>
    public void ForceUpdate()
    {
        if (followPlayer && playerTarget != null)
        {
            Vector3 targetPosition = playerTarget.position + followOffset;
            targetPosition.z = playerTarget.position.z - cameraDistance;
            transform.position = targetPosition;
        }
    }

    // ============================================================================
    // DEBUG & LOGGING
    // ============================================================================

    #if UNITY_EDITOR
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MinimapCamera] {message}", this);
        }
    }
    #endif

    // ============================================================================
    // EDITOR HELPERS
    // ============================================================================

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Draw camera frustum in scene view
        if (minimapCam != null)
        {
            Gizmos.color = Color.cyan;
            Matrix4x4 temp = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            // Draw orthographic frustum for 2D
            float size = minimapCam.orthographicSize;
            float aspect = minimapCam.aspect;
            Vector3 center = new Vector3(0, 0, cameraDistance / 2);
            Gizmos.DrawWireCube(center, new Vector3(size * aspect * 2, size * 2, cameraDistance));

            Gizmos.matrix = temp;
        }

        // Draw line to player if following
        if (followPlayer && playerTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }

    [ContextMenu("Reset Camera for 2D (XY Plane)")]
    private void ResetCameraFor2D()
    {
        transform.rotation = Quaternion.identity; // (0, 0, 0)

        if (playerTarget != null)
        {
            Vector3 pos = playerTarget.position;
            pos.z = playerTarget.position.z - cameraDistance;
            transform.position = pos;
        }
        else
        {
            transform.position = new Vector3(0, 0, -cameraDistance);
        }

    }
#endif
}

} // namespace SowurShield.Minimap
