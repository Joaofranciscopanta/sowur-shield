using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using SowurShield.Core;

namespace SowurShield.Minimap
{

/// <summary>
/// Main controller for the minimap system
/// Manages three states: Normal (top-right corner), Semi-Transparent, and Fullscreen
/// Integrates with UIManager for proper window management
/// </summary>
public class MinimapController : MonoBehaviour, IUIWindow
{
    [Header("References")]
    [SerializeField] private MinimapCamera minimapCamera;
    [SerializeField] private MinimapUI minimapUI;
    [SerializeField] private Transform player;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleMinimapAction;
    [SerializeField] private InputActionReference zoomInAction;
    [SerializeField] private InputActionReference zoomOutAction;
    [SerializeField] private InputActionReference panUpAction;
    [SerializeField] private InputActionReference panDownAction;
    [SerializeField] private InputActionReference panLeftAction;
    [SerializeField] private InputActionReference panRightAction;

    [Header("State Settings")]
    [SerializeField] private MinimapState currentState = MinimapState.Normal;
    [SerializeField] private float semiTransparentOpacity = 0.5f;

    [Header("Fullscreen Settings")]
    [SerializeField] private float[] zoomLevels = { 0.5f, 1f, 2f };
    [SerializeField] private int currentZoomIndex = 1;
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float mousePanSensitivity = 1f;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private Ease transitionEase = Ease.InOutQuad;

    // Editor-only: its sole reader sits inside #if UNITY_EDITOR in LogDebug. Leaving the field
    // outside the guard means a player build compiles a field nothing ever reads (CS0414).
    #if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    #endif

    // State
    private bool isFullscreenMode = false;
    private Vector3 fullscreenPanOffset = Vector3.zero;
    private Vector3 lastMousePosition;
    private bool isMouseDragging = false;

    // References
    private PlayerMove playerMove;
    private GameTimeController timeController;

    // IUIWindow implementation
    public string WindowName => "Minimap";
    public int WindowPriority => 5; // Between GameMenu (1) and Inventory (10)
    public bool IsWindowOpen => isFullscreenMode;
    public bool CanCloseWithEsc => true;

    // Properties
    public MinimapState CurrentState => currentState;
    public bool IsInFullscreenMode => isFullscreenMode;

    // Singleton
    public static MinimapController Instance { get; private set; }

    // Events
    public System.Action<MinimapState> OnStateChanged;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Auto-find references if not assigned
        if (minimapCamera == null)
            minimapCamera = GetComponentInChildren<MinimapCamera>();

        if (minimapUI == null)
            minimapUI = GetComponentInChildren<MinimapUI>();

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    private void Start()
    {
        // Find player references
        FindPlayerReferences();

        // Setup input
        SetupInput();

        // Initialize state
        InitializeState();

        // Register with UIManager
        RegisterWithUIManager();

        LogDebug("MinimapController initialized");
    }

    private void OnDestroy()
    {
        CleanupInput();
        UnregisterFromUIManager();

        // Restore player movement if destroyed while in fullscreen
        if (isFullscreenMode)
        {
            RestorePlayerMovement();
        }
    }

    private void Update()
    {
        // Handle fullscreen pan controls
        if (isFullscreenMode)
        {
            HandleFullscreenPan();
            HandleMouseDrag();
        }
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private void FindPlayerReferences()
    {
        if (player != null)
        {
            playerMove = player.GetComponent<PlayerMove>();
        }

        timeController = GameTimeController.instance;
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();
    }

    private void SetupInput()
    {
        if (toggleMinimapAction != null)
        {
            toggleMinimapAction.action.Enable();
            toggleMinimapAction.action.performed += OnToggleMinimap;
        }

        if (zoomInAction != null)
        {
            zoomInAction.action.Enable();
            zoomInAction.action.performed += OnZoomIn;
        }

        if (zoomOutAction != null)
        {
            zoomOutAction.action.Enable();
            zoomOutAction.action.performed += OnZoomOut;
        }

        // Pan actions
        if (panUpAction != null) panUpAction.action.Enable();
        if (panDownAction != null) panDownAction.action.Enable();
        if (panLeftAction != null) panLeftAction.action.Enable();
        if (panRightAction != null) panRightAction.action.Enable();
    }

    private void CleanupInput()
    {
        if (toggleMinimapAction != null)
        {
            toggleMinimapAction.action.performed -= OnToggleMinimap;
            toggleMinimapAction.action.Disable();
        }

        if (zoomInAction != null)
        {
            zoomInAction.action.performed -= OnZoomIn;
            zoomInAction.action.Disable();
        }

        if (zoomOutAction != null)
        {
            zoomOutAction.action.performed -= OnZoomOut;
            zoomOutAction.action.Disable();
        }

        // Disable pan actions
        if (panUpAction != null) panUpAction.action.Disable();
        if (panDownAction != null) panDownAction.action.Disable();
        if (panLeftAction != null) panLeftAction.action.Disable();
        if (panRightAction != null) panRightAction.action.Disable();
    }

    private void RegisterWithUIManager()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterWindow(this);
        }
    }

