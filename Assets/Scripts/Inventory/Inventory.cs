using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using SowurShield.Core;

namespace SowurShield.Inventory
{

public class Inventory : MonoBehaviour, ISaveable
{
    [Header("Inventory Settings")]
    public int inventorySize = 36; // 4 rows of 9 slots
    public int hotbarSize = 9; // First 9 slots are hotbar

    [Header("UI References")]
    public Transform slotParent; // Parent object containing all slot UI elements (DEPRECATED - use hotbarParent and storageParent)
    public GameObject slotPrefab; // Prefab for creating slots
    public ItemTooltip tooltip;
    public Transform hotbarParent; // Parent for hotbar slots (first 9 slots)
    public Transform storageParent; // Parent for storage slots (remaining 27 slots)
    public GameObject storagePanelBackground; // Wood window panel shown behind the storage grid while open

    [Header("Input Actions")]
    public InputActionReference inventoryToggleAction;
    public InputActionReference[] hotbarActions;

    [Header("Audio")]
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip useSound;

    // Inventory data - now using container system
    private InventoryContainer container;
    private List<InventorySlot> slotUIs = new List<InventorySlot>();

    // Selection and interaction
    private int selectedSlotIndex = 0;
    private InventorySlot selectedSlot;
    private bool isInventoryOpen = false;

    // Hotbar auto-refill tracking
    private Item[] lastHotbarItems; // Track last item in each hotbar slot for refill

    // Events
    public System.Action<int> OnHotbarSelectionChanged;
    public System.Action<ItemStack> OnItemUsed;
    public System.Action<ItemStack> OnItemAdded;
    public System.Action<ItemStack> OnItemRemoved;
    public System.Action<int> OnInventorySizeChanged;

    // Properties
    public ItemStack SelectedItem => selectedSlotIndex >= 0 && selectedSlotIndex < inventorySize
        ? container.GetSlot(selectedSlotIndex) : new ItemStack();
    public bool IsInventoryOpen => isInventoryOpen;
    public int SelectedSlotIndex => selectedSlotIndex;

    // Legacy compatibility method
    public Item GetSelectedItem()
    {
        ItemStack selected = SelectedItem;
        return selected.IsEmpty ? null : selected.item;
    }

    // Legacy compatibility methods for GroundItem and SoilBlockInteractable
    public bool CanAdd(Item item, int quantity = 1)
    {
        return container.CanAdd(item, quantity);
    }

    public bool Add(Item item, int quantity = 1)
    {
        return AddItem(item, quantity);
    }

    public bool Remove(Item item, int quantity = 1)
    {
        return RemoveItem(item, quantity);
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    private void Awake()
    {
        InitializeInventory();

        // Only register if this is on the Player GameObject
        if (gameObject.CompareTag("Player"))
        {
            // Register with SaveManager first (before SaveManager.Start() runs)
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RegisterSaveable(this);

            }
        }
    }

    private void Start()
    {
        SetupUI();
        SelectSlot(0); // Select first hotbar slot
        EnableInputActions();

        // Carry items across the combat round-trip (scene reload rebuilds this
        // inventory; disk saves may be unavailable — see InventorySceneSnapshot).
        // In the normal flow SceneTransitionManager restores AFTER it re-applies
        // save data (which would overwrite us) — only restore here when playing
        // the scene directly without a transition manager.
        if (SowurShield.Core.SceneTransitionManager.Instance == null)
            InventorySceneSnapshot.TryRestore(this);

        // Cache the background panel while it's still active in the scene (GameObject.Find
        // can't locate inactive objects), then hide it — the inventory starts closed.
        if (storagePanelBackground == null)
            storagePanelBackground = GameObject.Find("InventoryPanelBG");
        if (storagePanelBackground != null)
            storagePanelBackground.SetActive(false);
    }

