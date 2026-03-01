using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using SowurShield.Animals;
using SowurShield.Core;
using SowurShield.Inventory;

namespace SowurShield.Combat
{

/// <summary>
/// Main controller for the Team Assembler pre-combat screen.
/// Manages animal selection, grid positioning, feeding, and battle start.
///
/// SETUP IN UNITY:
/// 1. Create UI Canvas with this script
/// 2. Set up panels: Animal Selection Panel, Grid Panel, Info Panel
/// 3. Assign all UI references in Inspector
/// 4. This UI appears when player enters combat trigger zone in farm
/// </summary>
public class TeamAssemblerUI : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject assemblerPanel;
    [SerializeField] private GameObject animalSelectionPanel;
    [SerializeField] private Transform animalCardContainer; // Parent for animal cards
    [SerializeField] private GameObject gridPanel;

    [Header("Grid Setup")]
    [SerializeField] private Transform gridContainer; // Parent for grid slots
    [SerializeField] private GameObject gridSlotPrefab;
    [SerializeField] private int gridWidth = 9;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private int playerColumns = 3; // Rightmost columns (6-8)

    [Header("Animal Card Prefab")]
    [SerializeField] private GameObject animalCardPrefab;

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI zoneNameText;
    [SerializeField] private TextMeshProUGUI teamSizeText;
    [SerializeField] private TextMeshProUGUI foodRequirementsText;
    [SerializeField] private TextMeshProUGUI synergiesText;

    [Header("Buttons")]
    [SerializeField] private Button feedAllButton;
    [SerializeField] private Button clearGridButton;
    [SerializeField] private Button startBattleButton;
    [SerializeField] private Button cancelButton;

    [Header("Scene Management")]
    [SerializeField] private string combatSceneName = "CombatScene";

    [Header("DEBUG: Visibility Fixes")]
    [SerializeField] private bool disableViewportMask = false; // Set to true to disable RectMask2D for testing
    [SerializeField] private bool autoExpandViewport = true; // Auto-expand viewport to fit content (RECOMMENDED)
#pragma warning disable CS0414
    [SerializeField] private bool showVisualDebugBorders = false; // Draw colored debug borders for debugging
#pragma warning restore CS0414

    // Runtime data
    private List<AnimalSelectionCard> animalCards = new List<AnimalSelectionCard>();
    private List<GridPositionSlot> gridSlots = new List<GridPositionSlot>();
    private List<Animal> availableAnimals = new List<Animal>();
    private List<GameObject> debugBorders = new List<GameObject>(); // For visual debugging

    // Singleton
    public static TeamAssemblerUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Setup buttons
        if (feedAllButton != null) feedAllButton.onClick.AddListener(OnFeedAllClicked);
        if (clearGridButton != null) clearGridButton.onClick.AddListener(OnClearGridClicked);
        if (startBattleButton != null) startBattleButton.onClick.AddListener(OnStartBattleClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);

