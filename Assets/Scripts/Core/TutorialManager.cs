using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using System.Collections.Generic;

namespace SowurShield.Core
{

/// <summary>
/// Guided first-day tutorial. Shows tooltip prompts one step at a time,
/// advancing when the player completes the action (tracked via events).
///
/// Tutorial flow:
///   1. Till soil      — SoilBlockInteractable fires OnSoilTilled event
///   2. Plant seed     — SoilBlockInteractable fires OnSeedPlanted event
///   3. Water crop     — SoilBlockInteractable fires OnCropWatered event
///   4. Open sell box  — SellBox fires OnSellBoxOpened (or manual)
///   5. Sleep          — BedInteractable fires OnSleep / day advances
///   6. Harvest crop   — CropGrowthManager fires OnCropHarvested event
///
/// ISaveable persists progress in GameData.progressData.tutorialSteps.
///
/// SETUP IN UNITY:
///   1. Add TutorialManager to a persistent GameObject
///   2. Assign tutorialPanel, stepText, arrowIndicator (optional), skipButton
///   3. Call TutorialManager.Instance.StartTutorial() on new game
/// </summary>
public class TutorialManager : MonoBehaviour, ISaveable
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI stepText;
    [SerializeField] private TextMeshProUGUI stepCountText;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button nextButton; // Optional — for non-event steps

    [Header("Settings")]
    [SerializeField] private bool showOnNewGame = true;

    [Header("Tutorial Step Text")]
    [SerializeField] private LocalizedString tillSoilTitle_Localized; // table "Tutorial", key "tutorial.till_soil.title"
    [SerializeField] private LocalizedString tillSoilDescription_Localized; // table "Tutorial", key "tutorial.till_soil.description"
    [SerializeField] private LocalizedString plantSeedTitle_Localized; // table "Tutorial", key "tutorial.plant_seed.title"
    [SerializeField] private LocalizedString plantSeedDescription_Localized; // table "Tutorial", key "tutorial.plant_seed.description"
    [SerializeField] private LocalizedString waterCropTitle_Localized; // table "Tutorial", key "tutorial.water_crop.title"
    [SerializeField] private LocalizedString waterCropDescription_Localized; // table "Tutorial", key "tutorial.water_crop.description"
    [SerializeField] private LocalizedString petAnimalTitle_Localized; // table "Tutorial", key "tutorial.pet_animal.title"
    [SerializeField] private LocalizedString petAnimalDescription_Localized; // table "Tutorial", key "tutorial.pet_animal.description"
    [SerializeField] private LocalizedString sleepTitle_Localized; // table "Tutorial", key "tutorial.sleep.title"
    [SerializeField] private LocalizedString sleepDescription_Localized; // table "Tutorial", key "tutorial.sleep.description"
    [SerializeField] private LocalizedString harvestTitle_Localized; // table "Tutorial", key "tutorial.harvest.title"
    [SerializeField] private LocalizedString harvestDescription_Localized; // table "Tutorial", key "tutorial.harvest.description"
    [SerializeField] private LocalizedString stepProgressText_Localized; // table "Tutorial", key "tutorial.step_progress"

    // Tutorial steps in order. Built at runtime (not a static literal array) because
    // LocalizedString fields are populated via the Inspector and aren't available
    // as compile-time constants.
    private TutorialStep[] Steps;

    private void BuildSteps()
    {
        Steps = new TutorialStep[]
        {
            new TutorialStep("till_soil", tillSoilDescription_Localized, tillSoilTitle_Localized),
            new TutorialStep("plant_seed", plantSeedDescription_Localized, plantSeedTitle_Localized),
            new TutorialStep("water_crop", waterCropDescription_Localized, waterCropTitle_Localized),
            new TutorialStep("pet_animal", petAnimalDescription_Localized, petAnimalTitle_Localized),
            new TutorialStep("sleep", sleepDescription_Localized, sleepTitle_Localized),
            new TutorialStep("harvest", harvestDescription_Localized, harvestTitle_Localized),
        };
    }

    private int _currentStepIndex = -1; // -1 = not started
    private bool _tutorialComplete = false;
    private readonly HashSet<string> _completedStepIds = new HashSet<string>();

    // =========================================================================
    // Events — other systems call these to advance the tutorial
    // =========================================================================