    private void OnDestroy()
    {
        DisableInputActions();

        // Unregister from SaveManager if this was registered
        if (gameObject.CompareTag("Player") && SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }

    private void InitializeInventory()
    {
        // Initialize ItemDatabase
        var _ = ItemDatabase.Instance;

        // Create container
        container = new InventoryContainer(inventorySize, "PlayerInventory");

        // Subscribe to container events
        container.OnSlotChanged += (index, stack) => UpdateSlot(index);
        container.OnItemAdded += (item, qty) => OnItemAdded?.Invoke(new ItemStack(item, qty));
        container.OnItemRemoved += (item, qty) => OnItemRemoved?.Invoke(new ItemStack(item, qty));

        // Initialize hotbar tracking
        lastHotbarItems = new Item[hotbarSize];
    }

    private void SetupUI()
    {
        // Clear existing slots
        slotUIs.Clear();

        // NEW SYSTEM: Use hotbarParent and storageParent
        if (hotbarParent != null && storageParent != null && slotPrefab != null)
        {
            // RUNTIME FIX: Ensure GridLayoutGroups are properly configured
            EnsureGridLayoutGroup(hotbarParent, 9, TextAnchor.MiddleCenter);
            EnsureGridLayoutGroup(storageParent, 9, TextAnchor.UpperCenter);

            // Create 9 hotbar slots
            for (int i = 0; i < hotbarSize; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, hotbarParent);
                slotObj.name = $"HotbarSlot_{i}";

                InventorySlot slotUI = slotObj.GetComponent<InventorySlot>();
                if (slotUI != null)
                {
                    slotUIs.Add(slotUI);
                    slotUI.SetSlotIndex(i);
                    slotUI.SetItemStack(container.GetSlot(i));
                }
            }

            // Create 27 storage slots
            for (int i = hotbarSize; i < inventorySize; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, storageParent);
                slotObj.name = $"StorageSlot_{i}";

                InventorySlot slotUI = slotObj.GetComponent<InventorySlot>();
                if (slotUI != null)
                {
                    slotUIs.Add(slotUI);
                    slotUI.SetSlotIndex(i);
                    slotUI.SetItemStack(container.GetSlot(i));

                    // Hide storage slots initially
                    slotObj.SetActive(false);
                }
            }

        }
        // LEGACY SYSTEM: Fall back to old slotParent method if new system not set up
        else if (slotParent != null)
        {

            // Find existing slot UIs in the scene (from hotbar)
            InventorySlot[] existingSlots = slotParent.GetComponentsInChildren<InventorySlot>(true);

            // Add existing slots to our list in the correct order and assign indices
            for (int i = 0; i < existingSlots.Length; i++)
            {
                slotUIs.Add(existingSlots[i]);
                existingSlots[i].SetSlotIndex(i); // Assign proper slot index
            }

            // Create additional slots if needed (for full inventory beyond hotbar)
            int slotsNeeded = inventorySize - slotUIs.Count;
            for (int i = 0; i < slotsNeeded; i++)
            {
                CreateSlotUI(slotUIs.Count);
            }

            // Initially hide slots beyond hotbar
            for (int i = hotbarSize; i < slotUIs.Count; i++)
            {
                if (slotUIs[i] != null && slotUIs[i].gameObject != null)
                {
                    slotUIs[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
        }

        // Update all slot visuals
        UpdateAllSlots();
    }

    private void CreateSlotUI(int index)
    {
        if (slotPrefab == null || slotParent == null) return;

        GameObject slotObj = Instantiate(slotPrefab, slotParent);
        slotObj.name = $"Slot_{index}";

        InventorySlot slotUI = slotObj.GetComponent<InventorySlot>();
        if (slotUI != null)
        {
            slotUIs.Add(slotUI);
            slotUI.SetSlotIndex(index); // Assign proper slot index
            slotUI.SetItemStack(container.GetSlot(index));

            // Hide non-hotbar slots initially
            if (index >= hotbarSize)
            {
                slotObj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Ensures the parent Transform has a properly configured GridLayoutGroup
    /// This fixes the issue where StorageContainer was missing GridLayoutGroup
    /// </summary>
    private void EnsureGridLayoutGroup(Transform parent, int columns, TextAnchor alignment)
    {
        if (parent == null) return;

        GridLayoutGroup grid = parent.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = parent.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Configure grid settings to match prefab size (60x60) with tight spacing
        grid.cellSize = new Vector2(60, 60);
        grid.spacing = new Vector2(5, 5);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = alignment;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.padding = new RectOffset(0, 0, 0, 0);

    }

    // ============================================================================
    // INPUT MANAGEMENT
    // ============================================================================

    private void EnableInputActions()
    {
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.action.Enable();
            inventoryToggleAction.action.performed += OnInventoryToggle;
        }

        for (int i = 0; i < hotbarActions.Length; i++)
        {
            if (hotbarActions[i] != null)
            {
                hotbarActions[i].action.Enable();
                int slotIndex = i; // Capture index for closure
                hotbarActions[i].action.performed += (ctx) => OnHotbarSlot(slotIndex);
            }
        }
    }

    private void DisableInputActions()
    {
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.action.performed -= OnInventoryToggle;
            inventoryToggleAction.action.Disable();
        }

        for (int i = 0; i < hotbarActions.Length; i++)
        {
            if (hotbarActions[i] != null)
            {
                hotbarActions[i].action.Disable();
            }
        }
    }

    private void OnInventoryToggle(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }

    private void OnHotbarSlot(int slotIndex)
    {
        if (slotIndex < hotbarSize)
        {
            SelectSlot(slotIndex);
        }
    }

    // ============================================================================
    // INVENTORY OPERATIONS
    // ============================================================================

    public bool AddItem(Item item, int quantity = 1)
    {
        bool success = container.AddItem(item, quantity);

        // Play sound if any items were added
        if (success || container.GetItemCount(item) > 0)
        {
            PlaySound(pickupSound);
        }

        // Notify quest system so CollectItem objectives advance
        if (success && item != null)
            SowurShield.Dialogue.QuestManager.Instance?.OnInventoryItemCountChanged(
                item.itemName, container.GetItemCount(item));

        return success;
    }

    public bool RemoveItem(Item item, int quantity = 1)
    {
        return container.RemoveItem(item, quantity);
    }

    public int GetItemCount(Item item)
    {
        return container.GetItemCount(item);
    }

    public bool HasItem(Item item, int quantity = 1)
    {
        return container.HasItem(item, quantity);
    }

    public void ClearInventory()
    {
        container.ClearAll();
    }

    // ============================================================================
    // SLOT MANAGEMENT
    // ============================================================================

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;

        // Deselect previous slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlotIndex = index;

        // Select new slot
        if (index < slotUIs.Count)
        {
            selectedSlot = slotUIs[index];
            selectedSlot.SetSelected(true);
        }

        OnHotbarSelectionChanged?.Invoke(selectedSlotIndex);
    }

    public void SelectSlot(InventorySlot slotUI)
    {
        int index = slotUIs.IndexOf(slotUI);
        if (index >= 0 && index < hotbarSize)
        {
            SelectSlot(index);
        }
    }

    private void UpdateSlot(int index)
    {
        if (index >= 0 && index < slotUIs.Count && slotUIs[index] != null)
            slotUIs[index].SetItemStack(container.GetSlot(index));

        // Hotbar tracking is data, not UI. It used to sit inside the guard above, so it only
        // ran when a slot UI happened to exist — which made auto-refill silently depend on UI
        // construction order and impossible to test without a scene.
        if (index >= 0 && index < hotbarSize && lastHotbarItems != null)
        {
            ItemStack stack = container.GetSlot(index);
            lastHotbarItems[index] = stack.IsEmpty ? null : stack.item;
        }
    }

    private void UpdateAllSlots()
    {
        for (int i = 0; i < Mathf.Min(container.MaxSlots, slotUIs.Count); i++)
        {
            UpdateSlot(i);
        }
    }

    // ============================================================================
    // DRAG AND DROP OPERATIONS
    // ============================================================================

    // HandleSlotDrop was deleted in Etapa 4b: every drop now goes through
    // SlotTransferRouter -> ItemTransferService. Post-move bookkeeping that used to live
    // at the end of it (hotbar auto-refill, drop sound) is in OnSlotsChangedExternally.


    public void SplitStack(InventorySlot slotUI)
    {
        int slotIndex = slotUIs.IndexOf(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize) return;

        ItemStack stack = container.GetSlot(slotIndex);
        if (stack.IsEmpty || stack.quantity <= 1) return;

        // Find empty slot for split
        int emptySlotIndex = container.GetFirstEmptySlotIndex();
        if (emptySlotIndex == -1) return; // No empty slot available

        // Split the stack
        int splitAmount = stack.quantity / 2;
        ItemStack originalStack = stack.Clone();
        originalStack.quantity -= splitAmount;

        container.SetSlot(slotIndex, originalStack);
        container.SetSlot(emptySlotIndex, new ItemStack(stack.item, splitAmount));

        UpdateSlot(slotIndex);
        UpdateSlot(emptySlotIndex);
    }

    public void EndDragOperation()
    {
        // Called when drag operation ends without a valid drop
    }

    // ============================================================================
    // ITEM USAGE AND CONSUMPTION
    // ============================================================================

    public void UseItem(InventorySlot slotUI)
    {
        UseItemAt(slotUIs.IndexOf(slotUI));
    }

    /// <summary>
    /// Consume one item from a slot by index. Split out of <see cref="UseItem(InventorySlot)"/>
    /// so the consume-and-refill behaviour can be exercised without a slot UI.
    /// </summary>
    /// <returns>True if an item was actually consumed.</returns>
    public bool UseItemAt(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySize) return false;

        ItemStack stack = container.GetSlot(slotIndex);
        if (stack.IsEmpty || !stack.item.isConsumable) return false;

        Item usedItem = stack.item;

        // Apply the item's effect
        UseItem(usedItem);

        // Remove one from stack
        ItemStack updatedStack = stack.Clone();
        updatedStack.quantity--;
        if (updatedStack.quantity <= 0)
            updatedStack.Clear();

        container.SetSlot(slotIndex, updatedStack);

        // Refill AFTER the slot is actually empty in the container. This call used to run
        // BEFORE the write above, so CheckHotbarAutoRefill's "is this slot empty now?" guard
        // always saw the pre-consumption stack and returned immediately — hotbar auto-refill
        // never fired on the consume path (it worked on the drag path, where the call already
        // came after the writes).
        //
        // usedItem has to be handed over explicitly: the SetSlot above already fired
        // OnSlotChanged -> UpdateSlot, which wiped lastHotbarItems[slotIndex] on the way past.
        // Reading the tracking here would always find null and skip the refill.
        if (updatedStack.IsEmpty && slotIndex < hotbarSize)
            CheckHotbarAutoRefill(slotIndex, usedItem);

        UpdateSlot(slotIndex);
        PlaySound(useSound);
        OnItemUsed?.Invoke(new ItemStack(usedItem, 1));
        return true;
    }

    private void UseItem(Item item)
    {
        if (!item.isConsumable) return;

        // Handle item effects based on type
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            if (item.energyRestore > 0)
            {
                playerStats.RestoreEnergy(item.energyRestore);
            }
            if (item.healthRestore > 0)
            {
                playerStats.RestoreHealth(item.healthRestore);
            }
        }

    }

