using System.Collections.Generic;

namespace SowurShield.Inventory
{
    /// <summary>
    /// Interface for all inventory container types
    /// Allows UI and game systems to work with any container (player inventory, chests, shops, etc.)
    /// </summary>
    public interface IInventoryContainer
    {
        /// <summary>
        /// Maximum number of slots in this container
        /// </summary>
        int MaxSlots { get; }

        /// <summary>
        /// Unique identifier for this container (used for save/load)
        /// </summary>
        string ContainerID { get; set; }

        /// <summary>
        /// Add items to the container
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="quantity">Quantity to add</param>
        /// <returns>True if all items were added, false if some couldn't fit</returns>
        bool AddItem(Item item, int quantity);

        /// <summary>
        /// Remove items from the container
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="quantity">Quantity to remove</param>
        /// <returns>True if all items were removed, false if not enough items</returns>
        bool RemoveItem(Item item, int quantity);

        /// <summary>
        /// Get the item stack at a specific slot index
        /// </summary>
        ItemStack GetSlot(int index);

        /// <summary>
        /// Set the item stack at a specific slot index
        /// </summary>
        void SetSlot(int index, ItemStack stack);

        /// <summary>
        /// Get all non-empty item stacks
        /// </summary>
        List<ItemStack> GetAllItems();

        /// <summary>
        /// Get total quantity of a specific item
        /// </summary>
        int GetItemCount(Item item);

        /// <summary>
        /// Check if container has at least the specified quantity of an item
        /// </summary>
        bool HasItem(Item item, int quantity);

        /// <summary>
        /// Clear all items from the container
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Check if the container has any empty slots
        /// </summary>
        bool HasEmptySlot();

        /// <summary>
        /// Get the first empty slot index, or -1 if none available
        /// </summary>
        int GetFirstEmptySlotIndex();
    }
} // namespace SowurShield.Inventory