    public static void NotifyStepComplete(string stepId)
    {
        Instance?.AdvanceIfCurrentStep(stepId);
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (SaveManager.Instance != null)
                SaveManager.Instance.RegisterSaveable(this);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildSteps();

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (skipButton != null) skipButton.onClick.AddListener(CompleteTutorial);
        if (nextButton != null) nextButton.onClick.AddListener(ManualAdvance);

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (IsTutorialActive)
            ShowCurrentStep();
    }

    private void Start()
    {
        if (Instance == this && SaveManager.Instance != null)
            SaveManager.Instance.RegisterSaveable(this);
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.UnregisterSaveable(this);

        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void StartTutorial()
    {
        if (_tutorialComplete) return;

        _currentStepIndex = 0;
        // Skip already-done steps (in case of partial save)
        while (_currentStepIndex < Steps.Length &&
               _completedStepIds.Contains(Steps[_currentStepIndex].stepId))
        {
            _currentStepIndex++;
        }

        if (_currentStepIndex >= Steps.Length)
        {
            CompleteTutorial();
            return;
        }

        ShowCurrentStep();
    }

    public bool IsTutorialComplete => _tutorialComplete;
    public bool IsTutorialActive => _currentStepIndex >= 0 && !_tutorialComplete;

    public void CompleteTutorial()
    {
        _tutorialComplete = true;
        _currentStepIndex = Steps.Length;
        HidePanel();
    }

    // =========================================================================
    // Step Advancement
    // =========================================================================

    private void AdvanceIfCurrentStep(string stepId)
    {
        if (!IsTutorialActive) return;
        if (_currentStepIndex >= Steps.Length) return;
        if (Steps[_currentStepIndex].stepId != stepId) return;

        _completedStepIds.Add(stepId);
        _currentStepIndex++;

        if (_currentStepIndex >= Steps.Length)
        {
            CompleteTutorial();
            return;
        }

        // Skip already-complete steps
        while (_currentStepIndex < Steps.Length &&
               _completedStepIds.Contains(Steps[_currentStepIndex].stepId))
        {
            _currentStepIndex++;
        }

        if (_currentStepIndex >= Steps.Length)
            CompleteTutorial();
        else
            ShowCurrentStep();
    }

    private void ManualAdvance()
    {
        if (!IsTutorialActive) return;
        if (_currentStepIndex < 0 || _currentStepIndex >= Steps.Length) return;
        // Delegate to the single advancement path — it marks complete, advances,
        // skips finished steps, and shows/completes as needed.
        AdvanceIfCurrentStep(Steps[_currentStepIndex].stepId);
    }

    private void ShowCurrentStep()
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= Steps.Length) return;

        TutorialStep step = Steps[_currentStepIndex];

        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (stepText != null)      stepText.text = step.description.SafeGetLocalizedString();
        if (stepCountText != null)
        {
            stepProgressText_Localized.Arguments = new object[] { _currentStepIndex + 1, Steps.Length, step.title.SafeGetLocalizedString() };
            stepCountText.text = stepProgressText_Localized.SafeGetLocalizedString();
        }
    }

    private void HidePanel()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    // =========================================================================
    // ISaveable
    // =========================================================================

    public void SaveData(GameData gameData)
    {
        if (gameData?.progressData == null) return;

        gameData.progressData.hasCompletedTutorial = _tutorialComplete;

        foreach (var step in Steps)
            gameData.progressData.tutorialSteps[step.stepId] = _completedStepIds.Contains(step.stepId);
    }

    public void LoadData(GameData gameData)
    {
        if (gameData?.progressData == null) return;

        _tutorialComplete = gameData.progressData.hasCompletedTutorial;
        _completedStepIds.Clear();

        foreach (var kv in gameData.progressData.tutorialSteps)
        {
            if (kv.Value) _completedStepIds.Add(kv.Key);
        }

        if (!_tutorialComplete && showOnNewGame)
        {
            // Re-enter at the right step after load
            _currentStepIndex = -1; // StartTutorial will seek forward
            StartTutorial();
        }
    }
}

// ============================================================================
// Data
// ============================================================================

[System.Serializable]
public class TutorialStep
{
    public string stepId;
    public LocalizedString description;
    public LocalizedString title;

    public TutorialStep(string stepId, LocalizedString description, LocalizedString title)
    {
        this.stepId      = stepId;
        this.description = description;
        this.title       = title;
    }
}

} // namespace SowurShield.Core
