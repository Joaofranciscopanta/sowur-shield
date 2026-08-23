using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization;
using SowurShield.Core;

namespace SowurShield.Minimap
{

/// <summary>
/// Manages the minimap UI display and transitions
/// Handles RawImage display, opacity changes, position/scale animations
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform minimapPanel;
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Position Settings")]
    [SerializeField] private Vector2 normalPosition = new Vector2(-100, -100); // Top-right corner
    [SerializeField] private Vector2 normalSize = new Vector2(200, 200);
    [SerializeField] private Vector2 fullscreenSize = new Vector2(800, 800); // 80% of 1080p screen

    [Header("Visual Settings")]
    [SerializeField] private Color borderColor = Color.white;
    [SerializeField] private Image borderImage;

    [Header("Info Display")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TextMeshProUGUI zoomText;
    [SerializeField] private TextMeshProUGUI coordinatesText;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("Player Marker")]
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private Image playerMarkerImage;
    [SerializeField] private Color playerMarkerColor = Color.green;
    [SerializeField] private float playerMarkerSize = 10f;

    // Editor-only: its sole reader, LogDebug, is itself inside #if UNITY_EDITOR. Leaving the
    // field outside the guard means a player build compiles a field nothing ever reads (CS0414).
    #if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    #endif

    [Header("Localization")]
    [SerializeField] private LocalizedString zoomLabelLocalized; // table "Minimap", key "minimap.zoom_label"
    [SerializeField] private LocalizedString coordsLabelLocalized; // table "Minimap", key "minimap.coords_label"
    [SerializeField] private LocalizedString modeLabelLocalized; // table "Minimap", key "minimap.mode_label"

    // State
    private Vector2 currentTargetPosition;
    private Vector2 currentTargetSize;
    private float currentTargetOpacity = 1f;

    // Tweens
    private Tween positionTween;
    private Tween sizeTween;
    private Tween opacityTween;

    // Resolved once in Start rather than searched every frame (see UpdatePlayerMarker)
    private MinimapCamera cachedMinimapCamera;
    private Transform cachedPlayer;
    private bool useCanvasPlayerMarker = true;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (minimapPanel == null)
            minimapPanel = GetComponent<RectTransform>();

        if (minimapImage == null)
            minimapImage = GetComponentInChildren<RawImage>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        SetupUI();

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void Start()
    {
        // Connect to minimap camera - try multiple times if needed
        ConnectToMinimapCamera();

        // Retry connection after a short delay if it failed
        Invoke(nameof(RetryConnection), 0.5f);

        // Hide info panel initially
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Setup player marker
        SetupPlayerMarker();
    }

