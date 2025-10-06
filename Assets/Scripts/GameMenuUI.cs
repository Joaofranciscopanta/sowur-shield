using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles the UI elements and visual aspects of the game menu
/// Works with GameMenuManager to provide a complete menu system
/// </summary>
public class GameMenuUI : MonoBehaviour
{
    [Header("Main Menu Panel")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button saveInfoButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button quitToMenuButton;
    [SerializeField] private Button quitToDesktopButton;
    
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button settingsBackButton;
    
    [Header("Save Info Panel")]
    [SerializeField] private GameObject saveInfoPanel;
    [SerializeField] private TextMeshProUGUI saveInfoText;
    [SerializeField] private Button saveInfoBackButton;
    [SerializeField] private Button deleteSaveButton;
    
    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("Notification System")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image notificationIcon;
    [SerializeField] private float notificationDuration = 2f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color successColor = Color.green;
    
    // State tracking
    private bool isQuitToDesktop = false;
    private Coroutine notificationCoroutine;
    
    // References
    private GameMenuManager menuManager;
    
    private void Awake()
    {
        menuManager = GetComponent<GameMenuManager>();
        if (menuManager == null)
        {
            Debug.LogError("[GameMenuUI] GameMenuManager component not found on the same GameObject!");
        }
    }
    
    private void Start()
    {
        SetupButtons();
        SetupSettings();
        InitializePanels();
        LoadSettings();
    }
    
    // ============================================================================
    // INITIALIZATION
    // ============================================================================
    
    private void SetupButtons()
    {
        // Main menu buttons with proper initialization
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(() => menuManager?.ResumeGame());
            resumeButton.interactable = true;
        }
        
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => ShowSettingsPanel());
            settingsButton.interactable = true;
        }
        
        if (saveInfoButton != null)
        {
            saveInfoButton.onClick.AddListener(() => ShowSaveInfoPanel());
            saveInfoButton.interactable = true;
        }
        
        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(() => menuManager?.LoadGame());
            loadGameButton.interactable = true;
        }
        
        if (quitToMenuButton != null)
        {
            quitToMenuButton.onClick.AddListener(() => menuManager?.QuitToMainMenu());
            quitToMenuButton.interactable = true;
        }
        
        if (quitToDesktopButton != null)
        {
            quitToDesktopButton.onClick.AddListener(() => menuManager?.QuitToDesktop());
            quitToDesktopButton.interactable = true;
        }

        
        // Settings buttons
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(ShowMainPanel);
            settingsBackButton.interactable = true;
        }
        
        // Save info buttons
        if (saveInfoBackButton != null)
        {
            saveInfoBackButton.onClick.AddListener(ShowMainPanel);
            saveInfoBackButton.interactable = true;
        }
        
        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.AddListener(ShowDeleteSaveConfirmation);
            deleteSaveButton.interactable = true;
        }
        
        // Confirmation buttons
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.AddListener(OnConfirmYes);
            confirmYesButton.interactable = true;
        }
        
        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.AddListener(OnConfirmNo);
            confirmNoButton.interactable = true;
        }
    }
    
    private void SetupSettings()
    {
        // Volume sliders
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        
        // Fullscreen toggle
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        
        // Resolution dropdown
        if (resolutionDropdown != null)
        {
            PopulateResolutionDropdown();
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
    }
    
    private void InitializePanels()
    {
        // Show main panel by default
        ShowMainPanel();
        
        // Hide notification panel
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    // ============================================================================
    // PANEL MANAGEMENT
    // ============================================================================
    
    public void ShowMainPanel()
    {

        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(saveInfoPanel, false);
        SetPanelActive(confirmationPanel, false);
        
        UpdateLoadButtonState();
    }
    
    public void ShowSettingsPanel()
    {

        
        if (settingsPanel == null)
        {

            ShowNotification("Settings panel not configured!", true);
            return;
        }
        
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(settingsPanel, true);
        SetPanelActive(saveInfoPanel, false);
        SetPanelActive(confirmationPanel, false);
        
        RefreshSettingsUI();
    }
    
    public void ShowSaveInfoPanel()
    {

        
        if (saveInfoPanel == null)
        {

            ShowNotification("Save info panel not configured!", true);
            return;
        }
        
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(saveInfoPanel, true);
        SetPanelActive(confirmationPanel, false);
        
        UpdateSaveInfoDisplay();
    }
    
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);

        }
        else if (active)
        {

        }
    }
    
    // ============================================================================
    // SETTINGS MANAGEMENT
    // ============================================================================
    
    private void LoadSettings()
    {
        // Load audio settings
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
        
        // Load display settings
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle != null) fullscreenToggle.isOn = fullscreen;
        
        // Apply settings
        ApplyAudioSettings();
        ApplyDisplaySettings();
    }
    
    private void RefreshSettingsUI()
    {
        // Refresh UI elements to show current values
        if (masterVolumeSlider != null)
            AudioListener.volume = masterVolumeSlider.value;
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        // Apply to music audio sources (implement as needed)
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        // Apply to SFX audio sources (implement as needed)
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }
    
    private void OnFullscreenToggled(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
        resolutionDropdown.options.Clear();
        
        // Add common resolutions
        string[] resolutions = {
            "1920x1080", "1680x1050", "1600x900", "1440x900",
            "1366x768", "1280x720", "1024x768"
        };
        
        foreach (string res in resolutions)
        {
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(res));
        }
        
        resolutionDropdown.value = 0; // Default to first option
    }
    
    private void OnResolutionChanged(int index)
    {
        if (resolutionDropdown == null || index >= resolutionDropdown.options.Count)
            return;
        
        string resolutionString = resolutionDropdown.options[index].text;
        string[] parts = resolutionString.Split('x');
        
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                Screen.SetResolution(width, height, Screen.fullScreen);
                PlayerPrefs.SetString("Resolution", resolutionString);
                PlayerPrefs.Save();
            }
        }
    }
    
    private void ApplyAudioSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = masterVolume;
    }
    
    private void ApplyDisplaySettings()
    {
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        Screen.fullScreen = fullscreen;
        
        string resolution = PlayerPrefs.GetString("Resolution", "1920x1080");
        string[] parts = resolution.Split('x');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                Screen.SetResolution(width, height, fullscreen);
            }
        }
    }
    
    // ============================================================================
    // SAVE INFO MANAGEMENT
    // ============================================================================
    
    private void UpdateSaveInfoDisplay()
    {
        if (saveInfoText == null || SaveManager.Instance == null)
            return;
        
        var saveInfo = SaveManager.Instance.GetSaveFileInfo();
        
        if (saveInfo != null)
        {
            string infoText = $"📄 Save File Information\n\n" +
                             $"📁 File: {saveInfo.fileName}\n" +
                             $"📅 Created: {saveInfo.creationTime:MMM dd, yyyy HH:mm}\n" +
                             $"🕒 Last Saved: {saveInfo.lastWriteTime:MMM dd, yyyy HH:mm}\n" +
                             $"💾 Size: {FormatFileSize(saveInfo.fileSizeBytes)}\n\n" +
                             $"💡 Game saves automatically when you sleep in bed";
            
            saveInfoText.text = infoText;
            
            // Enable delete button
            if (deleteSaveButton != null)
                deleteSaveButton.interactable = true;
        }
        else
        {
            saveInfoText.text = "❌ No save file found\n\n" +
                               "🛏️ Sleep in a bed to save your progress automatically.\n\n" +
                               "The game will create a save file when you go to sleep for the first time.";
            
            // Disable delete button
            if (deleteSaveButton != null)
                deleteSaveButton.interactable = false;
        }
    }
    
    private void UpdateLoadButtonState()
    {
        if (loadGameButton != null && SaveManager.Instance != null)
        {
            loadGameButton.interactable = SaveManager.Instance.HasSaveFile();
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
    // CONFIRMATION DIALOGS
    // ============================================================================
    
    public void ShowQuitConfirmation(bool quitToDesktop)
    {
        isQuitToDesktop = quitToDesktop;
        
        SetPanelActive(confirmationPanel, true);
        
        if (confirmationText != null)
        {
            string message = quitToDesktop ? 
                "Quit to Desktop?\n\nAny unsaved progress will be lost.\nMake sure to sleep in a bed to save!" :
                "Return to Main Menu?\n\nAny unsaved progress will be lost.\nMake sure to sleep in a bed to save!";
            
            confirmationText.text = message;
        }
    }
    
    private void ShowDeleteSaveConfirmation()
    {
        isQuitToDesktop = false; // Reuse confirmation dialog for delete
        
        SetPanelActive(confirmationPanel, true);
        
        if (confirmationText != null)
        {
            confirmationText.text = "Delete Save File?\n\nThis action cannot be undone.\nAll your progress will be permanently lost!";
        }
    }
    
    private void OnConfirmYes()
    {
        SetPanelActive(confirmationPanel, false);
        
        if (confirmationText != null && confirmationText.text.Contains("Delete"))
        {
            // Delete save confirmation
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSaveFile();
                ShowNotification("Save file deleted!", false);
                UpdateSaveInfoDisplay();
                UpdateLoadButtonState();
            }
        }
        else
        {
            // Quit confirmation
            if (isQuitToDesktop)
            {
                menuManager?.DoQuitToDesktop();
            }
            else
            {
                menuManager?.DoQuitToMainMenu();
            }
        }
    }
    
    private void OnConfirmNo()
    {
        SetPanelActive(confirmationPanel, false);
    }
    
    // ============================================================================
    // NOTIFICATION SYSTEM
    // ============================================================================
    
    public void ShowNotification(string message, bool isError = false)
    {
        if (notificationPanel == null || notificationText == null)
            return;
        
        // Stop existing notification
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }
        
        // Setup notification
        notificationText.text = message;
        notificationText.color = isError ? errorColor : normalColor;
        
        if (notificationIcon != null)
            notificationIcon.color = isError ? errorColor : successColor;
        
        // Show notification
        notificationPanel.SetActive(true);
        notificationCoroutine = StartCoroutine(HideNotificationAfterDelay());
    }
    
    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSecondsRealtime(notificationDuration);
        
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
        
        notificationCoroutine = null;
    }
    
    // ============================================================================
    // PUBLIC METHODS
    // ============================================================================
    
    public void RefreshUI()
    {
        UpdateSaveInfoDisplay();
        UpdateLoadButtonState();
    }
    
    // ============================================================================
    // DEBUG/EDITOR METHODS
    // ============================================================================
    
    #if UNITY_EDITOR
    [ContextMenu("Test Notification")]
    public void TestNotification()
    {
        ShowNotification("Test notification message!", false);
    }
    
    [ContextMenu("Test Error Notification")]
    public void TestErrorNotification()
    {
        ShowNotification("Test error message!", true);
    }
    
    [ContextMenu("Show Settings Panel")]
    public void DebugShowSettings()
    {
        ShowSettingsPanel();
    }
    #endif
}