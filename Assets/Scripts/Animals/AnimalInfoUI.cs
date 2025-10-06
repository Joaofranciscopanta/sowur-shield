using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel that displays detailed information about an animal.
/// Shows picture, name, type, food requirements, and current status.
/// </summary>
public class AnimalInfoUI : MonoBehaviour
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

    private void Awake()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseUI);
        }
    }

    private void Update()
    {
        // Close with ESC key
        if (Input.GetKeyDown(KeyCode.Escape) && infoPanel != null && infoPanel.activeSelf)
        {
            CloseUI();
        }
    }

    /// <summary>
    /// Show the animal info UI for a specific animal
    /// </summary>
    public void ShowAnimalInfo(Animal animal)
    {
        if (animal == null || animal.AnimalData == null)
        {
            Debug.LogWarning("Cannot show info for null animal!");
            return;
        }

        currentAnimal = animal;
        AnimalData data = animal.AnimalData;

        // Set portrait
        if (animalPortrait != null && data.idleSprite != null)
        {
            animalPortrait.sprite = data.idleSprite;
            animalPortrait.gameObject.SetActive(true);
        }

        // Set name
        if (animalNameText != null)
        {
            animalNameText.text = data.animalName;
        }

        // Set type with classification
        if (animalTypeText != null)
        {
            string typeInfo = data.animalType;

            // Add family and class if available
            if (!string.IsNullOrEmpty(data.animalFamily) || !string.IsNullOrEmpty(data.animalClass))
            {
                typeInfo += "\n";
                if (!string.IsNullOrEmpty(data.animalClass))
                {
                    typeInfo += $"Class: {data.animalClass}";
                }
                if (!string.IsNullOrEmpty(data.animalFamily))
                {
                    typeInfo += $"\nFamily: {data.animalFamily}";
                }
            }

            animalTypeText.text = typeInfo;
        }

        // Set food requirements
        if (foodRequirementsText != null)
        {
            string requirements = "Daily Food Needs:\n";
            if (data.dailyFoodRequirements != null && data.dailyFoodRequirements.Count > 0)
            {
                foreach (FoodRequirement req in data.dailyFoodRequirements)
                {
                    requirements += $"• {req.quantityPerDay}x {req.itemName}\n";
                }
            }
            else
            {
                requirements += "No special food needed";
            }
            foodRequirementsText.text = requirements;
        }

        // Set food status
        UpdateFoodStatus();

        // Set buffs (placeholder for now)
        if (buffsText != null)
        {
            string buffs = "Status:\n";
            if (animal.HasBeenPetToday)
            {
                buffs += "• Happy (Petted today)\n";
            }
            if (!animal.NeedsFeeding)
            {
                buffs += "• Well Fed\n";
            }
            else
            {
                buffs += "• Hungry\n";
            }

            buffsText.text = buffs;
        }

        // Set production info
        if (productionText != null)
        {
            if (data.canProduce)
            {
                string production = $"Produces:\n";
                production += $"• {data.produceItemName}\n";
                production += $"• Every {data.productionIntervalDays} day(s)\n";
                production += $"• Amount: {data.minProduceAmount}-{data.maxProduceAmount}\n";
                productionText.text = production;
            }
            else
            {
                productionText.text = "No production";
            }
        }

        // Show panel
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }

        // Disable player movement
        DisablePlayerMovement();
    }

    private void UpdateFoodStatus()
    {
        if (currentAnimal == null) return;

        float foodPercentage = currentAnimal.GetFoodPercentage();

        // Update status text
        if (foodStatusText != null)
        {
            int totalRequired = 0;
            if (currentAnimal.AnimalData.dailyFoodRequirements != null)
            {
                foreach (FoodRequirement req in currentAnimal.AnimalData.dailyFoodRequirements)
                {
                    totalRequired += req.quantityPerDay;
                }
            }

            foodStatusText.text = $"Fed Today: {currentAnimal.FoodEatenToday}/{totalRequired}";

            // Color code the text
            if (foodPercentage >= 1f)
            {
                foodStatusText.color = wellFedColor;
            }
            else if (foodPercentage >= 0.5f)
            {
                foodStatusText.color = partiallyFedColor;
            }
            else
            {
                foodStatusText.color = hungryColor;
            }
        }

        // Update progress bar
        if (foodProgressBar != null)
        {
            foodProgressBar.fillAmount = foodPercentage;

            // Color code the bar
            if (foodPercentage >= 1f)
            {
                foodProgressBar.color = wellFedColor;
            }
            else if (foodPercentage >= 0.5f)
            {
                foodProgressBar.color = partiallyFedColor;
            }
            else
            {
                foodProgressBar.color = hungryColor;
            }
        }
    }

    public void CloseUI()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        currentAnimal = null;

        // Re-enable player movement
        EnablePlayerMovement();
    }

    private void DisablePlayerMovement()
    {
        PlayerMove player = FindObjectOfType<PlayerMove>();
        if (player != null)
        {
            player.enabled = false;
        }
    }

    private void EnablePlayerMovement()
    {
        PlayerMove player = FindObjectOfType<PlayerMove>();
        if (player != null)
        {
            player.enabled = true;
        }
    }

    public bool IsOpen()
    {
        return infoPanel != null && infoPanel.activeSelf;
    }
}
