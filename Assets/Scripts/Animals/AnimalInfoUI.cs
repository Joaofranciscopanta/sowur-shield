using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Colors")]
    [SerializeField] private Color wellFedColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private Color hungryColor = new Color(0.8f, 0.3f, 0.3f);
    [SerializeField] private Color partiallyFedColor = new Color(0.8f, 0.8f, 0.3f);

    private Animal currentAnimal;

    // =========================================================================
    // IUIWindow Implementation
    // =========================================================================

    public string WindowName => "AnimalInfo";
    public int WindowPriority => WindowPriority.Inventory; // Same tier as inventory
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
        if (infoPanel != null)
            infoPanel.SetActive(false);

        currentAnimal = null;
        EnablePlayerMovement();
    }

    public void OnWindowBlocked(string blockedBy)
    {
        Debug.Log($"[AnimalInfoUI] Cannot open — blocked by '{blockedBy}'");
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

        // Name
        if (animalNameText != null)
            animalNameText.text = data.animalName;

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

        // Buffs / Status
        if (buffsText != null)
        {
            string buffs = "Status:\n";
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
        PlayerMove player = FindObjectOfType<PlayerMove>();
        if (player != null)
            player.DisableMovement();
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = FindObjectOfType<PlayerMove>();
        if (player != null)
            player.EnableMovement();
    }

    // =========================================================================
    // Legacy
    // =========================================================================

    /// <summary>For backward compatibility — prefer CloseUI().</summary>
    public bool IsOpen() => IsWindowOpen;
}
