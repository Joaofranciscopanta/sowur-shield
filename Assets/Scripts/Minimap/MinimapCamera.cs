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
    [SerializeField] private float minOrthographicSize = 5f;
    [SerializeField] private float maxOrthographicSize = 30f;

    [Header("Render Settings")]
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private int renderTextureSize = 1024;
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // State
    private Vector3 panOffset = Vector3.zero;
    private float currentZoomLevel = 1f;
    private Tween currentZoomTween;

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
        minimapCam.cullingMask = minimapLayers;
        minimapCam.backgroundColor = backgroundColor;
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.depth = 10; // Render after main camera

        // Set near/far clip planes for 2D
        minimapCam.nearClipPlane = 0.1f;
        minimapCam.farClipPlane = 1000f;
    }

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
