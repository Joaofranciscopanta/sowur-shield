using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;
using SowurShield.Animals;
using UnityEngine.Localization;

namespace SowurShield.Core
{

/// <summary>
/// Manages the sleep confirmation panel UI
/// Shows information about what will happen when sleeping and handles user confirmation
/// </summary>
public class SleepConfirmationPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelContainer;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    
    [Header("Information Display")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI timeInfoText;
    [SerializeField] private TextMeshProUGUI sellBoxInfoText;
    [SerializeField] private TextMeshProUGUI saveInfoText;
    [SerializeField] private TextMeshProUGUI feedingTroughInfoText;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private TextMeshProUGUI cancelButtonText;
    
    [Header("Visual Effects")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Sleep Transition")]
    [SerializeField] private GameObject sleepFadeOverlay; // Black overlay for sleep fade
    [SerializeField] private float sleepFadeDuration = 2.0f;
    [SerializeField] private float sleepFadeHoldDuration = 1.0f;
    [SerializeField] private AnimationCurve sleepFadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    [SerializeField] private AudioClip panelOpenSound;
    [SerializeField] private AudioClip confirmSound;
    [SerializeField] private AudioClip cancelSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString titleLocalizedText; // table "Farming", key "farming.sleep.title"
    [SerializeField] private LocalizedString confirmLocalizedText; // table "Farming", key "farming.sleep.confirm"
    [SerializeField] private LocalizedString cancelLocalizedText; // table "Farming", key "farming.sleep.cancel"
    [SerializeField] private LocalizedString currentTimeText; // table "Farming", key "farming.sleep.current_time"
    [SerializeField] private LocalizedString wakeTomorrowText; // table "Farming", key "farming.sleep.wake_tomorrow"
    [SerializeField] private LocalizedString sellboxSummaryText; // table "Farming", key "farming.sleep.sellbox_summary"
    [SerializeField] private LocalizedString sellboxEmptyText; // table "Farming", key "farming.sleep.sellbox_empty"
    [SerializeField] private LocalizedString troughSummaryText; // table "Farming", key "farming.sleep.trough_summary"
    [SerializeField] private LocalizedString troughEmptyText; // table "Farming", key "farming.sleep.trough_empty"
    [SerializeField] private LocalizedString autosaveOnText; // table "Farming", key "farming.sleep.autosave_on"
    [SerializeField] private LocalizedString autosaveOffText; // table "Farming", key "farming.sleep.autosave_off"

    // Events
    public static Action OnSleepConfirmed;
    public static Action OnSleepCancelled;
    
    // State
    private bool isVisible = false;
    private Coroutine fadeCoroutine;
    
    // Game state control
    private PlayerMove playerMove;
    private GameTimeController timeController;
    private bool wasTimePaused = false;
    
    // Sleep fade state
    private CanvasGroup sleepFadeCanvasGroup;
    private Coroutine sleepFadeCoroutine;
    
    private void Awake()
    {
        InitializeReferences();
        SetupButtons();
        SetupDefaultTexts();
        
        // Start hidden
        if (panelContainer != null)
            panelContainer.SetActive(false);
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(Locale locale)
    {
        SetupDefaultTexts();
        if (panelContainer != null && panelContainer.activeSelf)
            UpdateInformationTexts();
    }

    private void InitializeReferences()
    {
        // Get audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        // Ensure canvas group exists
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null && panelContainer != null)
            canvasGroup = panelContainer.GetComponent<CanvasGroup>();
            
        // Initialize sleep fade overlay
        if (sleepFadeOverlay != null)
        {
            sleepFadeCanvasGroup = sleepFadeOverlay.GetComponent<CanvasGroup>();
            if (sleepFadeCanvasGroup == null)
                sleepFadeCanvasGroup = sleepFadeOverlay.AddComponent<CanvasGroup>();
            
            // Start with overlay invisible
            sleepFadeCanvasGroup.alpha = 0f;
            sleepFadeOverlay.SetActive(false);
        }
    }
    
    private void SetupButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
    }
    
    /// <summary>
    /// Public method to setup buttons after references are assigned
    /// </summary>
    public void InitializeAfterSetup()
    {
        SetupButtons();
        SetupDefaultTexts();
        
        // Button validation
        if (confirmButton == null || cancelButton == null)
        {
            enabled = false;
            return;
        }
    }
    
    private void SetupDefaultTexts()
    {
        if (titleText != null)
            titleText.text = titleLocalizedText.SafeGetLocalizedString();

        if (confirmButtonText != null)
            confirmButtonText.text = confirmLocalizedText.SafeGetLocalizedString();

        if (cancelButtonText != null)
            cancelButtonText.text = cancelLocalizedText.SafeGetLocalizedString();
    }
    
    /// <summary>
    /// Shows the confirmation panel with current game state information
    /// </summary>
    public void ShowConfirmation()
    {
        if (isVisible) return;
        
        // Find required components
        if (playerMove == null)
            playerMove = FindFirstObjectByType<PlayerMove>();
        if (timeController == null)
            timeController = GameTimeController.instance ?? FindFirstObjectByType<GameTimeController>();
        
        UpdateInformationTexts();
        
        if (panelContainer != null)
            panelContainer.SetActive(true);
        
        isVisible = true;
        
        // PAUSE GAME STATE
        PauseGameState();
        
        // Play sound effect
        PlaySound(panelOpenSound);
        
        // Fade in animation
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        fadeCoroutine = StartCoroutine(FadeIn());
        
        // Cursor management
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        

    }
    
    /// <summary>
    /// Hides the confirmation panel
    /// </summary>
    public void HideConfirmation()
    {
        if (!isVisible) return;
        
        isVisible = false;
        
        // RESUME GAME STATE FIRST
        ResumeGameState();
        
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        
        if (panelContainer != null)
            panelContainer.SetActive(false);
        

    }
    
    private void UpdateInformationTexts()
    {
        UpdateTimeInfo();
        UpdateSellBoxInfo();
        UpdateFeedingTroughInfo();
        UpdateSaveInfo();
    }
    
    private void UpdateTimeInfo()
    {
        if (timeInfoText != null)
        {
            GameTimeController timeController = GameTimeController.instance;
            if (timeController != null)
            {
                // Get current time using the same method as UIManagerPlayer
                int hour, minute;
                timeController.ProgressToTime(timeController.lastUIUpdateProgress, out hour, out minute);
                
                // Format time (24h format)
                string currentTime = $"{hour:D2}:{minute:D2}";
                int currentDay = timeController.currentDay;

                currentTimeText.Arguments = new object[] { currentDay, currentTime };
                timeInfoText.text = currentTimeText.SafeGetLocalizedString();
            }
            else
            {
                timeInfoText.text = wakeTomorrowText.SafeGetLocalizedString();
            }
        }
    }
    
    private void UpdateSellBoxInfo()
    {
        if (sellBoxInfoText != null)
        {
            SellBox[] sellBoxes = FindObjectsByType<SellBox>(FindObjectsSortMode.None);
            int totalValue = 0;
            int totalItems = 0;
            
            foreach (SellBox sellBox in sellBoxes)
            {
                // Check all SellBoxes, not just open ones, since they might have items
                totalValue += sellBox.GetTotalValue();
                totalItems += sellBox.GetTotalItemCount();
            }
            
            if (totalItems > 0)
            {
                sellboxSummaryText.Arguments = new object[] { totalItems, totalValue };
                sellBoxInfoText.text = sellboxSummaryText.SafeGetLocalizedString();
                sellBoxInfoText.color = Color.green;
            }
            else
            {
                sellBoxInfoText.text = sellboxEmptyText.SafeGetLocalizedString();
                sellBoxInfoText.color = new Color(0.176f, 0.165f, 0.149f, 1f);
            }
        }
    }
    
    private void UpdateFeedingTroughInfo()
    {
        if (feedingTroughInfoText != null)
        {
            FeedingTrough[] troughs = FindObjectsByType<FeedingTrough>(FindObjectsSortMode.None);
            int totalFeedable = 0;
            int totalTroughs = 0;

            foreach (FeedingTrough trough in troughs)
            {
                totalTroughs++;
                totalFeedable += trough.GetFeedableAnimalCount();
            }

            if (totalTroughs > 0 && totalFeedable > 0)
            {
                troughSummaryText.Arguments = new object[] { totalFeedable, totalFeedable != 1 ? "s" : "" };
                feedingTroughInfoText.text = troughSummaryText.SafeGetLocalizedString();
                feedingTroughInfoText.color = Color.green;
            }
            else if (totalTroughs > 0)
            {
                feedingTroughInfoText.text = troughEmptyText.SafeGetLocalizedString();
                feedingTroughInfoText.color = Color.yellow;
            }
            else
            {
                feedingTroughInfoText.text = "";
                feedingTroughInfoText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateSaveInfo()
    {
        if (saveInfoText != null)
        {
            // Not Color.cyan / Color.yellow. Both are near-white against the panel's cream
            // interior: cyan measured 1.12:1 against a 4.5:1 minimum, so the line telling the
            // player their game is about to be saved was effectively invisible. These are the
            // UITheme status tones, dark enough to read on cream while still reading as
            // "fine" and "careful".
            if (SaveManager.Instance != null)
            {
                saveInfoText.text = autosaveOnText.SafeGetLocalizedString();
                saveInfoText.color = new Color(0.180f, 0.420f, 0.220f); // deep green, 5.74:1
            }
            else
            {
                saveInfoText.text = autosaveOffText.SafeGetLocalizedString();
                saveInfoText.color = new Color(0.600f, 0.240f, 0.020f); // burnt orange, 6.22:1
            }
        }
    }
    
    private void OnConfirmClicked()
    {
        PlaySound(confirmSound);
        HideConfirmation();
        OnSleepConfirmed?.Invoke();
    }

    private void OnCancelClicked()
    {
        PlaySound(cancelSound);
        HideConfirmation();
        OnSleepCancelled?.Invoke();
    }
    
    private System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;
        
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / fadeInDuration;
            float curveValue = fadeInCurve.Evaluate(progress);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, curveValue);
            
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        fadeCoroutine = null;
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    
    private bool IsOtherUIActive()
    {
        // Check if GameMenuManager is open
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.IsMenuOpen)
            return true;
            
        // Check if SellBox is open
        SellBox[] sellBoxes = FindObjectsByType<SellBox>(FindObjectsSortMode.None);
        foreach (SellBox sellBox in sellBoxes)
        {
            if (sellBox.IsOpen)
                return true;
        }
        
        return false;
    }
    
    
    // Handle keyboard input
    private void Update()
    {
        if (!isVisible || Keyboard.current == null)
            return;

        // Handle ESC key to cancel
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OnCancelClicked();
        }

        // Handle Enter key to confirm
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            OnConfirmClicked();
        }
    }
    
