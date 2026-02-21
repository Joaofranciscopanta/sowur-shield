using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

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

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 1f, 0.5f); // Yellow tint
    [SerializeField] private Color inTeamColor = new Color(0.5f, 1f, 0.5f); // Green tint

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


        // Set name
        if (nameText != null)
        {
            nameText.text = animal.GetDisplayName();
        }
        else
        {
        }

        // Set portrait
        if (animalPortrait != null && animal.AnimalData != null)
        {
            animalPortrait.sprite = animal.AnimalData.idleSprite;
        }
        else
        {
        }

        // Set happiness
        if (happinessText != null)
        {
            AnimalCombatStats stats = animal.GetCombatStats();
            if (stats != null)
            {
                happinessText.text = $"❤️ {stats.happiness:F0}%";
            }
        }

        // Set food status
        if (foodStatusText != null)
        {
            if (TeamAssemblerData.Instance.IsAnimalInTeam(animal))
            {
                var positioned = TeamAssemblerData.Instance.team.Find(pa => pa.animalData == animal.AnimalData);
                if (positioned != null && positioned.isFed)
                {
                    foodStatusText.text = "Fed ✓";
                    foodStatusText.color = Color.green;
                }
                else
                {
                    foodStatusText.text = GetFoodRequirementText();
                    foodStatusText.color = Color.yellow;
                }
            }
            else
            {
                foodStatusText.text = GetFoodRequirementText();
                foodStatusText.color = Color.white;
            }
        }

        // Update background color
        isInTeam = TeamAssemblerData.Instance.IsAnimalInTeam(animal);
        UpdateBackgroundColor();

        // CRITICAL FIX: Ensure cardBackground is visible
        if (cardBackground != null)
        {
            if (cardBackground.sprite == null)
            {

                // Create a larger white texture for better compatibility
                Texture2D tex = new Texture2D(32, 32);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
                tex.Apply();

                cardBackground.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
                cardBackground.type = UnityEngine.UI.Image.Type.Simple; // Use Simple, not Sliced

            }

            // Ensure Image is enabled and has correct settings
            cardBackground.enabled = true;
            cardBackground.raycastTarget = true;

        }
        else
        {
        }

        // Debug visibility settings
        string imageDebug = "";
        if (cardBackground != null)
        {
            imageDebug += $"Background(Color={cardBackground.color}, Enabled={cardBackground.enabled}, Sprite={cardBackground.sprite != null}), ";
        }
        if (animalPortrait != null)
        {
            imageDebug += $"Portrait(Color={animalPortrait.color}, Enabled={animalPortrait.enabled}, Sprite={animalPortrait.sprite != null}), ";
        }

        // Check card world position to see if it's being clipped
        // Get RectTransform directly in case Awake() hasn't run yet
        RectTransform rt = rectTransform != null ? rectTransform : GetComponent<RectTransform>();
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
        }
    }

    /// <summary>
    /// Get food requirement text for this animal
    /// </summary>
    private string GetFoodRequirementText()
    {
        if (animal.AnimalData == null || animal.AnimalData.dailyFoodRequirements.Count == 0)
        {
            return "No food needed";
        }

        string text = "Need: ";
        foreach (FoodRequirement req in animal.AnimalData.dailyFoodRequirements)
        {
            text += $"{req.quantityPerDay}x {req.itemName} ";
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
            cardBackground.color = selectedColor;
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
