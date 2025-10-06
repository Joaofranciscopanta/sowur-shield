using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Enhanced inventory UI manager with modern design features
/// Handles visual presentation, filtering, sorting UI, and animations
/// </summary>
public class InventoryUIManager : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject inventoryPanel;
    public RectTransform slotsContainer;
    public Image panelBackground;

    [Header("Header Section")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI capacityText;
    public Button closeButton;

    [Header("Tab System")]
    public GameObject tabContainer;
    public Button allTabButton;
    public Button toolsTabButton;
    public Button seedsTabButton;
    public Button foodTabButton;
    public Button resourcesTabButton;
    public Color activeTabColor = new Color(1f, 0.8f, 0.3f);
    public Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Search & Sorting")]
    public TMP_InputField searchInputField;
    public Button sortByTypeButton;
    public Button sortByNameButton;
    public Button sortByValueButton;
    public Button sortByRarityButton;
    public TextMeshProUGUI sortModeText;

    [Header("Visual Settings")]
    public Color slotNormalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    public Color slotHoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color slotSelectedColor = new Color(0.4f, 0.4f, 0.1f, 1f);
    public Color slotEmptyColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);

    [Header("Rarity Colors")]
    public Color commonColor = Color.white;
    public Color uncommonColor = Color.green;
    public Color rareColor = Color.blue;
    public Color epicColor = new Color(0.5f, 0f, 0.5f); // Purple
    public Color legendaryColor = new Color(1f, 0.5f, 0f); // Orange

    [Header("Animation Settings")]
    public float slotScaleOnHover = 1.05f;
    public float animationSpeed = 5f;

    [Header("References")]
    public Inventory inventory;
    public GameObject slotPrefab;

    // Internal state
    private ItemType currentFilterType = (ItemType)(-1); // -1 means show all
    private InventorySorting.SortMode currentSortMode = InventorySorting.SortMode.Type;
    private string currentSearchTerm = "";
    private List<EnhancedInventorySlot> enhancedSlots = new List<EnhancedInventorySlot>();

    private void Start()
    {
        SetupUI();
        SetupEventListeners();
    }

    private void SetupUI()
    {
        // Set default values
        if (titleText != null)
            titleText.text = "Inventory";

        UpdateCapacityDisplay();

        // Set default tab
        SetActiveTab(allTabButton);
    }

    private void SetupEventListeners()
    {
        // Close button
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        // Tab buttons
        if (allTabButton != null)
            allTabButton.onClick.AddListener(() => FilterByType((ItemType)(-1)));

        if (toolsTabButton != null)
            toolsTabButton.onClick.AddListener(() => FilterByType(ItemType.Tool));

        if (seedsTabButton != null)
            seedsTabButton.onClick.AddListener(() => FilterByType(ItemType.Seed));

        if (foodTabButton != null)
            foodTabButton.onClick.AddListener(() => FilterByType(ItemType.Food));

        if (resourcesTabButton != null)
            resourcesTabButton.onClick.AddListener(() => FilterByType(ItemType.Resource));

        // Sort buttons
        if (sortByTypeButton != null)
            sortByTypeButton.onClick.AddListener(() => SortInventory(InventorySorting.SortMode.Type));

        if (sortByNameButton != null)
            sortByNameButton.onClick.AddListener(() => SortInventory(InventorySorting.SortMode.Name));

        if (sortByValueButton != null)
            sortByValueButton.onClick.AddListener(() => SortInventory(InventorySorting.SortMode.Value));

        if (sortByRarityButton != null)
            sortByRarityButton.onClick.AddListener(() => SortInventory(InventorySorting.SortMode.Rarity));

        // Search field
        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(OnSearchTextChanged);

        // Subscribe to inventory events
        if (inventory != null)
        {
            inventory.OnItemAdded += (stack) => UpdateCapacityDisplay();
            inventory.OnItemRemoved += (stack) => UpdateCapacityDisplay();
            inventory.OnInventorySizeChanged += (size) => UpdateCapacityDisplay();
        }
    }

    // ============================================================================
    // FILTERING
    // ============================================================================

    private void FilterByType(ItemType type)
    {
        currentFilterType = type;

        // Update active tab visual
        if ((int)type == -1 && allTabButton != null)
            SetActiveTab(allTabButton);
        else if (type == ItemType.Tool && toolsTabButton != null)
            SetActiveTab(toolsTabButton);
        else if (type == ItemType.Seed && seedsTabButton != null)
            SetActiveTab(seedsTabButton);
        else if (type == ItemType.Food && foodTabButton != null)
            SetActiveTab(foodTabButton);
        else if (type == ItemType.Resource && resourcesTabButton != null)
            SetActiveTab(resourcesTabButton);

        ApplyFiltersAndSearch();
    }

    private void SetActiveTab(Button activeButton)
    {
        // Reset all tabs
        Button[] tabs = { allTabButton, toolsTabButton, seedsTabButton, foodTabButton, resourcesTabButton };
        foreach (var tab in tabs)
        {
            if (tab != null)
            {
                var colors = tab.colors;
                colors.normalColor = inactiveTabColor;
                tab.colors = colors;
            }
        }

        // Set active tab
        if (activeButton != null)
        {
            var colors = activeButton.colors;
            colors.normalColor = activeTabColor;
            activeButton.colors = colors;
        }
    }

    private void OnSearchTextChanged(string searchTerm)
    {
        currentSearchTerm = searchTerm;
        ApplyFiltersAndSearch();
    }

    private void ApplyFiltersAndSearch()
    {
        if (inventory == null) return;

        // Get all items
        List<ItemStack> allItems = inventory.GetAllItems();

        // Apply type filter
        if ((int)currentFilterType != -1)
        {
            allItems = InventoryFiltering.FilterByType(allItems, currentFilterType);
        }

        // Apply search filter
        if (!string.IsNullOrEmpty(currentSearchTerm))
        {
            allItems = InventoryFiltering.SearchByName(allItems, currentSearchTerm);
        }

        // Update slot visibility based on filter
        UpdateSlotVisibility(allItems);
    }

    private void UpdateSlotVisibility(List<ItemStack> visibleItems)
    {
        // This would hide/show slots based on filtering
        // For now, we'll just highlight matching items
        // Full implementation would require more complex UI management

        Debug.Log($"Filtered to {visibleItems.Count} items");
    }

    // ============================================================================
    // SORTING
    // ============================================================================

    private void SortInventory(InventorySorting.SortMode mode)
    {
        if (inventory == null) return;

        currentSortMode = mode;
        inventory.SortInventoryBy(mode);

        // Update sort mode display
        if (sortModeText != null)
            sortModeText.text = $"Sort: {mode}";

        Debug.Log($"Inventory sorted by {mode}");
    }

    // ============================================================================
    // UI UPDATES
    // ============================================================================

    public void UpdateCapacityDisplay()
    {
        if (inventory == null || capacityText == null) return;

        int used = inventory.GetUsedSlotCount();
        int total = inventory.GetInventoryCapacity();

        capacityText.text = $"{used}/{total}";

        // Color code based on fullness
        float fillPercentage = (float)used / total;
        if (fillPercentage >= 0.9f)
            capacityText.color = Color.red;
        else if (fillPercentage >= 0.7f)
            capacityText.color = Color.yellow;
        else
            capacityText.color = Color.white;
    }

    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            UpdateCapacityDisplay();
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // Clear search
        if (searchInputField != null)
            searchInputField.text = "";

        currentSearchTerm = "";
    }

    // ============================================================================
    // VISUAL HELPERS
    // ============================================================================

    public Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return commonColor;
            case ItemRarity.Uncommon:
                return uncommonColor;
            case ItemRarity.Rare:
                return rareColor;
            case ItemRarity.Epic:
                return epicColor;
            case ItemRarity.Legendary:
                return legendaryColor;
            default:
                return commonColor;
        }
    }

    public void RegisterEnhancedSlot(EnhancedInventorySlot slot)
    {
        if (!enhancedSlots.Contains(slot))
            enhancedSlots.Add(slot);
    }

    public void UnregisterEnhancedSlot(EnhancedInventorySlot slot)
    {
        enhancedSlots.Remove(slot);
    }
}

