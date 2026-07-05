using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using System.Collections;

namespace SowurShield.Core
{

/// <summary>
/// Manages smooth scene transitions with loading screens, fade effects, and progress tracking
/// Singleton pattern for easy access from anywhere in the game
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    [Header("Loading Screen UI")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private Image backgroundImage;
    
    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float minimumLoadingTime = 2f; // Minimum time to show loading screen
    
    [Header("Loading Tips")]
    [SerializeField] private LocalizedString loadingTip01_Localized; // table "LoadingTips", key "loadingtips.tip01"
    [SerializeField] private LocalizedString loadingTip02_Localized; // table "LoadingTips", key "loadingtips.tip02"
    [SerializeField] private LocalizedString loadingTip03_Localized; // table "LoadingTips", key "loadingtips.tip03"
    [SerializeField] private LocalizedString loadingTip04_Localized; // table "LoadingTips", key "loadingtips.tip04"
    [SerializeField] private LocalizedString loadingTip05_Localized; // table "LoadingTips", key "loadingtips.tip05"
    [SerializeField] private LocalizedString loadingTip06_Localized; // table "LoadingTips", key "loadingtips.tip06"
    [SerializeField] private LocalizedString loadingTip07_Localized; // table "LoadingTips", key "loadingtips.tip07"
    
    [Header("Audio")]
    [SerializeField] private AudioClip transitionStartSound;
    [SerializeField] private AudioClip transitionCompleteSound;
    [SerializeField] private AudioSource audioSource;
    
    // Singleton
    public static SceneTransitionManager Instance { get; private set; }
    
    // State
    private bool isTransitioning = false;
    private Coroutine currentTransition;
    
    // Events
    public System.Action<string> OnSceneTransitionStarted;
    public System.Action<string> OnSceneTransitionCompleted;
    
    private void Awake()
    {
        // Singleton setup with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize loading screen as inactive
            if (loadingScreenPanel != null)
                loadingScreenPanel.SetActive(false);
                
            // Initialize fade canvas
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.gameObject.SetActive(false);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Find audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    // ============================================================================
    // PUBLIC API
    // ============================================================================
    
    /// <summary>
    /// Load a scene with smooth transition and loading screen
    /// </summary>
    public void LoadScene(string sceneName, bool showLoadingScreen = true, bool fadeTransition = true)
    {
        if (isTransitioning)
        {
            return;
        }
        
        
        if (currentTransition != null)
            StopCoroutine(currentTransition);
            
        currentTransition = StartCoroutine(LoadSceneRoutine(sceneName, showLoadingScreen, fadeTransition));
    }
    
    /// <summary>
    /// Quick scene load without transitions (for development/debugging)
    /// </summary>
    public void LoadSceneImmediate(string sceneName)
    {
        if (isTransitioning)
        {
            return;
        }
        
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// Check if currently transitioning between scenes
    /// </summary>
    public bool IsTransitioning => isTransitioning;
    
    // ============================================================================
    // TRANSITION COROUTINE
    // ============================================================================
    
    private IEnumerator LoadSceneRoutine(string sceneName, bool showLoadingScreen, bool fadeTransition)
    {
        isTransitioning = true;
        float transitionStartTime = Time.realtimeSinceStartup;
        
        // Play transition sound
        PlaySound(transitionStartSound);
        
        // Trigger event
        OnSceneTransitionStarted?.Invoke(sceneName);
        
        // Step 1: Fade out current scene (if enabled)
        if (fadeTransition)
        {
            yield return StartCoroutine(FadeOut());
        }
        
        // Step 2: Show loading screen (if enabled)
        if (showLoadingScreen)
        {
            ShowLoadingScreen();
            UpdateLoadingText("Preparing to load...");
            SetRandomLoadingTip();
        }
        
        // Step 3: Start async scene loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // Don't activate immediately
        
        // Step 4: Update loading progress
        while (!asyncLoad.isDone)
        {
            // Calculate progress (0.9 is max for loading, we handle the last 10% separately)
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (showLoadingScreen)
            {
                UpdateLoadingProgress(progress);
                
                if (progress < 0.3f)
                    UpdateLoadingText("Loading assets...");
                else if (progress < 0.6f)
                    UpdateLoadingText("Loading world data...");
                else if (progress < 0.9f)
                    UpdateLoadingText("Preparing game...");
                else
                    UpdateLoadingText("Almost ready...");
            }
            
            // Check if loading is complete
            if (asyncLoad.progress >= 0.9f)
            {
                // Ensure minimum loading time for better UX
                float elapsedTime = Time.realtimeSinceStartup - transitionStartTime;
                if (elapsedTime < minimumLoadingTime)
                {
                    float remainingTime = minimumLoadingTime - elapsedTime;
                    UpdateLoadingText("Finalizing...");
                    yield return new WaitForSecondsRealtime(remainingTime);
                }
                
                // Activate the scene
                UpdateLoadingProgress(1f);
                UpdateLoadingText("Complete!");
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        // Step 5: Brief pause to let new scene initialize
        yield return new WaitForSeconds(0.1f);
        
        // Step 6: Hide loading screen
        if (showLoadingScreen)
        {
            HideLoadingScreen();
        }
        
        // Step 7: Fade in new scene (if enabled)
        if (fadeTransition)
        {
            yield return StartCoroutine(FadeIn());
        }
        
        // Step 8: Cleanup
        isTransitioning = false;
        currentTransition = null;
        
        // Play completion sound
        PlaySound(transitionCompleteSound);
        
        // Trigger event
        OnSceneTransitionCompleted?.Invoke(sceneName);
        
    }
    
    // ============================================================================
    // LOADING SCREEN MANAGEMENT
    // ============================================================================
    
    private void ShowLoadingScreen()
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
        }
        
        // Reset progress bar
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        
    }
    
    private void HideLoadingScreen()
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }
        
    }
    
    private void UpdateLoadingProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }
    
