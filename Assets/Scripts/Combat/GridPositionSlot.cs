using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
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

    [Header("Colors")]
    [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    [SerializeField] private Color occupiedColor = new Color(0.5f, 1f, 0.5f, 0.7f); // Green
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.5f, 0.7f); // Yellow
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.7f); // Red

    // Grid position
    public Vector2Int gridPosition { get; private set; }

    // Assigned animal
    private Animal assignedAnimal;

    /// <summary>
    /// Initialize slot with grid position
    /// </summary>
    public void Initialize(Vector2Int position)
    {
        gridPosition = position;

        // Display position text
        if (positionText != null)
        {
            positionText.text = $"({position.x},{position.y})";
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

            // CRITICAL: Check if trying to move to same position
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
    /// Update visual appearance based on state
    /// </summary>
    private void UpdateVisuals()
    {
        if (slotBackground == null) return;

        // Set background color
        if (assignedAnimal != null)
        {
            slotBackground.color = occupiedColor;

            // Show animal icon with full opacity
            if (animalIcon != null && assignedAnimal.AnimalData != null)
            {
                animalIcon.enabled = true;
                animalIcon.sprite = assignedAnimal.AnimalData.idleSprite;
                animalIcon.color = new Color(1, 1, 1, 1); // Make fully visible
            }
        }
        else
        {
            slotBackground.color = emptyColor;

            // Hide animal icon
            if (animalIcon != null)
            {
                animalIcon.enabled = false;
                animalIcon.color = new Color(1, 1, 1, 0); // Make transparent
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
        // Highlight when dragging over
        if (eventData.pointerDrag != null && slotBackground != null)
        {
            AnimalSelectionCard draggedCard = eventData.pointerDrag.GetComponent<AnimalSelectionCard>();
            if (draggedCard != null)
            {
                // Show valid/invalid placement
                slotBackground.color = hoverColor;
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
