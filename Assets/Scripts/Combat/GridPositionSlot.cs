using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Localization;
using SowurShield.Core;
using SowurShield.Animals;

namespace SowurShield.Combat
{

/// <summary>
/// Represents a grid cell in the Team Assembler where animals can be positioned.
/// Supports drag-and-drop placement and visual feedback.
///
/// SETUP IN UNITY:
/// 1. Create UI Image (grid cell background)
/// 2. Add this script
/// 3. Optionally add TextMeshPro for position display
/// </summary>
public class GridPositionSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image animalIcon;
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private Image fedIndicator;

    [Header("Colors")]
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.08f);
    [SerializeField] private Color occupiedColor = new Color(0.35f, 0.85f, 0.4f, 0.35f);
    [SerializeField] private Color occupiedHungryColor = new Color(0.95f, 0.75f, 0.2f, 0.35f);
    [SerializeField] private Color hoverValidColor = new Color(0.35f, 0.85f, 0.4f, 0.5f);

    [Header("Fed Indicator Colors")]
    [SerializeField] private Color fedColor = new Color(0.35f, 0.85f, 0.4f);
    [SerializeField] private Color hungryColor = new Color(0.95f, 0.75f, 0.2f);

    [Header("Localization")]
    [SerializeField] private LocalizedString positionLabelText_Localized; // table "Combat", key "combat.grid.position_label"

    // Grid position
    public Vector2Int gridPosition { get; private set; }

    // Assigned animal
    private Animal assignedAnimal;

    private void Awake()
    {
        LocalizationManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDestroy()
    {
        LocalizationManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(Locale locale)
    {
        if (positionText == null) return;
        positionLabelText_Localized.Arguments = new object[] { gridPosition.x, gridPosition.y };
        positionText.text = positionLabelText_Localized.SafeGetLocalizedString();
    }

    /// <summary>
    /// Initialize slot with grid position
    /// </summary>
    public void Initialize(Vector2Int position)
    {
        gridPosition = position;

        // Auto-find slotBackground if not assigned in Inspector
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        // Auto-find animalIcon in children if not assigned
        if (animalIcon == null)
        {
            foreach (Transform child in transform)
            {
                Image img = child.GetComponent<Image>();
                if (img != null) { animalIcon = img; break; }
            }
        }

        // Display position text
        if (positionText != null)
        {
            positionLabelText_Localized.Arguments = new object[] { position.x, position.y };
            positionText.text = positionLabelText_Localized.SafeGetLocalizedString();
        }

        // Set default color
        UpdateVisuals();
    }

    /// <summary>
    /// Place an animal in this slot
    /// </summary>
    public bool PlaceAnimal(Animal animal)
    {
        if (animal == null)
        {
            return false;
        }

        // Check if slot is already occupied
        if (assignedAnimal != null)
        {
            // Try to swap positions
            return SwapAnimals(animal);
        }

        // Check if animal is already in team at another position
        var existingPosition = TeamAssemblerData.Instance.team.Find(pa => pa.animalData == animal.AnimalData);
        if (existingPosition != null)
        {
            TeamAssemblerData.Instance.RemoveAnimal(animal);

            // Clear the old slot's visual
            if (TeamAssemblerUI.Instance != null)
            {
                GridPositionSlot oldSlot = TeamAssemblerUI.Instance.GetSlotAtPosition(existingPosition.gridPosition);
                if (oldSlot != null)
                {
                    oldSlot.assignedAnimal = null;
                    oldSlot.UpdateVisuals();
                }
            }
        }

        // Add animal to team data
        bool success = TeamAssemblerData.Instance.AddAnimal(animal, gridPosition);

        if (success)
        {
            assignedAnimal = animal;

            // Auto-mark as fed if the animal was already fed today (via FeedingTrough or manual feeding)
            if (!animal.NeedsFeeding)
            {
                TeamAssemblerData.Instance.MarkAsFed(animal);
            }

            UpdateVisuals();

            // Update UI
            if (TeamAssemblerUI.Instance != null)
            {
                TeamAssemblerUI.Instance.UpdateInfoDisplay();
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Swap this slot's animal with another
    /// </summary>
    private bool SwapAnimals(Animal newAnimal)
    {
        if (assignedAnimal == null)
        {
            return PlaceAnimal(newAnimal);
        }

        // Get the position of the new animal (if it's already in team)
        var newAnimalPosition = TeamAssemblerData.Instance.team.Find(pa => pa.animalData == newAnimal.AnimalData);

        if (newAnimalPosition != null)
        {
            // Both animals are in team - swap positions
            Vector2Int oldPosition = newAnimalPosition.gridPosition;

            // Trying to move to the same position is a no-op
            if (oldPosition == gridPosition)
            {
                return false;
            }

            newAnimalPosition.gridPosition = gridPosition;

            var currentAnimalPosition = TeamAssemblerData.Instance.team.Find(pa => pa.animalData == assignedAnimal.AnimalData);
            if (currentAnimalPosition != null)
            {
                currentAnimalPosition.gridPosition = oldPosition;
            }

            // Update slot visuals
            Animal temp = assignedAnimal;
            assignedAnimal = newAnimal;

            // Update the other slot
            if (TeamAssemblerUI.Instance != null)
            {
                GridPositionSlot otherSlot = TeamAssemblerUI.Instance.GetSlotAtPosition(oldPosition);
                if (otherSlot != null)
                {
                    otherSlot.assignedAnimal = temp;
                    otherSlot.UpdateVisuals();
                }
            }

            UpdateVisuals();

            // Update UI
            if (TeamAssemblerUI.Instance != null)
            {
                TeamAssemblerUI.Instance.UpdateInfoDisplay();
            }

            return true;
        }
        else
        {
            // New animal not in team yet - replace current animal
            TeamAssemblerData.Instance.RemoveAnimal(assignedAnimal);
            assignedAnimal = null;
            return PlaceAnimal(newAnimal);
        }
    }

    /// <summary>
    /// Remove animal from this slot
    /// </summary>
    public void ClearSlot()
    {
        if (assignedAnimal != null)
        {
            TeamAssemblerData.Instance.RemoveAnimal(assignedAnimal);
            assignedAnimal = null;
            UpdateVisuals();
        }
    }

    /// <summary>
    /// Update visual appearance based on state: empty, occupied+fed, or occupied+hungry.
    /// The fed indicator (if assigned) shows a small colored dot on occupied slots.
    /// </summary>
    public void UpdateVisuals()
    {
        if (slotBackground == null) return;

        if (assignedAnimal != null)
        {
            var positioned = TeamAssemblerData.Instance.team.Find(pa => pa.animalData == assignedAnimal.AnimalData);
            bool fed = positioned != null && positioned.isFed;

            slotBackground.color = fed ? occupiedColor : occupiedHungryColor;

            // Show animal icon with full opacity
            if (animalIcon != null && assignedAnimal.AnimalData != null)
            {
                animalIcon.gameObject.SetActive(true);
                animalIcon.enabled = true;
                animalIcon.sprite = assignedAnimal.AnimalData.idleSprite;
                animalIcon.color = Color.white;
            }

            if (fedIndicator != null)
            {
                fedIndicator.enabled = true;
                fedIndicator.color = fed ? fedColor : hungryColor;
            }
        }
        else
        {
            slotBackground.color = emptyColor;

            // Hide animal icon
            if (animalIcon != null)
            {
                animalIcon.enabled = false;
                animalIcon.color = new Color(1f, 1f, 1f, 0f);
                animalIcon.gameObject.SetActive(false);
            }

            if (fedIndicator != null)
            {
                fedIndicator.enabled = false;
            }
        }
    }

    // ============================================================================
    // DRAG AND DROP INTERFACE
    // ============================================================================

    public void OnDrop(PointerEventData eventData)
    {
        // Get dragged animal card
        AnimalSelectionCard draggedCard = eventData.pointerDrag?.GetComponent<AnimalSelectionCard>();

        if (draggedCard != null)
        {
            Animal animal = draggedCard.GetAnimal();
            PlaceAnimal(animal);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Highlight when dragging a card over this slot
        if (eventData.pointerDrag != null && slotBackground != null)
        {
            AnimalSelectionCard draggedCard = eventData.pointerDrag.GetComponent<AnimalSelectionCard>();
            if (draggedCard != null)
            {
                // Empty slots (or slots that would swap) are valid drop targets
                slotBackground.color = hoverValidColor;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Restore original color
        UpdateVisuals();
    }

    /// <summary>
    /// Get the animal assigned to this slot
    /// </summary>
    public Animal GetAssignedAnimal()
    {
        return assignedAnimal;
    }
}

} // namespace SowurShield.Combat