    // Method to clear a slot from the inventory array (called during drag start)
    public ItemStack ClearSlotForDrag(InventorySlot slotUI)
    {
        int slotIndex = slotUIs.IndexOf(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize)
        {
            return new ItemStack();
        }

        ItemStack removedStack = container.GetSlot(slotIndex).Clone();
        container.SetSlot(slotIndex, new ItemStack());
        UpdateSlot(slotIndex); // Update the visual slot to show it's empty
        return removedStack;
    }

    // Method to restore a slot in the inventory array (called when drag fails)
    public void RestoreSlotFromDrag(InventorySlot slotUI, ItemStack itemStack)
    {
        int slotIndex = slotUIs.IndexOf(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize)
        {
            return;
        }

        container.SetSlot(slotIndex, itemStack.Clone());
        UpdateSlot(slotIndex);
    }

    // ============================================================================
    // HOTBAR AUTO-REFILL SYSTEM
    // ============================================================================

    /// <summary>
    /// Check if a hotbar slot needs auto-refill and perform it if needed
    /// Called when a hotbar slot empties (from consumption or drag/drop)
    /// </summary>
    /// <param name="knownLastItem">
    /// What the slot held immediately before it was emptied, when the caller already knows it.
    /// Writing the emptied stack through SetSlot fires OnSlotChanged -> UpdateSlot, which clears
    /// lastHotbarItems for that slot — so a caller that empties the slot first (UseItemAt) must
    /// pass the item in, otherwise the tracking it would have read is already gone. Callers that
    /// did not empty the slot themselves (the drag path) leave this null and use the tracking.
    /// </param>
    private void CheckHotbarAutoRefill(int slotIndex, Item knownLastItem = null)
    {
        // Only refill hotbar slots
        if (slotIndex < 0 || slotIndex >= hotbarSize) return;

        ItemStack currentStack = container.GetSlot(slotIndex);

        // Only refill if slot is now empty
        if (!currentStack.IsEmpty) return;

        // Get the last item that was in this slot
        Item refillItem = knownLastItem ?? lastHotbarItems[slotIndex];
        if (refillItem == null) return;

        // Search main inventory (slots hotbarSize to end) for matching item
        for (int i = hotbarSize; i < container.MaxSlots; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty && stack.item == refillItem)
            {
                // Move entire stack to hotbar
                container.SetSlot(slotIndex, stack.Clone());
                container.SetSlot(i, new ItemStack());

                UpdateSlot(slotIndex);
                UpdateSlot(i);

                return;
            }
        }

