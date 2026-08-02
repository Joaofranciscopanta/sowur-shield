using UnityEngine;

namespace SowurShield.Minimap
{

/// <summary>
/// Component for GameObjects to appear on the minimap
/// Attach to any object that should be visible on the minimap (NPCs, SellBox, Beds, etc.)
/// </summary>
public class MinimapIcon : MonoBehaviour
{
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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // State
    private Transform playerTransform;
    private int minimapLayer;
    private bool isInitialized = false;

    private void Awake()
    {
        minimapLayer = LayerMask.NameToLayer(minimapLayerName);
    }

    private void Start()
    {
        // Find player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // Create icon representation
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
        iconRenderer.sortingOrder = 100; // Render on top

        // Scale based on icon size
        iconObject.transform.localScale = Vector3.one * iconSize;

        // For 2D XY plane games, sprites naturally face the camera at (0,0,0)
        // No rotation needed - sprites are already in the correct orientation
        if (!rotateWithObject)
        {
            iconRenderer.transform.localRotation = Quaternion.identity; // (0, 0, 0) for 2D XY plane
        }
    }

    private Sprite GetDefaultIconSprite()
    {
        // Create a simple default sprite if none is assigned
        // For now, return null and Unity will use a white square
        return null;
    }

    // ============================================================================
    // VISIBILITY MANAGEMENT
    // ============================================================================

    private void UpdateVisibility()
    {
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
            iconObject.transform.localScale = Vector3.one * size;
        }
    }

    /// <summary>
    /// Set the icon type (updates color/sprite based on type)
    /// </summary>
    public void SetIconType(MinimapIconType type)
    {
        iconType = type;
        ApplyIconTypeDefaults();
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
