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
        // Clear previous team data so animals can be placed fresh
        TeamAssemblerData.Instance.ClearTeam();

        // Set zone info from StageManager (set by StageButton.OnClick)
        StageData selectedStage = StageManager.GetSelectedStage();
        if (selectedStage != null)
        {
            TeamAssemblerData.Instance.zoneName = selectedStage.stageName;
            TeamAssemblerData.Instance.zoneDifficulty = selectedStage.difficulty;
        }

        if (zoneNameText != null)
        {
            zoneNameText.text = $"Zone: {TeamAssemblerData.Instance.zoneName}";
        }

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

        // Show cursor and unlock it for UI interaction
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Disable player movement
        DisablePlayerMovement();

        // Update UI
        UpdateInfoDisplay();
    }

    /// <summary>
    /// Fix panel layout so AnimalSelectionPanel is on the left half
    /// and GridPanel is on the right half, both filling the AssemblerPanel.
    /// </summary>
    private void FixPanelLayout()
    {
        if (assemblerPanel == null) return;

        // AssemblerPanel — full screen stretch
        SetAnchors(assemblerPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // AnimalSelectionPanel — left 42%
        if (animalSelectionPanel != null)
            SetAnchors(animalSelectionPanel, new Vector2(0f, 0f), new Vector2(0.42f, 1f),
                       new Vector2(10, 55), new Vector2(-5, -10));

        // GridPanel — middle 30% (grid slots go here)
        if (gridPanel != null)
            SetAnchors(gridPanel, new Vector2(0.42f, 0f), new Vector2(0.72f, 1f),
                       new Vector2(5, 55), new Vector2(-5, -10));

        // GridContainer — centered inside GridPanel, fixed size, no ContentSizeFitter
        if (gridContainer != null)
        {
            ContentSizeFitter csf = gridContainer.GetComponent<ContentSizeFitter>();
            if (csf != null) csf.enabled = false;

            RectTransform r = gridContainer as RectTransform;
            if (r != null)
            {
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                // Size set later by SetupGrid after we know slot count
            }
        }

        // InfoPanel — right 28%
        if (gridPanel != null)
        {
            // Find InfoPanel as sibling of gridPanel
            Transform info = assemblerPanel.transform.Find("InfoPanel");
            if (info != null)
                SetAnchors(info.gameObject, new Vector2(0.72f, 0f), new Vector2(1f, 1f),
                           new Vector2(5, 55), new Vector2(-10, -10));
        }
    }

    private void SetAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform r = go.GetComponent<RectTransform>();
        if (r == null) return;
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
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

        // Ensure GridContainer has a Grid Layout Group
        GridLayoutGroup glg = gridContainer.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(80, 80);
        glg.spacing = new Vector2(5, 5);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperLeft;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = playerColumns; // 3 columns

        // Resize GridContainer to fit all slots
        RectTransform containerRect = gridContainer as RectTransform;
        if (containerRect != null)
        {
            float totalWidth = playerColumns * 80 + (playerColumns - 1) * 5;
            float totalHeight = gridHeight * 80 + (gridHeight - 1) * 5;
            containerRect.sizeDelta = new Vector2(totalWidth, totalHeight);
        }

        // Create grid slots (only player side - columns 6-8)
        // Player columns are the rightmost ones: from (gridWidth - playerColumns) to (gridWidth - 1)
        int playerStartColumn = gridWidth - playerColumns;

        for (int y = gridHeight - 1; y >= 0; y--) // top to bottom so Grid Layout Group rows are correct
        {
            for (int x = playerStartColumn; x < gridWidth; x++)
            {
                GameObject slotObj = Instantiate(gridSlotPrefab, gridContainer);
                slotObj.name = $"Slot_{x}_{y}";

                // Ensure LayoutElement so Grid Layout Group sizes the slot correctly
                LayoutElement le = slotObj.GetComponent<LayoutElement>();
                if (le == null) le = slotObj.AddComponent<LayoutElement>();
                le.preferredWidth = 80;
                le.preferredHeight = 80;
                le.minWidth = 80;
                le.minHeight = 80;

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

            // Content stretches full width of viewport, grows downward
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, containerRect.sizeDelta.y); // width 0 = stretch

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
        if (TeamAssemblerData.Instance.team.Count == 0)
        {
            Debug.LogWarning("[TeamAssembler] No animals in team — drag animals to the grid first.");
            return;
        }

        // Get food requirements for unfed animals in team
        Dictionary<string, int> requirements = TeamAssemblerData.Instance.GetTotalFoodRequirements();

        // If no food needed (all already fed), just update UI
        if (requirements.Count == 0)
        {
            foreach (var positioned in TeamAssemblerData.Instance.team)
                positioned.isFed = true;
            UpdateInfoDisplay();
            return;
        }

        // Find player inventory
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[TeamAssembler] Player not found.");
            return;
        }

        SowurShield.Inventory.Inventory playerInventory = player.GetComponent<SowurShield.Inventory.Inventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("[TeamAssembler] Player has no Inventory component.");
            return;
        }

        // Resolve item references and validate stock
        Dictionary<string, Item> foodItems = new Dictionary<string, Item>();
        bool hasAllFood = true;

        foreach (var req in requirements)
        {
            Item foodItem = FindItemByName(req.Key);
            if (foodItem == null)
            {
                Debug.LogWarning($"[TeamAssembler] Item not found: '{req.Key}'. Check that itemName in AnimalData matches exactly.");
                hasAllFood = false;
                continue;
            }

            int count = playerInventory.GetItemCount(foodItem);
            if (count < req.Value)
            {
                Debug.LogWarning($"[TeamAssembler] Not enough '{req.Key}': need {req.Value}, have {count}.");
                hasAllFood = false;
            }
            else
            {
                foodItems[req.Key] = foodItem;
            }
        }

        if (!hasAllFood)
            return;

        // Deduct food and mark all as fed
        foreach (var req in requirements)
        {
            if (foodItems.ContainsKey(req.Key))
                playerInventory.RemoveItem(foodItems[req.Key], req.Value);
        }

        foreach (var positioned in TeamAssemblerData.Instance.team)
            positioned.isFed = true;

        UpdateInfoDisplay();
    }

    /// <summary>
    /// Find an item by name from all loaded InventoryItem ScriptableObjects
    /// </summary>
    private Item FindItemByName(string itemName)
    {
        // Search all known resource subfolders
        string[] searchPaths = new string[]
        {
            "FarmingData/Seeds",
            "FarmingData/Crops",
            "Items",
            "FarmingData",
            ""
        };

        foreach (string path in searchPaths)
        {
            Item[] items = Resources.LoadAll<Item>(path);
            foreach (Item item in items)
            {
                if (item.itemName == itemName)
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
        int teamSize = TeamAssemblerData.Instance.team.Count;
        bool allFed  = TeamAssemblerData.Instance.AreAllAnimalsFed();
        bool valid   = TeamAssemblerData.Instance.IsTeamValid();

        Debug.Log($"[TeamAssembler] Start Battle clicked — team={teamSize}, allFed={allFed}, valid={valid}");

        foreach (var p in TeamAssemblerData.Instance.team)
            Debug.Log($"  • {p.GetDisplayName()} at {p.gridPosition}, isFed={p.isFed}, animalData={p.animalData?.animalName ?? "NULL"}");

        if (!valid)
        {
            if (teamSize == 0)
                Debug.LogWarning("[TeamAssembler] Cannot start: no animals in team.");
            else if (!allFed)
                Debug.LogWarning("[TeamAssembler] Cannot start: not all animals are fed. Use Feed All or feed manually.");
            return;
        }

        Debug.Log($"[TeamAssembler] Loading scene '{combatSceneName}'...");
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