    private void UnregisterFromUIManager()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterWindow(this);
        }
    }

    private void InitializeState()
    {
        // Start in normal mode
        SetState(MinimapState.Normal, immediate: true);
    }

    // ============================================================================
    // STATE MANAGEMENT
    // ============================================================================

    /// <summary>
    /// Toggle to next minimap state in cycle: Normal → SemiTransparent → Fullscreen → Normal
    /// </summary>
    public void ToggleMinimapState()
    {
        MinimapState nextState;

        switch (currentState)
        {
            case MinimapState.Normal:
                nextState = MinimapState.SemiTransparent;
                break;

            case MinimapState.SemiTransparent:
                nextState = MinimapState.Fullscreen;
                break;

            case MinimapState.Fullscreen:
                nextState = MinimapState.Normal;
                break;

            default:
                nextState = MinimapState.Normal;
                break;
        }

        SetState(nextState);
    }

    /// <summary>
    /// Set minimap to specific state
    /// </summary>
    public void SetState(MinimapState newState, bool immediate = false)
    {
        if (currentState == newState && !immediate)
            return;

        MinimapState previousState = currentState;
        currentState = newState;

        // Handle state transitions
        switch (newState)
        {
            case MinimapState.Normal:
                TransitionToNormal(immediate);
                break;

            case MinimapState.SemiTransparent:
                TransitionToSemiTransparent(immediate);
                break;

            case MinimapState.Fullscreen:
                TransitionToFullscreen(immediate);
                break;
        }

        // Trigger event
        OnStateChanged?.Invoke(newState);
    }

    private void TransitionToNormal(bool immediate)
    {
        isFullscreenMode = false;

        // Update UI
        if (minimapUI != null)
        {
            minimapUI.TransitionToNormal(immediate ? 0f : transitionDuration, transitionEase);
        }

        // Update camera
        if (minimapCamera != null)
        {
            minimapCamera.SetFollowPlayer(true);
            minimapCamera.ResetZoom(immediate);
        }

        // Restore player movement
        RestorePlayerMovement();

        // Close window in UIManager (if it was registered as open)
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryCloseWindow(this);
        }
    }

    private void TransitionToSemiTransparent(bool immediate)
    {
        isFullscreenMode = false;

        // Update UI
        if (minimapUI != null)
        {
            minimapUI.TransitionToSemiTransparent(semiTransparentOpacity, immediate ? 0f : transitionDuration, transitionEase);
        }

        // Camera stays following player
        if (minimapCamera != null)
        {
            minimapCamera.SetFollowPlayer(true);
        }

        // Restore player movement (if it was disabled)
        RestorePlayerMovement();

        // Close window in UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryCloseWindow(this);
        }
    }

    private void TransitionToFullscreen(bool immediate)
    {
        // Try to register as open window with UIManager
        if (UIManager.Instance != null)
        {
            bool success = UIManager.Instance.TryOpenWindow(this);
            if (!success)
            {
                // Revert to previous state
                currentState = MinimapState.Normal;
                return;
            }
        }

        isFullscreenMode = true;
        fullscreenPanOffset = Vector3.zero;
        currentZoomIndex = 1; // Reset to default zoom

        // Update UI
        if (minimapUI != null)
        {
            minimapUI.TransitionToFullscreen(immediate ? 0f : transitionDuration, transitionEase);
        }

        // Update camera
        if (minimapCamera != null)
        {
            minimapCamera.SetFollowPlayer(false);
            minimapCamera.SetPanOffset(fullscreenPanOffset);
            minimapCamera.SetZoomLevel(zoomLevels[currentZoomIndex], immediate);
        }

        // Disable player movement
        DisablePlayerMovement();
    }

    // ============================================================================
    // INPUT HANDLING
    // ============================================================================

    private void OnToggleMinimap(InputAction.CallbackContext context)
    {
        ToggleMinimapState();
    }

    private void OnZoomIn(InputAction.CallbackContext context)
    {
        if (isFullscreenMode)
        {
            ZoomIn();
        }
    }

    private void OnZoomOut(InputAction.CallbackContext context)
    {
        if (isFullscreenMode)
        {
            ZoomOut();
        }
    }

    // ============================================================================
    // FULLSCREEN CONTROLS
    // ============================================================================

    private void HandleFullscreenPan()
    {
        if (!isFullscreenMode)
            return;

        Vector3 panDelta = Vector3.zero;

        // Keyboard panning (arrow keys or input actions)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed || (panUpAction != null && panUpAction.action.ReadValue<float>() > 0.5f))
                panDelta.y += panSpeed * Time.deltaTime;

            if (Keyboard.current.downArrowKey.isPressed || (panDownAction != null && panDownAction.action.ReadValue<float>() > 0.5f))
                panDelta.y -= panSpeed * Time.deltaTime;

            if (Keyboard.current.leftArrowKey.isPressed || (panLeftAction != null && panLeftAction.action.ReadValue<float>() > 0.5f))
                panDelta.x -= panSpeed * Time.deltaTime;

            if (Keyboard.current.rightArrowKey.isPressed || (panRightAction != null && panRightAction.action.ReadValue<float>() > 0.5f))
                panDelta.x += panSpeed * Time.deltaTime;
        }

        if (panDelta != Vector3.zero)
        {
            ApplyPan(panDelta);
        }
    }

    private void HandleMouseDrag()
    {
        if (!isFullscreenMode)
            return;

        if (Mouse.current == null)
            return;

        // Check for mouse drag (left click + drag)
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
                isMouseDragging = true;
            }

            if (Mouse.current.leftButton.isPressed && isMouseDragging)
            {
                Vector3 currentMousePosition = Mouse.current.position.ReadValue();
                Vector3 mouseDelta = currentMousePosition - lastMousePosition;

                // Convert screen space delta to world space delta
                // Invert Y because screen space Y is inverted
                Vector3 worldDelta = new Vector3(
                    -mouseDelta.x * mousePanSensitivity * 0.01f,
                    -mouseDelta.y * mousePanSensitivity * 0.01f,
                    0
                );

                ApplyPan(worldDelta);

                lastMousePosition = currentMousePosition;
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isMouseDragging = false;
            }

            // Handle scroll wheel zoom
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.1f)
            {
                if (scroll > 0)
                    ZoomIn();
                else
                    ZoomOut();
            }
        }
    }

    private void ApplyPan(Vector3 delta)
    {
        fullscreenPanOffset += delta;

        if (minimapCamera != null)
        {
            minimapCamera.SetPanOffset(fullscreenPanOffset);
        }
    }

    /// <summary>
    /// Zoom in to next zoom level
    /// </summary>
    public void ZoomIn()
    {
        if (!isFullscreenMode)
            return;

        if (currentZoomIndex < zoomLevels.Length - 1)
        {
            currentZoomIndex++;
            ApplyZoom();
        }
    }

    /// <summary>
    /// Zoom out to previous zoom level
    /// </summary>
    public void ZoomOut()
    {
        if (!isFullscreenMode)
            return;

        if (currentZoomIndex > 0)
        {
            currentZoomIndex--;
            ApplyZoom();
        }
    }

    private void ApplyZoom()
    {
        if (minimapCamera != null)
        {
            minimapCamera.SetZoomLevel(zoomLevels[currentZoomIndex]);
        }

        if (minimapUI != null)
        {
            minimapUI.UpdateZoomIndicator(zoomLevels[currentZoomIndex]);
        }
    }

    // ============================================================================
    // PLAYER MOVEMENT CONTROL
    // ============================================================================

    private void DisablePlayerMovement()
    {
        if (playerMove != null)
        {
            playerMove.DisableMovement();
        }
    }

    private void RestorePlayerMovement()
    {
        if (playerMove != null)
        {
            playerMove.EnableMovement();
        }
    }

    // ============================================================================
    // IUIWINDOW IMPLEMENTATION
    // ============================================================================

    public void OpenWindow()
    {
        // Called by UIManager when opening in fullscreen mode
        SetState(MinimapState.Fullscreen);
    }

    public void CloseWindow()
    {
        // Called by UIManager when closing (ESC key)
        SetState(MinimapState.Normal);
    }

    public void OnWindowBlocked(string blockedBy)
    {
        // Could show a message to the user here
    }

    // ============================================================================
    // PUBLIC API
    // ============================================================================

    /// <summary>
    /// Programmatically set minimap to specific state
    /// </summary>
    public void SetMinimapState(MinimapState state)
    {
        SetState(state);
    }

    /// <summary>
    /// Check if minimap is currently in fullscreen mode
    /// </summary>
    public bool IsFullscreen()
    {
        return isFullscreenMode;
    }

    // ============================================================================
    // DEBUG & LOGGING
    // ============================================================================

    // The method stays defined in every build so its call sites need no guards; only the body
    // and the enableDebugLogs field are Editor-only. Guarding just the body while leaving the
    // field visible is what produced CS0414 — a field with no readers in a player build.
    private void LogDebug(string message)
    {
        #if UNITY_EDITOR
        if (enableDebugLogs)
        {
            Debug.Log($"[MinimapController] {message}", this);
        }
        #endif
    }
}

/// <summary>
/// Minimap display states
/// </summary>
public enum MinimapState
{
    Normal,          // Top-right corner, 100% opacity
    SemiTransparent, // Top-right corner, 50% opacity
    Fullscreen       // Center screen, 100% opacity, zoom/pan enabled
}

} // namespace SowurShield.Minimap
