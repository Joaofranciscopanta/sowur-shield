using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SowurShield.Core
{

/// <summary>
/// Manages the main game menu that opens with ESC key
/// Handles pausing, settings, save management, and quitting
/// </summary>
public class GameMenuManager : MonoBehaviour, IUIWindow
{
    [Header("Menu Settings")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private bool disablePlayerInputWhenOpen = true;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Input")]
    [SerializeField] private InputActionReference menuToggleAction;

    [Header("Audio")]
    [SerializeField] private AudioClip menuOpenSound;
    [SerializeField] private AudioClip menuCloseSound;
    [SerializeField] private AudioSource audioSource;

    // State
    private bool isMenuOpen = false;
    private float previousTimeScale = 1f;

    public bool IsMapOpen { get; private set;}


    // References
    private PlayerMove playerMove;
    private GameTimeController timeController;
    private GameMenuUI menuUI;

    // Events
    public System.Action<bool> OnMenuStateChanged; // bool = isOpen

    // Singleton for easy access
    public static GameMenuManager Instance { get; private set; }

    // Properties
    public bool IsMenuOpen => isMenuOpen;

    // IUIWindow implementation
    public string WindowName => "GameMenu";
    public int WindowPriority => 1;
    public bool IsWindowOpen => isMenuOpen;
    public bool CanCloseWithEsc => true;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Get components
        menuUI = GetComponent<GameMenuUI>();

        // Find audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Find references
        FindGameReferences();

        // Setup input
        SetupInput();

        // Initialize menu state
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // Register with UIManager
        RegisterWithUIManager();
    }

    private void OnDestroy()
    {
        CleanupInput();
        UnregisterFromUIManager();

        // Restore time scale if destroyed while paused
        if (isMenuOpen && pauseGameWhenOpen)
        {
            Time.timeScale = previousTimeScale;
        }
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

    public void OnWindowBlocked(string blockedBy)
    {
    }

    private void FindGameReferences()
    {
        // Find player controller
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();

        // Find time controller
        if (timeController == null)
            timeController = FindFirstObjectByType<GameTimeController>();
    }

    // ============================================================================
    // INPUT SYSTEM
    // ============================================================================

    private void SetupInput()
    {
        if (menuToggleAction != null)
        {
            menuToggleAction.action.Enable();
            menuToggleAction.action.performed += OnMenuTogglePressed;
        }
        else
        {
        }
    }

    private void CleanupInput()
    {
        if (menuToggleAction != null)
        {
            menuToggleAction.action.performed -= OnMenuTogglePressed;
            menuToggleAction.action.Disable();
        }
    }

    private void OnMenuTogglePressed(InputAction.CallbackContext context)
    {
        // Don't open menu if sleep confirmation panel is active
        SleepConfirmationPanel sleepPanel = FindFirstObjectByType<SleepConfirmationPanel>();
        if (sleepPanel != null && sleepPanel.gameObject.activeInHierarchy)
        {
            // Check if the panel is visible by looking for an active panel container
            bool isPanelVisible = false;
            var panelContainerField = typeof(SleepConfirmationPanel).GetField("panelContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            GameObject panelContainer = panelContainerField?.GetValue(sleepPanel) as GameObject;

            if (panelContainer != null && panelContainer.activeInHierarchy)
            {
                isPanelVisible = true;
            }

            if (isPanelVisible)
            {
                return;
            }
        }

            if (IsMapOpen)
                return;
        ToggleMenu();
    }

    // ============================================================================
    // MENU CONTROL
    // ============================================================================

    public void SetMapOpen(bool value)
    {
        IsMapOpen = value;
    }

    public void ToggleMenu()
    {

        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (isMenuOpen) return;

        // Use UIManager to try opening this window
        if (UIManager.Instance != null && !UIManager.Instance.TryOpenWindow(this))
        {
            return; // Window was blocked by another window
        }

        // If no UIManager, fall back to direct opening
        if (UIManager.Instance == null)
        {
            OpenWindow();
        }
    }

    public void CloseMenu()
    {
        if (!isMenuOpen) return;

        // Notify UIManager if available
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryCloseWindow(this);
        }
        else
        {
            // Direct close if no UIManager
            CloseWindow();
        }
    }

    // IUIWindow implementation methods
    public void OpenWindow()
    {
        // Direct opening logic (called by UIManager or fallback)
        if (isMenuOpen) return;

        // Validate required components
        if (menuPanel == null)
        {
            return;
        }

        if (menuUI == null)
        {
            return;
        }

        isMenuOpen = true;

        // Show menu panel
        menuPanel.SetActive(true);

        // Make sure main panel is shown
        menuUI.ShowMainPanel();

        // Pause game if enabled
        if (pauseGameWhenOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Pause time controller
            if (timeController != null)
                timeController.isPaused = true;
        }

        // Disable player input if enabled
        if (disablePlayerInputWhenOpen && playerMove != null)
        {
            // Note: PlayerMove doesn't have enable/disable methods yet
            // This would need to be implemented in PlayerMove script
        }

        // Play sound
        PlaySound(menuOpenSound);

        // Trigger events
        OnMenuStateChanged?.Invoke(true);

        // Update cursor state
        SetCursorState(true);

    }

    public void CloseWindow()
    {
        // Direct closing logic (called by UIManager or fallback)
        if (!isMenuOpen) return;

        isMenuOpen = false;

        // Hide menu panel
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // Unpause game if it was paused
        if (pauseGameWhenOpen)
        {
            Time.timeScale = previousTimeScale;

            // Unpause time controller
            if (timeController != null)
                timeController.isPaused = false;
        }

        // Re-enable player input
        if (disablePlayerInputWhenOpen && playerMove != null)
        {
            // Re-enable player input here
        }

        // Play sound
        PlaySound(menuCloseSound);

        // Trigger events
        OnMenuStateChanged?.Invoke(false);

        // Update cursor state
        SetCursorState(false);

    }

    // ============================================================================
    // MENU ACTIONS
    // ============================================================================

    public void ResumeGame()
    {
        CloseMenu();
    }

    public void ShowSettings()
    {
        if (menuUI != null)
        {
            menuUI.ShowSettingsPanel();
        }
        else
        {
        }
    }

    public void ShowSaveInfo()
    {
        if (menuUI != null)
        {
            menuUI.ShowSaveInfoPanel();
        }
        else
        {
        }
    }

    public void LoadGame()
    {
        ShowLoadSlotPicker();
    }

    public void ShowSaveSlotPicker()
    {
        menuUI?.ShowSaveSlotPanel();
    }

    public void ShowLoadSlotPicker()
    {
        if (SaveManager.Instance != null &&
            System.Linq.Enumerable.Any(SaveManager.Instance.GetAllSlotInfos(), s => !s.isEmpty))
        {
            menuUI?.ShowLoadSlotPanel();
        }
        else
        {
            menuUI?.ShowNotification("No save files found!", true);
        }
    }

    public void QuitToMainMenu()
    {
        // Show confirmation dialog
        if (menuUI != null)
        {
            menuUI.ShowQuitConfirmation(false); // false = quit to main menu
        }
        else
        {
            DoQuitToMainMenu();
        }
    }

    public void QuitToDesktop()
    {
        // Show confirmation dialog
        if (menuUI != null)
        {
            menuUI.ShowQuitConfirmation(true); // true = quit to desktop
        }
        else
        {
            DoQuitToDesktop();
        }
    }

    // ============================================================================
    // QUIT ACTIONS (called after confirmation)
    // ============================================================================

    public void DoQuitToMainMenu()
    {

        // Optional: Auto-save before quitting (commented out by default to match current UX)
        // if (SaveManager.Instance != null)
        // {
        //     Debug.Log("[GameMenuManager] Auto-saving before returning to main menu...");
        //     SaveManager.Instance.SaveGame();
        // }

        // Stop game music before returning to main menu
        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.OnReturnToMainMenu();
        }

        // Restore time scale before changing scenes
        Time.timeScale = 1f;

        // Re-enable cursor for main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player input to prevent interactions during transition
        if (playerMove != null)
        {
            playerMove.DisableMovement();
        }

        // Pause time controller if it exists
        if (timeController != null)
        {
            timeController.isPaused = false; // Reset pause state
        }

        // Close the menu immediately to prevent double-clicks
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        isMenuOpen = false;

        // Use SceneTransitionManager if available for smooth transition
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainMenu();
        }
        else
        {
            // Fallback to direct scene loading
            SceneManager.LoadScene(mainMenuSceneName);
        }

    }

    public void DoQuitToDesktop()
    {
        // Restore time scale
        Time.timeScale = 1f;


        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    private void SetCursorState(bool visible)
    {
        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void RefreshReferences()
    {
        FindGameReferences();
    }

    // ============================================================================
    // SCENE MANAGEMENT
    // ============================================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh references when scene changes
        FindGameReferences();

        // Close menu if it was open
        if (isMenuOpen)
        {
            isMenuOpen = false;
            if (menuPanel != null)
                menuPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ============================================================================
    // DEBUG/EDITOR METHODS
    // ============================================================================

    #if UNITY_EDITOR
    [ContextMenu("Toggle Menu")]
    public void DebugToggleMenu()
    {
        ToggleMenu();
    }

    [ContextMenu("Open Menu")]
    public void DebugOpenMenu()
    {
        OpenMenu();
    }

    [ContextMenu("Close Menu")]
    public void DebugCloseMenu()
    {
        CloseMenu();
    }

    [ContextMenu("Test Menu Sounds")]
    public void DebugTestSounds()
    {
        PlaySound(menuOpenSound);
    }
    #endif
}

} // namespace SowurShield.Core
