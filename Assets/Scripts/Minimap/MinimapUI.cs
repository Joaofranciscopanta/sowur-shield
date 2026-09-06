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

    /// <summary>
    /// Pulls the map image inside the frame's *painted* area.
    ///
    /// frame_decorative_border is 512px of which the art occupies x[46..472], y[67..458] — roughly
    /// 9-13% transparent padding, and not symmetrical. Border and map share the same rect, so the
    /// map ran out past the visible frame and the padding showed as a black gutter between the
    /// two. Measured from the art, not guessed, and expressed as fractions so it holds at both
    /// 200px and 800px.
    ///
    /// This is the same class of trap as the panel frames: the rect is not the painted area.
    /// </summary>
    private void InsetMapInsideFrame()
    {
        if (minimapImage == null) return;

        var mapRect = minimapImage.rectTransform;

        // A moldura procedural tem espessura CONSTANTE em pixels (nao uma fracao do rect),
        // porque e desenhada com 9-slice honesto. Entao o recuo do mapa tambem tem de ser
        // em pixels: ancorar por fracao encolhia o mapa proporcionalmente e, no fullscreen
        // de 1720px, abria um vao enorme entre o mapa e a moldura.
        mapRect.anchorMin = Vector2.zero;
        mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = new Vector2(FrameThicknessPx, FrameThicknessPx);
        mapRect.offsetMax = new Vector2(-FrameThicknessPx, -FrameThicknessPx);
    }

    /// <summary>
    /// Espessura pintada da moldura, em pixels de UI.
    ///
    /// A moldura antiga era o PNG frame_decorative_border com Image.Type.Sliced e borda de
    /// 9-slice de 24px, enquanto o ornamento pintado tem ~66px de largura. O Sliced nunca
    /// comprime a borda: fixava 24px de canto e esticava os outros 42px de desenho ao longo
    /// de cada aresta — no fullscreen, mais de 8x. Era essa a causa das bordas que nao
    /// batiam. A moldura de agora e gerada por codigo com a borda de 9-slice igual a
    /// espessura desenhada, entao so a faixa lisa estica.
    /// </summary>
    private const float FrameThicknessPx = 14f;

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

        ApplyFrameArt();
        InsetMapInsideFrame();
        SyncCameraAspect(normalSize);
    }

    /// <summary>
    /// Poe a moldura procedural no lugar da arte fatiada errado, e desliga o miolo.
    ///
    /// Dois defeitos numa linha so: o Image estava Sliced com borda de 24px sobre um
    /// ornamento de 66px (ver <see cref="FrameThicknessPx"/>), e com fillCenter LIGADO —
    /// isto e, pintava um retangulo opaco por cima do mapa que a moldura deveria emoldurar.
    /// </summary>
    private void ApplyFrameArt()
    {
        if (borderImage == null) return;

        borderImage.sprite = MinimapFrameSprite.Get();
        borderImage.type = Image.Type.Sliced;
        borderImage.fillCenter = false;   // a moldura emoldura; nao tapa
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;
        borderImage.pixelsPerUnitMultiplier = 1f;

        // A moldura tem de ficar por cima do mapa e por baixo dos marcadores, senao o
        // marcador do jogador desaparece sob o ornamento ao chegar perto da borda. Na cena
        // a ordem era MinimapImage, PlayerMarker, Border — com a moldura por CIMA de tudo.
        borderImage.rectTransform.SetAsLastSibling();
        if (playerMarker != null)
            playerMarker.SetAsLastSibling();
        if (infoPanel != null)
            infoPanel.transform.SetAsLastSibling();

        PlaceInfoPanel();
    }

    /// <summary>
    /// Poe o painel de informacao DENTRO da area util, recuado da moldura.
    ///
    /// Estava ancorado na base do rect com pivot 0 e posicao zero, ou seja encostado ao
    /// limite exterior — no fullscreen a legenda ficava por baixo do ornamento de madeira,
    /// meia dentro e meia fora. A moldura tem espessura fixa, entao o recuo tambem e fixo.
    /// </summary>
    private void PlaceInfoPanel()
    {
        if (infoPanel == null) return;

        var rt = infoPanel.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, FrameThicknessPx + InfoPanelMargin);
    }

    private const float InfoPanelMargin = 8f;

    /// <summary>
    /// Faz a textura do minimapa nascer com a mesma proporcao da janela onde e desenhada.
    ///
    /// O alvo de render era sempre quadrado. Nesta cena o painel de fullscreen mede
    /// 1720x940, entao o mundo aparecia esticado quase para o dobro da largura; e mesmo o
    /// HUD "quadrado" nao era quadrado por dentro, porque a janela util fica menor que o
    /// rect pela espessura da moldura.
    /// </summary>
    private void SyncCameraAspect(Vector2 panelSize)
    {
        if (cachedMinimapCamera == null)
            cachedMinimapCamera = FindFirstObjectByType<MinimapCamera>();
        if (cachedMinimapCamera == null) return;

        float w = panelSize.x - FrameThicknessPx * 2f;
        float h = panelSize.y - FrameThicknessPx * 2f;
        if (w <= 1f || h <= 1f) return;

        cachedMinimapCamera.SetAspect(w / h);

        // SetAspect pode trocar o RenderTexture por um de outro tamanho; a RawImage guarda
        // a referencia antiga e ficaria a desenhar uma textura ja libertada (um quadrado
        // preto). Reconectar e barato e cobre tambem o caso de ainda nao estar ligada.
        ConnectToMinimapCamera();
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

        // O painel toma a proporcao do MUNDO, nao um retangulo fixo.
        //
        // fullscreenSize estava em 1720x940 (1,86:1) enquanto a quinta mede ~30x31 unidades
        // — quase quadrada. Por melhor que fosse o enquadramento, sobravam faixas vazias
        // dos dois lados: metade da largura do painel nao tinha mundo para mostrar. Com o
        // painel a seguir a forma do mundo, o mapa preenche a moldura toda.
        Vector2 target = FullscreenSizeForWorld();

        // Change anchor to center
        minimapPanel.anchorMin = new Vector2(0.5f, 0.5f);
        minimapPanel.anchorMax = new Vector2(0.5f, 0.5f);
        minimapPanel.pivot = new Vector2(0.5f, 0.5f);

        // Force anchored position to zero first (center)
        minimapPanel.anchoredPosition = Vector2.zero;

        // Animate to center position and fullscreen size
        AnimatePosition(Vector2.zero, duration, ease);
        AnimateSize(target, duration, ease);
        AnimateOpacity(1f, duration, ease);

        // Update state text
        UpdateStateText("Fullscreen");
    }

    /// <summary>
    /// Tamanho do painel de fullscreen: a forma do mundo, dentro do que o ecra permite.
    ///
    /// <see cref="fullscreenSize"/> passa a ser o LIMITE (a caixa maxima que o painel pode
    /// ocupar), e nao a forma imposta. A altura manda, porque um ecra e sempre mais largo
    /// que alto; a largura sai da proporcao do mundo mais a moldura, e e cortada pelo
    /// limite caso o mundo seja muito largo.
    /// </summary>
    private Vector2 FullscreenSizeForWorld()
    {
        Vector2 limit = fullscreenSize;

        if (cachedMinimapCamera == null)
            cachedMinimapCamera = FindFirstObjectByType<MinimapCamera>();

        Bounds world;
        if (cachedMinimapCamera == null || !cachedMinimapCamera.TryGetWorldBounds(out world))
            return limit;

        if (world.size.y <= 0.01f) return limit;

        float worldAspect = world.size.x / world.size.y;
        if (worldAspect <= 0.01f || float.IsNaN(worldAspect)) return limit;

        // A area do mapa e o painel menos a moldura, dos dois lados.
        float mapH = limit.y - FrameThicknessPx * 2f;
        float mapW = mapH * worldAspect;

        float maxMapW = limit.x - FrameThicknessPx * 2f;
        if (mapW > maxMapW)
        {
            // Mundo largo demais para a caixa: manda a largura e a altura acompanha.
            mapW = maxMapW;
            mapH = mapW / worldAspect;
        }

        return new Vector2(mapW + FrameThicknessPx * 2f, mapH + FrameThicknessPx * 2f);
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

        // A proporcao e acertada pelo tamanho ALVO, nao a cada frame da animacao: refazer o
        // RenderTexture e caro e faria dezenas de realocacoes durante a transicao. O mapa
        // acompanha a animacao levemente esticado por ~0,3s e assenta certo no fim.
        SyncCameraAspect(targetSize);

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

        // Mapeado sobre a area UTIL — o rect menos a moldura — e nao sobre o rect inteiro.
        // Usar sizeDelta punha o marcador progressivamente fora do sitio quanto mais longe
        // do centro, ate cair por baixo do ornamento nas bordas: o mapa ocupa so a parte de
        // dentro, mas o marcador era espalhado pela largura toda do painel.
        float usableW = minimapPanel.rect.width - FrameThicknessPx * 2f;
        float usableH = minimapPanel.rect.height - FrameThicknessPx * 2f;

        Vector2 localPos = new Vector2(
            (viewportPos.x - 0.5f) * usableW,
            (viewportPos.y - 0.5f) * usableH
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
    /// The rect the map itself is drawn into — inset inside the decorative frame, so noticeably
    /// smaller than the panel. Anything sizing against "how big is the map on screen" wants this,
    /// not the panel.
    /// </summary>
    public RectTransform GetMapImageRect()
    {
        return minimapImage != null ? minimapImage.rectTransform : null;
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
