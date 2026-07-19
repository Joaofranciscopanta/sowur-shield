using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
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
    [SerializeField] private TextMeshProUGUI availableAnimalsTitleText; // "Available Animals" panel header

    [Header("Buttons")]
    [SerializeField] private Button feedAllButton;
    [SerializeField] private Button clearGridButton;
    [SerializeField] private Button startBattleButton;
    [SerializeField] private Button cancelButton;

    [Header("Scene Management")]
    [SerializeField] private string combatSceneName = "CombatScene";

    [Header("Localization")]
    [SerializeField] private LocalizedString zoneLabelText_Localized; // table "Combat", key "combat.teamassembler.zone_label"
    [SerializeField] private LocalizedString teamCountText_Localized; // table "Combat", key "combat.teamassembler.team_count"
    [SerializeField] private LocalizedString allFedText_Localized; // table "Combat", key "combat.teamassembler.all_fed"
    [SerializeField] private LocalizedString requiredFoodHeaderText_Localized; // table "Combat", key "combat.teamassembler.required_food_header"
    [SerializeField] private LocalizedString foodLineText_Localized; // table "Combat", key "combat.teamassembler.food_line"
    [SerializeField] private LocalizedString synergiesHeaderText_Localized; // table "Combat", key "combat.teamassembler.synergies_header"
    [SerializeField] private LocalizedString synergyLineText_Localized; // table "Combat", key "combat.teamassembler.synergy_line"
    [SerializeField] private LocalizedString noSynergiesText_Localized; // table "Combat", key "combat.teamassembler.no_synergies"
    [SerializeField] private LocalizedString availableAnimalsTitleText_Localized; // table "Combat", key "combat.teamassemblersetup.available_animals"

    // Runtime data
    private List<AnimalSelectionCard> animalCards = new List<AnimalSelectionCard>();
    private List<GridPositionSlot> gridSlots = new List<GridPositionSlot>();
    private List<Animal> availableAnimals = new List<Animal>();

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

    private void Start()
    {
        // If we just retreated here from a battle's "Retry" button, reopen the
        // assembler for the same stage so the player can reassemble their team.
        if (TeamAssemblerData.Instance.pendingReopenAssembler)
        {
            TeamAssemblerData.Instance.pendingReopenAssembler = false;
            OpenAssembler();
        }
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
            // GetDisplayName() resolves the localized name (with a fallback to the internal
            // stageName) — using stageName directly left the zone label half-English even
            // when the rest of the panel was localized (e.g. "Zona: Sunny Fields").
            TeamAssemblerData.Instance.zoneName = selectedStage.GetDisplayName();
            TeamAssemblerData.Instance.zoneDifficulty = selectedStage.difficulty;
        }

        // "Available Animals" panel header — was hardcoded English text directly in the
        // scene file with no LocalizeStringEvent, so it never picked up the PT/ES
        // translations already present in the string tables.
        if (availableAnimalsTitleText != null)
        {
            string localized = availableAnimalsTitleText_Localized.SafeGetLocalizedString();
            if (!string.IsNullOrEmpty(localized))
                availableAnimalsTitleText.text = localized;
        }

        if (zoneNameText != null)
        {
            zoneLabelText_Localized.Arguments = new object[] { TeamAssemblerData.Instance.zoneName };
            zoneNameText.text = zoneLabelText_Localized.SafeGetLocalizedString();
        }

        // Arrange the selection/grid/info panels before populating them
        FixPanelLayout();

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
        availableAnimals.AddRange(FindObjectsByType<Animal>(FindObjectsSortMode.None));
    }

    /// <summary>
    /// Create the grid for positioning animals
    /// </summary>
    private void SetupGrid()
    {
        if (gridContainer == null || gridSlotPrefab == null)
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
        if (animalCardContainer == null || animalCardPrefab == null)
        {
            return;
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

            if (card == null) continue;

            LayoutElement layoutElement = cardObj.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = cardObj.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 120;
                layoutElement.minHeight = 120;
            }

            card.Initialize(animal);
            animalCards.Add(card);
        }

        // Ensure the container stretches to fill the viewport width and grows downward
        if (animalCardContainer is RectTransform containerRect)
        {
            ContentSizeFitter sizeFitter = animalCardContainer.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
            {
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(0, containerRect.sizeDelta.y); // width 0 = stretch

            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
    }

    /// <summary>
    /// Refresh all animal cards and grid slot visuals (e.g. after feeding or clearing the team).
    /// </summary>
    private void RefreshAllCardsAndSlots()
    {
        foreach (AnimalSelectionCard card in animalCards)
        {
            if (card != null) card.RefreshCard();
        }

        foreach (GridPositionSlot slot in gridSlots)
        {
            if (slot != null) slot.UpdateVisuals();
        }
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
            teamCountText_Localized.Arguments = new object[] { teamSize }; // Max 15 slots on player grid
            teamSizeText.text = teamCountText_Localized.SafeGetLocalizedString();
        }

        // Update food requirements
        if (foodRequirementsText != null)
        {
            Dictionary<string, int> requirements = TeamAssemblerData.Instance.GetTotalFoodRequirements();

            if (requirements.Count == 0)
            {
                foodRequirementsText.text = allFedText_Localized.SafeGetLocalizedString();
            }
            else
            {
                string reqText = requiredFoodHeaderText_Localized.SafeGetLocalizedString();
                foreach (var req in requirements)
                {
                    foodLineText_Localized.Arguments = new object[] { req.Value, req.Key };
                    reqText += foodLineText_Localized.SafeGetLocalizedString();
                }
                foodRequirementsText.text = reqText;
            }
        }

        // Update synergies based on same-type animal stacking
        if (synergiesText != null)
        {
            Dictionary<AnimalData, int> typeCounts = new Dictionary<AnimalData, int>();
            foreach (var member in TeamAssemblerData.Instance.team)
            {
                if (member?.animalData == null) continue;

                typeCounts.TryGetValue(member.animalData, out int count);
                typeCounts[member.animalData] = count + 1;
            }

            string synergyText = synergiesHeaderText_Localized.SafeGetLocalizedString();
            bool hasSynergy = false;
            foreach (var entry in typeCounts)
            {
                AnimalData data = entry.Key;
                int count = entry.Value;

                if (data.canStack && count > 1)
                {
                    int stackCount = Mathf.Min(count, data.maxStackSize);
                    synergyLineText_Localized.Arguments = new object[] { stackCount, data.GetDisplayName() };
                    synergyText += synergyLineText_Localized.SafeGetLocalizedString();
                    hasSynergy = true;
                }
            }

            synergiesText.text = hasSynergy ? synergyText : noSynergiesText_Localized.SafeGetLocalizedString();
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
            RefreshAllCardsAndSlots();
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

        RefreshAllCardsAndSlots();
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

        RefreshAllCardsAndSlots();
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

        if (!valid)
        {
            if (teamSize == 0)
                Debug.LogWarning("[TeamAssembler] Cannot start: no animals in team.");
            else if (!allFed)
                Debug.LogWarning("[TeamAssembler] Cannot start: not all animals are fed. Use Feed All or feed manually.");
            return;
        }

        TeamAssemblerData.Instance.SaveToPrefs(); // Persist team across domain reload in builds

        // Snapshot the player inventory — the farm scene reloads on return from combat
        // and rebuilds the Inventory, which otherwise comes back empty when no disk
        // save is available (demo builds).
        SowurShield.Inventory.InventorySceneSnapshot.Capture(
            FindFirstObjectByType<SowurShield.Inventory.Inventory>());

        // Capture every ISaveable (including purchased animals) into memory before the
        // scene unloads. Without this, an animal bought from AnimalMarketUI but never
        // saved to disk has no record for AnimalPurchaseLoader to recreate it from when
        // the farm scene reloads on return from battle.
        if (SowurShield.Core.SaveManager.Instance != null)
            SowurShield.Core.SaveManager.Instance.CaptureRegisteredObjectsIntoCurrentGameData();

        Time.timeScale = 1f;
        Debug.LogWarning($"[TeamAssembler] OnStartBattleClicked — teamSize={teamSize}, " +
            $"selectedStage='{TeamAssemblerData.Instance.selectedStageName}', Time.timeScale set to {Time.timeScale}. " +
            $"Loading scene '{combatSceneName}'.");
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
