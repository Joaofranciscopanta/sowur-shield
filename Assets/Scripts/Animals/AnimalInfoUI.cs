using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SowurShield.Core;

namespace SowurShield.Animals
{

/// <summary>
/// UI panel that displays detailed information about an animal.
/// Shows portrait, name, type, food requirements, current status, and live production info.
/// Implements IUIWindow for proper UIManager integration and ESC key handling.
/// </summary>
public class AnimalInfoUI : MonoBehaviour, IUIWindow
{
    [Header("UI References")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Image animalPortrait;
    [SerializeField] private TextMeshProUGUI animalNameText;
    [SerializeField] private TextMeshProUGUI animalTypeText;
    [SerializeField] private TextMeshProUGUI foodRequirementsText;
    [SerializeField] private TextMeshProUGUI foodStatusText;
    [SerializeField] private Image foodProgressBar;
    [SerializeField] private TextMeshProUGUI buffsText;
    [SerializeField] private TextMeshProUGUI productionText;
    [SerializeField] private Button closeButton;

    [Header("Happiness UI")]
    [SerializeField] private Image happinessProgressBar;
    [SerializeField] private TextMeshProUGUI happinessText;
    [SerializeField] private TextMeshProUGUI happinessMultiplierText;

    [Header("Rename UI")]
    [SerializeField] private Button renameButton;
    [SerializeField] private GameObject renamePanel;
    [SerializeField] private TMP_InputField renameInputField;
    [SerializeField] private Button renameConfirmButton;
    [SerializeField] private Button renameCancelButton;

    [Header("Colors")]
    [SerializeField] private Color wellFedColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color hungryColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color partiallyFedColor = new Color(0.8f, 0.8f, 0.3f);
    [SerializeField] private Color happyColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color neutralColor = new Color(0.8f, 0.8f, 0.3f);
    [SerializeField] private Color sadColor = new Color(0.8f, 0.3f, 0.3f);

    private Animal currentAnimal;

    // =========================================================================
    // IUIWindow Implementation
    // =========================================================================

    public string WindowName => "AnimalInfo";
    public int WindowPriority => SowurShield.Core.WindowPriority.Inventory; // Same tier as inventory
    public bool IsWindowOpen => infoPanel != null && infoPanel.activeSelf;
    public bool CanCloseWithEsc => true;

    public void OpenWindow()
    {
        if (infoPanel != null)
            infoPanel.SetActive(true);

        DisablePlayerMovement();
    }

    public void CloseWindow()
    {
        HideRenamePanel();

        if (infoPanel != null)
            infoPanel.SetActive(false);

        currentAnimal = null;
        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy)
    {
        Debug.LogWarning($"[AnimalInfoUI] Cannot open — blocked by '{blockedBy}'");
    }

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Awake()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);

        // Wire rename UI buttons
        if (renameButton != null)
            renameButton.onClick.AddListener(ShowRenamePanel);
        if (renameConfirmButton != null)
            renameConfirmButton.onClick.AddListener(ConfirmRename);
        if (renameCancelButton != null)
            renameCancelButton.onClick.AddListener(HideRenamePanel);
        if (renamePanel != null)
            renamePanel.SetActive(false);

        // Register with UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.RegisterWindow(this);
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterWindow(this);
    }

    // =========================================================================
    // Show / Close
    // =========================================================================

