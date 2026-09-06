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

    /// <summary>The card_animal art itself, tinted to show team membership.</summary>
    [SerializeField] private Image cardFrame;

    // The serialized LocalizedString fields that used to live here are gone: the card is
    // built in code now, so they were always empty and resolving one threw a
    // NullReferenceException that took the whole list build down with it. Captions come
    // from Localize()/FormatHappiness() below, which construct the LocalizedString at
    // call time.

    // These tint `cardBackground`, which is NOT what the player sees: a CardBackgroundFrame
    // child draws the card_animal sprite on top, and that art has a CREAM interior. So the
    // card reads light no matter what is set here, and every caption on it must be dark ink.
    //
    // That is the whole bug. The name and happiness lines were cream — 1.0:1 against the art
    // behind them, i.e. invisible — while the food line was dark and perfectly readable. Two
    // text colours on one surface, one of which could never work.
    //
    // Worth stating plainly for the next reader: darkening these three values does nothing
    // visible. It was tried; the sprite covers them.
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.36f, 0.23f, 0.12f, 1f);
    [SerializeField] private Color hoverColor = new Color(0.45f, 0.29f, 0.15f, 1f);
    [SerializeField] private Color inTeamColor = new Color(0.15f, 0.32f, 0.18f, 1f);

    // Dark variants, because the card art they sit on is cream. The stock brighter greens and
    // ambers were chosen for a dark card and measure 1.3-1.4:1 on this one.
    [Header("Food Status Colors")]
    [SerializeField] private Color fedColor = new Color(0.15f, 0.45f, 0.20f);
    [SerializeField] private Color hungryColor = new Color(0.60f, 0.36f, 0.02f);
    [SerializeField] private Color notInTeamColor = new Color(0.35f, 0.33f, 0.30f);

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
    /// Wire up the references for a card built at runtime by TeamAssemblerUI.BuildCard.
    ///
    /// Note there is no cardBackground: the old card had a dark brown Image on the root
    /// under the cream card art, and because the art paints only part of the rect, the
    /// brown showed through as a band down every row. The frame art is the background.
    /// </summary>
    public void AssignBuiltReferences(Image frame, Image portrait, TextMeshProUGUI name,
        TextMeshProUGUI happiness, Image happinessBar, TextMeshProUGUI foodStatus)
    {
        cardFrame = frame;
        animalPortrait = portrait;
        nameText = name;
        happinessText = happiness;
        happinessFillBar = happinessBar;
        foodStatusText = foodStatus;
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
            happinessText.text = FormatHappiness(happinessPercent);
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
            // By individual animal, not by species — see PositionedAnimal.animalId.
            var positioned = TeamAssemblerData.Instance.FindMember(animal);
            bool fed = positioned != null && positioned.isFed;

            statusColor = fed ? fedColor : hungryColor;
            statusText = fed ? Localize("combat.selection.fed", "Fed") : GetFoodRequirementText();
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
    /// Happiness caption.
    ///
    /// Built from a runtime LocalizedString rather than the serialized
    /// happinessText_Localized field: this card is now created in code, so every
    /// [SerializeField] LocalizedString on it is empty and resolving one threw.
    /// </summary>
    private static string FormatHappiness(float percent)
    {
        var localized = new LocalizedString("Combat", "combat.selection.happiness");
        string text = localized.SafeGetLocalizedString(percent);
        return string.IsNullOrEmpty(text) ? $"Happiness: {percent:F0}%" : text;
    }

    /// <summary>Resolve a Combat-table key with an English fallback.</summary>
    private static string Localize(string key, string fallback)
    {
        string value = new LocalizedString("Combat", key).SafeGetLocalizedString();
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    /// <summary>
    /// Get food requirement text for this animal
    /// </summary>
    private string GetFoodRequirementText()
    {
        if (animal.AnimalData == null || animal.AnimalData.dailyFoodRequirements.Count == 0)
        {
            return Localize("combat.selection.no_food_needed", "No food needed");
        }

        // Show the favourite food, which is the one that earns the bonus. Before this,
        // every card listed the same CarrotSeed and feeding carried no decision at all.
        string preferred = FoodPreference.GetPreferredFood(animal.AnimalData);
        if (!string.IsNullOrEmpty(preferred))
        {
            Item preferredItem = ItemDatabase.GetItem(preferred);
            string name = preferredItem != null ? preferredItem.GetDisplayName() : preferred;
            return $"♥ {name}";
        }

        string text = Localize("combat.selection.needs", "Needs:");
        foreach (FoodRequirement req in animal.AnimalData.dailyFoodRequirements)
        {
            Item foodItem = ItemDatabase.GetItem(req.itemName);
            string itemName = foodItem != null ? foodItem.GetDisplayName() : req.itemName;
            text += $" {req.quantityPerDay}x {itemName}";
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

        // Tint the card art itself. A light green wash reads as "on the team" without
        // hiding the artwork the way an opaque background did.
        if (cardFrame != null)
        {
            cardFrame.color = isInTeam
                ? new Color(0.80f, 0.95f, 0.80f)
                : Color.white;
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

        if (cardFrame != null && !isInTeam)
        {
            cardFrame.color = new Color(1f, 0.97f, 0.88f);
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
