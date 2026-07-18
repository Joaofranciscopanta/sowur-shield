using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Animals;
using SowurShield.Inventory;

namespace SowurShield.Combat
{

/// <summary>
/// UI card representing an available animal for combat.
/// Supports drag-and-drop to grid positioning slots.
///
/// SETUP IN UNITY:
/// 1. Create UI panel with Image, TextMeshPro components
/// 2. Add this script
/// 3. Assign UI references
/// 4. Add EventSystem to scene for drag-drop
/// </summary>
public class AnimalSelectionCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private Image animalPortrait;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI happinessText;
    [SerializeField] private TextMeshProUGUI foodStatusText;
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image foodStatusIcon;
    [SerializeField] private Image happinessFillBar;

    [Header("Localization")]
    [SerializeField] private LocalizedString happinessText_Localized; // table "Combat", key "combat.selection.happiness"
    [SerializeField] private LocalizedString fedText_Localized; // table "Combat", key "combat.selection.fed"
    [SerializeField] private LocalizedString noFoodNeededText_Localized; // table "Combat", key "combat.selection.no_food_needed"
    [SerializeField] private LocalizedString needsText_Localized; // table "Combat", key "combat.selection.needs"
    [SerializeField] private LocalizedString foodLineText_Localized; // table "Combat", key "combat.selection.food_line"

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.18f, 0.18f, 0.22f, 0.95f);
    [SerializeField] private Color hoverColor = new Color(0.28f, 0.28f, 0.36f, 0.95f);
    [SerializeField] private Color inTeamColor = new Color(0.16f, 0.32f, 0.2f, 0.95f);

    [Header("Food Status Colors")]
    [SerializeField] private Color fedColor = new Color(0.35f, 0.85f, 0.4f);
    [SerializeField] private Color hungryColor = new Color(0.95f, 0.75f, 0.2f);
    [SerializeField] private Color notInTeamColor = new Color(0.6f, 0.6f, 0.6f);

    [Header("Happiness Bar Colors")]
    [SerializeField] private Color happinessLowColor = new Color(0.85f, 0.35f, 0.3f);
    [SerializeField] private Color happinessMidColor = new Color(0.95f, 0.75f, 0.2f);
    [SerializeField] private Color happinessHighColor = new Color(0.35f, 0.85f, 0.4f);

    // Animal data
    private Animal animal;
    private bool isInTeam = false;

    // Drag data
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        // Add canvas group if not present (for drag transparency)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        SowurShield.Core.LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        SowurShield.Core.LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(UnityEngine.Localization.Locale locale)
    {
        if (animal != null)
            RefreshCard();
    }

    /// <summary>
    /// Initialize card with animal data
    /// </summary>
    public void Initialize(Animal animalData)
    {
        animal = animalData;

        if (animal == null)
        {
            return;
        }

        RefreshCard();
    }

    /// <summary>
    /// Refresh card display
    /// </summary>
    public void RefreshCard()
    {
        if (animal == null)
        {
            return;
        }

        if (nameText != null)
        {
            nameText.text = animal.GetDisplayName();
        }

        if (animalPortrait != null && animal.AnimalData != null)
        {
            animalPortrait.sprite = animal.AnimalData.idleSprite;
        }

        AnimalCombatStats stats = animal.GetCombatStats();
        float happinessPercent = stats != null ? stats.happiness : 0f;

        if (happinessText != null)
        {
            happinessText_Localized.Arguments = new object[] { happinessPercent };
            happinessText.text = happinessText_Localized.SafeGetLocalizedString();
        }

        UpdateHappinessBar(happinessPercent);

        isInTeam = TeamAssemblerData.Instance.IsAnimalInTeam(animal);
        UpdateFoodStatus();
        UpdateBackgroundColor();
    }

    /// <summary>
    /// Update the happiness fill bar's width and color based on percent (0-100).
    /// </summary>
    private void UpdateHappinessBar(float happinessPercent)
    {
        if (happinessFillBar == null) return;

        float t = Mathf.Clamp01(happinessPercent / 100f);
        happinessFillBar.fillAmount = t;

        happinessFillBar.color = t < 0.34f ? happinessLowColor
            : t < 0.67f ? happinessMidColor
            : happinessHighColor;
    }

    /// <summary>
    /// Update the food status icon/text: green check when fed, yellow warning with
    /// requirement text when in-team but hungry, neutral when not yet on the team.
    /// </summary>
    private void UpdateFoodStatus()
    {
        if (foodStatusText == null && foodStatusIcon == null) return;

        Color statusColor;
        string statusText;

        if (isInTeam)
        {
            var positioned = TeamAssemblerData.Instance.team.Find(pa => pa.animalData == animal.AnimalData);
            bool fed = positioned != null && positioned.isFed;

            statusColor = fed ? fedColor : hungryColor;
            statusText = fed ? fedText_Localized.SafeGetLocalizedString() : GetFoodRequirementText();
        }
        else
        {
            statusColor = notInTeamColor;
            statusText = GetFoodRequirementText();
        }

        if (foodStatusText != null)
        {
            foodStatusText.text = statusText;
            foodStatusText.color = statusColor;
        }

        if (foodStatusIcon != null)
        {
            foodStatusIcon.color = statusColor;
        }
    }

    /// <summary>
    /// Get food requirement text for this animal
    /// </summary>
    private string GetFoodRequirementText()
    {
        if (animal.AnimalData == null || animal.AnimalData.dailyFoodRequirements.Count == 0)
        {
            return noFoodNeededText_Localized.SafeGetLocalizedString();
        }

        string text = needsText_Localized.SafeGetLocalizedString();
        foreach (FoodRequirement req in animal.AnimalData.dailyFoodRequirements)
        {
            Item foodItem = ItemDatabase.GetItem(req.itemName);
            foodLineText_Localized.Arguments = new object[] { req.quantityPerDay, foodItem != null ? foodItem.GetDisplayName() : req.itemName };
            text += foodLineText_Localized.SafeGetLocalizedString();
        }

        return text.Trim();
    }

    /// <summary>
    /// Update background color based on state
    /// </summary>
    private void UpdateBackgroundColor()
    {
        if (cardBackground != null)
        {
            cardBackground.color = isInTeam ? inTeamColor : normalColor;
        }
    }

    // ============================================================================
    // DRAG AND DROP INTERFACE
    // ============================================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (animal == null) return;

        // Store original position and parent
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        // Make card transparent during drag
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }

        // Move to canvas root so it renders on top
        transform.SetParent(canvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (animal == null || canvas == null) return;

        // Move card with mouse
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (animal == null)
        {
            return;
        }

        // Restore transparency
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // Check if dropped on a grid slot
        GridPositionSlot targetSlot = GetSlotUnderMouse(eventData);

        if (targetSlot != null)
        {
            // Try to place animal in grid
            bool success = targetSlot.PlaceAnimal(animal);

            if (success)
            {
                RefreshCard();
            }
            else
            {
                // Return to original position if placement failed
                ReturnToOriginalPosition();
            }
        }
        else
        {
            // Check if dropped on grid to remove from team
            if (isInTeam && IsOverGrid(eventData))
            {
                TeamAssemblerData.Instance.RemoveAnimal(animal);
                RefreshCard();

                // Update grid
                if (TeamAssemblerUI.Instance != null)
                {
                    TeamAssemblerUI.Instance.UpdateInfoDisplay();
                }
            }

            // Return to original position
            ReturnToOriginalPosition();
        }
    }

    /// <summary>
    /// Return card to original position
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = originalPosition;
    }

    /// <summary>
    /// Get grid slot under mouse cursor
    /// </summary>
    private GridPositionSlot GetSlotUnderMouse(PointerEventData eventData)
    {
        // Raycast to find object under cursor
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            GridPositionSlot slot = result.gameObject.GetComponent<GridPositionSlot>();
            if (slot != null)
            {
                return slot;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if mouse is over grid panel
    /// </summary>
    private bool IsOverGrid(PointerEventData eventData)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.name.Contains("Grid") || result.gameObject.name.Contains("Slot"))
            {
                return true;
            }
        }

        return false;
    }

    // ============================================================================
    // HOVER EFFECTS
    // ============================================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cardBackground != null && !isInTeam)
        {
            cardBackground.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateBackgroundColor();
    }

    /// <summary>
    /// Get the animal this card represents
    /// </summary>
    public Animal GetAnimal()
    {
        return animal;
    }
}

} // namespace SowurShield.Combat
