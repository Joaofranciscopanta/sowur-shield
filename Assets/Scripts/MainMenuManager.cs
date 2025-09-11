using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main menu manager that coordinates the main menu UI, settings, and scene transitions
/// Singleton pattern for easy access and consistency with GameMenuManager
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private MainMenuUI menuUI;
    [SerializeField] private GameObject backgroundElements;
    [SerializeField] private GameObject musicPlayer;
    
    [Header("Menu Settings")]
    [SerializeField] private bool pauseOnStart = false; // Usually false for main menu
    [SerializeField] private bool showCursorOnStart = true;
    
    [Header("Input")]
    [SerializeField] private InputActionReference navigationUpAction;
    [SerializeField] private InputActionReference navigationDownAction;
    [SerializeField] private InputActionReference confirmAction;
    [SerializeField] private InputActionReference cancelAction;
    
    [Header("Audio")]
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private float musicVolume = 0.7f;
    
    // Singleton
    public static MainMenuManager Instance { get; private set; }
    
    // State
    private bool isInitialized = false;
    
    // Events
    public System.Action OnMenuInitialized;
    public System.Action OnMenuDestroyed;
    
    private void Awake()
    {
        // Singleton setup (don't use DontDestroyOnLoad for main menu manager)
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Find UI reference if not assigned
        if (menuUI == null)
            menuUI = FindFirstObjectByType<MainMenuUI>();
            
        // Find music source if not assigned
        if (menuMusicSource == null)
            menuMusicSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        InitializeMainMenu();
    }
    
    private void OnDestroy()
    {
        OnMenuDestroyed?.Invoke();
        
        // Clear singleton reference when destroyed (scene change)
        if (Instance == this)
            Instance = null;
    }
    
    // ============================================================================
    // INITIALIZATION
    // ============================================================================
    
    private void InitializeMainMenu()
    {
        if (isInitialized) return;
        
        Debug.Log("[MainMenuManager] Initializing main menu...");
        
        // Setup cursor and input
        SetupCursorAndInput();
        
        // Setup audio
        SetupAudio();
        
        // Setup UI
        SetupUI();
        
        // Pause settings
        SetupTimeAndPause();
        
        // Mark as initialized
        isInitialized = true;
        
        // Trigger event
        OnMenuInitialized?.Invoke();
        
        Debug.Log("[MainMenuManager] Main menu initialization complete");
    }
    
    private void SetupCursorAndInput()
    {
        // Show cursor in main menu
        if (showCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // Enable input actions if assigned
        EnableInputActions();
        
        Debug.Log("[MainMenuManager] Cursor and input configured");
    }
    
    private void SetupAudio()
    {
        if (playMusicOnStart && menuMusicSource != null && menuMusicClip != null)
        {
            // Set up background music
            menuMusicSource.clip = menuMusicClip;
            menuMusicSource.loop = true;
            menuMusicSource.volume = musicVolume * PlayerPrefs.GetFloat("MusicVolume", 1f) * PlayerPrefs.GetFloat("MasterVolume", 1f);
            menuMusicSource.Play();
            
            Debug.Log("[MainMenuManager] Background music started");
        }
    }
    
    private void SetupUI()
    {
        if (menuUI != null)
        {
            // UI is already configured in its own Start method
            Debug.Log("[MainMenuManager] UI setup complete");
        }
        else
        {
            Debug.LogError("[MainMenuManager] MainMenuUI not found! Make sure MainMenuUI script is attached to a GameObject in the scene.");
        }
    }
    
    private void SetupTimeAndPause()
    {
        // Ensure time is running normally in main menu
        Time.timeScale = pauseOnStart ? 0f : 1f;
        
        Debug.Log($"[MainMenuManager] Time scale set to: {Time.timeScale}");
    }
    
    private void EnableInputActions()
    {
        // Enable navigation input actions if assigned
        if (navigationUpAction != null)
            navigationUpAction.action?.Enable();
            
        if (navigationDownAction != null)
            navigationDownAction.action?.Enable();
            
        if (confirmAction != null)
            confirmAction.action?.Enable();
            
        if (cancelAction != null)
            cancelAction.action?.Enable();
    }
    
    private void DisableInputActions()
    {
        // Disable navigation input actions
        if (navigationUpAction != null)
            navigationUpAction.action?.Disable();
            
        if (navigationDownAction != null)
            navigationDownAction.action?.Disable();
            
        if (confirmAction != null)
            confirmAction.action?.Disable();
            
        if (cancelAction != null)
            cancelAction.action?.Disable();
    }
    
    // ============================================================================
    // PUBLIC API
    // ============================================================================
    
    /// <summary>
    /// Start a new game (called by MainMenuUI)
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[MainMenuManager] Starting new game...");
        
        // Disable input to prevent multiple clicks
        DisableInputActions();
        
        // Use SceneTransitionManager if available, otherwise direct load
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainGameScene");
        }
    }
    
    /// <summary>
    /// Continue existing game (called by MainMenuUI)
    /// </summary>
    public void ContinueGame()
    {
        Debug.Log("[MainMenuManager] Continuing game...");
        
        // Disable input to prevent multiple clicks
        DisableInputActions();
        
        // Use SceneTransitionManager if available, otherwise direct load
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadMainGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainGameScene");
        }
    }
    
    /// <summary>
    /// Quit the application
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("[MainMenuManager] Quitting application...");
        
        // Save any necessary data before quitting
        PlayerPrefs.Save();
        
        // Quit
        Application.Quit();
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    /// <summary>
    /// Refresh the menu UI (useful after settings changes)
    /// </summary>
    public void RefreshUI()
    {
        if (menuUI != null)
        {
            menuUI.RefreshSaveStatus();
        }
        
        // Update audio volumes
        UpdateAudioVolumes();
        
        Debug.Log("[MainMenuManager] UI refreshed");
    }
    
    /// <summary>
    /// Update audio volumes based on current settings
    /// </summary>
    public void UpdateAudioVolumes()
    {
        if (menuMusicSource != null)
        {
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            menuMusicSource.volume = this.musicVolume * masterVolume * musicVolume;
        }
    }
    
    /// <summary>
    /// Show a specific panel (useful for external navigation)
    /// </summary>
    public void ShowPanel(string panelName)
    {
        if (menuUI != null)
        {
            menuUI.ShowPanel(panelName);
        }
    }
    
    // ============================================================================
    // INPUT HANDLERS (if using direct input instead of UI buttons)
    // ============================================================================
    
    private void Update()
    {
        // Handle ESC key to navigate back in menus
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            OnCancelPressed();
        }
    }
    
    private void OnCancelPressed()
    {
        // Handle back navigation or quit confirmation
        if (menuUI != null)
        {
            // For now, just show main panel
            menuUI.ShowPanel("main");
        }
    }
    
    // ============================================================================
    // SAVE SYSTEM INTEGRATION
    // ============================================================================
    
    /// <summary>
    /// Check if a save file exists and update UI accordingly
    /// </summary>
    public bool HasSaveFile()
    {
        return SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();
    }
    
    /// <summary>
    /// Get save file information for display
    /// </summary>
    public SaveFileInfo GetSaveFileInfo()
    {
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.GetSaveFileInfo();
        }
        return null;
    }
    
    // ============================================================================
    // SETTINGS INTEGRATION
    // ============================================================================
    
    /// <summary>
    /// Apply graphics settings
    /// </summary>
    public void ApplyGraphicsSettings()
    {
        // Apply resolution, fullscreen, quality settings
        // This would integrate with your graphics settings system
        Debug.Log("[MainMenuManager] Graphics settings applied");
    }
    
    /// <summary>
    /// Apply audio settings
    /// </summary>
    public void ApplyAudioSettings()
    {
        UpdateAudioVolumes();
        Debug.Log("[MainMenuManager] Audio settings applied");
    }
    
    // ============================================================================
    // DEBUG METHODS
    // ============================================================================
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugMainMenu()
    {
        Debug.Log("=== MAIN MENU DEBUG INFO ===");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"MenuUI Found: {menuUI != null}");
        Debug.Log($"SaveManager Available: {SaveManager.Instance != null}");
        Debug.Log($"Has Save File: {HasSaveFile()}");
        Debug.Log($"SceneTransitionManager Available: {SceneTransitionManager.Instance != null}");
        Debug.Log($"Music Playing: {menuMusicSource != null && menuMusicSource.isPlaying}");
        Debug.Log("=== END DEBUG INFO ===");
    }
    
    // ============================================================================
    // VALIDATION
    // ============================================================================
    
    private void OnValidate()
    {
        // Editor-only validation
        #if UNITY_EDITOR
        if (menuUI == null)
        {
            menuUI = FindFirstObjectByType<MainMenuUI>();
        }
        #endif
    }
}