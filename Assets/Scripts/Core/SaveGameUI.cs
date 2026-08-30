using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Localization;

namespace SowurShield.Core
{

/// <summary>
/// UI component for managing save/load operations and displaying save feedback to the player
/// </summary>
public class SaveGameUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject saveNotificationPanel;
    [SerializeField] private TextMeshProUGUI saveStatusText;
    [SerializeField] private Image saveStatusIcon;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button deleteSaveButton;
    
    [Header("Save Notification Settings")]
    [SerializeField] private float notificationDuration = 3f;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color infoColor = Color.blue;
    
    [Header("Save File Info")]
    [SerializeField] private GameObject saveInfoPanel;
    [SerializeField] private TextMeshProUGUI saveFileInfoText;
    [SerializeField] private Button showSaveInfoButton;
    
    [Header("Audio")]
    [SerializeField] private AudioClip saveSuccessSound;
    [SerializeField] private AudioClip saveErrorSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString saveInfoFoundText; // table "MainMenu", key "mainmenu.savegameui.save_info_found"
    [SerializeField] private LocalizedString saveInfoNotFoundText; // table "MainMenu", key "mainmenu.savegameui.save_info_not_found"
    
    private Coroutine notificationCoroutine;
    private bool isShowingSaveInfo = false;
    
    private void Start()
    {
        SetupUI();
        SubscribeToSaveEvents();
        UpdateSaveFileInfo();

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        UnsubscribeFromSaveEvents();
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(Locale locale)
    {
        if (isShowingSaveInfo)
            UpdateSaveFileInfo();
    }
    
    private void SetupUI()
    {
        // Hide panels initially
        if (saveNotificationPanel != null)
            saveNotificationPanel.SetActive(false);
            
        if (saveInfoPanel != null)
            saveInfoPanel.SetActive(false);
        
        // Setup button listeners            
        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
            
        if (deleteSaveButton != null)
            deleteSaveButton.onClick.AddListener(OnDeleteSaveClicked);
            
        if (showSaveInfoButton != null)
            showSaveInfoButton.onClick.AddListener(OnShowSaveInfoClicked);
        
        // Find audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    
    private void SubscribeToSaveEvents()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnSaveStarted += OnSaveStarted;
            SaveManager.Instance.OnSaveCompleted += OnSaveCompleted;
            SaveManager.Instance.OnLoadStarted += OnLoadStarted;
            SaveManager.Instance.OnLoadCompleted += OnLoadCompleted;
        }
    }
    