        // No matching items found - clear the tracking
        lastHotbarItems[slotIndex] = null;
    }

    // ============================================================================
    // UI MANAGEMENT
    // ============================================================================

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        // Background window panel follows the storage grid's visibility (cached in Start).
        if (storagePanelBackground != null)
            storagePanelBackground.SetActive(isInventoryOpen);

        // Show/hide inventory panels (hotbar always visible)
        for (int i = hotbarSize; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null && slotUIs[i].gameObject != null)
            {
                slotUIs[i].gameObject.SetActive(isInventoryOpen);
            }
        }

        // For any missing slots, create them now if inventory is being opened
        if (isInventoryOpen && slotUIs.Count < inventorySize)
        {
            int slotsToCreate = inventorySize - slotUIs.Count;
            for (int i = 0; i < slotsToCreate; i++)
            {
                CreateSlotUI(slotUIs.Count);
                // Show the newly created slot
                if (slotUIs[slotUIs.Count - 1] != null)
                {
                    slotUIs[slotUIs.Count - 1].gameObject.SetActive(true);
                }
            }
        }

        // Don't pause the game - just disable player movement if inventory is open
        // You might want to disable player movement through a different mechanism
    }

    public void ShowTooltip(ItemStack itemStack, Vector3 position)
    {
        if (tooltip != null && !itemStack.IsEmpty)
        {
            tooltip.ShowTooltip(itemStack, position);
        }
    }

    public void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.HideTooltip();
        }
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    public void SortInventory()
    {
        // Get all non-empty stacks
        List<ItemStack> allStacks = container.GetAllItems();

        // Sort by item type, then by name
        allStacks.Sort((a, b) =>
        {
            int typeCompare = a.item.itemType.CompareTo(b.item.itemType);
            if (typeCompare != 0) return typeCompare;
            return string.Compare(a.item.itemName, b.item.itemName);
        });

        // Clear main inventory (keep hotbar untouched)
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            container.SetSlot(i, new ItemStack());
        }

        // Place sorted items back, starting after hotbar
        int currentIndex = hotbarSize;
        foreach (ItemStack stack in allStacks)
        {
            // Skip items that are already in hotbar
            bool inHotbar = false;
            for (int h = 0; h < hotbarSize; h++)
            {
                ItemStack hotbarStack = container.GetSlot(h);
                if (!hotbarStack.IsEmpty && hotbarStack.item == stack.item && hotbarStack.quantity == stack.quantity)
                {
                    inHotbar = true;
                    break;
                }
            }

            if (inHotbar) continue;

            if (currentIndex >= inventorySize) break;
            container.SetSlot(currentIndex, stack);
            currentIndex++;
        }

        UpdateAllSlots();
    }

    public List<ItemStack> GetAllItems()
    {
        return container.GetAllItems();
    }

    /// <summary>Total slot count (hotbar + storage) — for per-slot snapshots.</summary>
    public int SlotCount => inventorySize;

    /// <summary>The backing container, so SlotTransferRouter can move items in and out of it.</summary>
    public IInventoryContainer Container => container;

    /// <summary>Index of one of this inventory's slot UIs, or -1 if it is not ours.</summary>
    public int IndexOfSlot(InventorySlot slotUI) => slotUI == null ? -1 : slotUIs.IndexOf(slotUI);

    /// <summary>
    /// Called by SlotTransferRouter after it moved items in or out of one of our slots.
    /// Covers the bookkeeping that used to live at the end of HandleSlotDrop — the container
    /// itself already refreshed the slot UI through OnSlotChanged.
    /// </summary>
    public void OnSlotsChangedExternally(int slotIndex)
    {
        CheckHotbarAutoRefill(slotIndex);
        UpdateSlot(slotIndex);
        PlaySound(dropSound);
    }

    /// <summary>Per-index slot access, used by InventorySceneSnapshot to preserve layout.</summary>
    public ItemStack GetSlotAt(int index) => container.GetSlot(index);

    public void SetSlotAt(int index, ItemStack stack) => container.SetSlot(index, stack);

    public List<ItemStack> GetItemsByType(ItemType itemType)
    {
        return container.GetItemsByType(itemType);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    // ============================================================================
    // SAVE/LOAD SUPPORT
    // ============================================================================

    [System.Serializable]
    public class InventoryData
    {
        public ItemStackData[] items;
        public int selectedSlotIndex;

        [System.Serializable]
        public class ItemStackData
        {
            public string itemName;
            public int quantity;

            public ItemStackData() { }
            public ItemStackData(ItemStack stack)
            {
                itemName = stack.IsEmpty ? "" : stack.item.itemName;
                quantity = stack.quantity;
            }
        }
    }

    public InventoryData GetInventoryData()
    {
        InventoryData data = new InventoryData();
        data.selectedSlotIndex = selectedSlotIndex;
        data.items = new InventoryData.ItemStackData[inventorySize];

        for (int i = 0; i < inventorySize; i++)
        {
            data.items[i] = new InventoryData.ItemStackData(container.GetSlot(i));
        }

        return data;
    }

    public void LoadInventoryData(InventoryData data)
    {
        if (data == null || data.items == null) return;

        // Load items using ItemDatabase
        for (int i = 0; i < Mathf.Min(data.items.Length, inventorySize); i++)
        {
            var itemData = data.items[i];
            if (string.IsNullOrEmpty(itemData.itemName))
            {
                container.SetSlot(i, new ItemStack());
            }
            else
            {
                // Use ItemDatabase for fast lookup
                Item item = ItemDatabase.GetItem(itemData.itemName);

                if (item != null)
                {
                    container.SetSlot(i, new ItemStack(item, itemData.quantity));
                }
                else
                {
                    container.SetSlot(i, new ItemStack());
                }
            }
        }

        // Load selected slot
        selectedSlotIndex = Mathf.Clamp(data.selectedSlotIndex, 0, hotbarSize - 1);

        UpdateAllSlots();
        SelectSlot(selectedSlotIndex);
    }

    // ============================================================================
    // ISAVEABLE IMPLEMENTATION
    // ============================================================================

    public void SaveData(GameData gameData)
    {
        gameData.inventoryData.selectedSlotIndex = selectedSlotIndex;
        gameData.inventoryData.inventorySize = inventorySize;
        gameData.inventoryData.inventoryItems.Clear();

        // Save all inventory items using container
        int itemCount = 0;
        for (int i = 0; i < inventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            gameData.inventoryData.inventoryItems.Add(new InventoryGameData.ItemStackData(stack));
            if (!stack.IsEmpty)
            {
                itemCount++;
            }
        }

    }

    public void LoadData(GameData gameData)
    {
        selectedSlotIndex = gameData.inventoryData.selectedSlotIndex;

        // Clear current inventory
        container.ClearAll();

        // Load items from save data using ItemDatabase
        int foundItems = 0;
        for (int i = 0; i < Mathf.Min(gameData.inventoryData.inventoryItems.Count, inventorySize); i++)
        {
            var itemData = gameData.inventoryData.inventoryItems[i];

            if (!itemData.IsEmpty)
            {
                foundItems++;

                // Use ItemDatabase for fast lookup
                Item item = ItemDatabase.GetItem(itemData.itemName);

                if (item != null)
                {
                    container.SetSlot(i, new ItemStack(item, itemData.quantity));
                }
                else
                {
                }
            }
        }


        // IMPORTANT: Ensure inventory is closed after loading
        isInventoryOpen = false;
        for (int i = hotbarSize; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null && slotUIs[i].gameObject != null)
            {
                slotUIs[i].gameObject.SetActive(false);
            }
        }

        // Update UI and selection
        UpdateAllSlots();
        SelectSlot(Mathf.Clamp(selectedSlotIndex, 0, hotbarSize - 1));
    }

    // ============================================================================
    // SORTING & FILTERING (Phase 2)
    // ============================================================================

    /// <summary>
    /// Sort inventory using specified sort mode
    /// Sorts only the main inventory (preserves hotbar)
    /// </summary>
    public void SortInventoryBy(InventorySorting.SortMode mode, InventorySorting.SortDirection direction = InventorySorting.SortDirection.Ascending)
    {
        // Get all items from main inventory (skip hotbar)
        List<ItemStack> mainInventoryItems = new List<ItemStack>();
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty)
            {
                mainInventoryItems.Add(stack.Clone());
            }
        }

        // Sort the items
        InventorySorting.Sort(mainInventoryItems, mode, direction);

        // Clear main inventory
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            container.SetSlot(i, new ItemStack());
        }

        // Place sorted items back
        int currentIndex = hotbarSize;
        foreach (ItemStack stack in mainInventoryItems)
        {
            if (currentIndex >= inventorySize) break;
            container.SetSlot(currentIndex, stack);
            currentIndex++;
        }

        UpdateAllSlots();
    }

    /// <summary>
    /// Sort with multiple criteria
    /// </summary>
    public void SortInventoryMultiple(params (InventorySorting.SortMode mode, InventorySorting.SortDirection direction)[] criteria)
    {
        List<ItemStack> mainInventoryItems = new List<ItemStack>();
        for (int i = hotbarSize; i < inventorySize; i++)
        {
            ItemStack stack = container.GetSlot(i);
            if (!stack.IsEmpty)
            {
                mainInventoryItems.Add(stack.Clone());
            }
        }

        InventorySorting.SortMultiple(mainInventoryItems, criteria);

        for (int i = hotbarSize; i < inventorySize; i++)
        {
            container.SetSlot(i, new ItemStack());
        }

        int currentIndex = hotbarSize;
        foreach (ItemStack stack in mainInventoryItems)
        {
            if (currentIndex >= inventorySize) break;
            container.SetSlot(currentIndex, stack);
            currentIndex++;
        }

        UpdateAllSlots();
    }

    /// <summary>
    /// Get filtered view of inventory items
    /// </summary>
    public List<ItemStack> GetFilteredItems(InventoryFiltering.IInventoryFilter filter)
    {
        List<ItemStack> allItems = container.GetAllItems();
        return InventoryFiltering.Filter(allItems, filter);
    }

    /// <summary>
    /// Get items by tag (convenience method)
    /// </summary>
    public List<ItemStack> GetItemsByTagFiltered(string tag)
    {
        List<ItemStack> allItems = container.GetAllItems();
        return InventoryFiltering.FilterByTag(allItems, tag);
    }

    /// <summary>
    /// Search items by name
    /// </summary>
    public List<ItemStack> SearchItems(string searchTerm)
    {
        List<ItemStack> allItems = container.GetAllItems();
        return InventoryFiltering.SearchByName(allItems, searchTerm);
    }

    // ============================================================================
    // INVENTORY UPGRADES (Phase 2)
    // ============================================================================

    /// <summary>
    /// Upgrade inventory size (adds more slots)
    /// </summary>
    public bool UpgradeInventorySize(int additionalSlots)
    {
        if (additionalSlots <= 0)
        {
            return false;
        }

        int oldSize = inventorySize;
        int newSize = oldSize + additionalSlots;

        // Update the container size
        container.SetMaxSlots(newSize);
        inventorySize = newSize;

        // Create new UI slots for the additional space
        if (slotParent != null && slotPrefab != null)
        {
            for (int i = oldSize; i < newSize; i++)
            {
                CreateSlotUI(i);
            }
        }

        OnInventorySizeChanged?.Invoke(newSize);

        return true;
    }

    /// <summary>
    /// Set inventory size to a specific value
    /// Warning: Shrinking inventory may lose items!
    /// </summary>
    public bool SetInventorySize(int newSize)
    {
        if (newSize < hotbarSize)
        {
            return false;
        }

        int oldSize = inventorySize;
        if (newSize == oldSize) return true;

        // Warn if shrinking
        if (newSize < oldSize)
        {
            int itemsInDangerZone = 0;
            for (int i = newSize; i < oldSize; i++)
            {
                if (!container.GetSlot(i).IsEmpty)
                    itemsInDangerZone++;
            }

            if (itemsInDangerZone > 0)
            {
            }
        }

        // Update container
        container.SetMaxSlots(newSize);
        inventorySize = newSize;

        // Handle UI slots
        if (newSize > oldSize)
        {
            // Create new slots
            if (slotParent != null && slotPrefab != null)
            {
                for (int i = oldSize; i < newSize; i++)
                {
                    CreateSlotUI(i);
                }
            }
        }
        else if (newSize < oldSize)
        {
            // Remove excess slots
            for (int i = newSize; i < slotUIs.Count; i++)
            {
                if (slotUIs[i] != null && slotUIs[i].gameObject != null)
                {
                    Destroy(slotUIs[i].gameObject);
                }
            }
            slotUIs.RemoveRange(newSize, slotUIs.Count - newSize);
        }

        OnInventorySizeChanged?.Invoke(newSize);
        UpdateAllSlots();

        return true;
    }

    /// <summary>
    /// Get current inventory capacity
    /// </summary>
    public int GetInventoryCapacity()
    {
        return inventorySize;
    }

    /// <summary>
    /// Get number of used slots
    /// </summary>
    public int GetUsedSlotCount()
    {
        int count = 0;
        for (int i = 0; i < inventorySize; i++)
        {
            if (!container.GetSlot(i).IsEmpty)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Get number of empty slots
    /// </summary>
    public int GetEmptySlotCount()
    {
        return inventorySize - GetUsedSlotCount();
    }

    /// <summary>
    /// Check if inventory is full
    /// </summary>
    public bool IsFull()
    {
        return !container.HasEmptySlot();
    }
}

} // namespace SowurShield.Inventory