    /// <summary>Show the animal info UI for a specific animal.</summary>
    public void ShowAnimalInfo(Animal animal)
    {
        if (animal == null || animal.AnimalData == null)
        {
            Debug.LogWarning("Cannot show info for null animal!");
            return;
        }

        currentAnimal = animal;
        PopulateUI(animal);

        // Use UIManager for proper window management
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TryOpenWindow(this);
        }
        else
        {
            // Fallback when UIManager is not present
            OpenWindow();
        }
    }

    public void CloseUI()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.TryCloseWindow(this);
        else
            CloseWindow();
    }

    // =========================================================================
    // UI Population
    // =========================================================================

    private void PopulateUI(Animal animal)
    {
        AnimalData data = animal.AnimalData;

        // Portrait
        if (animalPortrait != null && data.idleSprite != null)
        {
            animalPortrait.sprite = data.idleSprite;
            animalPortrait.gameObject.SetActive(true);
        }

        // Name (prefer custom name → data name → GO name)
        if (animalNameText != null)
            animalNameText.text = animal.GetDisplayName();

        // Type + Classification
        if (animalTypeText != null)
        {
            string typeInfo = data.animalType;
            if (!string.IsNullOrEmpty(data.animalFamily) || !string.IsNullOrEmpty(data.animalClass))
            {
                typeInfo += "\n";
                if (!string.IsNullOrEmpty(data.animalClass))
                    typeInfo += $"Class: {data.animalClass}";
                if (!string.IsNullOrEmpty(data.animalFamily))
                    typeInfo += $"\nFamily: {data.animalFamily}";
            }
            animalTypeText.text = typeInfo;
        }

        // Food requirements
        if (foodRequirementsText != null)
        {
            string requirements = "Daily Food Needs:\n";
            if (data.dailyFoodRequirements != null && data.dailyFoodRequirements.Count > 0)
            {
                foreach (FoodRequirement req in data.dailyFoodRequirements)
                    requirements += $"• {req.quantityPerDay}x {req.itemName}\n";
            }
            else
            {
                requirements += "No special food needed";
            }
            foodRequirementsText.text = requirements;
        }

        // Food status
        UpdateFoodStatus();

        // Happiness
        UpdateHappinessStatus();

        // Buffs / Status
        if (buffsText != null)
        {
            string buffs = "Status:\n";
            if (animal.IsIll)
            {
                string cureItem = animal.AnimalData?.illnessCureItemName ?? "Medicine";
                buffs += $"• ILL — use {cureItem} to cure\n";
            }
            if (animal.HasBeenPetToday)
                buffs += "• Happy (Petted today)\n";
            if (!animal.NeedsFeeding)
                buffs += "• Well Fed\n";
            else
                buffs += "• Hungry\n";
            buffsText.text = buffs;
        }

        // Production — live status
        UpdateProductionStatus();
    }

    private void UpdateFoodStatus()
    {
        if (currentAnimal == null) return;

        float foodPercentage = currentAnimal.GetFoodPercentage();

        if (foodStatusText != null)
        {
            int totalRequired = 0;
            if (currentAnimal.AnimalData.dailyFoodRequirements != null)
            {
                foreach (FoodRequirement req in currentAnimal.AnimalData.dailyFoodRequirements)
                    totalRequired += req.quantityPerDay;
            }

            foodStatusText.text = $"Fed Today: {currentAnimal.FoodEatenToday}/{totalRequired}";

            if (foodPercentage >= 1f)
                foodStatusText.color = wellFedColor;
            else if (foodPercentage >= 0.5f)
                foodStatusText.color = partiallyFedColor;
            else
                foodStatusText.color = hungryColor;
        }

        if (foodProgressBar != null)
        {
            foodProgressBar.fillAmount = foodPercentage;

            if (foodPercentage >= 1f)
                foodProgressBar.color = wellFedColor;
            else if (foodPercentage >= 0.5f)
                foodProgressBar.color = partiallyFedColor;
            else
                foodProgressBar.color = hungryColor;
        }
    }

    private void UpdateHappinessStatus()
    {
        if (currentAnimal == null) return;

        float happinessValue = currentAnimal.GetHappiness();
        float multiplier = currentAnimal.GetHappinessMultiplier();

        // Determine color based on happiness level
        Color barColor;
        if (happinessValue >= 70f)
            barColor = happyColor;
        else if (happinessValue >= 40f)
            barColor = neutralColor;
        else
            barColor = sadColor;

        // Update progress bar
        if (happinessProgressBar != null)
        {
            happinessProgressBar.fillAmount = happinessValue / 100f;
            happinessProgressBar.color = barColor;
        }

        // Update text
        if (happinessText != null)
        {
            happinessText.text = $"Happiness: {happinessValue:F0}/100";
            happinessText.color = barColor;
        }

        // Update multiplier display
        if (happinessMultiplierText != null)
        {
            happinessMultiplierText.text = $"Stat Multiplier: {multiplier:F2}x";
        }
    }

    /// <summary>Shows live production status — whether produce dropped today or when it's next due.</summary>
    private void UpdateProductionStatus()
    {
        if (productionText == null || currentAnimal == null) return;

        AnimalData data = currentAnimal.AnimalData;

        if (!data.canProduce)
        {
            productionText.text = "No production";
            return;
        }

        string production = $"Produces: {data.produceItemName}\n";
        production += $"Every {data.productionIntervalDays} day(s) | {data.minProduceAmount}-{data.maxProduceAmount}";

        if (data.happinessProductionBonus > 0f)
            production += $" (+{(int)(data.happinessProductionBonus * 100)}% if happy)";

        production += "\n";

        // Live status: did we already produce today?
        int today = currentAnimal.CurrentDay;
        bool producedToday = currentAnimal.LastProductionDay == today
                             && today % data.productionIntervalDays == 0;

        if (producedToday)
        {
            production += "✓ Produced today!";
        }
        else
        {
            // Find the next production day
            int daysUntilNext = data.productionIntervalDays - (today % data.productionIntervalDays);
            if (daysUntilNext == data.productionIntervalDays) daysUntilNext = 0; // today is a production day

            if (daysUntilNext == 0)
                production += "Ready to produce today";
            else
                production += $"Next production in {daysUntilNext} day(s)";

            if (data.produceOnlyIfFed && currentAnimal.NeedsFeeding)
                production += "\n⚠ Needs feeding first!";
        }

        productionText.text = production;
    }

    // =========================================================================
    // Player Movement Helpers
    // =========================================================================

    private void DisablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
            player.DisableMovement();
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = Object.FindFirstObjectByType<PlayerMove>();
        if (player != null)
            player.EnableMovement();
    }

    // =========================================================================
    // Rename System
    // =========================================================================

    /// <summary>Show the rename panel with the current name pre-filled.</summary>
    private void ShowRenamePanel()
    {
        if (renamePanel == null || currentAnimal == null) return;

        renamePanel.SetActive(true);

        if (renameInputField != null)
        {
            string existingName = currentAnimal.GetCustomName();
            renameInputField.text = string.IsNullOrEmpty(existingName)
                ? currentAnimal.AnimalData.animalName
                : existingName;
            renameInputField.characterLimit = 20;
            renameInputField.ActivateInputField();
        }
    }

    /// <summary>Hide the rename panel without saving.</summary>
    private void HideRenamePanel()
    {
        if (renamePanel != null)
            renamePanel.SetActive(false);
    }

    /// <summary>Confirm the rename and update the animal's custom name.</summary>
    private void ConfirmRename()
    {
        if (currentAnimal == null || renameInputField == null) return;

        string newName = renameInputField.text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            // Empty name resets to default
            currentAnimal.SetCustomName("");
        }
        else
        {
            currentAnimal.SetCustomName(newName);
        }

        // Update displayed name
        if (animalNameText != null)
            animalNameText.text = currentAnimal.GetDisplayName();

        HideRenamePanel();
    }

    // =========================================================================
    // Legacy
    // =========================================================================

    /// <summary>For backward compatibility — prefer CloseUI().</summary>
    public bool IsOpen() => IsWindowOpen;
}

} // namespace SowurShield.Animals