    private void UnsubscribeFromSaveEvents()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnSaveStarted -= OnSaveStarted;
            SaveManager.Instance.OnSaveCompleted -= OnSaveCompleted;
            SaveManager.Instance.OnLoadStarted -= OnLoadStarted;
            SaveManager.Instance.OnLoadCompleted -= OnLoadCompleted;
        }
    }
    
    // ============================================================================
    // SAVE MANAGER EVENT HANDLERS
    // ============================================================================
    
    private void OnSaveStarted()
    {
        ShowNotification("Saving game...", infoColor, notificationDuration);
    }
    
    private void OnSaveCompleted(bool success)
    {
        if (success)
        {
            ShowNotification("Game saved! Sweet dreams...", successColor, notificationDuration);
            PlaySound(saveSuccessSound, "SaveSuccess");
            UpdateSaveFileInfo();
        }
        else
        {
            ShowNotification("Failed to save game!", errorColor, notificationDuration * 1.5f);
            PlaySound(saveErrorSound, "Denied");
        }
    }
    
    private void OnLoadStarted()
    {
        ShowNotification("Loading game...", infoColor, notificationDuration);
        
        // Disable load button temporarily
        if (loadGameButton != null)
            loadGameButton.interactable = false;
    }
    
    private void OnLoadCompleted(bool success)
    {
        // Re-enable load button
        if (loadGameButton != null)
            loadGameButton.interactable = true;
        
        if (success)
        {
            ShowNotification("Game loaded successfully!", successColor, notificationDuration);
            UpdateSaveFileInfo();
        }
        else
        {
            ShowNotification("Failed to load game!", errorColor, notificationDuration * 1.5f);
        }
    }
    
    // ============================================================================
    // BUTTON HANDLERS
    // ============================================================================
    
    private void OnLoadGameClicked()
    {
        if (SaveManager.Instance != null)
        {
            if (SaveManager.Instance.HasSaveFile())
            {
                SaveManager.Instance.LoadGame();
            }
            else
            {
                ShowNotification("No save file found!", errorColor, notificationDuration);
            }
        }
        else
        {
            ShowNotification("SaveManager not found!", errorColor, notificationDuration);
        }
    }
    
    private void OnDeleteSaveClicked()
    {
        if (SaveManager.Instance != null)
        {
            if (SaveManager.Instance.HasSaveFile())
            {
                // Show confirmation dialog (implement as needed)
                SaveManager.Instance.DeleteSaveFile();
                ShowNotification("Save file deleted!", infoColor, notificationDuration);
                UpdateSaveFileInfo();
            }
            else
            {
                ShowNotification("No save file to delete!", errorColor, notificationDuration);
            }
        }
        else
        {
            ShowNotification("SaveManager not found!", errorColor, notificationDuration);
        }
    }
    
    private void OnShowSaveInfoClicked()
    {
        if (saveInfoPanel != null)
        {
            isShowingSaveInfo = !isShowingSaveInfo;
            saveInfoPanel.SetActive(isShowingSaveInfo);
            
            if (isShowingSaveInfo)
            {
                UpdateSaveFileInfo();
            }
        }
    }
    
    // ============================================================================
    // NOTIFICATION SYSTEM
    // ============================================================================
    
    private void ShowNotification(string message, Color color, float duration)
    {
        if (saveNotificationPanel == null || saveStatusText == null)
            return;
        
        // Stop any existing notification
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        
        // Setup notification
        saveStatusText.text = message;
        saveStatusText.color = color;
        
        if (saveStatusIcon != null)
            saveStatusIcon.color = color;
        
        // Show notification
        saveNotificationPanel.SetActive(true);
        notificationCoroutine = StartCoroutine(HideNotificationAfterDelay(duration));
        

    }
    
    private IEnumerator HideNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (saveNotificationPanel != null)
            saveNotificationPanel.SetActive(false);
        
        notificationCoroutine = null;
    }
    
    // ============================================================================
    // SAVE FILE INFO
    // ============================================================================
    
    private void UpdateSaveFileInfo()
    {
        if (saveFileInfoText == null || SaveManager.Instance == null)
            return;
        
        var saveInfo = SaveManager.Instance.GetSaveFileInfo();
        
        if (saveInfo != null)
        {
            saveInfoFoundText.Arguments = new object[]
            {
                saveInfo.fileName,
                saveInfo.creationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                saveInfo.lastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                FormatFileSize(saveInfo.fileSizeBytes)
            };
            saveFileInfoText.text = saveInfoFoundText.SafeGetLocalizedString();

            // Enable buttons that require a save file
            if (loadGameButton != null)
                loadGameButton.interactable = true;
            if (deleteSaveButton != null)
                deleteSaveButton.interactable = true;
        }
        else
        {
            saveFileInfoText.text = saveInfoNotFoundText.SafeGetLocalizedString();

            // Disable buttons that require a save file
            if (loadGameButton != null)
                loadGameButton.interactable = false;
            if (deleteSaveButton != null)
                deleteSaveButton.interactable = false;
        }
    }
    
    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024f:F1} KB";
        else
            return $"{bytes / (1024f * 1024f):F1} MB";
    }
    
    // ============================================================================
    // AUDIO
    // ============================================================================
    
    /// <summary>
    /// Both clip fields are unassigned, so saving happened in silence. Falls back to a
    /// shared SFXManager key; an assigned clip still takes precedence.
    /// </summary>
    private void PlaySound(AudioClip clip, string sfxKey = null)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            return;
        }

        if (!string.IsNullOrEmpty(sfxKey))
        {
            SFXManager.Play(sfxKey);
        }
    }
    
    // ============================================================================
    // PUBLIC METHODS
    // ============================================================================
    
    /// <summary>
    /// Show a custom notification to the player
    /// </summary>
    public void ShowCustomNotification(string message, bool isError = false)
    {
        Color color = isError ? errorColor : infoColor;
        ShowNotification(message, color, notificationDuration);
    }
    
    /// <summary>
    /// Check if there's a save file and update UI accordingly
    /// </summary>
    public void RefreshSaveFileStatus()
    {
        UpdateSaveFileInfo();
    }
    
    // ============================================================================
    // EDITOR/DEBUG METHODS
    // ============================================================================
    
    #if UNITY_EDITOR
    [ContextMenu("Test Sleep Save Notification")]
    public void TestSleepSaveNotification()
    {
        ShowNotification("Game saved! Sweet dreams...", successColor, 3f);
    }
    
    [ContextMenu("Test Error Notification")]
    public void TestErrorNotification()
    {
        ShowNotification("Failed to save game!", errorColor, 3f);
    }
    
    [ContextMenu("Update Save File Info")]
    public void DebugUpdateSaveFileInfo()
    {
        UpdateSaveFileInfo();
    }
    #endif
}

} // namespace SowurShield.Core