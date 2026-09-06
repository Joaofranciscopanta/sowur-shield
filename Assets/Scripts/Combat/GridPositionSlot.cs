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
        // Nothing language-dependent left on a slot: the raw grid coordinate it used to
        // print ("6,2") told the player nothing, and the front/back meaning is now shown
        // by labels on the grid itself.
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

        // A slot built in code has no position label at all. One instantiated from the old
        // scene prefab still carries its "6,2" caption, so blank it.
        if (positionText != null)
            positionText.text = "";

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

        // Check if this individual animal is already placed somewhere else on the grid.
        var existingPosition = TeamAssemblerData.Instance.FindMember(animal);
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

            // Auto-mark as fed if the animal was already fed today (via FeedingTrough or
            // manual feeding). The trough serves each animal its own dailyFoodRequirements,
            // which is its preferred food, so that counts toward the Well Fed synergy.
            if (!animal.NeedsFeeding)
            {
                TeamAssemblerData.Instance.MarkAsFed(animal, preferredFood: true);
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
        var newAnimalPosition = TeamAssemblerData.Instance.FindMember(newAnimal);

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

            var currentAnimalPosition = TeamAssemblerData.Instance.FindMember(assignedAnimal);
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
    /// Wire up references for a slot built at runtime by TeamAssemblerUI.BuildSlot.
    /// </summary>
    public void AssignBuiltReferences(Image background, Image icon, Image fed)
    {
        slotBackground = background;
        animalIcon = icon;
        fedIndicator = fed;
    }

    /// <summary>
    /// Take ownership of an animal already present in the team data, without re-adding it.
    /// Used when a saved profile repopulates the grid.
    /// </summary>
    public void AdoptAnimal(Animal animal)
    {
        assignedAnimal = animal;
        UpdateVisuals();
    }

    /// <summary>
    /// Drop this slot's visual reference without touching the team data. Used before
    /// re-applying a profile, where the team is rebuilt wholesale.
    /// </summary>
    public void ClearVisualOnly()
    {
        assignedAnimal = null;
        UpdateVisuals();
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
