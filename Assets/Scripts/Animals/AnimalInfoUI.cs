using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [Header("Combat Stats UI (Optional)")]
    [SerializeField] private TextMeshProUGUI combatStatsText;
    [SerializeField] private TextMeshProUGUI combatClassText;
    [SerializeField] private TextMeshProUGUI growthTrackingText;
    [SerializeField] private TextMeshProUGUI abilitiesText;

    [Header("Rename System")]
    [SerializeField] private GameObject renamePanel;
    [SerializeField] private TMP_InputField renameInputField;
    [SerializeField] private Button renameConfirmButton;
    [SerializeField] private Button renameCancelButton;
    [SerializeField] private Button openRenameButton;

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

        // Setup rename panel
        if (renamePanel != null)
        {
            renamePanel.SetActive(false);
        }

        if (openRenameButton != null)
        {
            openRenameButton.onClick.AddListener(OpenRenamePanel);
        }

        if (renameConfirmButton != null)
        {
            renameConfirmButton.onClick.AddListener(ConfirmRename);
        }

        if (renameCancelButton != null)
        {
            renameCancelButton.onClick.AddListener(CloseRenamePanel);
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

        // Set name (use custom name if available, otherwise breed name)
        if (animalNameText != null)
        {
            animalNameText.text = animal.GetDisplayName();
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

        // Update combat stats display
        UpdateCombatStatsDisplay();

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

    private void UpdateCombatStatsDisplay()
    {
        if (currentAnimal == null) return;

        AnimalCombatStats combatStats = currentAnimal.GetCombatStats();
        if (combatStats == null) return;

        // Combat stats
        if (combatStatsText != null)
        {
            combatStatsText.text = $"Combat Stats:\n" +
                                  $"⚔️ Attack: {combatStats.CurrentAttack:F1}\n" +
                                  $"🛡️ Defense: {combatStats.CurrentDefense:F1}\n" +
                                  $"⚡ Speed: {combatStats.CurrentSpeed:F1}\n" +
                                  $"❤️ Health: {combatStats.MaxHealth:F0}\n" +
                                  $"🎯 Accuracy: {combatStats.CurrentAccuracy * 100f:F0}%\n" +
                                  $"😊 Happiness: {combatStats.happiness:F0}/100 (x{combatStats.HappinessMultiplier:F2})";
        }

        // Combat class
        if (combatClassText != null && currentAnimal.AnimalData != null)
        {
            combatClassText.text = $"Class: {currentAnimal.AnimalData.combatClass}\n" +
                                  $"Family: {currentAnimal.AnimalData.animalFamily}";
        }

        // Growth tracking
        if (growthTrackingText != null)
        {
            var (petted, fed, quality) = currentAnimal.GetGrowthTracking();
            growthTrackingText.text = $"Growth Tracking:\n" +
                                     $"• Times Petted: {petted}\n" +
                                     $"• Proper Feedings: {fed}\n" +
                                     $"• Quality Products: {quality}\n\n" +
                                     $"Growth Multipliers:\n" +
                                     $"• Attack: x{combatStats.attackGrowth:F2}\n" +
                                     $"• Defense: x{combatStats.defenseGrowth:F2}\n" +
                                     $"• Speed: x{combatStats.speedGrowth:F2}\n" +
                                     $"• Health: x{combatStats.healthGrowth:F2}";
        }

        // Skills (Active + Passive)
        if (abilitiesText != null)
        {
            abilitiesText.text = "";

            // Display active skill
            AnimalSkill activeSkill = currentAnimal.GetActiveSkill();
            if (activeSkill != null)
            {
                abilitiesText.text += $"<b>⚔️ Active Skill:</b>\n";
                abilitiesText.text += $"• {activeSkill.skillName}\n";
                abilitiesText.text += $"  {activeSkill.description}\n\n";
            }
            else
            {
                abilitiesText.text += "<b>⚔️ Active Skill:</b> None\n\n";
            }

            // Display passive skills
            List<AnimalSkill> passiveSkills = currentAnimal.GetUnlockedPassiveSkills();
            abilitiesText.text += $"<b>🛡️ Passive Skills ({passiveSkills.Count}/4):</b>\n";

            if (passiveSkills.Count > 0)
            {
                foreach (AnimalSkill skill in passiveSkills)
                {
                    abilitiesText.text += $"• {skill.skillName}\n";
                    abilitiesText.text += $"  {skill.description}\n";
                }
            }
            else
            {
                abilitiesText.text += "  No passive skills unlocked yet\n";
            }

            // Show available skills that can be unlocked
            if (currentAnimal.AnimalData != null && currentAnimal.AnimalData.availablePassiveSkills != null)
            {
                int availableCount = currentAnimal.AnimalData.availablePassiveSkills.Count;
                int lockedCount = availableCount - passiveSkills.Count;

                if (lockedCount > 0)
                {
                    abilitiesText.text += $"\n<i>🔒 {lockedCount} skill(s) available to unlock</i>";
                }
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

    // ============================================================================
    // RENAME SYSTEM
    // ============================================================================

    private void OpenRenamePanel()
    {
        if (renamePanel == null || currentAnimal == null) return;

        // Show rename panel
        renamePanel.SetActive(true);

        // Pre-fill with current name
        if (renameInputField != null)
        {
            renameInputField.text = currentAnimal.GetDisplayName();
            renameInputField.Select();
            renameInputField.ActivateInputField();
        }
    }

    private void CloseRenamePanel()
    {
        if (renamePanel != null)
        {
            renamePanel.SetActive(false);
        }
    }

    private void ConfirmRename()
    {
        if (currentAnimal == null || renameInputField == null) return;

        string newName = renameInputField.text.Trim();

        // Validate name (not empty, reasonable length)
        if (string.IsNullOrEmpty(newName))
        {
            return;
        }

        if (newName.Length > 20)
        {
            return;
        }

        // Apply the new name
        currentAnimal.SetCustomName(newName);

        // Update the display
        if (animalNameText != null)
        {
            animalNameText.text = currentAnimal.GetDisplayName();
        }


        // Close rename panel
        CloseRenamePanel();
    }
}