    private void PauseGameState()
    {
        // Pause time controller
        if (timeController != null)
        {
            wasTimePaused = timeController.isPaused;
            timeController.isPaused = true;
        }

        // Disable player movement
        if (playerMove != null)
        {
            playerMove.DisableMovement();
        }
    }

    private void ResumeGameState()
    {
        // Resume time controller (only if it wasn't already paused)
        if (timeController != null)
        {
            timeController.isPaused = wasTimePaused;
        }

        // Re-enable player movement
        if (playerMove != null)
        {
            playerMove.EnableMovement();
        }
    }
    
    /// <summary>
    /// Starts the sleep fade transition - fades to black, holds, then calls callback
    /// </summary>
    public void StartSleepFade(System.Action onFadeComplete)
    {
        if (sleepFadeOverlay == null || sleepFadeCanvasGroup == null)
        {
            onFadeComplete?.Invoke();
            return;
        }
        
        if (sleepFadeCoroutine != null)
            StopCoroutine(sleepFadeCoroutine);
            
        sleepFadeCoroutine = StartCoroutine(SleepFadeSequence(onFadeComplete));
    }
    
    /// <summary>
    /// Fades back in from black after sleep is complete
    /// </summary>
    public void StartSleepFadeIn(System.Action onFadeComplete = null)
    {
        if (sleepFadeOverlay == null || sleepFadeCanvasGroup == null)
        {
            onFadeComplete?.Invoke();
            return;
        }
        
        if (sleepFadeCoroutine != null)
            StopCoroutine(sleepFadeCoroutine);
            
        sleepFadeCoroutine = StartCoroutine(SleepFadeInSequence(onFadeComplete));
    }
    