/// <summary>
/// Enhanced inventory slot with visual effects and animations
/// Attach this to each inventory slot UI element
/// </summary>
public class EnhancedInventorySlot : MonoBehaviour
{
    [Header("References")]
    public Image slotBackground;
    public Image itemIcon;
    public Image rarityBorder;
    public TextMeshProUGUI quantityText;
    public GameObject hoverHighlight;
    public GameObject selectedHighlight;

    [Header("Animation")]
    public bool animateOnHover = true;
    public float hoverScale = 1.05f;

    private InventoryUIManager uiManager;
    private InventorySlot inventorySlot;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private bool isHovered = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        inventorySlot = GetComponent<InventorySlot>();

        // Find UI manager
        uiManager = FindFirstObjectByType<InventoryUIManager>();

        if (uiManager != null)
            uiManager.RegisterEnhancedSlot(this);
    }

    private void OnDestroy()
    {
        if (uiManager != null)
            uiManager.UnregisterEnhancedSlot(this);
    }

    private void Update()
    {
        if (animateOnHover && isHovered)
        {
            // Smooth scale animation
            Vector3 targetScale = originalScale * hoverScale;
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * 10f);
        }
        else
        {
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, originalScale, Time.deltaTime * 10f);
        }
    }

    public void OnPointerEnter()
    {
        isHovered = true;

        if (hoverHighlight != null)
            hoverHighlight.SetActive(true);
    }

    public void OnPointerExit()
    {
        isHovered = false;

        if (hoverHighlight != null)
            hoverHighlight.SetActive(false);
    }

    public void UpdateVisuals(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
        {
            // Empty slot
            if (slotBackground != null)
                slotBackground.color = uiManager != null ? uiManager.slotEmptyColor : Color.gray;

            if (rarityBorder != null)
                rarityBorder.enabled = false;

            if (quantityText != null)
                quantityText.text = "";

            return;
        }

        // Has item
        if (slotBackground != null)
            slotBackground.color = uiManager != null ? uiManager.slotNormalColor : Color.white;

        // Rarity border
        if (rarityBorder != null && uiManager != null)
        {
            rarityBorder.enabled = true;
            rarityBorder.color = uiManager.GetRarityColor(stack.item.rarity);
        }

        // Quantity text
        if (quantityText != null)
        {
            if (stack.quantity > 1)
                quantityText.text = stack.quantity.ToString();
            else
                quantityText.text = "";
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected);
    }
}
