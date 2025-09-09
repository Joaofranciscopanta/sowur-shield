using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the main game menu that opens with ESC key
/// Handles pausing, settings, save management, and quitting
/// </summary>
public class GameMenuManager : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private bool disablePlayerInputWhenOpen = true;
    
    [Header("Input")]
    [SerializeField] private InputActionReference menuToggleAction;
    
    [Header("Audio")]
    [SerializeField] private AudioClip menuOpenSound;
    [SerializeField] private AudioClip menuCloseSound;
    [SerializeField] private AudioSource audioSource;
    
    // State
    private bool isMenuOpen = false;
    private float previousTimeScale = 1f;
    
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
    }
    
    private void OnDestroy()
    {
        CleanupInput();
        
        // Restore time scale if destroyed while paused
        if (isMenuOpen && pauseGameWhenOpen)
        {
            Time.timeScale = previousTimeScale;
        }
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
            Debug.LogWarning("[GameMenuManager] Menu toggle action not assigned! ESC menu won't work.");
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
                Debug.Log("[GameMenuManager] Sleep confirmation panel is active, ignoring ESC key for menu.");
                return;
            }
        }
        
        ToggleMenu();
    }
    
    // ============================================================================
    // MENU CONTROL
    // ============================================================================
    
    public void ToggleMenu()
    {
        Debug.Log($"[GameMenuManager] ToggleMenu called. Current state: {(isMenuOpen ? "Open" : "Closed")}");
        
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
        
        Debug.Log("[GameMenuManager] Opening menu...");
        
        // Validate required components
        if (menuPanel == null)
        {
            Debug.LogError("[GameMenuManager] Cannot open menu - menuPanel is not assigned!");
            return;
        }
        
        if (menuUI == null)
        {
            Debug.LogError("[GameMenuManager] Cannot open menu - GameMenuUI component not found!");
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
        
        Debug.Log("[GameMenuManager] Menu opened successfully");
    }
    
    public void CloseMenu()
    {
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
        
        Debug.Log("[GameMenuManager] Menu closed");
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
        Debug.Log("[GameMenuManager] ShowSettings called");
        if (menuUI != null)
        {
            menuUI.ShowSettingsPanel();
        }
        else
        {
            Debug.LogError("[GameMenuManager] Settings panel requested but GameMenuUI not found!");
        }
    }
    
    public void ShowSaveInfo()
    {
        Debug.Log("[GameMenuManager] ShowSaveInfo called");
        if (menuUI != null)
        {
            menuUI.ShowSaveInfoPanel();
        }
        else
        {
            Debug.LogError("[GameMenuManager] Save info requested but GameMenuUI not found!");
        }
    }
    
    public void LoadGame()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile())
        {
            CloseMenu(); // Close menu first
            SaveManager.Instance.LoadGame();
        }
        else
        {
            if (menuUI != null)
            {
                menuUI.ShowNotification("No save file found!", true);
            }
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
        // Restore time scale before changing scenes
        Time.timeScale = 1f;
        
        // Load main menu scene (adjust scene name as needed)
        SceneManager.LoadScene("MainMenu");
    }
    
    public void DoQuitToDesktop()
    {
        // Restore time scale
        Time.timeScale = 1f;
        
        Debug.Log("[GameMenuManager] Quitting to desktop");
        
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