    private System.Collections.IEnumerator SleepFadeSequence(System.Action onFadeComplete)
    {
        // Make sure overlay is active and visible
        sleepFadeOverlay.SetActive(true);
        sleepFadeCanvasGroup.alpha = 0f;

        // Fade to black
        float elapsed = 0f;
        while (elapsed < sleepFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / sleepFadeDuration;
            float curveValue = sleepFadeCurve.Evaluate(progress);

            sleepFadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, curveValue);
            yield return null;
        }

        sleepFadeCanvasGroup.alpha = 1f;
        // Hold in black
        yield return new WaitForSecondsRealtime(sleepFadeHoldDuration);

        sleepFadeCoroutine = null;
        onFadeComplete?.Invoke();
    }
    
    private System.Collections.IEnumerator SleepFadeInSequence(System.Action onFadeComplete)
    {
        // Fade back in
        float elapsed = 0f;
        while (elapsed < sleepFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / sleepFadeDuration;
            float curveValue = sleepFadeCurve.Evaluate(progress);

            sleepFadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, curveValue);
            yield return null;
        }

        sleepFadeCanvasGroup.alpha = 0f;
        sleepFadeOverlay.SetActive(false);

        // ENSURE GAME STATE IS FULLY RESTORED
        ForceGameStateRestore();

        sleepFadeCoroutine = null;
        onFadeComplete?.Invoke();
    }
    
    /// <summary>
    /// Forces game state restoration - fixes Unity editor pause issue
    /// </summary>
    public void ForceGameStateRestore()
    {
        // Force time controller to resume if it's still paused
        if (timeController != null && timeController.isPaused && !wasTimePaused)
        {
            timeController.isPaused = false;
        }

        // Force player movement to be enabled
        if (playerMove != null && !playerMove.IsMovementEnabled())
        {
            playerMove.EnableMovement();
        }

        #if UNITY_EDITOR
        // In editor, ensure time scale is correct
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }
        #endif
    }
    
    private void OnDestroy()
    {
        // Clean up coroutines
        if (sleepFadeCoroutine != null)
            StopCoroutine(sleepFadeCoroutine);

        // Clean up events
        OnSleepConfirmed = null;
        OnSleepCancelled = null;

        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }
}

} // namespace SowurShield.Core