        // Hide by default
        if (assemblerPanel != null) assemblerPanel.SetActive(false);
    }

    /// <summary>
    /// Check if the team assembler is currently open
    /// </summary>
    public bool IsOpen()
    {
        return assemblerPanel != null && assemblerPanel.activeSelf;
    }

    /// <summary>
    /// Open the team assembler UI
    /// </summary>
    public void OpenAssembler()
    {
        // Set zone info
        TeamAssemblerData.Instance.zoneName = "Forest";
        TeamAssemblerData.Instance.zoneDifficulty = 1;

        // if (zoneNameText != null)
        // {
        //     zoneNameText.text = $"Assemble Team - {this.zoneName}";
        // }

        // Find all animals in the scene
        FindAvailableAnimals();

        // Setup grid
        SetupGrid();

        // Create animal cards
        PopulateAnimalSelection();

        // Show panel
        if (assemblerPanel != null)
        {
            assemblerPanel.SetActive(true);
        }
        else
        {
        }

        // Show cursor and unlock it for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Disable player movement
        DisablePlayerMovement();

        // Update UI
        UpdateInfoDisplay();
    }

    /// <summary>
    /// Find all animals in the farm scene
    /// </summary>
    private void FindAvailableAnimals()
    {
        availableAnimals.Clear();

        // Find all Animal components in scene
        Animal[] allAnimals = FindObjectsByType<Animal>(FindObjectsSortMode.None);

        foreach (Animal animal in allAnimals)
        {
            // Only include animals that are owned by player (have custom names or are tamed)
            // For now, include all animals found
            availableAnimals.Add(animal);
        }

    }

    /// <summary>
    /// Create the grid for positioning animals
    /// </summary>
    private void SetupGrid()
    {
        if (gridContainer == null)
        {
            return;
        }

        if (gridSlotPrefab == null)
        {
            return;
        }

        // Clear existing slots
        foreach (GridPositionSlot slot in gridSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        gridSlots.Clear();

        // Create grid slots (only player side - columns 6-8)
        int playerStartColumn = gridWidth - playerColumns;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = playerStartColumn; x < gridWidth; x++)
            {
                GameObject slotObj = Instantiate(gridSlotPrefab, gridContainer);
                GridPositionSlot slot = slotObj.GetComponent<GridPositionSlot>();

                if (slot != null)
                {
                    slot.Initialize(new Vector2Int(x, y));
                    gridSlots.Add(slot);
                }
            }
        }

    }

    /// <summary>
    /// Populate animal selection panel with cards
    /// </summary>
    private void PopulateAnimalSelection()
    {
        if (animalCardContainer == null)
        {
            return;
        }

        if (animalCardPrefab == null)
        {
            return;
        }


        // CRITICAL VALIDATION: Check if container is properly configured
        if (animalCardContainer.name != "Content")
        {
        }
        else
        {
        }

        // Clear existing cards
        foreach (AnimalSelectionCard card in animalCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        animalCards.Clear();

        // Create card for each available animal
        foreach (Animal animal in availableAnimals)
        {
            GameObject cardObj = Instantiate(animalCardPrefab, animalCardContainer);
            AnimalSelectionCard card = cardObj.GetComponent<AnimalSelectionCard>();

            if (card != null)
            {
                // CRITICAL FIX: Ensure LayoutElement exists for proper sizing
                UnityEngine.UI.LayoutElement layoutElement = cardObj.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = cardObj.AddComponent<UnityEngine.UI.LayoutElement>();
                    layoutElement.preferredHeight = 120;
                    layoutElement.minHeight = 120;

                    // Force layout rebuild after adding component at runtime
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(cardObj.GetComponent<RectTransform>());
                }

                card.Initialize(animal);
                animalCards.Add(card);

                // Debug card visibility - check Canvas hierarchy
                RectTransform cardRect = cardObj.GetComponent<RectTransform>();
                Canvas parentCanvas = cardObj.GetComponentInParent<Canvas>();
                CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
                float alpha = (canvasGroup != null) ? canvasGroup.alpha : 1f;

            }
            else
            {
            }
        }


        // Force layout rebuild on container
        if (animalCardContainer != null)
        {
            RectTransform containerRect = animalCardContainer as RectTransform;
            ScrollRect scrollRect = animalCardContainer.GetComponentInParent<ScrollRect>();
            VerticalLayoutGroup layoutGroup = animalCardContainer.GetComponent<VerticalLayoutGroup>();
            ContentSizeFitter sizeFitter = animalCardContainer.GetComponent<ContentSizeFitter>();


            // CRITICAL FIX: Disable ContentSizeFitter horizontal fit if it exists
            // ContentSizeFitter with Horizontal=PreferredSize forces width to 0 when there's no preferred width set
            if (sizeFitter != null)
            {
                if (sizeFitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
                {
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
            }

            // ALWAYS set a proper width for the container (400px for animal cards)
            containerRect.sizeDelta = new Vector2(400f, containerRect.sizeDelta.y);

            // Set anchor/pivot for top-left positioning
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.pivot = new Vector2(0, 1);

            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

            // CRITICAL: Check for masking components that might be hiding the cards
            var rectMask2D = animalCardContainer.GetComponentInParent<UnityEngine.UI.RectMask2D>();
            var mask = animalCardContainer.GetComponentInParent<UnityEngine.UI.Mask>();

            if (rectMask2D != null)
            {
                RectTransform maskRect = rectMask2D.GetComponent<RectTransform>();
            }

            Vector3[] containerCorners = new Vector3[4];
            containerRect.GetWorldCorners(containerCorners);

            // CRITICAL: Check ScrollRect configuration
            if (scrollRect != null)
            {
                if (scrollRect.viewport != null)
                {
                    // Check if viewport has a RectMask2D
                    var viewportMask = scrollRect.viewport.GetComponent<UnityEngine.UI.RectMask2D>();
                    if (viewportMask != null)
                    {
                        // Check if container is actually inside viewport bounds
                        Vector3[] viewportCorners = new Vector3[4];
                        scrollRect.viewport.GetWorldCorners(viewportCorners);
                    }
                }
                else
                {
                }
            }
            else
            {
            }

            // ===================================================================
            // FIX #1: Temporarily disable RectMask2D for testing
            // ===================================================================
            if (disableViewportMask && scrollRect != null && scrollRect.viewport != null)
            {
                var viewportMask = scrollRect.viewport.GetComponent<UnityEngine.UI.RectMask2D>();
                if (viewportMask != null)
                {
                    viewportMask.enabled = false;
                }
            }

            // ===================================================================
            // FIX #2: Auto-expand viewport to fit content (PROPER SCROLLRECT)
            // ===================================================================
            if (autoExpandViewport && scrollRect != null && scrollRect.viewport != null)
            {
                RectTransform viewportRect = scrollRect.viewport;

                // Configure viewport to fill parent (proper ScrollRect setup)
                viewportRect.anchorMin = new Vector2(0, 0);
                viewportRect.anchorMax = new Vector2(1, 1);
                viewportRect.sizeDelta = Vector2.zero; // Fill parent completely
                viewportRect.anchoredPosition = Vector2.zero;


                // Verify RectMask2D is enabled for proper clipping
                var viewportMask = viewportRect.GetComponent<UnityEngine.UI.RectMask2D>();
                if (viewportMask != null)
                {
                    if (!viewportMask.enabled)
                    {
                        viewportMask.enabled = true;
                    }
                }
                else
                {
                }

                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);
            }

            // ===================================================================
            // FIX #3: Visual debug borders - DISABLED
            // ===================================================================
            // Clean up any existing debug borders from previous runs
            foreach (var border in debugBorders)
            {
                if (border != null) Destroy(border);
            }
            debugBorders.Clear();

            // Remove any debug borders that might still exist in the scene
            GameObject[] debugObjects = GameObject.FindGameObjectsWithTag("Untagged");
            foreach (GameObject obj in debugObjects)
            {
                if (obj.name.StartsWith("DEBUG_"))
                {
                    Destroy(obj);
                }
            }

            // Force all child card Images to be enabled
            foreach (Transform child in animalCardContainer)
            {
                var cardImage = child.GetComponent<UnityEngine.UI.Image>();
                if (cardImage != null && !cardImage.enabled)
                {
                    cardImage.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// Create a colored debug border around a RectTransform
    /// </summary>
    private GameObject CreateDebugBorder(RectTransform target, Color color, string name)
    {
        GameObject borderObj = new GameObject(name);
        borderObj.transform.SetParent(target, false);

        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        borderRect.anchoredPosition = Vector2.zero;

        UnityEngine.UI.Image borderImage = borderObj.AddComponent<UnityEngine.UI.Image>();
        borderImage.color = new Color(color.r, color.g, color.b, 0.3f); // Semi-transparent
        borderImage.raycastTarget = false; // Don't block clicks

        return borderObj;
    }

    /// <summary>
    /// Update info display (team size, food requirements, synergies)
    /// </summary>
    public void UpdateInfoDisplay()
    {
        // Update team size
        if (teamSizeText != null)
        {
            int teamSize = TeamAssemblerData.Instance.GetTeamSize();
            teamSizeText.text = $"Team: {teamSize}/15"; // Max 15 slots on player grid
        }

        // Update food requirements
        if (foodRequirementsText != null)
        {
            Dictionary<string, int> requirements = TeamAssemblerData.Instance.GetTotalFoodRequirements();

            if (requirements.Count == 0)
            {
                foodRequirementsText.text = "All animals fed!";
            }
            else
            {
                string reqText = "Required Food:\n";
                foreach (var req in requirements)
                {
                    reqText += $"• {req.Value}x {req.Key}\n";
                }
                foodRequirementsText.text = reqText;
            }
        }

        // Update synergies (placeholder - will expand in Phase 2)
        if (synergiesText != null)
        {
            synergiesText.text = "Synergies: TBD (Phase 2)";
        }

        // Update start battle button
        UpdateStartBattleButton();
    }

    /// <summary>
    /// Update start battle button state
    /// </summary>
    private void UpdateStartBattleButton()
    {
        if (startBattleButton != null)
        {
            bool canStart = TeamAssemblerData.Instance.IsTeamValid();
            startBattleButton.interactable = canStart;
        }
    }

    /// <summary>
    /// Feed all button clicked
    /// </summary>
    private void OnFeedAllClicked()
    {

        // Get food requirements
        Dictionary<string, int> requirements = TeamAssemblerData.Instance.GetTotalFoodRequirements();

        // Find player inventory (on Player GameObject)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        SowurShield.Inventory.Inventory playerInventory = player.GetComponent<SowurShield.Inventory.Inventory>();
        if (playerInventory == null)
        {
            return;
        }

        // Get all items from inventory to find by name
        Dictionary<string, Item> foodItems = new Dictionary<string, Item>();

        // Validate we have all required food
        bool hasAllFood = true;
        foreach (var req in requirements)
        {
            // Find item by name from all loaded items
            Item foodItem = FindItemByName(req.Key);

            if (foodItem == null)
            {
                hasAllFood = false;
                continue;
            }

            foodItems[req.Key] = foodItem;

            int count = playerInventory.GetItemCount(foodItem);
            if (count < req.Value)
            {
                hasAllFood = false;
            }
        }

        if (!hasAllFood)
        {
            // TODO: Show error message to player
            return;
        }

        // Deduct food from inventory and mark animals as fed
        foreach (var req in requirements)
        {
            if (foodItems.ContainsKey(req.Key))
            {
                playerInventory.RemoveItem(foodItems[req.Key], req.Value);
            }
        }

        // Mark all animals as fed
        foreach (var positioned in TeamAssemblerData.Instance.team)
        {
            positioned.isFed = true;
        }


        // Update UI
        UpdateInfoDisplay();
    }

    /// <summary>
    /// Find an item by name from all loaded InventoryItem ScriptableObjects
    /// </summary>
    private Item FindItemByName(string itemName)
    {
        // Load all InventoryItem assets
        Item[] allItems = Resources.LoadAll<Item>("");

        foreach (Item item in allItems)
        {
            if (item.itemName == itemName)
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// Clear grid button clicked
    /// </summary>
    private void OnClearGridClicked()
    {
        TeamAssemblerData.Instance.ClearTeam();

        // Refresh grid slots
        foreach (GridPositionSlot slot in gridSlots)
        {
            if (slot != null) slot.ClearSlot();
        }

        // Refresh animal cards
        foreach (AnimalSelectionCard card in animalCards)
        {
            if (card != null) card.RefreshCard();
        }

        UpdateInfoDisplay();

    }

    /// <summary>
    /// Start battle button clicked
    /// </summary>
    private void OnStartBattleClicked()
    {
        if (!TeamAssemblerData.Instance.IsTeamValid())
        {
            return;
        }


        // DEBUG: Check team before scene transition
        foreach (var positioned in TeamAssemblerData.Instance.team)
        {
            string animalName = positioned.GetDisplayName();
        }

        // Load combat scene
        SceneManager.LoadScene(combatSceneName);
    }

    /// <summary>
    /// Cancel button clicked
    /// </summary>
    private void OnCancelClicked()
    {
        CloseAssembler();
    }

    /// <summary>
    /// Close the team assembler UI
    /// </summary>
    public void CloseAssembler()
    {
        if (assemblerPanel != null) assemblerPanel.SetActive(false);

        // Restore cursor to game state (visible but not locked for gameplay)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Re-enable player movement
        EnablePlayerMovement();

    }

    /// <summary>
    /// Disable player movement while UI is open
    /// </summary>
    private void DisablePlayerMovement()
    {
        PlayerMove player = FindFirstObjectByType<PlayerMove>();
        if (player != null)
        {
            // Use DisableMovement() instead of disabling the component
            // This keeps the component active so it can block E key inputs
            player.DisableMovement();
        }
    }

    /// <summary>
    /// Enable player movement
    /// </summary>
    private void EnablePlayerMovement()
    {
        PlayerMove player = FindFirstObjectByType<PlayerMove>();
        if (player != null)
        {
            player.EnableMovement();
        }
    }

    /// <summary>
    /// Get grid slot at position
    /// </summary>
    public GridPositionSlot GetSlotAtPosition(Vector2Int position)
    {
        return gridSlots.Find(slot => slot.gridPosition == position);
    }
}

} // namespace SowurShield.Combat
