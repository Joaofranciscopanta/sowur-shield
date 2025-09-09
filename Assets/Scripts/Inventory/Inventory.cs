using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class Inventory : MonoBehaviour, ISaveable
{
    [Header("Inventory Settings")]
    public int inventorySize = 36; // 4 rows of 9 slots
    public int hotbarSize = 9; // First 9 slots are hotbar

    [Header("UI References")]
    public Transform slotParent; // Parent object containing all slot UI elements
    public GameObject slotPrefab; // Prefab for creating slots
    public ItemTooltip tooltip;
    public Transform hotbarParent; // Separate parent for hotbar if needed

    [Header("Input Actions")]
    public InputActionReference inventoryToggleAction;
    public InputActionReference[] hotbarActions;

    [Header("Audio")]
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip useSound;

    // Inventory data
    private ItemStack[] inventory;
    private List<InventorySlot> slotUIs = new List<InventorySlot>();

    // Selection and interaction
    private int selectedSlotIndex = 0;
    private InventorySlot selectedSlot;
    private bool isInventoryOpen = false;

    // Drag operation tracking
    private ItemStack draggedStack;
    private InventorySlot draggedFromSlot;

    // Events
    public System.Action<int> OnHotbarSelectionChanged;
    public System.Action<ItemStack> OnItemUsed;
    public System.Action<ItemStack> OnItemAdded;
    public System.Action<ItemStack> OnItemRemoved;

    // Properties
    public ItemStack SelectedItem => selectedSlotIndex >= 0 && selectedSlotIndex < inventory.Length
        ? inventory[selectedSlotIndex] : new ItemStack();
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
        if (item == null || quantity <= 0) return false;

        int remainingQuantity = quantity;

        // Check if we can stack with existing items
        if (item.isStackable)
        {
            for (int i = 0; i < inventorySize && remainingQuantity > 0; i++)
            {
                if (inventory[i].CanStack(item))
                {
                    int canAddToSlot = Mathf.Min(remainingQuantity, inventory[i].AvailableSpace);
                    remainingQuantity -= canAddToSlot;
                }
            }
        }

        // Check empty slots
        for (int i = 0; i < inventorySize && remainingQuantity > 0; i++)
        {
            if (inventory[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remainingQuantity, item.maxStackSize);
                remainingQuantity -= toAdd;
            }
        }

        return remainingQuantity == 0;
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
    }

    private void Start()
    {
        SetupUI();
        SelectSlot(0); // Select first hotbar slot
        EnableInputActions();
        
        // Register with SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
    }

    private void OnDestroy()
    {
        DisableInputActions();
        
        // Unregister from SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.UnregisterSaveable(this);
        }
    }

    private void InitializeInventory()
    {
        inventory = new ItemStack[inventorySize];
        for (int i = 0; i < inventorySize; i++)
        {
            inventory[i] = new ItemStack();
        }
    }

    private void SetupUI()
    {
        // Clear existing slots
        slotUIs.Clear();

        if (slotParent != null)
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
        }

        // Initially hide slots beyond hotbar
        for (int i = hotbarSize; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null && slotUIs[i].gameObject != null)
            {
                slotUIs[i].gameObject.SetActive(false);
            }
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
            slotUI.SetItemStack(inventory[index]);

            // Hide non-hotbar slots initially
            if (index >= hotbarSize)
            {
                slotObj.SetActive(false);
            }
        }
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
        if (item == null || quantity <= 0) return false;

        int remainingQuantity = quantity;

        // First, try to stack with existing items
        if (item.isStackable)
        {
            for (int i = 0; i < inventorySize && remainingQuantity > 0; i++)
            {
                if (inventory[i].CanStack(item))
                {
                    remainingQuantity = inventory[i].AddQuantity(remainingQuantity);
                    UpdateSlot(i);
                }
            }
        }

        // Then, fill empty slots
        for (int i = 0; i < inventorySize && remainingQuantity > 0; i++)
        {
            if (inventory[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remainingQuantity, item.maxStackSize);
                inventory[i] = new ItemStack(item, toAdd);
                remainingQuantity -= toAdd;
                UpdateSlot(i);
            }
        }

        // Play sound and trigger event if any items were added
        if (remainingQuantity < quantity)
        {
            PlaySound(pickupSound);
            OnItemAdded?.Invoke(new ItemStack(item, quantity - remainingQuantity));
        }

        return remainingQuantity == 0;
    }

    public bool RemoveItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        int remainingToRemove = quantity;

        for (int i = inventorySize - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            if (!inventory[i].IsEmpty && inventory[i].item == item)
            {
                int toRemove = Mathf.Min(remainingToRemove, inventory[i].quantity);
                inventory[i].quantity -= toRemove;
                remainingToRemove -= toRemove;

                if (inventory[i].quantity <= 0)
                {
                    inventory[i].Clear();
                }

                UpdateSlot(i);
            }
        }

        bool success = remainingToRemove < quantity;
        if (success)
        {
            OnItemRemoved?.Invoke(new ItemStack(item, quantity - remainingToRemove));
        }

        return remainingToRemove == 0;
    }

    public int GetItemCount(Item item)
    {
        if (item == null) return 0;

        int count = 0;
        for (int i = 0; i < inventorySize; i++)
        {
            if (!inventory[i].IsEmpty && inventory[i].item == item)
            {
                count += inventory[i].quantity;
            }
        }
        return count;
    }

    public bool HasItem(Item item, int quantity = 1)
    {
        return GetItemCount(item) >= quantity;
    }

    public void ClearInventory()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            inventory[i].Clear();
            UpdateSlot(i);
        }
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
        {
            slotUIs[index].SetItemStack(inventory[index]);
        }
    }

    private void UpdateAllSlots()
    {
        for (int i = 0; i < Mathf.Min(inventory.Length, slotUIs.Count); i++)
        {
            UpdateSlot(i);
        }
    }

    // ============================================================================
    // DRAG AND DROP OPERATIONS
    // ============================================================================

    public void HandleSlotDrop(InventorySlot fromSlot, InventorySlot toSlot)
    {
        int fromIndex = slotUIs.IndexOf(fromSlot);
        int toIndex = slotUIs.IndexOf(toSlot);

        if (fromIndex < 0 || toIndex < 0 || fromIndex >= inventorySize || toIndex >= inventorySize)
            return;

        // Get the dragged item instead of the (now empty) slot
        ItemStack fromStack = fromSlot.GetDraggedItem();
        ItemStack toStack = inventory[toIndex];
        
        if (fromStack == null || fromStack.IsEmpty)
        {
            return;
        }

        // Handle different drop scenarios
        if (toStack.IsEmpty)
        {
            // Move entire stack to empty slot
            inventory[toIndex] = fromStack.Clone();
            // Don't clear fromIndex - it's already cleared by the drag system
            
            // Consume the dragged item
            fromSlot.ConsumeDraggedItem();
        }
        else if (toStack.CanStack(fromStack.item))
        {
            // Stack compatible items
            int leftover = toStack.AddQuantity(fromStack.quantity);
            if (leftover == 0)
            {
                // All items were stacked - consume the dragged item
                fromSlot.ConsumeDraggedItem();
            }
            else
            {
                // Some items left over - restore leftover to fromSlot
                ItemStack leftoverStack = new ItemStack(fromStack.item, leftover);
                inventory[fromIndex] = leftoverStack; // Put leftover in inventory array
                fromSlot.MarkDragSuccessful(); // Mark as successful (partial consumption)
            }
        }
        else
        {
            // Swap stacks - put toStack in fromSlot, fromStack in toSlot
            inventory[fromIndex] = toStack.Clone();
            inventory[toIndex] = fromStack.Clone();
            
            // Consume the dragged item since swap succeeded
            fromSlot.ConsumeDraggedItem();
        }

        UpdateSlot(fromIndex);
        UpdateSlot(toIndex);
        PlaySound(dropSound);
    }

    public void SplitStack(InventorySlot slotUI)
    {
        int slotIndex = slotUIs.IndexOf(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize) return;

        ItemStack stack = inventory[slotIndex];
        if (stack.IsEmpty || stack.quantity <= 1) return;

        // Find empty slot for split
        int emptySlotIndex = -1;
        for (int i = 0; i < inventorySize; i++)
        {
            if (inventory[i].IsEmpty)
            {
                emptySlotIndex = i;
                break;
            }
        }

        if (emptySlotIndex == -1) return; // No empty slot available

        // Split the stack
        int splitAmount = stack.quantity / 2;
        inventory[emptySlotIndex] = new ItemStack(stack.item, splitAmount);
        inventory[slotIndex].quantity -= splitAmount;

        UpdateSlot(slotIndex);
        UpdateSlot(emptySlotIndex);
    }

    public void EndDragOperation()
    {
        // Called when drag operation ends without a valid drop
        // Reset any temporary states if needed
        draggedStack = null;
        draggedFromSlot = null;
    }

    // ============================================================================
    // ITEM USAGE AND CONSUMPTION
    // ============================================================================

    public void UseItem(InventorySlot slotUI)
    {
        int slotIndex = slotUIs.IndexOf(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize) return;

        ItemStack stack = inventory[slotIndex];
        if (stack.IsEmpty || !stack.item.isConsumable) return;

        // Use the item
        UseItem(stack.item);

        // Remove one from stack
        stack.quantity--;
        if (stack.quantity <= 0)
        {
            stack.Clear();
        }

        UpdateSlot(slotIndex);
        PlaySound(useSound);
        OnItemUsed?.Invoke(new ItemStack(stack.item, 1));
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

        ItemStack removedStack = inventory[slotIndex].Clone();
        inventory[slotIndex].Clear();
        UpdateSlot(slotIndex); // Update the visual slot to show it's empty
        return removedStack;
    }

    // Method to restore a slot in the inventory array (called when drag fails)
    public void RestoreSlotFromDrag(InventorySlot slotUI, ItemStack itemStack)
    {
        int slotIndex = slotUIs.IndexOf(slotUI);
        if (slotIndex < 0 || slotIndex >= inventorySize) 
        {
            Debug.LogWarning($"RestoreSlotFromDrag: Invalid slot index {slotIndex}");
            return;
        }

        inventory[slotIndex] = itemStack.Clone();
        UpdateSlot(slotIndex);
        Debug.Log($"RestoreSlotFromDrag: Restored {itemStack.quantity}x {itemStack.item?.itemName} to inventory slot {slotIndex}");
    }

    // ============================================================================
    // UI MANAGEMENT
    // ============================================================================

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

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
        // Create a list of non-empty stacks with their indices
        List<(ItemStack stack, int originalIndex)> nonEmptyStacks = new List<(ItemStack, int)>();

        for (int i = 0; i < inventorySize; i++)
        {
            if (!inventory[i].IsEmpty)
            {
                nonEmptyStacks.Add((inventory[i].Clone(), i));
            }
        }

        // Sort by item type, then by name
        nonEmptyStacks.Sort((a, b) =>
        {
            int typeCompare = a.stack.item.itemType.CompareTo(b.stack.item.itemType);
            if (typeCompare != 0) return typeCompare;
            return string.Compare(a.stack.item.itemName, b.stack.item.itemName);
        });

        // Clear inventory
        for (int i = 0; i < inventorySize; i++)
        {
            inventory[i].Clear();
        }

        // Place sorted items back, starting from hotbar end
        int currentIndex = hotbarSize;
        foreach (var (stack, _) in nonEmptyStacks)
        {
            if (currentIndex >= inventorySize) break;
            inventory[currentIndex] = stack;
            currentIndex++;
        }

        UpdateAllSlots();
    }

    public List<ItemStack> GetAllItems()
    {
        List<ItemStack> items = new List<ItemStack>();
        for (int i = 0; i < inventorySize; i++)
        {
            if (!inventory[i].IsEmpty)
            {
                items.Add(inventory[i].Clone());
            }
        }
        return items;
    }

    public List<ItemStack> GetItemsByType(ItemType itemType)
    {
        List<ItemStack> items = new List<ItemStack>();
        for (int i = 0; i < inventorySize; i++)
        {
            if (!inventory[i].IsEmpty && inventory[i].item.itemType == itemType)
            {
                items.Add(inventory[i].Clone());
            }
        }
        return items;
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
            data.items[i] = new InventoryData.ItemStackData(inventory[i]);
        }

        return data;
    }

    public void LoadInventoryData(InventoryData data)
    {
        if (data == null || data.items == null) return;

        // Load items
        for (int i = 0; i < Mathf.Min(data.items.Length, inventorySize); i++)
        {
            var itemData = data.items[i];
            if (string.IsNullOrEmpty(itemData.itemName))
            {
                inventory[i].Clear();
            }
            else
            {
                // Find item by name (you might want to use a more robust system)
                Item item = Resources.LoadAll<Item>("Items")
                    .FirstOrDefault(x => x.itemName == itemData.itemName);

                if (item != null)
                {
                    inventory[i] = new ItemStack(item, itemData.quantity);
                }
                else
                {
                    inventory[i].Clear();
                    Debug.LogWarning($"Could not find item: {itemData.itemName}");
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
        
        // Save all inventory items
        for (int i = 0; i < inventorySize; i++)
        {
            gameData.inventoryData.inventoryItems.Add(new InventoryGameData.ItemStackData(inventory[i]));
        }
        
        Debug.Log($"[Inventory] Saved inventory data with {inventory.Count(item => !item.IsEmpty)} items");
    }
    
    public void LoadData(GameData gameData)
    {
        selectedSlotIndex = gameData.inventoryData.selectedSlotIndex;
        
        // Ensure inventory size matches
        if (gameData.inventoryData.inventorySize != inventorySize)
        {
            Debug.LogWarning($"[Inventory] Save file inventory size ({gameData.inventoryData.inventorySize}) doesn't match current size ({inventorySize})");
        }
        
        // Clear current inventory
        for (int i = 0; i < inventorySize; i++)
        {
            inventory[i].Clear();
        }
        
        // Load items from save data
        for (int i = 0; i < Mathf.Min(gameData.inventoryData.inventoryItems.Count, inventorySize); i++)
        {
            var itemData = gameData.inventoryData.inventoryItems[i];
            if (!itemData.IsEmpty)
            {
                // Find item by name
                Item item = Resources.LoadAll<Item>("Items")
                    .FirstOrDefault(x => x.itemName == itemData.itemName);
                    
                if (item != null)
                {
                    inventory[i] = new ItemStack(item, itemData.quantity);
                }
                else
                {
                    Debug.LogWarning($"[Inventory] Could not find item: {itemData.itemName}");
                }
            }
        }
        
        // Update UI and selection
        UpdateAllSlots();
        SelectSlot(Mathf.Clamp(selectedSlotIndex, 0, hotbarSize - 1));
        
        int loadedItems = inventory.Count(item => !item.IsEmpty);
        Debug.Log($"[Inventory] Loaded inventory data with {loadedItems} items");
    }
}