    private void OnDestroy()
    {
        // Kill all active tweens
        KillAllTweens();
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void Update()
    {
        // Update player marker position if visible
        UpdatePlayerMarker();
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private void SetupUI()
    {
        if (minimapPanel == null)
            return;

        // Set initial anchors for top-right corner positioning
        minimapPanel.anchorMin = new Vector2(1, 1); // Top-right
        minimapPanel.anchorMax = new Vector2(1, 1);
        minimapPanel.pivot = new Vector2(1, 1);

        // Set initial position and size
        minimapPanel.anchoredPosition = normalPosition;
        minimapPanel.sizeDelta = normalSize;

        // Set initial opacity
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    private void ConnectToMinimapCamera()
    {
        if (minimapImage == null)
        {
            return;
        }

        var minimapCamera = FindFirstObjectByType<MinimapCamera>();
        if (minimapCamera != null)
        {
            var renderTexture = minimapCamera.GetRenderTexture();
            if (renderTexture != null)
            {
                minimapImage.texture = renderTexture;
            }
        }
    }

    private void RetryConnection()
    {
        // Check if already connected
        if (minimapImage != null && minimapImage.texture != null)
        {
            return;
        }

        // Try to connect again
        ConnectToMinimapCamera();
    }

    private void SetupPlayerMarker()
    {
        cachedMinimapCamera = FindFirstObjectByType<MinimapCamera>();

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        cachedPlayer = playerObj != null ? playerObj.transform : null;

        // If the player already renders a world-space marker, this Canvas one would draw a second
        // arrow on top of the first. Stand down and let the world icon do the job.
        useCanvasPlayerMarker = playerObj == null
                             || playerObj.GetComponentInChildren<MinimapIcon>(true) == null;

        if (playerMarker == null)
            return;

        if (!useCanvasPlayerMarker)
        {
            playerMarker.gameObject.SetActive(false);
            return;
        }

        if (playerMarkerImage != null)
        {
            // The marker's sprite was whatever the scene happened to carry — in SampleScene a
            // frame of the character spritesheet, tinted flat green at 10x10px, which rendered
            // as an indistinct smudge. A minimap marker needs a silhouette that survives being
            // tiny, so it uses the same procedural chevron the world icons use.
            playerMarkerImage.sprite = MinimapIconSprites.ForType(MinimapIconType.Player);
            playerMarkerImage.color = playerMarkerColor;
            playerMarker.sizeDelta = new Vector2(playerMarkerSize, playerMarkerSize);
            playerMarker.gameObject.SetActive(true);
        }
    }

    // ============================================================================
    // STATE TRANSITIONS
    // ============================================================================

    /// <summary>
    /// Transition to normal corner mode
    /// </summary>
    public void TransitionToNormal(float duration, Ease ease)
    {
        // CRITICAL FIX: Reset anchors to top-right BEFORE animating
        minimapPanel.anchorMin = new Vector2(1, 1); // Top-right
        minimapPanel.anchorMax = new Vector2(1, 1);
        minimapPanel.pivot = new Vector2(1, 1);

        // Hide info panel
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Animate to corner position
        AnimatePosition(normalPosition, duration, ease);
        AnimateSize(normalSize, duration, ease);
        AnimateOpacity(1f, duration, ease);

        // Update state text
        UpdateStateText("Normal");
    }

    /// <summary>
    /// Transition to semi-transparent mode
    /// </summary>
    public void TransitionToSemiTransparent(float opacity, float duration, Ease ease)
    {
        // CRITICAL FIX: Keep anchors at top-right
        minimapPanel.anchorMin = new Vector2(1, 1); // Top-right
        minimapPanel.anchorMax = new Vector2(1, 1);
        minimapPanel.pivot = new Vector2(1, 1);

        // Hide info panel
        if (infoPanel != null)
            infoPanel.SetActive(false);

        // Stay in corner but change opacity
        AnimatePosition(normalPosition, duration, ease);
        AnimateSize(normalSize, duration, ease);
        AnimateOpacity(opacity, duration, ease);

        // Update state text
        UpdateStateText("Semi-Transparent");
    }

    /// <summary>
    /// Transition to fullscreen mode
    /// </summary>
    public void TransitionToFullscreen(float duration, Ease ease)
    {
        // Show info panel
        if (infoPanel != null)
            infoPanel.SetActive(true);

        // Change anchor to center
        minimapPanel.anchorMin = new Vector2(0.5f, 0.5f);
        minimapPanel.anchorMax = new Vector2(0.5f, 0.5f);
        minimapPanel.pivot = new Vector2(0.5f, 0.5f);

        // Force anchored position to zero first (center)
        minimapPanel.anchoredPosition = Vector2.zero;

        // Animate to center position and fullscreen size
        AnimatePosition(Vector2.zero, duration, ease);
        AnimateSize(fullscreenSize, duration, ease);
        AnimateOpacity(1f, duration, ease);

        // Update state text
        UpdateStateText("Fullscreen");
    }

    // ============================================================================
    // ANIMATION HELPERS
    // ============================================================================

    private void AnimatePosition(Vector2 targetPosition, float duration, Ease ease)
    {
        currentTargetPosition = targetPosition;

        if (positionTween != null && positionTween.IsActive())
            positionTween.Kill();

        if (duration <= 0)
        {
            minimapPanel.anchoredPosition = targetPosition;
        }
        else
        {
            positionTween = DOTween.To(() => minimapPanel.anchoredPosition, x => minimapPanel.anchoredPosition = x, targetPosition, duration)
                .SetEase(ease)
                .SetUpdate(true); // Use unscaled time
        }
    }

    private void AnimateSize(Vector2 targetSize, float duration, Ease ease)
    {
        currentTargetSize = targetSize;

        if (sizeTween != null && sizeTween.IsActive())
            sizeTween.Kill();

        if (duration <= 0)
        {
            minimapPanel.sizeDelta = targetSize;
        }
        else
        {
            sizeTween = DOTween.To(() => minimapPanel.sizeDelta, x => minimapPanel.sizeDelta = x, targetSize, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }
    }

    private void AnimateOpacity(float targetOpacity, float duration, Ease ease)
    {
        currentTargetOpacity = targetOpacity;

        if (opacityTween != null && opacityTween.IsActive())
            opacityTween.Kill();

        if (canvasGroup == null)
            return;

        if (duration <= 0)
        {
            canvasGroup.alpha = targetOpacity;
        }
        else
        {
            opacityTween = DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, targetOpacity, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }
    }

    private void KillAllTweens()
    {
        if (positionTween != null && positionTween.IsActive())
            positionTween.Kill();

        if (sizeTween != null && sizeTween.IsActive())
            sizeTween.Kill();

        if (opacityTween != null && opacityTween.IsActive())
            opacityTween.Kill();
    }

    // ============================================================================
    // INFO DISPLAY
    // ============================================================================

    /// <summary>
    /// Update zoom level indicator
    /// </summary>
    public void UpdateZoomIndicator(float zoomLevel)
    {
        lastZoomLevel = zoomLevel;
        if (zoomText != null)
        {
            zoomLabelLocalized.Arguments = new object[] { zoomLevel };
            zoomText.text = zoomLabelLocalized.SafeGetLocalizedString();
        }
    }

    /// <summary>
    /// Update coordinates display
    /// </summary>
    public void UpdateCoordinates(Vector3 worldPosition)
    {
        if (coordinatesText != null)
        {
            coordsLabelLocalized.Arguments = new object[] { worldPosition.x, worldPosition.y };
            coordinatesText.text = coordsLabelLocalized.SafeGetLocalizedString();
        }
    }

    /// <summary>
    /// Update state indicator
    /// </summary>
    private string lastStateName = "Normal";
    private float lastZoomLevel = 1f;

    private void UpdateStateText(string stateName)
    {
        lastStateName = stateName;
        if (stateText != null)
        {
            modeLabelLocalized.Arguments = new object[] { stateName };
            stateText.text = modeLabelLocalized.SafeGetLocalizedString();
        }
    }

    private void HandleLanguageChanged(Locale locale)
    {
        UpdateStateText(lastStateName);
        UpdateZoomIndicator(lastZoomLevel);
    }

    // ============================================================================
    // PLAYER MARKER
    // ============================================================================

    /// <summary>
    /// Positions the Canvas-space player marker over the rendered map.
    ///
    /// Only runs when the player carries no world-space <see cref="MinimapIcon"/>. When it does —
    /// which is the case in SampleScene — the camera already photographs a chevron at the
    /// player's position, and drawing this one too put two arrows on the same spot, visibly
    /// stacked. The world icon wins because it scales with zoom and sorts against other markers;
    /// this one stays as the fallback for scenes that never set an icon up.
    ///
    /// The two per-frame Find calls it used to make are resolved once in Start instead.
    /// </summary>
    private void UpdatePlayerMarker()
    {
        if (playerMarker == null || !useCanvasPlayerMarker)
            return;

        if (cachedMinimapCamera == null || cachedPlayer == null)
            return;

        var camera = cachedMinimapCamera.GetCamera();
        if (camera == null)
            return;

        Vector3 viewportPos = camera.WorldToViewportPoint(cachedPlayer.position);

        // Check if player is within minimap view
        if (viewportPos.x < 0 || viewportPos.x > 1 || viewportPos.y < 0 || viewportPos.y > 1)
        {
            if (playerMarker.gameObject.activeSelf)
                playerMarker.gameObject.SetActive(false);
            return;
        }

        if (!playerMarker.gameObject.activeSelf)
            playerMarker.gameObject.SetActive(true);

        // Convert viewport to local position within minimap panel
        Vector2 localPos = new Vector2(
            (viewportPos.x - 0.5f) * minimapPanel.sizeDelta.x,
            (viewportPos.y - 0.5f) * minimapPanel.sizeDelta.y
        );

        playerMarker.anchoredPosition = localPos;
    }

    /// <summary>
    /// Set player marker visibility
    /// </summary>
    public void SetPlayerMarkerVisible(bool visible)
    {
        if (playerMarker != null)
        {
            playerMarker.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Set player marker color
    /// </summary>
    public void SetPlayerMarkerColor(Color color)
    {
        playerMarkerColor = color;
        if (playerMarkerImage != null)
        {
            playerMarkerImage.color = color;
        }
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Set the render texture to display
    /// </summary>
    public void SetRenderTexture(RenderTexture texture)
    {
        if (minimapImage != null)
        {
            minimapImage.texture = texture;
        }
    }

    /// <summary>
    /// Get current opacity
    /// </summary>
    public float GetOpacity()
    {
        return canvasGroup != null ? canvasGroup.alpha : 1f;
    }

    /// <summary>
    /// Force set opacity immediately
    /// </summary>
    public void SetOpacity(float opacity)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = opacity;
        }
    }

    /// <summary>
    /// Show/hide info panel
    /// </summary>
    public void SetInfoPanelVisible(bool visible)
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// Get the minimap panel RectTransform
    /// </summary>
    public RectTransform GetPanel()
    {
        return minimapPanel;
    }

    /// <summary>
    /// Force reconnect to minimap camera (useful for debugging)
    /// </summary>
    [ContextMenu("Force Reconnect Camera")]
    public void ForceReconnectCamera()
    {
        ConnectToMinimapCamera();
    }

    // ============================================================================
    // DEBUG & LOGGING
    // ============================================================================

    #if UNITY_EDITOR
    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[MinimapUI] {message}", this);
        }
    }
    #endif

    // ============================================================================
    // EDITOR HELPERS
    // ============================================================================

#if UNITY_EDITOR
    [ContextMenu("Test - Transition to Normal")]
    private void TestNormal()
    {
        TransitionToNormal(0.3f, Ease.InOutQuad);
    }

    [ContextMenu("Test - Transition to Semi-Transparent")]
    private void TestSemiTransparent()
    {
        TransitionToSemiTransparent(0.5f, 0.3f, Ease.InOutQuad);
    }

    [ContextMenu("Test - Transition to Fullscreen")]
    private void TestFullscreen()
    {
        TransitionToFullscreen(0.3f, Ease.InOutQuad);
    }

    // These two were empty shells — `if (x) { }` with both branches blank, left behind when their
    // Debug.Log calls were stripped. A menu item that reports nothing is worse than none, since it
    // reads as "checked, all fine". They now actually print what they claim to check.

    [ContextMenu("Debug - Check Texture Connection")]
    private void DebugTextureConnection()
    {
        if (minimapImage == null)
        {
            Debug.LogWarning("[MinimapUI] minimapImage is not assigned.", this);
            return;
        }

        if (minimapImage.texture == null)
            Debug.LogWarning("[MinimapUI] RawImage has no texture — the minimap will be blank.", this);
        else
            Debug.Log($"[MinimapUI] Connected to '{minimapImage.texture.name}' " +
                      $"({minimapImage.texture.width}x{minimapImage.texture.height}).", this);
    }

    [ContextMenu("Debug - Check Size Settings")]
    private void DebugSizeSettings()
    {
        if (minimapPanel == null)
        {
            Debug.LogWarning("[MinimapUI] minimapPanel is not assigned.", this);
            return;
        }

        Debug.Log($"[MinimapUI] panel size={minimapPanel.sizeDelta} pos={minimapPanel.anchoredPosition} " +
                  $"| normal={normalSize} fullscreen={fullscreenSize}", this);
    }

    [ContextMenu("Debug - Force Fullscreen Size")]
    private void DebugForceFullscreenSize()
    {
        if (minimapPanel != null)
        {
            minimapPanel.anchorMin = new Vector2(0.5f, 0.5f);
            minimapPanel.anchorMax = new Vector2(0.5f, 0.5f);
            minimapPanel.pivot = new Vector2(0.5f, 0.5f);
            minimapPanel.anchoredPosition = Vector2.zero;
            minimapPanel.sizeDelta = fullscreenSize;
        }
    }
#endif
}

} // namespace SowurShield.Minimap
