using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pure data container for inventory storage
/// No Unity dependencies - can be used for player inventory, chests, shops, etc.
/// Supports dynamic sizing for upgradeable inventories
/// </summary>
[System.Serializable]
public class InventoryContainer : IInventoryContainer
{
    // Data
    private ItemStack[] slots;
    private int maxSlots;
    private string containerID;

    // Events
    public System.Action<int, ItemStack> OnSlotChanged;
    public System.Action<Item, int> OnItemAdded;
    public System.Action<Item, int> OnItemRemoved;
    public System.Action<int> OnSizeChanged;

    // Properties
    public int MaxSlots => maxSlots;
    public string ContainerID
    {
        get => containerID;
        set => containerID = value;
    }

    // ============================================================================
    // INITIALIZATION
    // ============================================================================

    public InventoryContainer(int size = 36, string id = "default")
    {
        maxSlots = size;
        containerID = id;
        slots = new ItemStack[maxSlots];

        for (int i = 0; i < maxSlots; i++)
        {
            slots[i] = new ItemStack();
        }
    }

    // ============================================================================
    // CORE OPERATIONS
    // ============================================================================

    public bool AddItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        int remainingQuantity = quantity;

        // First, try to stack with existing items
        if (item.isStackable)
        {
            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                if (slots[i].CanStack(item))
                {
                    int before = slots[i].quantity;
                    remainingQuantity = slots[i].AddQuantity(remainingQuantity);

                    if (slots[i].quantity != before)
                    {
                        OnSlotChanged?.Invoke(i, slots[i]);
                    }
                }
            }
        }

        // Then, fill empty slots
        for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
        {
            if (slots[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remainingQuantity, item.maxStackSize);
                slots[i] = new ItemStack(item, toAdd);
                remainingQuantity -= toAdd;
                OnSlotChanged?.Invoke(i, slots[i]);
            }
        }

        // Trigger event if any items were added
        if (remainingQuantity < quantity)
        {
            OnItemAdded?.Invoke(item, quantity - remainingQuantity);
        }

        return remainingQuantity == 0;
    }

    public bool RemoveItem(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        int remainingToRemove = quantity;

        // Remove from slots, starting from the end
        for (int i = maxSlots - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            if (!slots[i].IsEmpty && slots[i].item == item)
            {
                int toRemove = Mathf.Min(remainingToRemove, slots[i].quantity);
                slots[i].quantity -= toRemove;
                remainingToRemove -= toRemove;

                if (slots[i].quantity <= 0)
                {
                    slots[i].Clear();
                }

                OnSlotChanged?.Invoke(i, slots[i]);
            }
        }

        bool success = remainingToRemove < quantity;
        if (success)
        {
            OnItemRemoved?.Invoke(item, quantity - remainingToRemove);
        }

        return remainingToRemove == 0;
    }

    public ItemStack GetSlot(int index)
    {
        if (index < 0 || index >= maxSlots)
        {
            return new ItemStack();
        }

        return slots[index];
    }

    public void SetSlot(int index, ItemStack stack)
    {
        if (index < 0 || index >= maxSlots)
        {
            return;
        }

        if (stack == null)
        {
            slots[index] = new ItemStack();
        }
        else
        {
            slots[index] = stack.Clone();
        }

        OnSlotChanged?.Invoke(index, slots[index]);
    }

    public List<ItemStack> GetAllItems()
    {
        List<ItemStack> items = new List<ItemStack>();
        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots[i].IsEmpty)
            {
                items.Add(slots[i].Clone());
            }
        }
        return items;
    }

    public int GetItemCount(Item item)
    {
        if (item == null) return 0;

        int count = 0;
        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots[i].IsEmpty && slots[i].item == item)
            {
                count += slots[i].quantity;
            }
        }
        return count;
    }

    public bool HasItem(Item item, int quantity = 1)
    {
        return GetItemCount(item) >= quantity;
    }

    public void ClearAll()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots[i].IsEmpty)
            {
                slots[i].Clear();
                OnSlotChanged?.Invoke(i, slots[i]);
            }
        }
    }

    public bool HasEmptySlot()
    {
        return GetFirstEmptySlotIndex() != -1;
    }

    public int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (slots[i].IsEmpty)
            {
                return i;
            }
        }
        return -1;
    }

    // ============================================================================
    // UTILITY METHODS
    // ============================================================================

    /// <summary>
    /// Check if an item can be added to the container
    /// </summary>
    public bool CanAdd(Item item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        int remainingQuantity = quantity;

        // Check if we can stack with existing items
        if (item.isStackable)
        {
            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                if (slots[i].CanStack(item))
                {
                    int canAddToSlot = Mathf.Min(remainingQuantity, slots[i].AvailableSpace);
                    remainingQuantity -= canAddToSlot;
                }
            }
        }

        // Check empty slots
        for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
        {
            if (slots[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remainingQuantity, item.maxStackSize);
                remainingQuantity -= toAdd;
            }
        }

        return remainingQuantity == 0;
    }

    /// <summary>
    /// Get all items of a specific type
    /// </summary>
    public List<ItemStack> GetItemsByType(ItemType itemType)
    {
        List<ItemStack> items = new List<ItemStack>();
        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots[i].IsEmpty && slots[i].item.itemType == itemType)
            {
                items.Add(slots[i].Clone());
            }
        }
        return items;
    }

    /// <summary>
    /// Find first slot containing a specific item
    /// </summary>
    public int FindSlotWithItem(Item item)
    {
        if (item == null) return -1;

        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots[i].IsEmpty && slots[i].item == item)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Resize the container (for upgradeable inventories)
    /// </summary>
    public void SetMaxSlots(int newSize)
    {
        if (newSize < 1)
        {
            return;
        }

        if (newSize == maxSlots) return;

        ItemStack[] newSlots = new ItemStack[newSize];

        // Copy existing items
        int copyCount = Mathf.Min(maxSlots, newSize);
        for (int i = 0; i < copyCount; i++)
        {
            newSlots[i] = slots[i].Clone();
        }

        // Initialize new slots (if expanding)
        for (int i = copyCount; i < newSize; i++)
        {
            newSlots[i] = new ItemStack();
        }

        // Warn if shrinking and losing items
        if (newSize < maxSlots)
        {
            int lostItems = 0;
            for (int i = newSize; i < maxSlots; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    lostItems++;
                }
            }
            if (lostItems > 0)
            {
            }
        }

        slots = newSlots;
        maxSlots = newSize;
        OnSizeChanged?.Invoke(newSize);

    }

    // ============================================================================
    // SAVE/LOAD SUPPORT
    // ============================================================================

    [System.Serializable]
    public class ContainerSaveData
    {
        public string containerID;
        public int maxSlots;
        public List<SlotSaveData> slots = new List<SlotSaveData>();

        [System.Serializable]
        public class SlotSaveData
        {
            public int index;
            public string itemName;
            public int quantity;

            public SlotSaveData() { }

            public SlotSaveData(int index, ItemStack stack)
            {
                this.index = index;
                this.itemName = stack.IsEmpty ? "" : stack.item.itemName;
                this.quantity = stack.quantity;
            }

            public bool IsEmpty => string.IsNullOrEmpty(itemName) || quantity <= 0;
        }
    }

    /// <summary>
    /// Get save data for this container
    /// </summary>
    public ContainerSaveData GetSaveData()
    {
        ContainerSaveData data = new ContainerSaveData();
        data.containerID = containerID;
        data.maxSlots = maxSlots;

        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots[i].IsEmpty)
            {
                data.slots.Add(new ContainerSaveData.SlotSaveData(i, slots[i]));
            }
        }

        return data;
    }

    /// <summary>
    /// Load data into this container
    /// </summary>
    public void LoadFromSaveData(ContainerSaveData data)
    {
        if (data == null)
        {
            return;
        }

        // Resize if needed
        if (data.maxSlots != maxSlots)
        {
            SetMaxSlots(data.maxSlots);
        }

        // Clear all slots first
        ClearAll();

        // Load items
        int loadedCount = 0;
        foreach (var slotData in data.slots)
        {
            if (slotData.IsEmpty) continue;
            if (slotData.index < 0 || slotData.index >= maxSlots) continue;

            Item item = ItemDatabase.GetItem(slotData.itemName);
            if (item != null)
            {
                slots[slotData.index] = new ItemStack(item, slotData.quantity);
                OnSlotChanged?.Invoke(slotData.index, slots[slotData.index]);
                loadedCount++;
            }
            else
            {
            }
        }

    }

    // ============================================================================
    // DEBUG
    // ============================================================================

    public void DebugPrint()
    {
        int itemCount = 0;
        for (int i = 0; i < maxSlots; i++)
        {
            if (!slots[i].IsEmpty)
            {
                itemCount++;
            }
        }
    }
}