    private void UpdateLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
        }
    }
    
    private void SetRandomLoadingTip()
    {
        LocalizedString[] loadingTips =
        {
            loadingTip01_Localized,
            loadingTip02_Localized,
            loadingTip03_Localized,
            loadingTip04_Localized,
            loadingTip05_Localized,
            loadingTip06_Localized,
            loadingTip07_Localized
        };

        if (tipText != null && loadingTips.Length > 0)
        {
            int randomIndex = Random.Range(0, loadingTips.Length);
            tipText.text = loadingTips[randomIndex].SafeGetLocalizedString();
        }
    }
    
    // ============================================================================
    // FADE EFFECTS
    // ============================================================================
    
    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;
        
        fadeCanvasGroup.gameObject.SetActive(true);
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeOutDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeInDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }
        
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }
    
    // ============================================================================
    // EVENT HANDLERS
    // ============================================================================
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        // You can add scene-specific initialization here
        switch (scene.name)
        {
            case "MainMenu":
                OnMainMenuLoaded();
                break;
            case "SampleScene":
                OnGameSceneLoaded();
                break;
            case "CombatScene":
                OnCombatSceneLoaded();
                break;
        }
    }
    
    private void OnMainMenuLoaded()
    {
        // MainMenuManager will handle stopping game music and starting menu music
        // in its InitializeMainMenu() method
    }

    private void OnGameSceneLoaded()
    {
        // Start (or resume, when returning from combat) seasonal farm music.
        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.OnStartGame();
        }

        // Newly instantiated scene objects (e.g. GroundItem) register themselves with
        // SaveManager on Awake but never get their persisted state otherwise — only an
        // explicit LoadGame() call does that. Re-apply it here so items already picked
        // up before a trip to CombatScene don't reappear when the farm scene reloads.
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ReapplyLoadedDataToRegisteredObjects();
        }

        // The combat inventory snapshot is newer than the re-applied save data —
        // restore it last so battle round-trips never wipe the player's items.
        var inventory = FindFirstObjectByType<SowurShield.Inventory.Inventory>();
        if (inventory != null)
            SowurShield.Inventory.InventorySceneSnapshot.TryRestore(inventory);
    }

    private void OnCombatSceneLoaded()
    {
        // Switch to combat music when entering the battle scene.
        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.OnEnterCombat();
        }
    }
    
    // ============================================================================
    // UTILITY METHODS
    // ============================================================================
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            float volume = PlayerPrefs.GetFloat("SFXVolume", 1f) * PlayerPrefs.GetFloat("MasterVolume", 1f);
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    // ============================================================================
    // CONVENIENT SCENE LOADING METHODS
    // ============================================================================
    
    /// <summary>
    /// Load the main game scene from main menu
    /// </summary>
    public void LoadMainGameScene()
    {
        LoadScene("SampleScene", true, true);
    }
    
    /// <summary>
    /// Return to main menu from game
    /// </summary>
    public void LoadMainMenu()
    {
        LoadScene("MainMenu", true, true);
    }
    
    /// <summary>
    /// Reload the current scene (useful for restarting)
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName, true, true);
    }
}

} // namespace SowurShield.Core