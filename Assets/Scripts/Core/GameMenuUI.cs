using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using SowurShield.UI;
using UnityEngine.Localization;

namespace SowurShield.Core
{

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
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Button settingsBackButton;
    
    [Header("Save Info Panel")]
    [SerializeField] private GameObject saveInfoPanel;
    [SerializeField] private TextMeshProUGUI saveInfoText;
    [SerializeField] private Button saveInfoBackButton;
    [SerializeField] private Button deleteSaveButton;
    
    [Header("Save Slot Panel (in-game)")]
    [SerializeField] private GameObject saveSlotPanel;
    [SerializeField] private Transform saveSlotListParent;
    [SerializeField] private GameObject saveSlotButtonPrefab;
    [SerializeField] private TextMeshProUGUI saveSlotPanelTitle;
    [SerializeField] private Button saveSlotBackButton;

    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Localized Strings")]
    [SerializeField] private LocalizedString settingsNotConfiguredText; // table "MainMenu", key "mainmenu.gamemenu.settings_not_configured"
    [SerializeField] private LocalizedString saveInfoNotConfiguredText; // table "MainMenu", key "mainmenu.gamemenu.saveinfo_not_configured"
    [SerializeField] private LocalizedString saveInfoFoundText; // table "MainMenu", key "mainmenu.gamemenu.save_info_found"
    [SerializeField] private LocalizedString saveInfoNotFoundText; // table "MainMenu", key "mainmenu.gamemenu.save_info_not_found"
    [SerializeField] private LocalizedString saveSlotTitleText; // table "MainMenu", key "mainmenu.gamemenu.save_slot_title"
    [SerializeField] private LocalizedString loadSlotTitleText; // table "MainMenu", key "mainmenu.gamemenu.load_slot_title"
    [SerializeField] private LocalizedString overwriteConfirmText; // table "MainMenu", key "mainmenu.gamemenu.overwrite_confirm"
    [SerializeField] private LocalizedString loadConfirmText; // table "MainMenu", key "mainmenu.gamemenu.load_confirm"
    [SerializeField] private LocalizedString savedToSlotText; // table "MainMenu", key "mainmenu.gamemenu.saved_to_slot"
    [SerializeField] private LocalizedString confirmQuitDesktopText; // table "MainMenu", key "mainmenu.gamemenu.confirm_quit_desktop"
    [SerializeField] private LocalizedString confirmQuitMainMenuText; // table "MainMenu", key "mainmenu.gamemenu.confirm_quit_mainmenu"
    [SerializeField] private LocalizedString confirmDeleteSaveText; // table "MainMenu", key "mainmenu.gamemenu.confirm_delete_save"
    [SerializeField] private LocalizedString saveDeletedText; // table "MainMenu", key "mainmenu.gamemenu.save_deleted"
    [SerializeField] private LocalizedString testNotificationText; // table "MainMenu", key "mainmenu.gamemenu.test_notification"
    [SerializeField] private LocalizedString testErrorText; // table "MainMenu", key "mainmenu.gamemenu.test_error"

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
    private bool isPendingDeleteConfirmation = false;
    private Coroutine notificationCoroutine;

    private enum InGameSlotMode { Save, Load }
    private InGameSlotMode currentInGameSlotMode;
    private string pendingInGameSlot;

    // Panel to restore when the confirmation dialog is dismissed
    private GameObject confirmationReturnPanel;
    
    // References
    private GameMenuManager menuManager;
    
    private void Awake()
    {
        menuManager = GetComponent<GameMenuManager>();
        if (menuManager == null)
        {
        }
    }

