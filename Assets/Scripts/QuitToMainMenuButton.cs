using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple script for quit buttons that return to the main menu
/// Can be used in any scene (sample scene, test scenes, etc.)
/// Just attach to a GameObject with a Button component
/// </summary>
[RequireComponent(typeof(Button))]
public class QuitToMainMenuButton : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showConfirmation = true;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("Confirmation Dialog (Optional)")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private UnityEngine.UI.Text confirmationText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;
    
    private Button quitButton;
    
    private void Start()
    {
        // Get the button component
        quitButton = GetComponent<Button>();
        
        // Add click listener
        quitButton.onClick.AddListener(OnQuitButtonClicked);
        
        // Setup confirmation dialog if it exists
        SetupConfirmationDialog();
        
        // Find audio source if not assigned
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        Debug.Log($"[QuitToMainMenuButton] Quit button setup complete on {gameObject.name}");
    }
    
    private void SetupConfirmationDialog()
    {
        if (confirmationPanel != null)
        {
            // Make sure confirmation panel starts inactive
            confirmationPanel.SetActive(false);
            
            // Setup yes/no buttons
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(ConfirmQuit);
                
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(CancelQuit);
        }
    }
    
    /// <summary>
    /// Called when the quit button is clicked
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Debug.Log("[QuitToMainMenuButton] Quit button clicked");
        
        // Play sound
        PlaySound(clickSound);
        
        if (showConfirmation && confirmationPanel != null)
        {
            ShowConfirmationDialog();
        }
        else
        {
            // No confirmation needed, quit immediately
            QuitToMainMenu();
        }
    }
    
    private void ShowConfirmationDialog()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            
            // Update confirmation text
            if (confirmationText != null)
            {
                confirmationText.text = "Return to Main Menu?\n\nAny unsaved progress will be lost.\nMake sure to save your game first!";
            }
            
            Debug.Log("[QuitToMainMenuButton] Showing confirmation dialog");
        }
    }
    
    /// <summary>
    /// Called when user confirms they want to quit
    /// </summary>
    public void ConfirmQuit()
    {
        Debug.Log("[QuitToMainMenuButton] Quit confirmed by user");
        
        // Hide confirmation dialog
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
            
        // Play sound
        PlaySound(clickSound);
        
        // Quit to main menu
        QuitToMainMenu();
    }
    
    /// <summary>
    /// Called when user cancels the quit
    /// </summary>
    public void CancelQuit()
    {
        Debug.Log("[QuitToMainMenuButton] Quit cancelled by user");
        
        // Hide confirmation dialog
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
            
        // Play sound
        PlaySound(clickSound);
    }
    
    /// <summary>
    /// Actually perform the quit to main menu
    /// </summary>
    private void QuitToMainMenu()
    {
        Debug.Log($"[QuitToMainMenuButton] Quitting to main menu scene: {mainMenuSceneName}");
        
        // Reset time scale (in case it was paused)
        Time.timeScale = 1f;
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Try to use GameMenuManager if it exists (for proper cleanup)
        if (GameMenuManager.Instance != null)
        {
            Debug.Log("[QuitToMainMenuButton] Using GameMenuManager for proper cleanup");
            GameMenuManager.Instance.DoQuitToMainMenu();
            return;
        }
        
        // Try to use SceneTransitionManager if available (for smooth transition)
        if (SceneTransitionManager.Instance != null)
        {
            Debug.Log("[QuitToMainMenuButton] Using SceneTransitionManager for smooth transition");
            SceneTransitionManager.Instance.LoadMainMenu();
            return;
        }
        
        // Fallback: Direct scene loading
        Debug.Log("[QuitToMainMenuButton] Using direct scene loading (no transition manager found)");
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    /// <summary>
    /// Play an audio clip
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            float volume = PlayerPrefs.GetFloat("SFXVolume", 1f) * PlayerPrefs.GetFloat("MasterVolume", 1f);
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    /// <summary>
    /// Public method to quit without confirmation (can be called from other scripts)
    /// </summary>
    public void QuitToMainMenuDirect()
    {
        QuitToMainMenu();
    }
    
    /// <summary>
    /// Enable/disable the confirmation dialog
    /// </summary>
    public void SetShowConfirmation(bool show)
    {
        showConfirmation = show;
    }
    
    /// <summary>
    /// Change the target main menu scene name
    /// </summary>
    public void SetMainMenuSceneName(string sceneName)
    {
        mainMenuSceneName = sceneName;
    }
}