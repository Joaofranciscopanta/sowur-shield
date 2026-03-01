using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.IO;
using System.Linq;

/// <summary>
/// Main menu UI handler for the game's title screen
/// Manages new game, continue, settings, and quit functionality
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private enum SlotPickerMode { Load, NewGame }

    [Header("Main Menu Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Slot Picker Panel")]
    [SerializeField] private GameObject slotPickerPanel;
    [SerializeField] private Transform slotListParent;
    [SerializeField] private GameObject saveSlotButtonPrefab;
    [SerializeField] private Button slotPickerBackButton;
    [SerializeField] private TextMeshProUGUI slotPickerTitleText;
    
    [Header("Settings Panel (Reuse GameMenuUI components)")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button settingsBackButton;
    
    [Header("Confirmation Dialog")]
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("Loading Screen")]
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider loadingProgressBar;
    
    [Header("Save Info Display")]
    [SerializeField] private GameObject saveInfoPanel;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI lastPlayedText;
    
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip gameStartSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Game Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";
    
    // State management
    private bool isNewGameOverwrite = false;
    private Coroutine loadingCoroutine;
    
    private void Start()
    {
        SetupUI();
        // Delay save file check to ensure all components are initialized
        StartCoroutine(DelayedSaveFileCheck());
    }
    
    private IEnumerator DelayedSaveFileCheck()
    {
        // Wait one frame to ensure all initialization is complete
        yield return null;
        
        CheckSaveFileAvailability();
        UpdateSaveInfoDisplay();
    }
    
    private void SetupUI()
    {
        // Setup main menu buttons with proper initialization
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
            newGameButton.interactable = true;
        }
            
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            // Continue button interactable state will be set by CheckSaveFileAvailability
        }

        if (loadGameButton != null)
        {
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
        }
            
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
            settingsButton.interactable = true;
        }
            
        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(OnCreditsClicked);
            creditsButton.interactable = true;
        }
            
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
            quitButton.interactable = true;
        }
        
        // Setup slot picker back button
        if (slotPickerBackButton != null)
            slotPickerBackButton.onClick.AddListener(ShowMainPanel);

        // Setup settings panel
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
            
        // Setup confirmation dialog
        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(OnConfirmationYes);
            
        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(OnConfirmationNo);
        
        // Initialize panels
        ShowMainPanel();
        
        // Find audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        // Load and apply saved settings first
        LoadAndApplySavedSettings();
        
        // Setup settings sliders and toggles
        SetupSettingsControls();
    }
    
    /// <summary>
    /// Load and apply all saved settings on startup
    /// </summary>
    private void LoadAndApplySavedSettings()
    {
        // Load and apply audio settings
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        
        // Apply audio settings immediately
        AudioListener.volume = masterVolume;
        
        // Load and apply graphics settings
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1; // Default to fullscreen
        int savedWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);
        int savedRefreshRate = PlayerPrefs.GetInt("RefreshRate", Screen.currentResolution.refreshRate);
        
        // Apply graphics settings
        try
        {
            if (Screen.fullScreen != fullscreen)
            {
                Screen.fullScreen = fullscreen;
            }
            
            // Only change resolution if it's different from current
            if (Screen.currentResolution.width != savedWidth || Screen.currentResolution.height != savedHeight)
            {
                Screen.SetResolution(savedWidth, savedHeight, fullscreen, savedRefreshRate);
            }
        }
        catch (System.Exception e)
        {
        }
        
    }
    
    private void SetupSettingsControls()
    {
        // Load current settings values and add listeners
        if (masterVolumeSlider != null)
        {
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            masterVolumeSlider.value = masterVolume;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            // Apply the loaded volume immediately
            AudioListener.volume = masterVolume;
        }
        
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }
        
        // Setup resolution dropdown
        SetupResolutionDropdown();
        
    }
    
    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) 
        {
            return;
        }
        
        resolutionDropdown.ClearOptions();
        
        // Get all available resolutions
        Resolution[] resolutions = Screen.resolutions;
        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
        
        // Filter out duplicate resolutions and low refresh rates
        System.Collections.Generic.List<Resolution> filteredResolutions = new System.Collections.Generic.List<Resolution>();
        System.Collections.Generic.HashSet<string> addedResolutions = new System.Collections.Generic.HashSet<string>();
        
        for (int i = resolutions.Length - 1; i >= 0; i--) // Start from highest resolution
        {
            Resolution res = resolutions[i];
            string resolutionString = $"{res.width} x {res.height}";
            
            // Only add if we haven't seen this resolution before (filters out different refresh rates)
            if (!addedResolutions.Contains(resolutionString) && res.width >= 1280 && res.height >= 720)
            {
                addedResolutions.Add(resolutionString);
                filteredResolutions.Add(res);
                options.Add($"{res.width} x {res.height}");
            }
        }
        
        // Reverse to show highest resolution first
        options.Reverse();
        filteredResolutions.Reverse();
        
        // Find current resolution index
        int currentResolutionIndex = 0;
        int savedWidth = PlayerPrefs.GetInt("ResolutionWidth", Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt("ResolutionHeight", Screen.currentResolution.height);
        
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            if (filteredResolutions[i].width == savedWidth && filteredResolutions[i].height == savedHeight)
            {
                currentResolutionIndex = i;
                break;
            }
        }
        
        // Add options to dropdown
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        
    }
    
    // ============================================================================
    // MAIN MENU BUTTON HANDLERS
    // ============================================================================
    
    private void OnNewGameClicked()
    {
        PlaySound(buttonClickSound);
        OpenSlotPicker(SlotPickerMode.NewGame);
    }

    private void OnContinueClicked()
    {
        PlaySound(buttonClickSound);

        // Continue loads the most recent save directly — no picker
        string mostRecent = GetMostRecentSlotFromDisk();
        if (!string.IsNullOrEmpty(mostRecent))
            LoadGameFromSlot(mostRecent);
    }

    private void OnLoadGameClicked()
    {
        PlaySound(buttonClickSound);
        OpenSlotPicker(SlotPickerMode.Load);
    }
    
    private void OnSettingsClicked()
    {
        PlaySound(buttonClickSound);
        ShowSettingsPanel();
    }
    
    private void OnCreditsClicked()
    {
        PlaySound(buttonClickSound);
        ShowCreditsPanel();
    }
    
    private void OnQuitClicked()
    {
        PlaySound(buttonClickSound);
        ShowConfirmationDialog(
            "Are you sure you want to quit the game?",
            "Quit Game"
        );
        isNewGameOverwrite = false; // Use this flag to distinguish quit vs new game
    }
    
    // ============================================================================
    // GAME FLOW METHODS
    // ============================================================================
    
    // ============================================================================
    // SLOT PICKER
    // ============================================================================

    private SlotPickerMode currentSlotPickerMode;

    private void OpenSlotPicker(SlotPickerMode mode)
    {
        currentSlotPickerMode = mode;

        if (slotPickerTitleText != null)
            slotPickerTitleText.text = mode == SlotPickerMode.Load ? "Load Game" : "New Game — Choose Slot";

        PopulateSlotPicker(mode);

        SetPanelActive(mainPanel, false);
        SetPanelActive(slotPickerPanel, true);
    }

    private void PopulateSlotPicker(SlotPickerMode mode)
    {
        if (slotListParent == null || saveSlotButtonPrefab == null)
            return;

        // Clear old buttons
        foreach (Transform child in slotListParent)
            Destroy(child.gameObject);

        // SaveManager may not exist in the main menu scene yet — read slots directly from disk
        SaveSlotInfo[] slots;
        if (SaveManager.Instance != null)
        {
            slots = SaveManager.Instance.GetAllSlotInfos();
        }
        else
        {
            slots = ReadSlotInfosFromDisk();
        }

        foreach (var info in slots)
        {
            GameObject go = Instantiate(saveSlotButtonPrefab, slotListParent);
            SaveSlotButton btn = go.GetComponent<SaveSlotButton>();
            if (btn == null) continue;

            string slotName = info.slotName;

            if (mode == SlotPickerMode.Load)
            {
                bool locked = info.isEmpty;
                btn.Initialize(
                    info,
                    locked ? null : (System.Action)(() => OnSlotSelected(slotName)),
                    // Delete in Load mode: erase save and repopulate list
                    info.isEmpty || info.isAutoSave ? null : (System.Action)(() => DeleteSlotAndRefresh(slotName)),
                    locked
                );
            }
            else // NewGame
            {
                btn.Initialize(
                    info,
                    // Click on slot = start new game in that slot
                    () => OnSlotSelected(slotName),
                    // Delete button = only erase, do NOT start game
                    info.isEmpty || info.isAutoSave ? null : (System.Action)(() => DeleteSlotAndRefresh(slotName)),
                    false
                );
            }
        }
    }

    private void OnSlotSelected(string slotName)
    {
        if (currentSlotPickerMode == SlotPickerMode.Load)
        {
            LoadGameFromSlot(slotName);
        }
        else
        {
            // NewGame: start directly in chosen slot (overwrite if occupied)
            StartNewGameInSlot(slotName);
        }
    }

    private string _pendingNewGameSlot;

    /// <summary>
    /// Deletes a slot's files and repopulates the picker — does NOT start the game.
    /// </summary>
    private void DeleteSlotAndRefresh(string slotName)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSlot(slotName);
        }
        else
        {
            string dir = Path.Combine(Application.persistentDataPath, "Saves", slotName);
            if (Directory.Exists(dir))
                foreach (string f in Directory.GetFiles(dir))
                    File.Delete(f);
        }

        // Repopulate the list in place so the player can choose another slot
        PopulateSlotPicker(currentSlotPickerMode);
        CheckSaveFileAvailability();
    }

    /// <summary>
    /// Returns the slot name with the most recent save timestamp, read directly from disk.
    /// </summary>
    private string GetMostRecentSlotFromDisk()
    {
        var slots = SaveManager.Instance != null
            ? SaveManager.Instance.GetAllSlotInfos()
            : ReadSlotInfosFromDisk();

        string best = null;
        System.DateTime bestTime = System.DateTime.MinValue;

        foreach (var s in slots)
        {
            if (s.isEmpty) continue;
            if (System.DateTime.TryParse(s.saveTimestamp, out System.DateTime t) && t > bestTime)
            {
                bestTime = t;
                best = s.slotName;
            }
        }

        return best;
    }

    /// <summary>
    /// Reads slot metadata directly from disk when SaveManager is not loaded yet.
    /// </summary>
    private SaveSlotInfo[] ReadSlotInfosFromDisk()
    {
        string[] slotNames = { "AutoSave", "Slot1", "Slot2", "Slot3" };
        var result = new SaveSlotInfo[slotNames.Length];

        string savesRoot = Path.Combine(Application.persistentDataPath, "Saves");

        for (int i = 0; i < slotNames.Length; i++)
        {
            string slotName = slotNames[i];
            string metaPath = Path.Combine(savesRoot, slotName, "SlotMeta.json");

            if (File.Exists(metaPath))
            {
                try
                {
                    var info = JsonUtility.FromJson<SaveSlotInfo>(File.ReadAllText(metaPath));
                    if (info != null)
                    {
                        info.slotName = slotName;
                        info.isAutoSave = slotName == "AutoSave";
                        result[i] = info;
                        continue;
                    }
                }
                catch { }
            }

            result[i] = new SaveSlotInfo
            {
                slotName = slotName,
                isAutoSave = slotName == "AutoSave",
                isEmpty = true
            };
        }

        return result;
    }

    private bool SlotHasSaveFile(string slotName)
    {
        if (SaveManager.Instance != null)
            return SaveManager.Instance.HasSaveFile(slotName);

        string path = Path.Combine(Application.persistentDataPath, "Saves", slotName, "GameSave.json");
        return File.Exists(path);
    }

    private void LoadGameFromSlot(string slotName)
    {
        // Store slot name statically so SaveManager can pick it up after scene load
        SaveManager.pendingActiveSlot = slotName;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SetActiveSlot(slotName);

        SaveManager.resetToStartOfDayAfterLoad = true;
        PlaySound(gameStartSound);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadMainGameScene();
        else
            StartCoroutine(LoadGameScene(true));
    }

    private void StartNewGameInSlot(string slotName)
    {
        // Store slot name statically so SaveManager can pick it up after scene load
        SaveManager.pendingActiveSlot = slotName;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetActiveSlot(slotName);
            SaveManager.Instance.DeleteSaveFile();
        }
        else
        {
            // Delete the save file directly if SaveManager not loaded yet
            string path = Path.Combine(Application.persistentDataPath, "Saves", slotName, "GameSave.json");
            if (File.Exists(path)) File.Delete(path);
            string meta = Path.Combine(Application.persistentDataPath, "Saves", slotName, "SlotMeta.json");
            if (File.Exists(meta)) File.Delete(meta);
        }

        SaveManager.initializeNewGameAfterLoad = true;
        PlaySound(gameStartSound);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.LoadMainGameScene();
        else
            StartCoroutine(LoadGameScene(false));
    }

    private IEnumerator LoadGameScene(bool loadExistingSave)
    {
        ShowLoadingPanel();
        
        // Start async scene loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
        asyncLoad.allowSceneActivation = false;
        
        // Update loading progress
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;
                
            if (loadingText != null)
                loadingText.text = $"Loading... {progress:P0}";
            
            // Check if loading is almost done
            if (asyncLoad.progress >= 0.9f)
            {
                if (loadingText != null)
                    loadingText.text = loadExistingSave ? "Loading save data..." : "Initializing new game...";
                    
                // Allow scene activation
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
        
        // Scene is loaded, now handle save data if needed
        if (loadExistingSave && SaveManager.Instance != null)
        {
            // Load the save file in the new scene
            yield return new WaitForSeconds(0.1f); // Brief pause to ensure scene is ready
            SaveManager.Instance.LoadGame();
        }
        
    }
    
    // ============================================================================
    // UI PANEL MANAGEMENT
    // ============================================================================
    
    private void ShowMainPanel()
    {
        SetPanelActive(mainPanel, true);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, false);
        SetPanelActive(confirmationPanel, false);
        SetPanelActive(loadingPanel, false);
        SetPanelActive(slotPickerPanel, false);
    }
    
    private void ShowSettingsPanel()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(settingsPanel, true);
        SetPanelActive(creditsPanel, false);
        SetPanelActive(confirmationPanel, false);
    }
    
    private void ShowCreditsPanel()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, true);
        SetPanelActive(confirmationPanel, false);
    }
    
    private void ShowLoadingPanel()
    {
        SetPanelActive(mainPanel, false);
        SetPanelActive(settingsPanel, false);
        SetPanelActive(creditsPanel, false);
        SetPanelActive(confirmationPanel, false);
        SetPanelActive(loadingPanel, true);
        
        if (loadingProgressBar != null)
            loadingProgressBar.value = 0f;
    }
    
    private void ShowConfirmationDialog(string message, string title = "Confirmation")
    {
        
        if (confirmationPanel == null)
        {
            return;
        }
        
        SetPanelActive(confirmationPanel, true);
        
        if (confirmationText != null)
        {
            confirmationText.text = message;
        }
        else
        {
        }
    }
    
    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
    
    // ============================================================================
    // CONFIRMATION DIALOG HANDLERS
    // ============================================================================
    
    private void OnConfirmationYes()
    {
        PlaySound(buttonClickSound);

        if (isNewGameOverwrite)
        {
            if (!string.IsNullOrEmpty(_pendingNewGameSlot))
                StartNewGameInSlot(_pendingNewGameSlot);

            _pendingNewGameSlot = null;
        }
        else
        {
            // Quit game using MainMenuManager
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.QuitApplication();
            }
            else
            {
                Application.Quit();

                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
        }

        SetPanelActive(confirmationPanel, false);
    }
    
    private void OnConfirmationNo()
    {
        PlaySound(buttonClickSound);
        SetPanelActive(confirmationPanel, false);
        ShowMainPanel();
    }
    
    // ============================================================================
    // SETTINGS HANDLERS
    // ============================================================================
    
    private void OnSettingsBackClicked()
    {
        PlaySound(buttonClickSound);
        SaveSettings();
        ShowMainPanel();
    }
    
    private void OnMasterVolumeChanged(float value)
    {
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("MasterVolume", value);
        
        // Apply immediately to Unity's audio system
        AudioListener.volume = value;
        
        // Update MainMenuManager's background music if it exists
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.UpdateAudioVolumes();
        }
        
        
        // Play a quick sound to test the volume change
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, 0.5f);
        }
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        
        // Update MainMenuManager's background music
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.UpdateAudioVolumes();
        }
        
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        
        
        // Play a test sound with the new SFX volume
        if (audioSource != null && buttonClickSound != null)
        {
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            audioSource.PlayOneShot(buttonClickSound, value * masterVolume);
        }
    }
    
    private void OnFullscreenToggled(bool isFullscreen)
    {
        
        try
        {
            // Apply fullscreen setting
            Screen.fullScreen = isFullscreen;
            
            // Save the setting
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
            
            // Play feedback sound
            PlaySound(buttonClickSound);
            
        }
        catch (System.Exception e)
        {
        }
    }
    
    private void OnResolutionChanged(int resolutionIndex)
    {
        // Get the filtered resolutions (same logic as in SetupResolutionDropdown)
        Resolution[] allResolutions = Screen.resolutions;
        System.Collections.Generic.List<Resolution> filteredResolutions = new System.Collections.Generic.List<Resolution>();
        System.Collections.Generic.HashSet<string> addedResolutions = new System.Collections.Generic.HashSet<string>();
        
        for (int i = allResolutions.Length - 1; i >= 0; i--)
        {
            Resolution res = allResolutions[i];
            string resolutionString = $"{res.width} x {res.height}";
            
            if (!addedResolutions.Contains(resolutionString) && res.width >= 1280 && res.height >= 720)
            {
                addedResolutions.Add(resolutionString);
                filteredResolutions.Add(res);
            }
        }
        
        filteredResolutions.Reverse(); // Match the dropdown order
        
        if (resolutionIndex >= 0 && resolutionIndex < filteredResolutions.Count)
        {
            Resolution selectedResolution = filteredResolutions[resolutionIndex];
            
            
            try
            {
                // Apply the resolution change
                Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen, selectedResolution.refreshRate);
                
                // Save the resolution choice
                PlayerPrefs.SetInt("ResolutionWidth", selectedResolution.width);
                PlayerPrefs.SetInt("ResolutionHeight", selectedResolution.height);
                PlayerPrefs.SetInt("RefreshRate", selectedResolution.refreshRate);
                
                // Play feedback sound
                PlaySound(buttonClickSound);
                
            }
            catch (System.Exception e)
            {
            }
        }
        else
        {
        }
    }
    
    private void SaveSettings()
    {
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Reset all settings to default values
    /// Call this from a "Reset to Defaults" button
    /// </summary>
    public void ResetSettingsToDefaults()
    {
        
        // Reset audio settings
        PlayerPrefs.SetFloat("MasterVolume", 1f);
        PlayerPrefs.SetFloat("MusicVolume", 1f);
        PlayerPrefs.SetFloat("SFXVolume", 1f);
        
        // Reset graphics settings
        PlayerPrefs.SetInt("Fullscreen", 1);
        PlayerPrefs.DeleteKey("ResolutionWidth");  // This will use current screen resolution
        PlayerPrefs.DeleteKey("ResolutionHeight");
        PlayerPrefs.DeleteKey("RefreshRate");
        
        // Apply the defaults immediately
        AudioListener.volume = 1f;
        Screen.fullScreen = true;
        
        // Update UI controls to reflect the new values
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (musicVolumeSlider != null) musicVolumeSlider.value = 1f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 1f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        
        // Refresh resolution dropdown
        SetupResolutionDropdown();
        
        // Update MainMenuManager's audio
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.UpdateAudioVolumes();
        }
        
        // Save the changes
        SaveSettings();
        
        // Play confirmation sound
        PlaySound(buttonClickSound);
        
    }
    
    // ============================================================================
    // SAVE FILE MANAGEMENT
    // ============================================================================
    
    private void CheckSaveFileAvailability()
    {
        bool hasSave;

        if (SaveManager.Instance != null)
            hasSave = SaveManager.Instance.GetAllSlotInfos().Any(s => !s.isEmpty);
        else
            hasSave = ReadSlotInfosFromDisk().Any(s => !s.isEmpty);

        if (continueButton != null)
        {
#if DEMO_BUILD
            continueButton.gameObject.SetActive(false);
#else
            continueButton.interactable = hasSave;
#endif

            var buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.color = hasSave ? Color.white : Color.gray;
        }
    }
    
    private void UpdateSaveInfoDisplay()
    {
        if (saveInfoPanel == null) return;
        
        if (SaveManager.Instance != null && SaveManager.Instance.HasSaveFile())
        {
            try 
            {
                // Get save file metadata (file info, not game data)
                var saveInfo = SaveManager.Instance.GetSaveFileInfo();
                if (saveInfo != null)
                {
                    // Show file metadata since we can't get game data without loading the full save
                    if (playerNameText != null)
                        playerNameText.text = "Save File Found";
                        
                    if (dayText != null)
                        dayText.text = $"File: {saveInfo.fileName}";
                        
                    if (moneyText != null)
                        moneyText.text = $"Size: {saveInfo.fileSizeBytes / 1024}KB";
                        
                    if (lastPlayedText != null)
                        lastPlayedText.text = $"Last Saved: {saveInfo.lastWriteTime:MM/dd/yyyy HH:mm}";
                        
                    saveInfoPanel.SetActive(true);
                }
                else
                {
                    saveInfoPanel.SetActive(false);
                }
            }
            catch
            {
                // Hide save info panel if there's any issue
                saveInfoPanel.SetActive(false);
            }
        }
        else
        {
            saveInfoPanel.SetActive(false);
        }
    }
    
    // ============================================================================
    // UTILITY METHODS
    // ============================================================================
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            audioSource.PlayOneShot(clip, sfxVolume);
        }
    }
    
    // ============================================================================
    // PUBLIC API (for external scripts)
    // ============================================================================
    
    /// <summary>
    /// External method to refresh save file availability (call after save/delete operations)
    /// </summary>
    public void RefreshSaveStatus()
    {
        CheckSaveFileAvailability();
        UpdateSaveInfoDisplay();
    }
    
    /// <summary>
    /// Show a specific panel from external scripts
    /// </summary>
    public void ShowPanel(string panelName)
    {
        switch (panelName.ToLower())
        {
            case "main":
                ShowMainPanel();
                break;
            case "settings":
                ShowSettingsPanel();
                break;
            case "credits":
                ShowCreditsPanel();
                break;
        }
    }
}