    private void Start()
    {
        ApplyTheme();
        SetupButtons();
        SetupSettings();
        InitializePanels();
        LoadSettings();

        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    /// <summary>
    /// Restyle the scene-wired pause menu with the cozy sprite kit + palette:
    /// wood panels, primary buttons (danger art on destructive actions), and
    /// notification colors mapped to theme tokens. Re-run after
    /// TransferReferencesFrom since it targets a new scene's objects.
    /// </summary>
    private void ApplyTheme()
    {
        UITheme theme = UIThemeStyler.LoadTheme();

        UIThemeStyler.StylePanel(mainMenuPanel, theme);
        UIThemeStyler.StylePanel(settingsPanel, theme);
        UIThemeStyler.StylePanel(saveInfoPanel, theme);
        UIThemeStyler.StylePanel(saveSlotPanel, theme);
        UIThemeStyler.StylePanel(confirmationPanel, theme);
        UIThemeStyler.StylePanel(notificationPanel, theme);

        UIThemeStyler.StyleButton(resumeButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(settingsButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(saveInfoButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(loadGameButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(quitToMenuButton, theme, UIThemeStyler.ButtonDangerPath);
        UIThemeStyler.StyleButton(quitToDesktopButton, theme, UIThemeStyler.ButtonDangerPath);
        UIThemeStyler.StyleButton(settingsBackButton, theme, UIThemeStyler.ButtonSmallPath);
        UIThemeStyler.StyleButton(saveInfoBackButton, theme, UIThemeStyler.ButtonSmallPath);
        UIThemeStyler.StyleButton(saveSlotBackButton, theme, UIThemeStyler.ButtonSmallPath);
        UIThemeStyler.StyleButton(deleteSaveButton, theme, UIThemeStyler.ButtonDangerPath);
        UIThemeStyler.StyleButton(confirmYesButton, theme, UIThemeStyler.ButtonPrimaryPath);
        UIThemeStyler.StyleButton(confirmNoButton, theme, UIThemeStyler.ButtonSmallPath);

        if (theme != null)
        {
            normalColor  = theme.backgroundCream;
            errorColor   = theme.negative;
            successColor = theme.positive;

            UIThemeStyler.TintText(confirmationText, theme.backgroundCream);
            UIThemeStyler.TintText(saveInfoText, theme.backgroundCream);
            UIThemeStyler.TintText(saveSlotPanelTitle, theme.highlightGold);

            // Headings with no serialized field of their own. "Game Menu" kept the scene's old
            // brown on the panel sprite's dark top border — a 1.06 contrast ratio, invisible.
            UIThemeStyler.StylePanelTitle(mainMenuPanel, theme);
            UIThemeStyler.StylePanelTitle(settingsPanel, theme);
            UIThemeStyler.StylePanelTitle(saveInfoPanel, theme);
        }
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(Locale locale)
    {
        if (settingsPanel != null && settingsPanel.activeSelf)
            RefreshSettingsUI();

        if (saveInfoPanel != null && saveInfoPanel.activeSelf)
            UpdateSaveInfoDisplay();
    }

    /// <summary>
    /// Re-point this persisted GameMenuUI at the UI panels/controls that belong to a
    /// freshly-loaded scene, then re-run setup so listeners and panel state target
    /// the new scene's objects instead of the (now destroyed) previous scene's UI.
    /// Called by GameMenuManager when a duplicate instance is found on scene load.
    /// </summary>
    public void TransferReferencesFrom(GameMenuUI other)
    {
        if (other == null) return;

        mainMenuPanel = other.mainMenuPanel;
        resumeButton = other.resumeButton;
        settingsButton = other.settingsButton;
        saveInfoButton = other.saveInfoButton;
        loadGameButton = other.loadGameButton;
        quitToMenuButton = other.quitToMenuButton;
        quitToDesktopButton = other.quitToDesktopButton;

        settingsPanel = other.settingsPanel;
        masterVolumeSlider = other.masterVolumeSlider;
        musicVolumeSlider = other.musicVolumeSlider;
        sfxVolumeSlider = other.sfxVolumeSlider;
        fullscreenToggle = other.fullscreenToggle;
        resolutionDropdown = other.resolutionDropdown;
        languageDropdown = other.languageDropdown;
        settingsBackButton = other.settingsBackButton;

        saveInfoPanel = other.saveInfoPanel;
        saveInfoText = other.saveInfoText;
        saveInfoBackButton = other.saveInfoBackButton;
        deleteSaveButton = other.deleteSaveButton;

        saveSlotPanel = other.saveSlotPanel;
        saveSlotListParent = other.saveSlotListParent;
        saveSlotButtonPrefab = other.saveSlotButtonPrefab;
        saveSlotPanelTitle = other.saveSlotPanelTitle;
        saveSlotBackButton = other.saveSlotBackButton;

        confirmationPanel = other.confirmationPanel;
        confirmationText = other.confirmationText;
        confirmYesButton = other.confirmYesButton;
        confirmNoButton = other.confirmNoButton;

        notificationPanel = other.notificationPanel;
        notificationText = other.notificationText;
        notificationIcon = other.notificationIcon;

        // Stop any notification timer tied to the old (destroyed) panel.
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
            notificationCoroutine = null;
        }

        // Re-wire listeners and reset panel visibility against the new scene's objects.
        ApplyTheme();
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
            saveInfoButton.onClick.AddListener(() => ShowSaveSlotPanel());
            saveInfoButton.interactable = true;
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(() => ShowLoadSlotPanel());
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

        // Save slot panel back button
        if (saveSlotBackButton != null)
        {
            saveSlotBackButton.onClick.AddListener(ShowMainPanel);
            saveSlotBackButton.interactable = true;
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

        // Language dropdown
        SetupLanguageDropdown();
    }

    private void SetupLanguageDropdown()
    {
        if (languageDropdown == null)
            return;

        languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "English", "Português", "Español" });

        string currentCode = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetCurrentLanguageCode()
            : PlayerPrefs.GetString(LocalizationManager.PlayerPrefsKey, "en");

        languageDropdown.value = currentCode switch
        {
            "pt" => 1,
            "es" => 2,
            _ => 0
        };

        languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
    }

    private void OnLanguageDropdownChanged(int index)
    {
        string code = index switch
        {
            1 => "pt",
            2 => "es",
            _ => "en"
        };

        LocalizationManager.Instance?.SetLanguage(code);
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
        SetPanelActive(saveSlotPanel, false);

        UpdateLoadButtonState();
    }
    
    public void ShowSettingsPanel()
    {

        
        if (settingsPanel == null)
        {

            ShowNotification(settingsNotConfiguredText.SafeGetLocalizedString(), true);
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

            ShowNotification(saveInfoNotConfiguredText.SafeGetLocalizedString(), true);
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

        if (languageDropdown != null && LocalizationManager.Instance != null)
        {
            string currentCode = LocalizationManager.Instance.GetCurrentLanguageCode();
            languageDropdown.SetValueWithoutNotify(currentCode switch
            {
                "pt" => 1,
                "es" => 2,
                _ => 0
            });
        }
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("MasterVolume", value);

        // Apply immediately to Unity's audio system
        AudioListener.volume = value;

        // Update GameMusicManager's volume in real-time
        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.UpdateVolume();
        }

        PlayerPrefs.Save();
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();

        // Update GameMusicManager's volume in real-time
        if (GameMusicManager.Instance != null)
        {
            GameMusicManager.Instance.UpdateVolume();
        }
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
            saveInfoFoundText.Arguments = new object[]
            {
                saveInfo.fileName,
                saveInfo.creationTime.ToString("MMM dd, yyyy HH:mm"),
                saveInfo.lastWriteTime.ToString("MMM dd, yyyy HH:mm"),
                FormatFileSize(saveInfo.fileSizeBytes)
            };
            saveInfoText.text = saveInfoFoundText.SafeGetLocalizedString();

            // Enable delete button
            if (deleteSaveButton != null)
                deleteSaveButton.interactable = true;
        }
        else
        {
            saveInfoText.text = saveInfoNotFoundText.SafeGetLocalizedString();

            // Disable delete button
            if (deleteSaveButton != null)
                deleteSaveButton.interactable = false;
        }
    }
    
    private void UpdateLoadButtonState()
    {
        if (loadGameButton != null && SaveManager.Instance != null)
        {
            bool anySave = System.Linq.Enumerable.Any(SaveManager.Instance.GetAllSlotInfos(), s => !s.isEmpty);
            loadGameButton.interactable = anySave;
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
    // IN-GAME SAVE/LOAD SLOT PANEL
    // ============================================================================

    public void ShowSaveSlotPanel()
    {
        currentInGameSlotMode = InGameSlotMode.Save;

        if (saveSlotPanelTitle != null)
            saveSlotPanelTitle.text = saveSlotTitleText.SafeGetLocalizedString();

        PopulateInGameSlotPanel();

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(saveSlotPanel, true);
    }

    public void ShowLoadSlotPanel()
    {
        currentInGameSlotMode = InGameSlotMode.Load;

        if (saveSlotPanelTitle != null)
            saveSlotPanelTitle.text = loadSlotTitleText.SafeGetLocalizedString();

        PopulateInGameSlotPanel();

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(saveSlotPanel, true);
    }

    private void PopulateInGameSlotPanel()
    {
        if (saveSlotListParent == null || saveSlotButtonPrefab == null || SaveManager.Instance == null)
            return;

        foreach (Transform child in saveSlotListParent)
            Destroy(child.gameObject);

        SaveSlotInfo[] slots = SaveManager.Instance.GetAllSlotInfos();

        foreach (var info in slots)
        {
            string slotName = info.slotName;

            if (currentInGameSlotMode == InGameSlotMode.Save)
            {
                // AutoSave is hidden entirely in the manual Save panel
                if (info.isAutoSave) continue;

                GameObject go = Instantiate(saveSlotButtonPrefab, saveSlotListParent);
                SaveSlotButton btn = go.GetComponent<SaveSlotButton>();
                if (btn == null) continue;

                btn.Initialize(
                    info,
                    (Action)(() => OnInGameSaveSlotSelected(slotName)),
                    null,
                    false
                );
            }
            else // Load
            {
                GameObject go = Instantiate(saveSlotButtonPrefab, saveSlotListParent);
                SaveSlotButton btn = go.GetComponent<SaveSlotButton>();
                if (btn == null) continue;

                bool locked = info.isEmpty;
                Action deleteAction = info.isEmpty || info.isAutoSave
                    ? null
                    : (Action)(() => DeleteSlotAndRefreshInGame(slotName));

                btn.Initialize(
                    info,
                    locked ? null : (Action)(() => OnInGameLoadSlotSelected(slotName)),
                    deleteAction,
                    locked
                );
            }
        }
    }

    private void DeleteSlotAndRefreshInGame(string slotName)
    {
        SaveManager.Instance?.DeleteSlot(slotName);
        PopulateInGameSlotPanel();
    }

    private void OnInGameSaveSlotSelected(string slotName)
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile(slotName))
        {
            // Confirm overwrite
            pendingInGameSlot = slotName;
            confirmationReturnPanel = saveSlotPanel;
            SetPanelActive(saveSlotPanel, false);
            SetPanelActive(confirmationPanel, true);
            if (confirmationText != null)
            {
                overwriteConfirmText.Arguments = new object[] { slotName };
                confirmationText.text = overwriteConfirmText.SafeGetLocalizedString();
            }
        }
        else
        {
            ExecuteInGameSave(slotName);
        }
    }

    private void OnInGameLoadSlotSelected(string slotName)
    {
        pendingInGameSlot = slotName;
        confirmationReturnPanel = saveSlotPanel;
        SetPanelActive(saveSlotPanel, false);
        SetPanelActive(confirmationPanel, true);
        if (confirmationText != null)
            confirmationText.text = loadConfirmText.SafeGetLocalizedString();
    }

    private void ExecuteInGameSave(string slotName)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveToSlot(slotName);
            savedToSlotText.Arguments = new object[] { slotName };
            ShowNotification(savedToSlotText.SafeGetLocalizedString(), false);
        }
        ShowMainPanel();
    }

    private void ExecuteInGameLoad(string slotName)
    {
        if (SaveManager.Instance != null)
        {
            menuManager?.CloseMenu();
            SaveManager.Instance.LoadFromSlot(slotName);
        }
    }

    // ============================================================================
    // CONFIRMATION DIALOGS
    // ============================================================================

    public void ShowQuitConfirmation(bool quitToDesktop)
    {
        isQuitToDesktop = quitToDesktop;
        isPendingDeleteConfirmation = false;

        confirmationReturnPanel = mainMenuPanel;
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(confirmationPanel, true);

        if (confirmationText != null)
        {
            confirmationText.text = quitToDesktop
                ? confirmQuitDesktopText.SafeGetLocalizedString()
                : confirmQuitMainMenuText.SafeGetLocalizedString();
        }
    }

    private void ShowDeleteSaveConfirmation()
    {
        isQuitToDesktop = false; // Reuse confirmation dialog for delete
        isPendingDeleteConfirmation = true;

        confirmationReturnPanel = mainMenuPanel;
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(confirmationPanel, true);

        if (confirmationText != null)
        {
            confirmationText.text = confirmDeleteSaveText.SafeGetLocalizedString();
        }
    }
    
    private void OnConfirmYes()
    {
        SetPanelActive(confirmationPanel, false);

        // In-game slot action (save overwrite or load confirmation)
        if (!string.IsNullOrEmpty(pendingInGameSlot))
        {
            string slot = pendingInGameSlot;
            pendingInGameSlot = null;
            confirmationReturnPanel = null;

            if (currentInGameSlotMode == InGameSlotMode.Save)
                ExecuteInGameSave(slot);
            else
                ExecuteInGameLoad(slot);
            return;
        }

        if (isPendingDeleteConfirmation)
        {
            isPendingDeleteConfirmation = false;

            // Delete save confirmation
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSaveFile();
                ShowNotification(saveDeletedText.SafeGetLocalizedString(), false);
                UpdateSaveInfoDisplay();
                UpdateLoadButtonState();
            }

            SetPanelActive(confirmationReturnPanel, true);
            confirmationReturnPanel = null;
        }
        else
        {
            // Quit confirmation
            confirmationReturnPanel = null;
            if (isQuitToDesktop)
                menuManager?.DoQuitToDesktop();
            else
                menuManager?.DoQuitToMainMenu();
        }
    }

    private void OnConfirmNo()
    {
        isPendingDeleteConfirmation = false;
        SetPanelActive(confirmationPanel, false);
        SetPanelActive(confirmationReturnPanel, true);
        confirmationReturnPanel = null;
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
        ShowNotification(testNotificationText.SafeGetLocalizedString(), false);
    }

    [ContextMenu("Test Error Notification")]
    public void TestErrorNotification()
    {
        ShowNotification(testErrorText.SafeGetLocalizedString(), true);
    }
    
    [ContextMenu("Show Settings Panel")]
    public void DebugShowSettings()
    {
        ShowSettingsPanel();
    }
    #endif
}

} // namespace SowurShield.Core